using System;
using System.Collections.Generic;
using System.Linq;
using GBAMusicStudio.Util;
using System.Threading;

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
        static readonly TimeBarrier time;
        static Thread thread;

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

        internal static ushort Tempo;
        static int tempoStack;
        static uint position;
        static Track[] tracks;
        static int longestTrack;

        internal static Song Song;
        internal static int NumTracks => Song == null ? 0 : Song.NumTracks.Clamp(0, 16);

        static SongPlayer()
        {
            FMOD.Factory.System_Create(out System);
            //System.setSoftwareFormat(13379, FMOD.SPEAKERMODE.DEFAULT, 0);
            var settings = new FMOD.ADVANCEDSETTINGS { resamplerMethod = FMOD.DSP_RESAMPLER.NOINTERP };
            System.setAdvancedSettings(ref settings);
            System.init(Config.DirectCount + 4, FMOD.INITFLAGS.NORMAL, (IntPtr)0);

            dsInstruments = new Instrument[Config.DirectCount];
            gbInstruments = new Instrument[4];
            for (int i = 0; i < dsInstruments.Length; i++)
                dsInstruments[i] = new Instrument();
            for (int i = 0; i < 4; i++)
                gbInstruments[i] = new Instrument();
            allInstruments = dsInstruments.Union(gbInstruments).ToArray();

            time = new TimeBarrier();

            Reset();
        }

        internal static void Reset()
        {
            if (ROM.Instance == null) return;

            tracks = new Track[16];
            for (byte i = 0; i < 16; i++)
            {
                switch (ROM.Instance.Game.Engine)
                {
                    case AEngine.M4A: tracks[i] = new M4ATrack(i); break;
                    case AEngine.MLSS: tracks[i] = new MLSSTrack(i); break;
                }
            }

            if (Sounds != null)
            {
                foreach (var s in Sounds.Values)
                    s.release();
                Sounds.Clear();
                Song = null;
                M4ASMulti.Cache.Clear();
                M4ASDrum.Cache.Clear();
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
            int variance = (int)(0x7F * Config.PSGVolume);
            byte high = (byte)(0x80 + variance);
            byte low = (byte)(0x80 - variance);

            byte[][] squares = new byte[][] {
                new byte[] { high, low, low, low, low, low, low, low }, // 12.5%
                new byte[] { high, high, low, low, low, low, low, low }, // 25%
                new byte[] { high, high, high, high, low, low, low, low }, // 50%
                new byte[] { high, high, high, high, high, high, low, low } // 75%
            };

            for (uint i = 0; i < 4; i++) // Squares
            {
                var ex = new FMOD.CREATESOUNDEXINFO()
                {
                    defaultfrequency = 3520,
                    format = FMOD.SOUND_FORMAT.PCM8,
                    length = 8,
                    numchannels = 1
                };
                // Three groups of 8 periods
                System.createSound(squares[i].Concat(squares[i]).Concat(squares[i]).ToArray(), FMOD.MODE.OPENMEMORY | FMOD.MODE.OPENRAW | FMOD.MODE.LOOP_NORMAL | FMOD.MODE.LOWMEM, ref ex, out FMOD.Sound snd);
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
                    buf[j] = (byte)rand.Next((int)(0xFF * Config.PSGVolume));

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
            System.getMasterChannelGroup(out FMOD.ChannelGroup parentGroup);
            parentGroup.setVolume(v);
        }
        internal static void SetMute(int i, bool m) => tracks[i].Group.setMute(m);
        internal static void SetPosition(uint p)
        {
            bool pause = State == State.Playing;
            if (pause) Pause();
            position = p;
            for (int i = NumTracks - 1; i >= 0; i--)
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

        internal static void RefreshSong()
        {
            DetermineLongestTrack();
            SetPosition(position);
        }
        static void DetermineLongestTrack()
        {
            for (int i = 0; i < NumTracks; i++)
            {
                if (Song.Commands[i].Last().AbsoluteTicks == Song.NumTicks - 1)
                {
                    longestTrack = i;
                    break;
                }
            }
        }

        internal static void Play()
        {
            Stop();

            if (NumTracks == 0)
            {
                SongEnded?.Invoke();
                return;
            }

            for (int i = 0; i < NumTracks; i++)
                tracks[i].Init();
            DetermineLongestTrack();

            position = 0; tempoStack = 0;
            Tempo = Engine.GetDefaultTempo();

            StartThread();
        }
        internal static void Pause()
        {
            if (State == State.Paused)
            {
                StartThread();
            }
            else
            {
                StopThread();
                System.getMasterChannelGroup(out FMOD.ChannelGroup parentGroup);
                parentGroup.setMute(true);
                State = State.Paused;
            }
        }
        internal static void Stop()
        {
            StopThread();
            foreach (Instrument i in allInstruments)
                i.Stop();
        }
        static void StartThread()
        {
            thread = new Thread(Tick);
            thread.Start();
            System.getMasterChannelGroup(out FMOD.ChannelGroup parentGroup);
            parentGroup.setMute(false);
            State = State.Playing;
        }
        static void StopThread()
        {
            if (State == State.Stopped) return;
            State = State.Stopped;
            if (thread != null && thread.IsAlive)
                thread.Join();
        }

        internal static (ushort, uint, uint[], byte[], byte[], sbyte[][], float[], byte[], byte[], int[], float[], string[]) GetSongState()
        {
            var offsets = new uint[NumTracks];
            var volumes = new byte[NumTracks];
            var delays = new byte[NumTracks];
            var notes = new sbyte[NumTracks][];
            var velocities = new float[NumTracks];
            var voices = new byte[NumTracks];
            var modulations = new byte[NumTracks];
            var bends = new int[NumTracks];
            var pans = new float[NumTracks];
            var types = new string[NumTracks];
            for (int i = 0; i < NumTracks; i++)
            {
                offsets[i] = Song.Commands[i][tracks[i].CommandIndex].Offset;
                volumes[i] = tracks[i].Volume;
                delays[i] = tracks[i].Delay;
                voices[i] = tracks[i].Voice;
                modulations[i] = tracks[i].MODDepth;
                bends[i] = tracks[i].Bend * tracks[i].BendRange;
                types[i] = Song.VoiceTable[tracks[i].Voice].ToString();

                Instrument[] instruments = tracks[i].Instruments.Clone().ToArray();
                bool none = instruments.Length == 0;
                Instrument loudest = none ? null : instruments.OrderByDescending(ins => ins.Velocity).ElementAt(0);
                pans[i] = none ? tracks[i].Pan / (float)Engine.GetPanpotRange() : loudest.Panpot;
                notes[i] = none ? new sbyte[0] : instruments.Where(ins => ins.State < ADSRState.Releasing).Select(ins => ins.DisplayNote).Distinct().ToArray();
                velocities[i] = none ? 0 : loudest.Velocity * (volumes[i] / (float)Engine.GetMaxVolume());
            }
            return (Tempo, position, offsets, volumes, delays, notes, velocities, voices, modulations, bends, pans, types);
        }

        static Instrument FindInstrument(Track track, IVoice voice)
        {
            Instrument instrument = null;
            if (voice is M4AVoice m4avoice)
            {
                switch (m4avoice.Type)
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
            }
            else
            {
                instrument = dsInstruments[0];
            }
            return instrument;
        }
        static void PlayNote(Track track, sbyte note, byte velocity, int duration)
        {
            // Hides base note, vel and dur
            int shift = note + track.KeyShift;
            note = (sbyte)(shift.Clamp(0, 127));

            if (!track.Ready) return;

            IVoice voice = Song.VoiceTable.GetVoiceFromNote(track.Voice, note, out bool fromDrum);
            Instrument instrument = FindInstrument(track, voice);

            if (instrument != null)
                instrument.Play(track, note, velocity, duration);
        }

        static void ExecuteNext(int i)
        {
            var track = tracks[i];
            var e = Song.Commands[i][track.CommandIndex];

            if (e.Command is GoToCommand goTo)
            {
                int gotoCmd = Song.Commands[i].FindIndex(c => c.Offset == goTo.Offset);
                if (longestTrack == i)
                    position = Song.Commands[i][gotoCmd].AbsoluteTicks - 1;
                track.CommandIndex = gotoCmd - 1; // -1 for incoming ++
            }
            else if (e.Command is CallCommand patt)
            {
                int callCmd = Song.Commands[i].FindIndex(c => c.Offset == patt.Offset);
                track.EndOfPattern = track.CommandIndex;
                track.CommandIndex = callCmd - 1; // -1 for incoming ++
            }
            else if (e.Command is ReturnCommand)
            {
                if (track.EndOfPattern != 0)
                {
                    track.CommandIndex = track.EndOfPattern;
                    track.EndOfPattern = 0;
                }
            }
            else if (e.Command is FinishCommand) track.Stopped = true;
            else if (e.Command is PriorityCommand prio) track.SetPriority(prio.Priority);
            else if (e.Command is TempoCommand tempo) Tempo = tempo.Tempo;
            else if (e.Command is KeyShiftCommand keysh) track.KeyShift = keysh.Shift;
            else if (e.Command is RestCommand w) track.Delay = w.Rest;
            else if (e.Command is VoiceCommand voice) track.SetVoice(voice.Voice);
            else if (e.Command is VolumeCommand vol) track.SetVolume(vol.Volume);
            else if (e.Command is PanpotCommand pan) track.SetPan(pan.Panpot);
            else if (e.Command is BendCommand bend) track.SetBend(bend.Bend);
            else if (e.Command is BendRangeCommand bendr) track.SetBendRange(bendr.Range);
            else if (e.Command is LFOSpeedCommand lfos) track.SetLFOSpeed(lfos.Speed);
            else if (e.Command is LFODelayCommand lfodl) track.SetLFODelay(lfodl.Delay);
            else if (e.Command is ModDepthCommand mod) track.SetMODDepth(mod.Depth);
            else if (e.Command is ModTypeCommand modt) track.SetMODType((MODT)modt.Type);
            else if (e.Command is TuneCommand tune) track.SetTune(tune.Tune);
            else if (e.Command is EndOfTieCommand eot)
            {
                IEnumerable<Instrument> ins = new Instrument[0];
                if (eot.Note == -1)
                    ins = track.Instruments.Where(inst => inst.NoteDuration == -1 && inst.State < ADSRState.Releasing);
                else
                {
                    byte note = (byte)(eot.Note + track.KeyShift).Clamp(0, 127);
                    // Could be problematic to do "DisplayNote" with high notes and a key shift up
                    ins = track.Instruments.Where(inst => inst.NoteDuration == -1 && inst.DisplayNote == note && inst.State < ADSRState.Releasing);
                }
                foreach (var inst in ins)
                    inst.State = ADSRState.Releasing;
            }
            else if (e.Command is NoteCommand n)
            {
                if (e.Command is MLSSNoteCommand mln)
                {
                    var mlTrack = (MLSSTrack)track;
                    mlTrack.Delay += (byte)mln.Duration;
                    PlayNote(track, mln.Note, 0x7F, mln.Duration);
                }
                else if (e.Command is M4ANoteCommand m4an)
                {
                    PlayNote(track, m4an.Note, m4an.Velocity, m4an.Duration);
                }
            }
            else if (e.Command is FreeNoteCommand ext)
            {
                var mlTrack = (MLSSTrack)track;
                mlTrack.Delay += ext.Extension;
                sbyte note = (sbyte)(ext.Note - 0x80);
                PlayNote(mlTrack, note, 0x7F, ext.Extension);
            }

            if (!track.Stopped)
                track.CommandIndex++;
        }
        static void Tick()
        {
            time.Start();
            while (State != State.Stopped)
            {
                // Do Song Tick
                tempoStack += Tempo;
                int wait = Engine.GetTempoWait();
                while (tempoStack >= wait)
                {
                    tempoStack -= wait;
                    bool allDone = true;
                    for (int i = NumTracks - 1; i >= 0; i--)
                    {
                        Track track = tracks[i];
                        if (!track.Stopped || track.Instruments.Any(ins => ins.State != ADSRState.Dead))
                            allDone = false;
                        while (track.Delay == 0 && !track.Stopped)
                            ExecuteNext(i);
                        track.Tick();
                    }
                    position++;
                    if (allDone)
                        SongEnded?.Invoke();
                }
                // Do Instrument Tick
                foreach (var i in allInstruments)
                    i.ADSRTick();
                System.update();
                // Wait for next frame
                time.Wait();
            }
            time.Stop();
        }
    }
}
