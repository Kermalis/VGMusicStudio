using Kermalis.EndianBinaryIO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kermalis.DLS2
{
    public sealed class DLS : IList<DLSChunk>, IReadOnlyList<DLSChunk>
    {
        private readonly List<DLSChunk> _chunks;

        public int Count => _chunks.Count;
        public bool IsReadOnly => false;
        public DLSChunk this[int index]
        {
            get => _chunks[index];
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _chunks[index] = value;
            }
        }

        public CollectionHeaderChunk CollectionHeader => GetChunk<CollectionHeaderChunk>();
        public ListChunk InstrumentList => GetListChunk("lins");
        public PoolTableChunk PoolTable => GetChunk<PoolTableChunk>();
        public ListChunk WavePool => GetListChunk("wvpl");

        private T GetChunk<T>() where T : DLSChunk
        {
            return (T)_chunks.Find(c => c is T);
        }
        private ListChunk GetListChunk(string str)
        {
            return (ListChunk)_chunks.Find(c => c is ListChunk lc && lc.Identifier == str);
        }

#if DEBUG
        public static void Main()
        {
            //new DLS(@"C:\Users\Kermalis\Documents\Emulation\GBA\Games\M\test.dls");
            //new DLS(@"C:\Users\Kermalis\Documents\Emulation\GBA\Games\M\test2.dls");
            //new DLS(@"C:\Users\Kermalis\Music\Samples, Presets, Soundfonts, VSTs, etc\Soundfonts\Arachno SoundFont - Version 1.0.dls");
            //new DLS(@"C:\Users\Kermalis\Music\Samples, Presets, Soundfonts, VSTs, etc\Soundfonts\Musyng Kite.dls");
            new DLS(@"C:\Users\Kermalis\Music\Samples, Presets, Soundfonts, VSTs, etc\Soundfonts\RSE Corrected Soundfont Revision 17.dls");
        }
#endif

        /// <summary>For creating.</summary>
        public DLS()
        {
            _chunks = new List<DLSChunk>()
            {
                new CollectionHeaderChunk(),
                new ListChunk("lins"),
                new PoolTableChunk(),
                new ListChunk("wvpl"),
            };
        }
        public DLS(string path)
        {
            using (var reader = new EndianBinaryReader(File.Open(path, FileMode.Open)))
            {
                _chunks = Init(reader);
            }
        }
        public DLS(Stream stream)
        {
            _chunks = Init(new EndianBinaryReader(stream));
        }
        private List<DLSChunk> Init(EndianBinaryReader reader)
        {
            string str = reader.ReadString(4, false);
            if (str != "RIFF")
            {
                throw new InvalidDataException("RIFF header was not found at the start of the file.");
            }
            uint size = reader.ReadUInt32();
            long endOffset = reader.BaseStream.Position + size;
            str = reader.ReadString(4, false);
            if (str != "DLS ")
            {
                throw new InvalidDataException("DLS header was not found at the expected offset.");
            }
            return DLSChunk.GetAllChunks(reader, endOffset);
        }

        public void UpdateCollectionHeader()
        {
            CollectionHeader.NumInstruments = (uint)InstrumentList.Count;
        }
        /// <summary>Updates the pointers in the <see cref="PoolTable"/>. Should be called after modifying <see cref="WavePool"/>.</summary>
        public void UpdatePoolTable()
        {
            ListChunk wvpl = WavePool;
            var newCues = new List<uint>(wvpl.Count);
            uint cur = 0;
            for (int i = 0; i < wvpl.Count; i++)
            {
                newCues.Add(cur);
                DLSChunk c = wvpl[i];
                c.UpdateSize();
                cur += c.Size + 8;
            }
            PoolTable.UpdateCues(newCues);
        }
        public void Save(string path)
        {
            UpdateCollectionHeader();
            UpdatePoolTable();

            using (var writer = new EndianBinaryWriter(File.Open(path, FileMode.Create)))
            {
                writer.Write("RIFF", 4);
                writer.Write(UpdateSize());
                writer.Write("DLS ", 4);
                foreach (DLSChunk c in _chunks)
                {
                    c.Write(writer);
                }
            }
        }

        public string GetHierarchy()
        {
            var str = new StringBuilder();
            int tabLevel = 0;
            void ApplyTabLevel()
            {
                for (int t = 0; t < tabLevel; t++)
                {
                    str.Append('\t');
                }
            }
            void Recursion(IReadOnlyList<DLSChunk> parent, string listName)
            {
                ApplyTabLevel();
                str.Append($"{listName} ({parent.Count})");
                tabLevel++;
                foreach (DLSChunk c in parent)
                {
                    str.AppendLine();
                    if (c is ListChunk lc)
                    {
                        Recursion(lc, $"{lc.ChunkName} '{lc.Identifier}'");
                    }
                    else
                    {
                        ApplyTabLevel();
                        str.Append($"<{c.ChunkName}>");
                        if (c is InfoSubChunk ic)
                        {
                            str.Append($" [\"{ic.Text}\"]");
                        }
                        else if (c is RawDataChunk dc)
                        {
                            str.Append($" [{dc.Data.Length} bytes]");
                        }
                    }
                }
#pragma warning disable IDE0059 // Unnecessary assignment of a value
                tabLevel--;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
            }
            Recursion(this, "RIFF 'DLS '");
            return str.ToString();
        }

        private uint UpdateSize()
        {
            uint size = 4;
            foreach (DLSChunk c in _chunks)
            {
                c.UpdateSize();
                size += c.Size + 8;
            }
            return size;
        }

        public void Add(DLSChunk chunk)
        {
            if (chunk is null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }
            _chunks.Add(chunk);
        }
        public void Clear()
        {
            _chunks.Clear();
        }
        public bool Contains(DLSChunk chunk)
        {
            return _chunks.Contains(chunk);
        }
        public void CopyTo(DLSChunk[] array, int arrayIndex)
        {
            _chunks.CopyTo(array, arrayIndex);
        }
        public int IndexOf(DLSChunk chunk)
        {
            return _chunks.IndexOf(chunk);
        }
        public void Insert(int index, DLSChunk chunk)
        {
            if (chunk is null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }
            _chunks.Insert(index, chunk);
        }
        public bool Remove(DLSChunk chunk)
        {
            return _chunks.Remove(chunk);
        }
        public void RemoveAt(int index)
        {
            _chunks.RemoveAt(index);
        }

        public IEnumerator<DLSChunk> GetEnumerator()
        {
            return _chunks.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _chunks.GetEnumerator();
        }
    }
}
