using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core.PSX.PSF
{
    internal class Player : IPlayer
    {
        private const long SongOffset = 0x120000; // Crash Bandicoot 2
        private const long SamplesOffset = 0x140000; // Crash Bandicoot 2
        private const int RefreshRate = 192; // TODO: A PSF can determine refresh rate regardless of region
        private readonly Track[] _tracks = new Track[0x10];
        private readonly Mixer _mixer;
        private readonly Config _config;
        private readonly TimeBarrier _time;
        private Thread _thread;
        private byte[] _exeBuffer;
        private VAB _vab;
        private long _dataOffset;
        private long _startOffset;
        private long _loopOffset;
        private byte _runningStatus;
        private ushort _ticksPerQuarterNote;
        private uint _microsecondsPerBeat;
        private uint _microsecondsPerTick;
        private long _tickStack;
        private long _ticksPerUpdate;
        private ushort _tempo;
        private long _deltaTicks;
        private long _elapsedLoops;

        public List<SongEvent>[] Events { get; private set; }
        public long MaxTicks { get; private set; }
        public long ElapsedTicks { get; private set; }
        public bool ShouldFadeOut { get; set; }
        public long NumLoops { get; set; }

        public PlayerState State { get; private set; }
        public event SongEndedEvent SongEnded;

        public Player(Mixer mixer, Config config)
        {
            for (byte i = 0; i < 0x10; i++)
            {
                _tracks[i] = new Track(i);
            }
            _mixer = mixer;
            _config = config;

            _time = new TimeBarrier(RefreshRate);
        }
        private void CreateThread()
        {
            _thread = new Thread(Tick) { Name = "PSF Player Tick" };
            _thread.Start();
        }
        private void WaitThread()
        {
            if (_thread != null && (_thread.ThreadState == ThreadState.Running || _thread.ThreadState == ThreadState.WaitSleepJoin))
            {
                _thread.Join();
            }
        }

        private void TEMPORARY_UpdateTimeVars()
        {
            _tempo = (ushort)(60000000 / _microsecondsPerBeat);
            _microsecondsPerTick = _microsecondsPerBeat / _ticksPerQuarterNote;
            _ticksPerUpdate = 1000000 / RefreshRate;
        }
        private void InitEmulation()
        {
            _dataOffset = SongOffset;
            _dataOffset += 4; // "pQES"
            _dataOffset += 4; // Version
            _ticksPerQuarterNote = (ushort)((_exeBuffer[_dataOffset++] << 8) | _exeBuffer[_dataOffset++]);
            _microsecondsPerBeat = (uint)((_exeBuffer[_dataOffset++] << 16) | (_exeBuffer[_dataOffset++] << 8) | _exeBuffer[_dataOffset++]);
            TEMPORARY_UpdateTimeVars();
            //dataOffset += 2; // Time Signature
            byte ts1 = _exeBuffer[_dataOffset++];
            double ts2 = Math.Pow(2, _exeBuffer[_dataOffset++]);
            _dataOffset += 4; // Unknown

            _startOffset = _dataOffset;
            _loopOffset = _tickStack = _elapsedLoops = ElapsedTicks = _deltaTicks = _runningStatus = 0;
            _mixer.ResetFade();
            for (int i = 0; i < _tracks.Length; i++)
            {
                _tracks[i].Init();
            }
        }
        public void LoadSong(long index)
        {
            PSF.Open(_config.BGMFiles[index], out _exeBuffer, out _);
            using (var reader = new EndianBinaryReader(new MemoryStream(_exeBuffer)))
            {
                uint ReadVarLen()
                {
                    uint value;
                    byte c;
                    if (((value = reader.ReadByte()) & 0x80) != 0)
                    {
                        value &= 0x7F;
                        do
                        {
                            value = (uint)((value << 7) + ((c = reader.ReadByte()) & 0x7F));
                        } while ((c & 0x80) != 0);
                    }
                    return value;
                }

                _vab = new VAB(reader);
                reader.BaseStream.Position = SongOffset;
                reader.Endianness = Endianness.BigEndian;
                reader.ReadString(4); // "pQES"
                reader.ReadUInt32(); // Version
                reader.ReadUInt16(); // Ticks per Quarter Note
                reader.ReadBytes(3); // Microseconds per Beat
                reader.ReadBytes(2); // Time signature
                reader.ReadBytes(4); // Unknown
                Events = new List<SongEvent>[0x10];
                for (int i = 0; i < 0x10; i++)
                {
                    Events[i] = new List<SongEvent>();
                }
                _runningStatus = 0;
                byte curTrack = 0;
                MaxTicks = 0;

                bool EventExists(long offset)
                {
                    return Events[curTrack].Any(e => e.Offset == offset);
                }
                bool cont = true;
                while (cont)
                {
                    long offset = reader.BaseStream.Position;
                    void AddEvent(ICommand command)
                    {
                        var ev = new SongEvent(offset, command);
                        ev.Ticks.Add(MaxTicks);
                        Events[curTrack].Add(ev);
                    }
                    MaxTicks += ReadVarLen();
                    byte cmd = reader.ReadByte();
                    void Invalid()
                    {
                        throw new Exception(string.Format("TODO", curTrack, offset, cmd));
                    }

                    if (cmd <= 0x7F)
                    {
                        cmd = _runningStatus;
                        reader.BaseStream.Position--;
                    }
                    else
                    {
                        _runningStatus = cmd;
                    }
                    curTrack = (byte)(cmd & 0xF);
                    switch (cmd & 0xF0)
                    {
                        case 0x90:
                        {
                            byte key = reader.ReadByte();
                            byte velocity = reader.ReadByte();
                            if (!EventExists(offset))
                            {
                                AddEvent(new NoteCommand { Key = key, Velocity = velocity });
                            }
                            break;
                        }
                        case 0xB0:
                        {
                            byte controller = reader.ReadByte();
                            byte value = reader.ReadByte();
                            if (!EventExists(offset))
                            {
                                AddEvent(new ControllerCommand { Controller = controller, Value = value });
                            }
                            switch (controller)
                            {
                                case 0x63:
                                {
                                    switch (value)
                                    {
                                        case 0x1E:
                                        {
                                            cont = false;
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                            break;
                        }
                        case 0xC0:
                        {
                            byte voice = reader.ReadByte();
                            if (!EventExists(offset))
                            {
                                AddEvent(new VoiceCommand { Voice = voice });
                            }
                            break;
                        }
                        case 0xE0:
                        {
                            byte bend1 = reader.ReadByte();
                            byte bend2 = reader.ReadByte();
                            if (!EventExists(offset))
                            {
                                AddEvent(new PitchBendCommand { Bend1 = bend1, Bend2 = bend2 });
                            }
                            break;
                        }
                        case 0xF0:
                        {
                            byte meta = reader.ReadByte();
                            switch (meta)
                            {
                                case 0x2F:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new FinishCommand());
                                    }
                                    cont = false;
                                    break;
                                }
                                case 0x51:
                                {
                                    uint tempo = (uint)((reader.ReadUInt16() << 8) | reader.ReadByte());
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new TempoCommand { Tempo = tempo });
                                    }
                                    break;
                                }
                                default: Invalid(); break; // TODO: Include this invalid portion
                            }
                            break;
                        }
                        default: Invalid(); break;
                    }
                }
            }
        }
        public void SetCurrentPosition(long ticks)
        {
            /*if (tracks == null)
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
                        while (tempoStack >= 240)
                        {
                            tempoStack -= 240;
                            for (int i = 0; i < tracks.Length; i++)
                            {
                                Track track = tracks[i];
                                if (!track.Stopped)
                                {
                                    track.Tick();
                                    while (track.Rest == 0 && !track.Stopped)
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
                        tempoStack += tempo;
                    }
                }
            finish:
                for (int i = 0; i < tracks.Length; i++)
                {
                    tracks[i].StopAllChannels();
                }
                Pause();
            }*/
        }
        public void Play()
        {
            if (State == PlayerState.Playing || State == PlayerState.Paused || State == PlayerState.Stopped)
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
        public void GetSongState(UI.SongInfoControl.SongInfo info)
        {
            info.Tempo = _tempo;
            for (int i = 0; i < 0x10; i++)
            {
                Track track = _tracks[i];
                UI.SongInfoControl.SongInfo.Track tin = info.Tracks[i];
                tin.Position = _dataOffset;
                tin.Rest = _deltaTicks;
                tin.Voice = track.Voice;
                tin.Type = "PCM";
                //tin.Volume = track.Volume;
                tin.PitchBend = track.PitchBend;
                //tin.Extra = track.Octave;
                //tin.Panpot = track.Panpot;

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
                        tin.Keys[numKeys++] = c.Key;
                        float a = (float)(0 + 0x40) / 0x80 * c.Volume / 0x7F;
                        if (a > left)
                        {
                            left = a;
                        }
                        a = (float)(0 + 0x40) / 0x80 * c.Volume / 0x7F;
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

        private void ExecuteNext()
        {
            uint ReadVarLen()
            {
                uint value;
                byte c;
                if (((value = _exeBuffer[_dataOffset++]) & 0x80) != 0)
                {
                    value &= 0x7F;
                    do
                    {
                        value = (uint)((value << 7) + ((c = _exeBuffer[_dataOffset++]) & 0x7F));
                    } while ((c & 0x80) != 0);
                }
                return value;
            }

            _deltaTicks = ReadVarLen();
            byte cmd = _exeBuffer[_dataOffset++];
            if (cmd <= 0x7F)
            {
                cmd = _runningStatus;
                _dataOffset--;
            }
            else
            {
                _runningStatus = cmd;
            }
            Track track = _tracks[cmd & 0xF];
            switch (cmd & 0xF0)
            {
                case 0x90:
                {
                    byte key = _exeBuffer[_dataOffset++];
                    byte velocity = _exeBuffer[_dataOffset++];
                    if (velocity == 0)
                    {
                        Channel[] chans = track.Channels.ToArray();
                        for (int i = 0; i < chans.Length; i++)
                        {
                            Channel c = chans[i];
                            if (c.Key == key)
                            {
                                c.Stop();
                                break;
                            }
                        }
                    }
                    else
                    {
                        VAB.Program p = _vab.Programs[track.Voice];
                        VAB.Instrument ins = _vab.Instruments[track.Voice];
                        byte num = p.NumTones;
                        for (int i = 0; i < num; i++)
                        {
                            VAB.Tone t = ins.Tones[i];
                            if (t.LowKey <= key && t.HighKey >= key)
                            {
                                (long sampleOffset, long sampleSize) = _vab.VAGs[t.SampleId - 1];
                                Channel c = _mixer.AllocateChannel();
                                if (c != null)
                                {
                                    c.Start(sampleOffset + SamplesOffset, sampleSize, _exeBuffer);
                                    c.Key = key;
                                    c.BaseKey = t.BaseKey;
                                    c.PitchTune = t.PitchTune;
                                    c.NoteVelocity = velocity;
                                    c.Owner = track;
                                    track.Channels.Add(c);
                                }
                                break;
                            }
                        }
                    }
                    break;
                }
                case 0xB0:
                {
                    byte controller = _exeBuffer[_dataOffset++];
                    byte value = _exeBuffer[_dataOffset++];
                    switch (controller)
                    {
                        case 0x63:
                        {
                            switch (value)
                            {
                                case 0x14:
                                {
                                    _loopOffset = _dataOffset;
                                    break;
                                }
                                case 0x1E:
                                {
                                    _dataOffset = _loopOffset;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                    break;
                }
                case 0xC0:
                {
                    byte voice = _exeBuffer[_dataOffset++];
                    track.Voice = voice;
                    break;
                }
                case 0xE0:
                {
                    ushort pitchBend = (ushort)((_exeBuffer[_dataOffset++] << 8) | _exeBuffer[_dataOffset++]);
                    track.PitchBend = pitchBend;
                    break;
                }
                case 0xF0:
                {
                    byte meta = _exeBuffer[_dataOffset++];
                    switch (meta)
                    {
                        case 0x2F:
                        {
                            _dataOffset = _startOffset;
                            break;
                        }
                        case 0x51:
                        {
                            _microsecondsPerBeat = (uint)((_exeBuffer[_dataOffset++] << 16) | (_exeBuffer[_dataOffset++] << 8) | _exeBuffer[_dataOffset++]);
                            TEMPORARY_UpdateTimeVars();
                            break;
                        }
                    }
                    break;
                }
            }
        }

        private void Tick()
        {
            _time.Start();
            while (true)
            {
                PlayerState state = State;
                bool playing = state == PlayerState.Playing;
                bool recording = state == PlayerState.Recording;
                if (!playing && !recording)
                {
                    goto stop;
                }

                while (_tickStack > _microsecondsPerTick)
                {
                    _tickStack -= _microsecondsPerTick;
                    if (_deltaTicks > 0)
                    {
                        _deltaTicks--;
                    }
                    while (_deltaTicks == 0)
                    {
                        ExecuteNext();
                    }
                    if (ElapsedTicks == MaxTicks)
                    {
                        for (int i = 0; i < 0x10; i++)
                        {
                            List<SongEvent> t = Events[i];
                            for (int j = 0; j < t.Count; j++)
                            {
                                SongEvent e = t[j];
                                if (e.Offset == _dataOffset)
                                {
                                    ElapsedTicks = e.Ticks[0];
                                    goto doneSearch;
                                }
                            }
                        }
                        throw new Exception();
                    doneSearch:
                        _elapsedLoops++;
                        if (ShouldFadeOut && !_mixer.IsFading() && _elapsedLoops > NumLoops)
                        {
                            _mixer.BeginFadeOut();
                        }
                    }
                    else
                    {
                        ElapsedTicks++;
                    }
                }
                _tickStack += _ticksPerUpdate;
                _mixer.ChannelTick();
                _mixer.Process(playing, recording);
                if (playing)
                {
                    _time.Wait();
                }
            }
        stop:
            _time.Stop();
        }
    }
}
