// GPWS mod for KSP
// License: CC-BY-NC-SA
// Author: bss, 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSP_GPWS.SimpleTypes
{
    public enum KindOfSound
    {
        UNAVAILABLE,
        NONE,
        SINK_RATE,
        SINK_RATE_PULL_UP,
        TERRAIN,
        TERRAIN_PULL_UP,
        DONT_SINK,
        TOO_LOW_GEAR,
        TOO_LOW_TERRAIN,
        TOO_LOW_FLAPS,
        GLIDESLOPE,
        ALTITUDE_CALLOUTS,
        BANK_ANGLE,
        WINDSHEAR,
        TRAFFIC,
        HORIZONTAL_SPEED,
        RETARD,
        V1,
        ROTATE,
        GEAR_UP,
        STALL
    };
}
