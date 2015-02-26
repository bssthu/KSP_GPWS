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
    class GUIToolbar : MonoBehaviour
    {
        private IButton btn;

        public void Awake()
        {
            btn = ToolbarManager.Instance.add("GPWS", "GPWSBtn");
            btn.TexturePath = "GPWS/gpws";
            btn.ToolTip = "GPWS settings";
            btn.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
            btn.OnClick += (e) => toggleToolbarButton(e, btn);
        }

        private void toggleToolbarButton(ClickEvent e, IButton btn)
        {
        }
    }
}
