using System;

namespace Kermalis.VGMusicStudio.Core
{
    internal class Engine : IDisposable
    {
        public enum EngineType : byte
        {
            None,
            GBA_MLSS,
            GBA_MP2K,
            NDS_DSE,
            NDS_SDAT,
            PSX_PSF
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
                case EngineType.GBA_MLSS:
                {
                    byte[] rom = (byte[])playerArg;
                    if (rom.Length > GBA.Utils.CartridgeCapacity)
                    {
                        throw new Exception($"The ROM is too large. Maximum size is 0x{GBA.Utils.CartridgeCapacity:X7} bytes.");
                    }
                    var config = new GBA.MLSS.Config(rom);
                    Config = config;
                    var mixer = new GBA.MLSS.Mixer(config);
                    Mixer = mixer;
                    Player = new GBA.MLSS.Player(mixer, config);
                    break;
                }
                case EngineType.GBA_MP2K:
                {
                    byte[] rom = (byte[])playerArg;
                    if (rom.Length > GBA.Utils.CartridgeCapacity)
                    {
                        throw new Exception($"The ROM is too large. Maximum size is 0x{GBA.Utils.CartridgeCapacity:X7} bytes.");
                    }
                    var config = new GBA.MP2K.Config(rom);
                    Config = config;
                    var mixer = new GBA.MP2K.Mixer(config);
                    Mixer = mixer;
                    Player = new GBA.MP2K.Player(mixer, config);
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
                case EngineType.PSX_PSF:
                {
                    string bgmPath = (string)playerArg;
                    var config = new PSX.PSF.Config(bgmPath);
                    Config = config;
                    var mixer = new PSX.PSF.Mixer();
                    Mixer = mixer;
                    Player = new PSX.PSF.Player(mixer, config);
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
