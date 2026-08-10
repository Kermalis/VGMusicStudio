using Kermalis.VGMusicStudio.Core.Util;
using System.Drawing;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

internal sealed class ExpressionCommand : ICommand
{
	public Color Color => Color.SteelBlue;
	public string Label => "Expression";
	public string Arguments => Expression.ToString();

	public byte Expression { get; set; }
}
internal sealed class FinishCommand : ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Finish";
	public string Arguments => string.Empty;
}
internal sealed class InvalidCommand : ICommand
{
	public Color Color => Color.MediumVioletRed;
	public string Label => $"Invalid 0x{Command:X}";
	public string Arguments => string.Empty;

	public byte Command { get; set; }
}
internal sealed class LoopStartCommand : ICommand
{
	public Color Color => Color.MediumSpringGreen;
	public string Label => "Loop Start";
	public string Arguments => $"0x{Offset:X}";

	public long Offset { get; set; }
}
internal sealed class NoteCommand : ICommand
{
	public Color Color => Color.SkyBlue;
	public string Label => "Note";
	public string Arguments => $"{ConfigUtils.GetKeyName(Note)} {OctaveChange} {Velocity} {Duration}";

	public byte Note { get; set; }
	public sbyte OctaveChange { get; set; }
	public byte Velocity { get; set; }
	public uint Duration { get; set; }
}
internal sealed class OctaveAddCommand : ICommand
{
	public Color Color => Color.SkyBlue;
	public string Label => "Add To Octave";
	public string Arguments => OctaveChange.ToString();

	public sbyte OctaveChange { get; set; }
}
internal sealed class OctaveSetCommand : ICommand
{
	public Color Color => Color.SkyBlue;
	public string Label => "Set Octave";
	public string Arguments => Octave.ToString();

	public byte Octave { get; set; }
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
	public string Arguments => $"{(sbyte)Bend}, {(sbyte)(Bend >> 8)}";

	public ushort Bend { get; set; }
}
internal sealed class SetPitchBendRangeCommand : ICommand
{
	public Color Color => Color.MediumPurple;
	public string Label => "Set Pitch Bend Range";
	public string Arguments => $"{PitchBendRange}";

	public byte PitchBendRange { get; set; }
}
internal sealed class RestCommand : ICommand
{
	public Color Color => Color.PaleVioletRed;
	public string Label => "Rest";
	public string Arguments => Rest.ToString();

	public uint Rest { get; set; }
}
internal sealed class CheckIntervalCommand : ICommand
{
	public Color Color => Color.DarkViolet;
	public string Label => "Check Interval";
	public string Arguments => Interval.ToString();

	public uint Interval { get; set; }
}
internal sealed class SkipBytesCommand : ICommand
{
	public Color Color => Color.MediumVioletRed;
	public string Label => $"Skip 0x{Command:X}";
	public string Arguments => string.Join(", ", SkippedBytes.Select(b => $"0x{b:X}"));

	public byte Command { get; set; }
	public byte[] SkippedBytes { get; set; } = null!;
}
internal sealed class TempoCommand : ICommand
{
	public Color Color => Color.DeepSkyBlue;
	public string Label => $"Tempo {Command - 0xA3}"; // The two possible tempo commands are 0xA4 and 0xA5
	public string Arguments => Tempo.ToString();

	public byte Command { get; set; }
	public byte Tempo { get; set; }
}
internal sealed class DalSegnoAlCodaCommand : ICommand
{
	public Color Color => Color.CornflowerBlue;
	public string Label => $"Dal Segno Al Coda: Repeat {Repeats} Time(s)";
	public string Arguments => Repeats.ToString();

	public byte Command { get; set; }
	public byte Repeats { get; set; }
}
internal sealed class DalSegnoAlFineCommand : ICommand
{
	public Color Color => Color.CornflowerBlue;
	public string Label => $"Dal Segno Al Fine";
	public string Arguments => string.Empty;

	public byte Command { get; set; }
}
internal sealed class ToCodaCommand : ICommand
{
	public Color Color => Color.Aquamarine;
	public string Label => $"To Coda";
	public string Arguments => string.Empty;

