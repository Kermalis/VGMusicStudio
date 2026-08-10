using System;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

public sealed class DSEEngine : Engine
{
	public static DSEEngine? DSEInstance { get; private set; }

	public override DSEConfig Config { get; }
	public override DSEMixer Mixer { get; }
	public override DSEPlayer Player { get; }

	public override bool IsFileSystemFormat { get; } = true;

	public DSEEngine(string mainSWDFile, string smdPath, bool useNewUI = false)
	{
		Config = new DSEConfig(mainSWDFile, smdPath, useNewUI);
		Mixer = new DSEMixer();
		Player = new DSEPlayer(Config, Mixer);

		DSEInstance = this;
		Instance = this;
	}

	public override void Reload()
	{
		var config = Config;
		Dispose();
		_ = new DSEEngine(config.MainSWDFile, config.SMDPath, true);
	}
	public override void Dispose()
	{
		base.Dispose();
		DSEInstance = null;
	}
}
