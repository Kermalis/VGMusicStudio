using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

internal sealed class DSETrack
{
	public readonly byte Index;
	private readonly int _startOffset;
	public byte Octave;
	public byte Voice;
	public byte Expression;
	public byte Volume;
	public sbyte Panpot;
	public uint Rest;
	public ushort PitchBend;
	public int CurOffset;
	public int LoopOffset;
	public bool Stopped;
	public uint LastNoteDuration;
	public uint LastRest;

	public readonly List<DSEChannel> Channels = new(0x10);

	public DSETrack(byte i, int startOffset)
	{
		Index = i;
		_startOffset = startOffset;
	}

	public void Init()
	{
		Expression = 0;
		Voice = 0;
		Volume = 0;
		Octave = 4;
		Panpot = 0;
		Rest = 0;
		PitchBend = 0;
		CurOffset = _startOffset;
		LoopOffset = -1;
		Stopped = false;
		LastNoteDuration = 0;
		LastRest = 0;
		StopAllChannels();
	}

	public void Tick()
	{
		if (Rest > 0)
		{
			Rest--;
		}
		for (int i = 0; i < Channels.Count; i++)
		{
			DSEChannel c = Channels[i];
			if (c.NoteLength > 0)
			{
				c.NoteLength--;
			}
		}
	}

	public void StopAllChannels()
	{
		DSEChannel[] chans = Channels.ToArray();
		for (int i = 0; i < chans.Length; i++)
		{
			chans[i].Stop();
		}
	}

	public void UpdateSongState(SongState.Track tin)
	{
		tin.Position = CurOffset;
		tin.Rest = Rest;
		tin.Voice = Voice;
		tin.Type = "PCM";
		tin.Volume = Volume;
		tin.PitchBend = PitchBend;
		tin.Extra = Octave;
		tin.Panpot = Panpot;

		DSEChannel[] channels = Channels.ToArray();
		if (channels.Length == 0)
		{
			tin.Keys[0] = byte.MaxValue;
			tin.LeftVolume = 0f;
			tin.RightVolume = 0f;
			//tin.Type = string.Empty;
		}
		else
		{
			int numKeys = 0;
			float left = 0f;
			float right = 0f;
			for (int j = 0; j < channels.Length; j++)
			{
				DSEChannel c = channels[j];
				c ??= new DSEChannel((byte)j); // Failsafe in the rare event that the c variable becomes null
				if (!DSEUtils.IsStateRemovable(c.State))
				{
					tin.Keys[numKeys++] = c.Key;
				}
				float a = (float)(-c.Panpot + 0x40) / 0x80 * c.Volume / 0x7F;
				if (a > left)
				{
					left = a;
				}
				a = (float)(c.Panpot + 0x40) / 0x80 * c.Volume / 0x7F;
				if (a > right)
				{
					right = a;
				}
			}
			tin.Keys[numKeys] = byte.MaxValue; // There's no way for numKeys to be after the last index in the array
			tin.LeftVolume = left;
			tin.RightVolume = right;
			//tin.Type = string.Join(", ", channels.Select(c => c.State.ToString()));
		}
	}
}
