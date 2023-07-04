using Adw;
using System;
using System.Runtime.InteropServices;

namespace Kermalis.VGMusicStudio.GTK4
{
    internal class Program
    {
        [DllImport("libadwaita-1.so.0", EntryPoint = "g_resource_load")]
        private static extern nint LinuxResourceLoad(string path);

        [DllImport("libadwaita-1.0.dylib", EntryPoint = "g_resource_load")]
        private static extern nint MacOSResourceLoad(string path);

        [DllImport("libadwaita-1-0.dll", EntryPoint = "g_resource_load")]
        private static extern nint WindowsResourceLoad(string path);

        [DllImport("libadwaita-1.so.0", EntryPoint = "g_resources_register")]
        private static extern void LinuxResourcesRegister(nint file);

        [DllImport("libadwaita-1.0.dylib", EntryPoint = "g_resources_register")]
        private static extern void MacOSResourcesRegister(nint file);

        [DllImport("libadwaita-1-0.dll", EntryPoint = "g_resources_register")]
        private static extern void WindowsResourcesRegister(nint file);

        [DllImport("libadwaita-1.so.0", EntryPoint = "g_file_get_path")]
        private static extern string LinuxFileGetPath(nint file);

        [DllImport("libadwaita-1.0.dylib", EntryPoint = "g_file_get_path")]
        private static extern string MacOSFileGetPath(nint file);

        [DllImport("libadwaita-1-0.dll", EntryPoint = "g_file_get_path")]
        private static extern string WindowsFileGetPath(nint file);

        private delegate void OpenCallback(nint application, nint[] files, int n_files, nint hint, nint data);

        [DllImport("libadwaita-1.so.0", EntryPoint = "g_signal_connect_data")]
        private static extern ulong LinuxSignalConnectData(nint instance, string signal, OpenCallback callback, nint data, nint destroy_data, int flags);

        [DllImport("libadwaita-1.0.dylib", EntryPoint = "g_signal_connect_data")]
        private static extern ulong MacOSSignalConnectData(nint instance, string signal, OpenCallback callback, nint data, nint destroy_data, int flags);

        [DllImport("libadwaita-1-0.dll", EntryPoint = "g_signal_connect_data")]
        private static extern ulong WindowsSignalConnectData(nint instance, string signal, OpenCallback callback, nint data, nint destroy_data, int flags);

        //[DllImport("libgtk-4-1.dll", EntryPoint = "gtk_file_chooser_cell_get_type")]
        //static extern nuint GetGType();

        private readonly Adw.Application app;

        // public Theme Theme => Theme.ThemeType;

        [STAThread]
        public static int Main(string[] args) => new Program().Run(args);
        public Program()
        {
            app = Application.New("org.Kermalis.VGMusicStudio.GTK4", Gio.ApplicationFlags.NonUnique);

            //var getType = GetGType();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {

            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {

            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {

            }
            else
            {

            }

            // var theme = new ThemeType();
            // // Set LibAdwaita Themes
            // app.StyleManager!.ColorScheme = theme switch
            // {
            //     ThemeType.System => ColorScheme.PreferDark,
            //     ThemeType.Light => ColorScheme.ForceLight,
            //     ThemeType.Dark => ColorScheme.ForceDark,
            //     _ => ColorScheme.PreferDark
            // };
            var win = new MainWindow(app);

            app.OnActivate += OnActivate;

            void OnActivate(Gio.Application sender, EventArgs e)
            {
                // Add Main Window
                app.AddWindow(win);
            }
        }

        public int Run(string[] args)
        {
            var argv = new string[args.Length + 1];
            argv[0] = "Kermalis.VGMusicStudio.GTK4";
            args.CopyTo(argv, 1);
            return app.Run(args.Length + 1, argv);
        }
    }
}
