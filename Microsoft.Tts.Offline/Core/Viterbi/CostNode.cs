//----------------------------------------------------------------------------
// <copyright file="CostNode.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements cost node in Viterbi search space
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Viterbi
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Cost node.
    /// </summary>
    public class CostNode : IComparable<CostNode>
    {
        #region Fields

        // the wave unit information of this node
        private WaveUnit _waveUnit;

        // cost between the unit instance itself and the target
        private float _targetCost;

        // the cost of the whole path ended with this instance
        private float _routeCost;

        private int _clustIndex;
        private int _index;

        // the previous node which giving the least path cost to this node
        private int _precedeNodeIndex;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Preceding node index.
        /// </summary>
        public int PrecedeNodeIndex
        {
            get { return _precedeNodeIndex; }
            set { _precedeNodeIndex = value; }
        }

        /// <summary>
        /// Gets or sets Index of this cost node.
        /// </summary>
        public int Index
        {
            get { return _index; }
            set { _index = value; }
        }

        /// <summary>
        /// Gets or sets Cluster index of the cluster that this cost node belongs to.
        /// </summary>
        public int ClusterIndex
        {
            get { return _clustIndex; }
            set { _clustIndex = value; }
        }

        /// <summary>
        /// Gets or sets Total route cost of this cost node.
        /// </summary>
        public float RouteCost
        {
            get { return _routeCost; }
            set { _routeCost = value; }
        }

        /// <summary>
        /// Gets or sets Target cost of this cost node.
        /// </summary>
        public float TargetCost
        {
            get { return _targetCost; }
            set { _targetCost = value; }
        }

        /// <summary>
        /// Gets or sets Wave unit associated with this cost node.
        /// </summary>
        public WaveUnit WaveUnit
        {
            get
            {
                return _waveUnit;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _waveUnit = value;
            }
        }

        /// <summary>
        /// Gets Key of this cost node.
        /// </summary>
        public long Key
        {
            get { return _waveUnit.SampleOffset; }
        }

        #endregion

        #region IComparable<CostNode> Members

        /// <summary>
        /// Compare this cost node with other one.
        /// </summary>
        /// <param name="other">Other cost node to compare with.</param>
        /// <returns>A 32-bit signed integer that indicates the relative order of the objects
        ///     being compared. The return value has the following meanings: Value Meaning
        ///     Less than zero This object is less than the other parameter.Zero This object
        ///     is equal to other. Greater than zero This object is greater than other. 
        /// </returns>
        int IComparable<CostNode>.CompareTo(CostNode other)
        {
            return CompareTo(other);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Convert to string presentation.
        /// </summary>
        /// <returns>String presentation of this instance.</returns>
        public override string ToString()
        {
            string message = string.Format(CultureInfo.InvariantCulture,
                "{0}, Index:{1}, PreIndex:{2}, ClusterIndex:{3}, TargertCost:{4:0.000} RouteCost:{5:0.000}, Other:{6}",
                _waveUnit.Name, _index, _precedeNodeIndex, _clustIndex,
                _targetCost, _routeCost, _waveUnit.ToString());

            return message;
        }

        /// <summary>
        /// Test this cost node is preceding of the given node.
        /// </summary>
        /// <param name="node">Cost node to test with.</param>
        /// <returns>True for yes, otherwise false.</returns>
        public bool IsPreceed(CostNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (node.WaveUnit == null)
            {
                throw new ArgumentNullException("node");
            }

            return this.WaveUnit.SampleOffset + this.WaveUnit.SampleLength
                == node.WaveUnit.SampleOffset;
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Compare this cost node with other one.
        /// </summary>
        /// <param name="other">Other cost node to compare with.</param>
        /// <returns>A 32-bit signed integer that indicates the relative order of the objects
        ///     being compared. The return value has the following meanings: Value Meaning
        ///     Less than zero This object is less than the other parameter.Zero This object
        ///     is equal to other. Greater than zero This object is greater than other. 
        /// </returns>
        protected int CompareTo(CostNode other)
        {
            if (this.RouteCost == other.RouteCost)
            {
                return 0;
            }
            else if (this.RouteCost > other.RouteCost)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        #endregion
    }
}