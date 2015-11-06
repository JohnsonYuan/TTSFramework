//----------------------------------------------------------------------------
// <copyright file="UnitIndexingFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements UnitIndexingFile.
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
    using Microsoft.Tts.Offline.Config;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;

    // IdKeyedWaveCandidateInfos is a SortedDictionary with key is the candidate id and the value is the WaveCandidateInfo.
    using IdKeyedWaveCandidateInfos = System.Collections.Generic.SortedDictionary<int, WaveCandidateInfo>;

    /// <summary>
    /// The wave candidate info will be writed into UnitIndexingFile.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class WaveCandidateInfo
    {
        #region Fields

        /// <summary>
        /// The size in byte of this object.
        /// </summary>
        public const uint DataSize = sizeof(uint) + // SetenceIdOffset
            sizeof(uint) + // frameIndex
            sizeof(ushort) + // frameNumber
            sizeof(ushort) + // IndexOfNonSilence
            sizeof(ushort) + // FrameIndexInSentence
            sizeof(byte) + // left margin frame
            sizeof(byte); // right margin frame

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the candidate name of this object.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets ID of this object, it is the candidate ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets Global ID of this object, it is the candidate global ID.
        /// </summary>
        public int GlobalId { get; set; }

        /// <summary>
        /// Gets or sets the frame offset in inventory.
        /// </summary>
        public uint FrameIndex { get; set; }

        /// <summary>
        /// Gets or sets the frame number in inventory.
        /// </summary>
        public ushort FrameNumber { get; set; }

        /// <summary>
        /// Gets or sets the sentence id offset in string pool.
        /// </summary>
        public uint SentenceIdOffset { get; set; }

        /// <summary>
        /// Gets or sets the sentence id.
        /// </summary>
        public string SentenceId { get; set; }

        /// <summary>
        /// Gets or sets the index of non-silence unit in sentence.
        /// </summary>
        public ushort IndexOfNonSilence { get; set; }

        /// <summary>
        /// Gets or sets the frame index in sentence.
        /// </summary>
        public ushort FrameIndexInSentence { get; set; }

        /// <summary>
        /// Gets or sets the maximum frame number of extend to left side on frame shifting.
        /// </summary>
        public byte LeftMarginInFrame { get; set; }

        /// <summary>
        /// Gets or sets the maximum frame number of extend to right side on frame shifting.
        /// </summary>
        public byte RightMarginInFrame { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Saves the wave candidate info into binary writer.
        /// </summary>
        /// <param name="writer">The given binary writer.</param>
        public void Save(BinaryWriter writer)
        {
            writer.Write((uint)SentenceIdOffset);
            writer.Write((uint)FrameIndex);
            writer.Write((ushort)FrameNumber);
            writer.Write((ushort)IndexOfNonSilence);
            writer.Write((ushort)FrameIndexInSentence);
            writer.Write((byte)LeftMarginInFrame);
            writer.Write((byte)RightMarginInFrame);
        }

        /// <summary>
        /// Loads the wave candidate info from binary reader.
        /// </summary>
        /// <param name="reader">The given binary reader.</param>
        public void Load(BinaryReader reader)
        {
            SentenceIdOffset = reader.ReadUInt32();
            FrameIndex = reader.ReadUInt32();
            FrameNumber = reader.ReadUInt16();
            IndexOfNonSilence = reader.ReadUInt16();
            FrameIndexInSentence = reader.ReadUInt16();
            LeftMarginInFrame = reader.ReadByte();
            RightMarginInFrame = reader.ReadByte();
        }

        /// <summary>
        /// Save info into string.
        /// </summary>
        /// <returns>String.</returns>
        public override string ToString()
        {
            string text = Helper.NeutralFormat("{0} {1}-{2} {3}-{4} {5}-{6} {7} {8}",
                SentenceId, FrameIndexInSentence, FrameIndexInSentence + FrameNumber,
                Id, GlobalId, FrameIndex, IndexOfNonSilence,
                LeftMarginInFrame, RightMarginInFrame);
            return text;
        }
        #endregion
    }

    /// <summary>
    /// The class used to write/read UNT file.
    /// </summary>
    [CLSCompliantAttribute(false)]
    public class UnitIndexingFile : IDisposable
    {
        #region Fields

        /// <summary>
        /// The file tag of UNT file.
        /// </summary>
        public const uint FileTag = (int)FontSectionTag.UnitInfo;

        /// <summary>
        /// The GUID of UNT file.
        /// </summary>
        public static readonly Guid FormatTag = VoiceFontTag.FmtIdUnitSelectData;

        /// <summary>
        /// The current version number of this UNT file.
        /// </summary>
        public const uint Version = (uint)FormatVersion.Tts30;

        /// <summary>
        /// The name indexed unit index id, which key is the unit name, and the value is the unit index id.
        /// </summary>
        private readonly IDictionary<string, int> _namedUnitIndexId;

        /// <summary>
        /// The wave candidates. The key is the unit index id, and the value is the IdKeyedWaveCandidateInfos.
        /// IdKeyedWaveCandidateInfos is a SortedDictionary with key is the candidate id and the value is the WaveCandidateInfo.
        /// </summary>
        private readonly Dictionary<int, IdKeyedWaveCandidateInfos> _waveCandidates = new Dictionary<int, IdKeyedWaveCandidateInfos>();

        /// <summary>
        /// The string pool.
        /// </summary>
        private readonly StringPool _stringPool = new StringPool();

        /// <summary>
        /// Save the name offsets in string pool for each unit.
        /// </summary>
        private Dictionary<string, uint> _unitNameStringPoolOffsets = new Dictionary<string, uint>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the UnitIndexingFile class. Can be followed with Load() operation and Add() methods.
        /// </summary>
        /// <param name="namedUnitTypeId">The Dictionary which key is unit name and the value is unit index id.</param>
        public UnitIndexingFile(IDictionary<string, int> namedUnitTypeId)
        {
            // Initializes build number.
            BuildNumber = FontBuildNumber.GetCurrentBuildNumber();

            // Ensures the UnitTypeId is continuous.
            if (namedUnitTypeId != null)
            {
                ValidateUnitNameIdMap(namedUnitTypeId);
                _namedUnitIndexId = namedUnitTypeId;
            }

            // Updates fields.
            // Firstly, adds the size of the header.
            uint size = (uint)(sizeof(uint) + // FileTag
                FormatTag.ToByteArray().Length + // FormatTag
                sizeof(uint) + // DataSize
                sizeof(uint) + // Version
                sizeof(uint) + // BuildNumber
                sizeof(uint) + // SamplePerFrame
                sizeof(uint) + // UnitIndexOffset
                sizeof(uint) + // UnitIndexCount
                sizeof(uint) + // CandidateDataOffset
                sizeof(uint) + // CandidateCount
                sizeof(uint) + // StringPoolOffset
                sizeof(uint)); // StringPoolSize

            // Secondly, the unit index section.
            UnitIndexOffset = size;

            // Calculates the unit index number, which is equal to the unit number.
            UnitIndexCount = (_namedUnitIndexId == null) ? 0 : (uint)_namedUnitIndexId.Count;
            size += UnitIndexCount * UnitIndexInfo.DataSize;

            // Thirdly, the candidate data section. And, currently, there is no candidate yet.
            CandidateDataOffset = size;
            CandidateCount = 0;

            // Set the Sample Per Frame
            SamplePerFrame = 0;

            // Get string pool offset
            foreach (string unitName in namedUnitTypeId.Keys)
            {
                _unitNameStringPoolOffsets.Add(unitName, (uint)_stringPool.PutString(unitName));
            }

            // And then, the string pool section.
            StringPoolOffset = size;
            StringPoolSize = (uint)_stringPool.Length;

            // Finally, updates the DataSize, which don't include the below three fields.
            DataSize = (uint)(size + StringPoolSize -
                sizeof(uint) - // FileTag
                FormatTag.ToByteArray().Length - // FormatTag
                sizeof(uint)); // DataSize
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the data size of this file.
        /// </summary>
        public uint DataSize { get; private set; }

        /// <summary>
        /// Gets or sets the build number of this file.
        /// </summary>
        public FontBuildNumber BuildNumber { get; set; }

        /// <summary>
        /// Gets or sets the sample per Frame.
        /// </summary>
        public uint SamplePerFrame { get; set; }

        /// <summary>
        /// Gets the unit index section offset.
        /// </summary>
        public uint UnitIndexOffset { get; private set; }

        /// <summary>
        /// Gets the unit index count.
        /// </summary>
        public uint UnitIndexCount { get; private set; }

        /// <summary>
        /// Gets the candidate data section offset.
        /// </summary>
        public uint CandidateDataOffset { get; private set; }

        /// <summary>
        /// Gets the total candidate count.
        /// </summary>
        public uint CandidateCount { get; private set; }

        /// <summary>
        /// Gets the string pool section offset.
        /// </summary>
        public uint StringPoolOffset { get; private set; }

        /// <summary>
        /// Gets the string pool size.
        /// </summary>
        public uint StringPoolSize { get; private set; }

        /// <summary>
        /// Gets the wave candiates.
        /// </summary>
        public Dictionary<int, IdKeyedWaveCandidateInfos> WaveCandidates
        {
            get
            {
                return _waveCandidates;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Compares two UNT files by ignoring builder number.
        /// </summary>
        /// <param name="left">The left file name of UNT file.</param>
        /// <param name="right">The right file name of UNT file.</param>
        /// <returns>True if the two UNT file is equal, otherwise false.</returns>
        public static bool Compare(string left, string right)
        {
            using (UnitIndexingFile leftFile = new UnitIndexingFile(null))
            {
                leftFile.Load(left);
                using (UnitIndexingFile rightFile = new UnitIndexingFile(null))
                {
                    rightFile.Load(right);

                    leftFile.BuildNumber = rightFile.BuildNumber;
                    string tempFile = left + Path.GetRandomFileName();
                    try
                    {
                        leftFile.Save(tempFile);
                        return Helper.CompareBinary(tempFile, right);
                    }
                    finally
                    {
                        Helper.SafeDelete(tempFile);
                    }
                }
            }
        }

        /// <summary>
        /// Builds a map between the phone name and its type id.
        /// </summary>
        /// <param name="phoneSet">The given phoneset.</param>
        /// <returns>The Dictionary which key is unit name and the value is unit index id.</returns>
        public static Dictionary<string, int> BuildPhoneNameIdMap(TtsPhoneSet phoneSet)
        {
            // Adds the phone one by one.
            int maxId = 0;
            Dictionary<string, int> result = new Dictionary<string, int>();
            foreach (Phone phone in phoneSet.Phones)
            {
                // Please notice here, the phone set file contains the runtime silence, but not the silence.
                string name = Phoneme.ToHtk(phone.Name);
                result.Add(name, phone.Id);
                if (phone.Id > maxId)
                {
                    maxId = phone.Id;
                }
            }

            // Ensures there is continuous and starts from 0.
            for (int i = 0; i <= maxId; ++i)
            {
                if (!result.ContainsValue(i))
                {
                    // Adds a null phoneme here for padding.
                    result.Add(Helper.NeutralFormat("_{0}_{1}_", Phoneme.Null, i), i);
                }
            }

            return result;
        }

        /// <summary>
        /// Validates the given map between the unit name and its type id.
        /// A valid mapping must be have continuous unit index id which starts from 0.
        /// </summary>
        /// <param name="namedUnitTypeId">The Dictionary which key is unit name and the value is unit index id.</param>
        public static void ValidateUnitNameIdMap(IDictionary<string, int> namedUnitTypeId)
        {
            HashSet<int> unitTypeIds = new HashSet<int>();
            for (int i = 0; i < namedUnitTypeId.Count; ++i)
            {
                unitTypeIds.Add(i);
            }

            unitTypeIds.UnionWith(namedUnitTypeId.Values);
            if (unitTypeIds.Count != namedUnitTypeId.Count)
            {
                throw new InvalidDataException("The map between name and unit index id is invalid");
            }
        }

        /// <summary>
        /// Adds a wave candidate info into unit indexing file.
        /// </summary>
        /// <param name="candidateInfo">The candidate information will be wrote into unit indexing file.</param>
        public void Add(WaveCandidateInfo candidateInfo)
        {
            if (_namedUnitIndexId == null)
            {
                throw new InvalidOperationException("Add() method can only be applied in object initialized with namedUnitTypeId");
            }

            if (candidateInfo == null)
            {
                throw new ArgumentNullException("candidateInfo");
            }

            if (!_namedUnitIndexId.ContainsKey(candidateInfo.Name))
            {
                throw new InvalidDataException(Helper.NeutralFormat("Unknown candidate name \"{0}\"", candidateInfo.Name));
            }

            int unitTypeId = _namedUnitIndexId[candidateInfo.Name];
            if (!_waveCandidates.ContainsKey(unitTypeId))
            {
                _waveCandidates.Add(unitTypeId, new IdKeyedWaveCandidateInfos());
            }

            if (_waveCandidates[unitTypeId].ContainsKey(candidateInfo.Id))
            {
                throw new InvalidDataException(Helper.NeutralFormat("Duplicated candidate id \"{0}\"", candidateInfo.Id));
            }

            _waveCandidates[unitTypeId].Add(candidateInfo.Id, candidateInfo);
            candidateInfo.SentenceIdOffset = (uint)_stringPool.PutString(candidateInfo.SentenceId);

            // Update fields.
            // Increment the candidate count.
            ++CandidateCount;

            // The candidate count will impact the string pool offset and the overall data size.
            StringPoolOffset += WaveCandidateInfo.DataSize;
            DataSize += WaveCandidateInfo.DataSize;

            // Since we didn't know the size of string pool increased or not, so it can be subtracted and then added.
            DataSize -= StringPoolSize;
            StringPoolSize = (uint)_stringPool.Length;
            DataSize += StringPoolSize;
        }

        /// <summary>
        /// Saves the unit indexing file.
        /// </summary>
        /// <param name="file">The given file name.</param>
        public void Save(string file)
        {
            ValidateContinuousCandidateId();

            FileStream fs = new FileStream(file, FileMode.Create);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    fs = null;
                    SaveHeader(writer);
                    SaveCandidates(writer);
                    SaveStringPool(writer);
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
        /// Loads the unit indexing file.
        /// </summary>
        /// <param name="file">The given file name.</param>
        public void Load(string file)
        {
            long fileLength = 0;
            FileStream fs = new FileStream(file, FileMode.Open);
            try
            {
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    fs = null;
                    fileLength = reader.BaseStream.Length;
                    LoadHeader(reader);
                    LoadStringPool(reader);
                    LoadCandidates(reader);
                }
            }
            finally
            {
                if (null != fs)
                {
                    fs.Dispose();
                }
            }

            // validate if the data loaded from file are consistent
            if ((UnitIndexOffset + (UnitIndexCount * UnitIndexInfo.DataSize) != CandidateDataOffset)
                || (CandidateDataOffset + (CandidateCount * WaveCandidateInfo.DataSize) != StringPoolOffset)
                || (StringPoolOffset + StringPoolSize != fileLength)
                || (_waveCandidates.Values.Select(p => p.Count).Sum() != CandidateCount))
            {
                throw new InvalidDataException("The UNT file is incorrected");
            }
        }

        /// <summary>
        /// Search the offset of certain candidate in unt file .
        /// </summary>
        /// <param name="unitName">The given unit name.</param>
        /// <param name="sentenceId">The given sentence id name.</param>
        /// <param name="indexOfNonSilence">Index of non silence in the sentence.</param>
        /// <returns>The offset of candidate in UNT file, -1 means "no found".</returns>
        public int SearchCandidateOffset(string unitName, string sentenceId, uint indexOfNonSilence)
        {
            int unitIndex = _namedUnitIndexId[unitName];
            int offset = 0;
            foreach (WaveCandidateInfo candidateInfo in _waveCandidates[unitIndex].Values)
            {
                if (indexOfNonSilence == candidateInfo.IndexOfNonSilence)
                {
                    string candidateSentenceId = _stringPool.GetString((int)candidateInfo.SentenceIdOffset);
                    if (candidateSentenceId.Equals(sentenceId))
                    {
                        break;
                    }
                }

                offset++;
            }

            if (offset == _waveCandidates[unitIndex].Count)
            {
                offset = -1;
            }

            return offset;
        }

        /// <summary>
        /// Save whole unit inventory to text file for log.
        /// </summary>
        /// <param name="file">Output file name.</param>
        public void SaveToText(string file)
        {
            ValidateContinuousCandidateId();
            using (StreamWriter sw = new StreamWriter(file))
            {
                for (int unitIndex = 0; unitIndex < UnitIndexCount; unitIndex++)
                {
                    if (_waveCandidates.ContainsKey(unitIndex))
                    {
                        string unitName = _waveCandidates[unitIndex].First().Value.Name;

                        // If the _waveCandidtes contains this id, it means there is some candidates belongs to this unit.
                        Debug.Assert(_waveCandidates[unitIndex].Count > 0, "The wave candidtes count of unit should be greater than 0");

                        sw.WriteLine("UnitIndex:{0}, Name:{1}, Count:{2}", unitIndex, unitName, _waveCandidates[unitIndex].Count);

                        foreach (WaveCandidateInfo wci in _waveCandidates[unitIndex].Values)
                        {
                            sw.WriteLine("\t{0}", wci.ToString());
                        }
                    }
                    else
                    {
                        sw.WriteLine("UnitIndex:{0}, Name:{1}, Count:{2}", unitIndex, "NULL", 0);
                    }
                }
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes the resources used in this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the RewindableTextReader.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources;
        /// False to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != _stringPool)
                {
                    _stringPool.Dispose();
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates the candidate Ids, which should be started with 0 and continuous.
        /// </summary>
        private void ValidateContinuousCandidateId()
        {
            foreach (IdKeyedWaveCandidateInfos keyedCandidateInfos in _waveCandidates.Values)
            {
                ICollection<int> expectedIds = new List<int>();
                for (int i = 0; i < keyedCandidateInfos.Count; ++i)
                {
                    expectedIds.Add(i);
                }

                if (!Helper.Compare(expectedIds, keyedCandidateInfos.Keys, true))
                {
                    throw new InvalidDataException("The Id for each kind of Candidate should be started with 0 and continuous");
                }
            }
        }

        /// <summary>
        /// Saves the header section to binary writer.
        /// </summary>
        /// <param name="writer">The given binary writer.</param>
        private void SaveHeader(BinaryWriter writer)
        {
            writer.Write((uint)FileTag);
            writer.Write(FormatTag.ToByteArray());
            writer.Write((uint)DataSize);
            writer.Write((uint)Version);
            writer.Write((int)BuildNumber.ToInt32());
            writer.Write((uint)SamplePerFrame);
            writer.Write((uint)UnitIndexOffset);
            writer.Write((uint)UnitIndexCount);
            writer.Write((uint)CandidateDataOffset);
            writer.Write((uint)CandidateCount);
            writer.Write((uint)StringPoolOffset);
            writer.Write((uint)StringPoolSize);
        }

        /// <summary>
        /// Saves the unit index and candidate data section to binary writer.
        /// </summary>
        /// <param name="writer">The given binary writer.</param>
        private void SaveCandidates(BinaryWriter writer)
        {
            // Unit index section.
            Debug.Assert(writer.BaseStream.Position == UnitIndexOffset, "This point is the unit index offset");
            UnitIndexInfo indexInfo = new UnitIndexInfo { Offset = CandidateDataOffset };
            for (int i = 0; i < UnitIndexCount; ++i)
            {
                if (_waveCandidates.ContainsKey(i))
                {
                    string unitName = _waveCandidates[i].First().Value.Name;

                    // If the _waveCandidtes contains this id, it means there is some candidates belongs to this unit.
                    Debug.Assert(_waveCandidates[i].Count > 0, "The wave candidtes count of unit should be greater than 0");

                    // Writes the unit index information.
                    indexInfo.Count = (uint)_waveCandidates[i].Count;
                    indexInfo.UnitNameOffset = _unitNameStringPoolOffsets[unitName];
                    indexInfo.Save(writer);

                    // Moves the offset to the next unit.
                    indexInfo.Offset += (uint)_waveCandidates[i].Count * WaveCandidateInfo.DataSize;
                }
                else
                {
                    // If the _waveCandidtes doesn't contain the id, it means there is no candidate belongs to this unit.
                    // For example, phoneme "silence" will have no candidates in RUS now.
                    // However, in this case, a InvalidUnitIndexInfo should be written.
                    UnitIndexInfo.InvalidIndexInfo.Save(writer);
                }
            }

            // Candidate data section.
            Debug.Assert(writer.BaseStream.Position == CandidateDataOffset, "This point is the candidate data offset");
            int globalId = 0;
            for (int i = 0; i < UnitIndexCount; ++i)
            {
                if (_waveCandidates.ContainsKey(i))
                {
                    foreach (WaveCandidateInfo wci in _waveCandidates[i].Values)
                    {
                        wci.Save(writer);

                        // double check the global ID
                        if (wci.GlobalId != globalId)
                        {
                            throw new InvalidDataException(
                                Helper.NeutralFormat("Global ID is not consistent, expect:{0}, real:{1}, unit:{2}, id:{3}",
                                globalId, wci.GlobalId, wci.Name, wci.Id));
                        }

                        globalId++;
                    }
                }
            }
        }

        /// <summary>
        /// Saves the string pool section to binary writer.
        /// </summary>
        /// <param name="writer">The given binary writer.</param>
        private void SaveStringPool(BinaryWriter writer)
        {
            // string pool section.
            Debug.Assert(writer.BaseStream.Position == StringPoolOffset, "This point is the string pool offset");
            Debug.Assert(_stringPool.Length == StringPoolSize, "The size of string pool must be equal");
            _stringPool.Save(writer);
        }

        /// <summary>
        /// Loads the header section from binary reader.
        /// </summary>
        /// <param name="reader">The given binary reader.</param>
        private void LoadHeader(BinaryReader reader)
        {
            uint uintData = reader.ReadUInt32();
            if (uintData != FileTag)
            {
                byte[] bytes = Helper.ToBytes(uintData);
                Debug.Assert(bytes.Length == 4, "uint is 4 bytes length");
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Unsupported file tag \"0x{0}, 0x{1}, 0x{2}, 0x{3}\"",
                    bytes[0].ToString("x", CultureInfo.InvariantCulture),
                    bytes[1].ToString("x", CultureInfo.InvariantCulture),
                    bytes[2].ToString("x", CultureInfo.InvariantCulture),
                    bytes[3].ToString("x", CultureInfo.InvariantCulture)));
            }

            Guid guid = new Guid(reader.ReadBytes(FormatTag.ToByteArray().Length));
            if (guid != FormatTag)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Unsupported GUID \"{0}\"", guid.ToString("D", CultureInfo.InvariantCulture)));
            }

            DataSize = reader.ReadUInt32();
            if (DataSize != reader.BaseStream.Length - reader.BaseStream.Position)
            {
                throw new InvalidDataException("Input stream is corrupt since data size is mismatched");
            }

            uintData = reader.ReadUInt32();
            if (uintData != Version)
            {
                throw new InvalidDataException("Unspported version");
            }

            BuildNumber = new FontBuildNumber(reader.ReadInt32());
            SamplePerFrame = reader.ReadUInt32();
            UnitIndexOffset = reader.ReadUInt32();
            UnitIndexCount = reader.ReadUInt32();

            CandidateDataOffset = reader.ReadUInt32();
            CandidateCount = reader.ReadUInt32();
            StringPoolOffset = reader.ReadUInt32();
            StringPoolSize = reader.ReadUInt32();
        }

        /// <summary>
        /// Loads the unit index and candidate data section from binary reader.
        /// </summary>
        /// <param name="reader">The given binary reader.</param>
        private void LoadCandidates(BinaryReader reader)
        {
            _waveCandidates.Clear();
            _namedUnitIndexId.Clear();

            reader.BaseStream.Seek(UnitIndexOffset, SeekOrigin.Begin);
            List<UnitIndexInfo> unitIndexInfos = new List<UnitIndexInfo>();
            for (int i = 0; i < UnitIndexCount; ++i)
            {
                UnitIndexInfo unitIndexInfo = new UnitIndexInfo();
                unitIndexInfo.Load(reader);
                unitIndexInfos.Add(unitIndexInfo);
            }

            int invalidUnit = 1;
            for (int i = 0; i < unitIndexInfos.Count; ++i)
            {
                if (unitIndexInfos[i].Offset != UnitIndexInfo.InvalidOffset)
                {
                    reader.BaseStream.Seek(unitIndexInfos[i].Offset, SeekOrigin.Begin);
                    _waveCandidates.Add(i, new IdKeyedWaveCandidateInfos());

                    string name = _stringPool.GetString((int)unitIndexInfos[i].UnitNameOffset);

                    _namedUnitIndexId.Add(name, i);
                    _unitNameStringPoolOffsets.Add(name, unitIndexInfos[i].UnitNameOffset);

                    int globalId = (int)((unitIndexInfos[i].Offset - CandidateDataOffset) / WaveCandidateInfo.DataSize);

                    for (int j = 0; j < unitIndexInfos[i].Count; ++j)
                    {
                        WaveCandidateInfo wci = new WaveCandidateInfo();
                        wci.Load(reader);
                        _waveCandidates[i].Add(j, wci);
                        wci.Id = j;
                        wci.GlobalId = globalId + j;
                        wci.Name = name;
                        wci.SentenceId = _stringPool.GetString((int)wci.SentenceIdOffset);
                    }
                }
                else
                {
                    string name = string.Empty;

                    if (invalidUnit == 1)
                    {
                        name = "hpl_SIL";
                    }
                    else if (invalidUnit == 2)
                    {
                        name = "hpr_SIL";
                    }
                    else
                    {
                        throw new InvalidDataException("There are more than 2 InvalidOffset unit, the number suppose  be two: hpl_SIL and hpr_SIL.");
                    }

                    invalidUnit++;

                    _namedUnitIndexId.Add(name, i);
                }
            }
        }

        /// <summary>
        /// Loads the string pool section from binary reader.
        /// </summary>
        /// <param name="reader">The given binary reader.</param>
        private void LoadStringPool(BinaryReader reader)
        {
            reader.BaseStream.Seek(StringPoolOffset, SeekOrigin.Begin);
            _stringPool.Load(reader, (int)StringPoolSize);
        }

        #endregion

        #region Internal classes

        /// <summary>
        /// The class to hold the unit index information.
        /// </summary>
        private class UnitIndexInfo
        {
            #region Fields

            /// <summary>
            /// The data size of unit index information.
            /// </summary>
            public const uint DataSize = sizeof(uint) + // UnitNameOffset
                sizeof(uint) + // Offset
                sizeof(uint);  // Count

            /// <summary>
            /// The invalid offset.
            /// </summary>
            public const uint InvalidOffset = uint.MaxValue;

            /// <summary>
            /// The invalid UnitIndexInfo.
            /// </summary>
            public static readonly UnitIndexInfo InvalidIndexInfo = new UnitIndexInfo
            {
                UnitNameOffset = InvalidOffset,
                Count = 0,
                Offset = InvalidOffset
            };

            #endregion

            #region Properties

            public uint UnitNameOffset { get; set; }

            /// <summary>
            /// Gets or sets the offset of this unit.
            /// </summary>
            public uint Offset { get; set; }

            /// <summary>
            /// Gets or sets the count of this unit.
            /// </summary>
            public uint Count { get; set; }

            #endregion

            #region Methods

            /// <summary>
            /// Saves the unit index info into binary writer.
            /// </summary>
            /// <param name="writer">The given binary writer.</param>
            public void Save(BinaryWriter writer)
            {
                writer.Write((uint)UnitNameOffset);
                writer.Write((uint)Offset);
                writer.Write((uint)Count);
            }

            /// <summary>
            /// Loads the unit index info from binary reader.
            /// </summary>
            /// <param name="reader">The given binary reader.</param>
            public void Load(BinaryReader reader)
            {
                UnitNameOffset = reader.ReadUInt32();
                Offset = reader.ReadUInt32();
                Count = reader.ReadUInt32();
            }

            #endregion
        }

        #endregion
    }
}