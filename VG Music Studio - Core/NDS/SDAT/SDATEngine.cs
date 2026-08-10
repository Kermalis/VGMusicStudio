namespace Kermalis.VGMusicStudio.Core.NDS.SDAT;

public sealed class SDATEngine : Engine
{
	public static SDATEngine? SDATInstance { get; private set; }

	public override SDATConfig Config { get; }
	public override SDATMixer Mixer { get; }
	public override SDATPlayer Player { get; }

	public override bool IsFileSystemFormat { get; } = true;

	public SDATEngine(SDAT sdat)
	{
		Config = new SDATConfig(sdat);
		Mixer = new SDATMixer();
		Player = new SDATPlayer(Config, Mixer);

		SDATInstance = this;
		Instance = this;
	}

	public override void Reload()
	{
		var config = Config;
		Dispose();
		_ = new SDATEngine(config.SDAT);
	}
	public override void Dispose()
	{
		base.Dispose();
		SDATInstance = null;
	}
}
