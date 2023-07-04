using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Commons.Media.PortAudio
{
	public class Configuration
	{
		public static int Version {
			get { return PortAudioInterop.Pa_GetVersion (); }
		}

		public static string VersionString {
			get { return Marshal.PtrToStringAnsi (PortAudioInterop.Pa_GetVersionText ()); }
		}

		public static string GetErrorText (PaErrorCode errorCode)
		{
			return Marshal.PtrToStringAuto (PortAudioInterop.Pa_GetErrorText (errorCode));
		}
		
		public static int HostApiCount {
			get { return PortAudioInterop.Pa_GetHostApiCount (); }
		}
		
		public static int DefaultHostApi {
			get { return PortAudioInterop.Pa_GetDefaultHostApi (); }
		}
		
		public static PaHostApiInfo GetHostApiInfo (int hostApi)
		{
			var ptr = PortAudioInterop.Pa_GetHostApiInfo (hostApi);
			if (ptr == IntPtr.Zero)
				ThrowLastError ();
			using (var cppptr = new CppInstancePtr (ptr))
				return Factory.Create<PaHostApiInfo> (cppptr);
		}
		
		public static int HostApiTypeIdToHostApiIndex (PaHostApiTypeId type)
		{
			return PortAudioInterop.Pa_HostApiTypeIdToHostApiIndex (type);
		}
		
		public static int HostApiDeviceIndexToDeviceIndex (int hostApi, int hostApiDeviceIndex)
		{
			return PortAudioInterop.Pa_HostApiDeviceIndexToDeviceIndex (hostApi, hostApiDeviceIndex);
		}
		
		public static PaHostErrorInfo GetLastHostErrorInfo ()
		{
			using (var cppptr = new CppInstancePtr (PortAudioInterop.Pa_GetLastHostErrorInfo ()))
				return Factory.Create<PaHostErrorInfo> (cppptr);
		}
		
		public static int DeviceCount {
			get { return PortAudioInterop.Pa_GetDeviceCount (); }
		}
		
		public static int DefaultInputDevice {
			get { return PortAudioInterop.Pa_GetDefaultInputDevice (); }
		}
		
		public static int DefaultOutputDevice {
			get { return PortAudioInterop.Pa_GetDefaultOutputDevice (); }
		}
		
		public static PaDeviceInfo GetDeviceInfo (int deviceIndex)
		{
			var ptr = PortAudioInterop.Pa_GetDeviceInfo (deviceIndex);
			if (ptr == IntPtr.Zero)
				ThrowLastError ();
			using (var cppptr = new CppInstancePtr (ptr))
				return Factory.Create<PaDeviceInfo> (cppptr);
		}
		
		public static PaErrorCode CheckIfFormatSupported (PaStreamParameters inputParameters, PaStreamParameters outputParameters, double sampleRate)
		{
			using (var input = Factory.ToNative<PaStreamParameters> (inputParameters))
				using (var output = Factory.ToNative<PaStreamParameters> (outputParameters))
					return PortAudioInterop.Pa_IsFormatSupported (input.Native, output.Native, sampleRate);
		}
		
		public static int GetSampleSize (PaSampleFormat format)
		{
			var ret = PortAudioInterop.Pa_GetSampleSize (format);
			HandleError ((PaErrorCode) ret);
			return ret;
		}
		
		internal static PaErrorCode HandleError (PaErrorCode errorCode)
		{
			if ((int) errorCode < 0)
				throw new PortAudioException (errorCode);
			return errorCode;
		}
		
		internal static void ThrowLastError ()
		{
			var ret = PortAudioInterop.Pa_GetLastHostErrorInfo ();
			var ei = Factory.Create<PaHostErrorInfo> (new CppInstancePtr (ret));
			if (ei.errorCode < 0)
				throw new PortAudioException (ei);
		}
	}
}
