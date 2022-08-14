using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    public class SysCommonMessageEventArgs : MidiEventArgs
    {
        private SysCommonMessage message;

        public SysCommonMessageEventArgs(SysCommonMessage message, int absoluteTicks = -1)
        {
            this.message = message;
            this.AbsoluteTicks = absoluteTicks;
        }

        public SysCommonMessage Message
        {
            get
            {
                return message;
            }
        }
    }
}
