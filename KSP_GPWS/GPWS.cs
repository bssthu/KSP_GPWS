// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015
// Last modified: 2015-01-17, 03:35:18

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
        private List<GPWSGear> gearList = new List<GPWSGear>();     // parts with module "GPWSGear"

        private bool enableGroundProximityWarning = true;
        private int[] groundProximityAltitudeArray = { 2500, 1000, 500, 400, 300, 200, 100, 50, 40, 30, 20, 10 };

        public void Awake()
        {
        }

        public void Start()
        {
            LoadSettings();
            GameEvents.onVesselChange.Add(findGears);
            if (FlightGlobals.ActiveVessel != null)
            {
                findGears(FlightGlobals.ActiveVessel);
            }
        }

        private void findGears(Vessel v)
        {
            gearList.Clear();

            if (null == v)
            {
                return;
            }

            for (int i = 0; i < v.parts.Count; i++)    // it is said that foreach costs more memory due to Unity Mono issues
            {
                Part p = v.parts[i];
                if (p.Modules.Contains("GPWSGear"))
                {
                    gearList.Add(p.Modules["GPWSGear"] as GPWSGear);
                    Log(String.Format("find {0}", p.name));
                }
            }
        }

        public void Update()
        {
        }

        public void OnDestroy()
        {
            GameEvents.onVesselChange.Remove(findGears);
            gearList.Clear();
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
                }
            }
        }

        public static void Log(String msg)
        {
            UnityEngine.Debug.Log("[GPWS] " + msg);
        }
    }
}
