using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSP_GPWS.Controller
{
    public class XInputWrapper
    {
        private delegate uint XInputGamePadGetStateDelegate(uint playerIndex, out XInputState state);
        private XInputGamePadGetStateDelegate XInputGamePadGetState;

        private delegate void XInputGamePadSetStateDelegate(uint playerIndex, float leftMotor, float rightMotor);
        private XInputGamePadSetStateDelegate XInputGamePadSetState;

        public bool DllAvailable
        {
            get
            {
                return _dllAvailable;
            }
            private set
            {
                _dllAvailable = value;
            }
        }
        private static bool _dllAvailable = true;

        public const uint ERROR_DEVICE_NOT_CONNECTED = 1167;

        public XInputWrapper()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (Util.IsWin32())
            {
                XInputGamePadGetState = XInputController.XInputGamePadGetState_x86;
                XInputGamePadSetState = XInputController.XInputGamePadSetState_x86;
            }
            else if (Util.IsWin64())
            {
                XInputGamePadGetState = XInputController.XInputGamePadGetState_x64;
                XInputGamePadSetState = XInputController.XInputGamePadSetState_x64;
            }
            else
            {
                XInputGamePadGetState = XInputController.XInputGamePadGetState;
                XInputGamePadSetState = XInputController.XInputGamePadSetState;
            }
        }

        public bool IsConnected(uint playerIndex)
        {
            Util.Log("11");
            if (!DllAvailable)
            {
                return false;
            }

            Util.Log("22");
            try
            {
                XInputState state;
                uint result = XInputGamePadGetState(playerIndex, out state);
                return result != ERROR_DEVICE_NOT_CONNECTED;
            }
            catch (DllNotFoundException)
            {
                Util.Log("33");
                if (XInputGamePadGetState != XInputController.XInputGamePadGetState)    // if _x86 or _x64 dll not exists
                {
                    XInputGamePadGetState = XInputController.XInputGamePadGetState;
                    return IsConnected(playerIndex);
                }
                else    // no dll at all
                {
                    DllAvailable = false;
                    return false;
                }
            }
            catch (BadImageFormatException) // use _x86 dll on x64 system, or _x64 dll on x86 system
            {
                Util.Log("44");
                DllAvailable = false;
                return false;
            }
        }

        public void SetVibration(uint playerIndex, float leftMotor, float rightMotor)
        {
            if (!DllAvailable)
            {
                return;
            }

            try
            {
                XInputGamePadSetState(playerIndex, leftMotor, rightMotor);
            }
            catch (DllNotFoundException)
            {
                if (XInputGamePadSetState != XInputController.XInputGamePadSetState)    // if _x86 or _x64 dll not exists
                {
                    XInputGamePadSetState = XInputController.XInputGamePadSetState;
                    SetVibration(playerIndex, leftMotor, rightMotor);
                }
                else    // no dll at all
                {
                    DllAvailable = false;
                    return;
                }
            }
            catch (BadImageFormatException) // use _x86 dll on x64 system, or _x64 dll on x86 system
            {
                DllAvailable = false;
                return;
            }
        }
    }
}
