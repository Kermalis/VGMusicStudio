using Kermalis.VGMusicStudio.Properties;
using Kermalis.VGMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT
{
    internal class Player : IPlayer
    {
        private readonly short[] _vars = new short[0x20]; // 16 player variables, then 16 global variables
        private readonly Track[] _tracks = new Track[0x10];
        private readonly Mixer _mixer;
        private readonly Config _config;
        private readonly TimeBarrier _time;
        private Thread _thread;
        private int _randSeed;
        private Random _rand;
        private SDAT.INFO.SequenceInfo _seqInfo;
        private SSEQ _sseq;
        private SBNK _sbnk;
        public byte Volume;
        private ushort _tempo;
        private int _tempoStack;
        private long _elapsedLoops;
        private bool _fadeOutBegan;

        public List<SongEvent>[] Events { get; private set; }
        public long MaxTicks { get; private set; }
        public long ElapsedTicks { get; private set; }
        public bool ShouldFadeOut { get; set; }
        public long NumLoops { get; set; }
        private int _longestTrack;

        public PlayerState State { get; private set; }
        public event SongEndedEvent SongEnded;

        public Player(Mixer mixer, Config config)
        {
            for (byte i = 0; i < 0x10; i++)
            {
                _tracks[i] = new Track(i, this);
            }
            _mixer = mixer;
            _config = config;

            _time = new TimeBarrier(192);
        }
        private void CreateThread()
        {
            _thread = new Thread(Tick) { Name = "SDAT Player Tick" };
            _thread.Start();
        }
        private void WaitThread()
        {
            if (_thread != null && (_thread.ThreadState == ThreadState.Running || _thread.ThreadState == ThreadState.WaitSleepJoin))
            {
                _thread.Join();
            }
        }

        private void InitEmulation()
        {
            _tempo = 120; // Confirmed: default tempo is 120 (MKDS 75)
            _tempoStack = 0;
            _elapsedLoops = ElapsedTicks = 0;
            _fadeOutBegan = false;
            Volume = _seqInfo.Volume;
            _rand = new Random(_randSeed);
            for (int i = 0; i < 0x10; i++)
            {
                _tracks[i].Init();
            }
            // Initialize player and global variables. Global variables should not have an effect in this program.
            for (int i = 0; i < 0x20; i++)
            {
                _vars[i] = i % 8 == 0 ? short.MaxValue : (short)0;
            }
        }
        private void SetTicks()
        {
            // TODO: (NSMB 81) (Spirit Tracks 18) does not count all ticks because the songs keep jumping backwards while changing vars and then using ModIfCommand to change events
            MaxTicks = 0;
            for (int i = 0; i < 0x10; i++)
            {
                if (Events[i] != null)
                {
                    Events[i] = Events[i].OrderBy(e => e.Offset).ToList();
                }
            }
            InitEmulation();
            bool[] done = new bool[0x10]; // We use this instead of track.Stopped just to be certain that emulating Monophony works as intended
            while (_tracks.Any(t => t.Allocated && t.Enabled && !done[t.Index]))
            {
                while (_tempoStack >= 240)
                {
                    _tempoStack -= 240;
                    for (int i = 0; i < 0x10; i++)
                    {
                        Track track = _tracks[i];
                        List<SongEvent> ev = Events[i];
                        if (track.Enabled && !track.Stopped)
                        {
                            track.Tick();
                            while (track.Rest == 0 && !track.WaitingForNoteToFinishBeforeContinuingXD && !track.Stopped)
                            {
                                int e = track.CurEvent;
                                ExecuteNext(i);
                                if (!done[i])
                                {
                                    ev[e].Ticks.Add(ElapsedTicks);
                                    if (track.Stopped || (track.CallStackDepth == 0 && ev[track.CurEvent].Ticks.Count > 0))
                                    {
                                        done[i] = true;
                                        if (ElapsedTicks > MaxTicks)
                                        {
                                            _longestTrack = i;
                                            MaxTicks = ElapsedTicks;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    ElapsedTicks++;
                }
                _tempoStack += _tempo;
                _mixer.ChannelTick();
                _mixer.EmulateProcess();
            }
            for (int i = 0; i < 0x10; i++)
            {
                _tracks[i].StopAllChannels();
            }
        }
        public void LoadSong(long index)
        {
            Stop();
            SDAT.INFO.SequenceInfo oldSeqInfo = _seqInfo;
            _seqInfo = _config.SDAT.INFOBlock.SequenceInfos.Entries[index];
            if (_seqInfo == null)
            {
                _sseq = null;
                _sbnk = null;
                Events = null;
            }
            else
            {
                if (oldSeqInfo == null || _seqInfo.Bank != oldSeqInfo.Bank)
                {
                    _voiceTypeCache = new string[byte.MaxValue + 1];
                }
                _sseq = new SSEQ(_config.SDAT.FATBlock.Entries[_seqInfo.FileId].Data);
                SDAT.INFO.BankInfo bankInfo = _config.SDAT.INFOBlock.BankInfos.Entries[_seqInfo.Bank];
                _sbnk = new SBNK(_config.SDAT.FATBlock.Entries[bankInfo.FileId].Data);
                for (int i = 0; i < 4; i++)
                {
                    if (bankInfo.SWARs[i] != 0xFFFF)
                    {
                        _sbnk.SWARs[i] = new SWAR(_config.SDAT.FATBlock.Entries[_config.SDAT.INFOBlock.WaveArchiveInfos.Entries[bankInfo.SWARs[i]].FileId].Data);
                    }
                }
                _randSeed = new Random().Next();

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
                                    return _sseq.Data[dataOffset++];
                                }
                                case ArgType.Short:
                                {
                                    return _sseq.Data[dataOffset++] | (_sseq.Data[dataOffset++] << 8);
                                }
                                case ArgType.VarLen:
                                {
                                    int read = 0, val = 0;
                                    byte b;
                                    do
                                    {
                                        b = _sseq.Data[dataOffset++];
                                        val = (val << 7) | (b & 0x7F);
                                        read++;
                                    }
                                    while (read < 4 && (b & 0x80) != 0);
                                    return val;
                                }
                                case ArgType.Rand:
                                {
                                    // Combine min and max into one int
                                    return _sseq.Data[dataOffset++] | (_sseq.Data[dataOffset++] << 8) | (_sseq.Data[dataOffset++] << 16) | (_sseq.Data[dataOffset++] << 24);
                                }
                                case ArgType.PlayerVar:
                                {
                                    // Return var index
                                    return _sseq.Data[dataOffset++];
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
                            byte cmd = _sseq.Data[dataOffset++];
                            void AddEvent<T>(T command) where T : SDATCommand, ICommand
                            {
                                command.RandMod = argOverrideType == ArgType.Rand;
                                command.VarMod = argOverrideType == ArgType.PlayerVar;
                                Events[i].Add(new SongEvent(offset, command));
                            }
                            void Invalid()
                            {
                                throw new Exception(string.Format(Strings.ErrorAlphaDreamDSEMP2KSDATInvalidCommand, i, offset, cmd));
                            }

                            if (cmd <= 0x7F)
                            {
                                byte velocity = _sseq.Data[dataOffset++];
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
                                            int trackIndex = _sseq.Data[dataOffset++];
                                            int offset24bit = _sseq.Data[dataOffset++] | (_sseq.Data[dataOffset++] << 8) | (_sseq.Data[dataOffset++] << 16);
                                            if (!EventExists(offset))
                                            {
                                                AddEvent(new OpenTrackCommand { Track = trackIndex, Offset = offset24bit });
                                                AddTrackEvents(trackIndex, offset24bit);
                                            }
                                            break;
                                        }
                                        case 0x94:
                                        {
                                            int offset24bit = _sseq.Data[dataOffset++] | (_sseq.Data[dataOffset++] << 8) | (_sseq.Data[dataOffset++] << 16);
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
                                            int offset24bit = _sseq.Data[dataOffset++] | (_sseq.Data[dataOffset++] << 8) | (_sseq.Data[dataOffset++] << 16);
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
                                                throw new Exception(string.Format(Strings.ErrorMP2KSDATNestedCalls, i));
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
                                    byte varIndex = _sseq.Data[dataOffset++];
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
                                                AddEvent(new PanpotCommand { Panpot = arg });
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
        public void SetCurrentPosition(long ticks)
        {
            if (_seqInfo == null)
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
                while (true)
                {
                    if (ElapsedTicks == ticks)
                    {
                        goto finish;
                    }
                    else
                    {
                        while (_tempoStack >= 240)
                        {
                            _tempoStack -= 240;
                            for (int i = 0; i < 0x10; i++)
                            {
                                Track track = _tracks[i];
                                if (track.Enabled && !track.Stopped)
                                {
                                    track.Tick();
                                    while (track.Rest == 0 && !track.WaitingForNoteToFinishBeforeContinuingXD && !track.Stopped)
                                    {
                                        ExecuteNext(i);
                                    }
                                }
                            }
                            ElapsedTicks++;
                            if (ElapsedTicks == ticks)
                            {
                                goto finish;
                            }
                        }
                        _tempoStack += _tempo;
                        _mixer.ChannelTick();
                        _mixer.EmulateProcess();
                    }
                }
            finish:
                for (int i = 0; i < 0x10; i++)
                {
                    _tracks[i].StopAllChannels();
                }
                Pause();
            }
        }
        public void Play()
        {
            if (_seqInfo == null)
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
            _mixer.CreateWaveWriter(fileName);
            InitEmulation();
            State = PlayerState.Recording;
            CreateThread();
            WaitThread();
            _mixer.CloseWaveWriter();
        }
        public void Dispose()
        {
            if (State == PlayerState.Playing || State == PlayerState.Paused || State == PlayerState.Stopped)
            {
                State = PlayerState.ShutDown;
                WaitThread();
            }
        }
        private string[] _voiceTypeCache;
        public void GetSongState(UI.SongInfoControl.SongInfo info)
        {
            info.Tempo = _tempo;
            for (int i = 0; i < 0x10; i++)
            {
                Track track = _tracks[i];
                if (track.Enabled)
                {
                    UI.SongInfoControl.SongInfo.Track tin = info.Tracks[i];
                    tin.Position = Events[i][track.CurEvent].Offset;
                    tin.Rest = track.Rest;
                    tin.Voice = track.Voice;
                    tin.LFO = track.LFODepth * track.LFORange;
                    if (_voiceTypeCache[track.Voice] == null)
                    {
                        if (_sbnk.NumInstruments <= track.Voice)
                        {
                            _voiceTypeCache[track.Voice] = "Empty";
                        }
                        else
                        {
                            InstrumentType t = _sbnk.Instruments[track.Voice].Type;
                            switch (t)
                            {
                                case InstrumentType.PCM: _voiceTypeCache[track.Voice] = "PCM"; break;
                                case InstrumentType.PSG: _voiceTypeCache[track.Voice] = "PSG"; break;
                                case InstrumentType.Noise: _voiceTypeCache[track.Voice] = "Noise"; break;
                                case InstrumentType.Drum: _voiceTypeCache[track.Voice] = "Drum"; break;
                                case InstrumentType.KeySplit: _voiceTypeCache[track.Voice] = "Key Split"; break;
                                default: _voiceTypeCache[track.Voice] = string.Format("Invalid {0}", (byte)t); break;
                            }
                        }
                    }
                    tin.Type = _voiceTypeCache[track.Voice];
                    tin.Volume = track.Volume;
                    tin.PitchBend = track.GetPitch();
                    tin.Extra = track.Portamento ? track.PortamentoTime : (byte)0;
                    tin.Panpot = track.GetPan();

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
                            if (c.State != EnvelopeState.Release)
                            {
                                tin.Keys[numKeys++] = c.Key;
                            }
                            float a = (float)(-c.Pan + 0x40) / 0x80 * c.Volume / 0x7F;
                            if (a > left)
                            {
                                left = a;
                            }
                            a = (float)(c.Pan + 0x40) / 0x80 * c.Volume / 0x7F;
                            if (a > right)
                            {
                                right = a;
                            }
                        }
                        tin.Keys[numKeys] = byte.MaxValue; // There's no way for numKeys to be after the last index in the array
                        tin.LeftVolume = left;
                        tin.RightVolume = right;
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
                SBNK.InstrumentData inst = _sbnk.GetInstrumentData(track.Voice, key);
                if (inst != null)
                {
                    InstrumentType type = inst.Type;
                    channel = _mixer.AllocateChannel(type, track);
                    if (channel != null)
                    {
                        if (track.Tie)
                        {
                            duration = -1;
                        }
                        SBNK.InstrumentData.DataParam param = inst.Param;
                        byte release = param.Release;
                        if (release == 0xFF)
                        {
                            duration = -1;
                            release = 0;
                        }
                        bool started = false;
                        switch (type)
                        {
                            case InstrumentType.PCM:
                            {
                                ushort[] info = param.Info;
                                SWAR.SWAV swav = _sbnk.GetSWAV(info[1], info[0]);
                                if (swav != null)
                                {
                                    channel.StartPCM(swav, duration);
                                    started = true;
                                }
                                break;
                            }
                            case InstrumentType.PSG:
                            {
                                channel.StartPSG((byte)param.Info[0], duration);
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
                            byte baseKey = param.BaseKey;
                            channel.BaseKey = type != InstrumentType.PCM && baseKey == 0x7F ? (byte)60 : baseKey;
                            channel.NoteVelocity = velocity;
                            channel.SetAttack(param.Attack);
                            channel.SetDecay(param.Decay);
                            channel.SetSustain(param.Sustain);
                            channel.SetRelease(release);
                            channel.StartingPan = (sbyte)(param.Pan - 0x40);
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
        private void ExecuteNext(int trackIndex)
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
                        return _rand.Next(min, max + 1);
                    }
                    case ArgType.PlayerVar:
                    {
                        return _vars[value];
                    }
                    default: throw new Exception();
                }
            }
            List<SongEvent> ev = Events[trackIndex];
            Track track = _tracks[trackIndex];
            bool increment = true, resetOverride = true, resetCmdWork = true;
            switch (ev[track.CurEvent].Command)
            {
                case AllocTracksCommand alloc:
                {
                    // Must be in the beginning of the first track to work
                    if (track.DoCommandWork && track.Index == 0 && track.CurEvent == 0)
                    {
                        for (int i = 0; i < 0x10; i++)
                        {
                            if ((alloc.Tracks & (1 << i)) != 0)
                            {
                                _tracks[i].Allocated = true;
                            }
                        }
                    }
                    break;
                }
                case CallCommand call:
                {
                    if (track.DoCommandWork && track.CallStackDepth < 3)
                    {
                        track.CallStack[track.CallStackDepth] = track.CurEvent + 1;
                        track.CallStackDepth++;
                        track.CurEvent = ev.FindIndex(c => c.Offset == call.Offset);
                        increment = false;
                    }
                    break;
                }
                case FinishCommand _:
                {
                    if (track.DoCommandWork)
                    {
                        track.Stopped = true;
                        increment = false;
                    }
                    break;
                }
                case ForceAttackCommand attack:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, attack.Attack);
                    if (track.DoCommandWork)
                    {
                        track.Attack = (byte)arg;
                    }
                    break;
                }
                case ForceDecayCommand decay:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, decay.Decay);
                    if (track.DoCommandWork)
                    {
                        track.Decay = (byte)arg;
                    }
                    break;
                }
                case ForceReleaseCommand release:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, release.Release);
                    if (track.DoCommandWork)
                    {
                        track.Release = (byte)arg;
                    }
                    break;
                }
                case ForceSustainCommand sustain:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, sustain.Sustain);
                    if (track.DoCommandWork)
                    {
                        track.Sustain = (byte)arg;
                    }
                    break;
                }
                case JumpCommand jump:
                {
                    if (track.DoCommandWork)
                    {
                        track.CurEvent = ev.FindIndex(c => c.Offset == jump.Offset);
                        increment = false;
                    }
                    break;
                }
                case LFODelayCommand delay:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Short : track.ArgOverrideType, delay.Delay);
                    if (track.DoCommandWork)
                    {
                        track.LFODelay = (ushort)arg;
                    }
                    break;
                }
                case LFODepthCommand depth:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, depth.Depth);
                    if (track.DoCommandWork)
                    {
                        track.LFODepth = (byte)arg;
                    }
                    break;
                }
                case LFORangeCommand range:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, range.Range);
                    if (track.DoCommandWork)
                    {
                        track.LFORange = (byte)arg;
                    }
                    break;
                }
                case LFOSpeedCommand speed:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, speed.Speed);
                    if (track.DoCommandWork)
                    {
                        track.LFOSpeed = (byte)arg;
                    }
                    break;
                }
                case LFOTypeCommand type:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, type.Type);
                    if (track.DoCommandWork)
                    {
                        track.LFOType = (LFOType)arg;
                    }
                    break;
                }
                case LoopEndCommand _:
                {
                    if (track.DoCommandWork && track.CallStackDepth != 0)
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
                        track.CurEvent = track.CallStack[track.CallStackDepth - 1];
                        increment = false;
                    }
                    break;
                }
                case LoopStartCommand loop:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, loop.NumLoops);
                    if (track.DoCommandWork && track.CallStackDepth < 3)
                    {
                        track.CallStack[track.CallStackDepth] = track.CurEvent;
                        track.CallStackLoops[track.CallStackDepth] = (byte)arg;
                        track.CallStackDepth++;
                    }
                    break;
                }
                case ModIfCommand _:
                {
                    if (track.DoCommandWork)
                    {
                        track.DoCommandWork = track.VariableFlag;
                        resetCmdWork = false;
                    }
                    break;
                }
                case ModRandCommand _:
                {
                    if (track.DoCommandWork)
                    {
                        track.ArgOverrideType = ArgType.Rand;
                        resetOverride = false;
                    }
                    break;
                }
                case ModVarCommand _:
                {
                    if (track.DoCommandWork)
                    {
                        track.ArgOverrideType = ArgType.PlayerVar;
                        resetOverride = false;
                    }
                    break;
                }
                case MonophonyCommand mono:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, mono.Mono);
                    if (track.DoCommandWork)
                    {
                        track.Mono = arg == 1;
                    }
                    break;
                }
                case NoteComand note:
                {
                    int duration = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.VarLen : track.ArgOverrideType, note.Duration);
                    if (track.DoCommandWork)
                    {
                        int k = note.Key + track.Transpose;
                        if (k < 0)
                        {
                            k = 0;
                        }
                        else if (k > 0x7F)
                        {
                            k = 0x7F;
                        }
                        byte key = (byte)k;
                        PlayNote(track, key, note.Velocity, duration);
                        track.PortamentoKey = key;
                        if (track.Mono)
                        {
                            track.Rest = duration;
                            if (duration == 0)
                            {
                                track.WaitingForNoteToFinishBeforeContinuingXD = true;
                            }
                        }
                    }
                    break;
                }
                case OpenTrackCommand open:
                {
                    if (trackIndex == 0)
                    {
                        Track truck = _tracks[open.Track];
                        if (track.DoCommandWork && truck.Allocated && !truck.Enabled)
                        {
                            truck.Enabled = true;
                            truck.CurEvent = Events[open.Track].FindIndex(c => c.Offset == open.Offset);
                        }
                    }
                    break;
                }
                case PanpotCommand panpot:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, panpot.Panpot);
                    if (track.DoCommandWork)
                    {
                        track.Panpot = (sbyte)(arg - 0x40);
                    }
                    break;
                }
                case PitchBendCommand bend:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, bend.Bend);
                    if (track.DoCommandWork)
                    {
                        track.PitchBend = (sbyte)arg;
                    }
                    break;
                }
                case PitchBendRangeCommand bendRange:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, bendRange.Range);
                    if (track.DoCommandWork)
                    {
                        track.PitchBendRange = (byte)arg;
                    }
                    break;
                }
                case PlayerVolumeCommand pVolume:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, pVolume.Volume);
                    if (track.DoCommandWork)
                    {
                        Volume = (byte)arg;
                    }
                    break;
                }
                case PortamentoControlCommand port:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, port.Portamento);
                    if (track.DoCommandWork)
                    {
                        int k = arg + track.Transpose;
                        if (k < 0)
                        {
                            k = 0;
                        }
                        else if (k > 0x7F)
                        {
                            k = 0x7F;
                        }
                        track.PortamentoKey = (byte)k;
                        track.Portamento = true;
                    }
                    break;
                }
                case PortamentoTimeCommand portTime:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, portTime.Time);
                    if (track.DoCommandWork)
                    {
                        track.PortamentoTime = (byte)arg;
                    }
                    break;
                }
                case PortamentoToggleCommand portToggle:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, portToggle.Portamento);
                    if (track.DoCommandWork)
                    {
                        track.Portamento = arg == 1;
                    }
                    break;
                }
                case PriorityCommand priority:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, priority.Priority);
                    if (track.DoCommandWork)
                    {
                        track.Priority = (byte)arg;
                    }
                    break;
                }
                case RestCommand rest:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.VarLen : track.ArgOverrideType, rest.Rest);
                    if (track.DoCommandWork)
                    {
                        track.Rest = arg;
                    }
                    break;
                }
                case ReturnCommand _:
                {
                    if (track.DoCommandWork && track.CallStackDepth != 0)
                    {
                        track.CallStackDepth--;
                        track.CurEvent = track.CallStack[track.CallStackDepth];
                        increment = false;
                    }
                    break;
                }
                case SweepPitchCommand sweep:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Short : track.ArgOverrideType, sweep.Pitch);
                    if (track.DoCommandWork)
                    {
                        track.SweepPitch = (short)arg;
                    }
                    break;
                }
                case TempoCommand tem:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Short : track.ArgOverrideType, tem.Tempo);
                    if (track.DoCommandWork)
                    {
                        _tempo = (ushort)arg;
                    }
                    break;
                }
                case TieCommand tie:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, tie.Tie);
                    if (track.DoCommandWork)
                    {
                        track.Tie = arg == 1;
                        track.StopAllChannels();
                    }
                    break;
                }
                case TrackExpressionCommand tExpression:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, tExpression.Expression);
                    if (track.DoCommandWork)
                    {
                        track.Expression = (byte)arg;
                    }
                    break;
                }
                case TrackVolumeCommand tVolume:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, tVolume.Volume);
                    if (track.DoCommandWork)
                    {
                        track.Volume = (byte)arg;
                    }
                    break;
                }
                case TransposeCommand transpose:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Byte : track.ArgOverrideType, transpose.Transpose);
                    if (track.DoCommandWork)
                    {
                        track.Transpose = (sbyte)arg;
                    }
                    break;
                }
                case VarAddCommand add:
                {
                    short arg = (short)ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Short : track.ArgOverrideType, add.Argument);
                    if (track.DoCommandWork)
                    {
                        _vars[add.Variable] += arg;
                    }
                    break;
                }
                case VarCmpEECommand cmpEE:
                {
                    short arg = (short)ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Short : track.ArgOverrideType, cmpEE.Argument);
                    if (track.DoCommandWork)
                    {
                        track.VariableFlag = _vars[cmpEE.Variable] == arg;
                    }
                    break;
                }
                case VarCmpGECommand cmpGE:
                {
                    short arg = (short)ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Short : track.ArgOverrideType, cmpGE.Argument);
                    if (track.DoCommandWork)
                    {
                        track.VariableFlag = _vars[cmpGE.Variable] >= arg;
                    }
                    break;
                }
                case VarCmpGGCommand cmpGG:
                {
                    short arg = (short)ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Short : track.ArgOverrideType, cmpGG.Argument);
                    if (track.DoCommandWork)
                    {
                        track.VariableFlag = _vars[cmpGG.Variable] > arg;
                    }
                    break;
                }
                case VarCmpLECommand cmpLE:
                {
                    short arg = (short)ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Short : track.ArgOverrideType, cmpLE.Argument);
                    if (track.DoCommandWork)
                    {
                        track.VariableFlag = _vars[cmpLE.Variable] <= arg;
                    }
                    break;
                }
                case VarCmpLLCommand cmpLL:
                {
                    short arg = (short)ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Short : track.ArgOverrideType, cmpLL.Argument);
                    if (track.DoCommandWork)
                    {
                        track.VariableFlag = _vars[cmpLL.Variable] < arg;
                    }
                    break;
                }
                case VarCmpNECommand cmpNE:
                {
                    short arg = (short)ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Short : track.ArgOverrideType, cmpNE.Argument);
                    if (track.DoCommandWork)
                    {
                        track.VariableFlag = _vars[cmpNE.Variable] != arg;
                    }
                    break;
                }
                case VarDivCommand div:
                {
                    short arg = (short)ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Short : track.ArgOverrideType, div.Argument);
                    if (track.DoCommandWork && arg != 0)
                    {
                        _vars[div.Variable] /= arg;
                    }
                    break;
                }
                case VarMulCommand mul:
                {
                    short arg = (short)ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Short : track.ArgOverrideType, mul.Argument);
                    if (track.DoCommandWork)
                    {
                        _vars[mul.Variable] *= arg;
                    }
                    break;
                }
                case VarRandCommand rnd:
                {
                    short arg = (short)ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Short : track.ArgOverrideType, rnd.Argument);
                    if (track.DoCommandWork)
                    {
                        bool negate = false;
                        if (arg < 0)
                        {
                            negate = true;
                            arg = (short)-arg;
                        }
                        short val = (short)_rand.Next(arg + 1);
                        if (negate)
                        {
                            val = (short)-val;
                        }
                        _vars[rnd.Variable] = val;
                    }
                    break;
                }
                case VarSetCommand set:
                {
                    short arg = (short)ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Short : track.ArgOverrideType, set.Argument);
                    if (track.DoCommandWork)
                    {
                        _vars[set.Variable] = arg;
                    }
                    break;
                }
                case VarShiftCommand shift:
                {
                    short arg = (short)ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Short : track.ArgOverrideType, shift.Argument);
                    if (track.DoCommandWork)
                    {
                        _vars[shift.Variable] = arg < 0 ? (short)(_vars[shift.Variable] >> -arg) : (short)(_vars[shift.Variable] << arg);
                    }
                    break;
                }
                case VarSubCommand sub:
                {
                    short arg = (short)ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.Short : track.ArgOverrideType, sub.Argument);
                    if (track.DoCommandWork)
                    {
                        _vars[sub.Variable] -= arg;
                    }
                    break;
                }
                case VoiceCommand voice:
                {
                    int arg = ReadArg(track.ArgOverrideType == ArgType.None ? ArgType.VarLen : track.ArgOverrideType, voice.Voice);
                    if (track.DoCommandWork)
                    {
                        track.Voice = (byte)arg;
                    }
                    break;
                }
            }
            if (increment)
            {
                track.CurEvent++;
            }
            if (resetOverride)
            {
                track.ArgOverrideType = ArgType.None;
            }
            if (resetCmdWork)
            {
                track.DoCommandWork = true;
            }
        }

        private void Tick()
        {
            _time.Start();
            while (State == PlayerState.Playing || State == PlayerState.Recording)
            {
                while (_tempoStack >= 240)
                {
                    _tempoStack -= 240;
                    bool allDone = true;
                    for (int i = 0; i < 0x10; i++)
                    {
                        Track track = _tracks[i];
                        if (track.Enabled)
                        {
                            track.Tick();
                            while (track.Rest == 0 && !track.WaitingForNoteToFinishBeforeContinuingXD && !track.Stopped)
                            {
                                ExecuteNext(i);
                            }
                            if (i == _longestTrack)
                            {
                                if (ElapsedTicks == MaxTicks)
                                {
                                    if (!track.Stopped)
                                    {
                                        List<long> t = Events[i][track.CurEvent].Ticks;
                                        ElapsedTicks = t.Count == 0 ? 0 : t[0] - track.Rest; // Prevent crashes with songs that don't load all ticks yet (See SetTicks())
                                        _elapsedLoops++;
                                        if (ShouldFadeOut && !_fadeOutBegan && _elapsedLoops > NumLoops)
                                        {
                                            _fadeOutBegan = true;
                                            _mixer.BeginFadeOut();
                                        }
                                    }
                                }
                                else
                                {
                                    ElapsedTicks++;
                                }
                            }
                            if (!track.Stopped || track.Channels.Count != 0)
                            {
                                allDone = false;
                            }
                        }
                    }
                    if (_fadeOutBegan && _mixer.IsFadeDone())
                    {
                        allDone = true;
                    }
                    if (allDone)
                    {
                        State = PlayerState.Stopped;
                        SongEnded?.Invoke();
                    }
                }
                _tempoStack += _tempo;
                _mixer.ChannelTick();
                _mixer.Process(State == PlayerState.Playing, State == PlayerState.Recording);
                if (State == PlayerState.Playing)
                {
                    _time.Wait();
                }
            }
            _time.Stop();
        }
    }
}
