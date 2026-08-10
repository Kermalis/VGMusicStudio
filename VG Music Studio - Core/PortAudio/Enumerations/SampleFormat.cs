// License:     APL 2.0
// Author:      Benjamin N. Summerton <https://16bpp.net>

using System;

namespace PortAudio
{
    /// <summary>
    /// NOTE: this doesn't exist an as actual enum in the native library, but we can make it a bit safer in C#
    ///
    /// A type used to specify one or more sample formats. Each value indicates
    /// a possible format for sound data passed to and from the stream callback,
    /// Pa_ReadStream and Pa_WriteStream.
    ///
    /// The standard formats paFloat32, paInt16, paInt32, paInt24, paInt8
    /// and aUInt8 are usually implemented by all implementations.
    ///
    /// The floating point representation (paFloat32) uses +1.0 and -1.0 as the
    /// maximum and minimum respectively.
    ///
    /// paUInt8 is an unsigned 8 bit format where 128 is considered "ground"
    ///
    /// The paNonInterleaved flag indicates that audio data is passed as an array
    /// of pointers to separate buffers, one buffer for each channel. Usually,
    /// when this flag is not used, audio data is passed as a single buffer with
    /// all channels interleaved.
    ///
    /// @see Pa_OpenStream, Pa_OpenDefaultStream, PaDeviceInfo
    /// @see paFloat32, paInt16, paInt32, paInt24, paInt8
    /// @see paUInt8, paCustomFormat, paNonInterleaved
    /// </summary>
    public enum SampleFormat : System.UInt32
    {
        Float32        = 0x00000001,
        Int32          = 0x00000002,

        /// <summary>Packed 24 bit format.</summary>
        Int24          = 0x00000004,

        Int16          = 0x00000008,
        Int8           = 0x00000010,
        UInt8          = 0x00000020,
        CustomFormat   = 0x00010000,

        NonInterleaved = 0x80000000,
    }
}
