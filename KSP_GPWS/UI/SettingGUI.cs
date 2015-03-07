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

namespace KSP_GPWS
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class SettingGUI : MonoBehaviour
    {
        private bool isHideUI = false;

        private float _descentRateFactor;
        private String tooLowGearAltitudeString;
        private bool showConfigs;

        private IPlaneConfig PlaneConfig;

        GUIStyle toggleStyle;
        GUIStyle buttonStyle;
        GUIStyle boxStyle;

        public void Awake()
        {
            GameEvents.onShowUI.Add(ShowUI);
            GameEvents.onHideUI.Add(HideUI);

            PlaneConfig = Settings.PlaneConfig;
            _descentRateFactor = (float)Math.Log10(Settings.PlaneConfig.DescentRateFactor);
            tooLowGearAltitudeString = Settings.PlaneConfig.TooLowGearAltitude.ToString();
            showConfigs = Settings.showConfigs;
        }

        public static void toggleSettingGUI(bool active)
        {
            Settings.guiIsActive = active;
            if (!active)
            {
                if (!Settings.UseBlizzy78Toolbar && GUIAppLaunchBtn.appBtn != null)
                {
                    GUIAppLaunchBtn.appBtn.SetFalse(false);
                }
            }
            Settings.saveToXML();
        }

        public static void toggleSettingGUI()
        {
            toggleSettingGUI(!Settings.guiIsActive);
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
                // on showConfigs changed: resize window, etc
                if (Settings.showConfigs != showConfigs)
                {
                    Settings.guiwindowPosition.height = 50;
                    Settings.showConfigs = showConfigs;
                    Settings.saveToXML();
                }
                // draw
                Settings.guiwindowPosition = GUILayout.Window("GPWSSetting".GetHashCode(), Settings.guiwindowPosition,
                        SettingWindowFunc, "GPWS Setting", GUILayout.ExpandHeight(true));
            }
        }

        private void ConfigureStyles()
        {
            if (toggleStyle == null)
            {
                toggleStyle = new GUIStyle(GUI.skin.toggle);
                toggleStyle.stretchHeight = true;
                toggleStyle.stretchWidth = true;
                toggleStyle.padding = new RectOffset(4, 4, 4, 4);
                toggleStyle.margin = new RectOffset(4, 4, 4, 4);
            }

            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.stretchHeight = true;
                buttonStyle.stretchWidth = true;
                buttonStyle.padding = new RectOffset(4, 4, 4, 4);
                buttonStyle.margin = new RectOffset(4, 4, 4, 4);
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.stretchHeight = true;
                boxStyle.stretchWidth = true;
                boxStyle.padding = new RectOffset(4, 4, 4, 4);
                boxStyle.margin = new RectOffset(4, 4, 4, 4);
                boxStyle.richText = true;
            }
        }

        private void SettingWindowFunc(int windowID)
        {
            PlaneConfig = Settings.PlaneConfig;
            ConfigureStyles();

            // begin drawing
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            {
                String text = Util.audio.kindOfSound.ToString();
                if (!PlaneConfig.EnableSystem)
                {
                    text = "UNAVAILABLE";
                }
                if (text == "UNAVAILABLE")
                {
                    text = "<color=white>" + text + "</color>";
                }
                else if (text != "NONE" && text != "ALTITUDE_CALLOUTS")
                {
                    text = "<color=red>" + text + "</color>";
                }
                GUILayout.Box(text, boxStyle, GUILayout.Height(30));

                drawConfigUI();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUI.DragWindow();   // allow moving window
        }

        private void drawConfigUI()
        {
            GUILayout.BeginVertical();
            {
                showConfigs = GUILayout.Toggle(
                        showConfigs, "select function", buttonStyle, GUILayout.Width(200), GUILayout.Height(20));

                if (showConfigs)
                {
                    drawPlaneSetting();
                }
            }
            GUILayout.EndVertical();
        }

        private void drawPlaneSetting()
        {
            PlaneConfig.EnableSystem =
                    GUILayout.Toggle(PlaneConfig.EnableSystem, "System", toggleStyle);

            // volume
            GUILayout.Label(String.Format("Volume: {0}%", Math.Round(Settings.Volume * 100.0f)));
            Settings.Volume = (float)Math.Round(GUILayout.HorizontalSlider(Settings.Volume, 0.0f, 1.0f), 2);

            // descent rate config
            PlaneConfig.EnableDescentRate =
                    GUILayout.Toggle(PlaneConfig.EnableDescentRate, "Descent Rate", toggleStyle);
            PlaneConfig.EnableClosureToTerrain =
                    GUILayout.Toggle(PlaneConfig.EnableClosureToTerrain, "Closure to Terrain", toggleStyle);

            GUILayout.Label(String.Format("Descent Rate Factor: {0}", PlaneConfig.DescentRateFactor));
            _descentRateFactor = GUILayout.HorizontalSlider(_descentRateFactor, -1.0f, 1.0f);
            PlaneConfig.DescentRateFactor = (float)Math.Round(Math.Pow(10, _descentRateFactor), 1);

            // altitude loss
            PlaneConfig.EnableAltitudeLoss =
                    GUILayout.Toggle(PlaneConfig.EnableAltitudeLoss, "Altitude Loss After Takeoff", toggleStyle);

            // terrain clearance
            PlaneConfig.EnableTerrainClearance =
                    GUILayout.Toggle(PlaneConfig.EnableTerrainClearance, "Terrain Clearance", toggleStyle);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Gear Alt");
                GUILayout.FlexibleSpace();
                tooLowGearAltitudeString =
                        GUILayout.TextField(tooLowGearAltitudeString, GUILayout.Height(30), GUILayout.Width(80));
                GUILayout.Label(PlaneConfig.UnitOfAltitude == UnitOfAltitude.FOOT ? "ft" : "m");
            }
            GUILayout.EndHorizontal();

            // altitude
            PlaneConfig.EnableAltitudeCallouts =
                    GUILayout.Toggle(PlaneConfig.EnableAltitudeCallouts, "Altitude Callouts", toggleStyle);

            // bank angle
            PlaneConfig.EnableTraffic =
                    GUILayout.Toggle(PlaneConfig.EnableTraffic, "Traffic", toggleStyle);

            // traffic
            PlaneConfig.EnableBankAngle =
                    GUILayout.Toggle(PlaneConfig.EnableBankAngle, "Bank Angle", toggleStyle);

            // save
            if (GUILayout.Button("Save", buttonStyle, GUILayout.Width(200), GUILayout.Height(30)))
            {
                float newTooLowGearAltitude;
                if (float.TryParse(tooLowGearAltitudeString, out newTooLowGearAltitude))
                {
                    PlaneConfig.TooLowGearAltitude = newTooLowGearAltitude;
                }
                // save
                Settings.SaveSettings();
            }
        }

        public void OnDestory()
        {
            GameEvents.onShowUI.Remove(ShowUI);
            GameEvents.onHideUI.Remove(HideUI);
            Settings.saveToXML();
        }
    }
}
