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
    public class Util
    {
        public List<GPWSGear> gearList = new List<GPWSGear>();     // parts with module "GPWSGear"

        // Audio
        public static AudioManager audio = new AudioManager();

        private static ScreenMessage screenMsg = new ScreenMessage("", 1, ScreenMessageStyle.UPPER_CENTER);

        public static void showScreenMessage(String msg)
        {
            screenMsg.message = msg;
            ScreenMessages.RemoveMessage(screenMsg);
            ScreenMessages.PostScreenMessage(screenMsg);
        }

        public void FindGears(Vessel v)
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

        public Part GetLowestGear()
        {
            if (gearList.Count <= 0)    // no vessel
            {
                return null;
            }
            Part lowestGearPart = gearList[0].part;
            float lowestGearAlt = float.PositiveInfinity;
            for (int i = 0; i < gearList.Count; i++)    // find lowest gear
            {
                Part p = gearList[i].part;
                // pos of part, rotate to fit ground coord.
                Vector3 rotatedPos = p.vessel.srfRelRotation * p.orgPos;
                float gearAltitude = (float)(FlightGlobals.ActiveVessel.altitude - rotatedPos.z);

                if (gearAltitude < lowestGearAlt)
                {
                    lowestGearPart = p;
                    lowestGearAlt = gearAltitude;
                }
            }
            return lowestGearPart;
        }

        public static bool GearIsDown(Part gear)
        {
            if (gear != null)
            {
                // ModuleLandingGear
                try
                {
                    if (gear.Modules.Contains("ModuleLandingGear") &&
                            gear.Modules["ModuleLandingGear"].Events["LowerLandingGear"].active)
                    {
                        return false;  // not down
                    }
                }
                catch (Exception) { }

                // FSwheel
                try
                {
                    if (gear.Modules.Contains("FSwheel"))
                    {
                        PartModule m = gear.Modules["FSwheel"];
                        if (m.GetType().GetField("deploymentState").GetValue(m).ToString() != "Deployed")
                        {
                            return false;  // not down
                        }
                    }
                }
                catch (Exception) { }
            }
            return true;
        }

        /// <summary>
        /// return height from surface to the lowest landing gear, in meters
        /// </summary>
        /// <returns></returns>
        public float GetGearHeightFromGround()
        {
            if (gearList.Count <= 0)    // no vessel
            {
                return float.PositiveInfinity;
            }

            Vessel vessel = gearList[0].part.vessel;
            if (FlightGlobals.ActiveVessel != vessel)   // not right vessel?
            {
                return float.PositiveInfinity;
            }

            float terrainHeight = (float)vessel.terrainAltitude;
            if (terrainHeight < 0)
            {
                terrainHeight = 0;
            }
            float radarAltitude = (float)vessel.altitude - terrainHeight;      // from vessel to surface, in meters

            Part lowestGearPart = gearList[0].part;
            // height from terrain to gear
            float lowestGearRA = float.PositiveInfinity;
            for (int i = 0; i < gearList.Count; i++)    // find lowest gear
            {
                Part p = gearList[i].part;
                // pos of part, rotate to fit ground coord.
                Vector3 rotatedPos = p.vessel.srfRelRotation * p.orgPos;
                float gearRadarAltitude = radarAltitude - rotatedPos.z;

                if (gearRadarAltitude < lowestGearRA)
                {
                    lowestGearPart = p;
                    lowestGearRA = gearRadarAltitude;
                }
            }
            return lowestGearRA;
        }

        public static void Log(String msg)
        {
            UnityEngine.Debug.Log("[GPWS] " + msg);
        }
    }
}
