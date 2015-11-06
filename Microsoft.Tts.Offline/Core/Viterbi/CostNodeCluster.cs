//----------------------------------------------------------------------------
// <copyright file="CostNodeCluster.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements cost node cluster for each column in Viterbi
// search space
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Viterbi
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;

    /// <summary>
    /// Costnode cluster, this is cost node candidates for certain unit specification.
    /// </summary>
    public class CostNodeCluster
    {
        #region Fields

        private Collection<CostNode> _costNodes = new Collection<CostNode>();
        private Dictionary<long, CostNode> _indexedNodes = new Dictionary<long, CostNode>();
        private int _index;

        private TtsUnit _ttsUnit;

        private int _bestNodeIndex;
        private float _concatenateCost;

        /// <summary>
        /// Gets or sets ConcatenateCost.
        /// </summary>
        public float ConcatenateCost
        {
            get { return _concatenateCost; }
            set { _concatenateCost = value; }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets BestNodeIndex.
        /// </summary>
        public int BestNodeIndex
        {
            get { return _bestNodeIndex; }
            set { _bestNodeIndex = value; }
        }

        /// <summary>
        /// Gets or sets Index of this cluster in all clusters in one utterance .
        /// </summary>
        public int Index
        {
            get { return _index; }
            set { _index = value; }
        }

        /// <summary>
        /// Gets or sets Specify which TtsUnit specification this cluster is for.
        /// </summary>
        public TtsUnit TtsUnit
        {
            get
            {
                return _ttsUnit;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _ttsUnit = value;
            }
        }

        /// <summary>
        /// Gets All nodes in this cluster organized through indexing.
        /// </summary>
        public Dictionary<long, CostNode> IndexedNodes
        {
            get { return _indexedNodes; }
        }

        /// <summary>
        /// Gets All nodes in this cluster.
        /// </summary>
        public Collection<CostNode> CostNodes
        {
            get { return _costNodes; }
        }
        #endregion

        #region Public methods

        /// <summary>
        /// Add new node to this cluster.
        /// </summary>
        /// <param name="node">Node to add.</param>
        public void AddNode(CostNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            // node should not be duplicately added
            if (!IndexedNodes.ContainsKey(node.Key))
            {
                IndexedNodes.Add(node.Key, node);
                CostNodes.Add(node);
            }
        }

        #endregion
    }
}