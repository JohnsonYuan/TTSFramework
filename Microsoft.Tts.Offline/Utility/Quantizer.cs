//----------------------------------------------------------------------------
// <copyright file="Quantizer.cs" company="Microsoft">
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
    using System.Diagnostics;

    /// <summary>
    /// Quantizer interface.
    /// </summary>
    public interface IQuantizer
    {
        /// <summary>
        /// Gets the target bits number to quantize to.
        /// </summary>
        int TargetSize { get; }

        /// <summary>
        /// Gets quantization error for the quantizer.
        /// </summary>
        float QuantizationError { get; }

        /// <summary>
        /// Gets the scale factor for dequantization.
        /// </summary>
        float DequantizeScaleFactor { get; }

        /// <summary>
        /// Gets the offset for dequantization.
        /// </summary>
        float DequantizeOffset { get; }

        /// <summary>
        /// Quantizes float to byte array.
        /// </summary>
        /// <param name="values">Float value to be quantized.</param>
        /// <returns>Quantized byte array.</returns>
        byte[] Quantize(float[] values);
    }

    /// <summary>
    /// Converts data length between different types.
    /// </summary>
    public static class DataLengthConverter
    {
        private static readonly int BitsPerByte = 8;

        /// <summary>
        /// Converts bit length in bytes.
        /// </summary>
        /// <param name="bits">Length of bits.</param>
        /// <param name="alignToBytes">Byte alignment for the conversion.</param>
        /// <returns>Bytes required to store the bits.</returns>
        public static long ConvertBitLengthToByte(long bits, int alignToBytes)
        {
            if (bits <= 0)
            {
                throw new ArgumentException("bits must be larger than 0", "bits");
            }

            if (alignToBytes < 1)
            {
                throw new ArgumentException("alignToBytes must be larger than 1", "alignToBytes");
            }

            int bitsPerAlignBlock = alignToBytes * BitsPerByte;
            return ((bits + bitsPerAlignBlock - 1) / bitsPerAlignBlock) * alignToBytes;
        }
    }

    /// <summary>
    /// Linear quantizer which quantizer a given range linearly.
    /// The range is defined by [floor, ceiling].
    /// Values below floor are set to floor and values above ceiling are set to ceiling.
    /// The formular for dequantize:
    ///     (quantized value) * DequantizeScaleFactor + DequantizeOffset.
    /// </summary>
    public class LinearQuantizer : IQuantizer
    {
        private double _delta;
        private int _maxQuantizedValue;

        /// <summary>
        /// Initializes a new instance of the LinearQuantizer class.
        /// </summary>
        /// <param name="targetSize">Target bits number to quantize to.
        /// Valid ones are byte/sbyte/short/ushort.</param>
        /// <param name="floor">The floor of the quantization range.</param>
        /// <param name="ceil">The ceiling of the quantization range.</param>
        public LinearQuantizer(int targetSize, float floor, float ceil)
        {
            if (floor >= ceil)
            {
                throw new ArgumentException("Floor should be less than Ceil");
            }

            if (targetSize < 1 || targetSize > 32)
            {
                throw new ArgumentException("Target size should be power of 2 within [1,32]");
            }

            _delta = ((double)ceil - floor) / (1 << targetSize);
            _maxQuantizedValue = (1 << targetSize) - 1;

            Floor = floor;
            Ceil = ceil;
            TargetSize = targetSize;
        }

        /// <summary>
        /// Gets the floor of the quantization range.
        /// </summary>
        public float Floor { get; private set; }

        /// <summary>
        /// Gets the ceiling of the quantization range.
        /// </summary>
        public float Ceil { get; private set; }

        /// <summary>
        /// Gets the target bits number to quantize to.
        /// </summary>
        public int TargetSize { get; private set; }

        /// <summary>
        /// Gets the scale factor for dequantization.
        /// </summary>
        public float DequantizeScaleFactor
        {
            get
            {
                return (Ceil - Floor) / (1 << TargetSize);
            }
        }

        /// <summary>
        /// Gets quantization error for the quantizer.
        /// </summary>
        public float QuantizationError
        {
            get
            {
                return (float)_delta / 2;
            }
        }

        /// <summary>
        /// Gets the offset for dequantization.
        /// </summary>
        public float DequantizeOffset
        {
            get
            {
                return (float)(Floor + (_delta / 2));
            }
        }

        /// <summary>
        /// Quantize float to byte array.
        /// </summary>
        /// <param name="values">Float values to be quantized.</param>
        /// <returns>Quantized byte array.</returns>
        public byte[] Quantize(float[] values)
        {
            int retAlignByte = 1;
            byte[] ret = new byte[DataLengthConverter.ConvertBitLengthToByte((long)values.Length * TargetSize, retAlignByte)];

            int streamBufferAlignByte = 4;
            BitStream bitStream = new BitStream(DataLengthConverter.ConvertBitLengthToByte((long)values.Length * TargetSize, streamBufferAlignByte));

            for (int i = 0; i < values.Length; i++)
            {
                float value = values[i];
                float clip = (float)Math.Max(Math.Min(value, Ceil - (_delta / 2)), Floor);
                double quantized = Math.Floor((clip - Floor) / _delta);

                Debug.Assert(
                    quantized >= 0 && quantized <= _maxQuantizedValue,
                    Helper.NeutralFormat("Quantized value {0} out of range: [0, {1}]", quantized, _maxQuantizedValue));

                int quantizedInt = Convert.ToInt32(quantized);

                bitStream.WriteBits(quantizedInt, TargetSize);
            }

            byte[] writenBytes = bitStream.ToBytes();

            int unusedBytes = writenBytes.Length - ret.Length;

            Debug.Assert(
                unusedBytes >= 0 && unusedBytes <= 3,
                "Unused bytes should be within [0, 3]");

            Buffer.BlockCopy(writenBytes, 0, ret, 0, ret.Length);

            for (int i = ret.Length; i < writenBytes.Length; i++)
            {
                Debug.Assert(writenBytes[i] == 0, "Unused bytes should be 0");
            }

            return ret;
        }
    }

    /// <summary>
    /// Null quantizer which quantizer float to its binary representation.
    /// </summary>
    public class NullQuantizer : IQuantizer
    {
        /// <summary>
        /// Gets the target byte number to quantize to.
        /// </summary>
        public int TargetSize
        {
            get
            {
                return sizeof(float) * 8;
            }
        }

        /// <summary>
        /// Gets the scale factor to be multiplied when dequantize.
        /// </summary>
        public float DequantizeScaleFactor
        {
            get
            {
                return 1.0f;
            }
        }

        /// <summary>
        /// Gets the offset to be added when dequantize.
        /// </summary>
        public float DequantizeOffset
        {
            get
            {
                return 0.0f;
            }
        }

        /// <summary>
        /// Gets quantization error for the quantizer.
        /// </summary>
        public float QuantizationError
        {
            get { return 0.0f; }
        }

        /// <summary>
        /// Quantize float to byte array.
        /// </summary>
        /// <param name="values">Float values to be quantized.</param>
        /// <returns>Quantized byte array.</returns>
        public byte[] Quantize(float[] values)
        {
            byte[] ret = new byte[sizeof(float) * values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                byte[] floatBytes = BitConverter.GetBytes(values[i]);
                Buffer.BlockCopy(floatBytes, 0, ret, i * sizeof(float), floatBytes.Length);
            }

            return ret;
        }
    }
}