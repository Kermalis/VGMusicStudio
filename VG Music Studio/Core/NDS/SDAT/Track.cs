using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.NDS.SDAT
{
    internal class Track
    {
        public readonly byte Index;
        private readonly Player _player;

        public bool Allocated;
        public bool Enabled;
        public bool Stopped;
        public bool Tie;
        public bool Mono;
        public bool Portamento;
        public bool WaitingForNoteToFinishBeforeContinuingXD; // Is this necessary?
        public byte Voice;
        public byte Priority;
        public byte Volume;
        public byte Expression;
        public byte LFORange;
        public byte PitchBendRange;
        public byte LFOSpeed;
        public byte LFODepth;
        public ushort LFODelay;
        public ushort LFOPhase;
        public ushort LFODelayCount;
        public LFOType LFOType;
        public sbyte PitchBend;
        public sbyte Panpot;
        public sbyte Transpose;
        public byte Attack;
        public byte Decay;
        public byte Sustain;
        public byte Release;
        public byte PortamentoKey;
        public byte PortamentoTime;
        public short SweepPitch;
        public int Rest;
        public int[] CallStack = new int[3];
        public byte[] CallStackLoops = new byte[3];
        public byte CallStackDepth;
        public int CurEvent;
        public bool VariableFlag; // Set by variable commands (0xB0 - 0xBD)
        public bool DoCommandWork;
        public ArgType ArgOverrideType;

        public readonly List<Channel> Channels = new List<Channel>(0x10);

        public int GetPitch()
        {
            int lfo = LFOType == LFOType.Pitch ? (LFORange * Utils.Sin(LFOPhase >> 8) * LFODepth) : 0;
            lfo = (int)(((long)lfo * 60) >> 14);
            return (PitchBend * PitchBendRange / 2) + lfo;
        }
        public int GetVolume()
        {
            int lfo = LFOType == LFOType.Volume ? (LFORange * Utils.Sin(LFOPhase >> 8) * LFODepth) : 0;
            lfo = (int)(((lfo & ~0xFC000000) >> 8) | ((lfo < 0 ? -1 : 0) << 6) | (((uint)lfo >> 26) << 18));
            return Utils.SustainTable[_player.Volume] + Utils.SustainTable[Volume] + Utils.SustainTable[Expression] + lfo;
        }
        public sbyte GetPan()
        {
            int lfo = LFOType == LFOType.Panpot ? (LFORange * Utils.Sin(LFOPhase >> 8) * LFODepth) : 0;
            lfo = (int)(((lfo & ~0xFC000000) >> 8) | ((lfo < 0 ? -1 : 0) << 6) | (((uint)lfo >> 26) << 18));
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

        public Track(byte i, Player player)
        {
            Index = i;
            _player = player;
        }
        public void Init()
        {
            Stopped = Tie = WaitingForNoteToFinishBeforeContinuingXD = Portamento = false;
            Allocated = Enabled = Index == 0;
            CurEvent = 0;
            ArgOverrideType = ArgType.None;
            Mono = VariableFlag = DoCommandWork = true;
            CallStackDepth = 0;
            Voice = LFODepth = 0;
            PitchBend = Panpot = Transpose = 0;
            LFOPhase = LFODelay = LFODelayCount = 0;
            LFORange = 1;
            LFOSpeed = 0x10;
            Priority = 0x40;
            Volume = Expression = 0x7F;
            Attack = Decay = Sustain = Release = 0xFF;
            PitchBendRange = 2;
            PortamentoKey = 60;
            PortamentoTime = 0;
            SweepPitch = 0;
            LFOType = LFOType.Pitch;
            Rest = 0;
            StopAllChannels();
        }
        public void Tick()
        {
            if (Rest > 0)
            {
                Rest--;
            }
            if (Channels.Count != 0)
            {
                // TickNotes:
                for (int i = 0; i < Channels.Count; i++)
                {
                    Channel c = Channels[i];
                    if (c.NoteDuration > 0)
                    {
                        c.NoteDuration--;
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
                    int speed = LFOSpeed << 6; // "<< 6" is "* 0x40"
                    int counter = (LFOPhase + speed) >> 8; // ">> 8" is "/ 0x100"
                    while (counter >= 0x80)
                    {
                        counter -= 0x80;
                    }
                    LFOPhase += (ushort)speed;
                    LFOPhase &= 0xFF;
                    LFOPhase |= (ushort)(counter << 8); // "<< 8" is "* 0x100"
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
