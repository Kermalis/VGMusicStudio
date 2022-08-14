using Kermalis.EndianBinaryIO;
using System;
using System.Linq;

namespace Kermalis.DLS2
{
    public sealed class InfoSubChunk : DLSChunk
    {
        private string _text;
        public string Text
        {
            get => _text;
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value.Any(c => c > sbyte.MaxValue))
                {
                    throw new ArgumentException("Text must be ASCII");
                }
                _text = value;
            }
        }

        public InfoSubChunk(string name, string text) : base(name)
        {
            Text = text;
        }
        internal InfoSubChunk(string name, EndianBinaryReader reader) : base(name, reader)
        {
            long endOffset = GetEndOffset(reader);
            _text = reader.ReadStringNullTerminated();
            EatRemainingBytes(reader, endOffset);
        }

        internal override void UpdateSize()
        {
            Size = (uint)_text.Length + 1; // +1 for \0
            if (Size % 2 != 0) // Align by 2 bytes
            {
                Size++;
            }
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(_text, (int)Size);
        }
    }
}
