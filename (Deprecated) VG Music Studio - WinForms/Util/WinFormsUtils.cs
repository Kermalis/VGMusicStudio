using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.WinForms.Util;

internal class WinFormsUtils : DialogUtils
{
    private static void Convert(string filterName, Span<string> fileExtensions)
    {
        string extensions;
        if (fileExtensions == null) fileExtensions = new string[1];
        if (fileExtensions.Length > 1)
        {
            extensions = $"|";
            foreach (string ext in fileExtensions)
            {
                extensions += $"*.{ext}";
                if (ext != fileExtensions[fileExtensions.Length])
                {
                    extensions += $";";
                }
            }
        }
        else
        {
            if (filterName.Contains('|'))
            {
                var filters = filterName.Split('|');
                fileExtensions[0] = filters[1];
            }
            extensions = fileExtensions[0];
            if (extensions.StartsWith('.'))
            {
                if (extensions.Contains(';'))
                {
                    var ext = extensions.Split(';');
                    fileExtensions[0] = ext[0];
                }
            }
            else if (extensions.StartsWith('*'))
            {
                var modifiedExt = extensions.Trim('*');
                if (modifiedExt.Contains(';'))
                {
                    var ext = modifiedExt.Split(';');
                    fileExtensions[0] = ext[0];
                }
                else
                {
                    fileExtensions[0] = modifiedExt;
                }
            }
            else
            {
                if (extensions.Contains(';'))
                {
                    var ext = extensions.Split(';');
                    fileExtensions[0] = $".{ext[0]}";
                }
                else
                {
                    fileExtensions[0] = extensions;
                }
            }
        }
    }

    public static string CreateLoadDialog(string title, object parent = null!) =>
        new WinFormsUtils().CreateLoadDialog(title, "", "", false, false, parent);
    public static string CreateLoadDialog(string extension, string title, string filter, object parent = null!) =>
        new WinFormsUtils().CreateLoadDialog(title, filter, [extension], true, true, parent);
    public static string CreateLoadDialog(Span<string> extensions, string title, string filter, object parent = null!) =>
        new WinFormsUtils().CreateLoadDialog(title, filter, extensions, true, true, parent);
    public override string CreateLoadDialog(string title, string filterName = "", string fileExtension = "", bool isFile = false, bool allowAllFiles = false, object? parent = null) =>
        CreateLoadDialog(title, filterName, [fileExtension], isFile, allowAllFiles);
    public override string CreateLoadDialog(string title, string filterName, Span<string> fileExtensions, bool isFile = false, bool allowAllFiles = false, object? parent = null)
    {
        if (isFile)
        {
            Convert(filterName, fileExtensions);
            var allFiles = "";
            if (allowAllFiles) allFiles = $"|{Strings.FilterAllFiles}|*.*";
            var d = new OpenFileDialog
            {
                DefaultExt = fileExtensions[0],
                ValidateNames = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Title = title,
                Filter = $"{filterName}{allFiles}",
            };
            if (d.ShowDialog() == DialogResult.OK)
            {
                return d.FileName;
            }
        }
        else
        {
            var d = new FolderBrowserDialog
            {
                Description = Strings.MenuOpenDSE,
                UseDescriptionForTitle = true,
            };
            if (d.ShowDialog() == DialogResult.OK)
            {
                return d.SelectedPath;
            }
        }
        return null!;
    }
    public static string CreateSaveDialog(string fileName, string extension, string title, string filter, object parent = null!) =>
        new WinFormsUtils().CreateSaveDialog(fileName, title, filter, [extension], false, false, parent);
    public static string CreateSaveDialog(string fileName, Span<string> extensions, string title, string filter, object parent = null!) =>
        new WinFormsUtils().CreateSaveDialog(fileName, title, filter, extensions, false, false, parent);
    public override string CreateSaveDialog(string fileName, string title, string filterName, string fileExtension = "", bool isFile = false, bool allowAllFiles = false, object? parent = null) =>
        CreateSaveDialog(fileName, title, filterName, [fileExtension], false);
    public override string CreateSaveDialog(string fileName, string title, string filterName, Span<string> fileExtensions, bool isFile = false, bool allowAllFiles = false, object? parent = null)
    {
        Convert(filterName, fileExtensions);
        var allFiles = "";
        if (allowAllFiles) allFiles = $"|{Strings.FilterAllFiles}|*.*";
        var d = new SaveFileDialog
        {
            FileName = fileName,
            DefaultExt = fileExtensions[0],
            AddExtension = true,
            ValidateNames = true,
            CheckPathExists = true,
            Title = title,
            Filter = $"{filterName}{allFiles}",
        };
        if (d.ShowDialog() == DialogResult.OK)
        {
            return d.FileName;
        }
        return null!;
    }
}
