//----------------------------------------------------------------------------
// <copyright file="RequestRateEventArgs.cs" company="MICROSOFT">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements RequestRateEventArgs
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Viterbi
{
    using System;

    /// <summary>
    /// RequestRateEventArgs.
    /// </summary>
    public class RequestRateEventArgs : EventArgs
    {
        #region Fields, const, member variables, etc.

        private CostNodeGlyph _costNodeGlyph;
        private int _rate;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestRateEventArgs"/> class.
        /// </summary>
        /// <param name="node">Node associated with this arguments.</param>
        /// <param name="rate">Rate.</param>
        public RequestRateEventArgs(CostNodeGlyph node, int rate)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            _costNodeGlyph = node;
            _rate = rate;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Rate.
        /// </summary>
        public int Rate
        {
            get { return _rate; }
            set { _rate = value; }
        }

        /// <summary>
        /// Gets CostNodeGlyph.
        /// </summary>
        public CostNodeGlyph CostNodeGlyph
        {
            get { return _costNodeGlyph; }
        }

        #endregion
    }
}