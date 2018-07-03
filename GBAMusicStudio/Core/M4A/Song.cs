using GBAMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static GBAMusicStudio.Core.M4A.M4AStructs;

namespace GBAMusicStudio.Core.M4A
{
    internal abstract class Song
    {
        protected SongHeader _header;
        internal int NumTracks => _header.NumTracks;

        internal List<SongEvent>[] Commands;
        int ticks = -1; // Cache the amount. Setting to -1 again will cause a refresh
        internal int NumTicks
        {
            get
            {
                if (ticks == -1)
                {
                    CalculateTicks();
                    foreach (var track in Commands)
                    {
                        if (track.Count == 0) continue; // Prevent crashes with invalid ones
                        uint length = track.Last().AbsoluteTicks;
                        if (length > ticks)
                            ticks = (int)length;
                    }
                }
                return ticks + 1;
            }
        }

        internal void CalculateTicks()
        {
            foreach (var track in Commands)
            {
                int length = 0, endOfPattern = 0;
                for (int i = 0; i < track.Count; i++)
                {
                    var e = track[i];
                    if (endOfPattern == 0)
                        e.AbsoluteTicks = (uint)length;

                    if (e.Command is RestCommand rest)
                        length += rest.Rest;
                    else if (e.Command is CallCommand call)
                    {
                        int jumpCmd = track.FindIndex(c => c.Offset == call.Offset);
                        endOfPattern = i;
                        i = jumpCmd - 1;
                    }
                    else if (e.Command is ReturnCommand && endOfPattern != 0)
                    {
                        i = endOfPattern;
                        endOfPattern = 0;
                    }
                }
            }
        }

        internal void SaveAsASM(string fileName)
        {
            if (NumTracks == 0)
                throw new InvalidDataException("This song has no tracks.");
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

                    var offsets = Commands[i].Where(e => e.Command is CallCommand || e.Command is GoToCommand).Select(e => (uint)(((dynamic)e.Command).Offset)).Distinct();
                    int jumps = 0;
                    var labels = new Dictionary<uint, string>();
                    foreach (uint o in offsets)
                        labels.Add(o, $"{label}_{num}_{jumps++:D3}");
                    int ticks = 0;
                    bool displayed = false;
                    foreach (var e in Commands[i])
                    {
                        var c = e.Command;

                        if (!displayed && ticks % 96 == 0)
                        {
                            file.WriteLine($"@ {ticks / 96:D3}\t----------------------------------------");
                            displayed = true;
                        }
                        if (offsets.Contains(e.Offset))
                            file.WriteLine($"{labels[e.Offset]}:");

                        file.Write("\t.byte\t");
                        if (c is TempoCommand tempo)
                            file.WriteLine($"TEMPO , {tempo.Tempo * 2}*{label}_tbs/2");
                        else if (c is RestCommand rest)
                        {
                            sbyte amt = SongEvent.RestToCMD[rest.Rest];
                            file.WriteLine($"W{amt:D2}");
                            int rem = rest.Rest - amt;
                            if (rem != 0)
                                file.WriteLine($"\t.byte\tW{rem:D2}");
                            ticks += rest.Rest; // TODO: Separate by 96 ticks
                            displayed = false;
                        }
                        else if (c is NoteCommand note)
                        {
                            sbyte baseDur = note.Duration == -1 ? (sbyte)-1 : SongEvent.RestToCMD[note.Duration];
                            int rem = note.Duration - baseDur;
                            string name = note.Duration == -1 ? "TIE" : $"N{baseDur:D2}";
                            string not = SongEvent.NoteName(note.Note, true);
                            string vel = $"v{note.Velocity:D3}";

                            if (note.Duration != -1 && rem != 0)
                                file.WriteLine($"\t{name}   , {not} , {vel}, gtp{rem}");
                            else
                                file.WriteLine($"\t{name}   , {not} , {vel}");
                        }
                        else if (c is EndOfTieCommand eot)
                        {
                            if (eot.Note != -1)
                                file.WriteLine("\tEOT");
                            else
                                file.WriteLine($"\tEOT   , {SongEvent.NoteName(eot.Note, true)}");
                        }
                        else if (c is VoiceCommand voice)
                            file.WriteLine($"\tVOICE , {voice.Voice}");
                        else if (c is VolumeCommand volume)
                            file.WriteLine($"\tVOL   , {volume.Volume}*{label}_mvl/mxv");
                        else if (c is PanpotCommand pan)
                            file.WriteLine($"\tPAN   , {SongEvent.CenterValueString(pan.Panpot)}");
                        else if (c is BendCommand bend)
                            file.WriteLine($"\tBEND  , {SongEvent.CenterValueString(bend.Bend)}");
                        else if (c is TuneCommand tune)
                            file.WriteLine($"\tTUNE  , {SongEvent.CenterValueString(tune.Tune)}");
                        else if (c is BendRangeCommand bendr)
                            file.WriteLine($"\tBENDR , {bendr.Range}");
                        else if (c is LFOSpeedCommand lfos)
                            file.WriteLine($"\tLFOS  , {lfos.Speed}");
                        else if (c is LFODelayCommand lfodl)
                            file.WriteLine($"\tLFODL , {lfodl.Delay}");
                        else if (c is ModDepthCommand mod)
                            file.WriteLine($"\tMOD   , {mod.Depth}");
                        else if (c is ModTypeCommand modt)
                            file.WriteLine($"\tMODT  , {modt.Type}");
                        else if (c is PriorityCommand prio)
                            file.WriteLine($"PRIO , {prio.Priority}");
                        else if (c is KeyShiftCommand keysh)
                            file.WriteLine($"KEYSH , {label}_key+{keysh.Shift}");
                        else if (c is GoToCommand goTo)
                        {
                            file.WriteLine("GOTO");
                            file.WriteLine($"\t .word\t{labels[goTo.Offset]}");
                        }
                        else if (c is FinishCommand fine)
                            file.WriteLine("FINE");
                        else if (c is CallCommand patt)
                        {
                            file.WriteLine("PATT");
                            file.WriteLine($"\t .word\t{labels[patt.Offset]}");
                        }
                        else if (c is ReturnCommand pend)
                            file.WriteLine("PEND");
                        else if (c is RepeatCommand rept)
                            file.WriteLine($"\tREPT  , {rept.Arg}");
                        else if (c is MemoryAccessCommand memacc)
                            file.WriteLine($"\tMEMACC, {memacc.Arg1,4}, {memacc.Arg2,4}, {memacc.Arg3}");
                        else if (c is LibraryCommand xcmd)
                            file.WriteLine($"\tXCMD  , {xcmd.Arg1,4}, {xcmd.Arg2}");
                    }
                }