	public byte Command { get; set; }
}
internal sealed class SetSWDAndBankCommand : ICommand
{
	public Color Color => Color.BlanchedAlmond;
	public string Label => $"Set SWD and Bank";
	public string Arguments => $"{WaveID}, {BankID}";

	public byte WaveID { get; set; }
	public byte BankID { get; set; }
}
internal sealed class SetBankHiCommand : ICommand
{
	public Color Color => Color.Beige;
	public string Label => $"Set Bank Hi";
	public string Arguments => $"{BankHiID}";

	public byte BankHiID { get; set; }
}
internal sealed class SetBankLoCommand : ICommand
{
	public Color Color => Color.Beige;
	public string Label => $"Set Bank Lo";
	public string Arguments => $"{BankLoID}";

	public byte BankLoID { get; set; }
}
internal sealed class SweepSongVolumeCommand : ICommand
{
	public Color Color => Color.DarkGreen;
	public string Label => $"Sweep Song Volume";
	public string Arguments => $"{SweepRate}, {SweepPitch}";

	public ushort SweepRate { get; set; }
	public byte SweepPitch { get; set; }
}
internal sealed class DisableEnvelopeCommand : ICommand
{
	public Color Color => Color.Aquamarine;
	public string Label => $"Disable Envelope";
	public string Arguments => string.Empty;

	public byte Command { get; set; }
}
internal sealed class SetEnvelopeAttackVolumeCommand : ICommand
{
	public Color Color => Color.Aquamarine;
	public string Label => $"Set Envelope: Attack Volume";
	public string Arguments => $"{AttackVolume}";

	public byte AttackVolume { get; set; }
}
internal sealed class SetEnvelopeAttackTimeCommand : ICommand
{
	public Color Color => Color.Aquamarine;
	public string Label => $"Set Envelope: Attack Time";
	public string Arguments => $"{AttackTime}";

	public byte AttackTime { get; set; }
}
internal sealed class SetEnvelopeHoldCommand : ICommand
{
	public Color Color => Color.Aquamarine;
	public string Label => $"Set Envelope: Hold";
	public string Arguments => $"{Hold}";

	public byte Hold { get; set; }
}
internal sealed class SetEnvelopeDecaySustainCommand : ICommand
{
	public Color Color => Color.Aquamarine;
	public string Label => $"Set Envelopes: Decay and Sustain";
	public string Arguments => $"{Decay}, {Sustain}";

	public byte Decay { get; set; }
	public byte Sustain { get; set; }
}
internal sealed class SetEnvelopeFadeCommand : ICommand
{
	public Color Color => Color.Aquamarine;
	public string Label => $"Set Envelope: Fade";
	public string Arguments => $"{Fade}";

	public byte Fade { get; set; }
}
internal sealed class SetEnvelopeReleaseCommand : ICommand
{
	public Color Color => Color.Aquamarine;
	public string Label => $"Set Envelope: Release";
	public string Arguments => $"{Release}";

	public byte Release { get; set; }
}
internal sealed class SetNoteVolumeCommand : ICommand
{
	public Color Color => Color.Purple;
	public string Label => $"Set Note Volume";
	public string Arguments => $"{NoteVolume}";

	public byte NoteVolume { get; set; }
}
internal sealed class SetChannelPanpotCommand : ICommand
{
	public Color Color => Color.Bisque;
	public string Label => $"Set Channel Panpot";
	public string Arguments => $"{ChannelPanpot}";

	public byte ChannelPanpot { get; set; }
}
internal sealed class FlagBeginCommand : ICommand
{
	public Color Color => Color.DarkGoldenrod;
	public string Label => $"Flag Begin with value {FlagValue}";
	public string Arguments => $"{FlagValue}";

	public byte FlagValue { get; set; }
}
internal sealed class FlagEndCommand : ICommand
{
	public Color Color => Color.DarkGoldenrod;
	public string Label => $"Flag End";
	public string Arguments => string.Empty;

	public byte Command { get; set; }
}
internal sealed class SetChannelVolumeCommand : ICommand
{
	public Color Color => Color.Tomato;
	public string Label => $"Set Channel Volume";
	public string Arguments => $"{ChannelVolume}";

