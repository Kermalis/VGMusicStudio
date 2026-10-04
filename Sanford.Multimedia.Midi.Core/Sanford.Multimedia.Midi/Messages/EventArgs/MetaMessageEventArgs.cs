using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Class for declaring metadata message events.
    /// </summary>
    public class MetaMessageEventArgs : MidiEventArgs
    {
        private MetaMessage message;

        /// <summary>
        /// Main function for declaring metadata message events.
        /// </summary>
        public MetaMessageEventArgs(MetaMessage message, int absoluteTicks = -1)
        {
            this.message = message;
            this.AbsoluteTicks = absoluteTicks;
        }

        /// <summary>
        /// Gets and returns the message.
        /// </summary>
        public MetaMessage Message
        {
            get
            {
                return message;
            }
        }
    }
}
