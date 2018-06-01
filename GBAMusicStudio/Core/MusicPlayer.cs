using MicroLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using GBAMusicStudio.Core.M4A;
using static GBAMusicStudio.Core.M4A.M4AStructs;
using GBAMusicStudio.MIDI;
using GBAMusicStudio.Util;

namespace GBAMusicStudio.Core
{
    enum State
    {
        Playing,
        Paused,
        Stopped
    }

    internal static class MusicPlayer
    {
        internal static readonly FMOD.System System;
        static readonly MicroTimer timer;

        static readonly Instrument[] dsInstruments;
        static readonly Instrument[] gbInstruments;
        static readonly Instrument[] allInstruments;

        internal static Dictionary<uint, FMOD.Sound> Sounds { get; private set; }
        internal static readonly uint SQUARE12_ID = 0xFFFFFFFF,
            SQUARE25_ID = SQUARE12_ID - 1,
            SQUARE50_ID = SQUARE25_ID - 1,
            SQUARE75_ID = SQUARE50_ID - 1,
            NOISE0_ID = SQUARE75_ID - 1,
            NOISE1_ID = NOISE0_ID - 1;

        static ushort tempo;
        static readonly Track[] tracks;

        static SongHeader header;
        internal static byte NumTracks { get => header.NumTracks; }
        internal static VoiceTable VoiceTable { get; private set; }

        static MusicPlayer()
        {
            FMOD.Factory.System_Create(out System);
            System.init(32, FMOD.INITFLAGS.NORMAL, (IntPtr)0);

            dsInstruments = new Instrument[Config.DirectCount];
            gbInstruments = new Instrument[4];
            for (int i = 0; i < dsInstruments.Length; i++)
                dsInstruments[i] = new Instrument();
            for (int i = 0; i < 4; i++)
                gbInstruments[i] = new Instrument();
            allInstruments = dsInstruments.Union(gbInstruments).ToArray();

            tracks = new Track[16];
            for (byte i = 0; i < 16; i++)
                tracks[i] = new Track(i);

            ClearSamples();

            timer = new MicroTimer();
            timer.MicroTimerElapsed += PlayLoop;
        }

        internal static void ClearSamples()
        {
            if (Sounds != null)
            {
                foreach (var s in Sounds.Values)
                    s.release();
                Sounds.Clear();
            }
            else
            {
                Sounds = new Dictionary<uint, FMOD.Sound>();
            }
            PSGSquare();
            PSGNoise();
        }
        static void PSGSquare()
        {
            byte[] simple = { 1, 2, 4, 6 };
            uint len = 0x100;
            var buf = new byte[len];

            for (uint i = 0; i < 4; i++) // Squares
            {
                for (int j = 0; j < len; j++)
                    buf[j] = (byte)(j < simple[i] * 0x20 ? 0xF * Config.PSGVolume : 0x0);
                var ex = new FMOD.CREATESOUNDEXINFO()
                {
                    defaultfrequency = 112640,
                    format = FMOD.SOUND_FORMAT.PCM8,
                    length = len,
                    numchannels = 1
                };
                System.createSound(buf, FMOD.MODE.OPENMEMORY | FMOD.MODE.OPENRAW | FMOD.MODE.LOOP_NORMAL | FMOD.MODE.LOWMEM, ref ex, out FMOD.Sound snd);
                Sounds.Add(SQUARE12_ID - i, snd);
            }
        }
        static void PSGNoise()
        {
            uint[] simple = { 32768, 256 };
            var rand = new Random();

            for (uint i = 0; i < 2; i++)
            {
                uint len = simple[i];
                var buf = new byte[len];

                for (int j = 0; j < len; j++)
                    buf[j] = (byte)rand.Next(0xF * Config.PSGVolume);

                var ex = new FMOD.CREATESOUNDEXINFO()
                {
                    defaultfrequency = 4096,
                    format = FMOD.SOUND_FORMAT.PCM8,
                    length = len,
                    numchannels = 1
                };
                System.createSound(buf, FMOD.MODE.OPENMEMORY | FMOD.MODE.OPENRAW | FMOD.MODE.LOOP_NORMAL | FMOD.MODE.LOWMEM, ref ex, out FMOD.Sound snd);
                Sounds.Add(NOISE0_ID - i, snd);
            }
        }

