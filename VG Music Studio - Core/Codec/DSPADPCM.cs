using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Core.Util;
using System;
using System.Diagnostics;

namespace Kermalis.VGMusicStudio.Core.Codec
{
	internal sealed class DSPADPCM
	{
		public static short Min = short.MinValue;
		public static short Max = short.MaxValue;

		public static double[] Tvec = new double[3];

		public DSPADPCMInfo Info;
		public byte[] Data;
		public short[] DataOutput;
		//public static short[]? DataOutput;

		public static class DSPADPCMConstants
		{
			public const int BytesPerFrame = 8;
			public const int SamplesPerFrame = 14;
			public const int NibblesPerFrame = 16;
		}

		public DSPADPCM(EndianBinaryReader r, int outputSize)
		{
			Info = new DSPADPCMInfo(r); // First, the DSP-ADPCM table is read and each variable is assigned with a value
			Data = new byte[(Info.NumAdpcmNibbles / 2) + 9]; // Next, the allocated size of the compressed sample data is determined based on NumAdpcmNibbles divided by 2, plus 9
			DataOutput = new short[outputSize]; // The data output needs to be the actual size of the sample data when uncompressed

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
		}
		#endregion

		#region DSP-ADPCM Convert

		//public object GetSamples(DSPADPCMInfo cxt, uint loopEnd)
		//{
		//    return DSPADPCMToPCM16(Data, loopEnd, cxt);
		//}

		public static Span<short> DSPADPCMToPCM16(DSPADPCM dspadpcm, DSPADPCMInfo cxt, int numChannels, bool useSubInterframes)
		{
			//Span<short> dataOutput = new short[outputSize]; // This is the new output data that's converted to PCM16
			for (int i = 0; i < dspadpcm.Data.Length; i++)
			{
				Decode(dspadpcm, ref cxt, numChannels, useSubInterframes);
			}
			return dspadpcm.DataOutput;
		}
		#endregion

