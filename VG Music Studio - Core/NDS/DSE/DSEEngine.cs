using System;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

public sealed class DSEEngine : Engine
{
	public static DSEEngine? DSEInstance { get; private set; }

	public override DSEConfig Config { get; }
	public override DSEMixer Mixer { get; }
	public override DSEPlayer Player { get; }

	public DSEEngine(string[] SWDFiles, string bgmPath)
	{
		Config = new DSEConfig(bgmPath);
		Mixer = new DSEMixer();
		Player = new DSEPlayer(SWDFiles, Config, Mixer);

		DSEInstance = this;
		Instance = this;
	}

	public override void Dispose()
	{
		base.Dispose();
		DSEInstance = null;
	}
}
