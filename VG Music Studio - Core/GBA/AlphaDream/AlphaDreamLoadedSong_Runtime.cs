using System;
using static System.Buffers.Binary.BinaryPrimitives;

namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

internal sealed partial class AlphaDreamLoadedSong
{
	private static bool TryGetVoiceEntry(byte[] rom, int voiceTableOffset, byte voice, byte key, out VoiceEntry e)
	{
		short voiceOffset = ReadInt16LittleEndian(rom.AsSpan(voiceTableOffset + (voice * 2)));
		short nextVoiceOffset = ReadInt16LittleEndian(rom.AsSpan(voiceTableOffset + ((voice + 1) * 2)));
		if (voiceOffset == nextVoiceOffset)
		{
			e = default;
			return false;
		}

		int pos = voiceTableOffset + voiceOffset; // Prevent object creation in the last iteration
		ref readonly var refE = ref VoiceEntry.Get(rom.AsSpan(pos));
		while (refE.MinKey > key || refE.MaxKey < key)
		{
			pos += 8;
			if (pos == nextVoiceOffset)
			{
				e = default;
				return false;
			}
			refE = ref VoiceEntry.Get(rom.AsSpan(pos));
		}
		e = refE;
		return true;
	}
	private void PlayNote(AlphaDreamTrack track, byte key, byte duration)
	{
		AlphaDreamConfig cfg = _player.Config;
		if (!TryGetVoiceEntry(cfg.ROM, cfg.VoiceTableOffset, track.Voice, key, out VoiceEntry entry))
		{
			return;
		}

		track.NoteDuration = duration;
		if (track.Index >= 8)
		{
			// TODO: "Sample" byte in VoiceEntry
			var sqr = (AlphaDreamSquareChannel)track.Channel;
			sqr.Init(key, new ADSR { A = 0xFF, D = 0x00, S = 0xFF, R = 0x00 }, track.Volume, track.Panpot, track.GetPitch());
		}
		else
		{
			int sto = cfg.SampleTableOffset;
			int sampleOffset = ReadInt32LittleEndian(cfg.ROM.AsSpan(sto + (entry.Sample * 4))); // Some entries are 0. If you play them, are they silent, or does it not care if they are 0?

			var pcm = (AlphaDreamPCMChannel)track.Channel;
			pcm.Init(key, new ADSR { A = 0xFF, D = 0x00, S = 0xFF, R = 0x00 }, sto + sampleOffset, entry.IsFixedFrequency == VoiceEntry.FIXED_FREQ_TRUE);
			pcm.SetVolume(track.Volume, track.Panpot);
			pcm.SetPitch(track.GetPitch());
		}
	}
	public void ExecuteNext(AlphaDreamTrack track, ref bool update)
	{
		byte[] rom = _player.Config.ROM;
		byte cmd = rom[track.DataOffset++];
		switch (cmd)
		{
			case 0x00: // Free Note
			{
				byte note = (byte)(rom[track.DataOffset++] - 0x80);
				if (_player.Config.AudioEngineVersion == AudioEngineVersion.Hamtaro)
				{
					track.Volume = rom[track.DataOffset++];
					update = true;
				}

				byte duration = rom[track.DataOffset++];
				track.Rest += duration;
				if (track.PrevCommand == 0 && track.Channel.Key == note)
				{
					track.NoteDuration += duration;
				}
				else
				{
					PlayNote(track, note, duration);
				}
				break;
			}
			case <= 0xDF: // Note
			{
				byte key = rom[track.DataOffset++];
				if (_player.Config.AudioEngineVersion == AudioEngineVersion.Hamtaro)
				{
					track.Volume = rom[track.DataOffset++];
					update = true;
				}

				track.Rest += cmd;
				if (track.PrevCommand == 0 && track.Channel.Key == key)
				{
					track.NoteDuration += cmd;
				}
				else
				{
					PlayNote(track, key, cmd);
				}
				break;
			}
			case 0xF0: // Voice
			{
				track.Voice = rom[track.DataOffset++];
				break;
			}
			case 0xF1: // Volume
			{
				track.Volume = rom[track.DataOffset++];
				update = true;
				break;
			}
			case 0xF2: // Panpot
			{
				track.Panpot = (sbyte)(rom[track.DataOffset++] - 0x80);
				update = true;
				break;
			}
			case 0xF4: // Pitch Bend Range
			{
				track.PitchBendRange = rom[track.DataOffset++];
				update = true;
				break;
			}
			case 0xF5: // Pitch Bend
			{
				track.PitchBend = (sbyte)rom[track.DataOffset++];
				update = true;
				break;
			}
			case 0xF6: // Rest
			{
				track.Rest = rom[track.DataOffset++];
				break;
			}
			case 0xF8: // Jump
			{
				track.DataOffset += 2 + ReadInt16LittleEndian(rom.AsSpan(track.DataOffset));
				break;
			}
			case 0xF9: // Track Tempo
			{
				_player.Tempo = rom[track.DataOffset++]; // TODO: Implement per track
				break;
			}
			case 0xFF: // Finish
			{
				track.Stopped = true;
				break;
			}
			default: throw new AlphaDreamInvalidCMDException(track.Index, track.DataOffset - 1, cmd);
		}

		track.PrevCommand = cmd;
	}
}
