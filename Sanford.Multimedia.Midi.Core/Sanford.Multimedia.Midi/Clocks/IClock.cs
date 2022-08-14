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
	/// Represents functionality for generating events for driving Sequence playback.
	/// </summary>
	public interface IClock
    {
        #region IClock Members

        /// <summary>
        /// Occurs when an IClock generates a tick.
        /// </summary>
        event EventHandler Tick;

        /// <summary>
        /// Occurs when an IClock starts generating Ticks.
        /// </summary>
        /// <remarks>
        /// When an IClock is started, it resets itself and generates ticks to
        /// drive playback from the beginning of the Sequence.
        /// </remarks>
        event EventHandler Started;

        /// <summary>
        /// Occurs when an IClock continues generating Ticks.
        /// </summary>
        /// <remarks>
        /// When an IClock is continued, it generates ticks to drive playback 
        /// from the current position within the Sequence.
        /// </remarks>
        event EventHandler Continued;

        /// <summary>
        /// Occurs when an IClock is stopped.
        /// </summary>
        event EventHandler Stopped;

        /// <summary>
        /// Gets a value indicating whether the IClock is running.
        /// </summary>
        bool IsRunning
        {
            get;
        }

        int Ticks
        {
            get;
        }

        #endregion
    }
}
