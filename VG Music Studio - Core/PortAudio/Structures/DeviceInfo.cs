// License:     APL 2.0
// Author:      Benjamin N. Summerton <https://16bpp.net>

using System;
using System.Text;
using System.Runtime.InteropServices;

using HostApiIndex = System.Int32;
using Time = System.Double;

namespace PortAudio
{
    /// <summary>
    /// A structure providing information and capabilities of PortAudio devices.
    /// Devices may support input, output or both input and output.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DeviceInfo
    {
        public int structVersion;           // this is struct version 2

        [MarshalAs(UnmanagedType.LPStr)]
        public string name;                 // Originally: `const char *`

        public HostApiIndex hostApi;        // note this is a host API index, not a type id

        public int maxInputChannels;
        public int maxOutputChannels;

        // Default latency values for interactive performance.
        public Time defaultLowInputLatency;
        public Time defaultLowOutputLatency;

        // Default latency values for robust non-interactive applications (eg. playing sound files).
        public Time defaultHighInputLatency;
        public Time defaultHighOutputLatency;

        public double defaultSampleRate;
    
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("DeviceInfo [");
            sb.AppendLine($"  structVersion={structVersion}");                     
            sb.AppendLine($"  name={name}");                     
            sb.AppendLine($"  hostApi={hostApi}");                  
            sb.AppendLine($"  maxInputChannels={maxInputChannels}");         
            sb.AppendLine($"  maxOutputChannels={maxOutputChannels}");        
            sb.AppendLine($"  defaultSampleRate={defaultSampleRate}");        
            sb.AppendLine($"  defaultLowInputLatency={defaultLowInputLatency}");   
            sb.AppendLine($"  defaultLowOutputLatency={defaultLowOutputLatency}");  
            sb.AppendLine($"  defaultHighInputLatency={defaultHighInputLatency}");  
            sb.AppendLine($"  defaultHighOutputLatency={defaultHighOutputLatency}"); 
            sb.AppendLine($"  defaultHighSampleRate={defaultSampleRate}"); 
            sb.AppendLine("]");
            return sb.ToString();
        }
    }
}
