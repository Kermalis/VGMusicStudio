using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K;

internal sealed class MP2KPCM8Channel : MP2KChannel
{
	private MP2KMixer _mixer;
	private MP2KSample _sInfo;
	private bool _isFixed;
	private bool _isSynth = false;
	private short _levelMPTcompressed = 0;
	private byte _shiftMPTcompressed = 0x38;

	private byte _envInterStep = 0;
	private byte _envLevelCur;
	private byte _envLevelPrev;
	private byte _leftVolCur = 0;
	private byte _leftVolPrev;
	private byte _rightVolCur = 0;
	private byte _rightVolPrev;

	private VoiceConfigType _type = VoiceConfigType.Invalid;

	private struct ProcArgs
	{
		internal float LVol;
		internal float RVol;
		internal float LVolStep;
		internal float RVolStep;
		internal float InterStep;
	};

	internal MP2KPCM8Channel(MP2KContext context, MP2KMixer mixer, MP2KTrack track, MP2KSample sInfo, ADSR env, NoteInfo note, bool isFixed)
		: base(track, note, env)
	{
		_mixer = mixer;
		_sInfo = sInfo;
		_isFixed = isFixed;
		if (sInfo.Header.LoopOffset == 0 && sInfo.Header.Length == 0)
		{
			// if (!((sInfo.Position + 16 + 8) >= MP2KEngine.MP2KInstance!.Config.ROM.Length))
			// {
			// 	Debug.WriteLine($"Sample Error: Sample data reaches beyond end of file: [{sInfo.Position:X8}]");
			// 	State = EnvelopeState.Dead;
			// 	return;
			// }

			if (sInfo.PCMData[1] == 0)
			{
				_type = VoiceConfigType.SynthPWM;
			}
			else if (sInfo.PCMData[1] == 1)
			{
				_type = VoiceConfigType.SynthSawtooth;
			}
			else
			{
				_type = VoiceConfigType.SynthTriangle;
			}

			_isSynth = true;
			return;
		}

		ResamplerType t = isFixed ? context.PlayerSoundMode.ResamplerTypeFixed : context.PlayerSoundMode.ResamplerTypeNormal;
		Rs = MP2KResampler.MakeResampler(t);

		if (sInfo.Header.Codec == CodecType.DPCM)
		{
			_type = VoiceConfigType.DPCM;
			long realEndPos = (sInfo.Header.Length + 63) / 64 * 0x21;
			// if (sInfo.Position + 16 + (realEndPos - sInfo.Position) >= MP2KEngine.MP2KInstance!.Config.ROM.Length)
			// {
			// 	Debug.WriteLine($"Sample Error: DPCM data reaches beyond end of file: [{sInfo.Position:X8}]");
			// 	State = EnvelopeState.Dead;
			// 	return;
			// }
		}
		else if ((uint)sInfo.Header.Length >= 0x80000000)
		{
			_type = VoiceConfigType.IMAADPCM;
			_sInfo.Header.Length = -_sInfo.Header.Length;
			if (!(sInfo.Position + 16 >= (_sInfo.Header.Length / 2u)))
			{
				Debug.WriteLine($"Sample Error: ADPCM data reaches beyond end of file: [{sInfo.Position:X8}]");
				State = EnvelopeState.Dead;
				return;
			}
		}
		else
		{
			_type = VoiceConfigType.PCM;
			// if (sInfo.Position + 16 + sInfo.Header.Length >= MP2KEngine.MP2KInstance!.Config.ROM.LongLength)
			// {
			// 	Debug.WriteLine($"Sample Error: PCM data reaches beyond end of file: [{sInfo.Position:X8}]");
			// 	State = EnvelopeState.Dead;
			// 	return;
			// }
		}
	}

