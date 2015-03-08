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
    public class GPWSPlane : IPlaneConfig, IBasicGPWSFunction
    {
        private IGPWSCommonData CommonData = null;

        /// <summary>
        /// parts with module "GPWSGear"
        /// </summary>
        private List<GPWSGear> gears = new List<GPWSGear>();

        private bool isGearDown = false;

        /// <summary>
        /// time of takeoff
        /// </summary>
        private float takeOffTime = float.NegativeInfinity;

        /// <summary>
        /// max RA when just takeoff
        /// </summary>
        private float heightJustTakeoff = 0.0f;

        // curves
        private FloatCurve sinkRateCurve = new FloatCurve();            // (alt, vSpeed)
        private FloatCurve sinkRatePullUpCurve = new FloatCurve();      // (alt, vSpeed)
        private FloatCurve terrainCurve = new FloatCurve();             // (radar alt, vSpeed)
        private FloatCurve terrainPullUpCurve = new FloatCurve();       // (radar alt, vSpeed)
        private FloatCurve terrainBCurve = new FloatCurve();            // (radar alt, vSpeed)
        private FloatCurve terrainPullUpBCurve = new FloatCurve();      // (radar alt, vSpeed)
        private FloatCurve dontSinkCurve = new FloatCurve();            // (radar alt, RA loss)
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
        public bool EnableBankAngle { get; set; }
        public bool EnableTraffic { get; set; }

        public float DescentRateFactor { get; set; }
        public float TooLowGearAltitude { get; set; }
        public int[] AltitudeArray { get; set; }

        /// <summary>
        /// use meters or feet, feet is recommanded.
        /// </summary>
        public UnitOfAltitude UnitOfAltitude { get; set; }

        #endregion

        #region IConfigNode Members

        public void Load(ConfigNode node)
        {
            EnableSystem = Util.ConvertValue<bool>(node, "EnableSystem");
            EnableDescentRate = Util.ConvertValue<bool>(node, "EnableDescentRate");
            EnableClosureToTerrain = Util.ConvertValue<bool>(node, "EnableClosureToTerrain");
            EnableAltitudeLoss = Util.ConvertValue<bool>(node, "EnableAltitudeLoss");
            EnableTerrainClearance = Util.ConvertValue<bool>(node, "EnableTerrainClearance");
            EnableAltitudeCallouts = Util.ConvertValue<bool>(node, "EnableAltitudeCallouts");
            EnableBankAngle = Util.ConvertValue<bool>(node, "EnableBankAngle");
            EnableTraffic = Util.ConvertValue<bool>(node, "EnableTraffic");

            DescentRateFactor = Util.ConvertValue<float>(node, "DescentRateFactor");
            TooLowGearAltitude = Util.ConvertValue<float>(node, "TooLowGearAltitude");
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
            UnitOfAltitude = Util.ConvertValue<UnitOfAltitude>(node, "UnitOfAltitude");
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
            node.AddValue("EnableBankAngle", EnableBankAngle);
            node.AddValue("EnableTraffic", EnableTraffic);

            node.AddValue("DescentRateFactor", DescentRateFactor);
            node.AddValue("TooLowGearAltitude", TooLowGearAltitude);
            node.AddValue("AltitudeArray", String.Join(",", Array.ConvertAll(AltitudeArray, x => x.ToString())));
            node.AddValue("UnitOfAltitude", UnitOfAltitude);
        }

        #endregion

        public void CheckConfigLegality()
        {
            DescentRateFactor = Math.Max(DescentRateFactor, 0.1f);
            DescentRateFactor = Math.Min(DescentRateFactor, 10.0f);
        }

        public GPWSPlane()
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
            EnableBankAngle = false;
            EnableTraffic = true;

            DescentRateFactor = 1.0f;
            TooLowGearAltitude = 500.0f;
            AltitudeArray = new int[] { 1000, 500, 400, 300, 200, 100, 50, 40, 30, 20, 10 };
            UnitOfAltitude = UnitOfAltitude.FOOT;
        }

        public void Initialize(IGPWSCommonData data)
        {
            CommonData = data;

            isGearDown = false;
            takeOffTime = float.NegativeInfinity;
            heightJustTakeoff = 0.0f;
            exitClosureToTerrainWarning = false;

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

            bankAngleCurve.Add(5, 10);
            bankAngleCurve.Add(30, 10);
            bankAngleCurve.Add(150, 40);
            bankAngleCurve.Add(2450, 55);
        }

        public bool PreUpdate()
        {
            // just takeoff
            if (CommonData.time - takeOffTime < 0.5f)
            {
                Util.audio.MarkNotPlaying();
                return false;
            }

            // check gear
            if (gears.Count <= 0)
            {
                Util.audio.SetUnavailable();
                return false;
            }

            // check atmosphere
            if (!FlightGlobals.getMainBody().atmosphere ||
                    FlightGlobals.ship_altitude > FlightGlobals.getMainBody().maxAtmosphereAltitude)
            {
                Util.audio.SetUnavailable();
                return false;
            }

            // on surface
            if (CommonData.ActiveVessel.Landed || CommonData.ActiveVessel.Splashed)
            {
                takeOffTime = CommonData.time;
                heightJustTakeoff = 0.0f;
                Util.audio.MarkNotPlaying();
                return false;
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

            if (CommonData.RadarAltitude > 0 && CommonData.RadarAltitude < float.PositiveInfinity)
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
                else if (!Util.audio.IsPlaying())
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
                    float vSpeed = Math.Abs((CommonData.Altitude - CommonData.LastAltitude) / (CommonData.time - CommonData.lastTime) * 60.0f);   // ft/min, altitude
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
                        return true;
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
                if (isGearDown || (CommonData.time - takeOffTime > 30))        // Mode B
                {
                    // is descending (radar altitude)
                    if ((CommonData.Altitude < 800.0f) && (CommonData.Altitude - CommonData.LastAltitude < 0))
                    {
                        // check if should warn
                        float vSpeed = Math.Abs((CommonData.RadarAltitude - CommonData.LastRadarAltitude) / (CommonData.time - CommonData.lastTime) * 60.0f);   // ft/min, radar altitude
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
                    if ((CommonData.Altitude < 2200.0f) && (CommonData.Altitude - CommonData.LastAltitude < 0))
                    {
                        // check if should warn
                        float vSpeed = Math.Abs((CommonData.RadarAltitude - CommonData.LastRadarAltitude) / (CommonData.time - CommonData.lastTime) * 60.0f);   // ft/min, radar altitude
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
                if ((CommonData.time - takeOffTime) < 15 && heightJustTakeoff < 1500)
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
                if (!isGearDown && CommonData.RadarAltitude < TooLowGearAltitude && CommonData.time - takeOffTime > 15)
                {
                    // play sound
                    Util.audio.PlaySound(KindOfSound.TOO_LOW_GEAR);
                    return true;
                }
                if ((CommonData.time - takeOffTime) < 5 && (CommonData.RadarAltitude < heightJustTakeoff))
                {
                    // play sound
                    Util.audio.PlaySound(KindOfSound.TOO_LOW_TERRAIN);
                    return true;
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
            // Altitude Callouts
            if (EnableAltitudeCallouts)
            {
                // is descending
                if (CommonData.RadarAltitude - CommonData.LastRadarAltitude < 0)
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
                    if (!vessel.isActiveVessel && !(vessel.Landed || vessel.Splashed) && vessel.mainBody == CommonData.ActiveVessel.mainBody)
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

        public void ChangeVessel(Vessel v)
        {
            if (v != null)
            {
                Util.UpdateGearList(v, ref gears);
            }
        }

        public void Clear()
        {
            gears.Clear();
        }
    }
}
