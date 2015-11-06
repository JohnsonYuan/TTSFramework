//----------------------------------------------------------------------------
// <copyright file="WaveFormat.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements wave format definition
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>Wave format type.</summary>
    public enum WaveFormatTag
    {
        /// <summary>
        /// Undefined.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// WAVE_FORMAT_PCM 
        /// Pcm assumes that the BlockAlign field contains exactly one set of 
        /// Samples (one block). Also, each sample must be byte-aligned within the block.
        /// </summary>
        Pcm = 1,

        /// <summary>
        /// WAVE_FORMAT_ALAW .
        /// </summary>
        Alaw = 6,

        /// <summary>
        /// WAVE_FORMAT_MULAW .
        /// </summary>
        Mulaw = 7
    }

    /// <summary>
    /// Supported types of channel number of waveform file.
    /// </summary>
    public enum WaveChannel
    {
        /// <summary>
        /// Undefined.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Mono channel.
        /// </summary>
        Mono = 1,

        /// <summary>
        /// Stereo channels.
        /// </summary>
        Stereo = 2,
    }

    /// <summary>
    /// Supported samples per second of waveform files.
    /// </summary>
    public enum WaveSamplesPerSecond
    {
        /// <summary>
        /// Undefined.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// 8k Hz for narrow band telephone scenario.
        /// </summary>
        Telephone = 8000,

        /// <summary>
        /// 16k Hz for desktop or wide band telephone scenario.
        /// </summary>
        Desktop = 16000,

        /// <summary>
        /// 22k Hz for FM radio audio quality.
        /// </summary>
        FmQuality = 22050,

        /// <summary>
        /// 44.1k Hz for CD audio quality, which is default delivered from recording studio.
        /// </summary>
        Recording = 44100
    }

    /// <summary>
    /// Supported bits per sample of waveform files.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
        "CA1027:MarkEnumsWithFlags", Justification = "Ignore.")]
    public enum WaveBitsPerSample
    {
        /// <summary>
        /// Undefined.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// 8 bits, usual for telephone scenario.
        /// </summary>
        Eight = 8,

        /// <summary>
        /// 16 bits, usual for desktop scenario.
        /// </summary>
        Sixteen = 16
    }

    /// <summary>
    /// Waveform format definition
    /// For related information see "Details about WAVEFORMATEX Fields" at 
    /// Http://www.microsoft.com/whdc/device/audio/multichaud.mspx.
    /// </summary>
    public struct WaveFormat : IEquatable<WaveFormat>
    {
        #region Fields

        private WaveFormatTag _formatTag;
        private short _channels;
        private int _samplesPerSecond;
        private int _averageBytesPerSecond;
        private short _blockAlign;
        private short _bitsPerSample;
        private short _byteExtSize;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Format tag/type.
        /// </summary>
        public WaveFormatTag FormatTag
        {
            get
            {
                return _formatTag;
            }

            set
            {
                if (value != WaveFormatTag.Undefined &&
                    value != WaveFormatTag.Pcm &&
                    value != WaveFormatTag.Alaw &&
                    value != WaveFormatTag.Mulaw)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Wave format type [{0}] is not supported.",
                        value);
                    throw new NotSupportedException(message);
                }

                _formatTag = value;
            }
        }

        /// <summary>
        /// Gets or sets Channel number the number of interleaved samples per block
        /// This is the number of individual channels in the stream.
        /// </summary>
        public short Channels
        {
            get
            {
                return _channels;
            }

            set
            {
                if (value <= 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid channel number [{0}], which should be positive integer.",
                        value);

                    throw new ArgumentException(message);
                }

                _channels = value;
            }
        }

        /// <summary>
        /// Gets or sets The intended sample rate for the stream.
        /// The number of blocks that should be processed in exactly one second.
        /// </summary>
        public int SamplesPerSecond
        {
            get
            {
                return _samplesPerSecond;
            }

            set
            {
                if (value <= 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid samples per second [{0}], which should be positive integer.",
                        value);
                    throw new ArgumentException(message);
                }

                _samplesPerSecond = value;
            }
        }

        /// <summary>
        /// Gets or sets Average byte number per second, used for buffer size estimation, 
        /// So this number is calculated on gross block size.
        /// This value alway is the product of BlockAlign and SamplesPerSecond.
        /// </summary>
        public int AverageBytesPerSecond
        {
            get
            {
                return _averageBytesPerSecond;
            }

            set
            {
                if (value <= 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid average bytes per second [{0}], which should be positive integer.",
                        value);
                    throw new ArgumentException(message);
                }

                _averageBytesPerSecond = value;
            }
        }

        /// <summary>
        /// Gets or sets Data block align.
        /// </summary>
        public short BlockAlign
        {
            get
            {
                return _blockAlign;
            }

            set
            {
                if (value <= 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid block align [{0}], which should be positive integer.",
                        value);
                    throw new ArgumentException(message);
                }

                _blockAlign = value;
            }
        }

        /// <summary>
        /// Gets or sets Bits per sample.
        /// </summary>
        public short BitsPerSample
        {
            get
            {
                return _bitsPerSample;
            }

            set
            {
                if (value <= 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid bits per sample [{0}], which should be positive integer.",
                        value);
                    throw new ArgumentException(message);
                }

                if (value % 8 != 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid bits per sample [{0}], which should be an integer multiple of 8.",
                        value);
                    throw new ArgumentException(message);
                }

                _bitsPerSample = value;
            }
        }

        /// <summary>
        /// Gets or sets Extension size of this format.
        /// </summary>
        public short ExtSize
        {
            get
            {
                return _byteExtSize;
            }

            set
            {
                if (value != 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "The length [{0}] of Wave format extention data is not supported.",
                        value);
                    throw new NotSupportedException(message);
                }

                _byteExtSize = value;
            }
        }
        #endregion

        #region Public static methods

        /// <summary>
        /// Validate the format data.
        /// </summary>
        /// <param name="format">Waveform format.</param>
        public static void Validate(WaveFormat format)
        {
            try
            {
                format.Load(format.ToBytes());
            }
            catch (Exception e)
            {
                if (!FilterFormatException(e))
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Operator ==.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool operator ==(WaveFormat left, WaveFormat right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Operator !=.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if not equal, otherwise false.</returns>
        public static bool operator !=(WaveFormat left, WaveFormat right)
        {
            return !left.Equals(right);
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Load format from data.
        /// </summary>
        /// <param name="chunk">Data to load from.</param>
        public void Load(byte[] chunk)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException("chunk");
            }

            try
            {
                MemoryStream ms = new MemoryStream(chunk);
                try
                {
                    using (BinaryReader br = new BinaryReader(ms))
                    {
                        ms = null;
                        FormatTag = (WaveFormatTag)br.ReadInt16();
                        Channels = br.ReadInt16();
                        SamplesPerSecond = br.ReadInt32();
                        AverageBytesPerSecond = br.ReadInt32();
                        BlockAlign = br.ReadInt16();
                        BitsPerSample = br.ReadInt16();

                        if (chunk.Length >= 18)
                        {
                            ExtSize = br.ReadInt16();
                        }

                        Validate();
                    }
                }
                finally
                {
                    if (null != ms)
                    {
                        ms.Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                if (!FilterFormatException(e))
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Save this format data into byte array.
        /// </summary>
        /// <returns>Byte array.</returns>
        public byte[] ToBytes()
        {
            if (ExtSize != 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                        "Wave format with extention data (length {0})is not supported.",
                        ExtSize);
                throw new NotSupportedException(message);
            }

            byte[] chunk = new byte[16];
            MemoryStream ms = new MemoryStream(chunk);
            try
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    ms = null;
                    bw.Write((short)FormatTag);
                    bw.Write(Channels);
                    bw.Write(SamplesPerSecond);
                    bw.Write(AverageBytesPerSecond);
                    bw.Write(BlockAlign);
                    bw.Write(BitsPerSample);
                }
            }
            finally
            {
                if (null != ms)
                {
                    ms.Dispose();
                }
            }

            return chunk;
        }

        /// <summary>
        /// Get hash code.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            // use default implementation
            return base.GetHashCode();
        }

        /// <summary>
        /// Equal this with other instance.
        /// </summary>
        /// <param name="obj">Other instance.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is WaveFormat))
            {
                return false;
            }

            return Equals((WaveFormat)obj);
        }

        #endregion

        #region IEquatable<WaveFormat> Members

        /// <summary>
        /// Equal this with other instance.
        /// </summary>
        /// <param name="other">Other instance.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public bool Equals(WaveFormat other)
        {
            return FormatTag == other.FormatTag &&
                Channels == other.Channels &&
                SamplesPerSecond == other.SamplesPerSecond &&
                AverageBytesPerSecond == other.AverageBytesPerSecond &&
                BlockAlign == other.BlockAlign &&
                BitsPerSample == other.BitsPerSample;
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Filter format data exception.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <returns>True if filtered, otherwise false.</returns>
        private static bool FilterFormatException(Exception exception)
        {
            if (exception is EndOfStreamException)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Malformed data found, for there is no enough data for Wave Format.");
                throw new InvalidDataException(message, exception);
            }
            else if (exception is ArgumentException
                || exception is NotSupportedException)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Malformed data found, for there is invalid data for Wave Format.");
                throw new InvalidDataException(message, exception);
            }

            return false;
        }

        /// <summary>
        /// Validate data consistence between fields.
        /// </summary>
        private void Validate()
        {
            if (BlockAlign % Channels != 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Malformed data found between BlockAlign [{0}] * Channels [{1}], for BlockAlign must be an integer multiple of Channels.",
                    BlockAlign, Channels);
                throw new InvalidDataException(message);
            }

            if (BlockAlign * SamplesPerSecond != AverageBytesPerSecond)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Malformed data found for BlockAlign [{0}] * SamplesPerSecond [{1}] != AverageBytesPerSecond [{2}].",
                    BlockAlign, SamplesPerSecond, AverageBytesPerSecond);
                throw new InvalidDataException(message);
            }
        }

        #endregion
    }
}