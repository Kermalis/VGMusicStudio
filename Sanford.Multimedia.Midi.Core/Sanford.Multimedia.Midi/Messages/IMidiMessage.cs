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
    /// Defines constants representing MIDI message types.
    /// </summary>
    public enum MessageType
    {
        Channel,

        SystemExclusive,

        SystemCommon,

        SystemRealtime,

        Meta,

        Short
    }

    /// <summary>
    /// Represents the basic functionality for all MIDI messages.
    /// </summary>
    public interface IMidiMessage
    {
        /// <summary>
        /// Gets a byte array representation of the MIDI message.
        /// </summary>
        /// <returns>
        /// A byte array representation of the MIDI message.
        /// </returns>
        byte[] GetBytes();

        /// <summary>
        /// Gets the MIDI message's status value.
        /// </summary>
        int Status
        {
            get;
        }

        /// <summary>
        /// Gets the MIDI event's type.
        /// </summary>
        MessageType MessageType
        {
            get;
        }

        /// <summary>
        /// Delta samples when the event should be processed in the next audio buffer.
        /// Leave at 0 for realtime input to play as fast as possible.
        /// Set to the desired sample in the next buffer if you play a midi sequence synchronized to the audio callback
        /// </summary>
        int DeltaFrames
        {
            get;
        }
    }
}
