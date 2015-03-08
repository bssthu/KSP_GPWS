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
            if (!Settings.UseBlizzy78Toolbar)
            {
                GameEvents.onGUIApplicationLauncherReady.Add(onGUIAppLauncherReady);
                GameEvents.onGUIApplicationLauncherDestroyed.Add(onGUIAppLauncherDestroyed);
            }
        }

        public void onGUIAppLauncherReady()
        {
            if (ApplicationLauncher.Ready)
            {
                if (appBtn == null)
                {
                    appBtn = ApplicationLauncher.Instance.AddModApplication(
                            onAppLaunchToggleOnOff,
                            onAppLaunchToggleOnOff,
                            () => { },
                            () => { },
                            () => { },
                            () => { },
                            ApplicationLauncher.AppScenes.FLIGHT,
                            (Texture)GameDatabase.Instance.GetTexture("GPWS/gpws", false));
                }
                if (Settings.guiIsActive)
                {
                    SettingGUI.toggleSettingGUI(true);
                }
            }
        }

        private void onAppLaunchToggleOnOff()
        {
            SettingGUI.toggleSettingGUI();
            appBtn.SetFalse(false);
        }

        public void onGUIAppLauncherDestroyed()
        {
            if (appBtn != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(appBtn);
                ApplicationLauncher.Instance.RemoveApplication(appBtn);
                appBtn = null;
            }
        }

        public void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(onGUIAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(onGUIAppLauncherDestroyed);
            if (appBtn != null)
            {
                ApplicationLauncher.Instance.RemoveApplication(appBtn);
                ApplicationLauncher.Instance.RemoveModApplication(appBtn);
                appBtn = null;
            }
        }
    }
}
