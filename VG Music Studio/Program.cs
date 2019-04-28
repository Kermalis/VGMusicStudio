using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.UI;
using System;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try
            {
                GlobalConfig.Init();
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(ex.Message, "Error Loading Global Config");
                return;
            }
            Application.EnableVisualStyles();
            Application.Run(MainForm.Instance);
        }
    }
}
