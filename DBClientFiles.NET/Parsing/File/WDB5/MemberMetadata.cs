﻿using DBClientFiles.NET.Parsing.Binding;
using DBClientFiles.NET.Parsing.Enums;

namespace DBClientFiles.NET.Parsing.File.WDB5
{
    internal sealed class MemberMetadata : BaseMemberMetadata
    {
        public override int Cardinality                       { get; internal set; } = -1;
        public override MemberMetadataProperties Properties   { get; internal set; } = 0;

        private CompressionData _compressionData;
        public override ref CompressionData CompressionData => ref _compressionData;

        public override T GetDefaultValue<T>()
        {
            return default;
        }
    }
}
