using Kermalis.EndianBinaryIO;
using System;

namespace Kermalis.DLS2
{
    // DLSID Chunk - Page 40 of spec
    public sealed class DLSIDChunk : DLSChunk
    {
        private DLSID _dlsid;
        public DLSID DLSID
        {
            get => _dlsid;
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _dlsid = value;
            }
        }

        public DLSIDChunk(DLSID id) : base("dlid")
        {
            DLSID = id;
        }
        public DLSIDChunk(EndianBinaryReader reader) : base("dlid", reader)
        {
            long endOffset = GetEndOffset(reader);
            DLSID = new DLSID(reader);
            EatRemainingBytes(reader, endOffset);
        }

        internal override void UpdateSize()
        {
            Size = 16; // DLSID
        }

        internal override void Write(EndianBinaryWriter writer)
        {
            base.Write(writer);
            DLSID.Write(writer);
        }
    }
}
