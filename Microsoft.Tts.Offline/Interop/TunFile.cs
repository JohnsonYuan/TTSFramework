//----------------------------------------------------------------------------
// <copyright file="TunFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements TunFile.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Interop
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.Offline.Config;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// BadUnitCandiateSourceInfo store the bad unit candiate.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class BadUnitCandiateSourceInfo : IEquatable<BadUnitCandiateSourceInfo>
    {
        #region fields
        private ulong _sentenceIdStringPoolOffset;
        private short _recordingIndexOfNonSilence;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="BadUnitCandiateSourceInfo" /> class.
        /// </summary>
        public BadUnitCandiateSourceInfo()
        {
            _sentenceIdStringPoolOffset = 0;
            _recordingIndexOfNonSilence = 0;
        }

        #region Properties
        /// <summary>
        /// Gets or sets the string pool offset of the sentence.
        /// </summary>
        public ulong SentenceIdStringPoolOffset
        {
            get
            {
                return _sentenceIdStringPoolOffset;
            }

            set
            {
                _sentenceIdStringPoolOffset = value;
            }
        }

        /// <summary>
        /// Gets or sets the recording index of non-silence.
        /// </summary>
        public short RecordingIndexOfNonSilence
        {
            get
            {
                return _recordingIndexOfNonSilence;
            }

            set
            {
                _recordingIndexOfNonSilence = value;
            }
        }
        #endregion

        /// <summary>
        /// Return the size of the instance saved in disk.
        /// </summary>
        /// <returns> The size of the instance.</returns>
        public static ulong Size()
        {
            return (ulong)(sizeof(uint) * 2);
        }

        /// <summary>
        /// Implement the interface of Equals.
        /// </summary>
        /// <param name="other">The other instance to compare.</param>
        /// <returns>The bool to indicate if two instance are equal.</returns>
        public bool Equals(BadUnitCandiateSourceInfo other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return this.RecordingIndexOfNonSilence == other.RecordingIndexOfNonSilence &&
                    this.SentenceIdStringPoolOffset == other.SentenceIdStringPoolOffset;
            }
        }
    }

    /// <summary>
    /// The class used to write/read TUN file.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class BadUnitListFile
    {
        private const uint Version = 0xff;         // data version
        private const uint BuildVersion = 0xff;    // build number

        /// <summary>
        /// The magic number in the file head.
        /// </summary>
        private const string FileTag = ".TUN";

        /// <summary>
        /// GUID for tun file.
        /// </summary>
        private readonly Guid FormatGuid = new Guid("{746FE290-5D32-4C77-AD06-75F0752C0CDF}");

        private uint tunVersion;
        private uint tunBuildVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="BadUnitListFile" /> class.
        /// </summary>
        /// <param name="version">The version of bad unit file.</param>
        /// <param name="buildVersion">The build versio of unit file.</param>
        public BadUnitListFile(uint version, uint buildVersion)
        {
            tunVersion = version;
            tunBuildVersion = buildVersion;
        }

        /// <summary>
        /// Append bad unit information to the file.
        /// </summary>
        /// <param name="path">The path of the file to append.</param>
        /// <param name="badUnit">The bad unit information.</param>
        public void AppendFile(string path, BadUnitCandiateSourceInfo badUnit)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                ulong leftDataSize = 0;
                uint badRecordCount = 0;
                List<BadUnitCandiateSourceInfo> badUnitRecords = new List<BadUnitCandiateSourceInfo>();
                using (BinaryReader br = new BinaryReader(fs))
                {
                    fs = null;
                    var bytesOfFileTag = Encoding.ASCII.GetBytes(FileTag);
                    var fileTagRead = br.ReadBytes(bytesOfFileTag.Length);

                    if (!fileTagRead.SequenceEqual(bytesOfFileTag))
                    {
                        throw new Exception("The tag in tun file is not equal with defintion.");
                    }

                    var guidBytesArray = FormatGuid.ToByteArray();
                    var guidRead = br.ReadBytes(guidBytesArray.Length);

                    if (!guidRead.SequenceEqual(guidBytesArray))
                    {
                        throw new Exception("The guid in tun file is not equal with defintion.");
                    }

                    // Skip version and build version
                    br.ReadBytes(sizeof(uint) * 2);
                    leftDataSize = br.ReadUInt64();

                    if (leftDataSize != (ulong)(br.BaseStream.Length - br.BaseStream.Position))
                    {
                        throw new Exception("The data size has error.");
                    }

                    var tunVerReaded = br.ReadUInt32();
                    var tunBuildVerReaded = br.ReadUInt32();

                    if (tunVerReaded != tunVersion || tunBuildVerReaded != tunBuildVersion)
                    {
                        throw new Exception("The mismatch between version or build version number.");
                    }

                    // Read offset;
                    br.ReadUInt32();
                    badRecordCount = br.ReadUInt32();

                    for (int i = 0; i < badRecordCount; i++)
                    {
                        var badUnitRecord = new BadUnitCandiateSourceInfo();
                        badUnitRecord.SentenceIdStringPoolOffset = br.ReadUInt32();
                        badUnitRecord.RecordingIndexOfNonSilence = (short)br.ReadUInt32();
                        badUnitRecords.Add(badUnitRecord);
                    }

                    if (!badUnitRecords.Contains(badUnit))
                    {
                        badUnitRecords.Add(badUnit);
                        leftDataSize += BadUnitCandiateSourceInfo.Size();
                    }

                    badUnitRecords.Sort(
                            delegate(BadUnitCandiateSourceInfo left, BadUnitCandiateSourceInfo right)
                            {
                                if (left.SentenceIdStringPoolOffset < right.SentenceIdStringPoolOffset)
                                {
                                    return 1;
                                }
                                else if (left.SentenceIdStringPoolOffset > right.SentenceIdStringPoolOffset)
                                {
                                    return -1;
                                }
                                else
                                {
                                    if (left.RecordingIndexOfNonSilence < right.RecordingIndexOfNonSilence)
                                    {
                                        return 1;
                                    }
                                    else
                                    {
                                        return -1;
                                    }
                                }
                            });

                    badRecordCount = (uint)badUnitRecords.Count();
                }

                fs = new FileStream(path, FileMode.Open, FileAccess.Write);
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    fs = null;
                    WriteHeader(bw, badRecordCount, leftDataSize);
                    foreach (var badUnitItem in badUnitRecords)
                    {
                        bw.Write((uint)badUnitItem.SentenceIdStringPoolOffset);
                        bw.Write((uint)badUnitItem.RecordingIndexOfNonSilence);
                    }
                }
            }
            finally
            {
                if (fs != null)
                {
                    fs.Dispose();
                }
            }
        }

        /// <summary>
        /// Create the tun file from the scratch.
        /// </summary>
        /// <param name="path">The path of the file to append.</param>
        /// <param name="badUnit">The bad unit information.</param>
        public void CreateFile(string path, BadUnitCandiateSourceInfo badUnit)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(path, FileMode.Create, FileAccess.Write);
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    fs = null;
                    WriteHeader(bw, 1, sizeof(uint) * 6);
                    bw.Write((uint)badUnit.SentenceIdStringPoolOffset);
                    bw.Write((uint)badUnit.RecordingIndexOfNonSilence);
                }
            }
            finally
            {
                if (fs != null)
                {
                    fs.Dispose();
                }
            }
        }

        private void WriteHeader(BinaryWriter bw, uint badRecordingsCount, ulong dataRemainSize)
        {
            bw.Write(Encoding.ASCII.GetBytes(FileTag));
            bw.Write(FormatGuid.ToByteArray());
            bw.Write(Version);
            bw.Write(BuildVersion);

            // Write data size
            bw.Write((ulong)dataRemainSize);
            bw.Write(tunVersion);
            bw.Write(tunBuildVersion);
            var badRecordingoffset = (ulong)bw.BaseStream.Position + sizeof(ulong);
            bw.Write((uint)badRecordingoffset);
            bw.Write((uint)badRecordingsCount);
        }
    }
}