	public byte ChannelVolume { get; set; }
}
internal sealed class SetFineTuneCommand : ICommand
{
	public Color Color => Color.Coral;
	public string Label => $"Set Fine Tune";
	public string Arguments => $"{FineTune}";

	public byte FineTune { get; set; }
}
internal sealed class AddToFineTuneCommand : ICommand
{
	public Color Color => Color.Coral;
	public string Label => $"Add To Fine Tune";
	public string Arguments => $"{FineTuneAddValue}";

	public byte FineTuneAddValue { get; set; }
}
internal sealed class SetCoarseTuneCommand : ICommand
{
	public Color Color => Color.DarkBlue;
	public string Label => $"Set Coarse Tune";
	public string Arguments => $"{CoarseTune}";

	public byte CoarseTune { get; set; }
}
internal sealed class AddToCoarseTuneCommand : ICommand
{
	public Color Color => Color.DarkBlue;
	public string Label => $"Add To Coarse Tune";
	public string Arguments => $"{CoarseTuneAddValue}";

	public ushort CoarseTuneAddValue { get; set; }
}
internal sealed class SweepTuneCommand : ICommand
{
	public Color Color => Color.Crimson;
	public string Label => $"Sweep Tune";
	public string Arguments => $"{SweepTuneRate}, {SweepTuneTarget}";

	public ushort SweepTuneRate { get; set; }
	public byte SweepTuneTarget { get; set; }
}
internal sealed class SetRandomNoteRangeCommand : ICommand
{
	public Color Color => Color.Beige;
	public string Label => $"Set Random Note Range: {RandomNoteRangeMin}-{RandomNoteRangeMax}";
	public string Arguments => $"{RandomNoteRangeMin}, {RandomNoteRangeMax}";

	public byte RandomNoteRangeMin { get; set; }
	public byte RandomNoteRangeMax { get; set; }
}
internal sealed class SetDetuneRangeCommand : ICommand
{
	public Color Color => Color.Honeydew;
	public string Label => $"Set Detune Range";
	public string Arguments => $"{DetuneRange}";

	public ushort DetuneRange { get; set; }
}
internal sealed class SetParamCommand : ICommand
{
	public Color Color => Color.DimGray;
	public string Label => $"Set Parameter And Value";
	public string Arguments => $"{ParamValue}, {ParamTarget}";

	public byte ParamValue { get; set; }
	public byte ParamTarget { get; set; }
}
internal sealed class ReplaceLFO1AsPitchCommand : ICommand
{
	public Color Color => Color.YellowGreen;
	public string Label => $"Replace LFO1 As Pitch";
	public string Arguments => $"{Rate}, {Depth}, {WaveformType}";

	public ushort Rate { get; set; }
	public ushort Depth { get; set; }
	public WaveformType WaveformType { get; set; }
}
internal sealed class SetLFO1DelayFade : ICommand
{
	public Color Color => Color.YellowGreen;
	public string Label => $"Set LFO1 To Delay Fade";
	public string Arguments => $"{Delay}, {FadeTime}";

	public ushort Delay { get; set; }
	public ushort FadeTime { get; set; }
}
internal sealed class SetLFO1ToPitchEnabledCommand : ICommand
{
	public Color Color => Color.YellowGreen;
	public string Label => $"Set LFO1 To Pitch Enabled";
	public string Arguments => $"{PitchEnabled}";

	public bool PitchEnabled { get; set; }
}
internal sealed class ReplaceLFO2AsVolumeCommand : ICommand
{
	public Color Color => Color.YellowGreen;
	public string Label => $"Replace LFO1 As Volume";
	public string Arguments => $"{Rate}, {Depth}, {WaveformType}";

	public ushort Rate { get; set; }
	public ushort Depth { get; set; }
	public WaveformType WaveformType { get; set; }
}
internal sealed class SetLFO2DelayFade : ICommand
{
	public Color Color => Color.YellowGreen;
	public string Label => $"Set LFO2 To Delay Fade";
	public string Arguments => $"{Delay}, {FadeTime}";

	public ushort Delay { get; set; }
	public ushort FadeTime { get; set; }
}
internal sealed class SetLFO2ToVolumeEnabledCommand : ICommand
{
	public Color Color => Color.YellowGreen;
	public string Label => $"Set LFO2 To Volume Enabled";
	public string Arguments => $"{VolumeEnabled}";

