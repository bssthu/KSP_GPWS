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
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class SettingGUI : MonoBehaviour
    {
        private bool isHideUI = false;

        private String descentRateFactorString;
        private String tooLowGearAltitudeString;
        private bool showConfigs;

        public void Awake()
        {
            GameEvents.onShowUI.Add(ShowUI);
            GameEvents.onHideUI.Add(HideUI);
            descentRateFactorString = Settings.descentRateFactor.ToString();
            tooLowGearAltitudeString = Settings.tooLowGearAltitude.ToString();
            showConfigs = Settings.showConfigs;
        }

        public static void toggleSettingGUI()
        {
            Settings.guiIsActive = !Settings.guiIsActive;
            if (!Settings.guiIsActive)
            {
                if (!Settings.useBlizzy78Toolbar && GUIAppLaunchBtn.appBtn != null)
                {
                    GUIAppLaunchBtn.appBtn.SetFalse(false);
                }
            }
        }

        public void HideUI()
        {
            isHideUI = true;
        }

        public void ShowUI()
        {
            isHideUI = false;
        }

        public void OnGUI()
        {
            if (Settings.guiIsActive && !isHideUI)
            {
                GUI.skin = HighLogic.Skin;
                // on showConfigs changed: resize window
                if (Settings.showConfigs != showConfigs)
                {
                    Settings.guiwindowPosition.height = 50;
                    Settings.showConfigs = showConfigs;
                }
                // draw
                Settings.guiwindowPosition = GUILayout.Window("GPWSSetting".GetHashCode(), Settings.guiwindowPosition,
                        SettingWindowFunc, "GPWS Setting", GUILayout.ExpandHeight(true));
            }
        }

        private void SettingWindowFunc(int windowID)
        {
            GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
            toggleStyle.stretchHeight = true;
            toggleStyle.stretchWidth = true;
            toggleStyle.padding = new RectOffset(4, 4, 4, 4);
            toggleStyle.margin = new RectOffset(4, 4, 4, 4);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.stretchHeight = true;
            buttonStyle.stretchWidth = true;
            buttonStyle.padding = new RectOffset(4, 4, 4, 4);
            buttonStyle.margin = new RectOffset(4, 4, 4, 4);

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.stretchHeight = true;
            boxStyle.stretchWidth = true;
            boxStyle.padding = new RectOffset(4, 4, 4, 4);
            boxStyle.margin = new RectOffset(4, 4, 4, 4);
            boxStyle.richText = true;

            // begin drawing
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            {
                String text = Tools.kindOfSound.ToString();
                if (!Settings.enableSystem)
                {
                    text = "UNAVAILABLE";
                }
                if (text == "UNAVAILABLE")
                {
                    text = "<color=white>" + text + "</color>";
                }
                else if (text != "NONE")
                {
                    text = "<color=red>" + text + "</color>";
                }
                GUILayout.Box(text, boxStyle, GUILayout.Height(30));

                drawConfigUI(toggleStyle, boxStyle, buttonStyle);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUI.DragWindow();   // allow moving window
        }

        private void drawConfigUI(GUIStyle toggleStyle, GUIStyle boxStyle, GUIStyle buttonStyle)
        {
            GUILayout.BeginVertical();
            {
                showConfigs = GUILayout.Toggle(
                        showConfigs, "select function", buttonStyle, GUILayout.Width(200), GUILayout.Height(20));

                if (showConfigs)
                {
                    Settings.enableSystem =
                            GUILayout.Toggle(Settings.enableSystem, "System", toggleStyle);

                    // descent rate config
                    Settings.enableDescentRate =
                            GUILayout.Toggle(Settings.enableDescentRate, "Descent Rate", toggleStyle);
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Descent Rate *");
                        GUILayout.FlexibleSpace();
                        descentRateFactorString =
                                GUILayout.TextField(descentRateFactorString, GUILayout.Height(30), GUILayout.Width(80));
                    }
                    GUILayout.EndHorizontal();

                    // terrain clearance
                    Settings.enableTerrainClearance =
                            GUILayout.Toggle(Settings.enableTerrainClearance, "Terrain Clearance", toggleStyle);
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Gear Alt");
                        GUILayout.FlexibleSpace();
                        tooLowGearAltitudeString =
                                GUILayout.TextField(tooLowGearAltitudeString, GUILayout.Height(30), GUILayout.Width(80));
                    }
                    GUILayout.EndHorizontal();

                    // altitude
                    Settings.enableAltitudeCallouts =
                            GUILayout.Toggle(Settings.enableAltitudeCallouts, "Altitude Callouts", toggleStyle);

                    // bank angle
                    Settings.enableBankAngle =
                            GUILayout.Toggle(Settings.enableBankAngle, "Bank Angle", toggleStyle);

                    // save
                    if (GUILayout.Button("Save", buttonStyle, GUILayout.Width(200), GUILayout.Height(30)))
                    {
                        float newDescentRateFactor;
                        if (float.TryParse(descentRateFactorString, out newDescentRateFactor))
                        {
                            Settings.descentRateFactor = newDescentRateFactor;
                        }
                        float newTooLowGearAltitude;
                        if (float.TryParse(tooLowGearAltitudeString, out newTooLowGearAltitude))
                        {
                            Settings.tooLowGearAltitude = newTooLowGearAltitude;
                        }
                        // save
                        Settings.SaveSettings();
                    }
                }
            }
            GUILayout.EndVertical();
        }

        public void OnDestory()
        {
            GameEvents.onShowUI.Remove(ShowUI);
            GameEvents.onHideUI.Remove(HideUI);
            Settings.saveToXML();
        }
    }
}
