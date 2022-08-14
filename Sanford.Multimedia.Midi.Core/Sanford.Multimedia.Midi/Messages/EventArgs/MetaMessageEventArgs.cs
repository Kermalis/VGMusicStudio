using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    public class MetaMessageEventArgs : MidiEventArgs
    {
        private MetaMessage message;

        public MetaMessageEventArgs(MetaMessage message, int absoluteTicks = -1)
        {
            this.message = message;
            this.AbsoluteTicks = absoluteTicks;
        }

        public MetaMessage Message
        {
            get
            {
                return message;
            }
        }
    }
}
