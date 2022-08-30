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
using System.Threading;
using Sanford.Threading;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// This is an abstract class for MIDI output devices.
    /// </summary>
    public abstract class OutputDeviceBase : MidiDevice
    {
        /// <summary>
        /// Handles resetting the MIDI output device.
        /// </summary>
        [DllImport("winmm.dll")]
        protected static extern int midiOutReset(IntPtr DeviceHandle);

        /// <summary>
        /// Handles the MIDI output device short messages.
        /// </summary>
        [DllImport("winmm.dll")]
        protected static extern int midiOutShortMsg(IntPtr DeviceHandle, int message);

        /// <summary>
        /// Handles preparing the headers for the MIDI output device.
        /// </summary>
        [DllImport("winmm.dll")]
        protected static extern int midiOutPrepareHeader(IntPtr DeviceHandle,
            IntPtr headerPtr, int sizeOfMidiHeader);

        /// <summary>
        /// Handles unpreparing the headers for the MIDI output device.
        /// </summary>
        [DllImport("winmm.dll")]
        protected static extern int midiOutUnprepareHeader(IntPtr DeviceHandle,
            IntPtr headerPtr, int sizeOfMidiHeader);

        /// <summary>
        /// Handles the MIDI output device long message.
        /// </summary>
        [DllImport("winmm.dll")]
        protected static extern int midiOutLongMsg(IntPtr DeviceHandle,
            IntPtr headerPtr, int sizeOfMidiHeader);

        /// <summary>
        /// Obtains the MIDI output device caps.
        /// </summary>
        [DllImport("winmm.dll")]
        protected static extern int midiOutGetDevCaps(IntPtr deviceID,
            ref MidiOutCaps caps, int sizeOfMidiOutCaps);

        /// <summary>
        /// Obtains the number of MIDI output devices.
        /// </summary>
        [DllImport("winmm.dll")]
        protected static extern int midiOutGetNumDevs();

        /// <summary>
        /// A construct integer that tells the compiler that hexadecimal value 0x3C7 means MOM_OPEN.
        /// </summary>
        protected const int MOM_OPEN = 0x3C7;

        /// <summary>
        /// A construct integer that tells the compiler that hexadecimal value 0x3C8 means MOM_CLOSE.
        /// </summary>
        protected const int MOM_CLOSE = 0x3C8;

        /// <summary>
        /// A construct integer that tells the compiler that hexadecimal value 0x3C9 means MOM_DONE.
        /// </summary>
        protected const int MOM_DONE = 0x3C9;

        /// <summary>
        /// This delegate is a generic delegate for the MIDI output devices.
        /// </summary>
        protected delegate void GenericDelegate<T>(T args);

        /// <summary>
        /// Represents the method that handles messages from Windows.
        /// </summary>
        protected delegate void MidiOutProc(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2);

        /// <summary>
        /// For releasing buffers.
        /// </summary>
        protected DelegateQueue delegateQueue = new DelegateQueue();

        /// <summary>
        /// This object remains locked in place.
        /// </summary>
        protected readonly object lockObject = new object();

        /// <summary>
        /// The number of buffers still in the queue.
        /// </summary>
        protected int bufferCount = 0;

        /// <summary>
        /// Builds MidiHeader structures for sending system exclusive messages.
        /// </summary>
        private MidiHeaderBuilder headerBuilder = new MidiHeaderBuilder();

        /// <summary>
        /// The device handle.
        /// </summary>
        protected IntPtr DeviceHandle = IntPtr.Zero;        

        /// <summary>
        /// Base class for output devices with an integer.
        /// </summary>
        /// <param name="deviceID">
        /// Device ID is used here.
        /// </param>
        public OutputDeviceBase(int deviceID) : base(deviceID)
        {
        }

        /// <summary>
        /// Disposes when it has been closed.
        /// </summary>
        ~OutputDeviceBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// This dispose function will dispose all delegates that are queued when closed.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                delegateQueue.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Sends the MIDI output channel device message.
        /// </summary>
        public virtual void Send(ChannelMessage message)
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            Send(message.Message);
        }

        /// <summary>
        /// Sends a short MIDI output channel device message.
        /// </summary>
        public virtual void SendShort(int message)
        {
            #region Require

            if (IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            Send(message);
        }

        /// <summary>
        /// Sends a system ex MIDI output channel device message.
        /// </summary>
        public virtual void Send(SysExMessage message)
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            lock(lockObject)
            {
                headerBuilder.InitializeBuffer(message);
                headerBuilder.Build();

                // Prepare system exclusive buffer.
                int result = midiOutPrepareHeader(Handle, headerBuilder.Result, SizeOfMidiHeader);

                // If the system exclusive buffer was prepared successfully.
                if(result == MidiDeviceException.MMSYSERR_NOERROR)
                {
                    bufferCount++;

                    // Send system exclusive message.
                    result = midiOutLongMsg(Handle, headerBuilder.Result, SizeOfMidiHeader);

                    // If the system exclusive message could not be sent.
                    if(result != MidiDeviceException.MMSYSERR_NOERROR)
                    {
                        midiOutUnprepareHeader(Handle, headerBuilder.Result, SizeOfMidiHeader);
                        bufferCount--;
                        headerBuilder.Destroy();

                        // Throw an exception.
                        throw new OutputDeviceException(result);
                    }
                }
                // Else the system exclusive buffer could not be prepared.
                else
                {
                    // Destroy system exclusive buffer.
                    headerBuilder.Destroy();

                    // Throw an exception.
                    throw new OutputDeviceException(result);
                }
            }
        }

        /// <summary>
        /// Sends a system common MIDI output device message.
        /// </summary>
        public virtual void Send(SysCommonMessage message)
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            Send(message.Message);
        }

        /// <summary>
        /// Sends a system realtime MIDI output device message.
        /// </summary>
        public virtual void Send(SysRealtimeMessage message)
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            Send(message.Message);
        }

        /// <summary>
        /// Resets the MIDI output device.
        /// </summary>
        public override void Reset()
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            lock(lockObject)
            {
                // Reset the OutputDevice.
                int result = midiOutReset(Handle); 

                if(result == MidiDeviceException.MMSYSERR_NOERROR)
                {
                    while(bufferCount > 0)
                    {
                        Monitor.Wait(lockObject);
                    }
                }
                else
                {
                    // Throw an exception.
                    throw new OutputDeviceException(result);
                }                
            }
        }

        /// <summary>
        /// Sends a MIDI output device message.
        /// </summary>
        protected void Send(int message)
        {
            lock(lockObject)
            {
                int result = midiOutShortMsg(Handle, message);

                if(result != MidiDeviceException.MMSYSERR_NOERROR)
                {
                    throw new OutputDeviceException(result);
                }
            }
        }

        /// <summary>
        /// Initializes the MIDI output device capabilities.
        /// </summary>
        public static MidiOutCaps GetDeviceCapabilities(int deviceID)
        {
            MidiOutCaps caps = new MidiOutCaps();

            // Get the device's capabilities.
            IntPtr devId = (IntPtr)deviceID;
            int result = midiOutGetDevCaps(devId, ref caps, Marshal.SizeOf(caps));

            // If the capabilities could not be retrieved.
            if(result != MidiDeviceException.MMSYSERR_NOERROR)
            {
                // Throw an exception.
                throw new OutputDeviceException(result);
            }

            return caps;
        }

        /// <summary>
        /// Handles Windows messages.
        /// </summary>
        protected virtual void HandleMessage(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2)
        {
            if(msg == MOM_OPEN)
            {
            }
            else if(msg == MOM_CLOSE)
            {
            }
            else if(msg == MOM_DONE)
            {
                delegateQueue.Post(ReleaseBuffer, param1);
            }
        }

        /// <summary>
        /// Releases buffers.
        /// </summary>
        private void ReleaseBuffer(object state)
        {
            lock(lockObject)
            {
                IntPtr headerPtr = (IntPtr)state;

                // Unprepare the buffer.
                int result = midiOutUnprepareHeader(Handle, headerPtr, SizeOfMidiHeader);

                if(result != MidiDeviceException.MMSYSERR_NOERROR)
                {
                    Exception ex = new OutputDeviceException(result);

                    OnError(new ErrorEventArgs(ex));
                }

                // Release the buffer resources.
                headerBuilder.Destroy(headerPtr);

                bufferCount--;

                Monitor.Pulse(lockObject);

                Debug.Assert(bufferCount >= 0);                
            }
        }

        /// <summary>
        /// When closed, disposes the object that is locked in place.
        /// </summary>
        public override void Dispose()
        {
            #region Guard

            if(IsDisposed)
            {
                return;
            }

            #endregion

            lock(lockObject)
            {
                Close();          
            }
        }

        /// <summary>
        /// Handles the MIDI output device pointer.
        /// </summary>
        public override IntPtr Handle
        {
            get
            {
                return DeviceHandle;
            }
        }

        /// <summary>
        /// Counts the number of MIDI output devices.
        /// </summary>
        public static int DeviceCount
        {
            get
            {
                return midiOutGetNumDevs();
            }
        }        
    }
}
