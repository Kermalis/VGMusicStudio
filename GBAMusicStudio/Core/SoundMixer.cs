using NAudio.Wave;
using System;
using System.Linq;

namespace GBAMusicStudio.Core
{
    class SoundMixer
    {
        static SoundMixer instance;
        public static SoundMixer Instance
        {
            get
            {
                if (instance == null)
                    instance = new SoundMixer();
                return instance;
            }
        }

        public readonly float SampleRateReciprocal, SamplesReciprocal;
        public readonly int SamplesPerBuffer;

        public float MasterVolume = 1; public float DSMasterVolume { get; private set; }
        int numTracks; // 1 will be for program use

        readonly WaveBuffer audio;
        float[][] trackBuffers;
        readonly bool[] mutes;
        Reverb[] reverbs;
        readonly DirectSoundChannel[] dsChannels;
        readonly SquareChannel sq1, sq2;
        readonly WaveChannel wave;
        readonly NoiseChannel noise;
        readonly Channel[] allChannels;
        readonly Channel[] gbChannels;

        readonly BufferedWaveProvider buffer;
        readonly IWavePlayer @out;

        private SoundMixer()
        {
            SamplesPerBuffer = Config.Instance.SampleRate / (Engine.AGB_FPS * Config.Instance.InterFrames);
            SampleRateReciprocal = 1f / Config.Instance.SampleRate; SamplesReciprocal = 1f / SamplesPerBuffer;

            dsChannels = new DirectSoundChannel[Config.Instance.DirectCount];
            for (int i = 0; i < Config.Instance.DirectCount; i++)
                dsChannels[i] = new DirectSoundChannel();

            gbChannels = new GBChannel[] { sq1 = new SquareChannel(), sq2 = new SquareChannel(), wave = new WaveChannel(), noise = new NoiseChannel() };
            allChannels = dsChannels.Union(gbChannels).ToArray();

            mutes = new bool[17]; // 0-15 for tracks, 16 for the program

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
            numTracks = ROM.Instance.Game.Engine.TrackLimit + 1; // 1 for program use

            trackBuffers = new float[numTracks][];
            reverbs = new Reverb[numTracks];

            int amt = SamplesPerBuffer * 2;
            for (int i = 0; i < numTracks; i++)
                trackBuffers[i] = new float[amt];

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

        public void SetMute(int owner, bool m) => mutes[owner] = m;

        public DirectSoundChannel NewDSNote(byte owner, ADSR env, Note note, byte vol, sbyte pan, int pitch, bool bFixed, WrappedSample sample, Track[] tracks)
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
            return nChn;
        }
        public GBChannel NewGBNote(byte owner, ADSR env, Note note, byte vol, sbyte pan, int pitch, M4AVoiceType type, object arg)
        {
            GBChannel nChn;
            switch (type)
            {
                case M4AVoiceType.Square1:
                    nChn = sq1;
                    if (nChn.State < ADSRState.Releasing && nChn.OwnerIdx < owner)
                        return null;
                    sq1.Init(owner, note, env, (SquarePattern)arg);
                    break;
                case M4AVoiceType.Square2:
                    nChn = sq2;
                    if (nChn.State < ADSRState.Releasing && nChn.OwnerIdx < owner)
                        return null;
                    sq2.Init(owner, note, env, (SquarePattern)arg);
                    break;
                case M4AVoiceType.Wave:
                    nChn = wave;
                    if (nChn.State < ADSRState.Releasing && nChn.OwnerIdx < owner)
                        return null;
                    wave.Init(owner, note, env, (int)arg);
                    break;
                case M4AVoiceType.Noise:
                    nChn = noise;
                    if (nChn.State < ADSRState.Releasing && nChn.OwnerIdx < owner)
                        return null;
                    noise.Init(owner, note, env, (NoisePattern)arg);
                    break;
                default: return null;
            }
            nChn.SetVolume(vol, pan);
            nChn.SetPitch(pitch);
            return nChn;
        }

        // Returns number of active notes
        public int TickNotes(int owner)
        {
            int active = 0;
            foreach (var c in allChannels)
                if (c.OwnerIdx == owner && c.TickNote())
                    active++;
            return active;
        }
        public bool AllDead(int owner)
        {
            return !allChannels.Any(c => c.OwnerIdx == owner);
        }
        public Channel[] GetChannels(int owner)
        {
            return allChannels.Where(c => c.OwnerIdx == owner).ToArray();
        }
        public void ReleaseChannels(int owner, int key)
        {
            foreach (var c in allChannels)
                if (c.OwnerIdx == owner && (key == -1 || (c.Note.OriginalKey == key && c.Note.Duration == -1)))
                    c.Release();
        }
        public void UpdateChannels(int owner, byte vol, sbyte pan, int pitch)
        {
            foreach (var c in allChannels)
                if (c.OwnerIdx == owner)
                {
                    c.SetVolume(vol, pan);
                    c.SetPitch(pitch);
                }
        }
        public void StopAllChannels()
        {
            foreach (var c in allChannels)
                c.Stop();
        }

        public void Process()
        {
            // Not initialized yet
            if (numTracks == 0)
                return;

            foreach (var buf in trackBuffers)
                Array.Clear(buf, 0, buf.Length);
            audio.Clear();

            foreach (var c in dsChannels)
                if (c.OwnerIdx != 0xFF)
                    c.Process(trackBuffers[c.OwnerIdx]);

            // Reverb only applies to DirectSound
            for (int i = 0; i < numTracks; i++)
                reverbs[i]?.Process(trackBuffers[i], SamplesPerBuffer);

            foreach (var c in gbChannels)
                if (c.OwnerIdx != 0xFF)
                    c.Process(trackBuffers[c.OwnerIdx]);

            for (int i = 0; i < numTracks; i++)
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
