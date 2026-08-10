using System;

namespace Kermalis.VGMusicStudio.Core.Util
{
    public abstract class DialogUtils
    {
        public abstract string CreateLoadDialog(
            string title, string filterName = "",
            string fileExtensions = "", bool isFile = false,
            bool allowAllFiles = false, object? parent = null);
        public abstract string CreateLoadDialog(
            string title, string filterName,
            Span<string> fileExtensions, bool isFile = false,
            bool allowAllFiles = false, object? parent = null);

        public abstract string CreateSaveDialog(
            string fileName, string title,
            string filterName = "", string fileExtension = "",
            bool isFile = false, bool allowAllFiles = false,
            object? parent = null);
        public abstract string CreateSaveDialog(
            string fileName, string title,
            string filterName, Span<string> fileExtensions,
            bool isFile = false, bool allowAllFiles = false,
            object? parent = null);
    }
}
