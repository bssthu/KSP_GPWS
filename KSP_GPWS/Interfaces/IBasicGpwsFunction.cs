// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP_GPWS.SimpleTypes;

namespace KSP_GPWS.Interfaces
{
    interface IBasicGPWSFunction
    {
        bool EnableSystem { get; set; }
        UnitOfAltitude UnitOfAltitude { get; set; }

        /// <summary>
        /// Run once.
        /// </summary>
        void InitializeConfig();

        void Initialize(IGpwsCommonData data);

        bool PreUpdate();

        void UpdateGPWS();

        void ChangeVessel(Vessel v);

        void CleanUp();
    }
}
