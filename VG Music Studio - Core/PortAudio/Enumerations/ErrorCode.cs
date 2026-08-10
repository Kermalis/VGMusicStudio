// License:     APL 2.0
// Author:      Benjamin N. Summerton <https://16bpp.net>

namespace PortAudio
{
    /// <summary>
    /// Error codes returned by PortAudio functions.
    /// Note that with the exception of paNoError, all PaErrorCodes are negative.
    /// </summary>
    public enum ErrorCode
    {
        NoError = 0,

        NotInitialized = -10000,
        UnanticipatedHostError,
        InvalidChannelCount,
        InvalidSampleRate,
        InvalidDevice,
        InvalidFlag,
        SampleFormatNotSupported,
        BadIODeviceCombination,
        InsufficientMemory,
        BufferTooBig,
        BufferTooSmall,
        NullCallback,
        BadStreamPtr,
        TimedOut,
        InternalError,
        DeviceUnavailable,
        IncompatibleHostApiSpecificStreamInfo,
        StreamIsStopped,
        StreamIsNotStopped,
        InputOverflowed,
        OutputUnderflowed,
        HostApiNotFound,
        InvalidHostApi,
        CanNotReadFromACallbackStream,
        CanNotWriteToACallbackStream,
        CanNotReadFromAnOutputOnlyStream,
        CanNotWriteToAnInputOnlyStream,
        IncompatibleStreamHostApi,
        BadBufferPtr
    }
}
