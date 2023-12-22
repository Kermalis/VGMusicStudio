using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Kermalis.VGMusicStudio.Core.Codec;

internal struct DSPADPCM
{
	public static short Min = short.MinValue;
	public static short Max = short.MaxValue;

	public static double[] Tvec = new double[3];

	public DSPADPCMInfo[] Info;
	private readonly ushort NumChannels;
	private ushort Channel;
	public byte[] Data;
	public short[]? DataOutput;

	private int DataOffset;
	private int SamplePos;
	public int Scale;
	public byte ByteValue;
	public int FrameOffset;
	public int[]? Coef1;
	public int[]? Coef2;
	public short[]? Hist1;
	public short[]? Hist2;
	private int Nibble;

	public static class DSPADPCMConstants
	{
		public const int BytesPerFrame = 8;
		public const int SamplesPerFrame = 14;
		public const int NibblesPerFrame = 16;
	}

	public DSPADPCM(EndianBinaryReader r, ushort? numChannels)
	{
		_ = numChannels == null ? NumChannels = 1 : NumChannels = (ushort)numChannels; // The number of waveform channels
		Info = new DSPADPCMInfo[NumChannels];
		for (int i = 0; i < NumChannels; i++)
		{
			Info[i] = new DSPADPCMInfo(r); // First, the 96 byte-long DSP-ADPCM header table is read and each variable is assigned with a value
		}
		Data = new byte[DataUtils.RoundUp((int)Info[0].NumAdpcmNibbles / 2, 16) * NumChannels]; // Next, this allocates the full size of the data, based on NumAdpcmNibbles divided by 2 and rounded up to the 16th byte
		DataOutput = new short[Info[0].NumSamples * NumChannels]; // This will allocate the size of the DataOutput array based on the value in NumSamples
		
		r.ReadBytes(Data); // This reads the compressed sample data based on the size allocated
		r.Stream.Align(16); // This will align the EndianBinaryReader stream offset to the 16th byte, since all sample data ends at every 16th byte
		return;
	}

	#region DSP-ADPCM Info
	public interface IDSPADPCMInfo
	{
		public short[] Coef { get; }
		public ushort Gain { get; }
		public ushort PredScale { get; }
		public short Yn1 { get; }
		public short Yn2 { get; }

		public ushort LoopPredScale { get; }
		public short LoopYn1 { get; }
		public short LoopYn2 { get; }
	}

	public class DSPADPCMInfo : IDSPADPCMInfo
	{
		public uint NumSamples { get; set; }
		public uint NumAdpcmNibbles { get; set; }
		public uint SampleRate { get; set; }
		public ushort LoopFlag { get; set; }
		public ushort Format { get; set; }
		public uint Sa { get; set; }
		public uint Ea { get; set; }
		public uint Ca { get; set; }
		public short[] Coef { get; set; }
		public ushort Gain { get; set; }
		public ushort PredScale { get; set; }
		public short Yn1 { get; set; }
		public short Yn2 { get; set; }

		public ushort LoopPredScale { get; set; }
		public short LoopYn1 { get; set; }
		public short LoopYn2 { get; set; }
		public ushort[] Padding { get; set; }

		public DSPADPCMInfo(EndianBinaryReader r)
		{
			NumSamples = r.ReadUInt32();

			NumAdpcmNibbles = r.ReadUInt32();

			SampleRate = r.ReadUInt32();

			LoopFlag = r.ReadUInt16();

			Format = r.ReadUInt16();

			Sa = r.ReadUInt32();

			Ea = r.ReadUInt32();

			Ca = r.ReadUInt32();

			Coef = new short[16];
			r.ReadInt16s(Coef);

			Gain = r.ReadUInt16();

			PredScale = r.ReadUInt16();

			Yn1 = r.ReadInt16();

			Yn2 = r.ReadInt16();

			LoopPredScale = r.ReadUInt16();

			LoopYn1 = r.ReadInt16();

			LoopYn2 = r.ReadInt16();

			Padding = new ushort[11];
			r.ReadUInt16s(Padding);
		}

