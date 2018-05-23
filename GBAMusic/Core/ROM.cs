using System.IO;
using System.Runtime.InteropServices;

namespace GBAMusic.Core
{
    class ROM : ROMReader
    {
        public const uint Map = 0x8000000;
        public const uint Capacity = 0x2000000;

        public static byte[] ROMFile { get; private set; }

        public static ROM Instance { get; private set; }

        private ROM() : base() {}

        public static void LoadROM(string filePath)
        {
            ROMFile = File.ReadAllBytes(filePath);
            Instance = new ROM();
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
