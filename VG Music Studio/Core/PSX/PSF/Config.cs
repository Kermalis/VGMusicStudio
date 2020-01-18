using Kermalis.VGMusicStudio.Properties;
using System;
using System.IO;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core.PSX.PSF
{
    internal class Config : Core.Config
    {
        public string BGMPath;
        public string[] BGMFiles;

        public Config(string bgmPath)
        {
            BGMPath = bgmPath;
            BGMFiles = Directory.EnumerateFiles(bgmPath).Where(f => f.EndsWith(".minipsf", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".psf", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (BGMFiles.Length == 0)
            {
                throw new Exception(Strings.ErrorDSENoSequences);
            }
            var songs = new Song[BGMFiles.Length];
            for (int i = 0; i < BGMFiles.Length; i++)
            {
                // TODO: Read title from tag
                songs[i] = new Song(i, Path.GetFileNameWithoutExtension(BGMFiles[i]));
            }
            Playlists.Add(new Playlist(Strings.PlaylistMusic, songs));
        }

        public override string GetSongName(long index)
        {
            return index < 0 || index >= BGMFiles.Length
                ? index.ToString()
                : '\"' + BGMFiles[index] + '\"';
        }
    }
}
