using System;
using System.Buffers.Binary;

namespace Kermalis.VGMusicStudio.Core.GBA;

public static class GBAUtils
{
    public const double AGB_FPS = 59.7275;
    public const int AGB_APPROX_FPS = 60;
    public const int INTERFRAMES = 4;
    public const int SYSTEM_CLOCK = 16_777_216; // 16.777216 MHz (16*1024*1024 Hz)

    public const int CARTRIDGE_OFFSET = 0x08_000_000;
    public const int CARTRIDGE_CAPACITY = 0x02_000_000;

    public static ReadOnlySpan<string> PSGTypes => new string[4] { "Square 1", "Square 2", "PCM4", "Noise" };
    public static bool IsValidRange(ReadOnlySpan<byte> rom, int offset, int length)
    {
        if (offset + length >= rom.Length)
            return false;
        return true;
    }
    public static bool IsValidRomOffset(ReadOnlySpan<byte> rom, int offset)
    {
        return
            (offset >= 0 && offset < Math.Min(CARTRIDGE_CAPACITY, rom.Length)) // 0 <= Offset < min(0x2000000, ROM.Length)
            || (offset >= CARTRIDGE_OFFSET && offset < Math.Min(CARTRIDGE_CAPACITY + CARTRIDGE_OFFSET, rom.Length + CARTRIDGE_OFFSET)); // 0x8000000 <= Offset < min(0xA000000, ROM.Length + 0x8000000)
    }
    public static int SanitizeOffset(ReadOnlySpan<byte> rom, int offset)
    {
        if (!IsValidRomOffset(rom, offset))
        {
            throw new ArgumentOutOfRangeException($"Offset 0x{offset:X} was invalid.");
        }
        if (offset >= CARTRIDGE_OFFSET)
        {
            return offset - CARTRIDGE_OFFSET;
        }
        return offset;
    }
    public static int ReadOffsetData(ReadOnlySpan<byte> rom, ReadOnlySpan<byte> data)
    {
        return SanitizeOffset(rom, BinaryPrimitives.ReadInt32LittleEndian(data));
    }
}
