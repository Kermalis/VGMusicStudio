using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GBAMusic.Core.M4AStructs;

namespace GBAMusic.Core
{
    internal class Instrument
    {
        internal bool Playing { get; private set; }
        internal bool Releasing { get; private set; }
        bool FixedFrequency;

        internal Track Track { get; private set; }
        internal FMOD.Channel Channel { get; private set; }

        ulong age;
        byte A, D, S, R, stage, noteWait;
        int velocity; // ADSR helper

        internal void UpdateFrequency()
        {
            if (Channel == null) return; // Can remove once all types are playing

            Channel.setPaused(true);
            Channel.getCurrentSound(out FMOD.Sound sound);
            sound.getDefaults(out float soundFrequency, out int soundPriority);
            float noteFrequency = (float)Math.Pow(2, ((Track.PrevNote - 60) / 12f)),
                bendFrequency = (float)Math.Pow(2, (Track.Bend * Track.BendRange) / (float)(64 * 12)),
                frequency = soundFrequency * noteFrequency * bendFrequency;
            Channel.setFrequency(FixedFrequency ? soundFrequency : frequency); // Not sure if fixed frequency ignores bends yet
            Channel.setVolume(Track.PrevVelocity / 127f);
            Channel.setPaused(false);
        }

        // Pass 255 to "wait" to trigger a TIE
        internal void Play(Track track, FMOD.System system, Dictionary<uint, FMOD.Sound> sounds, SVoice voice, byte wait)
        {
            if (voice is SDrum) return; // Can remove once all types are playing
            Track = track;
            noteWait = wait;
            Playing = true;
            Releasing = false;
            age = 0;
            velocity = 0;
            stage = 0;

            dynamic t = voice; // ADSR are in the same spot in each struct anyway
            A = t.A; D = t.D; S = t.S; R = t.R;

            if (voice is DirectSound direct)
            {
                FixedFrequency = direct.VoiceType == 0x8;
                system.playSound(sounds[direct.Address], Track.Group, true, out FMOD.Channel c);
                Channel = c;
            }
            else // GB instrument
            {
                FixedFrequency = false;
                A *= 17; D *= 17; S *= 17; R *= 17;
            }
            if (A == 0) A = 255;
            UpdateFrequency();
            track.PlayInstrument(this);
        }
        internal void Stop()
        {
            velocity = 0;
            stage++;
            Releasing = false;
            Playing = false;
            if (Channel != null)
                Channel.stop();
            if (Track != null)
            {
                Track.StopInstrument(this);
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
                    velocity += A;
                    if (velocity >= 255)
                    {
                        velocity = 255;
                        stage++;
                    }
                    else if (age > noteWait)
                        stage++;
                    break;
                case 1:
                    velocity = (velocity * D) / 256;
                    if (velocity < S)
                        velocity = S;
                    if (age >= noteWait)
                    {
                        stage++;
                        Releasing = true;
                    }
                    break;
                case 2:
                    velocity = (velocity * R) / 256;
                    if (velocity <= 0)
                    {
                        Stop();
                        return;
                    }
                    break;
            }
            Channel.setVolume(velocity / 255f);
            age++;
        }
    }
}