                file.WriteLine();
                file.WriteLine("@******************************************@");
                file.WriteLine("\t.align\t2");
                file.WriteLine();
                file.WriteLine($"{label}:");
                file.WriteLine($"\t.byte\t{NumTracks}\t@ NumTrks");
                file.WriteLine($"\t.byte\t{_header.NumBlocks}\t@ NumBlks");
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
    }

    internal abstract class M4ASong : Song
    {
        byte[] _binary;

        protected void Load(byte[] binary, SongHeader head)
        {
            _binary = binary;
            _header = head;
            Array.Resize(ref _header.Tracks, _header.NumTracks); // Not really necessary
            Commands = new List<SongEvent>[_header.NumTracks];
            for (int i = 0; i < _header.NumTracks; i++)
                Commands[i] = new List<SongEvent>();

            SongPlayer.VoiceTable = new VoiceTable();
            SongPlayer.VoiceTable.Load(_header.VoiceTable);

            if (NumTracks == 0 || NumTracks > 16) return;

            var reader = new ROMReader();
            reader.InitReader(_binary);

            for (int i = 0; i < NumTracks; i++)
            {
                reader.SetOffset(_header.Tracks[i]);

                byte cmd = 0, runCmd = 0, prevNote = 0, prevVelocity = 127;

                while (cmd != 0xB1)
                {
                    uint off = reader.Position;
                    ICommand command = null;

                    cmd = reader.ReadByte();
                    if (cmd >= 0xBD) // Commands that work within running status
                        runCmd = cmd;

                    #region TIE & Notes

                    if (runCmd >= 0xCF && cmd < 0x80) // Within running status
                    {
                        var o = reader.Position;
                        byte peek1 = reader.ReadByte(),
                            peek2 = reader.ReadByte();
                        reader.SetOffset(o);
                        if (peek1 >= 128) command = AddNoteEvent(cmd, prevVelocity, 0, runCmd, out prevNote, out prevVelocity);
                        else if (peek2 >= 128) command = AddNoteEvent(cmd, reader.ReadByte(), 0, runCmd, out prevNote, out prevVelocity);
                        else command = AddNoteEvent(cmd, reader.ReadByte(), reader.ReadByte(), runCmd, out prevNote, out prevVelocity);
                    }
                    else if (cmd >= 0xCF)
                    {
                        var o = reader.Position;
                        byte peek1 = reader.ReadByte(),
                            peek2 = reader.ReadByte(),
                            peek3 = reader.ReadByte();
                        reader.SetOffset(o);
                        if (peek1 >= 128) command = AddNoteEvent(prevNote, prevVelocity, 0, runCmd, out prevNote, out prevVelocity);
                        else if (peek2 >= 128) command = AddNoteEvent(reader.ReadByte(), prevVelocity, 0, runCmd, out prevNote, out prevVelocity);
                        // TIE cannot have an added duration so it needs to stop here
                        else if (cmd == 0xCF || peek3 >= 128) command = AddNoteEvent(reader.ReadByte(), reader.ReadByte(), 0, runCmd, out prevNote, out prevVelocity);
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
                            case 0xBE: command = new VolumeCommand { Volume = (sbyte)cmd }; break;
                            case 0xBF: command = new PanpotCommand { Panpot = (sbyte)(cmd - 0x40) }; break;
                            case 0xC0: command = new BendCommand { Bend = (sbyte)(cmd - 0x40) }; break;
                            case 0xC1: command = new BendRangeCommand { Range = cmd }; break;
                            case 0xC2: command = new LFOSpeedCommand { Speed = cmd }; break;
                            case 0xC3: command = new LFODelayCommand { Delay = cmd }; break;
                            case 0xC4: command = new ModDepthCommand { Depth = cmd }; break;
                            case 0xC5: command = new ModTypeCommand { Type = cmd }; break;
                            case 0xC8: command = new TuneCommand { Tune = (sbyte)(cmd - 0x40) }; break;
                            case 0xCD: command = new LibraryCommand { Arg1 = cmd, Arg2 = reader.ReadByte() }; break;
                            case 0xCE: command = new EndOfTieCommand { Note = (sbyte)cmd }; prevNote = cmd; break;
                        }
                    }
                    else if (cmd > 0xB0 && cmd < 0xCF)
                    {
                        switch (cmd)
                        {
                            case 0xB1: command = new FinishCommand(); break;
                            case 0xB2: command = new GoToCommand { Offset = reader.ReadPointer() }; break;
                            case 0xB3: command = new CallCommand { Offset = reader.ReadPointer() }; break;
                            case 0xB4: command = new ReturnCommand(); break;
                            case 0xB5: command = new RepeatCommand { Arg = reader.ReadByte() }; break;
                            case 0xB9: command = new MemoryAccessCommand { Arg1 = reader.ReadByte(), Arg2 = reader.ReadByte(), Arg3 = reader.ReadByte() }; break;
                            case 0xBA: command = new PriorityCommand { Priority = reader.ReadByte() }; break;
                            case 0xBB: command = new TempoCommand { Tempo = reader.ReadByte() }; break;
                            case 0xBC: command = new KeyShiftCommand { Shift = reader.ReadSByte() }; break;
                            // Commands that work within running status:
                            case 0xBD: command = new VoiceCommand { Voice = reader.ReadByte() }; break;
                            case 0xBE: command = new VolumeCommand { Volume = reader.ReadSByte() }; break;
                            case 0xBF: command = new PanpotCommand { Panpot = (sbyte)(reader.ReadByte() - 0x40) }; break;
                            case 0xC0: command = new BendCommand { Bend = (sbyte)(reader.ReadByte() - 0x40) }; break;
                            case 0xC1: command = new BendRangeCommand { Range = reader.ReadByte() }; break;
                            case 0xC2: command = new LFOSpeedCommand { Speed = reader.ReadByte() }; break;
                            case 0xC3: command = new LFODelayCommand { Delay = reader.ReadByte() }; break;
                            case 0xC4: command = new ModDepthCommand { Depth = reader.ReadByte() }; break;
                            case 0xC5: command = new ModTypeCommand { Type = reader.ReadByte() }; break;
                            case 0xC8: command = new TuneCommand { Tune = (sbyte)(reader.ReadByte() - 0x40) }; break;
                            case 0xCD: command = new LibraryCommand { Arg1 = reader.ReadByte(), Arg2 = reader.ReadByte() }; break;
                            case 0xCE: // EOT
                                sbyte note;

                                if (reader.PeekByte() < 128) { note = reader.ReadSByte(); prevNote = (byte)note; }
                                else { note = -1; }

                                command = new EndOfTieCommand { Note = note };
                                break;
                            default: Console.WriteLine("Invalid command: 0x{0:X} = {1}", reader.Position, cmd); break;
                        }
                    }

                    #endregion

                    Commands[i].Add(new SongEvent(off, command));
                }
            }