		public byte[] ToBytes()
		{
			_ = new byte[96];
			var numSamples = new byte[4];
			var numAdpcmNibbles = new byte[4];
			var sampleRate = new byte[4];
			var loopFlag = new byte[2];
			var format = new byte[2];
			var sa = new byte[4];
			var ea = new byte[4];
			var ca = new byte[4];
			var coef = new byte[32];
			var gain = new byte[2];
			var predScale = new byte[2];
			var yn1 = new byte[2];
			var yn2 = new byte[2];
			var loopPredScale = new byte[2];
			var loopYn1 = new byte[2];
			var loopYn2 = new byte[2];
			var padding = new byte[22];

			BinaryPrimitives.WriteUInt32BigEndian(numSamples, NumSamples);
			BinaryPrimitives.WriteUInt32BigEndian(numAdpcmNibbles, NumAdpcmNibbles);
			BinaryPrimitives.WriteUInt32BigEndian(sampleRate, SampleRate);
			BinaryPrimitives.WriteUInt16BigEndian(loopFlag, LoopFlag);
			BinaryPrimitives.WriteUInt16BigEndian(format, Format);
			BinaryPrimitives.WriteUInt32BigEndian(sa, Sa);
			BinaryPrimitives.WriteUInt32BigEndian(ea, Ea);
			BinaryPrimitives.WriteUInt32BigEndian(ca, Ca);
			int index = 0;
			for (int i = 0; i < 16; i++)
			{
				coef[index++] = (byte)(Coef[i] >> 8);
				coef[index++] = (byte)(Coef[i] & 0xff);
			}
			BinaryPrimitives.WriteUInt16BigEndian(gain, Gain);
			BinaryPrimitives.WriteUInt16BigEndian(predScale, PredScale);
			BinaryPrimitives.WriteInt16BigEndian(yn1, Yn1);
			BinaryPrimitives.WriteInt16BigEndian(yn2, Yn2);
			BinaryPrimitives.WriteUInt16BigEndian(loopPredScale, LoopPredScale);
			BinaryPrimitives.WriteInt16BigEndian(loopYn1, LoopYn1);
			BinaryPrimitives.WriteInt16BigEndian(loopYn2, LoopYn2);

			// A collection expression is used for combining all byte arrays into one, instead of using the Concat() function
			byte[]? bytes = [.. numSamples, .. numAdpcmNibbles, .. sampleRate,
				.. loopFlag, .. format, .. sa, .. ea, .. ca, .. coef, .. gain,
				.. predScale, .. yn1, .. yn2, .. loopPredScale, .. loopYn1, .. loopYn2, .. padding];

			return bytes;
		}
	}
	#endregion

	#region DSP-ADPCM Convert

	public static int NibblesToSamples(int nibbles)
	{
		var fullFrames = nibbles / 16;
		var remainder = nibbles % 16;

		return remainder > 0 ? (fullFrames * 14) + remainder - 2 : fullFrames * 14;
	}

	public static int BytesToSamples(int bytes, int channels)
	{
		return channels <= 0 ? 0 : (bytes / channels) / (8 * 14);
	}

	public readonly byte[] InfoToBytes()
	{
		var info = new DSPADPCMInfo[NumChannels];
		var infoData = new byte[96 * NumChannels];
		for (int i = 0; i < NumChannels; i++)
		{
			Array.Copy(info[i].ToBytes(), infoData, 96 * (i + 1));
		}
		return infoData;
	}

	public readonly byte[] DataOutputToBytes()
	{
		int index = 0;
		var data = new byte[DataOutput!.Length * 2];
		for (int i = 0; i < DataOutput.Length; i++)
		{
			data[index++] = (byte)(DataOutput[i] >> 8);
			data[index++] = (byte)DataOutput[i];
		}
		return data;
	}

	public readonly byte[] ConvertToWav()
	{
		// Creating the RIFF Wave header
		string fileID = "RIFF";
		uint fileSize = (uint)((DataOutput!.Length * 2) + 44); // File size must match the size of the samples and header size
		string waveID = "WAVE";
		string formatID = "fmt ";
		uint formatLength = 16; // Always a length 16
		ushort formatType = 1; // Always PCM16
		// Number of channels is already manually defined
		uint sampleRate = Info[0].SampleRate; // Sample Rate is read directly from the Info context
		ushort bitsPerSample = 16; // bitsPerSample must be written to AFTER numNibbles
		uint numNibbles = sampleRate * bitsPerSample * Channel / 8; // numNibbles must be written BEFORE bitsPerSample is written
		ushort bitRate = (ushort)((bitsPerSample * Channel) / 8);
		string dataID = "data";
		uint dataSize = (uint)(DataOutput!.Length * 2);

		var convertedData = new byte[dataSize];
		int index = 0;
		for (int i = 0; i < DataOutput!.Length; i++)
		{
			convertedData[index++] = (byte)(DataOutput![i] & 0xff);
			convertedData[index++] = (byte)(DataOutput![i] >> 8);
		}

		_ = new byte[4];
		var header2 = new byte[4];
		_ = new byte[4];
		_ = new byte[4];
		var header5 = new byte[4];
		var header6 = new byte[2];
		var header7 = new byte[2];
		var header8 = new byte[4];
		var header9 = new byte[4];
		var header10 = new byte[2];
		var header11 = new byte[2];
		_ = new byte[4];
		var header13 = new byte[4];

		byte[]? header1 = Encoding.ASCII.GetBytes(fileID);
		BinaryPrimitives.WriteUInt32LittleEndian(header2, fileSize);
		byte[]? header3 = Encoding.ASCII.GetBytes(waveID);
		byte[]? header4 = Encoding.ASCII.GetBytes(formatID);
		BinaryPrimitives.WriteUInt32LittleEndian(header5, formatLength);
		BinaryPrimitives.WriteUInt16LittleEndian(header6, formatType);
		BinaryPrimitives.WriteUInt16LittleEndian(header7, NumChannels);
		BinaryPrimitives.WriteUInt32LittleEndian(header8, sampleRate);
		BinaryPrimitives.WriteUInt32LittleEndian(header9, numNibbles);
		BinaryPrimitives.WriteUInt16LittleEndian(header10, bitRate);
		BinaryPrimitives.WriteUInt16LittleEndian(header11, bitsPerSample);
		byte[]? header12 = Encoding.ASCII.GetBytes(dataID);
		BinaryPrimitives.WriteUInt32LittleEndian(header13, dataSize);

		_ = new byte[44];
		byte[]? header = [ // Using this instead of the Concat() function, which does the exact same task of adding data into the array
			.. header1, .. header2, .. header3,
			.. header4, .. header5, .. header6,
			.. header7, .. header8, .. header9,
			.. header10, .. header11, .. header12, .. header13];
		_ = new byte[fileSize];
		byte[]? waveData = [.. header, .. convertedData];


		return waveData;
	}

