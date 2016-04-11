//----------------------------------------------------------------------------
// <copyright file="TrainingSentenceSet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines a common library to manipulate Htk training file.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Waveform;
    using Microsoft.Tts.ServiceProvider.FeatureExtractor;

    /// <summary>
    /// Store the information of Htk segmentation.
    /// </summary>
    public class Segment
    {
        #region Fields

        /// <summary>
        /// The Htk time unit.
        /// </summary>
        public const float HtkTimeUnit = 1.0e-7f;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the Segment class by given start time and end time.
        /// </summary>
        /// <param name="startTime">The given start time.</param>
        /// <param name="endTime">The given end time.</param>
        public Segment(long startTime, long endTime)
        {
            StartTime = startTime;
            EndTime = endTime;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets start time of this segment, in 1.0e-7s.
        /// </summary>
        public long StartTime { get; set; }

        /// <summary>
        /// Gets or sets end time of this segment, in 1.0e-7s.
        /// </summary>
        public long EndTime { get; set; }

        /// <summary>
        /// Gets or sets start time of this segment, in second.
        /// </summary>
        public float StartTimeInSecond
        {
            get
            {
                return (float)(StartTime * HtkTimeUnit);
            }

            set
            {
                StartTime = (long)((value / HtkTimeUnit) + 0.5f);
            }
        }

        /// <summary>
        /// Gets or sets end time of this segment, in second.
        /// </summary>
        public float EndTimeInSecond
        {
            get
            {
                return (float)(EndTime * HtkTimeUnit);
            }

            set
            {
                EndTime = (long)((value / HtkTimeUnit) + 0.5f);
            }
        }

        /// <summary>
        /// Gets the duration of this segment, in second.
        /// </summary>
        public float DurationInSecond
        {
            get
            {
                if (EndTimeInSecond < StartTimeInSecond)
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "The end time [{0}] should be bigger than the start time {[1]}.",
                        EndTimeInSecond, StartTimeInSecond));
                }

                return (float)(EndTimeInSecond - StartTimeInSecond);
            }
        }

        #endregion
    }

    /// <summary>
    /// The basic phone segment.
    /// </summary>
    public class PhoneSegment
    {
        #region Fields

        /// <summary>
        /// The state alignments data of this phoneme.
        /// </summary>
        private Segment[] _stateSegments;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the label of this phoneme.
        /// </summary>
        public Label Label { get; set; }

        /// <summary>
        /// Gets the name of the phone.
        /// </summary>
        public string Name
        {
            get
            {
                return Label.CentralPhoneme;
            }
        }

        /// <summary>
        /// Gets the left phoneme of this phoneme.
        /// </summary>
        public string LeftPhoneme
        {
            get
            {
                return Label.LeftPhoneme;
            }
        }

        /// <summary>
        /// Gets the right phoneme of this phoneme.
        /// </summary>
        public string RightPhoneme
        {
            get
            {
                return Label.RightPhoneme;
            }
        }

        /// <summary>
        /// Gets or sets the sentence which this phoneme belongs to.
        /// </summary>
        public Sentence Sentence { get; set; }

        /// <summary>
        /// Gets or sets the index of this phoneme in the sentences.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the index of this phoneme in those non-silence phoneme of the sentences.
        /// </summary>
        public int IndexOfNonSilence { get; set; }

        /// <summary>
        /// Gets or sets the start time of this phoneme, in 1.0e-7s.
        /// </summary>
        public long StartTime
        {
            get
            {
                return StateAlignments[0].StartTime;
            }

            set
            {
                if (_stateSegments == null)
                {
                    // Create a new phone level segmentation.
                    StateAlignments = new Segment[1];
                    StateAlignments[0] = new Segment(0, 0);
                }

                StateAlignments[0].StartTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the end time of this phoneme, in 1.0e-7s.
        /// </summary>
        public long EndTime
        {
            get
            {
                return StateAlignments[StateAlignments.Length - 1].EndTime;
            }

            set
            {
                if (_stateSegments == null)
                {
                    // Create a new phone level segmentation.
                    StateAlignments = new Segment[1];
                    StateAlignments[0] = new Segment(0, 0);
                }

                StateAlignments[StateAlignments.Length - 1].EndTime = value;
            }
        }

        /// <summary>
        /// Gets the start frame index of this phoneme.
        /// </summary>
        public int StartFrame
        {
            get
            {
                return (int)Math.Round(StartTimeInSecond * 1000 / Sentence.TrainingSet.MilliSecondsPerFrame);
            }
        }

        /// <summary>
        /// Gets of sets the end frame index of this phoneme.
        /// </summary>
        public int EndFrame
        {
            get
            {
                return (int)Math.Round(EndTimeInSecond * 1000 / Sentence.TrainingSet.MilliSecondsPerFrame);
            }
        }

        /// <summary>
        /// Gets or sets the state alignments data of this phoneme. Currently, there are usually 5 state in HTS.
        /// Phone level alignment will be considered as there is only 1 state.
        /// </summary>
        public Segment[] StateAlignments
        {
            get
            {
                if (_stateSegments == null || _stateSegments.Length <= 0)
                {
                    throw new InvalidOperationException("Unsupported operation since there is no alignment data in phoneme");
                }

                return _stateSegments;
            }

            set
            {
                _stateSegments = value;
            }
        }

        /// <summary>
        /// Gets the label remainings of the phoneme.
        /// The key is the state number, the value is respective remaining.
        /// </summary>
        public Dictionary<int, string[]> LabelRemainings { get; private set; }

        /// <summary>
        /// Gets or sets the start time of this phoneme, in second.
        /// </summary>
        public float StartTimeInSecond
        {
            get
            {
                return (float)(StartTime * Segment.HtkTimeUnit);
            }

            set
            {
                StartTime = (long)Math.Round(value / (Segment.HtkTimeUnit * 10000)) * 10000;
            }
        }

        /// <summary>
        /// Gets or sets the end time of this phoneme, in second.
        /// </summary>
        public float EndTimeInSecond
        {
            get
            {
                return (float)(EndTime * Segment.HtkTimeUnit);
            }

            set
            {
                EndTime = (long)Math.Round(value / (Segment.HtkTimeUnit * 10000)) * 10000;
            }
        }

        /// <summary>
        /// Gets the full-context label.
        /// </summary>
        public string FullContextLabel
        {
            get { return Label.Text; }
        }

        /// <summary>
        /// Gets or sets feature extraction engine output.
        /// </summary>
        [CLSCompliant(false)]
        public FeatureValue[] Features { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Loads the phoneme from the lines in the format of master label file.
        /// Please notice this load may be called multi-times to load the different information, such as full-context label, alignment data and so on.
        /// </summary>
        /// <param name="lines">The lines in master label file format.</param>
        /// <param name="index">The current index of the given lines.</param>
        public void Load(IList<string> lines, ref int index)
        {
            LabelLine labelLine = LabelLine.Parse(lines[index++]);
            if (Label != null)
            {
                // Not the first time to load the candidate. So, need verifications.
                if (Label.CentralPhoneme != labelLine.Label.CentralPhoneme)
                {
                    throw new InvalidDataException(
                        Helper.NeutralFormat("The data is mismatched when parse \"{0}\" in multi master label files, expected \"{1}\"", lines[index], Label.CentralPhoneme));
                }

                // Try to keep the longest label.
                if (labelLine.Label.FeatureValueCount > Label.FeatureValueCount)
                {
                    Label = labelLine.Label;
                }
            }
            else
            {
                // First time to load the candidate.
                Label = labelLine.Label;
            }

            LabelRemainings = new Dictionary<int, string[]>();

            if (labelLine.State != -1)
            {
                // It means the master label file have state-level alignment.
                if (labelLine.Segment == null)
                {
                    throw new InvalidDataException(Helper.NeutralFormat("State only available when the segment is available in \"{0}\"", lines[index]));
                }

                List<Segment> segments = new List<Segment> { labelLine.Segment };
                LabelRemainings.Add(labelLine.State, labelLine.Remaining);

                int state = labelLine.State;
                bool expectNextState = true;
                while (expectNextState && index < lines.Count)
                {
                    labelLine = LabelLine.Parse(lines[index++]);
                    expectNextState = labelLine.State > state;
                    if (expectNextState)
                    {
                        segments.Add(labelLine.Segment);
                        LabelRemainings.Add(labelLine.State, labelLine.Remaining);

                        Debug.Assert(
                            Label.CentralPhoneme == labelLine.Label.CentralPhoneme,
                            "The central phones of the labels of different states must be the same");
                    }
                    else
                    {
                        // This line isn't belong to this candidate.
                        --index;
                    }
                }

                StateAlignments = segments.ToArray();
            }
            else
            {
                if (labelLine.Segment != null)
                {
                    StateAlignments = new Segment[1];
                    StateAlignments[0] = labelLine.Segment;
                }

                LabelRemainings.Add(labelLine.State, labelLine.Remaining);
            }
        }

        /// <summary>
        /// Saves the candidate into the lines in the format of master label file.
        /// </summary>
        /// <param name="typeOption">The given label type.</param>
        /// <param name="alignOption">The given alignment data.</param>
        /// <param name="keepRemainingPart">Whether to keep the remaining part.</param>
        /// <returns>The lines contains the candidate information.</returns>
        public string[] Save(LabelTypeOptions typeOption, LabelAlignOptions alignOption, bool keepRemainingPart)
        {
            if (alignOption == LabelAlignOptions.StateAlign)
            {
                string[] result = new string[StateAlignments.Length];
                for (int i = 0; i < StateAlignments.Length; ++i)
                {
                    LabelLine labelHelper = new LabelLine
                    {
                        Label = Label,
                        Segment = StateAlignments[i],
                        State = 2 + i
                    };

                    if (LabelRemainings != null && LabelRemainings.Count > 0)
                    {
                        labelHelper.Remaining = LabelRemainings[2 + i];
                    }

                    // Since Htk state index starts with 2.
                    result[i] = labelHelper.ToString(typeOption, keepRemainingPart);
                }

                return result;
            }

            LabelLine labelLine = new LabelLine { State = -1, Label = Label };
            if (LabelRemainings != null && LabelRemainings.Count > 0)
            {
                if (LabelRemainings.ContainsKey(-1))
                {
                    labelLine.Remaining = LabelRemainings[-1];
                }
                else if (LabelRemainings.ContainsKey(2))
                {
                    labelLine.Remaining = LabelRemainings[2];
                }
            }

            switch (alignOption)
            {
                case LabelAlignOptions.NoAlign:
                    labelLine.Segment = null;
                    break;
                case LabelAlignOptions.PhonemeAlign:
                    if (_stateSegments == null)
                    {
                        throw new InvalidOperationException("Unsupported operation since there is no alignment data in Candidate");
                    }

                    labelLine.Segment = new Segment(StartTime, EndTime);
                    break;
                default:
                    throw new InvalidDataException("Unknown HtkLabelAlignOptions value");
            }

            return new[] { labelLine.ToString(typeOption, keepRemainingPart) };
        }

        #endregion
    }

    /// <summary>
    /// Sentence is used to store the all candidates inside the same sentence.
    /// </summary>
    public class Sentence
    {
        #region Fields

        /// <summary>
        /// The line in the master label file to indicate the end of sentence.
        /// </summary>
        public static readonly string EndOfSentence = ".";

        /// <summary>
        /// The list to contains all the phone segments in the sentence.
        /// </summary>
        private readonly List<PhoneSegment> _phoneSegments = new List<PhoneSegment>();

        /// <summary>
        /// The list to contains all the unit candidates in the sentence.
        /// </summary>
        private readonly List<UnitCandidate> _unitCandidates = new List<UnitCandidate>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the sentence id, which is an unique index to find the sentence.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets trainingSet represents which training set this sentence belongs to.
        /// </summary>
        public TrainingSentenceSet TrainingSet { get; set; }

        /// <summary>
        /// Gets the phone segment which belongs to this sentences.
        /// </summary>
        public IList<PhoneSegment> PhoneSegments
        {
            get
            {
                return _phoneSegments;
            }
        }

        /// <summary>
        /// Gets the unit candidates which belongs to this sentences.
        /// </summary>
        public IList<UnitCandidate> Candidates
        {
            get
            {
                return _unitCandidates;
            }
        }

        /// <summary>
        /// Gets or sets the global start frame index in acoustic data table.
        /// </summary>
        public int GlobalFrameIndex { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads one sentence from the master label file.
        /// Please notice this load may be called multi-times to load the different
        /// Information, such as full-context label, alignment data and so on.
        /// </summary>
        /// <param name="reader">StreamReader of master label file.</param>
        /// <returns>A bool value indicates whether end of sentence exists.</returns>
        public bool Load(StreamReader reader)
        {
            // Load all the lines belong to this sentence.
            bool endOfSentenceExist = false;
            List<string> lines = LoadLines(reader, ref endOfSentenceExist);

            int indexOfLines = 0;
            if (_phoneSegments.Count == 0)
            {
                // The first time to load the phone segment.
                int index = 0;
                int indexOfNonSilence = 0;
                while (indexOfLines < lines.Count)
                {
                    PhoneSegment phoneSegment = new PhoneSegment();
                    phoneSegment.Load(lines, ref indexOfLines);
                    phoneSegment.Index = index++;
                    phoneSegment.IndexOfNonSilence = (!Phoneme.IsSilenceFeature(phoneSegment.Name)) ? indexOfNonSilence++ : -1;
                    phoneSegment.Sentence = this;
                    _phoneSegments.Add(phoneSegment);
                }
            }
            else
            {
                // Reload information about the phone segment.
                try
                {
                    foreach (PhoneSegment phoneSegment in _phoneSegments)
                    {
                        phoneSegment.Load(lines, ref indexOfLines);
                    }
                }
                catch (InvalidDataException e)
                {
                    throw new InvalidDataException("Mismatched data between multi master label files", e);
                }
            }

            if (indexOfLines != lines.Count)
            {
                throw new InvalidDataException("Mismatched data between multi master label files");
            }

            return endOfSentenceExist;
        }

        /// <summary>
        /// Loads head and tail margins for each candidates.
        /// </summary>
        /// <param name="wave">WaveFile from which to load wave data.</param>
        /// <param name="marginLength">Cross correlation margin length in millisecond.</param>
        public void LoadMargin(WaveFile wave, int marginLength)
        {
            foreach (UnitCandidate candidate in Candidates)
            {
                candidate.LoadMargin(wave, marginLength);
            }
        }

        /// <summary>
        /// Saves one sentence into master label file.
        /// </summary>
        /// <param name="writer">StreamWriter to save the sentence to.</param>
        /// <param name="typeOption">The given label type.</param>
        /// <param name="alignOption">The given alignment data.</param>
        /// <param name="keepRemainingPart">Whether to keep the remaining part.</param>
        public void Save(StreamWriter writer, LabelTypeOptions typeOption, LabelAlignOptions alignOption, bool keepRemainingPart)
        {
            foreach (PhoneSegment phoneSegment in _phoneSegments)
            {
                ICollection<string> result = phoneSegment.Save(typeOption, alignOption, keepRemainingPart);
                foreach (string line in result)
                {
                    writer.WriteLine(line);
                }
            }
        }

        /// <summary>
        /// Build Unit Candidate List according to the unit candidate type.
        /// </summary>
        /// <param name="type">Unit type.</param>
        public void BuildUnitCandidates(UnitCandidateType type)
        {
            if (type == UnitCandidateType.Phone)
            {
                foreach (PhoneSegment phoneSegment in PhoneSegments)
                {
                    _unitCandidates.Add(new PhoneCandidate(phoneSegment));
                }
            }
            else if (type == UnitCandidateType.Halfphone)
            {
                foreach (PhoneSegment phoneSegment in PhoneSegments)
                {
                    _unitCandidates.Add(new HalfPhoneCandidate(phoneSegment, true));
                    _unitCandidates.Add(new HalfPhoneCandidate(phoneSegment, false));
                }
            }
            else
            {
                throw new Exception(Helper.NeutralFormat("Unsupported Unit Candidate Type: {0}", type.ToString()));
            }
        }

        /// <summary>
        /// Get start time of state in sentence.
        /// </summary>
        /// <param name="phoneIndex">Phone index.</param>
        /// <param name="stateIndex">State index.</param>
        /// <returns>Start time.</returns>
        public long GetStateStartTime(int phoneIndex, int stateIndex)
        {
            if (phoneIndex >= PhoneSegments.Count() || phoneIndex < 0)
            {
                throw new InvalidDataException(
                    Helper.NeutralFormat("PhoneIndex {0} doesn't existed in sentence.", phoneIndex));
            }

            if (stateIndex >= PhoneSegments[phoneIndex].StateAlignments.Count() || stateIndex < 0)
            {
                throw new InvalidDataException(
                    Helper.NeutralFormat("StateIndex {0} doesn't existed in PhoneSegments [{1}] in sentence.", stateIndex, phoneIndex));
            }

            return PhoneSegments[phoneIndex].StateAlignments[stateIndex].StartTime;
        }

        /// <summary>
        /// Get end time of state in sentence.
        /// </summary>
        /// <param name="phoneIndex">Phone index.</param>
        /// <param name="stateIndex">State index.</param>
        /// <returns>End time.</returns>
        public long GetStateEndTime(int phoneIndex, int stateIndex)
        {
            if (phoneIndex >= PhoneSegments.Count() || phoneIndex < 0)
            {
                throw new InvalidDataException(
                    Helper.NeutralFormat("PhoneIndex {0} doesn't existed in sentence.", phoneIndex));
            }

            if (stateIndex >= PhoneSegments[phoneIndex].StateAlignments.Count() || stateIndex < 0)
            {
                throw new InvalidDataException(
                    Helper.NeutralFormat("StateIndex {0} doesn't existed in PhoneSegments [{1}] in sentence.", stateIndex, phoneIndex));
            }

            return PhoneSegments[phoneIndex].StateAlignments[stateIndex].EndTime;
        }

        /// <summary>
        /// Get duration time of state.
        /// </summary>
        /// <param name="phoneIndex">Phone index.</param>
        /// <param name="stateIndex">State index.</param>
        /// <returns>Duration Time.</returns>
        public long GetStateDurTime(int phoneIndex, int stateIndex)
        {
            return GetStateEndTime(phoneIndex, stateIndex) - GetStateStartTime(phoneIndex, stateIndex);
        }

        /// <summary>
        /// Get duration time of all states in a phone.
        /// </summary>
        /// <param name="phoneIndex">Phone index.</param>
        /// <returns>Duration Times.</returns>
        public long[] GetAllStatesDurTime(int phoneIndex)
        {
            if (phoneIndex >= PhoneSegments.Count() || phoneIndex < 0)
            {
                throw new InvalidDataException(
                    Helper.NeutralFormat("PhoneIndex {0} doesn't existed in sentence.", phoneIndex));
            }

            int stateNum = PhoneSegments[phoneIndex].StateAlignments.Count();
            long[] durTimes = new long[stateNum];
            for (int i = 0; i < durTimes.Length; i++)
            {
                durTimes[i] = GetStateDurTime(phoneIndex, i);
            }

            return durTimes;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads the lines belong to this sentence from the given stream reader.
        /// </summary>
        /// <param name="reader">The given reader to load the lines.</param>
        /// <param name="endOfSentenceExist">A bool value indicates whether end of sentence exists.</param>
        /// <returns>The lines belong this sentence.</returns>
        private static List<string> LoadLines(StreamReader reader, ref bool endOfSentenceExist)
        {
            // Load all the lines belong to this sentence.
            List<string> lines = new List<string>();
            string line = string.Empty;
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                if (line != EndOfSentence)
                {
                    lines.Add(line);
                }
                else
                {
                    endOfSentenceExist = true;
                    break;
                }
            }

            return lines;
        }

        #endregion
    }

    /// <summary>
    /// Manipulate the Htk training data.
    /// </summary>
    public class TrainingSentenceSet
    {
        #region Fields

        /// <summary>
        /// The header of master label file.
        /// </summary>
        private const string MasterLabelFileHeader = "#!MLF!#";

        /// <summary>
        /// The regex to match the sentence id.
        /// </summary>
        private static readonly Regex RegexOfSentId = new Regex(@"""\*\/(.+)\.lab""");

        /// <summary>
        /// The string used to format sentence id into master label file.
        /// </summary>
        private const string SentIdFormatString = "\"*/{0}.lab\"";

        /// <summary>
        /// The key is the sentence id, and the value is the Sentence identified by the id.
        /// </summary>
        private readonly SortedDictionary<string, Sentence> _idKeyedSentences = new SortedDictionary<string, Sentence>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the key is the sentence id, and the value is the Sentence identified by the id.
        /// </summary>
        public IDictionary<string, Sentence> Sentences
        {
            get
            {
                return _idKeyedSentences;
            }
        }

        /// <summary>
        /// Gets or sets the file list map, which hold the directories structure.
        /// </summary>
        public FileListMap FileListMap { get; set; }

        /// <summary>
        /// Gets or sets the map, which gives all possible unit candidate one unique ID, 
        /// The Ids will be used in pre-selection module and UNT file generation
        /// The map will be generated in BuildUnitCandidateList().
        /// </summary>
        public Dictionary<string, int> UnitCandidateNameIds { get; set; }

        /// <summary>
        /// Gets or sets the millisecond per frame. The candidate's frame index will depends on this value. 
        /// </summary>
        public int MilliSecondsPerFrame { get; set; }

        #endregion

        #region public Methods

        /// <summary>
        /// Builds the file list and then save them to file.
        /// </summary>
        /// <param name="masterLabelFile">The file name of the master label file.</param>
        /// <param name="corpusPath">Path of the corpus for which file list will be built.</param>
        /// <param name="fileListFile">Path of the target fileList.</param>
        /// <param name="extension">Extension of the corpus.</param>
        public static void GenerateFileListFile(string masterLabelFile,
            string corpusPath, string fileListFile, string extension)
        {
            TrainingSentenceSet trainingSet = new TrainingSentenceSet();
            trainingSet.Load(masterLabelFile);
            trainingSet.GenerateFileListFile(corpusPath, fileListFile, extension);
        }

        /// <summary>
        /// Search specific files in one directory and then save them as a file list.
        /// </summary>
        /// <param name="fileDirectory">Directory which the files to be searched.</param>
        /// <param name="fileListFile">Path of the target fileList.</param>
        /// <param name="extension">File extension to be searched.</param>
        /// <param name="searchOption">Search option, include subdirectory or not.</param>
        public static void GenerateFileListFile(string fileDirectory, string fileListFile, string extension, SearchOption searchOption)
        {
            using (StreamWriter sw = new StreamWriter(fileListFile))
            {
                foreach (string file in Directory.GetFiles(fileDirectory, extension, searchOption))
                {
                    sw.WriteLine(file);
                }
            }
        }

        /// <summary>
        /// Converts the master label file to label files.
        /// </summary>
        /// <param name="mlfFileName">The name of target master label file.</param>
        /// <param name="alignmentDir">The directory of the alignment files.</param>
        /// <param name="alignOption">The given alignment data.</param>
        public static void ConvertMlfToLabelFiles(string mlfFileName, string alignmentDir, LabelAlignOptions alignOption)
        {
            TrainingSentenceSet set = new TrainingSentenceSet();
            set.Load(mlfFileName);

            foreach (KeyValuePair<string, Sentence> pair in set.Sentences)
            {
                string labelFile = FileExtensions.AppendExtensionName(pair.Key, FileExtensions.LabelFile);
                using (StreamWriter sw = new StreamWriter(Path.Combine(alignmentDir, labelFile)))
                {
                    pair.Value.Save(sw, LabelTypeOptions.FullContext, alignOption, true);
                }
            }
        }

        /// <summary>
        /// Converts label files to master label file.
        /// </summary>
        /// <param name="alignmentDir">The directory of alignment files.</param>
        /// <param name="mlfFileName">The name of target master label file.</param>
        public static void ConvertLabelFilesToMlf(string alignmentDir, string mlfFileName)
        {
            TrainingSentenceSet set = new TrainingSentenceSet();
            foreach (string labelFile in Directory.GetFiles(alignmentDir, Helper.NeutralFormat("*.{0}", FileExtensions.LabelFile)))
            {
                Sentence sentence = new Sentence();
                using (StreamReader sr = new StreamReader(labelFile))
                {
                    if (sentence.Load(sr))
                    {
                        throw new InvalidDataException("Sentence end is not expected");
                    }
                }

                string id = Path.GetFileNameWithoutExtension(labelFile);
                set.Sentences.Add(id, sentence);
            }

            set.Save(mlfFileName, LabelTypeOptions.FullContext, LabelAlignOptions.StateAlign, true);
        }

        /// <summary>
        /// Build unit candidate list from phone segment list.
        /// </summary>
        /// <param name="type">Unit candidate type.</param>
        public void BuildUnitCandidateList(UnitCandidateType type)
        {
            foreach (Sentence sentence in Sentences.Values)
            {
                sentence.BuildUnitCandidates(type);
            }

            // create unit candidate Id
            UnitCandidateNameIds = new Dictionary<string, int>();
            int id = 0;
            foreach (string candidateName in
                Sentences.Values.SelectMany(sent => sent.Candidates).Select(cand => cand.Name).Distinct().OrderBy(s => s))
            {
                UnitCandidateNameIds[candidateName] = id++;
            }

            TagIds();
        }

        /// <summary>
        /// Loads full-context label or mono align label from master label file.
        /// </summary>
        /// <param name="masterLabelFile">The file name of the master label file.</param>
        public void Load(string masterLabelFile)
        {
            using (StreamReader reader = new StreamReader(masterLabelFile))
            {
                // This is the header of master label file.
                string line = reader.ReadLine();
                if (line != MasterLabelFileHeader)
                {
                    throw new InvalidDataException(Helper.NeutralFormat("Master label file header expected, but input \"{0}\"", line));
                }

                while (!reader.EndOfStream)
                {
                    // Read the line for sentence id.
                    line = reader.ReadLine();
                    Match match = RegexOfSentId.Match(line);
                    if (match.Success)
                    {
                        bool endOfSentenceExist;
                        string sentId = match.Groups[1].Value;
                        if (_idKeyedSentences.ContainsKey(sentId))
                        {
                            // Not the first time to load the sentence.
                            endOfSentenceExist = _idKeyedSentences[sentId].Load(reader);
                        }
                        else
                        {
                            // Load the sentence in first time.
                            Sentence sentence = new Sentence { Id = sentId, TrainingSet = this };
                            endOfSentenceExist = sentence.Load(reader);
                            _idKeyedSentences[sentId] = sentence;
                        }

                        if (!endOfSentenceExist)
                        {
                            throw new InvalidDataException("Sentence end is expected");
                        }
                    }
                    else
                    {
                        throw new InvalidDataException(Helper.NeutralFormat("Sentence id expected, but input \"{0}\"", line));
                    }
                }
            }
        }

        /// <summary>
        /// Saves the full-context label or mono align label into master label file.
        /// </summary>
        /// <param name="masterLabelFile">The file name of the master label file for full-context label.</param>
        /// <param name="typeOption">The given label type.</param>
        /// <param name="alignOption">The given alignment data.</param>
        /// <param name="keepRemainingPart">Whether to keep the remaining part.</param>
        public void Save(string masterLabelFile, LabelTypeOptions typeOption, LabelAlignOptions alignOption, bool keepRemainingPart)
        {
            using (StreamWriter writer = new StreamWriter(masterLabelFile, false, Encoding.ASCII))
            {
                writer.WriteLine(MasterLabelFileHeader);

                foreach (KeyValuePair<string, Sentence> kvp in _idKeyedSentences)
                {
                    writer.WriteLine(SentIdFormatString, kvp.Key);
                    kvp.Value.Save(writer, typeOption, alignOption, keepRemainingPart);
                    writer.WriteLine(Sentence.EndOfSentence);
                }
            }
        }

        /// <summary>
        /// Loads head and tail margins for candidates of all sentences.
        /// </summary>
        /// <param name="waveDir">Wave directory.</param>
        /// <param name="marginLength">Cross correlation margin length in millisecond.</param>
        public void LoadMargin(string waveDir, int marginLength)
        {
            foreach (string sid in _idKeyedSentences.Keys)
            {
                string waveName = FileListMap.BuildPath(FileListMap, waveDir, sid, FileExtensions.Waveform);

                if (!File.Exists(waveName))
                {
                    throw new FileNotFoundException(Helper.NeutralFormat("Wave file is not found \"{0}\".", waveName));
                }

                WaveFile wave = new WaveFile();
                wave.Load(waveName);
                _idKeyedSentences[sid].LoadMargin(wave, marginLength);
            }
        }

        /// <summary>
        /// Generates the label list and then save them to file.
        /// </summary>
        /// <param name="labelListFile">The file name of the label list file.</param>
        /// <param name="typeOption">The given label type.</param>
        public void GenerateLabelListFile(string labelListFile, LabelTypeOptions typeOption)
        {
            SortedDictionary<string, object> labelList = (typeOption == LabelTypeOptions.MonoPhoneme) ?
                new SortedDictionary<string, object>(StringComparer.Ordinal) :
                new SortedDictionary<string, object>();
            foreach (Sentence sentence in _idKeyedSentences.Values)
            {
                foreach (PhoneSegment candidate in sentence.PhoneSegments)
                {
                    string value;
                    switch (typeOption)
                    {
                        case LabelTypeOptions.MonoPhoneme:
                            value = candidate.Name;
                            break;
                        case LabelTypeOptions.FullContext:
                            value = candidate.FullContextLabel;
                            break;
                        default:
                            throw new InvalidDataException("Unknown HtkLabelTypeOptions value");
                    }

                    if (!labelList.ContainsKey(value))
                    {
                        labelList.Add(value, null);
                    }
                }
            }

            using (StreamWriter writer = new StreamWriter(labelListFile, false, Encoding.ASCII))
            {
                foreach (string label in labelList.Keys)
                {
                    writer.WriteLine(label);
                }
            }
        }

        /// <summary>
        /// Build the file list and then save them to file.
        /// </summary>
        /// <param name="corpusPath">Path of the corpus for which file list will be built.</param>
        /// <param name="fileListFile">Path of the target fileList.</param>
        /// <param name="extension">Extension of the corpus.</param>
        public void GenerateFileListFile(string corpusPath, string fileListFile, string extension)
        {
            Helper.EnsureFolderExistForFile(fileListFile);
            using (StreamWriter writer = new StreamWriter(fileListFile))
            {
                foreach (string sentenceId in Sentences.Keys)
                {
                    writer.WriteLine(FileListMap.BuildPath(FileListMap, corpusPath, sentenceId, extension).ToHtkPath());
                }
            }
        }

        /// <summary>
        /// Tag subset candidates id according to the its sentence and position.
        /// </summary>
        /// <param name="subset">Subset of training sentences.</param>
        public void TagSubsetIds(TrainingSentenceSet subset)
        {
            foreach (Sentence subsetSentence in subset.Sentences.Values)
            {
                foreach (UnitCandidate subsetCandidate in subsetSentence.Candidates)
                {
                    var fullSetCandidate = GetCandidate(subsetSentence.Id, subsetCandidate.Index);

                    if (fullSetCandidate == null)
                    {
                        throw new InvalidDataException("Invalid candidate info.");
                    }

                    subsetCandidate.Id = fullSetCandidate.Id;
                    subsetCandidate.GlobalId = fullSetCandidate.GlobalId;
                }
            }
        }

        /// <summary>
        /// Gets a candidate by its sentence id and index.
        /// </summary>
        /// <param name="sentenceId">Sentence id of the candidate.</param>
        /// <param name="index">Index of the candidate.</param>
        /// <returns>The candidate if exists; null otherwise.</returns>
        public UnitCandidate GetCandidate(string sentenceId, int index)
        {
            UnitCandidate ret = null;

            if (Sentences.ContainsKey(sentenceId))
            {
                var sentence = Sentences[sentenceId];

                if (index < sentence.Candidates.Count)
                {
                    ret = sentence.Candidates[index];
                }
            }

            return ret;
        }

        /// <summary>
        /// Splits the training set into several pieces, the sentence count in each piece will be less than the
        /// Given value. This function is usually used to split the training set to perform computation in parallel.
        /// Please notice:
        ///  1. There is no same label in each piece.
        ///  2. The count of training set will be even since the count of CPU is always even.
        ///  3. The sentences in each piece will be balance as possible.
        /// </summary>
        /// <param name="maxSentenceInPiece">Max sentence count in one piece.</param>
        /// <returns>Array of training sentence set.</returns>
        public TrainingSentenceSet[] LabelFreeSplit(int maxSentenceInPiece)
        {
            int pieceCount = CalculatePiecesNumber(maxSentenceInPiece);
            int subSetSize = (int)Math.Ceiling((double)Sentences.Count / pieceCount);

            // The key is the full-context label, and the value is the sentence collection that has the full-context label.
            Dictionary<string, Collection<string>> fullContextLabeledSentenceIds = new Dictionary<string, Collection<string>>();
            foreach (Sentence sentence in Sentences.Values)
            {
                foreach (PhoneSegment candidate in sentence.PhoneSegments)
                {
                    if (!fullContextLabeledSentenceIds.ContainsKey(candidate.FullContextLabel))
                    {
                        fullContextLabeledSentenceIds.Add(candidate.FullContextLabel, new Collection<string>());
                    }

                    fullContextLabeledSentenceIds[candidate.FullContextLabel].Add(candidate.Sentence.Id);
                }
            }

            TrainingSentenceSet[] trainingSets = new TrainingSentenceSet[pieceCount];

            Dictionary<string, object> currentSentences = new Dictionary<string, object>();
            Dictionary<string, object> usedSentences = new Dictionary<string, object>();

            IEnumerator<KeyValuePair<string, Sentence>> mapSentenceEnum = _idKeyedSentences.GetEnumerator();

            for (int pieceIndex = 0; pieceIndex < pieceCount; pieceIndex++)
            {
                trainingSets[pieceIndex] = new TrainingSentenceSet { FileListMap = FileListMap };

                while (trainingSets[pieceIndex].Sentences.Count < subSetSize && usedSentences.Count < Sentences.Count)
                {
                    if (!mapSentenceEnum.MoveNext())
                    {
                        break;
                    }

                    if (!usedSentences.ContainsKey(mapSentenceEnum.Current.Key))
                    {
                        currentSentences.Clear();
                        currentSentences.Add(mapSentenceEnum.Current.Key, null);

                        TouchDuplicateLabels(trainingSets[pieceIndex], currentSentences, usedSentences, fullContextLabeledSentenceIds, subSetSize);
                    }
                }
            }

            return trainingSets;
        }

        #endregion

        #region private Methods

        /// <summary>
        /// Calculate how many pieces should be splitted into.
        /// </summary>
        /// <param name="maxSentenceInPiece">Max sentence count in one piece.</param>
        /// <returns>Piece count to split into.</returns>
        private int CalculatePiecesNumber(int maxSentenceInPiece)
        {
            int pieceCount;
            int sentenceCount = _idKeyedSentences.Count;
            if (sentenceCount <= maxSentenceInPiece)
            {
                pieceCount = 1;
            }
            else
            {
                pieceCount = (int)Math.Ceiling((double)sentenceCount / maxSentenceInPiece);
                if (pieceCount % 2 == 1)
                {
                    pieceCount += 1;
                }
            }

            return pieceCount;
        }

        /// <summary>
        /// Find duplicated full context labels, and put sentences with same full context labels in same piece.
        /// </summary>
        /// <param name="sentenceSet">Training sentence set.</param>
        /// <param name="currentSentences">Current sentences whose labels will be checked.</param>
        /// <param name="usedSentences">Sentences that have been checked.</param>
        /// <param name="fullContextLabeledSentenceIds">Map of full context label to sentence collection.</param>
        /// <param name="subSetSize">Max set size (sentence count).</param>
        private void TouchDuplicateLabels(
            TrainingSentenceSet sentenceSet,
            Dictionary<string, object> currentSentences,
            IDictionary<string, object> usedSentences,
            IDictionary<string, Collection<string>> fullContextLabeledSentenceIds,
            int subSetSize)
        {
            Dictionary<string, object> nextSentences = new Dictionary<string, object>();

            foreach (string currentSentenceId in currentSentences.Keys)
            {
                if (!sentenceSet._idKeyedSentences.ContainsKey(currentSentenceId))
                {
                    sentenceSet._idKeyedSentences.Add(currentSentenceId, _idKeyedSentences[currentSentenceId]);
                    usedSentences.Add(currentSentenceId, null);
                }

                foreach (PhoneSegment sourceCandidate in _idKeyedSentences[currentSentenceId].PhoneSegments)
                {
                    foreach (string nextSentenceId in fullContextLabeledSentenceIds[sourceCandidate.FullContextLabel])
                    {
                        if (!usedSentences.ContainsKey(nextSentenceId))
                        {
                        sentenceSet._idKeyedSentences.Add(nextSentenceId, _idKeyedSentences[nextSentenceId]);
                            usedSentences.Add(nextSentenceId, null);
                            nextSentences.Add(nextSentenceId, null);
                        }
                    }
                }
            }

            if (nextSentences.Keys.Count > 0 && sentenceSet.Sentences.Count < subSetSize && usedSentences.Count < FileListMap.Map.Count)
            {
                TouchDuplicateLabels(sentenceSet, nextSentences, usedSentences, fullContextLabeledSentenceIds, subSetSize);
            }
        }

        /// <summary>
        /// Tags the candidate id and candidate group id.
        /// </summary>
        private void TagIds()
        {
            // Reset the id in the sentence set.
            foreach (Sentence sentence in Sentences.Values)
            {
                foreach (UnitCandidate candidate in sentence.Candidates)
                {
                    candidate.ResetId();
                }
            }

            // Tag a new id for candidate in every sentences
            Dictionary<string, int> candidateIds = new Dictionary<string, int>();
            foreach (Sentence sentence in Sentences.Values)
            {
                foreach (UnitCandidate candidate in sentence.Candidates.Where(c => !c.SilenceCandidate))
                {
                    if (!candidateIds.ContainsKey(candidate.Name))
                    {
                        candidateIds.Add(candidate.Name, 0);
                    }

                    if (candidate.Id == UnitCandidate.InvalidId)
                    {
                        candidate.Id = candidateIds[candidate.Name];
                        candidateIds[candidate.Name] = candidate.Id + 1;
                    }
                }
            }

            int maxCandidateId = candidateIds.Values.Max();

            // Tag a the global id
            // sort the candidate group by the rule, firstly consider the name Ids, secondly consider the candidate.Id
            int globalId = 0;
            List<UnitCandidate> allCandidates = new List<UnitCandidate>(
                Sentences.Values.SelectMany(s => s.Candidates.Where(c => c.Id != UnitCandidate.InvalidId)));

            foreach (UnitCandidate candidate in allCandidates.OrderBy(c => UnitCandidateNameIds[c.Name]).ThenBy(c => c.Id))
            {
                if (candidate.GlobalId == UnitCandidate.InvalidId)
                {
                    candidate.GlobalId = globalId++;
                }
            }
        }

        #endregion
    }
}