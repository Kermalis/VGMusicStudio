namespace Kermalis.VGMusicStudio.Core;

public struct CommandArg
{
    public enum ValueType
    {
        Boolean,
        Number,
        Pointer
    }

    public string Name;
    public ValueType Type;
    public int Value;
    public int MinValue;
    public int MaxValue;
    public long Offset;
}

public struct CommandPointer
{
    public long Offset;
    public int Index;
}
