//----------------------------------------------------------------------------
// <copyright file="PSTDataLoader.cs" company="Microsoft">
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
    /// Load the pst data from pst file.
    /// </summary>
    public class PSTDataLoader
    {
        public PSTDataLoader(TtsEngine engine)
        {
            this.SpEngine = engine;
        }

        /// <summary>
        /// Gets or sets TTS engine used to load the pst file.
        /// </summary>
        public TtsEngine SpEngine { get; set; }

        /// <summary>
        /// Verify the pst data whether it is good pst data.
        /// </summary>
        /// <param name="data">The pst data.</param>
        /// <returns>Whether the pst data is valid.</returns>
        public bool VerifyPSTData(PSTData data)
        {
            bool isgoodData = true;
            List<int> intList = new List<int>();
            foreach (DecisionTree tree in data.DecisionForest.TreeList)
            {
                foreach (DecisionTreeNode node in tree.NodeList)
                {
                    if (node.NodeType == DecisionTreeNodeType.Leaf)
                    {
                        intList.Add(node.RefDataOffsets[0]);
                    }
                }
            }

            intList.Sort();
            List<int> resultList = new List<int>();
            data.CadidateSets.ForEach(r => resultList.Add((int)r.Position));
            resultList.Sort();
            for (int i = 0; i < intList.Count && isgoodData; i++)
            {
                if (intList[i] != resultList[i])
                {
                    isgoodData = false;
                }
            }

            return isgoodData;
        }

        /// <summary>
        /// Load the pst data.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <param name="data">The pst data.</param>
        /// <param name="phoneSet">The phone set.</param>
        /// <param name="posSet">The pos set.</param>
        public void LoadPSTData(BinaryReader reader, PSTData data, TtsPhoneSet phoneSet, TtsPosSet posSet)
        {
            PreselectionFileHeader header = ReadPSTHeader(reader);
            data.PSTHeader = header;
            HashSet<string> customFeatures = new HashSet<string>();
            data.QuestionSet = ReadQuestionSetAndStringPool(reader, header, data.StringPool, customFeatures);
            data.CustomFeatures = customFeatures;

            Dictionary<uint, string> questionIndex = new Dictionary<uint, string>();
            uint index = 0;
            data.QuestionSet.Items.ForEach(item => questionIndex.Add(index++, item.Name));

            data.TreeIndexes = new List<TreeIndex>();
            data.DecisionForest = ReadDecisionForest(reader, header, questionIndex, data.TreeIndexes, phoneSet, posSet);

            reader.BaseStream.Seek(header.StringPoolSize, SeekOrigin.Current);
            data.CadidateSets = ReadCandidateSets(reader, header);

            Dictionary<int, string> dict = BuildTreeIndexNameDict(SpEngine);
            data.TreeIndexNameDic = dict;
            SetDicisionTreeName(data.DecisionForest, data.TreeIndexes, dict);
            SetCandidateSetName(data.CadidateSets, data.DecisionForest);
        }

        /// <summary>
        /// Read pst header from pst file.
        /// </summary>
        /// <param name="reader">The binary reader reading the pst data.</param>
        /// <returns>The preselection file header.</returns>
        private PreselectionFileHeader ReadPSTHeader(BinaryReader reader)
        {
            PreselectionFileHeader header = new PreselectionFileHeader();
            header.Read(reader);
            return header;
        }

        /// <summary>
        /// Read the questions set and string pool.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <param name="header">The pst header.</param>
        /// <param name="stringPool">The string pool.</param>
        /// <param name="customFeatures">The custom features.</param>
        /// <returns>The hts question set.</returns>
        private HtsQuestionSet ReadQuestionSetAndStringPool(BinaryReader reader, PreselectionFileHeader header, StringPool stringPool, HashSet<string> customFeatures)
        {
            HtsFontSerializer serializer = new HtsFontSerializer();

            HtsQuestionSet questionSet = new HtsQuestionSet();
            using (PositionRecover recover = new PositionRecover(reader.BaseStream, header.StringPoolOffset))
            {
                byte[] buffer = new byte[header.StringPoolSize];
                Microsoft.Tts.ServiceProvider.HTSVoiceDataEncrypt.DecryptStringPool(
                    reader.ReadBytes((int)header.StringPoolSize), buffer);
                stringPool.PutBuffer(buffer);
            }

            serializer.Read(questionSet, reader, stringPool, customFeatures);
            return questionSet;
        }

        /// <summary>
        /// Read the decision forest.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <param name="header">The pst header.</param>
        /// <param name="questionIndex">The question index.</param>
        /// <param name="treeIndexes">The tree indexes.</param>
        /// <param name="phoneSet">The phone set.</param>
        /// <param name="posSet">The pos set.</param>
        /// <returns>The decision forest.</returns>
        private DecisionForest ReadDecisionForest(BinaryReader reader, PreselectionFileHeader header, Dictionary<uint, string> questionIndex, List<TreeIndex> treeIndexes, TtsPhoneSet phoneSet, TtsPosSet posSet)
        {
            DecisionForest forest = new DecisionForest("test");
            forest.PhoneSet = phoneSet;
            forest.PosSet = posSet;

            uint count = reader.ReadUInt32();
            for (int i = 0; i < count; i++)
            {
                TreeIndex treeIndex = new TreeIndex();
                treeIndex.Id = reader.ReadInt32();
                treeIndex.Offset = reader.ReadInt32();
                treeIndex.Size = reader.ReadInt32();
                treeIndexes.Add(treeIndex);
            }

            for (int i = 0; i < count; i++)
            {
                DecisionTree tree = new DecisionTree();
                tree = DecisionForestSerializer.Read(tree, reader, (uint)1, questionIndex);
                forest.TreeList.Add(tree);
            }

            return forest;
        }

        /// <summary>
        /// Read candidate set from the pst file.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <param name="header">The font header.</param>
        /// <returns>The list of the candidate set data.</returns>
        private List<CandidateSetData> ReadCandidateSets(BinaryReader reader, PreselectionFileHeader header)
        {
            uint count = reader.ReadUInt32();
            List<CandidateSetData> candidateSets = new List<CandidateSetData>((int)count);
            for (int i = 0; i < count; i++)
            {
                CandidateSetData set = ReadCandidateSet(reader, header);
                candidateSets.Add(set);
            }

            return candidateSets;
        }

        /// <summary>
        /// Set decision tree name based on phone name.
        /// </summary>
        /// <param name="forest">The decision forest.</param>
        /// <param name="treeIndexes">The tree indexes.</param>
        /// <param name="idNameMap">The id name map.</param>
        private void SetDicisionTreeName(DecisionForest forest, List<TreeIndex> treeIndexes, Dictionary<int, string> idNameMap)
        {
            for (int i = 0; i < forest.TreeList.Count; i++)
            {
                DecisionTree tree = forest.TreeList[i];
                int id = treeIndexes[i].Id;
                string name = idNameMap[id];
                tree.Name = name;
            }
        }

        /// <summary>
        /// Build tree index name dictionary inorder to set the candidate set name.
        /// </summary>
        /// <param name="engine">The service provider engine.</param>
        /// <returns>The dictionary contains the id name map.</returns>
        private Dictionary<int, string> BuildTreeIndexNameDict(TtsEngine engine)
        {
            Dictionary<int, string> dict = new Dictionary<int, string>();
            WuiManager manager = engine.RUSVoiceDataManager.GetWuiManagerByIndex(0);
            for (int i = 0; i < manager.GetUnitIndexNumber(); i++)
            {
                string hpuName = manager.GetUnitName(i);
                dict.Add(i, hpuName);
            }

            return dict;
        }

        /// <summary>
        /// Set the candidate set name.
        /// </summary>
        /// <param name="candidateSets">The candidate sets.</param>
        /// <param name="forest">The decision forest.</param>
        private void SetCandidateSetName(List<CandidateSetData> candidateSets, DecisionForest forest)
        {
            Dictionary<uint, string> dict = new Dictionary<uint, string>();
            foreach (DecisionTree tree in forest.TreeList)
            {
                int i = 0;
                foreach (DecisionTreeNode node in tree.NodeList)
                {
                    if (node.NodeType == DecisionTreeNodeType.Leaf)
                    {
                        i++;
                        string name = tree.Name + i.ToString();
                        dict.Add((uint)node.RefDataOffsets[0], name);
                        node.Name = name;
                    }
                }
            }

            foreach (CandidateSetData data in candidateSets)
            {
                data.Name = dict[data.Position];
            }
        }

        /// <summary>
        /// Read the detail content for a candidate set.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <param name="header">The pst header.</param>
        /// <returns>The candidate set data.</returns>
        private CandidateSetData ReadCandidateSet(BinaryReader reader, PreselectionFileHeader header)
        {
            CandidateSetData setData = new CandidateSetData();
            setData.Position = (uint)(reader.BaseStream.Position - header.CandidateSetSectionOffset);
            setData.CandidateGroupId = reader.ReadUInt32();
            setData.Candidates = SetUtil.Read(reader);
            return setData;
        }
    }
}
