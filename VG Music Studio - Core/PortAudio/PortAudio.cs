// License:     APL 2.0
// Author:      Benjamin N. Summerton <https://16bpp.net>

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PortAudio;

public static class Pa
{
    #region Constants
    /// <summary>
    /// A special <c>int</c> (DeviceIndex) value indicating that no device is available,
    /// or should be used.<br/>
    /// <br/>
    /// See: <see cref="int"/>
    /// </summary>
    public const int NoDevice = -1;

    /// <summary>
    /// Can be passed as the framesPerBuffer parameter to <c>Stream.Open()</c>
    /// or <c>Stream.OpenDefault()</c> to indicate that the stream callback will
    /// accept buffers of any size.
    /// </summary>
    public const uint FramesPerBufferUnspecified = 0;
    #endregion // Constants

    #region Properties
    /// <summary>
    /// Retrieve the release number of the currently running PortAudio build.
    /// For example, for version "19.5.1" this will return 0x00130501.<br/>
    /// <br/>
    /// See: <see cref="MakeVersionNumber"/>
    /// </summary>
    public static int Version
    {
        get => Native.Pa.Pa_GetVersion();
    }

    /// <summary>
    /// Retrieve version information for the currently running PortAudio build.<br/>
    /// <br/>
    /// See:<br/>
    /// <see cref="PortAudio.VersionInfo"/><br/>
    /// <see cref="MakeVersionNumber"/><br/>
    /// <br/>
    /// <version>Available as of 19.5.0.</version>
    /// </summary>
    /// <returns>
    /// A pointer to an immutable <c>PortAudio.VersionInfo</c> structure.
    /// </returns>
    /// <remarks>
    /// This function can be called at any time. It does not require PortAudio
    /// to be initialized. The structure pointed to is statically allocated. Do not
    /// attempt to free it or modify it.
    /// </remarks>
    public static VersionInfo VersionInfo
    {
        get => Marshal.PtrToStructure<VersionInfo>(Native.Pa.Pa_GetVersionInfo());
    }

    /// <summary>
    /// Retrieve the number of available host APIs. Even if a host API is
    /// available it may have no devices available.<br/>
    /// <br/>
    /// See: <see cref="int"/>
    /// </summary>
    /// <returns>
    /// A non-negative value indicating the number of available host APIs,
    /// or an <c>ErrorCode</c> (which are always negative) if PortAudio is not initialized
    /// or an error is encountered.
    /// </returns>
    public static int HostApiCount
    {
        get => Native.Pa.Pa_GetHostApiCount();
    }
    
    /// <summary>
    /// Retrieve the index of the default host API. The default host API will be
    /// the lowest common denominator host API on the current platform and is
    /// unlikely to provide the best performance.
    /// </summary>
    /// <returns>
    /// A non-negative value ranging from 0 to (<c>Pa.GetHostApiCount()</c>-1)
    /// indicating the default host API index or, an <c>ErrorCode</c> (which are always
    /// negative) if PortAudio is not initialized or an error is encountered.
    /// </returns>
    public static int DefaultHostApi
    {
        get => Native.Pa.Pa_GetDefaultHostApi();
    }

    /// <summary>
    /// Return information about the last host error encountered. The error
    /// information returned by <c>Pa.GetLastHostErrorInfo()</c> will never be modified
    /// asynchronously by errors occurring in other PortAudio owned threads
    /// (such as the thread that manages the stream callback.)<br/>
    /// <br/>
    /// This function is provided as a last resort, primarily to enhance debugging
    /// by providing clients with access to all available error information.
    /// </summary>
    /// <returns>
    /// A pointer to an immutable structure constraining information about
    /// the host error. The values in this structure will only be valid if a
    /// PortAudio function has previously returned the paUnanticipatedHostError
    /// error code.
    /// </returns>
    public static HostErrorInfo LastHostErrorInfo
    {
        get => Marshal.PtrToStructure<HostErrorInfo>(Native.Pa.Pa_GetLastHostErrorInfo());
    }

    /// <summary>
    /// Retrieve the number of available devices. The number of available devices
    /// may be zero.
    /// </summary>
    /// <returns>
    /// A non-negative value indicating the number of available devices
    /// or, an <c>ErrorCode</c> (which are always negative) if PortAudio is not initialized
    /// or an error is encountered.
    /// </returns>
    public static int DeviceCount
    {
        get => Native.Pa.Pa_GetDeviceCount();
    }

    /// <summary>
    /// Retrieve the index of the default input device. The result can be
    /// used in the inputDevice parameter to <c>Stream.Open()</c>.
    /// </summary>
    /// <returns>
    /// The default input device index for the default host API, or <c>NoDevice</c>
    /// if no default input device is available or an error was encountered.
    /// </returns>
    public static int DefaultInputDevice
    {
        get => Native.Pa.Pa_GetDefaultInputDevice();
    }

