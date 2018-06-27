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

    internal static class SongPlayer
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
        static uint position;
        static readonly Track[] tracks;

        internal static Song Song { get; private set; }
        internal static VoiceTable VoiceTable;
        internal static int NumTracks => Song == null ? 0 : Song.NumTracks;

        static SongPlayer()
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

            ClearVoices();

            timer = new MicroTimer();
            timer.MicroTimerElapsed += PlayLoop;
        }

        internal static void ClearVoices()
        {
            if (Sounds != null)
            {
                foreach (var s in Sounds.Values)
                    s.release();
                Sounds.Clear();
                VoiceTable = null;
                SMulti.LoadedMultis.Clear();
                SDrum.LoadedDrums.Clear();
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
        internal static void SetPosition(uint p)
        {
            bool pause = State == State.Playing;
            if (pause) Pause();
            position = p;
            for (int i = Song.NumTracks - 1; i >= 0; i--)
            {
                var track = tracks[i];
                track.Init();
                uint elapsed = 0;
                while (!track.Stopped)
                {
                    ExecuteNext(i);
                    // elapsed == 400, delay == 4, p == 402
                    if (elapsed <= p && elapsed + track.Delay > p)
                    {
                        track.Delay -= (byte)(p - elapsed);
                        foreach (var ins in track.Instruments)
                            ins.Stop();
                        break;
                    }
                    elapsed += track.Delay;
                    track.Delay = 0;
                }
            }
            if (pause) Pause();
        }

        internal static void LoadROMSong(ushort num, byte table)
        {
            Song = new ROMSong(num, table);

            MIDIKeyboard.Start();
        }
        internal static void LoadASMSong(Assembler assembler, string headerLabel)
        {
            Song = new ASMSong(assembler, headerLabel);
        }

        internal static void Play()
        {
            Stop();

            if (Song.NumTracks == 0 || Song.NumTracks > 16) // Maybe header.isvalid or something
            {
                SongEnded?.Invoke();
                return;
            }

            for (int i = 0; i < NumTracks; i++)
                tracks[i].Init();

            position = 0;
            SetTempo(150);
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

        internal static (ushort, uint, uint[], byte[], byte[], byte[][], float[], byte[], byte[], int[], float[], string[]) GetSongState()
        {
            var offsets = new uint[Song.NumTracks];
            var volumes = new byte[Song.NumTracks];
            var delays = new byte[Song.NumTracks];
            var notes = new byte[Song.NumTracks][];
            var velocities = new float[Song.NumTracks];
            var voices = new byte[Song.NumTracks];
            var modulations = new byte[Song.NumTracks];
            var bends = new int[Song.NumTracks];
            var pans = new float[Song.NumTracks];
            var types = new string[Song.NumTracks];
            for (int i = 0; i < Song.NumTracks; i++)
            {
                offsets[i] = Song.Commands[i][tracks[i].CommandIndex].Offset;
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
            return (tempo, position, offsets, volumes, delays, notes, velocities, voices, modulations, bends, pans, types);
        }

        static void PlayInstrument(Track track, byte note, byte velocity, byte duration)
        {
            note = (byte)(note + track.KeyShift).Clamp(0, 127);

            if (!track.Ready) return;

            Voice voice = VoiceTable.GetVoiceFromNote(track.Voice, note, out bool fromDrum);

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
                instrument.Play(track, note, velocity, duration);
        }

        static void ExecuteNext(int i)
        {
            var track = tracks[i];
            var cmd = Song.Commands[i][track.CommandIndex];

            switch (cmd.Command)
            {
                case Command.GoTo:
                    int gotoCmd = Song.Commands[i].FindIndex(c => c.Offset == cmd.Arguments[0]);
                    position = Song.Commands[i][gotoCmd].AbsoluteTicks - 1;
                    track.CommandIndex = gotoCmd - 1; // -1 for incoming ++
                    break;
                case Command.PATT:
                    int jumpCmd = Song.Commands[i].FindIndex(c => c.Offset == cmd.Arguments[0]);
                    track.EndOfPattern = track.CommandIndex;
                    track.CommandIndex = jumpCmd - 1; // -1 for incoming ++
                    break;
                case Command.PEND:
                    if (track.EndOfPattern != 0)
                    {
                        track.CommandIndex = track.EndOfPattern;
                        track.EndOfPattern = 0;
                    }
                    break;
                case Command.Finish: track.Stopped = true; break;
                case Command.Priority: track.SetPriority((byte)cmd.Arguments[0]); break;
                case Command.Tempo: SetTempo((ushort)cmd.Arguments[0]); break;
                case Command.KeyShift: track.KeyShift = (sbyte)cmd.Arguments[0]; break;
                case Command.NoteOn: PlayInstrument(track, (byte)cmd.Arguments[0], (byte)cmd.Arguments[1], (byte)cmd.Arguments[2]); break;
                case Command.Rest: track.Delay = (byte)cmd.Arguments[0]; break;
                case Command.Voice: track.SetVoice((byte)cmd.Arguments[0]); break;
                case Command.Volume: track.SetVolume((byte)cmd.Arguments[0]); break;
                case Command.Panpot: track.SetPan((sbyte)cmd.Arguments[0]); break;
                case Command.Bend: track.SetBend((sbyte)cmd.Arguments[0]); break;
                case Command.BendRange: track.SetBendRange((byte)cmd.Arguments[0]); break;
                case Command.LFOSpeed: track.SetLFOSpeed((byte)cmd.Arguments[0]); break;
                case Command.LFODelay: track.SetLFODelay((byte)cmd.Arguments[0]); break;
                case Command.MODDepth: track.SetMODDepth((byte)cmd.Arguments[0]); break;
                case Command.MODType: track.SetMODType((byte)cmd.Arguments[0]); break;
                case Command.Tune: track.SetTune((sbyte)cmd.Arguments[0]); break;
                case Command.EndOfTie:
                    int which = cmd.Arguments[0];
                    Instrument ins = null;
                    if (which == -1)
                        ins = track.Instruments.LastOrDefault(inst => inst.NoteDuration == 0xFF && inst.State != ADSRState.Releasing);
                    else
                    {
                        byte note = (byte)(which + track.KeyShift).Clamp(0, 127);
                        ins = track.Instruments.LastOrDefault(inst => inst.NoteDuration == 0xFF && inst.DisplayNote == note && inst.State != ADSRState.Releasing);
                    }
                    if (ins != null)
                        ins.State = ADSRState.Releasing;
                    break;
            }

            if (!track.Stopped)
                track.CommandIndex++;
        }
        static void PlayLoop(object sender, MicroTimerEventArgs e)
        {
            bool allDone = true;
            for (int i = Song.NumTracks - 1; i >= 0; i--)
            {
                Track track = tracks[i];
                if (!track.Stopped || track.Instruments.Any(ins => ins.State != ADSRState.Dead))
                    allDone = false;
                while (track.Delay == 0 && !track.Stopped)
                    ExecuteNext(i);
                track.Tick();
            }
            position++;
            System.update();
            if (allDone)
            {
                Stop();
                SongEnded?.Invoke();
            }
        }
    }
}
