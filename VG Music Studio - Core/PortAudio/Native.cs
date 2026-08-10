using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PortAudio.Native;

internal static partial class Pa
{
    [LibraryImport("PortAudioLib")]
    public static partial int Pa_GetVersion();

    [LibraryImport("PortAudioLib")]
    public static partial nint Pa_GetVersionInfo();           // Originally returns `const PaVersionInfo *`

    [LibraryImport("PortAudioLib")]
    public static partial nint Pa_GetErrorText([MarshalAs(UnmanagedType.I4)] int errorCode);     // Orignially returns `const char *`

    [LibraryImport("PortAudioLib")]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial int Pa_Initialize();

    [LibraryImport("PortAudioLib")]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial int Pa_Terminate();

    [LibraryImport("PortAudioLib")]
    public static partial int Pa_GetHostApiCount();

    [LibraryImport("PortAudioLib")]
    public static partial int Pa_GetDefaultHostApi();

    [LibraryImport("PortAudioLib")]
    public static partial nint Pa_GetHostApiInfo(int hostApi);

    [LibraryImport("PortAudioLib")]
    public static partial int Pa_HostApiTypeIdToHostApiIndex(HostApiTypeId type);

    [LibraryImport("PortAudioLib")]
    public static partial int Pa_HostApiDeviceIndexToDeviceIndex(int hostApi, int hostApiDeviceIndex);

    [LibraryImport("PortAudioLib")]
    public static partial nint Pa_GetLastHostErrorInfo();

    [LibraryImport("PortAudioLib")]
    public static partial int Pa_GetDeviceCount();

    [LibraryImport("PortAudioLib")]
    public static partial int Pa_GetDefaultInputDevice();

    [LibraryImport("PortAudioLib")]
    public static partial int Pa_GetDefaultOutputDevice();

    [LibraryImport("PortAudioLib")]
    public static partial nint Pa_GetDeviceInfo(int device);   // Originally returns `const PaDeviceInfo *`

    [LibraryImport("PortAudioLib")]
    public static partial void Pa_Sleep(int msec);
}


internal static partial class Stream
{
    [LibraryImport("PortAudioLib")]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial int Pa_OpenStream(
        out nint stream,                          // `PaStream **`
        nint inputParameters,                     // `const PaStreamParameters *`
        nint outputParameters,                    // `const PaStreamParameters *`
        double sampleRate,
        uint framesPerBuffer,
        StreamFlags streamFlags,
        nint streamCallback,                      // `PaStreamCallback *`
        nint userData                             // `void *`
    );

    [LibraryImport("PortAudioLib")]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial int Pa_OpenDefaultStream(
        out nint stream,
        int numInputChannels,
        int numOutputChannels,
        SampleFormat sampleFormat,
        double sampleRate,
        uint framesPerBuffer,
        nint streamCallback,
        nint userData
    );

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.I4)]
    public delegate StreamCallbackResult Callback(
        nint input, nint output,                // Originally `const void *, void *`
        uint frameCount,
        ref StreamCallbackTimeInfo timeInfo,        // Originally `const PaStreamCallbackTimeInfo*`
        StreamCallbackFlags statusFlags,
        nint userData                             // Orignially `void *`
    );

    [LibraryImport("PortAudioLib")]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial int Pa_CloseStream(nint stream);       // `PaStream *`

    [LibraryImport("PortAudioLib")]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial int Pa_SetStreamFinishedCallback(
        nint stream,                                                  // `PaStream *`
        nint streamFinishedCallback                                   // `PaStreamFinishedCallback *`
    );

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void FinishedCallback(
        nint userData                         // Originally `void *`
    );

    [LibraryImport("PortAudioLib")]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial int Pa_StartStream(nint stream);       // `PaStream *`

    [LibraryImport("PortAudioLib")]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial int Pa_StopStream(nint stream);        // `PaStream *`

    [LibraryImport("PortAudioLib")]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial int Pa_AbortStream(nint stream);       // `PaStream *`

    [LibraryImport("PortAudioLib")]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial int Pa_IsStreamStopped(nint stream);   // `PaStream *`

    [LibraryImport("PortAudioLib")]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial int Pa_IsStreamActive(nint stream);    // `PaStream *`

    [LibraryImport("PortAudioLib")]
    public static partial double Pa_GetStreamCpuLoad(nint stream);     // `PaStream *`

    [LibraryImport("PortAudioLib")]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial int Pa_ReadStream(
        nint stream,                                                    // `PaStream *`
        nint buffer,                                                    // `void *`
        ulong frames                                                    // `unsigned long`
    );

    [LibraryImport("PortAudioLib")]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial int Pa_WriteStream(
        nint stream,                                                    // `PaStream *`
        nint buffer,                                                    // `const void *`
        ulong frames                                                    // `unsigned long`
    );
}

internal static class Config
{
    internal static bool IsLoaded = false;
    // Based on the code from the Nickvision Application template https://github.com/NickvisionApps/Application
    // Code reference: https://github.com/NickvisionApps/Application/blob/28e3307b8242b2d335f8f65394a03afaf213363a/NickvisionApplication.GNOME/Program.cs#L50
    internal static void ImportLibrary()
    {
        try
        {
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), LibraryResolver);
        }
        catch
        {
            Debug.WriteLine("PortAudio Library is already loaded.");
            return;
        }
        IsLoaded = true;
    }

    // Code reference: https://github.com/NickvisionApps/Application/blob/28e3307b8242b2d335f8f65394a03afaf213363a/NickvisionApplication.GNOME/Program.cs#L136
    private static nint LibraryResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        string fileName;
        fileName = libraryName switch
        {
            "PortAudioLib" => "portaudio",
            _ => libraryName
        };
        return NativeLibrary.Load(fileName, assembly, searchPath);
    }
}
