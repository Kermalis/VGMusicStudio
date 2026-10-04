using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Class for system realtime message events.
    /// </summary>
    public class SysRealtimeMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Requests the start for the system realtime message event.
        /// </summary>
        public static readonly SysRealtimeMessageEventArgs Start = new SysRealtimeMessageEventArgs(SysRealtimeMessage.StartMessage);

        /// <summary>
        /// Requests to continue for the system realtime message event.
        /// </summary>
        public static readonly SysRealtimeMessageEventArgs Continue = new SysRealtimeMessageEventArgs(SysRealtimeMessage.ContinueMessage);

        /// <summary>
        /// Requests to stop for the system realtime message event.
        /// </summary>
        public static readonly SysRealtimeMessageEventArgs Stop = new SysRealtimeMessageEventArgs(SysRealtimeMessage.StopMessage);

        /// <summary>
        /// Requests the clock for the system realtime message event.
        /// </summary>
        public static readonly SysRealtimeMessageEventArgs Clock = new SysRealtimeMessageEventArgs(SysRealtimeMessage.ClockMessage);

        /// <summary>
        /// Requests the ticks for the system realtime message event.
        /// </summary>
        public static readonly SysRealtimeMessageEventArgs Tick = new SysRealtimeMessageEventArgs(SysRealtimeMessage.TickMessage);

        /// <summary>
        /// Requests the active sense for the system realtime message event.
        /// </summary>
        public static readonly SysRealtimeMessageEventArgs ActiveSense = new SysRealtimeMessageEventArgs(SysRealtimeMessage.ActiveSenseMessage);

        /// <summary>
        /// Requests to restart for the system realtime message event.
        /// </summary>
        public static readonly SysRealtimeMessageEventArgs Reset = new SysRealtimeMessageEventArgs(SysRealtimeMessage.ResetMessage);

        private SysRealtimeMessage message;

        private SysRealtimeMessageEventArgs(SysRealtimeMessage message)
        {
            this.message = message;
        }

        /// <summary>
        /// Gets and returns the message.
        /// </summary>
        public SysRealtimeMessage Message
        {
            get
            {
                return message;
            }
        }
    }
}
