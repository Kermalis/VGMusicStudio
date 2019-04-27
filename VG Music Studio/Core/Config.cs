using Kermalis.VGMusicStudio.Util;

namespace Kermalis.VGMusicStudio.Core
{
    internal abstract class Config
    {
        public virtual HSLColor GetColor(int voice) // Currently unused
        {
            return GlobalConfig.Instance.Colors[voice];
        }
    }
}
