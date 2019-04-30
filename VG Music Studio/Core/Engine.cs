using System;

namespace Kermalis.VGMusicStudio.Core
{
    internal class Engine : IDisposable
    {
        public enum EngineType : byte
        {
            None,
            GBA_M4A,
            GBA_MLSS,
            NDS_DSE,
            NDS_SDAT
        }

        public static Engine Instance { get; private set; }

        public EngineType Type { get; }
        public Config Config { get; private set; }
        public Mixer Mixer { get; private set; }
        public IPlayer Player { get; private set; }

        public Engine(EngineType type, object playerArg)
        {
            switch (type)
            {
                case EngineType.GBA_M4A:
                {
                    byte[] rom = (byte[])playerArg;
                    if (rom.Length > GBA.Utils.CartridgeCapacity)
                    {
                        throw new Exception($"ROM is too large. Maximum size is 0x{GBA.Utils.CartridgeCapacity:X7} bytes.");
                    }
                    var config = new GBA.M4A.Config(rom);
                    Config = config;
                    var mixer = new GBA.M4A.Mixer(config);
                    Mixer = mixer;
                    Player = new GBA.M4A.Player(mixer, config);
                    break;
                }
                case EngineType.GBA_MLSS:
                {
                    byte[] rom = (byte[])playerArg;
                    if (rom.Length > GBA.Utils.CartridgeCapacity)
                    {
                        throw new Exception($"ROM is too large. Maximum size is 0x{GBA.Utils.CartridgeCapacity:X7} bytes.");
                    }
                    var config = new GBA.MLSS.Config(rom);
                    Config = config;
                    var mixer = new GBA.MLSS.Mixer(config);
                    Mixer = mixer;
                    Player = new GBA.MLSS.Player(mixer, config);
                    break;
                }
                case EngineType.NDS_DSE:
                {
                    string bgmPath = (string)playerArg;
                    var config = new NDS.DSE.Config(bgmPath);
                    Config = config;
                    var mixer = new NDS.DSE.Mixer();
                    Mixer = mixer;
                    Player = new NDS.DSE.Player(mixer, config);
                    break;
                }
                case EngineType.NDS_SDAT:
                {
                    var sdat = (NDS.SDAT.SDAT)playerArg;
                    var config = new NDS.SDAT.Config(sdat);
                    Config = config;
                    var mixer = new NDS.SDAT.Mixer();
                    Mixer = mixer;
                    Player = new NDS.SDAT.Player(mixer, config);
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
            Type = type;
            Instance = this;
        }

        public void Dispose()
        {
            Config.Dispose();
            Config = null;
            Mixer.Dispose();
            Mixer = null;
            Player.Dispose();
            Player = null;
            Instance = null;
        }
    }
}
