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
    public class Tools
    {
        public List<GPWSGear> gearList = new List<GPWSGear>();     // parts with module "GPWSGear"

        // Audio
        private GameObject audioPlayer = new GameObject();
        private string audioPrefix = "GPWS/Sounds";
        public float Volume { get; set; }

        private AudioSource asGPWS = new AudioSource();
        private float lastPlayTime = 0.0f;

        ScreenMessage screenMsg = new ScreenMessage("", 1, ScreenMessageStyle.UPPER_CENTER);

        public enum KindOfSound
        {
            UNAVAILABLE,
            NONE,
            SINK_RATE,
            SINK_RATE_PULL_UP,
            TERRAIN,
            TERRAIN_PULL_UP,
            DONT_SINK,
            TOO_LOW_GEAR,
            TOO_LOW_TERRAIN,
            TOO_LOW_FLAPS,
            GLIDESLOPE,
            ALTITUDE_CALLOUTS,
            BANK_ANGLE,
            WINDSHEAR,
        };
        public static KindOfSound kindOfSound
        {
            get
            {
                return _kindOfSound;
            }
            private set
            {
                _kindOfSound = value;
            }
        }
        private static KindOfSound _kindOfSound = KindOfSound.NONE;

        public void AudioInitialize()
        {
            Volume = GameSettings.VOICE_VOLUME;

            asGPWS = audioPlayer.AddComponent<AudioSource>();
            asGPWS.volume = Volume;
            asGPWS.panLevel = 0;

            kindOfSound = KindOfSound.NONE;
            lastPlayTime = Time.time;
        }

        public void UpdateVolume()
        {
            Volume = GameSettings.VOICE_VOLUME * Settings.volume;
            asGPWS.volume = Volume;
        }

        public void PlaySound(KindOfSound kind, String detail = "")
        {
            if (Time.time - lastPlayTime < 0.3f)    // check time
            {
                return;
            }

            switch (kind)
            {
                case KindOfSound.SINK_RATE:
                    if (!IsPlaying(KindOfSound.SINK_RATE) && !IsPlaying(KindOfSound.SINK_RATE_PULL_UP))
                    {
                        PlayOneShot(kind, "sink_rate");
                    }
                    break;
                case KindOfSound.SINK_RATE_PULL_UP:
                    if (!IsPlaying(KindOfSound.SINK_RATE) && !IsPlaying(KindOfSound.SINK_RATE_PULL_UP))
                    {
                        PlayOneShot(kind, "sink_rate_pull_up");
                    }
                    break;
                case KindOfSound.TERRAIN:
                    if (!IsPlaying(KindOfSound.SINK_RATE) && !IsPlaying(KindOfSound.SINK_RATE_PULL_UP)
                        && !IsPlaying(KindOfSound.TERRAIN) && !IsPlaying(KindOfSound.TERRAIN_PULL_UP))
                    {
                        PlayOneShot(kind, detail == "" ? "terrain" : detail);
                    }
                    break;
                case KindOfSound.TERRAIN_PULL_UP:
                    if (!IsPlaying(KindOfSound.SINK_RATE) && !IsPlaying(KindOfSound.SINK_RATE_PULL_UP)
                        && !IsPlaying(KindOfSound.TERRAIN_PULL_UP))
                    {
                        PlayOneShot(kind, detail == "" ? "terrain_pull_up" : detail);
                    }
                    break;
                case KindOfSound.TOO_LOW_GEAR:
                    if (!IsPlaying(Tools.KindOfSound.TOO_LOW_GEAR)
                            && !IsPlaying(Tools.KindOfSound.TOO_LOW_TERRAIN)
                            && !IsPlaying(Tools.KindOfSound.TOO_LOW_FLAPS))
                    {
                        PlayOneShot(kind, "too_low_gear");
                    }
                    break;
                case KindOfSound.ALTITUDE_CALLOUTS:
                    PlayOneShot(kind, "gpws" + detail);
                    break;
                case KindOfSound.BANK_ANGLE:
                    if (!IsPlaying(Tools.KindOfSound.BANK_ANGLE))
                    {
                        PlayOneShot(kind, "bank_angle");
                    }
                    break;
                default:
                    break;
            }
        }

        private void PlayOneShot(KindOfSound kind, String filename)
        {
            if (asGPWS.isPlaying)
            {
                asGPWS.Stop();
            }

            asGPWS.clip = GameDatabase.Instance.GetAudioClip(audioPrefix + "/" + filename);
            asGPWS.Play();

            _kindOfSound = kind;
            lastPlayTime = Time.time;
            Log(String.Format("play " + filename));
        }

        public bool IsPlaying()
        {
            return asGPWS.isPlaying;
        }

        public bool IsPlaying(KindOfSound kind)
        {
            if (!asGPWS.isPlaying)
            {
                return false;
            }
            if (kind != kindOfSound)
            {
                return false;
            }
            return true;
        }

        public bool WasPlaying(KindOfSound kind)    // was or is playing
        {
            return kind == kindOfSound;
        }

        public static void SetUnavailable()
        {
            kindOfSound = KindOfSound.UNAVAILABLE;
        }

        public static void MarkNotPlaying()
        {
            kindOfSound = Tools.KindOfSound.NONE;
        }

        public void showScreenMessage(String msg)
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
