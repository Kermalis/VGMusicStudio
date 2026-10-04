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
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Sanford.Multimedia.Timers;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Sealed class stream for MIDI output devices.
    /// </summary>
    public sealed class OutputStream : OutputDeviceBase
    {
        [DllImport("winmm.dll")]
        private static extern int midiStreamOpen(ref IntPtr DeviceHandle, ref int deviceID, int reserved,
            OutputDevice.MidiOutProc proc, IntPtr instance, uint flag);

        [DllImport("winmm.dll")]
        private static extern int midiStreamClose(IntPtr DeviceHandle);

        [DllImport("winmm.dll")]
        private static extern int midiStreamOut(IntPtr DeviceHandle, IntPtr headerPtr, int sizeOfMidiHeader);

        [DllImport("winmm.dll")]
        private static extern int midiStreamPause(IntPtr DeviceHandle);

        [DllImport("winmm.dll")]
        private static extern int midiStreamPosition(IntPtr DeviceHandle, ref Time t, int sizeOfTime);

        [DllImport("winmm.dll")]
        private static extern int midiStreamProperty(IntPtr DeviceHandle, ref Property p, uint flags);

        [DllImport("winmm.dll")]
        private static extern int midiStreamRestart(IntPtr DeviceHandle);

        [DllImport("winmm.dll")]
        private static extern int midiStreamStop(IntPtr DeviceHandle);

        [StructLayout(LayoutKind.Sequential)]
        private struct Property
        {
            public int sizeOfProperty;
            public int property;
        }

        private const uint MIDIPROP_SET = 0x80000000;
        private const uint MIDIPROP_GET = 0x40000000;
        private const uint MIDIPROP_TIMEDIV = 0x00000001;
        private const uint MIDIPROP_TEMPO = 0x00000002;

        private const byte MEVT_CALLBACK = 0x40;

        private const byte MEVT_SHORTMSG = 0x00;
        private const byte MEVT_TEMPO = 0x01;
        private const byte MEVT_NOP = 0x02;
        private const byte MEVT_LONGMSG = 0x80;
        private const byte MEVT_COMMENT = 0x82;
        private const byte MEVT_VERSION = 0x84;

        private const int MOM_POSITIONCB = 0x3CA;

        private const int SizeOfMidiEvent = 12;

        private const int EventTypeIndex = 11;

        private const int EventCodeOffset = 8;

        private MidiOutProc midiOutProc;

        private int offsetTicks = 0;

        private byte[] streamID = new byte[4];

        private List<byte> events = new List<byte>();

        private MidiHeaderBuilder headerBuilder = new MidiHeaderBuilder();

        /// <summary>
        /// Handles the event for no operations.
        /// </summary>
        public event EventHandler<NoOpEventArgs> NoOpOccurred;

        /// <summary>
        /// Stream for MIDI output devices.
        /// </summary>
        public OutputStream(int deviceID) : base(deviceID)
        {
            midiOutProc = HandleMessage;

            int result = midiStreamOpen(ref DeviceHandle, ref deviceID, 1, midiOutProc, IntPtr.Zero, CALLBACK_FUNCTION);

            if(result != MidiDeviceException.MMSYSERR_NOERROR)
            {
                throw new OutputDeviceException(result);
            }
        }

        /// <summary>
        /// Disposes the streams when closed.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                lock(lockObject)
                {
                    Reset();

                    int result = midiStreamClose(Handle);

                    if(result != MidiDeviceException.MMSYSERR_NOERROR)
                    {
                        throw new OutputDeviceException(result);
                    }
                }
            }
            else
            {
                midiOutReset(Handle);
                midiStreamClose(Handle);
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// When the application is closed, this will dispose of any streams.
        /// </summary>
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
        /// Starts playing the stream.
        /// </summary>
        public void StartPlaying()
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException("OutputStream");
            }

            #endregion

            lock(lockObject)
            {
                int result = midiStreamRestart(Handle);

                if(result != MidiDeviceException.MMSYSERR_NOERROR)
                {
                    throw new OutputDeviceException(result);
                }
            }
        }

        /// <summary>
        /// Pauses playing the stream.
        /// </summary>
        public void PausePlaying()
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException("OutputStream");
            }

            #endregion

            lock(lockObject)
            {
                int result = midiStreamPause(Handle);

                if(result != MidiDeviceException.MMSYSERR_NOERROR)
                {
                    throw new OutputDeviceException(result);
                }
            }
        }

        /// <summary>
        /// Stops playing the stream.
        /// </summary>
        public void StopPlaying()
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException("OutputStream");
            }

            #endregion

            lock(lockObject)
            {
                int result = midiStreamStop(Handle);

                if(result != MidiDeviceException.MMSYSERR_NOERROR)
                {
                    throw new OutputDeviceException(result);
                }
            }
        }

        /// <summary>
        /// Resets the MIDI output device playing the stream.
        /// </summary>
        public override void Reset()
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException("OutputStream");
            }

            #endregion

            offsetTicks = 0;
            events.Clear();

            base.Reset();
        }

        /// <summary>
        /// Writes to the MIDI output device stream.
        /// </summary>
        public void Write(MidiEvent e)
        {
            switch(e.MidiMessage.MessageType)
            {
                case MessageType.Channel:
                case MessageType.SystemCommon:
                case MessageType.SystemRealtime:
                    Write(e.DeltaTicks, (ShortMessage)e.MidiMessage);
                    break;

                case MessageType.SystemExclusive:
                    Write(e.DeltaTicks, (SysExMessage)e.MidiMessage);
                    break;

                case MessageType.Meta:
                    Write(e.DeltaTicks, (MetaMessage)e.MidiMessage);
                    break;
            }
        }

        private void Write(int deltaTicks, ShortMessage message)
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException("OutputStream");
            }

            #endregion

            // Delta time.
            events.AddRange(BitConverter.GetBytes(deltaTicks + offsetTicks));

            // Stream ID.
            events.AddRange(streamID);

            // Event code.
            byte[] eventCode = message.GetBytes();
            eventCode[eventCode.Length - 1] = MEVT_SHORTMSG;            
            events.AddRange(eventCode);            

            offsetTicks = 0;
        }

        private void Write(int deltaTicks, SysExMessage message)
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException("OutputStream");
            }

            #endregion

            // Delta time.
            events.AddRange(BitConverter.GetBytes(deltaTicks + offsetTicks));

            // Stream ID.
            events.AddRange(streamID);

            // Event code.
            byte[] eventCode = BitConverter.GetBytes(message.Length);
            eventCode[eventCode.Length - 1] = MEVT_LONGMSG;
            events.AddRange(eventCode);

            byte[] sysExData;

            if(message.Length % 4 != 0)
            {
                sysExData = new byte[message.Length + (message.Length % 4)];
                message.GetBytes().CopyTo(sysExData, 0);
            }
            else
            {
                sysExData = message.GetBytes();
            }

            // SysEx data.
            events.AddRange(sysExData);

            offsetTicks = 0;
        }

        private void Write(int deltaTicks, MetaMessage message)
        {
            if(message.MetaType == MetaType.Tempo)
            {
                // Delta time.
                events.AddRange(BitConverter.GetBytes(deltaTicks + offsetTicks));

                // Stream ID.
                events.AddRange(streamID);

                TempoChangeBuilder builder = new TempoChangeBuilder(message);

                byte[] t = BitConverter.GetBytes(builder.Tempo);

                t[t.Length - 1] = MEVT_SHORTMSG | MEVT_TEMPO;

                // Event code.
                events.AddRange(t);

                offsetTicks = 0;
            }
            else
            {
                offsetTicks += deltaTicks;
            }
        }

        /// <summary>
        /// Writes the no operation for MIDI output device streams.
        /// </summary>
        public void WriteNoOp(int deltaTicks, int data)
        {
            // Delta time.
            events.AddRange(BitConverter.GetBytes(deltaTicks + offsetTicks));

            // Stream ID.
            events.AddRange(streamID);

            // Event code.
            byte[] eventCode = BitConverter.GetBytes(data);
            eventCode[eventCode.Length - 1] = (byte)(MEVT_NOP | MEVT_CALLBACK);
            events.AddRange(eventCode);

            offsetTicks = 0;            
        }

        /// <summary>
        /// Clears out all the MIDI output device streams when done.
        /// </summary>
        public void Flush()
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException("OutputStream");
            }

            #endregion

            lock(lockObject)
            {
                headerBuilder.InitializeBuffer(events);
                headerBuilder.Build();

                events.Clear();

                int result = midiOutPrepareHeader(Handle, headerBuilder.Result, SizeOfMidiHeader);

                if(result == MidiDeviceException.MMSYSERR_NOERROR)
                {
                    bufferCount++;
                }
                else
                {
                    headerBuilder.Destroy();

                    throw new OutputDeviceException(result);
                }

                result = midiStreamOut(Handle, headerBuilder.Result, SizeOfMidiHeader);

                if(result != MidiDeviceException.MMSYSERR_NOERROR)
                {
                    midiOutUnprepareHeader(Handle, headerBuilder.Result, SizeOfMidiHeader);

                    headerBuilder.Destroy();

                    throw new OutputDeviceException(result);
                }
            }
        }

        /// <summary>
        /// Initializes the amount of time for the MIDI output device stream.
        /// </summary>
        public Time GetTime(TimeType type)
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException("OutputStream");
            }

            #endregion

            Time t = new Time();

            t.type = (int)type;

            lock(lockObject)
            {
                int result = midiStreamPosition(Handle, ref t, Marshal.SizeOf(typeof(Time)));

                if(result != MidiDeviceException.MMSYSERR_NOERROR)
                {
                    throw new OutputDeviceException(result);
                }
            }

            return t;
        }

        private void OnNoOpOccurred(NoOpEventArgs e)
        {
            EventHandler<NoOpEventArgs> handler = NoOpOccurred;

            if(handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Handles the messages for the MIDI output device streams.
        /// </summary>
        protected override void HandleMessage(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2)
        {
            if(msg == MOM_POSITIONCB)
            {
                delegateQueue.Post(HandleNoOp, param1);
            }
            else
            {
                base.HandleMessage(DeviceHandle, msg, instance, param1, param2);
            }
        }

        private void HandleNoOp(object state)
        {
            IntPtr headerPtr = (IntPtr)state;
            MidiHeader header = (MidiHeader)Marshal.PtrToStructure(headerPtr, typeof(MidiHeader));

            byte[] midiEvent = new byte[SizeOfMidiEvent];

            for(int i = 0; i < midiEvent.Length; i++)
            {
                midiEvent[i] = Marshal.ReadByte(header.data, header.offset + i);
            }

            // If this is a NoOp event.
            if((midiEvent[EventTypeIndex] & MEVT_NOP) == MEVT_NOP)
            {
                // Clear the event type byte.
                midiEvent[EventTypeIndex] = 0;

                NoOpEventArgs e = new NoOpEventArgs(BitConverter.ToInt32(midiEvent, EventCodeOffset));

                context.Post(new SendOrPostCallback(delegate(object s)
                {
                    OnNoOpOccurred(e);
                }), null);
            }
        }

        /// <summary>
        /// Gets the size of the MIDI output device stream and sets the amount to be divided.
        /// </summary>
        public int Division
        {
            get
            {
                #region Require

                if(IsDisposed)
                {
                    throw new ObjectDisposedException("OutputStream");
                }

                #endregion

                Property d = new Property();

                d.sizeOfProperty = Marshal.SizeOf(typeof(Property));

                lock(lockObject)
                {
                    int result = midiStreamProperty(Handle, ref d, MIDIPROP_GET | MIDIPROP_TIMEDIV);

                    if(result != MidiDeviceException.MMSYSERR_NOERROR)
                    {
                        throw new OutputDeviceException(result);
                    }
                }

                return d.property;
            }
            set
            {
                #region Require

                if(IsDisposed)
                {
                    throw new ObjectDisposedException("OutputStream");
                }
                else if(value < PpqnClock.PpqnMinValue)
                {
                    throw new ArgumentOutOfRangeException("Ppqn", value,
                        "Pulses per quarter note is smaller than 24.");
                }

                #endregion

                Property d = new Property();

                d.sizeOfProperty = Marshal.SizeOf(typeof(Property));
                d.property = value;

                lock(lockObject)
                {
                    int result = midiStreamProperty(Handle, ref d, MIDIPROP_SET | MIDIPROP_TIMEDIV);

                    if(result != MidiDeviceException.MMSYSERR_NOERROR)
                    {
                        throw new OutputDeviceException(result);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the amount of tempo, then sets the tempo for the MIDI output device stream.
        /// </summary>
        public int Tempo
        {
            get
            {
                #region Require

                if(IsDisposed)
                {
                    throw new ObjectDisposedException("OutputStream");
                }

                #endregion

                Property t = new Property();
                t.sizeOfProperty = Marshal.SizeOf(typeof(Property));

                lock(lockObject)
                {
                    int result = midiStreamProperty(Handle, ref t, MIDIPROP_GET | MIDIPROP_TEMPO);

                    if(result != MidiDeviceException.MMSYSERR_NOERROR)
                    {
                        throw new OutputDeviceException(result);
                    }
                }

                return t.property;
            }
            set
            {
                #region Require

                if(IsDisposed)
                {
                    throw new ObjectDisposedException("OutputStream");
                }
                else if(value < 0)
                {
                    throw new ArgumentOutOfRangeException("Tempo", value,
                        "Tempo out of range.");
                }

                #endregion

                Property t = new Property();
                t.sizeOfProperty = Marshal.SizeOf(typeof(Property));
                t.property = value;

                lock(lockObject)
                {
                    int result = midiStreamProperty(Handle, ref t, MIDIPROP_SET | MIDIPROP_TEMPO);

                    if(result != MidiDeviceException.MMSYSERR_NOERROR)
                    {
                        throw new OutputDeviceException(result);
                    }
                }
            }
        }
    }
}
