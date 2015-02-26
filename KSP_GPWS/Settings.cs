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
        #region from_file

        public static bool enableSystem = true;

        public static int[] altitudeArray = { 2500, 1000, 500, 400, 300, 200, 100, 50, 40, 30, 20, 10 };
        public enum UnitOfAltitude
        {
            FOOT = 0,
            METER = 1,
        };
        public static UnitOfAltitude unitOfAltitude = UnitOfAltitude.FOOT;    // use meters or feet, feet is recommanded.
        public static float descentRateFactor = 1.0f;

        public static bool useBlizzy78Toolbar = false;

        #endregion

        #region in_memory

        public static Rect guiwindowPosition = new Rect(100, 100, 300, 350);

        #endregion



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
                        bool.TryParse(node.GetValue("enableGroundProximityWarning"), out enableSystem);
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
                            altitudeArray = new int[id];
                            for (int j = 0; j < id; j++)
                            {
                                altitudeArray[j] = tempAlt[j];
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
