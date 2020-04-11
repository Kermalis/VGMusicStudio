using Kermalis.EndianBinaryIO;
using System;
using System.IO;

namespace Kermalis.VGMusicStudio.Core.GBA.Rare
{
    internal class Config : Core.Config
    {
        public readonly byte[] ROM;
        public readonly EndianBinaryReader Reader;
        public readonly string GameCode;
        public readonly byte Version;

        public Config(byte[] rom)
        {
            ROM = rom;
            Reader = new EndianBinaryReader(new MemoryStream(rom));
            Playlists.Add(new Playlist("Music", Array.Empty<Song>()));
        }

        public override string GetSongName(long index)
        {
            return index.ToString();
        }

        public override void Dispose()
        {
            Reader.Dispose();
        }
    }
}
