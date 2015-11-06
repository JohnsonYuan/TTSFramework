//----------------------------------------------------------------------------
// <copyright file="IGlyph.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements IGlyph
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Viterbi
{
    using System;
    using System.Drawing;

    /// <summary>
    /// IGlyph interface for glyph for Viterbi view.
    /// </summary>
    public interface IGlyph
    {
        /// <summary>
        /// Gets or sets X position of this instance.
        /// </summary>
        int X
        {
            get;
            set;         
        }

        /// <summary>
        /// Gets or sets Y position of this instance.
        /// </summary>
        int Y
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Size of this instance.
        /// </summary>
        Size Size
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Ractange bound of this instance.
        /// </summary>
        Rectangle Rectangle
        {
            get;
            set;
        }

        /// <summary>
        /// Pre-calculate the layout of this instance.
        /// </summary>
        /// <param name="g">Graphics instance.</param>
        /// <param name="font">Font.</param>
        void PrecalcLayout(Graphics g, Font font);

        /// <summary>
        /// Draw this instance on the Graphics.
        /// </summary>
        /// <param name="g">Graphics to draw with.</param>
        /// <param name="rect">Boundary to draw on.</param>
        /// <param name="font">Font to draw with.</param>
        /// <param name="brush">Brush to draw with.</param>
        /// <param name="selected">Is in selected mode.</param>
        void Draw(Graphics g, Rectangle rect, Font font,
            Brush brush, bool selected);
    }
}