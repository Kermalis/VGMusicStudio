using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal sealed class MP2KTrack
{
	public readonly byte Index;
	private readonly int _startOffset;
	public byte Voice;
	public byte PitchBendRange;
	public byte Priority;
	public byte Volume;
	public byte Rest;
	public byte LFOPhase;
	public byte LFODelayCount;
	public byte LFOSpeed;
	public byte LFODelay;
	public byte LFODepth;
	public LFOType LFOType;
	public sbyte PitchBend;
	public sbyte Tune;
	public sbyte Panpot;
	public sbyte Transpose;
	public bool Ready;
	public bool Stopped;
	public int DataOffset;
	public int[] CallStack = new int[3];
	public byte CallStackDepth;
	public byte RunCmd;
	public byte PrevNote;
	public byte PrevVelocity;

	public readonly List<MP2KChannel> Channels = new();

	public int GetPitch()
	{
		int lfo = LFOType == LFOType.Pitch ? (MP2KUtils.Tri(LFOPhase) * LFODepth) >> 8 : 0;
		return (PitchBend * PitchBendRange) + Tune + lfo;
	}
	public byte GetVolume()
	{
		int lfo = LFOType == LFOType.Volume ? (MP2KUtils.Tri(LFOPhase) * LFODepth * 3 * Volume) >> 19 : 0;
		int v = Volume + lfo;
		if (v < 0)
		{
			v = 0;
		}
		else if (v > 0x7F)
		{
			v = 0x7F;
		}
		return (byte)v;
	}
	public sbyte GetPanpot()
	{
		int lfo = LFOType == LFOType.Panpot ? (MP2KUtils.Tri(LFOPhase) * LFODepth * 3) >> 12 : 0;
		int p = Panpot + lfo;
		if (p < -0x40)
		{
			p = -0x40;
		}
		else if (p > 0x3F)
		{
			p = 0x3F;
		}
		return (sbyte)p;
	}

	public MP2KTrack(byte i, int startOffset)
	{
		Index = i;
		_startOffset = startOffset;
	}
	public void Init()
	{
		Voice = 0;
		Priority = 0;
		Rest = 0;
		LFODelay = 0;
		LFODelayCount = 0;
		LFOPhase = 0;
		LFODepth = 0;
		CallStackDepth = 0;
		PitchBend = 0;
		Tune = 0;
		Panpot = 0;
		Transpose = 0;
		DataOffset = _startOffset;
		RunCmd = 0;
		PrevNote = 0;
		PrevVelocity = 0x7F;
		PitchBendRange = 2;
		LFOType = LFOType.Pitch;
		Ready = false;
		Stopped = false;
		LFOSpeed = 22;
		Volume = 100;
		StopAllChannels();
	}
	public void Tick()
	{
		if (Rest != 0)
		{
			Rest--;
		}
		if (LFODepth > 0)
		{
			LFOPhase += LFOSpeed;
		}
		else
		{
			LFOPhase = 0;
		}
		int active = 0;
		MP2KChannel[] chans = Channels.ToArray();
		for (int i = 0; i < chans.Length; i++)
		{
			if (chans[i].TickNote())
			{
				active++;
			}
		}
		if (active != 0)
		{
			if (LFODelayCount > 0)
			{
				LFODelayCount--;
				LFOPhase = 0;
			}
		}
		else
		{
			LFODelayCount = LFODelay;
		}
		if ((LFODelay == LFODelayCount && LFODelay != 0) || LFOSpeed == 0)
		{
			LFOPhase = 0;
		}
	}

	public void ReleaseChannels(int key)
	{
		MP2KChannel[] chans = Channels.ToArray();
		for (int i = 0; i < chans.Length; i++)
		{
			MP2KChannel c = chans[i];
			if (c.Note.OriginalNote == key && c.Note.Duration == -1)
			{
				c.Release();
			}
		}
	}
	public void StopAllChannels()
	{
		MP2KChannel[] chans = Channels.ToArray();
		for (int i = 0; i < chans.Length; i++)
		{
			chans[i].Stop();
		}
	}
	public void UpdateChannels()
	{
		byte vol = GetVolume();
		sbyte pan = GetPanpot();
		int pitch = GetPitch();
		for (int i = 0; i < Channels.Count; i++)
		{
			MP2KChannel c = Channels[i];
			c.SetVolume(vol, pan);
			c.SetPitch(pitch);
		}
	}

	public void UpdateSongState(SongState.Track tin, MP2KLoadedSong loadedSong, string?[] voiceTypeCache)
	{
		tin.Position = DataOffset;
		tin.Rest = Rest;
		tin.Voice = Voice;
		tin.LFO = LFODepth;
		ref string? cache = ref voiceTypeCache[Voice];
		if (cache is null)
		{
			loadedSong.UpdateInstrumentCache(Voice, out cache);
		}
		tin.Type = cache;
		tin.Volume = GetVolume();
		tin.PitchBend = GetPitch();
		tin.Panpot = GetPanpot();

		MP2KChannel[] channels = Channels.ToArray();
		if (channels.Length == 0)
		{
			tin.Keys[0] = byte.MaxValue;
			tin.LeftVolume = 0f;
			tin.RightVolume = 0f;
		}
		else
		{
			int numKeys = 0;
			float left = 0f;
			float right = 0f;
			for (int j = 0; j < channels.Length; j++)
			{
				MP2KChannel c = channels[j];
				if (c.State < EnvelopeState.Releasing)
				{
					tin.Keys[numKeys++] = c.Note.OriginalNote;
				}
				ChannelVolume vol = c.GetVolume();
				if (vol.LeftVol > left)
				{
					left = vol.LeftVol;
				}
				if (vol.RightVol > right)
				{
					right = vol.RightVol;
				}
			}
			tin.Keys[numKeys] = byte.MaxValue; // There's no way for numKeys to be after the last index in the array
			tin.LeftVolume = left;
			tin.RightVolume = right;
		}
	}
}
