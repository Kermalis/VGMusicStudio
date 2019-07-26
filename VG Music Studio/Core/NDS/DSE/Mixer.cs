using NAudio.Wave;
using System;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    internal class Mixer : Core.Mixer
    {
        private const int numChannels = 0x20;
        private readonly float samplesReciprocal;
        private readonly int samplesPerBuffer;
        private long fadeMicroFramesLeft;
        private float fadePos;
        private float fadeStepPerMicroframe;

        private readonly Channel[] channels;
        private readonly BufferedWaveProvider buffer;

        public Mixer()
        {
            // The sampling frequency of the mixer is 1.04876 MHz with an amplitude resolution of 24 bits, but the sampling frequency after mixing with PWM modulation is 32.768 kHz with an amplitude resolution of 10 bits.
            // - gbatek
            // I'm not using either of those because the samples per buffer leads to an overflow eventually
            const int sampleRate = 65456;
            samplesPerBuffer = 341; // TODO
            samplesReciprocal = 1f / samplesPerBuffer;

            channels = new Channel[numChannels];
            for (byte i = 0; i < numChannels; i++)
            {
                channels[i] = new Channel(i);
            }

            buffer = new BufferedWaveProvider(new WaveFormat(sampleRate, 16, 2))
            {
                DiscardOnBufferOverflow = true,
                BufferLength = samplesPerBuffer * 64
            };
            Init(buffer);
        }
        public override void Dispose()
        {
            base.Dispose();
            CloseWaveWriter();
        }

        public Channel AllocateChannel()
        {
            int GetScore(Channel c)
            {
                // Free channels should be used before releasing channels
                return c.Owner == null ? -2 : Utils.IsStateRemovable(c.State) ? -1 : 0;
            }
            Channel nChan = null;
            for (int i = 0; i < numChannels; i++)
            {
                Channel c = channels[i];
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
            return nChan != null && 0 >= GetScore(nChan) ? nChan : null;
        }

        public void ChannelTick()
        {
            for (int i = 0; i < numChannels; i++)
            {
                Channel chan = channels[i];
                if (chan.Owner != null)
                {
                    chan.Volume = (byte)chan.StepEnvelope();
                    if (chan.NoteLength == 0 && !Utils.IsStateRemovable(chan.State))
                    {
                        chan.SetEnvelopePhase7_2074ED8();
                    }
                    int vol = SDAT.Utils.SustainTable[chan.NoteVelocity] + SDAT.Utils.SustainTable[chan.Volume] + SDAT.Utils.SustainTable[chan.Owner.Volume] + SDAT.Utils.SustainTable[chan.Owner.Expression];
                    //int pitch = ((chan.Key - chan.BaseKey) << 6) + chan.SweepMain() + chan.Owner.GetPitch(); // "<< 6" is "* 0x40"
                    int pitch = (chan.Key - chan.RootKey) << 6; // "<< 6" is "* 0x40"
                    if (Utils.IsStateRemovable(chan.State) && vol <= -92544)
                    {
                        chan.Stop();
                    }
                    else
                    {
                        chan.Volume = SDAT.Utils.GetChannelVolume(vol);
                        chan.Panpot = chan.Owner.Panpot;
                        chan.Timer = SDAT.Utils.GetChannelTimer(chan.BaseTimer, pitch);
                    }
                }
            }
        }

        public void BeginFadeIn()
        {
            fadePos = 0f;
            fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * 192);
            fadeStepPerMicroframe = 1f / fadeMicroFramesLeft;
        }
        public void BeginFadeOut()
        {
            fadePos = 1f;
            fadeMicroFramesLeft = (long)(GlobalConfig.Instance.PlaylistFadeOutMilliseconds / 1000.0 * 192);
            fadeStepPerMicroframe = -1f / fadeMicroFramesLeft;
        }
        public bool IsFadeDone()
        {
            return fadeMicroFramesLeft == 0;
        }
        public void ResetFade()
        {
            fadeMicroFramesLeft = 0;
        }

        private WaveFileWriter waveWriter;
        public void CreateWaveWriter(string fileName)
        {
            waveWriter = new WaveFileWriter(fileName, buffer.WaveFormat);
        }
        public void CloseWaveWriter()
        {
            waveWriter?.Dispose();
        }
        public void Process(bool output, bool recording)
        {
            float fromMaster = 1f, toMaster = 1f;
            if (fadeMicroFramesLeft > 0)
            {
                const float scale = 10f / 6f;
                fromMaster *= (fadePos < 0f) ? 0f : (float)Math.Pow(fadePos, scale);
                fadePos += fadeStepPerMicroframe;
                toMaster *= (fadePos < 0f) ? 0f : (float)Math.Pow(fadePos, scale);
                fadeMicroFramesLeft--;
            }
            float masterStep = (toMaster - fromMaster) * samplesReciprocal;
            float masterLevel = fromMaster;
            byte[] b = new byte[4];
            for (int i = 0; i < samplesPerBuffer; i++)
            {
                int left = 0,
                    right = 0;
                for (int j = 0; j < numChannels; j++)
                {
                    Channel chan = channels[j];
                    if (chan.Owner != null)
                    {
                        bool muted = Mutes[chan.Owner.Index]; // Get mute first because chan.Process() can call chan.Stop() which sets chan.Owner to null
                        chan.Process(out short channelLeft, out short channelRight);
                        if (!muted)
                        {
                            left += channelLeft;
                            right += channelRight;
                        }
                    }
                }
                float f = left * masterLevel;
                if (f < short.MinValue)
                {
                    f = short.MinValue;
                }
                else if (f > short.MaxValue)
                {
                    f = short.MaxValue;
                }
                left = (int)f;
                b[0] = (byte)left;
                b[1] = (byte)(left >> 8);
                f = right * masterLevel;
                if (f < short.MinValue)
                {
                    f = short.MinValue;
                }
                else if (f > short.MaxValue)
                {
                    f = short.MaxValue;
                }
                right = (int)f;
                b[2] = (byte)right;
                b[3] = (byte)(right >> 8);
                masterLevel += masterStep;
                if (output)
                {
                    buffer.AddSamples(b, 0, 4);
                }
                if (recording)
                {
                    waveWriter.Write(b, 0, 4);
                }
            }
        }
    }
}