        internal static State State { get; private set; }
        internal delegate void SongEndedEvent();
        internal static event SongEndedEvent SongEnded;

        internal static void SetVolume(float v)
        {
            System.getMasterSoundGroup(out FMOD.SoundGroup parentGroup);
            parentGroup.setVolume(v);
        }
        internal static void SetTempo(ushort t)
        {
            if (t > 510) return;
            tempo = t;
            timer.Interval = (long)(2.5 * 1000 * 1000) / t;
        }
        internal static void SetMute(int i, bool m) => tracks[i].Group.setMute(m);

        internal static void LoadSong(ushort song)
        {
            Stop();
            header = ROM.Instance.ReadStruct<SongHeader>(ROM.Instance.ReadPointer(ROM.Instance.Game.SongTable + ((uint)8 * song)));
            Array.Resize(ref header.Tracks, header.NumTracks); // Not really necessary

            VoiceTable = new VoiceTable();
            VoiceTable.Load(header.VoiceTable);
            new VoiceTableSaver(); // Testing

            MIDIKeyboard.Start();
        }
        internal static void Play()
        {
            Stop();

            if (header.NumTracks == 0 || header.NumTracks > 16) // Maybe header.isvalid or something
            {
                SongEnded?.Invoke();
                return;
            }

            for (int i = 0; i < header.NumTracks; i++)
                tracks[i].Init(header.Tracks[i]);

            SetTempo(120);
            timer.Start();
            State = State.Playing;
        }
        internal static void Pause()
        {
            if (State == State.Paused)
            {
                timer.Start();
                State = State.Playing;
            }
            else
            {
                Stop();
                State = State.Paused;
            }
        }
        internal static void Stop()
        {
            timer.StopAndWait();
            foreach (Instrument i in allInstruments)
                i.Stop();
            State = State.Stopped;
        }
        static void PlayLoop(object sender, MicroTimerEventArgs e)
        {
            bool allStopped = true;
            for (int i = header.NumTracks - 1; i >= 0; i--)
            {
                Track track = tracks[i];
                if (!track.Stopped || track.Instruments.Any(ins => ins.State != ADSRState.Dead))
                    allStopped = false;
                while (track.Delay == 0 && !track.Stopped)
                    ExecuteNext(track);
                track.Tick();
            }
            System.update();
            if (allStopped)
            {
                Stop();
                SongEnded?.Invoke();
            }
        }

        internal static (ushort, uint[], byte[], byte[], byte[][], float[], byte[], byte[], int[], float[], string[]) GetSongState()
        {
            var positions = new uint[header.NumTracks];
            var volumes = new byte[header.NumTracks];
            var delays = new byte[header.NumTracks];
            var notes = new byte[header.NumTracks][];
            var velocities = new float[header.NumTracks];
            var voices = new byte[header.NumTracks];
            var modulations = new byte[header.NumTracks];
            var bends = new int[header.NumTracks];
            var pans = new float[header.NumTracks];
            var types = new string[header.NumTracks];
            for (int i = 0; i < header.NumTracks; i++)
            {
                positions[i] = tracks[i].Position;
                volumes[i] = tracks[i].Volume;
                delays[i] = tracks[i].Delay;
                voices[i] = tracks[i].Voice;
                modulations[i] = tracks[i].MODDepth;
                bends[i] = tracks[i].Bend * tracks[i].BendRange;
                types[i] = VoiceTable[tracks[i].Voice].ToString();

                Instrument[] instruments = tracks[i].Instruments.Clone().ToArray();
                bool none = instruments.Length == 0;
                Instrument loudest = none ? null : instruments.OrderByDescending(ins => ins.Velocity).ElementAt(0);
                pans[i] = none ? tracks[i].Pan / 64f : loudest.Panpot;
                notes[i] = none ? new byte[0] : instruments.Where(ins => ins.State != ADSRState.Releasing && ins.State != ADSRState.Dead).Select(ins => ins.DisplayNote).Distinct().ToArray();
                velocities[i] = none ? 0 : loudest.Velocity * (volumes[i] / 127f);
            }
            return (tempo, positions, volumes, delays, notes, velocities, voices, modulations, bends, pans, types);
        }

