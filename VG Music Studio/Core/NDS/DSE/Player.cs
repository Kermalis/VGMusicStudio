using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Properties;
using Kermalis.VGMusicStudio.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    internal class Player : IPlayer
    {
        private readonly Mixer mixer;
        private readonly Config config;
        private readonly TimeBarrier time;
        private readonly Thread thread;
        private SWD masterSWD;
        private SWD localSWD;
        private Track[] tracks;
        private byte tempo;
        private int tempoStack;
        private long elapsedLoops;
        private bool fadeOutBegan;

        public List<SongEvent>[] Events { get; private set; }
        public long MaxTicks { get; private set; }
        public long ElapsedTicks { get; private set; }
        private int longestTrack;

        public PlayerState State { get; private set; }
        public event SongEndedEvent SongEnded;

        public Player(Mixer mixer, Config config)
        {
            this.mixer = mixer;
            this.config = config;

            time = new TimeBarrier(192);
            thread = new Thread(Tick) { Name = "DSE Player Tick" };
            thread.Start();
        }

        private void InitEmulation()
        {
            tempo = 120;
            tempoStack = 0;
            elapsedLoops = ElapsedTicks = 0;
            fadeOutBegan = false;
            for (int i = 0; i < tracks.Length; i++)
            {
                tracks[i].Init();
            }
        }
        private void SetTicks()
        {
            MaxTicks = 0;
            for (int trackIndex = 0; trackIndex < Events.Length; trackIndex++)
            {
                Events[trackIndex] = Events[trackIndex].OrderBy(e => e.Offset).ToList();
                List<SongEvent> evs = Events[trackIndex];
                Track track = tracks[trackIndex];
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
                        ExecuteNext(trackIndex);
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
            tracks = null;
            masterSWD = new SWD(Path.Combine(config.BGMPath, "bgm.swd"));
            string bgm = config.BGMFiles[index];
            localSWD = new SWD(Path.ChangeExtension(bgm, "swd"));
            using (var reader = new EndianBinaryReader(new MemoryStream(File.ReadAllBytes(bgm))))
            {
                SMD.Header header = reader.ReadObject<SMD.Header>();
                SMD.ISongChunk songChunk;
                switch (header.Version)
                {
                    case 0x402:
                    {
                        songChunk = reader.ReadObject<SMD.SongChunk_V402>();
                        break;
                    }
                    case 0x415:
                    {
                        songChunk = reader.ReadObject<SMD.SongChunk_V415>();
                        break;
                    }
                    default: throw new Exception(string.Format(Strings.ErrorDSEInvalidHeaderVersion, header.Version));
                }
                tracks = new Track[songChunk.NumTracks];
                Events = new List<SongEvent>[songChunk.NumTracks];
                for (byte i = 0; i < songChunk.NumTracks; i++)
                {
                    Events[i] = new List<SongEvent>();
                    bool EventExists(long offset)
                    {
                        return Events[i].Any(e => e.Offset == offset);
                    }

                    long startPosition = reader.BaseStream.Position;
                    reader.BaseStream.Position += 0x14;
                    uint lastNoteDuration = 0, lastRest = 0; // TODO: https://github.com/Kermalis/VGMusicStudio/issues/37
                    bool cont = true;
                    while (cont)
                    {
                        long offset = reader.BaseStream.Position;
                        void AddEvent(ICommand command)
                        {
                            Events[i].Add(new SongEvent(offset, command));
                        }
                        byte cmd = reader.ReadByte();
                        if (cmd <= 0x7F)
                        {
                            byte arg = reader.ReadByte();
                            int numParams = (arg & 0xC0) >> 6;
                            int oct = ((arg & 0x30) >> 4) - 2;
                            int k = arg & 0xF;
                            if (k < 12)
                            {
                                uint duration;
                                switch (numParams)
                                {
                                    case 0:
                                    {
                                        duration = lastNoteDuration;
                                        break;
                                    }
                                    default: // Big Endian reading of 8, 16, or 24 bits
                                    {
                                        duration = 0;
                                        for (int b = 0; b < numParams; b++)
                                        {
                                            duration = (duration << 8) | reader.ReadByte();
                                        }
                                        lastNoteDuration = duration;
                                        break;
                                    }
                                }
                                if (!EventExists(offset))
                                {
                                    AddEvent(new NoteCommand { Key = (byte)k, OctaveChange = (sbyte)oct, Velocity = cmd, Duration = duration });
                                }
                            }
                            else
                            {
                                throw new Exception(string.Format(Strings.ErrorDSEInvalidKey, i, offset, k));
                            }
                        }
                        else if (cmd >= 0x80 && cmd <= 0x8F)
                        {
                            lastRest = Utils.FixedRests[cmd - 0x80];
                            if (!EventExists(offset))
                            {
                                AddEvent(new RestCommand { Rest = lastRest });
                            }
                        }
                        else // 0x90-0xFF
                        {
                            // TODO: 0x95 - a rest that may or may not repeat depending on some condition within channels
                            // TODO: 0x9E - may or may not jump somewhere else depending on an unknown structure
                            switch (cmd)
                            {
                                case 0x90:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new RestCommand { Rest = lastRest });
                                    }
                                    break;
                                }
                                case 0x91:
                                {
                                    lastRest = (uint)(lastRest + reader.ReadSByte());
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new RestCommand { Rest = lastRest });
                                    }
                                    break;
                                }
                                case 0x92:
                                {
                                    lastRest = reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new RestCommand { Rest = lastRest });
                                    }
                                    break;
                                }
                                case 0x93:
                                {
                                    lastRest = reader.ReadUInt16();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new RestCommand { Rest = lastRest });
                                    }
                                    break;
                                }
                                case 0x94:
                                {
                                    lastRest = (uint)(reader.ReadByte() | (reader.ReadByte() << 8) | (reader.ReadByte() << 16));
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new RestCommand { Rest = lastRest });
                                    }
                                    break;
                                }
                                case 0x96:
                                case 0x97:
                                case 0x9A:
                                case 0x9B:
                                case 0x9F:
                                case 0xA2:
                                case 0xA3:
                                case 0xA6:
                                case 0xA7:
                                case 0xAD:
                                case 0xAE:
                                case 0xB7:
                                case 0xB8:
                                case 0xB9:
                                case 0xBA:
                                case 0xBB:
                                case 0xBD:
                                case 0xC1:
                                case 0xC2:
                                case 0xC4:
                                case 0xC5:
                                case 0xC6:
                                case 0xC7:
                                case 0xC8:
                                case 0xC9:
                                case 0xCA:
                                case 0xCC:
                                case 0xCD:
                                case 0xCE:
                                case 0xCF:
                                case 0xD9:
                                case 0xDA:
                                case 0xDE:
                                case 0xE6:
                                case 0xEB:
                                case 0xEE:
                                case 0xF4:
                                case 0xF5:
                                case 0xF7:
                                case 0xF9:
                                case 0xFA:
                                case 0xFB:
                                case 0xFC:
                                case 0xFD:
                                case 0xFE:
                                case 0xFF:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new InvalidCommand { Command = cmd });
                                    }
                                    break;
                                }
                                case 0x98:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new FinishCommand());
                                    }
                                    cont = false;
                                    break;
                                }
                                case 0x99:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new LoopStartCommand { Offset = reader.BaseStream.Position });
                                    }
                                    break;
                                }
                                case 0xA0:
                                {
                                    byte octave = reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new OctaveSetCommand { Octave = octave });
                                    }
                                    break;
                                }
                                case 0xA1:
                                {
                                    sbyte change = reader.ReadSByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new OctaveAddCommand { OctaveChange = change });
                                    }
                                    break;
                                }
                                case 0xA4:
                                case 0xA5: // The code for these two is identical
                                {
                                    byte tempoArg = reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new TempoCommand { Command = cmd, Tempo = tempoArg });
                                    }
                                    break;
                                }
                                case 0xAB:
                                {
                                    byte[] bytes = reader.ReadBytes(1);
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new SkipBytesCommand { Command = cmd, SkippedBytes = bytes });
                                    }
                                    break;
                                }
                                case 0xAC:
                                {
                                    byte voice = reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new VoiceCommand { Voice = voice });
                                    }
                                    break;
                                }
                                case 0xCB:
                                case 0xF8:
                                {
                                    byte[] bytes = reader.ReadBytes(2);
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new SkipBytesCommand { Command = cmd, SkippedBytes = bytes });
                                    }
                                    break;
                                }
                                case 0xD7:
                                {
                                    ushort bend = reader.ReadUInt16();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new PitchBendCommand { Bend = bend });
                                    }
                                    break;
                                }
                                case 0xE0:
                                {
                                    byte volume = reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new VolumeCommand { Volume = volume });
                                    }
                                    break;
                                }
                                case 0xE3:
                                {
                                    byte expression = reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new ExpressionCommand { Expression = expression });
                                    }
                                    break;
                                }
                                case 0xE8:
                                {
                                    byte panArg = reader.ReadByte();
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new PanpotCommand { Panpot = (sbyte)(panArg - 0x40) });
                                    }
                                    break;
                                }
                                case 0x9D:
                                case 0xB0:
                                case 0xC0:
                                {
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new UnknownCommand { Command = cmd, Args = Array.Empty<byte>() });
                                    }
                                    break;
                                }
                                case 0x9C:
                                case 0xA9:
                                case 0xAA:
                                case 0xB1:
                                case 0xB2:
                                case 0xB3:
                                case 0xB5:
                                case 0xB6:
                                case 0xBC:
                                case 0xBE:
                                case 0xBF:
                                case 0xC3:
                                case 0xD0:
                                case 0xD1:
                                case 0xD2:
                                case 0xDB:
                                case 0xDF:
                                case 0xE1:
                                case 0xE7:
                                case 0xE9:
                                case 0xEF:
                                case 0xF6:
                                {
                                    byte[] args = reader.ReadBytes(1);
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new UnknownCommand { Command = cmd, Args = args });
                                    }
                                    break;
                                }
                                case 0xA8:
                                case 0xB4:
                                case 0xD3:
                                case 0xD5:
                                case 0xD6:
                                case 0xD8:
                                case 0xF2:
                                {
                                    byte[] args = reader.ReadBytes(2);
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new UnknownCommand { Command = cmd, Args = args });
                                    }
                                    break;
                                }
                                case 0xAF:
                                case 0xD4:
                                case 0xE2:
                                case 0xEA:
                                case 0xF3:
                                {
                                    byte[] args = reader.ReadBytes(3);
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new UnknownCommand { Command = cmd, Args = args });
                                    }
                                    break;
                                }
                                case 0xDD:
                                case 0xE5:
                                case 0xED:
                                case 0xF1:
                                {
                                    byte[] args = reader.ReadBytes(4);
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new UnknownCommand { Command = cmd, Args = args });
                                    }
                                    break;
                                }
                                case 0xDC:
                                case 0xE4:
                                case 0xEC:
                                case 0xF0:
                                {
                                    byte[] args = reader.ReadBytes(5);
                                    if (!EventExists(offset))
                                    {
                                        AddEvent(new UnknownCommand { Command = cmd, Args = args });
                                    }
                                    break;
                                }
                                default: throw new Exception(string.Format(Strings.ErrorDSEMLSSMP2KSDATInvalidCommand, i, offset, cmd));
                            }
                        }
                    }
                    tracks[i] = new Track(i);
                    uint chunkLength = reader.ReadUInt32(startPosition + 0xC);
                    reader.BaseStream.Position += chunkLength;
                    // Align 4
                    while (reader.BaseStream.Position % 4 != 0)
                    {
                        reader.BaseStream.Position++;
                    }
                }
                SetTicks();
            }
        }
        public void SetCurrentPosition(long ticks)
        {
            if (tracks == null)
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
            }
        }
        public void Play()
        {
            if (tracks == null)
            {
                SongEnded?.Invoke();
            }
            else if (State == PlayerState.Playing || State == PlayerState.Paused || State == PlayerState.Stopped)
            {
                Stop();
                InitEmulation();
                State = PlayerState.Playing;
            }
        }
        public void Pause()
        {
            if (State == PlayerState.Playing)
            {
                State = PlayerState.Paused;
            }
            else if (State == PlayerState.Paused || State == PlayerState.Stopped)
            {
                State = PlayerState.Playing;
            }
        }
        public void Stop()
        {
            if (State == PlayerState.Playing || State == PlayerState.Paused)
            {
                State = PlayerState.Stopped;
                for (int i = 0; i < tracks.Length; i++)
                {
                    tracks[i].StopAllChannels();
                }
            }
        }
        public void Dispose()
        {
            if (State == PlayerState.Playing || State == PlayerState.Paused || State == PlayerState.Stopped)
            {
                Stop();
                State = PlayerState.ShutDown;
                thread.Join();
            }
        }
        public void GetSongState(UI.TrackInfoControl.TrackInfo info)
        {
            info.Tempo = tempo;
            // TODO: Longest song is actually 18 tracks (bgm0168)
            for (int i = 0; i < tracks.Length - 1; i++)
            {
                Track track = tracks[i + 1];
                info.Positions[i] = Events[i + 1][track.CurEvent].Offset;
                info.Rests[i] = track.Rest;
                info.Voices[i] = track.Voice;
                info.Types[i] = "PCM";
                info.Volumes[i] = track.Volume;
                info.PitchBends[i] = track.PitchBend;
                info.Extras[i] = track.Octave;
                info.Panpots[i] = track.Panpot;

                Channel[] channels = track.Channels.ToArray();
                if (channels.Length == 0)
                {
                    info.Notes[i] = new byte[0];
                    info.Lefts[i] = 0;
                    info.Rights[i] = 0;
                    //info.Types[i] = string.Empty;
                }
                else
                {
                    float[] lefts = new float[channels.Length];
                    float[] rights = new float[channels.Length];
                    for (int j = 0; j < channels.Length; j++)
                    {
                        Channel c = channels[j];
                        lefts[j] = (float)(-c.Panpot + 0x40) / 0x80 * c.Volume / 0x7F;
                        rights[j] = (float)(c.Panpot + 0x40) / 0x80 * c.Volume / 0x7F;
                    }
                    info.Notes[i] = channels.Where(c => !Utils.IsStateRemovable(c.State)).Select(c => c.Key).ToArray();
                    info.Lefts[i] = lefts.Max();
                    info.Rights[i] = rights.Max();
                    //info.Types[i] = string.Join(", ", channels.Select(c => c.State.ToString()));
                }
            }
        }

        private void ExecuteNext(int trackIndex)
        {
            bool increment = true;
            List<SongEvent> ev = Events[trackIndex];
            Track track = tracks[trackIndex];
            switch (ev[track.CurEvent].Command)
            {
                case ExpressionCommand expression: track.Expression = expression.Expression; break;
                case FinishCommand _:
                {
                    if (track.LoopOffset == -1)
                    {
                        track.Stopped = true;
                    }
                    else
                    {
                        track.CurEvent = ev.FindIndex(c => c.Offset == track.LoopOffset);
                    }
                    increment = false;
                    break;
                }
                case InvalidCommand _: track.Stopped = true; increment = false; break;
                case LoopStartCommand loop: track.LoopOffset = loop.Offset; break;
                case NoteCommand note:
                {
                    Channel channel = mixer.AllocateChannel();
                    channel.Stop();
                    track.Octave += (byte)note.OctaveChange;
                    if (channel.StartPCM(localSWD, masterSWD, track.Voice, note.Key + (12 * track.Octave), note.Duration))
                    {
                        channel.NoteVelocity = note.Velocity;
                        channel.Owner = track;
                        track.Channels.Add(channel);
                    }
                    break;
                }
                case OctaveAddCommand octaveAdd: track.Octave = (byte)(track.Octave + octaveAdd.OctaveChange); break;
                case OctaveSetCommand octaveSet: track.Octave = octaveSet.Octave; break;
                case PanpotCommand panpot: track.Panpot = panpot.Panpot; break;
                case PitchBendCommand bend: track.PitchBend = bend.Bend; break;
                case RestCommand rest: track.Rest = rest.Rest; break;
                case TempoCommand tem: tempo = tem.Tempo; break;
                case VoiceCommand voice: track.Voice = voice.Voice; break;
                case VolumeCommand volume: track.Volume = volume.Volume; break;
            }
            if (increment)
            {
                track.CurEvent++;
            }
        }

        private void Tick()
        {
            time.Start();
            while (State != PlayerState.ShutDown)
            {
                if (State == PlayerState.Playing)
                {
                    while (tempoStack >= 240)
                    {
                        tempoStack -= 240;
                        bool allDone = true;
                        for (int i = 0; i < tracks.Length; i++)
                        {
                            Track track = tracks[i];
                            track.Tick();
                            while (track.Rest == 0 && !track.Stopped)
                            {
                                ExecuteNext(i);
                            }
                            if (i == longestTrack)
                            {
                                if (ElapsedTicks == MaxTicks)
                                {
                                    if (!track.Stopped)
                                    {
                                        ElapsedTicks = Events[i][track.CurEvent].Ticks[0] - track.Rest;
                                        elapsedLoops++;
                                        if (UI.MainForm.Instance.PlaylistPlaying && !fadeOutBegan && elapsedLoops > GlobalConfig.Instance.PlaylistSongLoops)
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
                            if (!track.Stopped || track.Channels.Count != 0)
                            {
                                allDone = false;
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
                    tempoStack += tempo;
                    mixer.ChannelTick();
                    mixer.Process();
                }
                time.Wait();
            }
            time.Stop();
        }
    }
}
