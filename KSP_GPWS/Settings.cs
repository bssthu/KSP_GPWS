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
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    class Settings : MonoBehaviour
    {
        public static bool enableGroundProximityWarning = true;

        public static int[] groundProximityAltitudeArray = { 2500, 1000, 500, 400, 300, 200, 100, 50, 40, 30, 20, 10 };
        public enum UnitOfAltitude
        {
            FOOT = 0,
            METER = 1,
        };
        public static UnitOfAltitude unitOfAltitude = UnitOfAltitude.FOOT;    // use meters or feet, feet is recommanded.
        public static float descentRateFactor = 1.0f;

        public static bool useBlizzy78Toolbar = false;

        public void Awake()
        {
            LoadSettings();
        }

        public static void LoadSettings()
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

                    if (node.HasValue("descentRateFactor"))
                    {
                        float.TryParse(node.GetValue("descentRateFactor"), out descentRateFactor);
                    }

                    if (node.HasValue("useBlizzy78Toolbar"))
                    {
                        bool.TryParse(node.GetValue("useBlizzy78Toolbar"), out useBlizzy78Toolbar);
                    }
                }   // End of has value "name"
            }
        }
    }
}
