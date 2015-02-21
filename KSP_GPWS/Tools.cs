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
            NONE,
            SINK_RATE,
            WOOP_WOOP_PULL_UP,
            TERRAIN,
            DONT_SINK,
            TOO_LOW_GEAR,
            TOO_LOW_TERRAIN,
            TOO_LOW_FLAPS,
            GLIDESLOPE,
            ALTITUDE_CALLOUTS,
            BANK_ANGLE,
            WINDSHEAR,
        };
        public KindOfSound kindOfSound = KindOfSound.NONE;    // use meters or feet, feet is recommanded.

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
            Volume = GameSettings.VOICE_VOLUME;
            asGPWS.volume = Volume;
        }

        public void PlayOneShot(String filename)
        {
            if (Time.time - lastPlayTime < 0.1f)    // check time
            {
                return;
            }

            if (asGPWS.isPlaying)
            {
                asGPWS.Stop();
            }
            asGPWS.clip = GameDatabase.Instance.GetAudioClip(audioPrefix + "/" + filename);
            asGPWS.Play();
            lastPlayTime = Time.time;
            Log(String.Format("play " + filename));
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
