using System;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

// All of this was written in C++ by ipatix; I just converted it
internal class MP2KReverb
{
    protected readonly float[] _reverbBuffer;
    protected float _intensity;
    protected int _bufferPos, _bufferPos2;

    internal MP2KReverb(byte intensity, int streamRate, byte numAgbBuffers)
    {
        _reverbBuffer = new float[streamRate / GBAUtils.AGB_APPROX_FPS * numAgbBuffers];
        SetLevel(intensity);
        int bufferLen = streamRate / GBAUtils.AGB_APPROX_FPS;
        _bufferPos = 0;
        _bufferPos2 = bufferLen;
    }

    internal void Process(Span<float> buffer, int samplesPerBuffer)
    {
        while (buffer.Length > 0)
        {
            // TODO change the semantics of ProcessInternal to return 'processed' instead of 'left' samples
            int left = ProcessInternal(buffer);
            buffer = buffer[^left..];
        }
    }

    internal void SetLevel(byte level)
    {
        _intensity = level / 128.0f;
    }

    protected virtual void Reset()
    {
        Array.Fill(_reverbBuffer, 0f, 0, _reverbBuffer.Length);
    }

    public static MP2KReverb MakeReverb(ReverbType reverbType, byte intensity, int sampleRate, byte numDmaBuffers)
    {
        return reverbType switch
        {
            ReverbType.Normal => new MP2KReverb(intensity, sampleRate, numDmaBuffers),
            ReverbType.None => new MP2KReverb(0, sampleRate, numDmaBuffers),
            ReverbType.Camelot1 => new Camelot1(intensity, sampleRate, numDmaBuffers),
            ReverbType.Camelot2 => new Camelot2(intensity, sampleRate, numDmaBuffers, 0.4140625f, -0.0625f),
            // Mario Power Tennis uses same coefficients as Mario Golf Advance Tour
            ReverbType.MGAT => new Camelot2(intensity, sampleRate, numDmaBuffers, 0.25f, -0.046875f),
            _ => throw new Exception($"MakeReverb: Invalid Reverb Effect: {(int)reverbType}"),
        };
    }

    protected virtual int ProcessInternal(Span<float> buffer)
    {
        Span<float> rbuf = _reverbBuffer;
        int count = Math.Min(Math.Min(_reverbBuffer.Length - _bufferPos2, _reverbBuffer.Length - _bufferPos), buffer.Length);
        bool reset = false, reset2 = false;
        if (_reverbBuffer.Length - _bufferPos == count)
        {
            reset = true;
        }
        if (_reverbBuffer.Length - _bufferPos2 == count)
        {
            reset2 = true;
        }
        for (int i = 0; i < count; i += 2)
        {
            float rev =
                (rbuf[_bufferPos] + rbuf[_bufferPos + 1] + rbuf[_bufferPos2] + rbuf[_bufferPos2 + 1]) * _intensity
                * (1.0f / 4.0f);
            rbuf[_bufferPos] = buffer[i] += rev;
            rbuf[_bufferPos + 1] = buffer[i + 1] += rev;
            _bufferPos += 2;
            _bufferPos2 += 2;
        }
        if (reset2)
        {
            _bufferPos2 = 0;
        }

        if (reset)
        {
            _bufferPos = 0;
        }

        return buffer.Length - count;
    }

    internal class Camelot1 : MP2KReverb
    {
        protected float[] _gsBuffer;

        internal Camelot1(byte intensity, int streamRate, byte numAgbBuffers)
            : base(intensity, streamRate, numAgbBuffers)
        {
            _gsBuffer = new float[streamRate / GBAUtils.AGB_APPROX_FPS];
            _bufferPos2 = 0;
        }

        protected override void Reset()
        {
            base.Reset();
            Array.Fill(_gsBuffer, 0f);
        }

