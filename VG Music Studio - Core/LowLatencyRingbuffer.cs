// Based on ipatix's implementation from agbplay_v2 branch.
// Original sources:
//    https://github.com/ipatix/agbplay/blob/agbplay_v2/src/agbplay/LowLatencyRingbuffer.cpp
//    https://github.com/ipatix/agbplay/blob/agbplay_v2/src/agbplay/LowLatencyRingbuffer.hpp
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core;

internal class LowLatencyRingbuffer
{
	internal struct Sample
	{
		internal float left;
		internal float right;
	}

	private readonly object? lockObj = new();
	private readonly object? cv = new();
	private List<Sample>? buffer;

	// Free variables
	private int freePos = 0;
	private int freeCount = 0;

	// Data variables
	private int dataPos = 0;
	private int dataCount = 0;

	// Atomic variable for System.Threading.Interlocked
	private int lastTake = 0;

	// Last put value
	private int lastPut = 0;

	// Number of buffers (beginning from 1)
	private int numBuffers = 1;

	// Made by cwills on StackOverflow, found here:
	// https://stackoverflow.com/questions/8546/is-there-a-try-to-lock-skip-if-timed-out-operation-in-c/23728163#23728163
	internal class TryLock : IDisposable
	{
		private object? locked;

		internal bool OwnsLock { get; private set; }

		internal TryLock(object obj)
		{
			if (Monitor.TryEnter(obj))
			{
				OwnsLock = true;
				locked = obj;
			}
		}

		public void Dispose()
		{
			if (OwnsLock)
			{
				Monitor.Exit(locked!);
				locked = null;
				OwnsLock = false;
			}
		}
	}

	public LowLatencyRingbuffer()
	{
		Reset();
	}

	public void Reset()
	{
		lock (lockObj!)
		{
			dataCount = 0;
			dataPos = 0;
			if (buffer is not null)
			{
				freeCount = buffer.Count;
			}
			freePos = 0;
		}
	}

	public void SetNumBuffers(int numBuffers)
	{
		lock (lockObj!)
		{
			if (numBuffers is 0)
				numBuffers = 1;
			this.numBuffers = numBuffers;
		}
	}

	public void Put(Span<Sample> inBuffer)
	{
		lastPut = inBuffer.Length;

		lock (lockObj!)
		{
			int bufferedNumBuffers = numBuffers;
			int bufferedLastTake = lastTake;
			int requiredBufferSize = bufferedNumBuffers * bufferedLastTake + lastPut;

			if (buffer!.Count < requiredBufferSize)
			{
				IncreaseBufferSize(requiredBufferSize);
			}

			while (dataCount > bufferedNumBuffers * bufferedLastTake)
			{
				Monitor.Wait(cv!);
			}

			while (inBuffer.Length > 0)
			{
				int elementsPut = PutSome(inBuffer);
				inBuffer = inBuffer.Slice(elementsPut);
			}
		}
	}

	public void Take(Span<Sample> outBuffer)
	{
		lastTake = outBuffer.Length;

		using (var tl = new TryLock(lockObj!))
		{
			if (!tl.OwnsLock || outBuffer.Length > dataCount)
			{
				outBuffer.Fill(new Sample{left = 0.0f, right = 0.0f});
				//Array.Fill(outBuffer.ToArray(), new Sample { left = 0.0f, right = 0.0f }, 0, outBuffer.Length);
				return;
			}

			while (outBuffer.Length > 0)
			{
				int elementsTaken = TakeSome(outBuffer);
				outBuffer = outBuffer.Slice(elementsTaken);
			}

			Monitor.Pulse(cv!);
		}
	}

	private int PutSome(Span<Sample> inBuffer)
	{
		Debug.Assert(inBuffer.Length <= freeCount);
		bool wrap = inBuffer.Length >= (buffer!.Count - freePos);

		int putCount;
		int newFreePos;
		if (wrap)
		{
			putCount = buffer.Count - freePos;
			newFreePos = 0;
		}
		else
		{
			putCount = buffer.Count;
			newFreePos = freePos + inBuffer.Length;
		}

		Array.Copy(inBuffer.ToArray(), 0, buffer.ToArray(), 0 + freePos, putCount);

		freePos = newFreePos;
		Debug.Assert(freeCount >= putCount);
		freeCount -= putCount;
		dataCount += putCount;
		return putCount;
	}

	private int TakeSome(Span<Sample> outBuffer)
	{
		Debug.Assert(outBuffer.Length <= dataCount);
		bool wrap = outBuffer.Length >= (buffer!.Count - dataPos);

		int takeCount;
		int newDataPos;
		if (wrap)
		{
			takeCount = buffer.Count - dataPos;
			newDataPos = 0;
		}
		else
		{
			takeCount = outBuffer.Length;
			newDataPos = dataPos + outBuffer.Length;
		}

		Array.Copy(buffer.ToArray(), 0 + dataPos, outBuffer.ToArray(), 0, takeCount);

		dataPos = newDataPos;
		freeCount += takeCount;
		Debug.Assert(dataCount >= takeCount);
		dataCount -= takeCount;
		return takeCount;
	}

	private void IncreaseBufferSize(int requiredBufferSize)
	{
		List<Sample> backupBuffer = new(new Sample[dataCount]);
		int beforeWraparoundSize = Math.Min(dataCount, buffer.Count - dataPos);
		Span<Sample> beforeWraparound = new Span<Sample>(new Sample[dataCount], 0 + dataPos, beforeWraparoundSize);
		int afterWraparoundSize = dataCount - beforeWraparoundSize;
		Span<Sample> afterWraparound = new Span<Sample>(new Sample[dataCount], 0, afterWraparoundSize);
		Array.Copy(buffer.ToArray(), 0 + dataPos, backupBuffer.ToArray(), 0, beforeWraparoundSize);
		Array.Copy(buffer.ToArray(), 0, backupBuffer.ToArray(), 0 + beforeWraparoundSize, afterWraparoundSize);
		Debug.Assert(beforeWraparoundSize + afterWraparoundSize == dataCount);
		Debug.Assert(dataCount <= requiredBufferSize);

		buffer.EnsureCapacity(requiredBufferSize);
		//buffer.CopyTo([.. backupBuffer]);
		Array.Copy(backupBuffer.ToArray(), 0, buffer.ToArray(), 0, dataCount);
		Array.Fill(buffer.ToArray(), new Sample { left = 0.0f, right = 0.0f }, 0 + dataCount, buffer.Count);

		dataPos = 0;
		freeCount = buffer.Count - dataCount;
		freePos = dataCount;
	}
}