		#region DSP-ADPCM Encode
		public static void Encode(short[] src, byte[] dst, DSPADPCMInfo cxt, uint samples)
		{
			short[] coefs = cxt.Coef;
			CorrelateCoefs(src, samples, coefs);

			int frameCount = (int)((samples / DSPADPCMConstants.SamplesPerFrame) + (samples % DSPADPCMConstants.SamplesPerFrame));

			short[] pcm = src;
			byte[] adpcm = dst;
			short[] pcmFrame = new short[DSPADPCMConstants.SamplesPerFrame + 2];
			byte[] adpcmFrame = new byte[DSPADPCMConstants.BytesPerFrame];

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

		public static void InnerProductMerge(double[] vecOut, short[] pcmBuf)
		{
			pcmBuf = new short[14];
			vecOut = Tvec;

			for (int i = 0; i <= 2; i++)
			{
				vecOut[i] = 0.0f;
				for (int x = 0; x < 14; x++)
					vecOut[i] -= pcmBuf[x - i] * pcmBuf[x];

			}
		}

		public static void OuterProductMerge(double[] mtxOut, short[] pcmBuf)
		{
			pcmBuf = new short[14];
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

		public static bool AnalyzeRanges(double[] mtx, int[] vecIdxsOut)
		{
			mtx[3] = Tvec[3];
			double[] recips = new double[3];
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

		public static void BidirectionalFilter(double[] mtx, int[] vecIdxs, double[] vecOut)
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

		public static bool QuadraticMerge(double[] inOutVec)
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

		public static void FinishRecord(double[] vIn, double[] vOut)
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

	public static void MatrixFilter(double[] src, double[] dst)
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

	public static void MergeFinishRecord(double[] src, double[] dst)
	{
		src = Tvec;
		dst = Tvec;
		int dstIndex = 0;
		double[] tmp = new double[dstIndex];
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

	public static double ContrastVectors(double[] source1, double[] source2)
	{
		source1 = Tvec;
		source2 = Tvec;
		double val = (source2[2] * source2[1] + -source2[1]) / (1.0 - source2[2] * source2[2]);
		double val1 = (source1[0] * source1[0]) + (source1[1] * source1[1]) + (source1[2] * source1[2]);
		double val2 = (source1[0] * source1[1]) + (source1[1] * source1[2]);
		double val3 = source1[0] * source1[2];
		return val1 + (2.0 * val * val2) + (2.0 * (-source2[1] * val + -source2[2]) * val3);
	}

	public static void FilterRecords(double[] vecBest, int exp, double[] records, int recordCount)
	{
		vecBest[8] = Tvec[8];
		records = Tvec;
		double[] bufferList = new double[8];
		bufferList[8] = Tvec[8];

		int[] buffer1 = new int[8];
		double[] buffer2 = Tvec;

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
					vecBest = new double[i];
					records = new double[z];
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

	public static void CorrelateCoefs(short[] source, uint samples, short[] coefsOut)
	{
		int numFrames = (int)((samples + 13) / 14);
		int frameSamples;

		short[] blockBuffer = new short[0x3800];
		short[] pcmHistBuffer = new short[2 + 14];

		double[] vec1 = Tvec;
		double[] vec2 = Tvec;

		double[] mtx = Tvec;
		mtx[3] = Tvec[3];
		int[] vecIdxs = new int[3];

		double[] records = new double[numFrames * 2];
		records = Tvec;
		int recordCount = 0;

		double[] vecBest = new double[8];
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

				pcmHistBuffer = new short[1];

				InnerProductMerge(vec1, pcmHistBuffer);
				if (Math.Abs(vec1[0]) > 10.0)
				{
					OuterProductMerge(mtx, pcmHistBuffer);
					if (!AnalyzeRanges(mtx, vecIdxs))
					{
						BidirectionalFilter(mtx, vecIdxs, vec1);
						if (!QuadraticMerge(vec1))
						{
							records = new double[recordCount];
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
			records = new double[z];
			vecBest = new double[0];
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
	public static void DSPEncodeFrame(short[] pcmInOut, int sampleCount, byte[] adpcmOut, short[] coefsIn)
	{
		pcmInOut = new short[16];
		adpcmOut = new byte[8];
		coefsIn = new short[8];
		coefsIn = new short[2];

		int[] inSamples = new int[8];
		inSamples = new int[16];
		int[] outSamples = new int[8];
		outSamples = new int[14];

		int bestIndex = 0;

		int[] scale = new int[8];
		double[] distAccum = new double[8];

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

	public static void EncodeFrame(short[] src, byte[] dst, short[] coefs, byte one)
	{
		coefs = new short[0 + 2];
		DSPEncodeFrame(src, 14, dst, coefs);
	}
		#endregion

		#region DSP-ADPCM Decode

		#region Method 1
		public static int DivideByRoundUp(int dividend, int divisor)
		{
			return (dividend + divisor - 1) / divisor;
		}

		public static sbyte GetHighNibble(byte value)
		{
			return (sbyte)((value >> 4) & 0xF);
		}

		public static sbyte GetLowNibble(byte value)
		{
			return (sbyte)((value) & 0xF);
		}

		public static short Clamp16(int value)
		{
			if (value > Max)
				return Max;
			if (value < Min)
				return Min;
			return (short)value;
		}

		public static void Decode(DSPADPCM samples, ref DSPADPCMInfo ctx, int numChannels, bool useSubInterframes)
		{
			int data_offset = 0;
			int sample_pos = 0;
			while (sample_pos < samples.Info.NumSamples)
			{
				// Decodes 1 single DSP frame of size 0x08 (src) into a 14 samples in a PCM buffer (dst)
				int hist1 = ctx.Yn1;
				int hist2 = ctx.Yn2;

				/* parse frame header */
				int scale = 1 << ((samples.Data[0x00] >> 0) & 0xf);
				int index = (samples.Data[0x00] >> 4) & 0xf;

				int coef1 = ctx.Coef[index * 2 + 0];
				int coef2 = ctx.Coef[index * 2 + 1];

				/* decode nibbles */
				for (int i = 0; i < DSPADPCMConstants.SamplesPerFrame; i++)
				{
					byte nibbles = samples.Data[0x01 + i / 2];

					int sample = (i & 1) != 0 ? /* high nibble first */
							get_low_nibble_signed(nibbles) :
							get_high_nibble_signed(nibbles);
					sample = ((sample * scale) << 11);
					sample = ((sample + 1024) + (coef1 * hist1) + (coef2 * hist2)) >> 11;
					sample = clamp16(sample);

					samples.DataOutput[(sample_pos + i) * numChannels] = (byte)sample;

					hist2 = hist1;
					hist1 = sample;
				}

				ctx.Yn1 = (short)hist1;
				ctx.Yn2 = (short)hist2;

				data_offset += DSPADPCMConstants.BytesPerFrame;
				sample_pos += DSPADPCMConstants.SamplesPerFrame;
			}
			
		}

		public void DecodeSample(DSPADPCM sample, ref DSPADPCMInfo ctx, bool useSubInterframes)
		{
			
		}

		#region Old Code
		//public static void Decode(Span<byte> src, Span<short> dst, ref DSPADPCMInfo cxt, uint samples)
		//{
		//	short hist1 = cxt.Yn1;
		//	short hist2 = cxt.Yn2;
		//	short[] coefs = cxt.Coef;
		//	int srcIndex = 0;
		//	int dstIndex = 0;

		//	int frameCount = DivideByRoundUp((int)samples, DSPADPCMConstants.SamplesPerFrame);
		//	int samplesRemaining = (int)samples;

		//	for (int i = 0; i < frameCount; i++)
		//	{
		//		int predictor = GetHighNibble(src[srcIndex]) & 0xF;
		//		int scale = 1 << GetLowNibble(src[srcIndex++]);
		//		short coef1 = coefs[predictor * 2];
		//		short coef2 = coefs[predictor * 2 + 1];

		//		int samplesToRead = Math.Min(DSPADPCMConstants.SamplesPerFrame, samplesRemaining);


		//		for (int s = 0; s < samplesToRead; s++)
		//		{
		//			// Get bits per byte
		//			//byte bits = src[srcIndex++];
		//			byte bits = src[srcIndex + (s >> 1)];
		//			int sample = (s % 2) == 0 ? GetHighNibble(bits) : GetLowNibble(bits);
		//			sample = sample >= 8 ? sample - 16 : sample;
		//			sample = (((scale * sample) << 11) + 1024 + ((coef1 * hist1) + (coef2 * hist2))) >> 11;
		//			short finalSample = Clamp16(sample);

		//			hist2 = hist1;
		//			hist1 = finalSample;

		//			//if (samplesToRead <= 14) { samplesToRead = 0; }
		//			//srcIndex += 1;
		//			dst[dstIndex++] = finalSample;
		//			if (dstIndex >= samplesToRead) break;
		//		}

		//		//samplesRemaining -= samplesToRead;
		//		srcIndex += samplesToRead / 2;
		//	}
		//}
		#endregion

		public static void GetLoopContext(Span<byte> src, ref DSPADPCMInfo cxt, uint samples)
		{
			short hist1 = cxt.Yn1;
			short hist2 = cxt.Yn2;
			short[] coefs = cxt.Coef;
			int srcIndex = 0;
			byte ps = 0;

			int frameCount = DivideByRoundUp((int)samples, DSPADPCMConstants.SamplesPerFrame);
			int samplesRemaining = (int)samples;

			for (int i = 0; i < frameCount; i++)
			{
				ps = src[srcIndex];
				int predictor = GetHighNibble(src[srcIndex]) & 0x7;
				int scale = 1 << GetLowNibble(src[srcIndex++]);
				short coef1 = coefs[predictor * 2];
				short coef2 = coefs[predictor * 2 + 1];

				int samplesToRead = Math.Min(DSPADPCMConstants.SamplesPerFrame, samplesRemaining);

				for (int s = 0; s < samplesToRead; s++)
				{
					int sample = s % 2 == 0 ? GetHighNibble(src[srcIndex]) : GetLowNibble(src[srcIndex++]);
					sample = sample >= 8 ? sample - 16 : sample;
					sample = (((scale * sample) << 11) + 1024 + (coef1 * hist1 + coef2 * hist2)) >> 11;
					short finalSample = Clamp16(sample);

					hist2 = hist1;
					hist1 = finalSample;
				}
				samplesRemaining -= samplesToRead;
			}

			cxt.LoopPredScale = ps;
			cxt.LoopYn1 = hist1;
			cxt.LoopYn2 = hist2;
		}
		#endregion

		#region Method 2

		public class PlayConfigType
		{
			int config_set; /* some of the mods below are set */

			/* modifiers */
			int play_forever;
			int ignore_loop;
			int force_loop;
			int really_force_loop;
			int ignore_fade;

			/* processing */
			double loop_count;
			int pad_begin;
			int trim_begin;
			int body_time;
			int trim_end;
			double fade_delay; /* not in samples for backwards compatibility */
			double fade_time;
			int pad_end;

			double pad_begin_s;
			double trim_begin_s;
			double body_time_s;
			double trim_end_s;
			//double fade_delay_s;
			//double fade_time_s;
			double pad_end_s;

			/* internal flags */
			int pad_begin_set;
			int trim_begin_set;
			int body_time_set;
			int loop_count_set;
			int trim_end_set;
			int fade_delay_set;
			int fade_time_set;
			int pad_end_set;

			/* for lack of a better place... */
			int is_txtp;
			int is_mini_txtp;

		}


		public class PlayStateType
		{
			int input_channels;
			int output_channels;

			int pad_begin_duration;
			int pad_begin_left;
			int trim_begin_duration;
			int trim_begin_left;
			int body_duration;
			int fade_duration;
			int fade_left;
			int fade_start;
			int pad_end_duration;
			//int pad_end_left;
			int pad_end_start;

			int play_duration;      /* total samples that the stream lasts (after applying all config) */
			int play_position;      /* absolute sample where stream is */

		}

		public class Stream
		{
			/* basic config */
			int num_samples;            /* the actual max number of samples */
			int sample_rate;            /* sample rate in Hz */
			public int channels;                   /* number of channels */
			CodecType coding_type;           /* type of encoding */
			LayoutType layout_type;           /* type of layout */
			MetaType meta_type;               /* type of metadata */

			/* loopin config */
			int loop_flag;                  /* is this stream looped? */
			int loop_start_sample;      /* first sample of the loop (included in the loop) */
			int loop_end_sample;        /* last sample of the loop (not included in the loop) */

			/* layouts/block config */
			int interleave_block_size;   /* interleave, or block/frame size (depending on the codec) */
			int interleave_first_block_size; /* different interleave for first block */
			int interleave_first_skip;   /* data skipped before interleave first (needed to skip other channels) */
			int interleave_last_block_size; /* smaller interleave for last block */
			int frame_size;              /* for codecs with configurable size */

			/* subsong config */
			int num_streams;                /* for multi-stream formats (0=not set/one stream, 1=one stream) */
			int stream_index;               /* selected subsong (also 1-based) */
			int stream_size;             /* info to properly calculate bitrate in case of subsongs */
			char[] stream_name = new char[255]; /* name of the current stream (info), if the file stores it and it's filled */

			/* mapping config (info for plugins) */
			uint channel_layout;        /* order: FL FR FC LFE BL BR FLC FRC BC SL SR etc (WAVEFORMATEX flags where FL=lowest bit set) */

			/* other config */
			int allow_dual_stereo;          /* search for dual stereo (file_L.ext + file_R.ext = single stereo file) */


			/* layout/block state */
			int full_block_size;         /* actual data size of an entire block (ie. may be fixed, include padding/headers, etc) */
			int current_sample;         /* sample point within the file (for loop detection) */
			int samples_into_block;     /* number of samples into the current block/interleave/segment/etc */
			int current_block_offset;     /* start of this block (offset of block header) */
			int current_block_size;      /* size in usable bytes of the block we're in now (used to calculate num_samples per block) */
			int current_block_samples;  /* size in samples of the block we're in now (used over current_block_size if possible) */
			int next_block_offset;        /* offset of header of the next block */

			/* loop state (saved when loop is hit to restore later) */
			int loop_current_sample;    /* saved from current_sample (same as loop_start_sample, but more state-like) */
			int loop_samples_into_block;/* saved from samples_into_block */
			int loop_block_offset;        /* saved from current_block_offset */
			int loop_block_size;         /* saved from current_block_size */
			int loop_block_samples;     /* saved from current_block_samples */
			int loop_next_block_offset;   /* saved from next_block_offset */
			int hit_loop;                   /* save config when loop is hit, but first time only */


			/* decoder config/state */
			int codec_endian;               /* little/big endian marker; name is left vague but usually means big endian */
			int codec_config;               /* flags for codecs or layouts with minor variations; meaning is up to them */
			int ws_output_size;         /* WS ADPCM: output bytes for this block */


			/* main state */
			public Channel[] ch;           /* array of channels */
			Channel start_ch;     /* shallow copy of channels as they were at the beginning of the stream (for resets) */
			Channel loop_ch;      /* shallow copy of channels as they were at the loop point (for loops) */
			IntPtr start_vgmstream;          /* shallow copy of the VGMSTREAM as it was at the beginning of the stream (for resets) */

			IntPtr mixing_data;              /* state for mixing effects */

			/* Optional data the codec needs for the whole stream. This is for codecs too
			 * different from vgmstream's structure to be reasonably shoehorned.
			 * Note also that support must be added for resetting, looping and
			 * closing for every codec that uses this, as it will not be handled. */
			IntPtr codec_data;
			/* Same, for special layouts. layout_data + codec_data may exist at the same time. */
			IntPtr layout_data;


			/* play config/state */
			int config_enabled;             /* config can be used */
			PlayConfigType config;           /* player config (applied over decoding) */
			PlayStateType pstate;            /* player state (applied over decoding) */
			int loop_count;                 /* counter of complete loops (1=looped once) */
			int loop_target;                /* max loops before continuing with the stream end (loops forever if not set) */
			short[] tmpbuf;               /* garbage buffer used for seeking/trimming */
			int tmpbuf_size;             /* for all channels (samples = tmpbuf_size / channels) */

		}

		/* read from a file, returns number of bytes read */
		public static int read_streamfile(Span<byte> dst, int offset, int length, StreamFile sf)
		{
			return read_streamfile(dst, offset, length, sf);
		}

		/* return file size */
		public static int get_streamfile_size(StreamFile sf)
		{
			return get_streamfile_size(sf);
		}

		public class StreamFile
		{

			/* read 'length' data at 'offset' to 'dst' */
			static int read(Span<byte> dst, int offset, int length, StreamFile[] sf)
			{
				return read(dst, offset, length, sf);
			}

			/* get max offset */
			static int get_size(StreamFile[] sf)
			{
				return get_size(sf);
			}

			//todo: DO NOT USE, NOT RESET PROPERLY (remove?)
			static int get_offset(StreamFile[] sf)
			{
				return get_offset(sf);
			}

			/* copy current filename to name buf */
			static void get_name(string name, int name_size, StreamFile[] sf)
			{
				sf.SetValue(name, name_size);
			}

			/* open another streamfile from filename */
			public StreamFile()
			{
				string filename;
				int buf_size;
				StreamFile[] sf;
			}

			/* free current STREAMFILE */
			//void (* close) (struct _StreamFile sf);

			/* Substream selection for formats with subsongs.
			 * Not ideal here, but it was the simplest way to pass to all init_vgmstream_x functions. */
			int stream_index; /* 0=default/auto (first), 1=first, N=Nth */

		}

		public class g72x_state
		{
			long yl;    /* Locked or steady state step size multiplier. */
			short yu;   /* Unlocked or non-steady state step size multiplier. */
			short dms;  /* Short term energy estimate. */
			short dml;  /* Long term energy estimate. */
			short ap;   /* Linear weighting coefficient of 'yl' and 'yu'. */

			short[] a = new short[2]; /* Coefficients of pole portion of prediction filter. */
			short[] b = new short[6]; /* Coefficients of zero portion of prediction filter. */
			short[] pk = new short[2];    /*
			 * Signs of previous two samples of a partially
			 * reconstructed signal.
			 */
			short[] dq = new short[6];    /*
			 * Previous 6 samples of the quantized difference
			 * signal represented in an internal floating point
			 * format.
			 */
			short[] sr = new short[2];    /*
			 * Previous 2 samples of the quantized difference
			 * signal represented in an internal floating point
			 * format.
			 */
			char td;    /* delayed tone detect, new in 1988 version */
		};

		public class Channel
		{
			public StreamFile streamfile = new StreamFile();     /* file used by this channel */
			public long channel_start_offset; /* where data for this channel begins */
			public long offset;               /* current location in the file */

			public int frame_header_offset;  /* offset of the current frame header (for WS) */
			public int samples_left_in_frame;  /* for WS */

			/* format specific */

			/* adpcm */
			public short[] adpcm_coef = new short[16];             /* formats with decode coefficients built in (DSP, some ADX) */
			public int[] adpcm_coef_3by32 = new int[0x60];     /* Level-5 0x555 */
			public short[] vadpcm_coefs = new short[8 * 2 * 8];        /* VADPCM: max 8 groups * max 2 order * fixed 8 subframe coefs */
			public short adpcm_history1_16;      /* previous sample */
			public int adpcm_history1_32;

			public short adpcm_history2_16;      /* previous previous sample */
			public int adpcm_history2_32;

			public short adpcm_history3_16;
			public int adpcm_history3_32;

			public short adpcm_history4_16;
			public int adpcm_history4_32;


			//double adpcm_history1_double;
			//double adpcm_history2_double;

			public int adpcm_step_index;               /* for IMA */
			public int adpcm_scale;                    /* for MS ADPCM */

			/* state for G.721 decoder, sort of big but we might as well keep it around */
			public g72x_state g72x_state = new g72x_state();

			/* ADX encryption */
			public int adx_channels;
			public short adx_xor;
			public short adx_mult;
			public short adx_add;

		};

		public static int[] nibble_to_int = new int[16] {0,1,2,3,4,5,6,7,-8,-7,-6,-5,-4,-3,-2,-1};

		public static int get_nibble_signed(byte n, int upper)
		{
			/*return ((n&0x70)-(n&0x80))>>4;*/
			return nibble_to_int[(n >> (upper != 0 ? 4 : 0)) & 0x0f];
		}

		public static int get_high_nibble_signed(byte n)
		{
			/*return ((n&0x70)-(n&0x80))>>4;*/
			return nibble_to_int[n >> 4];
		}

		public static int get_low_nibble_signed(byte n)
		{
			/*return (n&7)-(n&8);*/
			return nibble_to_int[n & 0xf];
		}

		public static int clamp16(int val)
		{
			if (val > 32767) return 32767;
			else if (val < -32768) return -32768;
			else return val;
		}

		public static void decode_ngc_dsp(Channel stream, Span<int> outbuf, int channelspacing, int first_sample, int samples_to_do)
		{
			byte[] frame = new byte[0x08] { 0,0,0,0,0,0,0,0 };
			int frame_offset;
			int i, frames_in, sample_count = 0;
			int bytes_per_frame, samples_per_frame;
			int coef_index, scale, coef1, coef2;
			int hist1 = stream.adpcm_history1_16;
			int hist2 = stream.adpcm_history2_16;


			/* external interleave (fixed size), mono */
			bytes_per_frame = 0x08;
			samples_per_frame = (bytes_per_frame - 0x01) * 2; /* always 14 */
			frames_in = first_sample / samples_per_frame;
			first_sample = first_sample % samples_per_frame;

			/* parse frame header */
			frame_offset = (int)((stream.offset + bytes_per_frame) * frames_in);
			read_streamfile(frame, frame_offset, bytes_per_frame, stream.streamfile); /* ignore EOF errors */
			scale = 1 << ((frame[0] >> 0) & 0xf);
			coef_index = (frame[0] >> 4) & 0xf;

			if (coef_index >= 8) { Debug.WriteLine($"DSP: incorrect coefs at %x\n", (uint)frame_offset); }
			//if (coef_index > 8) //todo not correctly clamped in original decoder?
			//    coef_index = 8;

			coef1 = stream.adpcm_coef[coef_index * 2 + 0];
			coef2 = stream.adpcm_coef[coef_index * 2 + 1];


			/* decode nibbles */
			for (i = first_sample; i < first_sample + samples_to_do; i++)
			{
				int sample = 0;
				byte nibbles = frame[0x01 + i / 2];

				sample = (i & 1) != 0 ? /* high nibble first */
						get_low_nibble_signed(nibbles) :
						get_high_nibble_signed(nibbles);
				sample = ((sample * scale) << 11);
				sample = (sample + 1024 + coef1 * hist1 + coef2 * hist2) >> 11;
				sample = clamp16(sample);

				outbuf[sample_count] = sample;
				sample_count += channelspacing;

				hist2 = hist1;
				hist1 = sample;
			}

			stream.adpcm_history1_16 = (short)hist1;
			stream.adpcm_history2_16 = (short)hist2;
		}


		/* read from memory rather than a file */
		public static void decode_ngc_dsp_subint_internal(Channel stream, Span<int> outbuf, int channelspacing, int first_sample, int samples_to_do, Span<byte> frame)
		{
			int i, sample_count = 0;
			int bytes_per_frame, samples_per_frame;
			int coef_index, scale, coef1, coef2;
			int hist1 = stream.adpcm_history1_16;
			int hist2 = stream.adpcm_history2_16;


			/* external interleave (fixed size), mono */
			bytes_per_frame = 0x08;
			samples_per_frame = (bytes_per_frame - 0x01) * 2; /* always 14 */
			first_sample = first_sample % samples_per_frame;
			if (samples_to_do > samples_per_frame) { Debug.WriteLine($"DSP: layout error, too many samples\n"); }

			/* parse frame header */
			scale = 1 << ((frame[0] >> 0) & 0xf);
			coef_index = (frame[0] >> 4) & 0xf;

			if (coef_index >= 8) { Debug.WriteLine($"DSP: incorrect coefs\n"); }
			//if (coef_index > 8) //todo not correctly clamped in original decoder?
			//    coef_index = 8;

			coef1 = stream.adpcm_coef[coef_index * 2 + 0];
			coef2 = stream.adpcm_coef[coef_index * 2 + 1];

			for (i = first_sample; i < first_sample + samples_to_do; i++)
			{
				int sample = 0;
				byte nibbles = frame[0x01 + i / 2];

				sample = (i & 1) != 0 ?
						get_low_nibble_signed(nibbles) :
						get_high_nibble_signed(nibbles);
				sample = ((sample * scale) << 11);
				sample = (sample + 1024 + coef1 * hist1 + coef2 * hist2) >> 11;
				sample = clamp16(sample);

				outbuf[sample_count] = sample;
				sample_count += channelspacing;

				hist2 = hist1;
				hist1 = sample;
			}

			stream.adpcm_history1_16 = (short)hist1;
			stream.adpcm_history2_16 = (short)hist2;
		}

		private static sbyte read_8bit(int offset, StreamFile sf)
		{
			byte[] buf = new byte[1];

			if (read_streamfile(buf, offset, 1, sf) != 1) return -1;
			return (sbyte)buf[0];
		}

		/* decode DSP with byte-interleaved frames (ex. 0x08: 1122112211221122) */
		public static void decode_ngc_dsp_subint(Channel stream, Span<int> outbuf, int channelspacing, int first_sample, int samples_to_do, int channel, int interleave)
		{
			byte[] frame = new byte[0x08];
			int i;
			int frames_in = first_sample / 14;

			for (i = 0; i < 0x08; i++)
			{
				/* base + current frame + subint section + subint byte + channel adjust */
				frame[i] = (byte)read_8bit(
						(int)((stream.offset
						+ frames_in) * (0x08 * channelspacing)
						+ i / interleave * interleave * channelspacing
						+ i % interleave
						+ interleave * channel), stream.streamfile);
			}

			decode_ngc_dsp_subint_internal(stream, outbuf, channelspacing, first_sample, samples_to_do, frame);
		}


		/*
		 * The original DSP spec uses nibble counts for loop points, and some
		 * variants don't have a proper sample count, so we (who are interested
		 * in sample counts) need to do this conversion occasionally.
		 */
		public static int dsp_nibbles_to_samples(int nibbles)
		{
			int whole_frames = nibbles / 16;
			int remainder = nibbles % 16;

			if (remainder > 0) return whole_frames * 14 + remainder - 2;
			else return whole_frames * 14;
		}

		public static int dsp_bytes_to_samples(int bytes, int channels)
		{
			if (channels <= 0) return 0;
			return bytes / channels / 8 * 14;
		}

		/* host endian independent multi-byte integer reading */
		public static short get_16bitBE(Span<byte> p)
		{
			return (short)(((ushort)p[0] << 8) | ((ushort)p[1]));
		}

		public static short get_16bitLE(Span<byte> p)
		{
			return (short)(((ushort)p[0]) | ((ushort)p[1] << 8));
		}

		public static short read_16bitLE(int offset, StreamFile sf)
		{
			byte[] buf = new byte[2];

			if (read_streamfile(buf, offset, 2, sf) != 2) return -1;
			return get_16bitLE(buf);
		}
		public static short read_16bitBE(int offset, StreamFile sf)
		{
			byte[] buf = new byte[2];

			if (read_streamfile(buf, offset, 2, sf) != 2) return -1;
			return get_16bitBE(buf);
		}

		/* reads DSP coefs built in the streamfile */
		public static void dsp_read_coefs_be(Stream vgmstream, StreamFile streamFile, int offset, int spacing)
		{
			dsp_read_coefs(vgmstream, streamFile, offset, spacing, 1);
		}
		public static void dsp_read_coefs_le(Stream vgmstream, StreamFile streamFile, int offset, int spacing)
		{
			dsp_read_coefs(vgmstream, streamFile, offset, spacing, 0);
		}
		public static void dsp_read_coefs(Stream vgmstream, StreamFile streamFile, int offset, int spacing, int be)
		{
			int ch, i;
			/* get ADPCM coefs */
			for (ch = 0; ch < vgmstream.channels; ch++)
			{
				for (i = 0; i < 16; i++)
				{
					vgmstream.ch[ch].adpcm_coef[i] = be != 0 ?
							read_16bitBE(offset + ch * spacing + i * 2, streamFile) :
							read_16bitLE(offset + ch * spacing + i * 2, streamFile);
				}
			}
		}

		/* reads DSP initial hist built in the streamfile */
		public static void dsp_read_hist_be(Stream vgmstream, StreamFile streamFile, int offset, int spacing)
		{
			dsp_read_hist(vgmstream, streamFile, offset, spacing, 1);
		}
		public static void dsp_read_hist_le(Stream vgmstream, StreamFile streamFile, int offset, int spacing)
		{
			dsp_read_hist(vgmstream, streamFile, offset, spacing, 0);
		}
		public static void dsp_read_hist(Stream vgmstream, StreamFile streamFile, int offset, int spacing, int be)
		{
			int ch;
			/* get ADPCM hist */
			for (ch = 0; ch < vgmstream.channels; ch++)
			{
				vgmstream.ch[ch].adpcm_history1_16 = be != 0 ?
						read_16bitBE(offset + ch * spacing + 0 * 2, streamFile) :
						read_16bitLE(offset + ch * spacing + 0 * 2, streamFile); ;
				vgmstream.ch[ch].adpcm_history2_16 = be != 0 ?
						read_16bitBE(offset + ch * spacing + 1 * 2, streamFile) :
						read_16bitLE(offset + ch * spacing + 1 * 2, streamFile); ;
			}
		}


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
}
