using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using KSP_GPWS.Controller;

namespace KSP_GPWS.Impl
{
    public class ControlerManager
    {
        private XInputWrapper xInput;
        private Thread shakeThread;

        public ControlerManager()
        {
            xInput = new XInputWrapper();
        }

        public void SetShake(float motor, int milliseconds)
        {
            if (shakeThread == null || shakeThread.ThreadState == ThreadState.Stopped
                    || shakeThread.ThreadState == ThreadState.Aborted)
            {
                shakeThread = new Thread(() =>
                {
                    if (xInput.IsConnected(0))
                    {
                        xInput.SetVibration(0, motor, 0.2f);
                        Thread.Sleep(milliseconds);
                    }
                });
                shakeThread.Start();
            }
        }
    }
}
