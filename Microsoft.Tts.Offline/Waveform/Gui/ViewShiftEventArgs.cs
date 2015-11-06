//----------------------------------------------------------------------------
// <copyright file="ViewShiftEventArgs.cs" company="MICROSOFT">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements ViewShiftEventArgs
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
    /// View shift EventArgs.
    /// </summary>
    public class ViewShiftEventArgs : EventArgs
    {
        #region Fields

        private int _sampleShift;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewShiftEventArgs"/> class.
        /// </summary>
        /// <param name="shift">Number of shifted samples.</param>
        public ViewShiftEventArgs(int shift)
        {
            _sampleShift = shift;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets SampleShift.
        /// </summary>
        public int SampleShift
        {
            get { return _sampleShift; }
            set { _sampleShift = value; }
        }

        #endregion
    }
}