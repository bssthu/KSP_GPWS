// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP_GPWS.Interfaces;

namespace KSP_GPWS.UI
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class GuiAppLaunchBtn : MonoBehaviour
    {
        public static ApplicationLauncherButton appBtn = null;

        public void Awake()
        {
            if (!Settings.UseBlizzy78Toolbar || !ToolbarManager.ToolbarAvailable)
            {
                onGuiAppLauncherReady();
            }
        }

        public void onGuiAppLauncherReady()
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
                    SettingGui.toggleSettingGui(true);
                }
            }
        }

        private void onAppLaunchToggleOnOff()
        {
            SettingGui.toggleSettingGui();
            appBtn.SetFalse(false);
        }

        public void onGuiAppLauncherDestroyed()
        {
            if (appBtn != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(appBtn);
                appBtn = null;
            }
        }

        public void OnDestroy()
        {
            onGuiAppLauncherDestroyed();
        }
    }
}
