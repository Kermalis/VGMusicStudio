using Kermalis.EndianBinaryIO;

namespace Kermalis.DLS2
{
    public sealed class DataChunk : RawDataChunk
    {
        public DataChunk(byte[] data) : base("data", data) { }
        internal DataChunk(EndianBinaryReader reader) : base("data", reader) { }
    }
}
