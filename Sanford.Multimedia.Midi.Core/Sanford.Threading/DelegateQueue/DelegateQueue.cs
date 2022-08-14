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
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Sanford.Collections.Generic;

namespace Sanford.Threading
{
    /// <summary>
    /// Represents an asynchronous queue of delegates.
    /// </summary>
    public partial class DelegateQueue : SynchronizationContext, IComponent, ISynchronizeInvoke
    {
        #region DelegateQueue Members

        #region Fields

        // The thread for processing delegates.
        private Thread delegateThread;

        // The deque for holding delegates.
        private Deque<DelegateQueueAsyncResult> delegateDeque = new Deque<DelegateQueueAsyncResult>();

        // The object to use for locking.
        private readonly object lockObject = new object();

        // The synchronization context in which this DelegateQueue was created.
        private SynchronizationContext context;

        // Inidicates whether the delegate queue has been disposed.
        private volatile bool disposed = false;

        // Thread ID counter for all DelegateQueues.
        private volatile static uint threadID = 0;

        private ISite site = null;

        #endregion

        #region Events

        /// <summary>
        /// Occurs after a method has been invoked as a result of a call to 
        /// the BeginInvoke or BeginInvokePriority methods.
        /// </summary>
        public event EventHandler<InvokeCompletedEventArgs> InvokeCompleted;

        /// <summary>
        /// Occurs after a method has been invoked as a result of a call to
        /// the Post and PostPriority methods.
        /// </summary>
        public event EventHandler<PostCompletedEventArgs> PostCompleted;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the DelegateQueue class.
        /// </summary>
        public DelegateQueue()
        {
            InitializeDelegateQueue();

            if(SynchronizationContext.Current == null)
            {
                context = new SynchronizationContext();
            }
            else
            {
                context = SynchronizationContext.Current;
            }
        }

        /// <summary>
        /// Initializes a new instance of the DelegateQueue class with the specified IContainer object.
        /// </summary>
        /// <param name="container">
        /// The IContainer to which the DelegateQueue will add itself.
        /// </param>
        public DelegateQueue(IContainer container)
        {
            ///
            /// Required for Windows.Forms Class Composition Designer support
            ///
            container.Add(this);

            InitializeDelegateQueue();
        }

        ~DelegateQueue()
        {
            Dispose(false);
        }

        // Initializes the DelegateQueue.
        private void InitializeDelegateQueue()
        {
            // Create thread for processing delegates.
            delegateThread = new Thread(DelegateProcedure);

            lock(lockObject)
            {
                // Increment to next thread ID.
                threadID++;

                // Create name for thread.
                delegateThread.Name = "Delegate Queue Thread: " + threadID.ToString();

                // Start thread.
                delegateThread.Start();

                Debug.WriteLine(delegateThread.Name + " Started.");

                // Wait for signal from thread that it is running.
                Monitor.Wait(lockObject);
            }
        }

        #endregion

