using System.Drawing;

namespace Kermalis.VGMusicStudio.Core.PSX.PSF
{
    internal class ControllerCommand : ICommand
    {
        public Color Color => Color.MediumVioletRed;
        public string Label => "Controller";
        public string Arguments => $"{Controller}, {Value}";

        public byte Controller { get; set; }
        public byte Value { get; set; }
    }
    internal class FinishCommand : ICommand
    {
        public Color Color => Color.MediumSpringGreen;
        public string Label => "Finish";
        public string Arguments => string.Empty;
    }
    internal class NoteCommand : ICommand
    {
        public Color Color => Color.SkyBlue;
        public string Label => $"Note {(Velocity == 0 ? "Off" : "On")}";
        public string Arguments => $"{Util.Utils.GetNoteName(Key)}{(Velocity == 0 ? string.Empty : $", {Velocity}")}";

        public byte Key { get; set; }
        public byte Velocity { get; set; }
    }
    internal class PitchBendCommand : ICommand
    {
        public Color Color => Color.MediumPurple;
        public string Label => "Pitch Bend";
        public string Arguments => $"0x{Bend1:X} 0x{Bend2:X}";

        public byte Bend1 { get; set; }
        public byte Bend2 { get; set; }
    }
    internal class TempoCommand : ICommand
    {
        public Color Color => Color.DeepSkyBlue;
        public string Label => "Tempo";
        public string Arguments => Tempo.ToString();

        public uint Tempo { get; set; }
    }
    internal class VoiceCommand : ICommand
    {
        public Color Color => Color.DarkSalmon;
        public string Label => "Voice";
        public string Arguments => Voice.ToString();

        public byte Voice { get; set; }
    }
}
