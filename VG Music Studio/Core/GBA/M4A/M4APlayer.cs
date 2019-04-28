using Kermalis.VGMusicStudio.Util;
using System.Linq;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core.GBA.M4A
{
    internal class M4APlayer : IPlayer
    {
        private readonly M4AMixer mixer;
        private readonly M4AConfig config;
        private readonly TimeBarrier time;
        private readonly Thread thread;
        private int voiceTableOffset;
        private Track[] tracks;
        private ushort tempo;
        private int tempoStack;

        public PlayerState State { get; private set; }
        public event SongEndedEvent SongEnded;

        public M4APlayer(M4AMixer mixer, M4AConfig config)
        {
            this.mixer = mixer;
            this.config = config;

            time = new TimeBarrier(GBAUtils.AGB_FPS);
            thread = new Thread(Tick) { Name = "M4APlayer Tick" };
            thread.Start();
        }

        public void LoadSong(long index)
        {
            SongEntry entry = config.Reader.ReadObject<SongEntry>(config.SongTableOffsets[0] + (index * 8));
            SongHeader header = config.Reader.ReadObject<SongHeader>(entry.HeaderOffset - GBAUtils.CartridgeOffset);
            voiceTableOffset = header.VoiceTableOffset - GBAUtils.CartridgeOffset;
            tracks = new Track[header.NumTracks];
            for (byte i = 0; i < tracks.Length; i++)
            {
                tracks[i] = new Track(i, config.ROM, header.TrackOffsets[i] - GBAUtils.CartridgeOffset);
            }
        }
        public void Play()
        {
            Stop();
            tempoStack = 0;
            tempo = 150;
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
            for (int i = 0; i < tracks.Length; i++)
            {
                Track track = tracks[i];
                info.Positions[i] = track.Reader.BaseStream.Position;
                info.Delays[i] = track.Delay;
                info.Voices[i] = track.Voice;
                info.Mods[i] = track.LFODepth;
                //info.Types[i] = "PCM";
                info.Volumes[i] = track.GetVolume();
                info.Pitches[i] = track.GetPitch();
                info.Panpots[i] = track.GetPanpot();

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
                        ChannelVolume vol = channels[j].GetVolume();
                        lefts[j] = vol.LeftVol;
                        rights[j] = vol.RightVol;
                    }
                    info.Notes[i] = channels.Where(c => c.State < EnvelopeState.Releasing).Select(c => c.Note.OriginalKey).ToArray();
                    info.Lefts[i] = lefts.Max();
                    info.Rights[i] = rights.Max();
                }
            }
        }

        private void PlayNote(Track track, byte key, byte velocity, byte addedDuration)
        {
            key = (byte)(key + track.Transpose).Clamp(0, 0x7F);
            track.PrevKey = key;
            track.PrevVelocity = velocity;
            if (track.Ready)
            {
                bool fromDrum = false;
                int offset = voiceTableOffset + (track.Voice * 12);
                while (true)
                {
                    VoiceEntry v = config.Reader.ReadObject<VoiceEntry>(offset);
                    if (v.Type == (int)VoiceFlags.KeySplit)
                    {
                        fromDrum = false; // In case there is a multi within a drum
                        byte inst = config.Reader.ReadByte(v.Int8 - GBAUtils.CartridgeOffset + key);
                        offset = v.Int4 - GBAUtils.CartridgeOffset + (inst * 12);
                    }
                    else if (v.Type == (int)VoiceFlags.Drum)
                    {
                        fromDrum = true;
                        offset = v.Int4 - GBAUtils.CartridgeOffset + (key * 12);
                    }
                    else
                    {
                        var note = new Note
                        {
                            Duration = track.RunCmd == 0xCF ? -1 : M4AUtils.ClockTable[track.RunCmd - 0xCF] + addedDuration, // TIE gets -1 duration
                            Velocity = velocity,
                            OriginalKey = key,
                            Key = fromDrum ? v.RootKey : key
                        };
                        var type = (VoiceType)(v.Type & 0x7);
                        switch (type)
                        {
                            case VoiceType.PCM8:
                            {
                                bool bFixed = (v.Type & (int)VoiceFlags.Fixed) == (int)VoiceFlags.Fixed;
                                bool bCompressed = false;//ROM.Instance.Game.Engine.HasPokemonCompression && (v.Type & (int)VoiceFlags.Compressed) == (int)VoiceFlags.Compressed;
                                mixer.AllocPCM8Channel(track, v.ADSR, note,
                                    track.GetVolume(), track.GetPanpot(), track.GetPitch(),
                                    bFixed, bCompressed, v.Int4 - GBAUtils.CartridgeOffset);
                                return;
                            }
                            case VoiceType.Square1:
                            case VoiceType.Square2:
                            {
                                mixer.AllocPSGChannel(track, v.ADSR, note,
                                    track.GetVolume(), track.GetPanpot(), track.GetPitch(),
                                    type, (SquarePattern)v.Int4);
                                return;
                            }
                            case VoiceType.PCM4:
                            {
                                mixer.AllocPSGChannel(track, v.ADSR, note,
                                    track.GetVolume(), track.GetPanpot(), track.GetPitch(),
                                    type, v.Int4 - GBAUtils.CartridgeOffset);
                                return;
                            }
                            case VoiceType.Noise:
                            {
                                mixer.AllocPSGChannel(track, v.ADSR, note,
                                    track.GetVolume(), track.GetPanpot(), track.GetPitch(),
                                    type, (NoisePattern)v.Int4);
                                return;
                            }
                        }
                    }
                }
            }
        }
        private void ExecuteNext(Track track, ref bool update)
        {
            byte cmd = track.Reader.ReadByte();
            if (cmd >= 0xBD) // Commands that work within running status
            {
                track.RunCmd = cmd;
            }
            #region TIE & Notes

            if (track.RunCmd >= 0xCF && cmd <= 0x7F) // Within running status
            {
                byte[] peek = track.Reader.PeekBytes(2);
                if (peek[0] > 0x7F)
                {
                    PlayNote(track, cmd, track.PrevVelocity, 0);
                }
                else if (peek[1] > 3)
                {
                    PlayNote(track, cmd, track.Reader.ReadByte(), 0);
                }
                else
                {
                    PlayNote(track, cmd, track.Reader.ReadByte(), track.Reader.ReadByte());
                }
            }
            else if (cmd >= 0xCF)
            {
                byte[] peek = track.Reader.PeekBytes(3);
                if (peek[0] > 0x7F)
                {
                    PlayNote(track, track.PrevKey, track.PrevVelocity, 0);
                }
                else if (peek[1] > 0x7F)
                {
                    PlayNote(track, track.Reader.ReadByte(), track.PrevVelocity, 0);
                }
                // TIE (0xCF) cannot have an added duration so it needs to stop here
                else if (cmd == 0xCF || peek[2] > 3)
                {
                    PlayNote(track, track.Reader.ReadByte(), track.Reader.ReadByte(), 0);
                }
                else
                {
                    PlayNote(track, track.Reader.ReadByte(), track.Reader.ReadByte(), track.Reader.ReadByte());
                }
            }

            #endregion

            #region Rests

            else if (cmd >= 0x80 && cmd <= 0xB0)
            {
                track.Delay = M4AUtils.ClockTable[cmd - 0x80];
            }

            #endregion

            #region Commands

            else if (track.RunCmd < 0xCF && cmd <= 0x7F) // Commands within running status
            {
                switch (track.RunCmd)
                {
                    case 0xBD: track.Voice = cmd; break;
                    case 0xBE: track.Volume = cmd; update = true; break;
                    case 0xBF: track.Panpot = (sbyte)(cmd - 0x40); update = true; break;
                    case 0xC0: track.Bend = (sbyte)(cmd - 0x40); update = true; break;
                    case 0xC1: track.BendRange = cmd; update = true; break;
                    case 0xC2: track.LFOSpeed = cmd; update = true; break;
                    case 0xC3: track.LFODelay = cmd; update = true; break;
                    case 0xC4: track.LFODepth = cmd; update = true; break;
                    case 0xC5: track.LFOType = (LFOType)cmd; update = true; break;
                    case 0xC8: track.Tune = (sbyte)(cmd - 0x40); update = true; break;
                    case 0xCD: track.Reader.ReadByte(); break; // Argument
                    case 0xCE:
                    {
                        track.ReleaseChannels(track.PrevKey = (byte)(cmd + track.Transpose).Clamp(0, 0x7F));
                        break;
                    }
                    default: // Invalid Command
                    {
                        break;
                    }
                }
            }
            else if (cmd > 0xB0 && cmd < 0xCF)
            {
                switch (cmd)
                {
                    case 0xB1: // FINE & PREV
                    case 0xB6:
                    {
                        track.Stopped = true;
                        // track.ReleaseAllTieingChannels();
                        break;
                    }
                    case 0xB2: track.Reader.BaseStream.Position = track.Reader.ReadInt32() - GBAUtils.CartridgeOffset; break;
                    case 0xB3:
                    {
                        int jump = track.Reader.ReadInt32() - GBAUtils.CartridgeOffset;
                        track.EndOfPattern = track.Reader.BaseStream.Position;
                        track.Reader.BaseStream.Position = jump;
                        break;
                    }
                    case 0xB4:
                    {
                        if (track.EndOfPattern != -1)
                        {
                            track.Reader.BaseStream.Position = track.EndOfPattern;
                            track.EndOfPattern = -1;
                        }
                        break;
                    }
                    case 0xB5: track.Reader.ReadBytes(2); break; // Times, Offset
                    case 0xB9: track.Reader.ReadBytes(3); break; // Operator, Address, Data
                    case 0xBA: track.Priority = track.Reader.ReadByte(); break;
                    case 0xBB: tempo = (ushort)(track.Reader.ReadByte() * 2); break;
                    case 0xBC: track.Transpose = track.Reader.ReadSByte(); break;
                    // Commands that work within running status:
                    case 0xBD: track.Voice = track.Reader.ReadByte(); track.Ready = true; break;
                    case 0xBE: track.Volume = track.Reader.ReadByte(); update = true; break;
                    case 0xBF: track.Panpot = (sbyte)(track.Reader.ReadByte() - 0x40); update = true; break;
                    case 0xC0: track.Bend = (sbyte)(track.Reader.ReadByte() - 0x40); update = true; break;
                    case 0xC1: track.BendRange = track.Reader.ReadByte(); update = true; break;
                    case 0xC2: track.LFOSpeed = track.Reader.ReadByte(); update = true; break;
                    case 0xC3: track.LFODelay = track.Reader.ReadByte(); update = true; break;
                    case 0xC4: track.LFODepth = track.Reader.ReadByte(); update = true; break;
                    case 0xC5: track.LFOType = (LFOType)track.Reader.ReadByte(); update = true; break;
                    case 0xC8: track.Tune = (sbyte)(track.Reader.ReadByte() - 0x40); update = true; break;
                    case 0xCD: track.Reader.ReadBytes(2); break; // Command, Argument
                    case 0xCE:
                    {
                        if (track.Reader.PeekByte() <= 0x7F)
                        {
                            track.ReleaseChannels(track.PrevKey = (byte)(track.Reader.ReadByte() + track.Transpose).Clamp(0, 0x7F));
                        }
                        else
                        {
                            track.ReleaseChannels(track.PrevKey);
                        }
                        break;
                    }
                    default: // Invalid Command
                    {
                        break;
                    }
                }
            }

            #endregion
        }

        private void Tick()
        {
            time.Start();
            while (State != PlayerState.ShutDown)
            {
                if (State == PlayerState.Playing)
                {
                    tempoStack += tempo;
                    while (tempoStack >= 150)
                    {
                        tempoStack -= 150;
                        bool allDone = true;
                        for (int i = 0; i < tracks.Length; i++)
                        {
                            Track track = tracks[i];
                            track.Tick();
                            bool update = false;
                            while (track.Delay == 0 && !track.Stopped)
                            {
                                ExecuteNext(track, ref update);
                            }
                            if (update || track.LFODepth > 0)
                            {
                                track.UpdateChannels();
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
                    mixer.Process();
                }
                time.Wait();
            }
            time.Stop();
        }
    }
}
