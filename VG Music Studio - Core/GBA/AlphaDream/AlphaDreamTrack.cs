namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream;

internal sealed class AlphaDreamTrack
{
	public readonly byte Index;
	public readonly string Type;
	public readonly AlphaDreamChannel Channel;

	public byte Voice;
	public byte PitchBendRange;
	public byte Volume;
	public byte Rest;
	public byte NoteDuration;
	public sbyte PitchBend;
	public sbyte Panpot;
	public bool IsEnabled;
	public bool Stopped;
	public int StartOffset;
	public int DataOffset;
	public byte PrevCommand;

	public int GetPitch()
	{
		return PitchBend * (PitchBendRange / 2);
	}

	public AlphaDreamTrack(byte i, AlphaDreamMixer mixer)
	{
		Index = i;
		if (i >= 8)
		{
			Type = GBAUtils.PSGTypes[i & 3];
			Channel = new AlphaDreamSquareChannel(mixer); // TODO: PSG Channels 3 and 4
		}
		else
		{
			Type = "PCM8";
			Channel = new AlphaDreamPCMChannel(mixer);
		}
	}
	// 0x819B040
	public void Init()
	{
		Voice = 0;
		Rest = 1; // Unsure why Rest starts at 1
		PitchBendRange = 2;
		NoteDuration = 0;
		PitchBend = 0;
		Panpot = 0; // Start centered; ROM sets this to 0x7F since it's unsigned there
		DataOffset = StartOffset;
		Stopped = false;
		Volume = 200;
		PrevCommand = 0xFF;
		//Tempo = 120;
		//TempoStack = 0;
	}
	public void Tick()
	{
		if (Rest != 0)
		{
			Rest--;
		}
		if (NoteDuration > 0)
		{
			NoteDuration--;
		}
	}

	public void UpdateSongState(SongState.Track tin)
	{
		tin.Position = DataOffset;
		tin.Rest = Rest;
		tin.Voice = Voice;
		tin.Type = Type;
		tin.Volume = Volume;
		tin.PitchBend = GetPitch();
		tin.Panpot = Panpot;
		if (NoteDuration != 0 && !Channel.Stopped)
		{
			tin.Keys[0] = Channel.Key;
			ChannelVolume vol = Channel.GetVolume();
			tin.LeftVolume = vol.LeftVol;
			tin.RightVolume = vol.RightVol;
		}
		else
		{
			tin.Keys[0] = byte.MaxValue;
			tin.LeftVolume = 0f;
			tin.RightVolume = 0f;
		}
	}
}
