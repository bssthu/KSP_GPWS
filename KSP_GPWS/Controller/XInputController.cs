using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace KSP_GPWS.Controller
{
    static class XInputController
    {
        [DllImport("XInputInterface")]
        public static extern uint XInputGamePadGetState(uint playerIndex, out XInputState state);
        [DllImport("XInputInterface_x86")]
        public static extern uint XInputGamePadGetState_x86(uint playerIndex, out XInputState state);
        [DllImport("XInputInterface_x64")]
        public static extern uint XInputGamePadGetState_x64(uint playerIndex, out XInputState state);

        [DllImport("XInputInterface", CallingConvention = CallingConvention.Cdecl)]
        public static extern void XInputGamePadSetState(uint playerIndex, float leftMotor, float rightMotor);
        [DllImport("XInputInterface_x86", CallingConvention = CallingConvention.Cdecl)]
        public static extern void XInputGamePadSetState_x86(uint playerIndex, float leftMotor, float rightMotor);
        [DllImport("XInputInterface_x64", CallingConvention = CallingConvention.Cdecl)]
        public static extern void XInputGamePadSetState_x64(uint playerIndex, float leftMotor, float rightMotor);
    }
}
