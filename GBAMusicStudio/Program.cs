using System;
using System.Windows.Forms;

namespace GBAMusicStudio
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("it-it");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UI.MainForm());
            // Bad coding that I have to include the following line, but I legitimately don't know why a system thread was remaining alive
            Environment.Exit(0);
        }
    }
}
