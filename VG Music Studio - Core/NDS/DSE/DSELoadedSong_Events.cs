using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core.Util;
using Kermalis.VGMusicStudio.Core.Util.EndianBinaryExtras;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

internal sealed partial class DSELoadedSong
{
	private void AddEvent(byte trackIndex, long cmdOffset, ICommand command)
	{
		Events[trackIndex].Add(new SongEvent(cmdOffset, command));
	}
	private bool EventExists(byte trackIndex, long cmdOffset)
	{
		return Events[trackIndex].Exists(e => e.Offset == cmdOffset);
	}

	private void AddTrackEvents(byte trackIndex, EndianBinaryReader r)
	{
		Events[trackIndex] = new List<SongEvent>();

		uint lastNoteDuration = 0;
		uint lastRest = 0;
		bool cont = true;
		while (cont)
		{
			long cmdOffset = r.Stream.Position;
			byte cmd = r.ReadByte();
			if (cmd <= 0x7F)
			{
				byte arg = r.ReadByte();
				int numParams = (arg & 0xC0) >> 6;
				int oct = ((arg & 0x30) >> 4) - 2;
				int n = arg & 0xF;
				if (n >= 12)
				{
					throw new DSEInvalidNoteException(trackIndex, (int)cmdOffset, n);
				}

				uint duration;
				if (numParams == 0)
				{
					duration = lastNoteDuration;
				}
				else // Big Endian reading of 8, 16, or 24 bits
				{
					duration = 0;
					for (int b = 0; b < numParams; b++)
					{
						duration = (duration << 8) | r.ReadByte();
					}
					lastNoteDuration = duration;
				}
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new NoteCommand { Note = (byte)n, OctaveChange = (sbyte)oct, Velocity = cmd, Duration = duration });
				}
			}
			else if (cmd >= 0x80 && cmd <= 0x8F)
			{
				lastRest = DSEUtils.FixedRests[cmd - 0x80];
				if (!EventExists(trackIndex, cmdOffset))
				{
					AddEvent(trackIndex, cmdOffset, new RestCommand { Rest = lastRest });
				}
			}
			else // 0x90-0xFF
			{
				// TODO: 0x95 - a rest that may or may not repeat depending on some condition within channels
				// TODO: 0x9E - may or may not jump somewhere else depending on an unknown structure
				switch (cmd)
				{
					case 0x90:
					{
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new RestCommand { Rest = lastRest });
						}
						break;
					}
					case 0x91:
					{
						lastRest = (uint)(lastRest + r.ReadSByte());
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new RestCommand { Rest = lastRest });
						}
						break;
					}
					case 0x92:
					{
						lastRest = r.ReadByte();
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new RestCommand { Rest = lastRest });
						}
						break;
					}
					case 0x93:
					{
						lastRest = r.ReadUInt16();
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new RestCommand { Rest = lastRest });
						}
						break;
					}
					case 0x94:
					{
						lastRest = new EndianBinaryReaderExtras(r).ReadUInt24();
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new RestCommand { Rest = lastRest });
						}
						break;
					}
					case 0x95:
					{
						uint intervals = r.ReadByte();
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new CheckIntervalCommand { Interval = intervals });
						}
						break;
					}
					case 0x96:
					{
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
						}
						break;
					}
					case 0x97:
					{
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
						}
						break;
					}
					case 0x98:
					{
						if (!EventExists(trackIndex, cmdOffset))
						{
							AddEvent(trackIndex, cmdOffset, new FinishCommand());
							r.Stream.Align(4);
						}
						cont = false;
						break;
					}
					case 0x99:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new LoopStartCommand { Offset = r.Stream.Position });
							}
							break;
						}
					case 0x9A:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0x9B:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0x9C:
						{
							byte repeats = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new DalSegnoAlCodaCommand { Command = cmd, Repeats = repeats });
							}
							break;
						}
					case 0x9D:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new DalSegnoAlFineCommand { Command = cmd });
							}
							break;
						}
					case 0x9E:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new ToCodaCommand { Command = cmd });
								Tracks[trackIndex].ToCodaCommand = new SongEvent(cmdOffset, new ToCodaCommand { Command = cmd });
							}
							break;
						}
					case 0x9F:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xA0:
						{
							byte octave = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new OctaveSetCommand { Octave = octave });
							}
							break;
						}
					case 0xA1:
						{
							sbyte change = r.ReadSByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new OctaveAddCommand { OctaveChange = change });
							}
							break;
						}
					case 0xA2:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xA3:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xA4:
						{
							byte tempoArg = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new TempoCommand { Command = cmd, Tempo = tempoArg });
							}
							break;
						}
					case 0xA5: // The code for these two is identical
						{
							byte tempoArg = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new TempoCommand { Command = cmd, Tempo = tempoArg });
							}
							break;
						}
					case 0xA6:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xA7:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xA8:
						{
							byte waveID = r.ReadByte();
							byte bankID = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetSWDAndBankCommand { WaveID = waveID, BankID = bankID });
							}
							break;
						}
					case 0xA9:
						{
							byte bankHiID = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetBankHiCommand { BankHiID = bankHiID });
							}
							break;
						}
					case 0xAA:
						{
							byte bankLoID = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetBankLoCommand { BankLoID = bankLoID });
							}
							break;
						}
					case 0xAB:
						{
							byte[] bytes = new byte[1];
							r.ReadBytes(bytes);
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SkipBytesCommand { Command = cmd, SkippedBytes = bytes });
							}
							break;
						}
					case 0xAC:
						{
							byte voice = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new VoiceCommand { Voice = voice });
							}
							break;
						}
					case 0xAD:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xAE:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xAF:
						{
							ushort sweepRate = r.ReadUInt16();
							byte sweepPitch = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SweepSongVolumeCommand { SweepRate = sweepRate, SweepPitch = sweepPitch });
							}
							break;
						}
					case 0xB0:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new DisableEnvelopeCommand { Command = cmd });
							}
							break;
						}
					case 0xB1:
						{
							byte attackVolume = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetEnvelopeAttackVolumeCommand { AttackVolume = attackVolume });
							}
							break;
						}
					case 0xB2:
						{
							byte attackTime = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetEnvelopeAttackTimeCommand { AttackTime = attackTime });
							}
							break;
						}
					case 0xB3:
						{
							byte hold = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetEnvelopeHoldCommand { Hold = hold });
							}
							break;
						}
					case 0xB4:
						{
							byte decay = r.ReadByte();
							byte sustain = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetEnvelopeDecaySustainCommand { Decay = decay, Sustain = sustain });
							}
							break;
						}
					case 0xB5:
						{
							byte fade = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetEnvelopeFadeCommand { Fade = fade });
							}
							break;
						}
					case 0xB6:
						{
							byte release = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetEnvelopeReleaseCommand { Release = release });
							}
							break;
						}
					case 0xB7:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xB8:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xB9:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xBA:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xBB:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xBC:
						{
							byte noteVolume = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetNoteVolumeCommand { NoteVolume = noteVolume });
							}
							break;
						}
					case 0xBD:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xBE:
						{
							byte channelPanpot = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetChannelPanpotCommand { ChannelPanpot = channelPanpot });
							}
							break;
						}
					case 0xBF:
						{
							byte flagValue = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new FlagBeginCommand { FlagValue = flagValue });
							}
							break;
						}
					case 0xC0:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new FlagEndCommand { Command = cmd });
							}
							break;
						}
					case 0xC1:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xC2:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xC3:
						{
							byte channelVolume = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetChannelVolumeCommand { ChannelVolume = channelVolume });
							}
							break;
						}
					case 0xC4:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xC5:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xC6:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xC7:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xC8:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xC9:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xCA:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xCB:
						{
							byte[] bytes = new byte[2];
							r.ReadBytes(bytes);
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SkipBytesCommand { Command = cmd, SkippedBytes = bytes });
							}
							break;
						}
					case 0xCC:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xCD:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xCE:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xCF:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xD0:
						{
							byte fineTune = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetFineTuneCommand { FineTune = fineTune });
							}
							break;
						}
					case 0xD1:
						{
							byte fineTuneAddValue = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new AddToFineTuneCommand { FineTuneAddValue = fineTuneAddValue });
							}
							break;
						}
					case 0xD2:
						{
							byte coarseTune = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetCoarseTuneCommand { CoarseTune = coarseTune });
							}
							break;
						}
					case 0xD3:
						{
							ushort coarseTuneAddValue = r.ReadUInt16();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new AddToCoarseTuneCommand { CoarseTuneAddValue = coarseTuneAddValue });
							}
							break;
						}
					case 0xD4:
						{
							ushort sweepTuneRate = r.ReadUInt16();
							byte sweepTuneTarget = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SweepTuneCommand { SweepTuneRate = sweepTuneRate, SweepTuneTarget = sweepTuneTarget });
							}
							break;
						}
					case 0xD5:
						{
							byte randomNoteRangeMin = r.ReadByte();
							byte randomNoteRangeMax = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetRandomNoteRangeCommand { RandomNoteRangeMin = randomNoteRangeMin, RandomNoteRangeMax = randomNoteRangeMax });
							}
							break;
						}
					case 0xD6:
						{
							ushort detuneRange = r.ReadUInt16();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetDetuneRangeCommand { DetuneRange = detuneRange });
							}
							break;
						}
					case 0xD7:
						{
							ushort bend = r.ReadUInt16();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new PitchBendCommand { Bend = bend });
							}
							break;
						}
					case 0xD8:
						{
							byte paramValue = r.ReadByte();
							byte paramTarget = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetParamCommand { ParamValue = paramValue, ParamTarget = paramTarget });
							}
							break;
						}
					case 0xD9:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xDA:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xDB:
						{
							byte pitchBendRange = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetPitchBendRangeCommand { PitchBendRange = pitchBendRange });
							}
							break;
						}
					case 0xDC:
						{
							ushort rate = r.ReadUInt16();
							ushort depth = r.ReadUInt16();
							WaveformType waveformType = (WaveformType)r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new ReplaceLFO1AsPitchCommand { Rate = rate, Depth = depth, WaveformType = waveformType });
							}
							break;
						}
					case 0xDD:
						{
							ushort delay = r.ReadUInt16();
							ushort fadeTime = r.ReadUInt16();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetLFO1DelayFade { Delay = delay, FadeTime = fadeTime });
							}
							break;
						}
					case 0xDE:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xDF:
						{
							bool pitchEnabled = r.ReadBoolean();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetLFO1ToPitchEnabledCommand { PitchEnabled = pitchEnabled });
							}
							break;
						}
					case 0xE0:
						{
							byte volume = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new VolumeCommand { Volume = volume });
							}
							break;
						}
					case 0xE1:
						{
							byte trackAddVol = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new AddToTrackVolCommand { TrackVolAdd = trackAddVol });
							}
							break;
						}
					case 0xE2:
						{
							ushort sweepRate = r.ReadUInt16();
							byte sweepVolume = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SweepTrackVolCommand { SweepRate = sweepRate, SweepVolume = sweepVolume });
							}
							break;
						}
					case 0xE3:
						{
							byte expression = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new ExpressionCommand { Expression = expression });
							}
							break;
						}
					case 0xE4:
						{
							ushort rate = r.ReadUInt16();
							ushort depth = r.ReadUInt16();
							WaveformType waveformType = (WaveformType)r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new ReplaceLFO2AsVolumeCommand{ Rate = rate, Depth = depth, WaveformType = waveformType });
							}
							break;
						}
					case 0xE5:
						{
							ushort delay = r.ReadUInt16();
							ushort fadeTime = r.ReadUInt16();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetLFO2DelayFade { Delay = delay, FadeTime = fadeTime });
							}
							break;
						}
					case 0xE6:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xE7:
						{
							bool volumeEnabled = r.ReadBoolean();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetLFO2ToVolumeEnabledCommand { VolumeEnabled = volumeEnabled });
							}
							break;
						}
					case 0xE8:
						{
							byte panArg = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new PanpotCommand { Panpot = (sbyte)(panArg - 0x40) });
							}
							break;
						}
					case 0xE9:
						{
							byte panArg = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new AddToPanpotCommand { PanpotAdd = (sbyte)(panArg - 0x40) });
							}
							break;
						}
					case 0xEA:
						{
							ushort sweepRate = r.ReadUInt16();
							byte panpotTarget = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SweepPanpotCommand { SweepRate = sweepRate, PanpotTarget = panpotTarget });
							}
							break;
						}
					case 0xEB:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xEC:
						{
							ushort rate = r.ReadUInt16();
							ushort depth = r.ReadUInt16();
							WaveformType waveformType = (WaveformType)r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new ReplaceLFO3AsPanpotCommand { Rate = rate, Depth = depth, WaveformType = waveformType });
							}
							break;
						}
					case 0xED:
						{
							ushort delay = r.ReadUInt16();
							ushort fadeTime = r.ReadUInt16();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetLFO3DelayFade { Delay = delay, FadeTime = fadeTime });
							}
							break;
						}
					case 0xEE:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xEF:
						{
							bool panpotEnabled = r.ReadBoolean();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetLFO3ToPanpotEnabledCommand { PanpotEnabled = panpotEnabled });
							}
							break;
						}
					case 0xF0:
						{
							ushort rate = r.ReadUInt16();
							ushort depth = r.ReadUInt16();
							WaveformType waveformType = (WaveformType)r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new ReplaceLFOCommand { Rate = rate, Depth = depth, WaveformType = waveformType });
							}
							break;
						}
					case 0xF1:
						{
							ushort delay = r.ReadUInt16();
							ushort fadeTime = r.ReadUInt16();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetLFODelayFadeCommand { Delay = delay, FadeTime = fadeTime });
							}
							break;
						}
					case 0xF2:
						{
							ParameterType paramType = (ParameterType)r.ReadByte();
							WaveformType waveformType = (WaveformType)r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetLFOParamCommand { ParamType = paramType, WaveformType = waveformType });
							}
							break;
						}
					case 0xF3:
						{
							byte target = r.ReadByte();
							bool enabled = r.ReadBoolean();
							byte targetID = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SetLFORouteCommand { Target = target, Enabled = enabled, TargetID = targetID });
							}
							break;
						}
					case 0xF4:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xF5:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xF6:
						{
							byte checkpoint = r.ReadByte();
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new ScenarioSyncCommand { Checkpoint = checkpoint });
							}
							break;
						}
					case 0xF7:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xF8:
						{
							byte[] bytes = new byte[2];
							r.ReadBytes(bytes);
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new SkipBytesCommand { Command = cmd, SkippedBytes = bytes });
							}
							break;
						}
					case 0xF9:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xFA:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xFB:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xFC:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xFD:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xFE:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					case 0xFF:
						{
							if (!EventExists(trackIndex, cmdOffset))
							{
								AddEvent(trackIndex, cmdOffset, new InvalidCommand { Command = cmd });
							}
							break;
						}
					default: throw new DSEInvalidCMDException(trackIndex, (int)cmdOffset, cmd);
				}
			}
		}
	}

	public void SetTicks()
	{
		MaxTicks = 0;
		for (int trackIndex = 0; trackIndex < Events.Length; trackIndex++)
		{
			List<SongEvent> evs = Events[trackIndex];
			evs.Sort((e1, e2) => e1.Offset.CompareTo(e2.Offset));

			DSETrack track = Tracks[trackIndex];
			track.Init();

			long elapsedTicks = 0;
			while (true)
			{
				SongEvent e = evs.Single(ev => ev.Offset == track.CurOffset);
				if (e.Ticks.Count > 0)
				{
					break;
				}

				e.Ticks.Add(elapsedTicks);
				ExecuteNext(track);
				if (track.Stopped)
				{
					break;
				}

				elapsedTicks += track.Rest;
				track.Rest = 0;
			}
			if (elapsedTicks > MaxTicks)
			{
				LongestTrack = trackIndex;
				MaxTicks = elapsedTicks;
			}
			track.StopAllChannels();
		}
	}
	internal void SetCurTick(long ticks)
	{
		while (true)
		{
			if (_player.ElapsedTicks == ticks)
			{
				goto finish;
			}

			switch (Header.Type)
			{
				case "smdl":
					{
						while (_player.TempoStack >= 240)
						{
							_player.TempoStack -= 240;
							for (int trackIndex = 0; trackIndex < Tracks.Length; trackIndex++)
							{
								DSETrack track = Tracks[trackIndex];
								if (!track.Stopped)
								{
									track.Tick();
									while (track.Rest == 0 && !track.Stopped)
									{
										ExecuteNext(track);
									}
								}
							}
							_player.ElapsedTicks++;
							if (_player.ElapsedTicks == ticks)
							{
								goto finish;
							}
						}
						break;
					}
				case "smdb":
					{
						while (_player.TempoStack >= 120)
						{
							_player.TempoStack -= 120;
							for (int trackIndex = 0; trackIndex < Tracks.Length; trackIndex++)
							{
								DSETrack track = Tracks[trackIndex];
								if (!track.Stopped)
								{
									track.Tick();
									while (track.Rest == 0 && !track.Stopped)
									{
										ExecuteNext(track);
									}
								}
							}
							_player.ElapsedTicks++;
							if (_player.ElapsedTicks == ticks)
							{
								goto finish;
							}
						}
						break;
					}
			}
			_player.TempoStack += _player.Tempo;
		}
	finish:
		for (int i = 0; i < Tracks.Length; i++)
		{
			Tracks[i].StopAllChannels();
		}
	}
}
