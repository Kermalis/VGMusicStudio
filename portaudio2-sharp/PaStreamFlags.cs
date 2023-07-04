namespace Commons.Media.PortAudio
{
	public enum PaStreamFlags
	{
		NoFlags = 0,
		ClipOff = 1,
		DitherOff = 2,
		NeverDropInput = 4,
		PrimeOutputBuffersUsingStreamCallback = 8,
		PlatformSpecificFlags = ~(0x0000FFFF)
	}
}
