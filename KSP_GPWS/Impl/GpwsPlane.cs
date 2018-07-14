// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP_GPWS.Interfaces;
using KSP_GPWS.SimpleTypes;

namespace KSP_GPWS.Impl
{
    public class GpwsPlane : IPlaneConfig, IBasicGpwsFunction
    {
        private IGpwsCommonData CommonData = null;

        /// <summary>
        /// parts with module "GPWSGear"
        /// </summary>
        private List<Part> gears = new List<Part>();

        private bool isGearDown = false;

        /// <summary>
        /// max RA when just takeoff
        /// </summary>
        private float heightJustTakeoff = 0.0f;

        private Queue<float> lastRadarAlt = new Queue<float>();
        private Queue<float> lastRadarAlt_time = new Queue<float>();
        private const int lastRadarAlt_Count = 3;

        /// <summary>
        /// in m/s or ft/s
        /// </summary>
        private float VerSpeedRA
        {
            get
            {
                if (lastRadarAlt.Count < lastRadarAlt_Count * 2 || lastRadarAlt_time.Count < lastRadarAlt_Count * 2)
                {
                    return 0.0f;
                }
                else
                {
                    float t0 = 0, tf = 0, h0 = 0, hf = 0;
                    for (int i = 0; i < lastRadarAlt_Count; i++)
                    {
                        h0 += lastRadarAlt.ElementAt(i);
                        hf += lastRadarAlt.ElementAt(lastRadarAlt_Count + i);
                        t0 += lastRadarAlt_time.ElementAt(i);
                        tf += lastRadarAlt_time.ElementAt(lastRadarAlt_Count + i);
                    }
                    return (hf - h0) / (tf - t0);
                }
            }
        }

        // curves
        private FloatCurve sinkRateCurve = new FloatCurve();            // (alt, vSpeed)
        private FloatCurve sinkRatePullUpCurve = new FloatCurve();      // (alt, vSpeed)
        private FloatCurve terrainCurve = new FloatCurve();             // (radar alt, vSpeed)
        private FloatCurve terrainPullUpCurve = new FloatCurve();       // (radar alt, vSpeed)
        private FloatCurve terrainBCurve = new FloatCurve();            // (radar alt, vSpeed)
        private FloatCurve terrainPullUpBCurve = new FloatCurve();      // (radar alt, vSpeed)
        private FloatCurve dontSinkCurve = new FloatCurve();            // (radar alt, RA loss)
        private FloatCurve tooLowTerrainCurve = new FloatCurve();       // (radar alt, vSpeed)
        private FloatCurve bankAngleCurve = new FloatCurve();           // (radar alt, bankAngle)

        private bool exitClosureToTerrainWarning = false;

        public int GearCount
        {
            get
            {
                return gears.Count;
            }
        }

        #region IPlaneConfig Members

        public bool EnableSystem { get; set; }
        public bool EnableDescentRate { get; set; }
        public bool EnableClosureToTerrain { get; set; }
        public bool EnableAltitudeLoss { get; set; }
        public bool EnableTerrainClearance { get; set; }
        public bool EnableAltitudeCallouts { get; set; }
        public bool EnableRetard { get; set; }
        public bool EnableBankAngle { get; set; }
        public bool EnableTraffic { get; set; }
        public bool EnableV1 { get; set; }
        public bool EnableRotate { get; set; }
        public bool EnableGearUp { get; set; }
        public bool EnableStall { get; set; }
        public bool EnableStallShake { get; set; }

        public float DescentRateFactor { get; set; }
        public float TooLowGearAltitude { get; set; }
        public float V1Speed { get; set; }
        public float TakeOffSpeed { get; set; }
        public float LandingSpeed { get; set; }
        public float StallAoa { get; set; }
        public int[] AltitudeArray { get; set; }

        /// <summary>
        /// use meters or feet, feet is recommanded.
        /// </summary>
        public UnitOfAltitude UnitOfAltitude { get; set; }

        #endregion

        #region IConfigNode Members

