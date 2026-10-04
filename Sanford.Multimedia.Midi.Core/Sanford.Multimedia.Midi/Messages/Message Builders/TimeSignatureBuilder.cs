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
    /// Provides easy to use functionality for time signature MetaMessages.
    /// </summary>
	public class TimeSignatureBuilder : IMessageBuilder
	{
        #region TimeSignature Members

        #region Constants

        // Default numerator value.
        private const byte DefaultNumerator = 4;

        // Default denominator value.
        private const byte DefaultDenominator = 2;

        // Default clocks per metronome click value.
        private const byte DefaultClocksPerMetronomeClick = 24;

        // Default thirty second notes per quarter note value.
        private const byte DefaultThirtySecondNotesPerQuarterNote = 8;

        #endregion

        #region Fields

        // The raw data making up the time signature meta message.
        private byte[] data = new byte[MetaMessage.TimeSigLength];

        // The built time signature meta message.
        private MetaMessage result = null;

        // Indicates whether any of the properties have changed since the
        // last time the message was built.
        private bool changed = true;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the TimeSignatureBuilder class.
        /// </summary>
        public TimeSignatureBuilder()
        {
            Numerator = DefaultNumerator;
            Denominator = DefaultDenominator;
            ClocksPerMetronomeClick = DefaultClocksPerMetronomeClick;
            ThirtySecondNotesPerQuarterNote = DefaultThirtySecondNotesPerQuarterNote;
        }

        /// <summary>
        /// Initializes a new instance of the TimeSignatureBuilder class with the 
        /// specified MetaMessage.
        /// </summary>
        /// <param name="message">
        /// The MetaMessage to use for initializing the TimeSignatureBuilder class.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If the specified MetaMessage is not a time signature type.
        /// </exception>
        /// <remarks>
        /// The TimeSignatureBuilder uses the specified MetaMessage to 
        /// initialize its property values.
        /// </remarks>
		public TimeSignatureBuilder(MetaMessage message)
        {
            Initialize(message);            
		}

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the TimeSignatureBuilder with the specified MetaMessage.
        /// </summary>
        /// <param name="message">
        /// The MetaMessage to use for initializing the TimeSignatureBuilder. 
        /// </param>
        /// <exception cref="ArgumentException">
        /// If the specified MetaMessage is not a time signature type.
        /// </exception>
        public void Initialize(MetaMessage message)
        {
            #region Require

            if(message.MetaType != MetaType.TimeSignature)
            {
                throw new ArgumentException("Wrong meta event type.", "message");
            }

            #endregion

            data = message.GetBytes();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the numerator.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Numerator is set to a value less than one.
        /// </exception>
        public byte Numerator
        {
            get
            {
                return data[0];
            }
            set
            {
                #region Require

                if(value < 1)
                {
                    throw new ArgumentOutOfRangeException("Numerator", value,
                        "Numerator out of range.");
                }

                #endregion

                data[0] = value;

                changed = true;
            }
        }

        /// <summary>
        /// Gets or sets the denominator.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Denominator is set to a value less than 2. 
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Denominator is set to a value that is not a power of 2.
        /// </exception>
        public byte Denominator
        {
            get
            {
                return Convert.ToByte(Math.Pow(2, data[1]));
            }
            set
            {
                #region Require

                if(value < 2 || value > 32)
                {
                    throw new ArgumentOutOfRangeException("Denominator must be between 2 and 32.");
                }
                else if((value & (value - 1)) != 0)
                {
                    throw new ArgumentException("Denominator must be a power of 2.");
                }

                #endregion

                data[1] = Convert.ToByte(Math.Log(value, 2));

                changed = true;
            }
        }

        /// <summary>
        /// Gets or sets the clocks per metronome click.
        /// </summary>
        /// <remarks>
        /// Clocks per metronome click determines how many MIDI clocks occur
        /// for each metronome click.
        /// </remarks>
        public byte ClocksPerMetronomeClick
        {
            get
            {
                return data[2];
            }
            set
            {
                data[2] = value;

                changed = true;
            }
        }

        /// <summary>
        /// Gets or sets how many thirty second notes there are for each
        /// quarter note.
        /// </summary>
        public byte ThirtySecondNotesPerQuarterNote
        {
            get
            {
                return data[3];
            }
            set
            {
                data[3] = value;

                changed = true;
            }
        }

        /// <summary>
        /// Gets the built message.
        /// </summary>
        public MetaMessage Result
        {
            get
            {
                return result;
            }
        }

        #endregion

        #endregion

        #region IMessageBuilder Members

        /// <summary>
        /// Builds the time signature MetaMessage.
        /// </summary>
        public void Build()
        {
            // If any of the properties have changed since the last time the
            // message was built.
            if(changed)
            {
                result = new MetaMessage(MetaType.TimeSignature, data);
                changed = false;
            }
        }

        #endregion
    }
}
