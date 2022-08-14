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
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Represents a MIDI device capable of receiving MIDI events.
    /// </summary>
    public partial class InputDevice : MidiDevice
    {
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                lock(lockObject)
                {
                    Reset();

                    int result = midiInClose(handle);

                    if(result == MidiDeviceException.MMSYSERR_NOERROR)
                    {
                        delegateQueue.Dispose();
                    }
                    else
                    {
                        throw new InputDeviceException(result);
                    }
                }
            }
            else
            {
                midiInReset(handle);
                midiInClose(handle);
            }

            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// The exception that is thrown when a error occurs with the InputDevice
    /// class.
    /// </summary>
    public class InputDeviceException : MidiDeviceException
    {
        #region InputDeviceException Members

        #region Win32 Midi Input Error Function

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern int midiInGetErrorText(int errCode, 
            StringBuilder errMsg, int sizeOfErrMsg);

        #endregion

        #region Fields

        // Error message.
        private StringBuilder errMsg = new StringBuilder(128);

        #endregion 

        #region Construction

        /// <summary>
        /// Initializes a new instance of the InputDeviceException class with
        /// the specified error code.
        /// </summary>
        /// <param name="errCode">
        /// The error code.
        /// </param>
        public InputDeviceException(int errCode) : base(errCode)
        {
            // Get error message.
            midiInGetErrorText(errCode, errMsg, errMsg.Capacity);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        public override string Message
        {
            get
            {
                return errMsg.ToString();
            }
        }

        #endregion

        #endregion
    }
}
