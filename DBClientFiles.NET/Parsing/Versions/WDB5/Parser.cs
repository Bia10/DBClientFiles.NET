﻿using DBClientFiles.NET.Parsing.Enumerators;
using DBClientFiles.NET.Parsing.Shared.Records;
using DBClientFiles.NET.Parsing.Shared.Segments;
using DBClientFiles.NET.Parsing.Shared.Segments.Handlers.Implementations;
using DBClientFiles.NET.Parsing.Versions.WDB5.Binding;
using DBClientFiles.NET.Parsing.Versions.WDB5.Segments.Handlers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace DBClientFiles.NET.Parsing.Versions.WDB5
{
    internal sealed class Parser<T> : BinaryFileParser<T, Serializer<T>>
    {
        public override int RecordCount => Header.RecordCount + Header.CopyTable.Length / (2 * 4);

        private ByteAlignedRecordReader _recordReader;

        public Parser(in StorageOptions options, Stream input) : base(options, input)
        {
        }

        public override IRecordReader GetRecordReader(int recordSize)
        {
            _recordReader.LoadStream(BaseStream, recordSize);
            return _recordReader;
        }

        public override void Before(ParsingStep step)
        {
            if (step != ParsingStep.Segments)
                return;
            
            Head = new Segment {
                Identifier = SegmentIdentifier.Header,
                Length = Unsafe.SizeOf<Header>(),

                Handler = new HeaderHandler(this)
            };

            var tail = Head.Next = new Segment {
                Identifier = SegmentIdentifier.FieldInfo,
                Length = Header.FieldCount * (2 + 2),

                Handler = new FieldInfoHandler<MemberMetadata>()
            };

            tail = tail.Next = new Segment {
                Identifier = SegmentIdentifier.Records,
                Length = Header.OffsetMap.Exists
                    ? Header.StringTable.Length - tail.EndOffset
                    : Header.RecordCount * Header.RecordSize
            };

            if (!Header.OffsetMap.Exists)
            {
                tail = tail.Next = new Segment {
                    Identifier = SegmentIdentifier.StringBlock,
                    Length = Header.StringTable.Length,

                    Handler = new StringBlockHandler(Options.InternStrings)
                };
            }
            else
            {
                tail = tail.Next = new Segment {
                    Identifier = SegmentIdentifier.OffsetMap,
                    Length = (4 + 2) * (Header.MaxIndex - Header.MinIndex + 1),

                    Handler = new OffsetMapHandler()
                };
            }

            if (Header.RelationshipTable.Exists)
            {
                tail = tail.Next = new Segment {
                    // Legacy foreign table, apparently used by only WMOMinimapTexture (WDB3/4/5) @Barncastle
                    Identifier = SegmentIdentifier.RelationshipTable,
                    Length = 4 * (Header.MaxIndex - Header.MinIndex + 1)
                };
            }

            if (Header.IndexTable.Exists)
            {
                tail = tail.Next = new Segment {
                    Identifier = SegmentIdentifier.IndexTable,
                    Length = 4 * Header.RecordCount,

                    Handler = new IndexTableHandler()
                };
            }

            if (Header.CopyTable.Exists)
            {
                tail.Next = new Segment {
                    Identifier = SegmentIdentifier.CopyTable,
                    Length = Header.CopyTable.Length,

                    Handler = new CopyTableHandler()
                };
            }
        }

        public override void After(ParsingStep step)
        {
            if (step != ParsingStep.Segments)
                return;
            
            // Pocket sized optimization to allocate a buffer large enough to contain every record without the need for reallocations
            _recordReader = new ByteAlignedRecordReader(this,
                FindSegmentHandler<OffsetMapHandler>(SegmentIdentifier.OffsetMap)?.GetLargestRecordSize() ?? Header.RecordSize);
        }

        protected override IEnumerator<T> CreateEnumerator()
        {
            var enumerator = !Header.OffsetMap.Exists
                ? (Enumerator<Parser<T>, T, Serializer<T>>) new RecordsEnumerator<Parser<T>, T, Serializer<T>>(this)
                : (Enumerator<Parser<T>, T, Serializer<T>>) new OffsetMapEnumerator<Parser<T>, T, Serializer<T>>(this);

            return enumerator.WithIndexTable().WithCopyTable();
        }
    }
}
