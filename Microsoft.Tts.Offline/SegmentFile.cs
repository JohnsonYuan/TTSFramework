//----------------------------------------------------------------------------
// <copyright file="SegmentFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements segment file
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Segmentation point information
    /// In segmentation file, this is presented as (start time and label).
    /// <example>
    /// 0.10000 sil.
    /// </example>
    /// </summary>
    public class WaveSegment
    {
        #region Fields

        private const float InvalidTime = -1.0f;

        // basic information
        private string _label;          // segment label
        private double _startTime;       // in second

        // undefined, normally this value is the start time of next segment
        private double _endTime = InvalidTime;

        // confidence for automatic alignment
        private float _confidence;

        private object _tag;

        /// <summary>
        /// Signal metrics
        /// The mean of a signal x is defined as the average value of its samples:
        /// <![CDATA[
        ///     meanOf(x)
        ///     {
        ///         mean = 0;
        ///         for ( i = 0; i < n; i++)
        ///         {
        ///             mean += x[n];
        ///         }
        ///         mean = mean / n;
        ///         return mean;
        ///     }
        /// ]]>
        /// The total energy of a signal x is defined as the sum of squared moduli:
        /// <![CDATA[
        ///     energyOf(x)
        ///     {
        ///         energy = 0;
        ///         for ( i = 0; i < n; i++)
        ///         {
        ///             energy += x[n]^2;
        ///         }
        ///         return energy;
        ///     }
        /// ]]>
        /// The average power of a signal x is defined as the energy per sample:
        /// Signal power represents energy per sample. 
        /// <![CDATA[
        ///     averagePowerOf(x)
        ///     {
        ///         return energyOf(x) / n;
        ///     }
        /// ]]>
        /// The root mean square (RMS) level of a signal x
        /// <![CDATA[
        ///     rootMeanSquareOf(x)
        ///     {
        ///         return Math.Sqrt(averagePowerOf(x));
        ///     }
        /// ]]>
        /// The variance (more precisely the sample variance) of the signal x
        /// Is defined as the power of the signal with its mean removed
        /// <![CDATA[
        ///     varianceOf(x)
        ///     {
        ///         variance = 0;
        ///         for ( i = 0; i < n; i++)
        ///         {
        ///             variance += (x[n] - meanOf(x))^2;
        ///         }
        ///         variance = variance / n;
        ///         return Math.Sqrt(variance);
        ///     }
        /// ]]>
        /// The norm of a signal  is defined as the square root of its total energy
        /// We think of normOf(x) as the length of x in N-space.
        /// Furthermore, |normOf(x) - normOf(y)| is regarded as the distance 
        /// Between x and y. The norm can also be thought of as the ``absolute value''
        /// Or ``radius'' of a vector.
        /// <![CDATA[
        ///     normOf(x)
        ///     {
        ///         return Math.Sqrt(energyOf(x));
        ///     }
        /// ]]>
        /// </summary>
        private float _rootMeanSquare;
        private float _averagePitch;
        private float _pitchRange;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Pitch range.
        /// </summary>
        public float PitchRange
        {
            get { return _pitchRange; }
            set { _pitchRange = value; }
        }

        /// <summary>
        /// Gets or sets Energy in root mean square.
        /// </summary>
        public float RootMeanSquare
        {
            get { return _rootMeanSquare; }
            set { _rootMeanSquare = value; }
        }

        /// <summary>
        /// Gets Logged energy, this is Math.Log(RootMeanSquare).
        /// </summary>
        public float Energy
        {
            get { return (float)Math.Log(_rootMeanSquare + 1); }
        }

        /// <summary>
        /// Gets or sets Average of pitch.
        /// </summary>
        public float AveragePitch
        {
            get { return _averagePitch; }
            set { _averagePitch = value; }
        }

        /// <summary>
        /// Gets or sets  the start time stamp fo this segment in original waveform file (second).
        /// </summary>
        public double StartTime
        {
            get { return _startTime; }
            set { _startTime = value; }
        }

        /// <summary>
        /// Gets or sets End time stamp fo this segment in original waveform file (second).
        /// </summary>
        public double EndTime
        {
            get
            {
                return _endTime;
            }

            set
            {
                if (value == WaveSegment.InvalidTime)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid time for end time of wave segment");
                    throw new ArgumentException(message, "value");
                }

                _endTime = value;
            }
        }

        /// <summary>
        /// Gets Duration of this segment (second).
        /// </summary>
        public double Duration
        {
            get { return EndTime - StartTime; }
        }

        /// <summary>
        /// Gets or sets Tag.
        /// </summary>
        public object Tag
        {
            get
            {
                return _tag;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _tag = value;
            }
        }

        /// <summary>
        /// Gets or sets Label/name of this segment.
        /// </summary>
        public string Label
        {
            get
            {
                return _label;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _label = value;
                }
                else
                {
                    throw new ArgumentNullException("value");
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the segment has silence feature.
        /// </summary>
        public bool IsSilenceFeature
        {
            get
            {
                return IsSilencePhone || IsShortPausePhone;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this segment is a silence segment.
        /// </summary>
        public bool IsSilencePhone
        {
            get
            {
                return Label != null && Phoneme.IsSilencePhone(Label);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this segment is a short pause segment.
        /// </summary>
        public bool IsShortPausePhone
        {
            get
            {
                return Label != null && Phoneme.IsShortPausePhone(Label);
            }
        }

        /// <summary>
        /// Gets or sets Confidence level, applied when do automatic alignment or something else.
        /// </summary>
        public float Confidence
        {
            get { return _confidence; }
            set { _confidence = value; }
        }

        /// <summary>
        /// Gets Left phone of this segment.
        /// </summary>
        public string LeftPhone
        {
            get
            {
                string[] items = Label.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
                return items[0];
            }
        }

        /// <summary>
        /// Gets Right phone of this segment.
        /// </summary>
        public string RightPhone
        {
            get
            {
                string[] items = Label.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
                return items[items.Length - 1];
            }
        }

        #endregion

        #region string presentation

        /// <summary>
        /// Format label.
        /// </summary>
        /// <param name="label">Label to format.</param>
        /// <returns>Formated label.</returns>
        public static string FormatLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                throw new ArgumentNullException("label");
            }

            return Regex.Replace(label, @"\s+", "+");
        }

        /// <summary>
        /// Convert to string presentation.
        /// </summary>
        /// <returns>String presentation.</returns>
        public override string ToString()
        {
            return ToString(false);
        }

        /// <summary>
        /// Convert to string presentation.
        /// </summary>
        /// <param name="hasEndTime">Has end time in string.</param>
        /// <returns>String presentation.</returns>
        public string ToString(bool hasEndTime)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(StartTime.ToString("F5", CultureInfo.InvariantCulture));
            builder.Append(" ");
            if (hasEndTime)
            {
                builder.Append(EndTime.ToString("F5", CultureInfo.InvariantCulture));
                builder.Append(" ");
            }

            builder.Append(Label);
            return builder.ToString();
        }

        #endregion

        #region Operations

        /// <summary>
        /// Clone current instance to a new one.
        /// </summary>
        /// <returns>A WaveSegment object.</returns>
        public WaveSegment Clone()
        {
            WaveSegment segment = new WaveSegment();
            segment._startTime = _startTime;
            segment._endTime = _endTime;
            segment._label = _label;
            segment._confidence = _confidence;
            segment._averagePitch = _averagePitch;
            segment._pitchRange = _pitchRange;
            segment._rootMeanSquare = _rootMeanSquare;
            segment._tag = _tag;
            return segment;
        }

        #endregion
    }

    /// <summary>
    /// Mapping between two alignment files.
    /// </summary>
    public class MapEntry
    {
        #region Fields

        private int _leftIndex;
        private int _rightIndex;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="MapEntry"/> class.
        /// </summary>
        /// <param name="left">Left segment index.</param>
        /// <param name="right">Right segment index.</param>
        public MapEntry(int left, int right)
        {
            LeftIndex = left;
            RightIndex = right;
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets Left side alignment file, -1 stands for dropped.
        /// </summary>
        public int RightIndex
        {
            get { return _rightIndex; }
            set { _rightIndex = value; }
        }

        /// <summary>
        /// Gets or sets Right side alignment file, -1 stands for dropped.
        /// </summary>
        public int LeftIndex
        {
            get { return _leftIndex; }
            set { _leftIndex = value; }
        }
        #endregion
    }

    /// <summary>
    /// Segment file process setting.
    /// </summary>
    public class SegmentSetting
    {
        #region Fields

        private bool _hasEndTime;   // false, as default alignment file
        private bool _hasHeadSilence = true;  // true, needed for default check on alignment file loading
        private bool _hasTailSilence = true;  // true, needed for default check on alignment file loading

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="SegmentSetting"/> class.
        /// </summary>
        public SegmentSetting()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SegmentSetting"/> class.
        /// </summary>
        /// <param name="hasEndTime">End time in segment file.</param>
        /// <param name="hasHeadSilence">Head silence in segment file.</param>
        /// <param name="hasTailSilence">Tail silence in segment file.</param>
        public SegmentSetting(bool hasEndTime, bool hasHeadSilence, bool hasTailSilence)
        {
            _hasEndTime = hasEndTime;
            _hasHeadSilence = hasHeadSilence;
            _hasTailSilence = hasTailSilence;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether Has End timestamp for each segment in alignment file.
        /// </summary>
        public bool HasEndTime
        {
            get
            {
                return _hasEndTime;
            }

            set
            {
                _hasEndTime = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether there has a silence segment at the head of segments in alignment file.
        /// </summary>
        public bool HasHeadSilence
        {
            get
            {
                return _hasHeadSilence;
            }

            set
            {
                _hasHeadSilence = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether there has a silence segment at the tail of segments in alignment file.
        /// </summary>
        public bool HasTailSilence
        {
            get
            {
                return _hasTailSilence;
            }

            set
            {
                _hasTailSilence = value;
            }
        }

        #endregion
    }

    /// <summary>
    /// Class to manage segmentation data.
    /// <example>
    /// 0.00000 sil
    /// 0.28000 ax
    /// 0.38000 m+eh
    /// 0.55000 r+ax
    /// 0.71000 k
    /// 0.76000 ax+n
    /// 0.92000 sil.
    /// </example>
    /// </summary>
    public class SegmentFile
    {
        #region Fields

        /// <summary>
        /// Segment file extension.
        /// </summary>
        public const string FileExtension = ".txt";

        private Collection<WaveSegment> _waveSegments = new Collection<WaveSegment>();
        private Collection<WaveSegment> _nonSilenceWaveSegments = new Collection<WaveSegment>();

        private string _filePath;

        #endregion

        #region Properties

        /// <summary>
        /// Gets Average automatic alignment confidence.
        /// </summary>
        public float AverageConfidence
        {
            get
            {
                float average = 0.0f;
                foreach (WaveSegment ws in _waveSegments)
                {
                    average += ws.Confidence;
                }

                return (_waveSegments.Count == 0) ? 0.0f : average / _waveSegments.Count;
            }
        }

        /// <summary>
        /// Gets Sentence id.
        /// </summary>
        public string Id
        {
            get
            {
                string name = Path.GetFileNameWithoutExtension(_filePath);
                if (string.IsNullOrEmpty(name))
                {
                    name = _filePath;
                }

                string[] items = name.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                return items[items.Length - 1];
            }
        }

        /// <summary>
        /// Gets or sets Sentence file path.
        /// </summary>
        public string FilePath
        {
            get
            {
                return _filePath;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _filePath = value;
                }
                else
                {
                    throw new ArgumentNullException("value");
                }
            }
        }

        /// <summary>
        /// Gets Segment collection.
        /// </summary>
        public Collection<WaveSegment> WaveSegments
        {
            get { return _waveSegments; }
        }

        /// <summary>
        /// Gets Non-silence segement collection.
        /// </summary>
        public Collection<WaveSegment> NonSilenceWaveSegments
        {
            get { return _nonSilenceWaveSegments; }
        }

        #endregion

        #region Static operations

        /// <summary>
        /// Assert two segments are equal with each other.
        /// </summary>
        /// <param name="leftDir">Left segment dir to compare.</param>
        /// <param name="rightDir">Right segment dir to compare.</param>
        public static void AssertSegmentsEqual(string leftDir, string rightDir)
        {
            Dictionary<string, string> lefts = FileListMap.Build(leftDir, ".txt");
            Dictionary<string, string> rights = FileListMap.Build(rightDir, ".txt");

            if (lefts.Count != rights.Count)
            {
                throw new InvalidDataException("number of result items doesn't match expected.");
            }

            foreach (string sid in lefts.Keys)
            {
                if (!rights.ContainsKey(sid))
                {
                    throw new InvalidDataException("expect result not exist :" + sid);
                }

                string leftData = File.ReadAllText(Path.Combine(leftDir, lefts[sid] + ".txt"));
                string rightData = File.ReadAllText(Path.Combine(rightDir, rights[sid] + ".txt"));

                if (leftData != rightData)
                {
                    throw new InvalidDataException("expect result not matching with result:" + sid);
                }
            }
        }

        /// <summary>
        /// Convert triphone MLF(mast label file) to phone MLF.
        /// </summary>
        /// <param name="triphoneMlf">Tri-phone based MLF file path.</param>
        /// <param name="phoneMlf">Phone based MLF file path.</param>
        public static void ConvertTriphoneMlf2Phone(string triphoneMlf,
            string phoneMlf)
        {
            using (StreamReader forcedAlignMlfReader = new StreamReader(triphoneMlf))
            {
                using (StreamWriter syllableMlfWriter = new StreamWriter(phoneMlf))
                {
                    string line = null;
                    line = forcedAlignMlfReader.ReadLine();
                    if (line != "#!MLF!#")
                    {
                        throw new InvalidDataException(triphoneMlf);
                    }

                    syllableMlfWriter.WriteLine(line);

                    while ((line = forcedAlignMlfReader.ReadLine()) != null)
                    {
                        syllableMlfWriter.WriteLine(line);

                        bool firstAlign = true;
                        while ((line = forcedAlignMlfReader.ReadLine()) != null)
                        {
                            if (line == ".")
                            {
                                syllableMlfWriter.WriteLine(line);
                                break;
                            }

                            string[] items = line.Split(new char[] { ' ' },
                                StringSplitOptions.RemoveEmptyEntries);
                            string[] phonemes = items[4].Split(new char[] { '-', '+' },
                                StringSplitOptions.RemoveEmptyEntries);

                            float align = float.Parse(items[0], CultureInfo.InvariantCulture) * 1e-7f;

                            string phoneme;
                            if (firstAlign)
                            {
                                if (!Phoneme.IsSilencePhone(items[4]))
                                {
                                    string message = string.Format(CultureInfo.InvariantCulture,
                                        "The begin segment of utterance alignment should be silence in line [{0}]",
                                        line);
                                    throw new InvalidDataException(message);
                                }

                                firstAlign = false;
                            }
                            else
                            {
                                // shift 10ms (half frame size) from frame side to center
                                align += 0.01f;
                            }

                            phoneme = (phonemes.Length == 1) ? phonemes[0] : phonemes[1];

                            // alignment point, phoneme, confidence
                            syllableMlfWriter.WriteLine(align.ToString("F5", CultureInfo.InvariantCulture)
                                + " " + phoneme + " " + items[3]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Convert phone MLF file to syllable MLF file.
        /// </summary>
        /// <param name="phoneMlf">Phone based MLF file path.</param>
        /// <param name="syllableMlf">Syllable based MLF file path.</param>
        public static void ConvertPhone2SyllableMlf(string phoneMlf, string syllableMlf)
        {
            using (StreamReader phoneMlfReader = new StreamReader(phoneMlf))
            {
                using (StreamWriter syllableMlfWriter = new StreamWriter(syllableMlf))
                {
                    string line = null;
                    line = phoneMlfReader.ReadLine();
                    if (line != "#!MLF!#")
                    {
                        throw new InvalidDataException("Invalid file header " + phoneMlf);
                    }

                    syllableMlfWriter.WriteLine(line);

                    Collection<string[]> segments = new Collection<string[]>();

                    while ((line = phoneMlfReader.ReadLine()) != null)
                    {
                        syllableMlfWriter.WriteLine(line);

                        // loading sentence segmentations
                        segments.Clear();
                        while ((line = phoneMlfReader.ReadLine()) != null)
                        {
                            if (line == ".")
                            {
                                break;
                            }

                            string[] items = line.Split(new char[] { ' ' },
                                StringSplitOptions.RemoveEmptyEntries);
                            segments.Add(items);
                        }

                      syllableMlfWriter.WriteLine(".");
                    }
                }
            }
        }

        /// <summary>
        /// Load MLF file.
        /// </summary>
        /// <param name="filePath">MLF file path.</param>
        /// <returns>Segment file dictionary, indexed by sentence id.</returns>
        public static Dictionary<string, SegmentFile> ReadAllDataFromMlf(string filePath)
        {
            Dictionary<string, SegmentFile> sfs = new Dictionary<string, SegmentFile>();

            using (StreamReader sr = new StreamReader(filePath))
            {
                string line = null;
                line = sr.ReadLine();
                if (line != "#!MLF!#")
                {
                    throw new InvalidDataException("Invalid file header " + filePath);
                }

                while ((line = sr.ReadLine()) != null)
                {
                    // line should be sentence file path
                    Match m = Regex.Match(line, @".*/(\S*)\.");
                    if (!m.Success)
                    {
                        throw new InvalidDataException("Invalid format in file "
                            + filePath + ", line " + line);
                    }

                    SegmentFile sf = new SegmentFile();

                    sf.FilePath = m.Groups[1].Value;

                    sf.Load(sr);

                    sfs.Add(sf.Id, sf);
                }
            }

            return sfs;
        }

        /// <summary>
        /// Load segment data from text reader stream.
        /// </summary>
        /// <param name="tr">Text reader to read segment from.</param>
        /// <returns>Wave segment collection.</returns>
        public static Collection<WaveSegment> ReadAllData(TextReader tr)
        {
            return ReadAllData(tr, new SegmentSetting());
        }

        /// <summary>
        /// Load segment data from text reader stream.
        /// </summary>
        /// <param name="tr">Text reader to read segment from.</param>
        /// <param name="setting">Setting.</param>
        /// <returns>Wave segment collection.</returns>
        public static Collection<WaveSegment> ReadAllData(TextReader tr, SegmentSetting setting)
        {
            if (tr == null)
            {
                throw new ArgumentNullException("tr");
            }

            if (setting == null)
            {
                throw new ArgumentNullException("setting");
            }

            Collection<WaveSegment> segs = new Collection<WaveSegment>();
            string line = null;
            while ((line = tr.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (line == ".")
                {
                    // end of section
                    break;
                }

                string[] items = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (items.Length < 2 && !setting.HasEndTime)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "The normal segment line of alignment file shoud be (timestamp) (label) [confidence score]. But '{0}' is found.",
                        line);
                    throw new InvalidDataException(message);
                }

                if (items.Length < 3 && setting.HasEndTime)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "The normal segment line of alignment file should be (timestamp) (timestamp) (label) [confidence score]. But '{0}' is found.",
                        line);
                    throw new InvalidDataException(message);
                }

                WaveSegment seg = new WaveSegment();
                try
                {
                    seg.StartTime = float.Parse(items[0], CultureInfo.InvariantCulture);
                    if (!setting.HasEndTime)
                    {
                        if (items.Length == 3)
                        {
                            seg.Confidence = float.Parse(items[2], CultureInfo.InvariantCulture);
                        }
                    }
                    else
                    {
                        seg.EndTime = float.Parse(items[1], CultureInfo.InvariantCulture);
                    }
                }
                catch (FormatException)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Malformed line found as '{0}'",
                        line);
                    throw new InvalidDataException(message);
                }

                seg.Label = Phoneme.ToOffline(items[setting.HasEndTime ? 2 : 1]);
                segs.Add(seg);
            }

            RemoveDuplicatedSilence(segs);

            for (int i = 0; i < segs.Count - 1; i++)
            {
                if (!setting.HasEndTime)
                {
                    segs[i].EndTime = segs[i + 1].StartTime;
                }
                else
                {
                    if (segs[i].StartTime > segs[i].EndTime)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "The start time of the {0}(th) segment [{1}] must not be later the end time of it.",
                            i, segs[i].Label);
                        throw new InvalidDataException(message);
                    }
                }

                if (segs[i].StartTime > segs[i + 1].StartTime)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "The start time of the {0}(th) segment [{1}] must not be later than the start time of the following segment [{2}].",
                        i, segs[i].Label, segs[i + 1].Label);
                    throw new InvalidDataException(message);
                }
            }

            return segs;
        }

        /// <summary>
        /// Tell whether one segment file is a valid one.
        /// </summary>
        /// <param name="filePath">Segment file to test.</param>
        /// <returns>True if valid, otherwise false.</returns>
        public static bool IsValid(string filePath)
        {
            return IsValid(filePath, new SegmentSetting());
        }

        /// <summary>
        /// Tell whether one segment file is a valid one.
        /// </summary>
        /// <param name="filePath">Segment file to test.</param>
        /// <param name="setting">Setting.</param>
        /// <returns>True if valid, otherwise false.</returns>
        public static bool IsValid(string filePath, SegmentSetting setting)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (setting == null)
            {
                throw new ArgumentNullException("setting");
            }

            Collection<WaveSegment> segments = null;
            try
            {
                segments = ReadAllData(filePath, setting);
            }
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid data found for alignment file [{0}], for {1}",
                    filePath, ide.Message);
                System.Diagnostics.Trace.WriteLine(message);
                return false;
            }

            if (segments.Count < 3 && setting.HasHeadSilence && setting.HasTailSilence)
            {
                // should start/end with Phoneme.Silence tags, and at least one other segment
                return false;
            }

            if ((setting.HasHeadSilence && !segments[0].IsSilencePhone)
                || (setting.HasTailSilence && !segments[segments.Count - 1].IsSilencePhone))
            {
                // should start/end with Phoneme.Silence tags
                return false;
            }

            for (int i = 1; i < segments.Count; i++)
            {
                if (segments[i - 1].StartTime >= segments[i].StartTime)
                {
                    // timestamp of preview segment should less than following one.
                    return false;
                }
            }

            for (int i = 0; setting.HasEndTime && i < segments.Count; i++)
            {
                if (segments[i].StartTime > segments[i].EndTime)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Load segment data from text file.
        /// </summary>
        /// <param name="filePath">Segment file path.</param>
        /// <returns>Wave segment collection.</returns>
        public static Collection<WaveSegment> ReadAllData(string filePath)
        {
            return ReadAllData(filePath, new SegmentSetting());
        }

        /// <summary>
        /// Load segment data from text file.
        /// </summary>
        /// <param name="filePath">Segment file path.</param>
        /// <param name="setting">Setting.</param>
        /// <returns>Wave segment collection.</returns>
        public static Collection<WaveSegment> ReadAllData(string filePath, SegmentSetting setting)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (setting == null)
            {
                throw new ArgumentNullException("setting");
            }

            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    return SegmentFile.ReadAllData(sr, setting);
                }
            }
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Failed to load alignment file [{0}].",
                    filePath);
                throw new InvalidDataException(message, ide);
            }
        }

        /// <summary>
        /// Match manual alignment segments with forced alignment segments.
        /// Manual alignment can only add or delete silence tags against forced alignment.
        /// </summary>
        /// <param name="leftSegments">Left wave segment collection.</param>
        /// <param name="rightSegments">Right wave segment collection.</param>
        /// <returns>Matched entry collection.</returns>
        public static Collection<MapEntry> MatchSegments(Collection<WaveSegment> leftSegments,
            Collection<WaveSegment> rightSegments)
        {
            if (leftSegments == null)
            {
                throw new ArgumentNullException("leftSegments");
            }

            if (rightSegments == null)
            {
                throw new ArgumentNullException("rightSegments");
            }

            Collection<MapEntry> map = new Collection<MapEntry>();
            int j = 0;
            for (int i = 0; i < rightSegments.Count; i++)
            {
                if (leftSegments.Count <= j)
                {
                    // fail to match
                    map.Clear();
                    break;
                }

                if (rightSegments[i] != null && rightSegments[i].IsSilenceFeature)
                {
                    if (leftSegments[j] != null && leftSegments[j].IsSilenceFeature)
                    {
                        // both segments are silence tags
                        map.Add(new MapEntry(j++, i));
                    }
                    else
                    {
                        // a new silence is inserted
                        map.Add(new MapEntry(-1, i));
                    }
                }
                else
                {
                    while (leftSegments.Count > j && leftSegments[j] != null && leftSegments[j].IsSilenceFeature)
                    {
                        // this silence segment is deleted
                        map.Add(new MapEntry(j++, -1));
                    }

                    if (leftSegments.Count > j && leftSegments[j] != null
                        && rightSegments[i] != null
                        && leftSegments[j].Label == rightSegments[i].Label)
                    {
                        // segment matchs
                        map.Add(new MapEntry(j++, i));
                    }
                    else
                    {
                        // un-matching detected
                        map.Clear();
                        break;
                    }
                }
            }

            return map;
        }

        /// <summary>
        /// Delete continue silence tags (duplicated) in segmentation sequence.
        /// </summary>
        /// <param name="segments">Wave segment collection.</param>
        public static void RemoveDuplicatedSilence(Collection<WaveSegment> segments)
        {
            if (segments == null)
            {
                throw new ArgumentNullException("segments");
            }

            for (int i = segments.Count - 1; i > 0; i--)
            {
                if (segments[i] != null && segments[i - 1] != null &&
                    segments[i].IsSilenceFeature &&
                    segments[i - 1].IsSilenceFeature)
                {
                    if (segments[i].Label != segments[i - 1].Label)
                    {
                        // If not both are silence phone or short pause phone, convert the (i - 1)th to silence.
                        segments[i - 1].Label = Phoneme.ToOffline(Phoneme.SilencePhone);
                    }

                    // Add the second one's duration to the first one
                    segments[i - 1].EndTime = segments[i].EndTime;
                    segments.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Serialize & deserialize

        /// <summary>
        /// Initialize from a segment file.
        /// </summary>
        /// <param name="filePath">Segment file.</param>
        /// <param name="fHasEndTime">Whether to check the ending silence.</param>
        /// <param name="fHasHeadSilence">Whether to check the head silence.</param>
        /// <param name="fHasTailSilence">Whether to check the tail silence.</param>
        public void Load(string filePath, bool fHasEndTime, bool fHasHeadSilence, bool fHasTailSilence)
        {
            SegmentSetting setting = new SegmentSetting()
            {
                HasEndTime = fHasEndTime,
                HasHeadSilence = fHasHeadSilence,
                HasTailSilence = fHasTailSilence
            };

            Load(filePath, setting);
        }

        /// <summary>
        /// Initialize from a segment file.
        /// </summary>
        /// <param name="filePath">Segment file.</param>
        public void Load(string filePath)
        {
            Load(filePath, new SegmentSetting());
        }

        /// <summary>
        /// Initialize from a segment file.
        /// </summary>
        /// <param name="filePath">Segment file.</param>
        /// <param name="setting">Setting.</param>
        public void Load(string filePath, SegmentSetting setting)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (setting == null)
            {
                throw new ArgumentNullException("setting");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }

            _filePath = filePath;

            try
            {
                using (TextReader tr = new StreamReader(filePath))
                {
                    Load(tr, setting);
                }
            }
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Failed to load alignment file [{0}].",
                    filePath);
                throw new InvalidDataException(message, ide);
            }

            if (WaveSegments.Count == 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Empty alignment file found at [{0}].",
                    filePath);
                throw new InvalidDataException(message);
            }

            if (setting.HasTailSilence && !Phoneme.IsSilencePhone(WaveSegments[WaveSegments.Count - 1].Label))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Last segment [{0}] in file [{1}] should be [{2}].",
                    WaveSegments[WaveSegments.Count - 1].Label, filePath, Phoneme.ToOffline(Phoneme.SilencePhone));
                throw new InvalidDataException(message);
            }
        }

        /// <summary>
        /// Initialize from text stream.
        /// </summary>
        /// <param name="tr">Segment text stream.</param>
        public void Load(TextReader tr)
        {
            Load(tr, new SegmentSetting());
        }

        /// <summary>
        /// Initialize from text stream.
        /// </summary>
        /// <param name="tr">Segment text stream.</param>
        /// <param name="setting">Setting.</param>
        public void Load(TextReader tr, SegmentSetting setting)
        {
            _waveSegments = SegmentFile.ReadAllData(tr, setting);

            UpdateNonSilenceWaveSegments();
        }

        /// <summary>
        /// Save this segment data into file.
        /// </summary>
        /// <param name="filePath">Target file to save.</param>
        public void Save(string filePath)
        {
            Save(filePath, new SegmentSetting());
        }

        /// <summary>
        /// Save this segment data into file.
        /// </summary>
        /// <param name="filePath">Target file to save.</param>
        /// <param name="setting">Setting.</param>
        public void Save(string filePath, SegmentSetting setting)
        {
            Helper.EnsureFolderExistForFile(filePath);
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                Save(sw, setting);
            }
        }

        /// <summary>
        /// Save this segment data into TextWriter.
        /// </summary>
        /// <param name="tw">Text writer to write the segments.</param>
        public void Save(TextWriter tw)
        {
            Save(tw, new SegmentSetting());
        }

        /// <summary>
        /// Save this segment data into TextWriter.
        /// </summary>
        /// <param name="tw">Text writer to write the segments.</param>
        /// <param name="setting">Setting.</param>
        public void Save(TextWriter tw, SegmentSetting setting)
        {
            if (tw == null)
            {
                throw new ArgumentNullException("tw");
            }

            foreach (WaveSegment ws in _waveSegments)
            {
                tw.WriteLine(ws.ToString(setting.HasEndTime));
            }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Converts silence (excluding intial/final silence) to short pause.
        /// </summary>
        /// <param name="threshold">The silence whose duration is greater than the threshold won't be convert to short pause.</param>
        public void ConvertSilenceToShortPause(double threshold)
        {
            if (!_waveSegments[0].IsSilencePhone || !_waveSegments[_waveSegments.Count - 1].IsSilencePhone)
            {
                throw new InvalidDataException("Intial/final should be silence.");
            }

            // intial/final silence should be skipped.
            for (int i = 1; i < _waveSegments.Count - 1; ++i)
            {
                if (_waveSegments[i].IsSilencePhone && _waveSegments[i].Duration < threshold)
                {
                    _waveSegments[i].Label = Phoneme.ToOffline(Phoneme.ShortPausePhone);
                }
            }
        }

        /// <summary>
        /// Removes the silence segment in the given index.
        /// The duration of silence phone will be equally split into the left and right adjacent phones.
        /// </summary>
        /// <param name="index">The index should be removed. The segment should have silence feature.</param>
        public void RemoveSilenceSegment(int index)
        {
            if (_waveSegments[index].IsSilenceFeature)
            {
                if (index == 0)
                {
                    if (index != _waveSegments.Count - 1)
                    {
                        _waveSegments[index + 1].StartTime = _waveSegments[index].StartTime;
                    }
                }
                else
                {
                    if (index == _waveSegments.Count - 1)
                    {
                        _waveSegments[index - 1].EndTime = _waveSegments[index].EndTime;
                    }
                    else
                    {
                        double lengthToMove = _waveSegments[index].Duration / 2;
                        _waveSegments[index - 1].EndTime += lengthToMove;
                        _waveSegments[index + 1].StartTime -= lengthToMove;
                    }
                }

                _waveSegments.RemoveAt(index);
            }
        }

        /// <summary>
        /// Insert a new short pause phone in the given index.
        /// The duration of the short pause will be equally splitted from the left and right adjacent phones.
        /// </summary>
        /// <param name="index">The new short pause phone will be inserted into the position represented by 'index'.</param>
        public void InsertShortPauseSegment(int index)
        {
            // Can not insert short pause in the first/last position
            Debug.Assert(index > 0 && index < _waveSegments.Count - 1);

            const float SplittingRatio = 0.25f;
            WaveSegment newShortPauseSegment = new WaveSegment();

            _waveSegments[index - 1].EndTime = _waveSegments[index - 1].StartTime + (_waveSegments[index - 1].Duration * (1 - SplittingRatio));
            _waveSegments[index].StartTime = _waveSegments[index].EndTime - (_waveSegments[index].Duration * (1 - SplittingRatio));

            newShortPauseSegment.StartTime = _waveSegments[index - 1].EndTime;
            newShortPauseSegment.EndTime = _waveSegments[index].StartTime;
            newShortPauseSegment.Label = Phoneme.ShortPausePhone;

            _waveSegments.Insert(index, newShortPauseSegment);
        }

        /// <summary>
        /// Update Non-silence wave segments from the all segments.
        /// </summary>
        public void UpdateNonSilenceWaveSegments()
        {
            if (_nonSilenceWaveSegments == null)
            {
                _nonSilenceWaveSegments = new Collection<WaveSegment>();
            }

            _nonSilenceWaveSegments.Clear();

            foreach (WaveSegment segment in _waveSegments)
            {
                if (!segment.IsSilenceFeature)
                {
                    _nonSilenceWaveSegments.Add(segment);
                }
            }
        }

        /// <summary>
        /// Shift certain timespan for each alignment boundary.
        /// </summary>
        /// <param name="shiftDuration">Shift duration.</param>
        public void Shift(float shiftDuration)
        {
            for (int i = 0; i < _waveSegments.Count; i++)
            {
                WaveSegment wg = _waveSegments[i];
                if (i == 0
                    && (wg.IsSilenceFeature || wg.Label == "s")
                    && wg.StartTime == 0.0f)
                {
                    // first silence, do not shift
                    continue;
                }
                else
                {
                    wg.StartTime += shiftDuration;
                }
            }
        }
        #endregion
    }
}