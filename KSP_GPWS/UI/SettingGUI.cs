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

namespace KSP_GPWS.UI
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class SettingGUI : MonoBehaviour
    {
        private bool isHideUI = false;

        private float descentRateFactorExp;
        private String tooLowGearAltitudeString;

        private String touchDownSpeedString;
        private String horizontalSpeedCheckAltitudeString;

        private bool showConfigs;

        private IPlaneConfig PlaneConfig;
        private ILanderConfig LanderConfig;

        GUIStyle toggleStyle;
        GUIStyle buttonStyle;
        GUIStyle boxStyle;

        SimpleTypes.VesselType vesselType;

        public void Awake()
        {
            GameEvents.onShowUI.Add(ShowUI);
            GameEvents.onHideUI.Add(HideUI);

            PlaneConfig = Settings.PlaneConfig;
            LanderConfig = Settings.LanderConfig;

            descentRateFactorExp = (float)Math.Log10(PlaneConfig.DescentRateFactor);
            tooLowGearAltitudeString = PlaneConfig.TooLowGearAltitude.ToString();

            touchDownSpeedString = LanderConfig.TouchDownSpeed.ToString();
            horizontalSpeedCheckAltitudeString = LanderConfig.HorizontalSpeedCheckAltitude.ToString();

            showConfigs = Settings.showConfigs;

            vesselType = SimpleTypes.VesselType.NONE;
        }

        public static void toggleSettingGUI(bool active)
        {
            Settings.guiIsActive = active;
            if (!active)
            {
                if (!(Settings.UseBlizzy78Toolbar && ToolbarManager.ToolbarAvailable) && GUIAppLaunchBtn.appBtn != null)
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
                // on showConfigs changed: resize window, save config
                if (Settings.showConfigs != showConfigs)
                {
                    Settings.guiwindowPosition.height = 50;
                    Settings.showConfigs = showConfigs;
                    Settings.saveToXML();
                }
                // on vessel type changed: resize window
                if (vesselType != GPWS.ActiveVesselType)
                {
                    Settings.guiwindowPosition.height = 50;
                }
                // draw
                Settings.guiwindowPosition = GUILayout.Window("GPWSSetting".GetHashCode(), Settings.guiwindowPosition,
                        WindowFunc, "GPWS Setting", GUILayout.ExpandHeight(true));
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

        private void WindowFunc(int windowID)
        {
            PlaneConfig = Settings.PlaneConfig;
            LanderConfig = Settings.LanderConfig;
            ConfigureStyles();

            // begin drawing
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            {
                GUILayout.Box(Util.audio.GetKindOfSoundRTF(), boxStyle, GUILayout.Height(30));

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
                vesselType = GPWS.ActiveVesselType;
                if (vesselType != SimpleTypes.VesselType.NONE)
                {
                    showConfigs = GUILayout.Toggle(
                            showConfigs, String.Format("select {0} function", vesselType.ToString().ToLower()),
                            buttonStyle, GUILayout.Width(200), GUILayout.Height(20));

                    if (showConfigs)
                    {
                        drawSetting();
                    }
                }
            }
            GUILayout.EndVertical();
        }

        private void drawSetting()
        {
            // volume
            GUILayout.Label(String.Format("Volume: {0}%", Math.Round(Settings.Volume * 100.0f)));
            Settings.Volume = (float)Math.Round(GUILayout.HorizontalSlider(Settings.Volume, 0.0f, 1.0f), 2);

            switch (GPWS.ActiveVesselType)
            {
                case SimpleTypes.VesselType.PLANE:
                    drawPlaneSetting();
                    break;
                case SimpleTypes.VesselType.LANDER:
                    drawLanderSetting();
                    break;
                default:
                    break;
            }

            // save
            if (GUILayout.Button("Save", buttonStyle, GUILayout.Width(200), GUILayout.Height(30)))
            {
                float newFloat;
                if (float.TryParse(tooLowGearAltitudeString, out newFloat))
                {
                    PlaneConfig.TooLowGearAltitude = newFloat;
                }
                if (float.TryParse(touchDownSpeedString, out newFloat))
                {
                    LanderConfig.TouchDownSpeed = newFloat;
                }
                if (float.TryParse(horizontalSpeedCheckAltitudeString, out newFloat))
                {
                    LanderConfig.HorizontalSpeedCheckAltitude = newFloat;
                }
                // save
                Settings.SaveSettings();
            }
        }

        private void drawPlaneSetting()
        {
            PlaneConfig.EnableSystem =
                    GUILayout.Toggle(PlaneConfig.EnableSystem, "System Enable", toggleStyle);

            // descent rate config
            PlaneConfig.EnableDescentRate =
                    GUILayout.Toggle(PlaneConfig.EnableDescentRate, "Descent Rate", toggleStyle);
            PlaneConfig.EnableClosureToTerrain =
                    GUILayout.Toggle(PlaneConfig.EnableClosureToTerrain, "Closure to Terrain", toggleStyle);

            GUILayout.Label(String.Format("Descent Rate Factor: {0}", PlaneConfig.DescentRateFactor));
            descentRateFactorExp = GUILayout.HorizontalSlider(descentRateFactorExp, -1.0f, 1.0f);
            PlaneConfig.DescentRateFactor = (float)Math.Round(Math.Pow(10, descentRateFactorExp), 1);

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
                GUILayout.Label(Util.GetShortString(PlaneConfig.UnitOfAltitude));
            }
            GUILayout.EndHorizontal();

            // altitude
            PlaneConfig.EnableAltitudeCallouts =
                    GUILayout.Toggle(PlaneConfig.EnableAltitudeCallouts, "Altitude Callouts", toggleStyle);

            // retard
            PlaneConfig.EnableRetard =
                    GUILayout.Toggle(PlaneConfig.EnableRetard, "Retard", toggleStyle);

            // bank angle
            PlaneConfig.EnableTraffic =
                    GUILayout.Toggle(PlaneConfig.EnableTraffic, "Traffic", toggleStyle);

            // traffic
            PlaneConfig.EnableBankAngle =
                    GUILayout.Toggle(PlaneConfig.EnableBankAngle, "Bank Angle", toggleStyle);
        }

        private void drawLanderSetting()
        {
            LanderConfig.EnableSystem =
                    GUILayout.Toggle(LanderConfig.EnableSystem, "System Enable", toggleStyle);

            // descent rate
            LanderConfig.EnableDescentRate =
                    GUILayout.Toggle(LanderConfig.EnableDescentRate, "Descent Rate", toggleStyle);

            GUILayout.Label(String.Format("Safety Factor: {0}", LanderConfig.DescentRateSafetyFactor));
            LanderConfig.DescentRateSafetyFactor = (float)Math.Round(GUILayout.HorizontalSlider(LanderConfig.DescentRateSafetyFactor, 1.0f, 4.0f), 1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Final Speed");
                GUILayout.FlexibleSpace();
                touchDownSpeedString =
                        GUILayout.TextField(touchDownSpeedString, GUILayout.Height(30), GUILayout.Width(80));
                GUILayout.Label(Util.GetShortString(LanderConfig.UnitOfAltitude) + "/s");
            }
            GUILayout.EndHorizontal();

            // horizontal speed
            LanderConfig.EnableHorizontalSpeed =
                    GUILayout.Toggle(LanderConfig.EnableHorizontalSpeed, "Horizontal Speed", toggleStyle);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("vh Check Alt");
                GUILayout.FlexibleSpace();
                horizontalSpeedCheckAltitudeString =
                        GUILayout.TextField(horizontalSpeedCheckAltitudeString, GUILayout.Height(30), GUILayout.Width(80));
                GUILayout.Label(Util.GetShortString(LanderConfig.UnitOfAltitude));
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(String.Format("Horizontal Speed Factor: {0}", LanderConfig.HorizontalSpeedFactor));
            LanderConfig.HorizontalSpeedFactor = (float)Math.Round(GUILayout.HorizontalSlider(LanderConfig.HorizontalSpeedFactor, 0.01f, 1.0f), 2);

            // altitude
            LanderConfig.EnableAltitudeCallouts =
                    GUILayout.Toggle(LanderConfig.EnableAltitudeCallouts, "Altitude Callouts", toggleStyle);

            // retard
            LanderConfig.EnableRetard =
                    GUILayout.Toggle(LanderConfig.EnableRetard, "Retard", toggleStyle);
        }

        public void OnDestory()
        {
            GameEvents.onShowUI.Remove(ShowUI);
            GameEvents.onHideUI.Remove(HideUI);
            Settings.saveToXML();
        }
    }
}
