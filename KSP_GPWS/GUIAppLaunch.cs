// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSP_GPWS
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class GUIAppLaunch
    {
        ApplicationLauncherButton appBtn = null;

        public void Awake()
        {
            if (!Settings.useBlizzy78Toolbar)
            {
            }
        }

        public void OnDestroy()
        {
            if (appBtn != null)
            {
                ApplicationLauncher.Instance.RemoveApplication(appBtn);
            }
        }
    }
}
