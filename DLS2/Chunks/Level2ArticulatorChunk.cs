using Kermalis.EndianBinaryIO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.DLS2
{
    // Level 2 Articulator Chunk - Page 49 of spec
    public sealed class Level2ArticulatorChunk : DLSChunk, IList<Level2ArticulatorConnectionBlock>, IReadOnlyList<Level2ArticulatorConnectionBlock>
    {
        private readonly List<Level2ArticulatorConnectionBlock> _connectionBlocks;

        public Level2ArticulatorConnectionBlock this[int index]
        {
            get => _connectionBlocks[index];
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _connectionBlocks[index] = value;
            }
        }
        public int Count => _connectionBlocks.Count;
        public bool IsReadOnly => false;

        public Level2ArticulatorChunk() : base("art2")
        {
            _connectionBlocks = new List<Level2ArticulatorConnectionBlock>();
        }
        internal Level2ArticulatorChunk(EndianBinaryReader reader) : base("art2", reader)
        {
            long endOffset = GetEndOffset(reader);
            uint byteSize = reader.ReadUInt32();
            if (byteSize != 8)
            {
                throw new InvalidDataException();
            }
            uint numConnectionBlocks = reader.ReadUInt32();
            _connectionBlocks = new List<Level2ArticulatorConnectionBlock>((int)numConnectionBlocks);
            for (uint i = 0; i < numConnectionBlocks; i++)
            {
                _connectionBlocks.Add(new Level2ArticulatorConnectionBlock(reader));
            }
            EatRemainingBytes(reader, endOffset);
        }

        internal override void UpdateSize()
        {
            Size = 4 // byteSize
                + 4 // _numConnectionBlocks
                + (uint)(12 * _connectionBlocks.Count); // _connectionBlocks
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(8u);
            writer.Write((uint)_connectionBlocks.Count);
            for (int i = 0; i < _connectionBlocks.Count; i++)
            {
                _connectionBlocks[i].Write(writer);
            }
        }

        public IEnumerator<Level2ArticulatorConnectionBlock> GetEnumerator()
        {
            return _connectionBlocks.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _connectionBlocks.GetEnumerator();
        }

        public void Add(Level2ArticulatorConnectionBlock item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            _connectionBlocks.Add(item);
        }
        public void Clear()
        {
            _connectionBlocks.Clear();
        }
        public void CopyTo(Level2ArticulatorConnectionBlock[] array, int arrayIndex)
        {
            _connectionBlocks.CopyTo(array, arrayIndex);
        }
        public bool Contains(Level2ArticulatorConnectionBlock item)
        {
            return _connectionBlocks.Contains(item);
        }
        public int IndexOf(Level2ArticulatorConnectionBlock item)
        {
            return _connectionBlocks.IndexOf(item);
        }
        public void Insert(int index, Level2ArticulatorConnectionBlock item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            _connectionBlocks.Insert(index, item);
        }
        public bool Remove(Level2ArticulatorConnectionBlock item)
        {
            return _connectionBlocks.Remove(item);
        }
        public void RemoveAt(int index)
        {
            _connectionBlocks.RemoveAt(index);
        }

    }
}
