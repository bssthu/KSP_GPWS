// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015
// Last modified: 2015-01-17, 01:02:50

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSP_GPWS
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GPWS : MonoBehaviour
    {
        public void Awake()
        {
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
