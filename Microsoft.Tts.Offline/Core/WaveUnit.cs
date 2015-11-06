//----------------------------------------------------------------------------
// <copyright file="WaveUnit.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements wave unit class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// Wave unit definition, which are represented for a wave segment
    /// In speech database.
    /// </summary>
    public class WaveUnit
    {
        #region Fields

        private static VoiceFont _voiceFont;

        private TtsUnitFeature _features;

        private string _name;
        private string _sentenceId;
        private int _indexInSentence;

        private long _sampleOffset;             // in whole database
        private long _epochOffset;              // in whole database

        private int _sampleOffsetInSentence;
        private int _epochOffsetInSentence;

        private long _sampleLength;
        private int _epochLength;               // each epoch data point in 4-byte

        private int _epoch16KCompressLength;    // each epoch data point in 1-byte
        private int _epoch8KCompressLength;     // each epoch data point in 1-byte

        private string _relativePath;

        private bool _neighborPre;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Voice font, currently work on.
        /// </summary>
        public static VoiceFont VoiceFont
        {
            get
            {
                return _voiceFont;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _voiceFont = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this unit is neighbor to the previous unit.
        /// </summary>
        public bool NeighborPre
        {
            get { return _neighborPre; }
            set { _neighborPre = value; }
        }

        /// <summary>
        /// Gets Wave unit key, "sentence id" + " " + "IndexInSentence".
        /// </summary>
        public string Key
        {
            get { return SentenceId + " " + IndexInSentence.ToString(CultureInfo.InvariantCulture); }
        }

        /// <summary>
        /// Gets or sets Tts unit feature.
        /// </summary>
        public TtsUnitFeature Features
        {
            get
            {
                return _features;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _features = value;
            }
        }

        /// <summary>
        /// Gets or sets Unit name.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _name = value;
            }
        }

        /// <summary>
        /// Gets or sets Sentence relative path, the second part/column in the file-list map file
        /// <param />
        /// <example>
        /// 20271 Alphabet/20271
        /// MNNO0001CD1 MNNO0001CD1.
        /// </example>
        /// </summary>
        public string RelativePath
        {
            get
            {
                return _relativePath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _relativePath = value;
            }
        }

        /// <summary>
        /// Gets or sets Length of the 8K Hz compressed epoch data of this unit.
        /// </summary>
        public int Epoch8KCompressLength
        {
            get { return _epoch8KCompressLength; }
            set { _epoch8KCompressLength = value; }
        }

        /// <summary>
        /// Gets or sets Length of the 16K Hz compressed epoch data of this unit.
        /// </summary>
        public int Epoch16KCompressLength
        {
            get { return _epoch16KCompressLength; }
            set { _epoch16KCompressLength = value; }
        }

        /// <summary>
        /// Gets or sets Offset of epoch of this unit over the whole epoch file in voice font.
        /// </summary>
        public long EpochOffset
        {
            get { return _epochOffset; }
            set { _epochOffset = value; }
        }

        /// <summary>
        /// Gets or sets Length of the uncompressed epoch data of this unit.
        /// </summary>
        public int EpochLength
        {
            get { return _epochLength; }
            set { _epochLength = value; }
        }

        /// <summary>
        /// Gets or sets Offset of epoch of this unit over the epoch file in the sentence.
        /// </summary>
        public int EpochOffsetInSentence
        {
            get { return _epochOffsetInSentence; }
            set { _epochOffsetInSentence = value; }
        }

        /// <summary>
        /// Gets or sets Sample offset of wave of this unit over the wave file in the sentence.
        /// </summary>
        public int SampleOffsetInSentence
        {
            get { return _sampleOffsetInSentence; }
            set { _sampleOffsetInSentence = value; }
        }

        /// <summary>
        /// Gets or sets Sample length of wave of this unit .
        /// </summary>
        public long SampleLength
        {
            get { return _sampleLength; }
            set { _sampleLength = value; }
        }

        /// <summary>
        /// Gets or sets Sample offset of this unit in whole speech database.
        /// </summary>
        public long SampleOffset
        {
            get { return _sampleOffset; }
            set { _sampleOffset = value; }
        }

        /// <summary>
        /// Gets or sets Slice index number in the sentence starting from 0, excluding Phoneme.Silence slice.
        /// </summary>
        /// <value></value>
        public int IndexInSentence
        {
            get { return _indexInSentence; }
            set { _indexInSentence = value; }
        }

        /// <summary>
        /// Gets or sets Sentence identity.
        /// </summary>
        public string SentenceId
        {
            get
            {
                return _sentenceId;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _sentenceId = value;
            }
        }

        #endregion

        #region Static operations

        /// <summary>
        /// Read all wave units from unit feature file, usual UnitFeature.xml.
        /// </summary>
        /// <param name="waveUnits">Wave unit dictionary.</param>
        /// <param name="featureFilePath">Unit feature file path.</param>
        /// <param name="samplesPerSecond">Samples per second.</param>
        public static void ReadAllData(Dictionary<long, WaveUnit> waveUnits,
            string featureFilePath, int samplesPerSecond)
        {
            if (waveUnits == null)
            {
                throw new ArgumentNullException("waveUnits");
            }

            if (string.IsNullOrEmpty(featureFilePath))
            {
                throw new ArgumentNullException("featureFilePath");
            }

            if (samplesPerSecond != 8000 && samplesPerSecond != 16000)
            {
                string message = Helper.NeutralFormat("Invalid sampling rate [{0}]," +
                    "Valid sample rate should be 8 kHz or 16 kHz", samplesPerSecond);
                throw new ArgumentException(message);
            }

            UnitFeatureFile featureFile = new UnitFeatureFile(featureFilePath);

            long sampleOffset = 0;
            long epochOffset = 0;

            foreach (string key in featureFile.Units.Keys)
            {
                UnitFeature unit = featureFile.Units[key];

                WaveUnit wu = new WaveUnit();
                wu.LoadWui(unit, samplesPerSecond);

                wu.SampleOffset = sampleOffset;
                wu.EpochOffset = epochOffset;

                epochOffset += wu.EpochLength;
                sampleOffset += wu.SampleLength;

                if (wu.SampleLength == 0)
                {
                    continue;
                }
                else
                {
                    waveUnits.Add(wu.SampleOffset, wu);
                }
            }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Parse text line in wave segment sequence file, usual WaveSegSeq.vec
        /// <param />
        /// The definitions of each column are listed as following:
        /// <param />
        /// (sentence id) (unit index in sentence)
        /// (sample offset in sentence) (sample length)
        /// (epoch offset) (epoch length) (8K compressed epoch length) (16K compressed epoch length)
        /// (relative path) (unit name)
        /// <example>
        /// MNNO0001CD1   0    25998   1514    162   15   16   15 MNNO0001CD1       ax
        /// MNNO0001CD1   1    27512   3050    177   36   36   36 MNNO0001CD1     m+eh
        /// MNNO0001CD1   2    30562   1800    213   29   30   29 MNNO0001CD1     r+ax.
        /// </example>
        /// </summary>
        /// <param name="line">Line in wave segment sequence file.</param>
        public void ParseUntFile(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                throw new ArgumentNullException("line");
            }

            DoParseUntFile(line);
        }

        /// <summary>
        /// Indicate weather a unit is neighboring with this unit.
        /// </summary>
        /// <param name="waveUnit">Wave unit instance.</param>
        /// <returns>0 if neighbor with, -1 false, or else the distance.</returns>
        internal int IsBeNeighbor(WaveUnit waveUnit)
        {
            int delta = (int)(SampleOffset + SampleLength - waveUnit.SampleOffset);
            return (delta == 0) ? (waveUnit.NeighborPre ? 0 : -1) : delta;
        }

        /// <summary>
        /// Parse text line in wave segment sequence file, usual WaveSegSeq.vec.
        /// </summary>
        /// <param name="line">Line of wave segment.</param>
        private void DoParseUntFile(string line)
        {
            string[] items = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            Debug.Assert(items.Length == 10);

            _sentenceId = items[0];
            _indexInSentence = int.Parse(items[1], CultureInfo.InvariantCulture);
            _name = items[9];
            _sampleLength = uint.Parse(items[3], CultureInfo.InvariantCulture);

            _sampleOffsetInSentence = int.Parse(items[2], CultureInfo.InvariantCulture);
            _epochOffsetInSentence = int.Parse(items[4], CultureInfo.InvariantCulture);
            _epochLength = int.Parse(items[5], CultureInfo.InvariantCulture);
            _epoch16KCompressLength = int.Parse(items[6], CultureInfo.InvariantCulture);
            _epoch8KCompressLength = int.Parse(items[7], CultureInfo.InvariantCulture);
            _relativePath = items[8];
        }

        /// <summary>
        /// Load wave unit information.
        /// </summary>
        /// <param name="unitFeature">Unit feature item.</param>
        /// <param name="samplesPerSecond">Sample rate.</param>
        private void LoadWui(UnitFeature unitFeature, int samplesPerSecond)
        {
            _sentenceId = unitFeature.SentenceId;
            _indexInSentence = unitFeature.Index;
            _name = TtsUnit.BuildUnitName(unitFeature.Name);

            _sampleOffsetInSentence = unitFeature.AcousticFeature.SampleOffset;

            if (samplesPerSecond == (int)WaveSamplesPerSecond.Telephone)
            {
                // Sample rate is 8k Hz
                _sampleLength = unitFeature.AcousticFeature.Sample8KLength;
            }
            else
            {
                // Sample rate is 16k Hz
                Debug.Assert(samplesPerSecond == (int)WaveSamplesPerSecond.Desktop);
                _sampleLength = unitFeature.AcousticFeature.SampleLength;
            }

            _epochOffsetInSentence = unitFeature.AcousticFeature.EpochOffset;
            _epochLength = unitFeature.AcousticFeature.EpochLength;
            _epoch16KCompressLength = unitFeature.AcousticFeature.Epoch16KCompressLength;
            _epoch8KCompressLength = unitFeature.AcousticFeature.Epoch8KCompressLength;

            _features = unitFeature.LingusitcFeature;
        }

        #endregion
    }
}