using Kermalis.VGMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core.GBA.M4A
{
    internal class Player : IPlayer
    {
        private readonly Mixer mixer;
        private readonly Config config;
        private readonly TimeBarrier time;
        private readonly Thread thread;
        private int voiceTableOffset;
        private Track[] tracks;
        private ushort tempo;
        private int tempoStack;
        private long loops;
        private bool fadeOutBegan;

        public List<SongEvent>[] Events { get; private set; }

        public PlayerState State { get; private set; }
        public event SongEndedEvent SongEnded;

        public Player(Mixer mixer, Config config)
        {
            this.mixer = mixer;
            this.config = config;

            time = new TimeBarrier(GBA.Utils.AGB_FPS);
            thread = new Thread(Tick) { Name = "M4APlayer Tick" };
            thread.Start();
        }

        private void SetTicks()
        {
            for (int trackIndex = 0; trackIndex < Events.Length; trackIndex++)
            {
                List<SongEvent> track = Events[trackIndex];
                int ticks = 0, endOfPattern = -1;
                for (int i = 0; i < track.Count; i++)
                {
                    SongEvent e = track[i];
                    if (endOfPattern == -1)
                    {
                        e.Ticks = ticks;
                    }
                    switch (e.Command)
                    {
                        case CallCommand call:
                        {
                            int callCmd = track.FindIndex(c => c.Offset == call.Offset);
                            if (callCmd == -1)
                            {
                                throw new Exception($"A call event has an invalid call offset.");
                            }
                            endOfPattern = i;
                            i = callCmd - 1;
                            break;
                        }
                        case JumpCommand jump:
                        {
                            int jumpCmd = track.FindIndex(c => c.Offset == jump.Offset);
                            if (jumpCmd == -1)
                            {
                                throw new Exception($"A jump event has an invalid jump offset.");
                            }
                            break;
                        }
                        case RestCommand rest: ticks += rest.Rest; break;
                        case ReturnCommand _:
                        {
                            if (endOfPattern != -1)
                            {
                                i = endOfPattern;
                                endOfPattern = -1;
                            }
                            break;
                        }
                    }
                }
            }
        }
        public void LoadSong(long index)
        {
            SongEntry entry = config.Reader.ReadObject<SongEntry>(config.SongTableOffsets[0] + (index * 8));
            SongHeader header = config.Reader.ReadObject<SongHeader>(entry.HeaderOffset - GBA.Utils.CartridgeOffset);
            voiceTableOffset = header.VoiceTableOffset - GBA.Utils.CartridgeOffset;
            tracks = new Track[header.NumTracks];
            Events = new List<SongEvent>[header.NumTracks];
            for (byte i = 0; i < header.NumTracks; i++)
            {
                tracks[i] = new Track(i);
                Events[i] = new List<SongEvent>();

                config.Reader.BaseStream.Position = header.TrackOffsets[i] - GBA.Utils.CartridgeOffset;

                byte cmd = 0, runCmd = 0, prevKey = 0, prevVelocity = 0x7F;
                long totalTicks = 0;
                while (cmd != 0xB1 && cmd != 0xB6)
                {
                    long offset = config.Reader.BaseStream.Position;
                    long ticks = totalTicks;
                    ICommand command = null;

                    void AddNoteEvent(byte key, byte velocity, byte addedDuration)
                    {
                        command = new NoteCommand
                        {
                            Key = prevKey = key,
                            Velocity = prevVelocity = velocity,
                            Duration = runCmd == 0xCF ? -1 : (Utils.ClockTable[runCmd - 0xCF] + addedDuration)
                        };
                    }

                    cmd = config.Reader.ReadByte();
                    if (cmd >= 0xBD) // Commands that work within running status
                    {
                        runCmd = cmd;
                    }

                    #region TIE & Notes

                    if (runCmd >= 0xCF && cmd <= 0x7F) // Within running status
                    {
                        byte[] peek = config.Reader.PeekBytes(2);
                        if (peek[0] > 0x7F)
                        {
                            AddNoteEvent(cmd, prevVelocity, 0);
                        }
                        else if (peek[1] > 3)
                        {
                            AddNoteEvent(cmd, config.Reader.ReadByte(), 0);
                        }
                        else
                        {
                            AddNoteEvent(cmd, config.Reader.ReadByte(), config.Reader.ReadByte());
                        }
                    }
                    else if (cmd >= 0xCF)
                    {
                        byte[] peek = config.Reader.PeekBytes(3);
                        if (peek[0] > 0x7F)
                        {
                            AddNoteEvent(prevKey, prevVelocity, 0);
                        }
                        else if (peek[1] > 0x7F)
                        {
                            AddNoteEvent(config.Reader.ReadByte(), prevVelocity, 0);
                        }
                        // TIE (0xCF) cannot have an added duration so it needs to stop here
                        else if (cmd == 0xCF || peek[2] > 3)
                        {
                            AddNoteEvent(config.Reader.ReadByte(), config.Reader.ReadByte(), 0);
                        }
                        else
                        {
                            AddNoteEvent(config.Reader.ReadByte(), config.Reader.ReadByte(), config.Reader.ReadByte());
                        }
                    }

                    #endregion

                    #region Rests

                    else if (cmd >= 0x80 && cmd <= 0xB0)
                    {
                        command = new RestCommand { Rest = Utils.ClockTable[cmd - 0x80] };
                    }

                    #endregion

                    #region Commands

                    else if (runCmd < 0xCF && cmd <= 0x7F)
                    {
                        switch (runCmd)
                        {
                            case 0xBD: command = new VoiceCommand { Voice = cmd }; break;
                            case 0xBE: command = new VolumeCommand { Volume = cmd }; break;
                            case 0xBF: command = new PanpotCommand { Panpot = (sbyte)(cmd - 0x40) }; break;
                            case 0xC0: command = new PitchBendCommand { Bend = (sbyte)(cmd - 0x40) }; break;
                            case 0xC1: command = new PitchBendRangeCommand { Range = cmd }; break;
                            case 0xC2: command = new LFOSpeedCommand { Speed = cmd }; break;
                            case 0xC3: command = new LFODelayCommand { Delay = cmd }; break;
                            case 0xC4: command = new LFODepthCommand { Depth = cmd }; break;
                            case 0xC5: command = new LFOTypeCommand { Type = cmd }; break;
                            case 0xC8: command = new TuneCommand { Tune = (sbyte)(cmd - 0x40) }; break;
                            case 0xCD: command = new LibraryCommand { Command = cmd, Argument = config.Reader.ReadByte() }; break;
                            case 0xCE: command = new EndOfTieCommand { Key = (sbyte)cmd }; prevKey = cmd; break;
                            default: Console.WriteLine("Invalid running status command at 0x{0:X7}: 0x{1:X}", offset, runCmd); break;
                        }
                    }
                    else if (cmd > 0xB0 && cmd < 0xCF)
                    {
                        switch (cmd)
                        {
                            case 0xB1:
                            case 0xB6: command = new FinishCommand { Type = cmd }; break;
                            case 0xB2: command = new JumpCommand { Offset = config.Reader.ReadInt32() - GBA.Utils.CartridgeOffset }; break;
                            case 0xB3: command = new CallCommand { Offset = config.Reader.ReadInt32() - GBA.Utils.CartridgeOffset }; break;
                            case 0xB4: command = new ReturnCommand(); break;
                            case 0xB5: command = new RepeatCommand { Times = config.Reader.ReadByte(), Offset = config.Reader.ReadInt32() - GBA.Utils.CartridgeOffset }; break;
                            case 0xB9: command = new MemoryAccessCommand { Operator = config.Reader.ReadByte(), Address = config.Reader.ReadByte(), Data = config.Reader.ReadByte() }; break;
                            case 0xBA: command = new PriorityCommand { Priority = config.Reader.ReadByte() }; break;
                            case 0xBB: command = new TempoCommand { Tempo = (ushort)(config.Reader.ReadByte() * 2) }; break;
                            case 0xBC: command = new TransposeCommand { Transpose = config.Reader.ReadSByte() }; break;
                            // Commands that work within running status:
                            case 0xBD: command = new VoiceCommand { Voice = config.Reader.ReadByte() }; break;
                            case 0xBE: command = new VolumeCommand { Volume = config.Reader.ReadByte() }; break;
                            case 0xBF: command = new PanpotCommand { Panpot = (sbyte)(config.Reader.ReadByte() - 0x40) }; break;
                            case 0xC0: command = new PitchBendCommand { Bend = (sbyte)(config.Reader.ReadByte() - 0x40) }; break;
                            case 0xC1: command = new PitchBendRangeCommand { Range = config.Reader.ReadByte() }; break;
                            case 0xC2: command = new LFOSpeedCommand { Speed = config.Reader.ReadByte() }; break;
                            case 0xC3: command = new LFODelayCommand { Delay = config.Reader.ReadByte() }; break;
                            case 0xC4: command = new LFODepthCommand { Depth = config.Reader.ReadByte() }; break;
                            case 0xC5: command = new LFOTypeCommand { Type = config.Reader.ReadByte() }; break;
                            case 0xC8: command = new TuneCommand { Tune = (sbyte)(config.Reader.ReadByte() - 0x40) }; break;
                            case 0xCD: command = new LibraryCommand { Command = config.Reader.ReadByte(), Argument = config.Reader.ReadByte() }; break;
                            case 0xCE:
                            {
                                int key;
                                if (config.Reader.PeekByte() <= 0x7F)
                                {
                                    key = config.Reader.ReadSByte();
                                    prevKey = (byte)key;
                                }
                                else
                                {
                                    key = -1;
                                }
                                command = new EndOfTieCommand { Key = key };
                                break;
                            }
                            default: Console.WriteLine("Invalid command at 0x{0:X7}: 0x{1:X}", offset, cmd); break;
                        }
                    }

                    #endregion

                    Events[i].Add(new SongEvent(offset, command));
                }
            }
            SetTicks();
        }
        public void Play()
        {
            Stop();
            tempo = 150;
            tempoStack = 0;
            loops = 0;
            fadeOutBegan = false;
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
            for (int i = 0; i < tracks.Length; i++)
            {
                tracks[i].StopAllChannels();
            }
        }
        public void Dispose()
        {
            Stop();
            State = PlayerState.ShutDown;
            thread.Join();
        }
        public void GetSongState(UI.TrackInfoControl.TrackInfo info)
        {
            info.Tempo = tempo;
            for (int i = 0; i < tracks.Length; i++)
            {
                Track track = tracks[i];
                info.Positions[i] = Events[i][track.CurEvent].Offset;
                info.Delays[i] = track.Delay;
                info.Voices[i] = track.Voice;
                info.Mods[i] = track.LFODepth;
                //info.Types[i] = "PCM";
                info.Volumes[i] = track.GetVolume();
                info.Pitches[i] = track.GetPitch();
                info.Panpots[i] = track.GetPanpot();

                Channel[] channels = track.Channels.ToArray(); // Copy so adding and removing from the other thread doesn't interrupt (plus Array looping is faster than List looping)
                if (channels.Length == 0)
                {
                    info.Notes[i] = new byte[0];
                    info.Lefts[i] = 0;
                    info.Rights[i] = 0;
                }
                else
                {
                    float[] lefts = new float[channels.Length];
                    float[] rights = new float[channels.Length];
                    for (int j = 0; j < channels.Length; j++)
                    {
                        ChannelVolume vol = channels[j].GetVolume();
                        lefts[j] = vol.LeftVol;
                        rights[j] = vol.RightVol;
                    }
                    info.Notes[i] = channels.Where(c => c.State < EnvelopeState.Releasing).Select(c => c.Note.OriginalKey).ToArray();
                    info.Lefts[i] = lefts.Max();
                    info.Rights[i] = rights.Max();
                }
            }
        }

        private void PlayNote(Track track, byte key, byte velocity, int duration)
        {
            key = (byte)(key + track.Transpose).Clamp(0, 0x7F);
            track.PrevKey = key;
            if (track.Ready)
            {
                bool fromDrum = false;
                int offset = voiceTableOffset + (track.Voice * 12);
                while (true)
                {
                    VoiceEntry v = config.Reader.ReadObject<VoiceEntry>(offset);
                    if (v.Type == (int)VoiceFlags.KeySplit)
                    {
                        fromDrum = false; // In case there is a multi within a drum
                        byte inst = config.Reader.ReadByte(v.Int8 - GBA.Utils.CartridgeOffset + key);
                        offset = v.Int4 - GBA.Utils.CartridgeOffset + (inst * 12);
                    }
                    else if (v.Type == (int)VoiceFlags.Drum)
                    {
                        fromDrum = true;
                        offset = v.Int4 - GBA.Utils.CartridgeOffset + (key * 12);
                    }
                    else
                    {
                        var note = new Note
                        {
                            Duration = duration,
                            Velocity = velocity,
                            OriginalKey = key,
                            Key = fromDrum ? v.RootKey : key
                        };
                        var type = (VoiceType)(v.Type & 0x7);
                        switch (type)
                        {
                            case VoiceType.PCM8:
                            {
                                bool bFixed = (v.Type & (int)VoiceFlags.Fixed) == (int)VoiceFlags.Fixed;
                                bool bCompressed = false;//ROM.Instance.Game.Engine.HasPokemonCompression && (v.Type & (int)VoiceFlags.Compressed) == (int)VoiceFlags.Compressed;
                                mixer.AllocPCM8Channel(track, v.ADSR, note,
                                    track.GetVolume(), track.GetPanpot(), track.GetPitch(),
                                    bFixed, bCompressed, v.Int4 - GBA.Utils.CartridgeOffset);
                                return;
                            }
                            case VoiceType.Square1:
                            case VoiceType.Square2:
                            {
                                mixer.AllocPSGChannel(track, v.ADSR, note,
                                    track.GetVolume(), track.GetPanpot(), track.GetPitch(),
                                    type, (SquarePattern)v.Int4);
                                return;
                            }
                            case VoiceType.PCM4:
                            {
                                mixer.AllocPSGChannel(track, v.ADSR, note,
                                    track.GetVolume(), track.GetPanpot(), track.GetPitch(),
                                    type, v.Int4 - GBA.Utils.CartridgeOffset);
                                return;
                            }
                            case VoiceType.Noise:
                            {
                                mixer.AllocPSGChannel(track, v.ADSR, note,
                                    track.GetVolume(), track.GetPanpot(), track.GetPitch(),
                                    type, (NoisePattern)v.Int4);
                                return;
                            }
                        }
                    }
                }
            }
        }
        private void ExecuteNext(int i, ref bool update, ref bool loop)
        {
            bool increment = true;
            List<SongEvent> ev = Events[i];
            Track track = tracks[i];
            switch (ev[track.CurEvent].Command)
            {
                case CallCommand call:
                {
                    int callCmd = ev.FindIndex(c => c.Offset == call.Offset);
                    track.EndOfPattern = track.CurEvent + 1;
                    track.CurEvent = callCmd;
                    increment = false;
                    break;
                }
                case EndOfTieCommand eot:
                {
                    if (eot.Key == -1)
                    {
                        track.ReleaseChannels(track.PrevKey);
                    }
                    else
                    {
                        track.ReleaseChannels((byte)(eot.Key + track.Transpose).Clamp(0, 0x7F));
                    }
                    break;
                }
                case FinishCommand _:
                {
                    track.Stopped = true;
                    increment = false;
                    //track.ReleaseAllTieingChannels();
                    break;
                }
                case JumpCommand jump:
                {
                    int jumpCmd = ev.FindIndex(c => c.Offset == jump.Offset);
                    track.CurEvent = jumpCmd;
                    loop = true;
                    increment = false;
                    break;
                }
                case LFODelayCommand lfodl: track.LFODelay = lfodl.Delay; track.LFOPhase = track.LFODelayCount = 0; update = true; break;
                case LFODepthCommand lfo: track.LFODepth = lfo.Depth; update = true; break;
                case LFOSpeedCommand lfos: track.LFOSpeed = lfos.Speed; track.LFOPhase = track.LFODelayCount = 0; update = true; break;
                case LFOTypeCommand lfot: track.LFOType = (LFOType)lfot.Type; update = true; break;
                case NoteCommand note: PlayNote(track, note.Key, note.Velocity, note.Duration); break;
                case PanpotCommand pan: track.Panpot = pan.Panpot; update = true; break;
                case PitchBendCommand bend: track.PitchBend = bend.Bend; update = true; break;
                case PitchBendRangeCommand bendr: track.PitchBendRange = bendr.Range; update = true; break;
                case PriorityCommand priority: track.Priority = priority.Priority; break;
                case RestCommand rest: track.Delay = rest.Rest; break;
                case ReturnCommand _:
                {
                    if (track.EndOfPattern != -1)
                    {
                        track.CurEvent = track.EndOfPattern;
                        track.EndOfPattern = -1;
                        increment = false;
                    }
                    break;
                }
                case TempoCommand tempo: this.tempo = tempo.Tempo; break;
                case TransposeCommand transpose: track.Transpose = transpose.Transpose; break;
                case TuneCommand tune: track.Tune = tune.Tune; update = true; break;
                case VoiceCommand voice: track.Voice = voice.Voice; track.Ready = true; break;
                case VolumeCommand volume: track.Volume = volume.Volume; update = true; break;
            }
            if (increment)
            {
                track.CurEvent++;
            }
        }

        private void Tick()
        {
            time.Start();
            while (State != PlayerState.ShutDown)
            {
                if (State == PlayerState.Playing)
                {
                    tempoStack += tempo;
                    while (tempoStack >= 150)
                    {
                        tempoStack -= 150;
                        bool allDone = true, loop = false;
                        for (int i = 0; i < tracks.Length; i++)
                        {
                            Track track = tracks[i];
                            track.Tick();
                            bool update = false;
                            while (track.Delay == 0 && !track.Stopped)
                            {
                                ExecuteNext(i, ref update, ref loop);
                            }
                            if (update || track.LFODepth > 0)
                            {
                                track.UpdateChannels();
                            }
                            if (!track.Stopped || track.Channels.Count != 0)
                            {
                                allDone = false;
                            }
                        }
                        if (loop)
                        {
                            loops++;
                            if (UI.MainForm.Instance.PlaylistPlaying && !fadeOutBegan && loops > GlobalConfig.Instance.PlaylistSongLoops)
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
                    mixer.Process();
                }
                time.Wait();
            }
            time.Stop();
        }
    }
}
