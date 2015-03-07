// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP_GPWS.SimpleTypes;

namespace KSP_GPWS
{
    public static class Util
    {
        // Audio
        public static AudioManager audio = new AudioManager();

        private static ScreenMessage screenMsg = new ScreenMessage("", 1, ScreenMessageStyle.UPPER_CENTER);

        public static void showScreenMessage(String msg)
        {
            screenMsg.message = msg;
            ScreenMessages.RemoveMessage(screenMsg);
            ScreenMessages.PostScreenMessage(screenMsg);
        }

        public static T ConvertValue<T>(ConfigNode node, String key, T def = default(T))
        {
            T value;
            return TryConvertValue(node, key, out value) ? value : def;
        }

        public static void ConvertValue<T>(ConfigNode node, String key, ref T value)
        {
            value = ConvertValue(node, key, value);
        }

        public static bool TryConvertValue<T>(ConfigNode node, String key, out T value)
        {
            value = default(T);

            if (!node.HasValue(key))
            {
                return false;
            }

            String str = node.GetValue(key);
            var typeConverter = TypeDescriptor.GetConverter(typeof(T));

            try
            {
                value = (T)typeConverter.ConvertFromInvariantString(str);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void FindGears(Vessel v, ref List<GPWSGear> gears)
        {
            gears.Clear();

            if (null == v)
            {
                return;
            }

            for (int i = 0; i < v.parts.Count; i++)    // it is said that foreach costs more memory due to Unity Mono issues
            {
                Part p = v.parts[i];
                if (p.Modules.Contains("GPWSGear"))
                {
                    gears.Add(p.Modules["GPWSGear"] as GPWSGear);
                    Log(String.Format("find {0}", p.name));
                }
            }
        }

        public static Part GetLowestGear(List<GPWSGear> gears)
        {
            if (gears.Count <= 0)    // no vessel
            {
                return null;
            }
            Part lowestGearPart = gears[0].part;
            float lowestGearAlt = float.PositiveInfinity;
            for (int i = 0; i < gears.Count; i++)    // find lowest gear
            {
                Part p = gears[i].part;
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
        public static float GetGearHeightFromGround(List<GPWSGear> gears)
        {
            if (gears.Count <= 0)    // no vessel
            {
                return float.PositiveInfinity;
            }

            Vessel vessel = gears[0].part.vessel;
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

            Part lowestGearPart = gears[0].part;
            // height from terrain to gear
            float lowestGearRA = float.PositiveInfinity;
            for (int i = 0; i < gears.Count; i++)    // find lowest gear
            {
                Part p = gears[i].part;
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
            UnityEngine.Debug.Log("[GPWS]Info: " + msg);
        }
    }
}
