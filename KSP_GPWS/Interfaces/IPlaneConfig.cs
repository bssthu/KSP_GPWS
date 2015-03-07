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
    public interface IPlaneConfig : IConfigNode
    {
        bool EnableSystem { get; set; }
        bool EnableDescentRate { get; set; }
        bool EnableClosureToTerrain { get; set; }
        bool EnableAltitudeLoss { get; set; }
        bool EnableTerrainClearance { get; set; }
        bool EnableAltitudeCallouts { get; set; }
        bool EnableBankAngle { get; set; }
        bool EnableTraffic { get; set; }

        float DescentRateFactor { get; set; }
        float TooLowGearAltitude { get; set; }
        int[] AltitudeArray { get; set; }

        /// <summary>
        /// use meters or feet, feet is recommanded.
        /// </summary>
        UnitOfAltitude UnitOfAltitude { get; set; }
    }
}
