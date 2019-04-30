using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT
{
    internal class Track
    {
        public readonly byte Index;
        private readonly Player player;

        public bool Enabled, Stopped;
        public bool Tie, ShouldWaitForNotesToFinish, WaitingForNoteToFinishBeforeContinuingXD, Portamento;
        public bool VariableFlag; // Set by variable commands (0xB0 - 0xBD)
        public byte Voice, Priority, Volume, Expression,
            LFORange, BendRange, LFOSpeed, LFODepth;
        public ushort LFODelay, LFOPhase, LFODelayCount;
        public LFOType LFOType;
        public sbyte Bend, Pan, KeyShift;
        public byte Attack, Decay, Sustain, Release;
        public byte PortamentoKey, PortamentoTime;
        public short SweepPitch;
        public int Delay;
        public int DataOffset;
        public int[] CallStack = new int[3];
        public byte[] CallStackLoops = new byte[3];
        public byte CallStackDepth;

        public readonly List<Channel> Channels = new List<Channel>(0x10);

        public int GetPitch()
        {
            int mod = LFOType == LFOType.Pitch ? (LFORange * Utils.Sin(LFOPhase >> 8) * LFODepth) : 0;
            mod = ((mod << 6) >> 14) | ((mod >> 26) << 18);
            return (Bend * BendRange / 2) + mod;
        }
        public int GetVolume()
        {
            int mod = LFOType == LFOType.Volume ? (LFORange * Utils.Sin(LFOPhase >> 8) * LFODepth) : 0;
            mod = (((mod << 6) >> 14) | ((mod >> 26) << 18)) << 6;
            return Utils.SustainTable[player.Volume] + Utils.SustainTable[Volume] + Utils.SustainTable[Expression] + mod;
        }
        public sbyte GetPan()
        {
            int mod = LFOType == LFOType.Panpot ? (LFORange * Utils.Sin(LFOPhase >> 8) * LFODepth) : 0;
            mod = ((mod << 6) >> 14) | ((mod >> 26) << 18);
            return (sbyte)Util.Utils.Clamp(Pan + mod, -0x40, 0x3F);
        }

        public Track(byte i, Player player)
        {
            Index = i;
            this.player = player;
        }
        public void Init()
        {
            Enabled = Stopped = Tie = WaitingForNoteToFinishBeforeContinuingXD = Portamento = false;
            DataOffset = 0;
            ShouldWaitForNotesToFinish = VariableFlag = true;
            CallStackDepth = 0;
            Voice = LFODepth = 0;
            Bend = Pan = KeyShift = 0;
            LFOPhase = LFODelay = LFODelayCount = 0;
            LFORange = 1;
            LFOSpeed = 0x10;
            Priority = 0x40;
            Volume = Expression = 0x7F;
            Attack = Decay = Sustain = Release = 0xFF;
            BendRange = 2;
            PortamentoKey = 60;
            PortamentoTime = 0;
            SweepPitch = 0;
            LFOType = LFOType.Pitch;
            Delay = 0;
            StopAllChannels();
        }
        public void Tick()
        {
            if (Delay > 0)
            {
                Delay--;
            }
            if (Channels.Count != 0)
            {
                // TickNotes:
                for (int i = 0; i < Channels.Count; i++)
                {
                    Channel c = Channels[i];
                    if (c.NoteLength > 0)
                    {
                        c.NoteLength--;
                    }
                    if (!c.AutoSweep && c.SweepCounter < c.SweepLength)
                    {
                        c.SweepCounter++;
                    }
                }
                // LFO:
                if (LFODelayCount > 0)
                {
                    LFODelayCount--;
                    LFOPhase = 0;
                }
                else
                {
                    int increment = LFOSpeed << 6; // "<< 6" is "* 0x40"
                    int i;
                    for (i = (LFOPhase + increment) >> 8; i >= 0x80; i -= 0x80) // ">> 8" is "/ 0x100"
                    {
                    }
                    LFOPhase += (ushort)increment;
                    LFOPhase &= 0xFF;
                    LFOPhase |= (ushort)(i << 8); // "<< 8" is "* 0x100"
                }
            }
            else
            {
                WaitingForNoteToFinishBeforeContinuingXD = false;
                LFOPhase = 0;
                LFODelayCount = LFODelay;
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
