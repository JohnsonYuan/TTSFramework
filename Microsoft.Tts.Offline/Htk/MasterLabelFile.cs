//----------------------------------------------------------------------------
// <copyright file="MasterLabelFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines a common library to manipulate master label file.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// The label information for a single unit.
    /// </summary>
    public class LabelInfo
    {
        #region Fields

        private static readonly char[] _splitChars = new char[] { ' ', '\t' };
        private static double _timeUnit = 1.0e-7f;

        private int _start = 0;
        private int _end = 0;
        private string _label = string.Empty;
        private string _secondLabel = string.Empty;

        #endregion

        #region Enums

        /// <summary>
        /// Hold the master label type.
        /// </summary>
        public enum LabelType
        {
            /// <summary>
            /// Indicate the master label has alignment data.
            /// </summary>
            WithAlignData,

            /// <summary>
            /// Indicate the master label has no alignment data.
            /// </summary>
            WithoutAlignData,

            /// <summary>
            /// Indicate the master label has second label.
            /// </summary>
            WithSecondLabel,
        }

        #endregion

        #region Property

        /// <summary>
        /// Gets or sets The start time of this label. Unit: 50000us.
        /// </summary>
        public int Start
        {
            get
            {
                return _start;
            }

            set
            {
                _start = value;
            }
        }

        /// <summary>
        /// Gets or sets The end time of this label. Unit: 50000us.
        /// </summary>
        public int End
        {
            get
            {
                return _end;
            }

            set
            {
                _end = value;
            }
        }

        /// <summary>
        /// Gets The duration of this label. Unit: 50000us.
        /// </summary>
        public int Duration
        {
            get
            {
                return _end - _start;
            }
        }

        /// <summary>
        /// Gets or sets The label string.
        /// </summary>
        public string Label
        {
            get
            {
                return _label;
            }

            set
            {
                _label = value;
            }
        }

        /// <summary>
        /// Gets or sets The second label string.
        /// </summary>
        public string SecondLabel
        {
            get
            {
                return _secondLabel;
            }

            set
            {
                _secondLabel = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Parse the line in master label file to generate a LabelInfo instance.
        /// </summary>
        /// <param name="line">The line from master label file.</param>
        /// <returns>A new LabelInfo instance according to the line.</returns>
        public static LabelInfo ParseLine(string line)
        {
            LabelInfo labelInfo = new LabelInfo();
            string[] parts = line.Split(_splitChars, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 3 || parts.Length == 4)
            {
                labelInfo.Start = int.Parse(parts[0], CultureInfo.InvariantCulture.NumberFormat);
                labelInfo.End = int.Parse(parts[1], CultureInfo.InvariantCulture.NumberFormat);
                labelInfo.Label = parts[2];
                if (parts.Length == 4)
                {
                    labelInfo.SecondLabel = parts[3];
                }

                Debug.Assert(labelInfo.End >= labelInfo.Start && labelInfo.Start >= 0, "The end time point mustn't less than the start and zero.");
            }
            else if (parts.Length == 1)
            {
                labelInfo.Label = parts[0];
            }
            else
            {
                string message = Helper.NeutralFormat("Unsupported label format : [{0}].", line);
                throw new InvalidDataException(message);
            }

            return labelInfo;
        }

        /// <summary>
        /// Convert a time into second.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <returns>Time in second.</returns>
        public static double ConvertToSecond(double time)
        {
            return time * _timeUnit;
        }

        /// <summary>
        /// Convert the object to string.
        /// </summary>
        /// <returns>String according to the object.</returns>
        public override string ToString()
        {
            return _label;
        }

        /// <summary>
        /// Convert the object to string.
        /// </summary>
        /// <param name="type">Indicate the type, which will impact the context of the string.</param>
        /// <returns>String according to the object.</returns>
        public string ToString(LabelType type)
        {
            string message = string.Empty;
            if (type == LabelType.WithoutAlignData)
            {
                return ToString();
            }
            else if (type == LabelType.WithSecondLabel && _secondLabel != string.Empty)
            {
                message = Helper.NeutralFormat("{0} {1} {2} {3}", _start, _end, _label, _secondLabel);
            }
            else
            {
                message = Helper.NeutralFormat("{0} {1} {2}", _start, _end, _label);
            }

            return message;
        }

        #endregion
    }

    /// <summary>
    /// The list of label information for multi units, for example, a sentence.
    /// </summary>
    public class LabelInfoSentence
    {
        #region Fields

        /// <summary>
        /// The string indicate the end of sentence.
        /// </summary>
        public static readonly string EndOfSentence = ".";

        private string _sid = string.Empty;
        private List<LabelInfo> _listLabelInfo = new List<LabelInfo>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Sentence Id.
        /// </summary>
        public string SentenceId
        {
            get
            {
                return _sid;
            }

            set
            {
                _sid = value;
            }
        }

        /// <summary>
        /// Gets The list of LabelInfo object.
        /// </summary>
        public List<LabelInfo> LabelInfos
        {
            get
            {
                return _listLabelInfo;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Load one sentence master label info from a stream reader.
        /// </summary>
        /// <param name="sr">The stream reader will be loaded from.</param>
        public void Load(StreamReader sr)
        {
            _listLabelInfo.Clear();
            string line = sr.ReadLine();
            if (line[0] == '"')
            {
                _sid = Path.GetFileNameWithoutExtension(line.Trim('"'));
            }

            bool inSent = true;
            while (!sr.EndOfStream)
            {
                line = sr.ReadLine();
                if (line.Equals(EndOfSentence, StringComparison.Ordinal))
                {
                    inSent = false;
                    break;
                }
                else
                {
                    LabelInfo li = LabelInfo.ParseLine(line);
                    _listLabelInfo.Add(li);
                }
            }

            if (inSent)
            {
                string message = Helper.NeutralFormat("File ended improperly when loading {0}.", _sid);
                throw new InvalidDataException(message);
            }
        }

        /// <summary>
        /// Save the sentence master label info to a stream writer.
        /// </summary>
        /// <param name="sw">The stream writer will be saved to.</param>
        /// <param name="type">Indicate the type, which will impact the context of the label.</param>
        public void Save(TextWriter sw, LabelInfo.LabelType type)
        {
            sw.WriteLine("\"*/{0}.lab\"", _sid);
            for (int i = 0; i < _listLabelInfo.Count; ++i)
            {
                sw.WriteLine(_listLabelInfo[i].ToString(type));
            }

            sw.WriteLine(EndOfSentence);
        }

        /// <summary>
        /// Convert the label info sentence into a segment file.
        /// </summary>
        /// <returns>SegmentFile.</returns>
        public SegmentFile ToSegmentFile()
        {
            SegmentFile segmentFile = new SegmentFile();
            foreach (LabelInfo lableInfo in _listLabelInfo)
            {
                WaveSegment segment = new WaveSegment();
                segment.Label = lableInfo.Label;
                segment.StartTime = LabelInfo.ConvertToSecond(lableInfo.Start);
                segment.EndTime = LabelInfo.ConvertToSecond(lableInfo.End);
                segmentFile.WaveSegments.Add(segment);
            }

            segmentFile.UpdateNonSilenceWaveSegments();
            return segmentFile;
        }

        #endregion
    }

    /// <summary>
    /// The file hold the master label of many sentences.
    /// </summary>
    public class MasterLabelFile
    {
        #region Fields

        /// <summary>
        /// The master label file header string.
        /// </summary>
        public static readonly string MasterLabelFileHeader = "#!MLF!#";

        private SortedDictionary<string, LabelInfoSentence> _dictLabelInfoSent =
            new SortedDictionary<string, LabelInfoSentence>(StringComparer.Ordinal);

        #endregion

        #region Properties

        /// <summary>
        /// Gets The dictionary of LabelInfoList by sentences.
        /// </summary>
        public SortedDictionary<string, LabelInfoSentence> SentLabelInfos
        {
            get
            {
                return _dictLabelInfoSent;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Load the master label file.
        /// </summary>
        /// <param name="masterLabelFileName">The master label file name.</param>
        public void Load(string masterLabelFileName)
        {
            using (StreamReader sr = new StreamReader(masterLabelFileName))
            {
                string line = sr.ReadLine(); // This is the header of master label file.
                Debug.Assert(MasterLabelFileHeader.Equals(line), "There must be a Htk master label file header.");
                while (!sr.EndOfStream)
                {
                    LabelInfoSentence labelInfoSent = new LabelInfoSentence();
                    labelInfoSent.Load(sr);
                    _dictLabelInfoSent.Add(labelInfoSent.SentenceId, labelInfoSent);
                }
            }
        }

        /// <summary>
        /// Save the master labels to file.
        /// </summary>
        /// <param name="file">The file name.</param>
        /// <param name="type">Indicate the type, which will impact the context of the master label.</param>
        public void Save(string file, LabelInfo.LabelType type)
        {
            using (StreamWriter sw = new StreamWriter(file, false, System.Text.Encoding.ASCII))
            {
                sw.WriteLine(MasterLabelFileHeader);

                foreach (KeyValuePair<string, LabelInfoSentence> kvp in _dictLabelInfoSent)
                {
                    kvp.Value.Save(sw, type);
                }
            }
        }

        /// <summary>
        /// Build a Htk format list from the master labels. The list is sorted without duplicate.
        /// </summary>
        /// <returns>The list of all the labels.</returns>
        public ICollection<string> BuildLabelList()
        {
            SortedDictionary<string, int> dict = new SortedDictionary<string, int>();
            foreach (LabelInfoSentence labelInfoSent in _dictLabelInfoSent.Values)
            {
                foreach (LabelInfo labelInfo in labelInfoSent.LabelInfos)
                {
                    if (!dict.ContainsKey(labelInfo.Label))
                    {
                        dict.Add(labelInfo.Label, 0);
                    }
                }
            }

            return dict.Keys;
        }

        #endregion
    }
}