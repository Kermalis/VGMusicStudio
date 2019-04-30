using Kermalis.EndianBinaryIO;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.GBA.MLSS
{
    internal class Track
    {
        public readonly byte Index;
        public readonly string Type;
        public readonly EndianBinaryReader Reader;
        public readonly Channel Channel;

        public byte Voice, BendRange, Volume, Delay, PrevCmd, NoteDuration;
        public sbyte Bend, Panpot;
        public bool Enabled, Stopped;
        public long StartOffset;

        public int GetPitch()
        {
            return Bend * (BendRange / 2);
        }

        public Track(byte i, byte[] rom, Mixer mixer)
        {
            Index = i;
            Type = i >= 8 ? i % 2 == 0 ? "Square 1" : "Square 2" : "PCM8";
            Reader = new EndianBinaryReader(new MemoryStream(rom));
            Channel = i >= 8 ? (Channel)new SquareChannel(mixer) : new PCMChannel(mixer);
        }
        public void Init()
        {
            Reader.BaseStream.Position = StartOffset;
            Voice = Delay = BendRange = NoteDuration = 0;
            Bend = Panpot = 0;
            Stopped = false;
            Volume = 0x7F;
            PrevCmd = 0xFF;
        }
        public void Tick()
        {
            if (Delay != 0)
            {
                Delay--;
            }
            if (NoteDuration > 0)
            {
                NoteDuration--;
            }
        }
    }
}