	#endregion

	#region DSP-ADPCM Encode

	public static void Encode(Span<short> src, Span<byte> dst, DSPADPCMInfo cxt, uint samples)
	{
		Span<short> coefs = cxt.Coef;
		CorrelateCoefs(src, samples, coefs);

		int frameCount = (int)((samples / DSPADPCMConstants.SamplesPerFrame) + (samples % DSPADPCMConstants.SamplesPerFrame));

		Span<short> pcm = src;
		Span<byte> adpcm = dst;
		Span<short> pcmFrame = new short[DSPADPCMConstants.SamplesPerFrame + 2];
		Span<byte> adpcmFrame = new byte[DSPADPCMConstants.BytesPerFrame];

		short srcIndex = 0;
		short dstIndex = 0;

		for (int i = 0; i < frameCount; ++i, pcm[srcIndex] += DSPADPCMConstants.SamplesPerFrame, adpcm[srcIndex] += DSPADPCMConstants.BytesPerFrame)
		{
			coefs = new short[2 + 0];

			DSPEncodeFrame(pcmFrame, DSPADPCMConstants.SamplesPerFrame, adpcmFrame, coefs);

			pcmFrame[0] = pcmFrame[14];
			pcmFrame[1] = pcmFrame[15];
		}

		cxt.Gain = 0;
		cxt.PredScale = dst[dstIndex++];
		cxt.Yn1 = 0;
		cxt.Yn2 = 0;
	}

	public static void InnerProductMerge(Span<double> vecOut, Span<short> pcmBuf)
	{
		pcmBuf = new short[14].AsSpan();
		vecOut = Tvec;

		for (int i = 0; i <= 2; i++)
		{
			vecOut[i] = 0.0f;
			for (int x = 0; x < 14; x++)
				vecOut[i] -= pcmBuf[x - i] * pcmBuf[x];

		}
	}

	public static void OuterProductMerge(Span<double> mtxOut, Span<short> pcmBuf)
	{
		pcmBuf = new short[14].AsSpan();
		mtxOut[3] = Tvec[3];

		for (int x = 1; x <= 2; x++)
			for (int y = 1; y <= 2; y++)
			{
				mtxOut[x] = 0.0;
				mtxOut[y] = 0.0;
				for (int z = 0; z < 14; z++)
					mtxOut[x + y] += pcmBuf[z - x] * pcmBuf[z - y];
			}
	}

