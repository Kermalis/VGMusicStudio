using Kermalis.GBAMusicStudio.UI;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using System;
using System.Linq;

namespace Kermalis.GBAMusicStudio.Core
{
    class SoundMixer : IAudioSessionEventsHandler
    {
        public static SoundMixer Instance { get; } = new SoundMixer();

        public float SampleRateReciprocal, SamplesReciprocal;
        public int SamplesPerBuffer;

        public float DSMasterVolume;
        int fadeMicroFramesLeft; float fadePos, fadeStepPerMicroframe;
        int numTracks; // Last will be for program use

        byte[] audio;
        byte[][] trackBuffers;
        public readonly bool[] Mutes;
        Reverb[] reverbs;
        readonly DirectSoundChannel[] dsChannels;
        readonly SquareChannel sq1, sq2;
        readonly WaveChannel wave;
        readonly NoiseChannel noise;
        readonly Channel[] allChannels;
        readonly GBChannel[] gbChannels;

        BufferedWaveProvider buffer;
        WasapiOut @out;
        AudioSessionControl appVolume;

        private SoundMixer()
        {
            dsChannels = new DirectSoundChannel[Config.Instance.DirectCount];
            for (int i = 0; i < Config.Instance.DirectCount; i++)
            {
                dsChannels[i] = new DirectSoundChannel();
            }

            gbChannels = new GBChannel[] { sq1 = new SquareChannel(), sq2 = new SquareChannel(), wave = new WaveChannel(), noise = new NoiseChannel() };
            allChannels = ((Channel[])dsChannels).Union(gbChannels).ToArray();

            Mutes = new bool[17]; // 0-15 for tracks, 16 for the program
        }
        public void Init(byte reverbAmt)
        {
            SamplesPerBuffer = (int)(ROM.Instance.Game.Engine.Frequency / Engine.AGB_FPS);
            SampleRateReciprocal = 1f / ROM.Instance.Game.Engine.Frequency; SamplesReciprocal = 1f / SamplesPerBuffer;
            buffer = new BufferedWaveProvider(new WaveFormat(ROM.Instance.Game.Engine.Frequency, 8, 2))
            {
                DiscardOnBufferOverflow = true
            };
            @out?.Stop();
            @out = new WasapiOut();
            @out.Init(buffer);
            if (appVolume == null)
            {
                SessionCollection sessions = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).AudioSessionManager.Sessions;
                int id = System.Diagnostics.Process.GetCurrentProcess().Id;
                for (int i = 0; i < sessions.Count; i++)
                {
                    AudioSessionControl session = sessions[i];
                    if (session.GetProcessID == id)
                    {
                        appVolume = session;
                        appVolume.RegisterEventClient(this);
                        break;
                    }
                }
            }
            @out.Play();
            int amt = SamplesPerBuffer * 2;
            audio = new byte[amt];
            DSMasterVolume = ROM.Instance.Game.Engine.Volume / (float)0xF;
            numTracks = 16 + 1; // 1 for program use

            trackBuffers = new byte[numTracks][];
            reverbs = new Reverb[numTracks];

            for (int i = 0; i < numTracks; i++)
            {
                trackBuffers[i] = new byte[amt];
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
                byte[] buf = trackBuffers[i];
                Array.Clear(buf, 0, buf.Length);
            }
            Array.Clear(audio, 0, audio.Length);

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

            float fromMaster = 1f, toMaster = 1f;
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
                byte[] buf = trackBuffers[i];
                for (int j = 0; j < SamplesPerBuffer; j++)
                {
                    audio[j * 2] += (byte)(buf[j * 2] * masterLevel);
                    audio[j * 2 + 1] += (byte)(buf[j * 2 + 1] * masterLevel);

                    masterLevel += masterStep;
                }
            }

            buffer.AddSamples(audio, 0, audio.Length);
        }

        bool ignoreVolChangeFromUI = false;
        public void OnVolumeChanged(float volume, bool isMuted)
        {
            if (!ignoreVolChangeFromUI)
            {
                MainForm.Instance.SetVolumeBarValue(volume);
            }
            ignoreVolChangeFromUI = false;
        }
        public void OnDisplayNameChanged(string displayName)
        {
            throw new NotImplementedException();
        }
        public void OnIconPathChanged(string iconPath)
        {
            throw new NotImplementedException();
        }
        public void OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex)
        {
            throw new NotImplementedException();
        }
        public void OnGroupingParamChanged(ref Guid groupingId)
        {
            throw new NotImplementedException();
        }
        // Fires on @out.Play() and @out.Stop()
        public void OnStateChanged(AudioSessionState state)
        {
            if (state == AudioSessionState.AudioSessionStateActive)
            {
                OnVolumeChanged(appVolume.SimpleAudioVolume.Volume, appVolume.SimpleAudioVolume.Mute);
            }
        }
        public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason)
        {
            throw new NotImplementedException();
        }
        public void SetVolume(float volume)
        {
            ignoreVolChangeFromUI = true;
            appVolume.SimpleAudioVolume.Volume = volume;
        }
    }
}
