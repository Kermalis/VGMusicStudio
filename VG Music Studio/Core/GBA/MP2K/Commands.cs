using System.Drawing;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K
{
    internal class CallCommand : ICommand
    {
        public Color Color => Color.MediumSpringGreen;
        public string Label => "Call";
        public string Arguments => $"0x{offset:X7}";

        private int offset;
        public int Offset { get => offset; set => offset = Util.Utils.Clamp(value, 0, GBA.Utils.CartridgeCapacity); }
    }
    internal class EndOfTieCommand : ICommand
    {
        public Color Color => Color.SkyBlue;
        public string Label => "End Of Tie";
        public string Arguments => key == -1 ? "All Ties" : Util.Utils.GetNoteName(key);

        private int key;
        public int Key { get => key; set => key = Util.Utils.Clamp(value, -1, 0x7F); }
    }
    internal class FinishCommand : ICommand
    {
        public Color Color => Color.MediumSpringGreen;
        public string Label => "Finish";
        public string Arguments => prev ? "Resume previous track" : "End track";

        private bool prev;
        public byte Type { get => (byte)(prev ? 0xB6 : 0xB1); set => prev = value == 0xB6; }
    }
    internal class JumpCommand : ICommand
    {
        public Color Color => Color.MediumSpringGreen;
        public string Label => "Jump";
        public string Arguments => $"0x{offset:X7}";

        private int offset;
        public int Offset { get => offset; set => offset = Util.Utils.Clamp(value, 0, GBA.Utils.CartridgeCapacity); }
    }
    internal class LFODelayCommand : ICommand
    {
        public Color Color => Color.LightSteelBlue;
        public string Label => "LFO Delay";
        public string Arguments => Delay.ToString();

        public byte Delay { get; set; }
    }
    internal class LFODepthCommand : ICommand
    {
        public Color Color => Color.LightSteelBlue;
        public string Label => "LFO Depth";
        public string Arguments => Depth.ToString();

        public byte Depth { get; set; }
    }
    internal class LFOSpeedCommand : ICommand
    {
        public Color Color => Color.LightSteelBlue;
        public string Label => "LFO Speed";
        public string Arguments => Speed.ToString();

        public byte Speed { get; set; }
    }
    internal class LFOTypeCommand : ICommand
    {
        public Color Color => Color.LightSteelBlue;
        public string Label => "LFO Type";
        public string Arguments => type.ToString();

        private LFOType type;
        public LFOType Type { get => type; set => type = (LFOType)Util.Utils.Clamp((byte)value, 0, 2); }
    }
    internal class LibraryCommand : ICommand
    {
        public Color Color => Color.SteelBlue;
        public string Label => "Library Call";
        public string Arguments => $"{Command}, {Argument}";

        public byte Command { get; set; }
        public byte Argument { get; set; }
    }
    internal class MemoryAccessCommand : ICommand
    {
        public Color Color => Color.SteelBlue;
        public string Label => "Memory Access";
        public string Arguments => $"{Operator}, {Address}, {Data}";

        public byte Operator { get; set; }
        public byte Address { get; set; }
        public byte Data { get; set; }
    }
    internal class NoteCommand : ICommand
    {
        public Color Color => Color.SkyBlue;
        public string Label => "Note";
        public string Arguments => $"{Util.Utils.GetNoteName(key)} {velocity} {duration}";

        private byte key;
        public byte Key { get => key; set => key = (byte)Util.Utils.Clamp(value, 0, 0x7F); }
        private byte velocity;
        public byte Velocity { get => velocity; set => velocity = (byte)Util.Utils.Clamp(value, 0, 0x7F); }
        private int duration;
        public int Duration { get => duration; set => duration = Util.Utils.Clamp(value, -1, 0x7F); }
    }
    internal class PanpotCommand : ICommand
    {
        public Color Color => Color.GreenYellow;
        public string Label => "Panpot";
        public string Arguments => panpot.ToString();

        private sbyte panpot;
        public sbyte Panpot { get => panpot; set => panpot = (sbyte)Util.Utils.Clamp(value, -0x40, 0x3F); }
    }
    internal class PitchBendCommand : ICommand
    {
        public Color Color => Color.MediumPurple;
        public string Label => "Pitch Bend";
        public string Arguments => bend.ToString();

        private sbyte bend;
        public sbyte Bend { get => bend; set => bend = (sbyte)Util.Utils.Clamp(value, -0x40, 0x3F); }
    }
    internal class PitchBendRangeCommand : ICommand
    {
        public Color Color => Color.MediumPurple;
        public string Label => "Pitch Bend Range";
        public string Arguments => Range.ToString();

        public byte Range { get; set; }
    }
    internal class PriorityCommand : ICommand
    {
        public Color Color => Color.SteelBlue;
        public string Label => "Priority";
        public string Arguments => Priority.ToString();

        public byte Priority { get; set; }
    }
    internal class RepeatCommand : ICommand
    {
        public Color Color => Color.MediumSpringGreen;
        public string Label => "Repeat";
        public string Arguments => $"{Times}, 0x{offset:X7}";

        public byte Times { get; set; }
        private int offset;
        public int Offset { get => offset; set => offset = Util.Utils.Clamp(value, 0, GBA.Utils.CartridgeCapacity); }
    }
    internal class RestCommand : ICommand
    {
        public Color Color => Color.PaleVioletRed;
        public string Label => "Rest";
        public string Arguments => rest.ToString();

        private byte rest;
        public byte Rest { get => rest; set => rest = (byte)Util.Utils.Clamp(value, 0, 96); }
    }
    internal class ReturnCommand : ICommand
    {
        public Color Color => Color.MediumSpringGreen;
        public string Label => "Return";
        public string Arguments => string.Empty;
    }
    internal class TempoCommand : ICommand
    {
        public Color Color => Color.DeepSkyBlue;
        public string Label => "Tempo";
        public string Arguments => tempo.ToString();

        private ushort tempo;
        public ushort Tempo
        {
            get => tempo;
            set
            {
                value /= 2; value *= 2; // Get rid of odd values
                tempo = (ushort)Util.Utils.Clamp(value, 0, 510);
            }
        }
    }
    internal class TransposeCommand : ICommand
    {
        public Color Color => Color.SkyBlue;
        public string Label => "Transpose";
        public string Arguments => Transpose.ToString();

        public sbyte Transpose { get; set; }
    }
    internal class TuneCommand : ICommand
    {
        public Color Color => Color.MediumPurple;
        public string Label => "Fine Tune";
        public string Arguments => tune.ToString();

        private sbyte tune;
        public sbyte Tune { get => tune; set => tune = (sbyte)Util.Utils.Clamp(value, -0x40, 0x3F); }
    }
    internal class VoiceCommand : ICommand
    {
        public Color Color => Color.DarkSalmon;
        public string Label => "Voice";
        public string Arguments => Voice.ToString();

        public byte Voice { get; set; }
    }
    internal class VolumeCommand : ICommand
    {
        public Color Color => Color.SteelBlue;
        public string Label => "Volume";
        public string Arguments => volume.ToString();

        private byte volume;
        public byte Volume { get => volume; set => volume = (byte)Util.Utils.Clamp(value, 0, 0x7F); }
    }
}