	internal override void Process(Span<float> buffer, MixingArgs args)
	{
		if (State == EnvelopeState.Dead)
		{
			return;
		}

		StepEnvelope();
		if (State == EnvelopeState.Dead)
		{
			return;
		}

		if (buffer.Length == 0)
		{
			return;
		}

		float samplesPerBufferInv = 1.0f / buffer.Length;

		ChannelVolume vol = GetVolume();
		vol.FromVolLeft *= args.Volume;
		vol.FromVolRight *= args.Volume;
		vol.ToVolLeft *= args.Volume;
		vol.ToVolRight *= args.Volume;

		ProcArgs cargs = new()
		{
			LVolStep = (vol.ToVolLeft - vol.FromVolLeft) * samplesPerBufferInv,
			RVolStep = (vol.ToVolRight - vol.FromVolRight) * samplesPerBufferInv,
			LVol = vol.FromVolLeft,
			RVol = vol.FromVolRight
		};

		if (_isFixed && !_isSynth)
		{
			cargs.InterStep = args.FixedModeRate * args.SampleRateInv;
		}
		else
		{
			cargs.InterStep = Freq * args.SampleRateInv;
		}

		if (_isSynth)
		{
			cargs.InterStep /= 64.0f;
			switch (_type)
			{
				case VoiceConfigType.SynthPWM:
					ProcessModPulse(buffer, cargs, samplesPerBufferInv);
					break;
				case VoiceConfigType.SynthSawtooth:
					ProcessSaw(buffer, cargs);
					break;
				case VoiceConfigType.SynthTriangle:
					ProcessTri(buffer, cargs);
					break;
				default:
					throw new Exception("Invalid Voice Config Type");
			}
		}
		else
		{
			ProcessNormal(buffer, cargs);
		}
		UpdateVolFade();
	}

	internal override void SetVolume(byte vol, sbyte pan)
	{
		if (!Stop)
		{
			int combinedPan = Math.Clamp(pan + Note.RhythmPan, -128, +128);
			if (combinedPan >= 126)
			{
				combinedPan = 128;
			}

			_leftVolCur = (byte)Math.Clamp(Note.Velocity * vol * (-combinedPan + 128) >> 15, 0, 255);
			_rightVolCur = (byte)Math.Clamp(Note.Velocity * vol * (combinedPan + 128) >> 15, 0, 255);
		}
	}

	internal override ChannelVolume GetVolume()
	{
		float envBase = _envLevelPrev;
		float envDelta = (_envLevelCur - envBase) / GBAUtils.INTERFRAMES;
		float finalFromEnv = envBase + envDelta * _envInterStep;
		float finalToEnv = envBase + envDelta * (_envInterStep + 1);

		ChannelVolume retval;
		retval.FromVolLeft = _leftVolPrev * finalFromEnv * (1.0f / 65536.0f);
		retval.FromVolRight = _rightVolPrev * finalFromEnv * (1.0f / 65536.0f);
		retval.ToVolLeft = _leftVolCur * finalToEnv * (1.0f / 65536.0f);
		retval.ToVolRight = _rightVolCur * finalToEnv * (1.0f / 65536.0f);
		return retval;
	}

	internal override void Release()
	{
		Stop = true;
	}

	internal override bool IsReleasing()
	{
		return Stop;
	}

	internal override void SetPitch(short pitch)
	{
		if (!Stop || Freq <= 0.0f)
		{
			Freq = _sInfo.MidCFrequency
				   * MathF.Pow(2.0f, ((Note.OriginalNote - 60) / 12f) + (pitch / 768f));
		}
	}

	internal override bool TickNote()
	{
		if (!Stop)
		{
			if (Note.Duration > 0)
			{
				Note.Duration--;
				if (Note.Duration == 0)
				{
					Stop = true;
					return false;
				}
			}
			return true;
		}
		else
		{
			return false;
		}
	}

	internal override VoiceConfigType GetVoiceType()
	{
		return _type switch
		{
			VoiceConfigType.PCM => VoiceConfigType.PCM,
			VoiceConfigType.DPCM => VoiceConfigType.DPCM,
			VoiceConfigType.IMAADPCM => VoiceConfigType.IMAADPCM,
			VoiceConfigType.SynthPWM => VoiceConfigType.SynthPWM,
			VoiceConfigType.SynthSawtooth => VoiceConfigType.SynthSawtooth,
			VoiceConfigType.SynthTriangle => VoiceConfigType.SynthTriangle,
			_ => VoiceConfigType.None,
		};
	}

