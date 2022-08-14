namespace Kermalis.DLS2
{
    public enum Level1ArticulatorSource : ushort
    {
        None = 0x0,
        LFO = 0x1,
        KeyOnVelocity = 0x2,
        KeyNumber = 0x3,
        EG1 = 0x4,
        EG2 = 0x5,
        PitchWheel = 0x6,
        Modulation_CC1 = 0x81,
        ChannelVolume_CC7 = 0x87,
        Pan_CC10 = 0x8A,
        Expression_CC11 = 0x8B,
        PitchBendRange_RPN0 = 0x100,
        FineTune_RPN1 = 0x101,
        CoarseTune_RPN2 = 0x102
    }

    public enum Level1ArticulatorDestination : ushort
    {
        None = 0x0,
        Gain = 0x1,
        Pitch = 0x3,
        Pan = 0x4,
        LFOFrequency = 0x104,
        LFOStartDelay = 0x105,
        EG1AttackTime = 0x206,
        EG1DecayTime = 0x207,
        EG1ReleaseTime = 0x209,
        EG1SustainLevel = 0x20A,
        EG2AttackTime = 0x30A,
        EG2DecayTime = 0x30B,
        EG2ReleaseTime = 0x30D,
        EG2SustainLevel = 0x30E
    }

    public enum Level1ArticulatorTransform : byte
    {
        None = 0x0,
        Concave = 0x1
    }
}
