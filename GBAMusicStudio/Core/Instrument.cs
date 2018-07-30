using GBAMusicStudio.Util;
using System;

namespace GBAMusicStudio.Core
{
    enum ADSRState
    {
        Rising,
        Playing,
        Releasing,
        Dead
    }

    internal class Instrument
    {
        internal ADSRState State = ADSRState.Dead;
        internal float Velocity { get { return ((NoteVelocity / 127f) * CurrentVelocity) / 255f; } }
        internal float Panpot { get { return ForcedPan != 0xFFF ? ForcedPan / (float)Engine.GetPanpotRange() : Track.APan; } }
        internal int Age { get; private set; }

        internal Track Track { get; private set; }
        IVoice Voice;
        bool fromDrum;
        FMOD.Channel Channel;
        FMOD.Sound Sound;
        internal sbyte DisplayNote { get; private set; }
        sbyte Note;
        internal int NoteDuration { get; private set; }
        byte NoteVelocity;
        int CurrentVelocity;

        byte A, D, S, R;
        bool FixedFrequency;
        int ForcedPan; // 0xFFF counts as disabled

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
            byte volume = (byte)((Velocity * Track.AVolume) * 255);
            if (volume > 0 && volume < 0xFF && !(Voice is M4ADirect_Sound))
            {
                if (Voice is M4APSG_Wave)
                    volume = (byte)((volume / (253 / 3) + 1) * (255 / 4));
                else
                    volume = (byte)((volume / (253 / 14) + 1) * (255 / 15));
            }
            Channel.setVolume(volume / 255f);
        }
        internal void UpdateFrequency()
        {
            if (Channel == null) return;
            Channel.getCurrentSound(out FMOD.Sound sound);
            sound.getDefaults(out float soundFrequency, out int soundPriority);
            float frequency;
            byte root = fromDrum ? Voice.GetRootNote() : (byte)60;
            if (Voice is M4ADirect_Sound)
            {
                frequency = soundFrequency * (float)Math.Pow(2, (Note - (120 - root)) / 12f + Track.APitch / 768f);
            }
            else if (Voice is M4APSG_Noise)
            {
                frequency = (0x1000 * (float)Math.Pow(8, (Note - (120 - root)) / 12f + Track.APitch / 768f)).Clamp(8, 0x80000); // Thanks ipatix
            }
            else
            {
                float fundamental = 220 * (float)Math.Pow(2, (Note - 69) / 12f + Track.APitch / 768f);
                frequency = fundamental * 0x10;
                if (Voice is M4APSG_Wave)
                    frequency *= 2;
            }
            Channel.setFrequency(FixedFrequency ? soundFrequency : frequency);
            UpdatePanpot();
            UpdateVolume();
            Channel.setPaused(false);
        }

        // Pass -1 to "duration" to trigger a TIE
        internal void Play(Track track, sbyte note, byte velocity, int duration)
        {
            Stop();
            Voice = SongPlayer.Song.VoiceTable.GetVoiceFromNote(track.Voice, note, out fromDrum);
            Sound = SongPlayer.Song.VoiceTable.GetSoundFromNote(track.Voice, note);

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
            ForcedPan = 0xFFF;
            A = dyn.A; D = dyn.D; S = dyn.S; R = dyn.R;

            if (Voice is M4APSG_Square_1 square1) // Square1s have sweeping
            {
                // TODO: Add sweeping
            }
            else if (Voice is M4ADirect_Sound || Voice is M4APSG_Noise) // Direct & Noise have panpot
            {
                if (dyn.Panpot >= 0x80)
                    ForcedPan = (sbyte)((dyn.Panpot - 0x80) - 64);
            }

            if (Voice is M4ADirect_Sound direct)
            {
                FixedFrequency = direct.Type == 0x8;
            }
            else // PSG instrument
            {
                A = (byte)(0xFF - A * 32);
                D *= 32;
                S *= 16;
                R *= 32;
            }

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

            if (++processStep >= Engine.INTERFRAMES)
            {
                processStep = 0;

                again:
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
                            goto again;
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
                if (State < ADSRState.Releasing && NoteDuration != -1 && Age >= NoteDuration)
                {
                    State = ADSRState.Releasing;
                    goto again;
                }
            }
            UpdateFrequency();
        }
        internal void NoteTick()
        {
            Age++;
        }

        public override string ToString() => $"Note {Note}; Age {Age}; State {State}; Track {Track}";
    }
}
