//----------------------------------------------------------------------------
// <copyright file="EnumerableExtension.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements EnumerableExtension class.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Extension methods for IEnumerable.
    /// </summary>
    public static class EnumerableExtension
    {
        /// <summary>
        /// Assert no duplication in given source.
        /// </summary>
        /// <typeparam name="TSource">Type of source.</typeparam>
        /// <param name="source">Source list.</param>
        public static void AssertNoDuplication<TSource>(this IEnumerable<TSource> source)
        {
            Debug.Assert(source.Distinct().Count() == source.Count(), "Item duplicated in source");
        }

        /// <summary>
        /// Get hash code for an integer array.
        /// </summary>
        /// <param name="values">Integer list.</param>
        /// <returns>The hash code.</returns>
        public static int GetHash(this IList<int> values)
        {
            Helper.ThrowIfNull(values);

            var ret = 0;

            for (int i = 0; i < values.Count; i++)
            {
                ret = (ret * 31) ^ values[i];
            }

            return ret;
        }

        /// <summary>
        /// Serialize a list to bytes.
        /// </summary>
        /// <typeparam name="T">List type.</typeparam>
        /// <param name="values">List items.</param>
        /// <param name="itemSize">Item size of list type.</param>
        /// <returns>The bytes serialized.</returns>
        public static byte[] Serialize<T>(this T[] values, int itemSize)
        {
            Helper.ThrowIfNull(values);

            var lengthBytes = BitConverter.GetBytes(values.Length);
            byte[] ret = new byte[lengthBytes.Length + (values.Length * itemSize)];

            Buffer.BlockCopy(lengthBytes, 0, ret, 0, lengthBytes.Length);
            Buffer.BlockCopy(values, 0, ret, lengthBytes.Length, values.Length * itemSize);

            return ret;
        }

        /// <summary>
        /// Adds two lists.
        /// </summary>
        /// <param name="list1">List 1.</param>
        /// <param name="list2">List 2.</param>
        /// <returns>The summed list.</returns>
        public static float[] Add(this IList<float> list1, IList<float> list2)
        {
            Helper.ThrowIfNullOrUnequalLength(list1, list2);

            return list1.Select((v, i) => v + list2[i]).ToArray();
        }

        /// <summary>
        /// Linearly sample items.
        /// </summary>
        /// <typeparam name="T">List type.</typeparam>
        /// <param name="items">List items.</param>
        /// <param name="count">The number of items to sample.</param>
        /// <returns>Items sampled.</returns>
        public static T[] LinearMap<T>(this IList<T> items, int count)
        {
            Helper.ThrowIfNull(items);

            double ratio = (double)(items.Count - 1) / (double)(count - 1);

            return Enumerable.Range(0, count)
                .Select(i => items[(int)Math.Round(i * ratio, MidpointRounding.AwayFromZero)]).ToArray();
        }

        /// <summary>
        /// Group list items into sub lists.
        /// </summary>
        /// <typeparam name="T">List type.</typeparam>
        /// <param name="values">List items.</param>
        /// <param name="lengths">The sub list lengths.</param>
        /// <returns>Items grouped.</returns>
        public static T[][] Group<T>(this IList<T> values, int[] lengths)
        {
            Helper.ThrowIfNull(values);
            Helper.ThrowIfNull(lengths);

            if (values.Count != lengths.Sum())
            {
                throw new ArgumentException("Invalid number of values for grouping");
            }

            var starts = lengths.Starts();

            return starts.Select((s, i) =>
                values.Skip(s).Take(lengths[i]).ToArray()).ToArray();
        }

        /// <summary>
        /// Get starts values of accumulated values.
        /// </summary>
        /// <param name="values">List values.</param>
        /// <returns>Start lists for values.</returns>
        public static int[] Starts(this IList<int> values)
        {
            Helper.ThrowIfNull(values);

            int[] ret = new int[values.Count];

            int cum = 0;

            for (int i = 0; i < values.Count; i++)
            {
                ret[i] = cum;
                cum += values[i];
            }

            return ret;
        }

        /// <summary>
        /// Sorts the given sequence.
        /// </summary>
        /// <typeparam name="TSource">Type of source.</typeparam>
        /// <typeparam name="TResult">Type of result.</typeparam>
        /// <param name="source">The source list.</param>
        /// <param name="selector">The selector for source list.</param>
        /// <returns>The sorted source array.</returns>
        public static TSource[] SortBy<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            TResult[] sort = source.Select(selector).ToArray();
            TSource[] array = source.ToArray();
            Array.Sort(sort, array);
            return array;
        }

        /// <summary>
        /// Concatenates items in an enumerable list with given delimiter.
        /// </summary>
        /// <typeparam name="T">Type of item in the list.</typeparam>
        /// <param name="list">List of items to concatenate.</param>
        /// <param name="delimiter">Delimiter for joint between items.</param>
        /// <returns>Joint string.</returns>
        public static string Concatenate<T>(this IEnumerable<T> list, string delimiter)
        {
            Helper.ThrowIfNull(list);

            if (delimiter == null)
            {
                delimiter = string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            foreach (T item in list)
            {
                if (builder.Length > 0)
                {
                    builder.Append(delimiter);
                }

                builder.Append(item);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Gets the index of the minimum value.
        /// </summary>
        /// <typeparam name="TSource">Type of source.</typeparam>
        /// <param name="source">The source list.</param>
        /// <returns>The index of the minimum value in the source list.</returns>
        public static int MinIndex<TSource>(this IList<TSource> source) where TSource : IComparable
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (source.Count == 0)
            {
                throw new ArgumentException("source");
            }

            int index = 0;
            for (int i = 1; i < source.Count; ++i)
            {
                if (source[i].CompareTo(source[index]) < 0)
                {
                    index = i;
                }
            }

            return index;
        }

        /// <summary>
        /// Gets the index of the maximum value.
        /// </summary>
        /// <typeparam name="TSource">Type of source.</typeparam>
        /// <param name="source">The source list.</param>
        /// <returns>The index of the maximum value in the source list.</returns>
        public static int MaxIndex<TSource>(this IList<TSource> source) where TSource : IComparable
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (source.Count == 0)
            {
                throw new ArgumentException("source");
            }

            int index = 0;
            for (int i = 1; i < source.Count; ++i)
            {
                if (source[i].CompareTo(source[index]) > 0)
                {
                    index = i;
                }
            }

            return index;
        }

        /// <summary>
        /// Uniformly takes maxCount elements from source.
        /// If source has less than maxCount elements, all will be taken.
        /// </summary>
        /// <typeparam name="TSource">Type of source.</typeparam>
        /// <param name="source">The source list.</param>
        /// <param name="maxCount">Max elements to be taken.</param>
        /// <returns>Elements taken.</returns>
        public static TSource[] UniformTake<TSource>(this IList<TSource> source, int maxCount)
        {
            TSource[] ret;

            if (source.Count <= maxCount)
            {
                ret = source.ToArray();
            }
            else
            {
                int interval = source.Count / maxCount;
                ret = Enumerable.Range(0, maxCount).Select(i => source[i * interval]).ToArray();
            }

            return ret;
        }

        /// <summary>
        /// Generates a sequence of permutations of given alphabet.
        /// </summary>
        /// <typeparam name="T">Type of alphabet element.</typeparam>
        /// <param name="alphabet">Alphabet to permute.</param>
        /// <param name="length">Length of each permutation.</param>
        /// <returns>The sequence of permutations of given alphabet.</returns>
        public static IEnumerable<T[]> Permute<T>(this T[] alphabet, int length)
        {
            long total = (long)Math.Pow(alphabet.Length, length);

            for (long i = 0; i < total; i++)
            {
                T[] ret = new T[length];

                var index = i;

                for (int idx = 0; idx < length; idx++)
                {
                    long rem;
                    index = Math.DivRem(index, alphabet.Length, out rem);
                    ret[idx] = alphabet[rem];
                }

                yield return ret;
            }
        }

        /// <summary>
        /// Split source into chunks with equal size of sizePerChunk.
        /// The last chunk may have less items.
        /// </summary>
        /// <typeparam name="TSource">Type of source.</typeparam>
        /// <param name="source">The source list.</param>
        /// <param name="sizePerChunk">Size of each chunk.</param>
        /// <returns>Chunks splitted.</returns>
        public static IEnumerable<TSource[]> Split<TSource>(this IEnumerable<TSource> source, int sizePerChunk)
        {
            Helper.ThrowIfNull(source);

            Func<TSource, int> selector = item => 1;

            return Split<TSource>(source, sizePerChunk, selector);
        }

        /// <summary>
        /// Split source into chunks with equal size of sizePerChunk.
        /// The last chunk may have less items.
        /// </summary>
        /// <typeparam name="TSource">Type of source.</typeparam>
        /// <param name="source">The source list.</param>
        /// <param name="sizePerChunk">Size of each chunk.</param>
        /// <param name="selector">Select size of each element.</param>
        /// <returns>Chunks splitted.</returns>
        public static IEnumerable<TSource[]> Split<TSource>(this IEnumerable<TSource> source, int sizePerChunk, Func<TSource, int> selector)
        {
            Helper.ThrowIfNull(source);
            Helper.ThrowIfNull(selector);

            List<TSource> chunk = new List<TSource>();

            int chunkSize = 0;
            foreach (var item in source)
            {
                chunk.Add(item);
                chunkSize += selector(item);

                if (chunkSize >= sizePerChunk)
                {
                    yield return chunk.ToArray();
                    chunk.Clear();
                    chunkSize = 0;
                }
            }

            if (chunk.Count > 0)
            {
                yield return chunk.ToArray();
            }
        }

        /// <summary>
        /// Performances action upon each item in the list.
        /// </summary>
        /// <typeparam name="T">Type of sources.</typeparam>
        /// <param name="sources">The source list.</param>
        /// <param name="action">The action is to be applied to each item.</param>
        public static void ForEach<T>(this IEnumerable<T> sources, Action<T> action)
        {
            Helper.ThrowIfNull(sources);
            Helper.ThrowIfNull(action);

            foreach (T item in sources)
            {
                action(item);
            }
        }

        /// <summary>
        /// Get count of items in the list.
        /// </summary>
        /// <typeparam name="T">Type of sources.</typeparam>
        /// <param name="sources">The source list.</param>
        /// <returns>Element count.</returns>
        public static int ElementCount<T>(this IEnumerable<T> sources)
        {
            Helper.ThrowIfNull(sources);

            int count = 0;
            foreach (T item in sources)
            {
                count++;
            }

            return count;
        }
    }
}