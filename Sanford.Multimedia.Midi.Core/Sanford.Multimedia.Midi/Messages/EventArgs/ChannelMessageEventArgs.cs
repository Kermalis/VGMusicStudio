using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// The class that contains events for channel messages.
    /// </summary>
    public class ChannelMessageEventArgs : MidiEventArgs
    {
        private ChannelMessage message;

        /// <summary>
        /// The function that contains events for channel messages.
        /// </summary>
        public ChannelMessageEventArgs(ChannelMessage message, int absoluteTicks = -1)
        {
            this.message = message;
            this.AbsoluteTicks = absoluteTicks;
        }

        /// <summary>
        /// Gets the channel messages.
        /// </summary>
        public ChannelMessage Message
        {
            get
            {
                return message;
            }
        }
    }
}
