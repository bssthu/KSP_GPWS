using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP_GPWS.Controller;
using UnityEngine;

namespace KSP_GPWS.Impl
{
    public class ControlerManager
    {
        private XInputWrapper xInput;

        public const float SHAKE_TIME = 1.0f;
        private float shakeStartTime = 0.0f;

        public ControlerManager()
        {
            xInput = new XInputWrapper();
        }

        public void SetShake(float leftMotor, float rightMotor)
        {
            for (uint playerIndex = 0; playerIndex < 4; playerIndex++)
            {
                if (xInput.IsConnected(playerIndex))
                {
                    shakeStartTime = now();
                    xInput.SetVibration(playerIndex, leftMotor, rightMotor);
                }
            }
        }

        public void ResetShake()
        {
            shakeStartTime = 0.0f;
            for (uint playerIndex = 0; playerIndex < 4; playerIndex++)
            {
                if (xInput.IsConnected(playerIndex))
                {
                    xInput.SetVibration(playerIndex, 0f, 0f);
                }
            }
        }

        // to auto stop shake
        public void CheckResetShake()
        {
            if (shakeStartTime > 0f && now() - shakeStartTime >= SHAKE_TIME)
            {
                ResetShake();
            }
        }

        private float now()
        {
            return Time.realtimeSinceStartup;
        }
    }
}
