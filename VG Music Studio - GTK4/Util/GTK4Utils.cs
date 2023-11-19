using GObject;
using Gtk;
using Kermalis.VGMusicStudio.Core.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kermalis.VGMusicStudio.GTK4.Util;

internal static class GTK4Utils
{

	// Error Handle
	private static GLib.Internal.ErrorOwnedHandle ErrorHandle = new GLib.Internal.ErrorOwnedHandle(IntPtr.Zero);

	// Callback
	private static Gio.Internal.AsyncReadyCallback? _saveCallback { get; set; }
	private static Gio.Internal.AsyncReadyCallback? _openCallback { get; set; }
	private static Gio.Internal.AsyncReadyCallback? _selectFolderCallback { get; set; }


	private static readonly Random _rng = new();

	public static string Print<T>(this IEnumerable<T> source, bool parenthesis = true)
	{
		string str = parenthesis ? "( " : "";
		str += string.Join(", ", source);
		str += parenthesis ? " )" : "";
		return str;
	}
	/// <summary>Fisher-Yates Shuffle</summary>
	public static void Shuffle<T>(this IList<T> source)
	{
		for (int a = 0; a < source.Count - 1; a++)
		{
			int b = _rng.Next(a, source.Count);
			(source[b], source[a]) = (source[a], source[b]);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static float Lerp(float progress, float from, float to)
	{
		return from + ((to - from) * progress);
	}
	/// <summary>Maps a value in the range [a1, a2] to [b1, b2]. Divide by zero occurs if a1 and a2 are equal</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static float Lerp(float value, float a1, float a2, float b1, float b2)
	{
		return b1 + ((value - a1) / (a2 - a1) * (b2 - b1));
	}

	public static string? CreateLoadDialog(Span<string> filterExtensions, string title, Span<string> filterNames, Window parent)
	{
		var ff = FileFilter.New();
		for (int i = 0; i < filterNames.Length; i++)
		{
			ff.SetName(filterNames[i]);
		}
		for (int i = 0; i < filterExtensions.Length; i++)
		{
			ff.AddPattern(filterExtensions[i]);
		}
		var allFiles = FileFilter.New();
		allFiles.SetName(Strings.GTKAllFiles);
		allFiles.AddPattern("*.*");

		var d = FileDialog.New();
		d.SetTitle(title);
		var filters = Gio.ListStore.New(FileFilter.GetGType());
		filters.Append(ff);
		filters.Append(allFiles);
		d.SetFilters(filters);
		string? path = null;
		_openCallback = (source, res, data) =>
		{
			var fileHandle = Gtk.Internal.FileDialog.OpenFinish(d.Handle, res, out ErrorHandle);
			if (fileHandle != IntPtr.Zero)
			{
				path = Marshal.PtrToStringUTF8(Gio.Internal.File.GetPath(fileHandle).DangerousGetHandle());
			}
			d.Unref();
		};
		if (path != null)
		{
			d.Unref();
			return path;
		}
		Gtk.Internal.FileDialog.Open(d.Handle, parent.Handle, IntPtr.Zero, _openCallback, IntPtr.Zero);
		//d.Open(Handle, IntPtr.Zero, _openCallback, IntPtr.Zero);
		return null;
	}

	public static string? CreateSaveDialog(string fileName, Span<string> filterExtensions, string title, Span<string> filterNames, Window parent)
	{
		var ff = FileFilter.New();
		for (int i = 0; i < filterNames.Length; i++)
		{
			ff.SetName(filterNames[i]);
		}
		for (int i = 0; i < filterExtensions.Length; i++)
		{
			ff.AddPattern(filterExtensions[i]);
		}
		var allFiles = FileFilter.New();
		allFiles.SetName(Strings.GTKAllFiles);
		allFiles.AddPattern("*.*");

		var d = FileDialog.New();
		d.SetTitle(title);
		var filters = Gio.ListStore.New(FileFilter.GetGType());
		filters.Append(ff);
		filters.Append(allFiles);
		d.SetFilters(filters);
		string? path = null;
		_saveCallback = (source, res, data) =>
		{
			var fileHandle = Gtk.Internal.FileDialog.SaveFinish(d.Handle, res, out ErrorHandle);
			if (fileHandle != IntPtr.Zero)
			{
				path = Marshal.PtrToStringUTF8(Gio.Internal.File.GetPath(fileHandle).DangerousGetHandle());
			}
			d.Unref();
		};
		if (path != null)
		{
			d.Unref();
			return path;
		}
		Gtk.Internal.FileDialog.Save(d.Handle, parent.Handle, IntPtr.Zero, _openCallback, IntPtr.Zero);
		//d.Open(Handle, IntPtr.Zero, _openCallback, IntPtr.Zero);
		return null;
	}
	public static string? CreateFolderDialog(string title, Window parent)
	{
		var d = FileDialog.New();
		d.SetTitle(title);

		string? path = null;
		_selectFolderCallback = (source, res, data) =>
		{
			var folderHandle = Gtk.Internal.FileDialog.SelectFolderFinish(d.Handle, res, out ErrorHandle);
			if (folderHandle != IntPtr.Zero)
			{
				var path = Marshal.PtrToStringUTF8(Gio.Internal.File.GetPath(folderHandle).DangerousGetHandle());
			}
			d.Unref();
		};
		if (path != null)
		{
			d.Unref();
			return path;
		}
		Gtk.Internal.FileDialog.SelectFolder(d.Handle, parent.Handle, IntPtr.Zero, _selectFolderCallback, IntPtr.Zero); // SelectFolder, Open and Save methods are currently missing from GirCore, but are available in the Gtk.Internal namespace, so we're using this until GirCore updates with the method bindings. See here: https://github.com/gircore/gir.core/issues/900
		return null;
	}
}
