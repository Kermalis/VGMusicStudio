using System;
using System.Globalization;

namespace GBAMusicStudio.Util
{
    public static class Utils
    {
        public static int ParseInt(string value)
        {
            var provider = new CultureInfo("en-US");
            if (value.StartsWith("0x")) value = value.Substring(2);
            if (int.TryParse(value, NumberStyles.HexNumber, provider, out int hex))
                return hex;
            if (int.TryParse(value, NumberStyles.Integer, provider, out int dec))
                return dec;
            throw new ArgumentException("\"value\" was invalid.");
        }
    }
}