    /// <summary>
    /// Retrieve the index of the default output device. The result can be
    /// used in the outputDevice parameter to <c>Stream.Open()</c>.
    /// </summary>
    /// <returns>
    /// The default output device index for the default host API, or <c>NoDevice</c>
    /// if no default output device is available or an error was encountered.
    /// </returns>
    /// <remarks>
    /// On the PC, the user can specify a default device by
    /// setting an environment variable. For example, to use device #1.<br/>
    /// <pre>
    /// set PA_RECOMMENDED_OUTPUT_DEVICE=1
    /// </pre><br/>
    /// The user should first determine the available device ids by using
    /// the supplied application "pa_devs".
    /// </remarks>
    public static int DefaultOutputDevice
    {
        get => Native.Pa.Pa_GetDefaultOutputDevice();
    }
    #endregion

    #region Methods
    /// <summary>
    /// Generate a packed integer version number in the same format used
    /// by <c>Pa.GetVersion()</c>. Use this to compare a specified version number with
    /// the currently running version.<br/>
    /// <br/>
    /// <example>
    /// For example:<br/>
    /// <code>
    ///     if (Pa.GetVersion() &lt; Pa.MakeVersionNumber(19,5,1)) { }
    /// </code>
    /// </example><br/>
    /// <br/>
    /// See:<br/>
    /// <see cref="GetVersion"/><br/>
    /// <see cref="GetVersionInfo"/><br/>
    /// <br/>
    /// <version>
    /// Available as of 19.5.0.
    /// </version>
    /// </summary>
    public static int MakeVersionNumber(int major, int minor, int subminor)
    {
        return ((major)&0xFF)<<16 | ((minor)&0xFF)<<8 | ((subminor)&0xFF);
    }

    /// <summary>
    /// Retrieve the release number of the currently running PortAudio build.
    /// For example, for version "19.5.1" this will return 0x00130501.<br/>
    /// <br/>
    /// See: <see cref="MakeVersionNumber"/>
    /// </summary>
    public static int GetVersion() =>
        Native.Pa.Pa_GetVersion();

    /// <summary>
    /// Retrieve version information for the currently running PortAudio build.<br/>
    /// <br/>
    /// See:<br/>
    /// <see cref="PortAudio.VersionInfo"/><br/>
    /// <see cref="MakeVersionNumber"/><br/>
    /// <br/>
    /// <version>Available as of 19.5.0.</version>
    /// </summary>
    /// <returns>
    /// A pointer to an immutable <c>PortAudio.VersionInfo</c> structure.
    /// </returns>
    /// <remarks>
    /// This function can be called at any time. It does not require PortAudio
    /// to be initialized. The structure pointed to is statically allocated. Do not
    /// attempt to free it or modify it.
    /// </remarks>
    public static VersionInfo GetVersionInfo() =>
        Marshal.PtrToStructure<VersionInfo>(Native.Pa.Pa_GetVersionInfo());

    /// <summary>
    /// Translate the supplied PortAudio error code into a human readable
    /// message.
    /// </summary>
    public static string GetErrorText(ErrorCode errorCode) =>
        Marshal.PtrToStringAnsi(Native.Pa.Pa_GetErrorText((int)errorCode))!;

    /// <summary>
    /// Library initialization function - call this before using PortAudio.
    /// This function initializes internal data structures and prepares underlying
    /// host APIs for use.  With the exception of <c>Pa.GetVersion()</c>, <c>Pa.GetVersionText()</c>,
    /// and <c>Pa.GetErrorText()</c>, this function MUST be called before using any other
    /// PortAudio API functions.<br/>
    /// <br/>
    /// If <c>Pa.Initialize()</c> is called multiple times, each successful
    /// call must be matched with a corresponding call to <c>Pa.Terminate()</c>.
    /// Pairs of calls to <c>Pa.Initialize()</c>/<c>Pa.Terminate()</c> may overlap, and are not
    /// required to be fully nested.<br/>
    /// <br/>
    /// Note that if <c>Pa.Initialize()</c> returns an error code, <c>Pa.Terminate()</c> should
    /// NOT be called.<br/>
    /// <br/>
    /// See: <see cref="Terminate"/>
    /// </summary>
    /// <returns>
    /// <c>NoError</c> if successful, otherwise an error code indicating the cause
    /// of failure.
    /// </returns>
    public static void Initialize()
    {
        if (!Native.Config.IsLoaded)
        {
            Native.Config.ImportLibrary();
        }
        
        ErrorCode ec = (ErrorCode)Native.Pa.Pa_Initialize();
        if (ec != ErrorCode.NoError)
            throw new PortAudioException(ec, $"Error initializing PortAudio. Error code: {ec}");
    }

