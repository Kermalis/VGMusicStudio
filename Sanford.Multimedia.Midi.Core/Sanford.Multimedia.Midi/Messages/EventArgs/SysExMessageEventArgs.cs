using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    public class SysExMessageEventArgs : MidiEventArgs
    {
        private SysExMessage message;

        public SysExMessageEventArgs(SysExMessage message, int absoluteTicks = -1)
        {
            this.message = message;
            this.AbsoluteTicks = absoluteTicks;
        }

        public SysExMessage Message
        {
            get
            {
                return message;
            }
        }
    }
}