        static byte WaitFromCMD(byte startCMD, byte cmd)
        {
            byte[] added = { 4, 4, 2, 2 };
            byte wait = (byte)(cmd - startCMD);
            byte add = wait > 24 ? (byte)24 : wait;
            for (int i = 24 + 1; i <= wait; i++)
                add += added[i % 4];
            return add;
        }
        static void PlayNote(Track track, byte note, byte velocity, byte addedDelay = 0)
        {
            note = (byte)(note + track.KeyShift).Clamp(0, 127);
            track.PrevNote = note;
            track.PrevVelocity = velocity;

            if (!track.Ready) return;

            Voice voice = VoiceTable.GetVoiceFromNote(track.Voice, note, out byte forcedNote);

            Instrument instrument = null;
            switch (voice.Type)
            {
                case 0x0:
                case 0x8:
                    var byAge = dsInstruments.OrderByDescending(ins => (ins.Track == null ? 16 : ins.Track.Index)).ThenByDescending(ins => ins.Age);
                    foreach (Instrument i in byAge) // Find free
                        if (i.State == ADSRState.Dead)
                        {
                            instrument = i;
                            break;
                        }
                    if (instrument == null) // Find prioritized
                        foreach (Instrument i in byAge)
                            if (track.Priority > i.Track.Priority)
                            {
                                instrument = i;
                                break;
                            }
                    if (instrument == null) // Find releasing
                        foreach (Instrument i in byAge)
                            if (i.State == ADSRState.Releasing)
                            {
                                instrument = i;
                                break;
                            }
                    if (instrument == null) // None available
                    {
                        var lowestOldest = byAge.First(); // Kill lowest track's oldest instrument if the track is lower than this one
                        if (lowestOldest.Track.Index >= track.Index)
                            instrument = lowestOldest;
                    }
                    break;
                case 0x1:
                case 0x9:
                    instrument = gbInstruments[0];
                    break;
                case 0x2:
                case 0xA:
                    instrument = gbInstruments[1];
                    break;
                case 0x3:
                case 0xB:
                    instrument = gbInstruments[2];
                    break;
                case 0x4:
                case 0xC:
                    instrument = gbInstruments[3];
                    break;
            }

            if (instrument != null)
                instrument.Play(track, note, track.RunCmd == 0xCF ? (byte)0xFF : WaitFromCMD(0xD0, track.RunCmd));
        }

