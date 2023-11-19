namespace Kermalis.VGMusicStudio.MIDI;

public sealed class MIDIEvent
{
	public int Ticks { get; internal set; }
	public int DeltaTicks => Prev is null ? Ticks : Ticks - Prev.Ticks;

	public MIDIMessage Message { get; set; }

	public MIDIEvent? Prev { get; internal set; }
	public MIDIEvent? Next { get; internal set; }

	internal MIDIEvent(int ticks, MIDIMessage msg)
	{
		Ticks = ticks;
		Message = msg;
	}
}