using System;
using System.Runtime.InteropServices;
using Kermalis.VGMusicStudio.Core.Formats;

namespace PortAudio;

public enum CallbackState
{
    Stop,
    Play,
    Pause
}

public class PortAudioPlayer
{
    public static CallbackState? CallbackState { get; protected set; }
    public float Volume = 1;
    private bool _isDisposed = true;
    private bool _isDisposing = false;
    private static int _readPos = 0;
    private readonly int _samplesPerBuffer;
    private int? _prevBufferNum = 0;
    private byte[]? _prevBuffer1;
    private byte[]? _prevBuffer2;
    private byte[]? _prevBuffer3;
    private byte[]? _prevBuffer4;
    private StreamParameters _oParams;
    public StreamParameters DefaultOutputParams { get; private set; }
    public HostApiInfo HostApiInfo { get; private set; }
    private readonly Stream _stream;


    internal PortAudioPlayer(SampleFormat sampleFormat, int samplesPerBuffer, Wave waveData)
    {
        _isDisposed = false;

        Pa.Initialize();

        // Try setting up an output device
        _oParams.Device = Pa.DefaultOutputDevice;
        if (_oParams.Device == Pa.NoDevice)
        {
            throw new Exception("No default audio output device is available.");
        }

        _oParams.Channels = 2;
        _oParams.SampleFormat = sampleFormat;
        _oParams.SuggestedLatency = Pa.GetDeviceInfo(_oParams.Device).defaultLowOutputLatency;
        _oParams.HostApiSpecificStreamInfo = IntPtr.Zero;

        // Set it as the default
        DefaultOutputParams = _oParams;

        _samplesPerBuffer = samplesPerBuffer;

        _stream = new Stream(
            null,
            _oParams,
            waveData!.SampleRate,
            (uint)samplesPerBuffer,
            StreamFlags.NoFlag,
            PlayCallback,
            waveData
        );

        HostApiInfo = Pa.GetHostApiInfo(Pa.DefaultHostApi);
    }

    public void Play()
    {
        CallbackState = PortAudio.CallbackState.Play;
        _stream!.Start();
    }

    public void Stop()
    {
        CallbackState = PortAudio.CallbackState.Stop;
        _stream!.Stop();
    }

    private static Span<sbyte> CastBytesToSBytes(Span<byte> byteMem)
    {
        return MemoryMarshal.Cast<byte, sbyte>(byteMem);
    }

    private static Span<short> CastBytesToShorts(Span<byte> byteMem)
    {
        return MemoryMarshal.Cast<byte, short>(byteMem);
    }

    private static Span<int> CastBytesToInts(Span<byte> byteMem)
    {
        return MemoryMarshal.Cast<byte, int>(byteMem);
    }

    private static Span<Int24> CastBytesToInt24s(Span<byte> byteMem)
    {
        return MemoryMarshal.Cast<byte, Int24>(byteMem);
    }

    private static Span<float> CastBytesToFloats(Span<byte> byteMem)
    {
        return MemoryMarshal.Cast<byte, float>(byteMem);
    }

    internal StreamCallbackResult PlayCallback(
        nint input, nint output,
        uint frameCount,
        ref StreamCallbackTimeInfo timeInfo,
        StreamCallbackFlags statusFlags,
        nint userData
    )
    {
        // Marshal.AllocHGlobal() or any related functions cannot and must not be used
        // in this callback, otherwise it will cause an OutOfMemoryException.
        //
        // The memory is already allocated by the output and userData params by
        // the PortAudio bindings.

        if (_stream is null)
        {
            _readPos = 0;
            return StreamCallbackResult.Abort;
        }

        Wave d = _stream!.GetUserData<Wave>(userData);
        
        if (d.Buffer is null)
        {
            _readPos = 0;
            return StreamCallbackResult.Continue;
        }

        if (!_stream.UDHandle.IsAllocated)
        {
            _readPos = 0;
            return StreamCallbackResult.Abort;
        }

        if (_prevBuffer1 is not null || _prevBuffer2 is not null || _prevBuffer3 is not null || _prevBuffer4 is not null)
        {
            if (d.Buffer.CompareTo(_prevBuffer1) is 0 && d.Buffer.CompareTo(_prevBuffer2) is 0 && d.Buffer.CompareTo(_prevBuffer3) is 0 && d.Buffer.CompareTo(_prevBuffer4) is 0)
            {
                CallbackState = PortAudio.CallbackState.Pause;
            }
            else
            {
                CallbackState = PortAudio.CallbackState.Play;
            }
        }
        else
        {
            _prevBuffer1 = new byte[d.Buffer.Length];
            _prevBuffer2 = new byte[d.Buffer.Length];
            _prevBuffer3 = new byte[d.Buffer.Length];
            _prevBuffer4 = new byte[d.Buffer.Length];
        }
        // RealignBufferPos(d);

        Option1(d, output, frameCount);

        while (d.BufferState is BufferState.Writing)
        {
            CallbackState = PortAudio.CallbackState.Pause;
            var allocatedReadPos = _readPos + frameCount;
            var allocatedWritePos = d.WritePosition + _samplesPerBuffer;
            if (allocatedReadPos >= d.WritePosition && allocatedReadPos < allocatedWritePos)
            {
                if ((_readPos + _samplesPerBuffer) > d.BufferLength)
                {
                    _readPos = d.WritePosition - _samplesPerBuffer;
                }
                else
                {
                    _readPos = d.WritePosition + _samplesPerBuffer;
                }
            }
        }

        CallbackState = PortAudio.CallbackState.Play;

        if (_prevBufferNum is 0)
        {
            d.Buffer.CopyTo(_prevBuffer1!, 0);
            _prevBufferNum = 1;
        }
        else if (_prevBufferNum is 1)
        {
            d.Buffer.CopyTo(_prevBuffer2!, 0);
            _prevBufferNum = 2;
        }
        else if (_prevBufferNum is 2)
        {
            d.Buffer.CopyTo(_prevBuffer3!, 0);
            _prevBufferNum = 3;
        }
        else if (_prevBufferNum is 3)
        {
            d.Buffer.CopyTo(_prevBuffer4!, 0);
            _prevBufferNum = 0;
        }

        if (!_isDisposing)
        {
            // Continue if the mixer isn't being disposed
            return StreamCallbackResult.Continue;
        }
        else
        {
            // Complete the callback if the mixer is being disposed
            d.ResetBuffer();
            _readPos = 0;
            _isDisposing = false;
            return StreamCallbackResult.Complete;
        }
    }

