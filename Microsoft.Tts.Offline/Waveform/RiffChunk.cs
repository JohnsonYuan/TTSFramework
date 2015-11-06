//----------------------------------------------------------------------------
// <copyright file="RiffChunk.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements RIFF chunk
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Riff chunk.
    /// </summary>
    public class RiffChunk
    {
        #region Fields

        private int _id;
        private int _size;
        private byte[] _data;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Identify.
        /// </summary>
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Gets or sets Data size.
        /// </summary>
        public int Size
        {
            get
            {
                return _size;
            }

            set
            {
                if (value < 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid chunk size [{0}], which should be positive integer.",
                        value);
                    throw new ArgumentException(message);
                }

                _size = value;
            }
        }

        /// <summary>
        /// Gets Total size for the chunk including head data.
        /// </summary>
        /// <value></value>
        public int TotalSize
        {
            get { return _data.Length + 8; }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Data in byte array.
        /// </summary>
        /// <returns>Chunk data.</returns>
        public byte[] GetData()
        {
            return _data;
        }

        /// <summary>
        /// Set new data.
        /// </summary>
        /// <param name="value">New value.</param>
        /// <returns>Old value.</returns>
        public byte[] SetData(byte[] value)
        {
            byte[] old = _data;
            if (value != null)
            {
                _data = value;
                _size = value.Length;
            }
            else
            {
                throw new ArgumentNullException("value");
            }

            return old;
        }

        /// <summary>
        /// Load data from binary stream for certain type.
        /// </summary>
        /// <param name="br">Binary stream.</param>
        /// <param name="chunkIdType">Id type.</param>
        public void Load(BinaryReader br, int chunkIdType)
        {
            try
            {
                // move following block into try {} to prevent magellen injection fault
                // which will be found by peverify.exe, see PS#9951
                // [IL]: Error: [Microsoft.Tts.Offline.dll : Microsoft.Tts.Offline.Waveform.RiffChunk::Load]
                //              [offset 0x00000166] Branch out of try block.
                if (br == null)
                {
                    throw new ArgumentNullException("br");
                }

                if (br.BaseStream == null)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "br.BaseStream should not be null");
                    throw new ArgumentNullException("br", message);
                }

                do
                {
                    Id = br.ReadInt32();
                    Size = br.ReadInt32();
                    if (Size < 0
                        || br.BaseStream.Position + Size > br.BaseStream.Length)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "Malformed data found while size [{0}] of RIFF chunk.",
                            Size);
                        throw new InvalidDataException(message);
                    }

                    try
                    {
                        _data = br.ReadBytes(Size);
                        if (_data.Length != Size)
                        {
                            string message = string.Format(CultureInfo.InvariantCulture,
                                "Malformed data found, for there is no enough data for RIFF chunk.");
                            throw new InvalidDataException(message);
                        }
                    }
                    catch (OutOfMemoryException ome)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "Not enough memory to load RIFF chunk with size [{0}] .",
                            Size);
                        throw new InvalidDataException(message, ome);
                    }
                }
                while (chunkIdType != Riff.IdUndefined && Id != chunkIdType);
            }
            catch (EndOfStreamException ese)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Malformed data found while reading RIFF chunk.");
                throw new InvalidDataException(message, ese);
            }
            catch (ArgumentException ae)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Malformed data found while reading header of RIFF chunk.");
                throw new InvalidDataException(message, ae);
            }
        }

        /// <summary>
        /// Append new data to original data.
        /// </summary>
        /// <param name="newData">New data array.</param>
        public void Append(byte[] newData)
        {
            if (newData == null)
            {
                return;
            }

            if (_data == null)
            {
                _data = new byte[newData.Length];
                Buffer.BlockCopy(newData, 0, _data, 0, newData.Length);
            }
            else
            {
                byte[] oldData = _data;
                _data = new byte[oldData.Length + newData.Length];
                Buffer.BlockCopy(oldData, 0, _data, 0, oldData.Length);
                Buffer.BlockCopy(newData, 0, _data, oldData.Length, newData.Length);
            }

            Size += newData.Length;
        }

        /// <summary>
        /// Save this to bindary stream.
        /// </summary>
        /// <param name="bw">Binary stream to save into.</param>
        public void Save(BinaryWriter bw)
        {
            if (bw == null)
            {
                throw new ArgumentNullException("bw");
            }

            bw.Write(Id);
            bw.Write(_data.Length);
            bw.Write(_data);
        }

        #endregion
    }
}