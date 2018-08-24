using System.Linq;
using GBAMusicStudio.Util;
using System.Threading;

namespace GBAMusicStudio.Core
{
    internal static class SongPlayer
    {
        static readonly TimeBarrier time;
        static Thread thread;

        internal static ushort Tempo;
        static int tempoStack;
        static uint position;
        static Track[] tracks;
        static int longestTrack;

        internal static Song Song { get; private set; }
        internal static int NumTracks => Song == null ? 0 : Song.NumTracks.Clamp(0, 16);

        static SongPlayer()
        {
            time = new TimeBarrier();
            thread = new Thread(Tick) { Name = "SongPlayer Tick" };
            thread.Start();

            Reset();
        }
        internal static void Reset()
        {
            if (ROM.Instance == null) return;

            tracks = new Track[16];
            for (byte i = 0; i < 16; i++)
            {
                switch (ROM.Instance.Game.Engine.Type)
                {
                    case EngineType.M4A: tracks[i] = new M4ATrack(i); break;
                    case EngineType.MLSS: tracks[i] = new MLSSTrack(i); break;
                }
            }

            Song = null;
        }

        internal static PlayerState State { get; private set; }
        internal delegate void SongEndedEvent();
        internal static event SongEndedEvent SongEnded;

        internal static void SetSong(Song song)
        {
            Song = song;
            VoiceTable.ClearCache();
            SoundMixer.Init(song.GetReverb());
        }
        internal static void SetPosition(uint p)
        {
            bool pause = State == PlayerState.Playing;
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
                        SoundMixer.StopAllChannels();
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

            State = PlayerState.Playing;
        }
        internal static void Pause()
        {
            State = (State == PlayerState.Paused ? PlayerState.Playing : PlayerState.Paused);
        }
        internal static void Stop()
        {
            if (State == PlayerState.Stopped) return;
            State = PlayerState.Stopped;
            SoundMixer.StopAllChannels();
        }
        internal static void ShutDown()
        {
            Stop();
            State = PlayerState.ShutDown;
            thread.Join();
        }

        internal static void GetSongState(UI.TrackInfo info)
        {
            info.Tempo = Tempo; info.Position = position;
            for (int i = 0; i < NumTracks; i++)
            {
                info.Positions[i] = Song.Commands[i][tracks[i].CommandIndex].Offset;
                info.Delays[i] = tracks[i].Delay;
                info.Voices[i] = tracks[i].Voice;
                info.Mods[i] = tracks[i].MODDepth;
                info.Types[i] = Song.VoiceTable[tracks[i].Voice].GetName();
                info.Volumes[i] = tracks[i].GetVolume();
                info.Pitches[i] = tracks[i].GetPitch();
                info.Pans[i] = tracks[i].GetPan();

                var channels = SoundMixer.GetChannels(i);
                bool none = channels.Length == 0;
                info.Lefts[i] = none ? 0 : channels.Select(c => c.GetVolume().FromLeftVol).Max();
                info.Rights[i] = none ? 0 : channels.Select(c => c.GetVolume().FromRightVol).Max();
                info.Notes[i] = none ? new sbyte[0] : channels.Where(c => c.State < ADSRState.Releasing).Select(c => c.Note.OriginalKey).Distinct().ToArray();
            }
        }

        internal static void PlayNote(Track track, sbyte note, byte velocity, int duration)
        {
            int shift = note + track.KeyShift;
            note = (sbyte)(shift.Clamp(0, 127));
            track.PrevNote = note;

            if (!track.Ready) return;

            WrappedVoice voice = Song.VoiceTable.GetVoiceFromNote(track.Voice, note, out bool fromDrum);

            var owner = track.Index;
            var aNote = new Note { Duration = duration, Velocity = velocity, OriginalKey = note, Key = fromDrum ? voice.Voice.GetRootNote() : note };
            if (voice.Voice is M4AVoice m4a)
            {
                M4AVoiceEntry entry = m4a.Entry;
                M4AVoiceType type = (M4AVoiceType)(entry.Type & 0x7);
                switch (type)
                {
                    case M4AVoiceType.Direct:
                        bool bFixed = (entry.Type & (int)M4AVoiceFlags.Fixed) == (int)M4AVoiceFlags.Fixed;
                        SoundMixer.NewDSNote(owner, entry.ADSR, aNote,
                            track.GetVolume(), track.GetPan(), track.GetPitch(),
                            bFixed, ((M4AWrappedDirect)voice).Sample.GetSample(), tracks);
                        break;
                    case M4AVoiceType.Square1:
                    case M4AVoiceType.Square2:
                        SoundMixer.NewGBNote(owner, entry.ADSR, aNote,
                                track.GetVolume(), track.GetPan(), track.GetPitch(),
                                type, entry.SquarePattern);
                        break;
                    case M4AVoiceType.Wave:
                        SoundMixer.NewGBNote(owner, entry.ADSR, aNote,
                                track.GetVolume(), track.GetPan(), track.GetPitch(),
                                type, entry.Address);
                        break;
                    case M4AVoiceType.Noise:
                        SoundMixer.NewGBNote(owner, entry.ADSR, aNote,
                                track.GetVolume(), track.GetPan(), track.GetPitch(),
                                type, entry.NoisePattern);
                        break;
                }
            }
            else if (voice.Voice is MLSSVoice mlssvoice)
            {
                MLSSVoiceEntry entry; bool bFixed = false; WrappedSample sample = null;
                try
                {
                    entry = mlssvoice.GetEntryFromNote(note);
                    bFixed = entry.IsFixedFrequency == 0x80;
                    sample = ((MLSSVoiceTable)Song.VoiceTable).Samples[entry.Sample].GetSample();
                }
                catch { System.Console.WriteLine("Track {0} tried to play a bad note: {1}", owner + 1, note); }
                finally
                {
                    if (sample != null)
                        SoundMixer.NewDSNote(owner, new ADSR { A = 0xFF, S = 0xFF }, aNote,
                                track.GetVolume(), track.GetPan(), track.GetPitch(),
                                bFixed, sample, tracks);
                }
            }
        }

