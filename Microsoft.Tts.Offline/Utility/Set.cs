//----------------------------------------------------------------------------
// <copyright file="Set.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//
// <summary>
//     This module implements class to qunantize float values into byte or short
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Set/Collection data presentation type.
    /// </summary>
    public enum SetType
    {
        /// <summary>
        /// Abstract set.
        /// </summary>
        AbstractSet = 0,

        /// <summary>
        /// Bit set enum.
        /// </summary>
        BitSet = 1,

        /// <summary>
        /// Index set enum.
        /// </summary>
        IndexSet = 2,

        /// <summary>
        /// Range set enum.
        /// </summary>
        RangeSet = 3,

        /// <summary>
        /// Byte set enum.
        /// </summary>
        ByteSet = 4
    }

    /// <summary>
    /// Set utilities.
    /// </summary>
    public static class SetUtil
    {
        /// <summary>
        /// Reads set from given reader.
        /// </summary>
        /// <param name="reader">Source reader.</param>
        /// <returns>Set as integer collection.</returns>
        public static ICollection<int> Read(BinaryReader reader)
        {
            ICollection<int> ret = null;

            SetType baseType = (SetType)reader.ReadInt32();
            if (baseType != SetType.AbstractSet)
            {
                throw new InvalidDataException("Unexpected set type");
            }

            int minValue = reader.ReadInt32();
            int maxValue = reader.ReadInt32();

            SetType actualType = (SetType)reader.ReadInt32();

            switch (actualType)
            {
                case SetType.AbstractSet:
                case SetType.BitSet:
                case SetType.ByteSet:
                    throw new NotSupportedException("Unsupported set type");
                case SetType.IndexSet:
                    ret = ReadIndexSet(reader);
                    break;
                case SetType.RangeSet:
                    ret = ReadRangeSet(reader);
                    break;
                default:
                    throw new NotSupportedException("Unknown set type");
            }

            return ret;
        }

        /// <summary>
        /// Write bitArray as given set type.
        /// </summary>
        /// <param name="setType">Set type to save bit array as.</param>
        /// <param name="bitArray">Represents the non negative integer set.</param>
        /// <param name="writer">Target writer.</param>
        /// <returns>Bytes written.</returns>
        public static int Write(SetType setType, BitArray bitArray, BinaryWriter writer)
        {
            int bytesWritten = 0;

            writer.Write((int)SetType.AbstractSet);
            writer.Write((int)0); // min value
            writer.Write((int)(bitArray.Length - 1)); // max value
            bytesWritten += sizeof(int) * 3;

            switch (setType)
            {
                case SetType.BitSet:
                    bytesWritten += SaveBitSet(bitArray, writer);
                    break;
                case SetType.IndexSet:
                    bytesWritten += SaveIndexSet(bitArray, writer);
                    break;
                case SetType.RangeSet:
                    bytesWritten += SaveRangeSet(bitArray, writer);
                    break;
                default:
                    throw new NotSupportedException("Unsupported set type for save operation");
            }

            return bytesWritten;
        }

        /// <summary>
        /// Read index set.
        /// </summary>
        /// <param name="reader">The reader object.</param>
        /// <returns>The index collection.</returns>
        private static ICollection<int> ReadIndexSet(BinaryReader reader)
        {
            List<int> ret = new List<int>();
            int indexCount = reader.ReadInt32();
            for (int i = 0; i < indexCount; i++)
            {
                ret.Add(reader.ReadInt32());
            }

            Debug.Assert(ret.Distinct().Count() == ret.Count, "Unexpected index set data");
            return ret;
        }

        /// <summary>
        /// Read range set.
        /// </summary>
        /// <param name="reader">The reader object.</param>
        /// <returns>The interger collection from range set.</returns>
        private static ICollection<int> ReadRangeSet(BinaryReader reader)
        {
            List<int> ret = new List<int>();

            int rangeCount = reader.ReadInt32();

            for (int i = 0; i < rangeCount; i++)
            {
                int lowerBound = reader.ReadInt32();
                int upperBound = reader.ReadInt32();

                ret.AddRange(Enumerable.Range(lowerBound, upperBound - lowerBound));
            }

            Debug.Assert(ret.Distinct().Count() == ret.Count, "Unexpected range data");

            return ret;
        }

        /// <summary>
        /// Save bit array as bit set.
        /// </summary>
        /// <param name="bitArray">The bit array object.</param>
        /// <param name="writer">The writer object.</param>
        /// <returns>Bytes writen.</returns>
        private static int SaveBitSet(BitArray bitArray, BinaryWriter writer)
        {
            int bytesWritten = 0;

            writer.Write((int)SetType.BitSet);
            writer.Write((int)bitArray.Length); // size
            writer.Write((int)bitArray.OfType<bool>().Count(x => x)); // size
            bytesWritten += sizeof(int) * 3;

            // ((_unitSet.Length + 31) >> 5) * 4
            // calculate the number of bytes to allocate to save
            // the bit set data. the data is INT (4 bytes) aligned
            byte[] data = new byte[((bitArray.Length + 31) >> 5) * 4];
            bitArray.CopyTo(data, 0);
            writer.Write(data, 0, data.Length);
            bytesWritten += data.Length;

            return bytesWritten;
        }

        /// <summary>
        /// Save bit array as index set.
        /// </summary>
        /// <param name="bitArray">The bit array object.</param>
        /// <param name="writer">The writer object.</param>
        /// <returns>Bytes writen.</returns>
        private static int SaveIndexSet(BitArray bitArray, BinaryWriter writer)
        {
            int bytesWritten = 0;

            writer.Write((int)SetType.IndexSet);
            writer.Write((int)bitArray.OfType<bool>().Count(x => x));
            bytesWritten += sizeof(int) * 2;

            for (int i = 0; i < bitArray.Length; i++)
            {
                if (bitArray[i])
                {
                    writer.Write(i);
                    bytesWritten += sizeof(int);
                }
            }

            return bytesWritten;
        }

        /// <summary>
        /// Save bit array as range set.
        /// </summary>
        /// <param name="bitArray">The bit array object.</param>
        /// <param name="writer">The writer object.</param>
        /// <returns>Bytes writen.</returns>
        private static int SaveRangeSet(BitArray bitArray, BinaryWriter writer)
        {
            int bytesWritten = 0;

            writer.Write((int)SetType.RangeSet);
            bytesWritten += sizeof(int);

            int[] lowerBounds = Enumerable.Range(0, bitArray.Length).Where(
                x => (x == 0 && bitArray[x]) || (x > 0 && !bitArray[x - 1] && bitArray[x])).ToArray();

            int[] upperBounds = Enumerable.Range(1, bitArray.Length).Where(
                x => (x == bitArray.Length && bitArray[x - 1]) || (x < bitArray.Length && bitArray[x - 1] && !bitArray[x])).ToArray();

            Debug.Assert(lowerBounds.Length == upperBounds.Length, "Extracted bit array boundary mismatch.");

            writer.Write(lowerBounds.Length);
            bytesWritten += sizeof(int);

            for (int i = 0; i < lowerBounds.Length; i++)
            {
                writer.Write(lowerBounds[i]);
                writer.Write(upperBounds[i]);
                bytesWritten += sizeof(int) * 2;
            }

            return bytesWritten;
        }
    }
}