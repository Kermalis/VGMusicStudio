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

namespace Sanford.Multimedia
{
    /// <summary>
    /// Refers the System.ApplicationException as DeviceException.
    /// </summary>
    public abstract class DeviceException : ApplicationException
    {
        #region Error Codes
        /// <summary>No error.</summary>
        public const int MMSYSERR_NOERROR = 0;
        /// <summary>Unspecified error.</summary>
        public const int MMSYSERR_ERROR = 1;
        /// <summary>Device ID out of range.</summary>
        public const int MMSYSERR_BADDEVICEID = 2;
        /// <summary>Driver failed enable.</summary>
        public const int MMSYSERR_NOTENABLED = 3;
        /// <summary>Device already allocated.</summary>
        public const int MMSYSERR_ALLOCATED = 4;
        /// <summary>Device handle is invalid.</summary>
        public const int MMSYSERR_INVALHANDLE = 5;
        /// <summary>No device driver present.</summary>
        public const int MMSYSERR_NODRIVER = 6;
        /// <summary>Memory allocation error.</summary>
        public const int MMSYSERR_NOMEM = 7;
        /// <summary>Function isn't supported.</summary>
        public const int MMSYSERR_NOTSUPPORTED = 8;
        /// <summary>Error value out of range.</summary>
        public const int MMSYSERR_BADERRNUM = 9;
        /// <summary>Invalid flag passed.</summary>
        public const int MMSYSERR_INVALFLAG = 10;
        /// <summary>Invalid parameter passed.</summary>
        public const int MMSYSERR_INVALPARAM = 11;
        /// <summary>
        /// Handle being used.<br></br>
        /// Simultaneously on another.<br></br>
        /// Thread (eg callback).<br></br>
        /// </summary>
        public const int MMSYSERR_HANDLEBUSY = 12;
        /// <summary>Specified alias not found.</summary>
        public const int MMSYSERR_INVALIDALIAS = 13;
        /// <summary>Bad registry database.</summary>
        public const int MMSYSERR_BADDB = 14;
        /// <summary>Registry key not found.</summary>
        public const int MMSYSERR_KEYNOTFOUND = 15;
        /// <summary>Registry read error.</summary>
        public const int MMSYSERR_READERROR = 16;
        /// <summary>Registry write error.</summary>
        public const int MMSYSERR_WRITEERROR = 17;
        /// <summary>Registry delete error.</summary>
        public const int MMSYSERR_DELETEERROR = 18;
        /// <summary>Registry value not found.</summary>
        public const int MMSYSERR_VALNOTFOUND = 19;
        /// <summary>Driver does not call DriverCallback.</summary>
        public const int MMSYSERR_NODRIVERCB = 20; 
        /// <summary>Last error.</summary>
        public const int MMSYSERR_LASTERROR = 20;

        #endregion

        private int errorCode;

        /// <summary>
        /// Calls the Device Exception error code.
        /// </summary>
        public DeviceException(int errorCode)
        {
            this.errorCode = errorCode;
        }

        /// <summary>
        /// Public integer for the error code.
        /// </summary>
        public int ErrorCode
        {
            get
            {
                return errorCode;
            }
        }
    }
}
