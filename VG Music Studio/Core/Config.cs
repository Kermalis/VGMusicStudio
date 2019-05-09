using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Kermalis.VGMusicStudio.Core
{
    internal abstract class Config : IDisposable
    {
        public class Song
        {
            public long Index;
            public string Name;

            public Song(long index, string name)
            {
                Index = index; Name = name;
            }

            public override bool Equals(object obj)
            {
                return !(obj is Song other) ? false : other.Index == Index;
            }
            public override int GetHashCode()
            {
                return Index.GetHashCode();
            }
            public override string ToString()
            {
                return Name;
            }
        }
        public class Playlist
        {
            public string Name;
            public List<Song> Songs;

            public Playlist(string name, IEnumerable<Song> songs)
            {
                Name = name; Songs = songs.ToList();
            }

            public override string ToString()
            {
                int songCount = Songs.Count;
                CultureInfo cul = System.Threading.Thread.CurrentThread.CurrentUICulture;

                if (cul.Equals(CultureInfo.CreateSpecificCulture("it")) // Italian
                    || cul.Equals(CultureInfo.CreateSpecificCulture("it-it"))) // Italian (Italy)
                {
                    // PlaylistName - (1 Canzoni)
                    // PlaylistName - (2 Canzoni)
                    return $"{Name} - ({songCount} {(songCount == 1 ? "Canzone" : "Canzoni")})";
                }
                else // Fallback to en-US
                {
                    // PlaylistName - (1 Song)
                    // PlaylistName - (2 Songs)
                    return $"{Name} - ({songCount} {(songCount == 1 ? "Song" : "Songs")})";
                }
            }
        }

        public List<Playlist> Playlists = new List<Playlist>();

        public virtual void Dispose() { }
    }
}
