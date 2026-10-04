using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Class for MIDI events.
    /// </summary>
    public class MidiEventArgs : EventArgs
    {
        /// <summary>
        /// Gets and sets the ticks for the MIDI events.
        /// </summary>
        public int AbsoluteTicks { get; set; }
    }
}
