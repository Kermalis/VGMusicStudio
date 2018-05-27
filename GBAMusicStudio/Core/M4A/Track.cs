using ThreadSafeList;

namespace GBAMusicStudio.Core.M4A
{
    internal class Track : ROMReader
    {
        internal readonly FMOD.ChannelGroup Group;
        internal readonly ThreadSafeList<Instrument> Instruments; // Instruments being played by this track
        readonly FMOD.DSP Mod2;

        internal byte Voice, Volume, Priority,
            Delay,
            RunCmd, PrevNote, PrevVelocity,
            BendRange, MODDepth, MODType;
        internal sbyte Bend, Pan;
        internal bool Stopped;
        internal uint EndOfPattern;

        internal Track(FMOD.System system) : base()
        {
            Instruments = new ThreadSafeList<Instrument>();
            system.createChannelGroup(null, out Group);
            system.createDSPByType(FMOD.DSP_TYPE.TREMOLO, out Mod2);
            Group.addDSP(2, Mod2);
        }

        internal void Init(uint offset)
        {
            InitReader();
            SetOffset(offset);
            Voice = Volume = Priority
                = Delay
                = RunCmd = PrevNote = PrevVelocity
                = MODDepth = MODType = 0;
            Bend = Pan = 0;
            BendRange = 2;
            Stopped = false;
            EndOfPattern = 0;
        }
        internal void Tick()
        {
            foreach (Instrument i in Instruments)
                i.Tick();
            if (Delay != 0)
                Delay--;
        }

        void UpdateFrequencies()
        {
            foreach (Instrument i in Instruments)
                i.UpdateFrequency();
        }
        void UpdatePanpots()
        {
            foreach (Instrument i in Instruments)
                i.UpdatePanpot();
        }
        void UpdateModulation()
        {
            float depth = MODDepth / 127f;
            switch (MODType)
            {
                case 2: Mod2.setParameterFloat((int)FMOD.DSP_TREMOLO.DEPTH, depth); break;
            }
        }

        internal void SetVolume(byte b)
        {
            Volume = b;
            Group.setVolume(b / 127f);
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
        internal void SetMODDepth(byte b)
        {
            MODDepth = b;
            UpdateModulation();
        }
        internal void SetMODType(byte b)
        {
            MODType = b;
            UpdateModulation();
        }
        internal void SetPriority(byte b)
        {
            Priority = b;
            foreach (Instrument i in Instruments)
                i.SetPriority(b);
        }
    }
}
