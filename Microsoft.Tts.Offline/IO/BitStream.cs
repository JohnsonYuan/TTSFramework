//----------------------------------------------------------------------------
// <copyright file="BitStream.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements class for bit stream operations.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Bit stream operations.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
        "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "Ignore.")]
    public class BitStream
    {
        #region Fields

        private const int MaxBitWidth = 32;

        /// <summary>
        /// Bit mask for shifting 32-bit data.
        /// </summary>
        private static readonly uint[] _bitMasks = new uint[]
        {
            0x00000000, 0x00000001, 0x00000003, 0x00000007,
            0x0000000F, 0x0000001F, 0x0000003F, 0x0000007F,
            0x000000FF, 0x000001FF, 0x000003FF, 0x000007FF,
            0x00000FFF, 0x00001FFF, 0x00003FFF, 0x00007FFF,
            0x0000FFFF, 0x0001FFFF, 0x0003FFFF, 0x0007FFFF,
            0x000FFFFF, 0x001FFFFF, 0x003FFFFF, 0x007FFFFF,
            0x00FFFFFF, 0x01FFFFFF, 0x03FFFFFF, 0x07FFFFFF,
            0x0FFFFFFF, 0x1FFFFFFF, 0x3FFFFFFF, 0x7FFFFFFF,
            0xFFFFFFFF
        };

        /// <summary>
        /// Store bitstream data for writting or reading.
        /// </summary>
        private byte[] _buffer;

        /// <summary>
        /// Current offset in _buffer when writting or reading.
        /// </summary>
        private int _index;

        /// <summary>
        /// 1. For writting data, firstly bits will be put into _cache till
        ///    the _cache is fullfilled, at that time, current 32-bit data in
        ///    _cache will be written to _buffer.
        /// 2. For reading data, firstly bits will be get from _cache till
        ///    the _cache is empty, at that time, next 32-bit data from _buffer
        ///    will be read into _cache.
        /// </summary>
        private uint _cache;

        /// <summary>
        /// 1. For writting data, _leftBits means how many bits can be written into _cache.
        /// 2. For reading data, _leftBits means how many bits can be read from _cache.
        /// </summary>
        private int _leftBits;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="BitStream"/> class.
        /// </summary>
        /// <param name="size">Bit stream buffer size.</param>
        public BitStream(long size)
        {
            if (size <= 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Buffer size must be greater than zero");
                throw new ArgumentException(message);
            }

            if (size % 4 != 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Buffer size must be the multiple of 4");
                throw new ArgumentException(message);
            }

            _buffer = new byte[size];
            _leftBits = MaxBitWidth;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitStream"/> class.
        /// </summary>
        /// <param name="buffer">BitStram buffer.</param>
        public BitStream(byte[] buffer)
        {
            if (buffer == null)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Empty buffer");
                throw new ArgumentNullException(message);
            }

            if (buffer.Length % 4 != 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Buffer size must be the multiple of 4");
                throw new ArgumentException(message);
            }

            _buffer = buffer;
            _index = 4;
            _cache = BitConverter.ToUInt32(_buffer, 0);
            _leftBits = MaxBitWidth;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Buffer size.
        /// </summary>
        public int Size
        {
            get { return _buffer.Length; }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Write bits into buffer: firstly bits will be put into _cache till
        /// The _cache is fullfilled, at that time, current 32-bit data in _cache
        /// Will be written to _buffer.
        /// </summary>
        /// <param name="bitValue">Bit value.</param>
        /// <param name="bitWidth">Bit width.</param>
        public void WriteBits(int bitValue, int bitWidth)
        {
            Debug.Assert(bitWidth > 0);
            Debug.Assert(bitWidth <= MaxBitWidth);

            if (bitWidth <= 0 || bitWidth > MaxBitWidth)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid bitWidth {0}, bitWidth should be in the range [0, 32]",
                    bitWidth);
                throw new InvalidDataException(message);
            }

            Debug.Assert(_leftBits > 0);
            Debug.Assert(_leftBits <= MaxBitWidth);

            uint val = (uint)bitValue;

            if (bitWidth <= _leftBits)
            {
                // There are enough room in the _cache for new input bit data.
                _cache >>= bitWidth;
                _cache |= val << (MaxBitWidth - bitWidth);
                _leftBits -= bitWidth;

                if (_leftBits == 0)
                {
                    // _cache is fullfilled, write data into _buffer.
                    WriteToBuffer(BitConverter.GetBytes(_cache), sizeof(uint));

                    // Reset _cache and _leftBits.
                    _cache = 0;
                    _leftBits = MaxBitWidth;
                }
            }
            else
            {
                // No enough room in _cache for new input bit data.

                // Firstly, fullfill _cache with input data.
                _cache >>= _leftBits;
                _cache |= val << (MaxBitWidth - _leftBits);

                // Remainder bits of input data.
                bitWidth -= _leftBits;
                val >>= _leftBits;

                // _cache is fullfilled, write data into _buffer.
                WriteToBuffer(BitConverter.GetBytes(_cache), sizeof(uint));

                // Reset _cache and _leftBits.
                _cache = 0;
                _leftBits = MaxBitWidth;

                // Secondly, write remainder bits of input data into _cache.
                _cache |= val << (MaxBitWidth - bitWidth);
                _leftBits -= bitWidth;
            }
        }

        /// <summary>
        /// Write 32-bit data.
        /// </summary>
        /// <param name="bitValue">Bit value.</param>
        public void WriteInt32(int bitValue)
        {
            WriteBits(bitValue, MaxBitWidth);
        }

        /// <summary>
        /// Write 16-bit data.
        /// </summary>
        /// <param name="bitValue">Bit value.</param>
        public void WriteInt16(int bitValue)
        {
            WriteBits(bitValue, 16);
        }

        /// <summary>
        /// Read bits from buffer: firstly bits will be get from _cache till
        /// The _cache is empty, at that time, next 32-bit data from _buffer
        /// Will be read into _cache.
        /// </summary>
        /// <param name="bitWidth">Bit width.</param>
        /// <returns>Read bit value.</returns>
        public int ReadBits(int bitWidth)
        {
            Debug.Assert(bitWidth > 0);
            Debug.Assert(bitWidth <= MaxBitWidth);

            if (bitWidth <= 0 || bitWidth > MaxBitWidth)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid bitWidth {0}, bitWidth should be in the range [0, 32]",
                    bitWidth);
                throw new InvalidDataException(message);
            }

            uint bitValue = 0;

            if (bitWidth <= _leftBits)
            {
                // There are enough bits in the _cache for reading.
                bitValue = _cache & _bitMasks[bitWidth];
                _cache >>= bitWidth;
                _leftBits -= bitWidth;

                if (_leftBits == 0)
                {
                    // _cache is empty, read next 32-bit data from _buffer.
                    _cache = ReadUInt32FromBuffer();
                    _leftBits = MaxBitWidth;
                }
            }
            else
            {
                // No enough bits in _cache.

                // Firstly, read the valid bits from _cache.
                bitValue = _cache;

                // Read next 32-bit data from _buffer to _cache.
                _cache = ReadUInt32FromBuffer();

                // At this point, the valid bit width in bitValue is _leftBits, and
                // remainder bit width of bitValue is (bitWidth - _leftBits), read them
                // from re-filled _cache.
                bitWidth -= _leftBits;
                bitValue |= (_cache & _bitMasks[bitWidth]) << _leftBits;
                _cache >>= bitWidth;
                _leftBits = MaxBitWidth - bitWidth;
            }

            return (int)bitValue;
        }

        /// <summary>
        /// Read 32-bit data.
        /// </summary>
        /// <returns>32-bit value read.</returns>
        public int ReadInt32()
        {
            return ReadBits(MaxBitWidth);
        }

        /// <summary>
        /// Read 16-bit data.
        /// </summary>
        /// <returns>16-bit result.</returns>
        public int ReadInt16()
        {
            return ReadBits(16);
        }

        /// <summary>
        /// Return bits buffer.
        /// </summary>
        /// <returns>Bits buffer result.</returns>
        public byte[] ToBytes()
        {
            // Flush bits in _cache.
            if (_leftBits < MaxBitWidth)
            {
                _cache >>= _leftBits;
                WriteToBuffer(BitConverter.GetBytes(_cache), sizeof(uint));
                _cache = 0;
                _leftBits = MaxBitWidth;
            }

            return _buffer;
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Write data into internal buffer.
        /// </summary>
        /// <param name="data">Input data.</param>
        /// <param name="size">Data size.</param>
        private void WriteToBuffer(byte[] data, int size)
        {
            Debug.Assert(_index + size <= _buffer.Length);
            if (_index + size > _buffer.Length)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The value of _index is {0}, it is not less than {1}",
                    _index, _buffer.Length - size);
                throw new ArgumentOutOfRangeException(message);
            }

            Buffer.BlockCopy(data, 0, _buffer, _index, size);
            _index += size;
        }

        /// <summary>
        /// Read 32-bit data from _buffer.
        /// </summary>
        /// <returns>32-bit data.</returns>
        private uint ReadUInt32FromBuffer()
        {
            uint data = 0;
            if (_index + 4 <= _buffer.Length)
            {
                data = BitConverter.ToUInt32(_buffer, _index);
                _index += 4;
            }

            return data;
        }

        #endregion
    }
}