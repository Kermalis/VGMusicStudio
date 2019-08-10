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
        private const long SongOffset = 0x120000;
        private const int RefreshRate = 60; // TODO: A PSF can determine refresh rate regardless of region
        private readonly Track[] tracks = new Track[0x10];
        private readonly Mixer mixer;
        private readonly Config config;
        private readonly TimeBarrier time;
        private Thread thread;
        private byte[] exeBuffer;
        private VAB vab;
        private long dataOffset;
        private byte runningStatus;
        private ushort ticksPerQuarterNote;
        private uint microsecondsPerBeat;
        private uint microsecondsPerTick;
        private long tickStack;
        private long ticksPerUpdate;
        private ushort tempo;
        private long deltaTicks;
        private long elapsedLoops;
        private bool fadeOutBegan;

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
                tracks[i] = new Track(i);
            }
            this.mixer = mixer;
            this.config = config;

            time = new TimeBarrier(RefreshRate);
        }
        private void CreateThread()
        {
            thread = new Thread(Tick) { Name = "PSF Player Tick" };
            thread.Start();
        }
        private void WaitThread()
        {
            if (thread != null && (thread.ThreadState == ThreadState.Running || thread.ThreadState == ThreadState.WaitSleepJoin))
            {
                thread.Join();
            }
        }

        private void TEMPORARY_UpdateTimeVars()
        {
            tempo = (ushort)(60000000 / microsecondsPerBeat);
            microsecondsPerTick = microsecondsPerBeat / ticksPerQuarterNote;
            ticksPerUpdate = 1000000 / RefreshRate;
        }
        private void InitEmulation()
        {
            dataOffset = SongOffset;
            dataOffset += 4; // "pQES"
            dataOffset += 4; // Version
            ticksPerQuarterNote = (ushort)((exeBuffer[dataOffset++] << 8) | exeBuffer[dataOffset++]);
            microsecondsPerBeat = (uint)((exeBuffer[dataOffset++] << 16) | (exeBuffer[dataOffset++] << 8) | exeBuffer[dataOffset++]);
            TEMPORARY_UpdateTimeVars();
            //dataOffset += 2; // Time Signature
            byte ts1 = exeBuffer[dataOffset++];
            double ts2 = Math.Pow(2, exeBuffer[dataOffset++]);
            dataOffset += 4; // Unknown

            tickStack = elapsedLoops = ElapsedTicks = deltaTicks = runningStatus = 0;
            fadeOutBegan = false;
            for (int i = 0; i < tracks.Length; i++)
            {
                tracks[i].Init();
            }
        }
        public void LoadSong(long index)
        {
            PSF.Open(config.BGMFiles[index], out exeBuffer, out _);
            using (var reader = new EndianBinaryReader(new MemoryStream(exeBuffer)))
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

                vab = new VAB(reader);
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
                runningStatus = 0;
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
                        cmd = runningStatus;
                        reader.BaseStream.Position--;
                    }
                    else
                    {
                        runningStatus = cmd;
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
                            ushort bend = reader.ReadUInt16();
                            if (!EventExists(offset))
                            {
                                AddEvent(new PitchBendCommand { Bend = bend });
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
        public void GetSongState(UI.SongInfoControl.SongInfo info)
        {
            info.Tempo = tempo;
            for (int i = 0; i < 0x10; i++)
            {
                Track track = tracks[i];
                UI.SongInfoControl.SongInfo.Track tin = info.Tracks[i];
                tin.Position = dataOffset;
                tin.Rest = deltaTicks;
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
                if (((value = exeBuffer[dataOffset++]) & 0x80) != 0)
                {
                    value &= 0x7F;
                    do
                    {
                        value = (uint)((value << 7) + ((c = exeBuffer[dataOffset++]) & 0x7F));
                    } while ((c & 0x80) != 0);
                }
                return value;
            }

            deltaTicks = ReadVarLen();
            byte cmd = exeBuffer[dataOffset++];
            if (cmd <= 0x7F)
            {
                cmd = runningStatus;
                dataOffset--;
            }
            else
            {
                runningStatus = cmd;
            }
            Track track = tracks[cmd & 0xF];
            switch (cmd & 0xF0)
            {
                case 0x90:
                {
                    byte key = exeBuffer[dataOffset++];
                    byte velocity = exeBuffer[dataOffset++];
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
                        Channel c = mixer.AllocateChannel();
                        c.Stop();
                        c.StartPSG(4, -1);
                        c.Key = key;
                        c.NoteVelocity = velocity;
                        c.Owner = track;
                        track.Channels.Add(c);
                    }
                    break;
                }
                case 0xB0:
                {
                    byte controller = exeBuffer[dataOffset++];
                    byte value = exeBuffer[dataOffset++];
                    break;
                }
                case 0xC0:
                {
                    byte voice = exeBuffer[dataOffset++];
                    track.Voice = voice;
                    break;
                }
                case 0xE0:
                {
                    ushort pitchBend = (ushort)((exeBuffer[dataOffset++] << 8) | exeBuffer[dataOffset++]);
                    track.PitchBend = pitchBend;
                    break;
                }
                case 0xF0:
                {
                    byte meta = exeBuffer[dataOffset++];
                    switch (meta)
                    {
                        case 0x2F:
                        {
                            track.Stopped = true;
                            break;
                        }
                        case 0x51:
                        {
                            microsecondsPerBeat = (uint)((exeBuffer[dataOffset++] << 16) | (exeBuffer[dataOffset++] << 8) | exeBuffer[dataOffset++]);
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
            time.Start();
            while (State == PlayerState.Playing || State == PlayerState.Recording)
            {
                while (tickStack > microsecondsPerTick)
                {
                    tickStack -= microsecondsPerTick;
                    if (deltaTicks > 0)
                    {
                        deltaTicks--;
                    }
                    while (deltaTicks == 0)
                    {
                        ExecuteNext();
                    }
                    bool allDone = true;
                    for (int i = 0; i < 0x10; i++)
                    {
                        Track track = tracks[i];
                        if (!track.Stopped || track.Channels.Count != 0)
                        {
                            allDone = false;
                        }
                    }
                    if (ElapsedTicks == MaxTicks)
                    {
                        ElapsedTicks = 0;
                        elapsedLoops++;
                        if (ShouldFadeOut && !fadeOutBegan && elapsedLoops > NumLoops)
                        {
                            fadeOutBegan = true;
                            mixer.BeginFadeOut();
                        }
                    }
                    else
                    {
                        ElapsedTicks++;
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
                tickStack += ticksPerUpdate;
                mixer.ChannelTick();
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