    /// <summary>
    /// Library termination function - call this when finished using PortAudio.
    /// This function deallocates all resources allocated by PortAudio since it was
    /// initialized by a call to <c>Pa.Initialize()</c>. In cases where<c>Pa.Initialize()</c> has
    /// been called multiple times, each call must be matched with a corresponding call
    /// to <c>Pa.Terminate()</c>. The final matching call to <c>Pa.Terminate()</c> will automatically
    /// close any PortAudio streams that are still open.<br/>
    /// <br/>
    /// <c>Pa.Terminate()</c> MUST be called before exiting a program which uses PortAudio.
    /// Failure to do so may result in serious resource leaks, such as audio devices
    /// not being available until the next reboot.<br/>
    /// <br/>
    /// See: <see cref="Initialize"/>
    /// </summary>
    /// <returns>
    /// <c>NoError</c> if successful, otherwise an error code indicating the cause
    /// of failure.
    /// </returns>
    public static void Terminate()
    {
        ErrorCode ec = (ErrorCode)Native.Pa.Pa_Terminate();
        if (ec != ErrorCode.NoError)
            throw new PortAudioException(ec, $"Error terminating PortAudio. Error code: {ec}");
    }

    /// <summary>
    /// Retrieve the number of available host APIs. Even if a host API is
    /// available it may have no devices available.<br/>
    /// <br/>
    /// See: <see cref="int"/>
    /// </summary>
    /// <returns>
    /// A non-negative value indicating the number of available host APIs,
    /// or an <c>ErrorCode</c> (which are always negative) if PortAudio is not initialized
    /// or an error is encountered.
    /// </returns>
    public static int GetHostApiCount() =>
        Native.Pa.Pa_GetHostApiCount();
    
    /// <summary>
    /// Retrieve the index of the default host API. The default host API will be
    /// the lowest common denominator host API on the current platform and is
    /// unlikely to provide the best performance.
    /// </summary>
    /// <returns>
    /// A non-negative value ranging from 0 to (<c>Pa.GetHostApiCount()</c>-1)
    /// indicating the default host API index or, an <c>ErrorCode</c> (which are always
    /// negative) if PortAudio is not initialized or an error is encountered.
    /// </returns>
    public static int GetDefaultHostApi() =>
        Native.Pa.Pa_GetDefaultHostApi();
    
    /// <summary>
    /// Retrieve a pointer to a structure containing information about a specific
    /// host Api.
    /// </summary>
    /// <param name="hostApi">
    /// A valid host API index ranging from 0 to (<c>Pa.GetHostApiCount()</c>-1)
    /// The returned structure is owned by the PortAudio implementation and must not
    /// be manipulated or freed. The pointer is only guaranteed to be valid between
    /// calls to <c>Pa.Initialize()</c> and <c>Pa.Terminate()</c>.
    /// </param>
    /// <returns>
    /// A pointer to an immutable <c>HostApiInfo</c> structure describing
    /// a specific host API. If the hostApi parameter is out of range or an error
    /// is encountered, the function returns <c>null</c>.
    /// </returns>
    public static HostApiInfo GetHostApiInfo(int hostApi) =>
        Marshal.PtrToStructure<HostApiInfo>(Native.Pa.Pa_GetHostApiInfo(hostApi));

    /// <summary>
    /// Convert a static host API unique identifier, into a runtime
    /// host API index.<br/>
    /// <br/>
    /// See: <see cref="HostApiTypeId"/>
    /// </summary>
    /// <param name="type">
    /// A unique host API identifier belonging to the <c>HostApiTypeId</c>
    /// enumeration.
    /// </param>
    /// <returns>
    /// A valid Paint ranging from 0 to (<c>Pa.GetHostApiCount()</c>-1) or,
    /// an <c>ErrorCode</c> (which are always negative) if PortAudio is not initialized
    /// or an error is encountered.<br/>
    /// <br/>
    /// The <c>HostApiNotFound</c> error code indicates that the host API specified by the
    /// type parameter is not available.
    /// </returns>
    public static int HostApiTypeIdToHostApiIndex(HostApiTypeId type) =>
        Native.Pa.Pa_HostApiTypeIdToHostApiIndex(type);

