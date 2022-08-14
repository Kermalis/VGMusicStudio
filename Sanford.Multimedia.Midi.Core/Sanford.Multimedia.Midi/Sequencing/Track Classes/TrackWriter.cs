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
using System.IO;

namespace Sanford.Multimedia.Midi
{
	/// <summary>
	/// Writes a Track to a Stream.
	/// </summary>
    internal class TrackWriter
    {
        private static readonly byte[] TrackHeader =
            {
                (byte)'M',
                (byte)'T',
                (byte)'r',
                (byte)'k'
            };

        // The Track to write to the Stream.
        private Track track = new Track();

        // The Stream to write to.
        private Stream stream;

        // Running status.
        private int runningStatus = 0;        

        // The Track data in raw bytes.
        private List<byte> trackData = new List<byte>();

        public void Write(Stream strm)
        {
            this.stream = strm;

            trackData.Clear();

            stream.Write(TrackHeader, 0, TrackHeader.Length);

            foreach(MidiEvent e in track.Iterator())
            {                
                WriteVariableLengthValue(e.DeltaTicks);

                switch(e.MidiMessage.MessageType)
                {
                    case MessageType.Channel:
                        Write((ChannelMessage)e.MidiMessage);
                        break;

                    case MessageType.SystemExclusive:
                        Write((SysExMessage)e.MidiMessage);
                        break;

                    case MessageType.Meta:
                        Write((MetaMessage)e.MidiMessage);
                        break;

                    case MessageType.SystemCommon:
                        Write((SysCommonMessage)e.MidiMessage);
                        break;

                    case MessageType.SystemRealtime:
                        Write((SysRealtimeMessage)e.MidiMessage);
                        break;
				
		    case MessageType.Short:
			Write((ShortMessage)e.MidiMessage);
			break;
                }
            }

            byte[] trackLength = BitConverter.GetBytes(trackData.Count);

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(trackLength);
            }

            stream.Write(trackLength, 0, trackLength.Length);

            foreach(byte b in trackData)
            {
                stream.WriteByte(b);
            }
        }

        private void WriteVariableLengthValue(int value)
        {
            int v = value;
            byte[] array = new byte[4];
            int count = 0;

            array[0] = (byte)(v & 0x7F);

            v >>= 7;

            while(v > 0)
            {
                count++;
                array[count] = (byte)((v & 0x7F) | 0x80);
                v >>= 7;
            }

            while(count >= 0)
            {
                trackData.Add(array[count]);
                count--;
            }
        }
	    
        private void Write(ShortMessage message)
	{
		trackData.AddRange(message.GetBytes());
	}

        private void Write(ChannelMessage message)
        {
            if(runningStatus != message.Status)
            {
                trackData.Add((byte)message.Status);
                runningStatus = message.Status;
            }

            trackData.Add((byte)message.Data1);

            if(ChannelMessage.DataBytesPerType(message.Command) == 2)
            {
                trackData.Add((byte)message.Data2);
            }
        }

        private void Write(SysExMessage message)
        {
            // System exclusive message cancel running status.
            runningStatus = 0;

            trackData.Add((byte)message.Status);

            WriteVariableLengthValue(message.Length - 1);

            for(int i = 1; i < message.Length; i++)
            {
                trackData.Add(message[i]);
            }
        }

        private void Write(MetaMessage message)
        {
            trackData.Add((byte)message.Status);
            trackData.Add((byte)message.MetaType);

            WriteVariableLengthValue(message.Length);

            trackData.AddRange(message.GetBytes());
        }

        private void Write(SysCommonMessage message)
        {
            // Escaped messages cancel running status.
            runningStatus = 0;

            // Escaped message.
            trackData.Add((byte)0xF7);

            trackData.Add((byte)message.Status);

            switch(message.SysCommonType)
            {
                case SysCommonType.MidiTimeCode:
                    trackData.Add((byte)message.Data1);
                    break;

                case SysCommonType.SongPositionPointer:
                    trackData.Add((byte)message.Data1);
                    trackData.Add((byte)message.Data2);
                    break;

                case SysCommonType.SongSelect:
                    trackData.Add((byte)message.Data1);
                    break;
            }
        }

        private void Write(SysRealtimeMessage message)
        {
            // Escaped messages cancel running status.
            runningStatus = 0;

            // Escaped message.
            trackData.Add((byte)0xF7);

            trackData.Add((byte)message.Status);
        }

        /// <summary>
        /// Gets or sets the Track to write to the Stream.
        /// </summary>
        public Track Track
        {
            get
            {
                return track;
            }
            set
            {
                #region Require

                if(value == null)
                {
                    throw new ArgumentNullException("Track");
                }

                #endregion

                runningStatus = 0;
                trackData.Clear();

                track = value;
            }
        }
    }
}
