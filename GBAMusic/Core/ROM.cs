using GBAMusic.Config;
using System.IO;
using System.Runtime.InteropServices;

namespace GBAMusic.Core
{
    internal class ROM : ROMReader
    {
        internal const uint Map = 0x8000000;
        internal const uint Capacity = 0x2000000;

        internal static ROM Instance { get; private set; } // If you want to read with the reader

        internal readonly string GameCode;
        internal readonly byte[] ROMFile;
        internal readonly GConfig Config;

        private ROM(string filePath)
        {
            Instance = this;
            ROMFile = File.ReadAllBytes(filePath);
            InitReader();
            GameCode = System.Text.Encoding.Default.GetString(ReadBytes(4, 0xAC));
            Config = new GConfig();
        }
        internal static void LoadROM(string filePath) => new ROM(filePath);

        internal T ReadStruct<T>(uint offset = 0xFFFFFFFF)
        {
            byte[] bytes = ReadBytes((uint)Marshal.SizeOf(typeof(T)), offset);
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theT = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return theT;
        }

        internal static bool IsValidRomOffset(uint offset) => (offset < Capacity) || (offset >= Map && offset < Map + Capacity);
    }
}
