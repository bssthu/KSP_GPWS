// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP_GPWS.Interfaces;
using KSP_GPWS.SimpleTypes;

namespace KSP_GPWS.Impl
{
    public class GPWSLander : ILanderConfig, IBasicGPWSFunction
    {
        private IGPWSCommonData CommonData = null;

        #region ILanderConfig
        public bool EnableSystem { get; set; }
        public bool EnableDescentRate { get; set; }
        public bool EnableHorizontalSpeed { get; set; }
        public bool EnableAltitudeCallouts { get; set; }
        public bool EnableRetard { get; set; }

        public float TouchDownSpeed { get; set; }
        public float DescentRateCheckAltitude { get; set; }
        public float DescentRateSafetyFactor { get; set; }
        public float HorizontalSpeedCheckAltitude { get; set; }
        public float HorizontalSpeedFactor { get; set; }
        public int[] AltitudeArray { get; set; }

        /// <summary>
        /// use meters or feet, default:meter
        /// </summary>
        public UnitOfAltitude UnitOfAltitude { get; set; }
        #endregion

        #region IConfigNode Members

        public void Load(ConfigNode node)
        {
            EnableSystem = Util.ConvertValue<bool>(node, "EnableSystem", EnableSystem);
            EnableDescentRate = Util.ConvertValue<bool>(node, "EnableDescentRate", EnableDescentRate);
            EnableHorizontalSpeed = Util.ConvertValue<bool>(node, "EnableHorizontalSpeed", EnableHorizontalSpeed);
            EnableAltitudeCallouts = Util.ConvertValue<bool>(node, "EnableAltitudeCallouts", EnableAltitudeCallouts);
            EnableRetard = Util.ConvertValue<bool>(node, "EnableRetard", EnableRetard);

            TouchDownSpeed = Util.ConvertValue<float>(node, "TouchDownSpeed", TouchDownSpeed);
            DescentRateCheckAltitude = Util.ConvertValue<float>(node, "DescentRateCheckAltitude", DescentRateCheckAltitude);
            DescentRateSafetyFactor = Util.ConvertValue<float>(node, "DescentRateSafetyFactor", DescentRateSafetyFactor);
            HorizontalSpeedCheckAltitude = Util.ConvertValue<float>(node, "HorizontalSpeedCheckAltitude", HorizontalSpeedCheckAltitude);
            HorizontalSpeedFactor = Util.ConvertValue<float>(node, "HorizontalSpeedFactor", HorizontalSpeedFactor);
            if (node.HasValue("AltitudeArray"))
            {
                String[] intstrings = node.GetValue("AltitudeArray").Split(',');
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
                    AltitudeArray = new int[id];
                    for (int j = 0; j < id; j++)
                    {
                        AltitudeArray[j] = tempAlt[j];
                    }
                }
            }
            UnitOfAltitude = Util.ConvertValue<UnitOfAltitude>(node, "UnitOfAltitude", UnitOfAltitude);
            // check legality
            CheckConfigLegality();
        }

        public void Save(ConfigNode node)
        {
            node.name = "Lander";
            node.AddValue("EnableSystem", EnableSystem);
            node.AddValue("EnableDescentRate", EnableDescentRate);
            node.AddValue("EnableHorizontalSpeed", EnableHorizontalSpeed);
            node.AddValue("EnableAltitudeCallouts", EnableAltitudeCallouts);
            node.AddValue("EnableRetard", EnableRetard);

            node.AddValue("TouchDownSpeed", TouchDownSpeed);
            node.AddValue("DescentRateCheckAltitude", DescentRateCheckAltitude);
            node.AddValue("DescentRateSafetyFactor", DescentRateSafetyFactor);
            node.AddValue("HorizontalSpeedCheckAltitude", HorizontalSpeedCheckAltitude);
            node.AddValue("HorizontalSpeedFactor", HorizontalSpeedFactor);
            node.AddValue("AltitudeArray", String.Join(",", Array.ConvertAll(AltitudeArray, x => x.ToString())));
            node.AddValue("UnitOfAltitude", UnitOfAltitude);
        }

        #endregion

        public void CheckConfigLegality()
        {
            TouchDownSpeed = Math.Max(TouchDownSpeed, 0.1f);
            DescentRateSafetyFactor = Math.Max(DescentRateSafetyFactor, 1.0f);
            DescentRateSafetyFactor = Math.Min(DescentRateSafetyFactor, 4.0f);
            HorizontalSpeedFactor = Math.Max(HorizontalSpeedFactor, 0.01f);
            HorizontalSpeedFactor = Math.Min(HorizontalSpeedFactor, 1.0f);
        }

        public GPWSLander()
        {
            InitializeConfig();
        }

        public void InitializeConfig()
        {
            EnableSystem = true;
            EnableDescentRate = true;
            EnableHorizontalSpeed = true;
            EnableAltitudeCallouts = true;
            EnableRetard = true;

            TouchDownSpeed = 5;
            DescentRateCheckAltitude = 10000;
            DescentRateSafetyFactor = 1.5f;
            HorizontalSpeedCheckAltitude = 400;
            HorizontalSpeedFactor = 0.2f;
            AltitudeArray = new int[] { 2500, 1000, 500, 100, 50, 40, 30, 20, 10 };
            UnitOfAltitude = UnitOfAltitude.METER;
        }

        public void Initialize(IGPWSCommonData data)
        {
            CommonData = data;

            initializeCurves();
        }

        private void initializeCurves()
        {
        }

        public bool PreUpdate()
        {
            // on surface
            if (CommonData.ActiveVessel.LandedOrSplashed)
            {
                // landed for more than 3 sec
                if (CommonData.time - CommonData.landingTime > 3)
                {
                    Util.audio.MarkNotPlaying();
                    return false;
                }
            }

            // just take off
            if (CommonData.time - CommonData.takeOffTime < 3)
            {
                return false;
            }

            return true;
        }

