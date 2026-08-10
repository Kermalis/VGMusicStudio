// License:     APL 2.0
// Author:      Benjamin N. Summerton <https://16bpp.net>

using System;

namespace PortAudio
{
    /// <summary>
    /// NOTE: this doesn't exist an as actual enum in the native library, but we can make it a bit safer in C#
    ///
    /// Flag bit constants for the statusFlags to PaStreamCallback.
    /// </summary>
    public enum StreamCallbackFlags : System.UInt32
    {
        /// <summary>
        /// In a stream opened with paFramesPerBufferUnspecified, indicates that
        /// input data is all silence (zeros) because no real data is available. In a
        /// stream opened without paFramesPerBufferUnspecified, it indicates that one or
        /// more zero samples have been inserted into the input buffer to compensate
        /// for an input underflow.
        /// </summary>
        InputUnderflow = 0x00000001,

        /// <summary>
        /// In a stream opened with paFramesPerBufferUnspecified, indicates that data
        /// prior to the first sample of the input buffer was discarded due to an
        /// overflow, possibly because the stream callback is using too much CPU time.
        /// Otherwise indicates that data prior to one or more samples in the
        /// input buffer was discarded.
        /// </summary>
        InputOverflow = 0x00000002,

        /// <summary>
        /// Indicates that output data (or a gap) was inserted, possibly because the
        /// stream callback is using too much CPU time.
        /// </summary>
        OutputUnderflow = 0x00000004,

        /// <summary>
        /// Indicates that output data will be discarded because no room is available.
        /// </summary>
        OutputOverflow = 0x00000008,

        /// <summary>
        /// Some of all of the output data will be used to prime the stream, input
        /// data may be zero.
        /// </summary>
        PrimingOutput = 0x00000010
    }
}
