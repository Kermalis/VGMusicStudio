using GBAMusicStudio.Util;
using System;
using System.Linq;
using System.Threading;

namespace GBAMusicStudio.Core
{
    class SongPlayer
    {
        public static SongPlayer Instance { get; } = new SongPlayer();

        readonly TimeBarrier time;
        readonly Thread thread;

        public short Tempo;
        int tempoStack;
        public bool PlaylistPlaying = false;
        // Number of loops that have passed
        int loops;
        bool fadeOutBegan;
        // Song position in ticks
        int position;
        Track[] tracks;
        int longestTrack;

        public Song Song { get; private set; }
        public int NumTracks => Song == null ? 0 : Song.NumTracks;

        public PlayerState State { get; private set; }
        public delegate void SongEndedEvent();
        public event SongEndedEvent SongEnded;

        private SongPlayer()
        {
            time = new TimeBarrier();
            thread = new Thread(Tick) { Name = "SongPlayer Tick" };
            thread.Start();

            Reset();
        }
        public void Reset()
        {
            if (ROM.Instance == null)
            {
                return;
            }

            byte amt = ROM.Instance.Game.Engine.TrackLimit;
            tracks = new Track[amt];
            for (byte i = 0; i < amt; i++)
            {
                switch (ROM.Instance.Game.Engine.Type)
                {
                    case EngineType.M4A: tracks[i] = new M4ATrack(i); break;
                    case EngineType.MLSS: tracks[i] = new MLSSTrack(i); break;
                }
            }

            Song = null;
        }

        public void SetSong(Song song)
        {
            Song = song;
            VoiceTable.ClearCache();
            SoundMixer.Instance.Init(song.GetReverb());
        }
        public void SetSongPosition(int p)
        {
            bool pause = State == PlayerState.Playing;
            if (pause)
            {
                Pause();
            }
            position = p;
            for (int i = NumTracks - 1; i >= 0; i--)
            {
                Track track = tracks[i];
                track.Init();
                int elapsed = 0;
                while (!track.Stopped)
                {
                    bool u = false, l = false;
                    ExecuteNext(i, ref u, ref l);
                    // elapsed == 400, delay == 4, p == 402
                    if (elapsed <= p && elapsed + track.Delay > p)
                    {
                        track.Delay -= (byte)(p - elapsed);
                        SoundMixer.Instance.StopAllChannels();
                        break;
                    }
                    elapsed += track.Delay;
                    track.Delay = 0;
                }
            }
            if (pause)
            {
                Pause();
            }
        }

        public void RefreshSong()
        {
            DetermineLongestTrack();
            SetSongPosition(position);
        }
        void DetermineLongestTrack()
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

        public void Play()
        {
            Stop();

            if (NumTracks == 0)
            {
                SongEnded?.Invoke();
                return;
            }

            for (int i = 0; i < NumTracks; i++)
            {
                tracks[i].Init();
            }
            DetermineLongestTrack();

            position = tempoStack = loops = 0;
            fadeOutBegan = false;
            SoundMixer.Instance.ResetFade();
            Tempo = Engine.GetDefaultTempo();

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
            SoundMixer.Instance.StopAllChannels();
        }
        public void ShutDown()
        {
            Stop();
            State = PlayerState.ShutDown;
            thread.Join();
        }

        public void GetSongState(UI.TrackInfo info)
        {
            info.Tempo = Tempo; info.Position = position;
            for (int i = 0; i < NumTracks; i++)
            {
                Track track = tracks[i];
                info.Positions[i] = Song.Commands[i][track.CommandIndex].GetOffset();
                info.Delays[i] = track.Delay;
                info.Voices[i] = track.Voice;
                info.Mods[i] = track.MODDepth;
                info.Types[i] = Song.VoiceTable[track.Voice].GetName();
                info.Volumes[i] = track.GetVolume();
                info.Pitches[i] = track.GetPitch();
                info.Pans[i] = track.GetPan();

                Channel[] channels = track.Channels.ToArray(); // Copy so adding and removing from the other thread doesn't interrupt (plus Array looping is faster than List looping)
                if (channels.Length == 0)
                {
                    info.Notes[i] = new sbyte[0];
                    info.Lefts[i] = 0;
                    info.Rights[i] = 0;
                }
                else
                {
                    var lefts = new float[channels.Length];
                    var rights = new float[channels.Length];
                    for (int j = 0; j < channels.Length; j++)
                    {
                        ChannelVolume vol = channels[j].GetVolume();
                        lefts[j] = vol.LeftVol;
                        rights[j] = vol.RightVol;
                    }
                    info.Notes[i] = channels.Where(c => c.State < EnvelopeState.Releasing).Select(c => c.Note.OriginalKey).Distinct().ToArray();
                    info.Lefts[i] = lefts.Max();
                    info.Rights[i] = rights.Max();
                }
            }
        }

