// License:     APL 2.0
// Author:      Benjamin N. Summerton <https://16bpp.net>

using System;
using System.Runtime.InteropServices;

using Time = System.Double;

namespace PortAudio
{
    /// <summary>
    /// Timing information for the buffers passed to the stream callback.
    ///
    /// Time values are expressed in seconds and are synchronised with the time base used by Pa_GetStreamTime() for the associated stream.
    ///
    /// @see PaStreamCallback, Pa_GetStreamTime
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct StreamCallbackTimeInfo
    {
        /// <summary>
        /// The time when the first sample of the input buffer was captured at the ADC input
        /// </summary>
        public Time inputBufferAdcTime;

        /// <summary>
        /// The time when the stream callback was invoked
        /// </summary>
        public Time currentTime;

        /// <summary>
        ///  The time when the first sample of the output buffer will output the DAC
        /// </summary>
        public Time outputBufferDacTime;
    }
}
