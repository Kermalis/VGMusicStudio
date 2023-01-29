//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Kermalis.VGMusicStudio.WinForms.API.Shell
{
    /// <summary>Represents the base class for all search-related classes.</summary>
    public class ShellSearchCollection : ShellContainer
    {
        internal ShellSearchCollection()
        {
        }

        internal ShellSearchCollection(IShellItem2 shellItem) : base(shellItem)
        {
        }
    }
}