        protected override int ProcessInternal(Span<float> buffer)
        {
            Span<float> rbuf = _reverbBuffer;
            int count = Math.Min(Math.Min(_reverbBuffer.Length - _bufferPos, _gsBuffer.Length - _bufferPos2), buffer.Length);
            bool reset = false, resetGS = false;

            if (count == _reverbBuffer.Length - _bufferPos)
            {
                reset = true;
            }

            if (count == _gsBuffer.Length - _bufferPos2)
            {
                resetGS = true;
            }

            for (int i = 0; i < count; i += 2)
            {
                float mixL = buffer[i] + _gsBuffer[_bufferPos2];
                float mixR = buffer[i + 1] + _gsBuffer[_bufferPos2 + 1];

                float lA = rbuf[_bufferPos];
                float rA = rbuf[_bufferPos + 1];

                buffer[i] = rbuf[_bufferPos] = mixL;
                buffer[i + 1] = rbuf[_bufferPos + 1] = mixR;

                float lRMix = 0.25f * mixL + 0.25f * rA;
                float rRMix = 0.25f * mixR + 0.25f * lA;

                _gsBuffer[_bufferPos2] = lRMix;
                _gsBuffer[_bufferPos2 + 1] = rRMix;

                _bufferPos += 2;
                _bufferPos2 += 2;
            }

            if (resetGS)
            {
                _bufferPos2 = 0;
            }

            if (reset)
            {
                _bufferPos = 0;
            }

            return buffer.Length - count;
        }
    }


    internal class Camelot2 : MP2KReverb
    {
        protected float[] _gs2Buffer;
        protected int _gs2Pos;
        protected float _rPrimFac, _rSecFac;


        internal Camelot2(byte intensity, int streamRate, byte numAgbBuffers, float rPrimFac, float rSecFac)
            : base(intensity, streamRate, numAgbBuffers)
        {
            _gs2Buffer = new float[streamRate / GBAUtils.AGB_APPROX_FPS];
            _gs2Pos = 0;
            _rPrimFac = rPrimFac;
            _rSecFac = rSecFac;

            // equivalent to the offset of -0xB0 samples for a 0x210 buffer size
            _bufferPos2 = _reverbBuffer.Length - (_gs2Buffer.Length / 3);
        }

        protected override void Reset()
        {
            base.Reset();
            Array.Fill(_gs2Buffer, 0f);
        }

        protected override int ProcessInternal(Span<float> buffer)
        {
            Span<float> rbuf = _reverbBuffer;
            int count = Math.Min(
                Math.Min(_reverbBuffer.Length - _bufferPos2, _reverbBuffer.Length - _bufferPos),
                Math.Min(buffer.Length, _gs2Buffer.Length - _gs2Pos)
            );
            bool reset = false, reset2 = false, resetgs2 = false;

            if (_reverbBuffer.Length - _bufferPos2 == count)
            {
                reset2 = true;
            }
            if (_reverbBuffer.Length - _bufferPos == count)
            {
                reset = true;
            }
            if ((_gs2Buffer.Length / 2) - _gs2Pos == count)
            {
                resetgs2 = true;
            }

            for (int i = 0; i < count; i += 2)
            {
                float mixL = buffer[i] + _gs2Buffer[_gs2Pos];
                float mixR = buffer[i + 1] + _gs2Buffer[_gs2Pos + 1];

                float lA = rbuf[_bufferPos];
                float rA = rbuf[_bufferPos + 1];

                buffer[i] = rbuf[_bufferPos] = mixL;
                buffer[i + 1] = rbuf[_bufferPos + 1] = mixR;

                float lRMix = lA * _rPrimFac + rA * _rSecFac;
                float rRMix = rA * _rPrimFac + lA * _rSecFac;

                float lB = rbuf[_bufferPos2 + 1] * 0.25f;
                float rB = mixR * 0.25f;

                _gs2Buffer[_gs2Pos] = lRMix + lB;
                _gs2Buffer[_gs2Pos + 1] = rRMix + rB;

                _bufferPos += 2;
                _bufferPos2 += 2;
                _gs2Pos++;
            }
            if (reset2)
            {
                _bufferPos2 = 0;
            }

            if (reset)
            {
                _bufferPos = 0;
            }

            if (resetgs2)
            {
                _gs2Pos = 0;
            }

            return buffer.Length - count;
        }
    }
}
