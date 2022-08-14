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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using Sanford.Collections;

namespace Sanford.Threading
{
	/// <summary>
	/// Provides functionality for timestamped delegate invocation.
	/// </summary>
    public partial class DelegateScheduler : IDisposable, IComponent
    {
        #region DelegateScheduler Members

        #region Fields

        /// <summary>
        /// A constant value representing an unlimited number of delegate invocations.
        /// </summary>
        public const int Infinite = -1;

        // Default polling interval.
        private const int DefaultPollingInterval = 10;

        // For queuing the delegates in priority order.
        private PriorityQueue queue = new PriorityQueue();

        // Used for timing events for polling the delegate queue.
        private System.Timers.Timer timer = new System.Timers.Timer(DefaultPollingInterval);

        // For storing tasks when the scheduler isn't running.
        private List<Task> tasks = new List<Task>();

        // A value indicating whether the DelegateScheduler is running.
        private bool running = false;

        // A value indicating whether the DelegateScheduler has been disposed.
        private bool disposed = false;

        private ISite site = null;

        #endregion

        #region Events

        /// <summary>
        /// Raised when a delegate is invoked.
        /// </summary>
        public event EventHandler<InvokeCompletedEventArgs> InvokeCompleted;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the DelegateScheduler class.
        /// </summary>
        public DelegateScheduler()
        {
            Initialize();            
        }

        /// <summary>
        /// Initializes a new instance of the DelegateScheduler class with the
        /// specified IContainer.
        /// </summary>
        public DelegateScheduler(IContainer container)
        {
            ///
            /// Required for Windows.Forms Class Composition Designer support
            ///
            container.Add(this);

            Initialize();
        }

        // Initializes the DelegateScheduler.
        private void Initialize()
        {
            timer.Elapsed += new ElapsedEventHandler(HandleElapsed);
        }

        ~DelegateScheduler()
        {
            Dispose(false);
        }

        #endregion

        #region Methods

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                Stop();

                timer.Dispose();

                Clear();

                disposed = true;

                OnDisposed(EventArgs.Empty);

