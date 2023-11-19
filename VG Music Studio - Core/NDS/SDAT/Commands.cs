using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Drawing;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT;

internal abstract class SDATCommand
{
	public bool RandMod { get; set; }
	public bool VarMod { get; set; }

	protected string GetValues(int value, string ifNot)
	{
		return RandMod ? $"[{(short)value}, {(short)(value >> 16)}]"
			: VarMod ? $"[{(byte)value}]"
			: ifNot;
	}
}

internal sealed class AllocTracksCommand : SDATCommand, ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Alloc Tracks";
	public string Arguments => $"{Convert.ToString(Tracks, 2).PadLeft(16, '0')}b";

	public ushort Tracks { get; set; }
}
internal sealed class CallCommand : SDATCommand, ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Call";
	public string Arguments => $"0x{Offset:X4}";

	public int Offset { get; set; }
}
internal sealed class FinishCommand : SDATCommand, ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Finish";
	public string Arguments => string.Empty;
}
internal sealed class ForceAttackCommand : SDATCommand, ICommand
{
	public Color Color => Color.LightSteelBlue;
	public string Label => "Force Attack";
	public string Arguments => GetValues(Attack, Attack.ToString());

	public int Attack { get; set; }
}
internal sealed class ForceDecayCommand : SDATCommand, ICommand
{
	public Color Color => Color.LightSteelBlue;
	public string Label => "Force Decay";
	public string Arguments => GetValues(Decay, Decay.ToString());

	public int Decay { get; set; }
}
internal sealed class ForceReleaseCommand : SDATCommand, ICommand
{
	public Color Color => Color.LightSteelBlue;
	public string Label => "Force Release";
	public string Arguments => GetValues(Release, Release.ToString());

	public int Release { get; set; }
}
internal sealed class ForceSustainCommand : SDATCommand, ICommand
{
	public Color Color => Color.LightSteelBlue;
	public string Label => "Force Sustain";
	public string Arguments => GetValues(Sustain, Sustain.ToString());

	public int Sustain { get; set; }
}
internal sealed class JumpCommand : SDATCommand, ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Jump";
	public string Arguments => $"0x{Offset:X4}";

	public int Offset { get; set; }
}
internal sealed class LFODelayCommand : SDATCommand, ICommand
{
	public Color Color => Color.LightSteelBlue;
	public string Label => "LFO Delay";
	public string Arguments => GetValues(Delay, Delay.ToString());

	public int Delay { get; set; }
}
internal sealed class LFODepthCommand : SDATCommand, ICommand
{
	public Color Color => Color.LightSteelBlue;
	public string Label => "LFO Depth";
	public string Arguments => GetValues(Depth, Depth.ToString());

	public int Depth { get; set; }
}
internal sealed class LFORangeCommand : SDATCommand, ICommand
{
	public Color Color => Color.LightSteelBlue;
	public string Label => "LFO Range";
	public string Arguments => GetValues(Range, Range.ToString());

	public int Range { get; set; }
}
internal sealed class LFOSpeedCommand : SDATCommand, ICommand
{
	public Color Color => Color.LightSteelBlue;
	public string Label => "LFO Speed";
	public string Arguments => GetValues(Speed, Speed.ToString());

	public int Speed { get; set; }
}
internal sealed class LFOTypeCommand : SDATCommand, ICommand
{
	public Color Color => Color.LightSteelBlue;
	public string Label => "LFO Type";
	public string Arguments => GetValues(Type, Type.ToString());

	public int Type { get; set; }
}
internal sealed class LoopEndCommand : SDATCommand, ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Loop End";
	public string Arguments => string.Empty;
}
internal sealed class LoopStartCommand : SDATCommand, ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Loop Start";
	public string Arguments => GetValues(NumLoops, NumLoops.ToString());

	public int NumLoops { get; set; }
}
internal sealed class ModIfCommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "If Modifier";
	public string Arguments => string.Empty;
}
internal sealed class ModRandCommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Rand Modifier";
	public string Arguments => string.Empty;
}
internal sealed class ModVarCommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Var Modifier";
	public string Arguments => string.Empty;
}
internal sealed class MonophonyCommand : SDATCommand, ICommand
{
	public Color Color => Color.SkyBlue;
	public string Label => "Monophony Toggle";
	public string Arguments => GetValues(Mono, (Mono == 1).ToString());

