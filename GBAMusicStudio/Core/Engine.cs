using System;

namespace GBAMusicStudio.Core
{
    internal enum AEngine
    {
        M4A,
        MLSS
    }
    internal static class Engine
    {
        internal const int BPM_PER_FRAME = 150, INTERFRAMES = 4;
        static readonly Exception BAD = new PlatformNotSupportedException("Invalid game engine.");

        internal static ushort GetDefaultTempo()
        {
            switch (ROM.Instance.Game.Engine)
            {
                case AEngine.M4A: return 150;
                case AEngine.MLSS: return 120;
            }
            throw BAD;
        }
        internal static int GetTicksPerBar()
        {
            switch (ROM.Instance.Game.Engine)
            {
                case AEngine.M4A: return 96;
                case AEngine.MLSS: return 48;
            }
            throw BAD;
        }
        internal static int GetTempoWait()
        {
            int baseWait = BPM_PER_FRAME * INTERFRAMES;
            return baseWait / (96 / GetTicksPerBar());
        }
    }
}
