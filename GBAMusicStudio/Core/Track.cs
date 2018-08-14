using GBAMusicStudio.Util;

namespace GBAMusicStudio.Core
{
    internal class Track
    {
        internal readonly byte Index;

        internal byte Voice, Priority, Volume, Delay,
            LFOPhase, LFODelayCount, BendRange,
            LFOSpeed, LFODelay, MODDepth,
            EchoVolume, EchoLength; // Unused for now
        internal MODT MODType;
        internal sbyte Bend, Tune, Pan, KeyShift, PrevNote;
        internal int CommandIndex, EndOfPattern;
        internal bool Ready, Stopped;

        int Tri(int index)
        {
            index = (index - 64) & 0xFF;
            return (index < 128) ? index * 12 - 768 : 2304 - index * 12;
        }

        internal int GetPitch()
        {
            int mod = MODType == MODT.Vibrate ? (Tri(LFOPhase) * MODDepth) >> 8 : 0;
            return Bend * BendRange + Tune + mod;
        }
        internal byte GetVolume()
        {
            int mod = MODType == MODT.Volume ? (Tri(LFOPhase) * MODDepth * 3 * Volume) >> 19 : 0;
            return (byte)(Volume + mod).Clamp(0, Engine.GetMaxVolume());
        }
        internal sbyte GetPan()
        {
            int mod = MODType == MODT.Panpot ? (Tri(LFOPhase) * MODDepth * 3) >> 12 : 0;
            byte range = Engine.GetPanpotRange();
            return (sbyte)(Pan + mod).Clamp(-range, range - 1);
        }

        internal Track(byte i) => Index = i;
        internal virtual void Init()
        {
            Voice = Priority = Delay = LFODelay = LFODelayCount = LFOPhase = MODDepth = EchoVolume = EchoLength = 0;
            Bend = Tune = Pan = KeyShift = 0;
            CommandIndex = EndOfPattern = 0;
            MODType = MODT.Vibrate;
            Ready = true;
            Stopped = false;
            BendRange = 2;
            LFOSpeed = 22;
            Volume = 127;
        }
        internal void Tick()
        {
            if (Delay != 0)
                Delay--;
            if (SoundMixer.TickNotes(Index) > 0)
            {
                if (LFODelayCount > 0)
                {
                    LFODelayCount--;
                    LFOPhase = 0;
                }
                else
                {
                    LFOPhase = (byte)(LFOPhase + LFOSpeed);
                }
            }
            else
            {
                LFOPhase = 0;
                LFODelayCount = LFODelay;
            }
        }

        public override string ToString() => $"Track {Index}; Voice {Voice}";
    }

    internal class M4ATrack : Track
    {
        internal M4ATrack(byte i) : base(i) { }

        internal override void Init()
        {
            base.Init();

            Ready = false;
            Volume = 100;
        }
    }

    internal class MLSSTrack : Track
    {
        internal MLSSTrack(byte i) : base(i) { }
    }
}
