using Kermalis.VGMusicStudio.Core.Util;
using System.Drawing;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal sealed class CallCommand : ISequenceCommand, ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Call";
	public string Arguments => $"0x{Pointer.Offset:X7}";

	public SequenceCommand SequenceType => SequenceCommand.Pattern;

	public CommandPointer Pointer { get; set; }
}
internal sealed class EndOfTieCommand : ISequenceCommand, IRunCommand, ICommand
{
	public Color Color => Color.SkyBlue;
	public string Label => "End Of Tie";
	public string Arguments => Note == -1 ? "All Ties" : ConfigUtils.GetKeyName(Note);

	public SequenceCommand SequenceType => SequenceCommand.EndOfTie;

	public bool IsRepeated { get; set; }

	public int Note { get; set; }
}
internal sealed class FinishCommand : ISequenceCommand, ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Finish";
	public string Arguments => Prev ? "Resume previous track" : "End track";

	public SequenceCommand SequenceType => SequenceCommand.Fine;

	public bool Prev { get; set; }
	public byte Type { get => (byte)(Prev ? 0xB6 : 0xB1); set => Prev = value == 0xB6; }
}
internal sealed class JumpCommand : ISequenceCommand, ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Jump";
	public string Arguments => $"0x{Pointer.Offset:X7}";

	public SequenceCommand SequenceType => SequenceCommand.Goto;

	public CommandPointer Pointer { get; set; }
}
internal sealed class LFODelayCommand : ISequenceCommand, IRunCommand, ICommand
{
	public Color Color => Color.LightSteelBlue;
	public string Label => "LFO Delay";
	public string Arguments => Delay.ToString();

	public SequenceCommand SequenceType => SequenceCommand.LFODelay;

	public bool IsRepeated { get; set; }

	public byte Delay { get; set; }
}
internal sealed class LFODepthCommand : ISequenceCommand, IRunCommand, ICommand
{
	public Color Color => Color.LightSteelBlue;
	public string Label => "LFO Depth";
	public string Arguments => Depth.ToString();

	public SequenceCommand SequenceType => SequenceCommand.Modulation;

	public bool IsRepeated { get; set; }

	public byte Depth { get; set; }
}
internal sealed class LFOSpeedCommand : ISequenceCommand, IRunCommand, ICommand
{
	public Color Color => Color.LightSteelBlue;
	public string Label => "LFO Speed";
	public string Arguments => Speed.ToString();

	public SequenceCommand SequenceType => SequenceCommand.LFOSpeed;

	public bool IsRepeated { get; set; }

	public byte Speed { get; set; }
}
internal sealed class LFOTypeCommand : ISequenceCommand, IRunCommand, ICommand
{
	public Color Color => Color.LightSteelBlue;
	public string Label => "LFO Type";
	public string Arguments => Type.ToString();

	public SequenceCommand SequenceType => SequenceCommand.ModulationType;

	public bool IsRepeated { get; set; }

	public LFOType Type { get; set; }
}
internal sealed class LibraryCommand : ISequenceCommand, IRunCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Library Call";
	public string Arguments => $"{LibraryCommandType}, {Argument}";

	public SequenceCommand SequenceType => SequenceCommand.ExtendedCommand;

	public bool IsRepeated { get; set; }

	public ExtendedCommandType LibraryCommandType { get; set; }
	public byte Argument { get; set; }
}
internal sealed class MemoryAccessCommand : ISequenceCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Memory Access";
	public string Arguments => Pointer.Offset > -1
				? $"{Operator}, {MemoryAreaAddress}, {Data}, 0x{Pointer.Offset:X7}"
				: $"{Operator}, {MemoryAreaAddress}, {Data}";

	public SequenceCommand SequenceType => SequenceCommand.MemoryAccess;

	public MemoryOperatorType Operator { get; set; }
	public byte MemoryAreaAddress { get; set; }
	public byte Data { get; set; }
	public CommandPointer Pointer { get; set; }
}
internal sealed class NoteCommand : IRunCommand, ICommand
{
	public Color Color => Color.SkyBlue;
	public string Label => "Note";
	public string Arguments => $"{ConfigUtils.GetKeyName(Note)} {Velocity} {Duration}";

