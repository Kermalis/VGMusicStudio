namespace PortAudio;

public struct HostErrorInfo
{
    /// <summary>
    /// The host API which returned the error code.
    /// </summary>
    public HostApiTypeId HostApiType;
    
    /// <summary>
    /// The error code returned.
    /// </summary>
    public long ErrorCode;
    
    /// <summary>
    /// A textual description of the error if available (encoded as UTF-8), otherwise a zero-length C string.
    /// </summary>
    public string ErrorText;
}