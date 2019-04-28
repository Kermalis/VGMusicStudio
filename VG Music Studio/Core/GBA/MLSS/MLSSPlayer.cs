using Kermalis.VGMusicStudio.Util;
using System;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core.GBA.MLSS
{
    internal class MLSSPlayer : IPlayer
    {
        private readonly Track[] tracks = new Track[0x10];
        private readonly MLSSMixer mixer;
        private readonly MLSSConfig config;
        private readonly TimeBarrier time;
        private readonly Thread thread;
        private byte tempo;
        private int tempoStack;

        public PlayerState State { get; private set; }
        public event SongEndedEvent SongEnded;

        public MLSSPlayer(MLSSMixer mixer, MLSSConfig config)
        {
            for (byte i = 0; i < tracks.Length; i++)
            {
                tracks[i] = new Track(i, config.ROM, mixer);
            }
            this.mixer = mixer;
            this.config = config;

            time = new TimeBarrier(GBAUtils.AGB_FPS);
            thread = new Thread(Tick) { Name = "MLSSPlayer Tick" };
            thread.Start();
        }

        public void LoadSong(long index)
        {
            int songOffset = config.Reader.ReadInt32(config.SongTableOffsets[0] + (index * 4)) - GBAUtils.CartridgeOffset;
            ushort trackBits = config.Reader.ReadUInt16(songOffset);
            for (int i = 0; i < 0x10; i++)
            {
                if ((trackBits & (1 << i)) != 0)
                {
                    tracks[i].Enabled = true;
                    tracks[i].StartOffset = songOffset + config.Reader.ReadUInt16();
                }
                else
                {
                    tracks[i].Enabled = false;
                    tracks[i].StartOffset = 0;
                }
            }
        }
        public void Play()
        {
            Stop();
            tempoStack = 0;
            tempo = 120;
            for (int i = 0; i < tracks.Length; i++)
            {
                tracks[i].Init();
            }
            State = PlayerState.Playing;
        }
        public void Pause()
        {
            State = State == PlayerState.Paused ? PlayerState.Playing : PlayerState.Paused;
        }
        public void Stop()
        {
            if (State == PlayerState.Stopped)
            {
                return;
            }
            State = PlayerState.Stopped;
        }
        public void ShutDown()
        {
            // TODO: Dispose tracks and track readers
            Stop();
            State = PlayerState.ShutDown;
            thread.Join();
        }
        public void GetSongState(UI.TrackInfoControl.TrackInfo info)
        {
            info.Tempo = tempo;
            for (int i = 0; i < 0x10; i++)
            {
                Track track = tracks[i];
                if (track.Enabled)
                {
                    info.Positions[i] = track.Reader.BaseStream.Position;
                    info.Delays[i] = track.Delay;
                    info.Voices[i] = track.Voice;
                    info.Types[i] = track.Type;
                    info.Volumes[i] = track.Volume;
                    info.Pitches[i] = track.GetPitch();
                    info.Panpots[i] = track.Panpot;
                    if (track.NoteDuration != 0 && !track.Channel.Stopped)
                    {
                        info.Notes[i] = new byte[] { track.Channel.Key };
                        ChannelVolume vol = track.Channel.GetVolume();
                        info.Lefts[i] = vol.LeftVol;
                        info.Rights[i] = vol.RightVol;
                    }
                    else
                    {
                        info.Notes[i] = Array.Empty<byte>();
                        info.Lefts[i] = info.Rights[i] = 0f;
                    }
                }
            }
        }

        private void PlayNote(Track track, byte key, byte duration)
        {
            short voiceOffset = config.Reader.ReadInt16(config.VoiceTableOffset + (track.Voice * 2));
            short nextVoiceOffset = config.Reader.ReadInt16(config.VoiceTableOffset + ((track.Voice + 1) * 2));
            int numEntries = (nextVoiceOffset - voiceOffset) / 8; // Each entry is 8 bytes
            VoiceEntry entry = null;
            config.Reader.BaseStream.Position = config.VoiceTableOffset + voiceOffset;
            for (int i = 0; i < numEntries; i++)
            {
                VoiceEntry e = config.Reader.ReadObject<VoiceEntry>();
                if (e.MinKey <= key && e.MaxKey >= key)
                {
                    entry = e;
                    break;
                }
            }
            if (entry != null)
            {
                track.NoteDuration = duration;
                if (track.Index >= 8)
                {
                    // TODO: "Sample" byte in VoiceEntry
                    // TODO: Check if this check is necessary
                    if (track.Voice >= 190 && track.Voice < 200)
                    {
                        ((SquareChannel)track.Channel).Init(key, new ADSR { A = 0xFF, D = 0x00, S = 0xFF, R = 0x00 }, track.Volume, track.Panpot, track.GetPitch());
                    }
                }
                else
                {
                    int sampleOffset = config.Reader.ReadInt32(config.SampleTableOffset + (entry.Sample * 4)); // Some entries are 0. If you play them, are they silent, or does it not care if they are 0?
                    ((PCMChannel)track.Channel).Init(key, new ADSR { A = 0xFF, D = 0x00, S = 0xFF, R = 0x00 }, config.SampleTableOffset + sampleOffset, entry.IsFixedFrequency == 0x80);
                    track.Channel.SetVolume(track.Volume, track.Panpot);
                    track.Channel.SetPitch(track.GetPitch());
                }
            }
        }
        private void ExecuteNext(Track track, ref bool update)
        {
            byte cmd = track.Reader.ReadByte();
            switch (cmd)
            {
                case 0x00:
                {
                    byte key = (byte)(track.Reader.ReadByte() - 0x80);
                    byte duration = track.Reader.ReadByte();
                    track.Delay += duration;
                    if (track.PrevCmd == 0x00 && track.Channel.Key == key)
                    {
                        track.NoteDuration += duration;
                    }
                    else
                    {
                        PlayNote(track, key, duration);
                    }
                    break;
                }
                case 0xF0: track.Voice = track.Reader.ReadByte(); break;
                case 0xF1: track.Volume = track.Reader.ReadByte(); update = true; break;
                case 0xF2: track.Panpot = (sbyte)(track.Reader.ReadByte() - 0x80); update = true; break;
                case 0xF4: track.BendRange = track.Reader.ReadByte(); update = true; break;
                case 0xF5: track.Bend = track.Reader.ReadSByte(); update = true; break;
                case 0xF6: track.Delay = track.Reader.ReadByte(); break;
                case 0xF8:
                {
                    short offset = track.Reader.ReadInt16();
                    track.Reader.BaseStream.Position += offset;
                    break;
                }
                case 0xF9: tempo = track.Reader.ReadByte(); break;
                case 0xFF: track.Stopped = true; break;
                default: // (0x01-0xEF)
                {
                    byte key = track.Reader.ReadByte();
                    track.Delay += cmd;
                    if (track.PrevCmd == 0x00 && track.Channel.Key == key)
                    {
                        track.NoteDuration += cmd;
                    }
                    else
                    {
                        PlayNote(track, key, cmd);
                    }
                    break;
                }
                case 0xF3:
                case 0xF7:
                case 0xFA:
                case 0xFB:
                case 0xFC:
                case 0xFD:
                case 0xFE: Console.WriteLine("Unknown command at 0x{0:X}: 0x{1:X}", track.Reader.BaseStream.Position, cmd); break;
            }
            track.PrevCmd = cmd;
        }

        private void Tick()
        {
            time.Start();
            while (State != PlayerState.ShutDown)
            {
                if (State == PlayerState.Playing)
                {
                    tempoStack += tempo;
                    while (tempoStack >= 75)
                    {
                        tempoStack -= 75;
                        bool allDone = true;
                        for (int i = 0; i < tracks.Length; i++)
                        {
                            Track track = tracks[i];
                            if (track.Enabled)
                            {
                                byte prevDuration = track.NoteDuration;
                                track.Tick();
                                bool update = false;
                                while (track.Delay == 0 && !track.Stopped)
                                {
                                    ExecuteNext(track, ref update);
                                }
                                if (prevDuration == 1 && track.NoteDuration == 0) // Note was not renewed
                                {
                                    track.Channel.State = EnvelopeState.Release;
                                }
                                if (update)
                                {
                                    track.Channel.SetVolume(track.Volume, track.Panpot);
                                    track.Channel.SetPitch(track.GetPitch());
                                }
                                if (!track.Stopped || track.NoteDuration != 0)
                                {
                                    allDone = false;
                                }
                            }
                        }
                        if (allDone)
                        {
                            Stop();
                            SongEnded?.Invoke();
                        }
                    }
                    mixer.Process(tracks);
                }
                time.Wait();
            }
            time.Stop();
        }
    }
}
