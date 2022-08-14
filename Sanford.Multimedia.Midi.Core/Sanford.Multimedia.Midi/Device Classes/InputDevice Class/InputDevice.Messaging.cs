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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Sanford.Multimedia;

namespace Sanford.Multimedia.Midi
{
    internal struct MidiInParams
    {
        public readonly IntPtr Param1;
        public readonly IntPtr Param2;

        public MidiInParams(IntPtr param1, IntPtr param2)
        {
            Param1 = param1;
            Param2 = param2;
        }
    }

    public partial class InputDevice : MidiDevice
    {
        /// <summary>
        /// Gets or sets a value indicating whether the midi input driver callback should be posted on a delegate queue with its own thread.
        /// Default is <c>true</c>. If set to <c>false</c> the driver callback directly calls the events for lowest possible latency.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the midi input driver callback should be posted on a delegate queue with its own thread; otherwise, <c>false</c>.
        /// </value>
        public bool PostDriverCallbackToDelegateQueue
        {
            get;
            set;
        }

        int FLastParam2;

        private void HandleMessage(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2)
        {
            var param = new MidiInParams(param1, param2);

            if (msg == MIM_OPEN)
            {
            }
            else if (msg == MIM_CLOSE)
            {
            }
            else if (msg == MIM_DATA)
            {
                if (PostDriverCallbackToDelegateQueue)
                    delegateQueue.Post(HandleShortMessage, param);
                else
                    HandleShortMessage(param);
            }
            else if (msg == MIM_MOREDATA)
            {
                if (PostDriverCallbackToDelegateQueue)
                    delegateQueue.Post(HandleShortMessage, param);
                else
                    HandleShortMessage(param);
            }
            else if (msg == MIM_LONGDATA)
            {
                if (PostDriverCallbackToDelegateQueue)
                    delegateQueue.Post(HandleSysExMessage, param);
                else
                    HandleSysExMessage(param);
            }
            else if (msg == MIM_ERROR)
            {
                if (PostDriverCallbackToDelegateQueue)
                    delegateQueue.Post(HandleInvalidShortMessage, param);
                else
                    HandleInvalidShortMessage(param);
            }
            else if (msg == MIM_LONGERROR)
            {
                if (PostDriverCallbackToDelegateQueue)
                    delegateQueue.Post(HandleInvalidSysExMessage, param);
                else
                    HandleInvalidSysExMessage(param);
            }
        }

        private void HandleShortMessage(object state)
        {

            var param = (MidiInParams)state;
            int message = param.Param1.ToInt32();
            int timestamp = param.Param2.ToInt32();

            //first send RawMessage
            OnShortMessage(new ShortMessageEventArgs(message, timestamp));

            int status = ShortMessage.UnpackStatus(message);

            if (status >= (int)ChannelCommand.NoteOff &&
                   status <= (int)ChannelCommand.PitchWheel +
                   ChannelMessage.MidiChannelMaxValue)
            {
                cmBuilder.Message = message;
                cmBuilder.Build();

                cmBuilder.Result.Timestamp = timestamp;
                OnMessageReceived(cmBuilder.Result);
                OnChannelMessageReceived(new ChannelMessageEventArgs(cmBuilder.Result));
            }
            else if (status == (int)SysCommonType.MidiTimeCode ||
                   status == (int)SysCommonType.SongPositionPointer ||
                   status == (int)SysCommonType.SongSelect ||
                   status == (int)SysCommonType.TuneRequest)
            {
                scBuilder.Message = message;
                scBuilder.Build();

                scBuilder.Result.Timestamp = timestamp;
                OnMessageReceived(scBuilder.Result);
                OnSysCommonMessageReceived(new SysCommonMessageEventArgs(scBuilder.Result));
            }
            else
            {
                SysRealtimeMessageEventArgs e = null;

                switch ((SysRealtimeType)status)
                {
                    case SysRealtimeType.ActiveSense:
                        e = SysRealtimeMessageEventArgs.ActiveSense;
                        break;

                    case SysRealtimeType.Clock:
                        e = SysRealtimeMessageEventArgs.Clock;
                        break;

                    case SysRealtimeType.Continue:
                        e = SysRealtimeMessageEventArgs.Continue;
                        break;

                    case SysRealtimeType.Reset:
                        e = SysRealtimeMessageEventArgs.Reset;
                        break;

                    case SysRealtimeType.Start:
                        e = SysRealtimeMessageEventArgs.Start;
                        break;

                    case SysRealtimeType.Stop:
                        e = SysRealtimeMessageEventArgs.Stop;
                        break;

                    case SysRealtimeType.Tick:
                        e = SysRealtimeMessageEventArgs.Tick;
                        break;
                }

                e.Message.Timestamp = timestamp;
                OnMessageReceived(e.Message);
                OnSysRealtimeMessageReceived(e);
            }
        }

