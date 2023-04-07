using Kermalis.MIDI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

public sealed class MIDISaveArgs
{
	public bool SaveCommandsBeforeTranspose; // TODO: I forgor why I would want this
	public bool ReverseVolume;
	public (int AbsoluteTick, (byte Numerator, byte Denominator))[] TimeSignatures;

	public MIDISaveArgs(bool saveCmdsBeforeTranspose, bool reverseVol, (int, (byte, byte))[] timeSignatures)
	{
		SaveCommandsBeforeTranspose = saveCmdsBeforeTranspose;
		ReverseVolume = reverseVol;
		TimeSignatures = timeSignatures;
	}
}

internal sealed partial class MP2KLoadedSong
{
	// TODO: Don't use events, read from rom
	public void SaveAsMIDI(string fileName, MIDISaveArgs args)
	{
		// TODO: FINE vs PREV
		// TODO: https://github.com/Kermalis/VGMusicStudio/issues/36
		// TODO: Nested calls
		// TODO: REPT

		// These TODO shouldn't affect matching because they are unsupported anyway:
		// TODO: Drums that use more than 127 notes need to use bank select
		// TODO: Use bank select with voices above 127

		byte baseVolume = 0x7F;
		if (args.ReverseVolume)
		{
			baseVolume = Events.SelectMany(e => e).Where(e => e.Command is VolumeCommand).Select(e => ((VolumeCommand)e.Command).Volume).Max();
			Debug.WriteLine($"Reversing volume back from {baseVolume}.");
		}

		var midi = new MIDIFile(MIDIFormat.Format1, TimeDivisionValue.CreatePPQN(24), Events.Length + 1);
		var metaTrack = new MIDITrackChunk();
		midi.AddChunk(metaTrack);

		foreach ((int AbsoluteTick, (byte Numerator, byte Denominator)) e in args.TimeSignatures)
		{
			metaTrack.InsertMessage(e.AbsoluteTick, MetaMessage.CreateTimeSignatureMessage(e.Item2.Numerator, e.Item2.Denominator));
		}

		for (byte trackIndex = 0; trackIndex < Events.Length; trackIndex++)
		{
			var track = new MIDITrackChunk();
			midi.AddChunk(track);

			bool foundTranspose = false;
			int endOfPattern = 0;
			long startOfPatternTicks = 0;
			long endOfPatternTicks = 0;
			sbyte transpose = 0;
			int? endTicks = null;
			var playing = new List<NoteCommand>();
			List<SongEvent> trackEvents = Events[trackIndex];
			for (int i = 0; i < trackEvents.Count; i++)
			{
				SongEvent e = trackEvents[i];
				int ticks = (int)(e.Ticks[0] + (endOfPatternTicks - startOfPatternTicks));

				// Preliminary check for saving events before transpose
				switch (e.Command)
				{
					case TransposeCommand c:
					{
						foundTranspose = true;
						break;
					}
					default: // If we should not save before transpose then skip this event
					{
						if (!args.SaveCommandsBeforeTranspose && !foundTranspose)
						{
							continue;
						}
						break;
					}
				}

				// Now do the event magic...
				switch (e.Command)
				{
					case CallCommand c:
					{
						int callCmd = trackEvents.FindIndex(ev => ev.Offset == c.Offset);
						endOfPattern = i;
						endOfPatternTicks = e.Ticks[0];
						i = callCmd - 1; // -1 for incoming ++
						startOfPatternTicks = trackEvents[callCmd].Ticks[0];
						break;
					}
					case EndOfTieCommand c:
					{
						NoteCommand? nc = c.Note == -1 ? playing.LastOrDefault() : playing.LastOrDefault(no => no.Note == c.Note);
						if (nc is not null)
						{
							int key = nc.Note + transpose;
							if (key < 0)
							{
								key = 0;
							}
							else if (key > 0x7F)
							{
								key = 0x7F;
							}
							track.InsertMessage(ticks, new NoteOnMessage(trackIndex, (MIDINote)key, 0));
							//track.InsertMessage(ticks, new NoteOffMessage(trackIndex, (MIDINote)key, 0));
							playing.Remove(nc);
						}
						break;
					}
					case FinishCommand _:
					{
						endTicks = ticks;
						goto endOfTrack;
					}
					case JumpCommand c:
					{
						if (trackIndex == 0)
						{
							int jumpCmd = trackEvents.FindIndex(ev => ev.Offset == c.Offset);
							metaTrack.InsertMessage((int)trackEvents[jumpCmd].Ticks[0], MetaMessage.CreateTextMessage(MetaMessageType.Marker, "["));
							metaTrack.InsertMessage(ticks, MetaMessage.CreateTextMessage(MetaMessageType.Marker, "]"));
						}
						break;
					}
					case LFODelayCommand c:
					{
						track.InsertMessage(ticks, new ControllerMessage(trackIndex, (ControllerType)26, c.Delay));
						break;
					}
					case LFODepthCommand c:
					{
						track.InsertMessage(ticks, new ControllerMessage(trackIndex, ControllerType.ModulationWheel, c.Depth));
						break;
					}
					case LFOSpeedCommand c:
					{
						track.InsertMessage(ticks, new ControllerMessage(trackIndex, (ControllerType)21, c.Speed));
						break;
					}
					case LFOTypeCommand c:
					{
						track.InsertMessage(ticks, new ControllerMessage(trackIndex, (ControllerType)22, (byte)c.Type));
						break;
					}
					case LibraryCommand c:
					{
						track.InsertMessage(ticks, new ControllerMessage(trackIndex, (ControllerType)30, c.Command));
						track.InsertMessage(ticks, new ControllerMessage(trackIndex, (ControllerType)29, c.Argument));
						break;
					}
					case MemoryAccessCommand c:
					{
						track.InsertMessage(ticks, new ControllerMessage(trackIndex, ControllerType.EffectControl2, c.Operator));
						track.InsertMessage(ticks, new ControllerMessage(trackIndex, (ControllerType)14, c.Address));
						track.InsertMessage(ticks, new ControllerMessage(trackIndex, ControllerType.EffectControl1, c.Data));
						break;
					}
					case NoteCommand c:
					{
						int note = c.Note + transpose;
						if (note < 0)
						{
							note = 0;
						}
						else if (note > 0x7F)
						{
							note = 0x7F;
						}
						track.InsertMessage(ticks, new NoteOnMessage(trackIndex, (MIDINote)note, c.Velocity));
						if (c.Duration != -1)
						{
							track.InsertMessage(ticks + c.Duration, new NoteOnMessage(trackIndex, (MIDINote)note, 0));
							//track.InsertMessage(ticks + c.Duration, new NoteOffMessage(trackIndex, (MIDINote)note, 0));
						}
						else
						{
							playing.Add(c);
						}
						break;
					}
					case PanpotCommand c:
					{
						track.InsertMessage(ticks, new ControllerMessage(trackIndex, ControllerType.Pan, (byte)(c.Panpot + 0x40)));
						break;
					}
					case PitchBendCommand c:
					{
						track.InsertMessage(ticks, new PitchBendMessage(trackIndex, 0, (byte)(c.Bend + 0x40)));
						break;
					}
					case PitchBendRangeCommand c:
					{
						track.InsertMessage(ticks, new ControllerMessage(trackIndex, (ControllerType)20, c.Range));
						break;
					}
					case PriorityCommand c:
					{
						track.InsertMessage(ticks, new ControllerMessage(trackIndex, ControllerType.ChannelVolumeLSB, c.Priority));
						break;
					}
					case ReturnCommand _:
					{
						if (endOfPattern != 0)
						{
							i = endOfPattern;
							endOfPattern = 0;
							startOfPatternTicks = 0;
							endOfPatternTicks = 0;
						}
						break;
					}
					case TempoCommand c:
					{
						metaTrack.InsertMessage(ticks, MetaMessage.CreateTempoMessage(c.Tempo));
						break;
					}
					case TransposeCommand c:
					{
						transpose = c.Transpose;
						break;
					}
					case TuneCommand c:
					{
						track.InsertMessage(ticks, new ControllerMessage(trackIndex, (ControllerType)24, (byte)(c.Tune + 0x40)));
						break;
					}
					case VoiceCommand c:
					{
						track.InsertMessage(ticks, new ProgramChangeMessage(trackIndex, (MIDIProgram)c.Voice));
						break;
					}
					case VolumeCommand c:
					{
						double d = baseVolume / (double)0x7F;
						int volume = (int)(c.Volume / d);
						// If there are rounding errors, fix them (happens if baseVolume is not 127 and baseVolume is not vol.Volume)
						if (volume * baseVolume / 0x7F == c.Volume - 1)
						{
							volume++;
						}
						track.InsertMessage(ticks, new ControllerMessage(trackIndex, ControllerType.ChannelVolume, (byte)volume));
						break;
					}
				}
			}
		endOfTrack:
			track.InsertMessage(endTicks ?? track.NumTicks, new MetaMessage(MetaMessageType.EndOfTrack, Array.Empty<byte>()));
		}

		metaTrack.InsertMessage(metaTrack.NumTicks, new MetaMessage(MetaMessageType.EndOfTrack, Array.Empty<byte>()));

		using (FileStream fs = File.Create(fileName))
		{
			midi.Save(fs);
		}
	}
}