        public Channel PlayNote(Track track, sbyte note, byte velocity, int duration)
        {
            int shift = note + track.KeyShift;
            note = (sbyte)shift.Clamp(0, 0x7F);
            track.PrevNote = note;

            if (!track.Ready)
            {
                return null;
            }
            WrappedVoice voice = null;
            bool fromDrum = false;
            try
            {
                voice = Song.VoiceTable.GetVoiceFromNote(track.Voice, note, out fromDrum);
            }
            catch
            {
                Console.WriteLine("Track {0} tried to play a bad note... Voice {1} Note {2}", track.Index, track.Voice, note);
                return null;
            }

            var aNote = new Note { Duration = duration, Velocity = velocity, OriginalKey = note, Key = fromDrum ? voice.Voice.GetRootNote() : note };
            if (voice.Voice is M4AVoiceEntry m4a)
            {
                var type = (M4AVoiceType)(m4a.Type & 0x7);
                switch (type)
                {
                    case M4AVoiceType.Direct:
                        {
                            bool bFixed = (m4a.Type & (int)M4AVoiceFlags.Fixed) == (int)M4AVoiceFlags.Fixed;
                            bool bCompressed = ROM.Instance.Game.Engine.HasPokemonCompression && (m4a.Type & (int)M4AVoiceFlags.Compressed) == (int)M4AVoiceFlags.Compressed;
                            return SoundMixer.Instance.NewDSNote(track, m4a.ADSR, aNote,
                                track.GetVolume(), track.GetPan(), track.GetPitch(),
                                bFixed, bCompressed, ((M4AWrappedDirect)voice).Sample.GetSample());
                        }
                    case M4AVoiceType.Square1:
                    case M4AVoiceType.Square2:
                        {
                            return SoundMixer.Instance.NewGBNote(track, m4a.ADSR, aNote,
                           track.GetVolume(), track.GetPan(), track.GetPitch(),
                           type, m4a.SquarePattern);
                        }
                    case M4AVoiceType.Wave:
                        {
                            return SoundMixer.Instance.NewGBNote(track, m4a.ADSR, aNote,
                                track.GetVolume(), track.GetPan(), track.GetPitch(),
                                type, m4a.Address - ROM.Pak);
                        }
                    case M4AVoiceType.Noise:
                        {
                            return SoundMixer.Instance.NewGBNote(track, m4a.ADSR, aNote,
                             track.GetVolume(), track.GetPan(), track.GetPitch(),
                             type, m4a.NoisePattern);
                        }
                }
            }
            else if (voice.Voice is MLSSVoice mlssvoice)
            {
                MLSSVoiceEntry entry;
                bool bSquare = false;
                bool bFixed = false; WrappedSample sample = null;
                try
                {
                    entry = mlssvoice.GetEntryFromNote(note);
                    bSquare = track.Voice >= 190 && track.Voice < 200;
                    if (!bSquare)
                    {
                        bFixed = entry.IsFixedFrequency == 0x80;
                        sample = ((MLSSVoiceTable)Song.VoiceTable).Samples[entry.Sample].GetSample();
                    }
                }
                catch
                {
                    Console.WriteLine("Track {0} tried to play a bad note... Voice {1} Note {2}", track.Index, track.Voice, note);
                    return null;
                }
                if (bSquare)
                {
                    int val = Math.Max(NumTracks, 8);
                    bool sq1 = track.Index == val - 2;
                    bool sq2 = track.Index == val - 1;
                    if (!sq1 && !sq2) // Tried to play a square in a bad track
                    {
                        return null;
                    }
                    M4AVoiceType type = sq1 ? M4AVoiceType.Square1 : sq2 ? M4AVoiceType.Square2 : M4AVoiceType.Invalid5; // Invalid5 wouldn't happen
                    return SoundMixer.Instance.NewGBNote(track, new ADSR { S = 0x7 }, aNote,
                        track.GetVolume(), track.GetPan(), track.GetPitch(),
                        type, (SquarePattern)entry.Sample);
                }
                else if (sample != null)
                {
                    return SoundMixer.Instance.NewDSNote(track, new ADSR { A = 0xFF, S = 0xFF }, aNote,
                            track.GetVolume(), track.GetPan(), track.GetPitch(),
                            bFixed, false, sample);
                }
            }
            return null;
        }