	private void StepEnvelope()
	{
		void Release()
		{
			if (Note.PseudoEchoVolume == 0 || Note.PseudoEchoLength == 0)
			{
				State = EnvelopeState.Dying;
				_envLevelCur = 0;
			}
			else
			{
				State = EnvelopeState.PseudoEcho;
				_envLevelCur = Note.PseudoEchoVolume;
			}
		}
		if (State == EnvelopeState.Initializing)
		{
			if (Stop)
			{
				State = EnvelopeState.Dead;
				return;
			}
			UpdateVolFade();

			if (Env.A == 0xFF)
			{
				_envLevelPrev = 0xFF;
			}
			else
			{
				_envLevelPrev = 0x0;
			}

			_envLevelCur = 0;
			_envInterStep = 0;
			State = EnvelopeState.Rising;
		}
		else
		{
			if (++_envInterStep < GBAUtils.INTERFRAMES)
			{
				return;
			}

			_envLevelPrev = _envLevelCur;
			_envInterStep = 0;
		}

		if (State == EnvelopeState.PseudoEcho)
		{
			if (--Note.PseudoEchoLength == 0)
			{
				State = EnvelopeState.Dying;
				_envLevelCur = 0;
			}
		}
		else if (Stop)
		{
			if (State == EnvelopeState.Dying)
			{
				State = EnvelopeState.Dead;
			}
			else
			{
				_envLevelCur = (byte)((_envLevelCur * Env.R) >> 8);
				if (_envLevelCur <= Note.PseudoEchoVolume)
				{
					Release();
				}
			}
		}
		else
		{
			if (State == EnvelopeState.Decaying)
			{
				_envLevelCur = (byte)((_envLevelCur * Env.D) >> 8);
				if (_envLevelCur <= Env.S)
				{
					_envLevelCur = Env.S;
					if (_envLevelCur == 0)
					{
						Release();
					}

					State = EnvelopeState.Playing;
				}
			}
			else if (State == EnvelopeState.Rising)
			{
				uint newLevel = (uint)(_envLevelCur + Env.A);
				if (newLevel >= 0xFF)
				{
					_envLevelCur = 0xFF;
					State = EnvelopeState.Decaying;
				}
				else
				{
					_envLevelCur = (byte)newLevel;
				}
			}
		}
	}

	private void UpdateVolFade()
	{
		_leftVolPrev = _leftVolCur;
		_rightVolPrev = _rightVolCur;
	}

	private void ProcessNormal(Span<float> buffer, ProcArgs cargs)
	{
		if (buffer.Length == 0)
		{
			return;
		}

		MP2KResampler.FetchCallback cb;
		if (_type == VoiceConfigType.PCM)
		{
			// cb = new(SampleFetchCallback);
			Process_Standard(buffer, GetVolume(), cargs.InterStep);
		}
		else if (_type == VoiceConfigType.DPCM)
		{
			cb = new(SampleFetchCallbackGFDPCMDecomp);
		}
		else if (_type == VoiceConfigType.IMAADPCM)
		{
			cb = new(SampleFetchCallbackMPTDecomp);
		}
		else
		{
			throw new InvalidEnumArgumentException();
		}

		// bool running = Rs!.Process(_mixer.TrackBuffers[Track.Index], cargs.InterStep, cb);

		for (int i = 0; i < buffer.Length; i += 2)
		{
			float samp = buffer[i];
			buffer[i] += (byte)(samp * cargs.LVol);
			buffer[i + 1] += (byte)(samp * cargs.RVol);
			cargs.LVol += cargs.LVolStep;
			cargs.RVol += cargs.RVolStep;
		}
		// if (!running)
		// {
		// 	Kill();
		// }
	}

	private void ProcessModPulse(Span<float> buffer, ProcArgs cargs, float samplesPerBufferInv)
	{
		const byte DUTY_BASE = 2;
		const byte DUTY_STEP = 3;
		const byte DEPTH = 4;
		const byte INIT_DUTY = 5;
		long fromPos;

		if (_envInterStep == 0)
		{
			fromPos = Pos += (uint)(_sInfo.PCMData[DUTY_STEP] << 24);
		}
		else
		{
			fromPos = (uint)Pos;
		}

		long toPos = fromPos + (uint)(_sInfo.PCMData[DUTY_STEP] << 24);

		static float CalcThresh(long val, byte bbase, byte depth, byte init)
		{
			long iThreshold = (uint)(init << 24) + val;
			iThreshold = (int)iThreshold < 0 ? ~iThreshold >> 8 : iThreshold >> 8;
			iThreshold = iThreshold * depth + (uint)(bbase << 24);
			return iThreshold / (float)0x100000000;
		}

		float fromThresh = CalcThresh(
			fromPos,
			(byte)_sInfo.PCMData[DUTY_BASE],
			(byte)_sInfo.PCMData[DEPTH],
			(byte)_sInfo.PCMData[INIT_DUTY]
		);
		float toThresh = CalcThresh(
			toPos,
			(byte)_sInfo.PCMData[DUTY_BASE],
			(byte)_sInfo.PCMData[DEPTH],
			(byte)_sInfo.PCMData[INIT_DUTY]
		);

		float deltaThresh = toThresh - fromThresh;
		float baseThresh = fromThresh + (deltaThresh * (_envInterStep * (1.0f / GBAUtils.INTERFRAMES)));
		float threshStep = deltaThresh * (1.0f / GBAUtils.INTERFRAMES) * samplesPerBufferInv;
		float fThreshold = baseThresh;

		for (int i = 0; i < buffer.Length; i += 2)
		{
			float baseSamp = InterPos < fThreshold ? 0.5f : -0.5f;
			baseSamp += 0.5f - fThreshold;
			fThreshold += threshStep;
			buffer[i] += (byte)(baseSamp * cargs.LVol);
			buffer[i + 1] += (byte)(baseSamp * cargs.RVol);

			cargs.LVol += cargs.LVolStep;
			cargs.RVol += cargs.RVolStep;

			InterPos += cargs.InterStep;
			if (InterPos >= 1.0f)
			{
				InterPos -= 1.0f;
			}
		}
	}

