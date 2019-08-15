using Ionic.Crc;
using Ionic.Zlib;
using Kermalis.EndianBinaryIO;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.PSX.PSF
{
    internal class PSF
    {
        private const int ExeBufferSize = 0x200000;

        public string FilePath;
        public byte[] FileBytes;
        public byte[] DecompressedEXE;
        public string Tag;

        public static void Open(string fileName, out byte[] exeBuffer, out PSF psf)
        {
            exeBuffer = new byte[ExeBufferSize];
            psf = new PSF(fileName);
            LoadLib1(psf, exeBuffer);
            PlaceEXE(psf, exeBuffer);
            LoadLib2(psf, exeBuffer);
        }

        private PSF(string fileName)
        {
            FilePath = fileName;
            FileBytes = File.ReadAllBytes(fileName);
            using (var reader = new EndianBinaryReader(new MemoryStream(FileBytes)))
            {
                if (reader.ReadString(3) != "PSF")
                {
                    throw new InvalidDataException();
                }
                if (reader.ReadByte() != 1)
                {
                    throw new InvalidDataException();
                }
                uint reservedSize = reader.ReadUInt32();
                uint exeSize = reader.ReadUInt32();
                int checksum = reader.ReadInt32();
                byte[] exe = new byte[exeSize];
                Array.Copy(FileBytes, 0x10 + reservedSize, exe, 0, exeSize);
                var crc32 = new CRC32();
                if (crc32.GetCrc32(new MemoryStream(exe)) != checksum)
                {
                    throw new InvalidDataException();
                }
                DecompressedEXE = ZlibStream.UncompressBuffer(exe);
                uint tagOffset = 0x10 + reservedSize + exeSize;
                Tag = reader.ReadString((int)(FileBytes.Length - tagOffset), tagOffset);
            }
        }
        private static void LoadLib1(PSF psf, byte[] exeBuffer)
        {
            foreach (KeyValuePair<string, string> kvp in GetTags(psf.Tag))
            {
                if (kvp.Key == "_lib")
                {
                    var lib = new PSF(Path.Combine(Path.GetDirectoryName(psf.FilePath), kvp.Value));
                    LoadLib1(lib, exeBuffer);
                    LoadLib2(lib, exeBuffer);
                    PlaceEXE(lib, exeBuffer);
                    break;
                }
            }
        }
        private static void LoadLib2(PSF psf, byte[] exeBuffer)
        {
            bool cont = true;
            for (int i = 2; cont; i++)
            {
                cont = false;
                foreach (KeyValuePair<string, string> kvp in GetTags(psf.Tag))
                {
                    if (kvp.Key == $"_lib{i}")
                    {
                        var lib = new PSF(Path.Combine(Path.GetDirectoryName(psf.FilePath), kvp.Value));
                        LoadLib1(lib, exeBuffer);
                        LoadLib2(lib, exeBuffer);
                        PlaceEXE(lib, exeBuffer);
                        cont = true;
                        break;
                    }
                }
            }
        }
        private static void PlaceEXE(PSF psf, byte[] exeBuffer)
        {
            using (var reader = new EndianBinaryReader(new MemoryStream(psf.DecompressedEXE)))
            {
                uint textSectionStart = reader.ReadUInt32(0x18) & 0x3FFFFF;
                uint textSectionSize = reader.ReadUInt32();
                Array.Copy(psf.DecompressedEXE, 0x800, exeBuffer, textSectionStart, textSectionSize);
            }
        }
        private static Dictionary<string, string> GetTags(string tag)
        {
            string[] tags = tag.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var mapped = new Dictionary<string, string>();
            for (int i = 0; i < tags.Length - 1; i++)
            {
                string str = tags[i];
                int index = str.IndexOf('=');
                mapped.Add(str.Substring(0, index), str.Substring(index + 1, str.Length - index - 1));
            }
            return mapped;
        }
    }
}