        // Returns a bool which indicates whether the track needs to update volume, pan, or pitch
        static bool ExecuteNext(int i)
        {
            bool update = false;

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
            else if (e.Command is FinishCommand)
            {
                track.Stopped = true;
                SoundMixer.ReleaseChannels(i, -1);
            }
            else if (e.Command is PriorityCommand prio) { track.Priority = prio.Priority; } // TODO: Update channel priorities
            else if (e.Command is TempoCommand tempo) { Tempo = tempo.Tempo; }
            else if (e.Command is KeyShiftCommand keysh) { track.KeyShift = keysh.Shift; }
            else if (e.Command is RestCommand w) { track.Delay = w.Rest; }
            else if (e.Command is VoiceCommand voice) { track.Voice = voice.Voice; track.Ready = true; }
            else if (e.Command is VolumeCommand vol) { track.Volume = vol.Volume; update = true; }
            else if (e.Command is PanpotCommand pan) { track.Pan = pan.Panpot; update = true; }
            else if (e.Command is BendCommand bend) { track.Bend = bend.Bend; update = true; }
            else if (e.Command is BendRangeCommand bendr) { track.BendRange = bendr.Range; update = true; }
            else if (e.Command is LFOSpeedCommand lfos) { track.LFOSpeed = lfos.Speed; track.LFOPhase = track.LFODelayCount = 0; update = true; }
            else if (e.Command is LFODelayCommand lfodl) { track.LFODelay = lfodl.Delay; track.LFOPhase = track.LFODelayCount = 0; update = true; }
            else if (e.Command is ModDepthCommand mod) { track.MODDepth = mod.Depth; update = true; }
            else if (e.Command is ModTypeCommand modt) { track.MODType = (MODType)modt.Type; update = true; }
            else if (e.Command is TuneCommand tune) { track.Tune = tune.Tune; update = true; }
            else if (e.Command is LibraryCommand xcmd)
            {
                if (xcmd.Command == 8)
                    track.EchoVolume = xcmd.Argument;
                else if (xcmd.Command == 9)
                    track.EchoLength = xcmd.Argument;
            }
            else if (e.Command is EndOfTieCommand eot)
            {
                if (eot.Note == -1)
                    SoundMixer.ReleaseChannels(i, track.PrevNote);
                else
                {
                    sbyte note = (sbyte)(eot.Note + track.KeyShift).Clamp(0, 127);
                    SoundMixer.ReleaseChannels(i, note);
                }
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

            return update;
        }
        static void Tick()
        {
            time.Start();
            while (State != PlayerState.ShutDown)
            {
                if (State == PlayerState.Playing)
                {
                    // Do Song Tick
                    tempoStack += Tempo;
                    int wait = Engine.GetTempoWait();
                    while (tempoStack >= wait)
                    {
                        tempoStack -= wait;
                        bool allDone = true;
                        for (int i = 0; i < NumTracks; i++)
                        {
                            Track track = tracks[i];
                            if (!track.Stopped || !SoundMixer.AllDead(i))
                                allDone = false;
                            track.Tick();
                            bool update = false;
                            while (track.Delay == 0 && !track.Stopped)
                                if (ExecuteNext(i))
                                    update = true;
                            if (update || track.MODDepth > 0)
                                SoundMixer.UpdateChannels(i, track.GetVolume(), track.GetPan(), track.GetPitch());
                        }
                        position++;
                        if (allDone)
                        {
                            Stop();
                            SongEnded?.Invoke();
                        }
                    }
                }
                // Do Instrument Tick
                if (State != PlayerState.Paused)
                    SoundMixer.Process();
                // Wait for next frame
                time.Wait();
            }
            time.Stop();
        }
    }
}
