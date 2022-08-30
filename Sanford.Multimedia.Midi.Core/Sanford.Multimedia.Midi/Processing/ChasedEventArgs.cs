using System;
using System.Collections;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// A class for chased events.
    /// </summary>
    public class ChasedEventArgs : EventArgs
    {
        private ICollection messages;

        /// <summary>
		/// Main function for chased events.
		/// </summary>
        public ChasedEventArgs(ICollection messages)
        {
            this.messages = messages;
        }

        /// <summary>
		/// Gets and returns messages.
		/// </summary>
        public ICollection Messages
        {
            get
            {
                return messages;
            }
        }
    }
}
