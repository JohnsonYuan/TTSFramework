//----------------------------------------------------------------------------
// <copyright file="AphFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements to manage APH file (AnyPrompt Help File)
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.AnyPrompt
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Config;

    /// <summary>
    /// Structure of candidate in APH file.
    /// </summary>
    public struct AphCandidate
    {
        /// <summary>
        /// Candidate index in voice font.
        /// </summary>
        public int VFontIndex;

        /// <summary>
        /// Wave duration (Millisecond).
        /// </summary>
        public int Duration;

        /// <summary>
        /// Energy (RMS).
        /// </summary>
        public float Energy;

        /// <summary>
        /// Average pitch (Hz).
        /// </summary>
        public float AveragePitch;

        /// <summary>
        /// Pitch pattern identifier.
        /// </summary>
        public int PitchPatternId;

        /// <summary>
        /// Pitch value collection.
        /// </summary>
        public float[] PitchValues;
    }

    /// <summary>
    /// Structure to manage unit in APH file.
    /// </summary>
    public struct AphUnit
    {
        #region Public Variables

        /// <summary>
        /// Unit identifier.
        /// </summary>
        public int UnitId;

        /// <summary>
        /// Maximum duration.
        /// </summary>
        public int MaxDuration;

        /// <summary>
        /// Minimum duration.
        /// </summary>
        public int MinDuration;

        /// <summary>
        /// Maximum energy.
        /// </summary>
        public float MaxEnergy;

        /// <summary>
        /// Minimum energy.
        /// </summary>
        public float MinEnergy;

        /// <summary>
        /// Unit candidate collection.
        /// </summary>
        public List<AphCandidate> Candidates;

        #endregion

        #region Private Delegates

        /// <summary>
        /// Delegate to get value of specific field in AphCandidate.
        /// </summary>
        /// <typeparam name="T1">Type of value.</typeparam>
        /// <param name="cand">Candidate.</param>
        /// <returns>Value.</returns>
        private delegate T1 GetValue<T1>(AphCandidate cand);

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets example candidate for each pitch pattern.
        /// </summary>
        /// <returns>Example candidates.</returns>
        public Collection<AphCandidate> Patterns
        {
            get
            {
                SortedList<int, AphCandidate> cands = new SortedList<int, AphCandidate>();

                foreach (AphCandidate cand in Candidates)
                {
                    if (cands.ContainsKey(cand.PitchPatternId))
                    {
                        continue;
                    }

                    cands.Add(cand.PitchPatternId, cand);
                }

                return new Collection<AphCandidate>(cands.Values);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get all candidate indexes with the same pitch pattern.
        /// </summary>
        /// <param name="patternId">Given pitch pattern.</param>
        /// <returns>Candidate index array.</returns>
        public int[] GetIndexes(int patternId)
        {
            List<int> cands = new List<int>();
            for (int i = 0; i < Candidates.Count; ++i)
            {
                if (Candidates[i].PitchPatternId == patternId)
                {
                    cands.Add(i);
                }
            }

            return cands.ToArray();
        }

        /// <summary>
        /// Filter candidate collection based on duration.
        /// </summary>
        /// <param name="vfontIndexes">Source candidate indexes.</param>
        /// <param name="lowerBound">Lower bound.</param>
        /// <param name="upperBound">Upper bound.</param>
        /// <returns>Filtered candidate indexes.</returns>
        public int[] FilterWithDuration(int[] vfontIndexes, int lowerBound,
            int upperBound)
        {
            return FilterCandidates<int>(vfontIndexes,
                delegate(AphCandidate cand)
                {
                    return cand.Duration;
                },
                lowerBound, upperBound);
        }

        /// <summary>
        /// Filter candidate collection based on energy.
        /// </summary>
        /// <param name="vfontIndexes">Source candidate indexes.</param>
        /// <param name="lowerBound">Lower bound.</param>
        /// <param name="upperBound">Upper bound.</param>
        /// <returns>Filtered candidate indexes.</returns>
        public int[] FilterWithEnergy(int[] vfontIndexes, float lowerBound,
            float upperBound)
        {
            return FilterCandidates<float>(vfontIndexes,
                delegate(AphCandidate cand)
                {
                    return cand.Energy;
                },
                lowerBound, upperBound);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Filter candidate collection.
        /// </summary>
        /// <typeparam name="T1">Type of item.</typeparam>
        /// <param name="vfontIndexes">Candidate indexes.</param>
        /// <param name="getValue">Delegate function.</param>
        /// <param name="lowerBound">Lower bound.</param>
        /// <param name="upperBound">Upper bound.</param>
        /// <returns>Filtered candidate indexes.</returns>
        private int[] FilterCandidates<T1>(int[] vfontIndexes, GetValue<T1> getValue,
            T1 lowerBound, T1 upperBound) where T1 : IComparable
        {
            if (vfontIndexes == null || vfontIndexes.Length == 0)
            {
                vfontIndexes = new int[Candidates.Count];
                for (int i = 0; i < Candidates.Count; ++i)
                {
                    vfontIndexes[i] = Candidates[i].VFontIndex;
                }
            }

            List<int> cands = new List<int>();
            for (int i = 0; i < vfontIndexes.Length; ++i)
            {
                T1 currentValue = getValue(Candidates[vfontIndexes[i]]);
                if (currentValue.CompareTo(lowerBound) >= 0 &&
                    currentValue.CompareTo(upperBound) <= 0)
                {
                    cands.Add(vfontIndexes[i]);
                }
            }

            return cands.ToArray();
        }

        #endregion
    }

    /// <summary>
    /// Structure of APH file header.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct AphFileHeader
    {
        #region Public Fields

        public int FormatTag;     // format tag
        public int Length;        // length of file data
        public int VerNumber;     // version number
        public int BuildNumber;   // build number
        public int LangId;        // language id
        public int PitchCount;    // number of pitch points in each unit candidate
        public int UnitCount;     // total number of units
        public int UnitOffset;    // offset to units

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the file size.
        /// </summary>
        public int FileSize
        {
            get
            {
                // Length is the data after Length field,
                // so the total file size is Length + 8:
                // FormatTag (int): 4 bytes
                // Length (int): 4 bytes
                return sizeof(int) + sizeof(int) + Length;
            }
        }

        #endregion

        #region Serialization/Deserialization

        /// <summary>
        /// Read APH file header.
        /// </summary>
        /// <param name="br">Binary reader.</param>
        /// <returns>APH file header.</returns>
        public static AphFileHeader Read(BinaryReader br)
        {
            if (br == null)
            {
                throw new ArgumentNullException("br");
            }

            int size = Marshal.SizeOf(typeof(AphFileHeader));
            byte[] buff = br.ReadBytes(size);

            if (buff.Length != size)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "No enough data for APH file header");
                throw new InvalidDataException(message);
            }

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(buff, 0, ptr, size);
                return (AphFileHeader)Marshal.PtrToStructure(ptr, typeof(AphFileHeader));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        /// <summary>
        /// Converts this instance into byte array.
        /// </summary>
        /// <returns>Byte array presenting this instance.</returns>
        public byte[] ToBytes()
        {
            byte[] buff = new byte[Marshal.SizeOf(typeof(AphFileHeader))];

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(buff.Length);
                Marshal.StructureToPtr(this, ptr, false);
                Marshal.Copy(ptr, buff, 0, buff.Length);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return buff;
        }

        #endregion
    }

    /// <summary>
    /// Class to manage APH file (AnyPrompt Helper Binary Data).
    /// </summary>
    public class AphFile : IDisposable
    {
        #region Private Variables

        private string _name;
        private Language _language;
        private int _version;
        private FontBuildNumber _build;
        private SortedList<int, AphUnit> _units;
        private float _maxEnergy;
        private float _minEnergy;
        private int _maxDuration;
        private int _minDuration;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="AphFile"/> class.
        /// </summary>
        public AphFile()
        {
            _units = new SortedList<int, AphUnit>();
            _maxEnergy = 0;
            _minEnergy = 0;
            _maxDuration = 0;
            _minDuration = 0;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets file name.
        /// </summary>
        public string FileName
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets Language.
        /// </summary>
        public Language Language
        {
            get { return _language; }
        }

        /// <summary>
        /// Gets format version.
        /// </summary>
        public int Version
        {
            get { return _version; }
        }

        /// <summary>
        /// Gets font build number.
        /// </summary>
        public FontBuildNumber Build
        {
            get { return _build; }
        }

        /// <summary>
        /// Gets Maximum energy.
        /// </summary>
        public float MaxEnergy
        {
            get { return _maxEnergy; }
        }

        /// <summary>
        /// Gets Minimum energy.
        /// </summary>
        public float MinEnergy
        {
            get { return _minEnergy; }
        }

        /// <summary>
        /// Gets Maximum duration.
        /// </summary>
        public int MaxDuration
        {
            get { return _maxDuration; }
        }

        /// <summary>
        /// Gets Minimum duration.
        /// </summary>
        public int MinDuration
        {
            get { return _minDuration; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Save APH file.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <param name="units">Unit collection.</param>
        /// <param name="verNumber">Version number.</param>
        /// <param name="buildNumber">Build number.</param>
        /// <param name="langId">Language identifier.</param>
        public static void Save(string fileName, Collection<AphUnit> units,
            int verNumber, int buildNumber, int langId)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            if (units == null)
            {
                throw new ArgumentNullException("units");
            }

            // APH file header
            AphFileHeader header;
            header.FormatTag = (int)FontSectionTag.AnyPromptHelper;
            header.Length = 0;  // tentatively value, will be rewritten
            header.VerNumber = verNumber;
            header.BuildNumber = buildNumber;
            header.LangId = langId;
            header.UnitCount = units.Count;
            header.UnitOffset = Marshal.SizeOf(typeof(AphFileHeader));

            int pitchCount = 0;
            if (units.Count > 0 && units[0].Candidates.Count > 0)
            {
                pitchCount = units[0].Candidates[0].PitchValues.Length;
            }

            if (pitchCount > 0)
            {
                header.PitchCount = pitchCount;
            }
            else
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid pitch count = [{0}]", pitchCount);
                throw new InvalidDataException(message);
            }

            Save(fileName, header, units);
        }

        /// <summary>
        /// Get unit candidate group.
        /// </summary>
        /// <param name="unitId">Unit identifier.</param>
        /// <returns>Unit candidate group.</returns>
        public AphUnit GetUnitGroup(int unitId)
        {
            if (!_units.ContainsKey(unitId))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid unitId [{0}]", unitId);
                throw new ArgumentException(message);
            }

            return _units[unitId];
        }

        /// <summary>
        /// Get unit candidate in APH file.
        /// </summary>
        /// <param name="unitId">Unit identifier.</param>
        /// <param name="candidateIndex">Candidate index.</param>
        /// <returns>Unit candidate in APH file.</returns>
        public AphCandidate GetCandidate(int unitId, int candidateIndex)
        {
            AphUnit unit = GetUnitGroup(unitId);

            if (candidateIndex >= unit.Candidates.Count)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Candidate index [{0}] is out of range", candidateIndex);
                throw new ArgumentException(message);
            }

            return unit.Candidates[candidateIndex];
        }

        #endregion

        #region Serialization/Deserialization

        /// <summary>
        /// Load APH file.
        /// </summary>
        /// <param name="fileName">File name.</param>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Ignore.")]
        public void Load(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            _units.Clear();
            _name = fileName;
            _maxEnergy = 0;
            _minEnergy = float.MaxValue;
            _maxDuration = 0;
            _minDuration = int.MaxValue;
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                // Read file header
                AphFileHeader header = AphFileHeader.Read(br);

                // verify file tag: "APH "
                if (header.FormatTag != (int)FontSectionTag.AnyPromptHelper)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid APH file tag [{0}]", header.FormatTag);
                    throw new InvalidDataException(message);
                }

                // verify file size
                if (fs.Length != header.FileSize)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid APH file data size: expected size = {0}, real size = {1}",
                        header.FileSize, fs.Length);
                    throw new InvalidDataException(message);
                }

                _language = (Language)header.LangId;
                _version = header.VerNumber;
                _build = new FontBuildNumber(header.BuildNumber);

                // Jump to unit group section
                fs.Seek(header.UnitOffset, SeekOrigin.Begin);

                // Read all unit groups
                for (uint i = 0; i < header.UnitCount; ++i)
                {
                    AphUnit unit;
                    unit.Candidates = new List<AphCandidate>();
                    unit.MinEnergy = float.MaxValue;
                    unit.MaxEnergy = 0;
                    unit.MinDuration = int.MaxValue;
                    unit.MaxDuration = 0;

                    // Unit identifier
                    unit.UnitId = br.ReadInt32();

                    // Count of unit candidates
                    uint count = br.ReadUInt32();

                    // Read all unit candidates
                    for (int j = 0; j < count; ++j)
                    {
                        AphCandidate candidate;
                        candidate.VFontIndex = j;
                        candidate.Duration = br.ReadInt32();
                        candidate.Energy = br.ReadSingle();
                        candidate.AveragePitch = br.ReadSingle();
                        candidate.PitchPatternId = br.ReadInt32();

                        // Global max/min energy
                        GetMaxMinValue<float>(candidate.Energy, ref _maxEnergy, ref _minEnergy);

                        // Local max/min energy
                        GetMaxMinValue<float>(candidate.Energy, ref unit.MaxEnergy, ref unit.MinEnergy);

                        // Global max/min duration
                        GetMaxMinValue<int>(candidate.Duration, ref _maxDuration, ref _minDuration);

                        // Local max/min duration
                        GetMaxMinValue<int>(candidate.Duration, ref unit.MaxDuration, ref unit.MinDuration);

                        // Read all pitch values
                        candidate.PitchValues = new float[header.PitchCount];
                        for (uint k = 0; k < header.PitchCount; ++k)
                        {
                            candidate.PitchValues[k] = br.ReadSingle();
                        }

                        unit.Candidates.Add(candidate);
                    }

                    if (_units.ContainsKey(unit.UnitId))
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "Duplicate unit group [unit id = {0}]", unit.UnitId);
                        throw new InvalidDataException(message);
                    }

                    _units.Add(unit.UnitId, unit);
                }
            }
        }

        /// <summary>
        /// Save APH file.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        public void Save(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            Save(fileName, new Collection<AphUnit>(_units.Values), _version,
                _build.ToInt32(), (int)_language);
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing flag.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _units.Clear();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Save APH file.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <param name="header">File header.</param>
        /// <param name="units">Unit collection.</param>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Ignore.")]
        private static void Save(string fileName, AphFileHeader header, Collection<AphUnit> units)
        {
            Debug.Assert(!string.IsNullOrEmpty(fileName) && units != null);

            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                // Set tentative value for data length
                header.Length = 0;
                int dataLength = 0;

                // Write file header
                byte[] headerBytes = header.ToBytes();
                bw.Write(headerBytes);
                dataLength += headerBytes.Length;

                // Write all unit groups
                foreach (AphUnit unit in units)
                {
                    // Write unit id
                    bw.Write(unit.UnitId);
                    dataLength += sizeof(int);

                    // Write unit candidate count
                    bw.Write(unit.Candidates.Count);
                    dataLength += sizeof(int);

                    // Write all candidates
                    foreach (AphCandidate candidate in unit.Candidates)
                    {
                        // Write duration
                        bw.Write(candidate.Duration);
                        dataLength += sizeof(int);

                        // Write energy
                        bw.Write(candidate.Energy);
                        dataLength += sizeof(float);

                        // Write average pitch
                        bw.Write(candidate.AveragePitch);
                        dataLength += sizeof(float);

                        // Write pitch pattern id
                        bw.Write(candidate.PitchPatternId);
                        dataLength += sizeof(int);

                        // Write pitch value array
                        if (candidate.PitchValues.Length != header.PitchCount)
                        {
                            string message = string.Format(CultureInfo.InvariantCulture,
                                "Invalid pitch count: Wanted=[{0}], Real=[{1}]",
                                header.PitchCount, candidate.PitchValues.Length);
                            throw new InvalidDataException(message);
                        }

                        foreach (float pitchValue in candidate.PitchValues)
                        {
                            // Write pitch value
                            bw.Write(pitchValue);
                            dataLength += sizeof(float);
                        }
                    }
                }

                // Re-write data length: not count FormatTag and Length fields
                // FormatTag (int) : 4 bytes
                // Length (int): 4 bytes
                dataLength -= sizeof(int) + sizeof(int);
                bw.Seek(4, SeekOrigin.Begin);
                bw.Write(dataLength);
            }
        }

        /// <summary>
        /// Get max/min value.
        /// </summary>
        /// <typeparam name="T1">Type of value.</typeparam>
        /// <param name="currentValue">Current value.</param>
        /// <param name="maxValue">Maximum value.</param>
        /// <param name="minValue">Minimum value.</param>
        private static void GetMaxMinValue<T1>(T1 currentValue, ref T1 maxValue, ref T1 minValue)
            where T1 : IComparable
        {
            if (currentValue.CompareTo(minValue) < 0)
            {
                minValue = currentValue;
            }

            if (currentValue.CompareTo(maxValue) > 0)
            {
                maxValue = currentValue;
            }
        }

        #endregion
    }
}