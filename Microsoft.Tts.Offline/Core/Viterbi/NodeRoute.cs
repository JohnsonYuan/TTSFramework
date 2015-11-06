//----------------------------------------------------------------------------
// <copyright file="NodeRoute.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements node route in Viterbi search space
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Viterbi
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Node route.
    /// </summary>
    public class NodeRoute : IComparable<NodeRoute>
    {
        #region Fields

        private int _index;
        private Collection<CostNode> _costNodes = new Collection<CostNode>();

        private bool _visible;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether this route is visible on rendering.
        /// </summary>
        public bool Visible
        {
            get { return _visible; }
            set { _visible = value; }
        }

        /// <summary>
        /// Gets Cost node collection on this route.
        /// </summary>
        public Collection<CostNode> CostNodes
        {
            get { return _costNodes; }
        }

        /// <summary>
        /// Gets or sets Index of this cost node route in whole viterbi view.
        /// </summary>
        public int Index
        {
            get { return _index; }
            set { _index = value; }
        }

        /// <summary>
        /// Gets Total cost of this node route.
        /// </summary>
        public float TotalCost
        {
            get
            {
                if (CostNodes.Count == 0)
                {
                    return 0.0f;
                }
                else
                {
                    return CostNodes[CostNodes.Count - 1].RouteCost;
                }
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Get continued selected groups.
        /// </summary>
        /// <returns>Unit group array.</returns>
        public string[] GetSelectedGroups()
        {
            List<string> groups = new List<string>();
            StringBuilder group = new StringBuilder();
            CostNode prevNode = null;
            foreach (CostNode currNode in _costNodes)
            {
                if (prevNode != null)
                {
                    if (prevNode.IsPreceed(currNode))
                    {
                        group.Append(" . ");
                    }
                    else
                    {
                        groups.Add(group.ToString());
                        group.Remove(0, group.Length);
                    }
                }

                group.Append(currNode.WaveUnit.Name);
                if (currNode.WaveUnit.Features.TtsStress != TtsStress.None
                    && TtsUnitFeature.IsVowel(currNode.WaveUnit.Features.PosInSyllable))
                {
                    group.AppendFormat(CultureInfo.InvariantCulture,
                        " {0}", (int)currNode.WaveUnit.Features.TtsStress);
                }

                prevNode = currNode;
            }

            groups.Add(group.ToString());

            return groups.ToArray();
        }

        #endregion

        #region IComparable<NodeRoute> Members

        /// <summary>
        /// Compare this node route instance with other instance.
        /// </summary>
        /// <param name="other">Other instance to compare with.</param>
        /// <returns> A 32-bit signed integer that indicates the relative order of the objects
        ///    being compared. The return value has the following meanings: Value Meaning
        ///    Less than zero This object is less than the other parameter.Zero This object
        ///    is equal to other. Greater than zero This object is greater than other.
        /// </returns>
        int IComparable<NodeRoute>.CompareTo(NodeRoute other)
        {
            return CompareTo(other);
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// Reverse cost nodes in the route.
        /// </summary>
        internal void ReverseCostNodes()
        {
            for (int i = 0; i < CostNodes.Count / 2; i++)
            {
                int j = CostNodes.Count - i - 1;

                SwitchNode(CostNodes, i, j);
            }
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Compare this node route instance with other instance.
        /// </summary>
        /// <param name="other">Other instance to compare with.</param>
        /// <returns> A 32-bit signed integer that indicates the relative order of the objects
        ///    being compared. The return value has the following meanings: Value Meaning
        ///    Less than zero This object is less than the other parameter.Zero This object
        ///    is equal to other. Greater than zero This object is greater than other.
        /// </returns>
        protected int CompareTo(NodeRoute other)
        {
            float othercost = other.TotalCost;
            float thisCost = this.TotalCost;

            if (othercost == thisCost)
            {
                return 0;
            }
            else if (othercost > thisCost)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Swith two cost nodes in the cost node collection.
        /// </summary>
        /// <param name="costNodes">Cost node collection to operate on.</param>
        /// <param name="i">First node index.</param>
        /// <param name="j">Second node index.</param>
        private static void SwitchNode(Collection<CostNode> costNodes, int i, int j)
        {
            CostNode nodeI = costNodes[i];
            CostNode nodeJ = costNodes[j];

            costNodes[j] = nodeI;
            costNodes[i] = nodeJ;
        }

        #endregion
    }
}