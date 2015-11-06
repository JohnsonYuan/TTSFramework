//----------------------------------------------------------------------------
// <copyright file="DecisionForestExtension.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines a common library to extend the decision forest.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Extension class for DecisionForest.
    /// </summary>
    public static class DecisionForestExtension
    {
        #region Public Methods

        /// <summary>
        /// Gets the selective tri-phone for each leaf nodes in the forest.
        /// </summary>
        /// <remarks>This method bases on this assumption: The leaf nodes in a forest must have different name.</remarks>
        /// <param name="forest">The given forest.</param>
        /// <param name="phonemes">The whole set of phoneme, which is used to initialize the tri-phone pairs.</param>
        /// <returns>The selective tri-phone for each leaf node in decision forest. The key is the leaf node, and the tri-phone pair set is the value.</returns>
        public static Dictionary<DecisionTreeNode, TriphoneSet> GetSelectiveTriphone(this DecisionForest forest, HashSet<string> phonemes)
        {
            Dictionary<DecisionTreeNode, TriphoneSet> nodeToTriphone = new Dictionary<DecisionTreeNode, TriphoneSet>();
            foreach (DecisionTree tree in forest.TreeList)
            {
                // For each decision tree in forest, we can get the selective tri-phone for its leaf nodes.
                Dictionary<DecisionTreeNode, TriphoneSet> result = GetSelectiveTriphone(tree, forest.Questions, phonemes);

                // Merge all selective tri-phone of decision tree into a single Dictionary to return.
                // Here, the leaf nodes in a forest must have different name.
                foreach (KeyValuePair<DecisionTreeNode, TriphoneSet> kvp in result)
                {
                    nodeToTriphone.Add(kvp.Key, kvp.Value);
                }
            }

            return nodeToTriphone;
        }

        /// <summary>
        /// Gets the pre-selection triphone for each leaf nodes in pre-selection forest.
        /// </summary>
        /// <param name="forest">The pre-selection forest.</param>
        /// <param name="labels">The given list of the labels in training set.</param>
        /// <returns>A Dictionary whose key is a Label to indicate a triphone, whose value is leaf nodes in forest.</returns>
        public static Dictionary<Label, DecisionTreeNode> GetPreselectionTriphone(this DecisionForest forest, List<Label> labels)
        {
            Dictionary<Label, DecisionTreeNode> triphoneToNode = new Dictionary<Label, DecisionTreeNode>();

            // In order to get the all pre-selection tri-phone, all full-context label in training set will be visited.
            foreach (Label label in labels)
            {
                // Get the tri-phone label from the full-context label.
                Label triphone = new Label(label);
                triphone.ResizeFeatureValue(LabelFeatureNameSet.Triphone);

                // If the tri-phone exists, it can be ignored. However, we can process it again to perform verifications.
                if (!triphoneToNode.ContainsKey(triphone))
                {
                    // Travel the forest using the full-context label to get the matched leaf nodes.
                    List<DecisionTreeNode> nodes = FilterDecisionForest(forest, label);

                    // Please notice: in pre-selection tree, each label only match one leaf node since the pre-selection tree is clustered by phone model.
                    if (nodes.Count != 1)
                    {
                        throw new InvalidDataException("The forest is not a invalid pre-selection forest.");
                    }

                    triphoneToNode.Add(triphone, nodes[0]);
                }
            }

            return triphoneToNode;
        }

        /// <summary>
        /// Gets the all leaf nodes which can match the given label in the decision forst.
        /// </summary>
        /// <param name="forest">The given decision forest.</param>
        /// <param name="label">The given label.</param>
        /// <returns>A List object contains all the matched leaf nodes.</returns>
        public static List<DecisionTreeNode> FilterDecisionForest(this DecisionForest forest, Label label)
        {
            List<DecisionTreeNode> nodes = new List<DecisionTreeNode>();

            // For each decision tree in forest, it need to be traveled.
            foreach (DecisionTree tree in forest.TreeList)
            {
                // Firstly, the tree should be matched by phone name if it is a phone-dependent tree.
                bool matched;
                string phone = tree.Phone;
                if (Phoneme.IsAnyPhone(phone))
                {
                    matched = true;
                }
                else
                {
                    matched = label.CentralPhoneme == phone;
                }

                if (matched)
                {
                    // Travel the matched tree, each tree will return a single leaf node.
                    nodes.Add(FilterTree(tree.NodeList[0], forest.Questions, label));
                }
            }

            return nodes;
        }

        /// <summary>
        /// Match a question in HTS.
        /// </summary>
        /// <param name="valueSet">The validate value for this question.</param>
        /// <param name="featureValue">The feature value to judge.</param>
        /// <returns>Match result(true/false).</returns>
        public static bool MatchHtsQuestion(ReadOnlyCollection<string> valueSet, string featureValue)
        {
            int length = featureValue.Length;
            bool questionMatched = false;
            foreach (string value in valueSet)
            {
                if (value.Length == length)
                {
                    bool matched = true;
                    for (int i = 0; i < length; i++)
                    {
                        if (value[i] != featureValue[i] && value[i] != '?')
                        {
                            matched = false;
                            break;
                        }
                    }

                    if (matched)
                    {
                        questionMatched = true;
                        break;
                    }
                }
            }

            return questionMatched;
        }

        /// <summary>
        /// Filters a tree to get the leaf node.
        /// </summary>
        /// <param name="node">The first node of the tree.</param>
        /// <param name="questions">A Dictionary to contains all the Question used by the tree.</param>
        /// <param name="label">The given label.</param>
        /// <returns>The leaf node which match the label.</returns>
        public static DecisionTreeNode FilterTree(DecisionTreeNode node, IDictionary<string, Question> questions, Label label)
        {
            while (node.NodeType != DecisionTreeNodeType.Leaf)
            {
                Question question = questions[node.QuestionName];

                // If the value set of question contains the feature value, it means matched with question mark.
                if (MatchHtsQuestion(question.ValueSet, label.GetFeatureValue(question.FeatureName)))
                {
                    node = node.RightChild;
                }
                else
                {
                    node = node.LeftChild;
                }
            }

            return node;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the selective tri-phone for each leaf nodes in the tree.
        /// </summary>
        /// <param name="tree">The given tree.</param>
        /// <param name="questions">The related questions.</param>
        /// <param name="phonemes">The whole set of phoneme, which is used to initialize the tri-phone pairs.</param>
        /// <returns>The selective tri-phone for each leaf node in decision tree.
        /// The key is the leaf node, and the tri-phone pair set is the value.</returns>
        private static Dictionary<DecisionTreeNode, TriphoneSet> GetSelectiveTriphone(DecisionTree tree, IDictionary<string, Question> questions, HashSet<string> phonemes)
        {
            Dictionary<DecisionTreeNode, TriphoneSet> nodeToTriphone = new Dictionary<DecisionTreeNode, TriphoneSet>();

            // Firstly, generate a whole tri-phone set.
            TriphoneSet set = new TriphoneSet(phonemes);

            // Phone-dependent tree should resize the central phone set.
            string phone = tree.Phone;
            if (!Phoneme.IsAnyPhone(phone))
            {
                set.CentralPhones.Clear();
                set.CentralPhones.Add(phone);
            }

            // Assign the tri-phone set to the root of the tree.
            nodeToTriphone.Add(tree.NodeList[0], set);

            GetSelectiveTriphone(nodeToTriphone, questions);
            return nodeToTriphone;
        }

        /// <summary>
        /// Gets the selective tri-phone for each leaf nodes in the tree.
        /// </summary>
        /// <param name="nodeToTriphone">The selective tri-phone for first node in decision tree. The key is the leaf node, and the tri-phone pair set is the value.</param>
        /// <param name="questions">The related questions.</param>
        private static void GetSelectiveTriphone(Dictionary<DecisionTreeNode, TriphoneSet> nodeToTriphone,
            IDictionary<string, Question> questions)
        {
            while (true)
            {
                // Check whether there are some non-leaf nodes?
                IEnumerable<DecisionTreeNode> list = nodeToTriphone.Keys.Where(o => o.NodeType == DecisionTreeNodeType.NonLeaf);
                if (list.Count() == 0)
                {
                    // All the data are leaf nodes, just break to return them.
                    break;
                }

                // Process the first non-leaf node.
                DecisionTreeNode node = list.First();
                TriphoneSet parentSet = nodeToTriphone[node];
                nodeToTriphone.Remove(node);
                Question question = questions[node.QuestionName];

                // Check the question of this node.
                // Copy from parent.
                TriphoneSet leftChildSet = parentSet;
                TriphoneSet rightChildSet = new TriphoneSet(parentSet);
                switch (question.FeatureName)
                {
                    case LabelFeatureNameSet.CentralPhonemeFeatureName:
                        // It is central phone question.
                        // Right child (YES child) will only contains the ones which occur simultaneously in the question set and parent set.
                        // Left child (NO child) will contains the ones which in parent set but not in question set.
                        rightChildSet.CentralPhones.IntersectWith(question.ValueSet);
                        leftChildSet.CentralPhones.ExceptWith(question.ValueSet);
                        break;
                    case LabelFeatureNameSet.LeftPhonemeFeatureName:
                        // Left phone question.
                        // Right child (YES child) will only contains the ones which occur simultaneously in the question set and parent set.
                        // Left child (NO child) will contains the ones which in parent set but not in question set.
                        rightChildSet.LeftPhones.IntersectWith(question.ValueSet);
                        leftChildSet.LeftPhones.ExceptWith(question.ValueSet);
                        break;
                    case LabelFeatureNameSet.RightPhonemeFeatureName:
                        // Right phone question.
                        // Right child (YES child) will only contains the ones which occur simultaneously in the question set and parent set.
                        // Left child (NO child) will contains the ones which in parent set but not in question set.
                        rightChildSet.RightPhones.IntersectWith(question.ValueSet);
                        leftChildSet.RightPhones.ExceptWith(question.ValueSet);
                        break;
                    default:
                        // This question have nothing about phone. Just copy the set from parent.
                        break;
                }

                // Add the two children.
                nodeToTriphone.Add(node.LeftChild, leftChildSet);
                nodeToTriphone.Add(node.RightChild, rightChildSet);
            }
        }

        #endregion
    }

    /// <summary>
    /// The tri-phone set.
    /// </summary>
    public class TriphoneSet
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the TriphoneSet class as an empty set.
        /// </summary>
        public TriphoneSet()
        {
            LeftPhones = new HashSet<string>();
            CentralPhones = new HashSet<string>();
            RightPhones = new HashSet<string>();
        }

        /// <summary>
        /// Initializes a new instance of the TriphoneSet class as a whole set of all given phonemes.
        /// </summary>
        /// <param name="phonemes">The given phonemes.</param>
        public TriphoneSet(IEnumerable<string> phonemes)
        {
            LeftPhones = new HashSet<string>(phonemes.OrderBy(p => p));
            CentralPhones = new HashSet<string>(phonemes.OrderBy(p => p));
            RightPhones = new HashSet<string>(phonemes.OrderBy(p => p));
        }

        /// <summary>
        /// Initializes a new instance of the TriphoneSet class as a copy of the given instance.
        /// </summary>
        /// <param name="set">The set to copied.</param>
        public TriphoneSet(TriphoneSet set)
        {
            LeftPhones = new HashSet<string>(set.LeftPhones.OrderBy(p => p));
            CentralPhones = new HashSet<string>(set.CentralPhones.OrderBy(p => p));
            RightPhones = new HashSet<string>(set.RightPhones.OrderBy(p => p));
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the left phoneme set.
        /// </summary>
        public HashSet<string> LeftPhones { get; set; }

        /// <summary>
        /// Gets or sets the central phoneme set.
        /// </summary>
        public HashSet<string> CentralPhones { get; set; }

        /// <summary>
        /// Gets or sets the right phoneme set.
        /// </summary>
        public HashSet<string> RightPhones { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the all possible tri-phone from this set.
        /// </summary>
        /// <returns>The list of Label object to hold the tri-phone.</returns>
        public List<Label> GetAllTriphone()
        {
            List<Label> list = new List<Label>();
            foreach (string left in LeftPhones)
            {
                foreach (string central in CentralPhones)
                {
                    foreach (string right in RightPhones)
                    {
                        Label label = new Label(LabelFeatureNameSet.Triphone);
                        label.LeftPhoneme = left;
                        label.CentralPhoneme = central;
                        label.RightPhoneme = right;
                        list.Add(label);
                    }
                }
            }

            return list;
        }

        #endregion
    }
}