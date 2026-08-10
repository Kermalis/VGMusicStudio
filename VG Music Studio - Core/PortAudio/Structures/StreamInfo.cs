using Time = double;

namespace PortAudio;

public struct StreamInfo
{
    /// <summary>
    /// This is struct version 1
    /// </summary>
    public int StructVersion;
    
    /// <summary>
    /// The input latency of the stream in seconds. This value provides the most
    /// accurate estimate of input latency available to the implementation. It may
    /// differ significantly from the suggestedLatency value passed to Pa_OpenStream().
    /// The value of this field will be zero (0.) for output-only streams.
    /// @see PaTime
    /// </summary>
    public Time InputLatency;
    
    /// <summary>
    /// The output latency of the stream in seconds. This value provides the most
    /// accurate estimate of output latency available to the implementation. It may
    /// differ significantly from the suggestedLatency value passed to Pa_OpenStream().
    /// The value of this field will be zero (0.) for input-only streams.
    /// @see PaTime
    /// </summary>
    public Time OutputLatency;
    
    /// <summary>
    /// The sample rate of the stream in Hertz (samples per second). In cases
    /// where the hardware sample rate is inaccurate and PortAudio is aware of it,
    /// the value of this field may be different from the sampleRate parameter
    /// passed to Pa_OpenStream(). If information about the actual hardware sample
    /// rate is not available, this field will have the same value as the sampleRate
    /// parameter passed to Pa_OpenStream().
    /// </summary>
    public double SampleRate;
}