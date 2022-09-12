using Kermalis.VGMusicStudio.Core.Util;
using System.Drawing;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

internal sealed class FinishCommand : ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Finish";
	public string Arguments => string.Empty;
}
internal sealed class FreeNoteHamtaroCommand : ICommand // TODO: When optimization comes, get rid of free note vs note and just have the label differ
{
	public Color Color => Color.SkyBlue;
	public string Label => "Free Note";
	public string Arguments => $"{ConfigUtils.GetKeyName(Note)} {Volume} {Duration}";

	public byte Note { get; set; }
	public byte Volume { get; set; }
	public byte Duration { get; set; }
}
internal sealed class FreeNoteMLSSCommand : ICommand
{
	public Color Color => Color.SkyBlue;
	public string Label => "Free Note";
	public string Arguments => $"{ConfigUtils.GetKeyName(Note)} {Duration}";

	public byte Note { get; set; }
	public byte Duration { get; set; }
}
internal sealed class JumpCommand : ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Jump";
	public string Arguments => $"0x{Offset:X7}";

	public int Offset { get; set; }
}
internal sealed class NoteHamtaroCommand : ICommand
{
	public Color Color => Color.SkyBlue;
	public string Label => "Note";
	public string Arguments => $"{ConfigUtils.GetKeyName(Note)} {Volume} {Duration}";

	public byte Note { get; set; }
	public byte Volume { get; set; }
	public byte Duration { get; set; }
}
internal sealed class NoteMLSSCommand : ICommand
{
	public Color Color => Color.SkyBlue;
	public string Label => "Note";
	public string Arguments => $"{ConfigUtils.GetKeyName(Note)} {Duration}";

	public byte Note { get; set; }
	public byte Duration { get; set; }
}
internal sealed class PanpotCommand : ICommand
{
	public Color Color => Color.GreenYellow;
	public string Label => "Panpot";
	public string Arguments => Panpot.ToString();

	public sbyte Panpot { get; set; }
}
internal sealed class PitchBendCommand : ICommand
{
	public Color Color => Color.MediumPurple;
	public string Label => "Pitch Bend";
	public string Arguments => Bend.ToString();

	public sbyte Bend { get; set; }
}
internal sealed class PitchBendRangeCommand : ICommand
{
	public Color Color => Color.MediumPurple;
	public string Label => "Pitch Bend Range";
	public string Arguments => Range.ToString();

	public byte Range { get; set; }
}
internal sealed class RestCommand : ICommand
{
	public Color Color => Color.PaleVioletRed;
	public string Label => "Rest";
	public string Arguments => Rest.ToString();

	public byte Rest { get; set; }
}
internal sealed class TrackTempoCommand : ICommand
{
	public Color Color => Color.DeepSkyBlue;
	public string Label => "Track Tempo";
	public string Arguments => Tempo.ToString();

	public byte Tempo { get; set; }
}
internal sealed class VoiceCommand : ICommand
{
	public Color Color => Color.DarkSalmon;
	public string Label => "Voice";
	public string Arguments => Voice.ToString();

	public byte Voice { get; set; }
}
internal sealed class VolumeCommand : ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Volume";
	public string Arguments => Volume.ToString();

	public byte Volume { get; set; }
}
