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
        public static Rect guiwindowPosition;
        public static bool isActive = false;
        private bool isHideUI = false;

        private String descentRateFactorString;

        public void Awake()
        {
            GameEvents.onShowUI.Add(ShowUI);
            GameEvents.onHideUI.Add(HideUI);
            descentRateFactorString = Settings.descentRateFactor.ToString();
        }

        public static void toggleSettingGUI()
        {
            isActive = !isActive;
            if (!isActive)
            {
                if (!Settings.useBlizzy78Toolbar && GUIAppLaunchBtn.appBtn != null)
                {
                    GUIAppLaunchBtn.appBtn.SetFalse();
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
            if (isActive && !isHideUI)
            {
                GUI.skin = HighLogic.Skin;
                guiwindowPosition = GUILayout.Window("GPWSSetting".GetHashCode(), guiwindowPosition, SettingWindowFunc,
                        "GPWS Setting", GUILayout.ExpandHeight(true));
            }
        }

        private void SettingWindowFunc(int windowID)
        {
            GUIStyle thisStyle = new GUIStyle(GUI.skin.toggle);
            thisStyle.stretchHeight = true;
            thisStyle.stretchWidth = true;
            thisStyle.padding = new RectOffset(4, 4, 4, 4);
            thisStyle.margin = new RectOffset(4, 4, 4, 4);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.stretchHeight = true;
            buttonStyle.stretchWidth = true;
            buttonStyle.padding = new RectOffset(4, 4, 4, 4);
            buttonStyle.margin = new RectOffset(4, 4, 4, 4);

            // begin drawing
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            {
                GUILayout.Label("select function", GUILayout.MinWidth(200));
                Settings.enableSystem =
                        GUILayout.Toggle(Settings.enableSystem, "System", thisStyle);
                if (Settings.enableSystem)
                {
                    Settings.enableAltitudeCallouts =
                            GUILayout.Toggle(Settings.enableAltitudeCallouts, "Altitude Callouts", thisStyle);

                    Settings.enableDescentRate =
                            GUILayout.Toggle(Settings.enableDescentRate, "Descent Rate", thisStyle);
                    if (Settings.enableDescentRate)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("Descent Rate Factor");
                            descentRateFactorString =
                                    GUILayout.TextField(descentRateFactorString, GUILayout.Height(30), GUILayout.MinWidth(30));
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Save", buttonStyle, GUILayout.Width(80), GUILayout.Height(30)))
                    {
                        float newDescentRateFactor;
                        if (float.TryParse(descentRateFactorString, out newDescentRateFactor))
                        {
                            Settings.descentRateFactor = newDescentRateFactor;
                        }
                        // save
                        Settings.SaveSettings();
                        toggleSettingGUI();
                    }
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Cancel", buttonStyle, GUILayout.Width(80), GUILayout.Height(30)))
                    {
                        toggleSettingGUI();
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        public void OnDestory()
        {
            GameEvents.onShowUI.Remove(ShowUI);
            GameEvents.onHideUI.Remove(HideUI);
            isActive = false;
        }
    }
}
