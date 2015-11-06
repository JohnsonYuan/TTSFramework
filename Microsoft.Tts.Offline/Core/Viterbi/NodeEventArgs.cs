//----------------------------------------------------------------------------
// <copyright file="NodeEventArgs.cs" company="MICROSOFT">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements NodeEventArgs
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Viterbi
{
    using System;
    using System.Windows.Forms;

    /// <summary>
    /// Description of NodeEventArgs class.
    /// </summary>
    public class NodeEventArgs : EventArgs
    {
        #region Fields, const, member variables, etc.

        private CostNodeGlyph _costNodeGlyph;
        private MouseEventArgs _mouseEventArgs;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeEventArgs"/> class.
        /// </summary>
        /// <param name="node">Cost node to associated with.</param>
        public NodeEventArgs(CostNodeGlyph node)
        {
            if (node != null)
            {
                _costNodeGlyph = node;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeEventArgs"/> class.
        /// </summary>
        /// <param name="node">Cost node to associated with.</param>
        /// <param name="e">Event to associated with.</param>
        public NodeEventArgs(CostNodeGlyph node, MouseEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            if (node != null)
            {
                _costNodeGlyph = node;
            }

            _mouseEventArgs = e;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets CodeNodeGlyph.
        /// </summary>
        public CostNodeGlyph CostNodeGlyph
        {
            get { return _costNodeGlyph; }
        }

        /// <summary>
        /// Gets MouseEventArgs.
        /// </summary>
        public MouseEventArgs MouseEventArgs
        {
            get { return _mouseEventArgs; }
        }

        #endregion
    }
}