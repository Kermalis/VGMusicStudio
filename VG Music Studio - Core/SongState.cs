namespace Kermalis.VGMusicStudio.Core;

public sealed class SongState
{
	public sealed class Track
	{
		public long Position;
		public byte Voice;
		public byte Volume;
		public int LFO;
		public long Rest;
		public sbyte Panpot;
		public float LeftVolume;
		public float RightVolume;
		public int PitchBend;
		public byte Extra;
		public string Type;
		public byte[] Keys;

		public int PreviousKeysTime; // TODO: Fix
		public string PreviousKeys;

		public Track()
		{
			Keys = new byte[MAX_KEYS];
			for (int i = 0; i < MAX_KEYS; i++)
			{
				Keys[i] = byte.MaxValue;
			}

			Type = null!;
			PreviousKeys = null!;
		}

		public void Reset()
		{
			Position = Rest = 0;
			Voice = Volume = Extra = 0;
			LFO = PitchBend = PreviousKeysTime = 0;
			Panpot = 0;
			LeftVolume = RightVolume = 0f;
			Type = PreviousKeys = null!;
			for (int i = 0; i < MAX_KEYS; i++)
			{
				Keys[i] = byte.MaxValue;
			}
		}
	}

	public const int MAX_KEYS = 32 + 1; // DSE is currently set to use 32 channels
	public const int MAX_TRACKS = 256; // PMD2 has a few songs with 18 tracks, PMD WiiWare has some that are 21+ tracks

	public ushort Tempo;
	public readonly Track[] Tracks;

	public SongState()
	{
		Tracks = new Track[MAX_TRACKS];
		for (int i = 0; i < MAX_TRACKS; i++)
		{
			Tracks[i] = new Track();
		}
	}

	public void Reset()
	{
		Tempo = 0;
		for (int i = 0; i < MAX_TRACKS; i++)
		{
			Tracks[i].Reset();
		}
	}
}
