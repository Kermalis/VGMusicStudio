using Kermalis.VGMusicStudio.Util;
using System;
using System.Linq;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT
{
    internal class SDATPlayer : IPlayer
    {
        private class SoundVar
        {
            public short Value;
        }

        // Mario Kart DS song 0 sets variables 0 and 1 for unknown reasons (controlling the demo is a likely cause, as the timing goes with the demo states)
        // Mario Kart DS song 75 sets variable 0 to random values to play a random intro
        // New Super Mario Bros "BGM_SAMPLE" uses variable 0 for unknown reasons
        // New Super Mario Bros (multiple songs) use variable 2 to cause enemies to jump/wave
        // New Super Mario Bros "BGM_AMB_SABAKU" uses variables 4 and 5 to change panpot
        // New Super Mario Bros "BGM_AMB_SKY" uses variable 5 to change panpot
        // Pokémon HGSS (some johto battle songs) use variable 0 for unknown reasons
        // Spirit Tracks song 18 uses variable 1 to help variable 19 check what segment the song should play next
        private readonly SoundVar[] soundVars = new SoundVar[0x20]; // Unsure of the exact amount
        private readonly Track[] tracks = new Track[0x10];
        private readonly SDATMixer mixer;
        private readonly SDATConfig config;
        private readonly TimeBarrier time;
        private readonly Thread thread;
        private SSEQ sseq;
        private SBNK sbnk;
        public byte Volume;
        private ushort tempo;
        private int tempoStack;

        public PlayerState State { get; private set; }
        public event SongEndedEvent SongEnded;

        public SDATPlayer(SDATMixer mixer, SDATConfig config)
        {
            for (byte i = 0; i < tracks.Length; i++)
            {
                tracks[i] = new Track(i, this);
            }
            for (int i = 0; i < soundVars.Length; i++)
            {
                soundVars[i] = new SoundVar();
            }
            this.mixer = mixer;
            this.config = config;

            time = new TimeBarrier(192);
            thread = new Thread(Tick) { Name = "SDATPlayer Tick" };
            thread.Start();
        }

        public void LoadSong(long index)
        {
            Stop();

            SDAT.INFO.SequenceInfo seqInfo = config.SDAT.INFOBlock.SequenceInfos.Entries[index];
            if (seqInfo != null)
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
            }
            else
            {
                sseq = null;
                sbnk = null;
            }
        }
        public void Play()
        {
            Stop();
            if (sseq != null)
            {
                tempoStack = 0;
                tempo = 120;
                for (int i = 0; i < 0x10; i++)
                {
                    tracks[i].Init();
                }
                for (int i = 0; i < soundVars.Length; i++)
                {
                    soundVars[i].Value = short.MinValue; // Not sure what the initial value is, but it is not [0,15]
                }

                Track track0 = tracks[0];
                tracks[0].Enabled = true;
                // Peek byte to check multi track
                if (sseq.Data[track0.DataOffset++] == 0xFE)
                {
                    // Track 1 enabled = bit 1 set, Track 4 enabled = bit 4 set, etc
                    int trackBits = sseq.Data[track0.DataOffset++] | (sseq.Data[track0.DataOffset++] << 8);
                    for (int i = 1; i < 0x10; i++)
                    {
                        if ((trackBits & (1 << i)) != 0)
                        {
                            tracks[i].Enabled = true;
                        }
                    }
                }
                else // Wasn't multi track so go back
                {
                    track0.DataOffset--;
                }

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
                    info.Pitches[i] = track.GetPitch();
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

        private enum ArgType { Byte, Short, VarLen, Rand, SoundVar }
        private int ReadArg(Track track, ArgType type)
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
                    return SDATUtils.RNG.Next(min, max + 1);
                }
                case ArgType.SoundVar: // GetSoundVarShort
                {
                    byte varIndex = sseq.Data[track.DataOffset++];
                    return soundVars[varIndex].Value;
                }
                default: throw new Exception();
            }
        }
        public void PlayNote(Track track, byte key, byte noteVelocity, int noteLength)
        {
            Channel channel = null;
            if (track.Tie && track.Channels.Count != 0)
            {
                channel = track.Channels.Last();
                channel.Key = key;
                channel.NoteVelocity = noteVelocity;
            }
            else
            {
                InstrumentData inst = sbnk.GetInstrumentData(track.Voice, key);
                if (inst != null)
                {
                    channel = mixer.AllocateChannel(inst.Type, track);
                    if (channel != null)
                    {
                        if (track.Tie)
                        {
                            noteLength = -1;
                        }
                        int release = inst.Param.Release;
                        if (release == 0xFF)
                        {
                            noteLength = -1;
                            release = 0;
                        }
                        bool started = false;
                        switch (inst.Type)
                        {
                            case InstrumentType.PCM:
                            {
                                SWAVInfo wave = sbnk.GetWave(inst.Param.Info[1], inst.Param.Info[0]);
                                if (wave != null)
                                {
                                    channel.StartPCM(wave, noteLength);
                                    started = true;
                                }
                                break;
                            }
                            case InstrumentType.PSG:
                            {
                                channel.StartPSG((byte)inst.Param.Info[0], noteLength);
                                started = true;
                                break;
                            }
                            case InstrumentType.Noise:
                            {
                                channel.StartNoise(noteLength);
                                started = true;
                                break;
                            }
                        }
                        channel.Stop();
                        if (started)
                        {
                            channel.Key = key;
                            channel.BaseKey = inst.Param.BaseKey;
                            channel.NoteVelocity = noteVelocity;
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
                    channel.SweepLength = noteLength;
                    channel.AutoSweep = false;
                }
                channel.SweepCounter = 0;
            }
        }

        private void ExecuteNext(Track track)
        {
            ArgType argOverrideType = 0;
            bool useOverrideType = false;
            bool doCmdWork = true;
            byte cmd = sseq.Data[track.DataOffset++];
        again:
            if (cmd == 0xA0) // Rand: [New Super Mario Bros (BGM_AMB_CHIKA)]
            {
                cmd = sseq.Data[track.DataOffset++];
                argOverrideType = ArgType.Rand;
                useOverrideType = true;
                goto again;
            }
            else if (cmd == 0xA1) // Var: [New Super Mario Bros (BGM_AMB_SABAKU)]
            {
                cmd = sseq.Data[track.DataOffset++];
                argOverrideType = ArgType.SoundVar;
                useOverrideType = true;
                goto again;
            }
            else if (cmd == 0xA2) // If: [Mario Kart DS (75)]
            {
                cmd = sseq.Data[track.DataOffset++];
                doCmdWork = track.VariableFlag;
                goto again;
            }

            if (cmd < 0x80) // Notes
            {
                byte velocity = sseq.Data[track.DataOffset++];
                int length = ReadArg(track, useOverrideType ? argOverrideType : ArgType.VarLen);
                if (doCmdWork)
                {
                    byte key = (byte)(cmd + track.KeyShift).Clamp(0x0, 0x7F);
                    PlayNote(track, key, velocity, Math.Max(-1, length));
                    track.PortamentoKey = key;
                    if (track.ShouldWaitForNotesToFinish)
                    {
                        track.Delay = length;
                        if (length == 0)
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
                    int arg = ReadArg(track, useOverrideType ? argOverrideType : ArgType.VarLen);
                    if (doCmdWork)
                    {
                        if (cmd == 0x80) // Rest
                        {
                            track.Delay = arg;
                        }
                        else if (cmd == 0x81 && arg <= byte.MaxValue) // Program Change
                        {
                            track.Voice = (byte)arg;
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
                            if (doCmdWork)
                            {
                                tracks[index].DataOffset = offset24bit;
                            }
                            break;
                        }
                        case 0x94: // Goto
                        {
                            int offset24bit = sseq.Data[track.DataOffset++] | (sseq.Data[track.DataOffset++] << 8) | (sseq.Data[track.DataOffset++] << 16);
                            if (doCmdWork)
                            {
                                track.DataOffset = offset24bit;
                            }
                            break;
                        }
                        case 0x95: // Call
                        {
                            int offset24bit = sseq.Data[track.DataOffset++] | (sseq.Data[track.DataOffset++] << 8) | (sseq.Data[track.DataOffset++] << 16);
                            if (doCmdWork && track.CallStackDepth < 3)
                            {
                                track.CallStack[track.CallStackDepth] = track.DataOffset;
                                track.CallStackDepth += 1;
                                track.DataOffset = offset24bit;
                            }
                            break;
                        }
                    }
                }
                else if (cmdGroup == 0xB0)
                {
                    byte varIndex = sseq.Data[track.DataOffset++];
                    short mathArg = (short)ReadArg(track, useOverrideType ? argOverrideType : ArgType.Short);
                    if (doCmdWork)
                    {
                        SoundVar var = soundVars[varIndex];
                        switch (cmd)
                        {
                            case 0xB0:
                            {
                                Console.Write("Setvar {0} {1} (Old: {2}", varIndex, mathArg, var.Value);
                                var.Value = mathArg;
                                Console.WriteLine(", New: {0})", var.Value);
                                break;
                            }
                            case 0xB1:
                            {
                                Console.Write("Addvar {0} {1} (Old: {2}", varIndex, mathArg, var.Value);
                                var.Value += mathArg;
                                Console.WriteLine(", New: {0})", var.Value);
                                break;
                            }
                            case 0xB2:
                            {
                                Console.Write("Subvar {0} {1} (Old: {2}", varIndex, mathArg, var.Value);
                                var.Value -= mathArg;
                                Console.WriteLine(", New: {0})", var.Value);
                                break;
                            }
                            case 0xB3:
                            {
                                Console.Write("Mulvar {0} {1} (Old: {2}", varIndex, mathArg, var.Value);
                                var.Value *= mathArg;
                                Console.WriteLine(", New: {0})", var.Value);
                                break;
                            }
                            case 0xB4:
                            {
                                Console.Write("Divvar {0} {1} (Old: {2}", varIndex, mathArg, var.Value);
                                if (mathArg != 0)
                                {
                                    var.Value /= mathArg;
                                }
                                Console.WriteLine(", New: {0})", var.Value);
                                break;
                            }
                            case 0xB5:
                            {
                                Console.Write("Shiftvar {0} {1} (Old: {2}", varIndex, mathArg, var.Value);
                                var.Value = mathArg < 0 ? (short)(var.Value >> -mathArg) : (short)(var.Value << mathArg);
                                Console.WriteLine(", New: {0})", var.Value);
                                break;
                            }
                            case 0xB6: // [Mario Kart DS (75)]
                            {
                                Console.Write("Randvar {0} {1} (Old: {2}", varIndex, mathArg, var.Value);
                                bool negate = false;
                                if (mathArg < 0)
                                {
                                    negate = true;
                                    mathArg = (short)-mathArg;
                                }
                                short val = (short)SDATUtils.RNG.Next(mathArg + 1);
                                if (negate)
                                {
                                    val = (short)-val;
                                }
                                var.Value = val;
                                Console.WriteLine(", New: {0})", var.Value);
                                break;
                            }
                            case 0xB8:
                            {
                                Console.WriteLine("CMPVar {0} == {1} (Value: {2})", varIndex, mathArg, var.Value);
                                track.VariableFlag = var.Value == mathArg;
                                break;
                            }
                            case 0xB9:
                            {
                                Console.WriteLine("CMPVar {0} >= {1} (Value: {2})", varIndex, mathArg, var.Value);
                                track.VariableFlag = var.Value >= mathArg;
                                break;
                            }
                            case 0xBA:
                            {
                                Console.WriteLine("CMPVar {0} > {1} (Value: {2})", varIndex, mathArg, var.Value);
                                track.VariableFlag = var.Value > mathArg;
                                break;
                            }
                            case 0xBB:
                            {
                                Console.WriteLine("CMPVar {0} <= {1} (Value: {2})", varIndex, mathArg, var.Value);
                                track.VariableFlag = var.Value <= mathArg;
                                break;
                            }
                            case 0xBC:
                            {
                                Console.WriteLine("CMPVar {0} < {1} (Value: {2})", varIndex, mathArg, var.Value);
                                track.VariableFlag = var.Value < mathArg;
                                break;
                            }
                            case 0xBD:
                            {
                                Console.WriteLine("CMPVar {0} != {1} (Value: {2})", varIndex, mathArg, var.Value);
                                track.VariableFlag = var.Value != mathArg;
                                break;
                            }
                        }
                    }
                }
                else if (cmdGroup == 0xC0 || cmdGroup == 0xD0)
                {
                    int cmdArg = ReadArg(track, useOverrideType ? argOverrideType : ArgType.Byte);
                    if (doCmdWork)
                    {
                        switch (cmd)
                        {
                            case 0xC0: // Panpot
                            {
                                track.Pan = (sbyte)(cmdArg - 0x40);
                                break;
                            }
                            case 0xC1: // Volume
                            {
                                track.Volume = (byte)cmdArg;
                                break;
                            }
                            case 0xC2: // Player Volume
                            {
                                Volume = (byte)cmdArg;
                                break;
                            }
                            case 0xC3: // Key Shift
                            {
                                track.KeyShift = (sbyte)cmdArg;
                                break;
                            }
                            case 0xC4: // Pitch Bend
                            {
                                track.Bend = (sbyte)cmdArg;
                                break;
                            }
                            case 0xC5: // Pitch Bend Range
                            {
                                track.BendRange = (byte)cmdArg;
                                break;
                            }
                            case 0xC6: // Priority
                            {
                                track.Priority = (byte)cmdArg;
                                break;
                            }
                            case 0xC7: // Mono/Poly
                            {
                                track.ShouldWaitForNotesToFinish = cmdArg == 1;
                                break;
                            }
                            case 0xC8: // Tie
                            {
                                track.Tie = cmdArg == 1;
                                track.StopAllChannels();
                                break;
                            }
                            case 0xC9: // Portamento Control
                            {
                                track.PortamentoKey = (byte)(cmdArg + track.KeyShift);
                                track.Portamento = true;
                                break;
                            }
                            case 0xCA: // LFO Depth
                            {
                                track.LFODepth = (byte)cmdArg;
                                break;
                            }
                            case 0xCB: // LFO Speed
                            {
                                track.LFOSpeed = (byte)cmdArg;
                                break;
                            }
                            case 0xCC: // LFO Type
                            {
                                track.LFOType = (LFOType)cmdArg;
                                break;
                            }
                            case 0xCD: // LFO Range
                            {
                                track.LFORange = (byte)cmdArg;
                                break;
                            }
                            case 0xCE: // Portamento Toggle
                            {
                                track.Portamento = cmdArg == 1;
                                break;
                            }
                            case 0xCF: // Portamento Time
                            {
                                track.PortamentoTime = (byte)cmdArg;
                                break;
                            }
                            case 0xD0: // Forced Attack
                            {
                                track.Attack = (byte)cmdArg;
                                break;
                            }
                            case 0xD1: // Forced Decay
                            {
                                track.Decay = (byte)cmdArg;
                                break;
                            }
                            case 0xD2: // Forced Sustain
                            {
                                track.Sustain = (byte)cmdArg;
                                break;
                            }
                            case 0xD3: // Forced Release
                            {
                                track.Release = (byte)cmdArg;
                                break;
                            }
                            case 0xD4: // Call
                            {
                                if (track.CallStackDepth < 3)
                                {
                                    track.CallStack[track.CallStackDepth] = track.DataOffset;
                                    track.CallStackLoops[track.CallStackDepth] = (byte)cmdArg;
                                    track.CallStackDepth += 1;
                                }
                                break;
                            }
                            case 0xD5: // Expression
                            {
                                track.Expression = (byte)cmdArg;
                                break;
                            }
                            case 0xD6: // Print
                            {
                                Console.WriteLine("Track {0}, Var {1}, Value{2}", track.Index, cmdArg, soundVars[cmdArg].Value);
                                break;
                            }
                        }
                    }
                }
                else if (cmdGroup == 0xE0)
                {
                    int cmdArg = ReadArg(track, useOverrideType ? argOverrideType : ArgType.Short);
                    if (doCmdWork)
                    {
                        switch (cmd)
                        {
                            case 0xE0: // LFO Delay
                            {
                                track.LFODelay = (ushort)cmdArg;
                                break;
                            }
                            case 0xE1: // Tempo
                            {
                                tempo = (ushort)cmdArg;
                                break;
                            }
                            case 0xE3: // Sweep Pitch
                            {
                                track.SweepPitch = (short)cmdArg;
                                break;
                            }
                        }
                    }
                }
                else if (cmdGroup == 0xF0)
                {
                    if (doCmdWork)
                    {
                        switch (cmd)
                        {
                            case 0xFC: // Loop End
                            {
                                if (track.CallStackDepth != 0)
                                {
                                    byte count = track.CallStackLoops[track.CallStackDepth - 1];
                                    if (count != 0)
                                    {
                                        count--;
                                        if (count == 0)
                                        {
                                            track.CallStackDepth -= 1;
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
                                if (track.CallStackDepth != 0)
                                {
                                    track.CallStackDepth -= 1;
                                    track.DataOffset = track.CallStack[track.CallStackDepth];
                                }
                                break;
                            }
                            case 0xFF: // End
                            {
                                track.Stopped = true;
                                break;
                            }
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
                        bool allDone = true;
                        for (int i = 0; i < 0x10; i++)
                        {
                            Track track = tracks[i];
                            if (track.Enabled)
                            {
                                track.Tick();
                                while (track.Delay == 0 && !track.WaitingForNoteToFinishBeforeContinuingXD && !track.Stopped)
                                {
                                    ExecuteNext(track);
                                }
                                if (!track.Stopped || track.Channels.Count != 0)
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
                    mixer.ChannelTick();
                    mixer.Process();
                }
                time.Wait();
            }
            time.Stop();
        }
    }
}