        static void ExecuteNext(Track track)
        {
            byte cmd = track.ReadByte();
            if (cmd >= 0xBD) // Commands that work within running status
                track.RunCmd = cmd;

            #region TIE & Notes

            if (track.RunCmd >= 0xCF && cmd < 0x80) // Within running status
            {
                var o = track.Position;
                byte peek1 = track.ReadByte(),
                    peek2 = track.ReadByte();
                track.SetOffset(o);
                if (peek1 >= 128) PlayNote(track, cmd, track.PrevVelocity);
                else if (peek2 >= 128) PlayNote(track, cmd, track.ReadByte());
                else PlayNote(track, cmd, track.ReadByte(), track.ReadByte());
            }
            else if (cmd >= 0xCF)
            {
                var o = track.Position;
                byte peek1 = track.ReadByte(),
                    peek2 = track.ReadByte(),
                    peek3 = track.ReadByte();
                track.SetOffset(o);
                if (peek1 >= 128) PlayNote(track, track.PrevNote, track.PrevVelocity);
                else if (peek2 >= 128) PlayNote(track, track.ReadByte(), track.PrevVelocity);
                else if (peek3 >= 128) PlayNote(track, track.ReadByte(), track.ReadByte());
                else PlayNote(track, track.ReadByte(), track.ReadByte(), track.ReadByte());
            }

            #endregion

            #region Waits

            else if (cmd >= 0x80 && cmd <= 0xB0)
            {
                track.Delay = WaitFromCMD(0x80, cmd);
            }

            #endregion

            #region Commands

            else if (track.RunCmd < 0xCF && cmd < 0x80) // Commands within running status
            {
                switch (track.RunCmd)
                {
                    case 0xBD: track.SetVoice(cmd); break; // VOICE
                    case 0xBE: track.SetVolume(cmd); break; // VOL
                    case 0xBF: track.SetPan(cmd); break; // PAN
                    case 0xC0: track.SetBend(cmd); break; // BEND
                    case 0xC1: track.SetBendRange(cmd); break; // BENDR
                    case 0xC2: track.SetLFOSpeed(cmd); break; // LFOS
                    case 0xC3: track.SetLFODelay(cmd); break; // LFODL
                    case 0xC4: track.SetMODDepth(cmd); break; // MOD
                    case 0xC5: track.SetMODType(cmd); break; // MODT
                    case 0xC8: track.SetTune(cmd); break; // TUNE
                    case 0xCD: track.ReadByte(); break; // XCMD
                    case 0xCE:
                        byte note = (byte)(cmd + track.KeyShift).Clamp(0, 127);
                        track.Instruments.First(ins => ins.NoteDuration == 0xFF && ins.DisplayNote == note).State = ADSRState.Releasing;
                        track.PrevNote = note;
                        break; // EOT
                }
            }
            else if (cmd > 0xB0 && cmd < 0xCF)
            {
                switch (cmd)
                {
                    case 0xB1: track.Stopped = true; break; // FINE
                    case 0xB2: track.SetOffset(track.ReadPointer()); break; // GOTO
                    case 0xB3: // PATT
                        uint jump = track.ReadPointer();
                        track.EndOfPattern = track.Position;
                        track.SetOffset(jump);
                        break;
                    case 0xB4: // PEND
                        if (track.EndOfPattern != 0)
                        {
                            track.SetOffset(track.EndOfPattern);
                            track.EndOfPattern = 0;
                        }
                        break;
                    case 0xB5: track.ReadByte(); break; // REPT
                    case 0xB9: track.ReadByte(); track.ReadByte(); track.ReadByte(); break; // MEMACC
                    case 0xBA: track.SetPriority(track.ReadByte()); break; // PRIO
                    case 0xBB: SetTempo((ushort)(track.ReadByte() * 2)); break; // TEMPO
                    case 0xBC: track.KeyShift = track.ReadSByte(); break; // KEYSH
                                                        // Commands that work within running status:
                    case 0xBD: track.SetVoice(track.ReadByte()); break; // VOICE
                    case 0xBE: track.SetVolume(track.ReadByte()); break; // VOL
                    case 0xBF: track.SetPan(track.ReadByte()); break; // PAN
                    case 0xC0: track.SetBend(track.ReadByte()); break; // BEND
                    case 0xC1: track.SetBendRange(track.ReadByte()); break; // BENDR
                    case 0xC2: track.SetLFOSpeed(track.ReadByte()); break; // LFOS
                    case 0xC3: track.SetLFODelay(track.ReadByte()); break; // LFODL
                    case 0xC4: track.SetMODDepth(track.ReadByte()); break; // MOD
                    case 0xC5: track.SetMODType(track.ReadByte()); break; // MODT
                    case 0xC8: track.SetTune(track.ReadByte()); break; // TUNE
                    case 0xCD: track.ReadByte(); track.ReadByte(); break; // XCMD
                    case 0xCE: // EOT
                        if (track.PeekByte() < 128)
                        {
                            byte note = (byte)(track.ReadByte() + track.KeyShift).Clamp(0, 127);
                            track.Instruments.First(ins => ins.NoteDuration == 0xFF && ins.DisplayNote == note).State = ADSRState.Releasing;
                            track.PrevNote = note;
                        }
                        else
                        {
                            track.Instruments.First(ins => ins.NoteDuration == 0xFF).State = ADSRState.Releasing;
                        }
                        break;
                    default: Console.WriteLine("Invalid command: 0x{0:X} = {1}", track.Position, cmd); break;
                }
            }

            #endregion
        }
    }
}
