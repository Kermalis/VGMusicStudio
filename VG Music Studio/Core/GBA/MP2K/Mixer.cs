using NAudio.Wave;
using System;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K
{
    internal class Mixer : Core.Mixer
    {
        public readonly int SampleRate;
        public readonly int SamplesPerBuffer;
        public readonly float SampleRateReciprocal;
        private readonly float samplesReciprocal;
        public readonly float PCM8MasterVolume;
        private long fadeMicroFramesLeft;
        private float fadePos;
        private float fadeStepPerMicroframe;

        public readonly Config Config;
        private readonly WaveBuffer audio;
        private readonly float[][] trackBuffers;
        private readonly PCM8Channel[] pcm8Channels;
        private readonly SquareChannel sq1, sq2;
        private readonly PCM4Channel pcm4;
        private readonly NoiseChannel noise;
        private readonly PSGChannel[] psgChannels;
        private readonly BufferedWaveProvider buffer;

        public Mixer(Config config)
        {
            Config = config;
            (SampleRate, SamplesPerBuffer) = Utils.FrequencyTable[config.SampleRate];
            SampleRateReciprocal = 1f / SampleRate;
            samplesReciprocal = 1f / SamplesPerBuffer;
            PCM8MasterVolume = config.Volume / 15f;

            pcm8Channels = new PCM8Channel[24];
            for (int i = 0; i < pcm8Channels.Length; i++)
            {
                pcm8Channels[i] = new PCM8Channel(this);
            }
            psgChannels = new PSGChannel[] { sq1 = new SquareChannel(this), sq2 = new SquareChannel(this), pcm4 = new PCM4Channel(this), noise = new NoiseChannel(this) };

            int amt = SamplesPerBuffer * 2;
            audio = new WaveBuffer(amt * sizeof(float)) { FloatBufferCount = amt };
            trackBuffers = new float[0x10][];
            for (int i = 0; i < trackBuffers.Length; i++)
            {
                trackBuffers[i] = new float[amt];
            }
            buffer = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 2))
            {
                DiscardOnBufferOverflow = true,
                BufferLength = SamplesPerBuffer * 64
            };
            Init(buffer);
        }
        public override void Dispose()
        {
            base.Dispose();
            CloseWaveWriter();
        }

        public PCM8Channel AllocPCM8Channel(Track owner, ADSR env, Note note, byte vol, sbyte pan, int pitch, bool bFixed, bool bCompressed, int sampleOffset)
        {
            PCM8Channel nChn = null;
            IOrderedEnumerable<PCM8Channel> byOwner = pcm8Channels.OrderByDescending(c => c.Owner == null ? 0xFF : c.Owner.Index);
            foreach (PCM8Channel i in byOwner) // Find free
            {
                if (i.State == EnvelopeState.Dead || i.Owner == null)
                {
                    nChn = i;
                    break;
                }
            }
            if (nChn == null) // Find releasing
            {
                foreach (PCM8Channel i in byOwner)
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
                foreach (PCM8Channel i in byOwner)
                {
                    if (owner.Priority > i.Owner.Priority)
                    {
                        nChn = i;
                        break;
                    }
                }
            }
            if (nChn == null) // None available
            {
                PCM8Channel lowest = byOwner.First(); // Kill lowest track's instrument if the track is lower than this one
                if (lowest.Owner.Index >= owner.Index)
                {
                    nChn = lowest;
                }
            }
            if (nChn != null) // Could still be null from the above if
            {
                nChn.Init(owner, note, env, sampleOffset, vol, pan, pitch, bFixed, bCompressed);
            }
            return nChn;
        }
        public PSGChannel AllocPSGChannel(Track owner, ADSR env, Note note, byte vol, sbyte pan, int pitch, VoiceType type, object arg)
        {
            PSGChannel nChn;
            switch (type)
            {
                case VoiceType.Square1:
                {
                    nChn = sq1;
                    if (nChn.State < EnvelopeState.Releasing && nChn.Owner.Index < owner.Index)
                    {
                        return null;
                    }
                    sq1.Init(owner, note, env, (SquarePattern)arg);
                    break;
                }
                case VoiceType.Square2:
                {
                    nChn = sq2;
                    if (nChn.State < EnvelopeState.Releasing && nChn.Owner.Index < owner.Index)
                    {
                        return null;
                    }
                    sq2.Init(owner, note, env, (SquarePattern)arg);
                    break;
                }
                case VoiceType.PCM4:
                {
                    nChn = pcm4;
                    if (nChn.State < EnvelopeState.Releasing && nChn.Owner.Index < owner.Index)
                    {
                        return null;
                    }
                    pcm4.Init(owner, note, env, (int)arg);
                    break;
                }
                case VoiceType.Noise:
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

        public void BeginFadeIn()
        {
            fadePos = 0f;
            fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * GBA.Utils.AGB_FPS);
            fadeStepPerMicroframe = 1f / fadeMicroFramesLeft;
        }
        public void BeginFadeOut()
        {
            fadePos = 1f;
            fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * GBA.Utils.AGB_FPS);
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

        private WaveFileWriter waveWriter;
        public void CreateWaveWriter(string fileName)
        {
            waveWriter = new WaveFileWriter(fileName, buffer.WaveFormat);
        }
        public void CloseWaveWriter()
        {
            waveWriter?.Dispose();
        }
        public void Process(bool output, bool recording)
        {
            for (int i = 0; i < trackBuffers.Length; i++)
            {
                float[] buf = trackBuffers[i];
                Array.Clear(buf, 0, buf.Length);
            }
            audio.Clear();

            for (int i = 0; i < pcm8Channels.Length; i++)
            {
                PCM8Channel c = pcm8Channels[i];
                if (c.Owner != null)
                {
                    c.Process(trackBuffers[c.Owner.Index]);
                }
            }

            for (int i = 0; i < psgChannels.Length; i++)
            {
                PSGChannel c = psgChannels[i];
                if (c.Owner != null)
                {
                    c.Process(trackBuffers[c.Owner.Index]);
                }
            }

            float fromMaster = 1f, toMaster = 1f;
            if (fadeMicroFramesLeft > 0)
            {
                const float scale = 10f / 6f;
                fromMaster *= (fadePos < 0f) ? 0f : (float)Math.Pow(fadePos, scale);
                fadePos += fadeStepPerMicroframe;
                toMaster *= (fadePos < 0f) ? 0f : (float)Math.Pow(fadePos, scale);
                fadeMicroFramesLeft--;
            }
            float masterStep = (toMaster - fromMaster) * samplesReciprocal;
            for (int i = 0; i < trackBuffers.Length; i++)
            {
                if (!Mutes[i])
                {
                    float masterLevel = fromMaster;
                    float[] buf = trackBuffers[i];
                    for (int j = 0; j < SamplesPerBuffer; j++)
                    {
                        audio.FloatBuffer[j * 2] += buf[j * 2] * masterLevel;
                        audio.FloatBuffer[(j * 2) + 1] += buf[(j * 2) + 1] * masterLevel;
                        masterLevel += masterStep;
                    }
                }
            }
            if (output)
            {
                buffer.AddSamples(audio.ByteBuffer, 0, audio.ByteBufferCount);
            }
            if (recording)
            {
                waveWriter.Write(audio.ByteBuffer, 0, audio.ByteBufferCount);
            }
        }
    }
}
