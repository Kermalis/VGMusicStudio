using System;
using static GBAMusicStudio.Core.M4A.M4AStructs;

namespace GBAMusicStudio.Core.M4A
{
    enum ADSRState
    {
        Dead,
        Rising,
        Playing,
        Releasing
    }

    internal class Instrument
    {
        internal ADSRState State;
        internal float Velocity { get { return ((NoteVelocity / 127f) * CurrentVelocity) / 255f; } }
        internal float Panpot { get { return (ForcedPan != 0x7F ? ForcedPan : Track.Pan) / 64f; } }
        internal ulong Age { get; private set; }

        internal Track Track { get; private set; }
        FMOD.Channel Channel;
        internal byte DisplayNote { get; private set; }
        byte Note;
        internal byte NoteDuration { get; private set; }
        byte NoteVelocity;
        int CurrentVelocity;

        byte A, D, S, R;
        byte RootNote;
        bool FixedFrequency;
        sbyte ForcedPan; // 0x7F counts as disabled

        internal void SetPriority(byte priority)
        {
            Channel.setPriority(priority);
        }
        internal void UpdatePanpot()
        {
            Channel.setPan(Panpot);
        }
        void UpdateVolume()
        {
            Channel.setVolume(Velocity);
        }
        internal void UpdateFrequency()
        {
            Channel.setPaused(true);
            Channel.getCurrentSound(out FMOD.Sound sound);
            sound.getDefaults(out float soundFrequency, out int soundPriority);
            float noteFrequency = (float)Math.Pow(2, ((Note - (120 - RootNote)) / 12f)),
                bendFrequency = (float)Math.Pow(2, (Track.Bend * Track.BendRange) / (float)(64 * 12)),
                frequency = soundFrequency * noteFrequency * bendFrequency;
            Channel.setFrequency(FixedFrequency ? soundFrequency : frequency); // Not sure if fixed frequency ignores bends yet
            UpdatePanpot();
            UpdateVolume();
            Channel.setPaused(false);
        }

        // Pass 0xFF to "duration" to trigger a TIE
        internal void Play(Track track, byte note, byte duration)
        {
            Stop();
            Voice voice = MusicPlayer.VoiceTable.GetVoiceFromNote(track.Voice, note, out Note);
            FMOD.Sound sound = MusicPlayer.VoiceTable.GetSoundFromNote(track.Voice, note);

            Track = track;
            DisplayNote = note;
            RootNote = voice.RootNote;
            NoteDuration = duration;
            NoteVelocity = track.PrevVelocity;
            Age = 0;
            CurrentVelocity = 0;
            State = ADSRState.Rising;

            dynamic dyn = voice; // ADSR are in the same spot in each struct
            FixedFrequency = false;
            ForcedPan = 0x7F;
            A = dyn.A; D = dyn.D; S = dyn.S; R = dyn.R;

            if (voice is Direct_Sound direct)
            {
                FixedFrequency = direct.Type == 0x8;
                if (direct.Panpot >= 0x80)
                    ForcedPan = (sbyte)((direct.Panpot ^ 0x80) - 64);
            }
            else // PSG instrument
            {
                RootNote += 9;
                A *= 17; D *= 17; S *= 17; R *= 17;
            }

            MusicPlayer.System.playSound(sound, Track.Group, true, out Channel);
            if (A == 0) A = 255;
            UpdateFrequency();
            track.Instruments.Add(this);
        }

        internal void Stop()
        {
            if (State == ADSRState.Dead) return;
            CurrentVelocity = 0;
            State = ADSRState.Dead;
            Channel.stop();
            Track.Instruments.Remove(this);
        }

        internal void Tick()
        {
            if (State == ADSRState.Dead) return;

            switch (State)
            {
                case ADSRState.Rising:
                    CurrentVelocity += A;
                    if (CurrentVelocity >= 255)
                    {
                        CurrentVelocity = 255;
                        State = ADSRState.Playing;
                    }
                    else if (NoteDuration != 0xFF && Age > NoteDuration)
                        State = ADSRState.Playing;
                    break;
                case ADSRState.Playing:
                    CurrentVelocity = (CurrentVelocity * D) / 256;
                    if (CurrentVelocity < S)
                        CurrentVelocity = S;
                    if (NoteDuration != 0xFF && Age >= NoteDuration)
                        State = ADSRState.Releasing;
                    break;
                case ADSRState.Releasing:
                    CurrentVelocity = (CurrentVelocity * R) / 256;
                    if (CurrentVelocity <= 0)
                    {
                        Stop();
                        return;
                    }
                    break;
            }
            UpdateVolume();
            Age++;
        }
    }
}
