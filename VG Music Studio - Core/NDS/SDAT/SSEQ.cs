using Kermalis.EndianBinaryIO;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT
{
	internal sealed class SSEQ
	{
		public FileHeader FileHeader; // "SSEQ"
		public string BlockType; // "DATA"
		public int BlockSize;
		public int DataOffset;

		public byte[] Data;

		public SSEQ(byte[] bytes)
		{
			using (var stream = new MemoryStream(bytes))
			{
				var er = new EndianBinaryReader(stream, ascii: true);
				FileHeader = new FileHeader(er);
				BlockType = er.ReadString_Count(4);
				BlockSize = er.ReadInt32();
				DataOffset = er.ReadInt32();

				Data = new byte[FileHeader.FileSize - DataOffset];
				stream.Position = DataOffset;
				er.ReadBytes(Data);
			}
		}
	}
}
