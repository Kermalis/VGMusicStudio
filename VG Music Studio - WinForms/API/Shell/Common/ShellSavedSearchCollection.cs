//Copyright (c) Microsoft Corporation.  All rights reserved.

using Kermalis.VGMusicStudio.WinForms.API.Internal;

namespace Kermalis.VGMusicStudio.WinForms.API.Shell
{
    /// <summary>Represents a saved search</summary>
    public class ShellSavedSearchCollection : ShellSearchCollection
    {
        internal ShellSavedSearchCollection(IShellItem2 shellItem)
            : base(shellItem) => CoreHelpers.ThrowIfNotVista();
    }
}