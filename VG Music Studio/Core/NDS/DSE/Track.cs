using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    internal class Track
    {
        public readonly byte Index;
        private readonly long _startOffset;
        public byte Octave;
        public byte Voice;
        public byte Expression;
        public byte Volume;
        public sbyte Panpot;
        public uint Rest;
        public ushort PitchBend;
        public long CurOffset;
        public long LoopOffset;
        public bool Stopped;
        public uint LastNoteDuration;
        public uint LastRest;

        public readonly List<Channel> Channels = new List<Channel>(0x10);

        public Track(byte i, long startOffset)
        {
            Index = i;
            _startOffset = startOffset;
        }

        public void Init()
        {
            Expression = 0;
            Voice = 0;
            Volume = 0;
            Octave = 4;
            Panpot = 0;
            Rest = 0;
            PitchBend = 0;
            CurOffset = _startOffset;
            LoopOffset = -1;
            Stopped = false;
            LastNoteDuration = 0;
            LastRest = 0;
            StopAllChannels();
        }

        public void Tick()
        {
            if (Rest > 0)
            {
                Rest--;
            }
            for (int i = 0; i < Channels.Count; i++)
            {
                Channel c = Channels[i];
                if (c.NoteLength > 0)
                {
                    c.NoteLength--;
                }
            }
        }

        public void StopAllChannels()
        {
            Channel[] chans = Channels.ToArray();
            for (int i = 0; i < chans.Length; i++)
            {
                chans[i].Stop();
            }
        }
    }
}
