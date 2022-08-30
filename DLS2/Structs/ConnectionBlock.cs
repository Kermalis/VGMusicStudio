using Kermalis.EndianBinaryIO;
using System;

namespace Kermalis.DLS2
{
    public sealed class Level1ArticulatorConnectionBlock
    {
        public Level1ArticulatorSource Source { get; set; }
        public Level1ArticulatorSource Control { get; set; }
        public Level1ArticulatorDestination Destination { get; set; }
        public Level1ArticulatorTransform Transform { get; set; }
        public int Scale { get; set; }

        public Level1ArticulatorConnectionBlock() { }
        internal Level1ArticulatorConnectionBlock(EndianBinaryReader reader)
        {
            Source = reader.ReadEnum<Level1ArticulatorSource>();
            Control = reader.ReadEnum<Level1ArticulatorSource>();
            Destination = reader.ReadEnum<Level1ArticulatorDestination>();
            Transform = reader.ReadEnum<Level1ArticulatorTransform>();
            Scale = reader.ReadInt32();
        }

        internal void Write(EndianBinaryWriter writer)
        {
            writer.Write(Source);
            writer.Write(Control);
            writer.Write(Destination);
            writer.Write(Transform);
            writer.Write(Scale);
        }
    }

    public sealed class Level2ArticulatorConnectionBlock
    {
        public Level2ArticulatorSource Source { get; set; }
        public Level2ArticulatorSource Control { get; set; }
        public Level2ArticulatorDestination Destination { get; set; }
        public ushort Transform_Raw { get; set; }
        public int Scale { get; set; }

        public bool InvertSource
        {
            get => (Transform_Raw >> 15) != 0;
            set
            {
                if (value)
                {
                    Transform_Raw |= 1 << 15;
                }
                else
                {
                    Transform_Raw &= unchecked((ushort)~(1 << 15));
                }
            }
        }
        public bool BipolarSource
        {
            get => ((Transform_Raw >> 14) & 1) != 0;
            set
            {
                if (value)
                {
                    Transform_Raw |= 1 << 14;
                }
                else
                {
                    Transform_Raw &= unchecked((ushort)~(1 << 14));
                }
            }
        }
        public Level2ArticulatorTransform TransformSource
        {
            get => (Level2ArticulatorTransform)((Transform_Raw >> 10) & 0xF);
            set
            {
                if (value > (Level2ArticulatorTransform)0xF)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                Transform_Raw &= unchecked((ushort)~(0xF << 10));
                Transform_Raw |= (ushort)((ushort)value << 10);
            }
        }

        public bool InvertDestination
        {
            get => ((Transform_Raw >> 9) & 1) != 0;
            set
            {
                if (value)
                {
                    Transform_Raw |= 1 << 9;
                }
                else
                {
                    Transform_Raw &= unchecked((ushort)~(1 << 9));
                }
            }
        }
        public bool BipolarDestination
        {
            get => ((Transform_Raw >> 8) & 1) != 0;
            set
            {
                if (value)
                {
                    Transform_Raw |= 1 << 8;
                }
                else
                {
                    Transform_Raw &= unchecked((ushort)~(1 << 8));
                }
            }
        }
        public Level2ArticulatorTransform TransformDestination
        {
            get => (Level2ArticulatorTransform)((Transform_Raw >> 4) & 0xF);
            set
            {
                if (value > (Level2ArticulatorTransform)0xF)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                Transform_Raw &= unchecked((ushort)~(0xF << 4));
                Transform_Raw |= (ushort)((ushort)value << 4);
            }
        }

        public Level2ArticulatorTransform TransformOutput
        {
            get => (Level2ArticulatorTransform)(Transform_Raw & 0xF);
            set
            {
                if (value > (Level2ArticulatorTransform)0xF)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                Transform_Raw &= unchecked((ushort)~0xF);
                Transform_Raw |= (ushort)value;
            }
        }

        public Level2ArticulatorConnectionBlock() { }
        internal Level2ArticulatorConnectionBlock(EndianBinaryReader reader)
        {
            Source = reader.ReadEnum<Level2ArticulatorSource>();
            Control = reader.ReadEnum<Level2ArticulatorSource>();
            Destination = reader.ReadEnum<Level2ArticulatorDestination>();
            Transform_Raw = reader.ReadUInt16();
            Scale = reader.ReadInt32();
        }

        internal void Write(EndianBinaryWriter writer)
        {
            writer.Write(Source);
            writer.Write(Control);
            writer.Write(Destination);
            writer.Write(Transform_Raw);
            writer.Write(Scale);
        }
    }
}
