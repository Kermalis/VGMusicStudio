using System;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT
{
    internal class Channel
    {
        public readonly byte Index;

        public Track Owner;
        public InstrumentType Type;
        public EnvelopeState State;
        public bool AutoSweep;
        public byte BaseKey, Key, NoteVelocity;
        public sbyte StartingPan, Pan;
        public int SweepCounter, SweepLength;
        public short SweepPitch;
        public int Velocity; // The SEQ Player treats 0 as the 100% amplitude value and -92544 (-723*128) as the 0% amplitude value. The starting ampltitude is 0% (-92544).
        public byte Volume; // From 0x00-0x7F (Calculated from Utils)
        public ushort BaseTimer, Timer;
        public int NoteLength;

        private byte attack;
        private int sustain;
        private ushort decay;
        private ushort release;

        private int pos;
        private short prevLeft, prevRight;

        // PCM8, PCM16, ADPCM
        private SWAVInfo wave;
        // PCM8, PCM16
        private int dataOffset;
        // ADPCM
        private ADPCMDecoder adpcmDecoder;
        private short adpcmLoopLastSample;
        private short adpcmLoopStepIndex;
        // PSG
        private byte psgDuty;
        private int psgCounter;
        // Noise
        private ushort noiseCounter;

        public Channel(byte i)
        {
            Index = i;
        }

        public void StartPCM(SWAVInfo wave, int noteLength)
        {
            Type = InstrumentType.PCM;
            dataOffset = 0;
            this.wave = wave;
            if (wave.Format == SWAVFormat.ADPCM)
            {
                adpcmDecoder = new ADPCMDecoder(wave.Samples);
            }
            BaseTimer = wave.Timer;
            Start(noteLength);
        }
        public void StartPSG(byte duty, int noteLength)
        {
            Type = InstrumentType.PSG;
            psgCounter = 0;
            psgDuty = duty;
            BaseTimer = 8006;
            Start(noteLength);
        }
        public void StartNoise(int noteLength)
        {
            Type = InstrumentType.Noise;
            noiseCounter = 0x7FFF;
            BaseTimer = 8006;
            Start(noteLength);
        }

        private void Start(int noteLength)
        {
            State = EnvelopeState.Attack;
            Velocity = -92544;
            pos = 0;
            prevLeft = prevRight = 0;
            NoteLength = noteLength;
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

        public int SweepMain()
        {
            if (SweepPitch != 0 && SweepCounter < SweepLength)
            {
                int sweep = (int)(Math.BigMul(SweepPitch, SweepLength - SweepCounter) / SweepLength);
                if (AutoSweep)
                {
                    SweepCounter++;
                }
                return sweep;
            }
            else
            {
                return 0;
            }
        }

        public void SetAttack(int a)
        {
            attack = SDATUtils.AttackTable[a];
        }
        public void SetDecay(int d)
        {
            decay = SDATUtils.DecayTable[d];
        }
        public void SetSustain(byte s)
        {
            sustain = SDATUtils.SustainTable[s];
        }
        public void SetRelease(int r)
        {
            release = SDATUtils.DecayTable[r];
        }
        public void StepEnvelope()
        {
            switch (State)
            {
                case EnvelopeState.Attack:
                {
                    Velocity = attack * Velocity / 0xFF;
                    if (Velocity == 0)
                    {
                        State = EnvelopeState.Decay;
                    }
                    break;
                }
                case EnvelopeState.Decay:
                {
                    Velocity -= decay;
                    if (Velocity <= sustain)
                    {
                        State = EnvelopeState.Sustain;
                        Velocity = sustain;
                    }
                    break;
                }
                case EnvelopeState.Release:
                {
                    Velocity -= release;
                    if (Velocity < -92544)
                    {
                        Velocity = -92544;
                    }
                    break;
                }
            }
        }

        public void Process(out short left, out short right)
        {
            if (Timer == 0)
            {
                left = prevLeft;
                right = prevRight;
            }
            else
            {
                int numSamples = (pos + 0x100) / Timer;
                pos = (pos + 0x100) % Timer;
                // prevLeft and prevRight are stored because numSamples can be 0.
                for (int i = 0; i < numSamples; i++)
                {
                    short samp = 0;
                    switch (Type)
                    {
                        case InstrumentType.PCM:
                        {
                            switch (wave.Format)
                            {
                                case SWAVFormat.PCM8:
                                {
                                    // If hit end
                                    if (dataOffset >= wave.Samples.Length)
                                    {
                                        if (wave.DoesLoop)
                                        {
                                            dataOffset = wave.LoopOffset * 4;
                                        }
                                        else
                                        {
                                            left = right = prevLeft = prevRight = 0;
                                            Stop();
                                            return;
                                        }
                                    }
                                    samp = (short)((sbyte)wave.Samples[dataOffset++] << 8);
                                    break;
                                }
                                case SWAVFormat.PCM16:
                                {
                                    // If hit end
                                    if (dataOffset >= wave.Samples.Length)
                                    {
                                        if (wave.DoesLoop)
                                        {
                                            dataOffset = wave.LoopOffset * 4;
                                        }
                                        else
                                        {
                                            left = right = prevLeft = prevRight = 0;
                                            Stop();
                                            return;
                                        }
                                    }
                                    samp = (short)(wave.Samples[dataOffset++] | (wave.Samples[dataOffset++] << 8));
                                    break;
                                }
                                case SWAVFormat.ADPCM:
                                {
                                    // If just looped
                                    if (adpcmDecoder.DataOffset == wave.LoopOffset * 4 && !adpcmDecoder.OnSecondNibble)
                                    {
                                        adpcmLoopLastSample = adpcmDecoder.LastSample;
                                        adpcmLoopStepIndex = adpcmDecoder.StepIndex;
                                    }
                                    // If hit end
                                    if (adpcmDecoder.DataOffset >= wave.Samples.Length && !adpcmDecoder.OnSecondNibble)
                                    {
                                        if (wave.DoesLoop)
                                        {
                                            adpcmDecoder.DataOffset = wave.LoopOffset * 4;
                                            adpcmDecoder.StepIndex = adpcmLoopStepIndex;
                                            adpcmDecoder.LastSample = adpcmLoopLastSample;
                                            adpcmDecoder.OnSecondNibble = false;
                                        }
                                        else
                                        {
                                            left = right = prevLeft = prevRight = 0;
                                            Stop();
                                            return;
                                        }
                                    }
                                    samp = adpcmDecoder.GetSample();
                                    break;
                                }
                            }
                            break;
                        }
                        case InstrumentType.PSG:
                        {
                            samp = psgCounter <= psgDuty ? short.MinValue : short.MaxValue;
                            psgCounter++;
                            if (psgCounter >= 8)
                            {
                                psgCounter = 0;
                            }
                            break;
                        }
                        case InstrumentType.Noise:
                        {
                            if ((noiseCounter & 1) != 0)
                            {
                                noiseCounter = (ushort)((noiseCounter >> 1) ^ 0x6000);
                                samp = -0x7FFF;
                            }
                            else
                            {
                                noiseCounter = (ushort)(noiseCounter >> 1);
                                samp = 0x7FFF;
                            }
                            break;
                        }
                    }
                    samp = (short)(samp * Volume / 0x7F);
                    prevLeft = (short)(samp * (-Pan + 0x40) / 0x80);
                    prevRight = (short)(samp * (Pan + 0x40) / 0x80);
                }
                left = prevLeft;
                right = prevRight;
            }
        }
    }
}