	public static bool AnalyzeRanges(Span<double> mtx, Span<int> vecIdxsOut)
	{
		mtx[3] = Tvec[3];
		Span<double> recips = new double[3].AsSpan();
		double val, tmp, min, max;

		/* Get greatest distance from zero */
		for (int x = 1; x <= 2; x++)
		{
			val = Math.Max(Math.Abs(mtx[x] + mtx[1]), Math.Abs(mtx[x] + mtx[2]));
			if (val < double.Epsilon)
				return true;

			recips[x] = 1.0 / val;
		}

		int maxIndex = 0;
		for (int i = 1; i <= 2; i++)
		{
			for (int x = 1; x < i; x++)
			{
				tmp = mtx[x] + mtx[i];
				for (int y = 1; y < x; y++)
					tmp -= (mtx[x] + mtx[y]) * (mtx[y] + mtx[i]);
				mtx[x + i] = tmp;
			}

			val = 0.0;
			for (int x = i; x <= 2; x++)
			{
				tmp = mtx[x] + mtx[i];
				for (int y = 1; y < i; y++)
					tmp -= (mtx[x] + mtx[y]) * (mtx[y] + mtx[i]);

				mtx[x + i] = tmp;
				tmp = Math.Abs(tmp) * recips[x];
				if (tmp >= val)
				{
					val = tmp;
					maxIndex = x;
				}
			}

			if (maxIndex != i)
			{
				for (int y = 1; y <= 2; y++)
				{
					tmp = mtx[maxIndex] + mtx[y];
					mtx[maxIndex + y] = mtx[i] + mtx[y];
					mtx[i + y] = tmp;
				}
				recips[maxIndex] = recips[i];
			}

			vecIdxsOut[i] = maxIndex;

			if (mtx[i] + mtx[i] == 0.0)
				return true;

			if (i != 2)
			{
				tmp = 1.0 / mtx[i] + mtx[i];
				for (int x = i + 1; x <= 2; x++)
					mtx[x + i] *= tmp;
			}
		}

		/* Get range */
		min = 1.0e10;
		max = 0.0;
		for (int i = 1; i <= 2; i++)
		{
			tmp = Math.Abs(mtx[i] + mtx[i]);
			if (tmp < min)
				min = tmp;
			if (tmp > max)
				max = tmp;
		}

		if (min / max < 1.0e-10)
			return true;

		return false;
	}

	public static void BidirectionalFilter(Span<double> mtx, Span<int> vecIdxs, Span<double> vecOut)
	{
		mtx[3] = Tvec[3];
		vecOut = Tvec;
		double tmp;

		for (int i = 1, x = 0; i <= 2; i++)
		{
			int index = vecIdxs[i];
			tmp = vecOut[index];
			vecOut[index] = vecOut[i];
			if (x != 0)
				for (int y = x; y <= i - 1; y++)
					tmp -= vecOut[y] * mtx[i] + mtx[y];
			else if (tmp != 0.0)
				x = i;
			vecOut[i] = tmp;
		}

		for (int i = 2; i > 0; i--)
		{
			tmp = vecOut[i];
			for (int y = i + 1; y <= 2; y++)
				tmp -= vecOut[y] * mtx[i] + mtx[y];
			vecOut[i] = tmp / mtx[i] + mtx[i];
		}

		vecOut[0] = 1.0;
	}

	public static bool QuadraticMerge(Span<double> inOutVec)
	{
		inOutVec = Tvec;

		double v0, v1, v2 = inOutVec[2];
		double tmp = 1.0 - (v2 * v2);

		if (tmp == 0.0)
			return true;

		v0 = (inOutVec[0] - (v2 * v2)) / tmp;
		v1 = (inOutVec[1] - (inOutVec[1] * v2)) / tmp;

		inOutVec[0] = v0;
		inOutVec[1] = v1;

		return Math.Abs(v1) > 1.0;
	}

	public static void FinishRecord(Span<double> vIn, Span<double> vOut)
	{
		vIn = Tvec;
		vOut = Tvec;
		for (int z = 1; z <= 2; z++)
		{
			if (vIn[z] >= 1.0)
				vIn[z] = 0.9999999999;

			else if (vIn[z] <= -1.0)
				vIn[z] = -0.9999999999;
		}
		vOut[0] = 1.0;
		vOut[1] = (vIn[2] * vIn[1]) + vIn[1];
		vOut[2] = vIn[2];
	}

	public static void MatrixFilter(Span<double> src, Span<double> dst)
	{
		src = Tvec;
		dst = Tvec;
		double[] mtx = new double[3];
		Tvec = mtx;

		mtx[2 + 0] = 1.0;
		for (int i = 1; i <= 2; i++)
			mtx[2 + i] = -src[i];

		for (int i = 2; i > 0; i--)
		{
			double val = 1.0 - ((mtx[i] + mtx[i]) * (mtx[i] + mtx[i]));
			for (int y = 1; y <= i; y++)
				mtx[i - 1 + y] = (((mtx[i] + mtx[i]) * (mtx[i] + mtx[y])) + mtx[i] + mtx[y]) / val;
		}

		dst[0] = 1.0;
		for (int i = 1; i <= 2; i++)
		{
			dst[i] = 0.0;
			for (int y = 1; y <= i; y++)
				dst[i] += (mtx[i] + mtx[y]) * dst[i - y];
		}
	}

	public static void MergeFinishRecord(Span<double> src, Span<double> dst)
	{
		src = Tvec;
		dst = Tvec;
		int dstIndex = 0;
		Span<double> tmp = new double[dstIndex].AsSpan();
		double val = src[0];

		dst[0] = 1.0;
		for (int i = 1; i <= 2; i++)
		{
			double v2 = 0.0;
			for (int y = 1; y < i; y++)
				v2 += dst[y] * src[i - y];

			if (val > 0.0)
				dst[i] = -(v2 + src[i]) / val;
			else
				dst[i] = 0.0;

			tmp[i] = dst[i];

			for (int y = 1; y < i; y++)
				dst[y] += dst[i] * dst[i - y];

			val *= 1.0 - (dst[i] * dst[i]);
		}

		FinishRecord(tmp, dst);
	}

