using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Util;
using System.IO;
using System.Linq;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    internal class DSEPlayer : IPlayer
    {
        private readonly DSEMixer mixer;
        private readonly string bgmPath;
        private readonly TimeBarrier time;
        private readonly Thread thread;
        private SWDL masterSWDL;
        private SWDL localSWDL;
        private Track[] tracks;
        private byte tempo;
        private int tempoStack;

        public PlayerState State { get; private set; }
        public event SongEndedEvent SongEnded;

        public DSEPlayer(DSEMixer mixer, string bgmPath)
        {
            this.mixer = mixer;
            this.bgmPath = bgmPath;

            time = new TimeBarrier(192);
            thread = new Thread(Tick) { Name = "DSEPlayer Tick" };
            thread.Start();
        }

        public void LoadSong(long index)
        {
            masterSWDL = new SWDL(Path.Combine(bgmPath, "bgm.swd"));
            localSWDL = new SWDL(Path.Combine(bgmPath, $"bgm{index:D4}.swd"));
            byte[] smdl = File.ReadAllBytes(Path.Combine(bgmPath, $"bgm{index:D4}.smd"));
            using (var reader = new EndianBinaryReader(new MemoryStream(smdl)))
            {
                SMDL.Header header = reader.ReadObject<SMDL.Header>();
                SMDL.ISongChunk songChunk;
                switch (header.Version)
                {
                    case 0x402:
                    {
                        songChunk = reader.ReadObject<SMDL.SongChunk_V402>();
                        break;
                    }
                    case 0x415:
                    {
                        songChunk = reader.ReadObject<SMDL.SongChunk_V415>();
                        break;
                    }
                    default: throw new InvalidDataException();
                }
                tracks = new Track[songChunk.NumTracks];
                for (byte i = 0; i < tracks.Length; i++)
                {
                    tracks[i] = new Track(i, smdl, reader.BaseStream.Position);
                    reader.BaseStream.Position += 0xC;
                    uint chunkLength = reader.ReadUInt32();
                    reader.BaseStream.Position += chunkLength;
                    // Align 4
                    while (reader.BaseStream.Position % 4 != 0)
                    {
                        reader.BaseStream.Position++;
                    }
                }
            }
        }
        public void Play()
        {
            Stop();
            tempoStack = 0;
            tempo = 120;
            for (int i = 0; i < tracks.Length; i++)
            {
                tracks[i].Init();
            }
            State = PlayerState.Playing;
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
            for (int i = 0; i < tracks.Length; i++)
            {
                tracks[i].StopAllChannels();
            }
        }
        public void ShutDown()
        {
            // TODO: Dispose tracks and track readers
            Stop();
            State = PlayerState.ShutDown;
            thread.Join();
        }
        public void GetSongState(UI.TrackInfoControl.TrackInfo info)
        {
            info.Tempo = tempo;
            // TODO: Longest song is actually 18 tracks (bgm0168)
            for (int i = 0; i < tracks.Length - 1; i++)
            {
                Track track = tracks[i + 1];
                info.Positions[i] = track.Reader.BaseStream.Position;
                info.Delays[i] = track.Delay;
                info.Voices[i] = track.Voice;
                //info.Mods[i] = track.LFODepth * track.LFORange;
                info.Types[i] = "PCM";
                info.Volumes[i] = track.Volume;
                //info.Pitches[i] = track.GetPitch();
                info.Extras[i] = track.Octave;
                info.Panpots[i] = track.Panpot;

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
                        lefts[j] = (float)(-c.Panpot + 0x40) / 0x80 * c.Volume / 0x7F;
                        rights[j] = (float)(c.Panpot + 0x40) / 0x80 * c.Volume / 0x7F;
                    }
                    info.Notes[i] = channels.Where(c => c.State != EnvelopeState.Release).Select(c => c.Key).ToArray();
                    info.Lefts[i] = lefts.Max();
                    info.Rights[i] = rights.Max();
                }
            }
        }

        private static readonly byte[] fixedDelays = new byte[0x10]
        {
            96, 72, 64, 48, 36, 32, 24, 18, 16, 12, 9, 8, 6, 4, 3, 2
        };
        private void ExecuteNext(Track track)
        {
            byte cmd = track.Reader.ReadByte();
            if (cmd >= 1 && cmd <= 0x7F) // Notes
            {
                byte arg = track.Reader.ReadByte();
                int numParams = (arg & 0xC0) >> 6;
                int octave = ((arg & 0x30) >> 4) - 2;
                int note = arg & 0xF;
                if (note < 12)
                {
                    uint duration;
                    switch (numParams)
                    {
                        case 0:
                        {

                            duration = track.LastNoteDuration;
                            break;
                        }
                        default: // Big Endian reading of 8, 16, or 24 bits
                        {
                            duration = 0;
                            for (int cntby = 0; cntby < numParams; ++cntby)
                            {
                                duration = (duration << 8) | track.Reader.ReadByte();
                            }
                            track.LastNoteDuration = duration;
                            break;
                        }
                    }
                    Channel channel = mixer.AllocateChannel(track);
                    channel.Stop();
                    track.Octave += (byte)octave;
                    if (channel.StartPCM(localSWDL, masterSWDL, track.Voice, note + (12 * track.Octave), duration))
                    {
                        channel.NoteVelocity = cmd;
                        channel.Owner = track;
                        track.Channels.Add(channel);
                    }
                }
                else
                {
                    ;
                }
                //sb.AppendLine(string.Format("Note: V_{0}, O_{1}, N_{2}, D_{3}", cmd, octave, noteNames[note], duration));
            }
            else if (cmd >= 0x80 && cmd <= 0x8F) // Fixed delays
            {
                track.LastDelay = track.Delay = fixedDelays[cmd - 0x80];
            }
            else // 0, 0x90-0xFF
            {
                switch (cmd)
                {
                    case 0x90: // Repeat last delay
                    {
                        track.Delay = track.LastDelay;
                        break;
                    }
                    case 0x91: // Add to last delay
                    {
                        track.LastDelay = track.Delay = track.LastDelay + track.Reader.ReadByte();
                        break;
                    }
                    case 0x92: // Delay 8bit
                    {
                        track.LastDelay = track.Delay = track.Reader.ReadByte();
                        break;
                    }
                    case 0x93: // Delay 16bit
                    {
                        track.LastDelay = track.Delay = track.Reader.ReadUInt16();
                        break;
                    }
                    case 0x94: // Delay 24bit
                    {
                        track.LastDelay = track.Delay = (uint)(track.Reader.ReadByte() | (track.Reader.ReadByte() << 8) | (track.Reader.ReadByte() << 16));
                        break;
                    }
                    case 0x98: // End
                    {
                        if (track.LoopOffset == -1)
                        {
                            track.Stopped = true;
                        }
                        else
                        {
                            track.Reader.BaseStream.Position = track.LoopOffset;
                        }
                        break;
                    }
                    case 0x99: // Loop Position
                    {
                        track.LoopOffset = track.Reader.BaseStream.Position;
                        break;
                    }
                    case 0xA0: // Set octave
                    {
                        track.Octave = track.Reader.ReadByte();
                        break;
                    }
                    case 0xA4: // Tempo
                    {
                        tempo = track.Reader.ReadByte();
                        break;
                    }
                    case 0xAC: // Program
                    {
                        track.Voice = track.Reader.ReadByte();
                        break;
                    }
                    case 0xD7:
                    {
                        track.Reader.ReadUInt16();
                        //sb.AppendLine(string.Format("Bend: {0}", reader.ReadUInt16()));
                        break;
                    }
                    case 0xE0: // Volume
                    {
                        track.Volume = track.Reader.ReadByte();
                        break;
                    }
                    case 0xE3: // Expression
                    {
                        track.Expression = track.Reader.ReadByte();
                        break;
                    }
                    case 0xE8: // Panpot
                    {
                        track.Panpot = (sbyte)(track.Reader.ReadByte() - 0x40);
                        break;
                    }
                    // Unknown commands with no params
                    case 0x9D: // bgm0113
                    case 0xC0: // bgm0100
                    {
                        break;
                    }
                    // Unknown commands with one param
                    case 0x9C: // bgm0113
                    case 0xA5: // bgm0001
                    case 0xA9: // bgm0000
                    case 0xAA: // bgm0000
                    case 0xB2: // bgm0100
                    case 0xB5: // bgm0100
                    case 0xBE: // bgm0003
                    case 0xBF: // bgm0151
                    case 0xD0: // bgm0100
                    case 0xD1: // bgm0113
                    case 0xD2: // bgm0116
                    case 0xD8: // bgm0000
                    case 0xDB: // bgm0000
                    case 0xF6: // bgm0001
                    {
                        track.Reader.ReadBytes(1);
                        break;
                    }
                    // Unknown commands with two params
                    case 0xA8: // bgm0001
                    case 0xB4: // bgm0180
                    case 0xD6: // bgm0101
                    {
                        track.Reader.ReadBytes(2);
                        break;
                    }
                    // Unknown commands with three params
                    case 0xD4: // bgm0100
                    case 0xE2: // bgm0100
                    case 0xEA: // bgm0100
                    {
                        track.Reader.ReadBytes(3);
                        break;
                    }
                    // Unknown commands with five params
                    case 0xDC: // bgm0182
                    {
                        track.Reader.ReadBytes(5);
                        break;
                    }
                    // Unknown commands with an unknown amount of params
                    default:
                    {
                        break;
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
                        for (int i = 0; i < tracks.Length; i++)
                        {
                            Track track = tracks[i];
                            track.Tick();
                            while (track.Delay == 0 && !track.Stopped)
                            {
                                ExecuteNext(track);
                            }
                            if (!track.Stopped || track.Channels.Count != 0)
                            {
                                allDone = false;
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
