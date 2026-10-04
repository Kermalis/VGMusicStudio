using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Class for system common message events.
    /// </summary>
    public class SysCommonMessageEventArgs : MidiEventArgs
    {
        private SysCommonMessage message;

        /// <summary>
        /// Main function for system common message events.
        /// </summary>
        public SysCommonMessageEventArgs(SysCommonMessage message, int absoluteTicks = -1)
        {
            this.message = message;
            this.AbsoluteTicks = absoluteTicks;
        }

        /// <summary>
        /// Gets and returns the message.
        /// </summary>
        public SysCommonMessage Message
        {
            get
            {
                return message;
            }
        }
    }
}
