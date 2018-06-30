using GBAMusicStudio.Util;

namespace GBAMusicStudio.Core.M4A
{
    enum MODT : byte
    {
        Vibrate,
        Volume,
        Panpot
    }
    interface ICommand
    {
        string Name { get; }
        string M4AName { get; }

        string Arguments { get; }
    }
    internal class SongEvent
    {
        internal uint Offset, AbsoluteTicks;
        internal ICommand Command;

        internal SongEvent(uint offset, ICommand command)
        {
            Offset = offset;
            Command = command;
        }

        public override string ToString() => $"{Command}\t-\t0x{Offset.ToString("X")}\t-\t{AbsoluteTicks}";

        internal static sbyte RestFromCMD(byte startCMD, byte cmd)
        {
            sbyte[] added = { 4, 4, 2, 2 };
            sbyte wait = (sbyte)(cmd - startCMD);
            sbyte add = wait > 24 ? (sbyte)24 : wait;
            for (int i = 24 + 1; i <= wait; i++)
                add += added[i % 4];
            return add;
        }
        internal static sbyte[] RestToCMD = {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
            17, 18, 19, 20, 21, 22, 23, 24, 24, 24, 24, 28, 28, 30, 30,
            32, 32, 32, 32, 36, 36, 36, 36, 40, 40, 42, 42, 44, 44, 44,
            44, 48, 48, 48, 48, 52, 52, 54, 54, 56, 56, 56, 56, 60, 60,
            60, 60, 64, 64, 66, 66, 68, 68, 68, 68, 72, 72, 72, 72, 76,
            76, 78, 78, 80, 80, 80, 80, 84, 84, 84, 84, 88, 88, 90, 90,
            92, 92, 92, 92, 96, 96, 96, 96
        };
    }

    #region Commands

    internal class TempoCommand : ICommand
    {
        public byte Tempo;

        public string Name => "Tempo";
        public string M4AName => "TEMPO";
        public string Arguments => (Tempo * 2).ToString();
    }
    internal class RestCommand : ICommand
    {
        sbyte rest;
        public sbyte Rest { get => rest; set => rest = value.Clamp((sbyte)0, (sbyte)96); }

        public string Name => "Rest";
        public string M4AName => "W" + SongEvent.RestToCMD[Rest];
        public string Arguments => Rest.ToString();
    }
    internal class NoteCommand : ICommand
    {
        sbyte note, vel, dur;
        public sbyte Note { get => note; set => note = value.Clamp((sbyte)0, (sbyte)127); }
        public sbyte Velocity { get => vel; set => vel = value.Clamp((sbyte)0, (sbyte)127); }
        public sbyte Duration { get => dur; set => dur = value.Clamp((sbyte)-1, (sbyte)99); }

        public string Name => dur == -1 ? "TIE" : "Note On";
        public string M4AName => dur == -1 ? "TIE" : "N" + SongEvent.RestToCMD[dur];
        public string Arguments => $"{Utils.NoteName(note)} {vel} {dur}";
    }
    internal class EndOfTieCommand : ICommand
    {
        sbyte note;
        public sbyte Note { get => note; set => note = value.Clamp((sbyte)-1, (sbyte)127); }

        public string Name => "End Of Tie";
        public string M4AName => "EOT";
        public string Arguments => Utils.NoteName(note);
    }
    internal class VoiceCommand : ICommand
    {
        public byte Voice;

        public string Name => "Voice";
        public string M4AName => "VOICE";
        public string Arguments => Voice.ToString();
    }
    internal class VolumeCommand : ICommand
    {
        sbyte vol;
        public sbyte Volume { get => vol; set => vol = value.Clamp((sbyte)0, (sbyte)127); }

        public string Name => "Volume";
        public string M4AName => "VOL";
        public string Arguments => vol.ToString();
    }
    internal class PanpotCommand : ICommand
    {
        sbyte pan;
        public sbyte Panpot { get => pan; set => pan = value.Clamp((sbyte)-64, (sbyte)63); }

