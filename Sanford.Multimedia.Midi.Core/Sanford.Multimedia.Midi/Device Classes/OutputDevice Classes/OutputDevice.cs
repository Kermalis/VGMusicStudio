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
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Sanford.Multimedia.Midi
{
	/// <summary>
	/// Represents a device capable of sending MIDI messages.
	/// </summary>
	public sealed class OutputDevice : OutputDeviceBase
	{
        #region Win32 Midi Output Functions and Constants

        [DllImport("winmm.dll")]
        private static extern int midiOutOpen(out IntPtr DeviceHandle, int deviceID,
            MidiOutProc proc, IntPtr instance, int flags);

        [DllImport("winmm.dll")]
        private static extern int midiOutClose(IntPtr DeviceHandle);

        #endregion 

        private MidiOutProc midiOutProc;

        private bool runningStatusEnabled = false;

        private int runningStatus = 0;        

        #region Construction

        /// <summary>
        /// Initializes a new instance of the OutputDevice class.
        /// </summary>
        public OutputDevice(int deviceID) : base(deviceID)
        {
            midiOutProc = HandleMessage;

            int result = midiOutOpen(out DeviceHandle, deviceID, midiOutProc, IntPtr.Zero, CALLBACK_FUNCTION);

            if(result != MidiDeviceException.MMSYSERR_NOERROR)
            {
                throw new OutputDeviceException(result);
            }
        }

        #endregion

        /// <summary>
        /// When closed, disposes of the MIDI output device.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                lock(lockObject)
                {
                    Reset();

                    // Close the OutputDevice.
                    int result = midiOutClose(Handle);

                    if(result != MidiDeviceException.MMSYSERR_NOERROR)
                    {
                        // Throw an exception.
                        throw new OutputDeviceException(result);
                    }
                }
            }
            else
            {
                midiOutReset(Handle);
                midiOutClose(Handle);
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Closes the OutputDevice.
        /// </summary>
        /// <exception cref="OutputDeviceException">
        /// If an error occurred while closing the OutputDevice.
        /// </exception>
        public override void Close()
        {
            #region Guard

            if(IsDisposed)
            {
                return;
            }

            #endregion

            Dispose(true);            
        }

        /// <summary>
        /// Resets the OutputDevice.
        /// </summary>
        public override void Reset()
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            runningStatus = 0;

            base.Reset();
        }

        /// <summary>
        /// Sends the MIDI output channel device message.
        /// </summary>
        public override void Send(ChannelMessage message)
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            lock(lockObject)
            {
                // If running status is enabled.
                if(runningStatusEnabled)
                {
                    // If the message's status value matches the running status.
                    if(message.Status == runningStatus)
                    {
                        // Send only the two data bytes without the status byte.
                        Send(message.Message >> 8);
                    }
                    // Else the message's status value does not match the running
                    // status.
                    else
                    {
                        // Send complete message with status byte.
                        Send(message.Message);

                        // Update running status.
                        runningStatus = message.Status;
                    }
                }
                // Else running status has not been enabled.
                else
                {
                    Send(message.Message);
                }
            }
        }

        /// <summary>
        /// Sends a system ex MIDI output device message.
        /// </summary>
        public override void Send(SysExMessage message)
        {
            // System exclusive cancels running status.
            runningStatus = 0;

            base.Send(message);
        }

        /// <summary>
        /// Sends a system common MIDI output device message.
        /// </summary>
        public override void Send(SysCommonMessage message)
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            // System common cancels running status.
            runningStatus = 0;

            base.Send(message);
        }

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the OutputDevice uses
        /// a running status.
        /// </summary>
        public bool RunningStatusEnabled
        {
            get
            {
                return runningStatusEnabled;
            }
            set
            {
                runningStatusEnabled = value;

                // Reset running status.
                runningStatus = 0;
            }
        }
        
        #endregion
    }

    /// <summary>
    /// The exception that is thrown when a error occurs with the OutputDevice
    /// class.
    /// </summary>
    public class OutputDeviceException : MidiDeviceException
    {
        #region OutputDeviceException Members

        #region Win32 Midi Output Error Function

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern int midiOutGetErrorText(int errCode, 
            StringBuilder message, int sizeOfMessage);

        #endregion

        #region Fields

        // The error message.
        private StringBuilder message = new StringBuilder(128);        

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the OutputDeviceException class with
        /// the specified error code.
        /// </summary>
        /// <param name="errCode">
        /// The error code.
        /// </param>
        public OutputDeviceException(int errCode) : base(errCode)
        {
            // Get error message.
            midiOutGetErrorText(errCode, message, message.Capacity);
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
                return message.ToString();
            }
        }        

        #endregion

        #endregion
    }
}
