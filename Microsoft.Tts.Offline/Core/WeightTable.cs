//----------------------------------------------------------------------------
// <copyright file="WeightTable.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Weigh Table, which is used to guide unit selection
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Config;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// APL file header serialization data block.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct WeightTableHeaderSerial
    {
        #region Fields

        internal uint Tag;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        internal byte[] FormatTag;
        internal uint Size;
        internal uint Version;
        internal uint Build;
        internal uint LangNumber;
        internal uint DataOffset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        internal byte[] Paddings;

        #endregion

        #region Operations

        /// <summary>
        /// Read a WeightTableHeaderSerial block from bindary stream.
        /// </summary>
        /// <param name="br">Binary reader to read data for weight table header.</param>
        /// <returns>Weight table header serial.</returns>
        public static WeightTableHeaderSerial Read(BinaryReader br)
        {
            int size = Marshal.SizeOf(typeof(WeightTableHeaderSerial));
            byte[] buff = br.ReadBytes(size);

            if (buff.Length != size)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Malformed data found, for there is no enough data for weight table header.");
                throw new InvalidDataException(message);
            }

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(buff, 0, ptr, size);
                return (WeightTableHeaderSerial)Marshal.PtrToStructure(ptr,
                    typeof(WeightTableHeaderSerial));
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
            byte[] buff = new byte[Marshal.SizeOf(typeof(WeightTableHeaderSerial))];

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
    /// Weight table.
    /// </summary>
    public class WeightTable
    {
        #region Fields

        private Language _language;
        private EngineType _engine;
        private int _langId;
        private int _phoneCount;
        private int _toneCount;

        // target cost
        private Dictionary<TtsFeature, float[][]> _targetCosts;

        private float[] _weightTargetCost;

        // concatenate cost
        private float[][] _notContinueConcateCost;
        private float[] _weightConcateCost;

        // global cost
        private float _totalTargetCostWeight;
        private float _totalConcateCostWeight;

        private float[] _weightSmoothCostComponent;

        private string _filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeightTable"/> class.
        /// </summary>
        /// <param name="language">Language of the weight table.</param>
        /// <param name="engine">Engine type.</param>
        public WeightTable(Language language, EngineType engine)
        {
            _language = language;
            _engine = engine;

            _targetCosts = new Dictionary<TtsFeature, float[][]>();
        }

        /// <summary>
        /// Gets or sets FilePath.
        /// </summary>
        public string FilePath
        {
            get
            {
                return _filePath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _filePath = value;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets RightToneTargetCost.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore.")]
        public float[][] RightToneTargetCost
        {
            get { return GetTargetCost(TtsFeature.RightContextTone); }
            set { SetTargetCost(TtsFeature.RightContextTone, value); }
        }

        /// <summary>
        /// Gets or sets LeftToneTargetCost.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore.")]
        public float[][] LeftToneTargetCost
        {
            get { return GetTargetCost(TtsFeature.LeftContextTone); }
            set { SetTargetCost(TtsFeature.LeftContextTone, value); }
        }

        /// <summary>
        /// Gets or sets TotalConcateCostWeight.
        /// </summary>
        public float TotalConcateCostWeight
        {
            get { return _totalConcateCostWeight; }
            set { _totalConcateCostWeight = value; }
        }

        /// <summary>
        /// Gets or sets TotalTargetCostWeight.
        /// </summary>
        public float TotalTargetCostWeight
        {
            get { return _totalTargetCostWeight; }
            set { _totalTargetCostWeight = value; }
        }

        /// <summary>
        /// Gets or sets WeightConcateCost.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore.")]
        public float[] WeightConcateCost
        {
            get
            {
                return _weightConcateCost;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _weightConcateCost = value;
            }
        }

        /// <summary>
        /// Gets or sets NotContinueConcateCost.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore.")]
        public float[][] NotContinueConcateCost
        {
            get
            {
                return _notContinueConcateCost;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _notContinueConcateCost = value;
            }
        }

        /// <summary>
        /// Gets or sets RightPhoneTargetCost.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore.")]
        public float[][] RightPhoneTargetCost
        {
            get { return GetTargetCost(TtsFeature.RightContextPhone); }
            set { SetTargetCost(TtsFeature.RightContextPhone, value); }
        }

        /// <summary>
        /// Gets or sets LeftPhoneTargetCost.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore.")]
        public float[][] LeftPhoneTargetCost
        {
            get { return GetTargetCost(TtsFeature.LeftContextPhone); }
            set { SetTargetCost(TtsFeature.LeftContextPhone, value); }
        }

        /// <summary>
        /// Gets or sets WeightTargetCost.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore.")]
        public float[] WeightTargetCost
        {
            get
            {
                return _weightTargetCost;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _weightTargetCost = value;
            }
        }

        /// <summary>
        /// Gets PosInSentWeight.
        /// </summary>
        public float PosInSentWeight
        {
            get { return _weightTargetCost[0]; }
        }

        /// <summary>
        /// Gets PosInWordWeight.
        /// </summary>
        public float PosInWordWeight
        {
            get { return _weightTargetCost[1]; }
        }

        /// <summary>
        /// Gets PosInSyllWeight.
        /// </summary>
        public float PosInSyllWeight
        {
            get { return _weightTargetCost[2]; }
        }

        /// <summary>
        /// Gets LeftPhoneWeight.
        /// </summary>
        public float LeftPhoneWeight
        {
            get { return _weightTargetCost[3]; }
        }

        /// <summary>
        /// Gets RightPhoneWeight.
        /// </summary>
        public float RightPhoneWeight
        {
            get { return _weightTargetCost[4]; }
        }

        /// <summary>
        /// Gets LeftToneWeight.
        /// </summary>
        public float LeftToneWeight
        {
            get { return _weightTargetCost[5]; }
        }

        /// <summary>
        /// Gets RightToneWeight.
        /// </summary>
        public float RightToneWeight
        {
            get { return _weightTargetCost[6]; }
        }

        /// <summary>
        /// Gets StessWeight.
        /// </summary>
        public float StessWeight
        {
            get { return _weightTargetCost[7]; }
        }

        /// <summary>
        /// Gets EmphasisWeight.
        /// </summary>
        public float EmphasisWeight
        {
            get { return _weightTargetCost[8]; }
        }

        /// <summary>
        /// Gets or sets EmphasisTargetCost.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore.")]
        public float[][] EmphasisTargetCost
        {
            get { return GetTargetCost(TtsFeature.TtsEmphasis); }
            set { SetTargetCost(TtsFeature.TtsEmphasis, value); }
        }

        /// <summary>
        /// Gets or sets StressTargetCost.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore.")]
        public float[][] StressTargetCost
        {
            get { return GetTargetCost(TtsFeature.TtsStress); }
            set { SetTargetCost(TtsFeature.TtsStress, value); }
        }

        /// <summary>
        /// Gets or sets PosInSentTargetCost.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore.")]
        public float[][] PosInSentTargetCost
        {
            get { return GetTargetCost(TtsFeature.PosInSentence); }
            set { SetTargetCost(TtsFeature.PosInSentence, value); }
        }

        /// <summary>
        /// Gets or sets PosInWordTargetCost.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore.")]
        public float[][] PosInWordTargetCost
        {
            get { return GetTargetCost(TtsFeature.PosInWord); }
            set { SetTargetCost(TtsFeature.PosInWord, value); }
        }

        /// <summary>
        /// Gets or sets PosInSyllTargetCost.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore.")]
        public float[][] PosInSyllTargetCost
        {
            get { return GetTargetCost(TtsFeature.PosInSyllable); }
            set { SetTargetCost(TtsFeature.PosInSyllable, value); }
        }

        /// <summary>
        /// Gets or sets ToneCount.
        /// </summary>
        public int ToneCount
        {
            get { return _toneCount; }
            set { _toneCount = value; }
        }

        /// <summary>
        /// Gets or sets PhoneCount.
        /// </summary>
        public int PhoneCount
        {
            get { return _phoneCount; }
            set { _phoneCount = value; }
        }

        /// <summary>
        /// Gets or sets LangId.
        /// </summary>
        public int LangId
        {
            get { return _langId; }
            set { _langId = value; }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Save weight table to apl file.
        /// </summary>
        /// <param name="filePath">Target file path to save weight table data.</param>
        /// <param name="buildNumber">Voice font build number.</param>
        public void SaveAsApl(string filePath, FontBuildNumber buildNumber)
        {
            Helper.EnsureFolderExistForFile(filePath);
            if (buildNumber == null)
            {
                throw new ArgumentNullException("buildNumber");
            }

            FileStream fs = new FileStream(filePath, FileMode.Create);
            try
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    fs = null;
                    uint size = (uint)Marshal.SizeOf(typeof(WeightTableHeaderSerial));

                    // Write header
                    WeightTableHeaderSerial aplHeader = new WeightTableHeaderSerial();
                    aplHeader.Tag = (uint)FontSectionTag.WeightTable;

                    ////sizeFieldOffset = sizeof(aplHeader.Tag + aplHeader.FormatTag)
                    int sizeFieldOffset = sizeof(uint) + aplHeader.FormatTag.Length;
                    aplHeader.Size = 0; // Update after all other sections have been written.
                    aplHeader.Version = (uint)FormatVersion.Tts30;
                    aplHeader.Build = (uint)buildNumber.ToInt32();
                    aplHeader.LangNumber = 1;
                    aplHeader.DataOffset = size;

                    bw.Write(aplHeader.ToBytes());

                    // Write Section
                    size += SaveSection(bw);

                    // Update "size" field in the header to sizeof(Tag + FormatTag + Size).
                    size -= sizeof(uint) + sizeof(uint) + (uint)aplHeader.FormatTag.Length;
                    bw.Seek(sizeFieldOffset, SeekOrigin.Begin);
                    bw.Write(size);
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
        /// Save weight table to apl file.
        /// </summary>
        /// <param name="filePath">The target location of the file to save into.</param>
        public void SaveAsApl(string filePath)
        {
            FontBuildNumber buildNumber = new FontBuildNumber();
            SaveAsApl(filePath, buildNumber);
        }

        /// <summary>
        /// Load text file of weight table configuration.
        /// </summary>
        /// <param name="filePath">Weight table file path.</param>
        public void Load(string filePath)
        {
            Dictionary<string, DataSection> sections = null;
            using (StreamReader sr = new StreamReader(filePath))
            {
                sections = LoadDataSections(sr);
            }

            _filePath = filePath;

            _langId = int.Parse(sections["<LangId>"].Lines[0], CultureInfo.InvariantCulture);
            System.Diagnostics.Debug.Assert(_langId == Localor.MapLanguageId(_language));

            // _PhoneCount = int.Parse(sections["<PhoneNum>"].Lines[0], CultureInfo.InvariantCulture);
            _toneCount = int.Parse(sections["<ToneNum>"].Lines[0], CultureInfo.InvariantCulture);

            float[][] posInSyllTargetCost = ParseSection2(sections, "<TargetCostPosInSyl>");
            if (posInSyllTargetCost != null)
            {
                _targetCosts.Add(TtsFeature.PosInSyllable, posInSyllTargetCost);
            }

            float[][] posInWordTargetCost = ParseSection2(sections, "<TargetCostPosInWord>");
            if (posInWordTargetCost != null)
            {
                _targetCosts.Add(TtsFeature.PosInWord, posInWordTargetCost);
            }

            float[][] posInSentTargetCost = ParseSection2(sections, "<TargetCostPosInSent>");
            if (posInSentTargetCost != null)
            {
                _targetCosts.Add(TtsFeature.PosInSentence, posInSentTargetCost);
            }

            float[][] leftPhoneTargetCost = ParseSection2(sections, "<TargetCostLeftPhone>");
            if (leftPhoneTargetCost != null)
            {
                _targetCosts.Add(TtsFeature.LeftContextPhone, leftPhoneTargetCost);
            }

            float[][] rightPhoneTargetCost = ParseSection2(sections, "<TargetCostRightPhone>");
            if (rightPhoneTargetCost != null)
            {
                _targetCosts.Add(TtsFeature.RightContextPhone, rightPhoneTargetCost);
            }

            System.Diagnostics.Debug.Assert(leftPhoneTargetCost.Length == rightPhoneTargetCost.Length);
            _phoneCount = leftPhoneTargetCost.Length;

            float[][] leftToneTargetCost = ParseSection2(sections, "<TargetCostLeftTone>");
            if (leftToneTargetCost != null)
            {
                _targetCosts.Add(TtsFeature.LeftContextTone, leftToneTargetCost);
            }

            float[][] rightToneTargetCost = ParseSection2(sections, "<TargetCostRightTone>");
            if (rightToneTargetCost != null)
            {
                _targetCosts.Add(TtsFeature.RightContextTone, rightToneTargetCost);
            }

            float[][] stressTargetCost = ParseSection2(sections, "<TargetCostStress>");
            if (stressTargetCost != null)
            {
                _targetCosts.Add(TtsFeature.TtsStress, stressTargetCost);
            }

            float[][] emphasisTargetCost = ParseSection2(sections, "<TargetCostEmph>");
            if (emphasisTargetCost != null)
            {
                _targetCosts.Add(TtsFeature.TtsEmphasis, emphasisTargetCost);
            }

            _weightSmoothCostComponent = ParseSection(sections, "<WeightSmoothCostComponent>");

            _notContinueConcateCost = ParseSection2(sections, "<SmoothCostNotContin>");

            _weightTargetCost = ParseSection(sections, "<WeightTargetCostComponent>");

            _totalTargetCostWeight =
                float.Parse(sections["<WeightTargetCost>"].Lines[0], CultureInfo.InvariantCulture);
            _totalConcateCostWeight =
                float.Parse(sections["<WeightSmoothCost>"].Lines[0], CultureInfo.InvariantCulture);
        }

        #endregion

        #region Internal operations

        /// <summary>
        /// Calculate the target cost.
        /// </summary>
        /// <param name="condidate">Feature of condidate unit.</param>
        /// <param name="target">Feature of target unit to find.</param>
        /// <returns>Cost.</returns>
        internal float CalcTargetCost(TtsUnitFeature condidate, TtsUnitFeature target)
        {
            float targetCost = 0.0f;

            // position cost: in sentence, in word and in syllable
            targetCost = CalcTargetCostPosInSentence(condidate.PosInSentence,
                target.PosInSentence);

            targetCost += CalcTargetCostPosInWord(condidate.PosInWord,
                target.PosInWord);

            targetCost += CalcTargetCostPosInSyl(condidate.PosInSyllable,
                target.PosInSyllable);

            // phone context cost: left phone and right phone
            targetCost += CalcTargetCostLeftContextPhone(condidate.LeftContextPhone,
                target.LeftContextPhone);

            targetCost += CalcTargetCostRightContextPhone(condidate.RightContextPhone,
                target.RightContextPhone);

            // tone context cost: left tone and right tone
            targetCost += CalcTargetCostLeftContextTone(condidate.LeftContextTone,
                target.LeftContextTone);

            targetCost += CalcTargetCostRightContextTone(condidate.RightContextTone,
                target.RightContextTone);

            // stress cost
            targetCost += CalcTargetCostTtsStress(condidate.TtsStress,
                target.TtsStress);

            // emphasis cost
            targetCost += CalcTargetCostTtsEmphasis(condidate.TtsEmphasis,
                target.TtsEmphasis);

            targetCost = targetCost * _totalTargetCostWeight;

            return targetCost;
        }

        /// <summary>
        /// Calculate the smoothing cost of not continued for break.
        /// </summary>
        /// <param name="ttsBreak">Break level.</param>
        /// <param name="jointTypeIndex">Joint type index of this break instance.</param>
        /// <returns>Cost.</returns>
        internal float CalcNotContinuedSmoothCost(TtsBreak ttsBreak, int jointTypeIndex)
        {
            return _notContinueConcateCost[(int)ttsBreak][jointTypeIndex] * _totalConcateCostWeight;
        }

        #endregion

        #region Private static operations

        /// <summary>
        /// Write float arrays to binary stream.
        /// </summary>
        /// <param name="bw">Binary writer to save data into.</param>
        /// <param name="arrays">Float arrays.</param>
        /// <returns>Total written bytes.</returns>
        private static uint WriteFloatArrays(BinaryWriter bw, float[][] arrays)
        {
            uint size = 0;

            if (arrays == null || arrays.Length == 0)
            {
                bw.Write((uint)0);
                size += sizeof(uint);
            }
            else
            {
                // Write number.
                bw.Write(arrays.Length * arrays[0].Length);
                size += sizeof(uint);

                // Write weighted cost.
                foreach (float[] arr in arrays)
                {
                    foreach (float val in arr)
                    {
                        bw.Write(val);
                        size += sizeof(float);
                    }
                }
            }

            return size;
        }

        /// <summary>
        /// Parse data section.
        /// </summary>
        /// <param name="sections">Sessions to get data from.</param>
        /// <param name="sectionName">Session name.</param>
        /// <returns>Float array.</returns>
        private static float[] ParseSection(Dictionary<string, DataSection> sections, string sectionName)
        {
            if (!sections.ContainsKey(sectionName) || sections[sectionName].Lines.Count == 0)
            {
                return null;
            }

            DataSection section = sections[sectionName];
            string line = section.Lines[0];
            string[] items = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            float[] weights = new float[items.Length];
            for (int j = 0; j < items.Length; j++)
            {
                weights[j] = float.Parse(items[j], CultureInfo.InvariantCulture);
            }

            return weights;
        }

        /// <summary>
        /// Parse session.
        /// </summary>
        /// <param name="sections">Sessions to get data from.</param>
        /// <param name="sectionName">Session name.</param>
        /// <returns>Float arrays.</returns>
        private static float[][] ParseSection2(Dictionary<string, DataSection> sections, string sectionName)
        {
            if (!sections.ContainsKey(sectionName) || sections[sectionName].Lines.Count == 0)
            {
                return null;
            }

            DataSection section = sections[sectionName];
            float[][] weights = new float[section.Lines.Count][];
            for (int i = 0; i < section.Lines.Count; i++)
            {
                string line = section.Lines[i];
                string[] items = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                weights[i] = new float[items.Length];
                for (int j = 0; j < items.Length; j++)
                {
                    weights[i][j] = float.Parse(items[j].Replace("f", string.Empty), CultureInfo.InvariantCulture);
                }
            }

            return weights;
        }

        /// <summary>
        /// Remove comment from line.
        /// </summary>
        /// <param name="line">Line to remove comment.</param>
        /// <returns>Line with comment removed.</returns>
        private static string RemoveComment(string line)
        {
            line = line.Trim();
            line = Regex.Replace(line, "//.*", string.Empty);
            line = Regex.Replace(line, @"/\*.*\*/", string.Empty);

            return line;
        }

        /// <summary>
        /// Write weighted cost table to binary stream.
        /// </summary>
        /// <param name="bw">Binary writer to save data into.</param>
        /// <param name="table">Cost table.</param>
        /// <param name="weight">Weight.</param>
        /// <returns>Total written bytes.</returns>
        private static uint WriteWeightTable(BinaryWriter bw, float[][] table, float weight)
        {
            uint size = 0;

            if (table != null && table.Length != 0)
            {
                // Multiple weight and each item in cost table.
                foreach (float[] arr in table)
                {
                    for (int i = 0; i < arr.Length; i++)
                    {
                        arr[i] *= weight;
                    }
                }

                size = WriteFloatArrays(bw, table);
            }
            else
            {
                bw.Write((uint)0);
                size += sizeof(uint);
            }

            return size;
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Write one section to binary stream.
        /// </summary>
        /// <param name="bw">Binary writer to save section into.</param>
        /// <returns>Total written bytes.</returns>
        private uint SaveSection(BinaryWriter bw)
        {
            uint size = 0;

            // Language header
            bw.Write((uint)_language);
            size += sizeof(uint);

            uint speakerId = 0;
            bw.Write((uint)speakerId);
            size += sizeof(uint);

            uint deployEnv = 0;
            bw.Write((uint)deployEnv);
            size += sizeof(uint);

            // Target cost tables
            // The nubmer of target cost tables
            // NOTE: update this field after write all target cost tables.
            int targetCostTableCount = 0;
            int costTableOffset = (int)bw.Seek(0, SeekOrigin.Current);
            bw.Write((uint)targetCostTableCount);
            size += sizeof(uint);

            FeatureMeta featureMeta = Localor.GetFeatureMeta(_language);
            float[][] targetCostTable;
            foreach (TtsFeature featureId in featureMeta.Metadata.Keys)
            {
                if (_targetCosts.ContainsKey(featureId))
                {
                    targetCostTable = _targetCosts[featureId];
                }
                else
                {
                    targetCostTable = null;
                }

                if (targetCostTable != null && ((int)featureId) < _weightTargetCost.Length)
                {
                    targetCostTableCount++;

                    bw.Write((uint)featureId);
                    size += sizeof(uint);

                    float weight = _totalTargetCostWeight * _weightTargetCost[(int)featureId];
                    size += WriteWeightTable(bw, targetCostTable, weight);
                }
            }

            // Update the nubmer of target cost tables.
            int currOffset = (int)bw.Seek(0, SeekOrigin.Current);
            bw.Seek(costTableOffset, SeekOrigin.Begin);
            bw.Write((uint)targetCostTableCount);
            bw.Seek(currOffset, SeekOrigin.Begin);

            // Joint cost table
            size += WriteWeightTable(bw, NotContinueConcateCost, _totalConcateCostWeight);

            return size;
        }

        /// <summary>
        /// Load all data section from stream.
        /// </summary>
        /// <param name="sr">Stream reader of weight table file to load data sessions.</param>
        /// <returns>Data sessions loaded.</returns>
        private Dictionary<string, DataSection> LoadDataSections(StreamReader sr)
        {
            Dictionary<string, DataSection> sections = new Dictionary<string, DataSection>();

            string line;
            while ((line = sr.ReadLine()) != null)
            {
                line = RemoveComment(line);
                while (!string.IsNullOrEmpty(line))
                {
                    line = LoadOneSection(sr, sections, line);
                }
            }

            return sections;
        }

        /// <summary>
        /// Load one data section from stream.
        /// </summary>
        /// <param name="sr">Stream reader of the weight table file.</param>
        /// <param name="sections">Session dictionary to save.</param>
        /// <param name="sectionName">Session name.</param>
        /// <returns>Next line not in current session.</returns>
        private string LoadOneSection(StreamReader sr,
            Dictionary<string, DataSection> sections, string sectionName)
        {
            const float DefualtValue = 1000f;
            System.Diagnostics.Debug.Assert(sectionName.StartsWith("<", StringComparison.Ordinal));

            string line = null;
            string[] phones = null;
            if (sectionName == "<TargetCostLeftPhone>" || sectionName == "<TargetCostRightPhone>")
            {
                // Read the first Comment line. It is phone define.
                while ((line = sr.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        break;
                    }
                }

                phones = line.Split("\t/ ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }

            DataSection section = new DataSection();
            while ((line = sr.ReadLine()) != null)
            {
                line = RemoveComment(line);
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (line.StartsWith("<", StringComparison.Ordinal))
                {
                    break;
                }

                section.Lines.Add(line);
            }

            if (phones != null)
            {
                DataSection oldSection = section;
                section = new DataSection();

                Dictionary<string, Dictionary<string, float>> values =
                    new Dictionary<string, Dictionary<string, float>>();

                Phoneme phoneme = Localor.GetPhoneme(_language, _engine);
                foreach (string ttsPhone in phoneme.TtsPhoneIds.Keys)
                {
                    values.Add(ttsPhone, new Dictionary<string, float>());
                    foreach (string ttsPhone2 in phoneme.TtsPhoneIds.Keys)
                    {
                        values[ttsPhone].Add(ttsPhone2, DefualtValue);
                    }
                }

                int i = 0;
                foreach (string valueLine in oldSection.Lines)
                {
                    string[] str = valueLine.Split(" \t".ToCharArray(),
                        StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < str.Length; ++j)
                    {
                        values[phones[i]][phones[j]] = 
                            float.Parse(str[j].Replace("f", string.Empty), CultureInfo.InvariantCulture);
                    }

                    ++i;
                }

                int maxId = GetLastTtsPhoneId();
                for (int sourcePhoneId = 0; sourcePhoneId <= maxId; ++sourcePhoneId)
                {
                    string sourcePhone = phoneme.TtsId2Phone(sourcePhoneId);
                    StringBuilder oneLine = new StringBuilder();
                    for (int targetPhoneId = 0; targetPhoneId <= maxId; ++targetPhoneId)
                    {
                        string targetPhone = phoneme.TtsId2Phone(targetPhoneId);
                        if (!string.IsNullOrEmpty(sourcePhone) && 
                            !string.IsNullOrEmpty(targetPhone) &&
                            values.ContainsKey(sourcePhone) &&
                            values[sourcePhone].ContainsKey(targetPhone))
                        {
                            oneLine.AppendFormat(CultureInfo.InvariantCulture,
                                "{0} ", values[sourcePhone][targetPhone]);
                        }
                        else
                        {
                            oneLine.AppendFormat(CultureInfo.InvariantCulture,
                                "{0} ", DefualtValue);
                        }
                    }

                    section.Lines.Add(oneLine.ToString());
                }
            }

            section.Name = sectionName;
            sections.Add(section.Name, section);

            return line;
        }

        /// <summary>
        /// Get the last phone id of phoneme for current language.
        /// </summary>
        /// <returns>Phone id.</returns>
        private int GetLastTtsPhoneId()
        {
            Phoneme phoneme = Localor.GetPhoneme(_language, _engine);
            int max = 0;
            foreach (int id in phoneme.TtsPhoneIds.Values)
            {
                max = Math.Max(max, id);
            }

            return max;
        }

        /// <summary>
        /// Calculate the target cost for emphasis feature.
        /// </summary>
        /// <param name="src">Source TtsEmphasis feature.</param>
        /// <param name="target">Target TtsEmphasis feature.</param>
        /// <returns>Cost.</returns>
        private float CalcTargetCostTtsEmphasis(TtsEmphasis src, TtsEmphasis target)
        {
            return CalcTargetCost(TtsFeature.TtsEmphasis, (int)src, (int)target);
        }

        /// <summary>
        /// Calculate the target cost for stress feature.
        /// </summary>
        /// <param name="src">Source TtsStress feature.</param>
        /// <param name="target">Target TtsStress feature.</param>
        /// <returns>Cost.</returns>
        private float CalcTargetCostTtsStress(TtsStress src, TtsStress target)
        {
            return CalcTargetCost(TtsFeature.TtsStress, (int)src, (int)target);
        }

        /// <summary>
        /// Calculate the target cost for right context tone feature.
        /// </summary>
        /// <param name="src">Source RightContextTone feature.</param>
        /// <param name="target">Target RightContextTone feature.</param>
        /// <returns>Cost.</returns>
        private float CalcTargetCostRightContextTone(int src, int target)
        {
            return CalcTargetCost(TtsFeature.RightContextTone, src, target);
        }

        /// <summary>
        /// Calculate the target cost for left context tone feature.
        /// </summary>
        /// <param name="src">Source CostLeftContextTone feature.</param>
        /// <param name="target">Target CostLeftContextTone feature.</param>
        /// <returns>Cost.</returns>
        private float CalcTargetCostLeftContextTone(int src, int target)
        {
            return CalcTargetCost(TtsFeature.LeftContextTone, src, target);
        }

        /// <summary>
        /// Calculate the target cost for right context phone feature.
        /// </summary>
        /// <param name="src">Source RightContextPhone feature.</param>
        /// <param name="target">Target RightContextPhone feature.</param>
        /// <returns>Cost.</returns>
        private float CalcTargetCostRightContextPhone(int src, int target)
        {
            return CalcTargetCost(TtsFeature.RightContextPhone, src, target);
        }

        /// <summary>
        /// Calculate the target cost for left context phone feature.
        /// </summary>
        /// <param name="src">Source LeftContextPhone feature.</param>
        /// <param name="target">Target LeftContextPhone feature.</param>
        /// <returns>Cost.</returns>
        private float CalcTargetCostLeftContextPhone(int src, int target)
        {
            return CalcTargetCost(TtsFeature.LeftContextPhone, src, target);
        }

        /// <summary>
        /// Calculate the target cost for position in syllable feature.
        /// </summary>
        /// <param name="src">Source PosInSyllable feature.</param>
        /// <param name="target">Target PosInSyllable feature.</param>
        /// <returns>Cost.</returns>
        private float CalcTargetCostPosInSyl(PosInSyllable src, PosInSyllable target)
        {
            return CalcTargetCost(TtsFeature.PosInSyllable, (int)src, (int)target);
        }

        /// <summary>
        /// Calculate the target cost for position in word feature.
        /// </summary>
        /// <param name="src">Source PosInWord feature.</param>
        /// <param name="target">Target PosInWord feature.</param>
        /// <returns>Cost.</returns>
        private float CalcTargetCostPosInWord(PosInWord src, PosInWord target)
        {
            return CalcTargetCost(TtsFeature.PosInWord, (int)src, (int)target);
        }

        /// <summary>
        /// Calculate the target cost for position in sentence feature.
        /// </summary>
        /// <param name="src">Source PosInSentence feature.</param>
        /// <param name="target">Target PosInSentence feature.</param>
        /// <returns>Cost.</returns>
        private float CalcTargetCostPosInSentence(PosInSentence src, PosInSentence target)
        {
            return CalcTargetCost(TtsFeature.PosInSentence, (int)src, (int)target);
        }

        /// <summary>
        /// Get target cost table.
        /// </summary>
        /// <param name="featureId">Feature id.</param>
        /// <returns>Target cost table.</returns>
        private float[][] GetTargetCost(TtsFeature featureId)
        {
            float[][] costTable = null;
            if (_targetCosts.ContainsKey(featureId))
            {
                costTable = _targetCosts[featureId];
            }

            return costTable;
        }

        /// <summary>
        /// Set target cost table.
        /// </summary>
        /// <param name="featureId">Feature id.</param>
        /// <param name="costTable">Target cost table.</param>
        private void SetTargetCost(TtsFeature featureId, float[][] costTable)
        {
            if (costTable == null)
            {
                throw new ArgumentNullException("costTable");
            }

            if (_targetCosts.ContainsKey(featureId))
            {
                _targetCosts[featureId] = costTable;
            }
            else
            {
                _targetCosts.Add(featureId, costTable);
            }
        }

        /// <summary>
        /// Calculate the target cost.
        /// </summary>
        /// <param name="featureId">Feature id.</param>
        /// <param name="src">Source.</param>
        /// <param name="target">Target.</param>
        /// <returns>Cost.</returns>
        private float CalcTargetCost(TtsFeature featureId, int src, int target)
        {
            float cost = 0;
            if (_targetCosts.ContainsKey(featureId))
            {
                cost = _targetCosts[featureId][src][target];
            }

            return cost;
        }

        #endregion
    }
}