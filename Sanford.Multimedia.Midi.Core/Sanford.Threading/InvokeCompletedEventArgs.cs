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
using System.ComponentModel;
using System.Reflection;

namespace Sanford.Threading
{
    /// <summary>
    /// Represents information about the InvokeCompleted event.
    /// </summary>
    public class InvokeCompletedEventArgs : AsyncCompletedEventArgs
    {
        private Delegate method;

        private object[] args;

        private object result;

        /// <summary>
        /// Represents the delegate, objects and exceptions for the InvokeCompleted event.
        /// </summary>
        /// <param name="method">
        /// Represents the delegate method used.
        /// </param>
        /// <param name="args">
        /// For any args to be used.
        /// </param>
        /// <param name="result">
        /// For any results that occur.
        /// </param>
        /// <param name="error">
        /// For any errors that may occur.
        /// </param>
        public InvokeCompletedEventArgs(Delegate method, object[] args, object result, Exception error) 
            : base(error, false, null)
        {
            this.method = method;
            this.args = args;
            this.result = result;
        }

        /// <summary>
        /// Initializes the args as an object.
        /// </summary>
        public object[] GetArgs()
        {
            return args;
        }

        /// <summary>
        /// Initializes method as a delegate.
        /// </summary>
        public Delegate Method
        {
            get
            {
                return method;
            }
        }

        /// <summary>
        /// Initializes result as an object.
        /// </summary>
        public object Result
        {
            get
            {
                return result;
            }
        }
    }
}
