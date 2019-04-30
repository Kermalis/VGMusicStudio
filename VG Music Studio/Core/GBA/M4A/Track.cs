using Kermalis.EndianBinaryIO;
using Kermalis.VGMusicStudio.Util;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.GBA.M4A
{
    internal class Track
    {
        public readonly byte Index;
        public readonly EndianBinaryReader Reader;
        public readonly long StartOffset;

        public byte Voice, BendRange, Priority, Volume,
            Delay, RunCmd, PrevKey, PrevVelocity,
            LFOPhase, LFODelayCount, LFOSpeed, LFODelay, LFODepth;
        public LFOType LFOType;
        public sbyte Bend, Tune, Panpot, Transpose;
        public long EndOfPattern;
        public bool Ready, Stopped;

        public readonly List<Channel> Channels = new List<Channel>();

        public int GetPitch()
        {
            int mod = LFOType == LFOType.Pitch ? (Utils.Tri(LFOPhase) * LFODepth) >> 8 : 0;
            return (Bend * BendRange) + Tune + mod;
        }
        public byte GetVolume()
        {
            int mod = LFOType == LFOType.Volume ? (Utils.Tri(LFOPhase) * LFODepth * 3 * Volume) >> 19 : 0;
            return (byte)(Volume + mod).Clamp(0, 0x7F);
        }
        public sbyte GetPanpot()
        {
            int mod = LFOType == LFOType.Panpot ? (Utils.Tri(LFOPhase) * LFODepth * 3) >> 12 : 0;
            return (sbyte)(Panpot + mod).Clamp(-0x40, 0x3F);
        }

        public Track(byte i, byte[] rom, long startOffset)
        {
            Index = i;
            Reader = new EndianBinaryReader(new MemoryStream(rom));
            StartOffset = startOffset;
        }
        public void Init()
        {
            Reader.BaseStream.Position = StartOffset;
            Voice = Priority = RunCmd = PrevKey = Delay = LFODelay = LFODelayCount = LFOPhase = LFODepth = 0;
            Bend = Tune = Panpot = Transpose = 0;
            EndOfPattern = -1;
            BendRange = 2;
            LFOType = LFOType.Pitch;
            Ready = Stopped = false;
            LFOSpeed = 22;
            Volume = 100;
            PrevVelocity = 0x7F;
            StopAllChannels();
        }
        public void Tick()
        {
            if (Delay != 0)
            {
                Delay--;
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
                    LFOPhase = (byte)(LFOPhase + LFOSpeed);
                }
            }
            else
            {
                LFOPhase = 0;
                LFODelayCount = LFODelay;
            }
        }

        public void ReleaseChannels(byte key)
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
