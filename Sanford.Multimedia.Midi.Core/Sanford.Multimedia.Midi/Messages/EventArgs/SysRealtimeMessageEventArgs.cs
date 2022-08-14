using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    public class SysRealtimeMessageEventArgs : EventArgs
    {
        public static readonly SysRealtimeMessageEventArgs Start = new SysRealtimeMessageEventArgs(SysRealtimeMessage.StartMessage);

        public static readonly SysRealtimeMessageEventArgs Continue = new SysRealtimeMessageEventArgs(SysRealtimeMessage.ContinueMessage);

        public static readonly SysRealtimeMessageEventArgs Stop = new SysRealtimeMessageEventArgs(SysRealtimeMessage.StopMessage);

        public static readonly SysRealtimeMessageEventArgs Clock = new SysRealtimeMessageEventArgs(SysRealtimeMessage.ClockMessage);

        public static readonly SysRealtimeMessageEventArgs Tick = new SysRealtimeMessageEventArgs(SysRealtimeMessage.TickMessage);

        public static readonly SysRealtimeMessageEventArgs ActiveSense = new SysRealtimeMessageEventArgs(SysRealtimeMessage.ActiveSenseMessage);

        public static readonly SysRealtimeMessageEventArgs Reset = new SysRealtimeMessageEventArgs(SysRealtimeMessage.ResetMessage);

        private SysRealtimeMessage message;

        private SysRealtimeMessageEventArgs(SysRealtimeMessage message)
        {
            this.message = message;
        }

        public SysRealtimeMessage Message
        {
            get
            {
                return message;
            }
        }
    }
}
