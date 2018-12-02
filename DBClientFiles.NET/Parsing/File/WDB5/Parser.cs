﻿using System.IO;
using DBClientFiles.NET.Parsing.Binding;
using DBClientFiles.NET.Parsing.File.Records;
using DBClientFiles.NET.Parsing.File.Segments;
using DBClientFiles.NET.Parsing.File.Segments.Handlers;
using DBClientFiles.NET.Parsing.File.Segments.Handlers.Implementations;

namespace DBClientFiles.NET.Parsing.File.WDB5
{
    internal sealed class Parser<T> : BinaryFileParser<T, Serializer<T>>
    {
        private IFileHeader _fileHeader;
        public override ref readonly IFileHeader Header => ref _fileHeader;

        public override int RecordCount => _fileHeader.RecordCount + _fileHeader.CopyTableLength / 2;

        private PackedRecordReader _recordReader;

        public Parser(in StorageOptions options, Stream input) : base(options, input)
        {
            _fileHeader = new Header(this);

            RegisterBlockHandler(new FieldInfoHandler<MemberMetadata>());
        }

        protected override IRecordReader GetRecordReader(int recordSize)
        {
            _recordReader.LoadStream(BaseStream, recordSize);
            return _recordReader;
        }

        protected override void Prepare()
        {
            var tail = Head.Next = new Block {
                Identifier = BlockIdentifier.FieldInfo,
                Length = Header.FieldCount * (2 + 2)
            };

            tail = tail.Next = new Block {
                Identifier = BlockIdentifier.Records,
                Length = Header.HasOffsetMap
                    ? Header.StringTableLength - tail.EndOffset
                    : Header.RecordCount * Header.RecordSize
            };

            if (!Header.HasOffsetMap)
            {
                tail = tail.Next = new Block
                {
                    Identifier = BlockIdentifier.StringBlock,
                    Length = Header.StringTableLength
                };

                RegisterBlockHandler(new StringBlockHandler(Options.InternStrings));
            }
            else
            {
                tail = tail.Next = new Block
                {
                    Identifier = BlockIdentifier.OffsetMap,
                    Length = (4 + 2) * (Header.MaxIndex - Header.MinIndex + 1)
                };

                RegisterBlockHandler(new OffsetMapHandler());
            }

            if (Header.HasForeignIds)
            {
                tail = tail.Next = new Block
                {
                    Identifier = BlockIdentifier.RelationShipTable,
                    Length = 4 * (Header.MaxIndex - Header.MinIndex + 1)
                };
            }

            if (Header.HasIndexTable)
            {
                tail = tail.Next = new Block
                {
                    Identifier = BlockIdentifier.IndexTable,
                    Length = 4 * Header.RecordCount
                };

                RegisterBlockHandler(new IndexTableHandler());
            }

            if (Header.CopyTableLength > 0)
            {
                tail = tail.Next = new Block
                {
                    Identifier = BlockIdentifier.CopyTable,
                    Length = Header.CopyTableLength
                };

                RegisterBlockHandler(new CopyTableHandler());
            }

            _recordReader = new PackedRecordReader(this, Header.RecordSize);
        }

    }
}