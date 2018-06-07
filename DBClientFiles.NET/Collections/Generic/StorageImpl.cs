﻿using DBClientFiles.NET.Exceptions;
using DBClientFiles.NET.Internals;
using DBClientFiles.NET.Internals.Versions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DBClientFiles.NET.Internals.Segments;
using DBClientFiles.NET.Internals.Serializers;
using DBClientFiles.NET.Internals.Versions.Headers;
using DBClientFiles.NET.Utils;

namespace DBClientFiles.NET.Collections.Generic
{
    public interface IStorage
    {
        Signatures Signature { get; }
        uint TableHash { get; }
        uint LayoutHash { get; }
    }

    internal static class _StorageImpl
    {
        public static MethodInfo InitializeReader { get; } = typeof(StorageImpl<>).GetMethod("InitializeFileReader", Type.EmptyTypes);
    }

    /// <summary>
    /// A basic implementation of IStorage that does all the heavy lifting. Used by DI in exposed containers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class StorageImpl<T> : IDisposable
        where T : class, new()
    {
        private StorageOptions Options { get; set; }
        private Stream Stream { get; set; }
        public IReader<T> File { get; private set; }
        public IFileHeader Header { get; private set; }

        public CodeGenerator<T> Generator { get; private set; }

        #region Life and death
        public void Dispose()
        {
            if (Options.CopyToMemory)
                Stream?.Dispose();

            Stream = null;
            File = null;
        }

        public StorageImpl(Stream dataStream, StorageOptions options)
        {
            Options = options;

            if (options.CopyToMemory && !(dataStream is MemoryStream))
            {
                Stream = new MemoryStream((int)(dataStream.Length - dataStream.Position));

                dataStream.CopyTo(Stream);
            }
            else
                Stream = dataStream;

            Members = new ExtendedMemberInfoCollection(typeof(T), options);
        }
        #endregion

        public ExtendedMemberInfoCollection Members { get; }

        public TKey ExtractKey<TKey>(T instance) where TKey : struct
        {
            return File.ExtractKey<TKey>(instance);
        }

        public IFileHeader InitializeHeaderInfo()
        {
            var signature = (Signatures)(Stream.ReadByte() | (Stream.ReadByte() << 8) | (Stream.ReadByte() << 16) | (Stream.ReadByte() << 24));
            Header = HeaderFactory.ReadHeader(signature, Stream);

            Members.IndexColumn = Header.IndexColumn;
            Members.HasIndexTable = Header.HasIndexTable;

            return Header;
        }

        public void InitializeFileReader<TKey>()
            where TKey : struct
        {
            if (File != null)
                return;

            switch (Header.Signature)
            {
                case Signatures.WDBC:
                    File = new WDBC<TKey, T>(Header, Stream, Options);
                    break;
                case Signatures.WDB2:
                    File = new WDB2<TKey, T>(Header, Stream, Options);
                    break;
                case Signatures.WDB5:
                    File = new WDB5<TKey, T>(Header, Stream, Options);
                    break;
                case Signatures.WDB6:
                    File = new WDB6<TKey, T>(Header, Stream, Options);
                    break;
                case Signatures.WDB3:
                case Signatures.WDB4:
                    throw new NotSupportedVersionException($"{Header.Signature} files cannot be read without client metadata.");
                case Signatures.WDC1:
                    File = new WDC1<TKey, T>(Header, Stream, Options);
                    break;
                case Signatures.WDC2:
                    File = new WDC2<TKey, T>(Header, Stream, Options);
                    break;
                default:
                    throw new NotSupportedVersionException($"Unknown signature 0x{(int)Header.Signature:X8}!");
            }
        }

        public void PrepareMemberInfo()
        {
            File.MemberStore = Members;

            // Prepare member informations as declared by the file.
            File.PrepareMemberInformations();

            // Map structure to fields.
            Members.MapMembers();

            // Prepare arity of arrays and validate
            Members.CalculateCardinalities();
        }

        public IEnumerable<T> Enumerate()
        {
            // Steal the generator
            Generator = File.Generator;

            File.ReadSegments();
            return File.ReadRecords();
        }
    }
}
