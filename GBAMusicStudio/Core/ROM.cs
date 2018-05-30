using System.IO;
using System.Runtime.InteropServices;

namespace GBAMusicStudio.Core
{
    public class ROM : ROMReader
    {
        public const uint Pak = 0x8000000;
        public const uint Capacity = 0x2000000;

        public static ROM Instance { get; private set; } // If you want to read with the reader

        public readonly byte[] ROMFile;
        public Game Game { get; private set; }

        public ROM(string filePath)
        {
            Instance = this;
            MusicPlayer.Stop();
            MusicPlayer.ClearSamples();
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
            byte[] bytes = ReadBytes((uint)Marshal.SizeOf(typeof(T)), offset);
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theT = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return theT;
        }

        public static bool IsValidRomOffset(uint offset) => (offset < Capacity) || (offset >= Pak && offset < Pak + Capacity);
    }
}
