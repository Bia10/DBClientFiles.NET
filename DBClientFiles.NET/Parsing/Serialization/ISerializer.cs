﻿using DBClientFiles.NET.Parsing.File;
using DBClientFiles.NET.Parsing.File.Records;

namespace DBClientFiles.NET.Parsing.Serialization
{
    internal interface ISerializer<T>
    {
        /// <summary>
        /// Deserializes an instance of <see cref="{T}"/> from the provided stream.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        T Deserialize(IRecordReader reader, IParser<T> parser);

        /// <summary>
        /// Given an instance of <see cref="{T}"/>, perform a deep copy operation and return a new object.
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        T Clone(in T origin);

        int GetRecordIndex(in T instance);
        void SetRecordIndex(out T instance, int index);

        void SetIndexColumn(int indexColumn);

        ref readonly StorageOptions Options { get; }

        void Initialize(IBinaryStorageFile storage);
    }
}
