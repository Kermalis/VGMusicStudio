using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GBAMusic
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            LoadLibrary(System.IO.Path.GetFullPath(string.Format(@"FMOD\{0}\fmod.dll", Environment.Is64BitProcess ? 64 : 32)));

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
