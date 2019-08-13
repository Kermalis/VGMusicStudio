using NAudio.Wave;
using System;

namespace Kermalis.VGMusicStudio.Core.PSX.PSF
{
    internal class Mixer : Core.Mixer
    {
        private readonly float samplesReciprocal;
        private readonly int samplesPerBuffer;
        private long fadeMicroFramesLeft;
        private float fadePos;
        private float fadeStepPerMicroframe;

        public Channel[] Channels;
        private readonly BufferedWaveProvider buffer;

        public Mixer()
        {
            const int sampleRate = 65456; // TODO
            samplesPerBuffer = 341; // TODO
            samplesReciprocal = 1f / samplesPerBuffer;

            Channels = new Channel[0x10];
            for (byte i = 0; i < 0x10; i++)
            {
                Channels[i] = new Channel(i);
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
                return c.Owner == null ? -2 : false ? -1 : 0;
                //return c.Owner == null ? -2 : c.State == EnvelopeState.Release ? -1 : 0;
            }
            Channel nChan = null;
            for (int i = 0; i < 0x10; i++)
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
            return nChan != null && 0 >= GetScore(nChan) ? nChan : null;
        }

        public void ChannelTick()
        {
            for (int i = 0; i < 0x10; i++)
            {
                Channel chan = Channels[i];
                if (chan.Owner != null)
                {
                    //chan.StepEnvelope();
                    int vol = NDS.SDAT.Utils.SustainTable[chan.NoteVelocity] + 0;
                    int pitch = ((chan.Key - chan.BaseKey) << 6) + chan.PitchTune; // "<< 6" is "* 0x40"
                    if (/*chan.State == EnvelopeState.Release && */vol <= -92544)
                    {
                        chan.Stop();
                    }
                    else
                    {
                        chan.Volume = NDS.SDAT.Utils.GetChannelVolume(vol);
                        chan.Timer = NDS.SDAT.Utils.GetChannelTimer(chan.BaseTimer, pitch);
                        chan.Pan = 0;
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
                for (int j = 0; j < 0x10; j++)
                {
                    Channel chan = Channels[j];
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
