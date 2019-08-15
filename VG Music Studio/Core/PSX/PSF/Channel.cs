namespace Kermalis.VGMusicStudio.Core.PSX.PSF
{
    internal class Channel
    {
        public readonly byte Index;

        public Track Owner;
        public ushort BaseTimer = NDS.Utils.ARM7_CLOCK / 44100, Timer;
        public byte NoteVelocity;
        public byte Volume = 0x7F;
        public sbyte Pan = 0x40;
        public byte BaseKey, Key;
        public byte PitchTune;

        private int pos;
        private short prevLeft, prevRight;

        private long dataOffset;
        private long loopOffset;
        private short[] decompressedSample;

        public Channel(byte i)
        {
            Index = i;
        }

        private static readonly float[][] idk = new float[5][]
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
            pos = 0;
            prevLeft = prevRight = 0;
            loopOffset = 0;
            dataOffset = 0;
            float prev1 = 0, prev2 = 0;
            decompressedSample = new short[0x50000];
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
                    decompressedSample[pi + (j * 2)] = (short)((bj << 28) >> shift);
                    decompressedSample[pi + (j * 2) + 1] = (short)(((bj & 0xF0) << 24) >> shift);
                }
                if (filter == 0)
                {
                    prev1 = decompressedSample[pi + 27];
                    prev2 = decompressedSample[pi + 26];
                }
                else
                {
                    float f1 = idk[filter][0];
                    float f2 = idk[filter][1];
                    float p1 = prev1;
                    float p2 = prev2;
                    for (int j = 0; j < 28; j++)
                    {
                        float t = decompressedSample[pi + j] + (p1 * f1) - (p2 * f2);
                        decompressedSample[pi + j] = (short)t;
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
                int numSamples = (pos + 0x100) / Timer;
                pos = (pos + 0x100) % Timer;
                // prevLeft and prevRight are stored because numSamples can be 0.
                for (int i = 0; i < numSamples; i++)
                {
                    short samp;
                    // If hit end
                    if (dataOffset >= decompressedSample.Length)
                    {
                        if (true)
                        //if (swav.DoesLoop)
                        {
                            dataOffset = loopOffset;
                        }
                        else
                        {
                            left = right = prevLeft = prevRight = 0;
                            Stop();
                            return;
                        }
                    }
                    samp = decompressedSample[dataOffset++];
                    samp = (short)(samp * Volume / 0x7F);
                    prevLeft = (short)(samp * (-Pan + 0x40) / 0x80);
                    prevRight = (short)(samp * (Pan + 0x40) / 0x80);
                }
            }
            left = prevLeft;
            right = prevRight;
        }
    }
}
