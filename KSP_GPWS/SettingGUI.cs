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

        public static void toggleSettingGUI()
        {
            isActive = !isActive;
        }

        public void OnGUI()
        {
            if (isActive)
            {
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

            GUILayout.BeginVertical();
            GUILayout.Label("gpws");
            Settings.enableSystem =
                    GUILayout.Toggle(Settings.enableSystem, "Enable System", thisStyle);
            GUILayout.EndVertical();

            GUI.DragWindow();
        }
    }
}
