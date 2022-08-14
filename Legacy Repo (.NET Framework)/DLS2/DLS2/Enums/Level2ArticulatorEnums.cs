namespace Kermalis.DLS2
{
    public enum Level2ArticulatorSource : ushort
    {
        None = 0x0,
        LFO = 0x1,
        KeyOnVelocity = 0x2,
        KeyNumber = 0x3,
        EG1 = 0x4,
        EG2 = 0x5,
        PitchWheel = 0x6,
        PolyPressure = 0x7,
        ChannelPressure = 0x8,
        Vibrato = 0x9,
        Modulation_CC1 = 0x81,
        ChannelVolume_CC7 = 0x87,
        Pan_CC10 = 0x8A,
        Expression_CC11 = 0x8B,
        ChorusSend_CC91 = 0xDB,
        Reverb_SendCC93 = 0xDD,
        PitchBendRange_RPN0 = 0x100,
        FineTune_RPN1 = 0x101,
        CoarseTune_RPN2 = 0x102
    }

    public enum Level2ArticulatorDestination : ushort
    {
        None = 0x0,
        Gain = 0x1,
        Pitch = 0x3,
        Pan = 0x4,
        KeyNumber = 0x5,
        Left = 0x10,
        Right = 0x11,
        Center = 0x12,
        LFEChannel = 0x13,
        LeftRear = 0x14,
        RightRear = 0x15,
        Chorus = 0x80,
        Reverb = 0x81,
        LFOFrequency = 0x104,
        LFOStartDelay = 0x105,
        VIBFrequency = 0x114,
        VIBStartDelay = 0x115,
        EG1AttackTime = 0x206,
        EG1DecayTime = 0x207,
        EG1ReleaseTime = 0x209,
        EG1SustainLevel = 0x20A,
        EG1DelayTime = 0x20B,
        EG1HoldTime = 0x20C,
        EG1ShutdownTime = 0x20D,
        EG2AttackTime = 0x30A,
        EG2DecayTime = 0x30B,
        EG2ReleaseTime = 0x30D,
        EG2SustainLevel = 0x30E,
        EG2DelayTime = 0x30F,
        EG2HoldTime = 0x310,
        FilterCutoff = 0x500,
        FilterResonance = 0x501
    }

    public enum Level2ArticulatorTransform : byte
    {
        None = 0x0,
        Concave = 0x1,
        Convex = 0x2,
        Switch = 0x3
    }
}
