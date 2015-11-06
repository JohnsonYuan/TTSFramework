//----------------------------------------------------------------------------
// <copyright file="PreselectionData.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines the pre-selection data used in RUS pre-selection.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Postion kind of wave snippet for candidate.
    /// </summary>
    public enum PositionKind
    {
        /// <summary>
        /// Head of candidate.
        /// </summary>
        Head,

        /// <summary>
        /// Tail of candidate.
        /// </summary>
        Tail
    }

    /// <summary>
    /// The leaf node for pre-selection, which contains many candidates here as a candidate group.
    /// </summary>
    public class CandidateGroup
    {
        #region Fields

        /// <summary>
        /// The all candidates in this gourp.
        /// </summary>
        private readonly List<UnitCandidate> _candidates = new List<UnitCandidate>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets group Id which is an unique index for each group.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets group name. Currently, it is a tri-phone name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets all the candidates which belong to this group.
        /// </summary>
        public ICollection<UnitCandidate> Candidates
        {
            get
            {
                return _candidates;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads the candidate group from file.
        /// </summary>
        /// <param name="reader">The StreamReader of candidate group data file.</param>
        /// <param name="sentenceSet">Sentence set where to find the candidates.</param>
        public void Load(StreamReader reader, TrainingSentenceSet sentenceSet)
        {
            if (reader.EndOfStream)
            {
                throw new InvalidDataException("Unexpected end of stream");
            }

            char[] splitter = new[] { ' ' };
            string line = reader.ReadLine();
            string[] columns = line.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            if (columns.Length != 3)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Unsupported format here \"{0}\"", line));
            }

            Name = columns[0];
            Id = int.Parse(columns[1]);
            int count = int.Parse(columns[2]);
            while (count != 0)
            {
                if (reader.EndOfStream)
                {
                    throw new InvalidDataException("Unexpected end of stream");
                }

                line = reader.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                // e.g. the format should be
                // sentID IndexOfNonSilence FullContextLabel MustHoldFlag
                columns = line.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length != 4)
                {
                    throw new InvalidDataException(Helper.NeutralFormat("Unsupported format here \"{0}\"", line));
                }

                UnitCandidate candidate = FindCandidate(sentenceSet, columns[0], int.Parse(columns[1]), columns[2]);
                if (bool.Parse(columns[3]))
                {
                    candidate.MustHold = true;
                }

                _candidates.Add(candidate);
                --count;
            }
        }

        /// <summary>
        /// Saves candidate group to file.
        /// </summary>
        /// <param name="fileWriter">The StreamWriter of the candidate group data file.</param>
        public void Save(StreamWriter fileWriter)
        {
            fileWriter.WriteLine("{0} {1} {2}", Name, Id, _candidates.Count);
            foreach (UnitCandidate candidate in _candidates)
            {
                fileWriter.WriteLine(
                    "{0} {1} {2} {3}",
                    candidate.Sentence.Id,
                    candidate.IndexOfNonSilence,
                    candidate.FullContextLabel,
                    candidate.MustHold);
            }
        }

        /// <summary>
        /// Builds margins for the candidate group.
        /// </summary>
        /// <param name="position">Position of wave margin.</param>
        /// <returns>A list of margins.</returns>
        public List<short[]> BuildMargins(PositionKind position)
        {
            List<short[]> margins = new List<short[]>();
            foreach (UnitCandidate candidate in Candidates)
            {
                if (position == PositionKind.Head)
                {
                    margins.Add(candidate.HeadMargin);
                }
                else if (position == PositionKind.Tail)
                {
                    margins.Add(candidate.TailMargin);
                }
                else
                {
                    throw new ArgumentException(
                        Helper.NeutralFormat("Unsupported position kind \"{0}\"", position.ToString()));
                }
            }

            return margins;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Finds the corresponding candidate in the given sentence set.
        /// </summary>
        /// <param name="sentenceSet">The given sentence set.</param>
        /// <param name="sentId">The sentence id which contains the candidate.</param>
        /// <param name="indexOfNonSilence">The index of non-silence unit of the candidate.</param>
        /// <param name="label">The label of the candidate.</param>
        /// <returns>The corresponding candidate.</returns>
        private static UnitCandidate FindCandidate(TrainingSentenceSet sentenceSet, string sentId, int indexOfNonSilence, string label)
        {
            if (!sentenceSet.Sentences.ContainsKey(sentId))
            {
                throw new InvalidDataException(Helper.NeutralFormat("Cannot find the sentence \"{0}\"", sentId));
            }

            Sentence sentence = sentenceSet.Sentences[sentId];
            UnitCandidate result = null;
            foreach (UnitCandidate candidate in sentence.Candidates)
            {
                if (candidate.IndexOfNonSilence == indexOfNonSilence)
                {
                    result = candidate;
                    break;
                }
            }

            if (result == null)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Cannot find the candidate \"{0}:{1}\"", sentId, indexOfNonSilence));
            }

            Label myLabel = new Label { Text = label };
            if (result.Label.CentralPhoneme != myLabel.CentralPhoneme)
            {
                throw new InvalidDataException(
                    Helper.NeutralFormat(
                    "Mismatched full-context label, expected current phone \"{0}\" but \"{1}\"",
                    result.Label.CentralPhoneme,
                    myLabel.CentralPhoneme));
            }

            return result;
        }

        #endregion
    }

    /// <summary>
    /// Pre-selection data object model, used to save/load/compiler the pre-selection data.
    /// </summary>
    public class PreSelectionData
    {
        #region Fields

        /// <summary>
        /// The key is the name of CandidateGroup (same as the leaf nodes' name of pre-selection decision forest), and the value is the specified CandidateGroup.
        /// </summary>
        private readonly Dictionary<string, CandidateGroup> _nameIndexedCandidateGroup;

        /// <summary>
        /// The pre-selection decision forest.
        /// </summary>
        private DecisionForest _decisionForest;

        /// <summary>
        /// The related sentence set.
        /// </summary>
        private TrainingSentenceSet _sentenceSet;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the PreSelectionData class as an empty object.
        /// </summary>
        public PreSelectionData()
        {
            _nameIndexedCandidateGroup = new Dictionary<string, CandidateGroup>();
        }

        /// <summary>
        /// Initializes a new instance of the PreSelectionData class according to given forest and sentenceSet.
        /// </summary>
        /// <param name="forest">The given forest.</param>
        /// <param name="sentenceSet">The given sentence set where to find candiates.</param>
        /// <param name="fullFeatureNameSet">The full feature set to parse tree.</param>
        public PreSelectionData(DecisionForest forest, TrainingSentenceSet sentenceSet, LabelFeatureNameSet fullFeatureNameSet)
        {
            if (forest == null)
            {
                throw new ArgumentNullException("forest");
            }

            if (sentenceSet == null)
            {
                throw new ArgumentNullException("sentenceSet");
            }

            if (fullFeatureNameSet == null)
            {
                throw new ArgumentNullException("fullFeatureNameSet");
            }

            _decisionForest = forest;
            _sentenceSet = sentenceSet;
            _nameIndexedCandidateGroup = new Dictionary<string, CandidateGroup>();

            // Create empty candidate group.
            foreach (DecisionTree tree in forest.TreeList)
            {
                foreach (DecisionTreeNode node in tree.LeafNodeMap.Values)
                {
                    CandidateGroup candidateGroup = new CandidateGroup
                    {
                        Name = node.Name,
                        Id = _nameIndexedCandidateGroup.Count
                    };

                    _nameIndexedCandidateGroup.Add(candidateGroup.Name, candidateGroup);
                }
            }

            // Travel the training sentence set to find the corresponding candidates.
            foreach (Sentence sentence in sentenceSet.Sentences.Values)
            {
                foreach (UnitCandidate candidate in sentence.Candidates)
                {
                    if (!candidate.SilenceCandidate)
                    {
                        candidate.Label.FeatureNameSet = fullFeatureNameSet;
                        DecisionTree[] linkedDecisionTrees = forest.TreeList.Where(t => t.Name == candidate.Name).ToArray();
                        Debug.Assert(linkedDecisionTrees.Length == 1,
                            Helper.NeutralFormat("Invalidated: More than 1 {0} Preselection tree are linked to unit {1}", linkedDecisionTrees.Length, candidate.Name));

                        DecisionTreeNode leafNode = DecisionForestExtension.FilterTree(linkedDecisionTrees[0].NodeList[0], forest.Questions, candidate.Label);
                        Debug.Assert(leafNode != null, Helper.NeutralFormat("cannot find leaf node for candidate {0} in sentence {1}", candidate.Name, sentence.Id));

                        _nameIndexedCandidateGroup[leafNode.Name].Candidates.Add(candidate);
                    }
                }
            }

            // Verify there is no empty candidate group.
            foreach (CandidateGroup candidateGroup in _nameIndexedCandidateGroup.Values)
            {
                if (candidateGroup.Candidates.Count <= 0)
                {
                    throw new InvalidDataException(
                        Helper.NeutralFormat("There is no candidate in candidate group \"{0}\"", candidateGroup.Name));
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the all pre-selection decision trees.
        /// </summary>
        public DecisionForest PreselectionForest
        {
            get
            {
                return _decisionForest;
            }
        }

        /// <summary>
        /// Gets the all candidate groups in pre-selection.
        /// </summary>
        public ICollection<CandidateGroup> CandidateGroups
        {
            get
            {
                return _nameIndexedCandidateGroup.Values;
            }
        }

        /// <summary>
        /// Gets the name - candidate groups in pre-selection.
        /// </summary>
        public IDictionary<string, CandidateGroup> CandidateGroupsDictionary
        {
            get
            {
                return _nameIndexedCandidateGroup;
            }
        }

        /// <summary>
        /// Gets distinct phone names.
        /// </summary>
        public string[] Names
        {
            get
            {
                return _nameIndexedCandidateGroup.Values
                    .Select(g => g.Candidates.First().Name).Distinct().ToArray();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Saves the pre-selection data as text.
        /// </summary>
        /// <param name="forestFile">The file name of decision forest.</param>
        /// <param name="candidateGroupFile">The file name of candidate group data.</param>
        public void SaveAsText(string forestFile, string candidateGroupFile)
        {
            _decisionForest.Save(forestFile);
            using (StreamWriter fileWriter = new StreamWriter(candidateGroupFile, false, Encoding.ASCII))
            {
                CandidateGroup[] groups = new CandidateGroup[_nameIndexedCandidateGroup.Count];
                foreach (KeyValuePair<string, CandidateGroup> kvp in _nameIndexedCandidateGroup)
                {
                    groups[kvp.Value.Id] = kvp.Value;
                }

                foreach (CandidateGroup group in groups)
                {
                    group.Save(fileWriter);
                }
            }
        }

        /// <summary>
        /// Loads the pre-selection data from text file.
        /// </summary>
        /// <param name="forestFile">The file name of decision forest.</param>
        /// <param name="candidateGroupFile">The file name of candidate group data.</param>
        /// <param name="sentenceSet">The given sentence set where to find candidates.</param>
        public void LoadFromText(string forestFile, string candidateGroupFile, TrainingSentenceSet sentenceSet)
        {
            _sentenceSet = sentenceSet;
            _decisionForest = new DecisionForest("pre-selection");
            _decisionForest.Load(forestFile);
            using (StreamReader fileReader = new StreamReader(candidateGroupFile))
            {
                while (!fileReader.EndOfStream)
                {
                    CandidateGroup candidateGroup = new CandidateGroup();
                    candidateGroup.Load(fileReader, sentenceSet);
                    _nameIndexedCandidateGroup.Add(candidateGroup.Name, candidateGroup);
                }
            }

            // Each leaf node must be in the candidate groups.
            int countOfLeafNodes = 0;
            foreach (DecisionTree tree in _decisionForest.TreeList)
            {
                countOfLeafNodes += tree.LeafNodeMap.Count;
                foreach (DecisionTreeNode node in tree.LeafNodeMap.Values)
                {
                    if (!_nameIndexedCandidateGroup.ContainsKey(node.Name))
                    {
                        throw new InvalidDataException(
                            Helper.NeutralFormat("Mismatched between file \"{0}\" and \"{1}\"", forestFile, candidateGroupFile));
                    }
                }
            }

            // Ensure candidate id is continuous and starts with zero.
            List<int> expected = new List<int>();
            for (int i = 0; i < _nameIndexedCandidateGroup.Count; ++i)
            {
                expected.Add(i);
            }

            if (!Helper.Compare(expected, _nameIndexedCandidateGroup.Select(pair => pair.Value.Id).ToArray(), true))
            {
                throw new InvalidDataException("The candidate group id should be continuous and starts with zero");
            }

                // The count of candidate group must be equal to the count of leaf nodes.
            if (countOfLeafNodes != _nameIndexedCandidateGroup.Count)
            {
                throw new InvalidDataException(
                    Helper.NeutralFormat("Mismatched between file \"{0}\" and \"{1}\"", forestFile, candidateGroupFile));
            }
        }

        /// <summary>
        /// Gets candidates of a phone, ordered by their id.
        /// </summary>
        /// <param name="phone">The phone name.</param>
        /// <returns>The candidates of the phone.</returns>
        public UnitCandidate[] GetCandidatesByPhone(string phone)
        {
            return _nameIndexedCandidateGroup.Values.SelectMany(g => g.Candidates)
                .Where(c => c.Name == phone).OrderBy(c => c.Id).ToArray();
        }

        /// <summary>
        /// Build cross correlation matrix.
        /// </summary>
        /// <param name="phoneList">Mono phone list.</param>
        /// <returns>Cross correlation matrix.</returns>
        public List<Pair<CandidateGroup, CandidateGroup>> BuildCCMatrix(IEnumerable<string> phoneList)
        {
            List<Pair<CandidateGroup, CandidateGroup>> matrix = new List<Pair<CandidateGroup, CandidateGroup>>();

            foreach (CandidateGroup group in CandidateGroups)
            {
                Label label = new Label(LabelFeatureNameSet.Triphone) { Text = group.Name };
                foreach (string phone in phoneList)
                {
                    string triphone = Helper.NeutralFormat("{0}-{1}+{2}", label.CentralPhoneme, label.RightPhoneme, phone);
                    if (_nameIndexedCandidateGroup.ContainsKey(triphone))
                    {
                        Pair<CandidateGroup, CandidateGroup> pair =
                            new Pair<CandidateGroup, CandidateGroup>(group, _nameIndexedCandidateGroup[triphone]);
                        matrix.Add(pair);
                    }
                }
            }

            return matrix;
        }

        #endregion
    }

    /// <summary>
    /// The distribution information for Hts decision tree.
    /// </summary>
    public class Distribution
    {
        #region Properties

        /// <summary>
        /// Gets or sets the unique identifier for the distribution. Here it will be the stream offset.
        /// For multi-stream scenario (i.e. logf0), it should be the first stream offset.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the distribution, exactly, it is the leaf nodes' name of HTS decision forest.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the state which this leaf nodes' represented.
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// Gets or sets the streams which this leaf nodes' represented.
        /// </summary>
        public int[] Streams { get; set; }

        #endregion
    }
}