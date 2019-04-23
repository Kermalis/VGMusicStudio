using Kermalis.MusicStudio.Core.GBA.M4A;
using Kermalis.MusicStudio.Core.NDS.DSE;
using Kermalis.MusicStudio.Core.NDS.SDAT;
using System;

namespace Kermalis.MusicStudio.Core
{
    class Engine
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

        public readonly EngineType Type;
        public readonly Mixer Mixer;
        public readonly IPlayer Player;

        public Engine(EngineType type, object playerArg)
        {
            switch (type)
            {
                case EngineType.GBA_M4A:
                    {
                        var rom = (byte[])playerArg;
                        var mixer = new M4AMixer(rom);
                        Mixer = mixer;
                        Player = new M4APlayer(mixer, rom);
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
            Player.ShutDown();
        }
    }
}
