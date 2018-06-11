namespace GBAMusicStudio.Core
{
    enum Command
    {
        // System events
        Tempo, // 1 - tempo

        // Track events
        NoteOn, // 3 - note, velocity, duration (-1 is a TIE)
        NoteOff, // 1 - note (-1 means to get the first TIEing)
        Rest, // 1 - duration
        Voice, // 1 - voice
        Volume, // 1 - volume (0 to 127)
        Panpot, // 1 - panpot (-64 to 63)
        Bend, // 1 - bend (-64 to 63)
        BendRange, // 1 - range
        LFOSpeed, // 1 - speed
        LFODelay, // 1 - delay
        MODDepth, // 1 - depth
        MODType, // 1 - type
        Tune, // 1 - tune (-64 to 63)
        Priority, // 1 - priority
        KeyShift, // 1 - shift
        GoTo, // 1 - offset
        Finish, // 0

        // GBA-Exclusive events
        PATT, // 1 - offset
        PEND, // 0
        REPT, // 1 - idk
        MEMACC, // 3 - idk, idk, idk
        XCMD, // 2 - idk, idk
    }

    internal class SongEvent
    {
        internal readonly uint Offset;

        internal readonly Command Command;
        internal readonly int[] Arguments;

        internal SongEvent(uint offset, Command command, int[] args)
        {
            Offset = offset;
            Command = command;
            Arguments = args;
        }

        public override string ToString() => Command.ToString();
    }
}
