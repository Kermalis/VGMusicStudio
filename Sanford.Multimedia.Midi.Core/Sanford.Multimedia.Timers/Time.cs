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
using System.Runtime.InteropServices;

namespace Sanford.Multimedia.Timers
{
    /// <summary>
    /// Defines constants representing the timing format used by the Time struct.
    /// </summary>
    public enum TimeType
    {
        Milliseconds = 0x0001,
        Samples      = 0x0002, 
        Bytes        = 0x0004,  
        Smpte        = 0x0008,
        Midi         = 0x0010,
        Ticks        = 0x0020
    }

	/// <summary>
    /// Represents the Windows Multimedia MMTIME structure.
	/// </summary>
    [StructLayout(LayoutKind.Explicit)]
	public struct Time
	{
        [FieldOffset(0)]
        public int type;

        [FieldOffset(4)]
        public int milliseconds;

        [FieldOffset(4)]
        public int samples;

        [FieldOffset(4)]
        public int byteCount;

        [FieldOffset(4)]
        public int ticks;

        //
        // SMPTE
        //

        [FieldOffset(4)]
        public byte hours; 

        [FieldOffset(5)]
        public byte minutes; 

        [FieldOffset(6)]
        public byte seconds; 

        [FieldOffset(7)]
        public byte frames; 

        [FieldOffset(8)]
        public byte framesPerSecond; 

        [FieldOffset(9)]
        public byte dummy; 

        [FieldOffset(10)]
        public byte pad1; 

        [FieldOffset(11)]
        public byte pad2;
        
        //
        // MIDI
        //

        [FieldOffset(4)]
        public int songPositionPointer;
	}
}
