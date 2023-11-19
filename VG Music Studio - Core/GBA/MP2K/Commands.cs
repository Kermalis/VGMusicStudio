using Kermalis.VGMusicStudio.Core.Util;
using System.Drawing;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal class CallCommand : ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Call";
	public string Arguments => $"0x{Offset:X7}";

	public int Offset { get; set; }
}
internal class EndOfTieCommand : ICommand
{
	public Color Color => Color.SkyBlue;
	public string Label => "End Of Tie";
	public string Arguments => Note == -1 ? "All Ties" : ConfigUtils.GetKeyName(Note);

	public int Note { get; set; }
}
internal class FinishCommand : ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Finish";
	public string Arguments => Prev ? "Resume previous track" : "End track";

	public bool Prev { get; set; }
}
internal class JumpCommand : ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Jump";
	public string Arguments => $"0x{Offset:X7}";

	public int Offset { get; set; }
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
	public string Arguments => Type.ToString();

	public LFOType Type { get; set; }
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
	public string Arguments => $"{ConfigUtils.GetKeyName(Note)} {Velocity} {Duration}";

	public byte Note { get; set; }
	public byte Velocity { get; set; }
	public int Duration { get; set; }
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
	public string Arguments => $"{Times}, 0x{Offset:X7}";

	public byte Times { get; set; }
	public int Offset { get; set; }
}
internal class RestCommand : ICommand
{
	public Color Color => Color.PaleVioletRed;
	public string Label => "Rest";
	public string Arguments => Rest.ToString();

	public byte Rest { get; set; }
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
	public string Arguments => Tempo.ToString();

	public ushort Tempo { get; set; }
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
	public string Arguments => Tune.ToString();

	public sbyte Tune { get; set; }
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
