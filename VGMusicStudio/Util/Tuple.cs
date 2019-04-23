using System;

namespace Kermalis.VGMusicStudio.Util
{
    class Pair<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
        public Pair(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
        public Tuple<T1, T2> ToTuple() => new Tuple<T1, T2>(Item1, Item2);
    }
    class Triple<T1, T2, T3>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public Triple(T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }
        public Tuple<T1, T2, T3> ToTuple() => new Tuple<T1, T2, T3>(Item1, Item2, Item3);
    }
}
