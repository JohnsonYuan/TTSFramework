//----------------------------------------------------------------------------
// <copyright file="ViewUpdateEventArgs.cs" company="MICROSOFT">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements ViewUpdatedEventArgs
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Data is in sample.
    /// </summary>
    public class ViewUpdateEventArgs : EventArgs
    {
        #region Fields

        private int _length;
        private int _viewOffset;
        private int _viewLength;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewUpdateEventArgs"/> class.
        /// </summary>
        /// <param name="length">Length.</param>
        /// <param name="viewOffset">View offset.</param>
        /// <param name="viewLength">View length.</param>
        public ViewUpdateEventArgs(int length, int viewOffset, int viewLength)
        {
            _length = length;
            _viewOffset = viewOffset;
            _viewLength = viewLength;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Length.
        /// </summary>
        public int Length
        {
            get { return _length; }
            set { _length = value; }
        }

        /// <summary>
        /// Gets or sets ViewLength.
        /// </summary>
        public int ViewLength
        {
            get { return _viewLength; }
            set { _viewLength = value; }
        }

        /// <summary>
        /// Gets or sets Offset.
        /// </summary>
        public int ViewOffset
        {
            get { return _viewOffset; }
            set { _viewOffset = value; }
        }

        #endregion
    }
}