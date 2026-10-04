using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Class for exclusive system message events.
    /// </summary>
    public class SysExMessageEventArgs : MidiEventArgs
    {
        private SysExMessage message;

        /// <summary>
        /// Main function for exclusive system message events.
        /// </summary>
        public SysExMessageEventArgs(SysExMessage message, int absoluteTicks = -1)
        {
            this.message = message;
            this.AbsoluteTicks = absoluteTicks;
        }

        /// <summary>
        /// Gets and returns the message.
        /// </summary>
        public SysExMessage Message
        {
            get
            {
                return message;
            }
        }
    }
}
