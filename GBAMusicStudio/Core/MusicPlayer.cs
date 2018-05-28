using MicroLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using GBAMusicStudio.Core.M4A;
using static GBAMusicStudio.Core.M4A.M4AStructs;
using GBAMusicStudio.MIDI;

namespace GBAMusicStudio.Core
{
    enum State
    {
        Playing,
        Paused,
        Stopped
    }

    internal class MusicPlayer
    {
        FMOD.System system;
        MicroTimer timer;

        Instrument[] dsInstruments;
        Instrument[] gbInstruments;
        Instrument[] allInstruments;

        Dictionary<uint, FMOD.Sound> sounds;
        internal static readonly uint SQUARE12_ID = 0xFFFFFFFF,
            SQUARE25_ID = SQUARE12_ID - 1,
            SQUARE50_ID = SQUARE25_ID - 1,
            SQUARE75_ID = SQUARE50_ID - 1,
            NOISE0_ID = SQUARE75_ID - 1,
            NOISE1_ID = NOISE0_ID - 1;

        ushort tempo;
        Track[] tracks;

        internal static MusicPlayer Instance { get; private set; }
        internal MusicPlayer()
        {
            if (Instance != null) return;
            Instance = this;
            FMOD.Factory.System_Create(out system);
            system.init(32, FMOD.INITFLAGS.NORMAL, (IntPtr)0);

            dsInstruments = new Instrument[Config.DirectCount];
            gbInstruments = new Instrument[4];
            for (int i = 0; i < dsInstruments.Length; i++)
                dsInstruments[i] = new Instrument();
            for (int i = 0; i < 4; i++)
                gbInstruments[i] = new Instrument();
            allInstruments = dsInstruments.Union(gbInstruments).ToArray();

            tracks = new Track[16];
            for (int i = 0; i < 16; i++)
                tracks[i] = new Track(system);

            ClearSamples();

            timer = new MicroTimer();
            timer.MicroTimerElapsed += PlayLoop;

            MIDIKeyboard.Instance.AddHandler(HandleChannelMessageReceived);
        }
        internal void ClearSamples()
        {
            if (sounds != null)
            {
                foreach (var s in sounds.Values)
                    s.release();
                sounds.Clear();
            }
            else
            {
                sounds = new Dictionary<uint, FMOD.Sound>();
            }
            PSGSquare();
            PSGNoise();
        }

        Dictionary<byte, FMOD.Channel> keys = new Dictionary<byte, FMOD.Channel>();
        void HandleChannelMessageReceived(object sender, Sanford.Multimedia.Midi.ChannelMessageEventArgs e)
        {
            var note = (byte)e.Message.Data1;
            if ((e.Message.Command == Sanford.Multimedia.Midi.ChannelCommand.NoteOn && e.Message.Data2 == 0)
                || e.Message.Command == Sanford.Multimedia.Midi.ChannelCommand.NoteOff)
            {
                if (keys.ContainsKey(note))
                {
                    keys[note].stop();
                    keys.Remove(note);
                }
            }
            else if (e.Message.Command == Sanford.Multimedia.Midi.ChannelCommand.NoteOn)
            {
                if (keys.ContainsKey(note))
                {
                    keys[note].stop();
                    keys.Remove(note);
                }
                else
                {
                    keys.Add(note, null);
                }
                var ins = ((voiceTable[48] as SMulti).Table[0].Instrument as Direct_Sound); // Testing
                var sound = sounds[ins.Address];
                system.playSound(sound, null, true, out FMOD.Channel c);
                keys[note] = c;
                sound.getDefaults(out float soundFrequency, out int soundPriority);
                float noteFrequency = (float)Math.Pow(2, ((note - (120 - ins.RootNote)) / 12f)),
                    frequency = soundFrequency * noteFrequency;
                c.setFrequency(frequency);
                c.setPaused(false);
            }
            system.update();
        }

