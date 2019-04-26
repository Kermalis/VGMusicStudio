using System;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(UI.MainForm.Instance);

            // Bad coding that I have to include the following line, but I legitimately don't know why a system thread was remaining alive
            Environment.Exit(0);
            // TODO: Check if Mixer.@out.Stop() fixes it
        }
    }
}
