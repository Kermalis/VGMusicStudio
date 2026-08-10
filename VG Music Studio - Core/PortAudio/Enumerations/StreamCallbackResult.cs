// License:     APL 2.0
// Author:      Benjamin N. Summerton <https://16bpp.net>

namespace PortAudio
{
    /// <summary>
    /// Allowable return values for the PaStreamCallback.
    /// @see PaStreamCallback
    /// </summary>
    public enum StreamCallbackResult
    {
        /// <summary>
        /// Signal that the stream should continue invoking the callback and processing audio.
        /// </summary>
        Continue = 0,

        /// <summary>
        /// Signal that the stream should stop invoking the callback and finish once all output samples have played.
        /// </summary>
        Complete = 1,

        /// <summary>
        /// Signal that the stream should stop invoking the callback and finish as soon as possible.
        /// </summary>
        Abort = 2,
    }
}
