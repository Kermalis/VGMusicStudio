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
using System.Collections;
using System.Runtime.InteropServices;

namespace Sanford.Multimedia.Midi
{
	/// <summary>
	/// Builds a pointer to a MidiHeader structure.
	/// </summary>
	internal class MidiHeaderBuilder
	{
        // The length of the system exclusive buffer.
        private int bufferLength;

        // The system exclusive data.
        private byte[] data;

        // Indicates whether the pointer to the MidiHeader has been built.
        private bool built = false;

        // The built pointer to the MidiHeader.
        private IntPtr result;

        /// <summary>
        /// Initializes a new instance of the MidiHeaderBuilder.
        /// </summary>
		public MidiHeaderBuilder()
		{
            BufferLength = 1;
		}

        #region Methods

        /// <summary>
        /// Builds the pointer to the MidiHeader structure.
        /// </summary>
        public void Build()
        {
            MidiHeader header = new MidiHeader();

            // Initialize the MidiHeader.
            header.bufferLength = BufferLength;
            header.bytesRecorded = BufferLength;
            header.data = Marshal.AllocHGlobal(BufferLength);
            header.flags = 0;

            // Write data to the MidiHeader.
            for(int i = 0; i < BufferLength; i++)
            {
                Marshal.WriteByte(header.data, i, data[i]);
            }

            try
            {
                result = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MidiHeader)));
            }
            catch(Exception)
            {
                Marshal.FreeHGlobal(header.data);

                throw;
            }

            try
            {
                Marshal.StructureToPtr(header, result, false);
            }
            catch(Exception)
            {
                Marshal.FreeHGlobal(header.data);
                Marshal.FreeHGlobal(result);

                throw;
            }

            built = true;
        }

        /// <summary>
        /// Initializes the MidiHeaderBuilder with the specified SysExMessage.
        /// </summary>
        /// <param name="message">
        /// The SysExMessage to use for initializing the MidiHeaderBuilder.
        /// </param>
        public void InitializeBuffer(SysExMessage message)
        {
            // If this is a start system exclusive message.
            if(message.SysExType == SysExType.Start)
            {
                BufferLength = message.Length;

                // Copy entire message.
                for(int i = 0; i < BufferLength; i++)
                {
                    data[i] = message[i];
                }
            }
            // Else this is a continuation message.
            else
            {
                BufferLength = message.Length - 1;

                // Copy all but the first byte of message.
                for(int i = 0; i < BufferLength; i++)
                {
                    data[i] = message[i + 1];
                }
            }
        }

        public void InitializeBuffer(ICollection events)
        {
            #region Require

            if(events == null)
            {
                throw new ArgumentNullException("events");
            }
            else if(events.Count % 4 != 0)
            {
                throw new ArgumentException("Stream events not word aligned.");
            }

            #endregion

            #region Guard

            if(events.Count == 0)
            {
                return;
            }

            #endregion

            BufferLength = events.Count;

            events.CopyTo(data, 0);
        }

        /// <summary>
        /// Releases the resources associated with the built MidiHeader pointer.
        /// </summary>
        public void Destroy()
        {
            #region Require

            if(!built)
            {
                throw new InvalidOperationException("Cannot destroy MidiHeader");
            }

            #endregion

            Destroy(result);
        }

        /// <summary>
        /// Releases the resources associated with the specified MidiHeader pointer.
        /// </summary>
        /// <param name="headerPtr">
        /// The MidiHeader pointer.
        /// </param>
        public void Destroy(IntPtr headerPtr)
        {
            MidiHeader header = (MidiHeader)Marshal.PtrToStructure(headerPtr, typeof(MidiHeader));

            Marshal.FreeHGlobal(header.data);
            Marshal.FreeHGlobal(headerPtr);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The length of the system exclusive buffer.
        /// </summary>
        public int BufferLength
        {
            get
            {
                return bufferLength;
            }
            set
            {
                #region Require

                if(value <= 0)
                {
                    throw new ArgumentOutOfRangeException("BufferLength", value, 
                        "MIDI header buffer length out of range.");
                }

                #endregion

                bufferLength = value;
                data = new byte[value];
            }
        }

        /// <summary>
        /// Gets the pointer to the MidiHeader.
        /// </summary>
        public IntPtr Result
        {
            get
            {
                return result;
            }
        }

        #endregion
	}
}
