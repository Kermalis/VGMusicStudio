using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using Kermalis.MIDI;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal sealed partial class MP2KLoadedSong
{
	private void TryPlayNote(MP2KTrack track, byte note, byte velocity, int addedDuration)
	{
		int n = note + track.Transpose;
		if (n < 0)
		{
			n = 0;
		}
		else if (n > 0x7F)
		{
			n = 0x7F;
		}
		note = (byte)n;
		track.PrevNote = note;
		track.PrevVelocity = velocity;
		// Tracks do not play unless they have had a voice change event
		if (track.Ready)
		{
			PlayNote(track, note, velocity, addedDuration);

			track.UpdateVolume = true;
			track.UpdatePitch = true;
		}
	}
	// private void PlayNote(byte[] rom, MP2KTrack track, byte cmd)
	private void PlayNote(MP2KTrack track, byte note, byte velocity, int addedDuration)
	{
		// foreach ((byte command, byte len) in MP2KUtils.NoteLUT)
		// {
		// 	if (cmd == command)
		// 	{
		// 		track.PrevLength = len;
		// 		break;
		// 	}
		// }

		// if (rom[track.Position] < 0x80)
		// {
		// 	track.PrevNote = rom[track.Position++];

		// 	if (rom[track.Position] < 0x80)
		// 	{
		// 		track.PrevVelocity = rom[track.Position++];

		// 		if (rom[track.Position] < 0x80)
		// 		{
		// 			track.PrevLength += rom[track.Position++];
		// 		}
		// 	}
		// }
		bool fromDrum = false;
		// int offset = _soundBankOffset + (track.Voice * 12);
		IVoice[] voices = Voicegroup.Voices;
		IVoice[] sub;
		WrappedVoice wv = (WrappedVoice)voices[track.Voice];
		byte rootNote;
		sbyte rythmPan = 0;
		while (true)
		{
			// var v = new VoiceEntry(rom.AsSpan(offset));
			var v = wv.VoiceEntry;
			if (v.Type == (int)VoiceFlags.KeySplit)
			{
				fromDrum = false; // In case there is a multi within a drum
								  // byte inst = rom[v.Int8 - GBAUtils.CARTRIDGE_OFFSET + note];
				sub = wv.Table!.Voices;
				int inst = wv.Keys![note];
				// offset = v.Int4 - GBAUtils.CARTRIDGE_OFFSET + (inst * 12);
				wv = (WrappedVoice)sub[inst];
				rootNote = track.PrevNote;
				if (wv.VoiceEntry.Type is (int)VoiceFlags.KeySplit or (int)VoiceFlags.Drum)
				{
					break;
				}
			}
			else if (v.Type == (int)VoiceFlags.Drum)
			{
				fromDrum = true;
				// offset = v.Int4 - GBAUtils.CARTRIDGE_OFFSET + (note * 12);
				sub = wv.Table!.Voices;
				wv = (WrappedVoice)sub[note];
				if ((v.PanSweep & 0x80) != 0)
				{
					rythmPan = (sbyte)((v.PanSweep - 0xC0) * 2);
				}
				rootNote = v.RootNote;
				if (wv.VoiceEntry.Type is (int)VoiceFlags.KeySplit or (int)VoiceFlags.Drum)
				{
					break;
				}
			}
			else
			{
				rootNote = v.RootNote;
				track.LFODelayCount = track.LFODelay;
				if (track.LFODelay != 0)
				{
					track.ResetLFOValue();
				}

				var ni = new NoteInfo
				{
					// Duration = track.RunCmd == 0xCF ? -1 : (MP2KUtils.RestTable[track.RunCmd - 0xCF] + addedDuration),
					Duration = addedDuration,
					// Duration = track.PrevLength,
					Note = fromDrum ? v.RootNote : note,
					OriginalNote = note,
					Velocity = velocity,
					Priority = track.Priority,
					RhythmPan = rythmPan,
					PseudoEchoVolume = track.PseudoEchoVolume,
					PseudoEchoLength = track.PseudoEchoLength,
					TrackIndex = track.Index,
					PSGLength = v.TimeLength,
				};
				// static int OrderChannels(MP2KChannel c)
				// {
				// 	return c.Track is null ? 0xFF : c.Track.Index;
				// }
				// int instPan = (v.PanSweep & 0x80) != 0 ? v.PanSweep - 0xC0 : 0;
				var type = (VoiceType)(v.Type & 0x7);
				switch (type)
				{
					case VoiceType.PCM8:
						{
							// SampleInfo sInfo = new(rom, v.Int4 - GBAUtils.CARTRIDGE_OFFSET);
							MP2KSample sInfo = wv.Sample!;

							bool bFixed = (v.Type & (int)VoiceFlags.Fixed) != 0;
							// bool bCompressed = _player.Config.HasPokemonCompression && ((v.Type & (int)VoiceFlags.Compressed) != 0);
							// _player.MMixer.AllocPCM8Channel(track, v.ADSR, ni,
							// 		track.GetVolume(), track.GetPanpot(), v.PanSweep, track.GetPitch(),
							// 		bFixed, bCompressed, v.Int4 - GBAUtils.CARTRIDGE_OFFSET);

							MP2KPCM8Channel nChn = new(_player.MContext, _player.MMixer, track, sInfo, v.ADSR, ni, bFixed);

							if (_player.MContext.PCM8Channels.Count <= track.Index)
							{
								_player.MContext.PCM8Channels.Add(nChn);
							}
							else if (_player.MContext.PCM8Channels[track.Index].Track.Index != track.Index || _player.MContext.PCM8Channels[track.Index] is null)
							{
								_player.MContext.PCM8Channels.Insert(track.Index, nChn);
							}
							else
							{
								_player.MContext.PCM8Channels[track.Index] = nChn;
							}
							return;
						}
					case VoiceType.Square1:
						{
							MP2KSquareChannel nChn = new(_player.MContext, _player.MMixer, track, v.Int4, v.ADSR, ni, v.PanSweep);
							if (nChn.State < EnvelopeState.Releasing && nChn.Track!.Index < track.Index)
							{
								return;
							}
							if (_player.MContext.Square1Channels.Count <= track.Index)
							{
								_player.MContext.Square1Channels.Add(nChn);
							}
							else if (_player.MContext.Square1Channels[track.Index].Track.Index != track.Index || _player.MContext.Square1Channels[track.Index] is null)
							{
								_player.MContext.Square1Channels.Insert(track.Index, nChn);
							}
							else
							{
								_player.MContext.Square1Channels[track.Index] = nChn;
							}
							return;
						}
					case VoiceType.Square2:
						{
							// _player.MMixer.AllocPSGChannel(track, v.ADSR, ni,
							// 		track.GetVolume(), track.GetPanpot(), v.PanSweep, track.GetPitch(),
							// 		type, (SquarePattern)v.Int4);
							MP2KSquareChannel nChn = new(_player.MContext, _player.MMixer, track, v.Int4, v.ADSR, ni, 0);
							if (nChn.State < EnvelopeState.Releasing && nChn.Track!.Index < track.Index)
							{
								return;
							}
							if (_player.MContext.Square2Channels.Count <= track.Index)
							{
								_player.MContext.Square2Channels.Add(nChn);
							}
							else if (_player.MContext.Square2Channels[track.Index].Track.Index != track.Index || _player.MContext.Square2Channels[track.Index] is null)
							{
								_player.MContext.Square2Channels.Insert(track.Index, nChn);
							}
							else
							{
								_player.MContext.Square2Channels[track.Index] = nChn;
							}
							return;
						}
					case VoiceType.PCM4:
						{
							// _player.MMixer.AllocPSGChannel(track, v.ADSR, ni,
							// 		track.GetVolume(), track.GetPanpot(), v.PanSweep, track.GetPitch(),
							// 		type, v.Int4 - GBAUtils.CARTRIDGE_OFFSET);
							MP2KPCM4Channel nChn = new(_player.MContext, _player.MMixer, track, wv.Sample!, v.ADSR, ni, _player.MContext.PlayerSoundMode.AccurateCh3Volume);
							if (nChn.State < EnvelopeState.Releasing && nChn.Track!.Index < track.Index)
							{
								return;
							}
							if (_player.MContext.PCM4Channels.Count <= track.Index)
							{
								_player.MContext.PCM4Channels.Add(nChn);
							}
							else if (_player.MContext.PCM4Channels[track.Index].Track.Index != track.Index || _player.MContext.PCM4Channels[track.Index] is null)
							{
								_player.MContext.PCM4Channels.Insert(track.Index, nChn);
							}
							else
							{
								_player.MContext.PCM4Channels[track.Index] = nChn;
							}
							return;
						}
					case VoiceType.Noise:
						{
							// _player.MMixer.AllocPSGChannel(track, v.ADSR, ni,
							// 		track.GetVolume(), track.GetPanpot(), v.PanSweep, track.GetPitch(),
							// 		type, (NoisePattern)v.Int4);
							MP2KNoiseChannel nChn = new(_player.MContext, _player.MMixer, track, v.Int4, v.ADSR, ni);
							if (nChn.State < EnvelopeState.Releasing && nChn.Track!.Index < track.Index)
							{
								return;
							}
							if (_player.MContext.NoiseChannels.Count <= track.Index)
							{
								_player.MContext.NoiseChannels.Add(nChn);
							}
							else if (_player.MContext.NoiseChannels[track.Index].Track.Index != track.Index || _player.MContext.NoiseChannels[track.Index] is null)
							{
								_player.MContext.NoiseChannels.Insert(track.Index, nChn);
							}
							else
							{
								_player.MContext.NoiseChannels[track.Index] = nChn;
							}
							return;
						}
				}
				return; // Prevent infinite loop with invalid instruments
			}
		}
	}
	public void ExecuteNext(MP2KTrack track, ref bool update)
	{
		List<SongEvent> events = Events[track.Index];
		SongEvent evt = events[track.EventPosition];
		ICommand cmd = evt.Command;
		track.ROMOffset = evt.Offset;
		track.ROMOffset++;

		// byte[] rom = _player.Config.ROM;
		// byte cmd = rom[track.ROMOffset++];
		// byte cmd = rom[track.Position];
		if ((cmd is ISequenceCommand run && run.SequenceType >= SequenceCommand.Voice) || cmd is NoteCommand) // Commands that work within running status
		{
			track.RunCmd = cmd;
		}
		// if (cmd < 0x80)
		// {
		// 	cmd = track.RunCmd;
		// 	if (cmd < 0x80)
		// 	{
		// 		FineCommand(track);
		// 		return;
		// 	}
		// }
		// else
		// {
		// 	track.Position++;
		// 	if (cmd >= 0xBD) // Commands that work within running status
		// 	{
		// 		track.RunCmd = cmd;
		// 	}
		// }

		// if (track.RunCmd is NoteCommand nc && cmd <= 0x7F) // Within running status
		// {
		// 	byte peek0 = rom[track.ROMOffset];
		// 	byte peek1 = rom[track.ROMOffset + 1];
		// 	byte velocity, addedDuration;
		// 	if (peek0 > 0x7F)
		// 	{
		// 		velocity = track.PrevVelocity;
		// 		addedDuration = 0;
		// 	}
		// 	else if (peek1 > 3)
		// 	{
		// 		track.ROMOffset++;
		// 		velocity = peek0;
		// 		addedDuration = 0;
		// 	}
		// 	else
		// 	{
		// 		track.ROMOffset += 2;
		// 		velocity = peek0;
		// 		addedDuration = peek1;
		// 	}
		// 	TryPlayNote(track, cmd, velocity, addedDuration);
		// }
		if (cmd is NoteCommand nc)
		{
			// byte peek0 = rom[track.ROMOffset];
			// byte peek1 = rom[track.ROMOffset + 1];
			// byte peek2 = rom[track.ROMOffset + 2];
			// byte key, velocity, addedDuration;
			if (nc.Note > 0x7F)
			{
				// key = track.PrevNote;
				// velocity = track.PrevVelocity;
				// addedDuration = 0;
			}
			else if (nc.Velocity > 0x7F)
			{
				track.ROMOffset++;
				// key = nc.Note;
				// velocity = track.PrevVelocity;
				// addedDuration = 0;
			}
			else if (nc.Note == 0xCF || nc.Duration > 3)
			{
				track.ROMOffset += 2;
				// key = nc.Note;
				// velocity = nc.Velocity;
				// addedDuration = 0;
			}
			else
			{
				if (nc.IsRepeated)
				{
					track.ROMOffset += 2;
				}
				else
				{
					track.ROMOffset += 3;
				}
				// key = nc.Note;
				// velocity = nc.Velocity;
				// addedDuration = (byte)nc.Duration;
			}
			TryPlayNote(track, nc.Note, nc.Velocity, nc.Duration);
			track.EventPosition++;
			// PlayNote(rom, track, cmd);
		}
		else if (cmd is RestCommand rc)
		{
			track.Rest = rc.Rest;
			track.EventPosition++;
			// foreach ((byte command, byte len) in MP2KUtils.DelayLUT)
			// {
			// 	if (cmd == command)
			// 	{
			// 		track.Rest = len;
			// 	}
			// }
		}
		// else if (track.RunCmd is IRunCommand && cmd <= 0x7F)
		// {
		// 	ExecuteCommand(track, (IRunCommand)cmd, ref update);
		// }
		else if (cmd is ISequenceCommand irc && irc.SequenceType > (SequenceCommand)0xB0 && irc.SequenceType < (SequenceCommand)0xCF)
		{
			ExecuteCommand(track, irc, ref update);
		}

		evt = events[track.EventPosition];
		track.ROMOffset = evt.Offset;

		if (!track.Ready)
		{
			return;
		}

		track.Pitch = track.GetPitch();

		if (!track.UpdateVolume && !track.UpdatePitch)
		{
			return;
		}

		void SetVolPitch(MP2KChannel chn)
		{
			if (chn is not null)
			{
				if (chn.Track == track)
				{
					if (track.UpdateVolume)
					{
						chn.SetVolume(track.GetVolume(), track.GetPanpot());
					}
					if (track.UpdatePitch)
					{
						chn.SetPitch(track.Pitch);
					}
				}
			}
		}

		foreach (MP2KPCM8Channel chn in _player.MContext.PCM8Channels)
		{
			SetVolPitch(chn);
		}
		foreach (MP2KSquareChannel chn in _player.MContext.Square1Channels)
		{
			SetVolPitch(chn);
		}
		foreach (MP2KSquareChannel chn in _player.MContext.Square2Channels)
		{
			SetVolPitch(chn);
		}
		foreach (MP2KPCM4Channel chn in _player.MContext.PCM4Channels)
		{
			SetVolPitch(chn);
		}
		foreach (MP2KNoiseChannel chn in _player.MContext.NoiseChannels)
		{
			SetVolPitch(chn);
		}

		track.UpdateVolume = false;
		track.UpdatePitch = false;
	}

	private void ExecuteCommand(MP2KTrack track, ISequenceCommand cmd, ref bool update)
	{
		// bool isRunning = track.RunCmd < 0xCF && cmd <= 0x7F;
		// SequenceCommand cmdType = isRunning ? (SequenceCommand)track.RunCmd : (SequenceCommand)cmd;
		switch (cmd)
		{
			case FinishCommand f:
				{
					FineCommand(track);
					break;
				}
			case JumpCommand j:
				{
					track.ROMOffset = j.Pointer.Offset;
					track.EventPosition = j.Pointer.Index;
					break;
				}
			case CallCommand c:
				{
					if (track.CallStackDepth >= track.CallStack.Length)
					{
						FineCommand(track);
						break;
					}
					track.CallStack[0][track.CallStackDepth] = track.ROMOffset + 4;
					track.CallStack[1][track.CallStackDepth++] = track.EventPosition + 1;
					track.ROMOffset = c.Pointer.Offset;
					track.EventPosition = c.Pointer.Index;
					break;
				}
			case ReturnCommand:
				{
					if (track.CallStackDepth is 0)
					{
						track.EventPosition++;
						break;
					}

					track.ROMOffset = track.CallStack[0][--track.CallStackDepth];
					track.EventPosition = (int)track.CallStack[1][track.CallStackDepth];
					break;
				}
			case RepeatCommand r:
				{
					// byte count = rom[track.ROMOffset++];
					track.ROMOffset++;
					if (r.Times is 0)
					{
						FineCommand(track);
					}
					if (++track.RepeatTimes < r.Times)
					{
						track.ROMOffset = r.Pointer.Offset;
						track.EventPosition = r.Pointer.Index;
					}
					else
					{
						track.RepeatTimes = 0;
						track.ROMOffset += 4;
						track.EventPosition++;
					}
					break;
				}
			case MemoryAccessCommand ma:
				{
					MemoryAccessCommand(track, ma);
					break;
				}
			case PriorityCommand p:
				{
					track.Priority = p.Priority;
					track.ROMOffset++;
					track.EventPosition++;
					break;
				}
			case TempoCommand t:
				{
					_player.Tempo = t.Tempo;
					track.ROMOffset++;
					track.EventPosition++;
					break;
				}
			case TransposeCommand k:
				{
					track.Transpose = k.Transpose;
					track.ROMOffset++;
					track.EventPosition++;
					break;
				}
			// Commands that work within running status:
			case VoiceCommand voi:
				{
					if (voi.IsRepeated)
					{
						track.Voice = voi.Voice;
					}
					else
					{
						track.ROMOffset++;
						track.Voice = voi.Voice;
						track.Ready = true; // To indicate that it's ready to be used in running status
					}
					track.EventPosition++;
					break;
				}
			case VolumeCommand vol:
				{
					track.Volume = vol.Volume;
					if (!vol.IsRepeated)
					{
						track.ROMOffset++;
					}
					track.UpdateVolume = true;
					update = true;
					track.EventPosition++;
					break;
				}
			case PanpotCommand pan:
				{
					track.Panpot = pan.Panpot;
					if (!pan.IsRepeated)
					{
						track.ROMOffset++;
					}
					track.UpdateVolume = true;
					update = true;
					track.EventPosition++;
					break;
				}
			case PitchBendCommand bend:
				{
					track.PitchBend = bend.Bend;
					if (!bend.IsRepeated)
					{
						track.ROMOffset++;
					}
					track.UpdatePitch = true;
					update = true;
					track.EventPosition++;
					break;
				}
			case PitchBendRangeCommand bendr:
				{
					track.PitchBendRange = bendr.Range;
					if (!bendr.IsRepeated)
					{
						track.ROMOffset++;
					}
					track.UpdatePitch = true;
					update = true;
					track.EventPosition++;
					break;
				}
			case LFOSpeedCommand lfos:
				{
					track.LFOSpeed = lfos.Speed;
					if (!lfos.IsRepeated)
					{
						track.ROMOffset++;
					}
					if (track.LFOSpeed is 0)
					{
						track.ResetLFOValue();
					}
					track.LFODelayCount = 0;
					update = true;
					track.EventPosition++;
					break;
				}
			case LFODelayCommand lfodl:
				{
					track.LFODelay = lfodl.Delay;
					if (!lfodl.IsRepeated)
					{
						track.ROMOffset++;
					}
					track.LFOPhase = 0;
					track.LFODelayCount = 0;
					update = true;
					track.EventPosition++;
					break;
				}
			case LFODepthCommand mod:
				{
					track.LFODepth = mod.Depth;
					if (!mod.IsRepeated)
					{
						track.ROMOffset++;
					}
					update = true;
					if (track.LFODepth is 0)
					{
						track.ResetLFOValue();
					}
					track.EventPosition++;
					break;
				}
			case LFOTypeCommand modt:
				{
					if (!modt.IsRepeated)
					{
						track.ROMOffset++;
					}
					if (modt.Type == track.LFOType)
					{
						return;
					}
					track.LFOType = modt.Type;
					track.UpdateVolume = true;
					track.UpdatePitch = true;
					update = true;
					track.EventPosition++;
					break;
				}
			case TuneCommand tune:
				{
					track.Tune = tune.Tune;
					if (!tune.IsRepeated)
					{
						track.ROMOffset++;
					}
					track.UpdatePitch = true;
					update = true;
					track.EventPosition++;
					break;
				}
			case LibraryCommand xcmd:
				{
					ExtendedCommand(track, xcmd);
					break;
				}
			case EndOfTieCommand eot:
				{
					byte key;
					if (eot.IsRepeated)
					{
						track.PrevNote = (byte)eot.Note;
						key = (byte)eot.Note;
					}
					else
					{
						key = (byte)eot.Note;
						if (key >= 0x80)
						{
							key = track.PrevNote;
						}
						else
						{
							track.ROMOffset++;
							track.PrevNote = key;
						}
					}
					foreach (MP2KChannel channel in track.Channels)
					{
						if (channel.State == EnvelopeState.Dead)
						{
							continue;
						}
						if (channel.IsReleasing())
						{
							continue;
						}
						if (channel.Note.Note == key)
						{
							channel.Release();
							break;
						}
					}
					track.EventPosition++;
					break;
				}
			default:
				{
					FineCommand(track);
					break;
				}
		}
	}

	internal static void FineCommand(MP2KTrack track)
	{
		for (int i = 0; i < track.Channels.Count; i++)
		{
			track.Channels[i].Release();
			track.Channels[i].RemoveFromTrack();
		}

		// if (track.Channels.Count > 0)
		// {
		// 	track.Channels.Clear();
		// }

		track.Stopped = true;
		// track.ActiveNotes.AsSpan().Clear();
		// track.ActiveVoiceTypes = VoiceConfigType.None;
	}

	internal void MemoryAccessCommand(MP2KTrack track, MemoryAccessCommand cmd)
	{
		track.ROMOffset++;
		ref byte memory = ref _player.MContext.MemAccArea.Span[cmd.MemoryAreaAddress];

		switch (cmd.Operator)
		{
			case MemoryOperatorType.MemSet:
				{
					memory = cmd.Data;
					break;
				}
			case MemoryOperatorType.MemAdd:
				{
					memory += cmd.Data;
					break;
				}
			case MemoryOperatorType.MemSub:
				{
					memory -= cmd.Data;
					break;
				}
			case MemoryOperatorType.MemMemSet:
				{
					memory = _player.MContext.MemAccArea.Span[cmd.Data];
					break;
				}
			case MemoryOperatorType.MemMemAdd:
				{
					memory += _player.MContext.MemAccArea.Span[cmd.Data];
					break;
				}
			case MemoryOperatorType.MemMemSub:
				{
					memory -= _player.MContext.MemAccArea.Span[cmd.Data];
					break;
				}
			case MemoryOperatorType.MemBEq:
				{
					if (memory == cmd.Data)
					{
						track.ROMOffset = cmd.Pointer.Offset;
						track.EventPosition = cmd.Pointer.Index;
						return;
					}
					break;
				}
			case MemoryOperatorType.MemBNEq:
				{
					if (memory != cmd.Data)
					{
						track.ROMOffset = cmd.Pointer.Offset;
						track.EventPosition = cmd.Pointer.Index;
						return;
					}
					break;
				}
			case MemoryOperatorType.MemBHi:
				{
					if (memory > cmd.Data)
					{
						track.ROMOffset = cmd.Pointer.Offset;
						track.EventPosition = cmd.Pointer.Index;
						return;
					}
					break;
				}
			case MemoryOperatorType.MemBHS:
				{
					if (memory >= cmd.Data)
					{
						track.ROMOffset = cmd.Pointer.Offset;
						track.EventPosition = cmd.Pointer.Index;
						return;
					}
					break;
				}
			case MemoryOperatorType.MemBLS:
				{
					if (memory <= cmd.Data)
					{
						track.ROMOffset = cmd.Pointer.Offset;
						track.EventPosition = cmd.Pointer.Index;
						return;
					}
					break;
				}
			case MemoryOperatorType.MemBLo:
				{
					if (memory < cmd.Data)
					{
						track.ROMOffset = cmd.Pointer.Offset;
						track.EventPosition = cmd.Pointer.Index;
						return;
					}
					break;
				}
			case MemoryOperatorType.MemMemBEq:
				{
					if (memory == _player.MContext.MemAccArea.Span[cmd.Data])
					{
						track.ROMOffset = cmd.Pointer.Offset;
						track.EventPosition = cmd.Pointer.Index;
						return;
					}
					break;
				}
			case MemoryOperatorType.MemMemBNEq:
				{
					if (memory != _player.MContext.MemAccArea.Span[cmd.Data])
					{
						track.ROMOffset = cmd.Pointer.Offset;
						track.EventPosition = cmd.Pointer.Index;
						return;
					}
					break;
				}
			case MemoryOperatorType.MemMemBHi:
				{
					if (memory > _player.MContext.MemAccArea.Span[cmd.Data])
					{
						track.ROMOffset = cmd.Pointer.Offset;
						track.EventPosition = cmd.Pointer.Index;
						return;
					}
					break;
				}
			case MemoryOperatorType.MemMemBHS:
				{
					if (memory >= _player.MContext.MemAccArea.Span[cmd.Data])
					{
						track.ROMOffset = cmd.Pointer.Offset;
						track.EventPosition = cmd.Pointer.Index;
						return;
					}
					break;
				}
			case MemoryOperatorType.MemMemBLS:
				{
					if (memory <= _player.MContext.MemAccArea.Span[cmd.Data])
					{
						track.ROMOffset = cmd.Pointer.Offset;
						track.EventPosition = cmd.Pointer.Index;
						return;
					}
					break;
				}
			case MemoryOperatorType.MemMemBLo:
				{
					if (memory < _player.MContext.MemAccArea.Span[cmd.Data])
					{
						track.ROMOffset = cmd.Pointer.Offset;
						track.EventPosition = cmd.Pointer.Index;
						return;
					}
					break;
				}
			default:
				{
					break;
				}
		}

		track.EventPosition++;
	}

	internal static void ExtendedCommand(MP2KTrack track, LibraryCommand xCmd)
	{
		track.ROMOffset++;
		switch (xCmd.LibraryCommandType)
		{
			case ExtendedCommandType.xWAVE:
				{
					track.ROMOffset += 4;
					track.EventPosition++;
					break;
				}
			case ExtendedCommandType.xTYPE:
				{
					track.ROMOffset++;
					track.EventPosition++;
					break;
				}
			case ExtendedCommandType.xATTA:
				{
					track.ROMOffset++;
					track.EventPosition++;
					break;
				}
			case ExtendedCommandType.xDECA:
				{
					track.ROMOffset++;
					track.EventPosition++;
					break;
				}
			case ExtendedCommandType.xSUST:
				{
					track.ROMOffset++;
					track.EventPosition++;
					break;
				}
			case ExtendedCommandType.xRELA:
				{
					track.ROMOffset++;
					track.EventPosition++;
					break;
				}
			case ExtendedCommandType.xIECV:
				{
					if (!xCmd.IsRepeated)
					{
						track.PseudoEchoVolume = xCmd.Argument;
					}
					track.ROMOffset++;
					track.EventPosition++;
					break;
				}
			case ExtendedCommandType.xIECL:
				{
					if (!xCmd.IsRepeated)
					{
						track.PseudoEchoLength = xCmd.Argument;
					}
					track.ROMOffset++;
					track.EventPosition++;
					break;
				}
			case ExtendedCommandType.xLENG:
				{
					track.ROMOffset++;
					track.EventPosition++;
					break;
				}
			case ExtendedCommandType.xSWEE:
				{
					track.ROMOffset++;
					track.EventPosition++;
					break;
				}
			case ExtendedCommandType.xWAIT:
				{
					track.Rest = xCmd.Argument;
					track.ROMOffset += 2;
					track.EventPosition++;
					break;
				}
			case ExtendedCommandType.xSOFF:
				{
					track.ROMOffset += 4;
					track.EventPosition++;
					break;
				}
			default:
				{
					FineCommand(track);
					break;
				}
		}
	}

	public void UpdateInstrumentCache(byte voice, out string str)
	{
		byte t = _player.Config.ROM[_voicegroupOffset + (voice * 12)];
		if (t == (byte)VoiceFlags.KeySplit)
		{
			str = "Key Split";
		}
		else if (t == (byte)VoiceFlags.Drum)
		{
			str = "Drum";
		}
		else
		{
			switch ((VoiceType)(t & 0x7)) // Disregard the other flags
			{
				case VoiceType.PCM8: str = "PCM8"; break;
				case VoiceType.Square1: str = "Square 1"; break;
				case VoiceType.Square2: str = "Square 2"; break;
				case VoiceType.PCM4: str = "PCM4"; break;
				case VoiceType.Noise: str = "Noise"; break;
				case VoiceType.Invalid5: str = "Invalid 5"; break;
				case VoiceType.Invalid6: str = "Invalid 6"; break;
				default: str = "Invalid 7"; break; // VoiceType.Invalid7
			}
		}
	}
	public void UpdateSongState(SongState info, string?[] voiceTypeCache)
	{
		for (int trackIndex = 0; trackIndex < Tracks.Length; trackIndex++)
		{
			Tracks[trackIndex].UpdateSongState(info.Tracks[trackIndex], this, voiceTypeCache);
		}
	}
}
