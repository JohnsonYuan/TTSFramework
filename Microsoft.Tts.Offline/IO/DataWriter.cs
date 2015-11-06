//----------------------------------------------------------------------------
// <copyright file="DataWriter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements binary data writer
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Extension to System.IO.Stream.
    /// </summary>
    public static class StreamExtension
    {
        /// <summary>
        /// Given stream instance, excerpt the number of bytes from current position.
        /// </summary>
        /// <param name="stream">Stream to except from.</param>
        /// <param name="size">The number of bytes to excerpt.</param>
        /// <returns>BinaryReader to the excerpted stream.</returns>
        [CLSCompliant(false)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public static BinaryReader Excerpt(this Stream stream, uint size)
        {
            Helper.ThrowIfNull(stream);
            Debug.Assert(stream.CanSeek && stream.CanRead,
                "Only seek-able and readable stream is supported here.");
            Debug.Assert(stream.Position >= size,
                Helper.NeutralFormat("There should has enough bytes to read from current position."));

            stream.Seek(-size, SeekOrigin.Current);
            byte[] buffer = new byte[size];
            int readCount = stream.Read(buffer, 0, buffer.Length);
            Debug.Assert(readCount == buffer.Length);
            MemoryStream excerpted = new MemoryStream(buffer);
            BinaryReader reader = new BinaryReader(excerpted);

            return reader;
        }
    }

    /// <summary>
    /// Binary data writer.
    /// </summary>
    public class DataWriter : BinaryWriter
    {
        /// <summary>
        /// The number used for padding.
        /// </summary>
        public const int Padding = 0;

        #region Construction
        /// <summary>
        /// Initializes a new instance of the DataWriter class.
        /// </summary>
        /// <param name="output">Output stream.</param>
        public DataWriter(Stream output) :
            base(output)
        {
        }
        #endregion

        #region Operations

        /// <summary>
        /// Write out signed byte.
        /// </summary>
        /// <param name="value">Value to write out.</param>
        /// <returns>Number of byte written out.</returns>
        [CLSCompliant(false)]
        public new uint Write(sbyte value)
        {
            base.Write(value);
            return sizeof(sbyte);
        }

        /// <summary>
        /// Write out unsigned byte.
        /// </summary>
        /// <param name="value">Value to write.</param>
        /// <returns>Number of byte written out.</returns>
        [CLSCompliant(false)]
        public new uint Write(byte value)
        {
            base.Write(value);
            return sizeof(byte);
        }

        /// <summary>
        /// Write out unsigned short.
        /// </summary>
        /// <param name="value">Value to write.</param>
        /// <returns>Number of byte written out.</returns>
        [CLSCompliant(false)]
        public new uint Write(ushort value)
        {
            base.Write(value);
            return sizeof(ushort);
        }

        /// <summary>
        /// Write out signed short.
        /// </summary>
        /// <param name="value">Value to write.</param>
        /// <returns>Number of byte written out.</returns>
        [CLSCompliant(false)]
        public new uint Write(short value)
        {
            base.Write(value);
            return sizeof(short);
        }

        /// <summary>
        /// Write out unsigned int.
        /// </summary>
        /// <param name="value">Value to write.</param>
        /// <returns>Number of byte written out.</returns>
        [CLSCompliant(false)]
        public new uint Write(uint value)
        {
            base.Write(value);
            return sizeof(uint);
        }

        /// <summary>
        /// Write out signed int.
        /// </summary>
        /// <param name="value">Value to write.</param>
        /// <returns>Number of byte written out.</returns>
        [CLSCompliant(false)]
        public new uint Write(int value)
        {
            base.Write(value);
            return sizeof(int);
        }

        /// <summary>
        /// Write out float.
        /// </summary>
        /// <param name="value">Value to write.</param>
        /// <returns>Number of byte written out.</returns>
        [CLSCompliant(false)]
        public new uint Write(float value)
        {
            Debug.Assert(!float.IsNaN(value) &&
                !float.IsNegativeInfinity(value) &&
                !float.IsPositiveInfinity(value) &&
                !float.IsInfinity(value));

            base.Write(value);
            return sizeof(float);
        }

        /// <summary>
        /// Write out array of bytes.
        /// </summary>
        /// <param name="value">Value to write.</param>
        /// <returns>Number of byte written out.</returns>
        [CLSCompliant(false)]
        public new uint Write(byte[] value)
        {
            base.Write(value);
            return (uint)(sizeof(byte) * value.Length);
        }

        #endregion
    }

    /// <summary>
    /// Provides auto reset Position of stream back to initial one.
    /// </summary>
    public class PositionRecover : IDisposable
    {
        #region Private fields

        private Stream _stream;
        private long _recoveredPosition;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the PositionRecover class.
        /// </summary>
        /// <param name="writer">Binary writer to track for recover to current stream position.</param>
        /// <param name="position">New position after hooked.</param>
        public PositionRecover(BinaryWriter writer, long position) :
            this(writer.BaseStream, position)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PositionRecover class.
        /// </summary>
        /// <param name="writer">Binary writer to track for recover to current stream position.</param>
        /// <param name="offset">New position after hooked.</param>
        /// <param name="origin">Relative offset.</param>
        public PositionRecover(BinaryWriter writer, long offset, SeekOrigin origin) :
            this(writer.BaseStream, offset, origin)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PositionRecover class.
        /// </summary>
        /// <param name="stream">Stream to track for recover to current stream position.</param>
        /// <param name="offset">New position after hooked.</param>
        /// <param name="origin">Relative offset.</param>
        public PositionRecover(Stream stream, long offset, SeekOrigin origin)
        {
            Helper.ThrowIfNull(stream);
            _stream = stream;
            _recoveredPosition = stream.Position;

            _stream.Seek(offset, origin);
        }

        /// <summary>
        /// Initializes a new instance of the PositionRecover class.
        /// </summary>
        /// <param name="stream">Stream to track for recover to current stream position.</param>
        /// <param name="position">New position after hooked.</param>
        public PositionRecover(Stream stream, long position) :
            this(stream, position, SeekOrigin.Begin)
        {
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose and recover back to original position while hooked.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes this object.
        /// </summary>
        /// <param name="disposing">Flag indicating whether recover position.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_stream != null)
                {
                    _stream.Seek(_recoveredPosition, SeekOrigin.Begin);
                    _stream = null;
                }
            }
        }

        #endregion
    }
}