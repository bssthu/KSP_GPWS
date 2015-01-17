// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015
// Last modified: 2015-01-17, 13:55:14

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
        public enum UnitOfAltitude
        {
            FOOT,
            METER,
        };
        private UnitOfAltitude unitOfAltitude = UnitOfAltitude.FOOT;    // use meters or feet

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
            if (gearList.Count <= 0)
            {
                return;
            }
            float vesselHeight = gearList[0].part.vessel.heightFromTerrain;
            if (vesselHeight < 0)
            {
                return;
            }

            Part lowestGearPart = gearList[0].part;
            float lowestGearHeight = float.PositiveInfinity;
            for (int i = 0; i < gearList.Count; i++)
            {
                Part p = gearList[i].part;
                // pos of part, rotate to fit ground coord.
                Vector3 rotatedPos = p.vessel.srfRelRotation * p.orgPos;
                float partHeightFromTerrain = vesselHeight - rotatedPos.z;

                if (partHeightFromTerrain < lowestGearHeight)
                {
                    lowestGearPart = p;
                    lowestGearHeight = partHeightFromTerrain;
                }
            }
            Log(String.Format("{0}, a={1}", lowestGearPart.name, lowestGearHeight));
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
                    if (node.HasValue("unitOfAltitude"))
                    {
                        try
                        {
                            unitOfAltitude = (UnitOfAltitude)Enum.Parse(typeof(UnitOfAltitude), "METER", true);
                        }
                        catch (Exception ex)
                        {
                            Log("Error: " + ex.Message);
                        }
                    }
                }   // End of has value "name"
            }
        }

        public static void Log(String msg)
        {
            UnityEngine.Debug.Log("[GPWS] " + msg);
        }
    }
}
