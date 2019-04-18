using System;
using System.Collections.Generic;
using System.Linq;

namespace Kermalis.MusicStudio.Core
{
    static class Engine
    {
        public const int BPM_PER_FRAME = 150, AGB_FPS = 60;
        static readonly Exception BAD = new PlatformNotSupportedException("Invalid game engine.");

        static readonly Dictionary<EngineType, ICommand[]> allowedCommands;
        static Engine()
        {
            var types = new Dictionary<EngineType, Type[]>()
            {
                { EngineType.M4A, new Type[] {
                    typeof(TempoCommand), typeof(RestCommand), typeof(M4ANoteCommand), typeof(EndOfTieCommand),
                    typeof(VoiceCommand), typeof(VolumeCommand), typeof(PanpotCommand), typeof(BendCommand),
                    typeof(TuneCommand), typeof(BendRangeCommand), typeof(LFOSpeedCommand), typeof(LFODelayCommand),
                    typeof(ModDepthCommand), typeof(ModTypeCommand), typeof(PriorityCommand), typeof(KeyShiftCommand),
                    typeof(GoToCommand), typeof(CallCommand), typeof(ReturnCommand), typeof(M4AFinishCommand),
                    typeof(RepeatCommand), typeof(MemoryAccessCommand), typeof(LibraryCommand)
                } },
                { EngineType.MLSS, new Type[] {
                    typeof(TempoCommand), typeof(RestCommand), typeof(MLSSNoteCommand), typeof(VoiceCommand),
                    typeof(VolumeCommand), typeof(PanpotCommand), typeof(GoToCommand), typeof(FinishCommand),
                    typeof(FreeNoteCommand)
                } }
            };

            allowedCommands = new Dictionary<EngineType, ICommand[]>();
            foreach (KeyValuePair<EngineType, Type[]> pair in types)
            {
                ICommand[] commands = pair.Value.Select(type => (ICommand)Activator.CreateInstance(type)).ToArray();
                allowedCommands.Add(pair.Key, commands);
            }
        }

        public static ICommand[] GetCommands()
        {
            return allowedCommands[ROM.Instance.Game.Engine.Type];
        }

        public static short GetDefaultTempo()
        {
            switch (ROM.Instance.Game.Engine.Type)
            {
                case EngineType.M4A: return 150;
                case EngineType.MLSS: return 120;
                default: throw BAD;
            }
        }
        public static int GetTicksPerBar()
        {
            switch (ROM.Instance.Game.Engine.Type)
            {
                case EngineType.M4A: return 96;
                case EngineType.MLSS: return 48;
                default: throw BAD;
            }
        }
        public static int GetTempoWait()
        {
            return BPM_PER_FRAME / (96 / GetTicksPerBar());
        }

        public static byte GetMaxVolume()
        {
            switch (ROM.Instance.Game.Engine.Type)
            {
                case EngineType.M4A: return 0x7F;
                case EngineType.MLSS: return 0xFF;
                default: throw BAD;
            }
        }
        public static byte GetPanpotRange()
        {
            switch (ROM.Instance.Game.Engine.Type)
            {
                case EngineType.M4A: return 0x40;
                case EngineType.MLSS: return 0x80;
                default: throw BAD;
            }
        }
        public static byte GetBendingRange()
        {
            switch (ROM.Instance.Game.Engine.Type)
            {
                case EngineType.M4A: return 0x40;
                case EngineType.MLSS: return 0x80;
                default: throw BAD;
            }
        }
    }
}