        public void Load(ConfigNode node)
        {
            EnableSystem = Util.ConvertValue<bool>(node, "EnableSystem", EnableSystem);
            EnableDescentRate = Util.ConvertValue<bool>(node, "EnableDescentRate", EnableDescentRate);
            EnableClosureToTerrain = Util.ConvertValue<bool>(node, "EnableClosureToTerrain", EnableClosureToTerrain);
            EnableAltitudeLoss = Util.ConvertValue<bool>(node, "EnableAltitudeLoss", EnableAltitudeLoss);
            EnableTerrainClearance = Util.ConvertValue<bool>(node, "EnableTerrainClearance", EnableTerrainClearance);
            EnableAltitudeCallouts = Util.ConvertValue<bool>(node, "EnableAltitudeCallouts", EnableAltitudeCallouts);
            EnableRetard = Util.ConvertValue<bool>(node, "EnableRetard", EnableRetard);
            EnableBankAngle = Util.ConvertValue<bool>(node, "EnableBankAngle", EnableBankAngle);
            EnableTraffic = Util.ConvertValue<bool>(node, "EnableTraffic", EnableTraffic);
            EnableV1 = Util.ConvertValue<bool>(node, "EnableV1", EnableV1);
            EnableRotate = Util.ConvertValue<bool>(node, "EnableRotate", EnableRotate);
            EnableGearUp = Util.ConvertValue<bool>(node, "EnableGearUp", EnableGearUp);
            EnableStall = Util.ConvertValue<bool>(node, "EnableStall", EnableStall);
            EnableStallShake = Util.ConvertValue<bool>(node, "EnableStallShake", EnableStallShake);

            DescentRateFactor = Util.ConvertValue<float>(node, "DescentRateFactor", DescentRateFactor);
            TooLowGearAltitude = Util.ConvertValue<float>(node, "TooLowGearAltitude", TooLowGearAltitude);
            V1Speed = Util.ConvertValue<float>(node, "V1Speed", V1Speed);
            TakeOffSpeed = Util.ConvertValue<float>(node, "TakeOffSpeed", TakeOffSpeed);
            LandingSpeed = Util.ConvertValue<float>(node, "LandingSpeed", LandingSpeed);
            StallAoa = Util.ConvertValue<float>(node, "StallAoa", StallAoa);
            if (node.HasValue("AltitudeArray"))
            {
                String[] intstrings = node.GetValue("AltitudeArray").Split(',');
                if (intstrings.Length > 0)
                {
                    int id = 0;
                    int[] tempAlt = new int[intstrings.Length];
                    for (int j = 0; j < intstrings.Length; j++)
                    {
                        if (int.TryParse(intstrings[j], out tempAlt[id]))
                        {
                            id++;
                        }
                    }
                    AltitudeArray = new int[id];
                    for (int j = 0; j < id; j++)
                    {
                        AltitudeArray[j] = tempAlt[j];
                    }
                }
            }
            UnitOfAltitude = Util.ConvertValue<UnitOfAltitude>(node, "UnitOfAltitude", UnitOfAltitude);
            // check legality
            CheckConfigLegality();
        }

        public void Save(ConfigNode node)
        {
            node.name = "Plane";
            node.AddValue("EnableSystem", EnableSystem);
            node.AddValue("EnableDescentRate", EnableDescentRate);
            node.AddValue("EnableClosureToTerrain", EnableClosureToTerrain);
            node.AddValue("EnableAltitudeLoss", EnableAltitudeLoss);
            node.AddValue("EnableTerrainClearance", EnableTerrainClearance);
            node.AddValue("EnableAltitudeCallouts", EnableAltitudeCallouts);
            node.AddValue("EnableRetard", EnableRetard);
            node.AddValue("EnableBankAngle", EnableBankAngle);
            node.AddValue("EnableTraffic", EnableTraffic);
            node.AddValue("EnableV1", EnableV1);
            node.AddValue("EnableRotate", EnableRotate);
            node.AddValue("EnableGearUp", EnableGearUp);
            node.AddValue("EnableStall", EnableStall);
            node.AddValue("EnableStallShake", EnableStallShake);

            node.AddValue("DescentRateFactor", DescentRateFactor);
            node.AddValue("TooLowGearAltitude", TooLowGearAltitude);
            node.AddValue("V1Speed", V1Speed);
            node.AddValue("TakeOffSpeed", TakeOffSpeed);
            node.AddValue("LandingSpeed", LandingSpeed);
            node.AddValue("StallAoa", StallAoa);
            node.AddValue("AltitudeArray", String.Join(",", Array.ConvertAll(AltitudeArray, x => x.ToString())));
            node.AddValue("UnitOfAltitude", UnitOfAltitude.ToString());
        }

        #endregion

        public void CheckConfigLegality()
        {
            DescentRateFactor = Math.Max(DescentRateFactor, 0.1f);
            DescentRateFactor = Math.Min(DescentRateFactor, 10.0f);
        }

        public GpwsPlane()
        {
            InitializeConfig();
        }