	public bool VolumeEnabled { get; set; }
}
internal sealed class ReplaceLFO3AsPanpotCommand : ICommand
{
	public Color Color => Color.YellowGreen;
	public string Label => $"Replace LFO3 As Panpot";
	public string Arguments => $"{Rate}, {Depth}, {WaveformType}";

	public ushort Rate { get; set; }
	public ushort Depth { get; set; }
	public WaveformType WaveformType { get; set; }
}
internal sealed class SetLFO3DelayFade : ICommand
{
	public Color Color => Color.YellowGreen;
	public string Label => $"Set LFO3 To Delay Fade";
	public string Arguments => $"{Delay}, {FadeTime}";

	public ushort Delay { get; set; }
	public ushort FadeTime { get; set; }
}
internal sealed class SetLFO3ToPanpotEnabledCommand : ICommand
{
	public Color Color => Color.YellowGreen;
	public string Label => $"Set LFO2 To Volume Enabled";
	public string Arguments => $"{PanpotEnabled}";

	public bool PanpotEnabled { get; set; }
}
internal sealed class ReplaceLFOCommand : ICommand
{
	public Color Color => Color.DarkSalmon;
	public string Label => $"Replace LFO";
	public string Arguments => $"{Rate}, {Depth}, {WaveformType}";

	public ushort Rate { get; set; }
	public ushort Depth { get; set; }
	public WaveformType WaveformType { get; set; }
}
internal sealed class SetLFODelayFadeCommand : ICommand
{
	public Color Color => Color.DarkSalmon;
	public string Label => $"Set LFO To Delay Fade";
	public string Arguments => $"{Delay}, {FadeTime}";

	public ushort Delay { get; set; }
	public ushort FadeTime { get; set; }
}
internal sealed class SetLFOParamCommand : ICommand
{
	public Color Color => Color.AliceBlue;
	public string Label => $"Set LFO Parameters";
	public string Arguments => $"{ParamType}, {WaveformType}";

	public ParameterType ParamType { get; set; }
	public WaveformType WaveformType { get; set; }
}
internal sealed class SetLFORouteCommand : ICommand
{
	public Color Color => Color.Lavender;
	public string Label => $"Set LFO Route";
	public string Arguments => $"{Target}, {Enabled}, {TargetID}";

	public byte Target { get; set; }
	public bool Enabled { get; set; }
	public byte TargetID { get; set; }
}
internal sealed class AddToTrackVolCommand : ICommand
{
	public Color Color => Color.Chocolate;
	public string Label => $"Add To Track Volume";
	public string Arguments => $"{TrackVolAdd}";

	public byte TrackVolAdd { get; set; }
}
internal sealed class SweepTrackVolCommand : ICommand
{
	public Color Color => Color.Aquamarine;
	public string Label => $"Sweep Track Volume";
	public string Arguments => $"{SweepRate}, {SweepVolume}";

	public ushort SweepRate { get; set; }
	public byte SweepVolume { get; set; }
}
internal sealed class AddToPanpotCommand : ICommand
{
	public Color Color => Color.BurlyWood;
	public string Label => $"Add To Panpot";
	public string Arguments => $"{PanpotAdd}";

	public sbyte PanpotAdd { get; set; }
}
internal sealed class SweepPanpotCommand : ICommand
{
	public Color Color => Color.ForestGreen;
	public string Label => $"Sweep Panpot";
	public string Arguments => $"{SweepRate}, {PanpotTarget}";

	public ushort SweepRate { get; set; }
	public byte PanpotTarget { get; set; }
}
internal sealed class ScenarioSyncCommand : ICommand
{
	public Color Color => Color.Pink;
	public string Label => $"Scenario Sync #{Checkpoint}";
	public string Arguments => $"{Checkpoint}";

	public byte Checkpoint { get; set; }
}
internal sealed class UnknownCommand : ICommand
{
	public Color Color => Color.MediumVioletRed;
	public string Label => $"Unknown 0x{Command:X}";
	public string Arguments => string.Join(", ", Args.Select(b => $"0x{b:X}"));

	public byte Command { get; set; }
	public byte[] Args { get; set; } = null!;
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
