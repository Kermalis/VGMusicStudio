using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace Kermalis.GBAMusicStudio
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            //Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("it-IT");
            Console.WriteLine("Culture: {0}", Thread.CurrentThread.CurrentCulture);
            Console.WriteLine("UI Culture: {0}", Thread.CurrentThread.CurrentUICulture);

            Application.EnableVisualStyles();
            Application.Run(UI.MainForm.Instance);

            // Bad coding that I have to include the following line, but I legitimately don't know why a system thread was remaining alive
            Environment.Exit(0);
            // TODO: Check if SoundMixer.@out.Stop() fixes it
        }
    }
}