	public static double ContrastVectors(Span<double> source1, Span<double> source2)
	{
		source1 = Tvec;
		source2 = Tvec;
		double val = (source2[2] * source2[1] + -source2[1]) / (1.0 - source2[2] * source2[2]);
		double val1 = (source1[0] * source1[0]) + (source1[1] * source1[1]) + (source1[2] * source1[2]);
		double val2 = (source1[0] * source1[1]) + (source1[1] * source1[2]);
		double val3 = source1[0] * source1[2];
		return val1 + (2.0 * val * val2) + (2.0 * (-source2[1] * val + -source2[2]) * val3);
	}

	public static void FilterRecords(Span<double> vecBest, int exp, Span<double> records, int recordCount)
	{
		vecBest[8] = Tvec[8];
		records = Tvec;
		Span<double> bufferList = new double[8].AsSpan();
		bufferList[8] = Tvec[8];

		Span<int> buffer1 = new int[8].AsSpan();
		Span<double> buffer2 = Tvec;

		int index;
		double value, tempVal = 0;

		for (int x = 0; x < 2; x++)
		{
			for (int y = 0; y < exp; y++)
			{
				buffer1[y] = 0;
				for (int i = 0; i <= 2; i++)
					bufferList[y + i] = 0.0;
			}
			for (int z = 0; z < recordCount; z++)
			{
				index = 0;
				value = 1.0e30;
				for (int i = 0; i < exp; i++)
				{
					vecBest = new double[i].AsSpan();
					records = new double[z].AsSpan();
					tempVal = ContrastVectors(vecBest, records);
					if (tempVal < value)
					{
						value = tempVal;
						index = i;
					}
				}
				buffer1[index]++;
				MatrixFilter(records, buffer2);
				for (int i = 0; i <= 2; i++)
					bufferList[index + i] += buffer2[i];
			}

			for (int i = 0; i < exp; i++)
				if (buffer1[i] > 0)
					for (int y = 0; y <= 2; y++)
						bufferList[i + y] /= buffer1[i];

			for (int i = 0; i < exp; i++)
				bufferList = new double[i];
			MergeFinishRecord(bufferList, vecBest);
		}
	}

	public static void CorrelateCoefs(Span<short> source, uint samples, Span<short> coefsOut)
	{
		int numFrames = (int)((samples + 13) / 14);
		int frameSamples;

		Span<short> blockBuffer = new short[0x3800].AsSpan();
		Span<short> pcmHistBuffer = new short[2 + 14].AsSpan();

		Span<double> vec1 = Tvec;
		Span<double> vec2 = Tvec;

		Span<double> mtx = Tvec;
		mtx[3] = Tvec[3];
		Span<int> vecIdxs = new int[3].AsSpan();

		Span<double> records = new double[numFrames * 2].AsSpan();
		records = Tvec;
		int recordCount = 0;

		Span<double> vecBest = new double[8].AsSpan();
		vecBest[8] = Tvec[8];

		int sourceIndex = 0;

		/* Iterate though 1024-block frames */
		for (int x = (int)samples; x > 0;)
		{
			if (x > 0x3800) /* Full 1024-block frame */
			{
				frameSamples = 0x3800;
				x -= 0x3800;
			}
			else /* Partial frame */
			{
				/* Zero lingering block samples */
				frameSamples = x;
				for (int z = 0; z < 14 && z + frameSamples < 0x3800; z++)
					blockBuffer[frameSamples + z] = 0;
				x = 0;
			}

			/* Copy (potentially non-frame-aligned PCM samples into aligned buffer) */
			source[sourceIndex] += (short)frameSamples;


			for (int i = 0; i < frameSamples;)
			{
				for (int z = 0; z < 14; z++)
					pcmHistBuffer[0 + z] = pcmHistBuffer[1 + z];
				for (int z = 0; z < 14; z++)
					pcmHistBuffer[1 + z] = blockBuffer[i++];

				pcmHistBuffer = new short[1].AsSpan();

				InnerProductMerge(vec1, pcmHistBuffer);
				if (Math.Abs(vec1[0]) > 10.0)
				{
					OuterProductMerge(mtx, pcmHistBuffer);
					if (!AnalyzeRanges(mtx, vecIdxs))
					{
						BidirectionalFilter(mtx, vecIdxs, vec1);
						if (!QuadraticMerge(vec1))
						{
							records = new double[recordCount].AsSpan();
							FinishRecord(vec1, records);
							recordCount++;
						}
					}
				}
			}
		}

		vec1[0] = 1.0;
		vec1[1] = 0.0;
		vec1[2] = 0.0;

		for (int z = 0; z < recordCount; z++)
		{
			records = new double[z].AsSpan();
			vecBest = new double[0].AsSpan();
			MatrixFilter(records, vecBest);
			for (int y = 1; y <= 2; y++)
				vec1[y] += vecBest[0] + vecBest[y];
		}
		for (int y = 1; y <= 2; y++)
			vec1[y] /= recordCount;

		MergeFinishRecord(vec1, vecBest);


		int exp = 1;
		for (int w = 0; w < 3;)
		{
			vec2[0] = 0.0;
			vec2[1] = -1.0;
			vec2[2] = 0.0;
			for (int i = 0; i < exp; i++)
				for (int y = 0; y <= 2; y++)
					vecBest[exp + i + y] = (0.01 * vec2[y]) + vecBest[i] + vecBest[y];
			++w;
			exp = 1 << w;
			FilterRecords(vecBest, exp, records, recordCount);
		}

		/* Write output */
		for (int z = 0; z < 8; z++)
		{
			double d;
			d = -vecBest[z] + vecBest[1] * 2048.0;
			if (d > 0.0)
				coefsOut[z * 2] = (d > 32767.0) ? (short)32767 : (short)Math.Round(d);
			else
				coefsOut[z * 2] = (d < -32768.0) ? (short)-32768 : (short)Math.Round(d);

			d = -vecBest[z] + vecBest[2] * 2048.0;
			if (d > 0.0)
				coefsOut[z * 2 + 1] = (d > 32767.0) ? (short)32767 : (short)Math.Round(d);
			else
				coefsOut[z * 2 + 1] = (d < -32768.0) ? (short)-32768 : (short)Math.Round(d);
		}
	}

