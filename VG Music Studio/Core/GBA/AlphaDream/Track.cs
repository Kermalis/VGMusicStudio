namespace Kermalis.VGMusicStudio.Core.GBA.AlphaDream
{
    internal class Track
    {
        public readonly byte Index;
        public readonly string Type;
        public readonly Channel Channel;

        public byte Voice;
        public byte PitchBendRange;
        public byte Volume;
        public byte Rest;
        public byte NoteDuration;
        public sbyte PitchBend;
        public sbyte Panpot;
        public bool Enabled;
        public bool Stopped;
        public int StartOffset;
        public int DataOffset;
        public byte PrevCommand;

        public int GetPitch()
        {
            return PitchBend * (PitchBendRange / 2);
        }

        public Track(byte i, Mixer mixer)
        {
            Index = i;
            if (i >= 8)
            {
                Type = Utils.PSGTypes[i & 3];
                Channel = new SquareChannel(mixer); // TODO: PSG Channels 3 and 4
            }
            else
            {
                Type = "PCM8";
                Channel = new PCMChannel(mixer);
            }
        }
        // 0x819B040
        public void Init()
        {
            Voice = 0;
            Rest = 1; // Unsure why Rest starts at 1
            PitchBendRange = 2;
            NoteDuration = 0;
            PitchBend = 0;
            Panpot = 0; // Start centered; ROM sets this to 0x7F since it's unsigned there
            DataOffset = StartOffset;
            Stopped = false;
            Volume = 200;
            PrevCommand = 0xFF;
            //Tempo = 120;
            //TempoStack = 0;
        }
        public void Tick()
        {
            if (Rest != 0)
            {
                Rest--;
            }
            if (NoteDuration > 0)
            {
                NoteDuration--;
            }
        }
    }
}
