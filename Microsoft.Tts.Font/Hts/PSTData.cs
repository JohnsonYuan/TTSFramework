//----------------------------------------------------------------------------
// <copyright file="PSTData.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines classes to write data tables.
// </summary>
//----------------------------------------------------------------------------
namespace Microsoft.Tts.Font
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.Font.Hts;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// PST Data Load from PST file directly.
    /// </summary>
    public class PSTData : IDisposable
    {
        private StringPool _stringPool = new StringPool();
        private bool _disposed = false;

        public PSTData()
        {
        }

        /// <summary>
        /// Gets or sets the string pool.
        /// </summary>
        public StringPool StringPool
        {
            get { return _stringPool; }
            set { _stringPool = value; }
        }

        /// <summary>
        /// Gets or sets the question set.
        /// </summary>
        public HtsQuestionSet QuestionSet { get; set; }

        /// <summary>
        /// Gets or sets the candaidte sets.
        /// </summary>
        public List<CandidateSetData> CadidateSets { get; set; }

        /// <summary>
        /// Gets or sets the Tree Index Name Dict.
        /// </summary>
        public Dictionary<int, string> TreeIndexNameDic { get; set; }

        /// <summary>
        /// Gets or sets the pst header.
        /// </summary>
        public PreselectionFileHeader PSTHeader { get; set; }

        /// <summary>
        /// Gets or sets the decision Forest.
        /// </summary>
        public DecisionForest DecisionForest { get; set; }

        /// <summary>
        /// Gets or sets the custom features.
        /// </summary>
        public HashSet<string> CustomFeatures { get; set; }

        /// <summary>
        /// Gets or sets the tree indexes.
        /// </summary>
        internal List<TreeIndex> TreeIndexes { get; set; }

        /// <summary>
        /// Returns the tree index by tree name.
        /// </summary>
        /// <param name="treeName">Tree name given.</param>
        /// <returns>The tree index.</returns>
        public int GetTreeIndexByName(string treeName)
        {
            foreach (var val in TreeIndexNameDic)
            {
                if (val.Value == treeName)
                {
                    return val.Key;
                }
            }

            return -1;
        }

        /// <summary>
        /// Dispose the work items.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_stringPool != null)
                {
                    _stringPool.Dispose();
                    _disposed = true;
                    _stringPool = null;
                }
            }
        }
    }

    /// <summary>
    /// Candidate set data.
    /// </summary>
    public class CandidateSetData : INodeData
    {
        /// <summary>
        /// Gets or sets the name of the candidate set data.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the position for the candidate set data.
        /// </summary>
        public uint Position { get; set; }

        /// <summary>
        /// Gets or sets the candidate group id.
        /// </summary>
        public uint CandidateGroupId { get; set; }

        /// <summary>
        /// Gets or sets the candidates indexes.
        /// </summary>
        public ICollection<int> Candidates { get; set; }

        /// <summary>
        /// Write the cadidate set data.
        /// </summary>
        /// <param name="writer">The Data writer to writer the candidate data.</param>
        /// <returns>The write position.</returns>
        public int Write(DataWriter writer)
        {
            int size = 0;
            size += (int)writer.Write(CandidateGroupId);

            var candidateIds = Candidates;

            Debug.Assert(
                !candidateIds.Any(x => x < 0),
                "Unsupported negative candiate Id.");

            bool[] values = new bool[candidateIds.Max() + 1];

            foreach (var id in candidateIds)
            {
                values[id] = true;
            }

            BitArray bitArray = new BitArray(values);
            size += SetUtil.Write(SetType.IndexSet, bitArray, writer);
            return size;
        }
    }

    /// <summary>
    /// Class to write the PST data directly.
    /// </summary>
    public class PSTDataWriter
    {
        /// <summary>
        /// Write the pst data.
        /// </summary>
        /// <param name="pstFile">The pst file name to be stored.</param>
        /// <param name="data">The pst data to be write.</param>
        /// <param name="ttsPhoneSet">The tts Phone set.</param>
        /// <param name="ttsPosSet">The tts pst set.</param>
        public void WritePSTData(string pstFile, PSTData data, TtsPhoneSet ttsPhoneSet, TtsPosSet ttsPosSet)
        {
            foreach (Question question in data.DecisionForest.QuestionList)
            {
                question.Language = ttsPhoneSet.Language;
                question.ValueSetToCodeValueSet(ttsPosSet, ttsPhoneSet, data.CustomFeatures);
            }

            FileStream file = new FileStream(pstFile, FileMode.Create);
            try
            {
                using (DataWriter writer = new DataWriter(file))
                {
                    file = null;
                    uint position = 0;

                    // Write header section place holder
                    PreselectionFileHeader header = new PreselectionFileHeader();
                    position += (uint)header.Write(writer);

                    HtsFontSerializer serializer = new HtsFontSerializer();

                    using (StringPool stringPool = new StringPool())
                    {
                        Dictionary<string, uint> questionIndexes = new Dictionary<string, uint>();

                        header.QuestionOffset = position;
                        header.QuestionSize = serializer.Write(
                            data.QuestionSet, writer, stringPool, questionIndexes, data.CustomFeatures);
                        position += header.QuestionSize;

                        // Write leaf referenced data to buffer
                        List<CandidateSetData> dataNodes = data.CadidateSets;
                        int val = data.CadidateSets.Sum(c => c.Candidates.Count);
                        using (MemoryStream candidateSetBuffer = new MemoryStream())
                        {
                            Dictionary<string, int> namedSetOffset = new Dictionary<string, int>();

                            int candidateSetSize = HtsFontSerializer.Write(
                                dataNodes, new DataWriter(candidateSetBuffer), namedSetOffset);

                            // Write decision forest
                            Dictionary<string, uint[]> namedOffsets =
                                namedSetOffset.ToDictionary(p => p.Key, p => new[] { (uint)p.Value });

                            header.DecisionTreeSectionOffset = position;

                            header.DecisionTreeSectionSize = (uint)Write(data.DecisionForest, data.TreeIndexes,
                                questionIndexes, data.QuestionSet, namedOffsets, new DecisionForestSerializer(), writer);
                            position += header.DecisionTreeSectionSize;

                            // Write string pool
                            header.StringPoolOffset = position;
                            header.StringPoolSize = HtsFontSerializer.Write(stringPool, writer);
                            position += header.StringPoolSize;

                            // Write leaf referenced data
                            header.CandidateSetSectionOffset = position;
                            header.CandidateSetSectionSize = writer.Write(candidateSetBuffer.ToArray());
                            position += header.CandidateSetSectionSize;
                        }

                        // Write header section place holder
                        using (PositionRecover recover = new PositionRecover(writer, 0))
                        {
                            header.Write(writer);
                        }
                    }
                }
            }
            finally
            {
                if (null != file)
                {
                    file.Dispose();
                }
            }
        }

        /// <summary>
        /// Write the decision forest and tree index.
        /// </summary>
        /// <param name="forest">The decision forest.</param>
        /// <param name="treeIndexes">Tree indexes.</param>
        /// <param name="questionIndexes">Question indexes.</param>
        /// <param name="questionSet">The Question set.</param>
        /// <param name="namedOffsets">The named Offsets.</param>
        /// <param name="forestSerializer">The forest serializer.</param>
        /// <param name="writer">The writer to write.</param>
        /// <returns>The postion after write.</returns>
        internal int Write(DecisionForest forest, List<TreeIndex> treeIndexes,
            Dictionary<string, uint> questionIndexes, HtsQuestionSet questionSet,
            IDictionary<string, uint[]> namedOffsets, DecisionForestSerializer forestSerializer, DataWriter writer)
        {
            Helper.ThrowIfNull(forest);
            Helper.ThrowIfNull(treeIndexes);
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(questionIndexes);
            Helper.ThrowIfNull(questionSet);

            int decisionTreeSectionStart = (int)writer.BaseStream.Position;
            int position = decisionTreeSectionStart;

            // Write tree index (place holder)
            position += (int)WriteTreeIndexes(writer, treeIndexes.ToArray());

            // Write trees
            for (int treeIndex = 0; treeIndex < forest.TreeList.Count; treeIndex++)
            {
                DecisionTree tree = forest.TreeList[treeIndex];
                TreeIndex index = treeIndexes[treeIndex];

                index.Offset = position - decisionTreeSectionStart;
                index.Size = (int)forestSerializer.Write(tree, writer, questionIndexes, namedOffsets);
                position += index.Size;
            }

            // Write tree index
            using (PositionRecover recover =
                new PositionRecover(writer, decisionTreeSectionStart, SeekOrigin.Begin))
            {
                WriteTreeIndexes(writer, treeIndexes.ToArray());
            }

            Debug.Assert(position % sizeof(uint) == 0, "Data should be 4-byte aligned.");

            return position - decisionTreeSectionStart;
        }

        /// <summary>
        /// Write tree indexes.
        /// </summary>
        /// <param name="writer">The writer object.</param>
        /// <param name="treeIndexes">The tree index array.</param>
        /// <returns>Bytes written.</returns>
        private uint WriteTreeIndexes(DataWriter writer, TreeIndex[] treeIndexes)
        {
            uint size = 0;

            // Write tree count
            size += writer.Write((uint)treeIndexes.Length);

            foreach (TreeIndex index in treeIndexes)
            {
                size += writer.Write(index.Id);
                size += writer.Write(index.Offset);
                size += writer.Write(index.Size);
            }

            return size;
        }
    }
}
