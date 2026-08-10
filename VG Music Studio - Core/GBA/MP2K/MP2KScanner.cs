using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Kermalis.EndianBinaryIO;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

public sealed class MP2KScanner(byte[] rom)
{
    private const int MIN_SONG_NUM = 4;    // lowest one seen is 7 in Tetris World
    private const int SEARCH_START = 0x200;

    private readonly byte[] _rom = rom;

    private readonly Dictionary<int, long> _wordPointerCache = [];    // pointers to 'words' (i.e. 4-byte aligned pointers)

    internal List<ScanResult> Scan()
    {
        List<ScanResult> results = [];

        InitPointerCache();

        long findPos = SEARCH_START;

        while (true)
        {
            /* 1. Determine songtable position */
            long songTablePos = 0;
            ushort songCount = 0;

            bool songtableValid = FindSongTable(ref findPos, ref songTablePos, ref songCount);
            if (!songtableValid)
            {
                break;
            }

            /* 2. Determine player table position */
            List<PlayerInfo> playerTableInfo = [];
            long playerTablePos = 0;
            bool playerTableValid = FindPlayerTable(songTablePos, ref playerTablePos, ref playerTableInfo);
            if (!playerTableValid)
            {
                continue;
            }

            /* 3. Determine sound mode. */
            uint soundMode = 0;
            long soundModePos = 0;
            bool soundModeValid = FindSoundMode(playerTablePos, ref soundModePos, ref soundMode);
            if (!soundModeValid)
            {
                continue;
            }

            /* 4. Save to result list. */
            ScanResult result = new()
            {
                MP2KSoundMode = new MP2KSoundMode()
                {
                    Volume = (byte)((soundMode >> 12) & 0xF),
                    Reverb = (byte)((soundMode >> 0) & 0xFF),
                    FrequencyIndex = (byte)((soundMode >> 16) & 0xF),
                    MaxChannels = (byte)((soundMode >> 8) & 0xF),
                    DACConfig = (byte)((soundMode >> 20) & 0xF),
                },
                PlayerTableInfo = playerTableInfo,
                SongTableInfo = new SongTableInfo()
                {
                    Position = songTablePos,
                    Count = songCount,
                    TableIndex = (byte)results.Count,
                },
            };

            results.Add(result);
        }

        return results;
    }

    private bool FindSongTable(ref long findStartPos, ref long songTablePos, ref ushort songCount)
    {
        for (long i = findStartPos; i < _rom.Length - 3; i += 4)
        {
            long candidatePos = i;
            bool candidateValid = true;

            /* check if MIN_SONG_NUM entries look like a valid song table */
            for (int j = 0; j < MIN_SONG_NUM; j++)
            {
                if (!IsValidSongTableEntry(i + j * 8))
                {
                    i += j * 8;
                    candidateValid = false;
                    break;
                }
            }

            if (!candidateValid)
            {
                continue;
            }

            /* scan for a reference to the song table (for verification) */
            if (!IsPosReferenced(candidatePos))
            {
                continue;
            }

            /* count number of songs */
            ushort candidateSongCount = 0;
            ushort candidatePopulatedSongCount = 0;

            // COUNT_AUTO == 0xFFFF, so use one less
            for (ushort songIndex = 0; songIndex < 0xFFFE; songIndex++)
            {
                long pos = candidatePos + 8 * songIndex;
                if (pos >= _rom.Length - 3)
                {
                    break;
                }

                if (!IsValidSongTableEntry(pos))
                {
                    break;
                }

                candidateSongCount++;
                candidatePopulatedSongCount = candidateSongCount;
            }

            /* Do not allow song tables which are entirely (or almost blank).
             * This way GSF sets are not accidentally detected with number of songs=0
             * (e.g. when the falsely detected songtable only consists of zero bytes. */
            if (candidatePopulatedSongCount < MIN_SONG_NUM)
            {
                continue;
            }

            /* return results */
            songCount = candidateSongCount;
            songTablePos = candidatePos;
            findStartPos = candidatePos + songCount * 8;
            return true;
        }

        return false;
    }