    /// <summary>
    /// Convert a host-API-specific device index to standard PortAudio device index.
    /// This function may be used in conjunction with the deviceCount field of
    /// <c>HostApiInfo</c> to enumerate all devices for the specified host API.<br/>
    /// <br/>
    /// See: <see cref="HostApiInfo"/>
    /// </summary>
    /// <param name="hostApi">
    /// A valid host API index ranging from 0 to (<c>Pa.GetHostApiCount()</c>-1)
    /// </param>
    /// <param name="hostApiDeviceIndex">
    /// A valid per-host device index in the range
    /// 0 to (<c>GetHostApiInfo(hostApi).DeviceCount</c>-1)
    /// </param>
    /// <returns>
    /// A non-negative <c>int</c> (DeviceIndex) ranging from 0 to (<c>Pa.GetDeviceCount()</c>-1)
    /// or, an <c>ErrorCode</c> (which are always negative) if PortAudio is not initialized
    /// or an error is encountered.<br/>
    /// <br/>
    /// A <c>InvalidHostApi</c> error code indicates that the host API index specified by
    /// the hostApi parameter is out of range.<br/>
    /// <br/>
    /// A <c>InvalidDevice</c> error code indicates that the hostApiDeviceIndex parameter
    /// is out of range.
    /// </returns>
    public static int HostApiDeviceIndexToDeviceIndex(int hostApi, int hostApiDeviceIndex) =>
        Native.Pa.Pa_HostApiDeviceIndexToDeviceIndex(hostApi, hostApiDeviceIndex);

    /// <summary>
    /// Return information about the last host error encountered. The error
    /// information returned by <c>Pa.GetLastHostErrorInfo()</c> will never be modified
    /// asynchronously by errors occurring in other PortAudio owned threads
    /// (such as the thread that manages the stream callback.)<br/>
    /// <br/>
    /// This function is provided as a last resort, primarily to enhance debugging
    /// by providing clients with access to all available error information.
    /// </summary>
    /// <returns>
    /// A pointer to an immutable structure constraining information about
    /// the host error. The values in this structure will only be valid if a
    /// PortAudio function has previously returned the paUnanticipatedHostError
    /// error code.
    /// </returns>
    public static HostErrorInfo GetLastHostErrorInfo() =>
        Marshal.PtrToStructure<HostErrorInfo>(Native.Pa.Pa_GetLastHostErrorInfo());

    /// <summary>
    /// Retrieve the number of available devices. The number of available devices
    /// may be zero.
    /// </summary>
    /// <returns>
    /// A non-negative value indicating the number of available devices
    /// or, an <c>ErrorCode</c> (which are always negative) if PortAudio is not initialized
    /// or an error is encountered.
    /// </returns>
    public static int GetDeviceCount() =>
        Native.Pa.Pa_GetDeviceCount();

    /// <summary>
    /// Retrieve the index of the default input device. The result can be
    /// used in the inputDevice parameter to <c>Stream.Open()</c>.
    /// </summary>
    /// <returns>
    /// The default input device index for the default host API, or <c>NoDevice</c>
    /// if no default input device is available or an error was encountered.
    /// </returns>
    public static int GetDefaultInputDevice() =>
        Native.Pa.Pa_GetDefaultInputDevice();

    /// <summary>
    /// Retrieve the index of the default output device. The result can be
    /// used in the outputDevice parameter to <c>Stream.Open()</c>.
    /// </summary>
    /// <returns>
    /// The default output device index for the default host API, or <c>NoDevice</c>
    /// if no default output device is available or an error was encountered.
    /// </returns>
    /// <remarks>
    /// On the PC, the user can specify a default device by
    /// setting an environment variable. For example, to use device #1.<br/>
    /// <pre>
    /// set PA_RECOMMENDED_OUTPUT_DEVICE=1
    /// </pre><br/>
    /// The user should first determine the available device ids by using
    /// the supplied application "pa_devs".
    /// </remarks>
    public static int GetDefaultOutputDevice() =>
        Native.Pa.Pa_GetDefaultOutputDevice();

    /// <summary>
    /// Retrieve a pointer to a PaDeviceInfo structure containing information
    /// about the specified device.<br/>
    /// <br/>
    /// See:<br/>
    /// <see cref="DeviceInfo"/><br/>
    /// <see cref="int"/>
    /// </summary>
    /// <param name="device">
    /// A valid device index in the range 0 to (<c>Pa.GetDeviceCount()</c>-1)
    /// </param>
    /// <returns>
    /// A pointer to an immutable PaDeviceInfo structure. If the device
    /// parameter is out of range the function returns <c>null</c>.
    /// </returns>
    /// <remarks>
    /// PortAudio manages the memory referenced by the returned pointer,
    /// the client must not manipulate or free the memory. The pointer is only
    /// guaranteed to be valid between calls to <c>Pa.Initialize()</c> and <c>Pa.Terminate()</c>.
    /// </remarks>
    public static DeviceInfo GetDeviceInfo(int device) =>
        Marshal.PtrToStructure<DeviceInfo>(Native.Pa.Pa_GetDeviceInfo(device));
    #endregion
}
