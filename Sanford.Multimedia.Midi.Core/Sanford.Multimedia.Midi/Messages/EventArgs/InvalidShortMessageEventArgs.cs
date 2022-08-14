using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    public class InvalidShortMessageEventArgs : MidiEventArgs
    {
        private int message;

        public InvalidShortMessageEventArgs(int message, int absoluteTicks = -1)
        {
            this.message = message;
            this.AbsoluteTicks = absoluteTicks;
        }

        public int Message
        {
            get
            {
                return message;
            }
        }
    }
}
