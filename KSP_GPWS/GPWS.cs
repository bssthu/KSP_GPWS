// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015
// Last modified: 2015-01-17, 02:20:08

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
            if (FlightGlobals.ActiveVessel == null)
            {
                return;
            }

            Vessel vessel = FlightGlobals.ActiveVessel;
            gearList.Clear();
            for (int i = 0; i < vessel.parts.Count; i++)    // it is said that foreach costs more memory due to Unity Mono issues
            {
                Part p = vessel.parts[i];
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

        void Log(String msg)
        {
            UnityEngine.Debug.Log("[GPWS] " + msg);
        }
    }
}
