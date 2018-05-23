using System.Collections.Generic;

namespace GBAMusic.Core
{
    internal class Track : ROMReader
    {
        internal readonly FMOD.ChannelGroup Group;
        internal readonly List<Instrument> Instruments; // Instruments being played by this track
        readonly FMOD.DSP Mod2;

        internal byte Voice, Volume, Priority,
            Delay,
            PrevCmd, PrevNote, PrevVelocity,
            BendRange, MODDepth, MODType;
        internal sbyte Bend;
        internal bool Stopped;
        internal uint EndOfPattern;

        internal Track(FMOD.System system, FMOD.ChannelGroup parentGroup) : base()
        {
            InitReader();
            Instruments = new List<Instrument>();
            system.createChannelGroup(null, out Group);
            parentGroup.addGroup(Group, false, out FMOD.DSPConnection c);

            system.createDSPByType(FMOD.DSP_TYPE.TREMOLO, out Mod2);
            Group.addDSP(2, Mod2);
        }

        internal void Init(uint offset)
        {
            SetOffset(offset);
            Voice = Volume = Priority
                = Delay
                = PrevCmd = PrevNote = PrevVelocity
                = BendRange = MODDepth = MODType = 0;
            Bend = 0;
            Stopped = false;
            EndOfPattern = 0;
        }
        internal void Tick()
        {
            foreach (Instrument i in Instruments.ToArray()) // The list can update from Tick()
                i.Tick();
            if (Delay != 0)
                Delay--;
        }

        void UpdateFrequencies()
        {
            foreach (Instrument i in Instruments)
            {
                i.UpdateFrequency();
            }
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
        internal void SetPan(byte b) => Group.setPan((b - 64) / 64f);
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
                i.Channel.setPriority(b);
        }
    }
}
