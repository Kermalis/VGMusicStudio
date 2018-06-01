using GBAMusicStudio.Util;
using ThreadSafeList;

namespace GBAMusicStudio.Core.M4A
{
    enum MODT
    {
        Vibrate,
        Volume,
        Panpot
    }

    internal class Track : ROMReader
    {
        internal readonly byte Index;
        internal readonly FMOD.ChannelGroup Group;
        internal readonly ThreadSafeList<Instrument> Instruments; // Instruments being played by this track

        internal byte Voice, Volume, Priority,
            Delay, LFOPhase, LFODelayCount,
            RunCmd, PrevNote, PrevVelocity,
            BendRange, LFOSpeed, LFODelay, MODDepth;
        MODT MODType;
        internal sbyte Bend, Tune, Pan, KeyShift;
        internal bool Ready, Stopped;
        internal uint EndOfPattern;

        int Tri(int index)
        {
            index = (index - 64) & 0xFF;
            return (index < 128) ? index * 12 - 768 : 2304 - index * 12;
        }

        internal int APitch
        {
            get
            {
                int mod = MODType == MODT.Vibrate ? (Tri(LFOPhase) * MODDepth) >> 8 : 0;
                return Bend * BendRange + Tune + mod;
            }
        }
        internal float AVolume
        {
            get
            {
                int mod = MODType == MODT.Volume ? (Tri(LFOPhase) * MODDepth * 3 * Volume) >> 19 : 0;
                return (Volume + mod).Clamp(0, 127) / 127f;
            }
        }
        internal float APan
        {
            get
            {
                int mod = MODType == MODT.Panpot ? (Tri(LFOPhase) * MODDepth * 3) >> 12 : 0;
                return (Pan + mod).Clamp(-64, 63) / 64f;
            }
        }

        internal Track(byte i) : base()
        {
            Index = i;
            Instruments = new ThreadSafeList<Instrument>();
            MusicPlayer.System.createChannelGroup(null, out Group);
        }

        internal void Init(uint offset)
        {
            InitReader();
            SetOffset(offset);
            Voice = Priority
                = Delay = RunCmd
                = LFODelay = LFODelayCount = LFOPhase = MODDepth = 0;
            Bend = Tune = Pan = KeyShift = 0;
            EndOfPattern = 0;
            MODType = MODT.Vibrate;
            Ready = Stopped = false;
            PrevNote = 60;
            PrevVelocity = 127;
            BendRange = 2;
            LFOSpeed = 22;
            Volume = 100;
        }

        internal void Tick()
        {
            if (Delay != 0)
                Delay--;
            foreach (Instrument i in Instruments)
                i.Tick();
            if (Instruments.Count > 0)
            {
                if (LFODelayCount > 0)
                {
                    LFODelayCount--;
                    LFOPhase = 0;
                }
                else
                {
                    LFOPhase += LFOSpeed;
                }
            }
            else
            {
                LFOPhase = 0;
                LFODelayCount = LFODelay;
            }
        }

        void UpdateFrequencies()
        {
            foreach (Instrument i in Instruments)
                i.UpdateFrequency();
        }
        void UpdateVolumes()
        {
            foreach (Instrument i in Instruments)
                i.UpdateVolume();
        }
        void UpdatePanpots()
        {
            foreach (Instrument i in Instruments)
                i.UpdatePanpot();
        }
        void UpdateModulation()
        {
            switch (MODType)
            {
                case MODT.Vibrate: UpdateFrequencies(); break;
                case MODT.Volume: UpdateVolumes(); break;
                case MODT.Panpot: UpdatePanpots(); break;
            }
        }

        internal void SetPriority(byte b)
        {
            Priority = b;
            foreach (Instrument i in Instruments)
                i.SetPriority(b);
        }
        internal void SetVoice(byte b)
        {
            Voice = b;
            Ready = true;
        }
        internal void SetVolume(byte b)
        {
            Volume = b;
            UpdateVolumes();
        }
        internal void SetPan(byte b)
        {
            Pan = (sbyte)(b - 64);
            UpdatePanpots();
        }
        internal void SetBend(byte b)
        {
            Bend = (sbyte)(b - 64);
            UpdateFrequencies();
        }
        internal void SetBendRange(byte b)
        {
            BendRange = b;
            UpdateFrequencies();
        }
        internal void SetLFOSpeed(byte b)
        {
            LFOSpeed = b;
            LFOPhase = LFODelayCount = 0;
            UpdateModulation();
        }
        internal void SetLFODelay(byte b)
        {
            LFODelay = b;
            LFOPhase = LFODelayCount = 0;
            UpdateModulation();
        }
        internal void SetMODDepth(byte b)
        {
            MODDepth = b;
            UpdateModulation();
        }
        internal void SetMODType(byte b)
        {
            MODType = (MODT)b;
            UpdateModulation();
        }
        internal void SetTune(byte b)
        {
            Tune = (sbyte)(b - 64);
            UpdateFrequencies();
        }
    }
}