    private bool FindPlayerTable(long songTablePos, ref long playerTablePos, ref List<PlayerInfo> playerTableInfo)
    {
        /* If we know the pointer to the song table, we can find the pointer to the player table by searching
         * through the ROM. Both player table and songtable are usually referenced in:
         * m4aSongNum{Start,StartOrChange,StartOrContinue,Stop,SongNumContinue}.
         * mplay_table and song_table will appear right after another in the literal pools of those functions.
         * Because we already know the song table pos, we can just check all the places where it is referenced,
         * and then check the 4 bytes before that reference. Those have to be a valid player table. */

        long songTableRef = songTablePos + GBAUtils.CARTRIDGE_OFFSET;

        int musicPlayerCount = 0;
        int playerTableStartPos = 0;
        int matchCount = 0;

        for (int i = SEARCH_START; i < _rom.Length - 3; i += 4)
        {
            /* 1. Search the entire ROM for a reference to our song table. */
            if (EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(i, 4), Endianness.LittleEndian) != songTableRef)
            {
                continue;
            }

            if (i < 4)
            {
                continue;
            }

            /* 2. Check if the pointer before the reference also looks like a pointer. */
            if (!GBAUtils.IsValidRomOffset(_rom, EndianBinaryPrimitives.ReadInt32(_rom.AsSpan(i - 4, 4), Endianness.LittleEndian) - GBAUtils.CARTRIDGE_OFFSET))
            {
                continue;
            }

            /* 3. Check how many music players we have. */
            int playerTablePosCandidate = GBAUtils.SanitizeOffset(_rom, EndianBinaryPrimitives.ReadInt32(_rom.AsSpan(i - 4, 4), Endianness.LittleEndian));
            int MAX_MUSIC_PLAYERS = 32;

            int musicPlayerCountCandidate;
            for (musicPlayerCountCandidate = 0; musicPlayerCountCandidate < MAX_MUSIC_PLAYERS; musicPlayerCountCandidate++)
            {
                if (!IsValidPlayerTableEntry(playerTablePosCandidate + musicPlayerCountCandidate * 12))
                {
                    break;
                }
            }

            if (musicPlayerCountCandidate == 0)
            {
                continue;
            }

            /* 4. If we found a player table previously, check if it is the same one. */
            if (playerTableStartPos != 0 && playerTableStartPos != playerTablePosCandidate)
            {
                Debug.WriteLine($"Found multiple player table candidates (candidateA={playerTableStartPos:X9} candidateB={playerTablePosCandidate:X9}. Bad ROM-hack?");
                break;
            }

            playerTableStartPos = playerTablePosCandidate;
            musicPlayerCount = musicPlayerCountCandidate;
            matchCount += 1;

            /* 4. If we have found 5 functions, that's enough, since that's the usual number of m4a functions
             * found in games. */
            if (matchCount == 5)
            {
                break;
            }
        }

        /* 5. If we haven't found anything, bail out. */
        if (matchCount == 0)
        {
            return false;
        }

        /* 6. We can still load games with fewer references to the player table. But let the user know, there
         *    may be a problem with the ROM. */
        if (matchCount < 5)
        {
            Debug.WriteLine($"Found unexpected number of player table matches ({matchCount}). Bad ROM-hack?");
        }

