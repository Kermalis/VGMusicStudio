using Adw;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Kermalis.VGMusicStudio.GTK4
{
    internal class Program
    {
        private readonly Adw.Application app;

        // public Theme Theme => Theme.ThemeType;

        [STAThread]
        public static int Main(string[] args) => new Program().Run(args);
        public Program()
        {
            app = Application.New("org.Kermalis.VGMusicStudio.GTK4", Gio.ApplicationFlags.NonUnique);

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