        void PSGSquare()
        {
            byte[] simple = { 1, 2, 4, 6 };
            uint len = 0x100;
            var buf = new byte[16 + len + 16]; // FMOD API requires 16 bytes of padding on each side

            for (uint i = 0; i < 4; i++) // Squares
            {
                for (int j = 0; j < len; j++)
                    buf[16 + j] = (byte)(j < simple[i] * 0x20 ? 0xBF : 0x0);
                var ex = new FMOD.CREATESOUNDEXINFO()
                {
                    defaultfrequency = 44100, //(int)(44100 * Math.Pow(2, (9 / 12f))), // Root note 69
                    format = FMOD.SOUND_FORMAT.PCM8,
                    length = len,
                    numchannels = 1
                };
                system.createSound(buf, FMOD.MODE.OPENMEMORY | FMOD.MODE.OPENRAW | FMOD.MODE.LOOP_NORMAL | FMOD.MODE.LOWMEM, ref ex, out FMOD.Sound snd);
                sounds.Add(SQUARE12_ID - i, snd);
            }
        }
        void PSGNoise()
        {
            uint[] simple = { 32768, 256 };
            var rand = new Random();

            for (uint i = 0; i < 2; i++)
            {
                uint len = simple[i];
                var buf = new byte[16 + len + 16]; // FMOD API requires 16 bytes of padding on each side

                for (int j = 0; j < len; j++)
                    buf[j + 16] = (byte)rand.Next();

                var ex = new FMOD.CREATESOUNDEXINFO()
                {
                    defaultfrequency = 22050,
                    format = FMOD.SOUND_FORMAT.PCM8,
                    length = len,
                    numchannels = 1
                };
                system.createSound(buf, FMOD.MODE.OPENMEMORY | FMOD.MODE.OPENRAW | FMOD.MODE.LOOP_NORMAL | FMOD.MODE.LOWMEM, ref ex, out FMOD.Sound snd);
                sounds.Add(NOISE0_ID - i, snd);
            }
        }

        SongHeader header;
        internal byte NumTracks { get => header.NumTracks; }
        VoiceTable voiceTable;

        internal State State { get; private set; }
        internal delegate void SongEndedEvent();
        internal event SongEndedEvent SongEnded;

        internal void SetVolume(float v)
        {
            system.getMasterSoundGroup(out FMOD.SoundGroup parentGroup);
            parentGroup.setVolume(v);
        }
        internal void SetTempo(ushort t)
        {
            if (t > 510) return;
            tempo = t;
            timer.Interval = (long)(2.5 * 1000 * 1000) / t;
        }

        internal void LoadSong(ushort song)
        {
            Stop();
            header = ROM.Instance.ReadStruct<SongHeader>(ROM.Instance.ReadPointer(ROM.Instance.Game.SongTable + ((uint)8 * song)));
            Array.Resize(ref header.Tracks, header.NumTracks); // Not really necessary

            voiceTable = new VoiceTable();
            voiceTable.LoadPCMSamples(header.VoiceTable, system, sounds);
            new VoiceTableSaver(voiceTable, sounds); // Testing

            MIDIKeyboard.Instance.Start();
        }
        internal void Play()
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
        internal void Pause()
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
        internal void Stop()
        {
            timer.StopAndWait();
            foreach (Instrument i in allInstruments)
                i.Stop();
            State = State.Stopped;
        }
        void PlayLoop(object sender, MicroTimerEventArgs e)
        {
            bool allStopped = true;
            for (int i = header.NumTracks - 1; i >= 0; i--)
            {
                Track track = tracks[i];
                if (!track.Stopped)
                    allStopped = false;
                while (track.Delay == 0 && !track.Stopped)
                    ExecuteNext(track);
                track.Tick();
            }
            system.update();
            if (allStopped)
            {
                Stop();
                SongEnded?.Invoke();
            }
        }

        internal (ushort, uint[], byte[], byte[], byte[][], float[], byte[], byte[], int[], float[], string[]) GetSongState()
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
                types[i] = voiceTable[tracks[i].Voice].ToString();

