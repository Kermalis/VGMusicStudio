using GBAMusicStudio.Util;

namespace GBAMusicStudio.Core
{
    class SongEvent : IOffset
    {
        int offset;
        public int AbsoluteTicks;
        public ICommand Command;

        public SongEvent(int offset, ICommand command)
        {
            SetOffset(offset);
            Command = command;
        }

        public int GetOffset() => offset;
        public void SetOffset(int newOffset) => offset = newOffset;

        public override string ToString() => $"{Command}\t-\t0x{offset:X7}\t-\t{AbsoluteTicks}";

        public static byte RestFromCMD(byte startCMD, byte cmd)
        {
            byte[] added = { 4, 4, 2, 2 };
            byte wait = (byte)(cmd - startCMD);
            byte add = wait > 24 ? (byte)24 : wait;
            for (int i = 24 + 1; i <= wait; i++)
                add += added[i % 4];
            return add;
        }
        public static byte[] RestToCMD = {
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

        public static string CenterValueString(sbyte value)
        {
            return string.Format("c_v{0}{1}", value >= 0 ? "+" : "", value);
        }
    }

    #region Commands

    class TempoCommand : ICommand
    {
        public string Name => "Tempo";

        short tempo;
        public short Tempo
        {
            get => tempo;
            set
            {
                short max = 0;
                switch (ROM.Instance.Game.Engine.Type)
                {
                    case EngineType.M4A: max = 510; value /= 2; value *= 2; break; // Get rid of odd values
                    case EngineType.MLSS: max = 0xFF; break;
                }
                tempo = value.Clamp((short)0, max);
            }
        }

        public string Arguments => Tempo.ToString();
    }
    class RestCommand : ICommand
    {
        public string Name => "Rest";

        byte rest;
        public byte Rest
        {
            get => rest;
            set
            {
                byte max = 0;
                switch (ROM.Instance.Game.Engine.Type)
                {
                    case EngineType.M4A: max = 96; break;
                    case EngineType.MLSS: max = 0xC0; break;
                }
                rest = value.Clamp((byte)0, max);
            }
        }

        public string Arguments => Rest.ToString();
    }
    class NoteCommand : ICommand
    {
        public string Name => "Note";

        protected sbyte note;
        protected byte vel;
        protected int dur;
        public sbyte Note { get => note; set => note = value.Clamp((sbyte)0, (sbyte)0x7F); }
        internal byte Velocity;
        internal int Duration;

        public string Arguments => string.Empty;
    }
    class EndOfTieCommand : ICommand
    {
        public string Name => "End Of Tie";

        sbyte note;
        public sbyte Note { get => note; set => note = value.Clamp((sbyte)-1, (sbyte)0x7F); }

        public string Arguments => SongEvent.NoteName(note);
    }
    class VoiceCommand : ICommand
    {
        public string Name => "Voice";

        public byte Voice;

        public string Arguments => Voice.ToString();
    }
    class VolumeCommand : ICommand
    {
        public string Name => "Volume";

        byte vol;
        public byte Volume { get => vol; set => vol = value.Clamp((byte)0, Engine.GetMaxVolume()); }

        public string Arguments => vol.ToString();
    }
    class PanpotCommand : ICommand
    {
        public string Name => "Panpot";

        sbyte pan;
        public sbyte Panpot
        {
            get => pan;
            set
            {
                byte range = Engine.GetPanpotRange();
                pan = value.Clamp((sbyte)-range, (sbyte)(range - 1));
            }
        }

        public string Arguments => pan.ToString();
    }
    class BendCommand : ICommand
    {
        public string Name => "Bend";

        sbyte bend;
        public sbyte Bend
        {
            get => bend;
            set
            {
                byte range = Engine.GetBendingRange();
                bend = value.Clamp((sbyte)-range, (sbyte)(range - 1));
            }
        }

        public string Arguments => bend.ToString();
    }
    class TuneCommand : ICommand
    {
        public string Name => "Fine Tuning";

        sbyte tune;
        public sbyte Tune { get => tune; set => tune = value.Clamp((sbyte)-64, (sbyte)63); }