        #region Methods

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                lock(lockObject)
                {
                    disposed = true;

                    Monitor.Pulse(lockObject);

                    GC.SuppressFinalize(this);
                }
            }
        }

        /// <summary>
        /// Executes the delegate on the main thread that this object executes on.
        /// </summary>
        /// <param name="method">
        /// A Delegate to a method that takes parameters of the same number and 
        /// type that are contained in args. 
        /// </param>
        /// <param name="args">
        /// An array of type Object to pass as arguments to the given method. 
        /// </param>
        /// <returns>
        /// An IAsyncResult interface that represents the asynchronous operation 
        /// started by calling this method.
        /// </returns>
        /// <remarks>
        /// The delegate is placed at the beginning of the queue. Its invocation
        /// takes priority over delegates already in the queue. 
        /// </remarks>
        public IAsyncResult BeginInvokePriority(Delegate method, params object[] args)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("DelegateQueue");
            }
            else if(method == null)
            {
                throw new ArgumentNullException();
            }

            #endregion

            DelegateQueueAsyncResult result;

            // If BeginInvokePriority was called from a different thread than the one
            // in which the DelegateQueue is running.
            if(InvokeRequired)
            {
                result = new DelegateQueueAsyncResult(this, method, args, false, NotificationType.BeginInvokeCompleted);

                lock(lockObject)
                {
                    // Put the method at the front of the queue.
                    delegateDeque.PushFront(result);

                    Monitor.Pulse(lockObject);
                }
            }
            // Else BeginInvokePriority was called from the same thread in which the 
            // DelegateQueue is running.
            else
            {
                result = new DelegateQueueAsyncResult(this, method, args, true, NotificationType.None);

                // The method is invoked here instead of placing it in the 
                // queue. The reason for this is that if EndInvoke is called 
                // from the same thread in which the DelegateQueue is running and
                // the method has not been invoked, deadlock will occur. 
                result.Invoke();
            }

            return result;
        }

        /// <summary>
        /// Executes the delegate on the main thread that this object executes on.
        /// </summary>
        /// <param name="method">
        /// A Delegate to a method that takes parameters of the same number and 
        /// type that are contained in args. 
        /// </param>
        /// <param name="args">
        /// An array of type Object to pass as arguments to the given method. 
        /// </param>
        /// <returns>
        /// An IAsyncResult interface that represents the asynchronous operation 
        /// started by calling this method.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The delegate is placed at the beginning of the queue. Its invocation
        /// takes priority over delegates already in the queue. 
        /// </para>
        /// <para>
        /// Unlike BeginInvoke, this method operates synchronously, that is, it 
        /// waits until the process completes before returning. Exceptions raised 
        /// during the call are propagated back to the caller.
        /// </para>
        /// </remarks>
        public object InvokePriority(Delegate method, params object[] args)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("DelegateQueue");
            }
            else if(method == null)
            {
                throw new ArgumentNullException();
            }

            #endregion

            object returnValue = null;

            // If InvokePriority was called from a different thread than the one
            // in which the DelegateQueue is running.
            if(InvokeRequired)
            {
                DelegateQueueAsyncResult result = new DelegateQueueAsyncResult(this, method, args, false, NotificationType.None);

                lock(lockObject)
                {
                    // Put the method at the back of the queue.
                    delegateDeque.PushFront(result);

                    Monitor.Pulse(lockObject);
                }

                // Wait for the result of the method invocation.
                returnValue = EndInvoke(result);
            }
            // Else InvokePriority was called from the same thread in which the 
            // DelegateQueue is running.
            else
            {
                // Invoke the method here rather than placing it in the queue.
                returnValue = method.DynamicInvoke(args);
            }

            return returnValue;
        }

        /// <summary>
        /// Executes the delegate on the main thread that this object executes on.
        /// </summary>
        /// <param name="callback">
        /// An optional asynchronous callback, to be called when the method is invoked.
        /// </param>
        /// <param name="state">
        /// A user-provided object that distinguishes this particular asynchronous invoke request from other requests. 
        /// </param>
        /// <param name="method">
        /// A Delegate to a method that takes parameters of the same number and 
        /// type that are contained in args. 
        /// </param>
        /// <param name="args">
        /// An array of type Object to pass as arguments to the given method. 
        /// </param>
        /// <returns>
        /// An IAsyncResult interface that represents the asynchronous operation 
        /// started by calling this method.
        /// </returns>
        public IAsyncResult BeginInvoke(AsyncCallback callback, object state, Delegate method, params object[] args)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("DelegateQueue");
            }
            else if(method == null)
            {
                throw new ArgumentNullException();
            }

            #endregion

            DelegateQueueAsyncResult result;

            if(InvokeRequired)
            {
                result = new DelegateQueueAsyncResult(this, callback, state, method, args, false, NotificationType.BeginInvokeCompleted);

                lock(lockObject)
                {
                    delegateDeque.PushBack(result);

                    Monitor.Pulse(lockObject);
                }
            }
            else
            {
                result = new DelegateQueueAsyncResult(this, callback, state, method, args, false, NotificationType.None);

                result.Invoke();
            }

            return result;
        }

        /// <summary>
        /// Dispatches an asynchronous message to this synchronization context. 
        /// </summary>
        /// <param name="d">
        /// The SendOrPostCallback delegate to call.
        /// </param>
        /// <param name="state">
        /// The object passed to the delegate.
        /// </param>
        /// <remarks>
        /// The Post method starts an asynchronous request to post a message. 
        /// </remarks>
        public void PostPriority(SendOrPostCallback d, object state)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("DelegateQueue");
            }
            else if(d == null)
            {
                throw new ArgumentNullException();
            }

            #endregion

            lock(lockObject)
            {
                DelegateQueueAsyncResult result = new DelegateQueueAsyncResult(this, d, new object[] { state }, false, NotificationType.PostCompleted);

                // Put the method at the front of the queue.
                delegateDeque.PushFront(result);

                Monitor.Pulse(lockObject);
            }
        }

        /// <summary>
        /// Dispatches an synchronous message to this synchronization context. 
        /// </summary>
        /// <param name="d">
        /// The SendOrPostCallback delegate to call.
        /// </param>
        /// <param name="state">
        /// The object passed to the delegate.
        /// </param>
        public void SendPriority(SendOrPostCallback d, object state)
        {
            InvokePriority(d, state);
        }       

        // Processes and invokes delegates.
        private void DelegateProcedure()
        {
            lock(lockObject)
            {
                // Signal the constructor that the thread is now running.
                Monitor.Pulse(lockObject);
            }

            // Set this DelegateQueue as the SynchronizationContext for this thread.
            SynchronizationContext.SetSynchronizationContext(this);

            // Placeholder for DelegateQueueAsyncResult objects.
            DelegateQueueAsyncResult result = null;

            // While the DelegateQueue has not been disposed.
            while(true)
            {
                // Critical section.
                lock(lockObject)
                {
                    // If the DelegateQueue has been disposed, break out of loop; we're done.
                    if(disposed)
                    {
                        break;
                    }

                    // If there are delegates waiting to be invoked.
                    if(delegateDeque.Count > 0)
                    {
                        result = delegateDeque.PopFront();
                    }
                    // Else there are no delegates waiting to be invoked.
                    else
                    {
                        // Wait for next delegate.
                        Monitor.Wait(lockObject);

                        // If the DelegateQueue has been disposed, break out of loop; we're done.
                        if(disposed)
                        {
                            break;
                        }

                        Debug.Assert(delegateDeque.Count > 0);

                        result = delegateDeque.PopFront();
                    }
                }

                Debug.Assert(result != null);

                // Invoke the delegate.
                result.Invoke();

                if(result.NotificationType == NotificationType.BeginInvokeCompleted)
                {
                    InvokeCompletedEventArgs e = new InvokeCompletedEventArgs(
                        result.Method,
                        result.GetArgs(),
                        result.ReturnValue,
                        result.Error);

                    OnInvokeCompleted(e);
                }
                else if(result.NotificationType == NotificationType.PostCompleted)
                {
                    object[] args = result.GetArgs();

                    Debug.Assert(args.Length == 1);
                    Debug.Assert(result.Method is SendOrPostCallback);

                    PostCompletedEventArgs e = new PostCompletedEventArgs(
                        (SendOrPostCallback)result.Method,
                         result.Error,
                         args[0]);

                    OnPostCompleted(e);
                }
                else
                {
                    Debug.Assert(result.NotificationType == NotificationType.None);
                }
            }

            Debug.WriteLine(delegateThread.Name + " Finished");
        }
        
        // Raises the InvokeCompleted event.
        protected virtual void OnInvokeCompleted(InvokeCompletedEventArgs e)
        {
            EventHandler<InvokeCompletedEventArgs> handler = InvokeCompleted;

            if(handler != null)
            {
                context.Post(delegate(object state)
                {
                    handler(this, e);
                }, null);
            }
        }

        // Raises the PostCompleted event.
        protected virtual void OnPostCompleted(PostCompletedEventArgs e)
        {
            EventHandler<PostCompletedEventArgs> handler = PostCompleted;

            if(handler != null)
            {
                context.Post(delegate(object state)
                {
                    handler(this, e);
                }, null);
            }
        }

        // Raises the Disposed event.
        protected virtual void OnDisposed(EventArgs e)
        {
            EventHandler handler = Disposed;

            if(handler != null)
            {
                context.Post(delegate(object state)
                {
                    handler(this, e);
                }, null);
            }
        }

        #endregion        

        #endregion

        #region SynchronizationContext Overrides

        /// <summary>
        /// Dispatches a synchronous message to this synchronization context. 
        /// </summary>
        /// <param name="d">
        /// The SendOrPostCallback delegate to call.
        /// </param>
        /// <param name="state">
        /// The object passed to the delegate.
        /// </param>
        /// <remarks>
        /// The Send method starts an synchronous request to send a message. 
        /// </remarks>
        public override void Send(SendOrPostCallback d, object state)
        {
            Invoke(d, state);
        }

        /// <summary>
        /// Dispatches an asynchronous message to this synchronization context. 
        /// </summary>
        /// <param name="d">
        /// The SendOrPostCallback delegate to call.
        /// </param>
        /// <param name="state">
        /// The object passed to the delegate.
        /// </param>
        /// <remarks>
        /// The Post method starts an asynchronous request to post a message. 
        /// </remarks>
        public override void Post(SendOrPostCallback d, object state)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("DelegateQueue");
            }
            else if(d == null)
            {
                throw new ArgumentNullException();
            }

            #endregion

            lock(lockObject)
            {
                delegateDeque.PushBack(new DelegateQueueAsyncResult(this, d, new object[] { state }, false, NotificationType.PostCompleted));

                Monitor.Pulse(lockObject);
            }
        }

        #endregion

        #region IComponent Members

        /// <summary>
        /// Represents the method that handles the Disposed delegate of a DelegateQueue.
        /// </summary>
        public event System.EventHandler Disposed;

        /// <summary>
        /// Gets or sets the ISite associated with the DelegateQueue.
        /// </summary>
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

        #region ISynchronizeInvoke Members

        /// <summary>
        /// Executes the delegate on the main thread that this DelegateQueue executes on.
        /// </summary>
        /// <param name="method">
        /// A Delegate to a method that takes parameters of the same number and type that 
        /// are contained in args. 
        /// </param>
        /// <param name="args">
        /// An array of type Object to pass as arguments to the given method. This can be 
        /// a null reference (Nothing in Visual Basic) if no arguments are needed. 
        /// </param>
        /// <returns>
        /// An IAsyncResult interface that represents the asynchronous operation started 
        /// by calling this method.
        /// </returns>
        /// <remarks>
        /// <para>The delegate is called asynchronously, and this method returns immediately. 
        /// You can call this method from any thread. If you need the return value from a process 
        /// started with this method, call EndInvoke to get the value.</para>
        /// <para>If you need to call the delegate synchronously, use the Invoke method instead.</para>
        /// </remarks>
        public IAsyncResult BeginInvoke(Delegate method, params object[] args)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("DelegateQueue");
            }
            else if(method == null)
            {
                throw new ArgumentNullException();
            }

            #endregion

            DelegateQueueAsyncResult result;

            if(InvokeRequired)
            {
                result = new DelegateQueueAsyncResult(this, method, args, false, NotificationType.BeginInvokeCompleted);

                lock(lockObject)
                {
                    delegateDeque.PushBack(result);

                    Monitor.Pulse(lockObject);
                }
            }
            else
            {
                result = new DelegateQueueAsyncResult(this, method, args, false, NotificationType.None);

                result.Invoke();
            }

            return result;
        }

        /// <summary>
        /// Waits until the process started by calling BeginInvoke completes, and then returns 
        /// the value generated by the process.
        /// </summary>
        /// <param name="result">
        /// An IAsyncResult interface that represents the asynchronous operation started 
        /// by calling BeginInvoke. 
        /// </param>
        /// <returns>
        /// An Object that represents the return value generated by the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// This method gets the return value of the asynchronous operation represented by the 
        /// IAsyncResult passed by this interface. If the asynchronous operation has not completed, this method will wait until the result is available.
        /// </remarks>
        public object EndInvoke(IAsyncResult result)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("DelegateQueue");
            }
            else if(!(result is DelegateQueueAsyncResult))
            {
                throw new ArgumentException();
            }
            else if(((DelegateQueueAsyncResult)result).Owner != this)
            {
                throw new ArgumentException();
            }

            #endregion

            result.AsyncWaitHandle.WaitOne();

            DelegateQueueAsyncResult r = (DelegateQueueAsyncResult)result;

            if(r.Error != null)
            {
                throw r.Error;
            }

            return r.ReturnValue;
        }

        /// <summary>
        /// Executes the delegate on the main thread that this DelegateQueue executes on.
        /// </summary>
        /// <param name="method">
        /// A Delegate that contains a method to call, in the context of the thread for the DelegateQueue.
        /// </param>
        /// <param name="args">
        /// An array of type Object that represents the arguments to pass to the given method.
        /// </param>
        /// <returns>
        /// An Object that represents the return value from the delegate being invoked, or a 
        /// null reference (Nothing in Visual Basic) if the delegate has no return value.
        /// </returns>
        /// <remarks>
        /// <para>Unlike BeginInvoke, this method operates synchronously, that is, it waits until 
        /// the process completes before returning. Exceptions raised during the call are propagated 
        /// back to the caller.</para>
        /// <para>Use this method when calling a method from a different thread to marshal the call 
        /// to the proper thread.</para>
        /// </remarks>
        public object Invoke(Delegate method, params object[] args)
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException("DelegateQueue");
            }
            else if(method == null)
            {
                throw new ArgumentNullException();
            }

            #endregion

            object returnValue = null;

            if(InvokeRequired)
            {
                DelegateQueueAsyncResult result = new DelegateQueueAsyncResult(this, method, args, false, NotificationType.None);

                lock(lockObject)
                {
                    delegateDeque.PushBack(result);

                    Monitor.Pulse(lockObject);
                }

                returnValue = EndInvoke(result);
            }
            else
            {
                // Invoke the method here rather than placing it in the queue.
                returnValue = method.DynamicInvoke(args);
            }

            return returnValue;
        }

        /// <summary>
        /// Gets a value indicating whether the caller must call Invoke.
        /// </summary>
        /// <value>
        /// <b>true</b> if the caller must call Invoke; otherwise, <b>false</b>.
        /// </value>
        /// <remarks>
        /// This property determines whether the caller must call Invoke when making 
        /// method calls to this DelegateQueue. If you are calling a method from a different 
        /// thread, you must use the Invoke method to marshal the call to the proper thread.
        /// </remarks>
        public bool InvokeRequired
        {
            get
            {
                return Thread.CurrentThread.ManagedThreadId != delegateThread.ManagedThreadId;
            }
        }        

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes of the DelegateQueue.
        /// </summary>
        public void Dispose()
        {
            #region Guards

            if(disposed)
            {
                return;
            }

            #endregion

            Dispose(true);

            OnDisposed(EventArgs.Empty);
        }

        #endregion                
    }
}
