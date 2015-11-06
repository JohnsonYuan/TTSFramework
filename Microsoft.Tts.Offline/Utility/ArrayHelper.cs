//----------------------------------------------------------------------------
// <copyright file="ArrayHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This is an array conversing helper class. It helps to processing
//     the wave file in different bit per sample.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Array helper class, some static function to help user to convert array types. 
    /// </summary>
    [CLSCompliant(false)]
    public static class ArrayHelper
    {
        #region Public static operations

        /// <summary>
        /// Convert short[] to Byte[] in memory order
        /// Like Marshal buffer copy.
        /// </summary>
        /// <param name="from">Source array to convert.</param>
        /// <returns>Result byte array.</returns>
        public static byte[] BinaryConvertArray(short[] from)
        {
            if (from == null)
            {
                throw new ArgumentNullException("from");
            }

            byte[] to = new byte[from.Length << 1];
            Buffer.BlockCopy(from, 0, to, 0, to.Length);
            return to;
        }

        /// <summary>
        /// Convert Byte[] to short[] in memory order.
        /// Like Marshal buffer copy.
        /// </summary>
        /// <param name="from">Source array to convert.</param>
        /// <returns>Result short array.</returns>
        public static short[] BinaryConvertArray(byte[] from)
        {
            if (from == null)
            {
                throw new ArgumentNullException("from");
            }

            short[] to = new short[from.Length >> 1];
            Buffer.BlockCopy(from, 0, to, 0, from.Length);
            return to;
        }

        /// <summary>
        /// Convert a T1[] to int[], T1 must have implemented IConvertible,
        /// The size of from must be bigger then size.
        /// </summary>
        /// <typeparam name="T1">Type of parameter.</typeparam>
        /// <param name="from">Source array to convert.</param>
        /// <param name="size">Size of the source array.</param>
        /// <returns>Result int array.</returns>
        public static int[] ToInt32<T1>(T1[] from, int size)
            where T1 : struct, IConvertible
        {
            if (from == null || from.Length == 0)
            {
                throw new ArgumentNullException("from");
            }

            if (from.Length < size)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "source array's size must not be less than {0}", size);
                throw new ArgumentException(message, "from");
            }

            int[] to = new int[size];

            for (int i = 0; i < size; ++i)
            {
                // the value type would not be null, avoid the PRESharp warning here
                // reviewed by xwhan, zweng
                // if (from[i] == null)
                // {
                //    throw new ArgumentException("from");
                // }
                to[i] = from[i].ToInt32(System.Globalization.CultureInfo.InvariantCulture);
            }

            return to;
        }

        /// <summary>
        /// Convert a T1[] to int[], T1 must have implemented IConvertible.
        /// </summary>
        /// <typeparam name="T1">Type of parameter.</typeparam>
        /// <param name="from">Source array to convert.</param>
        /// <returns>Result byte array.</returns>
        public static int[] ToInt32<T1>(T1[] from)
            where T1 : struct, IConvertible
        {
            if (from == null || from.Length == 0)
            {
                throw new ArgumentNullException("from");
            }

            return ToInt32<T1>(from, from.Length);
        }

        /// <summary>
        /// Convert a T1[] to Single[], T1 must have implemented IConvertible,
        /// The size of from must be bigger then size.
        /// </summary>
        /// <typeparam name="T1">Type of parameter.</typeparam>
        /// <param name="from">Source array to convert.</param>
        /// <param name="size">Size of the source array.</param>
        /// <returns>Result Single float array.</returns>
        public static float[] ToSingle<T1>(T1[] from, int size)
            where T1 : struct, IConvertible
        {
            if (from == null || from.Length == 0)
            {
                throw new ArgumentNullException("from");
            }

            if (from.Length < size)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "source array's size must not be less than {0}", size);
                throw new ArgumentException(message, "from");
            }

            float[] to = new float[size];

            for (int i = 0; i < size; ++i)
            {
                // the value type would not be null, avoid the PRESharp warning here
                // reviewed by xwhan, zweng
                // if (from[i] == null)
                // {
                //     throw new ArgumentException("from");
                // }
                to[i] = from[i].ToSingle(System.Globalization.CultureInfo.InvariantCulture);
            }

            return to;
        }

        /// <summary>
        /// Convert a T1[] to Single[], T1 must have implemented IConvertible.
        /// </summary>
        /// <typeparam name="T1">Type of parameter.</typeparam>
        /// <param name="from">Source array to convert.</param>
        /// <returns>Result Single float array.</returns>
        public static float[] ToSingle<T1>(T1[] from)
            where T1 : struct, IConvertible
        {
            if (from == null || from.Length == 0)
            {
                throw new ArgumentNullException("from");
            }

            return ToSingle<T1>(from, from.Length);
        }

        /// <summary>
        /// Convert a T1[] to ToInt16[], T1 must have implemented IConvertible,
        /// The size of from must be bigger then size.
        /// </summary>
        /// <typeparam name="T1">Type of parameter.</typeparam>
        /// <param name="from">Source array to convert.</param>
        /// <param name="size">Size of the source array.</param>
        /// <returns>Result short float array.</returns>
        public static short[] ToInt16<T1>(T1[] from, int size)
            where T1 : struct, IConvertible
        {
            if (from == null || from.Length == 0)
            {
                throw new ArgumentNullException("from");
            }

            if (from.Length < size)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "source array's size must not be less than {0}", size);
                throw new ArgumentException(message, "from");
            }

            short[] to = new short[size];

            for (int i = 0; i < size; ++i)
            {
                // the value type would not be null, avoid the PRESharp warning here
                // reviewed by xwhan, zweng
                // if (from[i] == null)
                // {
                //     throw new ArgumentException("from");
                // }
                to[i] = from[i].ToInt16(System.Globalization.CultureInfo.InvariantCulture);
            }

            return to;
        }

        /// <summary>
        /// Convert a T1[] to ToInt16[], T1 must have implemented IConvertible.
        /// </summary>
        /// <typeparam name="T1">Type of parameter.</typeparam>
        /// <param name="from">Source array to convert.</param>
        /// <returns>Result short float array.</returns>
        public static short[] ToInt16<T1>(T1[] from)
            where T1 : struct, IConvertible
        {
            if (from == null || from.Length == 0)
            {
                throw new ArgumentNullException("from");
            }

            return ToInt16<T1>(from, from.Length);
        }

        /// <summary>
        /// Performce n-bye alignment on the given memory block.
        /// The n is specified by the unit size.
        /// </summary>
        /// <param name="data">Byte array to be aligned to.</param>
        /// <param name="unitSize">Aling unit size.</param>
        /// <returns>Aligned byte array.</returns>
        public static byte[] BytesAlign(byte[] data, int unitSize)
        {
            Helper.ThrowIfNull(data);
            if (unitSize < 2)
            {
                throw new ArgumentException(Helper.NeutralFormat(
                    "Align unit size [{0}] should not less than 2", unitSize));
            }

            byte[] alignedData = data;
            if (alignedData.Length % unitSize != 0)
            {
                alignedData = new byte[data.Length + unitSize - (alignedData.Length % unitSize)];
                Buffer.BlockCopy(data, 0, alignedData, 0, data.Length);
            }

            return alignedData;
        }

        /// <summary>
        /// Append values to the current array.
        /// </summary>
        /// <param name="array">Array to be appended.</param>
        /// <param name="items">Items to append.</param>
        /// <returns>Append result array.</returns>
        /// <typeparam name="T">Type of elements.</typeparam>
        public static T[] Append<T>(this T[] array, params T[] items)
        {
            Helper.ThrowIfNull(array);
            Helper.ThrowIfNull(items);
            T[] result = new T[array.Length + items.Length];
            for (int i = 0; i < array.Length; i++)
            {
                result[i] = array[i];
            }

            for (int i = 0; i < items.Length; i++)
            {
                result[array.Length + i] = items[i];
            }

            return result;
        }

        /// <summary>
        /// Insert values to the current array.
        /// </summary>
        /// <param name="array">Array to be inserted.</param>
        /// <param name="position">Insert position.</param>
        /// <param name="items">Items to insert.</param>
        /// <returns>Insert result array.</returns>
        /// <typeparam name="T">Type of elements.</typeparam>
        public static T[] Insert<T>(this T[] array, int position, params T[] items)
        {
            Helper.ThrowIfNull(array);
            Helper.ThrowIfNull(items);
            if (position < 0 || position > array.Length)
            {
                throw new ArgumentException(Helper.NeutralFormat(
                    "Position [{0}] should between 0 and [{1}]",
                    position.ToString(CultureInfo.InvariantCulture),
                    array.Length.ToString(CultureInfo.InvariantCulture)));
            }

            T[] result = new T[array.Length + items.Length];
            for (int i = 0; i < position; i++)
            {
                result[i] = array[i];
            }

            for (int i = 0; i < items.Length; i++)
            {
                result[position + i] = items[i];
            }

            for (int i = position; i < array.Length; i++)
            {
                result[items.Length + i] = array[i];
            }

            return result;
        }

        #endregion
    }
}