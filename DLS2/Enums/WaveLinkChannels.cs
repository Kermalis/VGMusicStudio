using System;

namespace Kermalis.DLS2
{
    [Flags]
    public enum WaveLinkChannels : uint
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Center = 1 << 2,
        LowFrequencyEnergy = 1 << 3,
        SurroundLeft = 1 << 4,
        SurroundRight = 1 << 5,
        LeftOfCenter = 1 << 6,
        RightOfCenter = 1 << 7,
        SurroundCenter = 1 << 8,
        SideLeft = 1 << 9,
        SideRight = 1 << 10,
        Top = 1 << 11,
        TopFrontLeft = 1 << 12,
        TopFrontCenter = 1 << 13,
        TopFrontRight = 1 << 14,
        TopRearLeft = 1 << 15,
        TopRearCenter = 1 << 16,
        TopRearRight = 1 << 17
    }
}
