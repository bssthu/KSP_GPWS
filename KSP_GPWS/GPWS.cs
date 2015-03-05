// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSP_GPWS
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GPWS : UnityEngine.MonoBehaviour
    {
        const float M_TO_FT = 3.2808399f;

        private Tools tools = new Tools();

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

        // curves
        private FloatCurve sinkRateCurve = new FloatCurve();            // (alt, vSpeed)
        private FloatCurve sinkRatePullUpCurve = new FloatCurve();      // (alt, vSpeed)
        private FloatCurve terrainCurve = new FloatCurve();             // (radar alt, vSpeed)
        private FloatCurve terrainPullUpCurve = new FloatCurve();       // (radar alt, vSpeed)
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

            bankAngleCurve.Add(5, 10);
            bankAngleCurve.Add(30, 10);
            bankAngleCurve.Add(150, 40);
            bankAngleCurve.Add(2450, 55);
        }

        public void Start()
        {
            Tools.Log("Start");
            tools.AudioInitialize();

            GameEvents.onVesselChange.Add(tools.FindGears);
            if (FlightGlobals.ActiveVessel != null)
            {
                tools.FindGears(FlightGlobals.ActiveVessel);
            }

            // init
            lastAltitude = float.PositiveInfinity;
            lastGearHeight = float.PositiveInfinity;
            isGearDown = false;
            t0 = Time.time;
            takeOffTime = float.NegativeInfinity;
            lastTime = t0;
            exitClosureToTerrainWarning = false;
        }

        public void Update()
        {
            // check volume
            if (tools.Volume != GameSettings.VOICE_VOLUME)
            {
                tools.UpdateVolume();
            }

            time = Time.time - t0;
            // check time
            if (time < 2.0f)
            {
                Tools.SetUnavailable();
                saveData();
                return;
            }

            // check gear
            if (tools.gearList.Count <= 0)
            {
                Tools.SetUnavailable();
                return;
            }

            // check atmosphere
            if (!FlightGlobals.getMainBody().atmosphere ||
                    FlightGlobals.ship_altitude > FlightGlobals.getMainBody().maxAtmosphereAltitude)
            {
                Tools.SetUnavailable();
                return;
            }

            // on surface
            if (FlightGlobals.ActiveVessel.Landed || FlightGlobals.ActiveVessel.Splashed)
            {
                takeOffTime = time;
                saveData();
                Tools.MarkNotPlaying();
                return;
            }

            isGearDown = Tools.GearIsDown(tools.GetLowestGear());

            float gearHeightMeters = tools.GetGearHeightFromGround();
            // height in meters/feet
            if (Settings.UnitOfAltitude.FOOT == Settings.unitOfAltitude)
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
                else if (checkMode_4())  // Unsafe Terrain Clearance
                { }
                else if (checkMode_6())  // Advisory Callout
                { }
                else if (!tools.IsPlaying())
                {
                    Tools.MarkNotPlaying();
                }
            }

            saveData();

            //tools.showScreenMessage(unitOfAltitude.ToString() + " Time: " + Time.time);
        }

        private void saveData() // after Update
        {
            lastGearHeight = gearHeight;    // save last gear height
            lastAltitude = altitude;
            lastTime = time;        // save time of last frame
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
                if ((altitude < 30000.0f) && (altitude - lastAltitude < 0))
                {
                    float vSpeed = Math.Abs((altitude - lastAltitude) / (time - lastTime) * 60.0f);   // ft/min, altitude
                    // pull up
                    float maxVSpeedPullUp = Math.Abs(sinkRatePullUpCurve.Evaluate(gearHeight)) * Settings.descentRateFactor;
                    if (vSpeed > maxVSpeedPullUp)
                    {
                        // play sound
                        tools.PlaySound(Tools.KindOfSound.SINK_RATE_PULL_UP);
                        return true;
                    }
                    // sink rate
                    float maxVSpeedSinkRate = Math.Abs(sinkRateCurve.Evaluate(gearHeight)) * Settings.descentRateFactor;
                    if (vSpeed > maxVSpeedSinkRate)
                    {
                        // play sound
                        tools.PlaySound(Tools.KindOfSound.SINK_RATE);
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
                if (!isGearDown)        // Mode A
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
                            tools.PlaySound(Tools.KindOfSound.TERRAIN_PULL_UP);
                            exitClosureToTerrainWarning = false;
                            return true;
                        }
                        // terrain, terrain
                        float maxVSpeedTerrain = Math.Abs(terrainCurve.Evaluate(gearHeight)) * Settings.descentRateFactor;
                        if (vSpeed > maxVSpeedTerrain)
                        {
                            // play sound
                            tools.PlaySound(Tools.KindOfSound.TERRAIN);
                            exitClosureToTerrainWarning = false;
                            return true;
                        }
                        // continue warning if terrain clearance continues to decrease
                        if (!tools.IsPlaying() && !exitClosureToTerrainWarning)
                        {
                            if (tools.WasPlaying(Tools.KindOfSound.TERRAIN))
                            {
                                tools.PlaySound(Tools.KindOfSound.TERRAIN, "silence");
                            }
                            else if (tools.WasPlaying(Tools.KindOfSound.TERRAIN_PULL_UP))
                            {
                                tools.PlaySound(Tools.KindOfSound.TERRAIN, "silence");
                            }
                        }
                    }   // End of if is descending (RA)
                    else
                    {
                        exitClosureToTerrainWarning = true;
                    }
                }
                else        // Mode B
                {
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
                if (!isGearDown && gearHeight < Settings.tooLowGearAltitude)
                {
                    // play sound
                    tools.PlaySound(Tools.KindOfSound.TOO_LOW_GEAR);
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
                            tools.PlaySound(Tools.KindOfSound.ALTITUDE_CALLOUTS, threshold.ToString());
                            return true;
                        }
                    }
                }
            }
            // Bank Angle Callout
            if (Settings.enableBankAngle)
            {
                Vessel vessel = FlightGlobals.ActiveVessel;

                // https://github.com/Crzyrndm/Pilot-Assistant/blob/ebd426fe1a9a0fc75a674e5a45d69b1c6c66a438/PilotAssistant/Utility/FlightData.cs
                // surface vectors
                Vector3d planetUp = (vessel.findWorldCenterOfMass() - vessel.mainBody.position).normalized;
                // Vessel forward and right vetors, parallel to the surface
                Vector3d surfVesRight = Vector3d.Cross(planetUp, vessel.ReferenceTransform.up).normalized;
                // roll
                double roll = Vector3d.Angle(surfVesRight, vessel.ReferenceTransform.right)
                        * Math.Sign(Vector3d.Dot(surfVesRight, vessel.ReferenceTransform.forward));

                float bankAngle = (float)Math.Abs(roll);

                if (gearHeight > 5 && gearHeight < 2450)
                {
                    float maxBankAngle = Math.Abs(bankAngleCurve.Evaluate(gearHeight));
                    // check
                    if (bankAngle > maxBankAngle)
                    {
                        // play sound
                        if (!tools.IsPlaying(Tools.KindOfSound.BANK_ANGLE))
                        {
                            tools.PlaySound(Tools.KindOfSound.BANK_ANGLE);
                        }
                        return true;
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
