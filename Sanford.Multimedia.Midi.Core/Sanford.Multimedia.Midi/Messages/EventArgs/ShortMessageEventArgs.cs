using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Raw short message as int or byte array, useful when working with VST.
    /// </summary>
    public class ShortMessageEventArgs : MidiEventArgs
    {
        ShortMessage message;

        public ShortMessageEventArgs(ShortMessage message, int absoluteTicks = -1)
        {
            this.message = message;
            this.AbsoluteTicks = absoluteTicks;
        }

        public ShortMessageEventArgs(int message, int timestamp = 0, int absoluteTicks = -1)
        {
            this.message = new ShortMessage(message);
            this.message.Timestamp = timestamp;
            this.AbsoluteTicks = absoluteTicks;
        }

        public ShortMessageEventArgs(byte status, byte data1, byte data2, int absoluteTicks = -1)
        {
            this.message = new ShortMessage(status, data1, data2);
            this.AbsoluteTicks = absoluteTicks;
        }

        public ShortMessage Message
        {
            get
            {
                return message;
            }
        }

        public static ShortMessageEventArgs FromChannelMessage(ChannelMessageEventArgs arg)
        {
            return new ShortMessageEventArgs(arg.Message);
        }

        public static ShortMessageEventArgs FromSysCommonMessage(SysCommonMessageEventArgs arg)
        {
            return new ShortMessageEventArgs(arg.Message);
        }

        public static ShortMessageEventArgs FromSysRealtimeMessage(SysRealtimeMessageEventArgs arg)
        {
            return new ShortMessageEventArgs(arg.Message);
        }
    }
}
