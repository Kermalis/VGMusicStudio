using NAudio.Wave;
using System;
using System.Linq;

namespace GBAMusicStudio.Core
{
    internal static class SoundMixer
    {
        const int MAX_TRACKS = 17; // 16 for playback, 1 for program use

        internal static readonly float SampleRateReciprocal, SamplesReciprocal;
        internal static readonly uint SamplesPerBuffer;

        internal static float MasterVolume; internal static float DSMasterVolume { get; private set; }

        static readonly WaveBuffer audio;
        static readonly float[][] trackBuffers;
        static readonly bool[] mutes;
        static readonly Reverb[] reverbs;
        static readonly DirectSoundChannel[] dsChannels;
        static readonly SquareChannel sq1, sq2;
        static readonly WaveChannel wave;
        static readonly NoiseChannel noise;
        static readonly Channel[] allChannels;
        static readonly Channel[] gbChannels;

        static readonly BufferedWaveProvider buffer;
        static readonly WasapiOut @out;

        static SoundMixer()
        {
            SamplesPerBuffer = Config.SampleRate / (Engine.AGB_FPS * Engine.INTERFRAMES);
            SampleRateReciprocal = 1f / Config.SampleRate; SamplesReciprocal = 1f / SamplesPerBuffer;

            dsChannels = new DirectSoundChannel[Config.DirectCount];
            for (int i = 0; i < Config.DirectCount; i++)
                dsChannels[i] = new DirectSoundChannel();

            trackBuffers = new float[MAX_TRACKS][];
            reverbs = new Reverb[MAX_TRACKS];
            mutes = new bool[MAX_TRACKS];

            int amt = (int)(SamplesPerBuffer * 2);
            audio = new WaveBuffer(amt * 4) { FloatBufferCount = amt };
            for (int i = 0; i < MAX_TRACKS; i++)
                trackBuffers[i] = new float[amt];

            gbChannels = new GBChannel[] { sq1 = new SquareChannel(), sq2 = new SquareChannel(), wave = new WaveChannel(), noise = new NoiseChannel() };
            allChannels = dsChannels.Union(gbChannels).ToArray();

            buffer = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat((int)Config.SampleRate, 2))
            {
                DiscardOnBufferOverflow = true
            };
            @out = new WasapiOut();
            @out.Init(buffer);
            @out.Play();
        }
        internal static void Init(byte songReverb)
        {
            DSMasterVolume = ROM.Instance.Game.Engine.Volume / (float)0xF;

            byte engineReverb = ROM.Instance.Game.Engine.Reverb;
            byte reverb = (byte)(engineReverb >= 0x80 ? engineReverb & 0x7F : songReverb & 0x7F);
            for (int i = 0; i < MAX_TRACKS; i++)
            {
                byte numBuffers = (byte)(0x630 / (ROM.Instance.Game.Engine.Frequency / Engine.AGB_FPS));
                switch (ROM.Instance.Game.Engine.ReverbType)
                {
                    default: reverbs[i] = new Reverb(reverb, numBuffers); break;
                    case ReverbType.Camelot1: reverbs[i] = new ReverbCamelot1(reverb, numBuffers); break;
                    case ReverbType.None: reverbs[i] = new Reverb(0, numBuffers); break;
                }
            }
        }

        internal static void SetMute(int owner, bool m) => mutes[owner] = m;

        internal static void NewDSNote(byte owner, ADSR env, Note note, byte vol, sbyte pan, int pitch, bool bFixed, Sample sample, Track[] tracks)
        {
            DirectSoundChannel nChn = null;
            var byOwner = dsChannels.OrderByDescending(c => c.OwnerIdx);
            foreach (var i in byOwner) // Find free
                if (i.State == ADSRState.Dead || i.OwnerIdx == 0xFF)
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
            if (nChn == null) // Find prioritized
                foreach (var i in byOwner)
                    if (owner >= 16 || tracks[owner].Priority > tracks[i.OwnerIdx].Priority)
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
        internal static void NewGBNote(byte owner, ADSR env, Note note, byte vol, sbyte pan, int pitch, GBType type, object arg)
        {
            GBChannel nChn;
            switch (type)
            {
                default:
                    nChn = sq1;
                    if (nChn.State < ADSRState.Releasing && nChn.OwnerIdx < owner)
                        return;
                    sq1.Init(owner, note, env, (SquarePattern)arg);
                    break;
                case GBType.Square2:
                    nChn = sq2;
                    if (nChn.State < ADSRState.Releasing && nChn.OwnerIdx < owner)
                        return;
                    sq2.Init(owner, note, env, (SquarePattern)arg);
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
                    noise.Init(owner, note, env, (NoisePattern)arg);
                    break;
            }
            nChn.SetVolume(vol, pan);
            nChn.SetPitch(pitch);
        }

        // Returns number of active notes
        internal static int TickNotes(int owner)
        {
            int active = 0;
            foreach (var c in allChannels)
                if (c.OwnerIdx == owner && c.TickNote())
                    active++;
            return active;
        }
        internal static bool AllDead(int owner)
        {
            return allChannels.All(c => c.OwnerIdx == owner);
        }
        internal static Channel[] GetChannels(int owner)
        {
            return allChannels.Where(c => c.OwnerIdx == owner).ToArray();
        }
        internal static void ReleaseChannels(int owner, int key)
        {
            foreach (var c in allChannels)
                if (c.OwnerIdx == owner && (key == -1 || (c.Note.OriginalKey == key && c.Note.Duration == -1)))
                    c.Release();
        }
        internal static void UpdateChannels(int owner, byte vol, sbyte pan, int pitch)
        {
            foreach (var c in allChannels)
                if (c.OwnerIdx == owner)
                {
                    c.SetVolume(vol, pan);
                    c.SetPitch(pitch);
                }
        }
        internal static void StopAllChannels()
        {
            foreach (var c in allChannels)
                c.Stop();
        }

        internal static void Process()
        {
            foreach (var buf in trackBuffers)
                Array.Clear(buf, 0, buf.Length);
            audio.Clear();

            foreach (var c in dsChannels)
                if (c.OwnerIdx != 0xFF)
                    c.Process(trackBuffers[c.OwnerIdx]);

            // Reverb only applies to DirectSound
            for (int i = 0; i < trackBuffers.Length; i++)
                reverbs[i]?.Process(trackBuffers[i], (int)SamplesPerBuffer);

            foreach (var c in gbChannels)
                if (c.OwnerIdx != 0xFF)
                    c.Process(trackBuffers[c.OwnerIdx]);

            for (int i = 0; i < MAX_TRACKS; i++)
            {
                if (mutes[i]) continue;

                var buf = trackBuffers[i];
                for (int j = 0; j < SamplesPerBuffer; j++)
                {
                    audio.FloatBuffer[j * 2] += buf[j * 2] * MasterVolume;
                    audio.FloatBuffer[j * 2 + 1] += buf[j * 2 + 1] * MasterVolume;
                }
            }

            buffer.AddSamples(audio, 0, audio.ByteBufferCount);
        }
    }
}
