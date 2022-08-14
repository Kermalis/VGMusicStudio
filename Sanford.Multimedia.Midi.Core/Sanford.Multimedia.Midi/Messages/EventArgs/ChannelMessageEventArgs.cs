using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    public class ChannelMessageEventArgs : MidiEventArgs
    {
        private ChannelMessage message;

        public ChannelMessageEventArgs(ChannelMessage message, int absoluteTicks = -1)
        {
            this.message = message;
            this.AbsoluteTicks = absoluteTicks;
        }

        public ChannelMessage Message
        {
            get
            {
                return message;
            }
        }
    }
}
