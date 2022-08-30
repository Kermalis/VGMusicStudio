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

namespace Sanford.Multimedia.Midi
{
	/// <summary>
	/// Provides functionality for building song position pointer messages.
	/// </summary>
	public class SongPositionPointerBuilder : IMessageBuilder
	{
        #region SongPositionPointerBuilder Members

        #region Constants

        // The number of ticks per 16th note.
        private const int TicksPer16thNote = 6;

        // Used for packing and unpacking the song position.
        private const int Shift = 7;

        // Used for packing and unpacking the song position.
        private const int Mask = 127;

        #endregion

        #region Fields

        // The scale used for converting from the song position to the position
        // in ticks.
        private int tickScale;

        // Pulses per quarter note resolution.
        private int ppqn;

        // Used for building the SysCommonMessage to represent the song
        // position pointer.
        private SysCommonMessageBuilder builder;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the SongPositionPointerBuilder class.
        /// </summary>
        public SongPositionPointerBuilder()
        {
            builder = new SysCommonMessageBuilder();
            builder.Type = SysCommonType.SongPositionPointer;

            Ppqn = PpqnClock.PpqnMinValue;
        }

        /// <summary>
        /// Initializes a new instance of the SongPositionPointerBuilder class
        /// with the specified song position pointer message.
        /// </summary>
        /// <param name="message">
        /// The song position pointer message to use for initializing the 
        /// SongPositionPointerBuilder.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If message is not a song position pointer message.
        /// </exception>
        public SongPositionPointerBuilder(SysCommonMessage message)
        {
            builder = new SysCommonMessageBuilder();
            builder.Type = SysCommonType.SongPositionPointer;

            Initialize(message);

            Ppqn = PpqnClock.PpqnMinValue;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the SongPositionPointerBuilder with the specified 
        /// SysCommonMessage.
        /// </summary>
        /// <param name="message">
        /// The SysCommonMessage to use to initialize the 
        /// SongPositionPointerBuilder.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If the SysCommonMessage is not a song position pointer message.
        /// </exception>
        public void Initialize(SysCommonMessage message)
        {
            #region Require

            if(message == null)
            {
                throw new ArgumentNullException("message");
            }
            else if(message.SysCommonType != SysCommonType.SongPositionPointer)
            {
                throw new ArgumentException(
                    "Message is not a song position pointer message.");
            }

            #endregion

            builder.Initialize(message);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the sequence position in ticks.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Value is set to less than zero.
        /// </exception>
        /// <remarks>
        /// Note: the position in ticks value is converted to the song position
        /// pointer value. Since the song position pointer has a lower 
        /// resolution than the position in ticks, there is a probable loss of 
        /// resolution when setting the position in ticks value.
        /// </remarks>
        public int PositionInTicks
        {
            get
            {
                return SongPosition * tickScale * TicksPer16thNote;
            }
            set
            {
                #region Require

                if(value < 0)
                {
                    throw new ArgumentOutOfRangeException("PositionInTicks", value,
                        "Position in ticks out of range.");
                }

                #endregion

                SongPosition = value / (tickScale * TicksPer16thNote);
            }
        }        

        /// <summary>
        /// Gets or sets the PulsesPerQuarterNote object.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Value is not a multiple of 24.
        /// </exception>
        public int Ppqn
        {
            get
            {
                return ppqn;
            }
            set
            {
                #region Require

                if(value < PpqnClock.PpqnMinValue)
                {
                    throw new ArgumentOutOfRangeException("Ppqn", value,
                         "Pulses per quarter note is smaller than 24.");
                }

                #endregion

                ppqn = value;

                tickScale = ppqn / PpqnClock.PpqnMinValue;
            }
        }

        /// <summary>
        /// Gets or sets the song position.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Value is set to less than zero.
        /// </exception>
        public int SongPosition
        {
            get
            {
                return (builder.Data2 << Shift) | builder.Data1;                
            }
            set
            {
                #region Require

                if(value < 0)
                {
                    throw new ArgumentOutOfRangeException("SongPosition", value,
                        "Song position pointer out of range.");
                }

                #endregion

                builder.Data1 = value & Mask;
                builder.Data2 = value >> Shift;
            }
        }

        /// <summary>
        /// Gets the built song position pointer message.
        /// </summary>
        public SysCommonMessage Result
        {
            get
            {
                return builder.Result;
            }
        }

        #endregion

        #endregion

        #region IMessageBuilder Members

        /// <summary>
        /// Builds a song position pointer message.
        /// </summary>
        public void Build()
        {
            builder.Build();
        }

        #endregion
    }
}
