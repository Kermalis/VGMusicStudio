using System;
using System.Collections.Generic;
using static GBAMusic.Core.M4AStructs;

namespace GBAMusic.Core
{
    internal class Instrument
    {
        internal bool Playing { get; private set; }
        internal bool Releasing { get; private set; }
        internal byte Note { get; private set; }
        internal byte NoteDuration { get; private set; }
        byte NoteVelocity;
        int Velocity;
        internal float Volume { get { return ((NoteVelocity / 127f) * Velocity) / 255f; } }
        internal float Panpot { get { return (ForcedPan != 0x7F ? ForcedPan : Track.Pan) / 64f; } }
        internal ulong Age { get; private set; }

        internal Track Track { get; private set; }
        internal FMOD.Channel Channel { get; private set; }

        byte A, D, S, R, stage;
        bool FixedFrequency;
        sbyte ForcedPan; // 0x7F counts as disabled

        internal void UpdatePanpot()
        {
            if (Channel == null || Track == null) return; // Can remove once all types are playing
            Channel.setPan(Panpot);
        }
        void UpdateVolume()
        {
            if (Channel == null || Track == null) return; // Can remove once all types are playing
            Channel.setVolume(Volume);
        }
        internal void UpdateFrequency()
        {
            if (Channel == null || Track == null) return; // Can remove once all types are playing

            Channel.setPaused(true);
            Channel.getCurrentSound(out FMOD.Sound sound);
            sound.getDefaults(out float soundFrequency, out int soundPriority);
            float noteFrequency = (float)Math.Pow(2, ((Note - 60) / 12f)),
                bendFrequency = (float)Math.Pow(2, (Track.Bend * Track.BendRange) / (float)(64 * 12)),
                frequency = soundFrequency * noteFrequency * bendFrequency;
            Channel.setFrequency(FixedFrequency ? soundFrequency : frequency); // Not sure if fixed frequency ignores bends yet
            UpdatePanpot();
            UpdateVolume();
            Channel.setPaused(false);
        }

        // Pass 0xFF to "duration" to trigger a TIE
        internal void Play(Track track, FMOD.System system, Dictionary<uint, FMOD.Sound> sounds, SVoice voice, byte duration)
        {
            if (voice is SDrum) return; // Can remove once all types are playing
            Track = track;
            NoteDuration = duration;
            Note = track.PrevNote;
            NoteVelocity = track.PrevVelocity;
            Playing = true;
            Releasing = false;
            Age = 0;
            Velocity = 0;
            stage = 0;

            FixedFrequency = false;
            ForcedPan = 0x7F;

            dynamic dyn = voice; // ADSR are in the same spot in each struct
            A = dyn.A; D = dyn.D; S = dyn.S; R = dyn.R;

            if (voice is DirectSound direct)
            {
                FixedFrequency = direct.VoiceType == 0x8;
                if (direct.Panpot >= 0x80) ForcedPan = (sbyte)((direct.Panpot ^ 0x80) - 64);
                system.playSound(sounds[direct.Address], Track.Group, true, out FMOD.Channel c);
                Channel = c;
            }
            else // GB instrument
            {
                if (voice is SquareWave1 || voice is SquareWave2)
                {
                    uint id = MusicPlayer.SQUARE12_ID - dyn.Pattern;
                    system.playSound(sounds[id], Track.Group, true, out FMOD.Channel c);
                    Channel = c;
                }
                A *= 17; D *= 17; S *= 17; R *= 17;
            }
            if (A == 0) A = 255;
            UpdateFrequency();
            track.Instruments.Add(this);
        }
        internal void TriggerRelease()
        {
            stage = 2; // Make enum
            Releasing = true;
        }
        internal void Stop()
        {
            Velocity = 0;
            stage = 3; // Should make these an enum
            Releasing = false;
            Playing = false;
            if (Channel != null)
            {
                Channel.setVolume(0);
                Channel.stop();
            }
            if (Track != null)
            {
                Track.Instruments.Remove(this);
                Track = null;
            }
        }

        internal void Tick()
        {
            if (!Playing) return;
            if (Channel == null) return; // Can remove once all types are playing

            switch (stage)
            {
                case 0:
                    Velocity += A;
                    if (Velocity >= 255)
                    {
                        Velocity = 255;
                        stage++;
                    }
                    else if (NoteDuration != 0xFF && Age > NoteDuration)
                        stage++;
                    break;
                case 1:
                    Velocity = (Velocity * D) / 256;
                    if (Velocity < S)
                        Velocity = S;
                    if (NoteDuration != 0xFF && Age >= NoteDuration)
                        TriggerRelease();
                    break;
                case 2:
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
