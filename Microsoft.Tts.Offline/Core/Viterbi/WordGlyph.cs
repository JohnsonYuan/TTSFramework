//----------------------------------------------------------------------------
// <copyright file="WordGlyph.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements WordGlyph
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Viterbi
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Drawing;
    using System.IO;
    using System.Text;
    using System.Windows.Forms;

    /// <summary>
    /// Description of WordGlyph class.
    /// </summary>
    public class WordGlyph : IGlyph
    {
        #region Fields, const, member variables, etc.

        private Rectangle _rectangle = new Rectangle(0, 0, 30, 20);

        private ScriptWord _word;

        /// <summary>
        /// Gets or sets Word.
        /// </summary>
        public ScriptWord Word
        {
            get
            {
                return _word;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _word = value;
            }
        }

        #endregion

        #region IGlyph Members

        /// <summary>
        /// Gets or sets X position of this instance.
        /// </summary>
        public int X
        {
            get { return _rectangle.X; }
            set { _rectangle.X = value; }
        }

        /// <summary>
        /// Gets or sets Y position of this instance.
        /// </summary>
        public int Y
        {
            get { return _rectangle.Y; }
            set { _rectangle.Y = value; }
        }

        /// <summary>
        /// Gets or sets Size of this glyph.
        /// </summary>
        public Size Size
        {
            get { return _rectangle.Size; }
            set { _rectangle.Size = value; }
        }

        /// <summary>
        /// Gets or sets Clip of this glyph.
        /// </summary>
        public Rectangle Rectangle
        {
            get { return _rectangle; }
            set { _rectangle = value; }
        }

        /// <summary>
        /// Pre-calculate the layout of this instance.
        /// </summary>
        /// <param name="g">Graphics instance.</param>
        /// <param name="font">Font.</param>
        public void PrecalcLayout(Graphics g, Font font)
        {
            if (g == null)
            {
                throw new ArgumentNullException("g");
            }

            Size size = g.MeasureString(Word.Grapheme, font).ToSize();

            // add margin
            size.Height += 4;
            size.Width += 4;

            this.Size = size;
        }

        /// <summary>
        /// Draw this instance on the Graphics.
        /// </summary>
        /// <param name="g">Graphics to draw with.</param>
        /// <param name="rect">Boundary to draw on.</param>
        /// <param name="font">Font to draw with.</param>
        /// <param name="brush">Brush to draw with.</param>
        /// <param name="selected">Is in selected mode.</param>
        public void Draw(Graphics g, Rectangle rect, Font font, Brush brush, bool selected)
        {
            if (g == null)
            {
                throw new ArgumentNullException("g");
            }

            Color colorShadow = Color.LightGray;
            using (Pen penBlack = new Pen(Color.Black, 1))
            using (Pen penShadow = new Pen(colorShadow, 2))
            {
                // draw page border and shadow
                g.FillRectangle(Brushes.LightGray, rect); 
                g.DrawRectangle(penBlack, rect);
                rect.Inflate(-1, -1);
                g.DrawLine(penShadow, rect.Right + 3, rect.Top + 1,
                    rect.Right + 2, rect.Bottom + 3);
                g.DrawLine(penShadow, rect.Left + 1, rect.Bottom + 3,
                    rect.Right + 2, rect.Bottom + 3);
            }

            string label = Word.Grapheme;
            SizeF sz = g.MeasureString(label, font);

            g.DrawString(label, font, Brushes.DarkBlue,
                this.X + ((Size.Width - sz.Width) / 2),
                this.Y + ((Size.Height - sz.Height) / 2));
        }

        #endregion

        #region Override object methods

        /// <summary>
        /// Convert to string presentation.
        /// </summary>
        /// <returns>String presentation.</returns>
        public override string ToString()
        {
            return base.ToString();
        }

        #endregion
    }

    /// <summary>
    /// Description of NodeEventArgs class.
    /// </summary>
    public class WordEventArgs : EventArgs
    {
        #region Fields, const, member variables, etc.

        private WordGlyph _wordGlyph;
        private MouseEventArgs _mouseEventArgs;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="WordEventArgs"/> class.
        /// </summary>
        /// <param name="word">Word associated with this event arguments.</param>
        public WordEventArgs(WordGlyph word)
        {
            if (word != null)
            {
                _wordGlyph = word;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WordEventArgs"/> class.
        /// </summary>
        /// <param name="word">Word associated with this event arguments.</param>
        /// <param name="e">Mouse event.</param>
        public WordEventArgs(WordGlyph word, MouseEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            if (word != null)
            {
                _wordGlyph = word;
            }

            _mouseEventArgs = e;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets WordGlyph.
        /// </summary>
        public WordGlyph WordGlyph
        {
            get { return _wordGlyph; }
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