	public bool IsRepeated { get; set; }

	public byte Note { get; set; }
	public byte Velocity { get; set; }
	public int Duration { get; set; }
}
internal sealed class PanpotCommand : ISequenceCommand, IRunCommand, ICommand
{
	public Color Color => Color.GreenYellow;
	public string Label => "Panpot";
	public string Arguments => Panpot.ToString();

	public SequenceCommand SequenceType => SequenceCommand.Panpot;

	public bool IsRepeated { get; set; }

	public sbyte Panpot { get; set; }
}
internal sealed class PitchBendCommand : ISequenceCommand, IRunCommand, ICommand
{
	public Color Color => Color.MediumPurple;
	public string Label => "Pitch Bend";
	public string Arguments => Bend.ToString();

	public SequenceCommand SequenceType => SequenceCommand.Bend;

	public bool IsRepeated { get; set; }

	public sbyte Bend { get; set; }
}
internal sealed class PitchBendRangeCommand : ISequenceCommand, IRunCommand, ICommand
{
	public Color Color => Color.MediumPurple;
	public string Label => "Pitch Bend Range";
	public string Arguments => Range.ToString();

	public SequenceCommand SequenceType => SequenceCommand.BendRange;

	public bool IsRepeated { get; set; }

	public byte Range { get; set; }
}
internal sealed class PriorityCommand : ISequenceCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Priority";
	public string Arguments => Priority.ToString();

	public SequenceCommand SequenceType => SequenceCommand.Priority;

	public byte Priority { get; set; }
}
internal sealed class RepeatCommand : ISequenceCommand, ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Repeat";
	public string Arguments => $"{Times}, 0x{Pointer.Offset:X7}";

	public SequenceCommand SequenceType => SequenceCommand.Repeat;

	public byte Times { get; set; }
	public CommandPointer Pointer { get; set; }
}
internal sealed class RestCommand : ICommand
{
	public Color Color => Color.PaleVioletRed;
	public string Label => "Rest";
	public string Arguments => Rest.ToString();

	public byte Rest { get; set; }
}
internal sealed class ReturnCommand : ISequenceCommand, ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Return";
	public string Arguments => string.Empty;

	public SequenceCommand SequenceType => SequenceCommand.PatternEnd;
}
internal sealed class TempoCommand : ISequenceCommand, ICommand
{
	public Color Color => Color.DeepSkyBlue;
	public string Label => "Tempo";
	public string Arguments => Tempo.ToString();

	public SequenceCommand SequenceType => SequenceCommand.Tempo;

	public ushort Tempo { get; set; }
}
internal sealed class TransposeCommand : ISequenceCommand, ICommand
{
	public Color Color => Color.SkyBlue;
	public string Label => "Transpose";
	public string Arguments => Transpose.ToString();

	public SequenceCommand SequenceType => SequenceCommand.KeyShift;

	public sbyte Transpose { get; set; }
}
internal sealed class TuneCommand : ISequenceCommand, IRunCommand, ICommand
{
	public Color Color => Color.MediumPurple;
	public string Label => "Fine Tune";
	public string Arguments => Tune.ToString();

	public SequenceCommand SequenceType => SequenceCommand.Tune;

	public bool IsRepeated { get; set; }

	public sbyte Tune { get; set; }
}
internal sealed class VoiceCommand : ISequenceCommand, IRunCommand, ICommand
{
	public Color Color => Color.DarkSalmon;
	public string Label => "Voice";
	public string Arguments => Voice.ToString();

	public SequenceCommand SequenceType => SequenceCommand.Voice;

	public bool IsRepeated { get; set; }

	public byte Voice { get; set; }
}
internal sealed class VolumeCommand : ISequenceCommand, IRunCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Volume";
	public string Arguments => Volume.ToString();

	public SequenceCommand SequenceType => SequenceCommand.Volume;

	public bool IsRepeated { get; set; }

	public byte Volume { get; set; }
}
