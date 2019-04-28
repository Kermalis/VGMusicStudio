using Kermalis.VGMusicStudio.Util;
using System;

namespace Kermalis.VGMusicStudio.Core
{
    internal abstract class Config : IDisposable
    {
        public virtual void Dispose() { }

        public virtual HSLColor GetColor(int voice) // Currently unused
        {
            return GlobalConfig.Instance.Colors[voice];
        }
    }
}
