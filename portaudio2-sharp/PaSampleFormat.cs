namespace Commons.Media.PortAudio
{
	public enum PaSampleFormat
	{
		Float32 = 1,
		Int32 = 2,
		Int24 = 4,
		Int16 = 8,
		Int8 = 16,
		UInt8 = 32,
		Custom = 0x10000,
		NonInterleaved = ~(0x0FFFFFFF)
	}
}
