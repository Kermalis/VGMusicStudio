using Kermalis.MusicStudio.Core.NDS.SDAT;
using Kermalis.MusicStudio.Util;
using NAudio.Wave;

namespace Kermalis.MusicStudio.Core.NDS.DSE
{
    class DSEMixer : Mixer
    {
        public Channel[] Channels;

        readonly BufferedWaveProvider buffer;

        public DSEMixer()
        {
            Channels = new Channel[0x20];
            for (byte i = 0; i < Channels.Length; i++)
            {
                Channels[i] = new Channel(i);
            }

            Mutes = new bool[0x10];

            buffer = new BufferedWaveProvider(new WaveFormat(65456, 16, 2))
            {
                DiscardOnBufferOverflow = true,
                BufferLength = 0x5540
            };
            Init(buffer);
        }

        public Channel AllocateChannel(Track track)
        {
            int GetScore(Channel c)
            {
                // Free channels should be used before releasing channels which should be used before track priority
                return c.Owner == null ? -2 : c.State == EnvelopeState.Release ? -1 : c.Owner.Priority;
            }
            Channel nChan = null;
            for (int i = 0; i < Channels.Length; i++)
            {
                Channel c = Channels[i];
                if (nChan != null)
                {
                    int nScore = GetScore(nChan);
                    int cScore = GetScore(c);
                    if (cScore <= nScore && (cScore < nScore || c.Volume <= nChan.Volume))
                    {
                        nChan = c;
                    }
                }
                else
                {
                    nChan = c;
                }
            }
            if (nChan != null && track.Priority >= GetScore(nChan))
            {
                return nChan;
            }
            else
            {
                return null;
            }
        }

        public void ChannelTick()
        {
            for (int i = 0; i < Channels.Length; i++)
            {
                Channel chan = Channels[i];
                if (chan.Owner != null)
                {
                    chan.StepEnvelope();
                    if (chan.NoteLength == 0)
                    {
                        chan.State = EnvelopeState.Release;
                    }
                    int vol = SDATUtils.SustainTable[chan.NoteVelocity] + chan.Velocity + SDATUtils.SustainTable[chan.Owner.Volume] + SDATUtils.SustainTable[chan.Owner.Expression];
                    //int pitch = ((chan.Key - chan.BaseKey) << 6) + chan.SweepMain() + chan.Owner.GetPitch(); // "<< 6" is "* 0x40"
                    int pitch = ((chan.Key - chan.RootKey) << 6); // "<< 6" is "* 0x40"
                    if (chan.State == EnvelopeState.Release && vol <= -92544)
                    {
                        chan.Stop();
                    }
                    else
                    {
                        chan.Volume = SDATUtils.GetChannelVolume(vol);
                        chan.Panpot = chan.Owner.Panpot;
                        chan.Timer = SDATUtils.GetChannelTimer(chan.BaseTimer, pitch);
                    }
                }
            }
        }

        // Called 192 times a second
        public void Process()
        {
            for (int i = 0; i < 0x155; i++) // 0x155 (SamplesPerBuffer) == 0x5540/0x40
            {
                int left = 0, right = 0;
                for (int j = 0; j < Channels.Length; j++)
                {
                    Channel chan = Channels[j];
                    if (chan.Owner != null)
                    {
                        bool muted = Mutes[chan.Owner.Index - 1]; // Get mute first because chan.Process() can call chan.Stop() which sets chan.Owner to null
                        chan.Process(out short channelLeft, out short channelRight);
                        if (!muted)
                        {
                            left += channelLeft;
                            right += channelRight;
                        }
                    }
                }
                left = Utils.Clamp(left, short.MinValue, short.MaxValue);
                right = Utils.Clamp(right, short.MinValue, short.MaxValue);
                // Convert two shorts to four bytes
                buffer.AddSamples(new byte[] { (byte)(left & 0xFF), (byte)((left >> 8) & 0xFF), (byte)(right & 0xFF), (byte)((right >> 8) & 0xFF) }, 0, 4);
            }
        }
    }
}