        public void InitializeConfig()
        {
            EnableSystem = true;
            EnableDescentRate = true;
            EnableClosureToTerrain = true;
            EnableAltitudeLoss = true;
            EnableTerrainClearance = true;
            EnableAltitudeCallouts = true;
            EnableRetard = true;
            EnableBankAngle = false;
            EnableTraffic = true;
            EnableV1 = false;
            EnableRotate = false;
            EnableGearUp = true;
            EnableStall = true;
            EnableStallShake = true;

            DescentRateFactor = 1.0f;
            TooLowGearAltitude = 500.0f;
            V1Speed = 45.0f;
            TakeOffSpeed = 60.0f;
            LandingSpeed = 60.0f;
            StallAoa = 20.0f;
            AltitudeArray = new int[] { 1000, 500, 400, 300, 200, 100, 50, 40, 30, 20, 10 };
            UnitOfAltitude = UnitOfAltitude.FOOT;
        }

        public void Initialize(IGpwsCommonData data)
        {
            CommonData = data;

            isGearDown = false;
            heightJustTakeoff = 0.0f;
            exitClosureToTerrainWarning = false;

            lastRadarAlt.Clear();
            lastRadarAlt_time.Clear();

            initializeCurves();
        }

        private void initializeCurves()
        {
            // init curves, points are not accurate
            sinkRateCurve.Add(50, -1000);
            sinkRateCurve.Add(2500, -5000);
            sinkRatePullUpCurve.Add(50, -1500);
            sinkRatePullUpCurve.Add(100, -1600);
            sinkRatePullUpCurve.Add(2500, -7000);

            terrainCurve.Add(0, -4000);
            terrainCurve.Add(1400, -4600);
            terrainCurve.Add(1900, -7500);
            terrainCurve.Add(2100, -10000);
            terrainPullUpCurve.Add(0, -1500);
            terrainPullUpCurve.Add(1200, -3400);
            terrainPullUpCurve.Add(1350, -4000);
            terrainPullUpCurve.Add(1600, -6000);
            terrainBCurve.Add(0, -2000);
            terrainBCurve.Add(800, -2900);
            terrainPullUpBCurve.Add(0, -2400);
            terrainPullUpBCurve.Add(750, -3100);

            dontSinkCurve.Add(0, -0.1f);
            dontSinkCurve.Add(1500, -150);

            tooLowTerrainCurve.Add(0, 1.0f);
            tooLowTerrainCurve.Add(1.2f, 1.0f);
            tooLowTerrainCurve.Add(1.5f, 2.0f);

            bankAngleCurve.Add(5, 10);
            bankAngleCurve.Add(30, 10);
            bankAngleCurve.Add(150, 40);
            bankAngleCurve.Add(2450, 55);
        }

        public bool PreUpdate()
        {
            // check vessel state
            if (CommonData.ActiveVessel.state != Vessel.State.ACTIVE)
            {
                Util.audio.SetUnavailable();
                return false;
            }
            // check gear
            if (gears.Count <= 0)
            {
                Util.audio.SetUnavailable();
                return false;
            }

            // check atmosphere
            if (!FlightGlobals.getMainBody().atmosphere || CommonData.ActiveVessel.atmDensity <= 0)
            {
                Util.audio.SetUnavailable();
                return false;
            }

            // on surface
            if (CommonData.ActiveVessel.LandedOrSplashed)
            {
                heightJustTakeoff = 0.0f;
            }

            if (lastRadarAlt_time.Count == 0 || CommonData.CurrentTime - lastRadarAlt_time.ElementAt(0) > 0.2f)
            {
                lastRadarAlt.Enqueue(CommonData.RadarAltitude);
                lastRadarAlt_time.Enqueue(CommonData.CurrentTime);
            }
            if (lastRadarAlt_time.Count > lastRadarAlt_Count * 2)
            {
                lastRadarAlt.Dequeue();
                lastRadarAlt_time.Dequeue();
            }

            return true;
        }

