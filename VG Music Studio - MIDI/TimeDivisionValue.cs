namespace Kermalis.VGMusicStudio.MIDI;

public enum DivisionType : byte
{
	PPQN,
	SMPTE,
}

public enum SMPTEFormat : byte
{
	Smpte24 = 24,
	Smpte25 = 25,
	Smpte30Drop = 29,
	Smpte30 = 30,
}

// Section 2.1
public readonly struct TimeDivisionValue
{
	public const int PPQN_MIN_DIVISION = 24;

	public readonly ushort RawValue;

	public DivisionType Type => (DivisionType)(RawValue >> 15);

	public ushort PPQN_TicksPerQuarterNote => RawValue; // Type bit is already 0

	public SMPTEFormat SMPTE_Format => (SMPTEFormat)(-(sbyte)(RawValue >> 8)); // Upper 8 bits, negated
	public byte SMPTE_TicksPerFrame => (byte)RawValue; // Lower 8 bits

	public TimeDivisionValue(ushort rawValue)
	{
		RawValue = rawValue;
	}

	public static TimeDivisionValue CreatePPQN(ushort ticksPerQuarterNote)
	{
		return new TimeDivisionValue(ticksPerQuarterNote);
	}
	public static TimeDivisionValue CreateSMPTE(SMPTEFormat format, byte ticksPerFrame)
	{
		ushort rawValue = (ushort)((-(sbyte)format) << 8);
		rawValue |= ticksPerFrame;

		return new TimeDivisionValue(rawValue);
	}

	public bool Validate()
	{
		if (Type == DivisionType.PPQN)
		{
			return PPQN_TicksPerQuarterNote >= PPQN_MIN_DIVISION;
		}

		// SMPTE
		return SMPTE_Format is SMPTEFormat.Smpte24 or SMPTEFormat.Smpte25 or SMPTEFormat.Smpte30Drop or SMPTEFormat.Smpte30;
	}

	public override string ToString()
	{
		return string.Format("0x{0:X4}", RawValue);
	}
}
