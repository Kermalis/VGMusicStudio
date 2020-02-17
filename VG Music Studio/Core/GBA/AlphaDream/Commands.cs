using System.Drawing;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream
{
    internal class FinishCommand : ICommand
    {
        public Color Color => Color.MediumSpringGreen;
        public string Label => "Finish";
        public string Arguments => string.Empty;
    }
    internal class FreeNoteCommand : ICommand
    {
        public Color Color => Color.SkyBlue;
        public string Label => "Free Note";
        public string Arguments => $"{Util.Utils.GetNoteName(Key)} {Duration}";

        public byte Key { get; set; }
        public byte Duration { get; set; }
    }
    internal class JumpCommand : ICommand
    {
        public Color Color => Color.MediumSpringGreen;
        public string Label => "Jump";
        public string Arguments => $"0x{Offset:X7}";

        public int Offset { get; set; }
    }
    internal class NoteCommand : ICommand
    {
        public Color Color => Color.SkyBlue;
        public string Label => "Note";
        public string Arguments => $"{Util.Utils.GetNoteName(Key)} {Duration}";

        public byte Key { get; set; }
        public byte Duration { get; set; }
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
        public string Arguments => Bend.ToString();

        public sbyte Bend { get; set; }
    }
    internal class PitchBendRangeCommand : ICommand
    {
        public Color Color => Color.MediumPurple;
        public string Label => "Pitch Bend Range";
        public string Arguments => Range.ToString();

        public byte Range { get; set; }
    }
    internal class RestCommand : ICommand
    {
        public Color Color => Color.PaleVioletRed;
        public string Label => "Rest";
        public string Arguments => Rest.ToString();

        public byte Rest { get; set; }
    }
    internal class TempoCommand : ICommand
    {
        public Color Color => Color.DeepSkyBlue;
        public string Label => "Tempo";
        public string Arguments => Tempo.ToString();

        public byte Tempo { get; set; }
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
