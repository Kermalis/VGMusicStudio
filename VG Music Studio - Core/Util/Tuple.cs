using System;

namespace Kermalis.VGMusicStudio.Core.Util;

public class Pair<T1, T2>(T1 item1, T2 item2)
{
    public T1 Item1 = item1;
    public T2 Item2 = item2;

    public Tuple<T1, T2> ToTuple() => new(Item1, Item2);
}
public class Triple<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
{
    public T1 Item1 = item1;
    public T2 Item2 = item2;
    public T3 Item3 = item3;

    public Tuple<T1, T2, T3> ToTuple() => new(Item1, Item2, Item3);
}