	/* Make sure source includes the yn values (16 samples total) */
	public static void DSPEncodeFrame(Span<short> pcmInOut, int sampleCount, Span<byte> adpcmOut, Span<short> coefsIn)
	{
		pcmInOut = new short[16].AsSpan();
		adpcmOut = new byte[8].AsSpan();
		coefsIn = new short[8].AsSpan();
		coefsIn = new short[2].AsSpan();

		Span<int> inSamples = new int[8].AsSpan();
		inSamples = new int[16].AsSpan();
		Span<int> outSamples = new int[8].AsSpan();
		outSamples = new int[14].AsSpan();

		int bestIndex = 0;

		Span<int> scale = new int[8].AsSpan();
		Span<double> distAccum = new double[8].AsSpan();

		/* Iterate through each coef set, finding the set with the smallest error */
		for (int i = 0; i < 8; i++)
		{
			int v1, v2, v3;
			int distance, index;

			/* Set yn values */
			inSamples[i + 0] = pcmInOut[0];
			inSamples[i + 1] = pcmInOut[1];

			/* Round and clamp samples for this coef set */
			distance = 0;
			for (int s = 0; s < sampleCount; s++)
			{
				/* Multiply previous samples by coefs */
				inSamples[i + (s + 2)] = v1 = ((pcmInOut[s] * (coefsIn[i] + coefsIn[1])) + (pcmInOut[s + 1] * (coefsIn[i] + coefsIn[0]))) / 2048;
				/* Subtract from current sample */
				v2 = pcmInOut[s + 2] - v1;
				/* Clamp */
				v3 = (v2 >= 32767) ? 32767 : (v2 <= -32768) ? -32768 : v2;
				/* Compare distance */
				if (Math.Abs(v3) > Math.Abs(distance))
					distance = v3;
			}

			/* Set initial scale */
			for (scale[i] = 0; (scale[i] <= 12) && ((distance > 7) || (distance < -8)); scale[i]++, distance /= 2)
			{
			}
			scale[i] = (scale[i] <= 1) ? -1 : scale[i] - 2;

			do
			{
				scale[i]++;
				distAccum[i] = 0;
				index = 0;

				for (int s = 0; s < sampleCount; s++)
				{
					/* Multiply previous */
					v1 = (((inSamples[i] + inSamples[s]) * (coefsIn[i] + coefsIn[1])) + ((inSamples[i] + inSamples[s + 1]) * (coefsIn[i] + coefsIn[0])));
					/* Evaluate from real sample */
					v2 = (pcmInOut[s + 2] << 11) - v1;
					/* Round to nearest sample */
					v3 = (v2 > 0) ? (int)((double)v2 / (1 << scale[i]) / 2048 + 0.4999999f) : (int)((double)v2 / (1 << scale[i]) / 2048 - 0.4999999f);

					/* Clamp sample and set index */
					if (v3 < -8)
					{
						if (index < (v3 = -8 - v3))
							index = v3;
						v3 = -8;
					}
					else if (v3 > 7)
					{
						if (index < (v3 -= 7))
							index = v3;
						v3 = 7;
					}

					/* Store result */
					outSamples[i + s] = v3;

					/* Round and expand */
					v1 = (v1 + ((v3 * (1 << scale[i])) << 11) + 1024) >> 11;
					/* Clamp and store */
					inSamples[i + (s + 2)] = v2 = (v1 >= 32767) ? 32767 : (v1 <= -32768) ? -32768 : v1;
					/* Accumulate distance */
					v3 = pcmInOut[s + 2] - v2;
					distAccum[i] += v3 * (double)v3;
				}

				for (int x = index + 8; x > 256; x >>= 1)
					if (++scale[i] >= 12)
						scale[i] = 11;
			} while ((scale[i] < 12) && (index > 1));
		}

		double min = double.MaxValue;
		for (int i = 0; i < 8; i++)
		{
			if (distAccum[i] < min)
			{
				min = distAccum[i];
				bestIndex = i;
			}
		}

		/* Write converted samples */
		for (int s = 0; s < sampleCount; s++)
			pcmInOut[s + 2] = (short)(inSamples[bestIndex] + inSamples[s + 2]);

		/* Write ps */
		adpcmOut[0] = (byte)((bestIndex << 4) | (scale[bestIndex] & 0xF));

		/* Zero remaining samples */
		for (int s = sampleCount; s < 14; s++)
			outSamples[bestIndex + s] = 0;

		/* Write output samples */
		for (int y = 0; y < 7; y++)
		{
			adpcmOut[y + 1] = (byte)((outSamples[bestIndex] + outSamples[y * 2] << 4) | (outSamples[bestIndex] + outSamples[y * 2 + 1] & 0xF));
		}
	}

