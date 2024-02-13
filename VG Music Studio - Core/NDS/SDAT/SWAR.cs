using Kermalis.EndianBinaryIO;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT;

internal sealed class SWAR
{
	public sealed class SWAV
	{
		public SWAVFormat Format;
		public bool DoesLoop;
		public ushort SampleRate;
		/// <summary><see cref="NDSUtils.ARM7_CLOCK"/> / <see cref="SampleRate"/></summary>
		public ushort Timer;
		public ushort LoopOffset;
		public int Length;

		public byte[] Samples;

		public SWAV(EndianBinaryReader er)
		{
			Format = er.ReadEnum<SWAVFormat>();
			DoesLoop = er.ReadBoolean();
			SampleRate = er.ReadUInt16();
			Timer = er.ReadUInt16();
			LoopOffset = er.ReadUInt16();
			Length = er.ReadInt32();

			Samples = new byte[(LoopOffset * 4) + (Length * 4)];
			er.ReadBytes(Samples);
		}
	}

	public SDATFileHeader FileHeader; // "SWAR"
	public string BlockType; // "DATA"
	public int BlockSize;
	public byte[] Padding;
	public int NumWaves;
	public int[] WaveOffsets;

	public SWAV[] Waves;

	public SWAR(byte[] bytes)
	{
		using (var stream = new MemoryStream(bytes))
		{
			var er = new EndianBinaryReader(stream, ascii: true);
			FileHeader = new SDATFileHeader(er);
			BlockType = er.ReadString_Count(4);
			BlockSize = er.ReadInt32();
			Padding = new byte[32];
			er.ReadBytes(Padding);
			NumWaves = er.ReadInt32();
			WaveOffsets = new int[NumWaves];
			er.ReadInt32s(WaveOffsets);

			Waves = new SWAV[NumWaves];
			for (int i = 0; i < NumWaves; i++)
			{
				stream.Position = WaveOffsets[i];
				Waves[i] = new SWAV(er);
			}
		}
	}
}
