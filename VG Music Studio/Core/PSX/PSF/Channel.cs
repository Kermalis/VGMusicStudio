namespace Kermalis.VGMusicStudio.Core.PSX.PSF
{
    internal class Channel
    {
        public readonly byte Index;

        public Track Owner;
        public ushort BaseTimer, Timer;
        public int NoteDuration;
        public byte NoteVelocity;
        public byte Volume = 0x7F;
        public sbyte Pan = 0x40;
        public byte Key;

        private int pos;
        private short prevLeft, prevRight;

        // PSG
        private byte psgDuty;
        private int psgCounter;

        public Channel(byte i)
        {
            Index = i;
        }

        public void StartPSG(byte duty, int noteDuration)
        {
            psgCounter = 0;
            psgDuty = duty;
            BaseTimer = 8006;
            Start(noteDuration);
        }

        private void Start(int noteDuration)
        {
            //State = EnvelopeState.Attack;
            //Velocity = -92544;
            pos = 0;
            prevLeft = prevRight = 0;
            NoteDuration = noteDuration;
        }

        public void Stop()
        {
            if (Owner != null)
            {
                Owner.Channels.Remove(this);
            }
            Owner = null;
            Volume = 0;
        }

        public void Process(out short left, out short right)
        {
            if (Timer != 0)
            {
                int numSamples = (pos + 0x100) / Timer;
                pos = (pos + 0x100) % Timer;
                // prevLeft and prevRight are stored because numSamples can be 0.
                for (int i = 0; i < numSamples; i++)
                {
                    short samp = psgCounter <= psgDuty ? short.MinValue : short.MaxValue;
                    psgCounter++;
                    if (psgCounter >= 8)
                    {
                        psgCounter = 0;
                    }
                    samp = (short)(samp * Volume / 0x7F);
                    prevLeft = (short)(samp * (-Pan + 0x40) / 0x80);
                    prevRight = (short)(samp * (Pan + 0x40) / 0x80);
                }
            }
            left = prevLeft;
            right = prevRight;
        }
    }
}