        public void UpdateGPWS()
        {
            // change RA to RA of lowest gear
            float gearAltitudeInMeter = Util.GetLowestGearRadarAltitude(gears);
            // height in meters/feet
            if (UnitOfAltitude.FOOT == UnitOfAltitude)
            {
                CommonData.RadarAltitude = gearAltitudeInMeter * Util.M_TO_FT;
            }
            else
            {
                CommonData.RadarAltitude = gearAltitudeInMeter;
            }

            isGearDown = Util.GearDeployed(Util.GetLowestGear(gears));


            if (CommonData.CurrentTime - CommonData.TakeOffTime <= 0.1f && CommonData.CurrentTime - CommonData.LandingTime > 5.0f)  // taxi
            {
                if (checkMode_TakeoffSpeedCheck())
                { }
                else if (!Util.audio.IsPlaying())
                {
                    Util.audio.MarkNotPlaying();
                }
            }
            else if (CommonData.CurrentTime - CommonData.TakeOffTime < 1.5f)  // just takeoff
            {
                if (!Util.audio.IsPlaying())
                {
                    Util.audio.MarkNotPlaying();
                }
            }
            else if (CommonData.RadarAltitude > 0 && CommonData.RadarAltitude < float.PositiveInfinity) // flying
            {
                if (checkMode_1())  // Excessive Decent Rate
                { }
                else if (checkMode_2())  // Excessive Closure to Terrain
                { }
                else if (checkMode_3())  // Altitude Loss After TakeOff
                { }
                else if (checkMode_4())  // Unsafe Terrain Clearance
                { }
                else if (checkMode_Traffic())  // Traffic
                { }
                else if (checkMode_6())  // Advisory Callout
                { }
                // other
                if (checkMode_Stall())     // Stall
                { }
                if (checkMode_GearUp())
                { }
                if (!Util.audio.IsPlaying())
                {
                    Util.audio.MarkNotPlaying();
                }
            }
        }

