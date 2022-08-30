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
        /// <summary>
		/// Defined in milliseconds.
		/// </summary>
        Milliseconds = 0x0001,
        /// <summary>
		/// Defined in samples.
		/// </summary>
        Samples = 0x0002,
        /// <summary>
        /// Defined in bytes.
        /// </summary>
        Bytes = 0x0004,
        /// <summary>
        /// Defined in SMPTE.
        /// </summary>
        Smpte = 0x0008,
        /// <summary>
		/// Defined in MIDI.
		/// </summary>
        Midi = 0x0010,
        /// <summary>
		/// Defined in ticks.
		/// </summary>
        Ticks = 0x0020
    }

	/// <summary>
    /// Represents the Windows Multimedia MMTIME structure.
	/// </summary>
    [StructLayout(LayoutKind.Explicit)]
	public struct Time
	{
        /// <summary>
		/// Type.
		/// </summary>
        [FieldOffset(0)]
        public int type;

        /// <summary>
		/// Milliseconds.
		/// </summary>
        [FieldOffset(4)]
        public int milliseconds;

        /// <summary>
		/// Samples.
		/// </summary>
        [FieldOffset(4)]
        public int samples;

        /// <summary>
		/// Byte count.
		/// </summary>
        [FieldOffset(4)]
        public int byteCount;

        /// <summary>
		/// Ticks.
		/// </summary>
        [FieldOffset(4)]
        public int ticks;

        //
        // SMPTE
        //

        /// <summary>
		/// SMPTE hours.
		/// </summary>
        [FieldOffset(4)]
        public byte hours;

        /// <summary>
        /// SMPTE minutes.
        /// </summary>
        [FieldOffset(5)]
        public byte minutes;

        /// <summary>
        /// SMPTE seconds.
        /// </summary>
        [FieldOffset(6)]
        public byte seconds;

        /// <summary>
        /// SMPTE frames.
        /// </summary>
        [FieldOffset(7)]
        public byte frames;

        /// <summary>
        /// SMPTE frames per second.
        /// </summary>
        [FieldOffset(8)]
        public byte framesPerSecond;

        /// <summary>
        /// SMPTE dummy.
        /// </summary>
        [FieldOffset(9)]
        public byte dummy;

        /// <summary>
        /// SMPTE pad 1.
        /// </summary>
        [FieldOffset(10)]
        public byte pad1;

        /// <summary>
        /// SMPTE pad 2.
        /// </summary>
        [FieldOffset(11)]
        public byte pad2;

        //
        // MIDI
        //

        /// <summary>
        /// MIDI song position pointer.
        /// </summary>
        [FieldOffset(4)]
        public int songPositionPointer;
	}
}
