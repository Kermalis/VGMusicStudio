using Kermalis.VGMusicStudio.Util;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core.GBA.Rare
{
    internal class Player : IPlayer
    {
        private readonly Track[] _tracks = new Track[0x10];
        private readonly Mixer _mixer;
        private readonly Config _config;
        private readonly TimeBarrier _time;
        private Thread _thread;
        private byte _tempo;
        private int _tempoStack;
        private long _elapsedLoops;

        public List<SongEvent>[] Events { get; private set; }
        public long MaxTicks { get; private set; }
        public long ElapsedTicks { get; private set; }
        public bool ShouldFadeOut { get; set; }
        public long NumLoops { get; set; }
        private readonly int _longestTrack;

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

            _time = new TimeBarrier(Utils.AGB_FPS);
        }
        private void CreateThread()
        {
            _thread = new Thread(Tick) { Name = "Rare Player Tick" };
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
            _tempo = 120;
            _tempoStack = 0;
            _elapsedLoops = 0;
            ElapsedTicks = 0;
            _mixer.ResetFade();
            for (int i = 0; i < 0x10; i++)
            {
                //_tracks[i].Init();
            }
        }
        private void SetTicks()
        {
            /*MaxTicks = 0;
            bool u = false;
            for (int trackIndex = 0; trackIndex < 0x10; trackIndex++)
            {
                if (Events[trackIndex] != null)
                {
                    Events[trackIndex] = Events[trackIndex].OrderBy(e => e.Offset).ToList();
                    List<SongEvent> evs = Events[trackIndex];
                    Track track = _tracks[trackIndex];
                    track.Init();
                    ElapsedTicks = 0;
                    while (true)
                    {
                        SongEvent e = evs[track.CurEvent];
                        if (e.Ticks.Count > 0)
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
                        _longestTrack = trackIndex;
                        MaxTicks = ElapsedTicks;
                    }
                    track.NoteDuration = 0;
                }
            }*/
        }
        public void LoadSong(long index)
        {
            const int songTable = 0x6B28CC; // TODO
            int songOffset = _config.Reader.ReadInt32(songTable + (index * 4));
            Events = new List<SongEvent>[0x10];
            songOffset -= Utils.CartridgeOffset;
            Events[0] = new List<SongEvent>();
            bool EventExists(long offset)
            {
                return Events[0].Any(e => e.Offset == offset);
            }

            AddEvents(songOffset);
            void AddEvents(int startOffset)
            {
                _config.Reader.BaseStream.Position = startOffset;
                bool cont = true;
                while (cont)
                {
                    long offset = _config.Reader.BaseStream.Position;
                    void AddEvent(ICommand command)
                    {
                        Events[0].Add(new SongEvent(offset, command));
                    }
                    byte by = _config.Reader.ReadByte();
                    byte channel = (byte)(by >> 4);
                    byte cmd = (byte)(by & 0xF);
                    switch (cmd)
                    {
                        case 0x0:
                        {
                            uint arg = (uint)(_config.Reader.ReadByte() | (_config.Reader.ReadByte() << 8) | (_config.Reader.ReadByte() << 16));
                            if (!EventExists(offset))
                            {
                                AddEvent(new TempoCommand { Channel = channel, Data = arg });
                            }
                            break;
                        }
                        case 0x1:
                        {
                            byte arg = _config.Reader.ReadByte();
                            if (!EventExists(offset))
                            {
                                AddEvent(new Rest8Command { Channel = channel, Data = arg });
                            }
                            break;
                        }
                        case 0x2:
                        {
                            ushort arg = _config.Reader.ReadUInt16();
                            if (!EventExists(offset))
                            {
                                AddEvent(new Rest16Command { Channel = channel, Data = arg });
                            }
                            break;
                        }
                        case 0x3:
                        {
                            uint arg = (uint)(_config.Reader.ReadByte() | (_config.Reader.ReadByte() << 8) | (_config.Reader.ReadByte() << 16));
                            if (!EventExists(offset))
                            {
                                AddEvent(new Rest24Command { Channel = channel, Data = arg });
                            }
                            break;
                        }
                        case 0x4:
                        {
                            if (!EventExists(offset))
                            {
                                AddEvent(new Unk4Command { Channel = channel });
                            }
                            break;
                        }
                        case 0x5:
                        {
                            byte[] arg = _config.Reader.ReadBytes(2);
                            if (!EventExists(offset))
                            {
                                AddEvent(new NoteOnCommand { Channel = channel, Data = arg });
                            }
                            break;
                        }
                        case 0x6:
                        {
                            byte[] arg = _config.Reader.ReadBytes(2);
                            if (!EventExists(offset))
                            {
                                AddEvent(new NoteOffCommand { Channel = channel, Data = arg });
                            }
                            break;
                        }
                        case 0x7:
                        {
                            byte[] arg = _config.Reader.ReadBytes(2);
                            if (!EventExists(offset))
                            {
                                AddEvent(new VolumeCommand { Channel = channel, Data = arg });
                            }
                            break;
                        }
                        case 0x8:
                        {
                            byte arg = _config.Reader.ReadByte();
                            if (!EventExists(offset))
                            {
                                AddEvent(new VoiceCommand { Channel = channel, Data = arg });
                            }
                            break;
                        }
                        case 0x9:
                        {
                            if (!EventExists(offset))
                            {
                                AddEvent(new Unk9Command { Channel = channel });
                            }
                            break;
                        }
                        case 0xA:
                        {
                            byte[] arg = _config.Reader.ReadBytes(2);
                            if (!EventExists(offset))
                            {
                                AddEvent(new PitchCommand { Channel = channel, Data = arg });
                            }
                            break;
                        }
                        case 0xB:
                        {
                            cont = false;
                            if (!EventExists(offset))
                            {
                                AddEvent(new FinishCommand { Channel = channel });
                            }
                            break;
                        }
                        default: throw new System.Exception("TODO");
                    }
                }
            }
            SetTicks();
        }
        public void SetCurrentPosition(long ticks)
        {
            /*if (Events == null)
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
                        while (_tempoStack >= 75)
                        {
                            _tempoStack -= 75;
                            for (int i = 0; i < 0x10; i++)
                            {
                                Track track = _tracks[i];
                                if (track.Enabled && !track.Stopped)
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
                        _tempoStack += _tempo;
                    }
                }
            finish:
                for (int i = 0; i < 0x10; i++)
                {
                    _tracks[i].NoteDuration = 0;
                }
                Pause();
            }*/
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
                UI.SongInfoControl.SongInfo.Track tin = info.Tracks[i];
                tin.Type = "Test";
                /*Track track = _tracks[i];
                if (track.Enabled)
                {
                    UI.SongInfoControl.SongInfo.Track tin = info.Tracks[i];
                    tin.Position = Events[i][track.CurEvent].Offset;
                    tin.Rest = track.Rest;
                    tin.Voice = track.Voice;
                    tin.Type = track.Type;
                    tin.Volume = track.Volume;
                    tin.PitchBend = track.GetPitch();
                    tin.Panpot = track.Panpot;
                    if (track.NoteDuration != 0 && !track.Channel.Stopped)
                    {
                        tin.Keys[0] = track.Channel.Key;
                        ChannelVolume vol = track.Channel.GetVolume();
                        tin.LeftVolume = vol.LeftVol;
                        tin.RightVolume = vol.RightVol;
                    }
                    else
                    {
                        tin.Keys[0] = byte.MaxValue;
                        tin.LeftVolume = 0f;
                        tin.RightVolume = 0f;
                    }
                }*/
            }
        }

        /*private VoiceEntry GetVoiceEntry(byte voice, byte key)
        {
            int vto = _config.VoiceTableOffset;
            short voiceOffset = _config.Reader.ReadInt16(vto + (voice * 2));
            short nextVoiceOffset = _config.Reader.ReadInt16(vto + ((voice + 1) * 2));
            if (voiceOffset == nextVoiceOffset)
            {
                return null;
            }
            else
            {
                long pos = vto + voiceOffset; // Prevent object creation in the last iteration
                VoiceEntry e = _config.Reader.ReadObject<VoiceEntry>(pos);
                while (e.MinKey > key || e.MaxKey < key)
                {
                    pos += 8;
                    if (pos == nextVoiceOffset)
                    {
                        return null;
                    }
                    e = _config.Reader.ReadObject<VoiceEntry>();
                }
                return e;
            }
        }*/
        private void PlayNote(Track track, byte key, byte duration)
        {
            /*VoiceEntry entry = GetVoiceEntry(track.Voice, key);
            if (entry != null)
            {
                track.NoteDuration = duration;
                if (track.Index >= 8)
                {
                    // TODO: "Sample" byte in VoiceEntry
                    ((SquareChannel)track.Channel).Init(key, new ADSR { A = 0xFF, D = 0x00, S = 0xFF, R = 0x00 }, track.Volume, track.Panpot, track.GetPitch());
                }
                else
                {
                    int sto = _config.SampleTableOffset;
                    int sampleOffset = _config.Reader.ReadInt32(sto + (entry.Sample * 4)); // Some entries are 0. If you play them, are they silent, or does it not care if they are 0?
                    ((PCMChannel)track.Channel).Init(key, new ADSR { A = 0xFF, D = 0x00, S = 0xFF, R = 0x00 }, sto + sampleOffset, entry.IsFixedFrequency == 0x80);
                    track.Channel.SetVolume(track.Volume, track.Panpot);
                    track.Channel.SetPitch(track.GetPitch());
                }
            }*/
        }
        private void ExecuteNext(int trackIndex, ref bool update)
        {
            /*bool increment = true;
            List<SongEvent> ev = Events[trackIndex];
            Track track = _tracks[trackIndex];
            ICommand cmd = ev[track.CurEvent].Command;
            switch (cmd)
            {
                case FreeNoteHamtaroCommand freeNote:
                {
                    track.Volume = freeNote.Volume;
                    update = true;
                    track.Rest += freeNote.Duration;
                    if (track.PrevCommand is FreeNoteHamtaroCommand && track.Channel.Key == freeNote.Key)
                    {
                        track.NoteDuration += freeNote.Duration;
                    }
                    else
                    {
                        PlayNote(track, freeNote.Key, freeNote.Duration);
                    }
                    break;
                }
                case FreeNoteMLSSCommand freeNote:
                {
                    track.Rest += freeNote.Duration;
                    if (track.PrevCommand is FreeNoteMLSSCommand && track.Channel.Key == freeNote.Key)
                    {
                        track.NoteDuration += freeNote.Duration;
                    }
                    else
                    {
                        PlayNote(track, freeNote.Key, freeNote.Duration);
                    }
                    break;
                }
                case VoiceCommand voice: track.Voice = voice.Voice; break;
                case VolumeCommand volume: track.Volume = volume.Volume; update = true; break;
                case PanpotCommand panpot: track.Panpot = panpot.Panpot; update = true; break;
                case PitchBendRangeCommand bendRange: track.PitchBendRange = bendRange.Range; update = true; break;
                case PitchBendCommand bend: track.PitchBend = bend.Bend; update = true; break;
                case RestCommand rest: track.Rest = rest.Rest; break;
                case JumpCommand jump:
                {
                    track.CurEvent = ev.FindIndex(c => c.Offset == jump.Offset);
                    increment = false;
                    break;
                }
                case TempoCommand tem: _tempo = tem.Tempo; break;
                case FinishCommand _: track.Stopped = true; increment = false; break;
                case NoteHamtaroCommand note:
                {
                    track.Volume = note.Volume;
                    update = true;
                    track.Rest += note.Duration;
                    if (track.PrevCommand is FreeNoteHamtaroCommand && track.Channel.Key == note.Key)
                    {
                        track.NoteDuration += note.Duration;
                    }
                    else
                    {
                        PlayNote(track, note.Key, note.Duration);
                    }
                    break;
                }
                case NoteMLSSCommand note:
                {
                    track.Rest += note.Duration;
                    if (track.PrevCommand is FreeNoteMLSSCommand && track.Channel.Key == note.Key)
                    {
                        track.NoteDuration += note.Duration;
                    }
                    else
                    {
                        PlayNote(track, note.Key, note.Duration);
                    }
                    break;
                }
            }
            track.PrevCommand = cmd;
            if (increment)
            {
                track.CurEvent++;
            }*/
        }

        private void Tick()
        {
            /*_time.Start();
            while (true)
            {
                PlayerState state = State;
                bool playing = state == PlayerState.Playing;
                bool recording = state == PlayerState.Recording;
                if (!playing && !recording)
                {
                    goto stop;
                }

                void MixerProcess()
                {
                    _mixer.Process(_tracks, playing, recording);
                }

                while (_tempoStack >= 75)
                {
                    _tempoStack -= 75;
                    bool allDone = true;
                    for (int i = 0; i < 0x10; i++)
                    {
                        Track track = _tracks[i];
                        if (track.Enabled)
                        {
                            byte prevDuration = track.NoteDuration;
                            track.Tick();
                            bool update = false;
                            while (track.Rest == 0 && !track.Stopped)
                            {
                                ExecuteNext(i, ref update);
                            }
                            if (i == _longestTrack)
                            {
                                if (ElapsedTicks == MaxTicks)
                                {
                                    if (!track.Stopped)
                                    {
                                        ElapsedTicks = Events[i][track.CurEvent].Ticks[0] - track.Rest;
                                        _elapsedLoops++;
                                        if (ShouldFadeOut && !_mixer.IsFading() && _elapsedLoops > NumLoops)
                                        {
                                            _mixer.BeginFadeOut();
                                        }
                                    }
                                }
                                else
                                {
                                    ElapsedTicks++;
                                }
                            }
                            if (prevDuration == 1 && track.NoteDuration == 0) // Note was not renewed
                            {
                                track.Channel.State = EnvelopeState.Release;
                            }
                            if (!track.Stopped)
                            {
                                allDone = false;
                            }
                            if (track.NoteDuration != 0)
                            {
                                allDone = false;
                                if (update)
                                {
                                    track.Channel.SetVolume(track.Volume, track.Panpot);
                                    track.Channel.SetPitch(track.GetPitch());
                                }
                            }
                        }
                    }
                    if (_mixer.IsFadeDone())
                    {
                        allDone = true;
                    }
                    if (allDone)
                    {
                        MixerProcess();
                        State = PlayerState.Stopped;
                        SongEnded?.Invoke();
                    }
                }
                _tempoStack += _tempo;
                MixerProcess();
                if (playing)
                {
                    _time.Wait();
                }
            }
        stop:
            _time.Stop();*/
        }
    }
}
