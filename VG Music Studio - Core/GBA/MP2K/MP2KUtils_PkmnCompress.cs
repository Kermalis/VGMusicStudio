using System;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

partial class MP2KUtils
{
	private static ReadOnlySpan<sbyte> CompressionLookup => new sbyte[16]
	{
		0, 1, 4, 9, 16, 25, 36, 49, -64, -49, -36, -25, -16, -9, -4, -1,
	};

	// TODO: Do runtime
	// TODO: How large is the decompress buffer in-game?
	public static sbyte[] Decompress(ReadOnlySpan<byte> src, int sampleLength)
	{
		var samples = new List<sbyte>();
		sbyte compressionLevel = 0;
		int compressionByte = 0, compressionIdx = 0;

		for (int i = 0; true; i++)
		{
			byte b = src[i];
			if (compressionByte == 0)
			{
				compressionByte = 0x20;
				compressionLevel = (sbyte)b;
				samples.Add(compressionLevel);
				if (++compressionIdx >= sampleLength)
				{
					break;
				}
			}
			else
			{
				if (compressionByte < 0x20)
				{
					compressionLevel += CompressionLookup[b >> 4];
					samples.Add(compressionLevel);
					if (++compressionIdx >= sampleLength)
					{
						break;
					}
				}
				// Potential for 2 samples to be added here at the same time
				compressionByte--;
				compressionLevel += CompressionLookup[b & 0xF];
				samples.Add(compressionLevel);
				if (++compressionIdx >= sampleLength)
				{
					break;
				}
			}
		}

		return samples.ToArray();
	}
}
