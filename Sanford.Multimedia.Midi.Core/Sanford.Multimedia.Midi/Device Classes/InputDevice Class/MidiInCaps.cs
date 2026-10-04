#region License

/* Copyright (c) 2005 Leslie Sanford
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

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Represents MIDI input device capabilities.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MidiInCaps
    {
        #region MidiInCaps Members

        /// <summary>
        /// Manufacturer identifier of the device driver for the Midi output 
        /// device. 
        /// </summary>
        public short mid; 

        /// <summary>
        /// Product identifier of the Midi output device. 
        /// </summary>
        public short pid; 

        /// <summary>
        /// Version number of the device driver for the Midi output device. The 
        /// high-order byte is the major version number, and the low-order byte 
        /// is the minor version number. 
        /// </summary>
        public int driverVersion;

        /// <summary>
        /// Product name.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string name;         

        /// <summary>
        /// Optional functionality supported by the device. 
        /// </summary>
        public int support; 

        #endregion
    }
}
