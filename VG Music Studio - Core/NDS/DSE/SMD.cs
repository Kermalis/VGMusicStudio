using Kermalis.EndianBinaryIO;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

internal sealed class SMD
{
	public sealed class Header // Size 0x40
	{
		public string Type { get; set; } // "smdb" or "smdl"
		public byte[] Unknown1 { get; set; }
		public uint Length { get; set; }
		public ushort Version { get; set; }
		public byte[] Unknown2 { get; set; }
		public ushort Year { get; set; }
		public byte Month { get; set; }
		public byte Day { get; set; }
		public byte Hour { get; set; }
		public byte Minute { get; set; }
		public byte Second { get; set; }
		public byte Centisecond { get; set; }
		public string Label { get; set; }
		public byte[] Unknown3 { get; set; }

		public Header(EndianBinaryReader r)
		{
			Type = r.ReadString_Count(4);

			if (Type == "smdb") { r.Endianness = Endianness.BigEndian; }

			Unknown1 = new byte[4];
			r.ReadBytes(Unknown1);

			Length = r.ReadUInt32();

			Version = r.ReadUInt16();

			Unknown2 = new byte[10];
			r.ReadBytes(Unknown2);

			r.Endianness = Endianness.LittleEndian;

			Year = r.ReadUInt16();

			Month = r.ReadByte();

			Day = r.ReadByte();

			Hour = r.ReadByte();

			Minute = r.ReadByte();

			Second = r.ReadByte();

			Centisecond = r.ReadByte();

			Label = r.ReadString_Count(16);

			Unknown3 = new byte[16];
			r.ReadBytes(Unknown3);

            if (Type == "smdb") { r.Endianness = Endianness.BigEndian; }
        }
	}

	public interface ISongChunk
	{
		byte NumTracks { get; }
	}
	public sealed class SongChunk : ISongChunk // Size 0x40
	{
		public string Type { get; set; }
		public byte[] Unknown1 { get; set; }
		public ushort TicksPerQuarter { get; set; }
		public byte[] Unknown2 { get; set; }
		public byte NumTracks { get; set; }
		public byte NumChannels { get; set; }
		public byte[] Unknown3 { get; set; }

		public SongChunk(EndianBinaryReader r)
		{
			Type = r.ReadString_Count(4);

			Unknown1 = new byte[14];
			r.ReadBytes(Unknown1);

			TicksPerQuarter = r.ReadUInt16();

			Unknown2 = new byte[2];
			r.ReadBytes(Unknown2);

			NumTracks = r.ReadByte();

			NumChannels = r.ReadByte();

			Unknown3 = new byte[40];
			r.ReadBytes(Unknown3);
		}
	}
}
