using System;

namespace Kermalis.DLS2
{
    [Flags]
    public enum WaveSampleOptions : uint
    {
        None = 0,
        NoTruncation = 1 << 0,
        NoCompression = 1 << 1
    }
}