        public string Name => "Panpot";
        public string M4AName => "PAN";
        public string Arguments => pan.ToString();
    }
    internal class BendCommand : ICommand
    {
        sbyte bend;
        public sbyte Bend { get => bend; set => bend = value.Clamp((sbyte)-64, (sbyte)63); }

        public string Name => "Bend";
        public string M4AName => "BEND";
        public string Arguments => bend.ToString();
    }
    internal class TuneCommand : ICommand
    {
        sbyte tune;
        public sbyte Tune { get => tune; set => tune = value.Clamp((sbyte)-64, (sbyte)63); }

        public string Name => "Fine Tuning";
        public string M4AName => "TUNE";
        public string Arguments => tune.ToString();
    }
    internal class BendRangeCommand : ICommand
    {
        public byte Range;

        public string Name => "Bend Range";
        public string M4AName => "BENDR";
        public string Arguments => Range.ToString();
    }
    internal class LFOSpeedCommand : ICommand
    {
        public byte Speed;

        public string Name => "LFO Speed";
        public string M4AName => "LFOS";
        public string Arguments => Speed.ToString();
    }
    internal class LFODelayCommand : ICommand
    {
        public byte Delay;

        public string Name => "LFO Delay";
        public string M4AName => "LFODL";
        public string Arguments => Delay.ToString();
    }
    internal class ModDepthCommand : ICommand
    {
        public byte Depth;

        public string Name => "MOD Depth";
        public string M4AName => "MOD";
        public string Arguments => Depth.ToString();
    }
    internal class ModTypeCommand : ICommand
    {
        MODT type;
        public byte Type { get => (byte)type; set => type = (MODT)value.Clamp((byte)MODT.Vibrate, (byte)MODT.Panpot); }

        public string Name => "MOD Type";
        public string M4AName => "MODT";
        public string Arguments => type.ToString();
    }
    internal class PriorityCommand : ICommand
    {
        public byte Priority;

        public string Name => "Priority";
        public string M4AName => "PRIO";
        public string Arguments => Priority.ToString();
    }
    internal class KeyShiftCommand : ICommand
    {
        public sbyte Shift;

        public string Name => "Key Shift";
        public string M4AName => "KEYSH";
        public string Arguments => Shift.ToString();
    }
    internal class GoToCommand : ICommand
    {
        uint offset;
        public uint Offset { get => offset; set => offset = value.Clamp((uint)0, ROM.Capacity); }

        public string Name => "Go To";
        public string M4AName => "GOTO";
        public string Arguments => $"0x{offset:X}";
    }
    internal class FinishCommand : ICommand
    {
        public string Name => "Finish";
        public string M4AName => "FINE";
        public string Arguments => string.Empty;
    }

    #endregion

    #region M4A Commands

    internal class CallCommand : ICommand
    {
        uint offset;
        public uint Offset { get => offset; set => offset = value.Clamp((uint)0, ROM.Capacity); }

        public string Name => "Call";
        public string M4AName => "PATT";
        public string Arguments => $"0x{offset:X}";
    }
    internal class ReturnCommand : ICommand
    {
        public string Name => "Return";
        public string M4AName => "PEND";
        public string Arguments => string.Empty;
    }
    internal class RepeatCommand : ICommand
    {
        public byte Arg;

        public string Name => "Repeat";
        public string M4AName => "REPT";
        public string Arguments => Arg.ToString();
    }
    internal class MemoryAccessCommand : ICommand
    {
        public byte Arg1, Arg2, Arg3;

        public string Name => "Memory Access";
        public string M4AName => "MEMACC";
        public string Arguments => $"{Arg1}, {Arg2}, {Arg3}";
    }
    internal class LibraryCommand : ICommand
    {
        public byte Arg1, Arg2;

        public string Name => "Library Call";
        public string M4AName => "XCMD";
        public string Arguments => $"{Arg1}, {Arg2}";
    }

    #endregion
}
