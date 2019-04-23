using Kermalis.EndianBinaryIO;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    class DSE
    {
        // TODO: Move to SWDL since this is only used for that
        public static long FindChunk(EndianBinaryReader reader, string chunk)
        {
            long pos = -1;
            long oldPosition = reader.BaseStream.Position;
            reader.BaseStream.Position = 0;
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                string str = reader.ReadString(4);
                if (str == chunk)
                {
                    pos = reader.BaseStream.Position - 4;
                    break;
                }
                switch (str)
                {
                    case "smdl":
                        {
                            reader.BaseStream.Position += 0x3C;
                            break;
                        }
                    case "swdl":
                        {
                            reader.BaseStream.Position += 0x4C;
                            break;
                        }
                    default:
                        {
                            reader.BaseStream.Position += 0x8;
                            uint length = reader.ReadUInt32();
                            reader.BaseStream.Position += length;
                            // Align 4
                            while (reader.BaseStream.Position % 4 != 0)
                            {
                                reader.BaseStream.Position++;
                            }
                            break;
                        }
                }
            }
            reader.BaseStream.Position = oldPosition;
            return pos;
        }
    }
}
