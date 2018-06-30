using GBAMusicStudio.Util;
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
        internal float Panpot { get { return ForcedPan != 0x7F ? ForcedPan / 64f : Track.APan; } }
        internal int Age { get; private set; }

        internal Track Track { get; private set; }
        Voice Voice;
        bool fromDrum;
        FMOD.Channel Channel;
        FMOD.Sound Sound;
        internal sbyte DisplayNote { get; private set; }
        sbyte Note;
        internal sbyte NoteDuration { get; private set; }
        sbyte NoteVelocity;
        int CurrentVelocity;

        byte A, D, S, R;
        bool FixedFrequency;
        sbyte ForcedPan; // 0x7F counts as disabled

        internal void SetPriority(int priority)
        {
            Channel.setPriority(priority);
        }
        internal void UpdatePanpot()
        {
            if (Channel == null) return;
            Channel.setPan(Panpot);
        }
        internal void UpdateVolume()
        {
            if (Channel == null) return;
            Channel.setVolume(Velocity * Track.AVolume);
        }
        internal void UpdateFrequency()
        {
            if (Channel == null) return;
            Channel.setPaused(true);
            Channel.getCurrentSound(out FMOD.Sound sound);
            sound.getDefaults(out float soundFrequency, out int soundPriority);
            float frequency;
            byte root = fromDrum ? Voice.RootNote : (byte)60;
            if (Voice is Direct_Sound)
            {
                frequency = soundFrequency * (float)Math.Pow(2, (Note - (120 - root)) / 12f + Track.APitch / 768f);
            }
            else if (Voice is PSG_Noise)
            {
                frequency = (0x1000 * (float)Math.Pow(8, (Note - (120 - root)) / 12f + Track.APitch / 768f)).Clamp(8, 0x80000); // Thanks ipatix
            }
            else
            {
                float fundamental = 440 * (float)Math.Pow(2, (Note - 69) / 12f + Track.APitch / 768f);
                if (Voice is PSG_Wave)
                    frequency = fundamental * 0x10;
                else // Squares
                    frequency = fundamental * 0x100;
            }
            Channel.setFrequency(FixedFrequency ? soundFrequency : frequency);
            UpdatePanpot();
            UpdateVolume();
            Channel.setPaused(false);
        }

        // Pass -1 to "duration" to trigger a TIE
        internal void Play(Track track, sbyte note, sbyte velocity, sbyte duration)
        {
            Stop();
            Voice = SongPlayer.VoiceTable.GetVoiceFromNote(track.Voice, note, out fromDrum);
            Sound = SongPlayer.VoiceTable.GetSoundFromNote(track.Voice, note);

            Track = track;
            DisplayNote = note;
            Note = fromDrum ? (sbyte)60 : note;
            NoteDuration = duration;
            NoteVelocity = velocity;
            Age = -1;
            CurrentVelocity = 0;
            State = ADSRState.Rising;

            dynamic dyn = Voice; // ADSR are in the same spot in each struct
            FixedFrequency = false;
            ForcedPan = 0x7F;
            A = dyn.A; D = dyn.D; S = dyn.S; R = dyn.R;

            if (Voice is PSG_Square_1 square1) // Square1s have sweeping
            {
                // TODO: Add sweeping
            }
            else if (Voice is Direct_Sound || Voice is PSG_Noise) // Direct & Noise have panpot
            {
                if (dyn.Panpot >= 0x80)
                    ForcedPan = (sbyte)((dyn.Panpot - 0x80) - 64);
            }

            if (Voice is Direct_Sound direct)
            {
                FixedFrequency = direct.Type == 0x8;
            }
            else // PSG instrument
            {
                A *= 17; D *= 17; S *= 17; R *= 17;
            }

            if (A == 0)
                A = 255;
            track.Instruments.Add(this);
        }
        internal void Stop()
        {
            if (State == ADSRState.Dead) return;
            CurrentVelocity = 0;
            State = ADSRState.Dead;
            if (Channel != null)
                Channel.stop();
            Track.Instruments.Remove(this);
        }

        int processStep = 0;
        internal void ADSRTick()
        {
            if (State == ADSRState.Dead) return;

            if (++processStep >= Constants.INTERFRAMES)
            {
                processStep = 0;

                switch (State)
                {
                    case ADSRState.Rising:
                        if (CurrentVelocity == 0)
                            SongPlayer.System.playSound(Sound, Track.Group, true, out Channel);
                        CurrentVelocity += A;
                        if (CurrentVelocity >= 0xFF)
                        {
                            CurrentVelocity = 0xFF;
                            State = ADSRState.Playing;
                        }
                        break;
                    case ADSRState.Playing:
                        CurrentVelocity = (CurrentVelocity * D) >> 8;
                        if (CurrentVelocity < S)
                            CurrentVelocity = S;
                        break;
                    case ADSRState.Releasing:
                        CurrentVelocity = (CurrentVelocity * R) >> 8;
                        if (CurrentVelocity <= 0)
                        {
                            Stop();
                            return;
                        }
                        break;
                }
            }
            UpdateFrequency();
        }
        internal void NoteTick()
        {
            Age++;
            if (State < ADSRState.Releasing && NoteDuration != -1 && Age >= NoteDuration)
                State = ADSRState.Releasing;
        }

        public override string ToString() => $"Note {Note}; Age {Age}; State {State}; Track {Track}";
    }
}
