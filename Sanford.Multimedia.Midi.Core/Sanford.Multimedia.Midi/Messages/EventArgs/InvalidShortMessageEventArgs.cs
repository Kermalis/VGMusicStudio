using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// This class declares invalid short message events.
    /// </summary>
    public class InvalidShortMessageEventArgs : MidiEventArgs
    {
        private int message;

        /// <summary>
        /// Main function for when the invalid short message event is declared.
        /// </summary>
        public InvalidShortMessageEventArgs(int message, int absoluteTicks = -1)
        {
            this.message = message;
            this.AbsoluteTicks = absoluteTicks;
        }

        /// <summary>
        /// Gets and returns the message.
        /// </summary>
        public int Message
        {
            get
            {
                return message;
            }
        }
    }
}