            ICommand AddNoteEvent(byte note, byte velocity, byte addedDuration, byte runCmd, out byte prevNote, out byte prevVelocity)
            {
                return new NoteCommand
                {
                    Note = (sbyte)(prevNote = note),
                    Velocity = (sbyte)(prevVelocity = velocity),
                    Duration = (sbyte)(runCmd == 0xCF ? -1 : (SongEvent.RestFromCMD(0xCF, runCmd) + addedDuration))
                };
            }
        }
    }

    internal class ROMSong : M4ASong
    {
        readonly ushort num, table;

        internal ROMSong(ushort songNum, ushort tableNum)
        {
            num = songNum;
            table = tableNum;

            Load(ROM.Instance.ROMFile,
                ROM.Instance.ReadStruct<SongHeader>(ROM.Instance.ReadPointer(ROM.Instance.Game.SongTables[table] + ((uint)8 * num))));
        }
    }

    internal class ASMSong : M4ASong
    {
        internal ASMSong(Assembler assembler, string headerLabel)
        {
            var binary = assembler.Binary;
            Load(binary, Utils.ReadStruct<SongHeader>(binary, (uint)assembler[headerLabel]));
        }
    }

    /* This didn't work well. Also, there's no point in re-inventing the wheel (mid2agb by Nintendo exists)
    I used https://github.com/Kermalis/MidiSharp if you are interested

    internal class MIDISong : Song
    {
        readonly MidiSequence midi;
        readonly MidiTrackCollection tracks;

        internal MIDISong(string fileName)
        {
            using (Stream inputStream = File.OpenRead(fileName))
            {
                midi = MidiSequence.Open(inputStream);
            }

            tracks = midi.Tracks;
            if (midi.Tracks.Count > 16) // Merge channels
            {
                var channels = midi.Tracks.Select(t => t.Channel).Distinct().ToArray();
                foreach (var chan in channels)
                {
                    var thisChan = midi.Tracks.Where(t => t.Channel == chan).ToArray();
                    for (int i = 1; i < thisChan.Length; i++)
                    {
                        thisChan[0].Merge(thisChan[i]); // Merge ignores its own track
                        tracks.Remove(thisChan[i]);
                    }
                }
            }

            MidiTrack firstRegular = midi.Tracks.First(t => t.Channel != -1);
            foreach (var track in tracks.ToArray())
            {
                if (track.Channel == -1)
                {
                    firstRegular.Merge(track);
                    tracks.Remove(track);
                }
            }

            Commands = new List<SongEvent>[tracks.Count];
            for (int i = 0; i < tracks.Count; i++)
                Commands[i] = new List<SongEvent>();

            //float tsHelper = midi.TicksPerBeatOrFrame / 48f;
            //float bpmMultiplier = tsHelper * (4 / 2); // (Default TimeSignature) 

            for (int i = 0; i < NumTracks; i++)
            {
                var track = midi.Tracks[i];
                long previous = 0;

                foreach (var e in track.Events)
                {
                    Command command = 0;
                    var args = new int[0];

                    if (e is MetaMidiEvent me)
                    {
                        if (me is EndOfTrackMetaMidiEvent endme) command = Command.Finish;
                        else if (me is TempoMetaMidiEvent tme)
                        {
                            command = Command.Tempo; args = new int[] { ((60 * 1000 * 1000 / tme.Value) - 1) / 2 * 2 * 2 }; // Get rid of odd values
                        }
                        else if (me is TimeSignatureMetaMidiEvent tsme)
                        {
                            int ticksPerQuarterNote = 96 / tsme.MidiClocksPerClick; // 96 / 24 = 4 (Metronome clicks once per 4 notes)
                            int quarters = 32 / tsme.NumberOfNotated32nds; // 32 / 8 = 4 (4 quarter notes in quarter of a beat)
                            //bpmMultiplier = tsHelper * tsme.Numerator / tsme.Denominator; // For now, ignoring the above
                            continue;
                        }
                        else continue;
                    }
                    else if (e is VoiceMidiEvent ve)
                    {
                        if (ve is ProgramChangeVoiceMidiEvent pcve) { command = Command.Voice; args = new int[] { pcve.Number }; }
                        else if (ve is PitchWheelVoiceMidiEvent pwve) { command = Command.Bend; args = new int[] { pwve.Position - 0x40 }; }
                        else if (ve is OnNoteVoiceMidiEvent nve)
                        {
                            if (nve.Velocity != 0)
                            {
                                command = Command.NoteOn; args = new int[] { nve.Note, nve.Velocity, (int)nve.Duration };
                            }
                            else continue;
                        }
                        else if (ve is ControllerVoiceMidiEvent cve)
                        {
                            switch ((Controller)cve.Number)
                            {
                                case Controller.DataEntryCourse: command = Command.BendRange; args = new int[] { cve.Value }; break;
                                case Controller.PanPositionCourse: command = Command.Panpot; args = new int[] { cve.Value - 0x40 }; break;
                                case Controller.VolumeCourse: command = Command.Volume; args = new int[] { cve.Value }; break;
                                default: continue;
                            }
                        }
                        else continue;
                    }
                    else continue;

                    if (e.DeltaTime != 0)
                    {
                        Commands[i].Add(new SongEvent((uint)e.AbsoluteTime, Command.Rest, new int[] { (int)(e.AbsoluteTime - previous) }));
                        previous = e.AbsoluteTime;
                    }
                    Commands[i].Add(new SongEvent((uint)e.AbsoluteTime, command, args));
                }
            }
        }
    }*/
}
