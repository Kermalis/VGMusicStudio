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

namespace Sanford.Multimedia.Midi
{
	/// <summary>
	/// The base class for all MIDI device exception classes.
	/// </summary>
	public class MidiDeviceException : DeviceException
	{
        #region Error Codes

        public const int MIDIERR_UNPREPARED    = 64; /* header not prepared */
        public const int MIDIERR_STILLPLAYING  = 65; /* still something playing */
        public const int MIDIERR_NOMAP         = 66; /* no configured instruments */
        public const int MIDIERR_NOTREADY      = 67; /* hardware is still busy */
        public const int MIDIERR_NODEVICE      = 68; /* port no longer connected */
        public const int MIDIERR_INVALIDSETUP  = 69; /* invalid MIF */
        public const int MIDIERR_BADOPENMODE   = 70; /* operation unsupported w/ open mode */
        public const int MIDIERR_DONT_CONTINUE = 71; /* thru device 'eating' a message */
        public const int MIDIERR_LASTERROR     = 71; /* last error in range */

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the DeviceException class with the
        /// specified error code.
        /// </summary>
        /// <param name="errCode">
        /// The error code.
        /// </param>
		public MidiDeviceException(int errCode) : base(errCode)
		{
		}

        #endregion
	}
}
