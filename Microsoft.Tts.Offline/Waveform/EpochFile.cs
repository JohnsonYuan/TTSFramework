//----------------------------------------------------------------------------
// <copyright file="EpochFile.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements EggConverter
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Epoch file.
    /// </summary>
    public static class EpochFile
    {
        #region Public static operations

        /// <summary>
        /// Read all compressed epoch data from file path.
        /// </summary>
        /// <param name="filePath">File to read from.</param>
        /// <param name="offset">Byte offset in the file to start reading from.</param>
        /// <returns>Loaded byte array.</returns>
        public static byte[] ReadAllData(string filePath, int offset)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open,
                FileAccess.Read, FileShare.Read, 10000,
                FileOptions.SequentialScan))
            {
                fs.Seek(offset, SeekOrigin.Begin);
                if (fs.Length - offset == 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Malformed data found in file [{0}], for there is no data for epoch.",
                        filePath);
                    throw new InvalidDataException(message);
                }

                byte[] data = new byte[fs.Length - offset];
                int readCount = fs.Read(data, 0, data.Length);
                if (readCount != data.Length)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Malformed data found in file [{0}], for there is no enough data for epoch.",
                        filePath);
                    throw new InvalidDataException(message);
                }

                return data;
            }
        }

        /// <summary>
        /// Read all decoded epoch data from file.
        /// </summary>
        /// <param name="filePath">File to read from.</param>
        /// <param name="offset">Byte offset in the file to start reading from.</param>
        /// <returns>Loaded int array.</returns>
        public static int[] ReadAllDecodedData(string filePath, int offset)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open,
                FileAccess.Read, FileShare.Read, 10000,
                FileOptions.SequentialScan))
            {
                fs.Seek(offset, SeekOrigin.Begin);
                if (fs.Length - offset == 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Malformed data found in file [{0}], for there is no data for epoch.",
                        filePath);
                    throw new InvalidDataException(message);
                }

                byte[] data = new byte[fs.Length - offset];
                int readCount = fs.Read(data, 0, data.Length);
                if (readCount != data.Length)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Malformed data found in file [{0}], for there is no enough data for epoch.",
                        filePath);
                    throw new InvalidDataException(message);
                }

                if (data.Length % 4 != 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Malformed data found while loading decoded epoch file [{0}] for data length [{1}] must be an integer multiple of 4.",
                        filePath, data.Length);
                    throw new InvalidDataException(message);
                }

                int[] outData = new int[data.Length / 4];
                Buffer.BlockCopy(data, 0, outData, 0, data.Length);

                return outData;
            }
        }

        /// <summary>
        /// Write all decoded epoch data into file.
        /// </summary>
        /// <param name="filePath">File to save to.</param>
        /// <param name="targetEpochData">Epoch data to save.</param>
        public static void WriteAllDecodedData(string filePath,
            IEnumerable<int> targetEpochData)
        {
            if (targetEpochData == null)
            {
                throw new ArgumentNullException("targetEpochData");
            }

            FileStream fs = new FileStream(filePath, FileMode.Create);
            try
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    fs = null;
                    foreach (int epoch in targetEpochData)
                    {
                        bw.Write(epoch);
                    }
                }
            }
            finally
            {
                if (null != fs)
                {
                    fs.Dispose();
                }
            }
        }

        /// <summary>
        /// Compress epoch data from integer encoding to byte encoding
        /// <param />
        /// If outEpochs is null, the length of the compressed epoch will be return.
        /// </summary>
        /// <param name="sourceEpochs">Source epoch data to compress.</param>
        /// <param name="offset">Start offset to compress.</param>
        /// <param name="length">Length of epoch to compress.</param>
        /// <param name="outEpochs">Compressed epoch data.</param>
        /// <returns>Length of compressed epoch data.</returns>
        public static int CompressEpoch(int[] sourceEpochs, int offset, int length,
            byte[] outEpochs)
        {
            if (sourceEpochs == null)
            {
                throw new ArgumentNullException("sourceEpochs");
            }

            if (offset < 0 || length < 0 || offset + length > sourceEpochs.Length)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid data found, for the epoch range to compress is out of range of source epoch.");
                throw new InvalidDataException(message);
            }

            int i, j;

            for (j = 0, i = offset; i < offset + length; ++i, ++j)
            {
                int epoch = sourceEpochs[i];

                for (; epoch >= 127; epoch -= 127, ++j)
                {
                    if (outEpochs != null)
                    {
                        outEpochs[j] = 127;
                    }
                }

                for (; epoch <= -128; epoch -= -128, ++j)
                {
                    if (outEpochs != null)
                    {
                        outEpochs[j] = 0x80;
                    }
                }

                if (outEpochs != null)
                {
                    outEpochs[j] = (byte)epoch;
                }
            }

            return j;
        }

        /// <summary>
        /// Decompress epoch data from byte encoding to integer encoding
        /// <param />
        /// If outEpochs is null, the length of the decompressed epoch will be return.
        /// </summary>
        /// <param name="epochs">Source epoch data to decompress.</param>
        /// <param name="outEpoch">Decompressed epoch data.</param>
        /// <returns>Length of decompressed epoch data.</returns>
        public static int DecompressEpoch(byte[] epochs, int[] outEpoch)
        {
            if (epochs == null)
            {
                throw new ArgumentNullException("epochs");
            }

            int outIndex = 0;

            // expand data
            for (int i = 0; i < epochs.Length; ++i)
            {
                if (epochs[i] == 0x7f)
                {
                    int temp = (sbyte)epochs[i];
                    do
                    {
                        ++i;
                        if (i >= epochs.Length)
                        {
                            break;
                        }

                        temp += (sbyte)epochs[i];
                    }
                    while (epochs[i] == 0x7f);

                    if (outEpoch != null)
                    {
                        outEpoch[outIndex] = temp;
                    }

                    outIndex++;
                }
                else if (epochs[i] == 0x80)
                {
                    int temp = (sbyte)epochs[i];
                    if (temp != -128)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "Malformed data to decompressed at the position [{0}].",
                            i);
                        throw new InvalidDataException(message);
                    }

                    do
                    {
                        ++i;
                        if (i >= epochs.Length)
                        {
                            break;
                        }

                        temp += (sbyte)epochs[i];
                    }
                    while (epochs[i] == 0x80);

                    if (outEpoch != null)
                    {
                        outEpoch[outIndex] = temp;
                    }

                    outIndex++;
                }
                else
                {
                    if (outEpoch != null)
                    {
                        outEpoch[outIndex] = (sbyte)epochs[i];
                    }

                    outIndex++;
                }
            }

            return outIndex;
        }

        /// <summary>
        /// Used to transfer epoch values to position values. For example,
        /// If epoch buffer is -50, -40, 60, 70, then the position buffer will
        /// Be: -50, -90, 150, 220. The sign of position ones are the same as 
        /// The corresponding buffer ones to indicate voiced/unvoiced.
        /// </summary>
        /// <param name="epochs">Epoch data to convert.</param>
        /// <returns>Offset point array.</returns>
        public static int[] EpochToOffset(int[] epochs)
        {
            if (epochs == null || epochs.Length == 0)
            {
                throw new ArgumentNullException("epochs");
            }

            int[] epochOffsets = new int[epochs.Length];
            epochOffsets[0] = epochs[0];

            for (int epochIndex = 1; epochIndex < epochs.Length; epochIndex++)
            {
                epochOffsets[epochIndex] =
                    Math.Abs(epochOffsets[epochIndex - 1])
                    + Math.Abs(epochs[epochIndex]);

                if (epochs[epochIndex] < 0)
                {
                    epochOffsets[epochIndex] = -epochOffsets[epochIndex];
                }
            }

            return epochOffsets;
        }

        /// <summary>
        /// Convert 16k epoch to 8k epoch data.
        /// </summary>
        /// <param name="epoch">Source 16k Hz epoch data.</param>
        /// <returns>8k Hz epoch data.</returns>
        public static int[] EpochTo8k(int[] epoch)
        {
            if (epoch == null)
            {
                throw new ArgumentNullException("epoch");
            }

            int[] epoch8k = new int[epoch.Length];

            int remainder = 0;
            for (int i = 0; i < epoch.Length; i++)
            {
                int current = epoch[i];
                int sign = Math.Sign(current);

                current = (current * sign) + remainder;
                remainder = current - ((current / 2) * 2);
                current /= 2;
                current *= sign;

                epoch8k[i] = current;
            }

            return epoch8k;
        }

        #endregion
    }
}