using NAudio.Wave;
using System;
using System.Linq;

namespace Kermalis.MusicStudio.Core
{
    class SoundMixer
    {
        public static SoundMixer Instance { get; } = new SoundMixer();

        public readonly float SampleRateReciprocal, SamplesReciprocal;
        public readonly int SamplesPerBuffer;

        public float MasterVolume = 1; public float DSMasterVolume { get; private set; }
        int fadeMicroFramesLeft; float fadePos, fadeStepPerMicroframe;
        int numTracks; // Last will be for program use

        readonly WaveBuffer audio;
        float[][] trackBuffers;
        public readonly bool[] Mutes;
        Reverb[] reverbs;
        readonly DirectSoundChannel[] dsChannels;
        readonly SquareChannel sq1, sq2;
        readonly WaveChannel wave;
        readonly NoiseChannel noise;
        readonly Channel[] allChannels;
        readonly GBChannel[] gbChannels;

        readonly BufferedWaveProvider buffer;
        readonly IWavePlayer @out;

        private SoundMixer()
        {
            SamplesPerBuffer = Config.Instance.SampleRate / Engine.AGB_FPS;
            SampleRateReciprocal = 1f / Config.Instance.SampleRate; SamplesReciprocal = 1f / SamplesPerBuffer;

            dsChannels = new DirectSoundChannel[Config.Instance.DirectCount];
            for (int i = 0; i < Config.Instance.DirectCount; i++)
            {
                dsChannels[i] = new DirectSoundChannel();
            }

            gbChannels = new GBChannel[] { sq1 = new SquareChannel(), sq2 = new SquareChannel(), wave = new WaveChannel(), noise = new NoiseChannel() };
            allChannels = ((Channel[])dsChannels).Union(gbChannels).ToArray();

            Mutes = new bool[17]; // 0-15 for tracks, 16 for the program

            int amt = SamplesPerBuffer * 2;
            audio = new WaveBuffer(amt * 4) { FloatBufferCount = amt };

            buffer = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(Config.Instance.SampleRate, 2))
            {
                DiscardOnBufferOverflow = true
            };
            @out = new WasapiOut();
            @out.Init(buffer);
            @out.Play();
        }
        public void Init(byte reverbAmt)
        {
            DSMasterVolume = ROM.Instance.Game.Engine.Volume / (float)0xF;
            numTracks = 16 + 1; // 1 for program use

            trackBuffers = new float[numTracks][];
            reverbs = new Reverb[numTracks];

            int amt = SamplesPerBuffer * 2;
            for (int i = 0; i < numTracks; i++)
            {
                trackBuffers[i] = new float[amt];
            }

            ReverbType reverbType = ROM.Instance.Game.Engine.ReverbType;
            reverbType = ReverbType.None; // For now because of crashes

            byte engineReverb = ROM.Instance.Game.Engine.Reverb;
            byte reverb = (byte)(engineReverb >= 0x80 ? engineReverb & 0x7F : reverbAmt & 0x7F);
            for (int i = 0; i < numTracks; i++)
            {
                byte numBuffers = (byte)(0x630 / (ROM.Instance.Game.Engine.Frequency / Engine.AGB_FPS));
                switch (reverbType)
                {
                    default: reverbs[i] = new Reverb(reverb, numBuffers); break;
                    case ReverbType.Camelot1: reverbs[i] = new ReverbCamelot1(reverb, numBuffers); break;
                    case ReverbType.Camelot2: reverbs[i] = new ReverbCamelot2(reverb, numBuffers, 53 / 128f, -8 / 128f); break;
                    case ReverbType.MGAT: reverbs[i] = new ReverbCamelot2(reverb, numBuffers, 32 / 128f, -6 / 128f); break;
                    case ReverbType.None: reverbs[i] = null; break;
                }
            }
        }

        public void FadeIn()
        {
            fadePos = 0;
            fadeMicroFramesLeft = (int)(Config.Instance.PlaylistFadeOutLength / 1000f * Engine.AGB_FPS);
            fadeStepPerMicroframe = 1f / fadeMicroFramesLeft;
        }
        public void FadeOut()
        {
            fadePos = 1;
            fadeMicroFramesLeft = (int)(Config.Instance.PlaylistFadeOutLength / 1000f * Engine.AGB_FPS);
            fadeStepPerMicroframe = -1f / fadeMicroFramesLeft;
        }
        public bool IsFadeDone()
        {
            return fadeMicroFramesLeft == 0;
        }
        public void ResetFade()
        {
            fadeMicroFramesLeft = 0;
        }

