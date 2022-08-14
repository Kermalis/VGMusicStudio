using System;
using System.Collections;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    public class InvalidSysExMessageEventArgs : MidiEventArgs
    {
        private byte[] messageData;

        public InvalidSysExMessageEventArgs(byte[] messageData, int absoluteTicks = -1)
        {
            this.messageData = messageData;
            this.AbsoluteTicks = absoluteTicks;
        }

        public ICollection MessageData
        {
            get
            {
                return messageData;
            }
        }
    }
}
