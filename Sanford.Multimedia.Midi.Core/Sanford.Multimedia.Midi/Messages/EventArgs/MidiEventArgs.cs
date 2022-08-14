using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    public class MidiEventArgs : EventArgs
    {
        public int AbsoluteTicks { get; set; }
    }
}
