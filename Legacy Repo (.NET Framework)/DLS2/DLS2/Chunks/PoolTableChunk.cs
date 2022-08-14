using Kermalis.EndianBinaryIO;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.DLS2
{
    // Pool Table Chunk - Page 54 of spec
    public sealed class PoolTableChunk : DLSChunk, IReadOnlyList<uint>
    {
        private uint _numCues;
        private List<uint> _poolCues;

        public uint this[int index] => _poolCues[index];
        public int Count => (int)_numCues;

        internal PoolTableChunk() : base("ptbl")
        {
            _poolCues = new List<uint>();
        }
        internal PoolTableChunk(EndianBinaryReader reader) : base("ptbl", reader)
        {
            long endOffset = GetEndOffset(reader);
            uint byteSize = reader.ReadUInt32();
            if (byteSize != 8)
            {
                throw new InvalidDataException();
            }
            _numCues = reader.ReadUInt32();
            _poolCues = new List<uint>((int)_numCues);
            for (uint i = 0; i < _numCues; i++)
            {
                _poolCues.Add(reader.ReadUInt32());
            }
            EatRemainingBytes(reader, endOffset);
        }

        internal void UpdateCues(List<uint> newCues)
        {
            _numCues = (uint)newCues.Count;
            _poolCues = newCues;
        }

        internal override void UpdateSize()
        {
            Size = 4 // byteSize
                + 4 // _numCues
                + (4 * _numCues); // _poolCues
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(8u);
            writer.Write(_numCues);
            for (int i = 0; i < _numCues; i++)
            {
                writer.Write(_poolCues[i]);
            }
        }

        public IEnumerator<uint> GetEnumerator()
        {
            return _poolCues.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _poolCues.GetEnumerator();
        }
    }
}
