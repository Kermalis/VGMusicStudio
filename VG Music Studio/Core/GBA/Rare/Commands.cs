using System.Drawing;

namespace Kermalis.VGMusicStudio.Core.GBA.Rare
{
    internal class FinishCommand : ICommand
    {
        public Color Color => Color.SteelBlue;
        public string Label => "Finish";
        public string Arguments => $"{Channel}B";

        public byte Channel { get; set; }
    }
    internal class NoteOnCommand : ICommand
    {
        public Color Color => Color.SteelBlue;
        public string Label => "NoteOn";
        public string Arguments => $"{Channel}5 [0x{Data[0]:X2} 0x{Data[1]:X2}]";

        public byte Channel { get; set; }
        public byte[] Data { get; set; }
    }
    internal class NoteOffCommand : ICommand
    {
        public Color Color => Color.SteelBlue;
        public string Label => "NoteOff";
        public string Arguments => $"{Channel}6 [0x{Data[0]:X2} 0x{Data[1]:X2}]";

        public byte Channel { get; set; }
        public byte[] Data { get; set; }
    }
    internal class PitchCommand : ICommand
    {
        public Color Color => Color.SteelBlue;
        public string Label => "Pitch";
        public string Arguments => $"{Channel}A [0x{Data[0]:X2} 0x{Data[1]:X2}]";

        public byte Channel { get; set; }
        public byte[] Data { get; set; }
    }
    internal class Rest8Command : ICommand
    {
        public Color Color => Color.SteelBlue;
        public string Label => "Rest8";
        public string Arguments => $"{Channel}1 [{Data}]";

        public byte Channel { get; set; }
        public byte Data { get; set; }
    }
    internal class Rest16Command : ICommand
    {
        public Color Color => Color.SteelBlue;
        public string Label => "Rest16";
        public string Arguments => $"{Channel}2 [{Data}]";

        public byte Channel { get; set; }
        public ushort Data { get; set; }
    }
    internal class Rest24Command : ICommand
    {
        public Color Color => Color.SteelBlue;
        public string Label => "Rest24";
        public string Arguments => $"{Channel}3 [{Data}]";

        public byte Channel { get; set; }
        public uint Data { get; set; }
    }
    internal class TempoCommand : ICommand
    {
        public Color Color => Color.SteelBlue;
        public string Label => "Tempo";
        public string Arguments => $"{Channel}0 [{Data}]";

        public byte Channel { get; set; }
        public uint Data { get; set; }
    }
    internal class Unk4Command : ICommand
    {
        public Color Color => Color.SteelBlue;
        public string Label => "Unk4";
        public string Arguments => $"{Channel}4";

        public byte Channel { get; set; }
    }
    internal class Unk9Command : ICommand
    {
        public Color Color => Color.SteelBlue;
        public string Label => "Unk9";
        public string Arguments => $"{Channel}9";

        public byte Channel { get; set; }
    }
    internal class VoiceCommand : ICommand
    {
        public Color Color => Color.SteelBlue;
        public string Label => "Voice";
        public string Arguments => $"{Channel}8 [{Data}]";

        public byte Channel { get; set; }
        public byte Data { get; set; }
    }
    internal class VolumeCommand : ICommand
    {
        public Color Color => Color.SteelBlue;
        public string Label => "Volume";
        public string Arguments => $"{Channel}7 [0x{Data[0]:X2} 0x{Data[1]:X2}]";

        public byte Channel { get; set; }
        public byte[] Data { get; set; }
    }
}
