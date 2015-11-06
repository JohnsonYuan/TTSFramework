//----------------------------------------------------------------------------
// <copyright file="ClusterEventArgs.cs" company="MICROSOFT">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements ClusterEventArgs
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Viterbi
{
    using System;
    using System.Windows.Forms;

    /// <summary>
    /// Description of ClusterEventArgs class.
    /// </summary>
    public class ClusterEventArgs : EventArgs
    {
        #region Fields, const, member variables, etc.

        /// <summary>
        /// Cluster glyph associated with this event argments.
        /// </summary>
        private CostNodeClusterGlyph _costNodeClusterGlyph;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterEventArgs"/> class.
        /// </summary>
        /// <param name="cluster">Cluster glyph associated with this event argments.</param>
        public ClusterEventArgs(CostNodeClusterGlyph cluster)
        {
            if (cluster != null)
            {
                _costNodeClusterGlyph = cluster;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets CodeNodeGlyph.
        /// </summary>
        public CostNodeClusterGlyph CostNodeClusterGlyph
        {
            get { return _costNodeClusterGlyph; }
        }

        #endregion
    }
}