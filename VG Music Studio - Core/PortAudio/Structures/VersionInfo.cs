// License:     APL 2.0
// Author:      Benjamin N. Summerton <https://16bpp.net>

using System;
using System.Runtime.InteropServices;

namespace PortAudio
{
    /// <summary>
    /// A structure containing PortAudio API version information.
    /// @see Pa_GetVersionInfo, paMakeVersionNumber
    /// @version Available as of 19.5.0.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct VersionInfo
    {
        public int versionMajor;
        public int versionMinor;
        public int versionSubMinor;

        /// <summary>
        /// This is currently the Git revision hash but may change in the future.
        /// The versionControlRevision is updated by running a script before compiling the library.
        /// If the update does not occur, this value may refer to an earlier revision.
        /// </summary>
        [MarshalAs(UnmanagedType.LPStr)]
        public string versionControlRevision;      // Orignally `const char *`

        /// <summary>
        /// Version as a string, for example "PortAudio V19.5.0-devel, revision 1952M"
        /// </summary>
        [MarshalAs(UnmanagedType.LPStr)]
        public string versionText;                 // Orignally `const char *`

        public override string ToString() =>
            $"VersionInfo: v{versionMajor}.{versionMinor}.{versionSubMinor}";
    }
}
