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
    public interface ILanderConfig : IConfigNode
    {
        bool EnableSystem { get; set; }
        bool EnableDescentRate { get; set; }
        bool EnableHorizontalSpeed { get; set; }
        bool EnableAltitudeCallouts { get; set; }

        float TouchDownSpeed { get; set; }
        float HorizontalSpeedCheckAltitude { get; set; }

        /// <summary>
        /// max hSpeed = vSpeed * factor
        /// </summary>
        float HorizontalSpeedFactor { get; set; }

        int[] AltitudeArray { get; set; }

        /// <summary>
        /// use meters or feet, default:meter
        /// </summary>
        UnitOfAltitude UnitOfAltitude { get; set; }
    }
}
