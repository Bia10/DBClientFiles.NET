﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBClientFiles.NET.Parsing.File.Segments.Handlers
{
    internal sealed class OffsetMapHandler : IBlockHandler
    {
        private Memory<(int, short)> _store;

        public BlockIdentifier Identifier { get; } = BlockIdentifier.CopyTable;

        public void ReadBlock<T>(T reader, long startOffset, long length) where T : BinaryReader, IParser
        {
            if (startOffset == 0 || length == 0)
                return;

            reader.BaseStream.Seek(startOffset, SeekOrigin.Begin);
            int i = 0;
            Count = (int)(length / (sizeof(int) + sizeof(short)));

            _store = new Memory<(int, short)>(new (int, short)[Count]);

            while (reader.BaseStream.Position <= (startOffset + length))
            {
                var key = reader.ReadInt32();
                var value = reader.ReadInt16();

                if (key == 0 || value == 0)
                {
                    --Count;
                    continue;
                }

                _store.Span[i++] = (key, value);
            }

            _store = _store.Slice(0, Count);
        }

        public void WriteBlock<T, U>(T writer) where T : BinaryWriter, IWriter<U>
        {
        }

        public int Count { get; private set; }

        public int GetRecordOffset(int index) => _store.Span[index].Item1;
        public int GetRecordSize(int index) => _store.Span[index].Item2;
    }
}