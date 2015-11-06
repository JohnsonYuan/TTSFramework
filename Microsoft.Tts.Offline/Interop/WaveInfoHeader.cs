//----------------------------------------------------------------------------
// <copyright file="WaveInfoHeader.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements WaveInfoHeader
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Interop
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.Tts.Offline.Config;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// Class for WIH (Wave information Header) file.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class WaveInfoHeader
    {
        #region Fields

        /// <summary>
        /// The GUID of WIH file.
        /// </summary>
        public static readonly Guid WihFormatTag = VoiceFontTag.FmtIdWaveInfoHeader;

        /// <summary>
        /// Wave info header file tag: "WIH ".
        /// </summary>
        public const uint WihFileTag = 0x20484957;

        /// <summary>
        /// The version of this file.
        /// </summary>
        public const uint WihVersion = (uint)FormatVersion.Tts30;

        /// <summary>
        /// The build number of this file.
        /// </summary>
        public const uint WihBuild = 0;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the wave compress catalog.
        /// </summary>
        public WaveCompressCatalog Compression { get; set; }

        /// <summary>
        /// Gets or sets the sample count per second of wave inventory.
        /// </summary>
        public uint SamplesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the byte count per sample of wave inventory.
        /// </summary>
        public ushort BytesPerSample { get; set; }

        /// <summary>
        /// Gets or sets the WaveFormatTag of wave inventory.
        /// </summary>
        public WaveFormatTag FormatCategory { get; set; }

        /// <summary>
        /// Gets or sets the cross correlation margin length, in millisecond.
        /// </summary>
        public ushort CrossCorrelationMarginLength { get; set; }

        /// <summary>
        /// Gets a value indicating whether need obfuscation.
        /// </summary>
        public bool NeedObfuscation
        {
            get
            {
                return Compression != WaveCompressCatalog.Dmo;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Saves the wave information header into given file.
        /// </summary>
        /// <param name="file">
        /// The file name of target file.
        /// </param>
        public void Save(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("file");
            }

            // Validates this object.
            Validate();

            Helper.EnsureFolderExistForFile(file);
            FileStream fs = new FileStream(file, FileMode.Create);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    fs = null;
                    VoiceFontHeader voiceFontHeader = new VoiceFontHeader
                    {
                        FileTag = WihFileTag,
                        FormatTag = WihFormatTag,
                        DataSize = (ulong)GetFileDataSize(),
                        Version = WihVersion,
                        Build = WihBuild
                    };

                    voiceFontHeader.Save(writer);

                    writer.Write((byte)Compression);
                    writer.Write((byte)0);
                    writer.Write((short)0);

                    writer.Write((uint)SamplesPerSecond);

                    writer.Write((ushort)BytesPerSample);
                    writer.Write((ushort)FormatCategory);

                    ushort marginLengthInSample
                        = (ushort)(CrossCorrelationMarginLength * SamplesPerSecond / 1000.0f);
                    writer.Write((ushort)marginLengthInSample);
                    writer.Write((ushort)0);
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
        /// Loads the wave information header from the given file.
        /// </summary>
        /// <param name="file">
        /// File to load the wave information header.
        /// </param>
        public void Load(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("file");
            }

            FileStream fs = new FileStream(file, FileMode.Open);
            try
            {
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    fs = null;
                    VoiceFontHeader voiceFontHeader = new VoiceFontHeader();
                    voiceFontHeader.Load(reader);

                    if (voiceFontHeader.FileTag != WihFileTag)
                    {
                        throw new InvalidDataException(Helper.NeutralFormat("Unsupported file tag \"{0}\"", Helper.UintToString(voiceFontHeader.FileTag)));
                    }

                    if (voiceFontHeader.FormatTag != WihFormatTag)
                    {
                        throw new InvalidDataException(Helper.NeutralFormat("Unsupported GUID \"{0}\"", voiceFontHeader.FormatTag.ToString("D", CultureInfo.InvariantCulture)));
                    }

                    if (voiceFontHeader.Version != WihVersion)
                    {
                        throw new InvalidDataException(Helper.NeutralFormat("Unsupported version \"{0}\"", voiceFontHeader.Version));
                    }

                    if (voiceFontHeader.Build != WihBuild)
                    {
                        throw new InvalidDataException(Helper.NeutralFormat("Unsupported version \"{0}\"", voiceFontHeader.Build));
                    }

                    Compression = (WaveCompressCatalog)reader.ReadByte();
                    reader.ReadByte();
                    reader.ReadInt16();

                    SamplesPerSecond = reader.ReadUInt32();

                    BytesPerSample = reader.ReadUInt16();
                    FormatCategory = (WaveFormatTag)reader.ReadInt16();

                    ushort marginLengthInSample = reader.ReadUInt16();
                    if ((marginLengthInSample * 1000.0f) % SamplesPerSecond != 0)
                    {
                        throw new InvalidDataException("Invalid CrossCorrelationMarginLength");
                    }

                    CrossCorrelationMarginLength = (ushort)(marginLengthInSample * 1000.0f / SamplesPerSecond);
                    reader.ReadInt16();
                }
            }
            finally
            {
                if (null != fs)
                {
                    fs.Dispose();
                }
            }

            // Validates this object.
            try
            {
                Validate();
            }
            catch (NotSupportedException e)
            {
                throw new InvalidDataException("Input contains some not supported data", e);
            }
        }

        /// <summary>
        /// Validates the wave information header and throw exception when it is invalid.
        /// </summary>
        public void Validate()
        {
            if (WaveUtil.SupportedSamplesPerSecond.IndexOf((int)SamplesPerSecond) < 0)
            {
                throw new NotSupportedException(
                    Helper.NeutralFormat("Samples per second [{0}] is not supported. " +
                        "Only supports {0} samples per second.",
                        SamplesPerSecond,
                        WaveUtil.SupportedSamplesPerSecond.Concatenate(", ")));
            }

            switch (FormatCategory)
            {
                case WaveFormatTag.Mulaw:
                    if (SamplesPerSecond != 8000)
                    {
                        throw new NotSupportedException("Only support 8000 samplesPerSecond for Mulaw enconding");
                    }

                    if (BytesPerSample != 1)
                    {
                        throw new NotSupportedException("Only 1 bytesPerSample for Mulaw encoding");
                    }

                    if (Compression != WaveCompressCatalog.Unc)
                    {
                        throw new NotSupportedException("Only support uncompress encoding for Mulaw encoding");
                    }

                    break;
                case WaveFormatTag.Pcm:
                    if (BytesPerSample != 2)
                    {
                        throw new NotSupportedException("Only 2 bytesPerSample for PCM encoding");
                    }

                    break;
                default:
                    throw new NotSupportedException("Only Mulaw or PCM supported");
            }
        }

        /// <summary>
        /// Gets voice font file data size.
        /// </summary>
        /// <returns>Voice font file data size.</returns>
        private long GetFileDataSize()
        {
            return (sizeof(byte) * 2) + // Compression
                sizeof(short) +
                sizeof(uint) + // SamplesPerSecond
                (sizeof(ushort) * 4); // BytesPerSample + FormatCategory + marginLengthInSample
        }

        #endregion
    }
}