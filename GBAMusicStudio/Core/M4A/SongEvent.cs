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
        string Arguments { get; }
    }
    internal class SongEvent
    {
        public uint Offset, AbsoluteTicks;
        public ICommand Command;

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

        static readonly string[] m4aNotes = { "Cn", "Cs", "Dn", "Ds", "En", "Fn", "Fs", "Gn", "Gs", "An", "As", "Bn" };
        static readonly string[] notes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        public static string NoteName(sbyte note, bool m4aStyle = false)
        {
            if (note < 0)
                return note.ToString();
            var style = m4aStyle ? m4aNotes : notes;
            string str = style[note % 12] + ((note / 12) - 2);
            if (m4aStyle) str = str.Replace('-', 'M');
            return str;
        }

        internal static string CenterValueString(sbyte value)
        {
            return string.Format("c_v{0}{1}", value >= 0 ? "+" : "", value);
        }
    }

    #region Commands

    internal class TempoCommand : ICommand
    {
        public string Name => "Tempo";

        public byte Tempo;

        public string Arguments => (Tempo * 2).ToString();
    }
    internal class RestCommand : ICommand
    {
        public string Name => "Rest";

        sbyte rest;
        public sbyte Rest { get => rest; set => rest = value.Clamp((sbyte)0, (sbyte)96); }

        public string Arguments => Rest.ToString();
    }
    internal class NoteCommand : ICommand
    {
        public string Name => "Note On";

        sbyte note, vel, dur;
        public sbyte Note { get => note; set => note = value.Clamp((sbyte)0, (sbyte)127); }
        public sbyte Velocity { get => vel; set => vel = value.Clamp((sbyte)0, (sbyte)127); }
        public sbyte Duration { get => dur; set => dur = value.Clamp((sbyte)-1, (sbyte)99); }

        public string Arguments => $"{SongEvent.NoteName(note)} {vel} {dur}";
    }
    internal class EndOfTieCommand : ICommand
    {
        public string Name => "End Of Tie";

        sbyte note;
        public sbyte Note { get => note; set => note = value.Clamp((sbyte)-1, (sbyte)127); }

        public string Arguments => SongEvent.NoteName(note);
    }
    internal class VoiceCommand : ICommand
    {
        public string Name => "Voice";

        public byte Voice;

        public string Arguments => Voice.ToString();
    }
    internal class VolumeCommand : ICommand
    {
        public string Name => "Volume";

        sbyte vol;
        public sbyte Volume { get => vol; set => vol = value.Clamp((sbyte)0, (sbyte)127); }

        public string Arguments => vol.ToString();
    }
    internal class PanpotCommand : ICommand
    {
        public string Name => "Panpot";

        sbyte pan;
        public sbyte Panpot { get => pan; set => pan = value.Clamp((sbyte)-64, (sbyte)63); }

        public string Arguments => pan.ToString();
    }
    internal class BendCommand : ICommand
    {
        public string Name => "Bend";

        sbyte bend;
        public sbyte Bend { get => bend; set => bend = value.Clamp((sbyte)-64, (sbyte)63); }

        public string Arguments => bend.ToString();
    }
    internal class TuneCommand : ICommand
    {
        public string Name => "Fine Tuning";

        sbyte tune;
        public sbyte Tune { get => tune; set => tune = value.Clamp((sbyte)-64, (sbyte)63); }

        public string Arguments => tune.ToString();
    }
    internal class BendRangeCommand : ICommand
    {
        public string Name => "Bend Range";

        public byte Range;

        public string Arguments => Range.ToString();
    }
    internal class LFOSpeedCommand : ICommand
    {
        public string Name => "LFO Speed";

        public byte Speed;

        public string Arguments => Speed.ToString();
    }
    internal class LFODelayCommand : ICommand
    {
        public string Name => "LFO Delay";

        public byte Delay;

        public string Arguments => Delay.ToString();
    }
    internal class ModDepthCommand : ICommand
    {
        public string Name => "MOD Depth";

        public byte Depth;

        public string Arguments => Depth.ToString();
    }
    internal class ModTypeCommand : ICommand
    {
        public string Name => "MOD Type";

        MODT type;
        public byte Type { get => (byte)type; set => type = (MODT)value.Clamp((byte)MODT.Vibrate, (byte)MODT.Panpot); }

        public string Arguments => type.ToString();
    }
    internal class PriorityCommand : ICommand
    {
        public string Name => "Priority";

        public byte Priority;

        public string Arguments => Priority.ToString();
    }
    internal class KeyShiftCommand : ICommand
    {
        public string Name => "Key Shift";

        public sbyte Shift;

        public string Arguments => Shift.ToString();
    }
    internal class GoToCommand : ICommand
    {
        public string Name => "Go To";

        uint offset;
        public uint Offset { get => offset; set => offset = value.Clamp((uint)0, ROM.Capacity); }

        public string Arguments => $"0x{offset:X}";
    }
    internal class FinishCommand : ICommand
    {
        public string Name => "Finish";

        public string Arguments => string.Empty;
    }

    #endregion

    #region M4A Commands

    internal class CallCommand : ICommand
    {
        public string Name => "Call";

        uint offset;
        public uint Offset { get => offset; set => offset = value.Clamp((uint)0, ROM.Capacity); }

        public string Arguments => $"0x{offset:X}";
    }
    internal class ReturnCommand : ICommand
    {
        public string Name => "Return";

        public string Arguments => string.Empty;
    }
    internal class RepeatCommand : ICommand
    {
        public string Name => "Repeat";

        public byte Arg;

        public string Arguments => Arg.ToString();
    }
    internal class MemoryAccessCommand : ICommand
    {
        public string Name => "Memory Access";

        public byte Arg1, Arg2, Arg3;

        public string Arguments => $"{Arg1}, {Arg2}, {Arg3}";
    }
    internal class LibraryCommand : ICommand
    {
        public string Name => "Library Call";

        public byte Arg1, Arg2;

        public string Arguments => $"{Arg1}, {Arg2}";
    }

    #endregion
}
