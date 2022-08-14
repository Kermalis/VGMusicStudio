#region License

/* Copyright (c) 2007 Leslie Sanford
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
using System.Diagnostics;

namespace Sanford.Threading
{
    public class Task : IComparable
    {
        #region Task Members

        #region Fields

        // The number of times left to invoke the delegate associated with this Task.
        private int count;

        // The interval between delegate invocation.
        private int millisecondsTimeout;

        // The delegate to invoke.
        private Delegate method;

        // The arguments to pass to the delegate when it is invoked.
        private object[] args;

        // The time for the next timeout;
        private DateTime nextTimeout;

        // For locking.
        private readonly object lockObject = new object();

        #endregion

        #region Construction

        internal Task(
            int count,
            int millisecondsTimeout,
            Delegate method,
            object[] args)
        {
            this.count = count;
            this.millisecondsTimeout = millisecondsTimeout;
            this.method = method;
            this.args = args;

            ResetNextTimeout();
        }

        #endregion

        #region Methods

        internal void ResetNextTimeout()
        {
            nextTimeout = DateTime.Now.AddMilliseconds(millisecondsTimeout);
        }

        internal object Invoke(DateTime signalTime)
        {
            Debug.Assert(count == DelegateScheduler.Infinite || count > 0);

            object returnValue = method.DynamicInvoke(args);

            if(count == DelegateScheduler.Infinite)
            {
                nextTimeout = nextTimeout.AddMilliseconds(millisecondsTimeout);
            }
            else
            {
                count--;

                if(count > 0)
                {
                    nextTimeout = nextTimeout.AddMilliseconds(millisecondsTimeout);
                }
            }

            return returnValue;
        }

        public object[] GetArgs()
        {
            return args;
        }

        #endregion

        #region Properties

        public DateTime NextTimeout
        {
            get
            {
                return nextTimeout;
            }
        }

        public int Count
        {
            get
            {
                return count;
            }
        }

        public Delegate Method
        {
            get
            {
                return method;
            }
        }

        public int MillisecondsTimeout
        {
            get
            {
                return millisecondsTimeout;
            }
        }

        #endregion

        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            Task t = obj as Task;

            if(t == null)
            {
                throw new ArgumentException("obj is not the same type as this instance.");
            }

            return -nextTimeout.CompareTo(t.nextTimeout);
        }

        #endregion
    }
}
