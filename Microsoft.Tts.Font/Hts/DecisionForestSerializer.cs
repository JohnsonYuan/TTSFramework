//----------------------------------------------------------------------------
// <copyright file="DecisionForestSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements serializer for decision forest
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Font.Hts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider.Compress;

    /// <summary>
    /// Decision forest serializer.
    /// </summary>
    public class DecisionForestSerializer
    {
        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether EnableCompress.
        /// </summary>
        public bool EnableCompress
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Encoder.
        /// </summary>
        public LwHuffmEncoder Encoder
        {
            get;
            set;
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Read one Decision Forest from binary stream reader.
        /// </summary>
        /// <param name="forest">Decision forest to read.</param>
        /// <param name="reader">Binary stream reader.</param>
        /// <param name="header">Model header.</param>
        /// <param name="questionSet">Global question set.</param>
        /// <returns>Result forest, the same instance as the passed in decision forest.</returns>
        public static DecisionForest Read(DecisionForest forest,
            BinaryReader reader, HtsModelHeader header, HtsQuestionSet questionSet)
        {
            Helper.ThrowIfNull(forest);
            Helper.ThrowIfNull(reader);
            Helper.ThrowIfNull(header);
            Helper.ThrowIfNull(questionSet);

            uint basePosition = (uint)reader.BaseStream.Position;
            ////if (Encoder != null)
            ////{
            //// ramp up the encoder
            ////    Encoder.WarmUpData(reader.ReadBytes((int)header.TreeSize), 2);
            ////    reader.BaseStream.Position = basePosition;
            ////}

            forest.TreeList.Clear();

            NamedLocationDictionary treeLocatons = ReadTreeLocations(forest, reader);

            Dictionary<uint, string> questionIndex = new Dictionary<uint, string>();
            uint index = 0;
            foreach (Question question in questionSet.Items)
            {
                questionIndex.Add(index++, question.Name);
            }

            uint treeCount = reader.ReadUInt32();
            Debug.Assert(treeCount == treeLocatons.Sum(ls => ls.Value.Length),
                "The tree count should equal with number of locations of trees.");
            for (uint i = 0; i < treeCount; i++)
            {
                uint treePosition = (uint)reader.BaseStream.Position - basePosition;
                DecisionTree tree = new DecisionTree();
                tree.Name = BuildTreeName(treeLocatons, treePosition, forest);

                Read(tree, reader, header.StreamCount, questionIndex);
                forest.TreeList.Add(tree);
            }

            forest.BuildPhones(forest.PhoneSet);

            return forest;
        }

        /// <summary>
        /// Read decision tree from binary reader.
        /// </summary>
        /// <param name="tree">Decision tree to load.</param>
        /// <param name="reader">Binary reader.</param>
        /// <param name="streamCount">Stream count.</param>
        /// <param name="questionNames">Question names.</param>
        /// <returns>Retrieved decision tree.</returns>
        public static DecisionTree Read(DecisionTree tree, BinaryReader reader,
            uint streamCount, Dictionary<uint, string> questionNames)
        {
            Helper.ThrowIfNull(tree);
            Helper.ThrowIfNull(reader);
            Helper.ThrowIfNull(questionNames);
            uint basePosition = (uint)reader.BaseStream.Position;
            uint nodeCount = reader.ReadUInt32();
            tree.NodeList.Clear();
            for (uint i = 0; i < nodeCount; i++)
            {
                DecisionTreeNode node = ReadNode(reader, streamCount, questionNames, basePosition);

                tree.NodeList.Add(node);
            }

            BuildTreeHierarchy(tree);

            return tree;
        }

        /// <summary>
        /// Writer decision trees.
        /// </summary>
        /// <param name="forest">Decision tree forest.</param>
        /// <param name="writer">Font file writer.</param>
        /// <param name="questionIndexes">Index of global questions.</param>
        /// <param name="questionSet">Global question set.</param>
        /// <param name="namedOffsets">Leaf referenced data's offsets by name.</param>
        /// <returns>Size of bytes written out.</returns>
        public uint Write(DecisionForest forest, DataWriter writer,
            Dictionary<string, uint> questionIndexes, HtsQuestionSet questionSet, IDictionary<string, uint[]> namedOffsets)
        {
            Helper.ThrowIfNull(forest);
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(questionIndexes);
            Helper.ThrowIfNull(questionSet);

            NamedLocationDictionary treeLocations = BuildTreeLocations(forest.Phones.Keys, forest.StateCount);
            uint size = Write(treeLocations, writer, forest);

            size += writer.Write((uint)(forest.Phones.Count * forest.StateCount));

            foreach (DecisionTree tree in forest.TreeList)
            {
                uint treeSize = Write(tree, writer, questionIndexes, namedOffsets);
                treeLocations[tree.Phone][tree.EmittingStateIndex()].Offset = size;
                treeLocations[tree.Phone][tree.EmittingStateIndex()].Length = treeSize;
                size += treeSize;
            }

            Validate(treeLocations);

            using (PositionRecover recover = new PositionRecover(writer, -size, SeekOrigin.Current))
            {
                Write(treeLocations, writer, forest);
            }

            Debug.Assert(size % sizeof(uint) == 0, "Data should be 4-byte aligned.");

            return size;
        }

        /// <summary>
        /// Write out decision tree.
        /// </summary>
        /// <param name="tree">Decision tree to write out.</param>
        /// <param name="writer">Binary writer.</param>
        /// <param name="questionIndexes">Question index dictionary by question name.</param>
        /// <param name="namedOffsets">Leaf referenced data's offsets by name.</param>
        /// <returns>Size of bytes written.</returns>
        public uint Write(DecisionTree tree, DataWriter writer,
            Dictionary<string, uint> questionIndexes, IDictionary<string, uint[]> namedOffsets)
        {
            Helper.ThrowIfNull(tree);
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(questionIndexes);
            uint size = SinglePassWrite(tree, writer, questionIndexes, namedOffsets);

            // Re-write to update position information in parent nodes
            using (PositionRecover recover = new PositionRecover(writer, -size, SeekOrigin.Current))
            {
                SinglePassWrite(tree, writer, questionIndexes, namedOffsets);
            }

#if SERIALIZATION_CHECKING
            ConsistencyChecker.Check(tree.NodeList,
                Read(new DecisionTree(), writer.BaseStream.Excerpt(size),
                    (uint)tree.StreamCount(), Invert(questionIndexes)).NodeList);
#endif

            Debug.Assert(size % sizeof(uint) == 0, "Data should be 4-byte aligned.");

            return size;
        }

        #endregion

        #region Protected operations

        /// <summary>
        /// Reads one decision tree node from binary stream reader.
        /// </summary>
        /// <param name="reader">Binary data reader.</param>
        /// <param name="streamCount">The count of the stream number.</param>
        /// <param name="questionNames">Question names.</param>
        /// <param name="basePosition">Base position for the tree nodes.</param>
        /// <returns>Decision node.</returns>
        protected static DecisionTreeNode ReadNode(BinaryReader reader, uint streamCount,
            Dictionary<uint, string> questionNames, uint basePosition)
        {
            Helper.ThrowIfNull(reader);
            Helper.ThrowIfNull(questionNames);
            DecisionTreeNode node = new DecisionTreeNode();
            node.Position = (int)(reader.BaseStream.Position - basePosition);
            node.NodeType = (DecisionTreeNodeType)reader.ReadInt16();
            if (node.NodeType == DecisionTreeNodeType.NonLeaf)
            {
                node.QuestionIndex = (int)reader.ReadInt16();
                node.QuestionName = questionNames[(uint)node.QuestionIndex];

                node.LeftChild = new DecisionTreeNode();
                node.LeftChild.Position = (int)reader.ReadUInt32();
                node.RightChild = new DecisionTreeNode();
                node.RightChild.Position = (int)reader.ReadUInt32();
            }
            else
            {
                Debug.Assert(node.NodeType == DecisionTreeNodeType.Leaf,
                    "Beside non-leaf node, only lead node is supported.");
                reader.ReadInt16(); // Remove Padding
                node.RefDataOffsets = new int[streamCount];
                for (uint j = 0; j < streamCount; j++)
                {
                    node.RefDataOffsets[j] = (int)reader.ReadUInt32();
                }
            }

            return node;
        }

        /// <summary>
        /// Read the locations of decision tree sets from binary reader.
        /// </summary>
        /// <param name="forest">Decision forest.</param>
        /// <param name="reader">Binary reader.</param>
        /// <returns>Offset indexed locations.</returns>
        protected static NamedLocationDictionary ReadTreeLocations(DecisionForest forest, BinaryReader reader)
        {
            Helper.ThrowIfNull(forest);
            Helper.ThrowIfNull(reader);

            uint phoneCount = reader.ReadUInt32();
            uint stateCount = reader.ReadUInt32();

            NamedLocationDictionary namedLocations = new NamedLocationDictionary();
            for (uint i = 0; i < phoneCount; i++)
            {
                uint id = reader.ReadUInt32();
                Location[] locations = new Location[stateCount];
                for (uint j = 0; j < stateCount; j++)
                {
                    locations[j].Offset = reader.ReadUInt32();
                    locations[j].Length = reader.ReadUInt32();
                }

                namedLocations.Add(forest.PhoneSet.ToLabel((int)id), locations);
            }

            return namedLocations;
        }

        /// <summary>
        /// Writes one decision tree node into binary data stream.
        /// </summary>
        /// <param name="node">Decision node to write out.</param>
        /// <param name="tree">Decision tree, containing the decision node to write out.</param>
        /// <param name="writer">Binary data writer.</param>
        /// <param name="questionIndexes">Question indexes.</param>
        /// <param name="namedOffsets">Leaf referenced data's offsets by name.</param>
        /// <returns>Size of bytes written out.</returns>
        protected virtual uint WriteNode(DecisionTreeNode node, DecisionTree tree, DataWriter writer,
            Dictionary<string, uint> questionIndexes, IDictionary<string, uint[]> namedOffsets)
        {
            Helper.ThrowIfNull(node);
            Helper.ThrowIfNull(tree);
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(questionIndexes);

            uint size = writer.Write((ushort)node.NodeType);
            if (node.NodeType == DecisionTreeNodeType.NonLeaf)
            {
                size += writer.Write((ushort)questionIndexes[node.QuestionName]);
                size += writer.Write((uint)node.LeftChild.Position);
                size += writer.Write((uint)node.RightChild.Position);
            }
            else
            {
                size += writer.Write((ushort)DataWriter.Padding);

                foreach (uint offset in namedOffsets[node.Name])
                {
                    size += writer.Write(offset);
                }
            }

            return size;
        }

        /// <summary>
        /// Write out tree locations.
        /// </summary>
        /// <param name="treeLocations">Tree locations.</param>
        /// <param name="writer">Binary writer.</param>
        /// <param name="forest">Decision forest.</param>
        /// <returns>Size of bytes written out.</returns>
        protected virtual uint Write(NamedLocationDictionary treeLocations,
            DataWriter writer, DecisionForest forest)
        {
            Helper.ThrowIfNull(treeLocations);
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(forest);

            uint size = 0;

            size += writer.Write((uint)forest.Phones.Count);
            size += writer.Write((uint)forest.StateCount);

            foreach (string phone in treeLocations.Keys)
            {
                size += writer.Write((uint)forest.Phones[phone].Id);
                for (int i = 0; i < forest.StateCount; i++)
                {
                    size += writer.Write(treeLocations[phone][i].Offset);
                    size += writer.Write(treeLocations[phone][i].Length);
                }
            }

            Debug.Assert(size % sizeof(uint) == 0, "Data should be 4-byte aligned.");

            return size;
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Initializes the phone-named locations.
        /// </summary>
        /// <param name="phones">Phone name collection.</param>
        /// <param name="stateCount">The count of state.</param>
        /// <returns>Created named locations.</returns>
        private static NamedLocationDictionary BuildTreeLocations(IEnumerable<string> phones, int stateCount)
        {
            Helper.ThrowIfNull(phones);
            NamedLocationDictionary sets = new NamedLocationDictionary();
            foreach (string name in phones)
            {
                sets.Add(name, new Location[stateCount]);
            }

            return sets;
        }

        /// <summary>
        /// Valid named location to have non-zero offset and non-zero length.
        /// </summary>
        /// <param name="treeLocations">Phone label indexes locations of decision set.</param>
        private static void Validate(SortedDictionary<string, Location[]> treeLocations)
        {
            Helper.ThrowIfNull(treeLocations);
            foreach (string phone in treeLocations.Keys)
            {
                for (int i = 0; i < treeLocations[phone].Length; i++)
                {
                    Location pair = treeLocations[phone][i];
                    if (pair.Offset == 0 || pair.Length == 0)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                             "The decision nodes for phone [{0}], state [{1}] can not be found.", phone, i + 2);
                        throw new InvalidDataException(message);
                    }
                }
            }
        }

        /// <summary>
        /// Builds tree hierarchy throw node position.
        /// </summary>
        /// <param name="tree">Decision tree.</param>
        private static void BuildTreeHierarchy(DecisionTree tree)
        {
            Helper.ThrowIfNull(tree);
            var positionedNodes = tree.NodeList.ToDictionary(n => n.Position);
            foreach (DecisionTreeNode node in tree.NodeList.Where(n => n.NodeType == DecisionTreeNodeType.NonLeaf))
            {
                node.LeftChild = positionedNodes[node.LeftChild.Position];
                node.RightChild = positionedNodes[node.RightChild.Position];
                node.LeftChild.Parent = node;
                node.RightChild.Parent = node;
            }
        }

        /// <summary>
        /// Build the name of decision tree in HTK format.
        /// </summary>
        /// <param name="treeLocatons">Overall decision tree locations.</param>
        /// <param name="location">Location of current decision tree.</param>
        /// <param name="forest">Decision tree forest.</param>
        /// <returns>Tree name built.</returns>
        private static string BuildTreeName(NamedLocationDictionary treeLocatons,
            uint location, DecisionForest forest)
        {
            Helper.ThrowIfNull(treeLocatons);
            Helper.ThrowIfNull(forest);
            int stateIndex = FindStateIndex(treeLocatons, location);
            string phoneLabel = FindPhoneLabel(treeLocatons, location);
            return BuildTreeName(phoneLabel, stateIndex, forest.StreamIndexes);
        }

        /// <summary>
        /// Build the name of decision tree in HTK format.
        /// </summary>
        /// <param name="phoneLabel">Phone label.</param>
        /// <param name="stateIndex">State index of current tree.</param>
        /// <param name="streamIndexes">Stream indexes.</param>
        /// <returns>Tree name created.</returns>
        private static string BuildTreeName(string phoneLabel, int stateIndex, int[] streamIndexes)
        {
            Helper.ThrowIfNull(phoneLabel);
            Helper.ThrowIfNull(streamIndexes);
            StringBuilder streamLabel = new StringBuilder();
            if (streamIndexes.Sum() > 0)
            {
                streamLabel.Append(".stream[" + streamIndexes.Concatenate(",") + "]");
            }

            string phoneContext;
            if (Phoneme.IsAnyPhone(phoneLabel))
            {
                phoneContext = Helper.NeutralFormat(@"{{{0}}}", phoneLabel);
            }
            else
            {
                phoneContext = Helper.NeutralFormat(@"{{*-{0}+*}}", phoneLabel);
            }

            return Helper.NeutralFormat(@"{0}[{1}]{2}",
                phoneContext, stateIndex + DecisionForest.StateIndexBeginOffset, streamLabel);
        }

        /// <summary>
        /// Given the location, finds state index of the decision tree in the locations.
        /// </summary>
        /// <param name="treeLocatons">Overall locations of decision trees.</param>
        /// <param name="location">Location of target decision tree to find.</param>
        /// <returns>State index.</returns>
        private static int FindStateIndex(NamedLocationDictionary treeLocatons, uint location)
        {
            Helper.ThrowIfNull(treeLocatons);
            foreach (string id in treeLocatons.Keys)
            {
                for (int i = 0; i < treeLocatons[id].Length; i++)
                {
                    if (treeLocatons[id][i].Offset == location)
                    {
                        return i;
                    }
                }
            }

            throw new InvalidDataException("Wanted offset is not found.");
        }

        /// <summary>
        /// Given the location, finds phone id of the decision tree in the locations.
        /// </summary>
        /// <param name="treeLocatons">Overall locations of decision trees.</param>
        /// <param name="location">Location of target decision tree to find.</param>
        /// <returns>Phone id found.</returns>
        private static string FindPhoneLabel(NamedLocationDictionary treeLocatons, uint location)
        {
            Helper.ThrowIfNull(treeLocatons);
            foreach (string id in treeLocatons.Keys)
            {
                for (int i = 0; i < treeLocatons[id].Length; i++)
                {
                    if (treeLocatons[id][i].Offset == location)
                    {
                        return id;
                    }
                }
            }

            throw new InvalidDataException("Wanted offset is not found.");
        }

        /// <summary>
        /// Invert dictionary, i.e. turning value as key, key as value.
        /// </summary>
        /// <typeparam name="TKey">Type of key instance.</typeparam>
        /// <typeparam name="TValue">Type of value instance.</typeparam>
        /// <param name="dictionary">Dictionary to invert.</param>
        /// <returns>Inverted dictionary.</returns>
        private static Dictionary<TValue, TKey> Invert<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            Helper.ThrowIfNull(dictionary);
            Dictionary<TValue, TKey> result = new Dictionary<TValue, TKey>();
            foreach (TKey item in dictionary.Keys)
            {
                result.Add(dictionary[item], item);
            }

            return result;
        }

        /// <summary>
        /// Write out decision tree.
        /// </summary>
        /// <param name="tree">Decision tree to write out.</param>
        /// <param name="writer">Binary writer.</param>
        /// <param name="questionIndexes">Question index dictionary by question name.</param>
        /// <param name="namedOffsets">Leaf referenced data's offsets by name.</param>
        /// <returns>Size of bytes written.</returns>
        private uint SinglePassWrite(DecisionTree tree, DataWriter writer,
            Dictionary<string, uint> questionIndexes, IDictionary<string, uint[]> namedOffsets)
        {
            Helper.ThrowIfNull(tree);
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(questionIndexes);
            Debug.Assert(tree.NodeList.Count > 0, "Empty tree is not allowed.");
            uint size = writer.Write((uint)tree.NodeList.Count);

            foreach (DecisionTreeNode node in tree.NodeList)
            {
                node.Position = (int)size;
                size += WriteNode(node, tree, writer, questionIndexes, namedOffsets);
            }

            Debug.Assert(size % sizeof(uint) == 0, "Data should be 4-byte aligned.");

            return size;
        }

        #endregion
    }

    /// <summary>
    /// Name-indexed location dictionary.
    /// </summary>
    public class NamedLocationDictionary : SortedDictionary<string, Location[]>
    {
    }
}