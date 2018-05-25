using System.IO;
using System.Runtime.InteropServices;

namespace GBAMusicStudio.Core
{
    public class ROM : ROMReader
    {
        public const uint Map = 0x8000000;
        public const uint Capacity = 0x2000000;

        public static ROM Instance { get; private set; } // If you want to read with the reader

        public readonly string GameCode;
        public readonly byte[] ROMFile;
        public readonly Config Config;

        public ROM(string filePath)
        {
            Instance = this;
            ROMFile = File.ReadAllBytes(filePath);
            InitReader();
            GameCode = System.Text.Encoding.Default.GetString(ReadBytes(4, 0xAC));
            Config = new Config();
        }

        public T ReadStruct<T>(uint offset = 0xFFFFFFFF)
        {
            byte[] bytes = ReadBytes((uint)Marshal.SizeOf(typeof(T)), offset);
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theT = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return theT;
        }

        public static bool IsValidRomOffset(uint offset) => (offset < Capacity) || (offset >= Map && offset < Map + Capacity);
    }
}
