using Kermalis.EndianBinaryIO;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE;

internal sealed class SMD
{
	public sealed class Header // Size 0x40
	{
		[BinaryStringFixedLength(4)]
		public string Type { get; set; } = null!; // "smdb" or "smdl"
		[BinaryArrayFixedLength(4)]
		public byte[] Unknown1 { get; set; } = null!;
		public uint Length { get; set; }
		public ushort Version { get; set; }
		[BinaryArrayFixedLength(10)]
		public byte[] Unknown2 { get; set; } = null!;
		public ushort Year { get; set; }
		public byte Month { get; set; }
		public byte Day { get; set; }
		public byte Hour { get; set; }
		public byte Minute { get; set; }
		public byte Second { get; set; }
		public byte Centisecond { get; set; }
		[BinaryStringFixedLength(16)]
		public string Label { get; set; } = null!;
		[BinaryArrayFixedLength(16)]
		public byte[] Unknown3 { get; set; } = null!;
	}

	public interface ISongChunk
	{
		byte NumTracks { get; }
	}
	public sealed class SongChunk_V402 : ISongChunk // Size 0x20
	{
		[BinaryStringFixedLength(4)]
		public string Type { get; set; } = null!;
		[BinaryArrayFixedLength(16)]
		public byte[] Unknown1 { get; set; } = null!;
		public byte NumTracks { get; set; }
		public byte NumChannels { get; set; }
		[BinaryArrayFixedLength(4)]
		public byte[] Unknown2 { get; set; } = null!;
		public sbyte MasterVolume { get; set; }
		public sbyte MasterPanpot { get; set; }
		[BinaryArrayFixedLength(4)]
		public byte[] Unknown3 { get; set; } = null!;
	}
	public sealed class SongChunk_V415 : ISongChunk // Size 0x40
	{
		[BinaryStringFixedLength(4)]
		public string Type { get; set; } = null!;
		[BinaryArrayFixedLength(18)]
		public byte[] Unknown1 { get; set; } = null!;
		public byte NumTracks { get; set; }
		public byte NumChannels { get; set; }
		[BinaryArrayFixedLength(40)]
		public byte[] Unknown2 { get; set; } = null!;
	}
}
