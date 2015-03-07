// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP_GPWS.SimpleTypes;

namespace KSP_GPWS
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GPWSPlane : UnityEngine.MonoBehaviour
    {
        const float M_TO_FT = 3.2808399f;

        private Util tools = new Util();

        private Vessel activeVessel = FlightGlobals.ActiveVessel;

        private float gearHeight = 0.0f;
        private float lastGearHeight = float.PositiveInfinity;

        private float altitude = 0.0f;
        private float lastAltitude = float.PositiveInfinity;

        private bool isGearDown = false;

        private float t0 = 0.0f;
        // time since scene loaded
        private float time = 0.0f;
        // time of takeoff
        private float takeOffTime = float.NegativeInfinity;
        private float lastTime = 0.0f;
        // max RA when just takeoff
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

        public void Awake()
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

        public void Start()
        {
            Util.Log("Start");
            Util.audio.AudioInitialize();

            GameEvents.onVesselChange.Add(tools.FindGears);
            activeVessel = FlightGlobals.ActiveVessel;
            if (activeVessel != null)
            {
                tools.FindGears(activeVessel);
            }

            // init
            lastAltitude = float.PositiveInfinity;
            lastGearHeight = float.PositiveInfinity;
            isGearDown = false;
            t0 = Time.time;
            takeOffTime = float.NegativeInfinity;
            lastTime = t0;
            heightJustTakeoff = 0.0f;
            exitClosureToTerrainWarning = false;
        }

        private bool preUpdate()
        {
            time = Time.time - t0;
            // check time, prevent problem
            if (time < 2.0f)
            {
                Util.audio.SetUnavailable();
                saveData();
                return false;
            }
            if (time - takeOffTime < 0.5f)
            {
                Util.audio.MarkNotPlaying();
                saveData();
                return false;
            }

            // just switched
            if (FlightGlobals.ActiveVessel != activeVessel)
            {
                Util.audio.MarkNotPlaying();
                saveData();
                return false;
            }

            // check gear
            if (tools.gearList.Count <= 0)
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
            if (activeVessel.Landed || activeVessel.Splashed)
            {
                takeOffTime = time;
                heightJustTakeoff = 0.0f;
                saveData();
                Util.audio.MarkNotPlaying();
                return false;
            }

            return true;
        }

        public void Update()
        {
            if (!preUpdate())
            {
                return;
            }

            // check volume
            if (Util.audio.Volume != GameSettings.VOICE_VOLUME * Settings.volume)
            {
                Util.audio.UpdateVolume();
            }

            isGearDown = Util.GearIsDown(tools.GetLowestGear());
            float gearHeightMeters = tools.GetGearHeightFromGround();

            // height in meters/feet
            if (UnitOfAltitude.FOOT == Settings.unitOfAltitude)
            {
                gearHeight = gearHeightMeters * M_TO_FT;
                altitude = (float)(FlightGlobals.ship_altitude * M_TO_FT);
            }
            else
            {
                gearHeight = gearHeightMeters;
                altitude = (float)FlightGlobals.ship_altitude;
            }
            if (gearHeight > 0 && gearHeight < float.PositiveInfinity)
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

            saveData();
        }

        private void saveData() // after Update
        {
            lastGearHeight = gearHeight;    // save last gear height
            lastAltitude = altitude;
            lastTime = time;        // save time of last frame
            activeVessel = FlightGlobals.ActiveVessel;
        }

        /// <summary>
        /// Excessive Descent Rate
        /// SINK RATE / WOOP WOOP PULL UP
        /// </summary>
        /// <returns></returns>
        public bool checkMode_1()
        {
            if (Settings.enableDescentRate)
            {
                // is descending (altitude)
                if ((altitude < 2500.0f) && (altitude - lastAltitude < 0))
                {
                    float vSpeed = Math.Abs((altitude - lastAltitude) / (time - lastTime) * 60.0f);   // ft/min, altitude
                    // pull up
                    float maxVSpeedPullUp = Math.Abs(sinkRatePullUpCurve.Evaluate(gearHeight)) * Settings.descentRateFactor;
                    if (vSpeed > maxVSpeedPullUp)
                    {
                        // play sound
                        Util.audio.PlaySound(KindOfSound.SINK_RATE_PULL_UP);
                        return true;
                    }
                    // sink rate
                    float maxVSpeedSinkRate = Math.Abs(sinkRateCurve.Evaluate(gearHeight)) * Settings.descentRateFactor;
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
        public bool checkMode_2()
        {
            if (Settings.enableClosureToTerrain)
            {
                if (isGearDown || (time - takeOffTime > 30))        // Mode B
                {
                    // is descending (radar altitude)
                    if ((altitude < 800.0f) && (altitude - lastAltitude < 0))
                    {
                        // check if should warn
                        float vSpeed = Math.Abs((gearHeight - lastGearHeight) / (time - lastTime) * 60.0f);   // ft/min, radar altitude
                        // terrain pull up
                        float maxVSpeedPullUp = Math.Abs(terrainPullUpBCurve.Evaluate(gearHeight)) * Settings.descentRateFactor;
                        if (vSpeed > maxVSpeedPullUp)
                        {
                            // play sound
                            Util.audio.PlaySound(KindOfSound.TERRAIN_PULL_UP);
                            return true;
                        }
                        // terrain, terrain
                        float maxVSpeedTerrain = Math.Abs(terrainBCurve.Evaluate(gearHeight)) * Settings.descentRateFactor;
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
                    if ((altitude < 2200.0f) && (altitude - lastAltitude < 0))
                    {
                        // check if should warn
                        float vSpeed = Math.Abs((gearHeight - lastGearHeight) / (time - lastTime) * 60.0f);   // ft/min, radar altitude
                        // terrain pull up
                        float maxVSpeedPullUp = Math.Abs(terrainPullUpCurve.Evaluate(gearHeight)) * Settings.descentRateFactor;
                        if (vSpeed > maxVSpeedPullUp)
                        {
                            // play sound
                            Util.audio.PlaySound(KindOfSound.TERRAIN_PULL_UP);
                            exitClosureToTerrainWarning = false;
                            return true;
                        }
                        // terrain, terrain
                        float maxVSpeedTerrain = Math.Abs(terrainCurve.Evaluate(gearHeight)) * Settings.descentRateFactor;
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
        public bool checkMode_3()
        {
            if (Settings.enableAltitudeLoss)
            {
                if ((time - takeOffTime) < 15 && heightJustTakeoff < 1500)
                {
                    if (gearHeight >= heightJustTakeoff)
                    {
                        heightJustTakeoff = gearHeight;     // record height after takeoff
                    }
                    else
                    {
                        // loss radar altitude
                        float heightLoss = heightJustTakeoff - gearHeight;
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
        public bool checkMode_4()
        {
            if (Settings.enableTerrainClearance)
            {
                if (!isGearDown && gearHeight < Settings.tooLowGearAltitude && time - takeOffTime > 15)
                {
                    // play sound
                    Util.audio.PlaySound(KindOfSound.TOO_LOW_GEAR);
                    return true;
                }
                if ((time - takeOffTime) < 5 && (gearHeight < heightJustTakeoff))
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
        public bool checkMode_6()
        {
            // Altitude Callouts
            if (Settings.enableAltitudeCallouts)
            {
                // is descending
                if (gearHeight - lastGearHeight < 0)
                {
                    // lower than an altitude
                    foreach (float threshold in Settings.altitudeArray)
                    {
                        if (lastGearHeight > threshold && gearHeight < threshold)
                        {
                            // play sound
                            Util.audio.PlaySound(KindOfSound.ALTITUDE_CALLOUTS, threshold.ToString());
                            return true;
                        }
                    }
                }
            }
            // Bank Angle Callout
            if (Settings.enableBankAngle)
            {
                // bank angle from https://github.com/Crzyrndm/Pilot-Assistant/blob/ebd426fe1a9a0fc75a674e5a45d69b1c6c66a438/PilotAssistant/Utility/FlightData.cs
                // surface vectors
                Vector3d planetUp = (activeVessel.findWorldCenterOfMass() - activeVessel.mainBody.position).normalized;
                // Vessel forward and right vetors, parallel to the surface
                Vector3d surfVesRight = Vector3d.Cross(planetUp, activeVessel.ReferenceTransform.up).normalized;
                // roll
                double roll = Vector3d.Angle(surfVesRight, activeVessel.ReferenceTransform.right)
                        * Math.Sign(Vector3d.Dot(surfVesRight, activeVessel.ReferenceTransform.forward));

                float bankAngle = (float)Math.Abs(roll);

                if (gearHeight > 5 && gearHeight < 2450)
                {
                    float maxBankAngle = Math.Abs(bankAngleCurve.Evaluate(gearHeight));
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

        public bool checkMode_Traffic()
        {
            if (Settings.enableTraffic)
            {
                for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
                {
                    Vessel vessel = FlightGlobals.Vessels[i];
                    if (!vessel.isActiveVessel && !(vessel.Landed || vessel.Splashed) && vessel.mainBody == activeVessel.mainBody)
                    {
                        float distance = (float)(vessel.GetWorldPos3D() - activeVessel.GetWorldPos3D()).magnitude;
                        if (distance < 3889.2)  // 2.1NM
                        {
                            if (Math.Abs(vessel.altitude - activeVessel.altitude) < 600 / M_TO_FT)
                            {
                                Util.audio.PlaySound(KindOfSound.TRAFFIC);
                                return true;
                            }
                        }
                        else if (distance < 6111.6)  // 3.3NM
                        {
                            if (Math.Abs(vessel.altitude - activeVessel.altitude) < 850 / M_TO_FT)
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

        public void OnDestroy()
        {
            GameEvents.onVesselChange.Remove(tools.FindGears);
            tools.gearList.Clear();
        }
    }
}
