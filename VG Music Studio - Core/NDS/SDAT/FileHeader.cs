using Kermalis.EndianBinaryIO;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT;

public sealed class FileHeader
{
	public string FileType;
	public ushort FileEndianness;
	public ushort Version;
	public int FileSize;
	public ushort HeaderSize; // 16
	public ushort NumBlocks;

	public FileHeader(EndianBinaryReader er)
	{
		FileType = er.ReadString_Count(4);
		er.Endianness = Endianness.BigEndian;
		FileEndianness = er.ReadUInt16();
		er.Endianness = FileEndianness == 0xFFFE ? Endianness.LittleEndian : Endianness.BigEndian;
		Version = er.ReadUInt16();
		FileSize = er.ReadInt32();
		HeaderSize = er.ReadUInt16();
		NumBlocks = er.ReadUInt16();
	}
}
