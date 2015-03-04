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

        private float time0 = 0.0f;
        // time since scene loaded
        private float time = 0.0f;
        private float lastTime = 0.0f;

        // curves
        private FloatCurve sinkRateCurve = new FloatCurve();    // (alt, vSpeed)
        private FloatCurve pullUpCurve = new FloatCurve();      // (alt, vSpeed)
        private FloatCurve bankAngleCurve = new FloatCurve();   // (radar alt, bankAngle)

        public void Awake()
        {
            // init curves, points are not accurate
            sinkRateCurve.Add(50, -1000);
            sinkRateCurve.Add(2500, -5000);
            pullUpCurve.Add(50, -1500);
            pullUpCurve.Add(100, -1600);
            pullUpCurve.Add(2500, -7000);
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
            time0 = Time.time;
            lastTime = time0;
        }

        public void Update()
        {
            // check volume
            if (tools.Volume != GameSettings.VOICE_VOLUME)
            {
                tools.UpdateVolume();
            }

            time = Time.time - time0;
            // check time
            if (time < 3.0f)
            {
                Tools.SetUnavailable();
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
                else if (checkMode_4())  // Unsafe Terrain Clearance
                { }
                else if (checkMode_6())  // Advisory Callout
                { }
            }
            lastGearHeight = gearHeight;    // save last gear height
            lastAltitude = altitude;
            lastTime = time;        // save time of last frame

            if (!tools.IsPlaying())
            {
                Tools.MarkNotPlaying();
            }
            //tools.showScreenMessage(unitOfAltitude.ToString() + " Time: " + Time.time);
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
                    float maxVSpeedPullUp = Math.Abs(pullUpCurve.Evaluate(gearHeight)) * Settings.descentRateFactor;
                    if (vSpeed > maxVSpeedPullUp)
                    {
                        // play sound
                        tools.PlaySound(Tools.KindOfSound.WOOP_WOOP_PULL_UP);
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
