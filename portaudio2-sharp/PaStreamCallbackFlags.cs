namespace Commons.Media.PortAudio
{
	public enum PaStreamCallbackFlags
	{
		InputUnderflow = 1,
		InputOverflow = 2,
		OutputUnderflow = 4,
		OutputOverflow = 8,
		PrimingOutput = 16
	}
}
