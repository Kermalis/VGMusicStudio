using NAudio.Wave;
using System;
using System.Linq;

namespace GBAMusicStudio.Core
{
    internal class SoundMixer
    {
        internal readonly uint EngineSampleRate; internal readonly float SampleRateReciprocal;
        internal readonly uint SamplesPerBuffer; internal readonly float SamplesReciprocal;

        internal float MasterVolume; internal readonly float DSMasterVolume;

        readonly WaveBuffer audio;
        readonly WaveBuffer[] trackBuffers;
        readonly bool[] mutes;
        readonly Reverb[] reverbs;
        readonly DirectSoundChannel[] dsChannels;
        readonly SquareChannel sq1, sq2;
        readonly WaveChannel wave;
        readonly NoiseChannel noise;
        internal readonly Channel[] AllChannels;
        readonly Channel[] gbChannels;

        readonly BufferedWaveProvider buffer;
        readonly WasapiOut @out;

        internal SoundMixer(uint engineRate, byte reverb, ReverbType rType, float pcmVol)
        {
            EngineSampleRate = engineRate;
            SamplesPerBuffer = Config.SampleRate / (Engine.AGB_FPS * Engine.INTERFRAMES);
            SampleRateReciprocal = 1f / Config.SampleRate; SamplesReciprocal = 1f / SamplesPerBuffer;
            DSMasterVolume = pcmVol;

            dsChannels = new DirectSoundChannel[Config.DirectCount];
            for (int i = 0; i < Config.DirectCount; i++)
                dsChannels[i] = new DirectSoundChannel();

            trackBuffers = new WaveBuffer[16];
            mutes = new bool[16];
            reverbs = new Reverb[16];
            int amt = (int)(Engine.N_CHANNELS * SamplesPerBuffer);
            audio = new WaveBuffer(amt * 4) { FloatBufferCount = amt };
            for (int i = 0; i < 16; i++)
            {
                trackBuffers[i] = new WaveBuffer(amt * 4) // Floats are 32-bit
                {
                    FloatBufferCount = amt
                };
                byte numBuffers = (byte)(0x630 / (engineRate / Engine.AGB_FPS));
                switch (rType)
                {
                    default: reverbs[i] = new Reverb(reverb, numBuffers); break;
                    case ReverbType.None: reverbs[i] = new Reverb(0, numBuffers); break;
                }
            }

            gbChannels = new GBChannel[] { sq1 = new SquareChannel(), sq2 = new SquareChannel(), wave = new WaveChannel(), noise = new NoiseChannel() };
            AllChannels = dsChannels.Union(gbChannels).ToArray();

            buffer = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat((int)Config.SampleRate, Engine.N_CHANNELS))
            {
                DiscardOnBufferOverflow = true
            };
            @out = new WasapiOut();
            @out.Init(buffer);
            @out.Play();
        }

        internal void SetMute(int i, bool m) => mutes[i] = m;

        internal void NewDSNote(byte owner, ADSR env, Note note, byte vol, sbyte pan, int pitch, bool bFixed, Sample sample, Track[] tracks)
        {
            DirectSoundChannel nChn = null;
            var byOwner = dsChannels.OrderByDescending(c => c.OwnerIdx);
            foreach (var i in byOwner) // Find free
                if (i.State == ADSRState.Dead || i.OwnerIdx == 0xFF)
                {
                    nChn = i;
                    break;
                }
            if (nChn == null) // Find prioritized
                foreach (var i in byOwner)
                    if (tracks[owner].Priority > tracks[i.OwnerIdx].Priority)
                    {
                        nChn = i;
                        break;
                    }
            if (nChn == null) // Find releasing
                foreach (var i in byOwner)
                    if (i.State == ADSRState.Releasing)
                    {
                        nChn = i;
                        break;
                    }
            if (nChn == null) // None available
            {
                var lowest = byOwner.First(); // Kill lowest track's instrument if the track is lower than this one
                if (lowest.OwnerIdx >= owner)
                    nChn = lowest;
            }
            if (nChn != null) // Could still be null from the above if
                nChn.Init(owner, note, env, sample, vol, pan, pitch, bFixed);
        }
        internal void NewGBNote(byte owner, ADSR env, Note note, byte vol, sbyte pan, int pitch, GBType type, object arg)
        {
            T ObjToEnum<T>(object o)
            {
                T enumVal = (T)Enum.Parse(typeof(T), o.ToString());
                return enumVal;
            }

            GBChannel nChn;
            switch (type)
            {
                default:
                    nChn = sq1;
                    if (nChn.State < ADSRState.Releasing && nChn.OwnerIdx < owner)
                        return;
                    sq1.Init(owner, note, env, ObjToEnum<SquarePattern>(arg));
                    break;
                case GBType.Square2:
                    nChn = sq2;
                    if (nChn.State < ADSRState.Releasing && nChn.OwnerIdx < owner)
                        return;
                    sq2.Init(owner, note, env, ObjToEnum<SquarePattern>(arg));
                    break;
                case GBType.Wave:
                    nChn = wave;
                    if (nChn.State < ADSRState.Releasing && nChn.OwnerIdx < owner)
                        return;
                    wave.Init(owner, note, env, (byte[])arg);
                    break;
                case GBType.Noise:
                    nChn = noise;
                    if (nChn.State < ADSRState.Releasing && nChn.OwnerIdx < owner)
                        return;
                    noise.Init(owner, note, env, ObjToEnum<NoisePattern>(arg));
                    break;
            }
            nChn.SetVolume(vol, pan);
            nChn.SetPitch(pitch);
        }

        // Returns number of active notes
        internal int TickNotes(int owner)
        {
            int active = 0;
            foreach (var chn in AllChannels)
                if (chn.OwnerIdx == owner && chn.TickNote())
                    active++;
            return active;
        }
        internal void ReleaseChannels(int owner, int key)
        {
            foreach (var chn in AllChannels)
                if (chn.OwnerIdx == owner && (key == -1 || (chn.Note.OriginalKey == key && chn.Note.Duration == -1)))
                    chn.Release();
        }
        internal void UpdateChannels(int owner, byte vol, sbyte pan, int pitch)
        {
            foreach (var chn in AllChannels)
                if (chn.OwnerIdx == owner)
                {
                    chn.SetVolume(vol, pan);
                    chn.SetPitch(pitch);
                }
        }

        internal void Process()
        {
            foreach (var b in trackBuffers)
                b.Clear();
            audio.Clear();

            foreach (var chn in dsChannels)
                if (chn.OwnerIdx != 0xFF)
                    chn.Process(trackBuffers[chn.OwnerIdx].FloatBuffer, this);

            // Reverb only applies to DirectSound
            for (int i = 0; i < trackBuffers.Length; i++)
                reverbs[i].Process(trackBuffers[i].FloatBuffer, (int)SamplesPerBuffer);

            foreach (var chn in gbChannels)
                if (chn.OwnerIdx != 0xFF)
                    chn.Process(trackBuffers[chn.OwnerIdx].FloatBuffer, this);

            for (int i = 0; i < 16; i++)
            {
                if (mutes[i]) continue;

                var b = trackBuffers[i];
                for (int j = 0; j < SamplesPerBuffer; j++)
                {
                    audio.FloatBuffer[j * Engine.N_CHANNELS] += (b.FloatBuffer[j * Engine.N_CHANNELS] * MasterVolume);
                    audio.FloatBuffer[j * Engine.N_CHANNELS + 1] += (b.FloatBuffer[j * Engine.N_CHANNELS + 1] * MasterVolume);
                }
            }

            buffer.AddSamples(audio, 0, audio.ByteBufferCount);
        }
    }
}