                GC.SuppressFinalize(this);            
            }
        }
        
        /// <summary>
        /// Adds a delegate to the DelegateScheduler.
        /// </summary>
        /// <param name="count">
        /// The number of times the delegate should be invoked.
        /// </param>
        /// <param name="millisecondsTimeout">
        /// The time in milliseconds between delegate invocation.
        /// </param>
        /// <param name="method">
        /// </param>
        /// The delegate to invoke.
        /// <param name="args">
        /// The arguments to pass to the delegate when it is invoked.
        /// </param>
        /// <returns>
        /// A Task object representing the scheduled task.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// If the DelegateScheduler has already been disposed.
        /// </exception>
        /// <remarks>
        /// If an unlimited count is desired, pass the DelegateScheduler.Infinity 
        /// constant as the count argument.
        /// </remarks>
        public Task Add(
            int count,
            int millisecondsTimeout,
            Delegate method,            
            params object[] args)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("DelegateScheduler");
            }

            #endregion    

            Task t = new Task(count, millisecondsTimeout, method, args);

            lock(queue.SyncRoot)
            {
                // Only add the task to the DelegateScheduler if the count 
                // is greater than zero or set to Infinite.
                if(count > 0 || count == DelegateScheduler.Infinite)
                {
                    if(IsRunning)
                    {
                        queue.Enqueue(t);
                    }
                    else
                    {
                        tasks.Add(t);
                    }
                }
            }

            return t;
        }

        /// <summary>
        /// Removes the specified Task.
        /// </summary>
        /// <param name="task">
        /// The Task to be removed.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// If the DelegateScheduler has already been disposed.
        /// </exception>
        public void Remove(Task task)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("DelegateScheduler");
            }

            #endregion    

            #region Guard

            if(task == null)
            {
                return;
            }

            #endregion

            lock(queue.SyncRoot)
            {
                if(IsRunning)
                {
                    queue.Remove(task);
                }
                else
                {
                    tasks.Remove(task);                    
                }
            }
        }

        /// <summary>
        /// Starts the DelegateScheduler.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// If the DelegateScheduler has already been disposed.
        /// </exception>
        public void Start()
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            #region Guard

            if(IsRunning)
            {
                return;
            }

            #endregion

            lock(queue.SyncRoot)
            {
                Task t;

                while(tasks.Count > 0)
                {
                    t = tasks[tasks.Count - 1];

                    tasks.RemoveAt(tasks.Count - 1);

                    t.ResetNextTimeout();

                    queue.Enqueue(t);
                }

                running = true;

                timer.Start();
            }
        }

        /// <summary>
        /// Stops the DelegateScheduler.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// If the DelegateScheduler has already been disposed.
        /// </exception>
        public void Stop()
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            #region Guard

            if(!IsRunning)
            {
                return;
            }

            #endregion

            lock(queue.SyncRoot)
            {
                // While there are still tasks left in the queue.
                while(queue.Count > 0)
                {
                    // Remove task from queue and add it to the Task list
                    // to be used again next time the DelegateScheduler is run.
                    tasks.Add((Task)queue.Dequeue());
                }

                timer.Stop();

                running = false;
            }
        }

        /// <summary>
        /// Clears the DelegateScheduler of all tasks.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// If the DelegateScheduler has already been disposed.
        /// </exception>
        public void Clear()
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            lock(queue.SyncRoot)
            {
                queue.Clear();
                tasks.Clear();
            }
        }

        // Responds to the timer's Elapsed event by running any tasks that are due.
        private void HandleElapsed(object sender, ElapsedEventArgs e)
        {
            Debug.WriteLine("Signal time: " + e.SignalTime.ToString());

            lock(queue.SyncRoot)
            {
                #region Guard

                if(queue.Count == 0)
                {
                    return;
                }

                #endregion

                // Take a look at the first task in the queue to see if it's
                // time to run it.
                Task tk = (Task)queue.Peek();

                // The return value from the delegate that will be invoked.
                object returnValue;

                // While there are still tasks in the queue and it is time 
                // to run one or more of them.
                while(queue.Count > 0 && tk.NextTimeout <= e.SignalTime)
                {
                    // Remove task from queue.
                    queue.Dequeue();

                    // While it's time for the task to run.
                    while((tk.Count == Infinite || tk.Count > 0) && tk.NextTimeout <= e.SignalTime)
                    {
                        try
                        {
                            Debug.WriteLine("Invoking delegate.");
                            Debug.WriteLine("Next timeout: " + tk.NextTimeout.ToString());

                            // Invoke delegate.
                            returnValue = tk.Invoke(e.SignalTime);

                            OnInvokeCompleted(
                                new InvokeCompletedEventArgs(
                                tk.Method,
                                tk.GetArgs(),
                                returnValue,
                                null));
                        }
                        catch(Exception ex)
                        {
                            OnInvokeCompleted(
                                new InvokeCompletedEventArgs(
                                tk.Method,
                                tk.GetArgs(),
                                null,
                                ex));
                        }
                    }

                    // If this task should run again.
                    if(tk.Count == Infinite || tk.Count > 0)
                    {
                        // Enqueue task back into priority queue.
                        queue.Enqueue(tk);
                    }

                    // If there are still tasks in the queue.
                    if(queue.Count > 0)
                    {
                        // Take a look at the next task to see if it is
                        // time to run.
                        tk = (Task)queue.Peek();
                    }
                }
            }
        }

        // Raises the Disposed event.
        protected virtual void OnDisposed(EventArgs e)
        {
            EventHandler handler = Disposed;

            if(handler != null)
            {
                handler(this, e);
            }
        }

        // Raises the InvokeCompleted event.
        protected virtual void OnInvokeCompleted(InvokeCompletedEventArgs e)
        {
            EventHandler<InvokeCompletedEventArgs> handler = InvokeCompleted;

            if(handler != null)
            {
                handler(this, e);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the interval in milliseconds in which the 
        /// DelegateScheduler polls its queue of delegates in order to 
        /// determine when they should run.
        /// </summary>
        public double PollingInterval
        {
            get
            {
                #region Require

                if(disposed)
                {
                    throw new ObjectDisposedException("PriorityQueue");
                }

                #endregion

                return timer.Interval;
            }
            set
            {
                #region Require

                if(disposed)
                {
                    throw new ObjectDisposedException("PriorityQueue");
                }

                #endregion

                timer.Interval = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the DelegateScheduler is running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return running;
            }
        }

        /// <summary>
        /// Gets or sets the object used to marshal event-handler calls and delegate invocations.
        /// </summary>
        public ISynchronizeInvoke SynchronizingObject
        {
            get
            {
                return timer.SynchronizingObject;
            }
            set
            {
                timer.SynchronizingObject = value;
            }
        }

        #endregion

        #endregion

        #region IComponent Members

        public event System.EventHandler Disposed;

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

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            #region Guard

            if(disposed)
            {
                return;
            }

            #endregion

            Dispose(true);
        }

        #endregion        
    }
}
