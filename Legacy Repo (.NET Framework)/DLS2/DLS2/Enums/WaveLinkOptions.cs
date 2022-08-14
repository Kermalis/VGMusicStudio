using System;

namespace Kermalis.DLS2
{
    [Flags]
    public enum WaveLinkOptions : ushort
    {
        None = 0,
        PhaseMaster = 1 << 0,
        MultiChannel = 1 << 1
    }
}
