using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia
{
    /// <summary>
    /// This will handle any errors relating to Sanford.Multimedia.
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        private Exception ex;

        /// <summary>
        /// This represents the error itself.
        /// </summary>
        public ErrorEventArgs(Exception ex)
        {
            this.ex = ex;
        }

        /// <summary>
        /// Displays the error.
        /// </summary>
        /// <returns>
        /// The error that is associated with the issue.
        /// </returns>
        public Exception Error
        {
            get
            {
                return ex;
            }
        }
    }
}
