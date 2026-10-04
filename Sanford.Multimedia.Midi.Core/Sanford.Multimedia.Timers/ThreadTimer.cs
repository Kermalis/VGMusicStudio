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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Sanford.Multimedia.Timers
{
    /// <summary>
    /// Replacement for the Windows multimedia timer that also runs on Mono
    /// </summary>
    sealed class ThreadTimer : ITimer
    {
        ThreadTimerQueue queue;

        bool isRunning;
        TimerMode mode;
        TimeSpan period;
        TimeSpan resolution;

        static object[] emptyArgs = new object[] { EventArgs.Empty };

        public ThreadTimer()
            : this(ThreadTimerQueue.Instance)
        {
            if (!Stopwatch.IsHighResolution)
            {
                throw new NotImplementedException("Stopwatch is not IsHighResolution");
            }

            isRunning = false;
            mode = TimerMode.Periodic;
            resolution = TimeSpan.FromMilliseconds(1);
            period = resolution;

            tickRaiser = new EventRaiser(OnTick);
        }

        ThreadTimer(ThreadTimerQueue queue)
        {
            this.queue = queue;
        }

        internal void DoTick()
        {
            if (SynchronizingObject != null && SynchronizingObject.InvokeRequired)
            {
                SynchronizingObject.BeginInvoke(tickRaiser, emptyArgs);
            }
            else
            {
                OnTick(EventArgs.Empty);
            }
        }

        // Represents methods that raise events.
        private delegate void EventRaiser(EventArgs e);

        // Represents the method that raises the Tick event.
        private EventRaiser tickRaiser;

        // The ISynchronizeInvoke object to use for marshaling events.
        private ISynchronizeInvoke synchronizingObject = null;

        public bool IsRunning
        {
            get
            {
                return isRunning;
            }
        }

        public TimerMode Mode
        {
            get
            {
                #region Require

                if (disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                #endregion

                return mode;
            }

            set
            {
                #region Require

                if (disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                #endregion

                mode = value;

                if (IsRunning)
                {
                    Stop();
                    Start();
                }
            }
        }

        public int Period
        {
            get
            {
                #region Require

                if (disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                #endregion

                return (int) period.TotalMilliseconds;
            }
            set
            {
                #region Require

                if (disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                #endregion

                var wasRunning = IsRunning;
                
                if (wasRunning)
                {
                    Stop();
                }

                period = TimeSpan.FromMilliseconds(value);

                if (wasRunning)
                {
                    Start();
                }
            }
        }

        public TimeSpan PeriodTimeSpan
        {
            get { return period; }
        } 

        public int Resolution
        {
            get
            {
                return (int) resolution.TotalMilliseconds;
            }

            set
            {
                resolution = TimeSpan.FromMilliseconds(value);
            }
        }

        // For implementing IComponent.
        private ISite site = null;

        public ISite Site
        {
            get
            {
                return site;
            }

            set
            {
                site = value;
            }
        }

        /// <summary>
        /// Gets or sets the object used to marshal event-handler calls.
        /// </summary>
        public ISynchronizeInvoke SynchronizingObject
        {
            get
            {
                #region Require

                if (disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                #endregion

                return synchronizingObject;
            }
            set
            {
                #region Require

                if (disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                #endregion

                synchronizingObject = value;
            }
        }

        public event EventHandler Disposed;
        public event EventHandler Started;
        public event EventHandler Stopped;
        public event EventHandler Tick;

        public void Dispose()
        {
            Stop();
            disposed = true;
            OnDisposed(EventArgs.Empty);
        }

        #region Event Raiser Methods

        // Raises the Disposed event.
        private void OnDisposed(EventArgs e)
        {
            EventHandler handler = Disposed;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        // Raises the Started event.
        private void OnStarted(EventArgs e)
        {
            EventHandler handler = Started;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        // Raises the Stopped event.
        private void OnStopped(EventArgs e)
        {
            EventHandler handler = Stopped;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        // Raises the Tick event.
        private void OnTick(EventArgs e)
        {
            EventHandler handler = Tick;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion        

        bool disposed = false;

        public void Start()
        {
            #region Require

            if (disposed)
            {
                throw new ObjectDisposedException("Timer");
            }

            #endregion

            #region Guard

            if (IsRunning)
            {
                return;
            }

            #endregion

            // If the periodic event callback should be used.
            if (Mode == TimerMode.Periodic)
            {
                queue.Add(this);
                isRunning = true;
            }
            // Else the one shot event callback should be used.
            else
            {
                throw new NotImplementedException();
            }

            if (SynchronizingObject != null && SynchronizingObject.InvokeRequired)
            {
                SynchronizingObject.BeginInvoke(
                    new EventRaiser(OnStarted),
                    new object[] { EventArgs.Empty });
            }
            else
            {
                OnStarted(EventArgs.Empty);
            }
        }

        public void Stop()
        {
            #region Require

            if (disposed)
            {
                throw new ObjectDisposedException("Timer");
            }

            #endregion

            #region Guard

            if (!IsRunning)
            {
                return;
            }

            #endregion

            queue.Remove(this);
            isRunning = false;

            if (SynchronizingObject != null && SynchronizingObject.InvokeRequired)
            {
                SynchronizingObject.BeginInvoke(
                    new EventRaiser(OnStopped),
                    new object[] { EventArgs.Empty });
            }
            else
            {
                OnStopped(EventArgs.Empty);
            }
        }

    }
}
