using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP_GPWS.Controller;

namespace KSP_GPWS.Impl
{
    public class ControlerManager
    {
        private XInputWrapper xInput;

        public ControlerManager()
        {
            xInput = new XInputWrapper();
        }

        public void SetShake(float motor)
        {
            for (uint playerIndex = 0; playerIndex < 4; playerIndex++)
            {
                if (xInput.IsConnected(playerIndex))
                {
                    xInput.SetVibration(playerIndex, motor, 0.2f);
                }
            }
        }

        public void ResetShake()
        {
            for (uint playerIndex = 0; playerIndex < 4; playerIndex++)
            {
                if (xInput.IsConnected(playerIndex))
                {
                    xInput.SetVibration(playerIndex, 0f, 0f);
                }
            }
        }
    }
}
