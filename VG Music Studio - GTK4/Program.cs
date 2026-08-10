using Adw;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Kermalis.VGMusicStudio.GTK4
{
	internal class Program
	{
		private static readonly Application _app = Application.New("org.Kermalis.VGMusicStudio.GTK4", Gio.ApplicationFlags.FlagsNone);
		private static readonly OSPlatform Linux = OSPlatform.Linux;
		private static readonly OSPlatform FreeBSD = OSPlatform.FreeBSD;
		private static readonly OSPlatform Windows = OSPlatform.Windows;

		static void OnActivate(Gio.Application sender, EventArgs e)
		{
			//
		}

		[STAThread]
		public static void Main(string[] args)
		{
			//if (!RuntimeInformation.IsOSPlatform(Windows))
			//{
			//	Environment.SetEnvironmentVariable("Path", ".\\runtimes\\win-x64\\native", EnvironmentVariableTarget.Process);
			//}
			_app.Register(Gio.Cancellable.GetCurrent());

			if (!RuntimeInformation.IsOSPlatform(Linux) | !RuntimeInformation.IsOSPlatform(FreeBSD))
			{
				if (GLib.Functions.Getenv("GDK_BACKEND") is not "wayland")
				{
					GLib.Functions.Setenv("GSK_RENDERER", "cairo", false);
				}
			}

			_app.OnActivate += OnActivate;

			if (File.Exists(Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!) + "/org.Kermalis.VGMusicStudio.GTK4.gresource"))
			{
				//Load file from program directory, required for `dotnet run`
				Gio.Functions.ResourcesRegister(Gio.Functions.ResourceLoad(Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!) + "/org.Kermalis.VGMusicStudio.GTK4.gresource"));
			}
			else
			{
				var prefixes = new List<string> {
					Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!),
					Directory.GetParent(Directory.GetParent(Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!))!.FullName)!.FullName,
					Directory.GetParent(Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!))!.FullName,
					"/usr"
				};
				foreach (var prefix in prefixes)
				{
					if (File.Exists(prefix + "/share/org.Kermalis.VGMusicStudio.GTK4/org.Kermalis.VGMusicStudio.GTK4.gresource"))
					{
						Gio.Functions.ResourcesRegister(Gio.Functions.ResourceLoad(Path.GetFullPath(prefix + "/share/org.Kermalis.VGMusicStudio.GTK4/org.Kermalis.VGMusicStudio.GTK4.gresource")));
						break;
					}
				}
			}

			var argv = new string[args.Length + 1];
			argv[0] = "Kermalis.VGMusicStudio.GTK4";
			args.CopyTo(argv, 1);

			// Set an initial?
			string initial = "";
			if (args.Length > 0)
				initial = args[0].Trim();

			// Add Main Window
			var win = MainWindow.NewWithProperties([]);
			win.CreateWindow(_app);
			_app.AddWindow(win);
			win.Present();
			_app.Run(args);
		}
	}
}