        private void HandleSysExMessage(object state)
        {
            lock (lockObject)
            {
                var param = (MidiInParams)state;
                IntPtr headerPtr = param.Param1;

                MidiHeader header = (MidiHeader)Marshal.PtrToStructure(headerPtr, typeof(MidiHeader));

                if (!resetting)
                {
                    for (int i = 0; i < header.bytesRecorded; i++)
                    {
                        sysExData.Add(Marshal.ReadByte(header.data, i));
                    }

                    if (sysExData.Count > 1 && sysExData[0] == 0xF0 && sysExData[sysExData.Count - 1] == 0xF7)
                    {
                        SysExMessage message = new SysExMessage(sysExData.ToArray());
                        message.Timestamp = param.Param2.ToInt32();

                        sysExData.Clear();

                        OnMessageReceived(message);
                        OnSysExMessageReceived(new SysExMessageEventArgs(message));
                    }

                    int result = AddSysExBuffer();

                    if (result != DeviceException.MMSYSERR_NOERROR)
                    {
                        Exception ex = new InputDeviceException(result);

                        OnError(new ErrorEventArgs(ex));
                    }
                }

                ReleaseBuffer(headerPtr);
            }
        }

        private void HandleInvalidShortMessage(object state)
        {
            var param = (MidiInParams)state;
            OnInvalidShortMessageReceived(new InvalidShortMessageEventArgs(param.Param1.ToInt32()));
        }

        private void HandleInvalidSysExMessage(object state)
        {
            lock (lockObject)
            {
                var param = (MidiInParams)state;
                IntPtr headerPtr = param.Param1;

                MidiHeader header = (MidiHeader)Marshal.PtrToStructure(headerPtr, typeof(MidiHeader));

                if (!resetting)
                {
                    byte[] data = new byte[header.bytesRecorded];

                    Marshal.Copy(header.data, data, 0, data.Length);

                    OnInvalidSysExMessageReceived(new InvalidSysExMessageEventArgs(data));

                    int result = AddSysExBuffer();

                    if (result != DeviceException.MMSYSERR_NOERROR)
                    {
                        Exception ex = new InputDeviceException(result);

                        OnError(new ErrorEventArgs(ex));
                    }
                }

                ReleaseBuffer(headerPtr);
            }
        }

        private void ReleaseBuffer(IntPtr headerPtr)
        {
            int result = midiInUnprepareHeader(Handle, headerPtr, SizeOfMidiHeader);

            if (result != DeviceException.MMSYSERR_NOERROR)
            {
                Exception ex = new InputDeviceException(result);

                OnError(new ErrorEventArgs(ex));
            }

            headerBuilder.Destroy(headerPtr);

            bufferCount--;

            Debug.Assert(bufferCount >= 0);

            Monitor.Pulse(lockObject);
        }

        public int AddSysExBuffer()
        {
            int result;

            // Initialize the MidiHeader builder.
            headerBuilder.BufferLength = sysExBufferSize;
            headerBuilder.Build();

            // Get the pointer to the built MidiHeader.
            IntPtr headerPtr = headerBuilder.Result;

            // Prepare the header to be used.
            result = midiInPrepareHeader(Handle, headerPtr, SizeOfMidiHeader);

            // If the header was perpared successfully.
            if (result == DeviceException.MMSYSERR_NOERROR)
            {
                bufferCount++;

                // Add the buffer to the InputDevice.
                result = midiInAddBuffer(Handle, headerPtr, SizeOfMidiHeader);

                // If the buffer could not be added.
                if (result != MidiDeviceException.MMSYSERR_NOERROR)
                {
                    // Unprepare header - there's a chance that this will fail 
                    // for whatever reason, but there's not a lot that can be
                    // done at this point.
                    midiInUnprepareHeader(Handle, headerPtr, SizeOfMidiHeader);

                    bufferCount--;

                    // Destroy header.
                    headerBuilder.Destroy();
                }
            }
            // Else the header could not be prepared.
            else
            {
                // Destroy header.
                headerBuilder.Destroy();
            }

            return result;
        }
    }
}
