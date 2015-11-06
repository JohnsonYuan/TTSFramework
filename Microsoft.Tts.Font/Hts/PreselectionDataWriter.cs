//----------------------------------------------------------------------------
// <copyright file="PreSelectionDataWriter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines serialization classes for pre-selection data used in RUS pre-selection.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Font.Hts
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// Wrapps candidate group for serialization.
    /// </summary>
    public class PreselectionNodeData : INodeData
    {
        private CandidateGroup _candidateGroup;
        private int _position = 0;

        /// <summary>
        /// Initializes a new instance of the PreselectionNodeData class.
        /// </summary>
        /// <param name="candidateGroup">The candidate group to be wrapped.</param>
        public PreselectionNodeData(CandidateGroup candidateGroup)
        {
            _candidateGroup = candidateGroup;
        }

        /// <summary>
        /// Gets the candidate group position.
        /// </summary>
        public int Position
        {
            get { return _position; }
        }

        /// <summary>
        /// Gets the candidate group name.
        /// </summary>
        public string Name
        {
            get { return _candidateGroup.Name; }
        }

        /// <summary>
        /// Write preselection node data.
        /// </summary>
        /// <param name="writer">The writer object.</param>
        /// <returns>Bytes written.</returns>
        public int Write(DataWriter writer)
        {
            int size = 0;
            size += (int)writer.Write(_candidateGroup.Id);

            var candidateIds = _candidateGroup.Candidates.Select(c => c.Id);

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
    /// Preselection font file header.
    /// </summary>
    public class PreselectionFileHeader
    {
        /// <summary>
        /// Initializes a new instance of the PreselectionFileHeader class.
        /// </summary>
        public PreselectionFileHeader()
        {
            FontHeader = new VoiceFontHeader
            {
                FileTag = UnitCandidateSet.CostTableTag,
                FormatTag = VoiceFontTag.FmtIdPreselection,
                Version = 0,
                Build = 0,
            };
        }

        #region Fields

        /// <summary>
        /// Gets the common font header.
        /// </summary>
        public VoiceFontHeader FontHeader { get; private set; }

        /// <summary>
        /// Gets or sets the question offset.
        /// </summary>
        public uint QuestionOffset { get; set; }

        /// <summary>
        /// Gets or sets the  question size.
        /// </summary>
        public uint QuestionSize { get; set; }

        /// <summary>
        /// Gets or sets the string pool offset.
        /// </summary>
        public uint StringPoolOffset { get; set; }

        /// <summary>
        /// Gets or sets the string pool size.
        /// </summary>
        public uint StringPoolSize { get; set; }

        /// <summary>
        /// Gets or sets the decision tree section offset.
        /// </summary>
        public uint DecisionTreeSectionOffset { get; set; }

        /// <summary>
        /// Gets or sets the decision tree section size.
        /// </summary>
        public uint DecisionTreeSectionSize { get; set; }

        /// <summary>
        /// Gets or sets the candidate set section offset.
        /// </summary>
        public uint CandidateSetSectionOffset { get; set; }

        /// <summary>
        /// Gets or sets the candidate set section size.
        /// </summary>
        public uint CandidateSetSectionSize { get; set; }

        #endregion

        /// <summary>
        /// Write preselection file header.
        /// </summary>
        /// <param name="writer">The writer object.</param>
        /// <returns>Bytes writen.</returns>
        public int Write(BinaryWriter writer)
        {
            int preselectionHeaderSize = sizeof(uint) * 8;

            FontHeader.DataSize =
                QuestionSize +
                StringPoolSize +
                DecisionTreeSectionSize +
                CandidateSetSectionSize +
                (uint)preselectionHeaderSize;

            int fontFileHeaderSize = FontHeader.Save(writer);

            writer.Write(QuestionOffset);
            writer.Write(QuestionSize);
            writer.Write(DecisionTreeSectionOffset);
            writer.Write(DecisionTreeSectionSize);
            writer.Write(StringPoolOffset);
            writer.Write(StringPoolSize);
            writer.Write(CandidateSetSectionOffset);
            writer.Write(CandidateSetSectionSize);

            return fontFileHeaderSize + preselectionHeaderSize;
        }

        /// <summary>
        /// Read preselection file header.
        /// </summary>
        /// <param name="reader">The reader object.</param>
        public void Read(BinaryReader reader)
        {
            FontHeader.Load(reader);

            QuestionOffset = reader.ReadUInt32();
            QuestionSize = reader.ReadUInt32();
            DecisionTreeSectionOffset = reader.ReadUInt32();
            DecisionTreeSectionSize = reader.ReadUInt32();
            StringPoolOffset = reader.ReadUInt32();
            StringPoolSize = reader.ReadUInt32();
            CandidateSetSectionOffset = reader.ReadUInt32();
            CandidateSetSectionSize = reader.ReadUInt32();
        }
    }

    /// <summary>
    /// Writer for pre-selection forest.
    /// </summary>
    public class PreSelectionSerializer
    {
        private TtsPhoneSet _phoneSet = null;
        private TtsPosSet _posSet = null;

        /// <summary>
        /// Initializes a new instance of the PreSelectionSerializer class.
        /// </summary>
        /// <param name="phoneSet">Phone set object.</param>
        /// <param name="posSet">POS set object.</param>
        public PreSelectionSerializer(TtsPhoneSet phoneSet, TtsPosSet posSet)
        {
            _phoneSet = phoneSet;
            _posSet = posSet;
        }

        /// <summary>
        /// Writer decision trees.
        /// </summary>
        /// <param name="forest">Decision tree forest.</param>
        /// <param name="unitCandidateNameIds">Given candidate idx.</param>
        /// <param name="questionIndexes">Index of global questions.</param>
        /// <param name="questionSet">Global question set.</param>
        /// <param name="namedOffsets">Node name and referenced data offsets map.</param>
        /// <param name="forestSerializer">Forest serializer object.</param>
        /// <param name="writer">Writer object.</param>
        /// <returns>Size of bytes written out.</returns>
        public int Write(DecisionForest forest, IDictionary<string, int> unitCandidateNameIds,
            Dictionary<string, uint> questionIndexes, HtsQuestionSet questionSet,
            IDictionary<string, uint[]> namedOffsets, DecisionForestSerializer forestSerializer, DataWriter writer)
        {
            Helper.ThrowIfNull(forest);
            Helper.ThrowIfNull(unitCandidateNameIds);
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(questionIndexes);
            Helper.ThrowIfNull(questionSet);

            int decisionTreeSectionStart = (int)writer.BaseStream.Position;
            int position = decisionTreeSectionStart;

            var treeIndexes = forest.TreeList.Select(t =>
                new TreeIndex
                {
                    Id = unitCandidateNameIds[t.Name],
                    Offset = 0,
                    Size = 0
                }).ToArray();

            // Write tree index (place holder)
            position += (int)WriteTreeIndexes(writer, treeIndexes);

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
                WriteTreeIndexes(writer, treeIndexes);
            }

            Debug.Assert(position % sizeof(uint) == 0, "Data should be 4-byte aligned.");

            return position - decisionTreeSectionStart;
        }

        /// <summary>
        /// Save pre-selection forest.
        /// </summary>
        /// <param name="decisionForest">The forest with each tree corresponding to a unit.</param>
        /// <param name="candidateGroups">The candidate group collection.</param>
        /// <param name="unitCandidateNameIds">Given candidate idx.</param>
        /// <param name="customFeatures">Cusotmized linguistic feature list.</param>
        /// <param name="outputPath">The output path.</param>
        public void Write(DecisionForest decisionForest,
            ICollection<CandidateGroup> candidateGroups, 
            IDictionary<string, int> unitCandidateNameIds,
            HashSet<string> customFeatures,
            string outputPath)
        {
            foreach (Question question in decisionForest.QuestionList)
            {
                question.Language = _phoneSet.Language;
                question.ValueSetToCodeValueSet(_posSet, _phoneSet, customFeatures);
            }

            FileStream file = new FileStream(outputPath, FileMode.Create);
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

                    // Write feature, question and prepare string pool
                    HtsQuestionSet questionSet = new HtsQuestionSet
                    {
                        Items = decisionForest.QuestionList,
                        Header = new HtsQuestionSetHeader { HasQuestionName = false },
                        CustomFeatures = customFeatures,
                    };

                    using (StringPool stringPool = new StringPool())
                    {
                        Dictionary<string, uint> questionIndexes = new Dictionary<string, uint>();

                        header.QuestionOffset = position;
                        header.QuestionSize = serializer.Write(
                            questionSet, writer, stringPool, questionIndexes, customFeatures);
                        position += header.QuestionSize;

                        // Write leaf referenced data to buffer
                        IEnumerable<INodeData> dataNodes = GetCandidateNodes(candidateGroups);
                        using (MemoryStream candidateSetBuffer = new MemoryStream())
                        {
                            Dictionary<string, int> namedSetOffset = new Dictionary<string, int>();

                            int candidateSetSize = HtsFontSerializer.Write(
                                dataNodes, new DataWriter(candidateSetBuffer), namedSetOffset);

                            // Write decision forest
                            Dictionary<string, uint[]> namedOffsets =
                                namedSetOffset.ToDictionary(p => p.Key, p => new[] { (uint)p.Value });

                            header.DecisionTreeSectionOffset = position;

                            header.DecisionTreeSectionSize = (uint)Write(decisionForest, unitCandidateNameIds,
                                questionIndexes, questionSet, namedOffsets, new DecisionForestSerializer(), writer);
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

        /// <summary>
        /// Wrapps candidate group as node data for serialization.
        /// </summary>
        /// <param name="candidateGroups">The candidate group collection.</param>
        /// <returns>The node data enumerator.</returns>
        private IEnumerable<INodeData> GetCandidateNodes(ICollection<CandidateGroup> candidateGroups)
        {
            foreach (var candidateGroup in candidateGroups)
            {
                yield return new PreselectionNodeData(candidateGroup);
            }
        }
    }

    /// <summary>
    /// Index of decision tree for serialization.
    /// </summary>
    internal class TreeIndex
    {
        /// <summary>
        /// Gets or sets Id of the tree.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets tree offset when serialized.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Gets or sets tree size when serialized.
        /// </summary>
        public int Size { get; set; }
    }
}