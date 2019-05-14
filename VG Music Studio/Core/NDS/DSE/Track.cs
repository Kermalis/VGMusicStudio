using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    internal class Track
    {
        public readonly byte Index;
        public byte Octave;
        public byte Voice;
        public byte Expression, Volume;
        public sbyte Panpot;
        public uint Rest;
        public ushort PitchBend;
        public long LoopOffset;
        public bool Stopped;
        public int CurEvent;

        public readonly List<Channel> Channels = new List<Channel>(0x10);

        public Track(byte i)
        {
            Index = i;
        }

        public void Init()
        {
            Expression = Voice = Volume = 0;
            Octave = 4;
            Panpot = 0;
            Rest = 0;
            PitchBend = 0;
            LoopOffset = -1;
            Stopped = false;
            CurEvent = 0;
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