        public DirectSoundChannel NewDSNote(Track owner, ADSR env, Note note, byte vol, sbyte pan, int pitch, bool bFixed, bool bCompressed, WrappedSample sample)
        {
            DirectSoundChannel nChn = null;
            IOrderedEnumerable<DirectSoundChannel> byOwner = dsChannels.OrderByDescending(c => c.Owner == null ? 0xFF : c.Owner.Index);
            foreach (DirectSoundChannel i in byOwner) // Find free
            {
                if (i.State == EnvelopeState.Dead || i.Owner == null)
                {
                    nChn = i;
                    break;
                }
            }
            if (nChn == null) // Find releasing
            {
                foreach (DirectSoundChannel i in byOwner)
                {
                    if (i.State == EnvelopeState.Releasing)
                    {
                        nChn = i;
                        break;
                    }
                }
            }
            if (nChn == null) // Find prioritized
            {
                foreach (DirectSoundChannel i in byOwner)
                {
                    if (owner.Index >= 16 || owner.Priority > i.Owner.Priority)
                    {
                        nChn = i;
                        break;
                    }
                }
            }
            if (nChn == null) // None available
            {
                DirectSoundChannel lowest = byOwner.First(); // Kill lowest track's instrument if the track is lower than this one
                if (lowest.Owner.Index >= owner.Index)
                {
                    nChn = lowest;
                }
            }
            if (nChn != null) // Could still be null from the above if
            {
                nChn.Init(owner, note, env, sample, vol, pan, pitch, bFixed, bCompressed);
            }
            return nChn;
        }
        public GBChannel NewGBNote(Track owner, ADSR env, Note note, byte vol, sbyte pan, int pitch, M4AVoiceType type, object arg)
        {
            GBChannel nChn;
            switch (type)
            {
                case M4AVoiceType.Square1:
                    {
                        nChn = sq1;
                        if (nChn.State < EnvelopeState.Releasing && nChn.Owner.Index < owner.Index)
                        {
                            return null;
                        }
                        sq1.Init(owner, note, env, (SquarePattern)arg);
                        break;
                    }
                case M4AVoiceType.Square2:
                    {
                        nChn = sq2;
                        if (nChn.State < EnvelopeState.Releasing && nChn.Owner.Index < owner.Index)
                        {
                            return null;
                        }
                        sq2.Init(owner, note, env, (SquarePattern)arg);
                        break;
                    }
                case M4AVoiceType.Wave:
                    {
                        nChn = wave;
                        if (nChn.State < EnvelopeState.Releasing && nChn.Owner.Index < owner.Index)
                        {
                            return null;
                        }
                        wave.Init(owner, note, env, (int)arg);
                        break;
                    }
                case M4AVoiceType.Noise:
                    {
                        nChn = noise;
                        if (nChn.State < EnvelopeState.Releasing && nChn.Owner.Index < owner.Index)
                        {
                            return null;
                        }
                        noise.Init(owner, note, env, (NoisePattern)arg);
                        break;
                    }
                default: return null;
            }
            nChn.SetVolume(vol, pan);
            nChn.SetPitch(pitch);
            return nChn;
        }

        public void StopAllChannels()
        {
            for (int i = 0; i < allChannels.Length; i++)
            {
                allChannels[i].Stop();
            }
        }

        public void Process()
        {
            // Not initialized yet
            if (numTracks == 0)
            {
                return;
            }
            for (int i = 0; i < trackBuffers.Length; i++)
            {
                float[] buf = trackBuffers[i];
                Array.Clear(buf, 0, buf.Length);
            }
            audio.Clear();

            for (int i = 0; i < dsChannels.Length; i++)
            {
                DirectSoundChannel c = dsChannels[i];
                if (c.Owner != null)
                {
                    c.Process(trackBuffers[c.Owner.Index]);
                }
            }
            // Reverb only applies to DirectSound
            for (int i = 0; i < numTracks; i++)
            {
                reverbs[i]?.Process(trackBuffers[i], SamplesPerBuffer);
            }

            for (int i = 0; i < gbChannels.Length; i++)
            {
                GBChannel c = gbChannels[i];
                if (c.Owner != null)
                {
                    c.Process(trackBuffers[c.Owner.Index]);
                }
            }

            float fromMaster = MasterVolume, toMaster = MasterVolume;
            if (fadeMicroFramesLeft > 0)
            {
                const float scale = 10f / 6f;
                fromMaster *= (fadePos < 0) ? 0 : (float)Math.Pow(fadePos, scale);
                fadePos += fadeStepPerMicroframe;
                toMaster *= (fadePos < 0) ? 0 : (float)Math.Pow(fadePos, scale);
                fadeMicroFramesLeft--;
            }
            float masterStep = (toMaster - fromMaster) * SamplesReciprocal;
            for (int i = 0; i < numTracks; i++)
            {
                if (Mutes[i])
                {
                    continue;
                }

                float masterLevel = fromMaster;
                float[] buf = trackBuffers[i];
                for (int j = 0; j < SamplesPerBuffer; j++)
                {
                    audio.FloatBuffer[j * 2] += buf[j * 2] * masterLevel;
                    audio.FloatBuffer[j * 2 + 1] += buf[j * 2 + 1] * masterLevel;

                    masterLevel += masterStep;
                }
            }

            buffer.AddSamples(audio, 0, audio.ByteBufferCount);
        }
    }
}
