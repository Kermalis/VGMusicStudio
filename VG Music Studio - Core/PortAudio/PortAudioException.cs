// License:     APL 2.0
// Author:      Benjamin N. Summerton <https://16bpp.net>

using System;

namespace PortAudio
{
    public class PortAudioException : Exception
    {
        /// <summary>
        /// Error code (from the native PortAudio library).  Use `PortAudio.GetErrorText()` for some more details.
        /// </summary>
        public ErrorCode ErrorCode { get; private set; }

        /// <summary>
        /// Creates a new PortAudio error.
        /// </summary>
        public PortAudioException(ErrorCode ec) : base()
        {
            this.ErrorCode = ec;
        }

        /// <summary>
        /// Creates a new PortAudio error with a message attached.
        /// </summary>
        /// <param name="message">Message to send</param>
        public PortAudioException(ErrorCode ec, string message)
            : base(message)
        {
            this.ErrorCode = ec;
        }

        /// <summary>
        /// Creates a new PortAudio error with a message attached and an inner error.
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <param name="inner">The exception that occured inside of this one</param>
        public PortAudioException(ErrorCode ec, string message, Exception inner)
            : base(message, inner)
        {
            this.ErrorCode = ec;
        }
    }
}
