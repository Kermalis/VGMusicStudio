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

namespace Sanford.Multimedia.Midi
{
	/// <summary>
	/// Provides functionality for building ChannelMessages.
	/// </summary>
    public class ChannelMessageBuilder : IMessageBuilder
    {
        #region ChannelMessageBuilder Members

        #region Class Fields

        // Stores the ChannelMessages.
        private static Hashtable messageCache = Hashtable.Synchronized(new Hashtable());

        #endregion

        #region Fields

        // The channel message as a packed integer.
        private int message = 0;

        // The built ChannelMessage
        private ChannelMessage result = null;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the ChannelMessageBuilder class.
        /// </summary>
        public ChannelMessageBuilder()
        {
            Command = ChannelCommand.Controller;
            MidiChannel = 0;
            Data1 = (int)ControllerType.AllSoundOff;
            Data2 = 0;
        }

        /// <summary>
        /// Initializes a new instance of the ChannelMessageBuilder class with
        /// the specified ChannelMessageEventArgs.
        /// </summary>
        /// <param name="message">
        /// The ChannelMessageEventArgs to use for initializing the ChannelMessageBuilder.
        /// </param>
        /// <remarks>
        /// The ChannelMessageBuilder uses the specified ChannelMessageEventArgs to 
        /// initialize its property values.
        /// </remarks>
        public ChannelMessageBuilder(ChannelMessage message)
        {
            Initialize(message);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the ChannelMessageBuilder with the specified 
        /// ChannelMessageEventArgs.
        /// </summary>
        /// <param name="message">
        /// The ChannelMessageEventArgs to use for initializing the ChannelMessageBuilder.
        /// </param>
        public void Initialize(ChannelMessage message)
        {
            this.message = message.Message;
        }

        /// <summary>
        /// Clears the ChannelMessageEventArgs cache.
        /// </summary>
        public static void Clear()
        {
            messageCache.Clear();
        }        

        #endregion

        #region Properties

        /// <summary>
        /// Gets the number of messages in the ChannelMessageEventArgs cache.
        /// </summary>
        public static int Count
        {
            get
            {
                return messageCache.Count;
            }
        }

        /// <summary>
        /// Gets the built ChannelMessageEventArgs.
        /// </summary>
        public ChannelMessage Result
        {
            get
            {
                return result;
            }
        }

        /// <summary>
        /// Gets or sets the ChannelMessageEventArgs as a packed integer. 
        /// </summary>
        internal int Message
        {
            get
            {
                return message;
            }
            set
            {
                message = value;
            }
        }

        /// <summary>
        /// Gets or sets the Command value to use for building the 
        /// ChannelMessageEventArgs.
        /// </summary>
        public ChannelCommand Command
        {
            get
            {
                return ChannelMessage.UnpackCommand(message);
            }
            set
            {
                message = ChannelMessage.PackCommand(message, value);
            }
        }

        /// <summary>
        /// Gets or sets the MIDI channel to use for building the 
        /// ChannelMessageEventArgs.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// MidiChannel is set to a value less than zero or greater than 15.
        /// </exception>
        public int MidiChannel
        {
            get
            {
                return ChannelMessage.UnpackMidiChannel(message);
            }
            set
            {
                message = ChannelMessage.PackMidiChannel(message, value);
            }
        }

        /// <summary>
        /// Gets or sets the first data value to use for building the 
        /// ChannelMessageEventArgs.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Data1 is set to a value less than zero or greater than 127.
        /// </exception>
        public int Data1
        {
            get
            {
                return ShortMessage.UnpackData1(message);
            }
            set
            {
                message = ShortMessage.PackData1(message, value);
            }
        }

        /// <summary>
        /// Gets or sets the second data value to use for building the 
        /// ChannelMessageEventArgs.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Data2 is set to a value less than zero or greater than 127.
        /// </exception>
        public int Data2
        {
            get
            {
                return ShortMessage.UnpackData2(message);
            }
            set
            {
                message = ShortMessage.PackData2(message, value);
            }
        }

        #endregion

        #endregion

        #region IMessageBuilder Members

        /// <summary>
        /// Builds a ChannelMessageEventArgs.
        /// </summary>
        public void Build()
        {
            result = (ChannelMessage)messageCache[message];

            // If the message does not exist.
            if(result == null)
            {
                result = new ChannelMessage(message);

                // Add message to cache.
                messageCache.Add(message, result);
            }
        }

        #endregion
    }
}
