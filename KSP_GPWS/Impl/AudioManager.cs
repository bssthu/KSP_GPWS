// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP_GPWS.SimpleTypes;

namespace KSP_GPWS.Impl
{
    public class AudioManager
    {
        private GameObject audioPlayer;
        private string audioPrefix = "GPWS/Sounds";
        public float Volume { get; set; }

        private AudioSource asGPWS;
        private float lastPlayTime = 0.0f;

        public KindOfSound KindOfSound
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

        private String detail = "";

        public void AudioInitialize()
        {
            Volume = GameSettings.VOICE_VOLUME;

            if (audioPlayer == null)
            {
                audioPlayer = new GameObject();
            }
            if (asGPWS == null)
            {
                asGPWS = new AudioSource();
            }
            asGPWS = audioPlayer.AddComponent<AudioSource>();
            asGPWS.volume = Volume;
            asGPWS.panLevel = 0;

            KindOfSound = KindOfSound.NONE;
            lastPlayTime = Time.time;
        }

        public void UpdateVolume()
        {
            Volume = GameSettings.VOICE_VOLUME * Settings.Volume;
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
                case KindOfSound.DONT_SINK:
                    if (!IsPlaying(KindOfSound.SINK_RATE) && !IsPlaying(KindOfSound.SINK_RATE_PULL_UP)
                        && !IsPlaying(KindOfSound.TERRAIN) && !IsPlaying(KindOfSound.TERRAIN_PULL_UP)
                        && !IsPlaying(KindOfSound.DONT_SINK))
                    {
                        PlayOneShot(kind, "dont_sink");
                    }
                    break;
                case KindOfSound.TOO_LOW_GEAR:
                    if (!IsPlaying(KindOfSound.TOO_LOW_GEAR)
                            && !IsPlaying(KindOfSound.TOO_LOW_TERRAIN)
                            && !IsPlaying(KindOfSound.TOO_LOW_FLAPS))
                    {
                        PlayOneShot(kind, "too_low_gear");
                    }
                    break;
                case KindOfSound.TOO_LOW_TERRAIN:
                    if (!IsPlaying(KindOfSound.TOO_LOW_GEAR)
                            && !IsPlaying(KindOfSound.TOO_LOW_TERRAIN)
                            && !IsPlaying(KindOfSound.TOO_LOW_FLAPS))
                    {
                        PlayOneShot(kind, "too_low_terrain");
                    }
                    break;
                case KindOfSound.TRAFFIC:
                    if (!IsPlaying(KindOfSound.TRAFFIC))
                    {
                        PlayOneShot(kind, "traffic");
                    }
                    break;
                case KindOfSound.ALTITUDE_CALLOUTS:
                    this.detail = detail;
                    PlayOneShot(kind, "gpws" + detail);
                    break;
                case KindOfSound.BANK_ANGLE:
                    if (!IsPlaying(KindOfSound.BANK_ANGLE))
                    {
                        PlayOneShot(kind, "bank_angle");
                    }
                    break;
                case KindOfSound.HORIZONTAL_SPEED:
                    if (!IsPlaying(KindOfSound.SINK_RATE) && !IsPlaying(KindOfSound.SINK_RATE_PULL_UP))
                    {
                        PlayOneShot(kind, "horizontal_speed");
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
            Util.Log(String.Format("play " + filename));
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
            if (kind != KindOfSound)
            {
                return false;
            }
            return true;
        }

        public bool WasPlaying(KindOfSound kind)    // was or is playing
        {
            return kind == KindOfSound;
        }

        public void SetUnavailable()
        {
            KindOfSound = KindOfSound.UNAVAILABLE;
        }

        public void MarkNotPlaying()
        {
            KindOfSound = KindOfSound.NONE;
        }

        /// <summary>
        /// get KindOfSound in string in RTF format
        /// </summary>
        /// <returns></returns>
        public String GetKindOfSoundRTF()
        {
            String rtfText = KindOfSound.ToString();
            if (KindOfSound == KindOfSound.UNAVAILABLE)
            {
                rtfText = "<color=white>" + rtfText + "</color>";
            }
            else if (KindOfSound == KindOfSound.ALTITUDE_CALLOUTS)
            {
                UnitOfAltitude unit = UnitOfAltitude.NONE;
                if (GPWS.ActiveVesselType == SimpleTypes.VesselType.PLANE)
                {
                    unit = Settings.PlaneConfig.UnitOfAltitude;
                }
                else if (GPWS.ActiveVesselType == SimpleTypes.VesselType.LANDER)
                {
                    unit = Settings.LanderConfig.UnitOfAltitude;
                }
                rtfText = detail + Util.GetShortString(unit);
            }
            else if (KindOfSound != KindOfSound.NONE)
            {
                rtfText = "<color=red>" + rtfText + "</color>";
            }
            return rtfText;
        }
    }
}
