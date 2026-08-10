// License:     APL 2.0
// Author:      Benjamin N. Summerton <https://16bpp.net>

using System;

namespace PortAudio
{
    /// <summary>
    /// NOTE: this doesn't exist an as actual enum in the native library, but we can make it a bit safer in C#
    ///
    /// Flags used to control the behavior of a stream. They are passed as
    /// parameters to Pa_OpenStream or Pa_OpenDefaultStream. Multiple flags may be
    /// ORed together.
    ///
    /// @see Pa_OpenStream, Pa_OpenDefaultStream
    /// @see paNoFlag, paClipOff, paDitherOff, paNeverDropInput,
    /// paPrimeOutputBuffersUsingStreamCallback, paPlatformSpecificFlags
    /// </summary>
    public enum StreamFlags : System.UInt32
    {
        NoFlag = 0,

        /// <summary>
        /// Disable default clipping of out of range samples.
        /// </summary>
        ClipOff = 0x00000001,

        /// <summary>
        /// Disable default dithering.
        /// </summary>
        DitherOff = 0x00000002,

        /// <summary>
        /// Flag requests that where possible a full duplex stream will not discard
        /// overflowed input samples without calling the stream callback. This flag is
        /// only valid for full duplex callback streams and only when used in combination
        /// with the paFramesPerBufferUnspecified (0) framesPerBuffer parameter. Using
        /// this flag incorrectly results in a paInvalidFlag error being returned from
        /// Pa_OpenStream and Pa_OpenDefaultStream.
        ///
        /// @see paFramesPerBufferUnspecified
        /// </summary>
        NeverDropInput = 0x00000004,

        /// <summary>
        /// Call the stream callback to fill initial output buffers, rather than the
        /// default behavior of priming the buffers with zeros (silence). This flag has
        /// no effect for input-only and blocking read/write streams.
        /// </summary>
        PrimeOutputBuffersUsingStreamCallback = 0x00000008,

        /// <summary>
        /// A mask specifying the platform specific bits.
        /// </summary>
        PlatformSpecificFlags = 0xFFFF0000,
    }
}
