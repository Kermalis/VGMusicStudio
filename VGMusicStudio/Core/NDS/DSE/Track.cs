using Kermalis.EndianBinaryIO;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    class Track
    {
        public readonly byte Index;
        public readonly EndianBinaryReader Reader;
        public readonly long StartOffset;
        public int Priority; // Unused
        public byte Octave;
        public byte Voice;
        public byte Expression, Volume;
        public sbyte Panpot;
        public uint LastDelay, Delay;
        public long LoopOffset;
        public bool Stopped;
        public uint LastNoteDuration;

        public readonly List<Channel> Channels = new List<Channel>(0x10);

        public Track(byte i, byte[] smdl, long startOffset)
        {
            Index = i;
            Reader = new EndianBinaryReader(new MemoryStream(smdl));
            StartOffset = startOffset;

            //Reader.BaseStream.Position = startOffset + 0x10;
            //Reader.ReadByte(); // Track Id
            //Reader.ReadByte(); // Channel Id
            //Reader.ReadBytes(2); // Unknown
        }

        public void Init()
        {
            Reader.BaseStream.Position = StartOffset + 0x14;
            Priority = 0;
            Expression = Voice = Octave = Volume = 0;
            Panpot = 0;
            LastDelay = Delay = LastNoteDuration = 0;
            LoopOffset = -1;
            Stopped = false;
            StopAllChannels();
        }

        public void Tick()
        {
            if (Delay > 0)
            {
                Delay--;
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
