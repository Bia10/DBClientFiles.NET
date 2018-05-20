﻿using DBClientFiles.NET.Collections;
using DBClientFiles.NET.Collections.Generic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DBClientFiles.NET.Test
{
    public class StructureTester
    {
        public static void InspectInstance(object instance)
        {
            foreach (var memberInfo in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!memberInfo.PropertyType.IsArray)
                {
                    if (memberInfo.PropertyType == typeof(string))
                        Console.WriteLine($@"{memberInfo.Name}: ""{memberInfo.GetValue(instance)}""");
                    else
                        Console.WriteLine($@"{memberInfo.Name}: {memberInfo.GetValue(instance)}");
                }
                else
                {
                    var value = (Array)memberInfo.GetValue(instance);
                    Console.WriteLine($"{memberInfo.Name}: [{value.Length}] {{");
                    if (memberInfo.PropertyType == typeof(string[]))
                    {
                        for (var i = 0; i < value.Length; ++i)
                            if (!string.IsNullOrEmpty((string)value.GetValue(i)))
                                Console.WriteLine($@"{i.ToString().PadLeft(5)}: ""{value.GetValue(i)}""");
                    }
                    else
                        for (var i = 0; i < value.Length; ++i)
                            Console.WriteLine($@"{i.ToString().PadLeft(5)}: {value.GetValue(i)}");

                    Console.WriteLine("}");
                }
            }
        }
    }

    public class StructureTester<TValue> where TValue : class, new()
    {
        public void InspectInstance(TValue instance)
        {
            foreach (var memberInfo in typeof(TValue).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!memberInfo.PropertyType.IsArray)
                {
                    if (memberInfo.PropertyType == typeof(string))
                        Console.WriteLine($@"{memberInfo.Name}: ""{memberInfo.GetValue(instance)}""");
                    else
                        Console.WriteLine($@"{memberInfo.Name}: {memberInfo.GetValue(instance)}");
                }
                else
                {
                    var value = (Array)memberInfo.GetValue(instance);
                    Console.WriteLine($"{memberInfo.Name}: [{value.Length}] {{");
                    if (memberInfo.PropertyType == typeof(string[]))
                    {
                        for (var i = 0; i < value.Length; ++i)
                            if (!string.IsNullOrEmpty((string)value.GetValue(i)))
                                Console.WriteLine($@"{i.ToString().PadLeft(5)}: ""{value.GetValue(i)}""");
                    }
                    else
                        for (var i = 0; i < value.Length; ++i)
                            Console.WriteLine($@"{i.ToString().PadLeft(5)}: {value.GetValue(i)}");

                    Console.WriteLine("}");
                }
            }
        }

        public TimeSpan AccumulateList(Stream dataStream, StorageOptions options) => Accumulate<StorageList<TValue>>(dataStream, options, out var _);
        public TimeSpan AccumulateList(Stream dataStream) => AccumulateList(dataStream, StorageOptions.Default);

        public TimeSpan AccumulateDictionary<TKey>(Stream dataStream, StorageOptions options) where TKey : struct
            => Accumulate<StorageDictionary<TKey, TValue>>(dataStream, options);
        public TimeSpan AccumulateDictionary<TKey>(Stream dataStream) where TKey : struct
            => AccumulateDictionary<TKey>(dataStream, StorageOptions.Default);

        public TimeSpan Accumulate<TStorage>(Stream dataStream) where TStorage : StorageBase<TValue>
            => Accumulate<TStorage>(dataStream, out var _);
    
        public TimeSpan Accumulate<TStorage>(Stream dataStream, StorageOptions options) where TStorage : StorageBase<TValue>
            => Accumulate<TStorage>(out var instance, dataStream, options, out var _);

        public TimeSpan Accumulate<TStorage>(Stream dataStream, out TimeSpan deserializerGenerationTime) where TStorage : StorageBase<TValue>
            => Accumulate<TStorage>(dataStream, out deserializerGenerationTime);

        public TimeSpan Accumulate<TStorage>(Stream dataStream, StorageOptions options, out TimeSpan deserializerGenerationTime) where TStorage : StorageBase<TValue>
            => Accumulate<TStorage>(out var instance, dataStream, options, out deserializerGenerationTime);

        public TimeSpan Accumulate<TStorage>(out TStorage instance, Stream dataStream, StorageOptions options, out TimeSpan deserializerGenerationTime)
            where TStorage : StorageBase<TValue>
        {
            dataStream.Position = 0;

            var timer = new Stopwatch();
            {
                timer.Start();
                instance = (TStorage)typeof(TStorage).CreateInstance(dataStream, options);
                timer.Stop();
            }

#if PERFORMANCE
            deserializerGenerationTime = instance.LambdaGeneration;
#else
            deserializerGenerationTime = TimeSpan.Zero;
#endif

            return timer.Elapsed - TypeExtensions.LambdaGenerationTime;
        }

        public BenchmarkResult Benchmark<TStorage>(out TStorage instance, Stream dataStream, int iterationCount = 100) where TStorage : StorageBase<TValue>
            => Benchmark(out instance, dataStream, StorageOptions.Default, iterationCount);

        public BenchmarkResult Benchmark<TStorage>(out TStorage instance, Stream dataStream, StorageOptions options, int iterationCount = 100) where TStorage : StorageBase<TValue>
        {
            if (iterationCount == 0)
                throw new ArgumentOutOfRangeException(nameof(iterationCount));

            var benchmarkResult = new BenchmarkResult();
            benchmarkResult.RecordType = typeof(TValue);

            // Stupid workaround for the compiler not picking up assignment of instance in the loop
            benchmarkResult.TotalTimes.Add(Accumulate(out instance, dataStream, options, out var lambdaTime));
            benchmarkResult.LambdaGenerationTimes.Add(lambdaTime);

            for (var i = 1; i < iterationCount; ++i)
            {
                benchmarkResult.TotalTimes.Add(Accumulate<TStorage>(dataStream, options, out lambdaTime));
                benchmarkResult.LambdaGenerationTimes.Add(lambdaTime);
            }

            benchmarkResult.Container = (IList)instance;

            return benchmarkResult;
        }

        public BenchmarkResult Benchmark<TStorage>(Stream dataStream, int iterationCount = 100) where TStorage : StorageBase<TValue>
            => Benchmark<TStorage>(dataStream, StorageOptions.Default, iterationCount);

        public BenchmarkResult Benchmark<TStorage>(Stream dataStream, StorageOptions options, int iterationCount = 100) where TStorage : StorageBase<TValue>
            => Benchmark<TStorage>(out var instance, dataStream, options, iterationCount);
    }

    public class BenchmarkResult
    {
        public BenchmarkResult()
        {
            TotalTimes = new List<TimeSpan>();
            LambdaGenerationTimes = new List<TimeSpan>();

            RecordType = typeof(object);
            Signature = Signatures.WDBC;
        }

        public List<TimeSpan> TotalTimes { get; }
        public TimeSpan BestTime => TotalTimes.Min();
        public TimeSpan WorstTime => TotalTimes.Max();
        public TimeSpan AverageTime => new TimeSpan(Convert.ToInt64(TotalTimes.Average(t => t.Ticks)));
        public Type RecordType { get; set; }
        public Signatures Signature { get; }
        public IList Container { get; set; }

        public TimeSpan TimePercentile(double percentile)
        {
            TotalTimes.Sort();

            int N = TotalTimes.Count;
            double n = (N - 1) * percentile + 1;
            if (n == 1d)
                return TotalTimes.First();
            else if (n == N)
                return TotalTimes.Last();

            int k = (int)n;
            double d = n - k;
            return TotalTimes[k - 1] + new TimeSpan(Convert.ToInt64(d * (TotalTimes[k] - TotalTimes[k - 1]).Ticks));

        }

        public List<TimeSpan> LambdaGenerationTimes { get; }
        public TimeSpan BestLambdaGenerationTime => LambdaGenerationTimes.Min();
        public TimeSpan WorstLambdaGenerationTime => LambdaGenerationTimes.Max();
        public TimeSpan AverageLambdaGenerationTime => new TimeSpan(Convert.ToInt64(LambdaGenerationTimes.Average(t => t.Ticks)));

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append((RecordType.Name.ToString() + " " + Signature.ToString() + "[" + Container.Count + " entries]").PadRight(45) + "|");

            stringBuilder.Append(AverageTime.ToString(@"ss\.ffffff").PadRight(15) + "|");
            stringBuilder.Append(BestTime.ToString(@"ss\.ffffff").PadRight(15) + "|");
            stringBuilder.Append(WorstTime.ToString(@"ss\.ffffff").PadRight(20) + "|");
#if PERFORMANCE
            stringBuilder.Append(AverageLambdaGenerationTime.ToString(@"ss\.ffffff").PadRight(15) + "|");
            stringBuilder.Append(BestLambdaGenerationTime.ToString(@"ss\.ffffff").PadRight(15) + "|");
            stringBuilder.Append(WorstLambdaGenerationTime.ToString(@"ss\.ffffff") + "|");
#endif
            return stringBuilder.ToString();
        }

        public void WriteCSV(StreamWriter writer)
        {
            // writer.WriteLine("\""+RecordType.Name.ToString().Replace("Entry", "") + "\"," + Signature.ToString() + "," + Container.Count);
            for (var i = 0; i < TotalTimes.Count; ++i)
                writer.WriteLine("{0},{1},{2},,{3},{4}",
                    i,
                    TotalTimes[i].TotalMilliseconds, LambdaGenerationTimes[i].TotalMilliseconds,
                    TotalTimes[i].TotalMilliseconds / Container.Count, LambdaGenerationTimes[i].TotalMilliseconds / Container.Count);
            writer.WriteLine();
        }

        public static string Header
        {
            get
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("File name".PadRight(45) + "|");

                stringBuilder.Append("Avg".PadRight(15) + "|");
                stringBuilder.Append("Best".PadRight(15) + "|");
                stringBuilder.Append("Worst".PadRight(20) + "|");

#if PERFORMANCE
                stringBuilder.Append("Avg Lambda".PadRight(15) + "|");
                stringBuilder.Append("Best Lambda ".PadRight(15) + "|");
                stringBuilder.Append("Worst Lambda".PadRight(20) + "|");
#endif
                return stringBuilder.ToString();
            }
        }

#if PERFORMANCE
        public static string HeaderSep => new string('=', 45 + (15 + 15 + 20) + (15 + 15 + 20));
#else
        public static string HeaderSep => new string('=', 45 + (15 + 15 + 20));
#endif
    }
}
