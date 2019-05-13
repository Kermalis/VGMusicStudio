using Kermalis.VGMusicStudio.Util;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    internal class Channel
    {
        public readonly byte Index;

        public Track Owner;
        public EnvelopeState State;
        public byte RootKey, Key, NoteVelocity;
        public sbyte Panpot; // Not necessary
        public ushort BaseTimer, Timer;
        public uint NoteLength;
        public byte Volume;

        private int pos;
        private short prevLeft, prevRight;

        private int envelopeTimeLeft;
        private int volumeIncrement;
        private int velocity; // From 0-0x3FFFFFFF ((128 << 23) - 1)
        private byte targetVolume;

        private byte attackVolume, attack, decay, sustain, hold, decay2, release;

        // PCM8, PCM16, ADPCM
        private SWD.SampleBlock sample;
        // PCM8, PCM16
        private int dataOffset;
        // ADPCM
        private ADPCMDecoder adpcmDecoder;
        private short adpcmLoopLastSample;
        private short adpcmLoopStepIndex;

        public Channel(byte i)
        {
            Index = i;
        }

        public bool StartPCM(SWD localswd, SWD masterswd, byte voice, int key, uint noteLength)
        {
            SWD.IProgramInfo programInfo = localswd.Programs.ProgramInfos[voice];
            if (programInfo != null)
            {
                for (int i = 0; i < programInfo.SplitEntries.Length; i++)
                {
                    SWD.ISplitEntry split = programInfo.SplitEntries[i];
                    if (key >= split.LowKey && key <= split.HighKey)
                    {
                        sample = masterswd.Samples[split.SampleId];
                        Key = (byte)key;
                        RootKey = split.SampleRootKey;
                        BaseTimer = (ushort)(NDS.Utils.ARM7_CLOCK / sample.WavInfo.SampleRate);
                        if (sample.WavInfo.SampleFormat == SampleFormat.ADPCM)
                        {
                            adpcmDecoder = new ADPCMDecoder(sample.Data);
                        }
                        //attackVolume = sample.WavInfo.AttackVolume == 0 ? split.AttackVolume : sample.WavInfo.AttackVolume;
                        //attack = sample.WavInfo.Attack == 0 ? split.Attack : sample.WavInfo.Attack;
                        //decay = sample.WavInfo.Decay == 0 ? split.Decay : sample.WavInfo.Decay;
                        //sustain = sample.WavInfo.Sustain == 0 ? split.Sustain : sample.WavInfo.Sustain;
                        //hold = sample.WavInfo.Hold == 0 ? split.Hold : sample.WavInfo.Hold;
                        //decay2 = sample.WavInfo.Decay2 == 0 ? split.Decay2 : sample.WavInfo.Decay2;
                        //release = sample.WavInfo.Release == 0 ? split.Release : sample.WavInfo.Release;
                        //attackVolume = split.AttackVolume == 0 ? sample.WavInfo.AttackVolume : split.AttackVolume;
                        //attack = split.Attack == 0 ? sample.WavInfo.Attack : split.Attack;
                        //decay = split.Decay == 0 ? sample.WavInfo.Decay : split.Decay;
                        //sustain = split.Sustain == 0 ? sample.WavInfo.Sustain : split.Sustain;
                        //hold = split.Hold == 0 ? sample.WavInfo.Hold : split.Hold;
                        //decay2 = split.Decay2 == 0 ? sample.WavInfo.Decay2 : split.Decay2;
                        //release = split.Release == 0 ? sample.WavInfo.Release : split.Release;
                        attackVolume = split.AttackVolume == 0 ? sample.WavInfo.AttackVolume == 0 ? (byte)0x7F : sample.WavInfo.AttackVolume : split.AttackVolume;
                        attack = split.Attack == 0 ? sample.WavInfo.Attack == 0 ? (byte)0x7F : sample.WavInfo.Attack : split.Attack;
                        decay = split.Decay == 0 ? sample.WavInfo.Decay == 0 ? (byte)0x7F : sample.WavInfo.Decay : split.Decay;
                        sustain = split.Sustain == 0 ? sample.WavInfo.Sustain == 0 ? (byte)0x7F : sample.WavInfo.Sustain : split.Sustain;
                        hold = split.Hold == 0 ? sample.WavInfo.Hold == 0 ? (byte)0x7F : sample.WavInfo.Hold : split.Hold;
                        decay2 = split.Decay2 == 0 ? sample.WavInfo.Decay2 == 0 ? (byte)0x7F : sample.WavInfo.Decay2 : split.Decay2;
                        release = split.Release == 0 ? sample.WavInfo.Release == 0 ? (byte)0x7F : sample.WavInfo.Release : split.Release;
                        DetermineEnvelopeStartingPoint();
                        pos = 0;
                        prevLeft = prevRight = 0;
                        NoteLength = noteLength;
                        return true;
                    }
                }
            }
            return false;
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

        private bool CMDB1___sub_2074CA0()
        {
            bool b = true;
            bool ge = sample.WavInfo.EnvMult >= 0x7F;
            bool ee = sample.WavInfo.EnvMult == 0x7F;
            if (sample.WavInfo.EnvMult > 0x7F)
            {
                ge = attackVolume >= 0x7F;
                ee = attackVolume == 0x7F;
            }
            if (!ee & ge
                && attack > 0x7F
                && decay > 0x7F
                && sustain > 0x7F
                && hold > 0x7F
                && decay2 > 0x7F
                && release > 0x7F)
            {
                b = false;
            }
            return b;
        }
        private void DetermineEnvelopeStartingPoint()
        {
            State = EnvelopeState.Two; // This isn't actually placed in this func
            bool atLeastOneThingIsValid = CMDB1___sub_2074CA0(); // Neither is this
            if (atLeastOneThingIsValid)
            {
                if (attack != 0)
                {
                    velocity = attackVolume << 23;
                    State = EnvelopeState.Hold;
                    UpdateEnvelopePlan(0x7F, attack);
                }
                else
                {
                    velocity = 0x7F << 23;
                    if (hold != 0)
                    {
                        UpdateEnvelopePlan(0x7F, hold);
                        State = EnvelopeState.Decay;
                    }
                    else if (decay != 0)
                    {
                        UpdateEnvelopePlan(sustain, decay);
                        State = EnvelopeState.Decay2;
                    }
                    else
                    {
                        UpdateEnvelopePlan(0, release);
                        State = EnvelopeState.Six;
                    }
                }
                // Unk1E = 1
            }
            else if (State != EnvelopeState.One) // What should it be?
            {
                State = EnvelopeState.Zero;
                velocity = 0x7F << 23;
            }
        }
        public void SetEnvelopePhase7_2074ED8()
        {
            if (State != EnvelopeState.Zero)
            {
                UpdateEnvelopePlan(0, release);
                State = EnvelopeState.Seven;
            }
        }
        public int StepEnvelope()
        {
            if (State > EnvelopeState.Two)
            {
                if (envelopeTimeLeft != 0)
                {
                    envelopeTimeLeft--;
                    velocity = (velocity + volumeIncrement).Clamp(0, 0x3FFFFFFF);
                }
                else
                {
                    velocity = targetVolume << 23;
                    switch (State)
                    {
                        default: return velocity >> 23; // case 8
                        case EnvelopeState.Hold:
                        {
                            if (hold == 0)
                            {
                                goto LABEL_6;
                            }
                            else
                            {
                                UpdateEnvelopePlan(0x7F, hold);
                                State = EnvelopeState.Decay;
                            }
                            break;
                        }
                        case EnvelopeState.Decay:
                        LABEL_6:
                            {
                                if (decay == 0)
                                {
                                    velocity = sustain << 23;
                                    goto LABEL_9;
                                }
                                else
                                {
                                    UpdateEnvelopePlan(sustain, decay);
                                    State = EnvelopeState.Decay2;
                                }
                                break;
                            }
                        case EnvelopeState.Decay2:
                        LABEL_9:
                            {
                                if (decay2 == 0)
                                {
                                    goto LABEL_11;
                                }
                                else
                                {
                                    UpdateEnvelopePlan(0, decay2);
                                    State = EnvelopeState.Six;
                                }
                                break;
                            }
                        case EnvelopeState.Six:
                        LABEL_11:
                            {
                                UpdateEnvelopePlan(0, 0);
                                State = EnvelopeState.Two;
                                break;
                            }
                        case EnvelopeState.Seven:
                        {
                            State = EnvelopeState.Eight;
                            velocity = 0;
                            envelopeTimeLeft = 0;
                            break;
                        }
                    }
                }
            }
            return velocity >> 23;
        }
        private void UpdateEnvelopePlan(byte targetVolume, int envelopeParam)
        {
            if (envelopeParam == 0x7F)
            {
                volumeIncrement = 0;
                envelopeTimeLeft = int.MaxValue;
            }
            else
            {
                this.targetVolume = targetVolume;
                envelopeTimeLeft = sample.WavInfo.EnvMult == 0
                    ? Utils.Duration32[envelopeParam] * 1000 / 10000
                    : Utils.Duration16[envelopeParam] * sample.WavInfo.EnvMult * 1000 / 10000;
                volumeIncrement = envelopeTimeLeft == 0 ? 0 : ((targetVolume << 23) - velocity) / envelopeTimeLeft;
            }
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
                    switch (sample.WavInfo.SampleFormat)
                    {
                        case SampleFormat.PCM8:
                        {
                            // If hit end
                            if (dataOffset >= sample.Data.Length)
                            {
                                if (sample.WavInfo.Loop)
                                {
                                    dataOffset = (int)(sample.WavInfo.LoopStart * 4);
                                }
                                else
                                {
                                    left = right = prevLeft = prevRight = 0;
                                    Stop();
                                    return;
                                }
                            }
                            samp = (short)((sbyte)sample.Data[dataOffset++] << 8);
                            break;
                        }
                        case SampleFormat.PCM16:
                        {
                            // If hit end
                            if (dataOffset >= sample.Data.Length)
                            {
                                if (sample.WavInfo.Loop)
                                {
                                    dataOffset = (int)(sample.WavInfo.LoopStart * 4);
                                }
                                else
                                {
                                    left = right = prevLeft = prevRight = 0;
                                    Stop();
                                    return;
                                }
                            }
                            samp = (short)(sample.Data[dataOffset++] | (sample.Data[dataOffset++] << 8));
                            break;
                        }
                        case SampleFormat.ADPCM:
                        {
                            // If just looped
                            if (adpcmDecoder.DataOffset == sample.WavInfo.LoopStart * 4 && !adpcmDecoder.OnSecondNibble)
                            {
                                adpcmLoopLastSample = adpcmDecoder.LastSample;
                                adpcmLoopStepIndex = adpcmDecoder.StepIndex;
                            }
                            // If hit end
                            if (adpcmDecoder.DataOffset >= sample.Data.Length && !adpcmDecoder.OnSecondNibble)
                            {
                                if (sample.WavInfo.Loop)
                                {
                                    adpcmDecoder.DataOffset = (int)(sample.WavInfo.LoopStart * 4);
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
                        default: samp = 0; break;
                    }
                    samp = (short)(samp * Volume / 0x7F);
                    prevLeft = (short)(samp * (-Panpot + 0x40) / 0x80);
                    prevRight = (short)(samp * (Panpot + 0x40) / 0x80);
                }
            }
            left = prevLeft;
            right = prevRight;
        }
    }
}
