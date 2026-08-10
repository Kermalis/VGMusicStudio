using Kermalis.EndianBinaryIO;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Kermalis.VGMusicStudio.Core.Formats;

// Some code has been based on NAudio's BufferedWaveProvider and CircularBuffer
// Sources:
//      https://github.com/naudio/NAudio/blob/master/NAudio.Core/Wave/WaveProviders/BufferedWaveProvider.cs
//      https://github.com/naudio/NAudio/blob/master/NAudio.Core/Utils/CircularBuffer.cs
// NAudio License (MIT) - https://github.com/naudio/NAudio/blob/master/license.txt


public enum BufferState
{
    Idle,
    Reading,
    Writing
}

public class Wave
{
    public string? FileName;
    public ushort Channels;
    public uint SampleRate;
    public ushort BitsPerSample;
    public ushort ExtraSize;
    public ushort BlockAlign;
    public uint AverageBytesPerSecond;
    public bool IsLooped = false;
    public uint LoopStart;
    public uint LoopEnd;

    public BufferState BufferState { get; protected set; }

    public bool DiscardOnBufferOverflow { get; set; }
    public int BufferLength;

    public byte[]? Buffer;
    public int ReadPosition { get; private set; }
    public int WritePosition { get; private set; }
    private int ByteCount;
    private readonly object? LockObject = new();

    private long DataChunkSize;
    private long DataChunkLength;
    private long DataChunkPosition;
    private Stream? InStream;
    private Stream? OutStream;
    private EndianBinaryReader? Reader;
    private EndianBinaryWriter? Writer;

    public long Position
    {
        get
        {
            return InStream!.Position - DataChunkPosition;
        }
        set
        {
            lock (LockObject!)
            {
                value = Math.Min(value, DataChunkLength);
                // To keep it in sync
                value -= (value % BlockAlign);
                InStream!.Position = value + DataChunkPosition;
            }
        }
    }

    public int BufferedBytes
    {
        get
        {
            if (this != null)
            {
                return Count;
            }

            return 0;
        }
    }

    public int Count
    {
        get
        {
            lock (LockObject!)
            {
                return ByteCount;
            }
        }
    }

    public void CreateStream()
    {
        InStream = new MemoryStream();
        OutStream = new MemoryStream();
    }

    public void CreateInStream()
    {
        InStream = new MemoryStream();
    }

    public void CreateOutStream()
    {
        OutStream = new MemoryStream();
    }

