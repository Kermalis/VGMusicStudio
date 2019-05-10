using System.Drawing;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    internal class ExpressionCommand : ICommand
    {
        public Color Color => Color.SteelBlue;
        public string Label => "Expression";
        public string Arguments => Expression.ToString();

        public byte Expression { get; set; }
    }
    internal class FinishCommand : ICommand
    {
        public Color Color => Color.MediumSpringGreen;
        public string Label => "Finish";
        public string Arguments => string.Empty;
    }
    internal class LoopStartCommand : ICommand
    {
        public Color Color => Color.MediumSpringGreen;
        public string Label => "Loop Start";
        public string Arguments => $"0x{Offset:X}";

        public long Offset { get; set; }
    }
    internal class NoteCommand : ICommand
    {
        public Color Color => Color.SkyBlue;
        public string Label => "Note";
        public string Arguments => $"{Util.Utils.GetPianoKeyName(Key)} {OctaveChange} {Velocity} {Duration}";

        public byte Key { get; set; }
        public sbyte OctaveChange { get; set; }
        public byte Velocity { get; set; }
        public uint Duration { get; set; }
    }
    internal class OctaveCommand : ICommand
    {
        public Color Color => Color.SkyBlue;
        public string Label => "Octave";
        public string Arguments => Octave.ToString();

        public byte Octave { get; set; }
    }
    internal class PanpotCommand : ICommand
    {
        public Color Color => Color.GreenYellow;
        public string Label => "Panpot";
        public string Arguments => Panpot.ToString();

        public sbyte Panpot { get; set; }
    }
    internal class PitchBendCommand : ICommand
    {
        public Color Color => Color.MediumPurple;
        public string Label => "Pitch Bend";
        public string Arguments => $"{(sbyte)Bend}, {(sbyte)(Bend >> 8)}";

        public ushort Bend { get; set; }
    }
    internal class RestCommand : ICommand
    {
        public Color Color => Color.PaleVioletRed;
        public string Label => "Rest";
        public string Arguments => Rest.ToString();

        public uint Rest { get; set; }
    }
    internal class TempoCommand : ICommand
    {
        public Color Color => Color.DeepSkyBlue;
        public string Label => "Tempo";
        public string Arguments => Tempo.ToString();

        public byte Tempo { get; set; }
    }
    internal class UnknownCommand : ICommand
    {
        public Color Color => Color.LightSteelBlue;
        public string Label => $"Unknown 0x{Command:X}";
        public string Arguments => string.Join(", ", Args.Select(b => $"0x{b:X}"));

        public byte Command { get; set; }
        public byte[] Args { get; set; }
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
        public string Arguments => Volume.ToString();

        public byte Volume { get; set; }
    }
}
