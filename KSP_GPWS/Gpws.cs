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
    public partial class Gpws : MonoBehaviour, IGpwsCommonData
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
        public float CurrentTime { get; private set; }

        public float LastTime { get; private set; }

        /// <summary>
        /// time of takeoff
        /// </summary>
        public float TakeOffTime { get; private set; }

        /// <summary>
        /// time of landing/splashing
        /// </summary>
        public float LandingTime { get; private set; }

        private static GpwsPlane plane = null;
        private static GpwsLander lander = null;

        private IBasicGpwsFunction gpwsFunc;

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
                if (plane != null && plane.GearCount > 0)
                {
                    isPlane = true;
                }
                else if (lander != null)
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
            if (plane == null && lander == null)    // call once
            {
                plane = new GpwsPlane();
                Settings.PlaneConfig = plane as IPlaneConfig;
                lander = new GpwsLander();
                Settings.LanderConfig = lander as ILanderConfig;
            }
        }

        public void Awake()
        {
            plane.Initialize(this as IGpwsCommonData);
            lander.Initialize(this as IGpwsCommonData);
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

            TakeOffTime = float.NegativeInfinity;
            LandingTime = float.NegativeInfinity;

            Settings.ChangeVesselType = false;

            t0 = Time.time;
            CurrentTime = t0;
            LastTime = t0;
        }

        public void Start()
        {
            Util.Log("Start");
            Util.audio.Initialize();

            ActiveVessel = FlightGlobals.ActiveVessel;
            OnVesselChange(ActiveVessel);
        }

        private void OnVesselChange(Vessel v)
        {
            plane.ChangeVessel(v);
            lander.ChangeVessel(v);
            Util.audio.Stop();
        }

        private bool PreUpdate()
        {
            CurrentTime = Time.time - t0;
            ActiveVessel = FlightGlobals.ActiveVessel;

            // on surface
            if (ActiveVessel.LandedOrSplashed)
            {
                TakeOffTime = CurrentTime;
            }
            else
            {
                LandingTime = CurrentTime;
            }

            // check time, prevent problem
            if (CurrentTime < 2.0f)
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
                gpwsFunc = plane as IBasicGpwsFunction;
            }
            else if (ActiveVesselType == SimpleTypes.VesselType.LANDER)
            {
                gpwsFunc = lander as IBasicGpwsFunction;
            }
            else
            {
                Util.audio.SetUnavailable();
                return false;
            }

            // height in meters/feet
            if (UnitOfAltitude.FOOT == gpwsFunc.UnitOfAltitude)
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

            // check shake
            Util.controller.CheckResetShake();

            // if vessel changed, don't update
            if (ActiveVessel != LastActiveVessel)
            {
                Util.audio.SetUnavailable();
                return false;
            }

            if (!gpwsFunc.PreUpdate())
            {
                return false;
            }

            // enable
            if (!gpwsFunc.EnableSystem)
            {
                Util.audio.SetUnavailable();
                return false;
            }

            return true;
        }

        void UpdateGPWS()
        {
            gpwsFunc.UpdateGPWS();
        }

        public void Update()
        {
            if (PreUpdate())
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
            LastTime = CurrentTime;        // save time of last frame
            LastActiveVessel = ActiveVessel;
        }

        public void OnDestroy()
        {
            plane.CleanUp();
            lander.CleanUp();

            Util.controller.ResetShake();
        }
    }
}
