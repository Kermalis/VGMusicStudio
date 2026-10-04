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
    /// Represents the Windows Multimedia MIDIHDR structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MidiHeader
    {
        #region MidiHeader Members

        /// <summary>
        /// Pointer to MIDI data.
        /// </summary>
        public IntPtr data;

        /// <summary>
        /// Size of the buffer.
        /// </summary>
        public int bufferLength; 

        /// <summary>
        /// Actual amount of data in the buffer. This value should be less than 
        /// or equal to the value given in the dwBufferLength member.
        /// </summary>
        public int bytesRecorded; 

        /// <summary>
        /// Custom user data.
        /// </summary>
        public int user; 

        /// <summary>
        /// Flags giving information about the buffer.
        /// </summary>
        public int flags; 

        /// <summary>
        /// Reserved; do not use.
        /// </summary>
        public IntPtr next; 

        /// <summary>
        /// Reserved; do not use.
        /// </summary>
        public int reserved; 

        /// <summary>
        /// Offset into the buffer when a callback is performed. (This 
        /// callback is generated because the MEVT_F_CALLBACK flag is 
        /// set in the dwEvent member of the MidiEventArgs structure.) 
        /// This offset enables an application to determine which 
        /// event caused the callback. 
        /// </summary>
        public int offset; 

        /// <summary>
        /// Reserved; do not use.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=4)]
        public int[] reservedArray; 

        #endregion
    }
}
