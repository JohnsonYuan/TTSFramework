//----------------------------------------------------------------------------
// <copyright file="Riff.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements definition of RIFF header
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Win32.SafeHandles;

    /// <summary>
    /// Definition of RIFF.
    /// </summary>
    public class Riff
    {
        #region Const fields

        /// <summary>
        /// IdUndefined .
        /// </summary>
        public const int IdUndefined = -1;      // undefined

        /// <summary>
        /// IdRiff .
        /// </summary>
        public const int IdRiff = 0x46464952;   // RIFF

        /// <summary>
        /// IdWave .
        /// </summary>
        public const int IdWave = 0x45564157;   // WAVE

        /// <summary>
        /// IdFormat .
        /// </summary>
        public const int IdFormat = 0x20746d66; // fmt

        /// <summary>
        /// IdData .
        /// </summary>
        public const int IdData = 0x61746164;   // data

        /// <summary>
        /// IdText .
        /// </summary>
        public const int IdText = 0x74586554;   // tXeT

        #endregion

        #region Fields

        private int _size;
        private int _riffType = IdWave;
        private int _id = IdRiff;
        private Collection<RiffChunk> _chunks = new Collection<RiffChunk>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets Riff chunks in this Riff.
        /// </summary>
        public Collection<RiffChunk> Chunks
        {
            get { return _chunks; }
        }

        /// <summary>
        /// Gets or sets Identify for this Riff.
        /// </summary>
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Gets or sets Type of this Riff.
        /// </summary>
        public int RiffType
        {
            get { return _riffType; }
            set { _riffType = value; }
        }

        /// <summary>
        /// Gets or sets Data length/size of this Riff.
        /// </summary>
        public int Size
        {
            get
            {
                return _size;
            }

            set
            {
                if (value <= 0)
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
        /// Gets Total size for the riff data including head data.
        /// </summary>
        /// <value></value>
        public int TotalSize
        {
            get
            {
                int ret = 12;               // 4 bytes for rifftype

                foreach (RiffChunk chunk in Chunks)
                {
                    ret += chunk.TotalSize;
                }

                _size = ret - 8;
                return ret;
            }
        }

        #endregion

        #region Public static methods

        /// <summary>
        /// Load waveform data from file.
        /// </summary>
        /// <param name="filePath">Source file.</param>
        /// <returns>Riff loaded.</returns>
        public static Riff ReadWaveFile(string filePath)
        {
            using (FileStream fs =
                new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Riff riff = null;
                try
                {
                    riff = ReadWave(fs);
                }
                catch (InvalidDataException ide)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Fail to read waveform file [{0}] for invalid data.",
                        filePath);
                    throw new InvalidDataException(message, ide);
                }

                if (riff.GetChunk(Riff.IdFormat) == null)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "There is not format (fmt) chunk found in the waveform file.");
                    throw new InvalidDataException(message);
                }

                if (riff.GetChunk(Riff.IdData) == null)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "There is not data chunk found in the waveform file.");
                    throw new InvalidDataException(message);
                }

                return riff;
            }
        }

        /// <summary>
        /// Read waveform data from wave stream.
        /// </summary>
        /// <param name="stream">Wave stream.</param>
        /// <returns>Riff loaded.</returns>
        public static Riff ReadWave(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            Riff riff = new Riff();
            riff.LoadHead(br);

            while (br.BaseStream.Position != br.BaseStream.Length)
            {
                RiffChunk chunk = new RiffChunk();
                chunk.Load(br, Riff.IdUndefined);
                riff.Chunks.Add(chunk);

                if (br.BaseStream.Length - br.BaseStream.Position < 8)
                {
                    // Ignore chunk whose size is less than 8 bytes
                    break;
                }
            }

            return riff;
        }

        /// <summary>
        /// Save this Riff to waveform file.
        /// </summary>
        /// <param name="filePath">Target file.</param>
        /// <param name="riff">Riff data.</param>
        public static void SaveWaveFile(string filePath, Riff riff)
        {
            if (riff == null)
            {
                throw new ArgumentNullException("riff");
            }

            // Keep 4 bytes for rifftype, remove the other 8 bits riff head.
            riff.Size = riff.TotalSize - 8;

            FileStream filestream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            try
            {
                using (BinaryWriter bw = new BinaryWriter(filestream))
                {
                    filestream = null;
                    riff.SaveHead(bw);

                    if (riff.GetChunk(Riff.IdFormat) == null ||
                        riff.GetChunk(Riff.IdData) == null)
                    {
                        throw new InvalidDataException(
                            "Invalid chunks : Both Riff.IdFormat and Riff.IdData should be contained in chunks");
                    }

                    // Save IdFormat at first to keep consistence with original logical.
                    riff.GetChunk(Riff.IdFormat).Save(bw);

                    foreach (RiffChunk chunk in riff.Chunks)
                    {
                        if (chunk.Id != Riff.IdFormat)
                        {
                            riff.GetChunk(chunk.Id).Save(bw);
                        }
                    }
                }
            }
            finally
            {
                if (null != filestream)
                {
                    filestream.Dispose();
                }
            }
        }
        #endregion

        #region Public operations

        /// <summary>
        /// Find riff chunk for certain Id.
        /// </summary>
        /// <param name="id">Identify to find.</param>
        /// <returns>RiffChunk found.</returns>
        public RiffChunk GetChunk(int id)
        {
            foreach (RiffChunk chunk in Chunks)
            {
                if (chunk.Id == id)
                {
                    return chunk;
                }
            }

            return null;
        }

        #endregion

        #region IO Operations

        /// <summary>
        /// Load Riff data head from binary stream.
        /// </summary>
        /// <param name="br">Binart stream.</param>
        public void LoadHead(BinaryReader br)
        {
            if (br == null)
            {
                throw new ArgumentNullException("br");
            }

            try
            {
                Id = br.ReadInt32();
                if (Id != Riff.IdRiff)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Malformed data found while reading RIFF tag for RIFF chunk.");
                    throw new InvalidDataException(message);
                }

                Size = br.ReadInt32();
                RiffType = br.ReadInt32();
                if (RiffType != Riff.IdWave)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Malformed data found while reading WAVE tag for RIFF chunk.");
                    throw new InvalidDataException(message);
                }
            }
            catch (EndOfStreamException ese)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Malformed data found while reading header of RIFF chunk");
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
        /// Save this Riff to binary stream.
        /// </summary>
        /// <param name="bw">Binary writer to save.</param>
        public void SaveHead(BinaryWriter bw)
        {
            if (bw == null)
            {
                throw new ArgumentNullException("bw");
            }

            bw.Write(Id);
            bw.Write(_size);
            bw.Write(RiffType);
        }
        #endregion
    }
}