// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015
// Last modified: 2015-02-11, 01:05:39

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
        // settings
        private bool enableGroundProximityWarning = true;
        private int[] groundProximityAltitudeArray = { 2500, 1000, 500, 400, 300, 200, 100, 50, 40, 30, 20, 10 };
        public enum UnitOfAltitude
        {
            FOOT = 0,
            METER = 1,
        };
        private UnitOfAltitude unitOfAltitude = UnitOfAltitude.FOOT;    // use meters or feet

        private float gearHeight = 0.0f;
        private float lastGearHeight = float.PositiveInfinity;

        private Tools tools = new Tools();

        public void Awake()
        {
        }

        public void Start()
        {
            Tools.Log("Start");
            LoadSettings();
            tools.AudioInitialize();

            GameEvents.onVesselChange.Add(tools.FindGears);
            if (FlightGlobals.ActiveVessel != null)
            {
                tools.FindGears(FlightGlobals.ActiveVessel);
            }

            lastGearHeight = float.PositiveInfinity;
        }

        public void Update()
        {
            // check volume
            if (tools.Volume != GameSettings.VOICE_VOLUME)
            {
                tools.UpdateVolume();
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
            gearHeight = (UnitOfAltitude.FOOT == unitOfAltitude) ? gearHeightFeet : gearHeightMeters;
            if (gearHeight > 0 && gearHeight < float.PositiveInfinity)
            {
                if (checkMode_1())  // Excessive Decent Rate
                {
                    return;
                }
                if (checkMode_6())  // Excessive Decent Rate
                {
                    return;
                }
            }
            lastGearHeight = gearHeight;    // save last gear height

            //showScreenMessage(unitOfAltitude.ToString() + " Height: " + gearHeight.ToString());
        }

        /// <summary>
        /// Excessive Decent Rate
        /// SINK RATE / WOOP WOOP PULL UP
        /// </summary>
        /// <returns></returns>
        public bool checkMode_1()
        {
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
                foreach (float threshold in groundProximityAltitudeArray)
                {
                    if (lastGearHeight > threshold && gearHeight < threshold)
                    {
                        // play sound
                        tools.PlayOneShot("/gpws" + threshold);
                        Tools.Log(String.Format("play " + "/gpws" + threshold));
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

        public void LoadSettings()
        {
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("GPWS_SETTINGS"))
            {
                if (node.HasValue("name") && node.GetValue("name") == "gpwsSettings")
                {
                    if (node.HasValue("enableGroundProximityWarning"))
                    {
                        bool.TryParse(node.GetValue("enableGroundProximityWarning"), out enableGroundProximityWarning);
                    }

                    if (node.HasValue("groundProximityAltitudeArray"))
                    {
                        String[] intstrings = node.GetValue("groundProximityAltitudeArray").Split(',');
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
                            groundProximityAltitudeArray = new int[id];
                            for (int j = 0; j < id; j++)
                            {
                                groundProximityAltitudeArray[j] = tempAlt[j];
                            }
                        }
                    }

                    if (node.HasValue("unitOfAltitude"))
                    {
                        try
                        {
                            unitOfAltitude = (UnitOfAltitude)Enum.Parse(typeof(UnitOfAltitude),
                                node.GetValue("unitOfAltitude"), true);
                        }
                        catch (Exception ex)
                        {
                            Tools.Log("Error: " + ex.Message);
                        }
                    }
                }   // End of has value "name"
            }
        }
    }
}
