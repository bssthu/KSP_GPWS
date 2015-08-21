using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSP_GPWS.Interfaces
{
    public interface IGpwsCommonData
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

        float Speed { get; }

        float CurrentTime { get; }

        float LastTime { get; }

        /// <summary>
        /// time of takeoff.
        /// when on ground, takeOffTime = time
        /// </summary>
        float TakeOffTime { get; }

        /// <summary>
        /// time of landing/splashing.
        /// when flying, landingTime = time
        /// </summary>
        float LandingTime { get; }

        Vessel ActiveVessel { get; }
    }
}