	public static void EncodeFrame(Span<short> src, Span<byte> dst, Span<short> coefs, byte one)
	{
		coefs = new short[0 + 2];
		DSPEncodeFrame(src, 14, dst, coefs);
	}

	#endregion

	#region DSP-ADPCM Decode

	#region Method 1

	private static readonly sbyte[] NibbleToSbyte = [0, 1, 2, 3, 4, 5, 6, 7, -8, -7, -6, -5, -4, -3, -2, -1];

	public static int DivideByRoundUp(int dividend, int divisor)
	{
		return (dividend + divisor - 1) / divisor;
	}

	private static sbyte GetHighNibble(byte value)
	{
		return NibbleToSbyte[(value >> 4) & 0xF];
	}

	private static sbyte GetLowNibble(byte value)
	{
		return NibbleToSbyte[value & 0xF];
	}

	public static short Clamp16(int value)
	{
		if (value > Max)
			return Max;
		else if (value < Min)
			return Min;
		else return (short)value;
	}

	#region Current code
	public void Init(byte[] data, DSPADPCMInfo[] info)
	{
		Info = info;
		Data = data;
		DataOffset = 0;
		SamplePos = 0;
		FrameOffset = 0;

        Hist1 = new short[NumChannels];
        Hist2 = new short[NumChannels];
        Coef1 = new int[NumChannels];
        Coef2 = new int[NumChannels];
    }

	public void Decode()
	{
		Init(Data, Info); // Sets up the field variables

		// Each DSP-ADPCM frame is 8 bytes long: 1 byte for header, 7 bytes for sample data
		// This loop reads every 8 bytes and decodes them until the samplePos reaches NumSamples
		while (SamplePos < Info[0].NumSamples)
		{
			// This function will decode one frame at a time
			DecodeFrame();

			// The dataOffset is incremented by 8, and samplePos is incremented by 14 to prepare for the next frame
			DataOffset += DSPADPCMConstants.BytesPerFrame;
			SamplePos += DSPADPCMConstants.SamplesPerFrame;
		}

	}