	private void ProcessSaw(Span<float> buffer, ProcArgs cargs)
	{
		const uint fix = 0x70;

		for (int i = 0; i < buffer.Length; i += 2)
		{
			InterPos += cargs.InterStep;
			if (InterPos >= 1.0f)
			{
				InterPos -= 1.0f;
			}

			uint var1 = (uint)(InterPos * 256) - fix;
			uint var2 = (uint)(InterPos * 65536.0f) << 17;
			uint var3 = var1 - (var2 >> 27);
			Pos = var3 + (uint)((int)Pos >> 1);

			float baseSamp = (int)Pos / 256.0f;

			buffer[i] += (byte)(baseSamp * cargs.LVol);
			buffer[i + 1] += (byte)(baseSamp * cargs.RVol);

			cargs.LVol += cargs.LVolStep;
			cargs.RVol += cargs.RVolStep;
		}
	}

	private void ProcessTri(Span<float> buffer, ProcArgs cargs)
	{
		for (int i = 0; i < buffer.Length; i += 2)
		{
			InterPos += cargs.InterStep;
			if (InterPos >= 1.0f)
			{
				InterPos -= 1.0f;
			}

			float baseSamp;
			if (InterPos < 0.5f)
			{
				baseSamp = (4.0f * InterPos) - 1.0f;
			}
			else
			{
				baseSamp = 3.0f - (4.0f * InterPos);
			}

			buffer[i] += (byte)(baseSamp * cargs.LVol);
			buffer[i + 1] += (byte)(baseSamp * cargs.RVol);

			cargs.LVol += cargs.LVolStep;
			cargs.RVol += cargs.RVolStep;
		}
	}

	private void Process_Standard(Span<float> buffer, ChannelVolume vol, float interStep)
	{
		int bufPos = 0;
		int samplesPerBuffer;
		samplesPerBuffer = _mixer!.SamplesPerBuffer;

		do
		{
			float samp = _sInfo.PCMData[Pos] / (float)0x80;

			buffer[bufPos++] += samp * vol.FromVolLeft;
			buffer[bufPos++] += samp * vol.FromVolRight;

			InterPos += interStep;
			int posDelta = (int)InterPos;
			InterPos -= posDelta;
			Pos += posDelta;
			if (Pos >= _sInfo.Header.Length)
			{
				if (_sInfo.Header.DoesLoop != SampleHeader.LOOP_TRUE)
				{
					Kill();
					return;
				}

				Pos = _sInfo.Header.LoopOffset;
			}
		} while (--samplesPerBuffer > 0);
	}

	private bool SampleFetchCallback(ref List<float> fetchBuffer, int samplesRequired)
	{
		if (fetchBuffer.Count >= samplesRequired)
		{
			return true;
		}

		int samplesToFetch = samplesRequired - fetchBuffer.Count;
		int i = fetchBuffer.Count;

		do
		{
			int samplesTilLoop = (int)(_sInfo.Header.Length - Pos);
			int thisFetch = Math.Min(samplesTilLoop, samplesToFetch);

			samplesToFetch -= thisFetch;
			do
			{
				if (i >= fetchBuffer.Count)
				{
					fetchBuffer.Add(_sInfo.PCMData[Pos++] / 128.0f);
					i++;
				}
				else
				{
					fetchBuffer[i++] = _sInfo.PCMData[Pos++] / 128.0f;
				}
			} while (--thisFetch > 0);

			if (Pos >= _sInfo.Header.Length)
			{
				if (_sInfo.LoopEnabled)
				{
					Pos = _sInfo.Header.LoopOffset;
				}
				else
				{
					fetchBuffer.Clear();
					return false;
				}
			}
		} while (samplesToFetch > 0);
		return true;
	}

