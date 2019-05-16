using Kermalis.VGMusicStudio.Properties;
using Kermalis.VGMusicStudio.Util;
using Sanford.Multimedia.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K
{
    internal class Player : IPlayer
    {
        private readonly Mixer mixer;
        private readonly Config config;
        private readonly TimeBarrier time;
        private readonly Thread thread;
        private int voiceTableOffset = -1;
        private Track[] tracks;
        private ushort tempo;
        private int tempoStack;
        private long elapsedLoops;
        private bool fadeOutBegan;

        public List<SongEvent>[] Events { get; private set; }
        public long MaxTicks { get; private set; }
        public long ElapsedTicks { get; private set; }
        private int longestTrack;

        public PlayerState State { get; private set; }
        public event SongEndedEvent SongEnded;

        public Player(Mixer mixer, Config config)
        {
            this.mixer = mixer;
            this.config = config;

            time = new TimeBarrier(GBA.Utils.AGB_FPS);
            thread = new Thread(Tick) { Name = "MP2K Player Tick" };
            thread.Start();
        }

        private void InitEmulation()
        {
            tempo = 150;
            tempoStack = 0;
            elapsedLoops = ElapsedTicks = 0;
            fadeOutBegan = false;
            for (int i = 0; i < tracks.Length; i++)
            {
                tracks[i].Init();
            }
        }
        private void SetTicks()
        {
            MaxTicks = 0;
            bool u = false;
            for (int trackIndex = 0; trackIndex < Events.Length; trackIndex++)
            {
                Events[trackIndex] = Events[trackIndex].OrderBy(e => e.Offset).ToList();
                List<SongEvent> evs = Events[trackIndex];
                Track track = tracks[trackIndex];
                track.Init();
                ElapsedTicks = 0;
                while (true)
                {
                    SongEvent e = evs[track.CurEvent];
                    if (track.CallStackDepth == 0 && e.Ticks.Count > 0)
                    {
                        break;
                    }
                    else
                    {
                        e.Ticks.Add(ElapsedTicks);
                        ExecuteNext(trackIndex, ref u);
                        if (track.Stopped)
                        {
                            break;
                        }
                        else
                        {
                            ElapsedTicks += track.Rest;
                            track.Rest = 0;
                        }
                    }
                }
                if (ElapsedTicks > MaxTicks)
                {
                    longestTrack = trackIndex;
                    MaxTicks = ElapsedTicks;
                }
                track.StopAllChannels();
            }
        }
        public void LoadSong(long index)
        {
            tracks = null;
            Events = null;
            SongEntry entry = config.Reader.ReadObject<SongEntry>(config.SongTableOffsets[0] + (index * 8));
            SongHeader header = config.Reader.ReadObject<SongHeader>(entry.HeaderOffset - GBA.Utils.CartridgeOffset);
            int oldVoiceTableOffset = voiceTableOffset;
            voiceTableOffset = header.VoiceTableOffset - GBA.Utils.CartridgeOffset;
            if (oldVoiceTableOffset != voiceTableOffset)
            {
                voiceTypeCache = new string[byte.MaxValue + 1];
            }
            tracks = new Track[header.NumTracks];
            Events = new List<SongEvent>[header.NumTracks];
            for (byte i = 0; i < header.NumTracks; i++)
            {
                tracks[i] = new Track(i);
                Events[i] = new List<SongEvent>();
                bool EventExists(long offset)
                {
                    return Events[i].Any(e => e.Offset == offset);
                }

                byte runCmd = 0, prevKey = 0, prevVelocity = 0x7F; // TODO: https://github.com/Kermalis/VGMusicStudio/issues/37
                int callStackDepth = 0;
                AddEvents(header.TrackOffsets[i] - GBA.Utils.CartridgeOffset);
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
                        void EmulateNote(byte key, byte velocity, byte addedDuration)
                        {
                            prevKey = key;
                            prevVelocity = velocity;
                            if (!EventExists(offset))
                            {
                                AddEvent(new NoteCommand
                                {
                                    Key = key,
                                    Velocity = velocity,
                                    Duration = runCmd == 0xCF ? -1 : (Utils.RestTable[runCmd - 0xCF] + addedDuration)
                                });
                            }
                        }

                        byte cmd = config.Reader.ReadByte();
                        if (cmd >= 0xBD) // Commands that work within running status
                        {
                            runCmd = cmd;
                        }

                        #region TIE & Notes

                        if (runCmd >= 0xCF && cmd <= 0x7F) // Within running status
                        {
                            byte velocity, addedDuration;
                            byte[] peek = config.Reader.PeekBytes(2);
                            if (peek[0] > 0x7F)
                            {
                                velocity = prevVelocity;
                                addedDuration = 0;
                            }
                            else if (peek[1] > 3)
                            {
                                velocity = config.Reader.ReadByte();
                                addedDuration = 0;
                            }
                            else
                            {
                                velocity = config.Reader.ReadByte();
                                addedDuration = config.Reader.ReadByte();
                            }
                            EmulateNote(cmd, velocity, addedDuration);
                        }
                        else if (cmd >= 0xCF)
                        {
                            byte key, velocity, addedDuration;
                            byte[] peek = config.Reader.PeekBytes(3);
                            if (peek[0] > 0x7F)
                            {
                                key = prevKey;
                                velocity = prevVelocity;
                                addedDuration = 0;
                            }
                            else if (peek[1] > 0x7F)
                            {
                                key = config.Reader.ReadByte();
                                velocity = prevVelocity;
                                addedDuration = 0;
                            }
                            // TIE (0xCF) cannot have an added duration so it needs to stop here
                            else if (cmd == 0xCF || peek[2] > 3)
                            {
                                key = config.Reader.ReadByte();
                                velocity = config.Reader.ReadByte();
                                addedDuration = 0;
                            }
                            else
                            {
                                key = config.Reader.ReadByte();
                                velocity = config.Reader.ReadByte();
                                addedDuration = config.Reader.ReadByte();
                            }
                            EmulateNote(key, velocity, addedDuration);
                        }

                        #endregion

                        #region Rests

                        else if (cmd >= 0x80 && cmd <= 0xB0)
                        {
                            if (!EventExists(offset))
                            {
                                AddEvent(new RestCommand { Rest = Utils.RestTable[cmd - 0x80] });
                            }
                        }

                        #endregion

                        #region Commands

                        else if (runCmd < 0xCF && cmd <= 0x7F)
                        {
                            switch (runCmd)
                            {
                                case 0xBD:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new VoiceCommand { Voice = cmd });
                                    }
                                    break;
                                }
                                case 0xBE:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new VolumeCommand { Volume = cmd });
                                    }
                                    break;
                                }
                                case 0xBF:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new PanpotCommand { Panpot = (sbyte)(cmd - 0x40) });
                                    }
                                    break;
                                }
                                case 0xC0:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new PitchBendCommand { Bend = (sbyte)(cmd - 0x40) });
                                    }
                                    break;
                                }
                                case 0xC1:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new PitchBendRangeCommand { Range = cmd });
                                    }
                                    break;
                                }
                                case 0xC2:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new LFOSpeedCommand { Speed = cmd });
                                    }
                                    break;
                                }
                                case 0xC3:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new LFODelayCommand { Delay = cmd });
                                    }
                                    break;
                                }
                                case 0xC4:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new LFODepthCommand { Depth = cmd });
                                    }
                                    break;
                                }
                                case 0xC5:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new LFOTypeCommand { Type = (LFOType)cmd });
                                    }
                                    break;
                                }
                                case 0xC8:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new TuneCommand { Tune = (sbyte)(cmd - 0x40) });
                                    }
                                    break;
                                }
                                case 0xCD:
                                {
                                    byte arg = config.Reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new LibraryCommand { Command = cmd, Argument = arg });
                                    }
                                    break;
                                }
                                case 0xCE:
                                {
                                    prevKey = cmd;
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new EndOfTieCommand { Key = cmd });
                                    }
                                    break;
                                }
                                default: throw new Exception(string.Format(Strings.ErrorMP2KInvalidRunningStatusCommand, i, offset, runCmd));
                            }
                        }
                        else if (cmd > 0xB0 && cmd < 0xCF)
                        {
                            switch (cmd)
                            {
                                case 0xB1:
                                case 0xB6:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new FinishCommand { Type = cmd });
                                    }
                                    cont = false;
                                    break;
                                }
                                case 0xB2:
                                {
                                    int jumpOffset = config.Reader.ReadInt32() - GBA.Utils.CartridgeOffset;
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new JumpCommand { Offset = jumpOffset });
                                        if (!EventExists(jumpOffset))
                                        {
                                            AddEvents(jumpOffset);
                                        }
                                    }
                                    cont = false;
                                    break;
                                }
                                case 0xB3:
                                {
                                    int callOffset = config.Reader.ReadInt32() - GBA.Utils.CartridgeOffset;
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new CallCommand { Offset = callOffset });
                                    }
                                    if (callStackDepth < 3)
                                    {
                                        long backup = config.Reader.BaseStream.Position;
                                        callStackDepth++;
                                        AddEvents(callOffset);
                                        config.Reader.BaseStream.Position = backup;
                                    }
                                    else
                                    {
                                        throw new Exception(string.Format(Strings.ErrorMP2KSDATNestedCalls, i));
                                    }
                                    break;
                                }
                                case 0xB4:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new ReturnCommand());
                                    }
                                    if (callStackDepth != 0)
                                    {
                                        cont = false;
                                        callStackDepth--;
                                    }
                                    break;
                                }
                                /*case 0xB5: // TODO: Logic so this isn't an infinite loop
                                {
                                    byte times = config.Reader.ReadByte();
                                    int repeatOffset = config.Reader.ReadInt32() - GBA.Utils.CartridgeOffset;
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new RepeatCommand { Times = times, Offset = repeatOffset });
                                    }
                                    break;
                                }*/
                                case 0xB9:
                                {
                                    byte op = config.Reader.ReadByte();
                                    byte address = config.Reader.ReadByte();
                                    byte data = config.Reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new MemoryAccessCommand { Operator = op, Address = address, Data = data });
                                    }
                                    break;
                                }
                                case 0xBA:
                                {
                                    byte priority = config.Reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new PriorityCommand { Priority = priority });
                                    }
                                    break;
                                }
                                case 0xBB:
                                {
                                    byte tempoArg = config.Reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new TempoCommand { Tempo = (ushort)(tempoArg * 2) });
                                    }
                                    break;
                                }
                                case 0xBC:
                                {
                                    sbyte transpose = config.Reader.ReadSByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new TransposeCommand { Transpose = transpose });
                                    }
                                    break;
                                }
                                // Commands that work within running status:
                                case 0xBD:
                                {
                                    byte voice = config.Reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new VoiceCommand { Voice = voice });
                                    }
                                    break;
                                }
                                case 0xBE:
                                {
                                    byte volume = config.Reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new VolumeCommand { Volume = volume });
                                    }
                                    break;
                                }
                                case 0xBF:
                                {
                                    byte panArg = config.Reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new PanpotCommand { Panpot = (sbyte)(panArg - 0x40) });
                                    }
                                    break;
                                }
                                case 0xC0:
                                {
                                    byte bendArg = config.Reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new PitchBendCommand { Bend = (sbyte)(bendArg - 0x40) });
                                    }
                                    break;
                                }
                                case 0xC1:
                                {
                                    byte range = config.Reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new PitchBendRangeCommand { Range = range });
                                    }
                                    break;
                                }
                                case 0xC2:
                                {
                                    byte speed = config.Reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new LFOSpeedCommand { Speed = speed });
                                    }
                                    break;
                                }
                                case 0xC3:
                                {
                                    byte delay = config.Reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new LFODelayCommand { Delay = delay });
                                    }
                                    break;
                                }
                                case 0xC4:
                                {
                                    byte depth = config.Reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new LFODepthCommand { Depth = depth });
                                    }
                                    break;
                                }
                                case 0xC5:
                                {
                                    byte type = config.Reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new LFOTypeCommand { Type = (LFOType)type });
                                    }
                                    break;
                                }
                                case 0xC8:
                                {
                                    byte tuneArg = config.Reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new TuneCommand { Tune = (sbyte)(tuneArg - 0x40) });
                                    }
                                    break;
                                }
                                case 0xCD:
                                {
                                    byte command = config.Reader.ReadByte();
                                    byte arg = config.Reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new LibraryCommand { Command = command, Argument = arg });
                                    }
                                    break;
                                }
                                case 0xCE:
                                {
                                    int key = config.Reader.PeekByte() <= 0x7F ? (prevKey = config.Reader.ReadByte()) : -1;
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new EndOfTieCommand { Key = key });
                                    }
                                    break;
                                }
                                default: throw new Exception(string.Format(Strings.ErrorDSEMLSSMP2KSDATInvalidCommand, i, offset, cmd));
                            }
                        }

                        #endregion
                    }
                }
            }
            SetTicks();
        }
        public void SetCurrentPosition(long ticks)
        {
            if (Events == null)
            {
                SongEnded?.Invoke();
            }
            else if (State == PlayerState.Playing || State == PlayerState.Paused || State == PlayerState.Stopped)
            {
                if (State == PlayerState.Playing)
                {
                    Pause();
                }
                InitEmulation();
                bool u = false;
                while (true)
                {
                    if (ElapsedTicks == ticks)
                    {
                        goto finish;
                    }
                    else
                    {
                        while (tempoStack >= 150)
                        {
                            tempoStack -= 150;
                            for (int i = 0; i < tracks.Length; i++)
                            {
                                Track track = tracks[i];
                                if (!track.Stopped)
                                {
                                    track.Tick();
                                    while (track.Rest == 0 && !track.Stopped)
                                    {
                                        ExecuteNext(i, ref u);
                                    }
                                }
                            }
                            ElapsedTicks++;
                            if (ElapsedTicks == ticks)
                            {
                                goto finish;
                            }
                        }
                        tempoStack += tempo;
                    }
                }
            finish:
                for (int i = 0; i < tracks.Length; i++)
                {
                    tracks[i].StopAllChannels();
                }
                Pause();
            }
        }
        public class MIDISaveArgs
        {
            public bool SaveCommandsBeforeTranspose;
            public bool ReverseVolume;
            public List<(int AbsoluteTick, (byte Numerator, byte Denominator))> TimeSignatures;
        }
        public void SaveAsMIDI(string fileName, MIDISaveArgs args)
        {
            // TODO: FINE vs PREV
            // TODO: https://github.com/Kermalis/VGMusicStudio/issues/36
            // TODO: Nested calls
            // TODO: REPT
            byte baseVolume = 0x7F;
            if (args.ReverseVolume)
            {
                baseVolume = Events.SelectMany(e => e).Where(e => e.Command is VolumeCommand).Select(e => ((VolumeCommand)e.Command).Volume).Max();
                System.Diagnostics.Debug.WriteLine($"Reversing volume back from {baseVolume}.");
            }

            var midi = new Sequence(24) { Format = 1 };
            var metaTrack = new Sanford.Multimedia.Midi.Track();
            midi.Add(metaTrack);
            var ts = new TimeSignatureBuilder();
            foreach ((int AbsoluteTick, (byte Numerator, byte Denominator)) e in args.TimeSignatures)
            {
                ts.Numerator = e.Item2.Numerator;
                ts.Denominator = e.Item2.Denominator;
                ts.ClocksPerMetronomeClick = 24;
                ts.ThirtySecondNotesPerQuarterNote = 8;
                ts.Build();
                metaTrack.Insert(e.AbsoluteTick, ts.Result);
            }

            for (int i = 0; i < Events.Length; i++)
            {
                var track = new Sanford.Multimedia.Midi.Track();
                midi.Add(track);

                bool foundTranspose = false;
                int endOfPattern = 0;
                long startOfPatternTicks = 0, endOfPatternTicks = 0;
                sbyte transpose = 0;
                var playing = new List<NoteCommand>();
                for (int j = 0; j < Events[i].Count; j++)
                {
                    SongEvent e = Events[i][j];
                    int ticks = (int)(e.Ticks[0] + (endOfPatternTicks - startOfPatternTicks));

                    // Preliminary check for saving events before transpose
                    switch (e.Command)
                    {
                        case TransposeCommand keysh: foundTranspose = true; break;
                        default: // If we should not save before transpose then skip this event
                        {
                            if (!args.SaveCommandsBeforeTranspose && !foundTranspose)
                            {
                                continue;
                            }
                            break;
                        }
                    }
                    // Now do the event magic...
                    switch (e.Command)
                    {
                        case CallCommand patt:
                        {
                            int callCmd = Events[i].FindIndex(c => c.Offset == patt.Offset);
                            endOfPattern = j;
                            endOfPatternTicks = e.Ticks[0];
                            j = callCmd - 1; // -1 for incoming ++
                            startOfPatternTicks = Events[i][callCmd].Ticks[0];
                            break;
                        }
                        case EndOfTieCommand eot:
                        {
                            NoteCommand nc = eot.Key == -1 ? playing.LastOrDefault() : playing.LastOrDefault(no => no.Key == eot.Key);
                            if (nc != null)
                            {
                                int n = (nc.Key + transpose).Clamp(0, 0x7F);
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.NoteOff, i, n));
                                playing.Remove(nc);
                            }
                            break;
                        }
                        case FinishCommand _:
                        {
                            // If the track is not only the finish command, place the finish command at the correct tick
                            if (track.Count > 1)
                            {
                                track.EndOfTrackOffset = (int)(e.Ticks[0] - track.GetMidiEvent(track.Count - 2).AbsoluteTicks);
                            }
                            goto endOfTrack;
                        }
                        case JumpCommand goTo:
                        {
                            if (i == 0)
                            {
                                int jumpCmd = Events[i].FindIndex(c => c.Offset == goTo.Offset);
                                metaTrack.Insert((int)Events[i][jumpCmd].Ticks[0], new MetaMessage(MetaType.Marker, new byte[] { (byte)'[' }));
                                metaTrack.Insert(ticks, new MetaMessage(MetaType.Marker, new byte[] { (byte)']' }));
                            }
                            break;
                        }
                        case LFODelayCommand lfodl:
                        {
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 26, lfodl.Delay));
                            break;
                        }
                        case LFODepthCommand mod:
                        {
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, (int)ControllerType.ModulationWheel, mod.Depth));
                            break;
                        }
                        case LFOSpeedCommand lfos:
                        {
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 21, lfos.Speed));
                            break;
                        }
                        case LFOTypeCommand modt:
                        {
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 22, (byte)modt.Type));
                            break;
                        }
                        case LibraryCommand xcmd:
                        {
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 30, xcmd.Command));
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 29, xcmd.Argument));
                            break;
                        }
                        case MemoryAccessCommand memacc:
                        {
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 13, memacc.Operator));
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 14, memacc.Address));
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 12, memacc.Data));
                            break;
                        }
                        case NoteCommand note:
                        {
                            int n = (note.Key + transpose).Clamp(0, 0x7F);
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.NoteOn, i, n, note.Velocity));
                            if (note.Duration != -1)
                            {
                                track.Insert(ticks + note.Duration, new ChannelMessage(ChannelCommand.NoteOff, i, n));
                            }
                            else
                            {
                                playing.Add(note);
                            }
                            break;
                        }
                        case PanpotCommand pan:
                        {
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, (int)ControllerType.Pan, pan.Panpot + 0x40));
                            break;
                        }
                        case PitchBendCommand bend:
                        {
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.PitchWheel, i, 0, bend.Bend + 0x40));
                            break;
                        }
                        case PitchBendRangeCommand bendr:
                        {
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 20, bendr.Range));
                            break;
                        }
                        case PriorityCommand prio:
                        {
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, (int)ControllerType.VolumeFine, prio.Priority));
                            break;
                        }
                        case ReturnCommand _:
                        {
                            if (endOfPattern != 0)
                            {
                                j = endOfPattern;
                                endOfPattern = 0;
                                startOfPatternTicks = endOfPatternTicks = 0;
                            }
                            break;
                        }
                        case TempoCommand tempo:
                        {
                            var change = new TempoChangeBuilder { Tempo = 60000000 / tempo.Tempo };
                            change.Build();
                            metaTrack.Insert(ticks, change.Result);
                            break;
                        }
                        case TransposeCommand keysh:
                        {
                            transpose = keysh.Transpose;
                            break;
                        }
                        case TuneCommand tune:
                        {
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 24, tune.Tune));
                            break;
                        }
                        case VoiceCommand voice:
                        {
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.ProgramChange, i, voice.Voice));
                            break;
                        }
                        case VolumeCommand vol:
                        {
                            double d = baseVolume / (double)0x7F;
                            int volume = (int)(vol.Volume / d);
                            // If there are rounding errors, fix them (happens if baseVolume is not 127 and baseVolume is not vol.Volume)
                            if (volume * baseVolume / 0x7F == vol.Volume - 1)
                            {
                                volume++;
                            }
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, (int)ControllerType.Volume, volume));
                            break;
                        }
                    }
                }
            endOfTrack:;
            }
            midi.Save(fileName);
        }
        public void Play()
        {
            if (Events == null)
            {
                SongEnded?.Invoke();
            }
            else if (State == PlayerState.Playing || State == PlayerState.Paused || State == PlayerState.Stopped)
            {
                Stop();
                InitEmulation();
                State = PlayerState.Playing;
            }
        }
        public void Pause()
        {
            if (State == PlayerState.Playing)
            {
                State = PlayerState.Paused;
            }
            else if (State == PlayerState.Paused || State == PlayerState.Stopped)
            {
                State = PlayerState.Playing;
            }
        }
        public void Stop()
        {
            if (State == PlayerState.Playing || State == PlayerState.Paused)
            {
                State = PlayerState.Stopped;
                for (int i = 0; i < tracks.Length; i++)
                {
                    tracks[i].StopAllChannels();
                }
            }
        }
        public void Dispose()
        {
            if (State == PlayerState.Playing || State == PlayerState.Paused || State == PlayerState.Stopped)
            {
                Stop();
                State = PlayerState.ShutDown;
                thread.Join();
            }
        }
        private string[] voiceTypeCache;
        public void GetSongState(UI.SongInfoControl.SongInfo info)
        {
            info.Tempo = tempo;
            for (int i = 0; i < tracks.Length; i++)
            {
                Track track = tracks[i];
                UI.SongInfoControl.SongInfo.Track tin = info.Tracks[i];
                tin.Position = Events[i][track.CurEvent].Offset;
                tin.Rest = track.Rest;
                tin.Voice = track.Voice;
                tin.LFO = track.LFODepth;
                if (voiceTypeCache[track.Voice] == null)
                {
                    byte t = config.ROM[voiceTableOffset + (track.Voice * 0xC)]; // Don't use config.Reader because it is not thread-safe
                    if (t == (byte)VoiceFlags.KeySplit)
                    {
                        voiceTypeCache[track.Voice] = "Key Split";
                    }
                    else if (t == (byte)VoiceFlags.Drum)
                    {
                        voiceTypeCache[track.Voice] = "Drum";
                    }
                    else
                    {
                        switch ((VoiceType)(t & 0x7))
                        {
                            case VoiceType.PCM8: voiceTypeCache[track.Voice] = "PCM8"; break; // TODO: Golden Sun
                            case VoiceType.Square1: voiceTypeCache[track.Voice] = "Square 1"; break;
                            case VoiceType.Square2: voiceTypeCache[track.Voice] = "Square 2"; break;
                            case VoiceType.PCM4: voiceTypeCache[track.Voice] = "PCM4"; break;
                            case VoiceType.Noise: voiceTypeCache[track.Voice] = "Noise"; break;
                            case VoiceType.Invalid5: voiceTypeCache[track.Voice] = "Invalid 5"; break;
                            case VoiceType.Invalid6: voiceTypeCache[track.Voice] = "Invalid 6"; break;
                            case VoiceType.Invalid7: voiceTypeCache[track.Voice] = "Invalid 7"; break;
                        }
                    }
                }
                tin.Type = voiceTypeCache[track.Voice];
                tin.Volume = track.GetVolume();
                tin.PitchBend = track.GetPitch();
                tin.Panpot = track.GetPanpot();

                Channel[] channels = track.Channels.ToArray();
                if (channels.Length == 0)
                {
                    tin.Keys[0] = byte.MaxValue;
                    tin.LeftVolume = 0f;
                    tin.RightVolume = 0f;
                }
                else
                {
                    int numKeys = 0;
                    float left = 0f;
                    float right = 0f;
                    for (int j = 0; j < channels.Length; j++)
                    {
                        Channel c = channels[j];
                        if (c.State < EnvelopeState.Releasing)
                        {
                            tin.Keys[numKeys++] = c.Note.OriginalKey;
                        }
                        ChannelVolume vol = c.GetVolume();
                        if (vol.LeftVol > left)
                        {
                            left = vol.LeftVol;
                        }
                        if (vol.RightVol > right)
                        {
                            right = vol.RightVol;
                        }
                    }
                    tin.Keys[numKeys] = byte.MaxValue; // There's no way for numKeys to be after the last index in the array
                    tin.LeftVolume = left;
                    tin.RightVolume = right;
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
        private void ExecuteNext(int trackIndex, ref bool update)
        {
            bool increment = true;
            List<SongEvent> ev = Events[trackIndex];
            Track track = tracks[trackIndex];
            switch (ev[track.CurEvent].Command)
            {
                case CallCommand call:
                {
                    if (track.CallStackDepth < 3)
                    {
                        int callCmd = ev.FindIndex(c => c.Offset == call.Offset);
                        track.CallStack[track.CallStackDepth] = track.CurEvent + 1;
                        track.CallStackDepth++;
                        track.CurEvent = callCmd;
                        increment = false;
                    }
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
                    increment = false;
                    break;
                }
                case LFODelayCommand lfodl: track.LFODelay = lfodl.Delay; track.LFOPhase = track.LFODelayCount = 0; update = true; break;
                case LFODepthCommand lfo: track.LFODepth = lfo.Depth; update = true; break;
                case LFOSpeedCommand lfos: track.LFOSpeed = lfos.Speed; track.LFOPhase = track.LFODelayCount = 0; update = true; break;
                case LFOTypeCommand lfot: track.LFOType = lfot.Type; update = true; break;
                case NoteCommand note: PlayNote(track, note.Key, note.Velocity, note.Duration); break;
                case PanpotCommand pan: track.Panpot = pan.Panpot; update = true; break;
                case PitchBendCommand bend: track.PitchBend = bend.Bend; update = true; break;
                case PitchBendRangeCommand bendr: track.PitchBendRange = bendr.Range; update = true; break;
                case PriorityCommand priority: track.Priority = priority.Priority; break;
                case RestCommand rest: track.Rest = rest.Rest; break;
                case ReturnCommand _:
                {
                    if (track.CallStackDepth != 0)
                    {
                        track.CallStackDepth--;
                        track.CurEvent = track.CallStack[track.CallStackDepth];
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
                    while (tempoStack >= 150)
                    {
                        tempoStack -= 150;
                        bool allDone = true;
                        for (int i = 0; i < tracks.Length; i++)
                        {
                            Track track = tracks[i];
                            track.Tick();
                            bool update = false;
                            while (track.Rest == 0 && !track.Stopped)
                            {
                                ExecuteNext(i, ref update);
                            }
                            if (i == longestTrack)
                            {
                                if (ElapsedTicks == MaxTicks)
                                {
                                    if (!track.Stopped)
                                    {
                                        ElapsedTicks = Events[i][track.CurEvent].Ticks[0] - track.Rest;
                                        elapsedLoops++;
                                        if (UI.MainForm.Instance.PlaylistPlaying && !fadeOutBegan && elapsedLoops > GlobalConfig.Instance.PlaylistSongLoops)
                                        {
                                            fadeOutBegan = true;
                                            mixer.BeginFadeOut();
                                        }
                                    }
                                }
                                else
                                {
                                    ElapsedTicks++;
                                }
                            }
                            if (!track.Stopped)
                            {
                                allDone = false;
                            }
                            if (track.Channels.Count > 0)
                            {
                                allDone = false;
                                if (update || track.LFODepth > 0)
                                {
                                    track.UpdateChannels();
                                }
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
                    tempoStack += tempo;
                    mixer.Process();
                }
                time.Wait();
            }
            time.Stop();
        }
    }
}
