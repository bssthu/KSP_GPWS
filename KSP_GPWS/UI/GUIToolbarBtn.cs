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
    class GUIToolbarBtn : MonoBehaviour
    {
        private IButton btn = null;

        public void Awake()
        {
            if (Settings.UseBlizzy78Toolbar && ToolbarManager.ToolbarAvailable)
            {
                btn = ToolbarManager.Instance.add("GPWS", "GPWSBtn");
                btn.TexturePath = "GPWS/gpws";
                btn.ToolTip = "GPWS settings";
                btn.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
                btn.OnClick += (e) => SettingGUI.toggleSettingGUI();
            }
        }

        public void OnDestroy()
        {
            if (btn != null)
            {
                btn.Destroy();
                btn = null;
            }
        }
    }
}
