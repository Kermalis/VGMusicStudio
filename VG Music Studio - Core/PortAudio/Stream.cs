// License:     APL 2.0
// Author:      Benjamin N. Summerton <https://16bpp.net>

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PortAudio
{
    /// <summary>
    /// A single PaStream can provide multiple channels of real-time
    /// streaming audio input and output to a client application. A stream
    /// provides access to audio hardware represented by one or more
    /// PaDevices. Depending on the underlying Host API, it may be possible
    /// to open multiple streams using the same device, however this behavior
    /// is implementation defined. Portable applications should assume that
    /// a PaDevice may be simultaneously used by at most one PaStream.
    ///
    /// Pointers to PaStream objects are passed between PortAudio functions that
    /// operate on streams.
    ///
    /// @see Pa_OpenStream, Pa_OpenDefaultStream, Pa_OpenDefaultStream, Pa_CloseStream,
    /// Pa_StartStream, Pa_StopStream, Pa_AbortStream, Pa_IsStreamActive,
    /// Pa_GetStreamTime, Pa_GetStreamCpuLoad
    /// </summary>
    public class Stream : IDisposable
    {
        // Clean & manually managed data
        private bool disposed = false;
        private nint streamPtr = nint.Zero;      // `Stream *`
        private GCHandle userDataHandle;

        // Callback structures
        private _NativeInterfacingCallback<Callback>? streamCallback;
        private _NativeInterfacingCallback<FinishedCallback>? finishedCallback;

        /// <summary>
        /// The input parameters for this stream, if any
        /// </summary>
        /// <value>will be `null` if the user never supplied any</value>
        public StreamParameters? InputParameters { get; private set; }

        /// <summary>
        /// The output parameters for this stream, if any
        /// </summary>
        /// <value>will be `null` if the user never supplied any</value>
        public StreamParameters? OutputParameters { get; private set; }


        #region Constructors & Cleanup
        /// <summary>
        /// Opens a stream for either input, output or both.
        ///
        /// @param stream The address of a PaStream pointer which will receive
        /// a pointer to the newly opened stream.
        ///
        /// @param inputParameters A structure that describes the input parameters used by
        /// the opened stream. See PaStreamParameters for a description of these parameters.
        /// inputParameters must be NULL for output-only streams.
        ///
        /// @param outputParameters A structure that describes the output parameters used by
        /// the opened stream. See PaStreamParameters for a description of these parameters.
        /// outputParameters must be NULL for input-only streams.
        ///
        /// @param sampleRate The desired sampleRate. For full-duplex streams it is the
        /// sample rate for both input and output
        ///
        /// @param framesPerBuffer The number of frames passed to the stream callback
        /// function, or the preferred block granularity for a blocking read/write stream.
        /// The special value paFramesPerBufferUnspecified (0) may be used to request that
        /// the stream callback will receive an optimal (and possibly varying) number of
        /// frames based on host requirements and the requested latency settings.
        /// Note: With some host APIs, the use of non-zero framesPerBuffer for a callback
        /// stream may introduce an additional layer of buffering which could introduce
        /// additional latency. PortAudio guarantees that the additional latency
        /// will be kept to the theoretical minimum however, it is strongly recommended
        /// that a non-zero framesPerBuffer value only be used when your algorithm
        /// requires a fixed number of frames per stream callback.
        ///
        /// @param streamFlags Flags which modify the behavior of the streaming process.
        /// This parameter may contain a combination of flags ORed together. Some flags may
        /// only be relevant to certain buffer formats.
        ///
        /// @param streamCallback A pointer to a client supplied function that is responsible
        /// for processing and filling input and output buffers. If this parameter is NULL
        /// the stream will be opened in 'blocking read/write' mode. In blocking mode,
        /// the client can receive sample data using Pa_ReadStream and write sample data
        /// using Pa_WriteStream, the number of samples that may be read or written
        /// without blocking is returned by Pa_GetStreamReadAvailable and
        /// Pa_GetStreamWriteAvailable respectively.
        ///
        /// @param userData A client supplied pointer which is passed to the stream callback
        /// function. It could for example, contain a pointer to instance data necessary
        /// for processing the audio buffers. This parameter is ignored if streamCallback
        /// is NULL.
        ///   NOTE: userData will no longer be automatically GC'd normally by C#.  The cleanup
        ///         of that will be handled by this class upon `Dipose()` or deletion. You (the
        ///         programmer), shouldn't have to worry about this.
        ///
        /// @return
        /// Upon success Pa_OpenStream() returns paNoError and places a pointer to a
        /// valid PaStream in the stream argument. The stream is inactive (stopped).
        /// If a call to Pa_OpenStream() fails, a non-zero error code is returned (see
        /// PaError for possible error codes) and the value of stream is invalid.
        ///
        /// @see PaStreamParameters, PaStreamCallback, Pa_ReadStream, Pa_WriteStream,
        /// Pa_GetStreamReadAvailable, Pa_GetStreamWriteAvailable
        /// </summary>
        /// <param name="inputParameters"></param>
        /// <param name="outputParameters"></param>
        /// <param name="sampleRate"></param>
        /// <param name="framesPerBuffer"></param>
        /// <param name="streamFlags"></param>
        /// <param name="callback"></param>
        /// <param name="userData"></param>
        public Stream(
            StreamParameters? inputParameters,
            StreamParameters? outputParameters,
            double sampleRate,
            uint framesPerBuffer,
            StreamFlags streamFlags,
            Callback callback,
            object userData
        )
        {
            // Setup the steam's callback
            streamCallback = new _NativeInterfacingCallback<Callback>(callback);

            // Take control of the userdata object
            userDataHandle = GCHandle.Alloc(userData);

            // Set the ins and the outs
            InputParameters = inputParameters;
            OutputParameters = outputParameters;

            // If the in/out params are set, then we need to make some P/Invoke friendly memory
            nint inputParametersPtr = nint.Zero;
            nint outputParametersPtr = nint.Zero;
            if (inputParameters.HasValue)
            {
                inputParametersPtr = Marshal.AllocHGlobal(Marshal.SizeOf(inputParameters.Value));
                Marshal.StructureToPtr<StreamParameters>(inputParameters.Value, inputParametersPtr, false);
            }
            if (outputParameters.HasValue)
            {
                outputParametersPtr = Marshal.AllocHGlobal(Marshal.SizeOf(outputParameters.Value));
                Marshal.StructureToPtr<StreamParameters>(outputParameters.Value, outputParametersPtr, false);
            }

            // Open the stream
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_OpenStream(
                out streamPtr,
                inputParametersPtr,
                outputParametersPtr,
                sampleRate,
                framesPerBuffer,
                streamFlags,
                streamCallback.Ptr,
                GCHandle.ToIntPtr(userDataHandle)
            );
            if (ec != ErrorCode.NoError)
                throw new PortAudioException(ec, "Error opening PortAudio Stream.\nError Code: " + ec.ToString());

            // Cleanup the in/out params ptrs
            if (inputParametersPtr != nint.Zero)
                Marshal.FreeHGlobal(inputParametersPtr);
            if (outputParametersPtr != nint.Zero)
                Marshal.FreeHGlobal(outputParametersPtr);
        }
        
        /// <summary>
        /// A simplified version of Pa_OpenStream() that opens the default input
        /// and/or output devices.
        ///
        /// @param stream The address of a PaStream pointer which will receive
        /// a pointer to the newly opened stream.
        ///
        /// @param numInputChannels  The number of channels of sound that will be supplied
        /// to the stream callback or returned by Pa_ReadStream(). It can range from 1 to
        /// the value of maxInputChannels in the PaDeviceInfo record for the default input
        /// device. If 0 the stream is opened as an output-only stream.
        ///
        /// @param numOutputChannels The number of channels of sound to be delivered to the
        /// stream callback or passed to Pa_WriteStream. It can range from 1 to the value
        /// of maxOutputChannels in the PaDeviceInfo record for the default output device.
        /// If 0 the stream is opened as an input-only stream.
        ///
        /// @param sampleFormat The sample format of both the input and output buffers
        /// provided to the callback or passed to and from Pa_ReadStream() and Pa_WriteStream().
        /// sampleFormat may be any of the formats described by the PaSampleFormat
        /// enumeration.
        ///
        /// @param sampleRate Same as Pa_OpenStream parameter of the same name.
        /// @param framesPerBuffer Same as Pa_OpenStream parameter of the same name.
        /// @param streamCallback Same as Pa_OpenStream parameter of the same name.
        /// @param userData Same as Pa_OpenStream parameter of the same name.
        ///
        /// @return As for Pa_OpenStream
        ///
        /// @see Pa_OpenStream, PaStreamCallback
        /// </summary>
        /// <param name="numInputChannels"></param>
        /// <param name="numOutputChannels"></param>
        /// <param name="sampleFormat"></param>
        /// <param name="sampleRate"></param>
        /// <param name="framesPerBuffer"></param>
        /// <param name="callback"></param>
        /// <param name="userData"></param>
        public Stream(
            int numInputChannels,
            int numOutputChannels,
            SampleFormat sampleFormat,
            double sampleRate,
            uint framesPerBuffer,
            Callback callback,
            object userData
        )
        {
            // Setup the stream's callback
            streamCallback = new _NativeInterfacingCallback<Callback>(callback);

            // Take control of the userdata object
            userDataHandle = GCHandle.Alloc(userData);

            // Open the stream
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_OpenDefaultStream(
                out streamPtr,
                numInputChannels,
                numOutputChannels,
                sampleFormat,
                sampleRate,
                framesPerBuffer,
                streamCallback.Ptr,
                GCHandle.ToIntPtr(userDataHandle)
            );
            if (ec != ErrorCode.NoError)
                throw new PortAudioException(ec, "Error opening PortAudio Stream.\nError Code: " + ec.ToString());
        }
        ~Stream()
        {
            Dispose(false);
        }

        /// <summary>
        /// Cleanup resources (for the IDisposable interface)
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Does the actual disposing work
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            // Free Managed Resources
            if (disposing)
            {
            }

            // Free Unmanaged resources
            Close();
            userDataHandle.Free();
            streamCallback!.Free();
            if (finishedCallback != null)
                finishedCallback.Free();

            disposed = true;
        }
        #endregion // Constructors & Cleanup

        /// <summary>
        /// Set a callback to be triggered when the stream is done.
        /// </summary>
        public void SetFinishedCallback(FinishedCallback fcb)
        {
            finishedCallback = new _NativeInterfacingCallback<FinishedCallback>(fcb);

            // TODO what happens if a callback is already set?  Find out and make the necessary adjustments
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_SetStreamFinishedCallback(streamPtr, finishedCallback.Ptr);
            if (ec != ErrorCode.NoError)
                throw new PortAudioException(ec, "Error setting finished callback for PortAudio Stream.\nError Code: " + ec.ToString());
        }

        #region Operations
        /// <summary>
        /// Closes an audio stream. If the audio stream is active it
        /// discards any pending buffers as if Pa_AbortStream() had been called.
        /// </summary>
        public void Close()
        {
            // Did we already clean up?
            if (streamPtr == nint.Zero)
                return;

            ErrorCode ec = (ErrorCode)Native.Stream.Pa_CloseStream(streamPtr);
            if (ec != ErrorCode.NoError)
                throw new PortAudioException(ec, "Error closing PortAudio Stream.\nError Code: " + ec.ToString());

            // Reset the handle, since we've cleaned up
            streamPtr = nint.Zero;
        }

        /// <summary>
        /// Commences audio processing.
        /// </summary>
        public void Start()
        {
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_StartStream(streamPtr);
            if (ec != ErrorCode.NoError)
                throw new PortAudioException(ec, "Error starting PortAudio Stream.\nError Code: " + ec.ToString());
        }

        /// <summary>
        /// Terminates audio processing. It waits until all pending
        /// audio buffers have been played before it returns.
        /// </summary>
        public void Stop()
        {
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_StopStream(streamPtr);
            if (ec != ErrorCode.NoError)
                if (ec == ErrorCode.TimedOut)
                    throw new PortAudioException(ec, "Unable to stop PortAudio stream due to an active callback loop.\n" +
                        "A StreamCallbackResult must be set to 'Complete' or 'Abort' before a stream can be stopped.\n" +
                        "Error Code: " + ec.ToString());
                else
                    throw new PortAudioException(ec, "Error stopping PortAudio Stream.\nError Code: " + ec.ToString());
        }

        /// <summary>
        /// Terminates audio processing immediately without waiting for pending
        /// buffers to complete.
        /// </summary>
        public void Abort()
        {
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_AbortStream(streamPtr);
            if (ec != ErrorCode.NoError)
                throw new PortAudioException(ec, "Error aborting PortAudio Stream.\nError Code: " + ec.ToString());
        }

        #region ReadInput
        /// <summary>
        /// Reads the audio input when the stream is opened and processing.
        /// The stream must be started with `Pa_StartStream()` before using this.
        /// 
        /// @param buffer The audio input buffer that will be read.
        /// 
        /// @param frames The number of frames in the audio input buffer.
        /// </summary>
        public void ReadInput(Span<byte> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_ReadStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error reading PortAudio Input Stream.\nError Code: " + ec.ToString());
            }
        }
        public void ReadInput(Span<short> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_ReadStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error reading PortAudio Input Stream.\nError Code: " + ec.ToString());
            }
        }
        public void ReadInput(Span<int> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_ReadStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error reading PortAudio Input Stream.\nError Code: " + ec.ToString());
            }
        }
        public void ReadInput(Span<long> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_ReadStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error reading PortAudio Input Stream.\nError Code: " + ec.ToString());
            }
        }
        public void ReadInput(Span<Int128> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_ReadStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error reading PortAudio Input Stream.\nError Code: " + ec.ToString());
            }
        }
        public void ReadInput(Span<Half> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_ReadStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error reading PortAudio Input Stream.\nError Code: " + ec.ToString());
            }
        }
        public void ReadInput(Span<float> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_ReadStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error reading PortAudio Input Stream.\nError Code: " + ec.ToString());
            }
        }
        public void ReadInput(Span<double> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_ReadStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error reading PortAudio Input Stream.\nError Code: " + ec.ToString());
            }
        }
        public void ReadInput(Span<decimal> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_ReadStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error reading PortAudio Input Stream.\nError Code: " + ec.ToString());
            }
        }
        #endregion

        #region WriteOutput
        /// <summary>
        /// Writes the audio output when the stream is opened and processing.
        /// The stream must be started with `Pa_StartStream()` before using this.
        /// 
        /// @param buffer The audio output buffer that will be written.
        /// 
        /// @param frames The number of frames in the audio output buffer.
        /// </summary>
        public void WriteOutput(Span<byte> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_WriteStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error writing PortAudio Output Stream.\nError Code: " + ec.ToString());
            }
        }
        public void WriteOutput(Span<short> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_WriteStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error writing PortAudio Output Stream.\nError Code: " + ec.ToString());
            }
        }
        public void WriteOutput(Span<int> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_WriteStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error writing PortAudio Output Stream.\nError Code: " + ec.ToString());
            }
        }
        public void WriteOutput(Span<long> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_WriteStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error writing PortAudio Output Stream.\nError Code: " + ec.ToString());
            }
        }
        public void WriteOutput(Span<Int128> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_WriteStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error writing PortAudio Output Stream.\nError Code: " + ec.ToString());
            }
        }
        public void WriteOutput(Span<Half> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_WriteStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error writing PortAudio Output Stream.\nError Code: " + ec.ToString());
            }
        }
        public void WriteOutput(Span<float> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_WriteStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error writing PortAudio Output Stream.\nError Code: " + ec.ToString());
            }
        }
        public void WriteOutput(Span<double> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_WriteStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error writing PortAudio Output Stream.\nError Code: " + ec.ToString());
            }
        }
        public void WriteOutput(Span<decimal> buffer, ulong frames)
        {
            nint buffPtr;
            unsafe
            {
                buffPtr = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
            }
            ErrorCode ec = (ErrorCode)Native.Stream.Pa_WriteStream(streamPtr, buffPtr, frames);
            if (ec != ErrorCode.NoError)
            {
                throw new PortAudioException(ec, "Error writing PortAudio Output Stream.\nError Code: " + ec.ToString());
            }
        }
        #endregion

        #endregion // Operations

        #region Properties
        /// <summary>
        /// Determine whether the stream is stopped.
        /// A stream is considered to be stopped prior to a successful call to
        /// Pa_StartStream and after a successful call to Pa_StopStream or Pa_AbortStream.
        /// If a stream callback returns a value other than paContinue the stream is NOT
        /// considered to be stopped.
        ///
        /// @return Returns one (1) when the stream is stopped, zero (0) when
        /// the stream is running or, a PaErrorCode (which are always negative) if
        /// PortAudio is not initialized or an error is encountered.
        ///
        /// @see Pa_StopStream, Pa_AbortStream, Pa_IsStreamActive
        /// </summary>
        public bool IsStopped
        {
            get
            {
                ErrorCode ec = (ErrorCode)Native.Stream.Pa_IsStreamStopped(streamPtr);

                // Yes, No, or wat?
                if ((int)ec == 1)
                    return true;
                else if ((int)ec == 0)
                    return false;
                else
                    throw new PortAudioException(ec, "Error checking if PortAudio Stream is stopped");
            }
        }

        /// <summary>
        /// Determine whether the stream is active.
        /// A stream is active after a successful call to Pa_StartStream(), until it
        /// becomes inactive either as a result of a call to Pa_StopStream() or
        /// Pa_AbortStream(), or as a result of a return value other than paContinue from
        /// the stream callback. In the latter case, the stream is considered inactive
        /// after the last buffer has finished playing.
        ///
        /// @return Returns one (1) when the stream is active (ie playing or recording
        /// audio), zero (0) when not playing or, a PaErrorCode (which are always negative)
        /// if PortAudio is not initialized or an error is encountered.
        ///
        /// @see Pa_StopStream, Pa_AbortStream, Pa_IsStreamStopped
        /// </summary>
        public bool IsActive
        {
            get
            {
                ErrorCode ec = (ErrorCode)Native.Stream.Pa_IsStreamActive(streamPtr);

                // Yes, No, or wat?
                if ((int)ec == 1)
                    return true;
                else if ((int)ec == 0)
                    return false;
                else
                    throw new PortAudioException(ec, "Error checking if PortAudio Stream is active");
            }
        }

        /// <summary>
        /// Retrieve CPU usage information for the specified stream.
        /// The "CPU Load" is a fraction of total CPU time consumed by a callback stream's
        /// audio processing routines including, but not limited to the client supplied
        /// stream callback. This function does not work with blocking read/write streams.
        ///
        /// This function may be called from the stream callback function or the
        /// application.
        ///
        /// @return
        /// A floating point value, typically between 0.0 and 1.0, where 1.0 indicates
        /// that the stream callback is consuming the maximum number of CPU cycles possible
        /// to maintain real-time operation. A value of 0.5 would imply that PortAudio and
        /// the stream callback was consuming roughly 50% of the available CPU time. The
        /// return value may exceed 1.0. A value of 0.0 will always be returned for a
        /// blocking read/write stream, or if an error occurs.
        /// </summary>
        public double CpuLoad
        {
            get => Native.Stream.Pa_GetStreamCpuLoad(streamPtr);
        }
        #endregion Properties

        #region Programmer Friendly Callbacks
        /// <summary>
        /// Functions of type PaStreamCallback are implemented by PortAudio clients.
        /// They consume, process or generate audio in response to requests from an
        /// active PortAudio stream.
        ///
        /// When a stream is running, PortAudio calls the stream callback periodically.
        /// The callback function is responsible for processing buffers of audio samples
        /// passed via the input and output parameters.
        ///
        /// The PortAudio stream callback runs at very high or real-time priority.
        /// It is required to consistently meet its time deadlines. Do not allocate
        /// memory, access the file system, call library functions or call other functions
        /// from the stream callback that may block or take an unpredictable amount of
        /// time to complete.
        ///
        /// In order for a stream to maintain glitch-free operation the callback
        /// must consume and return audio data faster than it is recorded and/or
        /// played. PortAudio anticipates that each callback invocation may execute for
        /// a duration approaching the duration of frameCount audio frames at the stream
        /// sample rate. It is reasonable to expect to be able to utilise 70% or more of
        /// the available CPU time in the PortAudio callback. However, due to buffer size
        /// adaption and other factors, not all host APIs are able to guarantee audio
        /// stability under heavy CPU load with arbitrary fixed callback buffer sizes.
        /// When high callback CPU utilisation is required the most robust behavior
        /// can be achieved by using paFramesPerBufferUnspecified as the
        /// Pa_OpenStream() framesPerBuffer parameter.
        ///
        /// @param input and @param output are either arrays of interleaved samples or;
        /// if non-interleaved samples were requested using the paNonInterleaved sample
        /// format flag, an array of buffer pointers, one non-interleaved buffer for
        /// each channel.
        ///
        /// The format, packing and number of channels used by the buffers are
        /// determined by parameters to Pa_OpenStream().
        ///
        /// @param frameCount The number of sample frames to be processed by
        /// the stream callback.
        ///
        /// @param timeInfo Timestamps indicating the ADC capture time of the first sample
        /// in the input buffer, the DAC output time of the first sample in the output buffer
        /// and the time the callback was invoked.
        /// See PaStreamCallbackTimeInfo and Pa_GetStreamTime()
        ///
        /// @param statusFlags Flags indicating whether input and/or output buffers
        /// have been inserted or will be dropped to overcome underflow or overflow
        /// conditions.
        ///
        /// @param userData The value of a user supplied pointer passed to
        /// Pa_OpenStream() intended for storing synthesis data etc.
        ///   NOTE: In the implementing callback, you can use the `GetUserData()` method to
        ///         retrive the actual object.
        ///
        /// @return
        /// The stream callback should return one of the values in the
        /// ::PaStreamCallbackResult enumeration. To ensure that the callback continues
        /// to be called, it should return paContinue (0). Either paComplete or paAbort
        /// can be returned to finish stream processing, after either of these values is
        /// returned the callback will not be called again. If paAbort is returned the
        /// stream will finish as soon as possible. If paComplete is returned, the stream
        /// will continue until all buffers generated by the callback have been played.
        /// This may be useful in applications such as soundfile players where a specific
        /// duration of output is required. However, it is not necessary to utilize this
        /// mechanism as Pa_StopStream(), Pa_AbortStream() or Pa_CloseStream() can also
        /// be used to stop the stream. The callback must always fill the entire output
        /// buffer irrespective of its return value.
        ///
        /// @see Pa_OpenStream, Pa_OpenDefaultStream
        ///
        /// @note With the exception of Pa_GetStreamCpuLoad() it is not permissible to call
        /// PortAudio API functions from within the stream callback.
        /// </summary>
        public delegate StreamCallbackResult Callback(
            nint input, nint output,                // Originally `const void *, void *`
            uint frameCount,
            ref StreamCallbackTimeInfo timeInfo,        // Originally `const PaStreamCallbackTimeInfo*`
            StreamCallbackFlags statusFlags,
            nint userDataPtr                          // Orignially `void *`
        );

        /// <summary>
        /// Functions of type PaStreamFinishedCallback are implemented by PortAudio
        /// clients. They can be registered with a stream using the Pa_SetStreamFinishedCallback
        /// function. Once registered they are called when the stream becomes inactive
        /// (ie once a call to Pa_StopStream() will not block).
        /// A stream will become inactive after the stream callback returns non-zero,
        /// or when Pa_StopStream or Pa_AbortStream is called. For a stream providing audio
        /// output, if the stream callback returns paComplete, or Pa_StopStream() is called,
        /// the stream finished callback will not be called until all generated sample data
        /// has been played.
        ///
        /// @param userData The userData parameter supplied to Pa_OpenStream()
        ///   NOTE: In the implementing callback, you can use the `GetUserData()` method to
        ///         retrive the actual object.
        ///
        /// @see Pa_SetStreamFinishedCallback
        /// </summary>
        public delegate void FinishedCallback(
            nint userDataPtr                          // Originally `void *`
        );
        #endregion // Callbacks

        /// <summary>
        /// This function will retrieve the `userData` of the stream from it's pointer.
        ///
        /// This is meant to be used by the callbacks for `Callback` and `FinishedCallback`, and
        /// their `userDataPtr`.
        /// </summary>
        /// <param name="userDataPtr"></param>
        /// <typeparam name="UD">The type of data that was put into the stream</typeparam>
        /// <returns></returns>
        public UD GetUserData<UD>(nint userDataPtr)
        {
            UDHandle = GCHandle.FromIntPtr(userDataPtr);
            return (UD)GCHandle.FromIntPtr(userDataPtr).Target!;
        }
        internal GCHandle UDHandle
        {
            get; private set;
        }

        /// <summary>
        /// This is an internal structure to aid with C# Callbacks that interface with P/Invoke functions.
        ///
        /// The constructor, the `Free()` method, and the `Ptr` property are all that you can use, and are
        /// the most important parts.
        /// </summary>
        /// <typeparam name="CB">Callback</typeparam>
        private class _NativeInterfacingCallback<CB>
            where CB : Delegate
        {
            /// <summary>
            /// The callback itself (needs to be a delegate)
            /// </summary>
            private CB callback;

            /// <summary>
            /// GC Handle to the callback
            /// </summary>
            private GCHandle handle;

            /// <summary>
            /// Get the pointer to where the function/delegate lives in memory
            /// </summary>
            public nint Ptr { get; private set; } = nint.Zero;

            /// <summary>
            /// Setup the data structure.
            ///
            /// When done with it, don't forget to call the Free() method.
            /// </summary>
            /// <param name="cb"></param>
            public _NativeInterfacingCallback(CB cb)
            {
                callback = cb ?? throw new ArgumentNullException(nameof(cb));
                handle = GCHandle.Alloc(cb);
                Ptr = Marshal.GetFunctionPointerForDelegate<CB>(cb);
            }

            /// <summary>
            /// Manually clean up memory
            /// </summary>
            public void Free()
            {
                handle.Free();
            }
        }
    }
}
