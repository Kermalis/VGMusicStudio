using Kermalis.EndianBinaryIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal sealed partial class MP2KLoadedSong
{
    private void AddEvent(byte trackIndex, long cmdOffset, ICommand command)
    {
        Events[trackIndex].Add(new SongEvent(cmdOffset, command));
    }
    public override void InsertEvent(SongEvent e, int trackIndex, int insertIndex)
    {
        Events[trackIndex]!.Insert(insertIndex, e);
        CalculateTicks(trackIndex);
    }
    public override void ChangeEvent(SongEvent ev, decimal vArgsVal1, byte vArgsVal2, bool changed)
    {
        if (ev.Command is VoiceCommand voice && voice.Voice == vArgsVal1)
        {
            voice.Voice = vArgsVal2;
            changed = true;
        }
    }
    public override void RemoveEvent(int trackIndex, int eventIndex)
    {
        Events[trackIndex].RemoveAt(eventIndex);
        CalculateTicks(trackIndex);
    }
    public override CommandArg[] GetCommandMembers(int trackIndex, int eventIndex)
    {
        var se = MP2KEngine.MP2KInstance!.Player.LoadedSong!.Events[trackIndex]![eventIndex]!;
        MemberInfo[] ic = typeof(ICommand).GetMembers();
        MemberInfo[] iseq = typeof(ISequenceCommand).GetMembers();
        MemberInfo[] irun = typeof(IRunCommand).GetMembers();
        MemberInfo[] ignore = ic.Combine(iseq).Combine(irun);
        bool FindMembers(MemberInfo m)
        {
            bool ByName(MemberInfo a)
            {
                return m.Name == a.Name;
            }
            return !ignore.Any(ByName) && (m is FieldInfo || m is PropertyInfo);
        }
        MemberInfo[] mi = se.Command == null ? [] : [.. se.Command.GetType().GetMembers().Where(FindMembers)];
        CommandArg[] args = new CommandArg[mi.Length];
        for (int i = 0; i < mi.Length; i++)
        {
            TypeInfo valueType;
            object value;
            args[i].Name = mi[i].Name;
            if (mi[i].MemberType == MemberTypes.Field)
            {
                valueType = (TypeInfo)((FieldInfo)mi[i]).FieldType;
                value = ((FieldInfo)mi[i]).GetValue(se.Command)!;
            }
            else
            {
                valueType = (TypeInfo)((PropertyInfo)mi[i]).PropertyType;
                value = ((PropertyInfo)mi[i]).GetValue(se.Command)!;
            }

            switch (valueType.Name)
            {
                case "CommandPointer":
                    {
                        CommandPointer ptr = (CommandPointer)value;
                        args[i].Type = CommandArg.ValueType.Pointer;
                        args[i].Value = ptr.Index;
                        args[i].Offset = ptr.Offset;
                        break;
                    }
                case "Boolean":
                    {
                        args[i].Type = CommandArg.ValueType.Boolean;
                        args[i].Value = (int)Convert.ChangeType(value, TypeCode.Int32);
                        break;
                    }
                default:
                    {
                        args[i].Type = CommandArg.ValueType.Number;
                        args[i].Value = (int)Convert.ChangeType(value, TypeCode.Int32);
                        break;
                    }
            }

            object lower = null!;
            object upper = null!;
            foreach (var val in valueType.DeclaredFields)
            {
                if (val.Name == "MinValue")
                {
                    lower = val.GetValue(mi[i])!;
                }
                if (val.Name == "MaxValue")
                {
                    upper = val.GetValue(mi[i])!;
                }
            }

            if (lower is not null && upper is not null)
            {
                args[i].MinValue = (int)Convert.ChangeType(lower, TypeCode.Int32);
                args[i].MaxValue = (int)Convert.ChangeType(upper, TypeCode.Int32);
            }
            else
            {
                args[i].MinValue = -100;
                args[i].MaxValue = 100;
            }
        }
        return args;
    }

    public override bool CallOrJumpCommand(SongEvent e)
    {
        return e.Command is CallCommand || e.Command is JumpCommand;
    }
    private bool EventExists(byte trackIndex, long cmdOffset)
    {
        return Events[trackIndex].Exists(e => e.Offset == cmdOffset);
    }

    private void EmulateNote(byte trackIndex, long cmdOffset, byte key, byte velocity, byte addedDuration, ref byte runCmd, ref byte prevKey, ref byte prevVelocity)
    {
        prevKey = key;
        prevVelocity = velocity;
        if (EventExists(trackIndex, cmdOffset))
        {
            return;
        }

        AddEvent(trackIndex, cmdOffset, new NoteCommand
        {
            Note = key,
            Velocity = velocity,
            Duration = runCmd == 0xCF ? -1 : (MP2KUtils.RestTable[runCmd - 0xCF] + addedDuration),
            IsRepeated = runCmd >= 0xCF && key <= 0x7F,
        });
    }

    private void AddTrackEvents(byte trackIndex, long trackStart)
    {
        Events[trackIndex] = new List<SongEvent>();
        byte runCmd = 0;
        byte prevKey = 0;
        byte prevVelocity = 0x7F;
        int callStackDepth = 0;
        AddEvents(trackIndex, trackStart, ref runCmd, ref prevKey, ref prevVelocity, ref callStackDepth);
    }
    private void AddEvents(byte trackIndex, long startOffset, ref byte runCmd, ref byte prevKey, ref byte prevVelocity, ref int callStackDepth)
    {
        using (var ms = new MemoryStream(_player.Config.ROM))
        {
            var r = new EndianBinaryReader(ms, ascii: true);
            r.Stream.Position = startOffset;

            Span<byte> peek = stackalloc byte[3];
            bool cont = true;
            while (cont)
            {
                long offset = r.Stream.Position;

                byte cmd = r.ReadByte();
                if (cmd >= 0xBD) // Commands that work within running status
                {
                    runCmd = cmd;
                }

                #region TIE & Notes

                if (runCmd >= 0xCF && cmd <= 0x7F) // Within running status
                {
                    byte velocity, addedDuration;
                    r.PeekBytes(peek.Slice(0, 2));
                    if (peek[0] > 0x7F)
                    {
                        velocity = prevVelocity;
                        addedDuration = 0;
                    }
                    else if (peek[1] > 3)
                    {
                        velocity = r.ReadByte();
                        addedDuration = 0;
                    }
                    else
                    {
                        velocity = r.ReadByte();
                        addedDuration = r.ReadByte();
                    }
                    EmulateNote(trackIndex, offset, cmd, velocity, addedDuration, ref runCmd, ref prevKey, ref prevVelocity);
                }
                else if (cmd >= 0xCF)
                {
                    byte key, velocity, addedDuration;
                    r.PeekBytes(peek);
                    if (peek[0] > 0x7F)
                    {
                        key = prevKey;
                        velocity = prevVelocity;
                        addedDuration = 0;
                    }
                    else if (peek[1] > 0x7F)
                    {
                        key = r.ReadByte();
                        velocity = prevVelocity;
                        addedDuration = 0;
                    }
                    // TIE (0xCF) cannot have an added duration so it needs to stop here
                    else if (cmd == 0xCF || peek[2] > 3)
                    {
                        key = r.ReadByte();
                        velocity = r.ReadByte();
                        addedDuration = 0;
                    }
                    else
                    {
                        key = r.ReadByte();
                        velocity = r.ReadByte();
                        addedDuration = r.ReadByte();
                    }
                    EmulateNote(trackIndex, offset, key, velocity, addedDuration, ref runCmd, ref prevKey, ref prevVelocity);
                }

                #endregion

                #region Rests

                else if (cmd is >= 0x80 and <= 0xB0)
                {
                    if (!EventExists(trackIndex, offset))
                    {
                        AddEvent(trackIndex, offset, new RestCommand { Rest = MP2KUtils.RestTable[cmd - 0x80] });
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
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new VoiceCommand { Voice = cmd, IsRepeated = true });
                                }
                                break;
                            }
                        case 0xBE:
                            {
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new VolumeCommand { Volume = cmd, IsRepeated = true });
                                }
                                break;
                            }
                        case 0xBF:
                            {
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new PanpotCommand { Panpot = (sbyte)(cmd - 0x40), IsRepeated = true });
                                }
                                break;
                            }
                        case 0xC0:
                            {
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new PitchBendCommand { Bend = (sbyte)(cmd - 0x40), IsRepeated = true });
                                }
                                break;
                            }
                        case 0xC1:
                            {
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new PitchBendRangeCommand { Range = cmd, IsRepeated = true });
                                }
                                break;
                            }
                        case 0xC2:
                            {
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new LFOSpeedCommand { Speed = cmd, IsRepeated = true });
                                }
                                break;
                            }
                        case 0xC3:
                            {
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new LFODelayCommand { Delay = cmd, IsRepeated = true });
                                }
                                break;
                            }
                        case 0xC4:
                            {
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new LFODepthCommand { Depth = cmd, IsRepeated = true });
                                }
                                break;
                            }
                        case 0xC5:
                            {
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new LFOTypeCommand { Type = (LFOType)cmd, IsRepeated = true });
                                }
                                break;
                            }
                        case 0xC8:
                            {
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new TuneCommand { Tune = (sbyte)(cmd - 0x40), IsRepeated = true });
                                }
                                break;
                            }
                        case 0xCD:
                            {
                                byte arg = r.ReadByte();
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new LibraryCommand { LibraryCommandType = (ExtendedCommandType)cmd, Argument = arg, IsRepeated = true });
                                }
                                break;
                            }
                        case 0xCE:
                            {
                                prevKey = cmd;
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new EndOfTieCommand { Note = cmd, IsRepeated = true });
                                }
                                break;
                            }
                        default: throw new MP2KInvalidRunningStatusCMDException(trackIndex, (int)offset, runCmd);
                    }
                }
                else if (cmd is > 0xB0 and < 0xCF)
                {
                    switch (cmd)
                    {
                        case 0xB1:
                        case 0xB6:
                            {
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new FinishCommand { Prev = cmd == 0xB6 });
                                }
                                cont = false;
                                break;
                            }
                        case 0xB2:
                            {
                                CommandPointer pointer = new()
                                {
                                    Offset = r.ReadInt32() - GBAUtils.CARTRIDGE_OFFSET
                                };
                                bool GetSongEvent(SongEvent ev)
                                {
                                    return ev.Offset == pointer.Offset;
                                }
                                pointer.Index = Events[trackIndex].FindIndex(GetSongEvent);
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new JumpCommand { Pointer = pointer });
                                    if (!EventExists(trackIndex, pointer.Offset))
                                    {
                                        AddEvents(trackIndex, pointer.Offset, ref runCmd, ref prevKey, ref prevVelocity, ref callStackDepth);
                                    }
                                }
                                break;
                            }
                        case 0xB3:
                            {
                                CommandPointer pointer = new()
                                {
                                    Offset = r.ReadInt32() - GBAUtils.CARTRIDGE_OFFSET
                                };
                                bool GetSongEvent(SongEvent ev)
                                {
                                    return ev.Offset == pointer.Offset;
                                }
                                pointer.Index = Events[trackIndex].FindIndex(GetSongEvent);
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new CallCommand { Pointer = pointer });
                                }
                                if (callStackDepth < 3)
                                {
                                    long backup = r.Stream.Position;
                                    callStackDepth++;
                                    AddEvents(trackIndex, pointer.Offset, ref runCmd, ref prevKey, ref prevVelocity, ref callStackDepth);
                                    r.Stream.Position = backup;
                                }
                                else
                                {
                                    throw new MP2KTooManyNestedCallsException(trackIndex);
                                }
                                break;
                            }
                        case 0xB4:
                            {
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new ReturnCommand());
                                }
                                if (callStackDepth != 0)
                                {
                                    cont = false;
                                    callStackDepth--;
                                }
                                break;
                            }
                        case 0xB5:
                            {
                                byte times = r.ReadByte();
                                CommandPointer pointer = new()
                                {
                                    Offset = r.ReadInt32() - GBAUtils.CARTRIDGE_OFFSET
                                };
                                bool GetSongEvent(SongEvent ev)
                                {
                                    return ev.Offset == pointer.Offset;
                                }
                                pointer.Index = Events[trackIndex].FindIndex(GetSongEvent);
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new RepeatCommand { Times = times, Pointer = pointer });
                                }
                                break;
                            }
                        case 0xB9:
                            {
                                byte op = r.ReadByte();
                                byte address = r.ReadByte();
                                byte data = r.ReadByte();
                                CommandPointer pointer = new()
                                {
                                    Offset = -1,
                                    Index = -1
                                };
                                if (op >= (byte)MemoryOperatorType.MemBEq && op <= (byte)MemoryOperatorType.MemMemBLo)
                                {
                                    pointer.Offset = r.ReadInt32() - GBAUtils.CARTRIDGE_OFFSET;
                                    bool GetSongEvent(SongEvent ev)
                                    {
                                        return ev.Offset == pointer.Offset;
                                    }
                                    pointer.Index = Events[trackIndex].FindIndex(GetSongEvent);
                                }
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new MemoryAccessCommand { Operator = (MemoryOperatorType)op, MemoryAreaAddress = address, Data = data, Pointer = pointer });
                                }
                                break;
                            }
                        case 0xBA:
                            {
                                byte priority = r.ReadByte();
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new PriorityCommand { Priority = priority });
                                }
                                break;
                            }
                        case 0xBB:
                            {
                                byte tempoArg = r.ReadByte();
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new TempoCommand { Tempo = (ushort)(tempoArg * 2) });
                                }
                                break;
                            }
                        case 0xBC:
                            {
                                sbyte transpose = r.ReadSByte();
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new TransposeCommand { Transpose = transpose });
                                }
                                break;
                            }
                        // Commands that work within running status:
                        case 0xBD:
                            {
                                byte voice = r.ReadByte();
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new VoiceCommand { Voice = voice, IsRepeated = false });
                                }
                                break;
                            }
                        case 0xBE:
                            {
                                byte volume = r.ReadByte();
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new VolumeCommand { Volume = volume, IsRepeated = false });
                                }
                                break;
                            }
                        case 0xBF:
                            {
                                byte panArg = r.ReadByte();
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new PanpotCommand { Panpot = (sbyte)(panArg - 0x40), IsRepeated = false });
                                }
                                break;
                            }
                        case 0xC0:
                            {
                                byte bendArg = r.ReadByte();
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new PitchBendCommand { Bend = (sbyte)(bendArg - 0x40), IsRepeated = false });
                                }
                                break;
                            }
                        case 0xC1:
                            {
                                byte range = r.ReadByte();
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new PitchBendRangeCommand { Range = range, IsRepeated = false });
                                }
                                break;
                            }
                        case 0xC2:
                            {
                                byte speed = r.ReadByte();
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new LFOSpeedCommand { Speed = speed, IsRepeated = false });
                                }
                                break;
                            }
                        case 0xC3:
                            {
                                byte delay = r.ReadByte();
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new LFODelayCommand { Delay = delay, IsRepeated = false });
                                }
                                break;
                            }
                        case 0xC4:
                            {
                                byte depth = r.ReadByte();
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new LFODepthCommand { Depth = depth, IsRepeated = false });
                                }
                                break;
                            }
                        case 0xC5:
                            {
                                byte type = r.ReadByte();
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new LFOTypeCommand { Type = (LFOType)type, IsRepeated = false });
                                }
                                break;
                            }
                        case 0xC8:
                            {
                                byte tuneArg = r.ReadByte();
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new TuneCommand { Tune = (sbyte)(tuneArg - 0x40), IsRepeated = false });
                                }
                                break;
                            }
                        case 0xCD:
                            {
                                byte command = r.ReadByte();
                                byte arg = r.ReadByte();
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new LibraryCommand { LibraryCommandType = (ExtendedCommandType)command, Argument = arg, IsRepeated = false });
                                }
                                break;
                            }
                        case 0xCE:
                            {
                                int key = r.PeekByte() <= 0x7F ? (prevKey = r.ReadByte()) : -1;
                                if (!EventExists(trackIndex, offset))
                                {
                                    AddEvent(trackIndex, offset, new EndOfTieCommand { Note = key, IsRepeated = false });
                                }
                                break;
                            }
                        default: throw new MP2KInvalidCMDException(trackIndex, (int)offset, cmd);
                    }
                }

                #endregion
            }
        }
    }

    public void CalculateTicks(int trackIndex)
    {
        List<SongEvent> track = Events[trackIndex];

        int length = 0, endOfPattern = 0;
        for (int i = 0; i < track.Count; i++)
        {
            SongEvent e = track[i];
            if (endOfPattern == 0)
            {
                e.Ticks.Add(length);
            }
            if (e.Command is RestCommand rest)
            {
                length += rest.Rest;
            }
            else if (e.Command is CallCommand call)
            {
                bool FindByOffset(SongEvent c)
                {
                    return c.Offset == call.Pointer.Offset;
                }
                int jumpCmd = track.FindIndex(FindByOffset);
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
    public void SetTicks()
    {
        MaxTicks = 0;
        bool u = false;
        for (int trackIndex = 0; trackIndex < Events.Length; trackIndex++)
        {
            List<SongEvent> evs = Events[trackIndex];
            int SortByOffset(SongEvent ev1, SongEvent ev2)
            {
                return ev1.Offset.CompareTo(ev2.Offset);
            }
            evs.Sort(SortByOffset);

            MP2KTrack track = Tracks[trackIndex];
            track.Init();

            _player.ElapsedTicks = 0;
            while (true)
            {
                bool GetSongEvent(SongEvent ev)
                {
                    return ev.Offset == track.ROMOffset;
                }
                SongEvent e = evs.Single(GetSongEvent);
                if (track.CallStackDepth == 0 && e.Ticks.Count > 0)
                {
                    break;
                }

                e.Ticks.Add(_player.ElapsedTicks);
                ExecuteNext(track, ref u);
                if (track.Stopped)
                {
                    break;
                }

                _player.ElapsedTicks += track.Rest;
                track.Rest = 0;
            }
            if (_player.ElapsedTicks > MaxTicks)
            {
                LongestTrack = trackIndex;
                MaxTicks = _player.ElapsedTicks;
            }
            track.StopAllChannels();
        }
    }
    internal void SetCurTick(long ticks)
    {
        bool u = false;
        while (true)
        {
            if (_player.ElapsedTicks == ticks)
            {
                goto finish;
            }
            while (_player.TempoStack >= 150)
            {
                _player.TempoStack -= 150;
                for (int trackIndex = 0; trackIndex < Tracks.Length; trackIndex++)
                {
                    MP2KTrack track = Tracks[trackIndex];
                    if (!track.Stopped)
                    {
                        track.Tick();
                        while (track.Rest == 0 && !track.Stopped)
                        {
                            ExecuteNext(track, ref u);
                        }
                    }
                }
                _player.ElapsedTicks++;
                if (_player.ElapsedTicks == ticks)
                {
                    goto finish;
                }
            }
            _player.TempoStack += _player.Tempo;
        }
    finish:
        for (int i = 0; i < Tracks.Length; i++)
        {
            Tracks[i].StopAllChannels();
        }
    }
}
