using System;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal sealed partial class MP2KLoadedSong
{
	private void TryPlayNote(MP2KTrack track, byte note, byte velocity, byte addedDuration)
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
			PlayNote(_player.Config.ROM, track, note, velocity, addedDuration);
		}
	}
	private void PlayNote(byte[] rom, MP2KTrack track, byte note, byte velocity, byte addedDuration)
	{
		bool fromDrum = false;
		int offset = _voiceTableOffset + (track.Voice * 12);
		while (true)
		{
			var v = new VoiceEntry(rom.AsSpan(offset));
			if (v.Type == (int)VoiceFlags.KeySplit)
			{
				fromDrum = false; // In case there is a multi within a drum
				byte inst = rom[v.Int8 - GBAUtils.CARTRIDGE_OFFSET + note];
				offset = v.Int4 - GBAUtils.CARTRIDGE_OFFSET + (inst * 12);
			}
			else if (v.Type == (int)VoiceFlags.Drum)
			{
				fromDrum = true;
				offset = v.Int4 - GBAUtils.CARTRIDGE_OFFSET + (note * 12);
			}
			else
			{
				var ni = new NoteInfo
				{
					Duration = track.RunCmd == 0xCF ? -1 : (MP2KUtils.RestTable[track.RunCmd - 0xCF] + addedDuration),
					Velocity = velocity,
					OriginalNote = note,
					Note = fromDrum ? v.RootNote : note,
				};
				var type = (VoiceType)(v.Type & 0x7);
				int instPan = v.Pan;
				instPan = (instPan & 0x80) != 0 ? instPan - 0xC0 : 0;
				switch (type)
				{
					case VoiceType.PCM8:
					{
						bool bFixed = (v.Type & (int)VoiceFlags.Fixed) != 0;
						bool bCompressed = _player.Config.HasPokemonCompression && ((v.Type & (int)VoiceFlags.Compressed) != 0);
						_player.MMixer.AllocPCM8Channel(track, v.ADSR, ni,
							track.GetVolume(), track.GetPanpot(), instPan, track.GetPitch(),
							bFixed, bCompressed, v.Int4 - GBAUtils.CARTRIDGE_OFFSET);
						return;
					}
					case VoiceType.Square1:
					case VoiceType.Square2:
					{
						_player.MMixer.AllocPSGChannel(track, v.ADSR, ni,
							track.GetVolume(), track.GetPanpot(), instPan, track.GetPitch(),
							type, (SquarePattern)v.Int4);
						return;
					}
					case VoiceType.PCM4:
					{
						_player.MMixer.AllocPSGChannel(track, v.ADSR, ni,
							track.GetVolume(), track.GetPanpot(), instPan, track.GetPitch(),
							type, v.Int4 - GBAUtils.CARTRIDGE_OFFSET);
						return;
					}
					case VoiceType.Noise:
					{
						_player.MMixer.AllocPSGChannel(track, v.ADSR, ni,
							track.GetVolume(), track.GetPanpot(), instPan, track.GetPitch(),
							type, (NoisePattern)v.Int4);
						return;
					}
				}
				return; // Prevent infinite loop with invalid instruments
			}
		}
	}
	public void ExecuteNext(MP2KTrack track, ref bool update)
	{
		byte[] rom = _player.Config.ROM;
		byte cmd = rom[track.DataOffset++];
		if (cmd >= 0xBD) // Commands that work within running status
		{
			track.RunCmd = cmd;
		}

		if (track.RunCmd >= 0xCF && cmd <= 0x7F) // Within running status
		{
			byte peek0 = rom[track.DataOffset];
			byte peek1 = rom[track.DataOffset + 1];
			byte velocity, addedDuration;
			if (peek0 > 0x7F)
			{
				velocity = track.PrevVelocity;
				addedDuration = 0;
			}
			else if (peek1 > 3)
			{
				track.DataOffset++;
				velocity = peek0;
				addedDuration = 0;
			}
			else
			{
				track.DataOffset += 2;
				velocity = peek0;
				addedDuration = peek1;
			}
			TryPlayNote(track, cmd, velocity, addedDuration);
		}
		else if (cmd >= 0xCF)
		{
			byte peek0 = rom[track.DataOffset];
			byte peek1 = rom[track.DataOffset + 1];
			byte peek2 = rom[track.DataOffset + 2];
			byte key, velocity, addedDuration;
			if (peek0 > 0x7F)
			{
				key = track.PrevNote;
				velocity = track.PrevVelocity;
				addedDuration = 0;
			}
			else if (peek1 > 0x7F)
			{
				track.DataOffset++;
				key = peek0;
				velocity = track.PrevVelocity;
				addedDuration = 0;
			}
			else if (cmd == 0xCF || peek2 > 3)
			{
				track.DataOffset += 2;
				key = peek0;
				velocity = peek1;
				addedDuration = 0;
			}
			else
			{
				track.DataOffset += 3;
				key = peek0;
				velocity = peek1;
				addedDuration = peek2;
			}
			TryPlayNote(track, key, velocity, addedDuration);
		}
		else if (cmd >= 0x80 && cmd <= 0xB0)
		{
			track.Rest = MP2KUtils.RestTable[cmd - 0x80];
		}
		else if (track.RunCmd < 0xCF && cmd <= 0x7F)
		{
			switch (track.RunCmd)
			{
				case 0xBD:
				{
					track.Voice = cmd;
					//track.Ready = true; // This is unnecessary because if we're in running status of a voice command, then Ready was already set
					break;
				}
				case 0xBE:
				{
					track.Volume = cmd;
					update = true;
					break;
				}
				case 0xBF:
				{
					track.Panpot = (sbyte)(cmd - 0x40);
					update = true;
					break;
				}
				case 0xC0:
				{
					track.PitchBend = (sbyte)(cmd - 0x40);
					update = true;
					break;
				}
				case 0xC1:
				{
					track.PitchBendRange = cmd;
					update = true;
					break;
				}
				case 0xC2:
				{
					track.LFOSpeed = cmd;
					track.LFOPhase = 0;
					track.LFODelayCount = 0;
					update = true;
					break;
				}
				case 0xC3:
				{
					track.LFODelay = cmd;
					track.LFOPhase = 0;
					track.LFODelayCount = 0;
					update = true;
					break;
				}
				case 0xC4:
				{
					track.LFODepth = cmd;
					update = true;
					break;
				}
				case 0xC5:
				{
					track.LFOType = (LFOType)cmd;
					update = true;
					break;
				}
				case 0xC8:
				{
					track.Tune = (sbyte)(cmd - 0x40);
					update = true;
					break;
				}
				case 0xCD:
				{
					track.DataOffset++;
					break;
				}
				case 0xCE:
				{
					track.PrevNote = cmd;
					int k = cmd + track.Transpose;
					if (k < 0)
					{
						k = 0;
					}
					else if (k > 0x7F)
					{
						k = 0x7F;
					}
					track.ReleaseChannels(k);
					break;
				}
				default: throw new MP2KInvalidRunningStatusCMDException(track.Index, track.DataOffset - 1, track.RunCmd);
			}
		}
		else if (cmd > 0xB0 && cmd < 0xCF)
		{
			switch (cmd)
			{
				case 0xB1:
				case 0xB6:
				{
					track.Stopped = true;
					//track.ReleaseAllTieingChannels(); // Necessary?
					break;
				}
				case 0xB2:
				{
					track.DataOffset = (rom[track.DataOffset++] | (rom[track.DataOffset++] << 8) | (rom[track.DataOffset++] << 16) | (rom[track.DataOffset++] << 24)) - GBAUtils.CARTRIDGE_OFFSET;
					break;
				}
				case 0xB3:
				{
					if (track.CallStackDepth >= 3)
					{
						throw new MP2KTooManyNestedCallsException(track.Index);
					}

					int callOffset = (rom[track.DataOffset++] | (rom[track.DataOffset++] << 8) | (rom[track.DataOffset++] << 16) | (rom[track.DataOffset++] << 24)) - GBAUtils.CARTRIDGE_OFFSET;
					track.CallStack[track.CallStackDepth] = track.DataOffset;
					track.CallStackDepth++;
					track.DataOffset = callOffset;
					break;
				}
				case 0xB4:
				{
					if (track.CallStackDepth != 0)
					{
						track.CallStackDepth--;
						track.DataOffset = track.CallStack[track.CallStackDepth];
					}
					break;
				}
				/*case 0xB5: // TODO: Logic so this isn't an infinite loop
				{
					byte times = config.Reader.ReadByte();
					int repeatOffset = config.Reader.ReadInt32() - GBA.Utils.CartridgeOffset;
					if (!EventExists(offset))
					{
						AddEvent(new RepeatCommand { Times = times, Offset = repeatOffset });
					}
					break;
				}*/
				case 0xB9:
				{
					track.DataOffset += 3;
					break;
				}
				case 0xBA:
				{
					track.Priority = rom[track.DataOffset++];
					break;
				}
				case 0xBB:
				{
					_player.Tempo = (ushort)(rom[track.DataOffset++] * 2);
					break;
				}
				case 0xBC:
				{
					track.Transpose = (sbyte)rom[track.DataOffset++];
					break;
				}
				// Commands that work within running status:
				case 0xBD:
				{
					track.Voice = rom[track.DataOffset++];
					track.Ready = true;
					break;
				}
				case 0xBE:
				{
					track.Volume = rom[track.DataOffset++];
					update = true;
					break;
				}
				case 0xBF:
				{
					track.Panpot = (sbyte)(rom[track.DataOffset++] - 0x40);
					update = true;
					break;
				}
				case 0xC0:
				{
					track.PitchBend = (sbyte)(rom[track.DataOffset++] - 0x40);
					update = true;
					break;
				}
				case 0xC1:
				{
					track.PitchBendRange = rom[track.DataOffset++];
					update = true;
					break;
				}
				case 0xC2:
				{
					track.LFOSpeed = rom[track.DataOffset++];
					track.LFOPhase = 0;
					track.LFODelayCount = 0;
					update = true;
					break;
				}
				case 0xC3:
				{
					track.LFODelay = rom[track.DataOffset++];
					track.LFOPhase = 0;
					track.LFODelayCount = 0;
					update = true;
					break;
				}
				case 0xC4:
				{
					track.LFODepth = rom[track.DataOffset++];
					update = true;
					break;
				}
				case 0xC5:
				{
					track.LFOType = (LFOType)rom[track.DataOffset++];
					update = true;
					break;
				}
				case 0xC8:
				{
					track.Tune = (sbyte)(rom[track.DataOffset++] - 0x40);
					update = true;
					break;
				}
				case 0xCD:
				{
					track.DataOffset += 2;
					break;
				}
				case 0xCE:
				{
					byte peek = rom[track.DataOffset];
					if (peek > 0x7F)
					{
						track.ReleaseChannels(track.PrevNote);
					}
					else
					{
						track.DataOffset++;
						track.PrevNote = peek;
						int k = peek + track.Transpose;
						if (k < 0)
						{
							k = 0;
						}
						else if (k > 0x7F)
						{
							k = 0x7F;
						}
						track.ReleaseChannels(k);
					}
					break;
				}
				default: throw new MP2KInvalidCMDException(track.Index, track.DataOffset - 1, cmd);
			}
		}
	}

	public void UpdateInstrumentCache(byte voice, out string str)
	{
		byte t = _player.Config.ROM[_voiceTableOffset + (voice * 12)];
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