	private bool SampleFetchCallbackGFDPCMDecomp(ref List<float> fetchBuffer, int samplesRequired)
	{
		const byte DPCM_BLOCK_SIZE = 64;
		if (fetchBuffer.Count >= samplesRequired)
		{
			return true;
		}

		int samplesToFetch = samplesRequired - fetchBuffer.Count;
		int i = fetchBuffer.Count;

		Span<sbyte> decodeBuffer = stackalloc sbyte[DPCM_BLOCK_SIZE];
		int decodedBlockIdx = ~0;

		do
		{
			int samplesTilLoop = (int)(_sInfo.Header.Length - Pos);
			int thisFetch = Math.Min(samplesTilLoop, samplesToFetch);

			samplesToFetch -= thisFetch;
			do
			{
				int currentBlock = (int)(Pos / DPCM_BLOCK_SIZE);
				if (decodedBlockIdx != currentBlock)
				{
					sbyte[] deltaTable = [
						0, 1, 4, 9, 16, 25, 36, 49, -64, -49, -36, -25, -16, -9, -4, -1
					];

					int currentBlockPos = currentBlock * 0x21;

					sbyte acc = _sInfo.PCMData[currentBlockPos];
					decodeBuffer[0] = acc;
					acc += deltaTable[_sInfo.PCMData[currentBlockPos + 1] & 0xF];
					decodeBuffer[1] = acc;
					for (int j = 2, h = 2; j < DPCM_BLOCK_SIZE; j += 2, h++)
					{
						acc += deltaTable[(_sInfo.PCMData[currentBlockPos + h] & 0xF0) >> 4];
						decodeBuffer[j + 0] = acc;
						acc += deltaTable[_sInfo.PCMData[currentBlockPos + h] & 0xF];
						decodeBuffer[j + 1] = acc;
					}
					decodedBlockIdx = currentBlock;
				}

				fetchBuffer.Add(decodeBuffer[(int)(Pos++ % DPCM_BLOCK_SIZE)] / 128.0f);
			} while (--thisFetch > 0);

			if (Pos >= _sInfo.Header.Length)
			{
				if (_sInfo.LoopEnabled)
				{
					Pos = _sInfo.Header.LoopOffset;
				}
				else
				{
					fetchBuffer.Clear();
					return false;
				}
			}
		} while (samplesToFetch > 0);
		return true;
	}

	private bool SampleFetchCallbackMPTDecomp(ref List<float> fetchBuffer, int samplesRequired)
	{
		if (fetchBuffer.Count >= samplesRequired)
		{
			return true;
		}

		int samplesToFetch = samplesRequired - fetchBuffer.Count;
		int i = fetchBuffer.Count;

		do
		{
			int samplesTilLoop = (int)(_sInfo.Header.Length - Pos);
			int thisFetch = Math.Min(samplesTilLoop, samplesToFetch);

			samplesToFetch -= thisFetch;
			do
			{
				bool loNibble = (Pos & 1) != 0;
				long samplePos = Pos++ >> (int)1u;
				sbyte data = _sInfo.PCMData[samplePos];

				uint nibble;
				if (loNibble)
				{
					nibble = (uint)(data << 28) & 0xF0000000u;
				}
				else
				{
					nibble = (uint)(data << 24) & 0xF0000000u;
				}

				if (_shiftMPTcompressed <= 63)
				{
					int actualShift = _shiftMPTcompressed >> (int)1u;
					_levelMPTcompressed = (short)(_levelMPTcompressed + ((int)nibble >> actualShift));
				}

				if ((nibble & 0x80000000) != 0)
				{
					nibble = (uint)-(int)nibble;
				}

				_shiftMPTcompressed = (byte)(_shiftMPTcompressed + 4);
				_shiftMPTcompressed = (byte)(_shiftMPTcompressed - (nibble >> (int)28u));

				fetchBuffer.Add(_levelMPTcompressed / 128.0f);
			} while (--thisFetch > 0);

			if (Pos >= _sInfo.Header.Length)
			{
				fetchBuffer.Clear();
				return false;
			}
		} while (samplesToFetch > 0);
		return true;
	}
}