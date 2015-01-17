// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015
// Last modified: 2015-01-18, 02:01:18

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSP_GPWS
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GPWS : UnityEngine.MonoBehaviour
    {
        private List<GPWSGear> gearList = new List<GPWSGear>();     // parts with module "GPWSGear"

        // settings
        private bool enableGroundProximityWarning = true;
        private int[] groundProximityAltitudeArray = { 2500, 1000, 500, 400, 300, 200, 100, 50, 40, 30, 20, 10 };
        public enum UnitOfAltitude
        {
            FOOT = 0,
            METER = 1,
        };
        private UnitOfAltitude unitOfAltitude = UnitOfAltitude.FOOT;    // use meters or feet

        // Audio
        private GameObject audioPlayer = new GameObject();
        private string audioPrefix = "GPWS/Sounds";
        private float volume = 0;

        private AudioSource asGPWS = new AudioSource();

        private float lastGearHeight = float.PositiveInfinity;

        ScreenMessage screenMsg = new ScreenMessage("", 1, ScreenMessageStyle.UPPER_CENTER);

        public void Awake()
        {
        }

        public void Start()
        {
            Log("Start");
            LoadSettings();
            AudioInitialize();

            GameEvents.onVesselChange.Add(findGears);
            if (FlightGlobals.ActiveVessel != null)
            {
                findGears(FlightGlobals.ActiveVessel);
            }

            lastGearHeight = float.PositiveInfinity;
        }

        private void findGears(Vessel v)
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

        public void Update()
        {
            // check volume
            if (volume != GameSettings.VOICE_VOLUME)
            {
                UpdateVolume();
            }

            // check atmosphere
            if (!FlightGlobals.getMainBody().atmosphere ||
                    FlightGlobals.ship_altitude > FlightGlobals.getMainBody().maxAtmosphereAltitude)
            {
                return;
            }

            float gearHeight = getGearHeightFromGround();
            if (gearHeight > 0 && gearHeight < float.PositiveInfinity)
            {
                if (UnitOfAltitude.FOOT == unitOfAltitude)  // meters or feet
                {
                    gearHeight *= 3.2808399f;
                }

                // is descending
                if ((lastGearHeight != float.PositiveInfinity) && (gearHeight - lastGearHeight < 0))
                {
                    // lower than an altitude
                    foreach (float threshold in groundProximityAltitudeArray)
                    {
                        if (lastGearHeight > threshold && gearHeight < threshold)
                        {
                            // play sound
                            if (asGPWS.isPlaying)
                            {
                                asGPWS.Stop();
                            }
                            asGPWS.PlayOneShot(GameDatabase.Instance.GetAudioClip(audioPrefix + "/gpws" + threshold));
                            Log(String.Format("play " + audioPrefix + "/gpws" + threshold));
                        }
                    }
                }
                lastGearHeight = gearHeight;    // save last gear height
            }

            //showScreenMessage(unitOfAltitude.ToString() + " Height: " + gearHeight.ToString());
        }

        /// <summary>
        /// return height from surface to the lowest landing gear, in meters
        /// </summary>
        /// <returns></returns>
        public float getGearHeightFromGround()
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

        public void OnDestroy()
        {
            GameEvents.onVesselChange.Remove(findGears);
            gearList.Clear();
        }

        public void LoadSettings()
        {
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("GPWS_SETTINGS"))
            {
                if (node.HasValue("name") && node.GetValue("name") == "gpwsSettings")
                {
                    if (node.HasValue("enableGroundProximityWarning"))
                    {
                        bool.TryParse(node.GetValue("enableGroundProximityWarning"), out enableGroundProximityWarning);
                    }

                    if (node.HasValue("groundProximityAltitudeArray"))
                    {
                        String[] intstrings = node.GetValue("groundProximityAltitudeArray").Split(',');
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
                            groundProximityAltitudeArray = new int[id];
                            for (int j = 0; j < id; j++)
                            {
                                groundProximityAltitudeArray[j] = tempAlt[j];
                            }
                        }
                    }

                    if (node.HasValue("unitOfAltitude"))
                    {
                        try
                        {
                            unitOfAltitude = (UnitOfAltitude)Enum.Parse(typeof(UnitOfAltitude),
                                node.GetValue("unitOfAltitude"), true);
                        }
                        catch (Exception ex)
                        {
                            Log("Error: " + ex.Message);
                        }
                    }
                }   // End of has value "name"
            }
        }

        private void AudioInitialize()
        {
            volume = GameSettings.VOICE_VOLUME;

            asGPWS = audioPlayer.AddComponent<AudioSource>();
            asGPWS.volume = volume;
            asGPWS.panLevel = 0;
        }

        private void UpdateVolume()
        {
            volume = GameSettings.VOICE_VOLUME;
            asGPWS.volume = volume;
        }

        private void showScreenMessage(String msg)
        {
            screenMsg.message = msg;
            ScreenMessages.RemoveMessage(screenMsg);
            ScreenMessages.PostScreenMessage(screenMsg);
        }

        public static void Log(String msg)
        {
            UnityEngine.Debug.Log("[GPWS] " + msg);
        }
    }
}
