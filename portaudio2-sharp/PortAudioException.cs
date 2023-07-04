using System;

namespace Commons.Media.PortAudio
{
	public class PortAudioException : Exception
	{
		public PortAudioException ()
			: this ("PortAudio error")
		{
		}
		
		public PortAudioException (string message)
			: base (message)
		{
		}
		
		public PortAudioException (PaErrorCode errorCode)
			: this (Configuration.GetErrorText (errorCode))
		{
		}
		
		public PortAudioException (PaHostErrorInfo info)
			: this (info.errorText)
		{
		}
	}
}
