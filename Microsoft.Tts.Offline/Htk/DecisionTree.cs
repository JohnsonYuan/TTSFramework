//----------------------------------------------------------------------------
// <copyright file="DecisionTree.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines a common library to manipulate certain decision tree
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
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Tree Node type: leaf or non leaf.
    /// </summary>
    public enum DecisionTreeNodeType
    {
        /// <summary>
        /// Non leaf node type.
        /// </summary>
        NonLeaf = 0,       // Non-leaf node

        /// <summary>
        /// Leaf node type.
        /// </summary>
        Leaf               // Leaf node
    }

    /// <summary>
    /// Functions to parse the naming schema of decision tree in HTK format
    /// As to tree names, for example:
    ///     {*}
    ///     {*-y+*}
    ///     {*}[2].stream[2,3,4]
    ///     {*}[2]
    ///     {*-y+*}[4].stream[1]
    ///     {*-zh+*}[5].stream[2,3,4].
    /// </summary>
    public static class DecisionTreeName
    {
        /// <summary>
        /// Parses stream indexes from macro of tree name.
        /// </summary>
        /// <param name="macro">Tree macro name.</param>
        /// <returns>Stream indexes.</returns>
        public static int[] ParseStreamIndexes(string macro)
        {
            Helper.ThrowIfNull(macro);

            int[] indexes;
            Match match = Regex.Match(macro, @"stream\[(.+)\]");
            if (!match.Success)
            {
                try
                {
                    // Try to parse state index.
                    ParseStateIndex(macro);

                    // If there is state information, it will have default stream indexes.
                    indexes = new[] { 1 };
                }
                catch (InvalidOperationException)
                {
                    throw new InvalidOperationException("The tree name contains no stream information");
                }
            }
            else
            {
                string[] items = match.Groups[1].Value.Split(',');
                indexes = new int[items.Length];
                for (int i = 0; i < items.Length; ++i)
                {
                    indexes[i] = int.Parse(items[i], CultureInfo.InvariantCulture);
                }
            }

            return indexes;
        }

        /// <summary>
        /// Parses state index in the decision tree name.
        /// </summary>
        /// <param name="macro">Tree macro name.</param>
        /// <returns>State index.</returns>
        public static int ParseStateIndex(string macro)
        {
            Helper.ThrowIfNull(macro);

            Match match = Regex.Match(macro, @"\{.+?\}\[(.+?)\]");
            if (!match.Success)
            {
                throw new InvalidOperationException("The tree name contains no state information");
            }

            int stateIndex = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            Debug.Assert(stateIndex >= 2, "Support HTK state index in tree should start from 2.");

            return stateIndex;
        }

        /// <summary>
        /// Parses phone label in the decision tree name.
        /// </summary>
        /// <param name="macro">Tree macro name.</param>
        /// <returns>Phone label.</returns>
        public static string ParsePhoneLabel(string macro)
        {
            Helper.ThrowIfNull(macro);

            string label = string.Empty;
            Match match = Regex.Match(macro, @"\{\*\-(\w+)\+\*\}");
            if (match.Success)
            {
                label = match.Groups[1].Value;
                Debug.Assert(!string.IsNullOrEmpty(label), "Phone label should not be empty.");
            }
            else if (Regex.Match(macro, @"\{\*\}").Success)
            {
                // Handle phone-independent-model, "{*}"
                label = Phoneme.AnyPhone;
            }

            return label;
        }
    }

    /// <summary>
    /// Implementation of decision tree node.
    /// </summary>
    public class DecisionTreeNode
    {
        #region Field

        /// <summary>
        /// Invalid node index, used to present leaf node's index.
        /// </summary>
        public static readonly int InvalidNodeIndex = -1;

        private string _name;
        private string _questionName;
        private DecisionTreeNodeType _nodeType;
        private DecisionTreeNode _parent;
        private DecisionTreeNode _leftChild;
        private DecisionTreeNode _rightChild;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Name, can be node index ("-5") in decision tree,
        /// Or state macro name ("zh_logF0_s2_1") in MMF.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets Non-leaf node index in decision tree. return InvalidNodeIndex if as leaf node.
        /// </summary>
        public int Index
        {
            get
            {
                int index = InvalidNodeIndex;
                Match match = Regex.Match(Name.Trim(), @"^\-([0-9]+)$");
                if (match.Success)
                {
                    index = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                }

                return index;
            }
        }

        /// <summary>
        /// Gets or sets Position in font.
        /// </summary>
        public int Position
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Question name.
        /// </summary>
        public string QuestionName
        {
            get { return _questionName; }
            set { _questionName = value; }
        }

        /// <summary>
        /// Gets or sets Question index in font.
        /// </summary>
        public int QuestionIndex
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets The offsets of this models.
        /// </summary>
        public int[] RefDataOffsets
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the node Type of this tree node.
        /// </summary>
        public DecisionTreeNodeType NodeType
        {
            get { return _nodeType; }
            set { _nodeType = value; }
        }

        /// <summary>
        /// Gets or sets Parent Node.
        /// </summary>
        public DecisionTreeNode Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        /// <summary>
        /// Gets or sets the left child node, as No child.
        /// </summary>
        public DecisionTreeNode LeftChild
        {
            get { return _leftChild; }
            set { _leftChild = value; }
        }

        /// <summary>
        /// Gets or sets the right child node, as Yes child.
        /// </summary>
        public DecisionTreeNode RightChild
        {
            get { return _rightChild; }
            set { _rightChild = value; }
        }

        /// <summary>
        /// Gets Enumerate current node and its children layer by layer.
        /// </summary>
        public IEnumerable<DecisionTreeNode> LayeredNodes
        {
            get
            {
                Queue<DecisionTreeNode> queue = new Queue<DecisionTreeNode>();
                queue.Enqueue(this);
                while (queue.Count != 0)
                {
                    DecisionTreeNode node = queue.Dequeue();
                    if (node.NodeType == DecisionTreeNodeType.NonLeaf)
                    {
                        Debug.Assert(node.LeftChild != null && node.RightChild != null,
                            "Non-leaf node should have both valid left and right children.");
                        queue.Enqueue(node.LeftChild);
                        queue.Enqueue(node.RightChild);
                    }

                    yield return node;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get hash code.
        /// </summary>
        /// <returns>The hash code of this object.</returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        /// <summary>
        /// Test equal with obj.
        /// </summary>
        /// <param name="obj">Object to test with.</param>
        /// <returns>True if equal, otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            DecisionTreeNode node = obj as DecisionTreeNode;
            if (node != null)
            {
                return Name == node.Name;
            }

            return false;
        }

        #endregion
    }

    /// <summary>
    /// Implementation of decision tree.
    /// </summary>
    public class DecisionTree
    {
        #region Const

        /// <summary>
        /// Start Symbol of decision tree nodes.
        /// </summary>
        private const string StartSymbol = "{";

        /// <summary>
        /// End Symbol of decision tree nodes.
        /// </summary>
        private const string EndSymbol = "}";

        /// <summary>
        /// The char is around the leaf node name, e.g. "a_lsp_s2_1",.
        /// </summary>
        private const char LeafNameChar = '\"';

        #endregion

        #region Fields

        private string _name;
        private List<DecisionTreeNode> _nodeList = new List<DecisionTreeNode>();
        private Dictionary<string, DecisionTreeNode> _leafNodeMap = new Dictionary<string, DecisionTreeNode>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the DecisionTree class.
        /// </summary>
        public DecisionTree()
        {
        }

        /// <summary>
        /// Initializes a new instance of the DecisionTree class.
        /// </summary>
        /// <param name="name">The name of the decision tree.</param>
        /// <param name="nodeList">The node list of the decision tree.</param>
        public DecisionTree(string name, List<DecisionTreeNode> nodeList)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (nodeList == null)
            {
                throw new ArgumentNullException("nodeList");
            }

            _name = name;
            _nodeList = nodeList;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of this decision tree.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets Tree node list.
        /// </summary>
        public IList<DecisionTreeNode> NodeList
        {
            get { return _nodeList; }
        }

        /// <summary>
        /// Gets Leaf node map.
        /// </summary>
        public IDictionary<string, DecisionTreeNode> LeafNodeMap
        {
            get { return _leafNodeMap; }
        }

        /// <summary>
        /// Gets Phone label used in HTK tree.
        /// </summary>
        public string Phone
        {
            get { return DecisionTreeName.ParsePhoneLabel(Name); }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// PreOrder decision tree traversal.
        /// </summary>
        /// <param name="node">Start node for traversal.</param>
        /// <param name="visitor">Visitor funclet.</param>
        /// <returns>True if the visitor needs continue at current node; false otherwise.</returns>
        public static bool PreOrderVisit(DecisionTreeNode node, Func<DecisionTreeNode, bool> visitor)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (visitor == null)
            {
                throw new ArgumentNullException("visitor");
            }

            var needContinue = visitor(node);

            if (needContinue && node.NodeType == DecisionTreeNodeType.NonLeaf)
            {
                needContinue = PreOrderVisit(node.LeftChild, visitor);
            }

            if (needContinue && node.NodeType == DecisionTreeNodeType.NonLeaf)
            {
                needContinue = PreOrderVisit(node.RightChild, visitor);
            }

            return needContinue;
        }

        /// <summary>
        /// Load the decision tree.
        /// </summary>
        /// <param name="treeLines">Multiple lines to contain the tree information.</param>
        public void Load(Collection<string> treeLines)
        {
            Helper.ThrowIfNull(treeLines);

            // Verify treeLines' format
            if (!(treeLines.Count == 2 || (treeLines.Count >= 4 && treeLines[1].Equals(StartSymbol) &&
                treeLines[treeLines.Count - 1].Equals(EndSymbol))))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Tree \"{0}\" has invalidate format", treeLines[0]));
            }

            Dictionary<string, DecisionTreeNode> nameNodeMap = new Dictionary<string, DecisionTreeNode>();
            Name = treeLines[0].Trim();

            // Standard format
            if (treeLines.Count >= 4)
            {
                // add non-leaf node in the beginning
                for (int i = 2; i < treeLines.Count - 1; i++)
                {
                    DecisionTreeNode node = new DecisionTreeNode();
                    _nodeList.Add(node);
                    nameNodeMap.Add(NodeIdxToName(i - 2), node);
                }

                for (int i = 2; i < treeLines.Count - 1; i++)
                {
                    string line = treeLines[i];
                    string[] infos = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (infos.Length != 4)
                    {
                        throw new InvalidDataException(Helper.NeutralFormat(
                            "Tree node format should be \"Name Question NoChild YesChild\": {0}", line));
                    }

                    Debug.Assert(infos[0].Equals(NodeIdxToName(i - 2)));

                    // pick up non-leaf node
                    DecisionTreeNode node = nameNodeMap[infos[0]];
                    node.Name = infos[0];
                    node.QuestionName = infos[1];
                    node.NodeType = DecisionTreeNodeType.NonLeaf;
                    if (infos[2].IndexOf('-') == 0)
                    {
                        // non leaf node
                        Debug.Assert(nameNodeMap.ContainsKey(infos[2]));
                        DecisionTreeNode childNode = nameNodeMap[infos[2]];
                        node.LeftChild = childNode;
                        childNode.Parent = node;
                    }
                    else
                    {
                        // leaf node
                        Debug.Assert(infos[2].IndexOf(LeafNameChar) == 0);
                        DecisionTreeNode leafNode = new DecisionTreeNode();
                        leafNode.Name = infos[2].Trim(LeafNameChar);
                        leafNode.NodeType = DecisionTreeNodeType.Leaf;
                        leafNode.Parent = node;
                        node.LeftChild = leafNode;
                        _leafNodeMap.Add(leafNode.Name, leafNode);
                        _nodeList.Add(leafNode);
                    }

                    if (infos[3].IndexOf('-') == 0)
                    {
                        // non leaf node
                        Debug.Assert(nameNodeMap.ContainsKey(infos[3]));
                        DecisionTreeNode childNode = nameNodeMap[infos[3]];
                        node.RightChild = childNode;
                        childNode.Parent = node;
                    }
                    else
                    {
                        // leaf node
                        Debug.Assert(infos[3].IndexOf(LeafNameChar) == 0);
                        DecisionTreeNode leafNode = new DecisionTreeNode();
                        leafNode.Name = infos[3].Trim(LeafNameChar);
                        leafNode.NodeType = DecisionTreeNodeType.Leaf;
                        leafNode.Parent = node;
                        node.RightChild = leafNode;
                        _leafNodeMap.Add(leafNode.Name, leafNode);
                        _nodeList.Add(leafNode);
                    }
                }
            }
            else
            {
                // one leaf node format
                DecisionTreeNode leafNode = new DecisionTreeNode();
                leafNode.Name = treeLines[1].Trim(LeafNameChar);
                leafNode.NodeType = DecisionTreeNodeType.Leaf;
                _leafNodeMap.Add(leafNode.Name, leafNode);
                _nodeList.Add(leafNode);
            }
        }

        /// <summary>
        /// Save the decision tree.
        /// </summary>
        /// <param name="textWriter">Output stream.</param>
        public void Save(TextWriter textWriter)
        {
            Helper.ThrowIfNull(textWriter);

            textWriter.WriteLine(" " + Name);
            if (NodeList.Count > 1)
            {
                // standard format
                textWriter.WriteLine(StartSymbol);
                foreach (DecisionTreeNode treeNode in NodeList)
                {
                    if (treeNode.NodeType == DecisionTreeNodeType.NonLeaf)
                    {
                        textWriter.Write(" {0,3} {1,-45} ", treeNode.Name, treeNode.QuestionName);
                        if (treeNode.LeftChild.NodeType == DecisionTreeNodeType.Leaf)
                        {
                            textWriter.Write(" {0,15} ", LeafNameChar + treeNode.LeftChild.Name + LeafNameChar);
                        }
                        else
                        {
                            textWriter.Write("  {0,5}    ", treeNode.LeftChild.Name);
                        }

                        if (treeNode.RightChild.NodeType == DecisionTreeNodeType.Leaf)
                        {
                            textWriter.Write(" {0,15} ", LeafNameChar + treeNode.RightChild.Name + LeafNameChar);
                        }
                        else
                        {
                            textWriter.Write("  {0,5}    ", treeNode.RightChild.Name);
                        }

                        textWriter.WriteLine();
                    }
                }

                textWriter.WriteLine(EndSymbol);
            }
            else
            {
                // one leaf node format
                textWriter.WriteLine("   {1}{0}{2}", _nodeList[0].Name, LeafNameChar, LeafNameChar);
            }
        }

        /// <summary>
        /// Delete leaf nodes from tree.
        /// </summary>
        /// <param name="leafSet">The collection to contain the name of leaf nodes.</param>
        public void DeleteLeaves(ICollection<string> leafSet)
        {
            Helper.ThrowIfNull(leafSet);

            int nodeCount = NodeList.Count;
            foreach (string leafName in leafSet)
            {
                if (_leafNodeMap.ContainsKey(leafName))
                {
                    DecisionTreeNode leafNode = _leafNodeMap[leafName];
                    DecisionTreeNode parentNode = leafNode.Parent;

                    // parentNode != null is the standard format
                    // parent happens when only one leaf node in the tree, action: keep the leaf node without change
                    if (parentNode != null)
                    {
                        DecisionTreeNode anotherChildNode = null;
                        if (parentNode.LeftChild == leafNode)
                        {
                            anotherChildNode = parentNode.RightChild;
                        }
                        else
                        {
                            anotherChildNode = parentNode.LeftChild;
                        }

                        Debug.Assert(anotherChildNode != null);

                        DecisionTreeNode grandParentNode = parentNode.Parent;
                        if (grandParentNode != null)
                        {
                            if (grandParentNode.LeftChild == parentNode)
                            {
                                grandParentNode.LeftChild = anotherChildNode;
                                anotherChildNode.Parent = grandParentNode;
                            }
                            else
                            {
                                grandParentNode.RightChild = anotherChildNode;
                                anotherChildNode.Parent = grandParentNode;
                            }
                        }
                        else
                        {
                            // grandparentNode==null happens when only one non-leaf node with two child leaf nodes in the tree
                            anotherChildNode.Parent = null;
                        }

                        _leafNodeMap.Remove(leafName);
                        _nodeList.Remove(leafNode);
                        _nodeList.Remove(parentNode);
                    }
                }
            }

            Debug.Assert(nodeCount >= NodeList.Count);
            AssignNameToNonLeafNode();
        }

        /// <summary>
        /// Assign name/id to non-leaf node.
        /// </summary>
        public void AssignNameToNonLeafNode()
        {
            TravelOneNode(_nodeList[0], 0);

            // the goal is to sort all nodes as below sequence
            // appropriate order: 0 -1 -2 -3 -4 ... leafnode1 leafnode2 leafnode3
            _nodeList.Sort(Compare);
        }

        /// <summary>
        /// Check if the decision tree match a phone.
        /// </summary>
        /// <param name="phone">The phone name.</param>
        /// <returns>True if match, false otherwise.</returns>
        public bool MatchPhone(string phone)
        {
            return Phone == phone || Phone == Phoneme.AnyPhone;
        }

        /// <summary>
        /// Tests whether this object is equal to the given one.
        /// </summary>
        /// <param name="obj">The given object to test whether is equal to this object.</param>
        /// <returns>True if the object is equal to the given one, otherwise false.</returns>
        public override bool Equals(object obj)
        {
            DecisionTree other = obj as DecisionTree;
            if (other == null)
            {
                return false;
            }

            return other._name == _name;
        }

        /// <summary>
        /// Gets the hash code of this object.
        /// </summary>
        /// <returns>The hash code of this object.</returns>
        public override int GetHashCode()
        {
            return _name.GetHashCode();
        }

        #endregion

        #region Private Function

        /// <summary>
        /// Convert list idx to node name.
        /// </summary>
        /// <param name="idx">Idx of node list.</param>
        /// <returns>Name of the node index.</returns>
        private static string NodeIdxToName(int idx)
        {
            return (0 - idx).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Assign a id to certain node and its children.
        /// </summary>
        /// <param name="node">Tree node to visit.</param>
        /// <param name="seq">Node id sequence.</param>
        /// <returns>The next id.</returns>
        private static int TravelOneNode(DecisionTreeNode node, int seq)
        {
            if (node.NodeType == DecisionTreeNodeType.NonLeaf)
            {
                node.Name = NodeIdxToName(seq);
                seq++;
                seq = TravelOneNode(node.LeftChild, seq);
                seq = TravelOneNode(node.RightChild, seq);
            }

            return seq;
        }

        /// <summary>
        /// Compare the DecisionTreeNode, used by List.Sort() method.
        /// </summary>
        /// <param name="firstNode">Source DecisionTreeNode.</param>
        /// <param name="secondNode">Destination DecisionTreeNode.</param>
        /// <returns>Positive for x greater than y, 0 for equal and negative for x less than y.</returns>        
        private static int Compare(DecisionTreeNode firstNode, DecisionTreeNode secondNode)
        {
            Helper.ThrowIfNull(firstNode);
            Helper.ThrowIfNull(secondNode);

            // give leaf node a very low ID to ensure leaf node was put in the end.
            int firstId = int.MinValue / 2;
            int secondId = int.MinValue / 2;
            if (firstNode.NodeType == DecisionTreeNodeType.NonLeaf)
            {
                firstId = int.Parse(firstNode.Name, CultureInfo.InvariantCulture);
            }

            if (secondNode.NodeType == DecisionTreeNodeType.NonLeaf)
            {
                secondId = int.Parse(secondNode.Name, CultureInfo.InvariantCulture);
            }

            return secondId - firstId;
        }

        #endregion
    }
}