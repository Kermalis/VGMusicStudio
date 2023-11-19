using Kermalis.VGMusicStudio.Core.Util;
using System.Drawing;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

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
internal class InvalidCommand : ICommand
{
	public Color Color => Color.MediumVioletRed;
	public string Label => $"Invalid 0x{Command:X}";
	public string Arguments => string.Empty;

	public byte Command { get; set; }
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
	public string Arguments => $"{ConfigUtils.GetKeyName(Note)} {OctaveChange} {Velocity} {Duration}";

	public byte Note { get; set; }
	public sbyte OctaveChange { get; set; }
	public byte Velocity { get; set; }
	public uint Duration { get; set; }
}
internal class OctaveAddCommand : ICommand
{
	public Color Color => Color.SkyBlue;
	public string Label => "Add To Octave";
	public string Arguments => OctaveChange.ToString();

	public sbyte OctaveChange { get; set; }
}
internal class OctaveSetCommand : ICommand
{
	public Color Color => Color.SkyBlue;
	public string Label => "Set Octave";
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
internal class SkipBytesCommand : ICommand
{
	public Color Color => Color.MediumVioletRed;
	public string Label => $"Skip 0x{Command:X}";
	public string Arguments => string.Join(", ", SkippedBytes.Select(b => $"0x{b:X}"));

	public byte Command { get; set; }
	public byte[] SkippedBytes { get; set; }
}
internal class TempoCommand : ICommand
{
	public Color Color => Color.DeepSkyBlue;
	public string Label => $"Tempo {Command - 0xA3}"; // The two possible tempo commands are 0xA4 and 0xA5
	public string Arguments => Tempo.ToString();

	public byte Command { get; set; }
	public byte Tempo { get; set; }
}
internal class UnknownCommand : ICommand
{
	public Color Color => Color.MediumVioletRed;
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