        /// <summary>
        /// Excessive Descent Rate
        /// SINK RATE / WOOP WOOP PULL UP
        /// </summary>
        /// <returns></returns>
        private bool checkMode_1()
        {
            if (EnableDescentRate)
            {
                // is descending (altitude)
                if ((CommonData.Altitude < 2500.0f) && (CommonData.Altitude - CommonData.LastAltitude < 0))
                {
                    // ft/min, altitude
                    float vSpeed = Math.Abs((CommonData.Altitude - CommonData.LastAltitude)
                            / (CommonData.CurrentTime - CommonData.LastTime) * 60.0f);
                    // pull up
                    float maxVSpeedPullUp = Math.Abs(sinkRatePullUpCurve.Evaluate(CommonData.RadarAltitude)) * DescentRateFactor;
                    if (vSpeed > maxVSpeedPullUp)
                    {
                        // play sound
                        Util.audio.PlaySound(KindOfSound.SINK_RATE_PULL_UP);
                        return true;
                    }
                    // sink rate
                    float maxVSpeedSinkRate = Math.Abs(sinkRateCurve.Evaluate(CommonData.RadarAltitude)) * DescentRateFactor;
                    if (vSpeed > maxVSpeedSinkRate)
                    {
                        // play sound
                        Util.audio.PlaySound(KindOfSound.SINK_RATE);
                        return false;   // is warning
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Excessive Closure to Terrain
        /// TERRAIN, TERRAIN / PULL UP
        /// </summary>
        /// <returns></returns>
        private bool checkMode_2()
        {
            if (EnableClosureToTerrain)
            {
                if (isGearDown || (CommonData.CurrentTime - CommonData.TakeOffTime > 30) || (CommonData.Speed < LandingSpeed * 1.2f))        // Mode B
                {
                    // is descending (radar altitude)
                    if ((CommonData.RadarAltitude < 800.0f) && (CommonData.RadarAltitude - CommonData.LastRadarAltitude < 0))
                    {
                        // check if should warn
                        // ft/min, radar altitude
                        float vSpeed = Math.Abs(VerSpeedRA) * 60.0f;
                        // terrain pull up
                        float maxVSpeedPullUp = Math.Abs(terrainPullUpBCurve.Evaluate(CommonData.RadarAltitude)) * DescentRateFactor;
                        if (vSpeed > maxVSpeedPullUp)
                        {
                            // play sound
                            Util.audio.PlaySound(KindOfSound.TERRAIN_PULL_UP);
                            return true;
                        }
                        // terrain, terrain
                        float maxVSpeedTerrain = Math.Abs(terrainBCurve.Evaluate(CommonData.RadarAltitude)) * DescentRateFactor;
                        if (vSpeed > maxVSpeedTerrain)
                        {
                            // play sound
                            Util.audio.PlaySound(KindOfSound.TERRAIN);
                            return true;
                        }
                    }
                }
                else        // Mode A
                {
                    // is descending (radar altitude)
                    if ((CommonData.RadarAltitude < 2200.0f) && (CommonData.RadarAltitude - CommonData.LastRadarAltitude < 0))
                    {
                        // check if should warn
                        // ft/min, radar altitude
                        float vSpeed = Math.Abs(VerSpeedRA) * 60.0f;
                        // terrain pull up
                        float maxVSpeedPullUp = Math.Abs(terrainPullUpCurve.Evaluate(CommonData.RadarAltitude)) * DescentRateFactor;
                        if (vSpeed > maxVSpeedPullUp)
                        {
                            // play sound
                            Util.audio.PlaySound(KindOfSound.TERRAIN_PULL_UP);
                            exitClosureToTerrainWarning = false;
                            return true;
                        }
                        // terrain, terrain
                        float maxVSpeedTerrain = Math.Abs(terrainCurve.Evaluate(CommonData.RadarAltitude)) * DescentRateFactor;
                        if (vSpeed > maxVSpeedTerrain)
                        {
                            // play sound
                            Util.audio.PlaySound(KindOfSound.TERRAIN);
                            exitClosureToTerrainWarning = false;
                            return true;
                        }
                        // continue warning if terrain clearance continues to decrease
                        if (!Util.audio.IsPlaying() && !exitClosureToTerrainWarning)
                        {
                            if (Util.audio.WasPlaying(KindOfSound.TERRAIN))
                            {
                                Util.audio.PlaySound(KindOfSound.TERRAIN, "silence");
                            }
                            else if (Util.audio.WasPlaying(KindOfSound.TERRAIN_PULL_UP))
                            {
                                Util.audio.PlaySound(KindOfSound.TERRAIN, "silence");
                            }
                        }
                    }   // End of if is descending (RA)
                    else
                    {
                        exitClosureToTerrainWarning = true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Altitude Loss After TakeOff
        /// DON'T SINK, DON'T SINK
        /// </summary>
        /// <returns></returns>
        private bool checkMode_3()
        {
            if (EnableAltitudeLoss)
            {
                if ((CommonData.CurrentTime - CommonData.TakeOffTime) < 15 && heightJustTakeoff < 1500)
                {
                    if (CommonData.RadarAltitude >= heightJustTakeoff)
                    {
                        heightJustTakeoff = CommonData.RadarAltitude;     // record height after takeoff
                    }
                    else
                    {
                        // loss radar altitude
                        float heightLoss = heightJustTakeoff - CommonData.RadarAltitude;
                        float maxHeightLoss = Math.Abs(dontSinkCurve.Evaluate(heightJustTakeoff));
                        if (heightLoss > maxHeightLoss)
                        {
                            // play sound
                            Util.audio.PlaySound(KindOfSound.DONT_SINK);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Unsafe Terrain Clearance
        /// TOO LOW TERRAIN / TOO LOW GEAR / TOO LOW FLAPS
        /// </summary>
        /// <returns></returns>
        private bool checkMode_4()
        {
            if (EnableTerrainClearance)
            {
                if (!isGearDown && (CommonData.RadarAltitude < TooLowGearAltitude)
                        && (CommonData.CurrentTime - CommonData.TakeOffTime > 15) && (CommonData.Speed < LandingSpeed * 1.2f))
                {
                    // play sound
                    Util.audio.PlaySound(KindOfSound.TOO_LOW_GEAR);
                    return true;
                }
                if (!isGearDown && (CommonData.CurrentTime - CommonData.TakeOffTime > 15))
                {
                    float tooLowTerrainAltitude = tooLowTerrainCurve.Evaluate(CommonData.Speed / LandingSpeed) * TooLowGearAltitude;
                    if (CommonData.RadarAltitude < tooLowTerrainAltitude)
                    {
                        // play sound
                        Util.audio.PlaySound(KindOfSound.TOO_LOW_TERRAIN);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Advisory Callout
        /// Altitude Callouts / Bank Angle Callout
        /// </summary>
        /// <returns></returns>
        private bool checkMode_6()
        {
            // Throttle Check
            if (EnableRetard)
            {
                // is descending
                if (CommonData.RadarAltitude - CommonData.LastRadarAltitude < 0)
                {
                    // lower than an altitude
                    if (CommonData.RadarAltitude < 15)
                    {
                        if ((CommonData.ActiveVessel.ctrlState.mainThrottle > 0) && (CommonData.CurrentTime - CommonData.TakeOffTime > 5))
                        {
                            // play sound
                            Util.audio.PlaySound(KindOfSound.RETARD);
                            return true;
                        }
                    }
                }
            }
            // Altitude Callouts
            if (EnableAltitudeCallouts)
            {
                // is descending
                if (CommonData.RadarAltitude - CommonData.LastRadarAltitude < 0 && CommonData.RadarAltitude > 0)
                {
                    // lower than an altitude
                    foreach (float threshold in AltitudeArray)
                    {
                        if (CommonData.LastRadarAltitude > threshold && CommonData.RadarAltitude < threshold)
                        {
                            // play sound
                            Util.audio.PlaySound(KindOfSound.ALTITUDE_CALLOUTS, threshold.ToString());
                            return true;
                        }
                    }
                }
            }
            // Bank Angle Callout
            if (EnableBankAngle)
            {
                float bankAngle = Util.BankAngle(CommonData.ActiveVessel);

                if (CommonData.RadarAltitude > 5 && CommonData.RadarAltitude < 2450)
                {
                    float maxBankAngle = Math.Abs(bankAngleCurve.Evaluate(CommonData.RadarAltitude));
                    // check
                    if (bankAngle > maxBankAngle)
                    {
                        // play sound
                        if (!Util.audio.IsPlaying(KindOfSound.BANK_ANGLE))
                        {
                            Util.audio.PlaySound(KindOfSound.BANK_ANGLE);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        private bool checkMode_Traffic()
        {
            if (EnableTraffic)
            {
                for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
                {
                    Vessel vessel = FlightGlobals.Vessels[i];
                    if (!vessel.isActiveVessel && vessel.situation == Vessel.Situations.FLYING
                            && (vessel.mainBody == CommonData.ActiveVessel.mainBody)
                            && (vessel.vesselType != VesselType.Debris)
                            && (vessel.vesselType != VesselType.Flag))
                    {
                        float distance = (float)(vessel.GetWorldPos3D() - CommonData.ActiveVessel.GetWorldPos3D()).magnitude;
                        if (distance < 2.1 * Util.NM_TO_M)  // 2.1NM
                        {
                            if (Math.Abs(vessel.altitude - CommonData.ActiveVessel.altitude) < 600 / Util.M_TO_FT)
                            {
                                Util.audio.PlaySound(KindOfSound.TRAFFIC);
                                return true;
                            }
                        }
                        else if (distance < 3.3 * Util.NM_TO_M)  // 3.3NM
                        {
                            if (Math.Abs(vessel.altitude - CommonData.ActiveVessel.altitude) < 850 / Util.M_TO_FT)
                            {
                                Util.audio.PlaySound(KindOfSound.TRAFFIC);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private bool checkMode_Stall()
        {
            if (EnableStall)
            {
                float aoa = Util.Aoa(CommonData.ActiveVessel);
                if (Math.Abs(aoa) > StallAoa)
                {
                    Util.audio.PlaySound(KindOfSound.STALL);
                    if (EnableStallShake)
                    {
                        float factor = (Math.Abs(aoa) - StallAoa) / (90.0f - StallAoa);
                        float motor = 0.1f + Math.Max(Math.Min(factor, 1.0f), 0.0f) * 0.2f;
                        Util.controller.SetShake(motor, motor * 0.4f);
                    }
                    return true;
                }
            }
            return false;
        }

        private bool checkMode_TakeoffSpeedCheck()
        {
            if (EnableV1)
            {
                if (CommonData.HorSpeed >= V1Speed && CommonData.LastHorSpeed < V1Speed)
                {
                    Util.audio.PlaySound(KindOfSound.V1);
                    return true;
                }
            }
            if (EnableRotate)
            {
                if (CommonData.HorSpeed >= TakeOffSpeed && CommonData.LastHorSpeed < TakeOffSpeed)
                {
                    Util.audio.PlaySound(KindOfSound.ROTATE);
                    return true;
                }
            }
            return false;
        }

        private bool checkMode_GearUp()
        {
            if (EnableGearUp)
            {
                float checkGearUpTime = 5;  // check at 5s
                if ((CommonData.CurrentTime - CommonData.TakeOffTime) > checkGearUpTime
                    && (CommonData.LastTime - CommonData.TakeOffTime) < checkGearUpTime
                    && isGearDown)
                {
                    if (CommonData.VerSpeed >= 0)   // vspeed > 0
                    {
                        // play sound
                        Util.audio.PlaySound(KindOfSound.GEAR_UP);
                        return true;
                    }
                }
            }
            return false;
        }

        public void ChangeVessel(Vessel v)
        {
            if (v != null)
            {
                Util.UpdateGearList(v, ref gears);
            }
        }

        public void CleanUp()
        {
            gears.Clear();
        }
    }
}
