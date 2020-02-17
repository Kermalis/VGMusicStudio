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
        public class MIDISaveArgs
        {
            public bool SaveCommandsBeforeTranspose;
            public bool ReverseVolume;
            public List<(int AbsoluteTick, (byte Numerator, byte Denominator))> TimeSignatures;
        }

        private readonly Mixer mixer;
        private readonly Config config;
        private readonly TimeBarrier time;
        private Thread thread;
        private int voiceTableOffset = -1;
        private Track[] tracks;
        private ushort tempo;
        private int tempoStack;
        private long elapsedLoops;
        private bool fadeOutBegan;

        public List<SongEvent>[] Events { get; private set; }
        public long MaxTicks { get; private set; }
        public long ElapsedTicks { get; private set; }
        public bool ShouldFadeOut { get; set; }
        public long NumLoops { get; set; }
        private int longestTrack;

        public PlayerState State { get; private set; }
        public event SongEndedEvent SongEnded;

        public Player(Mixer mixer, Config config)
        {
            this.mixer = mixer;
            this.config = config;

            time = new TimeBarrier(GBA.Utils.AGB_FPS);
        }
        private void CreateThread()
        {
            thread = new Thread(Tick) { Name = "MP2K Player Tick" };
            thread.Start();
        }
        private void WaitThread()
        {
            if (thread != null && (thread.ThreadState == ThreadState.Running || thread.ThreadState == ThreadState.WaitSleepJoin))
            {
                thread.Join();
            }
        }

        private void InitEmulation()
        {
            tempo = 150;
            tempoStack = 0;
            elapsedLoops = 0;
            ElapsedTicks = 0;
            fadeOutBegan = false;
            for (int trackIndex = 0; trackIndex < tracks.Length; trackIndex++)
            {
                tracks[trackIndex].Init();
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
                    SongEvent e = evs.Single(ev => ev.Offset == track.CurOffset);
                    if (track.CallStackDepth == 0 && e.Ticks.Count > 0)
                    {
                        break;
                    }
                    else
                    {
                        e.Ticks.Add(ElapsedTicks);
                        ExecuteNext(track, ref u);
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
            if (tracks != null)
            {
                for (int i = 0; i < tracks.Length; i++)
                {
                    tracks[i].StopAllChannels();
                }
                tracks = null;
            }
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
            for (byte trackIndex = 0; trackIndex < header.NumTracks; trackIndex++)
            {
                long trackStart = header.TrackOffsets[trackIndex] - GBA.Utils.CartridgeOffset;
                tracks[trackIndex] = new Track(trackIndex, trackStart);
                Events[trackIndex] = new List<SongEvent>();
                bool EventExists(long offset)
                {
                    return Events[trackIndex].Any(e => e.Offset == offset);
                }

                byte runCmd = 0, prevKey = 0, prevVelocity = 0x7F;
                int callStackDepth = 0;
                AddEvents(trackStart);
                void AddEvents(long startOffset)
                {
                    config.Reader.BaseStream.Position = startOffset;
                    bool cont = true;
                    while (cont)
                    {
                        long offset = config.Reader.BaseStream.Position;
                        void AddEvent(ICommand command)
                        {
                            Events[trackIndex].Add(new SongEvent(offset, command));
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
                                default: throw new Exception(string.Format(Strings.ErrorMP2KInvalidRunningStatusCommand, trackIndex, offset, runCmd));
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
                                        AddEvent(new FinishCommand { Prev = cmd == 0xB6 });
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
                                        throw new Exception(string.Format(Strings.ErrorMP2KSDATNestedCalls, trackIndex));
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
                                default: throw new Exception(string.Format(Strings.ErrorAlphaDreamDSEMP2KSDATInvalidCommand, trackIndex, offset, cmd));
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
                            for (int trackIndex = 0; trackIndex < tracks.Length; trackIndex++)
                            {
                                Track track = tracks[trackIndex];
                                if (!track.Stopped)
                                {
                                    track.Tick();
                                    while (track.Rest == 0 && !track.Stopped)
                                    {
                                        ExecuteNext(track, ref u);
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
        // TODO: Don't use events, read from rom
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

            using (var midi = new Sequence(24) { Format = 1 })
            {
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

                for (int trackIndex = 0; trackIndex < Events.Length; trackIndex++)
                {
                    var track = new Sanford.Multimedia.Midi.Track();
                    midi.Add(track);

                    bool foundTranspose = false;
                    int endOfPattern = 0;
                    long startOfPatternTicks = 0, endOfPatternTicks = 0;
                    sbyte transpose = 0;
                    var playing = new List<NoteCommand>();
                    for (int i = 0; i < Events[trackIndex].Count; i++)
                    {
                        SongEvent e = Events[trackIndex][i];
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
                                int callCmd = Events[trackIndex].FindIndex(c => c.Offset == patt.Offset);
                                endOfPattern = i;
                                endOfPatternTicks = e.Ticks[0];
                                i = callCmd - 1; // -1 for incoming ++
                                startOfPatternTicks = Events[trackIndex][callCmd].Ticks[0];
                                break;
                            }
                            case EndOfTieCommand eot:
                            {
                                NoteCommand nc = eot.Key == -1 ? playing.LastOrDefault() : playing.LastOrDefault(no => no.Key == eot.Key);
                                if (nc != null)
                                {
                                    int key = nc.Key + transpose;
                                    if (key < 0)
                                    {
                                        key = 0;
                                    }
                                    else if (key > 0x7F)
                                    {
                                        key = 0x7F;
                                    }
                                    track.Insert(ticks, new ChannelMessage(ChannelCommand.NoteOff, trackIndex, key));
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
                                if (trackIndex == 0)
                                {
                                    int jumpCmd = Events[trackIndex].FindIndex(c => c.Offset == goTo.Offset);
                                    metaTrack.Insert((int)Events[trackIndex][jumpCmd].Ticks[0], new MetaMessage(MetaType.Marker, new byte[] { (byte)'[' }));
                                    metaTrack.Insert(ticks, new MetaMessage(MetaType.Marker, new byte[] { (byte)']' }));
                                }
                                break;
                            }
                            case LFODelayCommand lfodl:
                            {
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, trackIndex, 26, lfodl.Delay));
                                break;
                            }
                            case LFODepthCommand mod:
                            {
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, trackIndex, (int)ControllerType.ModulationWheel, mod.Depth));
                                break;
                            }
                            case LFOSpeedCommand lfos:
                            {
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, trackIndex, 21, lfos.Speed));
                                break;
                            }
                            case LFOTypeCommand modt:
                            {
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, trackIndex, 22, (byte)modt.Type));
                                break;
                            }
                            case LibraryCommand xcmd:
                            {
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, trackIndex, 30, xcmd.Command));
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, trackIndex, 29, xcmd.Argument));
                                break;
                            }
                            case MemoryAccessCommand memacc:
                            {
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, trackIndex, 13, memacc.Operator));
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, trackIndex, 14, memacc.Address));
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, trackIndex, 12, memacc.Data));
                                break;
                            }
                            case NoteCommand note:
                            {
                                int key = note.Key + transpose;
                                if (key < 0)
                                {
                                    key = 0;
                                }
                                else if (key > 0x7F)
                                {
                                    key = 0x7F;
                                }
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.NoteOn, trackIndex, key, note.Velocity));
                                if (note.Duration != -1)
                                {
                                    track.Insert(ticks + note.Duration, new ChannelMessage(ChannelCommand.NoteOff, trackIndex, key));
                                }
                                else
                                {
                                    playing.Add(note);
                                }
                                break;
                            }
                            case PanpotCommand pan:
                            {
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, trackIndex, (int)ControllerType.Pan, pan.Panpot + 0x40));
                                break;
                            }
                            case PitchBendCommand bend:
                            {
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.PitchWheel, trackIndex, 0, bend.Bend + 0x40));
                                break;
                            }
                            case PitchBendRangeCommand bendr:
                            {
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, trackIndex, 20, bendr.Range));
                                break;
                            }
                            case PriorityCommand prio:
                            {
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, trackIndex, (int)ControllerType.VolumeFine, prio.Priority));
                                break;
                            }
                            case ReturnCommand _:
                            {
                                if (endOfPattern != 0)
                                {
                                    i = endOfPattern;
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
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, trackIndex, 24, tune.Tune));
                                break;
                            }
                            case VoiceCommand voice:
                            {
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.ProgramChange, trackIndex, voice.Voice));
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
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, trackIndex, (int)ControllerType.Volume, volume));
                                break;
                            }
                        }
                    }
                endOfTrack:;
                }
                midi.Save(fileName);
            }
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
                CreateThread();
            }
        }
        public void Pause()
        {
            if (State == PlayerState.Playing)
            {
                State = PlayerState.Paused;
                WaitThread();
            }
            else if (State == PlayerState.Paused || State == PlayerState.Stopped)
            {
                State = PlayerState.Playing;
                CreateThread();
            }
        }
        public void Stop()
        {
            if (State == PlayerState.Playing || State == PlayerState.Paused)
            {
                State = PlayerState.Stopped;
                WaitThread();
            }
        }
        public void Record(string fileName)
        {
            mixer.CreateWaveWriter(fileName);
            InitEmulation();
            State = PlayerState.Recording;
            CreateThread();
            WaitThread();
            mixer.CloseWaveWriter();
        }
        public void Dispose()
        {
            if (State == PlayerState.Playing || State == PlayerState.Paused || State == PlayerState.Stopped)
            {
                State = PlayerState.ShutDown;
                WaitThread();
            }
        }
        private string[] voiceTypeCache;
        public void GetSongState(UI.SongInfoControl.SongInfo info)
        {
            info.Tempo = tempo;
            for (int trackIndex = 0; trackIndex < tracks.Length; trackIndex++)
            {
                Track track = tracks[trackIndex];
                UI.SongInfoControl.SongInfo.Track tin = info.Tracks[trackIndex];
                tin.Position = track.CurOffset;
                tin.Rest = track.Rest;
                tin.Voice = track.Voice;
                tin.LFO = track.LFODepth;
                if (voiceTypeCache[track.Voice] == null)
                {
                    byte t = config.ROM[voiceTableOffset + (track.Voice * 0xC)];
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

        // TODO: Don't use config.Reader (Or make ReadObjectCached(offset))
        private void PlayNote(Track track, byte key, byte velocity, byte addedDuration)
        {
            int k = key + track.Transpose;
            if (k < 0)
            {
                k = 0;
            }
            else if (k > 0x7F)
            {
                k = 0x7F;
            }
            key = (byte)k;
            track.PrevKey = key;
            track.PrevVelocity = velocity;
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
                            Duration = track.RunCmd == 0xCF ? -1 : (Utils.RestTable[track.RunCmd - 0xCF] + addedDuration),
                            Velocity = velocity,
                            OriginalKey = key,
                            Key = fromDrum ? v.RootKey : key
                        };
                        var type = (VoiceType)(v.Type & 0x7);
                        switch (type)
                        {
                            case VoiceType.PCM8:
                            {
                                bool bFixed = (v.Type & (int)VoiceFlags.Fixed) != 0;
                                bool bCompressed = config.HasPokemonCompression && ((v.Type & (int)VoiceFlags.Compressed) != 0);
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
        private void ExecuteNext(Track track, ref bool update)
        {
            byte cmd = config.ROM[track.CurOffset++];
            if (cmd >= 0xBD) // Commands that work within running status
            {
                track.RunCmd = cmd;
            }
            if (track.RunCmd >= 0xCF && cmd <= 0x7F) // Within running status
            {
                byte peek0 = config.ROM[track.CurOffset];
                byte peek1 = config.ROM[track.CurOffset + 1];
                byte velocity, addedDuration;
                if (peek0 > 0x7F)
                {
                    velocity = track.PrevVelocity;
                    addedDuration = 0;
                }
                else if (peek1 > 3)
                {
                    track.CurOffset++;
                    velocity = peek0;
                    addedDuration = 0;
                }
                else
                {
                    track.CurOffset += 2;
                    velocity = peek0;
                    addedDuration = peek1;
                }
                PlayNote(track, cmd, velocity, addedDuration);
            }
            else if (cmd >= 0xCF)
            {
                byte peek0 = config.ROM[track.CurOffset];
                byte peek1 = config.ROM[track.CurOffset + 1];
                byte peek2 = config.ROM[track.CurOffset + 2];
                byte key, velocity, addedDuration;
                if (peek0 > 0x7F)
                {
                    key = track.PrevKey;
                    velocity = track.PrevVelocity;
                    addedDuration = 0;
                }
                else if (peek1 > 0x7F)
                {
                    track.CurOffset++;
                    key = peek0;
                    velocity = track.PrevVelocity;
                    addedDuration = 0;
                }
                else if (cmd == 0xCF || peek2 > 3)
                {
                    track.CurOffset += 2;
                    key = peek0;
                    velocity = peek1;
                    addedDuration = 0;
                }
                else
                {
                    track.CurOffset += 3;
                    key = peek0;
                    velocity = peek1;
                    addedDuration = peek2;
                }
                PlayNote(track, key, velocity, addedDuration);
            }
            else if (cmd >= 0x80 && cmd <= 0xB0)
            {
                track.Rest = Utils.RestTable[cmd - 0x80];
            }
            else if (track.RunCmd < 0xCF && cmd <= 0x7F)
            {
                switch (track.RunCmd)
                {
                    case 0xBD:
                    {
                        track.Voice = cmd;
                        //track.Ready = true; // This is unnecessary because if we're in running status of a voice command, then Ready was already set
                        break;
                    }
                    case 0xBE:
                    {
                        track.Volume = cmd;
                        update = true;
                        break;
                    }
                    case 0xBF:
                    {
                        track.Panpot = (sbyte)(cmd - 0x40);
                        update = true;
                        break;
                    }
                    case 0xC0:
                    {
                        track.PitchBend = (sbyte)(cmd - 0x40);
                        update = true;
                        break;
                    }
                    case 0xC1:
                    {
                        track.PitchBendRange = cmd;
                        update = true;
                        break;
                    }
                    case 0xC2:
                    {
                        track.LFOSpeed = cmd;
                        track.LFOPhase = 0;
                        track.LFODelayCount = 0;
                        update = true;
                        break;
                    }
                    case 0xC3:
                    {
                        track.LFODelay = cmd;
                        track.LFOPhase = 0;
                        track.LFODelayCount = 0;
                        update = true;
                        break;
                    }
                    case 0xC4:
                    {
                        track.LFODepth = cmd;
                        update = true;
                        break;
                    }
                    case 0xC5:
                    {
                        track.LFOType = (LFOType)cmd;
                        update = true;
                        break;
                    }
                    case 0xC8:
                    {
                        track.Tune = (sbyte)(cmd - 0x40);
                        update = true;
                        break;
                    }
                    case 0xCD:
                    {
                        track.CurOffset++;
                        break;
                    }
                    case 0xCE:
                    {
                        track.PrevKey = cmd;
                        int k = cmd + track.Transpose;
                        if (k < 0)
                        {
                            k = 0;
                        }
                        else if (k > 0x7F)
                        {
                            k = 0x7F;
                        }
                        track.ReleaseChannels(k);
                        break;
                    }
                    default: throw new Exception(string.Format(Strings.ErrorMP2KInvalidRunningStatusCommand, track.Index, track.CurOffset, track.RunCmd));
                }
            }
            else if (cmd > 0xB0 && cmd < 0xCF)
            {
                switch (cmd)
                {
                    case 0xB1:
                    case 0xB6:
                    {
                        track.Stopped = true;
                        //track.ReleaseAllTieingChannels(); // Necessary?
                        break;
                    }
                    case 0xB2:
                    {
                        track.CurOffset = (config.ROM[track.CurOffset++] | (config.ROM[track.CurOffset++] << 8) | (config.ROM[track.CurOffset++] << 16) | (config.ROM[track.CurOffset++] << 24)) - GBA.Utils.CartridgeOffset;
                        break;
                    }
                    case 0xB3:
                    {
                        if (track.CallStackDepth < 3)
                        {
                            long callOffset = (config.ROM[track.CurOffset++] | (config.ROM[track.CurOffset++] << 8) | (config.ROM[track.CurOffset++] << 16) | (config.ROM[track.CurOffset++] << 24)) - GBA.Utils.CartridgeOffset;
                            track.CallStack[track.CallStackDepth] = track.CurOffset;
                            track.CallStackDepth++;
                            track.CurOffset = callOffset;
                        }
                        else
                        {
                            throw new Exception(string.Format(Strings.ErrorMP2KSDATNestedCalls, track.Index));
                        }
                        break;
                    }
                    case 0xB4:
                    {
                        if (track.CallStackDepth != 0)
                        {
                            track.CallStackDepth--;
                            track.CurOffset = track.CallStack[track.CallStackDepth];
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
                        track.CurOffset += 3;
                        break;
                    }
                    case 0xBA:
                    {
                        track.Priority = config.ROM[track.CurOffset++];
                        break;
                    }
                    case 0xBB:
                    {
                        tempo = (ushort)(config.ROM[track.CurOffset++] * 2);
                        break;
                    }
                    case 0xBC:
                    {
                        track.Transpose = (sbyte)config.ROM[track.CurOffset++];
                        break;
                    }
                    // Commands that work within running status:
                    case 0xBD:
                    {
                        track.Voice = config.ROM[track.CurOffset++];
                        track.Ready = true;
                        break;
                    }
                    case 0xBE:
                    {
                        track.Volume = config.ROM[track.CurOffset++];
                        update = true;
                        break;
                    }
                    case 0xBF:
                    {
                        track.Panpot = (sbyte)(config.ROM[track.CurOffset++] - 0x40);
                        update = true;
                        break;
                    }
                    case 0xC0:
                    {
                        track.PitchBend = (sbyte)(config.ROM[track.CurOffset++] - 0x40);
                        update = true;
                        break;
                    }
                    case 0xC1:
                    {
                        track.PitchBendRange = config.ROM[track.CurOffset++];
                        update = true;
                        break;
                    }
                    case 0xC2:
                    {
                        track.LFOSpeed = config.ROM[track.CurOffset++];
                        track.LFOPhase = 0;
                        track.LFODelayCount = 0;
                        update = true;
                        break;
                    }
                    case 0xC3:
                    {
                        track.LFODelay = config.ROM[track.CurOffset++];
                        track.LFOPhase = 0;
                        track.LFODelayCount = 0;
                        update = true;
                        break;
                    }
                    case 0xC4:
                    {
                        track.LFODepth = config.ROM[track.CurOffset++];
                        update = true;
                        break;
                    }
                    case 0xC5:
                    {
                        track.LFOType = (LFOType)config.ROM[track.CurOffset++];
                        update = true;
                        break;
                    }
                    case 0xC8:
                    {
                        track.Tune = (sbyte)(config.ROM[track.CurOffset++] - 0x40);
                        update = true;
                        break;
                    }
                    case 0xCD:
                    {
                        track.CurOffset += 2;
                        break;
                    }
                    case 0xCE:
                    {
                        byte peek = config.ROM[track.CurOffset];
                        if (peek > 0x7F)
                        {
                            track.ReleaseChannels(track.PrevKey);
                        }
                        else
                        {
                            track.CurOffset++;
                            track.PrevKey = peek;
                            int k = peek + track.Transpose;
                            if (k < 0)
                            {
                                k = 0;
                            }
                            else if (k > 0x7F)
                            {
                                k = 0x7F;
                            }
                            track.ReleaseChannels(k);
                        }
                        break;
                    }
                    default: throw new Exception(string.Format(Strings.ErrorAlphaDreamDSEMP2KSDATInvalidCommand, track.Index, track.CurOffset, cmd));
                }
            }
        }

        private void Tick()
        {
            time.Start();
            while (State == PlayerState.Playing || State == PlayerState.Recording)
            {
                while (tempoStack >= 150)
                {
                    tempoStack -= 150;
                    bool allDone = true;
                    for (int trackIndex = 0; trackIndex < tracks.Length; trackIndex++)
                    {
                        Track track = tracks[trackIndex];
                        track.Tick();
                        bool update = false;
                        while (track.Rest == 0 && !track.Stopped)
                        {
                            ExecuteNext(track, ref update);
                        }
                        if (trackIndex == longestTrack)
                        {
                            if (ElapsedTicks == MaxTicks)
                            {
                                if (!track.Stopped)
                                {
                                    List<SongEvent> evs = Events[trackIndex];
                                    for (int i = 0; i < evs.Count; i++)
                                    {
                                        SongEvent ev = evs[i];
                                        if (ev.Offset == track.CurOffset)
                                        {
                                            ElapsedTicks = ev.Ticks[0] - track.Rest;
                                            break;
                                        }
                                    }
                                    elapsedLoops++;
                                    if (ShouldFadeOut && !fadeOutBegan && elapsedLoops > NumLoops)
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
                        State = PlayerState.Stopped;
                        SongEnded?.Invoke();
                    }
                }
                tempoStack += tempo;
                mixer.Process(State == PlayerState.Playing, State == PlayerState.Recording);
                if (State == PlayerState.Playing)
                {
                    time.Wait();
                }
            }
            time.Stop();
        }
    }
}
