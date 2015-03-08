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
using KSP_GPWS.Impl;

namespace KSP_GPWS
{
    public static class Util
    {
        /// <summary>
        /// m to feet
        /// </summary>
        public const float M_TO_FT = 3.2808399f;

        /// <summary>
        /// nmi to m
        /// </summary>
        public const float NM_TO_M = 1852.0f;

        /// <summary>
        /// Audio
        /// </summary>
        public static AudioManager audio = new AudioManager();

        private static ScreenMessage screenMsg = new ScreenMessage("", 1, ScreenMessageStyle.UPPER_CENTER);

        public static void ShowScreenMessage(String msg)
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

        public static void UpdateGearList(Vessel v, ref List<GPWSGear> gears)
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
                    Log(String.Format("find {0} in {1}", p.name, p.vessel.name));
                }
            }
        }

        public static bool GearDeployed(Part gear)
        {
            if (gear != null)
            {
                // ModuleLandingGear
                try
                {
                    if (gear.Modules.Contains("ModuleLandingGear"))
                    {
                        ModuleLandingGear landingGear = gear.Modules["ModuleLandingGear"] as ModuleLandingGear;
                        if (landingGear.gearState != ModuleLandingGear.GearStates.DEPLOYED)
                        {
                            return false;  // not down
                        }
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

        public static Part GetLowestGear(List<GPWSGear> gears)
        {
            if (gears.Count <= 0)    // no gear
            {
                return null;
            }

            Part lowestGearPart = gears[0].part;
            // height from vessel to gear
            float lowestGearFromVessel = float.PositiveInfinity;
            for (int i = 0; i < gears.Count; i++)    // find lowest gear
            {
                Part p = gears[i].part;
                float gearFromVessel = AltitudeFromVessel(p);

                if (gearFromVessel < lowestGearFromVessel)
                {
                    lowestGearPart = p;
                    lowestGearFromVessel = gearFromVessel;
                }
            }
            return lowestGearPart;
        }

        /// <summary>
        /// return height from surface to the lowest landing gear, in meters
        /// </summary>
        /// <returns></returns>
        public static float GetLowestGearRadarAltitude(List<GPWSGear> gears)
        {
            return RadarAltitude(GetLowestGear(gears));
        }

        public static float RadarAltitude(Vessel v)
        {
            if (v == null)
            {
                return float.PositiveInfinity;
            }

            float terrainAltitude = Math.Max((float)v.terrainAltitude, 0);

            return (float)(v.altitude - terrainAltitude);
        }

        public static float RadarAltitude(Part p)
        {
            if (p == null)
            {
                return float.PositiveInfinity;
            }

            float radarAltitude = RadarAltitude(p.vessel);      // from vessel to surface, in meters

            return radarAltitude + AltitudeFromVessel(p);
        }

        private static float AltitudeFromVessel(Part p)
        {
            if (p == null)
            {
                return float.PositiveInfinity;
            }

            // pos of part, rotate to fit ground coord.
            Vector3 rotatedPos = p.vessel.srfRelRotation * p.orgPos;

            return -rotatedPos.z;
        }

        /// <summary>
        /// bank angle from https://github.com/Crzyrndm/Pilot-Assistant/blob/ebd426fe1a9a0fc75a674e5a45d69b1c6c66a438/PilotAssistant/Utility/FlightData.cs
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static float BankAngle(Vessel v)
        {
            // surface vectors
            Vector3d planetUp = (v.findWorldCenterOfMass() - v.mainBody.position).normalized;
            // Vessel forward and right vetors, parallel to the surface
            Vector3d surfVesRight = Vector3d.Cross(planetUp, v.ReferenceTransform.up).normalized;
            // roll
            double roll = Vector3d.Angle(surfVesRight, v.ReferenceTransform.right)
                    * Math.Sign(Vector3d.Dot(surfVesRight, v.ReferenceTransform.forward));

            float bankAngle = (float)Math.Abs(roll);

            return bankAngle;
        }

        public static void Log(String msg)
        {
            UnityEngine.Debug.Log("[GPWS]Info: " + msg);
        }
    }
}
