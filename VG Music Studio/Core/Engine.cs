using Kermalis.VGMusicStudio.Core.GBA.M4A;
using Kermalis.VGMusicStudio.Core.GBA.MLSS;
using Kermalis.VGMusicStudio.Core.NDS.DSE;
using Kermalis.VGMusicStudio.Core.NDS.SDAT;
using System;

namespace Kermalis.VGMusicStudio.Core
{
    internal class Engine
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
                    var mixer = new MLSSMixer(rom);
                    Mixer = mixer;
                    Player = new MLSSPlayer(mixer, rom);
                    break;
                }
                case EngineType.NDS_DSE:
                {
                    var mixer = new DSEMixer();
                    Mixer = mixer;
                    Player = new DSEPlayer(mixer, (string)playerArg);
                    break;
                }
                case EngineType.NDS_SDAT:
                {
                    var mixer = new SDATMixer();
                    Mixer = mixer;
                    Player = new SDATPlayer(mixer, (SDAT)playerArg);
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
            Type = type;
            Instance = this;
        }

        public void ShutDown()
        {
            // TODO: Dispose
            Config = null;
            Mixer = null;
            Player.ShutDown();
            Player = null;
            Instance = null;
        }
    }
}
