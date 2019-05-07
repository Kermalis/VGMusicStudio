using Kermalis.VGMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT
{
    internal class Player : IPlayer
    {
        private readonly short[] vars = new short[0x20]; // Unsure of the exact amount
        private readonly Track[] tracks = new Track[0x10];
        private readonly Mixer mixer;
        private readonly Config config;
        private readonly TimeBarrier time;
        private readonly Thread thread;
        private int randSeed;
        private Random rand;
        private SSEQ sseq;
        private SBNK sbnk;
        public byte Volume; // This is only set in LoadSong(). It should probably be set in Play() as well.
        private ushort tempo;
        private int tempoStack;
        private long loops;
        private bool fadeOutBegan;

        public List<SongEvent>[] Events { get; private set; }

        public PlayerState State { get; private set; }
        public event SongEndedEvent SongEnded;

        public Player(Mixer mixer, Config config)
        {
            for (byte i = 0; i < tracks.Length; i++)
            {
                tracks[i] = new Track(i, this);
            }
            this.mixer = mixer;
            this.config = config;

            time = new TimeBarrier(192);
            thread = new Thread(Tick) { Name = "SDATPlayer Tick" };
            thread.Start();
        }

        private void InitEmulation()
        {
            rand = new Random(randSeed);
            for (int i = 0; i < 0x10; i++)
            {
                tracks[i].Init();
            }
            for (int i = 0; i < vars.Length; i++)
            {
                vars[i] = short.MinValue; // Not sure what the initial value is, but it is not [0,15]
            }
        }
        private void SetTicks()
        {
            for (int i = 0; i < 0x10; i++)
            {
                if (Events[i] != null)
                {
                    Events[i] = Events[i].OrderBy(e => e.Offset).ToList();
                }
            }
            InitEmulation();
            SetTicks(0, 0, 0);
            // TODO: Tie means that we cannot know exactly how many ticks something will last (NSMB 83)
            // We can either stop counting ticks after a tie note or we can emulate the channel as well
            // TODO: Notes with 0 length have the same issue (MKDS 75)
            // TODO: (NSMB 81) (Spirit Tracks 18) does not count all ticks because the song keeps jumping backwards while changing vars and then using if mod
            // TODO: Rand will not be fully accurate until all events that have the possibility of being randomized are emulated here
            void SetTicks(int trackIndex, int startOffset, long startTicks)
            {
                Track track = tracks[trackIndex];
                List<SongEvent> evs = Events[trackIndex];
                long ticks = startTicks;
                bool cont = true;
                int i = evs.FindIndex(c => c.Offset == startOffset);
                while (cont)
                {
                    int ReadArg(ArgType type, int value)
                    {
                        switch (type)
                        {
                            case ArgType.Byte:
                            {
                                return (byte)value;
                            }
                            case ArgType.Short:
                            {
                                return (short)value;
                            }
                            case ArgType.VarLen:
                            {
                                return value;
                            }
                            case ArgType.Rand:
                            {
                                short min = (short)value;
                                short max = (short)((value >> 16) & 0xFFFF);
                                return rand.Next(min, max + 1);
                            }
                            case ArgType.PlayerVar:
                            {
                                return vars[value];
                            }
                            default: throw new Exception();
                        }
                    }
                    ArgType argOverrideType = ArgType.None;
                    bool doCmdWork = true;
                again:
                    SongEvent e = evs[i++];
                    e.Ticks.Add(ticks);
                    switch (e.Command)
                    {
                        case AllocTracksCommand alloc:
                        {
                            // Must be in the beginning of the first track to work
                            if (doCmdWork && trackIndex == 0 && e.Offset == 0)
                            {
                                for (int t = 0; t < 0x10; t++)
                                {
                                    if ((alloc.Tracks & (1 << t)) != 0)
                                    {
                                        tracks[t].Allocated = true;
                                    }
                                }
                            }
                            break;
                        }
                        case CallCommand call:
                        {
                            if (doCmdWork)
                            {
                                int callCmd = evs.FindIndex(c => c.Offset == call.Offset);
                                if (track.CallStackDepth < 3)
                                {
                                    track.CallStack[track.CallStackDepth] = i;
                                    track.CallStackDepth++;
                                    i = callCmd;
                                }
                            }
                            break;
                        }
                        case FinishCommand _:
                        {
                            if (doCmdWork)
                            {
                                cont = false;
                            }
                            break;
                        }
                        case LoopStartCommand loopStart:
                        {
                            byte arg = (byte)ReadArg(argOverrideType == ArgType.None ? ArgType.Byte : argOverrideType, loopStart.NumLoops);
                            if (doCmdWork && track.CallStackDepth < 3)
                            {
                                track.CallStack[track.CallStackDepth] = i;
                                track.CallStackLoops[track.CallStackDepth] = arg;
                                track.CallStackDepth++;
                            }
                            break;
                        }
                        case LoopEndCommand _:
                        {
                            if (doCmdWork && track.CallStackDepth != 0)
                            {
                                byte count = track.CallStackLoops[track.CallStackDepth - 1];
                                if (count == 0) // Break from permanent loop
                                {
                                    cont = false;
                                }
                                else
                                {
                                    count--;
                                    if (count == 0)
                                    {
                                        track.CallStackDepth--;
                                    }
                                    else
                                    {
                                        track.CallStackLoops[track.CallStackDepth - 1] = count;
                                        i = track.CallStack[track.CallStackDepth - 1];
                                    }
                                }
                            }
                            break;
                        }
                        case ModIfCommand _:
                        {
                            if (doCmdWork)
                            {
                                doCmdWork = track.VariableFlag;
                                goto again;
                            }
                            break;
                        }
                        case ModRandCommand _:
                        {
                            if (doCmdWork)
                            {
                                argOverrideType = ArgType.Rand;
                                goto again;
                            }
                            break;
                        }
                        case ModVarCommand _:
                        {
                            if (doCmdWork)
                            {
                                argOverrideType = ArgType.PlayerVar;
                                goto again;
                            }
                            break;
                        }
                        case MonophonyCommand mono:
                        {
                            int arg = ReadArg(argOverrideType == ArgType.None ? ArgType.Byte : argOverrideType, mono.Mono);
                            if (doCmdWork)
                            {
                                track.Mono = arg == 1;
                            }
                            break;
                        }
                        case NoteComand note:
                        {
                            int duration = ReadArg(argOverrideType == ArgType.None ? ArgType.VarLen : argOverrideType, note.Duration);
                            if (doCmdWork && track.Mono)
                            {
                                ticks += duration;
                            }
                            break;
                        }
                        case JumpCommand jump:
                        {
                            if (doCmdWork)
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
                            }
                            break;
                        }
                        case OpenTrackCommand open:
                        {
                            Track truck = tracks[open.Track];
                            if (doCmdWork && truck.Allocated && !truck.Enabled)
                            {
                                SetTicks(open.Track, open.Offset, ticks);
                            }
                            break;
                        }
                        case RestCommand rest:
                        {
                            int arg = ReadArg(argOverrideType == ArgType.None ? ArgType.VarLen : argOverrideType, rest.Rest);
                            if (doCmdWork)
                            {
                                ticks += arg;
                            }
                            break;
                        }
                        case ReturnCommand _:
                        {
                            if (doCmdWork)
                            {
                                if (track.CallStackDepth != 0)
                                {
                                    track.CallStackDepth--;
                                    i = track.CallStack[track.CallStackDepth];
                                }
                            }
                            break;
                        }
                        case VarSetCommand varSet:
                        {
                            short arg = (short)ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType, varSet.Argument);
                            if (doCmdWork)
                            {
                                vars[varSet.Variable] = arg;
                            }
                            break;
                        }
                        case VarAddCommand varAdd:
                        {
                            short arg = (short)ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType, varAdd.Argument);
                            if (doCmdWork)
                            {
                                vars[varAdd.Variable] += arg;
                            }
                            break;
                        }
                        case VarSubCommand varSub:
                        {
                            short arg = (short)ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType, varSub.Argument);
                            if (doCmdWork)
                            {
                                vars[varSub.Variable] -= arg;
                            }
                            break;
                        }
                        case VarMulCommand varMul:
                        {
                            short arg = (short)ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType, varMul.Argument);
                            if (doCmdWork)
                            {
                                vars[varMul.Variable] *= arg;
                            }
                            break;
                        }
                        case VarDivCommand varDiv:
                        {
                            short arg = (short)ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType, varDiv.Argument);
                            if (doCmdWork && arg != 0)
                            {
                                vars[varDiv.Variable] /= arg;
                            }
                            break;
                        }
                        case VarShiftCommand varShift:
                        {
                            short arg = (short)ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType, varShift.Argument);
                            if (doCmdWork)
                            {
                                vars[varShift.Variable] = arg < 0 ? (short)(vars[varShift.Variable] >> -arg) : (short)(vars[varShift.Variable] << arg);
                            }
                            break;
                        }
                        case VarRandCommand varRand:
                        {
                            short arg = (short)ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType, varRand.Argument);
                            if (doCmdWork)
                            {
                                bool negate = false;
                                if (arg < 0)
                                {
                                    negate = true;
                                    arg = (short)-arg;
                                }
                                short val = (short)rand.Next(arg + 1);
                                if (negate)
                                {
                                    val = (short)-val;
                                }
                                vars[varRand.Variable] = val;
                            }
                            break;
                        }
                        case VarCmpEECommand varCmpEE:
                        {
                            short arg = (short)ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType, varCmpEE.Argument);
                            if (doCmdWork)
                            {
                                track.VariableFlag = vars[varCmpEE.Variable] == arg;
                            }
                            break;
                        }
                        case VarCmpGECommand varCmpGE:
                        {
                            short arg = (short)ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType, varCmpGE.Argument);
                            if (doCmdWork)
                            {
                                track.VariableFlag = vars[varCmpGE.Variable] >= arg;
                            }
                            break;
                        }
                        case VarCmpGGCommand varCmpGG:
                        {
                            short arg = (short)ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType, varCmpGG.Argument);
                            if (doCmdWork)
                            {
                                track.VariableFlag = vars[varCmpGG.Variable] > arg;
                            }
                            break;
                        }
                        case VarCmpLECommand varCmpLE:
                        {
                            short arg = (short)ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType, varCmpLE.Argument);
                            if (doCmdWork)
                            {
                                track.VariableFlag = vars[varCmpLE.Variable] <= arg;
                            }
                            break;
                        }
                        case VarCmpLLCommand varCmpLL:
                        {
                            short arg = (short)ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType, varCmpLL.Argument);
                            if (doCmdWork)
                            {
                                track.VariableFlag = vars[varCmpLL.Variable] < arg;
                            }
                            break;
                        }
                        case VarCmpNECommand varCmpNE:
                        {
                            short arg = (short)ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType, varCmpNE.Argument);
                            if (doCmdWork)
                            {
                                track.VariableFlag = vars[varCmpNE.Variable] != arg;
                            }
                            break;
                        }
                    }
                }
            }
        }
        public void LoadSong(long index)
        {
            Stop();
            SDAT.INFO.SequenceInfo seqInfo = config.SDAT.INFOBlock.SequenceInfos.Entries[index];
            if (seqInfo == null)
            {
                sseq = null;
                sbnk = null;
                Events = null;
            }
            else
            {
                sseq = new SSEQ(config.SDAT.FATBlock.Entries[seqInfo.FileId].Data);
                SDAT.INFO.BankInfo bankInfo = config.SDAT.INFOBlock.BankInfos.Entries[seqInfo.Bank];
                sbnk = new SBNK(config.SDAT.FATBlock.Entries[bankInfo.FileId].Data);
                for (int i = 0; i < 4; i++)
                {
                    if (bankInfo.SWARs[i] != 0xFFFF)
                    {
                        sbnk.SWARs[i] = new SWAR(config.SDAT.FATBlock.Entries[config.SDAT.INFOBlock.WaveArchiveInfos.Entries[bankInfo.SWARs[i]].FileId].Data);
                    }
                }
                Volume = seqInfo.Volume;
                randSeed = new Random().Next();

                // RECURSION INCOMING
                Events = new List<SongEvent>[0x10];
                AddTrackEvents(0, 0);
                void AddTrackEvents(int i, int trackStartOffset)
                {
                    if (Events[i] == null)
                    {
                        Events[i] = new List<SongEvent>();
                    }
                    int callStackDepth = 0;
                    AddEvents(trackStartOffset);
                    bool EventExists(long offset)
                    {
                        return Events[i].Any(e => e.Offset == offset);
                    }
                    void AddEvents(int startOffset)
                    {
                        int dataOffset = startOffset;
                        int ReadArg(ArgType type)
                        {
                            switch (type)
                            {
                                case ArgType.Byte:
                                {
                                    return sseq.Data[dataOffset++];
                                }
                                case ArgType.Short:
                                {
                                    return sseq.Data[dataOffset++] | (sseq.Data[dataOffset++] << 8);
                                }
                                case ArgType.VarLen:
                                {
                                    int read = 0, val = 0;
                                    byte b;
                                    do
                                    {
                                        b = sseq.Data[dataOffset++];
                                        val = (val << 7) | (b & 0x7F);
                                        read++;
                                    }
                                    while (read < 4 && (b & 0x80) != 0);
                                    return val;
                                }
                                case ArgType.Rand:
                                {
                                    // Combine min and max into one int
                                    return sseq.Data[dataOffset++] | (sseq.Data[dataOffset++] << 8) | (sseq.Data[dataOffset++] << 16) | (sseq.Data[dataOffset++] << 24);
                                }
                                case ArgType.PlayerVar:
                                {
                                    // Return var index
                                    return sseq.Data[dataOffset++];
                                }
                                default: throw new Exception();
                            }
                        }
                        bool cont = true;
                        while (cont)
                        {
                            bool @if = false;
                            int offset = dataOffset;
                            ArgType argOverrideType = ArgType.None;
                        again:
                            byte cmd = sseq.Data[dataOffset++];
                            void AddEvent<T>(T command) where T : SDATCommand, ICommand
                            {
                                command.RandMod = argOverrideType == ArgType.Rand;
                                command.VarMod = argOverrideType == ArgType.PlayerVar;
                                Events[i].Add(new SongEvent(offset, command));
                            }
                            void Invalid()
                            {
                                throw new Exception($"Invalid command at 0x{offset:X4}: 0x{cmd:X}");
                            }

                            if (cmd <= 0x7F)
                            {
                                byte velocity = sseq.Data[dataOffset++];
                                int duration = ReadArg(argOverrideType == ArgType.None ? ArgType.VarLen : argOverrideType);
                                if (!EventExists(offset))
                                {
                                    AddEvent(new NoteComand { Key = cmd, Velocity = velocity, Duration = duration });
                                }
                            }
                            else
                            {
                                int cmdGroup = cmd & 0xF0;
                                if (cmdGroup == 0x80)
                                {
                                    int arg = ReadArg(argOverrideType == ArgType.None ? ArgType.VarLen : argOverrideType);
                                    switch (cmd)
                                    {
                                        case 0x80:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new RestCommand { Rest = arg });
                                            }
                                            break;
                                        }
                                        case 0x81: // RAND PROGRAM: [BW2 (2249)]
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new VoiceCommand { Voice = arg }); // TODO: Bank change
                                            }
                                            break;
                                        }
                                        default: Invalid(); break;
                                    }
                                }
                                else if (cmdGroup == 0x90)
                                {
                                    switch (cmd)
                                    {
                                        case 0x93:
                                        {
                                            if (i != 0)
                                            {
                                                throw new Exception($"Track {i} has a \"{nameof(OpenTrackCommand)}\".");
                                            }
                                            int trackIndex = sseq.Data[dataOffset++];
                                            int offset24bit = sseq.Data[dataOffset++] | (sseq.Data[dataOffset++] << 8) | (sseq.Data[dataOffset++] << 16);
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new OpenTrackCommand { Track = trackIndex, Offset = offset24bit });
                                                AddTrackEvents(trackIndex, offset24bit);
                                            }
                                            break;
                                        }
                                        case 0x94:
                                        {
                                            int offset24bit = sseq.Data[dataOffset++] | (sseq.Data[dataOffset++] << 8) | (sseq.Data[dataOffset++] << 16);
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new JumpCommand { Offset = offset24bit });
                                                if (!EventExists(offset24bit))
                                                {
                                                    AddEvents(offset24bit);
                                                }
                                            }
                                            if (!@if)
                                            {
                                                cont = false;
                                            }
                                            break;
                                        }
                                        case 0x95:
                                        {
                                            int offset24bit = sseq.Data[dataOffset++] | (sseq.Data[dataOffset++] << 8) | (sseq.Data[dataOffset++] << 16);
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new CallCommand { Offset = offset24bit });
                                            }
                                            if (callStackDepth < 3)
                                            {
                                                if (!EventExists(offset24bit))
                                                {
                                                    callStackDepth++;
                                                    AddEvents(offset24bit);
                                                }
                                            }
                                            else
                                            {
                                                throw new Exception($"Too many nested call events in track {i}.");
                                            }
                                            break;
                                        }
                                        default: Invalid(); break;
                                    }
                                }
                                else if (cmdGroup == 0xA0)
                                {
                                    switch (cmd)
                                    {
                                        case 0xA0: // [New Super Mario Bros (BGM_AMB_CHIKA)] [BW2 (1917, 1918)]
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new ModRandCommand());
                                            }
                                            argOverrideType = ArgType.Rand;
                                            offset++;
                                            goto again;
                                        }
                                        case 0xA1: // [New Super Mario Bros (BGM_AMB_SABAKU)]
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new ModVarCommand());
                                            }
                                            argOverrideType = ArgType.PlayerVar;
                                            offset++;
                                            goto again;
                                        }
                                        case 0xA2: // [Mario Kart DS (75)] [BW2 (1917, 1918)]
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new ModIfCommand());
                                            }
                                            @if = true;
                                            offset++;
                                            goto again;
                                        }
                                        default: Invalid(); break;
                                    }
                                }
                                else if (cmdGroup == 0xB0)
                                {
                                    byte varIndex = sseq.Data[dataOffset++];
                                    int arg = ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType);
                                    switch (cmd)
                                    {
                                        case 0xB0:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new VarSetCommand { Variable = varIndex, Argument = arg });
                                            }
                                            break;
                                        }
                                        case 0xB1:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new VarAddCommand { Variable = varIndex, Argument = arg });
                                            }
                                            break;
                                        }
                                        case 0xB2:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new VarSubCommand { Variable = varIndex, Argument = arg });
                                            }
                                            break;
                                        }
                                        case 0xB3:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new VarMulCommand { Variable = varIndex, Argument = arg });
                                            }
                                            break;
                                        }
                                        case 0xB4:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new VarDivCommand { Variable = varIndex, Argument = arg });
                                            }
                                            break;
                                        }
                                        case 0xB5:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new VarShiftCommand { Variable = varIndex, Argument = arg });
                                            }
                                            break;
                                        }
                                        case 0xB6: // [Mario Kart DS (75)]
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new VarRandCommand { Variable = varIndex, Argument = arg });
                                            }
                                            break;
                                        }
                                        case 0xB8:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new VarCmpEECommand { Variable = varIndex, Argument = arg });
                                            }
                                            break;
                                        }
                                        case 0xB9:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new VarCmpGECommand { Variable = varIndex, Argument = arg });
                                            }
                                            break;
                                        }
                                        case 0xBA:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new VarCmpGGCommand { Variable = varIndex, Argument = arg });
                                            }
                                            break;
                                        }
                                        case 0xBB:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new VarCmpLECommand { Variable = varIndex, Argument = arg });
                                            }
                                            break;
                                        }
                                        case 0xBC:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new VarCmpLLCommand { Variable = varIndex, Argument = arg });
                                            }
                                            break;
                                        }
                                        case 0xBD:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new VarCmpNECommand { Variable = varIndex, Argument = arg });
                                            }
                                            break;
                                        }
                                        default: Invalid(); break;
                                    }
                                }
                                else if (cmdGroup == 0xC0 || cmdGroup == 0xD0)
                                {
                                    int arg = ReadArg(argOverrideType == ArgType.None ? ArgType.Byte : argOverrideType);
                                    switch (cmd)
                                    {
                                        case 0xC0:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new PanpotCommand { Panpot = arg - (argOverrideType == ArgType.None ? 0x40 : 0) });
                                            }
                                            break;
                                        }
                                        case 0xC1:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new TrackVolumeCommand { Volume = arg });
                                            }
                                            break;
                                        }
                                        case 0xC2:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new PlayerVolumeCommand { Volume = arg });
                                            }
                                            break;
                                        }
                                        case 0xC3:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new TransposeCommand { Transpose = arg });
                                            }
                                            break;
                                        }
                                        case 0xC4:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new PitchBendCommand { Bend = arg });
                                            }
                                            break;
                                        }
                                        case 0xC5:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new PitchBendRangeCommand { Range = arg });
                                            }
                                            break;
                                        }
                                        case 0xC6:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new PriorityCommand { Priority = arg });
                                            }
                                            break;
                                        }
                                        case 0xC7:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new MonophonyCommand { Mono = arg });
                                            }
                                            break;
                                        }
                                        case 0xC8:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new TieCommand { Tie = arg });
                                            }
                                            break;
                                        }
                                        case 0xC9:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new PortamentoControlCommand { Portamento = arg });
                                            }
                                            break;
                                        }
                                        case 0xCA:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new LFODepthCommand { Depth = arg });
                                            }
                                            break;
                                        }
                                        case 0xCB:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new LFOSpeedCommand { Speed = arg });
                                            }
                                            break;
                                        }
                                        case 0xCC:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new LFOTypeCommand { Type = arg });
                                            }
                                            break;
                                        }
                                        case 0xCD:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new LFORangeCommand { Range = arg });
                                            }
                                            break;
                                        }
                                        case 0xCE:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new PortamentoToggleCommand { Portamento = arg });
                                            }
                                            break;
                                        }
                                        case 0xCF:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new PortamentoTimeCommand { Time = arg });
                                            }
                                            break;
                                        }
                                        case 0xD0:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new ForceAttackCommand { Attack = arg });
                                            }
                                            break;
                                        }
                                        case 0xD1:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new ForceDecayCommand { Decay = arg });
                                            }
                                            break;
                                        }
                                        case 0xD2:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new ForceSustainCommand { Sustain = arg });
                                            }
                                            break;
                                        }
                                        case 0xD3:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new ForceReleaseCommand { Release = arg });
                                            }
                                            break;
                                        }
                                        case 0xD4:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new LoopStartCommand { NumLoops = arg });
                                            }
                                            break;
                                        }
                                        case 0xD5:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new TrackExpressionCommand { Expression = arg });
                                            }
                                            break;
                                        }
                                        case 0xD6:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new VarPrintCommand { Variable = arg });
                                            }
                                            break;
                                        }
                                        default: Invalid(); break;
                                    }
                                }
                                else if (cmdGroup == 0xE0)
                                {
                                    int arg = ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType);
                                    switch (cmd)
                                    {
                                        case 0xE0:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new LFODelayCommand { Delay = arg });
                                            }
                                            break;
                                        }
                                        case 0xE1:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new TempoCommand { Tempo = arg });
                                            }
                                            break;
                                        }
                                        case 0xE3:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new SweepPitchCommand { Pitch = arg });
                                            }
                                            break;
                                        }
                                        default: Invalid(); break;
                                    }
                                }
                                else // if (cmdGroup == 0xF0)
                                {
                                    switch (cmd)
                                    {
                                        case 0xFC: // [HGSS(1353)]
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new LoopEndCommand());
                                            }
                                            break;
                                        }
                                        case 0xFD:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new ReturnCommand());
                                            }
                                            if (!@if && callStackDepth != 0)
                                            {
                                                cont = false;
                                                callStackDepth--;
                                            }
                                            break;
                                        }
                                        case 0xFE:
                                        {
                                            ushort bits = (ushort)ReadArg(ArgType.Short);
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new AllocTracksCommand { Tracks = bits });
                                            }
                                            if (i != 0)
                                            {
                                                throw new Exception($"Track {i} has a \"{nameof(AllocTracksCommand)}\".");
                                            }
                                            break;
                                        }
                                        case 0xFF:
                                        {
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new FinishCommand());
                                            }
                                            if (!@if)
                                            {
                                                cont = false;
                                            }
                                            break;
                                        }
                                        default: Invalid(); break;
                                    }
                                }
                            }
                        }
                    }
                }
                SetTicks();
            }
        }
        public void Play()
        {
            Stop();
            if (sseq != null)
            {
                tempo = 120; // Confirmed: default tempo is 120 (MKDS 75)
                tempoStack = 0;
                loops = 0;
                fadeOutBegan = false;
                InitEmulation();
                State = PlayerState.Playing;
            }
            else
            {
                SongEnded?.Invoke();
            }
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
            for (int i = 0; i < 0x10; i++)
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
            for (int i = 0; i < 0x10; i++)
            {
                Track track = tracks[i];
                if (track.Enabled)
                {
                    info.Positions[i] = track.DataOffset;
                    info.Delays[i] = track.Delay;
                    info.Voices[i] = track.Voice;
                    info.Mods[i] = track.LFODepth * track.LFORange;
                    info.Types[i] = sbnk.NumInstruments <= track.Voice ? "???" : sbnk.Instruments[track.Voice].Type.ToString();
                    info.Volumes[i] = track.Volume;
                    info.PitchBends[i] = track.GetPitch();
                    info.Extras[i] = track.Portamento ? track.PortamentoTime : (byte)0;
                    info.Panpots[i] = track.GetPan();

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
                            Channel c = channels[j];
                            lefts[j] = (float)(-c.Pan + 0x40) / 0x80 * c.Volume / 0x7F;
                            rights[j] = (float)(c.Pan + 0x40) / 0x80 * c.Volume / 0x7F;
                        }
                        info.Notes[i] = channels.Where(c => c.State != EnvelopeState.Release).Select(c => c.Key).ToArray();
                        info.Lefts[i] = lefts.Max();
                        info.Rights[i] = rights.Max();
                    }
                }
            }
        }

        public void PlayNote(Track track, byte key, byte velocity, int duration)
        {
            Channel channel = null;
            if (track.Tie && track.Channels.Count != 0)
            {
                channel = track.Channels.Last();
                channel.Key = key;
                channel.NoteVelocity = velocity;
            }
            else
            {
                SBNK.InstrumentData inst = sbnk.GetInstrumentData(track.Voice, key);
                if (inst != null)
                {
                    channel = mixer.AllocateChannel(inst.Type, track);
                    if (channel != null)
                    {
                        if (track.Tie)
                        {
                            duration = -1;
                        }
                        int release = inst.Param.Release;
                        if (release == 0xFF)
                        {
                            duration = -1;
                            release = 0;
                        }
                        bool started = false;
                        switch (inst.Type)
                        {
                            case InstrumentType.PCM:
                            {
                                SWAR.SWAV swav = sbnk.GetSWAV(inst.Param.Info[1], inst.Param.Info[0]);
                                if (swav != null)
                                {
                                    channel.StartPCM(swav, duration);
                                    started = true;
                                }
                                break;
                            }
                            case InstrumentType.PSG:
                            {
                                channel.StartPSG((byte)inst.Param.Info[0], duration);
                                started = true;
                                break;
                            }
                            case InstrumentType.Noise:
                            {
                                channel.StartNoise(duration);
                                started = true;
                                break;
                            }
                        }
                        channel.Stop();
                        if (started)
                        {
                            channel.Key = key;
                            channel.BaseKey = inst.Param.BaseKey;
                            channel.NoteVelocity = velocity;
                            channel.SetAttack(inst.Param.Attack);
                            channel.SetDecay(inst.Param.Decay);
                            channel.SetSustain(inst.Param.Sustain);
                            channel.SetRelease(release);
                            channel.StartingPan = (sbyte)(inst.Param.Pan - 0x40);
                            channel.Owner = track;
                            track.Channels.Add(channel);
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }
            if (channel != null)
            {
                if (track.Attack != 0xFF)
                {
                    channel.SetAttack(track.Attack);
                }
                if (track.Decay != 0xFF)
                {
                    channel.SetDecay(track.Decay);
                }
                if (track.Sustain != 0xFF)
                {
                    channel.SetSustain(track.Sustain);
                }
                if (track.Release != 0xFF)
                {
                    channel.SetRelease(track.Release);
                }
                channel.SweepPitch = track.SweepPitch;
                if (track.Portamento)
                {
                    channel.SweepPitch += (short)((track.PortamentoKey - key) << 6); // "<< 6" is "* 0x40"
                }
                if (track.PortamentoTime != 0)
                {
                    channel.SweepLength = (track.PortamentoTime * track.PortamentoTime * Math.Abs(channel.SweepPitch)) >> 11; // ">> 11" is "/ 0x800"
                    channel.AutoSweep = true;
                }
                else
                {
                    channel.SweepLength = duration;
                    channel.AutoSweep = false;
                }
                channel.SweepCounter = 0;
            }
        }
        private void ExecuteNext(Track track, ref bool loop)
        {
            int ReadArg(ArgType type)
            {
                switch (type)
                {
                    case ArgType.Byte: // GetByte
                    {
                        return sseq.Data[track.DataOffset++];
                    }
                    case ArgType.Short: // GetShort
                    {
                        return sseq.Data[track.DataOffset++] | (sseq.Data[track.DataOffset++] << 8);
                    }
                    case ArgType.VarLen: // GetVariableLengthInt
                    {
                        int read = 0, val = 0;
                        byte b;
                        do
                        {
                            b = sseq.Data[track.DataOffset++];
                            val = (val << 7) | (b & 0x7F);
                            read++;
                        }
                        while (read < 4 && (b & 0x80) != 0);
                        return val;
                    }
                    case ArgType.Rand: // GetRandomShort
                    {
                        short min = (short)(sseq.Data[track.DataOffset++] | (sseq.Data[track.DataOffset++] << 8));
                        short max = (short)(sseq.Data[track.DataOffset++] | (sseq.Data[track.DataOffset++] << 8));
                        return rand.Next(min, max + 1);
                    }
                    case ArgType.PlayerVar: // GetPlayerVarShort
                    {
                        byte varIndex = sseq.Data[track.DataOffset++];
                        return vars[varIndex];
                    }
                    default: throw new Exception();
                }
            }
            ArgType argOverrideType = ArgType.None;
            bool doCmdWork = true;
        again:
            byte cmd = sseq.Data[track.DataOffset++];

            if (cmd < 0x80) // Notes
            {
                byte velocity = sseq.Data[track.DataOffset++];
                int duration = ReadArg(argOverrideType == ArgType.None ? ArgType.VarLen : argOverrideType);
                if (doCmdWork)
                {
                    byte key = (byte)(cmd + track.KeyShift).Clamp(0x0, 0x7F);
                    PlayNote(track, key, velocity, Math.Max(-1, duration));
                    track.PortamentoKey = key;
                    if (track.Mono)
                    {
                        track.Delay = duration;
                        if (duration == 0)
                        {
                            track.WaitingForNoteToFinishBeforeContinuingXD = true;
                        }
                    }
                }
            }
            else
            {
                int cmdGroup = cmd & 0xF0;
                if (cmdGroup == 0x80)
                {
                    int arg = ReadArg(argOverrideType == ArgType.None ? ArgType.VarLen : argOverrideType);
                    if (doCmdWork)
                    {
                        switch (cmd)
                        {
                            case 0x80:
                            {
                                track.Delay = arg;
                                break;
                            }
                            case 0x81:
                            {
                                track.Voice = (byte)arg;
                                break;
                            }
                        }
                    }
                }
                else if (cmdGroup == 0x90)
                {
                    switch (cmd)
                    {
                        case 0x93: // Open Track
                        {
                            int index = sseq.Data[track.DataOffset++];
                            int offset24bit = sseq.Data[track.DataOffset++] | (sseq.Data[track.DataOffset++] << 8) | (sseq.Data[track.DataOffset++] << 16);
                            Track truck = tracks[index];
                            if (doCmdWork && truck.Allocated && !truck.Enabled)
                            {
                                truck.Enabled = true;
                                truck.DataOffset = offset24bit;
                            }
                            break;
                        }
                        case 0x94: // Jump
                        {
                            int offset24bit = sseq.Data[track.DataOffset++] | (sseq.Data[track.DataOffset++] << 8) | (sseq.Data[track.DataOffset++] << 16);
                            if (doCmdWork)
                            {
                                track.DataOffset = offset24bit;
                                loop = true; // TODO: Check context of the jump (were we in this tick before?)
                            }
                            break;
                        }
                        case 0x95: // Call
                        {
                            int offset24bit = sseq.Data[track.DataOffset++] | (sseq.Data[track.DataOffset++] << 8) | (sseq.Data[track.DataOffset++] << 16);
                            if (doCmdWork && track.CallStackDepth < 3)
                            {
                                track.CallStack[track.CallStackDepth] = track.DataOffset;
                                track.CallStackDepth++;
                                track.DataOffset = offset24bit;
                            }
                            break;
                        }
                    }
                }
                else if (cmdGroup == 0xA0)
                {
                    if (doCmdWork)
                    {
                        switch (cmd)
                        {
                            case 0xA0:
                            {
                                argOverrideType = ArgType.Rand;
                                goto again;
                            }
                            case 0xA1:
                            {
                                argOverrideType = ArgType.PlayerVar;
                                goto again;
                            }
                            case 0xA2:
                            {
                                doCmdWork = track.VariableFlag;
                                goto again;
                            }
                        }
                    }
                }
                else if (cmdGroup == 0xB0)
                {
                    byte varIndex = sseq.Data[track.DataOffset++];
                    short arg = (short)ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType);
                    if (doCmdWork)
                    {
                        switch (cmd)
                        {
                            case 0xB0:
                            {
                                vars[varIndex] = arg;
                                break;
                            }
                            case 0xB1:
                            {
                                vars[varIndex] += arg;
                                break;
                            }
                            case 0xB2:
                            {
                                vars[varIndex] -= arg;
                                break;
                            }
                            case 0xB3:
                            {
                                vars[varIndex] *= arg;
                                break;
                            }
                            case 0xB4:
                            {
                                if (arg != 0)
                                {
                                    vars[varIndex] /= arg;
                                }
                                break;
                            }
                            case 0xB5:
                            {
                                vars[varIndex] = arg < 0 ? (short)(vars[varIndex] >> -arg) : (short)(vars[varIndex] << arg);
                                break;
                            }
                            case 0xB6:
                            {
                                bool negate = false;
                                if (arg < 0)
                                {
                                    negate = true;
                                    arg = (short)-arg;
                                }
                                short val = (short)rand.Next(arg + 1);
                                if (negate)
                                {
                                    val = (short)-val;
                                }
                                vars[varIndex] = val;
                                break;
                            }
                            case 0xB8:
                            {
                                track.VariableFlag = vars[varIndex] == arg;
                                break;
                            }
                            case 0xB9:
                            {
                                track.VariableFlag = vars[varIndex] >= arg;
                                break;
                            }
                            case 0xBA:
                            {
                                track.VariableFlag = vars[varIndex] > arg;
                                break;
                            }
                            case 0xBB:
                            {
                                track.VariableFlag = vars[varIndex] <= arg;
                                break;
                            }
                            case 0xBC:
                            {
                                track.VariableFlag = vars[varIndex] < arg;
                                break;
                            }
                            case 0xBD:
                            {
                                track.VariableFlag = vars[varIndex] != arg;
                                break;
                            }
                        }
                    }
                }
                else if (cmdGroup == 0xC0 || cmdGroup == 0xD0)
                {
                    int arg = ReadArg(argOverrideType == ArgType.None ? ArgType.Byte : argOverrideType);
                    if (doCmdWork)
                    {
                        switch (cmd)
                        {
                            case 0xC0: // Panpot
                            {
                                track.Pan = (sbyte)(arg - 0x40);
                                break;
                            }
                            case 0xC1: // Volume
                            {
                                track.Volume = (byte)arg;
                                break;
                            }
                            case 0xC2: // Player Volume
                            {
                                Volume = (byte)arg;
                                break;
                            }
                            case 0xC3: // Key Shift
                            {
                                track.KeyShift = (sbyte)arg;
                                break;
                            }
                            case 0xC4: // Pitch Bend
                            {
                                track.Bend = (sbyte)arg;
                                break;
                            }
                            case 0xC5: // Pitch Bend Range
                            {
                                track.BendRange = (byte)arg;
                                break;
                            }
                            case 0xC6: // Priority
                            {
                                track.Priority = (byte)arg;
                                break;
                            }
                            case 0xC7: // Mono/Poly
                            {
                                track.Mono = arg == 1;
                                break;
                            }
                            case 0xC8: // Tie
                            {
                                track.Tie = arg == 1;
                                track.StopAllChannels();
                                break;
                            }
                            case 0xC9: // Portamento Control
                            {
                                track.PortamentoKey = (byte)(arg + track.KeyShift);
                                track.Portamento = true;
                                break;
                            }
                            case 0xCA: // LFO Depth
                            {
                                track.LFODepth = (byte)arg;
                                break;
                            }
                            case 0xCB: // LFO Speed
                            {
                                track.LFOSpeed = (byte)arg;
                                break;
                            }
                            case 0xCC: // LFO Type
                            {
                                track.LFOType = (LFOType)arg;
                                break;
                            }
                            case 0xCD: // LFO Range
                            {
                                track.LFORange = (byte)arg;
                                break;
                            }
                            case 0xCE: // Portamento Toggle
                            {
                                track.Portamento = arg == 1;
                                break;
                            }
                            case 0xCF: // Portamento Time
                            {
                                track.PortamentoTime = (byte)arg;
                                break;
                            }
                            case 0xD0: // Forced Attack
                            {
                                track.Attack = (byte)arg;
                                break;
                            }
                            case 0xD1: // Forced Decay
                            {
                                track.Decay = (byte)arg;
                                break;
                            }
                            case 0xD2: // Forced Sustain
                            {
                                track.Sustain = (byte)arg;
                                break;
                            }
                            case 0xD3: // Forced Release
                            {
                                track.Release = (byte)arg;
                                break;
                            }
                            case 0xD4: // Loop Start
                            {
                                if (track.CallStackDepth < 3)
                                {
                                    track.CallStack[track.CallStackDepth] = track.DataOffset;
                                    track.CallStackLoops[track.CallStackDepth] = (byte)arg;
                                    track.CallStackDepth++;
                                }
                                break;
                            }
                            case 0xD5: // Expression
                            {
                                track.Expression = (byte)arg;
                                break;
                            }
                            case 0xD6: // Print
                            {
                                Console.WriteLine("Track {0}, Var {1}, Value{2}", track.Index, arg, vars[arg]);
                                break;
                            }
                        }
                    }
                }
                else if (cmdGroup == 0xE0)
                {
                    int arg = ReadArg(argOverrideType == ArgType.None ? ArgType.Short : argOverrideType);
                    if (doCmdWork)
                    {
                        switch (cmd)
                        {
                            case 0xE0: // LFO Delay
                            {
                                track.LFODelay = (ushort)arg;
                                break;
                            }
                            case 0xE1: // Tempo
                            {
                                tempo = (ushort)arg;
                                break;
                            }
                            case 0xE3: // Sweep Pitch
                            {
                                track.SweepPitch = (short)arg;
                                break;
                            }
                        }
                    }
                }
                else // if (cmdGroup == 0xF0)
                {
                    switch (cmd)
                    {
                        case 0xFC: // Loop End
                        {
                            if (doCmdWork && track.CallStackDepth != 0)
                            {
                                byte count = track.CallStackLoops[track.CallStackDepth - 1];
                                if (count != 0)
                                {
                                    count--;
                                    if (count == 0)
                                    {
                                        track.CallStackDepth--;
                                        break;
                                    }
                                }
                                track.CallStackLoops[track.CallStackDepth - 1] = count;
                                track.DataOffset = track.CallStack[track.CallStackDepth - 1];
                            }
                            break;
                        }
                        case 0xFD: // Return
                        {
                            if (doCmdWork && track.CallStackDepth != 0)
                            {
                                track.CallStackDepth--;
                                track.DataOffset = track.CallStack[track.CallStackDepth];
                            }
                            break;
                        }
                        case 0xFE: // Alloc Tracks
                        {
                            int trackBits = sseq.Data[track.DataOffset++] | (sseq.Data[track.DataOffset++] << 8);
                            // Must be in the beginning of the first track to work. We can tell it's the beginning by checking the offset after reading cmd and param
                            if (doCmdWork && track.Index == 0 && track.DataOffset == 3)
                            {
                                for (int i = 0; i < 0x10; i++)
                                {
                                    if ((trackBits & (1 << i)) != 0)
                                    {
                                        tracks[i].Allocated = true;
                                    }
                                }
                            }
                            break;
                        }
                        case 0xFF: // End
                        {
                            if (doCmdWork)
                            {
                                track.Stopped = true;
                            }
                            break;
                        }
                    }
                }
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
                    while (tempoStack >= 240)
                    {
                        tempoStack -= 240;
                        bool allDone = true, loop = false;
                        for (int i = 0; i < 0x10; i++)
                        {
                            Track track = tracks[i];
                            if (track.Enabled)
                            {
                                track.Tick();
                                while (track.Delay == 0 && !track.WaitingForNoteToFinishBeforeContinuingXD && !track.Stopped)
                                {
                                    ExecuteNext(track, ref loop);
                                }
                                if (!track.Stopped || track.Channels.Count != 0)
                                {
                                    allDone = false;
                                }
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
                    mixer.ChannelTick();
                    mixer.Process();
                }
                time.Wait();
            }
            time.Stop();
        }
    }
}
