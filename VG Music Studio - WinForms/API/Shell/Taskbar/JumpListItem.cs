//Copyright (c) Microsoft Corporation.  All rights reserved.

using Kermalis.VGMusicStudio.WinForms.API.Shell;

namespace Kermalis.VGMusicStudio.WinForms.API.Taskbar
{
    /// <summary>
    /// Represents a jump list item.
    /// </summary>
    public class JumpListItem : ShellFile, IJumpListItem
    {
        /// <summary>
        /// Creates a jump list item with the specified path.
        /// </summary>
        /// <param name="path">The path to the jump list item.</param>
        /// <remarks>The file type should associate the given file  
        /// with the calling application.</remarks>
        public JumpListItem(string path) : base(path) { }

        #region IJumpListItem Members

        /// <summary>
        /// Gets or sets the target path for this jump list item.
        /// </summary>
        public new string Path
        {
            get => base.Path;
            set => base.ParsingName = value;
        }

        #endregion
    }
}