	public void DecodeFrame()
	{
		// It will decode 1 single DSP frame of size 0x08 (src) into 14 samples in a PCM buffer (dst)
        for (int i = 0; i < NumChannels; i++)
		{
			Hist1![i] = Info[i].Yn1;
			Hist2![i] = Info[i].Yn2;
		}

		// Parsing the frame's header byte
		Scale = 1 << ((Data[DataOffset]) & 0xf);
		int coefIndex = ((Data[DataOffset] >> 4) & 0xf) * 2;

        // Parsing the coefficient pairs, based on the nibble's value
        for (int i = 0; i < NumChannels; i++)
		{
			Coef1![i] = Info[i].Coef[coefIndex + 0];
			Coef2![i] = Info[i].Coef[coefIndex + 1];
		}

		// This loop decodes the frame's nibbles, each of which are 4-bits long (half a byte in length)
		for (FrameOffset = 0; FrameOffset < DSPADPCMConstants.SamplesPerFrame * NumChannels; FrameOffset += NumChannels)
		{
			// This ensures multi-channel DSP-ADPCM data is decoded as well
			for (Channel = 0; Channel < NumChannels; Channel++)
			{
				// Stores the value of the entire byte based on the frame's offset
				ByteValue = Data[DataOffset + 0x01 + FrameOffset / 2];

				// This function decodes one nibble within a frame into a sample
				short sample = GetSample();

				// The DSP-ADPCM frame may have bytes that go beyond the DataOutput length, if this happens, this will safely finish the DecodeFrame function's task as is
				if ((SamplePos + FrameOffset) * (Channel + 1) >= DataOutput!.Length) { return; }

				// The PCM16 sample is stored into the array entry, based on the sample offset and frame offset, multiplied by which wave channel is being used
				DataOutput[(SamplePos + FrameOffset) * (Channel + 1)] = sample;

				// History values are stored, hist1 is copied into hist2 and the PCM16 sample is copied into hist1, before moving onto the next byte in the frame
				Hist2![Channel] = Hist1![Channel];
				Hist1[Channel] = sample;
			}
		}

		// After the frame is decoded, the values in hist1 and hist2 are copied into Yn1 and Yn2 to prepare for the next frame
		for (int i = 0; i < NumChannels; i++)
		{
			Info[i].Yn1 = Hist1![i];
			Info[i].Yn2 = Hist2![i];
        }
	}

	public short GetSample()
	{
		Nibble = (FrameOffset & 1) != 0 ? // This conditional operator will store the value of the nibble
				GetLowNibble(ByteValue) : // If the byte is not 0, it will obtain the least significant nibble (4-bits)
				GetHighNibble(ByteValue); // Otherwise, if the byte is 0, it will obtain the most significant nibble (4-bits)
		int largerVal = (Nibble * Scale) << 11; // The nibble's value is multiplied by scale's value, then 11 bits are shifted left, making the value larger
		int newVal = (largerVal + 1024 + (Coef1![Channel] * Hist1![Channel]) + (Coef2![Channel] * Hist2![Channel])) >> 11; // Coefficients are multiplied by the value stored in hist1 and hist2 respectively, then the values are added together to make a new value
		short sample = Clamp16(newVal); // The new value is then clamped into a 16-bit value, which makes a PCM16 sample

		return sample;
	}

	#endregion


	#endregion


	#endregion

	#region DSP-ADPCM Math
	public static uint GetBytesForADPCMBuffer(uint samples)
	{
		uint frames = samples / DSPADPCMConstants.SamplesPerFrame;
		if ((samples % DSPADPCMConstants.SamplesPerFrame) == frames)
			frames++;

		return frames * DSPADPCMConstants.BytesPerFrame;
	}

	public static uint GetBytesForADPCMSamples(uint samples)
	{
		uint extraBytes = 0;
		uint frames = samples / DSPADPCMConstants.SamplesPerFrame;
		uint extraSamples = (samples % DSPADPCMConstants.SamplesPerFrame);

		if (extraSamples == frames)
		{
			extraBytes = (extraSamples / 2) + (extraSamples % 2) + 1;
		}

		return DSPADPCMConstants.BytesPerFrame * frames + extraBytes;
	}

	public static uint GetBytesForPCMBuffer(uint samples)
	{
		uint frames = samples / DSPADPCMConstants.SamplesPerFrame;
		if ((samples % DSPADPCMConstants.SamplesPerFrame) == frames)
			frames++;

		return frames * DSPADPCMConstants.SamplesPerFrame * sizeof(int);
	}

	public static uint GetBytesForPCMSamples(uint samples)
	{
		return samples * sizeof(int);
	}

	public static uint GetNibbleAddress(uint samples)
	{
		int frames = (int)(samples / DSPADPCMConstants.SamplesPerFrame);
		int extraSamples = (int)(samples % DSPADPCMConstants.SamplesPerFrame);

		return (uint)(DSPADPCMConstants.NibblesPerFrame * frames + extraSamples + 2);
	}

	public static uint GetNibblesForNSamples(uint samples)
	{
		uint frames = samples / DSPADPCMConstants.SamplesPerFrame;
		uint extraSamples = (samples % DSPADPCMConstants.SamplesPerFrame);
		uint extraNibbles = extraSamples == 0 ? 0 : extraSamples + 2;

		return DSPADPCMConstants.NibblesPerFrame * frames + extraNibbles;
	}

	public static uint GetSampleForADPCMNibble(uint nibble)
	{
		uint frames = nibble / DSPADPCMConstants.NibblesPerFrame;
		uint extraNibbles = (nibble % DSPADPCMConstants.NibblesPerFrame);
		uint samples = DSPADPCMConstants.SamplesPerFrame * frames;

		return samples + extraNibbles - 2;
	}
	#endregion
}
