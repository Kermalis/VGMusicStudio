using GBAMusicStudio.Util;
using System.IO;

namespace GBAMusicStudio.Core
{
    public class ROM : ROMReader
    {
        public const uint Pak = 0x8000000;
        public const uint Capacity = 0x2000000;

        public static ROM Instance { get; private set; } // If you want to read with the reader

        public readonly byte[] ROMFile;
        public AGame Game { get; private set; }

        public ROM(string filePath)
        {
            Instance = this;
            SongPlayer.Stop();
            SongPlayer.ClearSamples();
            ROMFile = File.ReadAllBytes(filePath);
            InitReader();
            ReloadGameConfig();
        }
        public void ReloadGameConfig()
        {
            Game = Config.Games[System.Text.Encoding.Default.GetString(ReadBytes(4, 0xAC))];
        }

        public T ReadStruct<T>(uint offset = 0xFFFFFFFF)
        {
            if (IsValidRomOffset(offset))
                SetOffset(offset);
            return Utils.ReadStruct<T>(ROMFile, Position);
        }

        public static bool IsValidRomOffset(uint offset) => (offset < Capacity && offset < Instance.ROMFile.Length) || (offset >= Pak && offset < Pak + Capacity && offset < Instance.ROMFile.Length + Pak);
    }
}
