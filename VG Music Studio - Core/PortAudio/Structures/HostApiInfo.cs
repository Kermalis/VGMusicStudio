using System.Runtime.InteropServices;

using DeviceIndex = int;

namespace PortAudio;

/// <summary>
/// A structure containing information about a particular host API.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct HostApiInfo
{
    /// <summary>
    /// this is struct version 1
    /// </summary>
    public int StructVersion;
    
    /// <summary>
    /// The well known unique identifier of this host API @see PaHostApiTypeId
    /// </summary>
    public HostApiTypeId Type;
    
    /// <summary>
    /// A textual description of the host API for display on user interfaces. Encoded as UTF-8.
    /// </summary>
    [MarshalAs(UnmanagedType.LPStr)]
    public string Name;
    
    /// <summary>
    /// The number of devices belonging to this host API. This field may be
    /// used in conjunction with Pa_HostApiDeviceIndexToDeviceIndex() to enumerate
    /// all devices for this host API.
    /// @see Pa_HostApiDeviceIndexToDeviceIndex
    /// </summary>
    public int DeviceCount;
    
    /// <summary>
    /// The default input device for this host API. The value will be a
    /// device index ranging from 0 to (Pa_GetDeviceCount()-1), or paNoDevice
    /// if no default input device is available.
    /// </summary>
    public DeviceIndex DefaultInputDevice;
    
    /// <summary>
    /// The default output device for this host API. The value will be a
    /// device index ranging from 0 to (Pa_GetDeviceCount()-1), or paNoDevice
    /// if no default output device is available.
    /// </summary>
    public DeviceIndex DefaultOutputDevice;
}