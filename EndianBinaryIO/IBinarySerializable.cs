namespace Kermalis.EndianBinaryIO
{
    public interface IBinarySerializable
    {
        void Read(EndianBinaryReader r);
        void Write(EndianBinaryWriter w);
    }
}
