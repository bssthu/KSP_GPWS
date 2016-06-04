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
using ModuleWheels;

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
        public static AudioManager audio
        {
            get
            {
                return _audio;
            }
        }
        private static AudioManager _audio = new AudioManager();

        /// <summary>
        /// XInput
        /// </summary>
        public static ControlerManager controller
        {
            get
            {
                return _controller;
            }
        }
        private static ControlerManager _controller = new ControlerManager();

        private static ScreenMessage screenMsg = new ScreenMessage("", 1, ScreenMessageStyle.UPPER_CENTER);

        public static void ShowScreenMessage(String msg)
        {
            screenMsg.message = msg;
            ScreenMessages.RemoveMessage(screenMsg);
            ScreenMessages.PostScreenMessage(screenMsg);
        }

        public static bool IsWin32()
        {
            return (IntPtr.Size == 4) && (Environment.OSVersion.Platform == PlatformID.Win32NT);
        }

        public static bool IsWin64()
        {
            return (IntPtr.Size == 8) && (Environment.OSVersion.Platform == PlatformID.Win32NT);
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

        public static void UpdateGearList(Vessel v, ref List<PartModule> gears)
        {
            gears.Clear();

            if (null == v)
            {
                return;
            }

            for (int i = 0; i < v.parts.Count; i++)    // it is said that foreach costs more memory due to Unity Mono issues
            {
                Part p = v.parts[i];
                if (p.Modules.Contains<GPWSGear>())
                {
                    Util.Log("found one!!!");
                    gears.Add(p.Modules.GetModule<GPWSGear>());
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
                    if (gear.Modules.Contains<ModuleWheelDeployment>())
                    {
                        ModuleWheelDeployment m = gear.Modules.GetModule<ModuleWheelDeployment>();
                        if (m.stateString != "Deployed")
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

        public static Part GetLowestGear(List<PartModule> gears)
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
        public static float GetLowestGearRadarAltitude(List<PartModule> gears)
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

        /// <summary>
        /// AOA from SteamGauges.AirGauge
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static float Aoa(Vessel v)
        {
            Transform self = v.ReferenceTransform;
            return AngleAroundNormal(v.GetSrfVelocity(), self.up, self.right) * -1f;
        }

        // return signed angle in relation to normal's 2d plane
        // From NavyFish's docking alignment
        private static float AngleAroundNormal(Vector3 a, Vector3 b, Vector3 up)
        {
            return AngleSigned(Vector3.Cross(up, a), Vector3.Cross(up, b), up);
        }

        // -180 to 180 angle
        // From NavyFish's docking alignment
        private static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 up)
        {
            if (Vector3.Dot(Vector3.Cross(v1, v2), up) < 0) //greater than 90 i.e v1 left of v2
                return -Vector3.Angle(v1, v2);
            return Vector3.Angle(v1, v2);
        }

        /// <summary>
        /// max acc, in m/s
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static float GetMaxAcceleration(Vessel v)
        {
            return GetMaxThrust(v) / v.GetTotalMass();  // kN / T
        }

        /// <summary>
        /// max thrust, in kN
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static float GetMaxThrust(Vessel v)
        {
            float maxThrust = 0.0f;

            for (int i = 0; i < v.Parts.Count; i++)
            {
                if (v.Parts[i].Modules.Contains<ModuleEngines>())
                {
                    ModuleEngines me = v.Parts[i].Modules.GetModule<ModuleEngines>();
                    if (!me.engineShutdown && me.EngineIgnited && !me.flameout)
                    {
                        float me_isp = me.atmosphereCurve.Evaluate((float)(v.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres));
                        float g0 = 9.82f;
                        float me_maxThrust = me.maxFuelFlow * g0 * me_isp;
                        maxThrust += me_maxThrust;
                    }
                }
                if (v.Parts[i].Modules.Contains<ModuleEnginesFX>())
                {
                    ModuleEnginesFX me = v.Parts[i].Modules.GetModule<ModuleEnginesFX>();
                    if (!me.engineShutdown && me.EngineIgnited && !me.flameout)
                    {
                        float me_isp = me.atmosphereCurve.Evaluate((float)(v.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres));
                        float g0 = 9.82f;
                        float me_maxThrust = me.maxFuelFlow * g0 * me_isp;
                        maxThrust += me_maxThrust;
                    }
                }
            }

            return maxThrust;
        }

        public static String GetShortString(UnitOfAltitude unitOfAltitude)
        {
            if (unitOfAltitude == UnitOfAltitude.FOOT)
            {
                return "ft";
            }
            else if (unitOfAltitude == UnitOfAltitude.METER)
            {
                return "m";
            }
            else
            {
                return "";
            }
        }

        public static void Log(String msg)
        {
            UnityEngine.Debug.Log("[GPWS]Info: " + msg);
        }
    }
}
