using System;
using System.Collections.Generic;
using static GBAMusic.Core.M4AStructs;

namespace GBAMusic.Core
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
        internal bool Playing { get; private set; }
        internal bool Releasing { get; private set; }
        internal float Volume { get { return ((NoteVelocity / 127f) * Velocity) / 255f; } }
        internal float Panpot { get { return (ForcedPan != 0x7F ? ForcedPan : Track.Pan) / 64f; } }
        internal ulong Age { get; private set; }

        internal Track Track { get; private set; }
        internal FMOD.Channel Channel { get; private set; }
        internal byte Note { get; private set; }
        internal byte NoteDuration { get; private set; }
        byte NoteVelocity;
        int Velocity;
        byte RootNote;

        byte A, D, S, R;
        ADSRState state;
        bool FixedFrequency;
        sbyte ForcedPan; // 0x7F counts as disabled

        internal void UpdatePanpot()
        {
            Channel.setPan(Panpot);
        }
        void UpdateVolume()
        {
            Channel.setVolume(Volume);
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
        internal void Play(Track track, FMOD.System system, Dictionary<uint, FMOD.Sound> sounds, Voice voice, byte note, byte duration)
        {
            Track = track;
            Note = note;
            RootNote = voice.RootNote;
            NoteDuration = duration;
            NoteVelocity = track.PrevVelocity;
            Playing = true;
            Releasing = false;
            Age = 0;
            Velocity = 0;
            state = ADSRState.Playing;

            dynamic dyn = voice; // ADSR are in the same spot in each struct
            FixedFrequency = false;
            ForcedPan = 0x7F;
            A = dyn.A; D = dyn.D; S = dyn.S; R = dyn.R;
            FMOD.Sound sound = null;

            if (voice is DirectSound direct)
            {
                FixedFrequency = direct.VoiceType == 0x8;
                if (direct.Panpot >= 0x80) ForcedPan = (sbyte)((direct.Panpot ^ 0x80) - 64);
                sound = sounds[direct.Address];
            }
            else // GB instrument
            {
                if (voice is SquareWave1 || voice is SquareWave2)
                    sound = sounds[MusicPlayer.SQUARE12_ID - dyn.Pattern];
                else if (voice is GBWave wave)
                    sound = sounds[wave.Address];
                else if (voice is Noise noise)
                    sound = sounds[MusicPlayer.NOISE_ID];
                A *= 17; D *= 17; S *= 17; R *= 17;
            }

            system.playSound(sound, Track.Group, true, out FMOD.Channel c);
            Channel = c;
            if (A == 0) A = 255;
            UpdateFrequency();
            track.Instruments.Add(this);
        }
        internal void TriggerRelease()
        {
            state = ADSRState.Releasing;
            Releasing = true;
        }
        internal void Stop()
        {
            Playing = false;
            Releasing = false;
            Velocity = 0;
            state = ADSRState.Dead;
            if (Age != 0)
            {
                Channel.setVolume(0);
                Channel.stop();
                Track.Instruments.Remove(this);
            }
        }

        internal void Tick()
        {
            if (!Playing) return;

            switch (state)
            {
                case ADSRState.Rising:
                    Velocity += A;
                    if (Velocity >= 255)
                    {
                        Velocity = 255;
                        state++;
                    }
                    else if (NoteDuration != 0xFF && Age > NoteDuration)
                        state++;
                    break;
                case ADSRState.Playing:
                    Velocity = (Velocity * D) / 256;
                    if (Velocity < S)
                        Velocity = S;
                    if (NoteDuration != 0xFF && Age >= NoteDuration)
                        TriggerRelease();
                    break;
                case ADSRState.Releasing:
                    Velocity = (Velocity * R) / 256;
                    if (Velocity <= 0)
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
