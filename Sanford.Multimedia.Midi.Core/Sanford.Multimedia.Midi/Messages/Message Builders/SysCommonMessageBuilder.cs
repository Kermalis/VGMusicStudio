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
	/// Provides functionality for building SysCommonMessages.
	/// </summary>
	public class SysCommonMessageBuilder : IMessageBuilder
    {
        #region SysCommonMessageBuilder Members

        #region Class Fields

        // Stores the SystemCommonMessages.
        private static Hashtable messageCache = Hashtable.Synchronized(new Hashtable());

        #endregion

        #region Fields

        // The SystemCommonMessage as a packed integer.
        private int message = 0;

        // The built SystemCommonMessage.
        private SysCommonMessage result = null;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the SysCommonMessageBuilder class.
        /// </summary>
        public SysCommonMessageBuilder()
        {
            Type = SysCommonType.TuneRequest;
        }

        /// <summary>
        /// Initializes a new instance of the SysCommonMessageBuilder class 
        /// with the specified SystemCommonMessage.
        /// </summary>
        /// <param name="message">
        /// The SysCommonMessage to use for initializing the 
        /// SysCommonMessageBuilder.
        /// </param>
        /// <remarks>
        /// The SysCommonMessageBuilder uses the specified SysCommonMessage to 
        /// initialize its property values.
        /// </remarks>
        public SysCommonMessageBuilder(SysCommonMessage message)
        {
            Initialize(message);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the SysCommonMessageBuilder with the specified 
        /// SysCommonMessage.
        /// </summary>
        /// <param name="message">
        /// The SysCommonMessage to use for initializing the 
        /// SysCommonMessageBuilder.
        /// </param>
        public void Initialize(SysCommonMessage message)
        {
            this.message = message.Message;
        }

        /// <summary>
        /// Clears the SysCommonMessageBuilder cache.
        /// </summary>
        public static void Clear()
        {
            messageCache.Clear();
        }        

        #endregion

        #region Properties

        /// <summary>
        /// Gets the number of messages in the SysCommonMessageBuilder cache.
        /// </summary>
        public static int Count
        {
            get
            {
                return messageCache.Count;
            }
        }

        /// <summary>
        /// Gets the built SysCommonMessage.
        /// </summary>
        public SysCommonMessage Result
        {
            get
            {
                return result;
            }
        }

        /// <summary>
        /// Gets or sets the SysCommonMessage as a packed integer.
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
        /// Gets or sets the type of SysCommonMessage.
        /// </summary>
        public SysCommonType Type
        {
            get
            {
                return (SysCommonType)ShortMessage.UnpackStatus(message);
            }
            set
            {
                message = ShortMessage.PackStatus(message, (int)value);
            }
        }

        /// <summary>
        /// Gets or sets the first data value to use for building the 
        /// SysCommonMessage.
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
        /// SysCommonMessage.
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
        /// Builds a SysCommonMessage.
        /// </summary>
        public void Build()
        {
            result = (SysCommonMessage)messageCache[message];

            if(result == null)
            {
                result = new SysCommonMessage(message);

                messageCache.Add(message, result);
            }
        }

        #endregion
    }
}
