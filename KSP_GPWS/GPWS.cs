// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP_GPWS.SimpleTypes;
using KSP_GPWS.Interfaces;
using KSP_GPWS.Impl;

namespace KSP_GPWS
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public partial class GPWS : MonoBehaviour, IGPWSCommonData
    {
        public Vessel ActiveVessel
        {
            get
            {
                return _activeVessel;
            }
            private set
            {
                _activeVessel = value;
            }
        }
        private static Vessel _activeVessel = null;

        public float RadarAltitude { get; set; }

        public float Altitude { get; set; }

        public float HorSpeed { get;  private set; }

        public float VerSpeed { get; private set; }

        public float Speed { get; private set; }

        private Vessel LastActiveVessel = null;

        public float LastRadarAltitude { get; private set; }

        public float LastAltitude { get; private set; }

        public float LastHorSpeed { get; private set; }

        public float LastVerSpeed { get; private set; }

        // time scene loaded
        private float t0 = 0.0f;

        /// time in seconds since scene loaded
        public float time { get; private set; }

        public float lastTime { get; private set; }

        /// <summary>
        /// time of takeoff
        /// </summary>
        public float takeOffTime { get; private set; }

        /// <summary>
        /// time of landing/splashing
        /// </summary>
        public float landingTime { get; private set; }

        private static GPWSPlane Plane = null;
        private static GPWSLander Lander = null;

        private IBasicGPWSFunction GPWSFunc;

        public static SimpleTypes.VesselType ActiveVesselType
        {
            get
            {
                bool isPlane = false;
                bool isLander = false;

                if (_activeVessel == null || _activeVessel.isEVA)
                {
                    return SimpleTypes.VesselType.NONE;
                }
                if (Plane != null && Plane.GearCount > 0)
                {
                    isPlane = true;
                }
                else if (Lander != null)
                {
                    isLander = true;
                }

                if ((isPlane && !Settings.ChangeVesselType) || (isLander && Settings.ChangeVesselType))
                {
                    return SimpleTypes.VesselType.PLANE;
                }
                else if ((isLander && !Settings.ChangeVesselType) || (isPlane && Settings.ChangeVesselType))
                {
                    return SimpleTypes.VesselType.LANDER;
                }
                else
                {
                    return SimpleTypes.VesselType.NONE;
                }
            }
        }

        public static void InitializeGPWSFunctions()
        {
            if (Plane == null && Lander == null)    // call once
            {
                Plane = new GPWSPlane();
                Settings.PlaneConfig = Plane as IPlaneConfig;
                Lander = new GPWSLander();
                Settings.LanderConfig = Lander as ILanderConfig;
            }
        }

        public void Awake()
        {
            Plane.Initialize(this as IGPWSCommonData);
            Lander.Initialize(this as IGPWSCommonData);
            initializeVariables();
        }

        private void initializeVariables()
        {
            ActiveVessel = null;

            RadarAltitude = 0.0f;
            Altitude = 0.0f;
            HorSpeed = 0.0f;
            VerSpeed = 0.0f;
            LastRadarAltitude = float.PositiveInfinity;
            LastAltitude = float.PositiveInfinity;
            LastHorSpeed = 0.0f;
            LastVerSpeed = 0.0f;

            takeOffTime = float.NegativeInfinity;
            landingTime = float.NegativeInfinity;

            Settings.ChangeVesselType = false;

            t0 = Time.time;
            time = t0;
            lastTime = t0;
        }

        public void Start()
        {
            Util.Log("Start");
            Util.audio.AudioInitialize();

            ActiveVessel = FlightGlobals.ActiveVessel;
            OnVesselChange(ActiveVessel);
        }

        private void OnVesselChange(Vessel v)
        {
            Plane.ChangeVessel(ActiveVessel);
            Lander.ChangeVessel(ActiveVessel);
        }

        private bool preUpdate()
        {
            time = Time.time - t0;
            ActiveVessel = FlightGlobals.ActiveVessel;

            // on surface
            if (ActiveVessel.LandedOrSplashed)
            {
                takeOffTime = time;
            }
            else
            {
                landingTime = time;
            }

            // check time, prevent problem
            if (time < 2.0f)
            {
                Util.audio.SetUnavailable();
                return false;
            }

            // just switched, use new vessel
            if (ActiveVessel != LastActiveVessel)
            {
                OnVesselChange(ActiveVessel);
            }

            // check vessel type
            if (ActiveVesselType == SimpleTypes.VesselType.PLANE)
            {
                GPWSFunc = Plane as IBasicGPWSFunction;
            }
            else if (ActiveVesselType == SimpleTypes.VesselType.LANDER)
            {
                GPWSFunc = Lander as IBasicGPWSFunction;
            }
            else
            {
                Util.audio.SetUnavailable();
                return false;
            }

            // height in meters/feet
            if (UnitOfAltitude.FOOT == GPWSFunc.UnitOfAltitude)
            {
                RadarAltitude = Util.RadarAltitude(ActiveVessel) * Util.M_TO_FT;
                Altitude = (float)(FlightGlobals.ship_altitude * Util.M_TO_FT);
            }
            else
            {
                RadarAltitude = Util.RadarAltitude(ActiveVessel);
                Altitude = (float)FlightGlobals.ship_altitude;
            }

            // speed
            HorSpeed = (float)ActiveVessel.horizontalSrfSpeed;
            VerSpeed = (float)ActiveVessel.verticalSpeed;
            Speed = (float)Math.Sqrt(HorSpeed * HorSpeed + VerSpeed * VerSpeed);

            // check volume
            if (Util.audio.Volume != GameSettings.VOICE_VOLUME * Settings.Volume)
            {
                Util.audio.UpdateVolume();
            }

            // if vessel changed, don't update
            if (ActiveVessel != LastActiveVessel)
            {
                Util.audio.SetUnavailable();
                return false;
            }

            if (!GPWSFunc.PreUpdate())
            {
                return false;
            }

            // enable
            if (!GPWSFunc.EnableSystem)
            {
                Util.audio.SetUnavailable();
                return false;
            }

            return true;
        }

        void UpdateGPWS()
        {
            GPWSFunc.UpdateGPWS();
        }

        public void Update()
        {
            if (preUpdate())
            {
                UpdateGPWS();
            }

            saveData();
        }

        private void saveData() // after Update
        {
            LastRadarAltitude = RadarAltitude;    // save last gear height
            LastAltitude = Altitude;
            LastHorSpeed = HorSpeed;    // save last speed
            LastVerSpeed = VerSpeed;
            lastTime = time;        // save time of last frame
            LastActiveVessel = ActiveVessel;
        }

        public void OnDestroy()
        {
            Plane.CleanUp();
            Lander.CleanUp();
        }
    }
}
