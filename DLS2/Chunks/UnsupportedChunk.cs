using Kermalis.EndianBinaryIO;

namespace Kermalis.DLS2
{
    public sealed class UnsupportedChunk : RawDataChunk
    {
        public UnsupportedChunk(string name, byte[] data) : base(name, data) { }
        internal UnsupportedChunk(string name, EndianBinaryReader reader) : base(name, reader) { }
    }
}
