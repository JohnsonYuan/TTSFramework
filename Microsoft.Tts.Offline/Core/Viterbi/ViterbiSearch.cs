//----------------------------------------------------------------------------
// <copyright file="ViterbiSearch.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Viterbi search
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Viterbi
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Text;
    using Microsoft.Tts.Offline.Cart;
    using Microsoft.Tts.Offline.Interop;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Viterbi search algorithm and data.
    /// </summary>
    public class ViterbiSearch
    {
        #region Fields

        private Collection<CostNodeCluster> _costNodeClusters = new Collection<CostNodeCluster>();
        private Collection<NodeRoute> _nodeRoutes = new Collection<NodeRoute>();

        private NodeRoute _selectedRoute;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Current selected route for rendering.
        /// </summary>
        public NodeRoute SelectedRoute
        {
            get
            {
                return _selectedRoute;
            }

            set
            {
                if (value == null)
                {
                    _selectedRoute = null;
                }
                else
                {
                    _selectedRoute = value;
                }
            }
        }

        /// <summary>
        /// Gets All routes for certain unit specification.
        /// </summary>
        public Collection<NodeRoute> NodeRoutes
        {
            get { return _nodeRoutes; }
        }

        /// <summary>
        /// Gets All node clusters for each unit's specification.
        /// </summary>
        public Collection<CostNodeCluster> CostNodeClusters
        {
            get { return _costNodeClusters; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Find route from all routes according to wave units information.
        /// </summary>
        /// <param name="wus">Wave unit collection.</param>
        /// <returns>Found route.</returns>
        public NodeRoute FindRoute(Collection<WaveUnit> wus)
        {
            if (wus == null)
            {
                throw new ArgumentNullException("wus");
            }

            foreach (NodeRoute route in NodeRoutes)
            {
                int nodeIndex = 0;
                bool found = true;
                for (int i = 0; i < wus.Count; i++)
                {
                    WaveUnit currentUnit = wus[i];
                    if (currentUnit == null)
                    {
                        System.Diagnostics.Debug.Assert(false);
                        continue;
                    }

                    if (Phoneme.IsSilencePhone(currentUnit.Name) || Phoneme.IsShortPausePhone(currentUnit.Name))
                    {
                        // skip silence
                        continue;
                    }

                    if (route.CostNodes[nodeIndex].WaveUnit.SampleOffset != currentUnit.SampleOffset
                        || route.CostNodes[nodeIndex].WaveUnit.SampleLength != currentUnit.SampleLength)
                    {
                        found = false;
                        break;
                    }

                    nodeIndex++;
                }

                if (found)
                {
                    return route;
                }
            }

            return null;
        }

        /// <summary>
        /// Find route which contains target node.
        /// </summary>
        /// <param name="targetNode">Node to find.</param>
        /// <returns>NodeRoute.</returns>
        public NodeRoute FindRoute(CostNode targetNode)
        {
            foreach (NodeRoute route in _nodeRoutes)
            {
                foreach (CostNode node in route.CostNodes)
                {
                    if (node == targetNode)
                    {
                        return route;
                    }
                }
            }

            return null;
        }

        #endregion

        #region Search best path

        /// <summary>
        /// Sort all node routes according to cost.
        /// </summary>
        internal void SortNodeRoutes()
        {
            for (int i = 0; i < NodeRoutes.Count; i++)
            {
                for (int j = i; j < NodeRoutes.Count; j++)
                {
                    if (((IComparable<NodeRoute>)NodeRoutes[i]).CompareTo(NodeRoutes[j]) > 0)
                    {
                        NodeRoute routeJ = NodeRoutes[j];
                        NodeRoute routeI = NodeRoutes[i];
                        NodeRoutes[i] = routeJ;
                        NodeRoutes[j] = routeI;
                    }
                }
            }
        }

        #endregion
    }
}