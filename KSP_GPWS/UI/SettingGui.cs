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
    class SettingGui : MonoBehaviour
    {
        private bool isHideUI = false;

        private float descentRateFactorExp;
        private String tooLowGearAltitudeString;

        private String touchDownSpeedString;
        private String horizontalSpeedCheckAltitudeString;

        private bool showConfigs;

        private IPlaneConfig planeConfig;
        private ILanderConfig landerConfig;

        GUIStyle toggleStyle;
        GUIStyle buttonStyle;
        GUIStyle boxStyle;

        SimpleTypes.VesselType vesselType;

        public void Awake()
        {
            GameEvents.onShowUI.Add(ShowUI);
            GameEvents.onHideUI.Add(HideUI);

            planeConfig = Settings.PlaneConfig;
            landerConfig = Settings.LanderConfig;

            descentRateFactorExp = (float)Math.Log10(planeConfig.DescentRateFactor);
            tooLowGearAltitudeString = planeConfig.TooLowGearAltitude.ToString();

            touchDownSpeedString = landerConfig.TouchDownSpeed.ToString();
            horizontalSpeedCheckAltitudeString = landerConfig.HorizontalSpeedCheckAltitude.ToString();

            showConfigs = Settings.showConfigs;

            vesselType = SimpleTypes.VesselType.NONE;
        }

        public static void toggleSettingGui(bool active)
        {
            Settings.guiIsActive = active;
            if (!active)
            {
                if (!(Settings.UseBlizzy78Toolbar && ToolbarManager.ToolbarAvailable) && GuiAppLaunchBtn.appBtn != null)
                {
                    GuiAppLaunchBtn.appBtn.SetFalse(false);
                }
            }
            Settings.SaveToXml();
        }

        public static void toggleSettingGui()
        {
            toggleSettingGui(!Settings.guiIsActive);
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
                    Settings.SaveToXml();
                }
                // on vessel type changed: resize window
                if (vesselType != Gpws.ActiveVesselType)
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
            planeConfig = Settings.PlaneConfig;
            landerConfig = Settings.LanderConfig;
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
                vesselType = Gpws.ActiveVesselType;
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
            Settings.ChangeVesselType = GUILayout.Toggle(Settings.ChangeVesselType, "change vessel type",
                    buttonStyle, GUILayout.Width(200), GUILayout.Height(20));
            // volume
            GUILayout.Label(String.Format("Volume: {0}%", Math.Round(Settings.Volume * 100.0f)));
            Settings.Volume = (float)Math.Round(GUILayout.HorizontalSlider(Settings.Volume, 0.0f, 1.0f), 2);

            switch (Gpws.ActiveVesselType)
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
                    planeConfig.TooLowGearAltitude = newFloat;
                }
                if (float.TryParse(touchDownSpeedString, out newFloat))
                {
                    landerConfig.TouchDownSpeed = newFloat;
                }
                if (float.TryParse(horizontalSpeedCheckAltitudeString, out newFloat))
                {
                    landerConfig.HorizontalSpeedCheckAltitude = newFloat;
                }
                // save
                Settings.SaveSettings();
            }
        }

        private void drawPlaneSetting()
        {
            planeConfig.EnableSystem =
                    GUILayout.Toggle(planeConfig.EnableSystem, "System Enable", toggleStyle);

            // descent rate config
            planeConfig.EnableDescentRate =
                    GUILayout.Toggle(planeConfig.EnableDescentRate, "Descent Rate", toggleStyle);
            planeConfig.EnableClosureToTerrain =
                    GUILayout.Toggle(planeConfig.EnableClosureToTerrain, "Closure to Terrain", toggleStyle);

            GUILayout.Label(String.Format("Descent Rate Factor: {0}", planeConfig.DescentRateFactor));
            descentRateFactorExp = GUILayout.HorizontalSlider(descentRateFactorExp, -1.0f, 1.0f);
            planeConfig.DescentRateFactor = (float)Math.Round(Math.Pow(10, descentRateFactorExp), 1);

            // altitude loss
            planeConfig.EnableAltitudeLoss =
                    GUILayout.Toggle(planeConfig.EnableAltitudeLoss, "Altitude Loss After Takeoff", toggleStyle);

            // terrain clearance
            planeConfig.EnableTerrainClearance =
                    GUILayout.Toggle(planeConfig.EnableTerrainClearance, "Terrain Clearance", toggleStyle);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Gear Alt");
                GUILayout.FlexibleSpace();
                tooLowGearAltitudeString =
                        GUILayout.TextField(tooLowGearAltitudeString, GUILayout.Height(30), GUILayout.Width(80));
                GUILayout.Label(Util.GetShortString(planeConfig.UnitOfAltitude));
            }
            GUILayout.EndHorizontal();

            // altitude
            planeConfig.EnableAltitudeCallouts =
                    GUILayout.Toggle(planeConfig.EnableAltitudeCallouts, "Altitude Callouts", toggleStyle);

            // retard
            planeConfig.EnableRetard =
                    GUILayout.Toggle(planeConfig.EnableRetard, "Retard", toggleStyle);

            // bank angle
            planeConfig.EnableTraffic =
                    GUILayout.Toggle(planeConfig.EnableTraffic, "Traffic", toggleStyle);

            // traffic
            planeConfig.EnableBankAngle =
                    GUILayout.Toggle(planeConfig.EnableBankAngle, "Bank Angle", toggleStyle);

            // v1
            planeConfig.EnableV1 =
                    GUILayout.Toggle(planeConfig.EnableV1, "V1", toggleStyle);

            // rotate
            planeConfig.EnableRotate =
                    GUILayout.Toggle(planeConfig.EnableRotate, "Rotate", toggleStyle);

            // gear up
            planeConfig.EnableGearUp =
                    GUILayout.Toggle(planeConfig.EnableGearUp, "Gear Up", toggleStyle);

            // stall
            GUILayout.BeginHorizontal();
            {
                planeConfig.EnableStall =
                        GUILayout.Toggle(planeConfig.EnableStall, "Stall", toggleStyle);
                GUILayout.Space(50);
                planeConfig.EnableStallShake =
                        GUILayout.Toggle(planeConfig.EnableStallShake, "Shake", toggleStyle);
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(String.Format("Max AOA: {0} deg", planeConfig.StallAoa));
            planeConfig.StallAoa = (float)Math.Round(GUILayout.HorizontalSlider(planeConfig.StallAoa, 0.0f, 180.0f), 0);

            // take off speed
            GUILayout.Label(String.Format("V1 Speed: {0} m/s", planeConfig.V1Speed));
            planeConfig.V1Speed = (float)Math.Round(GUILayout.HorizontalSlider(planeConfig.V1Speed, 10.0f, 200.0f), 0);
            GUILayout.Label(String.Format("Take Off Speed: {0} m/s", planeConfig.TakeOffSpeed));
            planeConfig.TakeOffSpeed = (float)Math.Round(GUILayout.HorizontalSlider(planeConfig.TakeOffSpeed, 10.0f, 200.0f), 0);

            // landing speed
            GUILayout.Label(String.Format("Landing Speed: {0} m/s", planeConfig.LandingSpeed));
            planeConfig.LandingSpeed = (float)Math.Round(GUILayout.HorizontalSlider(planeConfig.LandingSpeed, 10.0f, 200.0f), 0);
        }

        private void drawLanderSetting()
        {
            landerConfig.EnableSystem =
                    GUILayout.Toggle(landerConfig.EnableSystem, "System Enable", toggleStyle);

            // descent rate
            landerConfig.EnableDescentRate =
                    GUILayout.Toggle(landerConfig.EnableDescentRate, "Descent Rate", toggleStyle);

            GUILayout.Label(String.Format("Safety Factor: {0}", landerConfig.DescentRateSafetyFactor));
            landerConfig.DescentRateSafetyFactor = (float)Math.Round(GUILayout.HorizontalSlider(landerConfig.DescentRateSafetyFactor, 1.0f, 4.0f), 1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Final Speed");
                GUILayout.FlexibleSpace();
                touchDownSpeedString =
                        GUILayout.TextField(touchDownSpeedString, GUILayout.Height(30), GUILayout.Width(80));
                GUILayout.Label(Util.GetShortString(landerConfig.UnitOfAltitude) + "/s");
            }
            GUILayout.EndHorizontal();

            // horizontal speed
            landerConfig.EnableHorizontalSpeed =
                    GUILayout.Toggle(landerConfig.EnableHorizontalSpeed, "Horizontal Speed", toggleStyle);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("vh Check Alt");
                GUILayout.FlexibleSpace();
                horizontalSpeedCheckAltitudeString =
                        GUILayout.TextField(horizontalSpeedCheckAltitudeString, GUILayout.Height(30), GUILayout.Width(80));
                GUILayout.Label(Util.GetShortString(landerConfig.UnitOfAltitude));
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(String.Format("Horizontal Speed Factor: {0}", landerConfig.HorizontalSpeedFactor));
            landerConfig.HorizontalSpeedFactor = (float)Math.Round(GUILayout.HorizontalSlider(landerConfig.HorizontalSpeedFactor, 0.01f, 1.0f), 2);

            // altitude
            landerConfig.EnableAltitudeCallouts =
                    GUILayout.Toggle(landerConfig.EnableAltitudeCallouts, "Altitude Callouts", toggleStyle);

            // retard
            landerConfig.EnableRetard =
                    GUILayout.Toggle(landerConfig.EnableRetard, "Retard", toggleStyle);
        }

        public void OnDestory()
        {
            GameEvents.onShowUI.Remove(ShowUI);
            GameEvents.onHideUI.Remove(HideUI);
            Settings.SaveToXml();
        }
    }
}