    public void CreateFileStream(string fileName)
    {
        OutStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read);
        Writer = new EndianBinaryWriter(OutStream, ascii: true);
    }

    public Wave CreateFormat(uint sampleRate, ushort channels, ushort blockAlign, uint averageBytesPerSecond, ushort bitsPerSample, bool isLooped = false, uint loopStart = 0, uint loopEnd = 0)
    {
        Channels = channels;
        SampleRate = sampleRate;
        AverageBytesPerSecond = averageBytesPerSecond;
        BlockAlign = blockAlign;
        BitsPerSample = bitsPerSample;
        ExtraSize = 0;
        if (isLooped)
        {
            IsLooped = isLooped;
            LoopStart = loopStart;
            LoopEnd = loopEnd;
        }
        return this;
    }
    public Wave CreateIeeeFloatWave(uint sampleRate, ushort channels, ushort bits = 32, bool isLooped = false, uint loopStart = 0, uint loopEnd = 0) => CreateFormat(sampleRate, channels, (ushort)(channels * (bits / 8)), sampleRate * (ushort)(channels * (bits / 8)), bits, isLooped, loopStart, loopEnd);

    public void AddSamples(Span<byte> buffer, int offset, int count)
    {
        if (Engine.Instance!.Player.State == PlayerState.Playing)
        {
            BufferState = BufferState.Writing;
            Buffer ??= new byte[BufferLength];

            if (WriteBuffer(buffer, offset, count) < count && !DiscardOnBufferOverflow)
            {
                throw new InvalidOperationException("The buffer is full and cannot be written to.");
            }
            BufferState = BufferState.Idle;
        }
        else
        {
            Buffer ??= new byte[BufferLength];
            Array.Clear(Buffer);
        }
    }

    public int ReadBuffer(Span<byte> data, int offset, int count)
    {
        lock (LockObject!)
        {
            if (count > ByteCount)
            {
                count = ByteCount;
            }

            int num = 0;
            int readToEnd = Math.Min(Buffer!.Length - ReadPosition, count);
            var src = new Span<byte>(Buffer, ReadPosition, readToEnd);
            var dst = data.Slice(offset, readToEnd);
            src.CopyTo(dst);
            num += readToEnd;
            ReadPosition += readToEnd;
            ReadPosition %= Buffer.Length;
            if (num < count)
            {
                src = new Span<byte>(Buffer, ReadPosition, count - num);
                dst = data.Slice(offset + num, count - num);
                src.CopyTo(dst);
                ReadPosition += count - num;
                num = count;
            }

            ByteCount -= num;
            return num;
        }
    }

    public int WriteBuffer(Span<byte> data, int offset, int count)
    {
        lock (LockObject!)
        {
            int bytesWritten = 0;
            ByteCount = Buffer!.Length % count;
            if (ByteCount >= Buffer!.Length)
            {
                ByteCount = 0;
            }
            if (count > Buffer!.Length - ByteCount)
            {
                count = Buffer.Length - ByteCount;
            }

            int writeToEnd = Math.Min(Buffer.Length - WritePosition, count);
            var src = data.Slice(offset, writeToEnd);
            var dst = new Span<byte>(Buffer, WritePosition, writeToEnd);
            src.CopyTo(dst);
            WritePosition += writeToEnd;
            WritePosition %= Buffer.Length;
            bytesWritten += writeToEnd;
            if (bytesWritten < count)
            {
                Debug.Assert(WritePosition == 0);
                src = data.Slice(offset + bytesWritten, count - bytesWritten);
                dst = new Span<byte>(Buffer, WritePosition, count - bytesWritten);
                src.CopyTo(dst);
                WritePosition += count - bytesWritten;
                bytesWritten = count;
            }

            ByteCount += bytesWritten;
            return bytesWritten;
        }
    }

    public void ResetBuffer()
    {
        lock (LockObject!)
        {
            Array.Clear(Buffer!);
            ByteCount = 0;
            ReadPosition = 0;
            WritePosition = 0;
        }
    }

    public int Read(Span<byte> array, int offset, int count)
    {
        if (count % BlockAlign != 0)
        {
            throw new ArgumentException(
                $"Must read complete blocks: requested {count}, block align is {BlockAlign}");
        }
        lock (LockObject!)
        {
            // sometimes there is more junk at the end of the file past the data chunk
            if (Position + count > DataChunkLength)
            {
                count = (int)(DataChunkLength - Position);
            }
            return InStream!.Read(array.ToArray(), offset, count);
        }
    }

    public void Write(Span<byte> data, int offset, int count)
    {
        if (OutStream!.Length + count > uint.MaxValue)
        {
            throw new ArgumentException("WAV file too large", nameof(count));
        }

        OutStream.Write(data.ToArray(), offset, count);
        DataChunkSize += count;
    }

    public Span<byte> WriteBytes(Span<long> data, WaveEncoding encoding = WaveEncoding.Pcm16)
    {
        var convertedData = new byte[data.Length * 2];
        int index = 0;
        for (int i = 0; i < data.Length; i++)
        {
            convertedData[index++] = (byte)(data[i] & 0xff);
            convertedData[index++] = (byte)(data[i] >> 8);
            convertedData[index++] = (byte)(data[i] >> 16);
            convertedData[index++] = (byte)(data[i] >> 24);
            convertedData[index++] = (byte)(data[i] >> 32);
            convertedData[index++] = (byte)(data[i] >> 40);
            convertedData[index++] = (byte)(data[i] >> 48);
            convertedData[index++] = (byte)(data[i] >> 56);
        }

        return WriteBytes(convertedData, encoding);
    }

    public Span<byte> WriteBytes(Span<int> data, WaveEncoding encoding = WaveEncoding.Pcm16)
    {
        var convertedData = new byte[data.Length * 2];
        int index = 0;
        for (int i = 0; i < data.Length; i++)
        {
            convertedData[index++] = (byte)(data[i] & 0xff);
            convertedData[index++] = (byte)(data[i] >> 8);
            convertedData[index++] = (byte)(data[i] >> 16);
            convertedData[index++] = (byte)(data[i] >> 24);
        }

        return WriteBytes(convertedData, encoding);
    }

    public Span<byte> WriteBytes(Span<short> data, WaveEncoding encoding = WaveEncoding.Pcm16)
    {
        var convertedData = new byte[data.Length * 2];
        int index = 0;
        for (int i = 0; i < data.Length; i++)
        {
            convertedData[index++] = (byte)(data[i] & 0xff);
            convertedData[index++] = (byte)(data[i] >> 8);
        }

        return WriteBytes(convertedData, encoding);
    }

    public Span<byte> WriteBytes(Span<byte> data, WaveEncoding encoding = WaveEncoding.Pcm16)
    {
        // Creating the RIFF Wave header
        string fileID = "RIFF";
        uint fileSize = (uint)(data.Length + 44); // File size must match the size of the samples and header size
        string waveID = "WAVE";
        string formatID = "fmt ";
        uint formatLength = 16; // Always a length 16
        ushort formatType = (ushort)encoding; // 1 is PCM16, 2 is ADPCM, etc.
                                              // Number of channels is already manually defined
        uint sampleRate = SampleRate; // Sample Rate is read directly from the Info context
        ushort bitsPerSample = 16; // bitsPerSample must be written to AFTER numNibbles
        uint numNibbles = sampleRate * bitsPerSample * Channels / 8; // numNibbles must be written BEFORE bitsPerSample is written
        ushort bitRate = (ushort)(bitsPerSample * Channels / 8);
        string dataID = "data";
        uint dataSize = (uint)data.Length;

        byte[] samplerChunk;

        if (IsLooped)
        {
            string samplerID = "smpl";
            uint samplerSize = 0;

            uint manufacturer = 0;
            uint product = 0;
            uint samplePeriod = 0;
            uint midiUnityNote = 0;
            uint midiPitchFraction = 0;
            uint smpteFormat = 0;
            uint smpteOffset = 0;
            uint numSampleLoops = 1;
            uint samplerDataSize = 0;

            samplerSize += 36;

            if (numSampleLoops > 0)
            {
                var loopID = new uint[numSampleLoops];
                var loopType = new uint[numSampleLoops];
                var loopStart = new uint[numSampleLoops];
                var loopEnd = new uint[numSampleLoops];
                var loopFraction = new uint[numSampleLoops];
                var loopNumPlayback = new uint[numSampleLoops];

                var loopHeaderSize = 0;
                for (int i = 0; i < numSampleLoops; i++)
                {
                    loopID[i] = 0;
                    loopType[i] = 0;
                    loopStart[i] = LoopStart;
                    loopEnd[i] = LoopEnd;
                    loopFraction[i] = 0;
                    loopNumPlayback[i] = 0;

                    loopHeaderSize += 24;
                    samplerSize += 24;
                }
                var loopHeader = new byte[loopHeaderSize];
                var lw = new EndianBinaryWriter(new MemoryStream(loopHeader))
                {
                    ASCII = true
                };
                for (int i = 0; i < numSampleLoops; i++)
                {
                    lw.WriteUInt32(loopID[i]);
                    lw.WriteUInt32(loopType[i]);
                    lw.WriteUInt32(loopStart[i]);
                    lw.WriteUInt32(loopEnd[i]);
                    lw.WriteUInt32(loopFraction[i]);
                    lw.WriteUInt32(loopNumPlayback[i]);
                }
                samplerChunk = new byte[samplerSize + loopHeaderSize];

                var sw = new EndianBinaryWriter(new MemoryStream(samplerChunk))
                {
                    ASCII = true
                };
                sw.WriteChars(samplerID);
                sw.WriteUInt32(samplerSize);
                sw.WriteUInt32(manufacturer);
                sw.WriteUInt32(product);
                sw.WriteUInt32(samplePeriod);
                sw.WriteUInt32(midiUnityNote);
                sw.WriteUInt32(midiPitchFraction);
                sw.WriteUInt32(smpteFormat);
                sw.WriteUInt32(smpteOffset);
                sw.WriteUInt32(numSampleLoops);
                sw.WriteUInt32(samplerDataSize);
                sw.WriteBytes(loopHeader);

                fileSize += (uint)samplerChunk.Length;

                var waveData = new byte[fileSize];
                var w = new EndianBinaryWriter(new MemoryStream(waveData))
                {
                    ASCII = true
                };
                w.WriteChars(fileID);
                w.WriteUInt32(fileSize);
                w.WriteChars(waveID);
                w.WriteChars(formatID);
                w.WriteUInt32(formatLength);
                w.WriteUInt16(formatType);
                w.WriteUInt16(Channels);
                w.WriteUInt32(sampleRate);
                w.WriteUInt32(numNibbles);
                w.WriteUInt16(bitRate);
                w.WriteUInt16(bitsPerSample);
                w.WriteChars(dataID);
                w.WriteUInt32(dataSize);
                w.WriteBytes(data);
                w.WriteBytes(samplerChunk);

                return waveData;
            }
            else
            {
                samplerChunk = new byte[samplerSize + 8];

                var sw = new EndianBinaryWriter(new MemoryStream(samplerChunk))
                {
                    ASCII = true
                };
                sw.WriteChars(samplerID);
                sw.WriteUInt32(samplerSize);
                sw.WriteUInt32(manufacturer);
                sw.WriteUInt32(product);
                sw.WriteUInt32(samplePeriod);
                sw.WriteUInt32(midiUnityNote);
                sw.WriteUInt32(midiPitchFraction);
                sw.WriteUInt32(smpteFormat);
                sw.WriteUInt32(smpteOffset);
                sw.WriteUInt32(numSampleLoops);
                sw.WriteUInt32(samplerDataSize);

                fileSize += (uint)samplerChunk.Length;

                var waveData = new byte[fileSize];
                var w = new EndianBinaryWriter(new MemoryStream(waveData))
                {
                    ASCII = true
                };
                w.WriteChars(fileID);
                w.WriteUInt32(fileSize);
                w.WriteChars(waveID);
                w.WriteChars(formatID);
                w.WriteUInt32(formatLength);
                w.WriteUInt16(formatType);
                w.WriteUInt16(Channels);
                w.WriteUInt32(sampleRate);
                w.WriteUInt32(numNibbles);
                w.WriteUInt16(bitRate);
                w.WriteUInt16(bitsPerSample);
                w.WriteChars(dataID);
                w.WriteUInt32(dataSize);
                w.WriteBytes(data);
                w.WriteBytes(samplerChunk);

                return waveData;
            }
        }
        else
        {
            var waveData = new byte[fileSize];
            var w = new EndianBinaryWriter(new MemoryStream(waveData))
            {
                ASCII = true
            };
            w.WriteChars(fileID);
            w.WriteUInt32(fileSize);
            w.WriteChars(waveID);
            w.WriteChars(formatID);
            w.WriteUInt32(formatLength);
            w.WriteUInt16(formatType);
            w.WriteUInt16(Channels);
            w.WriteUInt32(sampleRate);
            w.WriteUInt32(numNibbles);
            w.WriteUInt16(bitRate);
            w.WriteUInt16(bitsPerSample);
            w.WriteChars(dataID);
            w.WriteUInt32(dataSize);
            w.WriteBytes(data);

            return waveData;
        }
    }
    protected virtual void WriteHeader(EndianBinaryWriter writer)
    {
        // Make sure the stream is at position 0 before writing the header
        Writer!.Stream.Position = 0;
        Writer.ASCII = true;

        // Creating the RIFF Wave headers
        writer.WriteChars("RIFF");
        writer.WriteUInt32((uint)(DataChunkSize + 44));
        writer.WriteChars("WAVE");
        writer.WriteChars("fmt ");
        writer.WriteUInt32((uint)(18 + ExtraSize));
        writer.WriteInt16((short)WaveEncoding.Pcm16);
        writer.WriteInt16((short)Channels);
        writer.WriteUInt32(SampleRate);
        writer.WriteUInt32(AverageBytesPerSecond);
        writer.WriteInt16((short)BlockAlign);
        writer.WriteInt16((short)BitsPerSample);
        writer.WriteUInt16(ExtraSize);
        writer.WriteChars("data");
        writer.WriteUInt32((uint)DataChunkSize);
    }

    internal void Dispose(bool disposing)
    {
        if (disposing && OutStream != null)
        {
            try
            {
                WriteHeader(Writer!);
            }
            finally
            {
                Writer = null!;
                OutStream.Dispose();
                OutStream = null;
            }
        }
        if (disposing && InStream != null)
        {
            InStream.Dispose();
            InStream = null;
        }
    }

    ~Wave()
    {
        Dispose(disposing: false);
    }
}
