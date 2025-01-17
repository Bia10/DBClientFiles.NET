﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace DBClientFiles.NET.Utils.Extensions
{
    internal static class CollectionsExtensions
    {
        public static IEnumerable<T> MakeEnumerable<T>(this IEnumerator<T> enumerator)
        {
            return new Enumerable<T>(enumerator);
        }

        private class Enumerable<T> : IEnumerable<T>, IDisposable
        {
            private readonly IEnumerator<T> _enumerator;

            public Enumerable(IEnumerator<T> enumerator)
            {
                _enumerator = enumerator;
            }

            public IEnumerator<T> GetEnumerator() => _enumerator;

            IEnumerator IEnumerable.GetEnumerator() => _enumerator;

            public void Dispose()
            {
                _enumerator.Dispose();
            }
        }
    }
}