    private void Option1(Wave d, nint output, uint frameCount)
    {

        switch (_oParams.SampleFormat)
        {
            case SampleFormat.UInt8:
                {
                    Span<byte> buffer;
                    unsafe
                    {
                        // Apply buffer value
                        buffer = new Span<byte>((byte*)output, (int)(frameCount * 2));
                    }

                    _readPos %= d.Buffer!.Length;

                    // If we're reading data, play it back
                    if (CallbackState == PortAudio.CallbackState.Play)
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            if (_readPos + i >= buffer.Length)
                            {
                                break;
                            }
                            buffer[i] = (byte)(d.Buffer[_readPos + i] * Volume);
                        }
                    }
                    else
                    {
                        buffer.Clear();
                    }

                    _readPos += buffer.Length;

                    if (_readPos >= d.Buffer.Length)
                    {
                        _readPos = 0;
                    }

                    if (_isDisposing)
                    {
                        buffer.Clear();
                    }

                    break;
                }
            case SampleFormat.Int8:
                {
                    Span<sbyte> buffer;
                    Span<sbyte> waveBuffer = CastBytesToSBytes(d.Buffer);
                    unsafe
                    {
                        // Apply buffer value
                        buffer = new Span<sbyte>((sbyte*)output, (int)(frameCount * 2));
                    }

                    _readPos %= waveBuffer.Length;

                    // If we're reading data, play it back
                    if (CallbackState == PortAudio.CallbackState.Play)
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            if (_readPos + i >= waveBuffer.Length)
                            {
                                break;
                            }
                            buffer[i] = (sbyte)(waveBuffer[_readPos + i] * Volume);
                        }
                    }
                    else
                    {
                        buffer.Clear();
                    }

                    _readPos += buffer.Length;

                    if (_readPos >= waveBuffer.Length)
                    {
                        _readPos = 0;
                    }

                    if (_isDisposing)
                    {
                        buffer.Clear();
                    }

                    break;
                }
            case SampleFormat.Int16:
                {
                    Span<short> buffer;
                    Span<short> waveBuffer = CastBytesToShorts(d.Buffer);
                    unsafe
                    {
                        // Apply buffer value
                        buffer = new Span<short>((short*)output, (int)(frameCount * 2));
                    }

                    _readPos %= waveBuffer.Length;

                    // If we're reading data, play it back
                    if (CallbackState == PortAudio.CallbackState.Play)
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            if (_readPos + i >= waveBuffer.Length)
                            {
                                break;
                            }
                            buffer[i] = (short)(waveBuffer[_readPos + i] * Volume);
                        }
                    }
                    else
                    {
                        buffer.Clear();
                    }

                    _readPos += buffer.Length;

                    if (_readPos >= waveBuffer.Length)
                    {
                        _readPos = 0;
                    }

                    if (_isDisposing)
                    {
                        buffer.Clear();
                    }

                    break;
                }
            case SampleFormat.Int24:
                {
                    Span<Int24> buffer;
                    Span<Int24> waveBuffer = CastBytesToInt24s(d.Buffer);
                    unsafe
                    {
                        // Apply buffer value
                        buffer = new Span<Int24>((Int24*)output, (int)(frameCount * 2));
                    }

                    _readPos %= waveBuffer.Length;

                    // If we're reading data, play it back
                    if (CallbackState == PortAudio.CallbackState.Play)
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            if (_readPos + i >= waveBuffer.Length)
                            {
                                break;
                            }
                            buffer[i] = (Int24)(waveBuffer[_readPos + i] * Volume);
                        }
                    }
                    else
                    {
                        buffer.Clear();
                    }

                    _readPos += buffer.Length;

                    if (_readPos >= waveBuffer.Length)
                    {
                        _readPos = 0;
                    }

                    if (_isDisposing)
                    {
                        buffer.Clear();
                    }

                    break;
                }
            case SampleFormat.Int32:
                {
                    Span<int> buffer;
                    Span<int> waveBuffer = CastBytesToInts(d.Buffer);
                    unsafe
                    {
                        // Apply buffer value
                        buffer = new Span<int>((int*)output, (int)(frameCount * 2));
                    }

                    _readPos %= waveBuffer.Length;

                    // If we're reading data, play it back
                    if (CallbackState == PortAudio.CallbackState.Play)
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            if (_readPos + i >= waveBuffer.Length)
                            {
                                break;
                            }
                            buffer[i] = (int)(waveBuffer[_readPos + i] * Volume);
                        }
                    }
                    else
                    {
                        buffer.Clear();
                    }

                    _readPos += buffer.Length;

                    if (_readPos >= waveBuffer.Length)
                    {
                        _readPos = 0;
                    }

                    if (_isDisposing)
                    {
                        buffer.Clear();
                    }

                    break;
                }
            case SampleFormat.Float32:
                {
                    Span<float> buffer;
                    Span<float> waveBuffer = CastBytesToFloats(d.Buffer);
                    unsafe
                    {
                        // Apply buffer value
                        buffer = new Span<float>((float*)output, (int)(frameCount * 2));
                    }

                    _readPos %= waveBuffer.Length;

                    // If we're reading data, play it back
                    if (CallbackState == PortAudio.CallbackState.Play)
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            if (_readPos + i >= waveBuffer.Length)
                            {
                                break;
                            }
                            buffer[i] = (float)(waveBuffer[_readPos + i] * Volume);
                        }
                    }
                    else
                    {
                        buffer.Clear();
                    }

                    _readPos += buffer.Length;

                    if (_readPos >= waveBuffer.Length)
                    {
                        _readPos = 0;
                    }

                    if (_isDisposing)
                    {
                        buffer.Clear();
                    }

                    break;
                }
        }
    }

    internal void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposing = true;
            _stream.Dispose();
            Pa.Terminate();
        }
        _isDisposed = true;
    }

    // // Experimental realignment func to prevent reading from buffers being written to
    // protected static void RealignBufferPos(Wave waveData)
    // {
    // 	var count = waveData.Count / 4;
    // 	var writePos = waveData.WritePosition / 4;

    // 	if (writePos - count < 0)
    // 	{
    // 		if (ReadPos.Equals((writePos - count + (waveData.BufferLength / 4))..^(waveData.BufferLength / 4)))
    // 		{
    // 			if (ReadPos < writePos)
    // 			{
    // 				ReadPos -= count;
    // 				if (ReadPos <= 0)
    // 				{
    // 					ReadPos += waveData.BufferLength / 4;
    // 				}
    // 			}
    // 			else
    // 			{
    // 				ReadPos += count;
    // 				if (ReadPos + count >= (waveData.BufferLength / 4))
    // 				{
    // 					ReadPos -= waveData.BufferLength / 4;
    // 				}
    // 			}
    // 		}
    // 		else if (ReadPos.Equals(writePos..^(writePos + count)))
    // 		{
    // 			if (ReadPos < writePos)
    // 			{
    // 				ReadPos -= count;
    // 				if (ReadPos <= 0)
    // 				{
    // 					ReadPos += waveData.BufferLength / 4;
    // 				}
    // 			}
    // 			else
    // 			{
    // 				ReadPos += count;
    // 				if (ReadPos + count >= (waveData.BufferLength / 4))
    // 				{
    // 					ReadPos -= waveData.BufferLength / 4;
    // 				}
    // 			}
    // 		}
    // 	}
    // 	if (writePos > count && writePos < (waveData.BufferLength / 4))
    // 	{
    // 		if (ReadPos.Equals((writePos - count)..^(writePos + count)))
    // 		{
    // 			if (ReadPos < writePos)
    // 			{
    // 				ReadPos -= count;
    // 				if (ReadPos <= 0)
    // 				{
    // 					ReadPos += waveData.BufferLength / 4;
    // 				}
    // 			}
    // 			else
    // 			{
    // 				ReadPos += count;
    // 				if (ReadPos + count >= (waveData.BufferLength / 4))
    // 				{
    // 					ReadPos -= waveData.BufferLength / 4;
    // 				}
    // 			}
    // 		}
    // 	}
    // }
}
