using Kermalis.EndianBinaryIO;
using System;
#if !DEBUG
using System.Collections.Generic;
#endif
using System.Linq;

namespace Kermalis.DLS2
{
    public sealed class DLSID
    {
        public uint Data1 { get; set; }
        public ushort Data2 { get; set; }
        public ushort Data3 { get; set; }
        public byte[] Data4 { get; }

        public static DLSID Query_GMInHardware { get; } = new DLSID(0x178F2F24, 0xC364, 0x11D1, new byte[] { 0xA7, 0x60, 0x00, 0x00, 0xF8, 0x75, 0xAC, 0x12 });
        public static DLSID Query_GSInHardware { get; } = new DLSID(0x178F2F25, 0xC364, 0x11D1, new byte[] { 0xA7, 0x60, 0x00, 0x00, 0xF8, 0x75, 0xAC, 0x12 });
        public static DLSID Query_XGInHardware { get; } = new DLSID(0x178F2F26, 0xC364, 0x11D1, new byte[] { 0xA7, 0x60, 0x00, 0x00, 0xF8, 0x75, 0xAC, 0x12 });
        public static DLSID Query_SupportsDLS1 { get; } = new DLSID(0x178F2F27, 0xC364, 0x11D1, new byte[] { 0xA7, 0x60, 0x00, 0x00, 0xF8, 0x75, 0xAC, 0x12 });
        public static DLSID Query_SampleMemorySize { get; } = new DLSID(0x178F2F28, 0xC364, 0x11D1, new byte[] { 0xA7, 0x60, 0x00, 0x00, 0xF8, 0x75, 0xAC, 0x12 });
        public static DLSID Query_SamplePlaybackRate { get; } = new DLSID(0x2A91F713, 0xA4BF, 0x11D2, new byte[] { 0xBB, 0xDF, 0x00, 0x60, 0x08, 0x33, 0xDB, 0xD8 });
        public static DLSID Query_ManufacturersID { get; } = new DLSID(0xB03E1181, 0x8095, 0x11D2, new byte[] { 0xA1, 0xEF, 0x00, 0x60, 0x08, 0x33, 0xDB, 0xD8 });
        public static DLSID Query_ProductID { get; } = new DLSID(0xB03E1182, 0x8095, 0x11D2, new byte[] { 0xA1, 0xEF, 0x00, 0x60, 0x08, 0x33, 0xDB, 0xD8 });
        public static DLSID Query_SupportsDLS2 { get; } = new DLSID(0xF14599E5, 0x4689, 0x11D2, new byte[] { 0xAF, 0xA6, 0x00, 0xAA, 0x00, 0x24, 0xD8, 0xB6 });

        public DLSID()
        {
            Data4 = new byte[8];
        }
        internal DLSID(EndianBinaryReader reader)
        {
            Data1 = reader.ReadUInt32();
            Data2 = reader.ReadUInt16();
            Data3 = reader.ReadUInt16();
            Data4 = reader.ReadBytes(8);
        }
        public DLSID(uint data1, ushort data2, ushort data3, byte[] data4)
        {
            if (data4 is null)
            {
                throw new ArgumentNullException(nameof(data4));
            }
            if (data4.Length != 8)
            {
                throw new ArgumentOutOfRangeException(nameof(data4.Length));
            }
            Data1 = data1;
            Data2 = data2;
            Data3 = data3;
            Data4 = data4;
        }
        public DLSID(byte[] data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (data.Length != 16)
            {
                throw new ArgumentOutOfRangeException(nameof(data.Length));
            }
            Data1 = (uint)EndianBitConverter.BytesToInt32(data, 0, Endianness.LittleEndian);
            Data2 = (ushort)EndianBitConverter.BytesToInt16(data, 4, Endianness.LittleEndian);
            Data3 = (ushort)EndianBitConverter.BytesToInt16(data, 6, Endianness.LittleEndian);
            Data4 = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                Data4[i] = data[8 + i];
            }
        }

        public void Write(EndianBinaryWriter writer)
        {
            writer.Write(Data1);
            writer.Write(Data2);
            writer.Write(Data3);
            writer.Write(Data4);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this))
            {
                return true;
            }
            if (obj is DLSID id)
            {
                return id.Data1 == Data1 && id.Data2 == Data2 && id.Data3 == Data3 && id.Data4.SequenceEqual(Data4);
            }
            return false;
        }
        public override int GetHashCode()
        {
            // .NET Standard does not have this method
#if DEBUG
            return HashCode.Combine(Data1, Data2, Data3, Data4);
#else
            int hashCode = -0x8CAC62A;
            hashCode = hashCode * -0x5AAAAAD7 + Data1.GetHashCode();
            hashCode = hashCode * -0x5AAAAAD7 + Data2.GetHashCode();
            hashCode = hashCode * -0x5AAAAAD7 + Data3.GetHashCode();
            hashCode = hashCode * -0x5AAAAAD7 + EqualityComparer<byte[]>.Default.GetHashCode(Data4);
            return hashCode;
#endif
        }
        public override string ToString()
        {
            string str = Data1.ToString("X8") + '-' + Data2.ToString("X4") + '-' + Data3.ToString("X4") + '-';
            for (int i = 0; i < 2; i++)
            {
                str += Data4[i].ToString("X2");
            }
            str += '-';
            for (int i = 2; i < 8; i++)
            {
                str += Data4[i].ToString("X2");
            }
            return str;
        }
    }
}
