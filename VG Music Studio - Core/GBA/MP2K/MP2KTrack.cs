using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal sealed class MP2KTrack
{
	public readonly byte Index;
	private readonly int _startOffset;
	public byte Voice;
	public short Pitch;
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
	public long ROMOffset;
	public int EventPosition;
	public long[][] CallStack = new long[2][];
	public byte CallStackDepth;
	public bool RepeatActivated = false;
	public byte RepeatTimes;
	public ICommand RunCmd = null!;
	public byte PrevNote;
	public byte PrevVelocity;
	public byte PrevDuration;
    internal byte PseudoEchoVolume;
    internal byte PseudoEchoLength;
	public MP2KReverb Reverb = null!;
	internal bool UpdateVolume;
	internal bool UpdatePitch;

    public readonly List<MP2KChannel> Channels = [];
	public float[] Buffer = [];

	public short GetPitch()
	{
		int lfo = LFOType == LFOType.Pitch ? (MP2KUtils.Tri(LFOPhase) * LFODepth) >> 8 : 0;
		return (short)((PitchBend * PitchBendRange) + Tune + lfo);
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

	internal void ResetLFOValue()
	{
		LFOPhase = 0;

		if (LFOType == LFOType.Pitch)
		{
			UpdatePitch = true;
		}
		else
		{
			UpdateVolume = true;
		}
	}

	public MP2KTrack(byte i, int startOffset, int samplesPerBuffer)
	{
		Index = i;
		_startOffset = startOffset;
		Buffer = new float[samplesPerBuffer * 2];
		CallStack[0] = new long[3];
		CallStack[1] = new long[3];
	}
	public void Init()
	{
		Voice = 0;
		Pitch = 0;
		Priority = 0;
		Rest = 0;
		LFODelay = 0;
		LFODelayCount = 0;
		LFOPhase = 0;
		LFODepth = 0;
		PseudoEchoVolume = 0;
		PseudoEchoLength = 0;
		CallStackDepth = 0;
		PitchBend = 0;
		Tune = 0;
		Panpot = 0;
		Transpose = 0;
		ROMOffset = _startOffset;
		EventPosition = 0;
		PrevNote = 0;
		PrevVelocity = 0x7F;
		PrevDuration = 0;
		PitchBendRange = 2;
		LFOType = LFOType.Pitch;
		Ready = false;
		Stopped = false;
		UpdateVolume = false;
		UpdatePitch = false;
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
			chans[i].Kill();
		}
	}
	public void UpdateChannels()
	{
		byte vol = GetVolume();
		sbyte pan = GetPanpot();
		short pitch = GetPitch();
		for (int i = 0; i < Channels.Count; i++)
		{
			MP2KChannel c = Channels[i];
			c.SetVolume(vol, pan);
			c.SetPitch(pitch);
		}
	}

	public void UpdateSongState(SongState.Track tin, MP2KLoadedSong loadedSong, string?[] voiceTypeCache)
	{
		tin.Position = ROMOffset;
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
		// tin.Reverb = Reverb = loadedSong.Header.Reverb;

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
				if (c is not null)
				{
					if (c.State < EnvelopeState.Releasing)
					{
						tin.Keys[numKeys] = c.Note.OriginalNote;
						if (numKeys < tin.Keys.Length - 1)
						{
							numKeys++;
						}
					}
					ChannelVolume vol = c.GetVolume();
					if (vol.FromVolLeft > left)
					{
						left = vol.FromVolLeft;
					}
					if (vol.FromVolRight > right)
					{
						right = vol.FromVolRight;
					}
				}
			}
			tin.Keys[numKeys] = byte.MaxValue; // There's no way for numKeys to be after the last index in the array
			tin.LeftVolume = left;
			tin.RightVolume = right;
		}
	}
}
