﻿using System;
using System.Collections.Generic;
using DBClientFiles.NET.Internals.Binding;
using DBClientFiles.NET.Internals.Generators;

namespace DBClientFiles.NET.Internals
{
    internal interface IReader
    {
        bool PrepareMemberInformations();
        void ReadSegments();

        ExtendedMemberInfoCollection MemberStore
        {
            get;
            set;
        }
    }

    internal interface IReader<T> : IReader, IDisposable
        where T : class, new()
    {
        IEnumerable<InstanceProxy<T>> ReadRecords();

        CodeGenerator<T> Generator { get; }

        U ExtractKey<U>(T instance) where U : struct;
    }
}
