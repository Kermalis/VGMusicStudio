using GBAMusicStudio.Util;

namespace GBAMusicStudio.Core
{
    class Track
    {
        public readonly byte Index;

        public byte Voice, Priority, Volume, Delay,
            LFOPhase, LFODelayCount, BendRange,
            LFOSpeed, LFODelay, MODDepth,
            EchoVolume, EchoLength; // Unused for now
        public MODType MODType;
        public sbyte Bend, Tune, Pan, KeyShift, PrevNote;
        public int CommandIndex, EndOfPattern;
        public bool Ready, Stopped;

        int Tri(int index)
        {
            index = (index - 64) & 0xFF;
            return (index < 128) ? index * 12 - 768 : 2304 - index * 12;
        }

        public int GetPitch()
        {
            int mod = MODType == MODType.Vibrate ? (Tri(LFOPhase) * MODDepth) >> 8 : 0;
            byte range = BendRange;
            if (ROM.Instance.Game.Engine.Type == EngineType.MLSS)
                range /= 2;
            return Bend * range + Tune + mod;
        }
        public byte GetVolume()
        {
            int mod = MODType == MODType.Volume ? (Tri(LFOPhase) * MODDepth * 3 * Volume) >> 19 : 0;
            return (byte)(Volume + mod).Clamp(0, Engine.GetMaxVolume());
        }
        public sbyte GetPan()
        {
            int mod = MODType == MODType.Panpot ? (Tri(LFOPhase) * MODDepth * 3) >> 12 : 0;
            byte range = Engine.GetPanpotRange();
            return (sbyte)(Pan + mod).Clamp(-range, range - 1);
        }

        public Track(byte i) => Index = i;
        public virtual void Init()
        {
            Voice = Priority = Delay = LFODelay = LFODelayCount = LFOPhase = MODDepth = EchoVolume = EchoLength = BendRange = 0;
            Bend = Tune = Pan = KeyShift = 0;
            CommandIndex = EndOfPattern = 0;
            MODType = MODType.Vibrate;
            Ready = true;
            Stopped = false;
            LFOSpeed = 22;
            Volume = 127;
        }
        public void Tick()
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

    class M4ATrack : Track
    {
        public M4ATrack(byte i) : base(i) { }

        public override void Init()
        {
            base.Init();

            Ready = false;
            BendRange = 2;
            Volume = 100;
        }
    }

    class MLSSTrack : Track
    {
        public MLSSTrack(byte i) : base(i) { }
    }
}