	public int Mono { get; set; }
}
internal sealed class NoteComand : SDATCommand, ICommand
{
	public Color Color => Color.SkyBlue;
	public string Label => "Note";
	public string Arguments => $"{ConfigUtils.GetKeyName(Note)}, {Velocity}, {GetValues(Duration, Duration.ToString())}";

	public byte Note { get; set; }
	public byte Velocity { get; set; }
	public int Duration { get; set; }
}
internal sealed class OpenTrackCommand : SDATCommand, ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Open Track";
	public string Arguments => $"{Track}, 0x{Offset:X4}";

	public byte Track { get; set; }
	public int Offset { get; set; }
}
internal sealed class PanpotCommand : SDATCommand, ICommand
{
	public Color Color => Color.GreenYellow;
	public string Label => "Panpot";
	public string Arguments => GetValues(Panpot, Panpot.ToString());

	public int Panpot { get; set; }
}
internal sealed class PitchBendCommand : SDATCommand, ICommand
{
	public Color Color => Color.MediumPurple;
	public string Label => "Pitch Bend";
	public string Arguments => GetValues(Bend, Bend.ToString());

	public int Bend { get; set; }
}
internal sealed class PitchBendRangeCommand : SDATCommand, ICommand
{
	public Color Color => Color.MediumPurple;
	public string Label => "Pitch Bend Range";
	public string Arguments => GetValues(Range, Range.ToString());

	public int Range { get; set; }
}
internal sealed class PlayerVolumeCommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Player Volume";
	public string Arguments => GetValues(Volume, Volume.ToString());

	public int Volume { get; set; }
}
internal sealed class PortamentoControlCommand : SDATCommand, ICommand
{
	public Color Color => Color.HotPink;
	public string Label => "Portamento Control";
	public string Arguments => GetValues(Portamento, Portamento.ToString());

	public int Portamento { get; set; }
}
internal sealed class PortamentoToggleCommand : SDATCommand, ICommand
{
	public Color Color => Color.HotPink;
	public string Label => "Portamento Toggle";
	public string Arguments => GetValues(Portamento, (Portamento == 1).ToString());

	public int Portamento { get; set; }
}
internal sealed class PortamentoTimeCommand : SDATCommand, ICommand
{
	public Color Color => Color.HotPink;
	public string Label => "Portamento Time";
	public string Arguments => GetValues(Time, Time.ToString());

	public int Time { get; set; }
}
internal sealed class PriorityCommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Priority";
	public string Arguments => GetValues(Priority, Priority.ToString());

	public int Priority { get; set; }
}
internal sealed class RestCommand : SDATCommand, ICommand
{
	public Color Color => Color.PaleVioletRed;
	public string Label => "Rest";
	public string Arguments => GetValues(Rest, Rest.ToString());

	public int Rest { get; set; }
}
internal sealed class ReturnCommand : SDATCommand, ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Return";
	public string Arguments => string.Empty;
}
internal sealed class SweepPitchCommand : SDATCommand, ICommand
{
	public Color Color => Color.MediumPurple;
	public string Label => "Sweep Pitch";
	public string Arguments => GetValues(Pitch, Pitch.ToString());

	public int Pitch { get; set; }
}
internal sealed class TempoCommand : SDATCommand, ICommand
{
	public Color Color => Color.DeepSkyBlue;
	public string Label => "Tempo";
	public string Arguments => GetValues(Tempo, Tempo.ToString());

	public int Tempo { get; set; }
}
internal sealed class TieCommand : SDATCommand, ICommand
{
	public Color Color => Color.SkyBlue;
	public string Label => "Tie";
	public string Arguments => GetValues(Tie, (Tie == 1).ToString());

