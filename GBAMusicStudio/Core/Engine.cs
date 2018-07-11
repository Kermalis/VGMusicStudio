using System;
using System.Collections.Generic;
using System.Linq;

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

        static readonly Dictionary<AEngine, ICommand[]> allowedCommands;
        static Engine()
        {
            var types = new Dictionary<AEngine, Type[]>()
            {
                { AEngine.M4A, new Type[] {
                    typeof(TempoCommand), typeof(RestCommand), typeof(M4ANoteCommand), typeof(EndOfTieCommand),
                    typeof(VoiceCommand), typeof(VolumeCommand), typeof(PanpotCommand), typeof(BendCommand),
                    typeof(TuneCommand), typeof(BendRangeCommand), typeof(LFOSpeedCommand), typeof(LFODelayCommand),
                    typeof(ModDepthCommand), typeof(ModTypeCommand), typeof(PriorityCommand), typeof(KeyShiftCommand),
                    typeof(GoToCommand), typeof(CallCommand), typeof(ReturnCommand), typeof(M4AFinishCommand),
                    typeof(RepeatCommand), typeof(MemoryAccessCommand), typeof(LibraryCommand)
                } },
                { AEngine.MLSS, new Type[] {
                    typeof(TempoCommand), typeof(RestCommand), typeof(MLSSNoteCommand), typeof(VoiceCommand),
                    typeof(VolumeCommand), typeof(PanpotCommand), typeof(GoToCommand), typeof(FinishCommand),
                    typeof(FreeNoteCommand)
                } }
            };

            allowedCommands = new Dictionary<AEngine, ICommand[]>();
            foreach (var pair in types)
            {
                var commands = pair.Value.Select(type => (ICommand)Activator.CreateInstance(type)).ToArray();
                allowedCommands.Add(pair.Key, commands);
            }
        }
        
        internal static ICommand[] GetCommands()
        {
            return allowedCommands[ROM.Instance.Game.Engine];
        }

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

        internal static byte GetMaxVolume()
        {
            switch (ROM.Instance.Game.Engine)
            {
                case AEngine.M4A: return 0x7F;
                case AEngine.MLSS: return 0xFF;
            }
            throw BAD;
        }
        internal static byte GetPanpotRange()
        {
            switch (ROM.Instance.Game.Engine)
            {
                case AEngine.M4A: return 0x40;
                case AEngine.MLSS: return 0x80;
            }
            throw BAD;
        }
    }
}