        /* 7. Return results */
        playerTableInfo.Clear();
        for (int i = 0; i < musicPlayerCount; i++)
        {
            int playerPos = playerTableStartPos + i * 12;
            PlayerInfo pi = new()
            {
                MaxTracks = _rom[playerPos + 8],
                UsePriority = _rom[playerPos + 10]
            };
            playerTableInfo.Add(pi);
        }
        playerTablePos = playerTableStartPos;
        return true;
    }

    private bool FindSoundMode(long playerTablePos, ref long soundModePos, ref uint soundMode)
    {
        if (FindSoundModeNormal(playerTablePos, ref soundModePos, ref soundMode))
        {
            return true;
        }

        return FindSoundModeMetroid(playerTablePos, ref soundModePos, ref soundMode);
    }

    private bool FindSoundModeNormal(long playerTablePos, ref long soundModePos, ref uint soundMode)
    {
        /* We have to find the literal pool from m4aSoundInit(). */
        int playerTableReferencePos = 0;
        int findStartPos = SEARCH_START;
        while (IsPosReferenced(playerTablePos, ref findStartPos, ref playerTableReferencePos))
        {
            /* If we have found a reference, check if the following pattern exists:
             * [0x00] mix code (ROM-addr)
             * [0x04] mix code (RAM-addr)
             * [0x08] mix code size (CpuSet Arg)
             * [0x0C] SoundInfo ptr (RAM-addr)
             * [0x10] CgbChan ptr (RAM-addr)
             * [0x14] sound mode <---- data of interest
             * [0x18] player table len (0xNN, 0x00, 0x00, 0x00)
             * [0x1C] player table pos (ROM-addr) <---- we pivot from this supplied address
             * [0x20] memacc area TODO confirm (RAM-addr)
             */

            if (playerTableReferencePos < 0x1C)
            {
                continue;
            }

            int signaturePos = playerTableReferencePos - 0x1C;

            if ((signaturePos + 0x24) > _rom.Length)
            {
                continue;
            }

            // fmt::print("Found reference to playerTable=0x{:x} at 0x{:x}\n", playerTablePos, playerTableReferencePos);

            // fmt::print("sound mode signature:\n");
            // fmt::print(" - mix code ROM addr: 0x{:08x}\n", rom.ReadU32(signaturePos + 0));
            // fmt::print(" - mix code RAM addr: 0x{:08x}\n", rom.ReadU32(signaturePos + 4));
            // fmt::print(" - mix code size: 0x{:08x}\n", rom.ReadU32(signaturePos + 8));
            // fmt::print(" - SoundInfo ptr: 0x{:08x}\n", rom.ReadU32(signaturePos + 12));
            // fmt::print(" - CgbChan ptr: 0x{:08x}\n", rom.ReadU32(signaturePos + 16));
            // fmt::print(" - sound mode: 0x{:08x}\n", rom.ReadU32(signaturePos + 20));
            // fmt::print(" - player table len: 0x{:08x}\n", rom.ReadU32(signaturePos + 24));
            // fmt::print(" - player table pos: 0x{:08x}\n", rom.ReadU32(signaturePos + 28));
            // fmt::print(" - memacc area: 0x{:08x}\n", rom.ReadU32(signaturePos + 32));

            /* check mix code (ROM-addr) */
            if (!GBAUtils.IsValidRomOffset(_rom, EndianBinaryPrimitives.ReadInt32(_rom.AsSpan(signaturePos + 0x0, 4), Endianness.LittleEndian) - GBAUtils.CARTRIDGE_OFFSET))
            {
                continue;
            }

            // fmt::print("mix code ROM valid\n");

            /* check mix code (RAM-addr) */
            if (!IsValidRamPointer(EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x4, 4), Endianness.LittleEndian)))
            {
                continue;
            }

            // fmt::print("mix code RAM valid\n");

            /* check mix code size (CpuSet Arg) */
            uint cpusetArg = EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x8, 4), Endianness.LittleEndian);
            if ((cpusetArg & (1 << 26)) == 0)    // Is 32 bit copy?
            {
                continue;
            }
            if ((cpusetArg & 0x1FFFFF) >= 0x800)    // Is data smaller than 0x800 words? (usually just SEARCH_START)
            {
                continue;
            }

            // fmt::print("mix code size valid\n");

            /* check SoundInfo pointer (RAM addr) */
            if (!IsValidRamPointer(EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0xC, 4), Endianness.LittleEndian)))
            {
                continue;
            }

            // fmt::print("SoundInfo valid\n");

            /* check CgbChan pointer (RAM addr) */
            if (!IsValidRamPointer(EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x10, 4), Endianness.LittleEndian)))
            {
                continue;
            }

            // fmt::print("CgbChan valid\n");

            /* check sound mode */
            int soundModePosCandidate = signaturePos + 0x14;
            uint soundModeCandidate = EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(soundModePosCandidate, 4), Endianness.LittleEndian);
            uint maxchn = (soundModeCandidate >> 8) & 0xF;
            uint freq = (soundModeCandidate >> 16) & 0xF;
            uint dac = (soundModeCandidate >> 20) & 0xF;
            if ((soundModeCandidate & 0xFF) != 0)    // reserved byte must be 0
            {
                continue;
            }
            if (maxchn < 1 || maxchn > 12)
            {
                continue;
            }
            if (freq == 0 || freq > 12)
            {
                continue;
            }
            if (dac < 8 || dac > 11)
            {
                continue;
            }

            // fmt::print("sound mode valid\n");

            /* check player table len */
            uint playerTableLen = EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x18, 4), Endianness.LittleEndian);
            if (playerTableLen > 32)
            {
                continue;
            }

            // fmt::print("player table len valid\n");

            /* check player table pos (probably redundant as it's an argument) */
            if (!GBAUtils.IsValidRomOffset(_rom, EndianBinaryPrimitives.ReadInt32(_rom.AsSpan(signaturePos + 0x1C, 4), Endianness.LittleEndian) - GBAUtils.CARTRIDGE_OFFSET))
            {
                continue;
            }

            // fmt::print("player table pos valid\n");

            /* check memacc address (TODO is this really the memacc address?) */
            if (!IsValidRamPointer(EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x20, 4), Endianness.LittleEndian)))
            {
                continue;
            }

            // fmt::print("memacc valid\n");

            soundModePos = soundModePosCandidate;
            soundMode = soundModeCandidate;
            return true;
        }

        return false;
    }

    private bool FindSoundModeMetroid(long playerTablePos, ref long soundModePos, ref uint soundMode)
    {
        int playerTableReferencePos = 0;
        int findStartPos = SEARCH_START;
        while (IsPosReferenced(playerTablePos, ref findStartPos, ref playerTableReferencePos))
        {
            /* If we have found a reference, check if the following pattern exists:
             * [0x00] sound initialized? (RAM-addr)
             * [0x04] REG_IE
             * [0x08] REG_SOUNDCNT_X
             * [0x0C] REG_SOUNDCNT_H
             * [0x10] SOUNDCNT_H init value
             * [0x14] REG_SOUNDBIAS (upper byte)
             * [0x18] REG_SOUND1CNT_H (upper byte)
             * [0x1C] REG_SOUNDCNT_L
             * [0x20] mix code-ptr (RAM-addr)
             * [0x24] mix code (RAM-addr)
             * [0x28] mix code (ROM-addr)
             * [0x2C] DMA ctrl data
             * [0x30] reverb code-ptr (RAM-addr)
             * [0x34] reverb code (RAM-addr)
             * [0x38] reverb code (ROM-addr)
             * [0x3C] DMA ctrl data
             * [0x40] downsampler code-ptr (RAM-addr)
             * [0x44] downsampler code (RAM-addr)
             * [0x48] downsampler code (ROM-addr)
             * [0x4C] DMA ctrl data
             * [0x50] other DMA ctrl data???
             * [0x54] ???
             * [0x58] player table len (0xNN, 0x00, 0x00, 0x00)
             * [0x5C] sound mode <---- data of interest
             * [0x60] ??? (RAM-addr)
             * [0x64] other DMA ctrl data???
             * [0x68] REG_DMA3SAD
             * [0x6C] player table pos (ROM-addr) <---- we pivot from this supplied address
             * [0x70] other DMA ctrl data???
             * [0x74] ??? ... This stuff seems to vary between Metroid Zero Mission and Wario Ware Twisted
             */

            if (playerTableReferencePos < 0x6C)
            {
                continue;
            }

            int signaturePos = playerTableReferencePos - 0x6C;

            if ((signaturePos + 0x78) > _rom.Length)
            {
                continue;
            }

            /* Check actual signature */

            /* [0x00] */
            if (!IsValidRamPointer(EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x0, 4), Endianness.LittleEndian)))
            {
                continue;
            }

            /* [0x04] */
            if (EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x4, 4), Endianness.LittleEndian) != 0x04000200)
            {
                continue;
            }

            /* [0x08] */
            if (EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x8, 4), Endianness.LittleEndian) != 0x04000084)
            {
                continue;
            }

            /* [0x0C] */
            if (EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0xC, 4), Endianness.LittleEndian) != 0x04000082)
            {
                continue;
            }

            /* [0x14] */
            if (EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x14, 4), Endianness.LittleEndian) != 0x04000089)
            {
                continue;
            }

            /* [0x18] */
            if (EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x18, 4), Endianness.LittleEndian) != 0x04000063)
            {
                continue;
            }

            /* [0x1C] */
            if (EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x1C, 4), Endianness.LittleEndian) != 0x04000080)
            {
                continue;
            }

            /* [0x20] */
            if (!IsValidRamPointer(EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x20, 4), Endianness.LittleEndian)))
            {
                continue;
            }

            /* [0x24] */
            if (!IsValidIwramPointer(EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x24, 4), Endianness.LittleEndian)))
            {
                continue;
            }

            /* [0x28] */
            if (!GBAUtils.IsValidRomOffset(_rom, EndianBinaryPrimitives.ReadInt32(_rom.AsSpan(signaturePos + 0x28, 4), Endianness.LittleEndian) - GBAUtils.CARTRIDGE_OFFSET))
            {
                continue;
            }

            /* [0x30] */
            if (!IsValidRamPointer(EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x30, 4), Endianness.LittleEndian)))
            {
                continue;
            }

            /* [0x34] */
            if (!IsValidIwramPointer(EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x34, 4), Endianness.LittleEndian)))
            {
                continue;
            }

            /* [0x38] */
            if (!GBAUtils.IsValidRomOffset(_rom, EndianBinaryPrimitives.ReadInt32(_rom.AsSpan(signaturePos + 0x38, 4), Endianness.LittleEndian) - GBAUtils.CARTRIDGE_OFFSET))
            {
                continue;
            }

            /* [0x40] */
            if (!IsValidRamPointer(EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x40, 4), Endianness.LittleEndian)))
            {
                continue;
            }

            /* [0x44] */
            if (!IsValidIwramPointer(EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x44, 4), Endianness.LittleEndian)))
            {
                continue;
            }

            /* [0x48] */
            if (!GBAUtils.IsValidRomOffset(_rom, EndianBinaryPrimitives.ReadInt32(_rom.AsSpan(signaturePos + 0x48, 4), Endianness.LittleEndian) - GBAUtils.CARTRIDGE_OFFSET))
            {
                continue;
            }

            /* [0x58] */
            uint playerTableLen = EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x58, 4), Endianness.LittleEndian);
            if (playerTableLen > 32)
            {
                continue;
            }

            /* [0x5C] */
            int soundModePosCandidate = signaturePos + 0x5C;
            uint soundModeCandidate = EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(soundModePosCandidate, 4), Endianness.LittleEndian);
            uint maxchn = (soundModeCandidate >> 8) & 0xF;
            uint freq = (soundModeCandidate >> 16) & 0xF;
            uint dac = (soundModeCandidate >> 20) & 0xF;
            /* Compared to "Normal" sound modes vs Metroid sound modes is, that the
             * reserved byte appears to be actually used. It does something with stereo/mono
             * switching, but we probably don't care about that in agbplay. */
            if (maxchn < 1 || maxchn > 12)
            {
                continue;
            }

            if (freq == 0 || freq > 12)
            {
                continue;
            }

            if (dac < 8 || dac > 11)
            {
                continue;
            }

            /* [0x60] */
            if (!IsValidRamPointer(EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x60, 4), Endianness.LittleEndian)))
            {
                continue;
            }

            /* [0x68] */
            if (EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(signaturePos + 0x68, 4), Endianness.LittleEndian) != 0x040000D4)
            {
                continue;
            }

            /* [0x6C] */
            /* check player table pos (probably redundant as it's an argument) */
            if (!GBAUtils.IsValidRomOffset(_rom, EndianBinaryPrimitives.ReadInt32(_rom.AsSpan(signaturePos + 0x6C, 4), Endianness.LittleEndian) - GBAUtils.CARTRIDGE_OFFSET))
            {
                continue;
            }

            soundModePos = soundModePosCandidate;
            soundMode = soundModeCandidate;
            return true;
        }

        return false;
    }

    private bool IsPosReferenced(long pos)
    {
        return _wordPointerCache.ContainsValue(pos + GBAUtils.CARTRIDGE_OFFSET);
    }

    private bool IsPosReferenced(long pos, ref int findStartPos, ref int referencePos)
    {
        bool foundReference = false;
        for (int j = findStartPos; j < _rom.Length - 3; j += 4)
        {
            int referenceCandidate = EndianBinaryPrimitives.ReadInt32(_rom.AsSpan(j, 4), Endianness.LittleEndian);
            if (!GBAUtils.IsValidRomOffset(_rom, referenceCandidate - GBAUtils.CARTRIDGE_OFFSET))
            {
                continue;
            }

            if (referenceCandidate - GBAUtils.CARTRIDGE_OFFSET != pos)
            {
                continue;
            }

            foundReference = true;
            referencePos = j;
            findStartPos = j + 4;
            break;
        }
        return foundReference;
    }

    private bool IsPosReferenced(ref List<int> poss, ref int index)
    {
        for (int j = SEARCH_START; j < _rom.Length - 3; j += 4)
        {
            int referenceCandidate = EndianBinaryPrimitives.ReadInt32(_rom.AsSpan(j, 4), Endianness.LittleEndian);
            if (!GBAUtils.IsValidRomOffset(_rom, referenceCandidate - GBAUtils.CARTRIDGE_OFFSET))
            {
                continue;
            }

            for (int i = 0; i < poss.Count; i++)
            {
                if (referenceCandidate - GBAUtils.CARTRIDGE_OFFSET == poss[i])
                {
                    index = i;
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsValidSongTableEntry(long pos)
    {
        // Make sure the range is valid
        if (!GBAUtils.IsValidRange(_rom, (int)pos, 8))
        {
            return false;
        }

        // 1. Validate pointer
        if (!GBAUtils.IsValidRomOffset(_rom, EndianBinaryPrimitives.ReadInt32(_rom.AsSpan((int)pos + 0, 4), Endianness.LittleEndian) - GBAUtils.CARTRIDGE_OFFSET))
        {
            return false;
        }

        // 2. Make sure the player numbers are valid
        // z1 and z2 must be 0, while p1 and p2 must have identical values
        byte p1 = _rom[pos + 4];
        byte z1 = _rom[pos + 5];
        byte p2 = _rom[pos + 6];
        byte z2 = _rom[pos + 7];

        if (z1 != 0 || z2 != 0 || p1 != p2)
        {
            return false;
        }

        // 3. Check if the song is valid
        // This value must use a ReadInt32, instead of trying to SanitizeOffset
        int songPos = EndianBinaryPrimitives.ReadInt32(_rom.AsSpan((int)pos + 0, 4), Endianness.LittleEndian) - GBAUtils.CARTRIDGE_OFFSET;

        if (songPos + 4 >= _rom.Length)
        {
            return false;
        }

        byte nTracks = _rom[songPos + 0];
        byte nBlocks = _rom[songPos + 1];    // this field is not used, should be 0
        byte prio = _rom[songPos + 2];
        byte rev = _rom[songPos + 3];

        // 3.1. Allow empty songs (songs with fields set to 0)
        if ((nTracks | nBlocks | prio | rev) == 0)
        {
            return true;
        }

        // 3.2. Validate voicegroup pointer
        if (!GBAUtils.IsValidRomOffset(_rom, EndianBinaryPrimitives.ReadInt32(_rom.AsSpan(songPos + 4, 4), Endianness.LittleEndian) - GBAUtils.CARTRIDGE_OFFSET))
        {
            return false;
        }

        // 3.3. Verify track pointers
        for (int i = 0; i < nTracks; i++)
        {
            if (!GBAUtils.IsValidRomOffset(_rom, EndianBinaryPrimitives.ReadInt32(_rom.AsSpan(songPos + 8 + (i * 4), 4), Endianness.LittleEndian) - GBAUtils.CARTRIDGE_OFFSET))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsValidPlayerTableEntry(int pos)
    {
        /* A player table entry usually looks like this:
         * - RAM pointer
         * - RAM pointer
         * - max track count
         * - unknown flag (0 or 1)
         *
         * Optionally, all fields may be zero.
         */
        uint playerPtr = EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(pos + 0, 4), Endianness.LittleEndian);
        uint trackPtr = EndianBinaryPrimitives.ReadUInt32(_rom.AsSpan(pos + 4, 4), Endianness.LittleEndian);
        ushort trackLimit = EndianBinaryPrimitives.ReadUInt16(_rom.AsSpan(pos + 8, 4), Endianness.LittleEndian);
        ushort unknown = EndianBinaryPrimitives.ReadUInt16(_rom.AsSpan(pos + 10, 4), Endianness.LittleEndian);

        if (playerPtr == 0 && trackPtr == 0 && trackLimit == 0 && unknown == 0)
        {
            return true;
        }

        if (!IsValidRamPointer(playerPtr))
        {
            return false;
        }

        if (!IsValidRamPointer(trackPtr))
        {
            return false;
        }

        if (trackLimit > 16)
        {
            return false;
        }

        if (unknown > 1)
        {
            return false;
        }

        return true;
    }

    private void InitPointerCache()
    {
        if (_wordPointerCache.Count > 0)
        {
            return;
        }

        _wordPointerCache.Clear();

        /* find list of all pointers in ROM */
        for (int i = SEARCH_START; i < _rom.Length - 3; i += 4)
        {
            int ptr = EndianBinaryPrimitives.ReadInt32(_rom.AsSpan(i, 4), Endianness.LittleEndian);
            if (GBAUtils.IsValidRomOffset(_rom, ptr) && (ptr % 4) == 0)
            {
                _wordPointerCache.Add(i, ptr);
            }
        }
    }

    private bool IsValidIwramPointer(uint word)
    {
        if (word >= 0x03000000 && word <= 0x03007FFF)
        {
            return true;
        }

        return false;
    }

    private bool IsValidEwramPointer(uint word)
    {
        if (word >= 0x02000000 && word <= 0x0203FFFF)
        {
            return true;
        }

        return false;
    }

    private bool IsValidRamPointer(uint word)
    {
        if (IsValidIwramPointer(word))
        {
            return true;
        }

        if (IsValidEwramPointer(word))
        {
            return true;
        }

        return false;
    }
}
