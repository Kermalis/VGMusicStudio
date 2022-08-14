#region License

/* Copyright (c) 2006 Leslie Sanford
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to 
 * deal in the Software without restriction, including without limitation the 
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
 * sell copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software. 
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 * THE SOFTWARE.
 */

#endregion

#region Contact

/*
 * Leslie Sanford
 * Email: jabberdabber@hotmail.com
 */

#endregion

using System;

namespace Sanford.Multimedia.Midi
{
    public class MidiEvent
    {
        private object owner = null;

        private int absoluteTicks;

        private IMidiMessage message;

        private MidiEvent next = null;

        private MidiEvent previous = null;

        internal MidiEvent(object owner, int absoluteTicks, IMidiMessage message)
        {
            #region Require

            if(owner == null)
            {
                throw new ArgumentNullException("owner");
            }
            else if(absoluteTicks < 0)
            {
                throw new ArgumentOutOfRangeException("absoluteTicks", absoluteTicks,
                    "Absolute ticks out of range.");
            }
            else if(message == null)
            {
                throw new ArgumentNullException("e");
            }

            #endregion

            this.owner = owner;
            this.absoluteTicks = absoluteTicks;
            this.message = message;
        }

        internal void SetAbsoluteTicks(int absoluteTicks)
        {
            this.absoluteTicks = absoluteTicks;
        }

        internal object Owner
        {
            get
            {
                return owner;
            }
        }

        public int AbsoluteTicks
        {
            get
            {
                return absoluteTicks;
            }
        }

        public int DeltaTicks
        {
            get
            {
                int deltaTicks;

                if(Previous != null)
                {
                    deltaTicks = AbsoluteTicks - previous.AbsoluteTicks;
                }
                else
                {
                    deltaTicks = AbsoluteTicks;
                }

                return deltaTicks;
            }
        }

        public IMidiMessage MidiMessage
        {
            get
            {
                return message;
            }
        }

        internal MidiEvent Next
        {
            get
            {
                return next;
            }
            set
            {
                next = value;
            }
        }

        internal MidiEvent Previous
        {
            get
            {
                return previous;
            }
            set
            {
                previous = value;
            }
        }
    }
}
