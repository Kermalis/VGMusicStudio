using Kermalis.VGMusicStudio.Core;
using Kermalis.VGMusicStudio.Properties;
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
#if DEBUG
            //Debug.GBAGameCodeScan(@"C:\Users\Kermalis\Documents\Emulation\GBA\Games");
#endif
            try
            {
                GlobalConfig.Init();
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show(ex.Message, Strings.ErrorGlobalConfig);
                return;
            }
            Application.EnableVisualStyles();
            Application.Run(MainForm.Instance);
        }
    }
}
