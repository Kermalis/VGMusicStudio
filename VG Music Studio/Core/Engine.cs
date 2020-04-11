using System;

namespace Kermalis.VGMusicStudio.Core
{
    internal class Engine : IDisposable
    {
        public enum EngineType : byte
        {
            None,
            GBA_AlphaDream,
            GBA_MP2K,
            GBA_Rare,
            NDS_DSE,
            NDS_SDAT
        }

        private static readonly Exception _gbaRomTooLargeException = new Exception($"The ROM is too large. Maximum size is 0x{GBA.Utils.CartridgeCapacity:X7} bytes.");

        public static Engine Instance { get; private set; }

        public EngineType Type { get; }
        public Config Config { get; private set; }
        public Mixer Mixer { get; private set; }
        public IPlayer Player { get; private set; }

        public Engine(EngineType type, object playerArg)
        {
            switch (type)
            {
                case EngineType.GBA_AlphaDream:
                {
                    byte[] rom = (byte[])playerArg;
                    if (rom.Length > GBA.Utils.CartridgeCapacity)
                    {
                        throw _gbaRomTooLargeException;
                    }
                    var config = new GBA.AlphaDream.Config(rom);
                    Config = config;
                    var mixer = new GBA.AlphaDream.Mixer(config);
                    Mixer = mixer;
                    Player = new GBA.AlphaDream.Player(mixer, config);
                    break;
                }
                case EngineType.GBA_MP2K:
                {
                    byte[] rom = (byte[])playerArg;
                    if (rom.Length > GBA.Utils.CartridgeCapacity)
                    {
                        throw _gbaRomTooLargeException;
                    }
                    var config = new GBA.MP2K.Config(rom);
                    Config = config;
                    var mixer = new GBA.MP2K.Mixer(config);
                    Mixer = mixer;
                    Player = new GBA.MP2K.Player(mixer, config);
                    break;
                }
                case EngineType.GBA_Rare:
                {
                    byte[] rom = (byte[])playerArg;
                    if (rom.Length > GBA.Utils.CartridgeCapacity)
                    {
                        throw _gbaRomTooLargeException;
                    }
                    var config = new GBA.Rare.Config(rom);
                    Config = config;
                    var mixer = new GBA.Rare.Mixer(config);
                    Mixer = mixer;
                    Player = new GBA.Rare.Player(mixer, config);
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
