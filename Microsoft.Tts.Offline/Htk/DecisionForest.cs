//----------------------------------------------------------------------------
// <copyright file="DecisionForest.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines a common library to manipulate decision tree file
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Decision tree extension for HTS.
    /// </summary>
    public static class DecisionTreeHtsExtension
    {
        #region forest extensions

        /// <summary>
        /// Gets the model type of this decision tree.
        /// </summary>
        /// <param name="forest">The forest object.</param>
        /// <returns>The model type.</returns>
        public static HmmModelType ModelType(this DecisionForest forest)
        {
            HmmModelType modelType = forest.TreeList.FirstOrDefault().ModelType();
            Debug.Assert(forest.TreeList.Where(t => t.ModelType() != modelType).Count() == 0,
                "All trees in current forest should share the same model type.");

            return modelType;
        }

        #endregion

        #region tree extensions

        /// <summary>
        /// Model type.
        /// </summary>
        /// <param name="tree">The tree object.</param>
        /// <returns>The model type.</returns>
        public static HmmModelType ModelType(this DecisionTree tree)
        {
            IEnumerable<DecisionTreeNode> leafNodes = tree.NodeList.Where(n => n.NodeType == DecisionTreeNodeType.Leaf);
            HmmModelType modelType = leafNodes.FirstOrDefault().ModelType();
            Debug.Assert(leafNodes.Where(n => n.ModelType() != modelType).Count() == 0,
                "All leaf node in tree should share the same model type.");
            return modelType;
        }

        /// <summary>
        /// State index in HTK format.
        /// </summary>
        /// <param name="tree">The tree object.</param>
        /// <returns>The state index.</returns>
        public static int StateIndex(this DecisionTree tree)
        {
            return DecisionTreeName.ParseStateIndex(tree.Name);
        }

        /// <summary>
        /// The index of state in emitting states.
        /// </summary>
        /// <param name="tree">The tree object.</param>
        /// <returns>The emitting state index.</returns>
        public static int EmittingStateIndex(this DecisionTree tree)
        {
            return tree.StateIndex() - DecisionForest.StateIndexBeginOffset;
        }

        /// <summary>
        /// Stream indexes of this tree, presenting in HTS models
        ///     for example:  {*-zh+*}[5].stream[2,3,4]
        ///     No stream information in duration:  {*-ax+*}[2]; assume as one stream with index as 1.
        /// </summary>
        /// <param name="tree">The tree object.</param>
        /// <returns>The stream indexes.</returns>
        public static int[] StreamIndexes(this DecisionTree tree)
        {
            return DecisionTreeName.ParseStreamIndexes(tree.Name);
        }

        /// <summary>
        /// Stream count.
        /// </summary>
        /// <param name="tree">The tree object.</param>
        /// <returns>The stream count.</returns>
        public static int StreamCount(this DecisionTree tree)
        {
            return tree.StreamIndexes().Length;
        }

        /// <summary>
        /// With given stream index, prune one stream from tree
        ///     Note: one stream must be left at least.
        /// </summary>
        /// <param name="tree">The tree object.</param>
        /// <param name="streamIndex">Stream index.</param>
        public static void PruneStream(this DecisionTree tree, int streamIndex)
        {
            if (!tree.StreamIndexes().Contains(streamIndex))
            {
                throw new ArgumentException(
                    Helper.NeutralFormat("Stream index {0} to prune is not in tree {1}.", streamIndex, tree.Name));
            }

            if (tree.StreamIndexes().Length == 1)
            {
                throw new NotSupportedException(Helper.NeutralFormat("There must have one stream in model at least."));
            }

            string oldString = tree.StreamIndexes().Select(i => i.ToString(CultureInfo.InvariantCulture)).Concatenate(",");
            string newString = tree.StreamIndexes().Where(i => i != streamIndex)
                .Select(i => i.ToString(CultureInfo.InvariantCulture)).Concatenate(",");

            tree.Name = tree.Name.Replace("stream[" + oldString + "]", "stream[" + newString + "]");
        }

        #endregion

        #region node extensions

        /// <summary>
        /// Model type in leaf node's node. Return Invalid if as non-leaf node.
        /// </summary>
        /// <param name="node">The node object.</param>
        /// <returns>The model type.</returns>
        public static HmmModelType ModelType(this DecisionTreeNode node)
        {
            Debug.Assert(node.NodeType == DecisionTreeNodeType.Leaf,
                "Only leaf node contains name with encoded model type.");
            return HmmStreamName.ParseModelType(node.Name);
        }

        #endregion
    }

    /// <summary>
    /// Extension to TTS phone set.
    /// </summary>
    public static class TtsPhoneSetExtension
    {
        /// <summary>
        /// Convert phone string label to Phone instance, while considering HTK silence phone.
        /// </summary>
        /// <param name="phoneSet">Phone set used to convert.</param>
        /// <param name="phoneLabel">Phone label string.</param>
        /// <returns>Phone instance found in the phone set, null if not.</returns>
        public static Phone ToPhone(this TtsPhoneSet phoneSet, string phoneLabel)
        {
            Phone ttsPhone = phoneSet.GetPhone(Phoneme.ToOffline(phoneLabel));
            if (ttsPhone == null && Phoneme.IsAnyPhone(phoneLabel))
            {
                ttsPhone = new Phone(Phoneme.AnyPhone, Phoneme.AnyPhoneId);
            }

            return ttsPhone;
        }

        /// <summary>
        /// Converts to Phone Id in phone set table to HTK phone label.
        /// </summary>
        /// <param name="phoneSet">Phone set used to convert phone id into phone string label.</param>
        /// <param name="id">Id of phone to convert to string.</param>
        /// <returns>Phone string label.</returns>
        public static string ToLabel(this TtsPhoneSet phoneSet, int id)
        {
            Helper.ThrowIfNull(phoneSet);
            string label = string.Empty;

            if (id == Phoneme.AnyPhoneId)
            {
                label = Phoneme.AnyPhone;
            }
            else
            {
                Phone ttsPhone = phoneSet.Phones.First(ph => ph.Id == id);
                if (ttsPhone != null)
                {
                    label = Phoneme.ToHtk(ttsPhone.Name);
                }
            }

            return label;
        }
    }

    /// <summary>
    /// Implementation of class DecisionForest.
    /// </summary>
    public class DecisionForest
    {
        #region Fields

        /// <summary>
        /// HTK state index begin offset.
        /// </summary>
        public const int StateIndexBeginOffset = 2;

        private string _name;
        private SortedDictionary<string, Question> _nameIndexedQuestions = new SortedDictionary<string, Question>();
        private List<DecisionTree> _treeList = new List<DecisionTree>();

        private Dictionary<string, Phone> _phones = new Dictionary<string, Phone>();

        private int[] _streamIndexes;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the DecisionForest class.
        /// </summary>
        /// <param name="name">Name of the decision forest.</param>
        public DecisionForest(string name)
        {
            Helper.ThrowIfNull(name);
            _name = name;
        }

        /// <summary>
        /// Initializes a new instance of the DecisionForest class.
        /// </summary>
        /// <param name="name">The name of the decision forest.</param>
        /// <param name="questionList">The questions of the decision forest.</param>
        /// <param name="treeList">The tree list of the decision forest.</param>
        public DecisionForest(string name, ICollection<Question> questionList, List<DecisionTree> treeList)
        {
            Helper.ThrowIfNull(name);
            Helper.ThrowIfNull(questionList);
            Helper.ThrowIfNull(treeList);

            _name = name;
            _treeList = treeList;
            foreach (Question question in questionList)
            {
                _nameIndexedQuestions.Add(question.Name, question);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the decision forest name.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the full phone list used in this forest.
        /// </summary>
        public Dictionary<string, Phone> Phones
        {
            get { return _phones; }
        }

        /// <summary>
        /// Gets the tree list.
        /// </summary>
        public IList<DecisionTree> TreeList
        {
            get { return _treeList; }
        }

        /// <summary>
        /// Gets the full leaf nodes used in this forest.
        /// </summary>
        public IEnumerable<DecisionTreeNode> LeafNodes
        {
            get
            {
                foreach (DecisionTree tree in TreeList)
                {
                    foreach (DecisionTreeNode node in tree.NodeList
                        .Where(n => n.NodeType == DecisionTreeNodeType.Leaf))
                    {
                        yield return node;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the full non-leaf nodes used in this forest.
        /// </summary>
        public IEnumerable<DecisionTreeNode> NonLeafNodes
        {
            get
            {
                foreach (DecisionTree tree in TreeList)
                {
                    foreach (DecisionTreeNode node in tree.NodeList
                        .Where(n => n.NodeType == DecisionTreeNodeType.NonLeaf))
                    {
                        yield return node;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the question list.
        /// </summary>
        public ICollection<Question> QuestionList
        {
            get { return _nameIndexedQuestions.Values; }
        }

        /// <summary>
        /// Gets the count of states (HMM), addressed by this decision forest.
        /// </summary>
        public int StateCount
        {
            get { return TreeList.Count != 0 ? TreeList.Max(t => t.StateIndex()) - StateIndexBeginOffset + 1 : 0; }
        }

        /// <summary>
        /// Gets the number of streams, addressed in this decision forest.
        /// </summary>
        public int StreamCount
        {
            get { return TreeList.FirstOrDefault().StreamCount(); }
        }

        /// <summary>
        /// Gets or sets the stream indexes, for each stream in this decision forest, in overall composited models.
        /// </summary>
        public int[] StreamIndexes
        {
            get
            {
                if (_streamIndexes == null)
                {
                    _streamIndexes = TreeList.FirstOrDefault().StreamIndexes();
                }

                return _streamIndexes;
            }

            set
            {
                _streamIndexes = value;
            }
        }

        /// <summary>
        /// Gets or sets phone set.
        /// </summary>
        public TtsPhoneSet PhoneSet
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the part of speech set.
        /// </summary>
        public TtsPosSet PosSet
        {
            get;
            set;
        }

        /// <summary>
        /// Gets questions, the key is the name of the question.
        /// </summary>
        public SortedDictionary<string, Question> Questions
        {
            get { return _nameIndexedQuestions; }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Combines given files of forests to a single forest.
        /// </summary>
        /// <param name="name">The new forest name.</param>
        /// <param name="filePaths">The given files of forests to be combined.</param>
        /// <returns>The new forest which contains all the forests.</returns>
        public static DecisionForest Combine(string name, IEnumerable<string> filePaths)
        {
            return Combine(name, filePaths.Select(f => new DecisionForest(name).Load(f)));
        }

        /// <summary>
        /// Combines given forests to a single forest.
        /// </summary>
        /// <param name="name">The new forest name.</param>
        /// <param name="forests">The given forests to be combined.</param>
        /// <returns>The new forest which contains all the forests.</returns>
        public static DecisionForest Combine(string name, IEnumerable<DecisionForest> forests)
        {
            IEnumerable<DecisionTree> trees = new List<DecisionTree>();
            SortedDictionary<string, Question> nameIndexedQuestions = new SortedDictionary<string, Question>();
            foreach (DecisionForest forest in forests)
            {
                trees = trees.Union(forest.TreeList);
                foreach (Question question in forest.Questions.Values)
                {
                    if (!nameIndexedQuestions.ContainsKey(question.Name))
                    {
                        nameIndexedQuestions.Add(question.Name, question);
                    }
                    else
                    {
                        if (question.Expression != nameIndexedQuestions[question.Name].Expression)
                        {
                            throw new InvalidDataException(Helper.NeutralFormat("question \"{0}\" have two different expressions", question.Name));
                        }
                    }
                }
            }

            DecisionForest newForest = new DecisionForest(name)
            {
                _nameIndexedQuestions = nameIndexedQuestions,
                _treeList = trees.ToList()
            };

            return newForest;
        }

        #endregion

        #region Serialization operations

        /// <summary>
        /// Saves the questions.
        /// </summary>
        /// <param name="textWriter">Text writer to save only questions.</param>
        public void SaveQuestions(TextWriter textWriter)
        {
            Helper.ThrowIfNull(textWriter);

            foreach (string name in Questions.Keys)
            {
                textWriter.WriteLine(Questions[name].Expression);
            }

            textWriter.WriteLine();
        }

        /// <summary>
        /// Saves the forest.
        /// </summary>
        /// <param name="textWriter">Text writer to save only questions.</param>
        public void SaveForest(TextWriter textWriter)
        {
            Helper.ThrowIfNull(textWriter);

            foreach (DecisionTree tree in _treeList)
            {
                tree.Save(textWriter);
                textWriter.WriteLine();
            }
        }

        /// <summary>
        /// Saves questions and forest.
        /// </summary>
        /// <param name="textWriter">Text writer to save questions and forest.</param>
        public void Save(TextWriter textWriter)
        {
            SaveQuestions(textWriter);
            SaveForest(textWriter);
        }

        /// <summary>
        /// Saves the questions.
        /// </summary>
        /// <param name="questionFileName">Name of the output file.</param>
        public void SaveQuestions(string questionFileName)
        {
            Helper.ThrowIfNull(questionFileName);

            using (StreamWriter streamWriter = new StreamWriter(questionFileName, false, Encoding.ASCII))
            {
                SaveQuestions(streamWriter);
            }
        }

        /// <summary>
        /// Saves the forest.
        /// </summary>
        /// <param name="forestFileName">Name of the output file.</param>
        public void SaveForest(string forestFileName)
        {
            Helper.ThrowIfNull(forestFileName);

            using (StreamWriter streamWriter = new StreamWriter(forestFileName, false, Encoding.ASCII))
            {
                SaveForest(streamWriter);
            }
        }

        /// <summary>
        /// Saves questions and forest.
        /// </summary>
        /// <param name="fileName">Name of the output file.</param>
        public void Save(string fileName)
        {
            Helper.ThrowIfNull(fileName);

            using (StreamWriter streamWriter = new StreamWriter(fileName, false, Encoding.ASCII))
            {
                Save(streamWriter);
            }
        }

        /// <summary>
        /// Loads the forest from the forest file, for example, .INF file.
        /// </summary>
        /// <param name="forestFileName">The location of the input file.</param>
        /// <returns>DecisionForest.</returns>
        public DecisionForest Load(string forestFileName)
        {
            Helper.ThrowIfNull(forestFileName);
            bool quesSection = true;
            bool treeSection = false;
            Collection<string> treeLines = new Collection<string>();
            foreach (string line in Helper.FileLines(forestFileName, Encoding.ASCII, false))
            {
                // Load question sets and count decision tree
                if (quesSection)
                {
                    if (line.IndexOf(Question.QuestionKeyword, StringComparison.Ordinal) < 0)
                    {
                        quesSection = false;
                    }
                    else
                    {
                        Question question = new Question(line);
                        _nameIndexedQuestions.Add(question.Name, question);
                    }
                }

                if (!quesSection)
                {
                    string trimLine = line.Trim();
                    if (!string.IsNullOrEmpty(trimLine))
                    {
                        // tree start
                        if (!treeSection)
                        {
                            treeSection = true;
                            treeLines.Clear();
                        }

                        treeLines.Add(trimLine);
                    }
                    else
                    {
                        // tree end
                        if (treeSection)
                        {
                            DecisionTree tree = new DecisionTree();
                            tree.Load(treeLines);
                            _treeList.Add(tree);
                            treeSection = false;
                        }
                    }
                }
            }

            if (treeSection)
            {
                throw new InvalidDataException(
                    Helper.NeutralFormat("Invalidate last line for Decision Tree {0}", forestFileName));
            }

            return this;
        }

        /// <summary>
        /// Builds phone ids included in this forest.
        /// </summary>
        /// <param name="phoneSet">Phone set used to convert phone string label to phone instance.</param>
        public void BuildPhones(TtsPhoneSet phoneSet)
        {
            Helper.ThrowIfNull(phoneSet);
            _phones.Clear();
            foreach (DecisionTree tree in TreeList)
            {
                if (!_phones.ContainsKey(tree.Phone))
                {
                    _phones.Add(tree.Phone, phoneSet.ToPhone(tree.Phone));
                }
            }
        }

        /// <summary>
        /// Rebuilds question list from questions used in decision nodes.
        /// </summary>
        public void ReSortQuestions()
        {
            IDictionary<string, Question> namedQuestions = _nameIndexedQuestions;

            _nameIndexedQuestions = new SortedDictionary<string, Question>();
            foreach (string name in NonLeafNodes.Select(n => n.QuestionName).Distinct())
            {
                _nameIndexedQuestions.Add(name, namedQuestions[name]);
            }
        }

        #endregion

        #region Tree operations

        /// <summary>
        /// Deletes leaf nodes from tree.
        /// </summary>
        /// <param name="leafSet">The collection to contain the name of leaf nodes.</param>
        public void DeleteLeaves(ICollection<string> leafSet)
        {
            Helper.ThrowIfNull(leafSet);
            foreach (DecisionTree tree in _treeList)
            {
                tree.DeleteLeaves(leafSet);
            }
        }

        /// <summary>
        /// Prunes streams in forest by updating the stream tags in the names of trees.
        /// </summary>
        /// <param name="removingStreams">Indexes of streams to remove.</param>
        public void PruneStream(int[] removingStreams)
        {
            Helper.ThrowIfNull(removingStreams);
            foreach (DecisionTree tree in TreeList)
            {
                for (int i = 0; i < removingStreams.Length; i++)
                {
                    tree.PruneStream(removingStreams[i]);
                }
            }

            StreamIndexes = TreeList.First().StreamIndexes();
        }

        #endregion
    }
}