// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015
// Last modified: 2015-01-17, 02:45:18

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSP_GPWS
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GPWS : UnityEngine.MonoBehaviour
    {
        private List<GPWSGear> gearList = new List<GPWSGear>();     // parts with module "GPWSGear"

        public void Awake()
        {
        }

        public void Start()
        {
            GameEvents.onVesselChange.Add(findGears);
            if (FlightGlobals.ActiveVessel != null)
            {
                findGears(FlightGlobals.ActiveVessel);
            }
        }

        private void findGears(Vessel v)
        {
            gearList.Clear();

            if (null == v)
            {
                return;
            }

            for (int i = 0; i < v.parts.Count; i++)    // it is said that foreach costs more memory due to Unity Mono issues
            {
                Part p = v.parts[i];
                if (p.Modules.Contains("GPWSGear"))
                {
                    gearList.Add(p.Modules["GPWSGear"] as GPWSGear);
                    Log(String.Format("find {0}", p.name));
                }
            }
        }

        public void Update()
        {
        }

        public void OnDestroy()
        {
            GameEvents.onVesselChange.Remove(findGears);
        }

        void Log(String msg)
        {
            UnityEngine.Debug.Log("[GPWS] " + msg);
        }
    }
}
