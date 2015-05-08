using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSP_GPWS.Interfaces
{
    public interface IGPWSCommonData
    {
        float RadarAltitude { get; set; }

        float LastRadarAltitude { get; }

        float Altitude { get; set; }

        float LastAltitude { get; }

        /// <summary>
        /// in m/s
        /// </summary>
        float HorSpeed { get; }

        float LastHorSpeed { get; }

        /// <summary>
        /// in m/s
        /// </summary>
        float VerSpeed { get; }

        float LastVerSpeed { get; }

        float time { get; }

        float lastTime { get; }

        Vessel ActiveVessel { get; }
    }
}
