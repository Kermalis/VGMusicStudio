using Kermalis.VGMusicStudio.Core.Properties;
using Kermalis.VGMusicStudio.Core.Util;
using Kermalis.VGMusicStudio.WinForms.Util;
using System;
using System.Windows.Forms;

namespace Kermalis.VGMusicStudio.WinForms;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
#if DEBUG
		//VGMSDebug.SimulateLanguage("en");
		//VGMSDebug.SimulateLanguage("es");
		//VGMSDebug.SimulateLanguage("fr");
		//VGMSDebug.SimulateLanguage("it");
		//VGMSDebug.SimulateLanguage("ru");
		//VGMSDebug.GBAGameCodeScan(@"C:\Users\Kermalis\Documents\Emulation\GBA\Games");
#endif
		try
		{
			GlobalConfig.Init();
		}
		catch (Exception ex)
		{
			FlexibleMessageBox.Show(ex, Strings.ErrorGlobalConfig);
			return;
		}

		ApplicationConfiguration.Initialize();
		Application.Run(MainForm.Instance);
	}
}