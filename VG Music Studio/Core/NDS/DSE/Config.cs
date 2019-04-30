using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.NDS.DSE
{
    internal class Config : Core.Config
    {
        public string BGMPath;
        public string[] BGMFiles;

        public Config(string bgmPath)
        {
            BGMPath = bgmPath;
            BGMFiles = Directory.GetFiles(bgmPath, "bgm*.smd", SearchOption.TopDirectoryOnly);
            if (BGMFiles.Length == 0)
            {
                throw new Exception("There are no \"bgm(NNNN).smd\" files.");
            }
            var songs = new Song[BGMFiles.Length];
            for (int i = 0; i < BGMFiles.Length; i++)
            {
                using (var reader = new EndianBinaryReader(File.OpenRead(BGMFiles[i])))
                {
                    SMD.Header header = reader.ReadObject<SMD.Header>();
                    songs[i] = new Song(i, header.Label);
                }
            }
            Playlists.Add(new Playlist("Music", songs));
        }
    }
}
