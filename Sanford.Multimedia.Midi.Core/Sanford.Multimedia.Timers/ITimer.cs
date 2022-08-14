#region License

/* Copyright (c) 2015 Andreas Grimme
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

using System;
using System.ComponentModel;

namespace Sanford.Multimedia.Timers
{
    public interface ITimer : IComponent
    {
        /// <summary>
        /// Gets a value indicating whether the Timer is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the timer mode.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// If the timer has already been disposed.
        /// </exception>
        TimerMode Mode { get; set; }

        /// <summary>
        /// Period between timer events in milliseconds.
        /// </summary>
        int Period { get; set; }

        /// <summary>
        /// Resolution of the timer in milliseconds.
        /// </summary>
        int Resolution { get; set; }

        /// <summary>
        /// Gets or sets the object used to marshal event-handler calls.
        /// </summary>
        ISynchronizeInvoke SynchronizingObject { get; set; }

        /// <summary>
        /// Occurs when the Timer has started;
        /// </summary>
        event EventHandler Started;

        /// <summary>
        /// Occurs when the Timer has stopped;
        /// </summary>
        event EventHandler Stopped;

        /// <summary>
        /// Occurs when the time period has elapsed.
        /// </summary>
        event EventHandler Tick;

        /// <summary>
        /// Starts the timer.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// The timer has already been disposed.
        /// </exception>
        /// <exception cref="TimerStartException">
        /// The timer failed to start.
        /// </exception>
        void Start();

        /// <summary>
        /// Stops timer.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// If the timer has already been disposed.
        /// </exception>
        void Stop();
    }
}