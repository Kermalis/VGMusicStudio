using Kermalis.VGMusicStudio.Core.GBA;
using Kermalis.VGMusicStudio.Core.GBA.M4A;
using Kermalis.VGMusicStudio.Core.GBA.MLSS;
using Kermalis.VGMusicStudio.Core.NDS.DSE;
using Kermalis.VGMusicStudio.Core.NDS.SDAT;
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
                    if (rom.Length > GBAUtils.CartridgeCapacity)
                    {
                        throw new Exception($"ROM is too large. Maximum size is 0x{GBAUtils.CartridgeCapacity:X7} bytes.");
                    }
                    var config = new M4AConfig(rom);
                    Config = config;
                    var mixer = new M4AMixer(config);
                    Mixer = mixer;
                    Player = new M4APlayer(mixer, config);
                    break;
                }
                case EngineType.GBA_MLSS:
                {
                    byte[] rom = (byte[])playerArg;
                    if (rom.Length > GBAUtils.CartridgeCapacity)
                    {
                        throw new Exception($"ROM is too large. Maximum size is 0x{GBAUtils.CartridgeCapacity:X7} bytes.");
                    }
                    var config = new MLSSConfig(rom);
                    Config = config;
                    var mixer = new MLSSMixer(config);
                    Mixer = mixer;
                    Player = new MLSSPlayer(mixer, config);
                    break;
                }
                case EngineType.NDS_DSE:
                {
                    string bgmPath = (string)playerArg;
                    var config = new DSEConfig(bgmPath);
                    Config = config;
                    var mixer = new DSEMixer();
                    Mixer = mixer;
                    Player = new DSEPlayer(mixer, config);
                    break;
                }
                case EngineType.NDS_SDAT:
                {
                    var sdat = (SDAT)playerArg;
                    var config = new SDATConfig(sdat);
                    Config = config;
                    var mixer = new SDATMixer();
                    Mixer = mixer;
                    Player = new SDATPlayer(mixer, config);
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
