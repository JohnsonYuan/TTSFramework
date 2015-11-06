//----------------------------------------------------------------------------
// <copyright file="IMultiFrameControler.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      interface of multiple frame controler.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls
{
    using System.Collections.ObjectModel;

    /// <summary>
    /// Interface of multiple frame controler.
    /// </summary>
    public interface IMultiFrameControler
    {
        /// <summary>
        /// Gets multiple selected frames.
        /// </summary>
        ObservableCollection<int> MultiSelectedFrames { get; }

        /// <summary>
        /// Scroll to frame.
        /// </summary>
        /// <param name="frameIndex">Frame index.</param>
        void ScrollToFrame(int frameIndex);
    }
}