using GBAMusicStudio.Util;
using System;
using System.IO;

namespace GBAMusicStudio.Core
{
    class ROM : ROMReader
    {
        public const uint Pak = 0x8000000;
        public const uint Capacity = 0x2000000;

        public static ROM Instance { get; private set; }

        public readonly byte[] ROMFile;
        public AGame Game { get; private set; }
        public SongTable[] SongTables { get; private set; }

        public ROM(string filePath)
        {
            Instance = this;
            SongPlayer.Instance.Stop();
            ROMFile = File.ReadAllBytes(filePath);
            InitReader();
            HandleConfigLoaded();
            SongPlayer.Instance.Reset();
        }
        public void HandleConfigLoaded()
        {
            Game = Config.Instance.Games[System.Text.Encoding.Default.GetString(ReadBytes(4, 0xAC))];
            SongTables = new SongTable[Game.SongTables.Length];
            for (int i = 0; i < Game.SongTables.Length; i++)
            {
                uint o = Game.SongTables[i], s = Game.SongTableSizes[i];
                switch (Game.Engine.Type)
                {
                    case EngineType.M4A: SongTables[i] = new M4ASongTable(o, s); break;
                    case EngineType.MLSS: SongTables[i] = new MLSSSongTable(o, s); break;
                }
            }
        }

        public T ReadStruct<T>(uint offset = 0xFFFFFFFF)
        {
            if (offset != 0xFFFFFFFF)
                Position = offset;
            return Utils.ReadStruct<T>(ROMFile, Position);
        }

        public static bool IsValidRomOffset(uint offset) => (offset < Capacity && offset < Instance.ROMFile.Length) || (offset >= Pak && offset < Pak + Capacity && offset < Instance.ROMFile.Length + Pak);
        public static uint SanitizeOffset(uint offset)
        {
            if (!IsValidRomOffset(offset))
                throw new ArgumentOutOfRangeException("\"offset\" was invalid.");
            if (offset >= Pak)
                return offset - Pak;
            return offset;
        }
    }
}
