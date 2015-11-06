//----------------------------------------------------------------------------
// <copyright file="VoiceFontHeader.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     Voice font header definition class.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.IO;

    /// <summary>
    /// Common voice font header.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class VoiceFontHeader
    {
        /// <summary>
        /// Gets or sets Tag of the file.
        /// </summary>
        public uint FileTag
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets GUID of the file format.
        /// </summary>
        public Guid FormatTag
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Bytes of data after this field.
        /// </summary>
        public ulong DataSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Version Number.
        /// </summary>
        public uint Version
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Build Number.
        /// </summary>
        public uint Build
        {
            get;
            set;
        }

        /// <summary>
        /// Save the header with given writer.
        /// </summary>
        /// <param name="writer">The binary writer used to save the header.</param>
        /// <returns>Size.</returns>
        public int Save(BinaryWriter writer)
        {
            int size = 0;

            writer.Write(FileTag);
            size += sizeof(uint);

            writer.Write(FormatTag.ToByteArray());
            size += FormatTag.ToByteArray().Length;

            writer.Write(Version);
            size += sizeof(uint);

            writer.Write(Build);
            size += sizeof(uint);

            writer.Write(DataSize);
            size += sizeof(ulong);

            return size;
        }

        /// <summary>
        /// Read the header from given reader.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        public void Load(BinaryReader reader)
        {
            FileTag = reader.ReadUInt32();
            FormatTag = new Guid(reader.ReadBytes(new Guid().ToByteArray().Length));
            Version = reader.ReadUInt32();
            Build = reader.ReadUInt32();
            DataSize = reader.ReadUInt64();
        }
    }
}