                Instrument[] instruments = tracks[i].Instruments.Clone().ToArray();
                bool none = instruments.Length == 0;
                Instrument loudest = none ? null : instruments.OrderByDescending(ins => ins.Velocity).ElementAt(0);
                pans[i] = none ? tracks[i].Pan / 64f : loudest.Panpot;
                notes[i] = none ? new byte[0] : instruments.Where(ins => ins.State != ADSRState.Releasing && ins.State != ADSRState.Dead).Select(ins => ins.DisplayNote).Distinct().ToArray();
                velocities[i] = none ? 0 : loudest.Velocity * (volumes[i] / 127f);
            }
            return (tempo, positions, volumes, delays, notes, velocities, voices, modulations, bends, pans, types);
        }

        byte WaitFromCMD(byte startCMD, byte cmd)
        {
            byte[] added = { 4, 4, 2, 2 };
            byte wait = (byte)(cmd - startCMD);
            byte add = wait > 24 ? (byte)24 : wait;
            for (int i = 24 + 1; i <= wait; i++)
                add += added[i % 4];
            return add;
        }

        void PlayNote(Track track, byte note, byte velocity, byte addedDelay = 0)
        {
            // new_note is for drums to have a nice root note
            byte new_note = track.PrevNote = note;
            track.PrevVelocity = velocity;

            Instrument instrument = null;
            Voice voice = voiceTable[track.Voice].Instrument;

            Read:
            switch (voice.VoiceType)
            {
                case 0x0:
                case 0x8:
                    var byAge = dsInstruments.OrderByDescending(ins => ins.Age);
                    foreach (Instrument i in byAge) // Find free
                        if (i.State == ADSRState.Dead)
                        {
                            instrument = i;
                            break;
                        }
                    if (instrument == null) // Find prioritized
                        foreach (Instrument i in byAge)
                            if (track.Priority > i.Track.Priority)
                                instrument = i;
                    if (instrument == null) // Find releasing
                        foreach (Instrument i in byAge)
                            if (i.State == ADSRState.Releasing)
                                instrument = i;
                    if (instrument == null) // None available; kill newest
                        instrument = byAge.Last();
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
                case 0x40:
                    var split = (Split)voice;
                    var multi = (SMulti)voiceTable[track.Voice];
                    byte inst = ROM.Instance.ReadByte(split.Keys + note);
                    voice = multi.Table[inst].Instrument;
                    new_note = note; // In case there is a multi within a drum
                    goto Read;
                case 0x80:
                    var drum = (SDrum)voiceTable[track.Voice];
                    voice = drum.Table[note].Instrument;
                    new_note = 60; // See, I told you it was nice
                    goto Read;
            }

            instrument.Play(track, system, sounds, voice, new_note, note, track.RunCmd == 0xCF ? (byte)0xFF : WaitFromCMD(0xD0, track.RunCmd));
        }

        void ExecuteNext(Track track)
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
                    case 0xBD: track.Voice = cmd; break; // VOICE
                    case 0xBE: track.SetVolume(cmd); break; // VOL
                    case 0xBF: track.SetPan(cmd); break; // PAN
                    case 0xC0: track.SetBend(cmd); break; // BEND
                    case 0xC1: track.SetBendRange(cmd); break; // BENDR
                    case 0xC4: track.SetMODDepth(cmd); break; // MOD
                    case 0xC5: track.SetMODType(cmd); break; // MODT
                    case 0xCD: track.ReadByte(); break; // XCMD
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
                    case 0xBC: track.ReadByte(); break; // KEYSH
                                                        // Commands that work within running status:
                    case 0xBD: track.Voice = cmd; break; // VOICE
                    case 0xBE: track.SetVolume(track.ReadByte()); break; // VOL
                    case 0xBF: track.SetPan(track.ReadByte()); break; // PAN
                    case 0xC0: track.SetBend(track.ReadByte()); break; // BEND
                    case 0xC1: track.SetBendRange(track.ReadByte()); break; // BENDR
                    case 0xC2: track.ReadByte(); break; // LFOS
                    case 0xC3: track.ReadByte(); break; // LFODL
                    case 0xC4: track.SetMODDepth(track.ReadByte()); break; // MOD
                    case 0xC5: track.SetMODType(track.ReadByte()); break; // MODT
                    case 0xC8: track.ReadByte(); break; // TUNE
                    case 0xCD: track.ReadByte(); track.ReadByte(); break; // XCMD
                    case 0xCE: // EOT
                        Instrument i = null;
                        if (track.PeekByte() < 128)
                        {
                            byte note = track.ReadByte();
                            i = track.Instruments.FirstOrDefault(ins => ins.NoteDuration == 0xFF && ins.DisplayNote == note);
                            track.PrevNote = note;
                        }
                        else
                        {
                            i = track.Instruments.FirstOrDefault(ins => ins.NoteDuration == 0xFF);
                        }
                        if (i != null)
                            i.State = ADSRState.Releasing;
                        break;
                    default: Console.WriteLine("Invalid command: 0x{0:X} = {1}", track.Position, cmd); break;
                }
            }

            #endregion
        }
    }
}
