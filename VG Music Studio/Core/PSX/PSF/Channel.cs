namespace Kermalis.VGMusicStudio.Core.PSX.PSF
{
    internal class Channel
    {
        public readonly byte Index;

        public Track Owner;
        public ushort BaseTimer = NDS.Utils.ARM7_CLOCK / 44100;
        public ushort Timer;
        public byte NoteVelocity;
        public byte Volume = 0x7F;
        public sbyte Pan = 0x40;
        public byte BaseKey, Key;
        public byte PitchTune;

        private int _pos;
        private short _prevLeft;
        private short _prevRight;

        private long _dataOffset;
        private long _loopOffset;
        private short[] _decompressedSample;

        public Channel(byte i)
        {
            Index = i;
        }

        private static readonly float[][] _idk = new float[5][]
        {
            new float[2] { 0f, 0f },
            new float[2] { 60f / 64f, 0f },
            new float[2] { 115f / 64f, 52f / 64f },
            new float[2] { 98f / 64f, 55f / 64f },
            new float[2] { 122f / 64f, 60f / 64f }
        };
        public void Start(long sampleOffset, long sampleSize, byte[] exeBuffer)
        {
            Stop();
            //State = EnvelopeState.Attack;
            //Velocity = -92544;
            _pos = 0;
            _prevLeft = _prevRight = 0;
            _loopOffset = 0;
            _dataOffset = 0;
            float prev1 = 0, prev2 = 0;
            _decompressedSample = new short[0x50000];
            for (long i = 0; i < sampleSize; i += 16)
            {
                byte b0 = exeBuffer[sampleOffset + i];
                byte b1 = exeBuffer[sampleOffset + i + 1];
                int range = b0 & 0xF;
                int filter = (b0 & 0xF0) >> 4;
                bool end = (b1 & 0x1) != 0;
                bool looping = (b1 & 0x2) != 0;
                bool loop = (b1 & 0x4) != 0;

                // Decomp
                long pi = i * 28 / 16;
                int shift = range + 16;
                for (int j = 0; j < 14; j++)
                {
                    sbyte bj = (sbyte)exeBuffer[sampleOffset + i + 2 + j];
                    _decompressedSample[pi + (j * 2)] = (short)((bj << 28) >> shift);
                    _decompressedSample[pi + (j * 2) + 1] = (short)(((bj & 0xF0) << 24) >> shift);
                }
                if (filter == 0)
                {
                    prev1 = _decompressedSample[pi + 27];
                    prev2 = _decompressedSample[pi + 26];
                }
                else
                {
                    float f1 = _idk[filter][0];
                    float f2 = _idk[filter][1];
                    float p1 = prev1;
                    float p2 = prev2;
                    for (int j = 0; j < 28; j++)
                    {
                        float t = _decompressedSample[pi + j] + (p1 * f1) - (p2 * f2);
                        _decompressedSample[pi + j] = (short)t;
                        p2 = p1;
                        p1 = t;
                    }
                    prev1 = p1;
                    prev2 = p2;
                }
            }
        }

        public void Stop()
        {
            if (Owner != null)
            {
                Owner.Channels.Remove(this);
            }
            Owner = null;
            Volume = 0;
        }

        public void Process(out short left, out short right)
        {
            if (Timer != 0)
            {
                int numSamples = (_pos + 0x100) / Timer;
                _pos = (_pos + 0x100) % Timer;
                // prevLeft and prevRight are stored because numSamples can be 0.
                for (int i = 0; i < numSamples; i++)
                {
                    short samp;
                    // If hit end
                    if (_dataOffset >= _decompressedSample.Length)
                    {
                        if (true)
                        //if (swav.DoesLoop)
                        {
                            _dataOffset = _loopOffset;
                        }
                        else
                        {
                            left = right = _prevLeft = _prevRight = 0;
                            Stop();
                            return;
                        }
                    }
                    samp = _decompressedSample[_dataOffset++];
                    samp = (short)(samp * Volume / 0x7F);
                    _prevLeft = (short)(samp * (-Pan + 0x40) / 0x80);
                    _prevRight = (short)(samp * (Pan + 0x40) / 0x80);
                }
            }
            left = _prevLeft;
            right = _prevRight;
        }
    }
}
