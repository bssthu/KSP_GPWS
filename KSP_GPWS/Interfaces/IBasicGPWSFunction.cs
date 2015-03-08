// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSP_GPWS.Interfaces
{
    interface IBasicGPWSFunction
    {
        /// <summary>
        /// Run once.
        /// </summary>
        void InitializeConfig();

        void Initialize(IGPWSCommonData data);

        bool PreUpdate();

        void UpdateGPWS();

        void SetVesselInfo(Vessel v);
    }
}
