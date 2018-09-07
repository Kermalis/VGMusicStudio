using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace GBAMusicStudio.Core
{
    class ROM
    {
        public const uint Pak = 0x8000000;
        public const uint Capacity = 0x2000000;

        public static ROM Instance { get; private set; }

        public readonly byte[] ROMFile;
        public readonly EndianBinaryReader Reader;
        public readonly EndianBinaryWriter Writer;

        public AGame Game { get; private set; }
        public SongTable[] SongTables { get; private set; }

        public ROM(string filePath)
        {
            Instance = this;
            SongPlayer.Instance.Stop();
            ROMFile = File.ReadAllBytes(filePath);
            var stream = Stream.Synchronized(new MemoryStream(ROMFile));
            Reader = new EndianBinaryReader(stream);
            Writer = new EndianBinaryWriter(stream);
            HandleConfigLoaded();
            SongPlayer.Instance.Reset();
        }
        public void HandleConfigLoaded()
        {
            Game = Config.Instance.Games[Reader.ReadString(4, 0xAC)];
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

        public static bool IsValidRomOffset(uint offset)
        {
            return
                (offset < Math.Min(Capacity, Instance.ROMFile.Length)) // 0 <= offset < min(0x2000000, ROMFile.Length)
                || (offset >= Pak && offset < Math.Min(Capacity + Pak, Instance.ROMFile.Length + Pak)); // 0x8000000 <= offset < min(0xA000000, ROMFile.Length + 0x8000000)
        }
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
