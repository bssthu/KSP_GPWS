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
    class GUIAppLaunchBtn : MonoBehaviour
    {
        public static ApplicationLauncherButton appBtn = null;

        public void Awake()
        {
            if (!Settings.useBlizzy78Toolbar)
            {
                GameEvents.onGUIApplicationLauncherReady.Add(onGUIAppLauncherReady);
                GameEvents.onGUIApplicationLauncherDestroyed.Add(onGUIAppLauncherDestoryd);
            }
        }

        public void onGUIAppLauncherReady()
        {
            if (ApplicationLauncher.Ready && appBtn == null)
            {
                appBtn = ApplicationLauncher.Instance.AddModApplication(
                        onAppLaunchToggleOn,
                        onAppLaunchToggleOff,
                        () => { },
                        () => { },
                        () => { },
                        () => { },
                        ApplicationLauncher.AppScenes.FLIGHT,
                        (Texture)GameDatabase.Instance.GetTexture("GPWS/gpws", false));
            }
        }

        private void onAppLaunchToggleOn()
        {
            SettingGUI.isActive = true;
        }

        private void onAppLaunchToggleOff()
        {
            SettingGUI.isActive = false;
        }

        public void onGUIAppLauncherDestoryd()
        {
            if (appBtn != null)
            {
                ApplicationLauncher.Instance.RemoveApplication(appBtn);
                appBtn = null;
            }
        }

        public void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(onGUIAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(onGUIAppLauncherDestoryd);
            if (appBtn != null)
            {
                ApplicationLauncher.Instance.RemoveApplication(appBtn);
                appBtn = null;
            }
        }
    }
}
