using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.GBA.MP2K
{
    internal class Track
    {
        public readonly byte Index;
        private readonly long _startOffset;
        public byte Voice;
        public byte PitchBendRange;
        public byte Priority;
        public byte Volume;
        public byte Rest;
        public byte LFOPhase;
        public byte LFODelayCount;
        public byte LFOSpeed;
        public byte LFODelay;
        public byte LFODepth;
        public LFOType LFOType;
        public sbyte PitchBend;
        public sbyte Tune;
        public sbyte Panpot;
        public sbyte Transpose;
        public bool Ready;
        public bool Stopped;
        public long CurOffset;
        public long[] CallStack = new long[3];
        public byte CallStackDepth;
        public byte RunCmd;
        public byte PrevKey;
        public byte PrevVelocity;

        public readonly List<Channel> Channels = new List<Channel>();

        public int GetPitch()
        {
            int lfo = LFOType == LFOType.Pitch ? (Utils.Tri(LFOPhase) * LFODepth) >> 8 : 0;
            return (PitchBend * PitchBendRange) + Tune + lfo;
        }
        public byte GetVolume()
        {
            int lfo = LFOType == LFOType.Volume ? (Utils.Tri(LFOPhase) * LFODepth * 3 * Volume) >> 19 : 0;
            int v = Volume + lfo;
            if (v < 0)
            {
                v = 0;
            }
            else if (v > 0x7F)
            {
                v = 0x7F;
            }
            return (byte)v;
        }
        public sbyte GetPanpot()
        {
            int lfo = LFOType == LFOType.Panpot ? (Utils.Tri(LFOPhase) * LFODepth * 3) >> 12 : 0;
            int p = Panpot + lfo;
            if (p < -0x40)
            {
                p = -0x40;
            }
            else if (p > 0x3F)
            {
                p = 0x3F;
            }
            return (sbyte)p;
        }

        public Track(byte i, long startOffset)
        {
            Index = i;
            _startOffset = startOffset;
        }
        public void Init()
        {
            Voice = 0;
            Priority = 0;
            Rest = 0;
            LFODelay = 0;
            LFODelayCount = 0;
            LFOPhase = 0;
            LFODepth = 0;
            CallStackDepth = 0;
            PitchBend = 0; 
            Tune = 0;
            Panpot = 0;
            Transpose = 0;
            CurOffset = _startOffset;
            RunCmd = 0;
            PrevKey = 0;
            PrevVelocity = 0x7F;
            PitchBendRange = 2;
            LFOType = LFOType.Pitch;
            Ready = false;
            Stopped = false;
            LFOSpeed = 22;
            Volume = 100;
            StopAllChannels();
        }
        public void Tick()
        {
            if (Rest != 0)
            {
                Rest--;
            }
            int active = 0;
            Channel[] chans = Channels.ToArray();
            for (int i = 0; i < chans.Length; i++)
            {
                if (chans[i].TickNote())
                {
                    active++;
                }
            }
            if (active != 0)
            {
                if (LFODelayCount > 0)
                {
                    LFODelayCount--;
                    LFOPhase = 0;
                }
                else
                {
                    LFOPhase += LFOSpeed;
                }
            }
            else
            {
                LFOPhase = 0;
                LFODelayCount = LFODelay;
            }
        }

        public void ReleaseChannels(int key)
        {
            Channel[] chans = Channels.ToArray();
            for (int i = 0; i < chans.Length; i++)
            {
                Channel c = chans[i];
                if (c.Note.OriginalKey == key && c.Note.Duration == -1)
                {
                    c.Release();
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
        public void UpdateChannels()
        {
            byte vol = GetVolume();
            sbyte pan = GetPanpot();
            int pitch = GetPitch();
            for (int i = 0; i < Channels.Count; i++)
            {
                Channel c = Channels[i];
                c.SetVolume(vol, pan);
                c.SetPitch(pitch);
            }
        }
    }
}