        public string Arguments => tune.ToString();
    }
    class BendRangeCommand : ICommand
    {
        public string Name => "Bend Range";

        public byte Range;

        public string Arguments => Range.ToString();
    }
    class LFOSpeedCommand : ICommand
    {
        public string Name => "LFO Speed";

        public byte Speed;

        public string Arguments => Speed.ToString();
    }
    class LFODelayCommand : ICommand
    {
        public string Name => "LFO Delay";

        public byte Delay;

        public string Arguments => Delay.ToString();
    }
    class ModDepthCommand : ICommand
    {
        public string Name => "MOD Depth";

        public byte Depth;

        public string Arguments => Depth.ToString();
    }
    class ModTypeCommand : ICommand
    {
        public string Name => "MOD Type";

        MODType type;
        public byte Type { get => (byte)type; set => type = (MODType)value.Clamp((byte)MODType.Vibrate, (byte)MODType.Panpot); }

        public string Arguments => type.ToString();
    }
    class PriorityCommand : ICommand
    {
        public string Name => "Priority";

        public byte Priority;

        public string Arguments => Priority.ToString();
    }
    class KeyShiftCommand : ICommand
    {
        public string Name => "Key Shift";

        public sbyte Shift;

        public string Arguments => Shift.ToString();
    }
    class GoToCommand : ICommand
    {
        public string Name => "Go To";

        int offset;
        public int Offset { get => offset; set => offset = value.Clamp(0, ROM.Capacity); }

        public string Arguments => $"0x{offset:X7}";
    }
    class FinishCommand : ICommand
    {
        public string Name => "Finish";

        public string Arguments => string.Empty;
    }

    #endregion

    #region M4A Commands

    class M4ANoteCommand : NoteCommand
    {
        public new byte Velocity { get => vel; set => vel = value.Clamp((byte)0, (byte)0x7F); }
        public new int Duration { get => dur; set => dur = value.Clamp(-1, 0x7F); }

        public new string Arguments => $"{SongEvent.NoteName(note)} {vel} {dur}";
    }
    class M4AFinishCommand : FinishCommand
    {
        bool prev; // PREV is 0xB6, FINE is 0xB1
        public byte Type { get => (byte)(prev ? 0xB6 : 0xB1); set => prev = value == 0xB6; }

        public new string Arguments => prev ? "Resume previous track" : "End track";
    }
    class CallCommand : ICommand
    {
        public string Name => "Call";

        int offset;
        public int Offset { get => offset; set => offset = value.Clamp(0, ROM.Capacity); }

        public string Arguments => $"0x{offset:X7}";
    }
    class ReturnCommand : ICommand
    {
        public string Name => "Return";

        public string Arguments => string.Empty;
    }
    class RepeatCommand : ICommand
    {
        public string Name => "Repeat";

        public byte Times;
        int offset;
        public int Offset { get => offset; set => offset = value.Clamp(0, ROM.Capacity); }

        public string Arguments => $"{Times}, 0x{offset:X7}";
    }
    class MemoryAccessCommand : ICommand
    {
        public string Name => "Memory Access";

        public byte Arg1, Arg2, Arg3;

        public string Arguments => $"{Arg1}, {Arg2}, {Arg3}";
    }
    class LibraryCommand : ICommand
    {
        public string Name => "Library Call";

        public byte Command, Argument;

        public string Arguments => $"{Command}, {Argument}";
    }

    #endregion

    #region MLSS Commands

    class FreeNoteCommand : ICommand
    {
        public string Name => "Free Note";

        byte note = 0x80;
        byte ext;
        public byte Note { get => note; set => note = value.Clamp((byte)0x80, (byte)0xFF); }
        public byte Duration { get => ext; set => ext = value.Clamp((byte)0, (byte)0xC0); }

        public string Arguments => $"{SongEvent.NoteName((sbyte)(note - 0x80))}, {Duration}";
    }
    class MLSSNoteCommand : NoteCommand
    {
        internal new byte Velocity { get => 127; }
        public new int Duration { get => dur; set => dur = value.Clamp(0, 0xC0); }

        public new string Arguments => $"{SongEvent.NoteName(note)}, {Duration}";
    }

    #endregion
}
