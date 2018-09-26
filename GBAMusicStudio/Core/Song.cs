using GBAMusicStudio.Properties;
using GBAMusicStudio.Util;
using Kermalis.EndianBinaryIO;
using Sanford.Multimedia.Midi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GBAMusicStudio.Core
{
    abstract class Song : IOffset
    {
        int offset;
        public VoiceTable VoiceTable;
        public List<SongEvent>[] Commands;
        public int NumTracks => Commands == null ? 0 : Commands.Length;
        int ticks = -1; // Cache the amount. Setting to -1 again will cause a refresh
        public int NumTicks
        {
            get
            {
                if (ticks == -1)
                {
                    CalculateTicks();
                    foreach (var track in Commands)
                    {
                        if (track.Count == 0) continue; // Prevent crashes with invalid ones
                        int length = track.Last().AbsoluteTicks;
                        if (length > ticks)
                            ticks = length;
                    }
                }
                return ticks + 1;
            }
        }

        public virtual byte GetReverb() => 0;

        public int GetOffset() => offset;
        public void SetOffset(int newOffset) => offset = newOffset;

        public void CalculateTicks()
        {
            for (int i = 0; i < NumTracks; i++)
                CalculateTicks(i);
        }
        public void CalculateTicks(int trackIndex)
        {
            var track = Commands[trackIndex];

            int length = 0, endOfPattern = 0;
            for (int i = 0; i < track.Count; i++)
            {
                var e = track[i];
                if (endOfPattern == 0)
                    e.AbsoluteTicks = length;

                if (e.Command is RestCommand rest)
                    length += rest.Rest;
                else if (this is MLSSSong)
                {
                    if (e.Command is FreeNoteCommand ext)
                        length += ext.Duration;
                    else if (e.Command is MLSSNoteCommand mlnote)
                        length += mlnote.Duration;
                }
                else if (e.Command is CallCommand call)
                {
                    int jumpCmd = track.FindIndex(c => c.GetOffset() == call.Offset);
                    endOfPattern = i;
                    i = jumpCmd - 1;
                }
                else if (e.Command is ReturnCommand && endOfPattern != 0)
                {
                    i = endOfPattern;
                    endOfPattern = 0;
                }
            }

            ticks = -1; // Trigger recount of NumTicks
        }
        public void InsertEvent(SongEvent e, int trackIndex, int insertIndex)
        {
            Commands[trackIndex].Insert(insertIndex, e);
            CalculateTicks(trackIndex);
        }
        public void RemoveEvent(int trackIndex, int eventIndex)
        {
            Commands[trackIndex].RemoveAt(eventIndex);
            CalculateTicks(trackIndex);
        }

        public abstract void SaveAsASM(string fileName);
        public abstract void SaveAsMIDI(string fileName, MIDISaveArgs args);
        protected void AddTimeSignaturesToTrack(MIDISaveArgs args, Sanford.Multimedia.Midi.Track track)
        {
            var ts = new TimeSignatureBuilder();
            foreach (var e in args.TimeSignatures)
            {
                ts.Numerator = e.Item2.Item1;
                ts.Denominator = e.Item2.Item2;
                ts.ClocksPerMetronomeClick = 24;
                ts.ThirtySecondNotesPerQuarterNote = 8;
                ts.Build();
                track.Insert(e.Item1, ts.Result);
            }
        }
    }

    abstract class M4ASong : Song
    {
        byte[] _binary;
        public M4ASongHeader Header;

        public override byte GetReverb() => Header.Reverb;

        protected void Load(byte[] binary, EndianBinaryReader reader, int headerOffset)
        {
            _binary = binary;
            Header = reader.ReadObject<M4ASongHeader>(headerOffset);

            VoiceTable = VoiceTable.LoadTable<M4AVoiceTable>(Header.VoiceTable - ROM.Pak);

            Commands = new List<SongEvent>[Header.NumTracks];
            for (int i = 0; i < Header.NumTracks; i++)
                Commands[i] = new List<SongEvent>();

            if (Header.NumTracks > ROM.Instance.Game.Engine.TrackLimit)
                throw new InvalidDataException(string.Format(Strings.ErrorTooManyTracks, Header.NumTracks));

            for (int i = 0; i < NumTracks; i++)
            {
                reader.BaseStream.Position = Header.Tracks[i] - ROM.Pak;

                byte cmd = 0, runCmd = 0, prevNote = 0, prevVelocity = 0x7F;

                while (cmd != 0xB1 && cmd != 0xB6)
                {
                    int off = (int)reader.BaseStream.Position;
                    ICommand command = null;

                    cmd = reader.ReadByte();
                    if (cmd >= 0xBD) // Commands that work within running status
                        runCmd = cmd;

                    #region TIE & Notes

                    if (runCmd >= 0xCF && cmd < 0x80) // Within running status
                    {
                        var peek = reader.PeekBytes(2);
                        if (peek[0] >= 0x80) command = AddNoteEvent(cmd, prevVelocity, 0, runCmd, out prevNote, out prevVelocity);
                        else if (peek[1] > 3 || peek[1] < 1) command = AddNoteEvent(cmd, reader.ReadByte(), 0, runCmd, out prevNote, out prevVelocity);
                        else command = AddNoteEvent(cmd, reader.ReadByte(), reader.ReadByte(), runCmd, out prevNote, out prevVelocity);
                    }
                    else if (cmd >= 0xCF)
                    {
                        var peek = reader.PeekBytes(3);
                        if (peek[0] >= 0x80) command = AddNoteEvent(prevNote, prevVelocity, 0, runCmd, out prevNote, out prevVelocity);
                        else if (peek[1] >= 0x80) command = AddNoteEvent(reader.ReadByte(), prevVelocity, 0, runCmd, out prevNote, out prevVelocity);
                        // TIE cannot have an added duration so it needs to stop here
                        else if (cmd == 0xCF || peek[2] > 3 || peek[2] < 1) command = AddNoteEvent(reader.ReadByte(), reader.ReadByte(), 0, runCmd, out prevNote, out prevVelocity);
                        else command = AddNoteEvent(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), runCmd, out prevNote, out prevVelocity);
                    }

                    #endregion

                    #region Rests

                    else if (cmd >= 0x80 && cmd <= 0xB0)
                        command = new RestCommand { Rest = SongEvent.RestFromCMD(0x80, cmd) };

                    #endregion

                    #region Commands

                    else if (runCmd < 0xCF && cmd < 0x80) // Commands within running status
                    {
                        switch (runCmd)
                        {
                            case 0xBD: command = new VoiceCommand { Voice = cmd }; break;
                            case 0xBE: command = new VolumeCommand { Volume = cmd }; break;
                            case 0xBF: command = new PanpotCommand { Panpot = (sbyte)(cmd - 0x40) }; break;
                            case 0xC0: command = new BendCommand { Bend = (sbyte)(cmd - 0x40) }; break;
                            case 0xC1: command = new BendRangeCommand { Range = cmd }; break;
                            case 0xC2: command = new LFOSpeedCommand { Speed = cmd }; break;
                            case 0xC3: command = new LFODelayCommand { Delay = cmd }; break;
                            case 0xC4: command = new ModDepthCommand { Depth = cmd }; break;
                            case 0xC5: command = new ModTypeCommand { Type = cmd }; break;
                            case 0xC8: command = new TuneCommand { Tune = (sbyte)(cmd - 0x40) }; break;
                            case 0xCD: command = new LibraryCommand { Command = cmd, Argument = reader.ReadByte() }; break;
                            case 0xCE: command = new EndOfTieCommand { Note = (sbyte)cmd }; prevNote = cmd; break;
                        }
                    }
                    else if (cmd > 0xB0 && cmd < 0xCF)
                    {
                        switch (cmd)
                        {
                            case 0xB1: // FINE & PREV
                            case 0xB6: command = new M4AFinishCommand { Type = cmd }; break;
                            case 0xB2: command = new GoToCommand { Offset = reader.ReadInt32() - ROM.Pak }; break;
                            case 0xB3: command = new CallCommand { Offset = reader.ReadInt32() - ROM.Pak }; break;
                            case 0xB4: command = new ReturnCommand(); break;
                            case 0xB5: command = new RepeatCommand { Times = reader.ReadByte(), Offset = reader.ReadInt32() - ROM.Pak }; break;
                            case 0xB9: command = new MemoryAccessCommand { Operator = reader.ReadByte(), Address = reader.ReadByte(), Data = reader.ReadByte() }; break;
                            case 0xBA: command = new PriorityCommand { Priority = reader.ReadByte() }; break;
                            case 0xBB: command = new TempoCommand { Tempo = (short)(reader.ReadByte() * 2) }; break;
                            case 0xBC: command = new KeyShiftCommand { Shift = reader.ReadSByte() }; break;
                            // Commands that work within running status:
                            case 0xBD: command = new VoiceCommand { Voice = reader.ReadByte() }; break;
                            case 0xBE: command = new VolumeCommand { Volume = reader.ReadByte() }; break;
                            case 0xBF: command = new PanpotCommand { Panpot = (sbyte)(reader.ReadByte() - 0x40) }; break;
                            case 0xC0: command = new BendCommand { Bend = (sbyte)(reader.ReadByte() - 0x40) }; break;
                            case 0xC1: command = new BendRangeCommand { Range = reader.ReadByte() }; break;
                            case 0xC2: command = new LFOSpeedCommand { Speed = reader.ReadByte() }; break;
                            case 0xC3: command = new LFODelayCommand { Delay = reader.ReadByte() }; break;
                            case 0xC4: command = new ModDepthCommand { Depth = reader.ReadByte() }; break;
                            case 0xC5: command = new ModTypeCommand { Type = reader.ReadByte() }; break;
                            case 0xC8: command = new TuneCommand { Tune = (sbyte)(reader.ReadByte() - 0x40) }; break;
                            case 0xCD: command = new LibraryCommand { Command = reader.ReadByte(), Argument = reader.ReadByte() }; break;
                            case 0xCE: // EOT
                                sbyte note;

                                if (reader.PeekByte() < 0x80)
                                {
                                    note = reader.ReadSByte();
                                    prevNote = (byte)note;
                                }
                                else
                                {
                                    note = -1;
                                }

                                command = new EndOfTieCommand { Note = note };
                                break;
                            default: Console.WriteLine("Invalid command: 0x{0:X7} = {1}", off, cmd); break;
                        }
                    }

                    #endregion

                    Commands[i].Add(new SongEvent(off, command));
                }
            }

            ICommand AddNoteEvent(byte note, byte velocity, byte addedDuration, byte runCmd, out byte prevNote, out byte prevVelocity)
            {
                return new M4ANoteCommand
                {
                    Note = (sbyte)(prevNote = note),
                    Velocity = prevVelocity = velocity,
                    Duration = (short)(runCmd == 0xCF ? -1 : (SongEvent.RestFromCMD(0xCF, runCmd) + addedDuration))
                };
            }
        }

        public override void SaveAsASM(string fileName)
        {
            if (NumTracks == 0)
                throw new InvalidDataException(Strings.ErrorNoTracks);

            using (var file = new StreamWriter(fileName))
            {
                string label = Assembler.FixLabel(Path.GetFileNameWithoutExtension(fileName));
                file.WriteLine("\t.include \"MPlayDef.s\"");
                file.WriteLine();
                file.WriteLine($"\t.equ\t{label}_grp, voicegroup000");
                file.WriteLine($"\t.equ\t{label}_pri, 0");
                file.WriteLine($"\t.equ\t{label}_rev, 0");
                file.WriteLine($"\t.equ\t{label}_mvl, 127");
                file.WriteLine($"\t.equ\t{label}_key, 0");
                file.WriteLine($"\t.equ\t{label}_tbs, 1");
                file.WriteLine($"\t.equ\t{label}_exg, 1");
                file.WriteLine($"\t.equ\t{label}_cmp, 1");
                file.WriteLine();
                file.WriteLine("\t.section .rodata");
                file.WriteLine($"\t.global\t{label}");
                file.WriteLine("\t.align\t2");

                for (int i = 0; i < Commands.Length; i++)
                {
                    int num = i + 1;
                    file.WriteLine();
                    file.WriteLine($"@**************** Track {num} ****************@");
                    file.WriteLine();
                    file.WriteLine($"{label}_{num}:");

                    var offsets = Commands[i].Where(e => e.Command is CallCommand || e.Command is GoToCommand || e.Command is RepeatCommand)
                        .Select(e => (int)(((dynamic)e.Command).Offset)).Distinct(); // Get all offsets we need labels for
                    int jumps = 0;
                    var labels = new Dictionary<int, string>();
                    foreach (int o in offsets)
                        labels.Add(o, $"{label}_{num}_{jumps++:D3}");
                    int ticks = 0;
                    bool displayed = false;
                    foreach (var e in Commands[i])
                    {
                        void DisplayRest(int rest)
                        {
                            byte amt = SongEvent.RestToCMD[rest];
                            file.WriteLine($"\t.byte\tW{amt:D2}");
                            int rem = rest - amt;
                            if (rem != 0)
                                file.WriteLine($"\t.byte\tW{rem:D2}");
                            ticks += rest; // TODO: Separate by 96 ticks
                            displayed = false;
                        }

                        var c = e.Command;

                        if (!displayed && ticks % 96 == 0)
                        {
                            file.WriteLine($"@ {ticks / 96:D3}\t----------------------------------------");
                            displayed = true;
                        }
                        int eOffset = e.GetOffset();
                        if (offsets.Contains(eOffset))
                            file.WriteLine($"{labels[eOffset]}:");

                        if (c == null)
                            continue;

                        if (c is TempoCommand tempo)
                            file.WriteLine($"\t.byte\tTEMPO , {tempo.Tempo}*{label}_tbs/2");
                        else if (c is RestCommand rest)
                        {
                            DisplayRest(rest.Rest);
                        }
                        else if (c is NoteCommand note)
                        {
                            // Hide base note, velocity and duration
                            dynamic dynote = note;
                            byte baseDur = dynote.Duration == -1 ? (byte)0 : SongEvent.RestToCMD[dynote.Duration];
                            int rem = dynote.Duration - baseDur;
                            string name = dynote.Duration == -1 ? "TIE" : $"N{baseDur:D2}";
                            string not = SongEvent.NoteName(dynote.Note, true);
                            string vel = $"v{dynote.Velocity:D3}";

                            if (dynote.Duration != -1 && rem != 0)
                                file.WriteLine($"\t.byte\t\t{name}   , {not} , {vel}, gtp{rem}");
                            else
                                file.WriteLine($"\t.byte\t\t{name}   , {not} , {vel}");
                        }
                        else if (c is EndOfTieCommand eot)
                        {
                            if (eot.Note != -1)
                                file.WriteLine("\t.byte\t\tEOT");
                            else
                                file.WriteLine($"\t.byte\t\tEOT   , {SongEvent.NoteName(eot.Note, true)}");
                        }
                        else if (c is VoiceCommand voice)
                            file.WriteLine($"\t.byte\t\tVOICE , {voice.Voice}");
                        else if (c is VolumeCommand volume)
                            file.WriteLine($"\t.byte\t\tVOL   , {volume.Volume}*{label}_mvl/mxv");
                        else if (c is PanpotCommand pan)
                            file.WriteLine($"\t.byte\t\tPAN   , {SongEvent.CenterValueString(pan.Panpot)}");
                        else if (c is BendCommand bend)
                            file.WriteLine($"\t.byte\t\tBEND  , {SongEvent.CenterValueString(bend.Bend)}");
                        else if (c is TuneCommand tune)
                            file.WriteLine($"\t.byte\t\tTUNE  , {SongEvent.CenterValueString(tune.Tune)}");
                        else if (c is BendRangeCommand bendr)
                            file.WriteLine($"\t.byte\t\tBENDR , {bendr.Range}");
                        else if (c is LFOSpeedCommand lfos)
                            file.WriteLine($"\t.byte\t\tLFOS  , {lfos.Speed}");
                        else if (c is LFODelayCommand lfodl)
                            file.WriteLine($"\t.byte\t\tLFODL , {lfodl.Delay}");
                        else if (c is ModDepthCommand mod)
                            file.WriteLine($"\t.byte\t\tMOD   , {mod.Depth}");
                        else if (c is ModTypeCommand modt)
                            file.WriteLine($"\t.byte\t\tMODT  , {modt.Type}");
                        else if (c is PriorityCommand prio)
                            file.WriteLine($"\t.byte\tPRIO , {prio.Priority}");
                        else if (c is KeyShiftCommand keysh)
                            file.WriteLine($"\t.byte\tKEYSH , {label}_key+{keysh.Shift}");
                        else if (c is GoToCommand goTo)
                        {
                            file.WriteLine("\t.byte\tGOTO");
                            file.WriteLine($"\t .word\t{labels[goTo.Offset]}");
                        }
                        else if (c is RepeatCommand rept)
                        {
                            file.WriteLine($"\t.byte\t\tREPT  , {rept.Times}");
                            file.WriteLine($"\t .word\t{labels[rept.Offset]}");
                        }
                        else if (c is M4AFinishCommand fine)
                        {
                            if (fine.Type == 0xB1)
                                file.WriteLine("\t.byte\tFINE");
                            else
                                file.WriteLine("\t.byte\t0xB6\t@PREV");
                        }
                        else if (c is CallCommand patt)
                        {
                            file.WriteLine("\t.byte\tPATT");
                            file.WriteLine($"\t .word\t{labels[patt.Offset]}");
                        }
                        else if (c is ReturnCommand pend)
                            file.WriteLine("\t.byte\tPEND");
                        else if (c is MemoryAccessCommand memacc)
                            file.WriteLine($"\t.byte\t\tMEMACC, {memacc.Operator,4}, {memacc.Address,4}, {memacc.Data}");
                        else if (c is LibraryCommand xcmd)
                            file.WriteLine($"\t.byte\t\tXCMD  , {xcmd.Command,4}, {xcmd.Argument}");
                    }
                }

                file.WriteLine();
                file.WriteLine("@******************************************@");
                file.WriteLine("\t.align\t2");
                file.WriteLine();
                file.WriteLine($"{label}:");
                file.WriteLine($"\t.byte\t{NumTracks}\t@ NumTrks");
                file.WriteLine($"\t.byte\t{(this is M4ASong m4asong ? m4asong.Header.NumBlocks : 0)}\t@ NumBlks");
                file.WriteLine($"\t.byte\t{label}_pri\t@ Priority");
                file.WriteLine($"\t.byte\t{label}_rev\t@ Reverb.");
                file.WriteLine();
                file.WriteLine($"\t.word\t{label}_grp");
                file.WriteLine();
                for (int i = 0; i < NumTracks; i++)
                    file.WriteLine($"\t.word\t{label}_{i + 1}");
                file.WriteLine();
                file.WriteLine("\t.end");
            }
        }
        public override void SaveAsMIDI(string fileName, MIDISaveArgs args)
        {
            if (NumTracks == 0)
                throw new InvalidDataException(Strings.ErrorNoTracks);

            CalculateTicks();
            var midi = new Sequence(24) { Format = 1 };
            var metaTrack = new Sanford.Multimedia.Midi.Track();
            midi.Add(metaTrack);
            AddTimeSignaturesToTrack(args, metaTrack);

            for (int i = 0; i < NumTracks; i++)
            {
                var track = new Sanford.Multimedia.Midi.Track();
                midi.Add(track);

                bool foundKeysh = false;
                int endOfPattern = 0, startOfPatternTicks = 0, endOfPatternTicks = 0, shift = 0;
                var playing = new List<M4ANoteCommand>();

                for (int j = 0; j < Commands[i].Count; j++)
                {
                    var e = Commands[i][j];
                    int ticks = e.AbsoluteTicks + (endOfPatternTicks - startOfPatternTicks);

                    // Preliminary check for saving events before keysh
                    switch (e.Command)
                    {
                        case KeyShiftCommand keysh: foundKeysh = true; break;
                        default:
                            // If we should not save before keysh then skip this event
                            if (!args.SaveBeforeKeysh && !foundKeysh)
                                continue;
                            break;
                    }
                    // Now do the event magic...
                    switch (e.Command)
                    {
                        case KeyShiftCommand keysh:
                            shift = keysh.Shift;
                            break;
                        case M4ANoteCommand note:
                            int n = (note.Note + shift).Clamp(0, 0x7F);
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.NoteOn, i, n, note.Velocity));
                            if (note.Duration != -1)
                                track.Insert(ticks + note.Duration, new ChannelMessage(ChannelCommand.NoteOff, i, n));
                            else
                                playing.Add(note);
                            break;
                        case EndOfTieCommand eot:
                            M4ANoteCommand nc = null;

                            if (eot.Note == -1)
                                nc = playing.LastOrDefault();
                            else
                                nc = playing.LastOrDefault(no => no.Note == eot.Note);

                            if (nc != null)
                            {
                                n = (nc.Note + shift).Clamp(0, 0x7F);
                                track.Insert(ticks, new ChannelMessage(ChannelCommand.NoteOff, i, n));
                                playing.Remove(nc);
                            }
                            break;
                        case PriorityCommand prio:
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, (int)ControllerType.VolumeFine, prio.Priority));
                            break;
                        case VoiceCommand voice:
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.ProgramChange, i, voice.Voice));
                            break;
                        case VolumeCommand vol:
                            // If we want to match a BaseVolume then we need to reverse calculate
                            double d = args.BaseVolume / (double)0x7F;
                            int volume = (int)(vol.Volume / d);
                            // If there are rounding errors, fix them (happens if BaseVolume is not 127 and BaseVolume is not vol.Volume)
                            if (volume * args.BaseVolume / 0x7F == vol.Volume - 1)
                                volume++;
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, (int)ControllerType.Volume, volume));
                            break;
                        case PanpotCommand pan:
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, (int)ControllerType.Pan, pan.Panpot + 0x40));
                            break;
                        case BendCommand bend:
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.PitchWheel, i, 0, bend.Bend + 0x40));
                            break;
                        case BendRangeCommand bendr:
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 20, bendr.Range));
                            break;
                        case LFOSpeedCommand lfos:
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 21, lfos.Speed));
                            break;
                        case LFODelayCommand lfodl:
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 26, lfodl.Delay));
                            break;
                        case ModDepthCommand mod:
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, (int)ControllerType.ModulationWheel, mod.Depth));
                            break;
                        case ModTypeCommand modt:
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 22, modt.Type));
                            break;
                        case TuneCommand tune:
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 24, tune.Tune));
                            break;
                        case LibraryCommand xcmd:
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 30, xcmd.Command));
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 29, xcmd.Argument));
                            break;
                        case MemoryAccessCommand memacc:
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 13, memacc.Operator));
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 14, memacc.Address));
                            track.Insert(ticks, new ChannelMessage(ChannelCommand.Controller, i, 12, memacc.Data));
                            break;
                        case TempoCommand tempo:
                            var change = new TempoChangeBuilder { Tempo = (60000000 / tempo.Tempo) };
                            change.Build();
                            metaTrack.Insert(ticks, change.Result);
                            break;
                        case CallCommand patt:
                            int callCmd = Commands[i].FindIndex(c => c.GetOffset() == patt.Offset);
                            endOfPattern = j;
                            endOfPatternTicks = e.AbsoluteTicks;
                            j = callCmd - 1; // -1 for incoming ++
                            startOfPatternTicks = Commands[i][j + 1].AbsoluteTicks;
                            break;
                        case ReturnCommand _:
                            if (endOfPattern != 0)
                            {
                                j = endOfPattern;
                                endOfPattern = startOfPatternTicks = endOfPatternTicks = 0;
                            }
                            break;
                        case GoToCommand goTo:
                            if (i == 0)
                            {
                                int jumpCmd = Commands[i].FindIndex(c => c.GetOffset() == goTo.Offset);
                                metaTrack.Insert(Commands[i][jumpCmd].AbsoluteTicks, new MetaMessage(MetaType.Marker, new byte[] { (byte)'[' }));
                                metaTrack.Insert(ticks, new MetaMessage(MetaType.Marker, new byte[] { (byte)']' }));
                            }
                            break;
                        case FinishCommand _:
                            // TODO: FINE vs PREV
                            // If the track is not only the finish command, place the finish command at the correct tick
                            if (track.Count > 1)
                                track.EndOfTrackOffset = e.AbsoluteTicks - track.GetMidiEvent(track.Count - 2).AbsoluteTicks;
                            goto endOfTrack;
                    }
                }

                endOfTrack:;
            }
            midi.Save(fileName);
        }
    }

    class M4AROMSong : M4ASong
    {
        public M4AROMSong(int offset)
        {
            SetOffset(offset);
            Load(ROM.Instance.ROMFile, ROM.Instance.Reader, offset);
        }
    }

    class M4AASMSong : M4ASong
    {
        public M4AASMSong(Assembler assembler, string headerLabel)
        {
            SetOffset(assembler.BaseOffset);
            var binary = assembler.Binary;
            var reader = new EndianBinaryReader(new MemoryStream(binary));
            Load(binary, reader, assembler[headerLabel]);
        }
    }

    abstract class MLSSSong : Song
    {
        public override void SaveAsASM(string fileName)
        {
            throw new PlatformNotSupportedException(Strings.ErrorEngineSaveASM);
        }
        public override void SaveAsMIDI(string fileName, MIDISaveArgs args)
        {
            if (NumTracks == 0)
                throw new InvalidDataException(Strings.ErrorNoTracks);

            CalculateTicks();
            var midi = new Sequence(48 * 2) { Format = 1 };
            var metaTrack = new Sanford.Multimedia.Midi.Track();
            midi.Add(metaTrack);
            // TOOD: Check if this is valid
            //AddTimeSignaturesToTrack(args, metaTrack);

            for (int i = 0; i < NumTracks; i++)
            {
                var track = new Sanford.Multimedia.Midi.Track();
                midi.Add(track);

                FreeNoteCommand freeNote = null;
                MidiEvent freeNoteOff = null;

                for (int j = 0; j < Commands[i].Count; j++)
                {
                    var e = Commands[i][j];

                    // Extended note ended ended and wasn't renewed
                    if (freeNoteOff != null && freeNoteOff.AbsoluteTicks < e.AbsoluteTicks * 2)
                    {
                        freeNote = null;
                        freeNoteOff = null;
                    }

                    switch (e.Command)
                    {
                        case VolumeCommand vol:
                            track.Insert(e.AbsoluteTicks * 2, new ChannelMessage(ChannelCommand.Controller, i, (int)ControllerType.Volume, vol.Volume / 2));
                            break;
                        case VoiceCommand voice:
                            track.Insert(e.AbsoluteTicks * 2, new ChannelMessage(ChannelCommand.ProgramChange, i, voice.Voice));
                            break;
                        case PanpotCommand pan:
                            track.Insert(e.AbsoluteTicks * 2, new ChannelMessage(ChannelCommand.Controller, i, (int)ControllerType.Pan, pan.Panpot / 2 + 0x40));
                            break;
                        case BendCommand bend:
                            track.Insert(e.AbsoluteTicks * 2, new ChannelMessage(ChannelCommand.PitchWheel, i, 0, bend.Bend / 2 + 0x40));
                            break;
                        case BendRangeCommand bendr:
                            track.Insert(e.AbsoluteTicks * 2, new ChannelMessage(ChannelCommand.Controller, i, 20, bendr.Range / 2));
                            break;
                        case MLSSNoteCommand note:
                            // Extended note is playing and it should be extended by this note
                            if (freeNote != null && freeNote.Note - 0x80 == note.Note)
                            {
                                // Move the note off command
                                track.Move(freeNoteOff, freeNoteOff.AbsoluteTicks + note.Duration * 2);
                            }
                            // Extended note is playing but this note is different OR there is no extended note playing
                            // Either way we play a new note and forget that one
                            else
                            {
                                track.Insert(e.AbsoluteTicks * 2, new ChannelMessage(ChannelCommand.NoteOn, i, note.Note, 0x7F));
                                track.Insert(e.AbsoluteTicks * 2 + note.Duration * 2, new ChannelMessage(ChannelCommand.NoteOff, i, note.Note));
                                freeNote = null;
                                freeNoteOff = null;
                            }
                            break;
                        case FreeNoteCommand free:
                            // Extended note is playing and it should be extended
                            if (freeNote != null && freeNote.Note == free.Note)
                            {
                                // Move the note off command
                                track.Move(freeNoteOff, freeNoteOff.AbsoluteTicks + free.Duration * 2);
                            }
                            // Extended note is playing but this note is different OR there is no extended note playing
                            // Either way we play a new note and forget that one
                            else
                            {
                                track.Insert(e.AbsoluteTicks * 2, new ChannelMessage(ChannelCommand.NoteOn, i, free.Note - 0x80, 0x7F));
                                track.Insert(e.AbsoluteTicks * 2 + free.Duration * 2, new ChannelMessage(ChannelCommand.NoteOff, i, free.Note - 0x80));
                                freeNote = free;
                                freeNoteOff = track.GetMidiEvent(track.Count - 2); // -1 would be the end of track event
                            }
                            break;
                        case TempoCommand tempo:
                            if (i == 0)
                            {
                                var change = new TempoChangeBuilder { Tempo = (60000000 / tempo.Tempo) };
                                change.Build();
                                metaTrack.Insert(e.AbsoluteTicks * 2, change.Result);
                            }
                            break;
                        case GoToCommand goTo:
                            if (i == 0)
                            {
                                int jumpCmd = Commands[i].FindIndex(c => c.GetOffset() == goTo.Offset);
                                metaTrack.Insert(Commands[i][jumpCmd].AbsoluteTicks * 2, new MetaMessage(MetaType.Marker, new byte[] { (byte)'[' }));
                                metaTrack.Insert(e.AbsoluteTicks * 2, new MetaMessage(MetaType.Marker, new byte[] { (byte)']' }));
                            }
                            break;
                        case FinishCommand _:
                            // If the track is not only the finish command, place the finish command at the correct tick
                            if (track.Count > 1)
                                track.EndOfTrackOffset = e.AbsoluteTicks - track.GetMidiEvent(track.Count - 2).AbsoluteTicks;
                            goto endOfTrack;
                    }
                }

                endOfTrack:;
            }
            midi.Save(fileName);
        }
    }

    class MLSSROMSong : MLSSSong
    {
        public MLSSROMSong(int offset)
        {
            SetOffset(offset);
            VoiceTable = VoiceTable.LoadTable<MLSSVoiceTable>(0, true); // 0 won't be used in the Load method

            int amt = GetTrackAmount(ROM.Instance.Reader.ReadInt16(offset));

            Commands = new List<SongEvent>[amt];
            for (int i = 0; i < amt; i++)
            {
                Commands[i] = new List<SongEvent>();
                int track = offset + ROM.Instance.Reader.ReadInt16(offset + 2 + (i * 2));
                ROM.Instance.Reader.BaseStream.Position = track;

                byte cmd = 0;
                while (cmd != 0xFF && cmd != 0xF8)
                {
                    int off = (int)ROM.Instance.Reader.BaseStream.Position;
                    ICommand command = null;

                    cmd = ROM.Instance.Reader.ReadByte();
                    switch (cmd)
                    {
                        case 0: command = new FreeNoteCommand { Note = ROM.Instance.Reader.ReadByte(), Duration = ROM.Instance.Reader.ReadByte() }; break;
                        case 0xF0: command = new VoiceCommand { Voice = ROM.Instance.Reader.ReadByte() }; break;
                        case 0xF1: command = new VolumeCommand { Volume = ROM.Instance.Reader.ReadByte() }; break;
                        case 0xF2: command = new PanpotCommand { Panpot = (sbyte)(ROM.Instance.Reader.ReadByte() - 0x80) }; break;
                        case 0xF4: command = new BendRangeCommand { Range = ROM.Instance.Reader.ReadByte() }; break;
                        case 0xF5: command = new BendCommand { Bend = ROM.Instance.Reader.ReadSByte() }; break;
                        case 0xF6: command = new RestCommand { Rest = ROM.Instance.Reader.ReadByte() }; break;
                        case 0xF8:
                            short offsetFromEnd = ROM.Instance.Reader.ReadInt16();
                            command = new GoToCommand { Offset = (int)(ROM.Instance.Reader.BaseStream.Position + offsetFromEnd) };
                            break;
                        case 0xF9: command = new TempoCommand { Tempo = ROM.Instance.Reader.ReadByte() }; break;
                        case 0xFF: command = new FinishCommand(); break;
                        default: command = new MLSSNoteCommand { Duration = cmd, Note = ROM.Instance.Reader.ReadSByte() }; break;
                    }
                    Commands[i].Add(new SongEvent(off, command));
                }
            }
        }

        int GetTrackAmount(short bits)
        {
            int num = 0;
            for (int i = 0; i < 16; i++)
            {
                if ((bits & 1 << i) != 0)
                {
                    num++;
                }
            }
            return num;
        }
    }

    class MLSSMIDISong : MLSSSong
    {
        public MLSSMIDISong(string fileName)
        {
            throw new PlatformNotSupportedException(Strings.ErrorEngineOpenMIDI);
        }
    }
}