	public int Tie { get; set; }
}
internal sealed class TrackExpressionCommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Track Expression";
	public string Arguments => GetValues(Expression, Expression.ToString());

	public int Expression { get; set; }
}
internal sealed class TrackVolumeCommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Track Volume";
	public string Arguments => GetValues(Volume, Volume.ToString());

	public int Volume { get; set; }
}
internal sealed class TransposeCommand : SDATCommand, ICommand
{
	public Color Color => Color.SkyBlue;
	public string Label => "Transpose";
	public string Arguments => GetValues(Transpose, Transpose.ToString());

	public int Transpose { get; set; }
}
internal sealed class VarAddCommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Var Add";
	public string Arguments => $"{Variable}, {GetValues(Argument, Argument.ToString())}";

	public byte Variable { get; set; }
	public int Argument { get; set; }
}
internal sealed class VarCmpEECommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Var ==";
	public string Arguments => $"{Variable}, {GetValues(Argument, Argument.ToString())}";

	public byte Variable { get; set; }
	public int Argument { get; set; }
}
internal sealed class VarCmpGECommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Var >=";
	public string Arguments => $"{Variable}, {GetValues(Argument, Argument.ToString())}";

	public byte Variable { get; set; }
	public int Argument { get; set; }
}
internal sealed class VarCmpGGCommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Var >";
	public string Arguments => $"{Variable}, {GetValues(Argument, Argument.ToString())}";

	public byte Variable { get; set; }
	public int Argument { get; set; }
}
internal sealed class VarCmpLECommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Var <=";
	public string Arguments => $"{Variable}, {GetValues(Argument, Argument.ToString())}";

	public byte Variable { get; set; }
	public int Argument { get; set; }
}
internal sealed class VarCmpLLCommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Var <";
	public string Arguments => $"{Variable}, {GetValues(Argument, Argument.ToString())}";

	public byte Variable { get; set; }
	public int Argument { get; set; }
}
internal sealed class VarCmpNECommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Var !=";
	public string Arguments => $"{Variable}, {GetValues(Argument, Argument.ToString())}";

	public byte Variable { get; set; }
	public int Argument { get; set; }
}
internal sealed class VarDivCommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Var Div";
	public string Arguments => $"{Variable}, {GetValues(Argument, Argument.ToString())}";

	public byte Variable { get; set; }
	public int Argument { get; set; }
}
internal sealed class VarMulCommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Var Mul";
	public string Arguments => $"{Variable}, {GetValues(Argument, Argument.ToString())}";

	public byte Variable { get; set; }
	public int Argument { get; set; }
}
internal sealed class VarPrintCommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Var Print";
	public string Arguments => GetValues(Variable, Variable.ToString());

	public int Variable { get; set; }
}
internal sealed class VarRandCommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Var Rand";
	public string Arguments => $"{Variable}, {GetValues(Argument, Argument.ToString())}";

	public byte Variable { get; set; }
	public int Argument { get; set; }
}
internal sealed class VarSetCommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Var Set";
	public string Arguments => $"{Variable}, {GetValues(Argument, Argument.ToString())}";

	public byte Variable { get; set; }
	public int Argument { get; set; }
}
internal sealed class VarShiftCommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Var Shift";
	public string Arguments => $"{Variable}, {GetValues(Argument, Argument.ToString())}";

	public byte Variable { get; set; }
	public int Argument { get; set; }
}
internal sealed class VarSubCommand : SDATCommand, ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Var Sub";
	public string Arguments => $"{Variable}, {GetValues(Argument, Argument.ToString())}";

	public byte Variable { get; set; }
	public int Argument { get; set; }
}
internal sealed class VoiceCommand : SDATCommand, ICommand
{
	public Color Color => Color.DarkSalmon;
	public string Label => "Voice";
	public string Arguments => GetValues(Voice, Voice.ToString());

	public int Voice { get; set; }
}