        // "update" signals if the track needs to update volume, pan, or pitch
        // "loop" signals if the track just looped
        void ExecuteNext(int i, ref bool update, ref bool loop)
        {
            Track track = tracks[i];
            var mlTrack = track as MLSSTrack;
            SongEvent e = Song.Commands[i][track.CommandIndex];

            // MLSS
            // If a note is extending and the moment passed
            if (mlTrack != null && mlTrack.FreeChannel != null
                && mlTrack.FreeNoteEnd < e.AbsoluteTicks)
            {
                mlTrack.FreeChannel = null;
            }

            // Do these and calculate nextE if necessary
            if (e.Command is GoToCommand goTo)
            {
                int gotoCmd = Song.Commands[i].FindIndex(c => c.GetOffset() == goTo.Offset);
                if (longestTrack == i)
                {
                    position = Song.Commands[i][gotoCmd].AbsoluteTicks - 1;
                }
                track.CommandIndex = gotoCmd - 1; // -1 for incoming ++
                track.NextCommandIndex = track.CommandIndex + 1;
                loop = true;
            }
            else if (e.Command is CallCommand patt)
            {
                int callCmd = Song.Commands[i].FindIndex(c => c.GetOffset() == patt.Offset);
                track.EndOfPattern = track.CommandIndex;
                track.CommandIndex = callCmd - 1; // -1 for incoming ++
                track.NextCommandIndex = track.CommandIndex + 1;
            }
            else if (e.Command is ReturnCommand)
            {
                if (track.EndOfPattern != 0)
                {
                    track.CommandIndex = track.EndOfPattern;
                    track.NextCommandIndex = track.CommandIndex + 1;
                    track.EndOfPattern = 0;
                }
            }
            else
            {
                if (e.Command is FinishCommand)
                {
                    track.Stopped = true;
                    track.ReleaseChannels(-1);
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
                    {
                        track.EchoVolume = xcmd.Argument;
                    }
                    else if (xcmd.Command == 9)
                    {
                        track.EchoLength = xcmd.Argument;
                    }
                }
                else if (e.Command is EndOfTieCommand eot)
                {
                    if (eot.Note == -1)
                    {
                        track.ReleaseChannels(track.PrevNote);
                    }
                    else
                    {
                        track.ReleaseChannels((eot.Note + track.KeyShift).Clamp(0, 127));
                    }
                }
                else if (e.Command is NoteCommand n)
                {
                    if (e.Command is MLSSNoteCommand mln)
                    {
                        mlTrack.Delay += (byte)mln.Duration;
                        if (mlTrack.FreeChannel == null || mlTrack.FreeChannel.Note.OriginalKey != mln.Note)
                        {
                            PlayNote(track, mln.Note, 0x7F, mln.Duration);
                            mlTrack.FreeChannel = null;
                        }
                    }
                    else if (e.Command is M4ANoteCommand m4an)
                    {
                        PlayNote(track, m4an.Note, m4an.Velocity, m4an.Duration);
                    }
                }
                else if (e.Command is FreeNoteCommand free)
                {
                    mlTrack.Delay += free.Duration;
                    sbyte note = (sbyte)(free.Note - 0x80);
                    if (mlTrack.FreeChannel == null || mlTrack.FreeChannel.Note.OriginalKey != note)
                    {
                        mlTrack.FreeChannel = PlayNote(track, note, 0x7F, free.Duration);
                        mlTrack.FreeNoteEnd = e.AbsoluteTicks + free.Duration;
                    }
                }
            }

            // MLSS
            // If a note is extending and the next tick it ends but has the chance of extending
            if (track.NextCommandIndex < Song.Commands[i].Count)
            {
                SongEvent nextE = Song.Commands[i][track.NextCommandIndex];
                if (mlTrack != null && mlTrack.FreeChannel != null
                    && mlTrack.FreeNoteEnd == nextE.AbsoluteTicks)
                {
                    // Find note/extension next tick
                    SongEvent nextNoteEvent = Song.Commands[i].Where(c => c.AbsoluteTicks == nextE.AbsoluteTicks)
                        .SingleOrDefault(c => c.Command is MLSSNoteCommand || c.Command is FreeNoteCommand);
                    if (nextNoteEvent != null)
                    {
                        dynamic nextNote = nextNoteEvent.Command;
                        int note = nextNote is FreeNoteCommand ? nextNote.Note - 0x80 : nextNote.Note;
                        if (mlTrack.FreeChannel.Note.OriginalKey == note)
                        {
                            int extension = nextNote.Duration;
                            mlTrack.FreeChannel.Note.Duration += extension;
                            mlTrack.FreeNoteEnd += extension;
                        }
                    }
                }
            }

            // Increment command index
            if (!track.Stopped)
            {
                track.CommandIndex++;
                track.NextCommandIndex++;
            }
        }
        void Tick()
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
                        bool allDone = true, loop = false;
                        for (int i = 0; i < NumTracks; i++)
                        {
                            Track track = tracks[i];
                            track.Tick();
                            bool update = false;
                            while (track.Delay == 0 && !track.Stopped)
                            {
                                ExecuteNext(i, ref update, ref loop);
                            }
                            if (update || track.MODDepth > 0)
                            {
                                track.UpdateChannels();
                            }
                            if (!track.Stopped || track.Channels.Count != 0)
                            {
                                allDone = false;
                            }
                        }
                        position++;
                        if (loop)
                        {
                            loops++;
                            if (PlaylistPlaying && loops > Config.Instance.PlaylistSongLoops && !fadeOutBegan)
                            {
                                SoundMixer.Instance.FadeOut();
                                fadeOutBegan = true;
                            }
                        }
                        if (fadeOutBegan && SoundMixer.Instance.IsFadeDone())
                        {
                            allDone = true;
                        }
                        if (allDone)
                        {
                            Stop();
                            SongEnded?.Invoke();
                        }
                    }
                }
                // Do Instrument Tick
                if (State != PlayerState.Paused)
                {
                    SoundMixer.Instance.Process();
                }
                // Wait for next frame
                time.Wait();
            }
            time.Stop();
        }
    }
}
