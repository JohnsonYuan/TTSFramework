//----------------------------------------------------------------------------
// <copyright file="CartTree.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Cart Tree
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Cart
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// CART tree.
    /// </summary>
    public class CartTree
    {
        #region Fields

        private Collection<CartNode> _nodes = new Collection<CartNode>();
        private CartNode _root;
        private MetaCart _metaCart;

        #endregion

        #region Construciton

        /// <summary>
        /// Initializes a new instance of the <see cref="CartTree"/> class.
        /// </summary>
        /// <param name="metaCart">Metadata of CART tree.</param>
        public CartTree(MetaCart metaCart)
        {
            if (metaCart == null)
            {
                throw new ArgumentNullException("metaCart");
            }

            _metaCart = metaCart;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets MetaCart.
        /// </summary>
        public MetaCart MetaCart
        {
            get { return _metaCart; }
        }

        /// <summary>
        /// Gets All nodes in this tree.
        /// </summary>
        public Collection<CartNode> Nodes
        {
            get { return _nodes; }
        }

        /// <summary>
        /// Gets or sets The root nod of this tree.
        /// </summary>
        public CartNode Root
        {
            get
            {
                return _root;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _root = value;
            }
        }

        #endregion

        #region Public static operations

        /// <summary>
        /// Parse set string presentation to BitArray.
        /// </summary>
        /// <param name="setPresent">Set present in string.</param>
        /// <param name="unitSet">Unit set.</param>
        public static void ParseSetPresentation(string setPresent, BitArray unitSet)
        {
            if (string.IsNullOrEmpty(setPresent))
            {
                throw new ArgumentNullException("setPresent");
            }

            if (unitSet == null)
            {
                throw new ArgumentNullException("unitSet");
            }

            string[] items = setPresent.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            unitSet.SetAll(false);
            unitSet.Length = int.Parse(items[1], CultureInfo.InvariantCulture);

            if (items[0] == "B")
            {
                int k = 0;
                for (int j = items[2].Length - 1; j >= 0; --j, ++k)
                {
                    string hex = items[2][j].ToString(CultureInfo.InvariantCulture);
                    int val = int.Parse(hex, System.Globalization.NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture);
                    if ((val & 0x1) != 0)
                    {
                        unitSet.Set(k * 4, true);
                    }

                    if ((val & 0x2) != 0)
                    {
                        unitSet.Set((k * 4) + 1, true);
                    }

                    if ((val & 4) != 0)
                    {
                        unitSet.Set((k * 4) + 2, true);
                    }

                    if ((val & 8) != 0)
                    {
                        unitSet.Set((k * 4) + 3, true);
                    }
                }
            }
            else if (items[0] == "I")
            {
                for (int i = 3; i < items.Length; i++)
                {
                    unitSet.Set(int.Parse(items[i], CultureInfo.InvariantCulture), true);
                }
            }
            else
            {
                Debug.Assert(false);
                string message = string.Format(CultureInfo.InvariantCulture,
                        "Only Bit set or Index set is supported for CART tree in text format. But the set type [{0}] is found.",
                        items[0]);
                throw new NotSupportedException(message);
            }
        }

        /// <summary>
        /// Present BitArray into string line.
        /// </summary>
        /// <param name="unitSet">Unit set.</param>
        /// <returns>Set present.</returns>
        public static string ComposeSetPresent(BitArray unitSet)
        {
            if (unitSet == null)
            {
                throw new ArgumentNullException("unitSet");
            }

            StringBuilder sb = new StringBuilder();

            if (unitSet.Length / 4 < unitSet.Count * 4)
            {
                sb.Append(" B ");
                sb.Append(unitSet.Length);
                sb.Append(" ");
                StringBuilder hexBuilder = new StringBuilder();
                for (int i = 0; i < unitSet.Length; i = i + 4)
                {
                    int hex = 0;
                    if (unitSet[i])
                    {
                        hex += 1;
                    }

                    if (i + 1 < unitSet.Length && unitSet[i + 1])
                    {
                        hex += 2;
                    }

                    if (i + 2 < unitSet.Length && unitSet[i + 2])
                    {
                        hex += 4;
                    }

                    if (i + 3 < unitSet.Length && unitSet[i + 3])
                    {
                        hex += 8;
                    }

                    hexBuilder.Insert(0, hex.ToString("X", CultureInfo.InvariantCulture));
                }

                if (hexBuilder.Length % 2 != 0)
                {
                    hexBuilder.Insert(0, "0");
                }

                sb.Append(hexBuilder.ToString());
            }
            else
            {
                sb.Append(" I ");
                sb.Append(unitSet.Count);
                sb.Append(" ");
                for (int i = 0; i < unitSet.Length; i++)
                {
                    if (unitSet[i])
                    {
                        sb.Append(i.ToString(CultureInfo.InvariantCulture));
                        sb.Append(" ");
                    }
                }
            }

            return sb.ToString().Trim();
        }

        #endregion

        #region Operations

        /// <summary>
        /// Search all node in this tree for which node that satisfies the special feature.
        /// </summary>
        /// <param name="feature">Unit feature.</param>
        /// <returns>Cart node most closing to the feature.</returns>
        public CartNode Test(TtsUnitFeature feature)
        {
            return Root.Test(feature);
        }

        #endregion

        #region Serialize & deserialize

        /// <summary>
        /// Load CART tree from binary stream.
        /// </summary>
        /// <param name="br">Binary reader to load CART tree from.</param>
        public void Load(BinaryReader br)
        {
            Nodes.Clear();

            CartNode node = new CartNode(_metaCart);
            node.Load(br);

            Root = node;
        }

        /// <summary>
        /// Save CART tree into binary format.
        /// </summary>
        /// <param name="bw">Binary reader to save CART tree.</param>
        public void Save(BinaryWriter bw)
        {
            Debug.Assert(Root != null);
            if (Root == null)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Root should not be null");
                throw new ArgumentException(message);
            }

            Root.Save(bw);
        }

        /// <summary>
        /// Load CART tree from text file.
        /// </summary>
        /// <param name="filePath">Text CART file to load.</param>
        public void Load(string filePath)
        {
            Nodes.Clear();

            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line = null;
                    while ((line = sr.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (string.IsNullOrEmpty(line))
                        {
                            continue;
                        }

                        Match m = Regex.Match(line, @"(\S+)\s+(\S+)\s+(\S+)\s+(.*)");
                        if (!m.Success)
                        {
                            System.Diagnostics.Debug.Assert(m.Success);
                        }

                        CartNode node = new CartNode(_metaCart);
                        node.Index = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                        node.ParentIndex = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                        node.QuestionLogic = m.Groups[3].Value;
                        node.SetPresent = m.Groups[4].Value;

                        if (node.QuestionLogic != "*")
                        {
                            node.Question = new Question(MetaCart);
                            node.Question.Parse(node.QuestionLogic);
                        }

                        if (node.SetPresent != "*")
                        {
                            ParseSetPresentation(node.SetPresent, node.UnitSet);
                        }

                        Nodes.Add(node);
                        System.Diagnostics.Debug.Assert(node.Index == Nodes.IndexOf(node) + 1);
                    }
                }
            }
            catch (NotSupportedException nse)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Failed to load CART tree file in text format from [{0}]",
                    filePath);
                throw new InvalidDataException(message, nse);
            }

            // PostBuild
            PostLoad();
        }

        /// <summary>
        /// Save CART tree to text file.
        /// </summary>
        /// <param name="filePath">Text CART file to save.</param>
        public void Save(string filePath)
        {
            int nodeIndex = 1;
            CartNode currNode = Root;

            Microsoft.Tts.Offline.Utility.Helper.EnsureFolderExistForFile(filePath);
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.Unicode))
            {
                // iterate each speech segment
                while (currNode != null)
                {
                    // pre-visit
                    // dump here
                    currNode.Index = nodeIndex++;
                    sw.Write(currNode.Index);
                    if (currNode.Parent == null)
                    {
                        sw.Write(" 0");
                    }
                    else
                    {
                        if (currNode.Parent.LeftChild == currNode)
                        {
                            sw.Write(" {0}", currNode.Parent.Index);
                        }
                        else
                        {
                            sw.Write(" -{0}", currNode.Parent.Index);
                        }
                    }

                    if (currNode.LeftChild != null)
                    {
                        sw.WriteLine(" {0} *", currNode.QuestionLogic);
                    }
                    else
                    {
                        sw.Write(" * ");
                        sw.WriteLine(ComposeSetPresent(currNode.UnitSet));
                    }

                    // visit sub segment first
                    if (currNode.LeftChild != null)
                    {
                        currNode = currNode.LeftChild;
                    }
                    else
                    {
                        while (currNode != null)
                        {
                            // break out of current loop when there is a sibling
                            if (currNode.Parent == null)
                            {
                                currNode = null;
                            }
                            else if (currNode.Parent != null && currNode.Parent.RightChild == currNode)
                            {
                                currNode = currNode.Parent;
                            }
                            else
                            {
                                currNode = currNode.Parent.RightChild;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Run post-load.
        /// </summary>
        private void PostLoad()
        {
            for (int i = Nodes.Count - 1; i >= 0; --i)
            {
                CartNode node = Nodes[i];
                if (node.ParentIndex == 0)
                {
                    Root = node;
                    break;
                }
                else if (node.ParentIndex > 0)
                {
                    if (node.ParentIndex - 1 < 0 || node.ParentIndex - 1 >= Nodes.Count)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "The cart tree file is malformed for the parent index [{0}] is out of range [{1}].",
                            node.ParentIndex - 1, Nodes.Count);
                        throw new InvalidDataException(message);
                    }

                    node.Parent = Nodes[node.ParentIndex - 1];
                    if (node.Parent == null)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "The cart tree file is malformed for the parent index [{0}] has no parent.",
                            node.ParentIndex - 1);
                        throw new InvalidDataException(message);
                    }

                    System.Diagnostics.Debug.Assert(node.Parent.LeftChild == null);
                    node.Parent.LeftChild = node;
                }
                else
                {
                    // node.ParentIndex < 0
                    if (-node.ParentIndex - 1 < 0 || -node.ParentIndex - 1 >= Nodes.Count)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "The cart tree file is malformed for the parent index [{0}] is out of range [{1}].",
                            -node.ParentIndex - 1, Nodes.Count);
                        throw new InvalidDataException(message);
                    }

                    node.Parent = Nodes[-node.ParentIndex - 1];
                    if (node.Parent == null)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "The cart tree file is malformed for the parent index [{0}] has no parent.",
                            -node.ParentIndex - 1);
                        throw new InvalidDataException(message);
                    }

                    System.Diagnostics.Debug.Assert(node.Parent.RightChild == null);
                    node.Parent.RightChild = node;
                }
            }
        }

        #endregion
    }
}