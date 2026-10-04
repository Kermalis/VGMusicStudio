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
using System.ComponentModel;
using System.Diagnostics;

namespace Sanford.Multimedia.Midi
{
    #region System Realtime Message Types
    
    /// <summary>
    /// Defines constants representing the various system realtime message types.
    /// </summary>
    public enum SysRealtimeType
    {
        /// <summary>
        /// Represents the clock system realtime type.
        /// </summary>
        Clock = 0xF8,

        /// <summary>
        /// Represents the tick system realtime type.
        /// </summary>
        Tick,

        /// <summary>
        /// Represents the start system realtime type.
        /// </summary>
        Start,

        /// <summary>
        /// Represents the continue system realtime type.
        /// </summary>
        Continue,

        /// <summary>
        /// Represents the stop system realtime type.
        /// </summary>
        Stop,    
    
        /// <summary>
        /// Represents the active sense system realtime type.
        /// </summary>
        ActiveSense = 0xFE,

        /// <summary>
        /// Represents the reset system realtime type.
        /// </summary>
        Reset
    }

    #endregion

	/// <summary>
	/// Represents MIDI system realtime messages.
	/// </summary>
	/// <remarks>
	/// System realtime messages are MIDI messages that are primarily concerned 
	/// with controlling and synchronizing MIDI devices. 
	/// </remarks>
	[ImmutableObject(true)]
	public sealed class SysRealtimeMessage : ShortMessage
	{
        #region SysRealtimeMessage Members

        #region System Realtime Messages

        /// <summary>
        /// The instance of the system realtime start message.
        /// </summary>
        public static readonly SysRealtimeMessage StartMessage = 
            new SysRealtimeMessage(SysRealtimeType.Start);

        /// <summary>
        /// The instance of the system realtime continue message.
        /// </summary>
        public static readonly SysRealtimeMessage ContinueMessage = 
            new SysRealtimeMessage(SysRealtimeType.Continue);

        /// <summary>
        /// The instance of the system realtime stop message.
        /// </summary>
        public static readonly SysRealtimeMessage StopMessage = 
            new SysRealtimeMessage(SysRealtimeType.Stop);

        /// <summary>
        /// The instance of the system realtime clock message.
        /// </summary>
        public static readonly SysRealtimeMessage ClockMessage = 
            new SysRealtimeMessage(SysRealtimeType.Clock);

        /// <summary>
        /// The instance of the system realtime tick message.
        /// </summary>
        public static readonly SysRealtimeMessage TickMessage = 
            new SysRealtimeMessage(SysRealtimeType.Tick);

        /// <summary>
        /// The instance of the system realtime active sense message.
        /// </summary>
        public static readonly SysRealtimeMessage ActiveSenseMessage = 
            new SysRealtimeMessage(SysRealtimeType.ActiveSense);

        /// <summary>
        /// The instance of the system realtime reset message.
        /// </summary>
        public static readonly SysRealtimeMessage ResetMessage = 
            new SysRealtimeMessage(SysRealtimeType.Reset);

        #endregion

        // Make construction private so that a system realtime message cannot 
        // be constructed directly.
        private SysRealtimeMessage(SysRealtimeType type)
        {
            msg = (int)type;

            #region Ensure

            Debug.Assert(SysRealtimeType == type);

            #endregion
        }

        #region Methods

        /// <summary>
        /// Returns a value for the current SysRealtimeMessage suitable for use in 
        /// hashing algorithms.
        /// </summary>
        /// <returns>
        /// A hash code for the current SysRealtimeMessage.
        /// </returns>
        public override int GetHashCode()
        {
            return msg;
        }

        /// <summary>
        /// Determines whether two SysRealtimeMessage instances are equal.
        /// </summary>
        /// <param name="obj">
        /// The SysRealtimeMessage to compare with the current SysRealtimeMessage.
        /// </param>
        /// <returns>
        /// <b>true</b> if the specified SysRealtimeMessage is equal to the current 
        /// SysRealtimeMessage; otherwise, <b>false</b>.
        /// </returns>
        public override bool Equals(object obj)
        {
            #region Guard

            if(!(obj is SysRealtimeMessage))
            {
                return false;
            }

            #endregion

            SysRealtimeMessage message = (SysRealtimeMessage)obj;

            return this.msg == message.msg;
        }

        #endregion

        #region Properties
        
        /// <summary>
        /// Gets the SysRealtimeType.
        /// </summary>
        public SysRealtimeType SysRealtimeType
        {
            get
            {
                return (SysRealtimeType)msg;
            }
        }
   
        /// <summary>
        /// Gets the MessageType.
        /// </summary>
        public override MessageType MessageType
        {
            get
            {
                return MessageType.SystemRealtime;
            }
        }

        #endregion

        #endregion
    }
}
