using Kermalis.EndianBinaryIO;
using NAudio.Wave;
using System;
using System.IO;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.GBA.M4A
{
    internal class M4AMixer : Mixer
    {
        public readonly float SampleRateReciprocal, SamplesReciprocal;
        public readonly int SamplesPerBuffer;
        public float DSMasterVolume = 12f / 15f; // TODO

        public readonly EndianBinaryReader Reader;
        private readonly WaveBuffer audio;
        private readonly float[][] trackBuffers;
        private readonly DirectSoundChannel[] dsChannels;
        private readonly SquareChannel sq1, sq2;
        private readonly WaveChannel wave;
        private readonly NoiseChannel noise;
        private readonly GBChannel[] gbChannels;
        private readonly BufferedWaveProvider buffer;

        public M4AMixer(byte[] rom)
        {
            Reader = new EndianBinaryReader(new MemoryStream(rom));
            SamplesPerBuffer = 224; // TODO
            SampleRateReciprocal = 1f / 13379; // TODO
            SamplesReciprocal = 1f / SamplesPerBuffer;

            dsChannels = new DirectSoundChannel[24];
            for (int i = 0; i < dsChannels.Length; i++)
            {
                dsChannels[i] = new DirectSoundChannel(this);
            }
            gbChannels = new GBChannel[] { sq1 = new SquareChannel(this), sq2 = new SquareChannel(this), wave = new WaveChannel(this), noise = new NoiseChannel(this) };

            Mutes = new bool[0x10];

            int amt = SamplesPerBuffer * 2;
            audio = new WaveBuffer(amt * sizeof(float)) { FloatBufferCount = amt };
            trackBuffers = new float[0x10][];
            for (int i = 0; i < trackBuffers.Length; i++)
            {
                trackBuffers[i] = new float[amt];
            }
            //buffer = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2)) // TODO
            buffer = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(13379, 2)) // TODO
            {
                DiscardOnBufferOverflow = true,
                BufferLength = SamplesPerBuffer * 64
            };
            Init(buffer);
        }

        public DirectSoundChannel NewDSNote(Track owner, ADSR env, Note note, byte vol, sbyte pan, int pitch, bool bFixed, bool bCompressed, int sampleOffset)
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
                    if (owner.Priority > i.Owner.Priority)
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
                nChn.Init(owner, note, env, sampleOffset, vol, pan, pitch, bFixed, bCompressed);
            }
            return nChn;
        }
        public GBChannel NewGBNote(Track owner, ADSR env, Note note, byte vol, sbyte pan, int pitch, VoiceType type, object arg)
        {
            GBChannel nChn;
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
                    nChn = wave;
                    if (nChn.State < EnvelopeState.Releasing && nChn.Owner.Index < owner.Index)
                    {
                        return null;
                    }
                    wave.Init(owner, note, env, (int)arg);
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

        public void Process()
        {
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

            for (int i = 0; i < gbChannels.Length; i++)
            {
                GBChannel c = gbChannels[i];
                if (c.Owner != null)
                {
                    c.Process(trackBuffers[c.Owner.Index]);
                }
            }

            for (int i = 0; i < Mutes.Length; i++)
            {
                if (!Mutes[i])
                {
                    float[] buf = trackBuffers[i];
                    for (int j = 0; j < SamplesPerBuffer; j++)
                    {
                        audio.FloatBuffer[j * 2] += buf[j * 2];
                        audio.FloatBuffer[(j * 2) + 1] += buf[(j * 2) + 1];
                    }
                }
            }

            buffer.AddSamples(audio, 0, audio.ByteBufferCount);
        }
    }
}

