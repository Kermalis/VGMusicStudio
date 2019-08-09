using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.PSX.PSF
{
    internal class Track
    {
        public readonly byte Index;

        public byte Voice;
        public bool Stopped;
        public ushort PitchBend;

        public readonly List<Channel> Channels = new List<Channel>(0x10);

        public Track(byte i)
        {
            Index = i;
        }
        public void Init()
        {
            Voice = 0;
            PitchBend = 0;
            Stopped = false;
            StopAllChannels();
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
