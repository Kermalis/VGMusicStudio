using Kermalis.VGMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core.GBA.MLSS
{
    internal class Player : IPlayer
    {
        private readonly Track[] tracks = new Track[0x10];
        private readonly Mixer mixer;
        private readonly Config config;
        private readonly TimeBarrier time;
        private readonly Thread thread;
        private byte tempo;
        private int tempoStack;
        private long elapsedLoops, elapsedTicks;
        private bool fadeOutBegan;

        public List<SongEvent>[] Events { get; private set; }
        public long NumTicks { get; private set; }

        public PlayerState State { get; private set; }
        public event SongEndedEvent SongEnded;

        public Player(Mixer mixer, Config config)
        {
            for (byte i = 0; i < tracks.Length; i++)
            {
                tracks[i] = new Track(i, config.ROM, mixer);
            }
            this.mixer = mixer;
            this.config = config;

            time = new TimeBarrier(Utils.AGB_FPS);
            thread = new Thread(Tick) { Name = "MLSSPlayer Tick" };
            thread.Start();
        }

        private void SetTicks()
        {
            for (int trackIndex = 0; trackIndex < 0x10; trackIndex++)
            {
                if (Events[trackIndex] != null)
                {
                    Events[trackIndex] = Events[trackIndex].OrderBy(e => e.Offset).ToList();
                    List<SongEvent> evs = Events[trackIndex];
                    long ticks = 0;
                    bool cont = true;
                    int i = 0;
                    while (cont)
                    {
                        SongEvent e = evs[i++];
                        e.Ticks.Add(ticks);
                        switch (e.Command)
                        {
                            case FinishCommand _:
                            {
                                cont = false;
                                break;
                            }
                            case FreeNoteCommand freeNote:
                            {
                                ticks += freeNote.Duration;
                                break;
                            }
                            case JumpCommand jump:
                            {
                                int jumpCmd = evs.FindIndex(c => c.Offset == jump.Offset);
                                if (evs[jumpCmd].Offset > e.Offset || evs[jumpCmd].Ticks.Count == 0)
                                {
                                    i = jumpCmd;
                                }
                                else
                                {
                                    cont = false;
                                }
                                break;
                            }
                            case NoteCommand note:
                            {
                                ticks += note.Duration;
                                break;
                            }
                            case RestCommand rest:
                            {
                                ticks += rest.Rest;
                                break;
                            }
                        }
                    }
                }
            }
        }
        public void LoadSong(long index)
        {
            int songOffset = config.Reader.ReadInt32(config.SongTableOffsets[0] + (index * 4));
            if (songOffset == 0)
            {
                Events = null;
            }
            else
            {
                Events = new List<SongEvent>[0x10];
                songOffset -= Utils.CartridgeOffset;
                ushort trackBits = config.Reader.ReadUInt16(songOffset);
                for (int i = 0, usedTracks = 0; i < 0x10; i++)
                {
                    Track track = tracks[i];
                    if ((trackBits & (1 << i)) != 0)
                    {
                        track.Enabled = true;
                        track.StartOffset = songOffset + config.Reader.ReadInt16(songOffset + 2 + (2 * usedTracks++));
                        Events[i] = new List<SongEvent>();
                        bool EventExists(long offset)
                        {
                            return Events[i].Any(e => e.Offset == offset);
                        }

                        AddEvents(track.StartOffset);
                        void AddEvents(int startOffset)
                        {
                            config.Reader.BaseStream.Position = startOffset;
                            bool cont = true;
                            while (cont)
                            {
                                long offset = config.Reader.BaseStream.Position;
                                void AddEvent(ICommand command)
                                {
                                    Events[i].Add(new SongEvent(offset, command));
                                }
                                byte cmd = config.Reader.ReadByte();
                                switch (cmd)
                                {
                                    case 0x00:
                                    {
                                        byte keyArg = config.Reader.ReadByte();
                                        byte duration = config.Reader.ReadByte();
                                        if (!EventExists(offset))
                                        {
                                            AddEvent(new FreeNoteCommand { Key = (byte)(keyArg - 0x80), Duration = duration });
                                        }
                                        break;
                                    }
                                    case 0xF0:
                                    {
                                        byte voice = config.Reader.ReadByte();
                                        if (!EventExists(offset))
                                        {
                                            AddEvent(new VoiceCommand { Voice = voice });
                                        }
                                        break;
                                    }
                                    case 0xF1:
                                    {
                                        byte volume = config.Reader.ReadByte();
                                        if (!EventExists(offset))
                                        {
                                            AddEvent(new VolumeCommand { Volume = volume });
                                        }
                                        break;
                                    }
                                    case 0xF2:
                                    {
                                        byte panArg = config.Reader.ReadByte();
                                        if (!EventExists(offset))
                                        {
                                            AddEvent(new PanpotCommand { Panpot = (sbyte)(panArg - 0x80) });
                                        }
                                        break;
                                    }
                                    case 0xF4:
                                    {
                                        byte range = config.Reader.ReadByte();
                                        if (!EventExists(offset))
                                        {
                                            AddEvent(new PitchBendRangeCommand { Range = range });
                                        }
                                        break;
                                    }
                                    case 0xF5:
                                    {
                                        sbyte bend = config.Reader.ReadSByte();
                                        if (!EventExists(offset))
                                        {
                                            AddEvent(new PitchBendCommand { Bend = bend });
                                        }
                                        break;
                                    }
                                    case 0xF6:
                                    {
                                        byte rest = config.Reader.ReadByte();
                                        if (!EventExists(offset))
                                        {
                                            AddEvent(new RestCommand { Rest = rest });
                                        }
                                        break;
                                    }
                                    case 0xF8:
                                    {
                                        short jumpOffset = config.Reader.ReadInt16();
                                        if (!EventExists(offset))
                                        {
                                            int off = (int)(config.Reader.BaseStream.Position + jumpOffset);
                                            AddEvent(new JumpCommand { Offset = off });
                                            if (!EventExists(off))
                                            {
                                                AddEvents(off);
                                            }
                                        }
                                        cont = false;
                                        break;
                                    }
                                    case 0xF9:
                                    {
                                        byte tempoArg = config.Reader.ReadByte();
                                        if (!EventExists(offset))
                                        {
                                            AddEvent(new TempoCommand { Tempo = tempoArg });
                                        }
                                        break;
                                    }
                                    case 0xFF:
                                    {
                                        if (!EventExists(offset))
                                        {
                                            AddEvent(new FinishCommand());
                                        }
                                        cont = false;
                                        break;
                                    }
                                    default:
                                    {
                                        if (cmd <= 0xEF)
                                        {
                                            byte key = config.Reader.ReadByte();
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new NoteCommand { Key = key, Duration = cmd });
                                            }
                                        }
                                        else
                                        {
                                            throw new Exception($"Unknown command at 0x{offset:X7}: 0x{cmd:X}");
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        track.Enabled = false;
                        track.StartOffset = 0;
                    }
                }
                SetTicks();
            }
        }
        public void SetCurrentPosition(long ticks)
        {

        }
        public void Play()
        {
            Stop();
            tempo = 120;
            tempoStack = 0;
            elapsedLoops = elapsedTicks = 0;
            fadeOutBegan = false;
            for (int i = 0; i < 0x10; i++)
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
        public void Dispose()
        {
            Stop();
            State = PlayerState.ShutDown;
            thread.Join();
            for (int i = 0; i < 0x10; i++)
            {
                tracks[i].Reader.Dispose();
            }
        }
        public void GetSongState(UI.TrackInfoControl.TrackInfo info)
        {
            info.Tempo = tempo;
            info.Ticks = elapsedTicks;
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
                    info.PitchBends[i] = track.GetPitch();
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
        private void ExecuteNext(Track track, ref bool update, ref bool loop)
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
                    loop = true; // TODO: Check context of the jump (were we in this tick before?)
                    break;
                }
                case 0xF9: tempo = track.Reader.ReadByte(); break;
                case 0xFF: track.Stopped = true; break;
                default:
                {
                    if (cmd <= 0xEF)
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
                    }
                    else
                    {
                        Console.WriteLine("Unknown command at 0x{0:X7}: 0x{1:X}", track.Reader.BaseStream.Position - 1, cmd);
                    }
                    break;
                }
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
                        bool allDone = true, loop = false;
                        for (int i = 0; i < 0x10; i++)
                        {
                            Track track = tracks[i];
                            if (track.Enabled)
                            {
                                byte prevDuration = track.NoteDuration;
                                track.Tick();
                                bool update = false;
                                while (track.Delay == 0 && !track.Stopped)
                                {
                                    ExecuteNext(track, ref update, ref loop);
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
                        if (loop)
                        {
                            elapsedLoops++;
                            if (UI.MainForm.Instance.PlaylistPlaying && !fadeOutBegan && elapsedLoops > GlobalConfig.Instance.PlaylistSongLoops)
                            {
                                fadeOutBegan = true;
                                mixer.BeginFadeOut();
                            }
                        }
                        if (fadeOutBegan && mixer.IsFadeDone())
                        {
                            allDone = true;
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
