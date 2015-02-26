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
        private float gearHeight = 0.0f;
        private float lastGearHeight = float.PositiveInfinity;

        private Tools tools = new Tools();

        private float time0 = 0.0f;
        // time since scene loaded
        private float time = 0.0f;
        private float lastTime = 0.0f;

        // curves
        private FloatCurve sinkRateCurve = new FloatCurve();
        private FloatCurve pullUpCurve = new FloatCurve();

        public void Awake()
        {
            Settings.LoadSettings();
            // init curves, points are not accurate
            sinkRateCurve.Add(50, -1000);
            sinkRateCurve.Add(2500, -5000);
            pullUpCurve.Add(50, -1500);
            pullUpCurve.Add(100, -1600);
            pullUpCurve.Add(2200, -7000);
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

            lastGearHeight = float.PositiveInfinity;
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
                return;
            }

            // check atmosphere
            if (!FlightGlobals.getMainBody().atmosphere ||
                    FlightGlobals.ship_altitude > FlightGlobals.getMainBody().maxAtmosphereAltitude)
            {
                return;
            }

            float gearHeightMeters = tools.GetGearHeightFromGround();
            float gearHeightFeet = gearHeightMeters * 3.2808399f;
            // height in meters/feet
            gearHeight = (Settings.UnitOfAltitude.FOOT == Settings.unitOfAltitude) ? gearHeightFeet : gearHeightMeters;
            if (gearHeight > 0 && gearHeight < float.PositiveInfinity)
            {
                if (checkMode_1())  // Excessive Decent Rate
                { }
                else if (checkMode_6())  // Advisory Callout
                { }
            }
            lastGearHeight = gearHeight;    // save last gear height
            lastTime = time;        // save time of last frame

            //tools.showScreenMessage(unitOfAltitude.ToString() + " Time: " + Time.time);
        }

        /// <summary>
        /// Excessive Decent Rate
        /// SINK RATE / WOOP WOOP PULL UP
        /// </summary>
        /// <returns></returns>
        public bool checkMode_1()
        {
            // is descending
            if ((lastGearHeight != float.PositiveInfinity) && (gearHeight - lastGearHeight < 0))
            {
                float vSpeed = Math.Abs((gearHeight - lastGearHeight) / (time - lastTime) * 60.0f);   // ft/min, radar altitude
                // pull up
                float maxVSpeedPullUp = Math.Abs(pullUpCurve.Evaluate(gearHeight)) * Settings.descentRateFactor;
                if (vSpeed > maxVSpeedPullUp)
                {
                    // play sound
                    if (!tools.IsPlaying(Tools.KindOfSound.WOOP_WOOP_PULL_UP))
                    {
                        tools.PlayOneShot("pull_up");
                        tools.kindOfSound = Tools.KindOfSound.WOOP_WOOP_PULL_UP;
                    }
                    return true;
                }
                // sink rate
                float maxVSpeedSinkRate = Math.Abs(sinkRateCurve.Evaluate(gearHeight)) * Settings.descentRateFactor;
                if (vSpeed > maxVSpeedSinkRate)
                {
                    // play sound
                    if (!tools.IsPlaying(Tools.KindOfSound.SINK_RATE)
                            && !tools.IsPlaying(Tools.KindOfSound.WOOP_WOOP_PULL_UP))
                    {
                        tools.PlayOneShot("sink_rate");
                        tools.kindOfSound = Tools.KindOfSound.SINK_RATE;
                    }
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
            // is descending
            if ((lastGearHeight != float.PositiveInfinity) && (gearHeight - lastGearHeight < 0))
            {
                // lower than an altitude
                foreach (float threshold in Settings.altitudeArray)
                {
                    if (lastGearHeight > threshold && gearHeight < threshold)
                    {
                        // play sound
                        tools.PlayOneShot("gpws" + threshold);
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
