using System;
using System.Collections;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// This class declares invalid exclusive system message events.
    /// </summary>
    public class InvalidSysExMessageEventArgs : MidiEventArgs
    {
        private byte[] messageData;

        /// <summary>
        /// Main function for declared invalid exclusive system message events.
        /// </summary>
        public InvalidSysExMessageEventArgs(byte[] messageData, int absoluteTicks = -1)
        {
            this.messageData = messageData;
            this.AbsoluteTicks = absoluteTicks;
        }

        /// <summary>
        /// Gets and returns the message data.
        /// </summary>
        public ICollection MessageData
        {
            get
            {
                return messageData;
            }
        }
    }
}
