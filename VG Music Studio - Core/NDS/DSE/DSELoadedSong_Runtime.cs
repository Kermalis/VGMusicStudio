using System;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

internal sealed partial class DSELoadedSong
{
	public void UpdateSongState(SongState info)
	{
		for (int trackIndex = 0; trackIndex < Tracks.Length; trackIndex++)
		{
			Tracks[trackIndex].UpdateSongState(info.Tracks[trackIndex]);
		}
	}

	public void ExecuteNext(DSETrack track)
	{
		// Note: Within a SMD file, all event values
		// after a command byte are all written in
		// Little Endian byte order, regardless of
		// whenever the DSE version is PlayStation 2,
		// DS or Wii, even if the hardware CPU
		// (eg. PowerPC) natively reads in a different
		// byte order.

		byte cmd = SMDFile[track.CurOffset++];
		if (cmd <= 0x7F)
		{
			byte arg = SMDFile[track.CurOffset++];
			int numParams = (arg & 0xC0) >> 6;
			int oct = ((arg & 0x30) >> 4) - 2;
			int n = arg & 0xF;
			if (n >= 12)
			{
				throw new DSEInvalidNoteException(track.Index, track.CurOffset - 2, n);
			}

			uint duration;
			if (numParams == 0)
			{
				duration = track.LastNoteDuration;
			}
			else
			{
				duration = 0;
				for (int b = 0; b < numParams; b++)
				{
					duration = (duration << 8) | SMDFile[track.CurOffset++];
				}
				track.LastNoteDuration = duration;
			}
			DSEChannel channel = _player.DMixer!.AllocateChannel()
				?? throw new Exception("Not enough channels");

			channel.Stop();
			track.Octave = (byte)(track.Octave + oct);
			if (channel.StartChannel(LocalSWD!, _player.MainSWD, track, n + (12 * track.Octave), duration))
			{
				channel.NoteVelocity = cmd;
				track.Channels.Add(channel);
			}
			channel.SweepCounter = 0;
		}
		else if (cmd is >= 0x80 and <= 0x8F)
		{
			track.LastRest = DSEUtils.FixedRests[cmd - 0x80];
			track.Rest = track.LastRest;
		}
		else // 0x90-0xFF
		{
			// TODO: 0x95, 0x9E
			switch (cmd)
			{
				// Pause processing events for duration of last
				// pause(include Fixed duration pauses). Doesn't silence the track.
				case 0x90:
					{
						track.Rest = track.LastRest;
						break;
					}

				// Pause for duration of last pause(include Fixed duration pauses) + a signed nb Ticks
				// Doesn't silence the track.(seen in bgm0021)
				case 0x91:
					{
						track.LastRest = (uint)(track.LastRest + (sbyte)SMDFile[track.CurOffset++]);
						track.Rest = track.LastRest;
						break;
					}

				// Pause processing events for nb Ticks
				// Doesn't silence the track.
				case 0x92:
					{
						track.LastRest = SMDFile[track.CurOffset++];
						track.Rest = track.LastRest;
						break;
					}

				// Pause processing events for specified
				// amount of ticks. (little endian)
				// Doesn't silence the track.
				case 0x93:
					{
						track.LastRest = (uint)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						track.Rest = track.LastRest;
						break;
					}

				// Pause processing events for specified
				// amount of ticks. (little endian)
				// Doesn't silence the track.
				case 0x94:
					{
						track.LastRest = (uint)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8) | (SMDFile[track.CurOffset++] << 16));
						track.Rest = track.LastRest;
						break;
					}

				// Pause until all notes are released! 
				// It re-checks at the rate specified as param in ticks!
				// So it will always pause at least "tick_interval" ticks.
				// (uint8) tick_interval
				case 0x95:
					{
						for (track.TickInterval = SMDFile[track.CurOffset++]; track.TickInterval > 0; track.TickInterval--)
						{
							track.LastNoteDuration = track.TickInterval;
						}
						break;
					}

				// Invalid (Disable track)
				case 0x96:

				// Invalid (Disable track)
				case 0x97:
					{
						track.Stopped = true;
						break;
					}

				// End of track terminator + Filler
				// Can have a delta-time prefixed.
				case 0x98:
					{
						if (track.LoopOffset == -1)
						{
							track.Stopped = true;
						}
						else
						{
							track.CurOffset = track.LoopOffset;
						}
						break;
					}

				// Sets the looping point for the
				// track.
				case 0x99:
					{
						track.LoopOffset = track.CurOffset;
						break;
					}

				// Invalid (Disable track)
				case 0x9A:

				// Invalid (Disable track)
				case 0x9B:
					{
						track.Stopped = true;
						break;
					}

				// Repeat mark (Dal Segno Al Coda): (Seen in bgm0113.smd)
				// Start of the segment to repeat, when the next event 0x9D is processed. (confirmed ?)
				// (uint8) nb_repeats
				case 0x9C:
					{
						track.DalSegnoCommands!.Add(new SongEvent(track.CurOffset + 1, new DalSegnoAlCodaCommand { Command = cmd, Repeats = SMDFile[track.CurOffset++] }));
						break;
					}

				// Repeat from mark (Dal Segno Al Fine):
				// Repeats the segment starting at the previous event 0x9C, the amount of times specified in event 0x9C. (confirmed ?)
				case 0x9D:
					{
						var segno = (DalSegnoAlCodaCommand)track.DalSegnoCommands![^1].Command;
						if (segno.Repeats > 0)
						{
							segno.Repeats--;
							track.CurOffset = (int)track.DalSegnoCommands[^1].Offset;
						}
						else
						{
							track.DalSegnoCommands.Remove(track.DalSegnoCommands[^1]);
							if (track.ToCodaCommand is not null && track.DalSegnoCommands.Capacity is 0)
							{
								track.CurOffset = (int)track.ToCodaCommand!.Offset;
							}
						}
						break;
					}

				// After repeat mark (To Coda):
				// After the segment has been repeated enough times, it will then jump to this from the last 0x9D event.(confirmed ?)
				case 0x9E:
					{
						break;
					}

				// Invalid (Disable track)
				case 0x9F:
					{
						track.Stopped = true;
						break;
					}

				// Reset the current pitch to the octave specified.
				// ( Ex: To play C6, you'd set this to 6, for C4,
				// you'd set it to 4, and so on! )
				case 0xA0:
					{
						track.Octave = SMDFile[track.CurOffset++];
						break;
					}

				// Adds the value specified to the current track octave.
				case 0xA1:
					{
						track.Octave = (byte)(track.Octave + (sbyte)SMDFile[track.CurOffset++]);
						break;
					}

				// Invalid (Disable track)
				case 0xA2:

				// Invalid (Disable track)
				case 0xA3:
					{
						track.Stopped = true;
						break;
					}

				// Sets the tempo in BPM. Only on Track#0
				case 0xA4:
					{
						_player.Tempo = SMDFile[track.CurOffset++];
						break;
					}

				// Sets the tempo in BPM. Only on Track#0 ?
				case 0xA5:
					{
						_player.Tempo = SMDFile[track.CurOffset++];
						break;
					}

				// Invalid (Disable track)
				case 0xA6:

				// Invalid (Disable track)
				case 0xA7:
					{
						track.Stopped = true;
						break;
					}

				// SetSWDAndBank (Seen in ev_e09b.sed)
				case 0xA8:
					{
						track.WaveIDIndex = SMDFile[track.CurOffset++];
						track.BankIDIndex = SMDFile[track.CurOffset++];
						break;
					}

				// SetBankHi Sets the bank id high byte
				// (Value from the file's header at offset 0xF)
				case 0xA9:
					{
						track.BankHi = SMDFile[track.CurOffset++];
						break;
					}

				// SetBankLo Sets the bank id low byte
				// (Value from the file's header at offset 0xE)
				case 0xAA:
					{
						track.BankLo = SMDFile[track.CurOffset++];
						break;
					}

				// Skips processing the next byte.
				case 0xAB:
					{
						track.CurOffset++;
						break;
					}

				// Set the current instrument/program.
				// Program/Instrument IDs are from the associated SWD file.
				case 0xAC:
					{
						track.Voice = SMDFile[track.CurOffset++];
						break;
					}

				// Invalid (Disable track)
				case 0xAD:

				// Invalid (Disable track)
				case 0xAE:
					{
						track.Stopped = true;
						break;
					}

				// SweepSongVolume
				case 0xAF:
					{
						track.SweepRate = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						track.SweepPitch = SMDFile[track.CurOffset++];
						break;
					}

				// DisableEnvelope
				case 0xB0:
					{
						track.AttackVolume = 0;
						track.AttackTime = 0;
						track.Hold = 0;
						track.Decay = 0;
						track.Sustain = 0;
						track.Fade = 0;
						track.Release = 0;
						break;
					}

				// SetEnvelopeAttackVolume
				case 0xB1:
					{
						track.AttackVolume = SMDFile[track.CurOffset++];
						break;
					}

				// SetEnvelopeAttackTime (Seen in ev_e09b.sed)
				case 0xB2:
					{
						track.AttackTime = SMDFile[track.CurOffset++];
						break;
					}

				// SetEnvelopeHold
				case 0xB3:
					{
						track.Hold = SMDFile[track.CurOffset++];
						break;
					}

				// SetEnvelopeDecaySustain (Seen in ev_e09b.sed)
				// (setting either to 0xFF means it won't change it)
				case 0xB4:
					{
						if (SMDFile[track.CurOffset] is not 0xFF)
						{
							track.Decay = SMDFile[track.CurOffset++];
						}
						else
						{
							track.CurOffset++;
						}
						if (SMDFile[track.CurOffset] is not 0xFF)
						{
							track.Sustain = SMDFile[track.CurOffset++];
						}
						else
						{
							track.CurOffset++;
						}
						break;
					}

				// SetEnvelopeFade (Seen in bgm0100.smd)
				case 0xB5:
					{
						track.Fade = SMDFile[track.CurOffset++];
						break;
					}

				// SetEnvelopeRelease
				case 0xB6:
					{
						track.Release = SMDFile[track.CurOffset++];
						break;
					}

				// Invalid (Disable track)
				case 0xB7:

				// Invalid (Disable track)
				case 0xB8:

				// Invalid (Disable track)
				case 0xB9:

				// Invalid (Disable track)
				case 0xBA:

				// Invalid (Disable track)
				case 0xBB:
					{
						track.Stopped = true;
						break;
					}

				// SetNoteVolume (? Does he means velocity, aftertouch, or something else?)
				case 0xBC:
					{
						track.NoteVolume = SMDFile[track.CurOffset++];
						break;
					}

				// Invalid (Disable track)
				case 0xBD:
					{
						track.Stopped = true;
						break;
					}

				// SetChannelPanpot: Set the panpot at the CHANNEL level.
				// (seen in last 3 tracks of bgm0048.smd)
				case 0xBE:
					{
						track.ChannelPanpot = SMDFile[track.CurOffset++];
						break;
					}

				// SetFlag0 (note by coda : sets or clears flag bit 0)
				case 0xBF:
					{
						track.FlagEnded = false;
						track.FlagValue = SMDFile[track.CurOffset++];
						break;
					}

				// SetFlag1 (note by coda: sets flag bit 1)
				case 0xC0:
					{
						track.FlagEnded = true;
						break;
					}

				// Invalid (Disable track)
				case 0xC1:

				// Invalid (Disable track)
				case 0xC2:
					{
						track.Stopped = true;
						break;
					}

				// SetChannelVolume: Set the volume at the CHANNEL level(used in inazuma eleven a lot)
				case 0xC3:
					{
						track.ChannelVolume = SMDFile[track.CurOffset++];
						break;
					}

				// Invalid (Disable track)
				case 0xC4:

				// Invalid (Disable track)
				case 0xC5:

				// Invalid (Disable track)
				case 0xC6:

				// Invalid (Disable track)
				case 0xC7:

				// Invalid (Disable track)
				case 0xC8:

				// Invalid (Disable track)
				case 0xC9:

				// Invalid (Disable track)
				case 0xCA:
					{
						track.Stopped = true;
						break;
					}

				// Skip Next 2 bytes. Seen in Professor Layton and the Last Specter, BGM_10.SMD
				case 0xCB:
					{
						track.CurOffset += 2;
						break;
					}

				// Invalid (Disable track)
				case 0xCC:

				// Invalid (Disable track)
				case 0xCD:

				// Invalid (Disable track)
				case 0xCE:

				// Invalid (Disable track)
				case 0xCF:
					{
						track.Stopped = true;
						break;
					}

				// SetFineTune (Seen in bgm0100.smd)
				case 0xD0:
					{
						track.FineTune = SMDFile[track.CurOffset++];
						break;
					}

				// AddToFineTune (Seen in bgm0113.smd)
				case 0xD1:
					{
						track.FineTuneAdd = SMDFile[track.CurOffset++];
						break;
					}

				// SetCoarseTune (Seen in bgm0116.smd)
				case 0xD2:
					{
						track.CoarseTune = SMDFile[track.CurOffset++];
						break;
					}

				// AddToCoarseTune (Seen in bgm0116.smd) (That seems wrong because setcoarsetune param is smaller than this one)
				case 0xD3:
					{
						track.CoarseTuneAdd = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						break;
					}

				// SweepTune (Seen in ev_e09b.sed)
				case 0xD4:
					{
						track.SweepTuneRate = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						track.SweepTuneTarget = SMDFile[track.CurOffset++];
						break;
					}

				// SetRandomNoteRange
				case 0xD5:
					{
						track.RandomNoteRangeMin = SMDFile[track.CurOffset++];
						track.RandomNoteRangeMax = SMDFile[track.CurOffset++];
						break;
					}

				// SetDetuneRange: random detune (Seen in bgm0101.smd)
				case 0xD6:
					{
						track.DetuneRange = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						break;
					}

				// Pitch bend. Works the same as the MIDI event 0xE0 Pitch Wheel.
				// The uint16 is stored in big endian order.
				// 500 == 1 semitone. Negative val, means increase pitch, positive the opposite.
				case 0xD7:
					{
						track.PitchBend = (ushort)((SMDFile[track.CurOffset++] << 8) | SMDFile[track.CurOffset++]);
						break;
					}

				// SetParam (note by coda: modifies an unused parameter)
				case 0xD8:
					{
						track.CurOffset += 2;
						break;
					}

				// Invalid (Disable track)
				case 0xD9:

				// Invalid (Disable track)
				case 0xDA:
					{
						track.Stopped = true;
						break;
					}

				// SetPitchBendRange: In semitones (Used in bgm0000, and bgm0134 (Value usually range between 0,2,4,7,12,24))
				case 0xDB:
					{
						track.PitchBendRange = SMDFile[track.CurOffset++];
						break;
					}

				// ReplaceLFO1AsPitch (Seen in ev_e09b.sed)
				case 0xDC:
					{
						track.LFO1Rate = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						track.LFO1Depth = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						track.LFO1WaveformType = (WaveformType)SMDFile[track.CurOffset++];
						break;
					}

				// SetLFO1DelayFade (Seen in ev_e09b.sed)
				case 0xDD:
					{
						track.LFO1Delay = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						track.LFO1FadeTime = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						break;
					}

				// Invalid (Disable track)
				case 0xDE:
					{
						track.Stopped = true;
						break;
					}

				// SetLFO1ToPitchEnabled: (Used in bgm0000)
				// If true, turns on LFO1 and connects it to pitch. If false, disables LFO1
				case 0xDF:
					{
						track.LFO1PitchEnabled = SMDFile[track.CurOffset] is 0 || SMDFile[track.CurOffset] is 1;
						track.CurOffset++;
						break;
					}

				// Track volume, similar to GM CC#7.
				case 0xE0:
					{
						track.Volume = SMDFile[track.CurOffset++];
						break;
					}

				// AddToTrackVol
				case 0xE1:
					{
						track.VolumeAdd = SMDFile[track.CurOffset++];
						break;
					}

				// SweepTrackVol (Seen in ev_e09b.sed)
				case 0xE2:
					{
						track.SweepRate = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						track.SweepVolume = SMDFile[track.CurOffset++];
						break;
					}

				// Similar to the GM CC#11.
				// Its a secondary volume control
				// there to avoid messing with the
				// dynamic range of instruments
				// when changing track volume!
				case 0xE3:
					{
						track.Expression = SMDFile[track.CurOffset++];
						break;
					}

				// ReplaceLFO2AsVolume
				case 0xE4:
					{
						track.LFO2Rate = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						track.LFO2Depth = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						track.LFO2WaveformType = (WaveformType)SMDFile[track.CurOffset++];
						break;
					}

				// SetLFO2DelayFade
				case 0xE5:
					{
						track.LFO2Delay = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						track.LFO2FadeTime = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						break;
					}

				// Invalid (Disable track)
				case 0xE6:
					{
						track.Stopped = true;
						break;
					}

				// SetLFO2ToVolumeEnabled:
				// If true, turns on LFO2 and connects it to volume. If false, disables LFO2.
				case 0xE7:
					{
						track.LFO2VolumeEnabled = SMDFile[track.CurOffset] is 0 || SMDFile[track.CurOffset] is 1;
						track.CurOffset++;
						break;
					}

				// SetPanpot
				// 0x00 == Full Left
				// 0x40 == Middle
				// 0x80 == Full Right
				case 0xE8:
					{
						track.Panpot = (sbyte)(SMDFile[track.CurOffset++] - 0x40);
						break;
					}

				// AddToPan
				case 0xE9:
					{
						track.PanpotAdd = SMDFile[track.CurOffset++];
						break;
					}

				// SweepPanpot (Seen in bgm0100.smd)
				case 0xEA:
					{
						track.SweepRate = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						track.PanpotTarget = SMDFile[track.CurOffset++];
						break;
					}

				// Invalid (Disable track)
				case 0xEB:
					{
						track.Stopped = true;
						break;
					}

				// ReplaceLFO3AsPanpot (Seen in ev_e09b.sed)
				case 0xEC:
					{
						track.LFO3Rate = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						track.LFO3Depth = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						track.LFO3WaveformType = (WaveformType)SMDFile[track.CurOffset++];
						break;
					}

				// SetLFO3DelayFade (Seen in ev_e09b.sed)
				case 0xED:
					{
						track.LFO3Delay = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						track.LFO3FadeTime = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						break;
					}

				// Invalid (Disable track)
				case 0xEE:
					{
						track.Stopped = true;
						break;
					}

				// SetLFO3ToPanpotEnabled: (Seen in ev_e09b.sed)
				// If true, turns on LFO3 and connects it to pan. If false, disables LFO3.
				case 0xEF:
					{
						track.LFO3PanpotEnabled = SMDFile[track.CurOffset] is 0 || SMDFile[track.CurOffset] is 1;
						track.CurOffset++;
						break;
					}

				// ReplaceLFO: (Seen in ev_e09b.sed)
				// Rate is in Hertz.
				// LFO delay and LFO fade time are set to 0.
				// *For waveform ids see "LFO Waveforms IDs" below
				case 0xF0:
					{
						track.LFORate = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						track.LFODepth = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						track.LFOWaveformType = (WaveformType)SMDFile[track.CurOffset++];
						break;
					}

				// SetLFODelayFade: (Seen in ev_e09b.sed)
				case 0xF1:
					{
						track.LFODelay = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						track.LFOFadeTime = (ushort)(SMDFile[track.CurOffset++] | (SMDFile[track.CurOffset++] << 8));
						break;
					}

				// SetLFOParam: (Seen in ev_e09b.sed)
				// 
				// *For parameter ids see "LFO Parameter IDs" below
				// *For waveform ids see "LFO Waveforms IDs" below
				case 0xF2:
					{
						track.LFOParamType = (ParameterType)SMDFile[track.CurOffset++];
						track.LFOParamWaveformType = (WaveformType)SMDFile[track.CurOffset++];
						break;
					}

				// SetLFORoute (Seen in ev_e09b.sed)
				// 
				// *For target ids see "LFO Target IDs" below
				case 0xF3:
					{
						track.LFOTarget = SMDFile[track.CurOffset++];
						track.LFOEnabled = SMDFile[track.CurOffset] is 0 || SMDFile[track.CurOffset] is 1;
						track.CurOffset++;
						track.LFOType = (LFOType)SMDFile[track.CurOffset++];
						break;
					}

				// Invalid (Disable track)
				case 0xF4:

				// Invalid (Disable track)
				case 0xF5:
					{
						track.Stopped = true;
						break;
					}

				// ScenarioSync(Seen in bgm0001.smd) param value is usually between 0x0-0xf Seems to be used to sync music and script engine scene!
				case 0xF6:
					{
						track.CurOffset++;
						break;
					}

				// Invalid (Disable track)
				case 0xF7:
					{
						track.Stopped = true;
						break;
					}

				// Skips processing next 2 bytes.
				case 0xF8:
					{
						track.CurOffset += 2;
						break;
					}

				// Invalid (Disable track)
				case 0xF9:

				// Invalid (Disable track)
				case 0xFA:

				// Invalid (Disable track)
				case 0xFB:

				// Invalid (Disable track)
				case 0xFC:

				// Invalid (Disable track)
				case 0xFD:

				// Invalid (Disable track)
				case 0xFE:

				// Invalid (Disable track)
				case 0xFF:
					{
						track.Stopped = true;
						break;
					}

				default: throw new DSEInvalidCMDException(track.Index, track.CurOffset - 1, cmd);
			}
		}
	}
}