        public void UpdateGPWS()
        {
            if (CommonData.RadarAltitude > 0 && CommonData.RadarAltitude < float.PositiveInfinity)
            {
                if (checkMode_sinkRate())   // Decent Rate
                { }
                else if (checkMode_hSpeed())    // Horizontal Speed
                { }
                else if (checkMode_retard())    // Throttle Check
                { }
                else if (checkMode_altitudeCallout())   // Altitude Callouts
                { }
                else if (!Util.audio.IsPlaying())
                {
                    Util.audio.MarkNotPlaying();
                }
            }
        }

        /// <summary>
        /// Descent Rate
        /// SINK RATE / MASTER WARN
        /// </summary>
        /// <returns></returns>
        private bool checkMode_sinkRate()
        {
            if (EnableDescentRate)
            {
                Vessel vessel = CommonData.ActiveVessel;
                if (CommonData.RadarAltitude < DescentRateCheckAltitude && vessel.orbit.PeA < 0 && vessel.verticalSpeed < 0)  // landing
                {
                    // only simple physics
                    double acc = Util.GetMaxAcceleration(vessel);
                    double surfaceAlt = vessel.mainBody.Radius + Math.Max(vessel.terrainAltitude, 0);
                    double surfaceG = vessel.mainBody.gravParameter / (surfaceAlt * surfaceAlt);         // g = GM / r^2
                    double vel = Math.Abs(vessel.verticalSpeed);
                    double vel0 = TouchDownSpeed;
                    if (UnitOfAltitude == SimpleTypes.UnitOfAltitude.FOOT)
                    {
                        vel0 = vel0 / Util.M_TO_FT;     // to m/s
                    }

                    // some checks
                    if (vel < vel0)
                    {
                        return false;
                    }
                    if (acc < surfaceG)
                    {
                        playSinkRate();
                        return true;
                    }

                    // I use a bigger g.
                    // (surfaceG + currentG)/2 is smaller than equivalence g.
                    // Safety first.
                    double minRA = (vel - vel0) * (vel + vel0) * 0.5 / (acc - surfaceG);

                    if (minRA * DescentRateSafetyFactor >= Util.RadarAltitude(vessel))
                    {
                        playSinkRate();
                        return true;
                    }
                }
            }
            return false;
        }

        private void playSinkRate()
        {
            // play sound
            Util.audio.PlaySound(KindOfSound.SINK_RATE);
        }

        /// <summary>
        /// Horizontal Speed
        /// HORIZONTAL SPEED
        /// </summary>
        /// <returns></returns>
        private bool checkMode_hSpeed()
        {
            if (EnableHorizontalSpeed)
            {
                if (CommonData.RadarAltitude < HorizontalSpeedCheckAltitude)
                {
                    float hSpeed = CommonData.HorSpeed;
                    float vSpeed = CommonData.VerSpeed;
                    // speed in meters/feet per s
                    if (UnitOfAltitude.FOOT == UnitOfAltitude)
                    {
                        hSpeed = hSpeed * Util.M_TO_FT;
                        vSpeed = vSpeed * Util.M_TO_FT;
                    }
                    if (vSpeed < 0)
                    {
                        if (hSpeed > (CommonData.RadarAltitude + TouchDownSpeed * 1.0f) * HorizontalSpeedFactor)
                        {
                            // play sound
                            Util.audio.PlaySound(KindOfSound.HORIZONTAL_SPEED);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Throttle Check
        /// RETARD
        /// </summary>
        /// <returns></returns>
        private bool checkMode_retard()
        {
            if (EnableRetard)
            {
                Vessel vessel = CommonData.ActiveVessel;
                if (vessel.ctrlState.mainThrottle > 0)
                {
                    // landed
                    if (vessel.LandedOrSplashed)
                    {
                        // play sound
                        Util.audio.PlaySound(KindOfSound.RETARD);
                        return true;
                    }

                    // about to land
                    if (vessel.orbit.PeA < 0 && vessel.verticalSpeed < 0)
                    {
                        double surfaceAlt = vessel.mainBody.Radius + Math.Max(vessel.terrainAltitude, 0);
                        double surfaceG = vessel.mainBody.gravParameter / (surfaceAlt * surfaceAlt);         // g = GM / r^2

                        double vel = Math.Abs(vessel.verticalSpeed);
                        double finalV_square = 2 * surfaceG * Util.RadarAltitude(vessel) + vel * vel;

                        double velDown = TouchDownSpeed;
                        if (UnitOfAltitude == SimpleTypes.UnitOfAltitude.FOOT)
                        {
                            velDown = velDown / Util.M_TO_FT;     // to m/s
                        }

                        if (finalV_square < velDown * velDown)
                        {
                            // play sound
                            Util.audio.PlaySound(KindOfSound.RETARD);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Altitude Callouts
        /// </summary>
        /// <returns></returns>
        private bool checkMode_altitudeCallout()
        {
            if (EnableAltitudeCallouts)
            {
                // is descending
                if (CommonData.RadarAltitude - CommonData.LastRadarAltitude < 0)
                {
                    // lower than an altitude
                    foreach (float threshold in AltitudeArray)
                    {
                        if (CommonData.LastRadarAltitude > threshold && CommonData.RadarAltitude < threshold)
                        {
                            // play sound
                            Util.audio.PlaySound(KindOfSound.ALTITUDE_CALLOUTS, threshold.ToString());
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void ChangeVessel(Vessel v)
        {
        }

        public void CleanUp()
        {
        }
    }
}
