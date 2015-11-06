//----------------------------------------------------------------------------
// <copyright file="CostNodeGlyph.cs" company="MICROSOFT">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements CostNodeGlyph
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Viterbi
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Windows.Forms;
    using Microsoft.Tts.Offline.Viterbi;
    
    /// <summary>
    /// Mouse State.
    /// </summary>
    public enum MouseState
    {
        /// <summary>
        /// Non-defined.
        /// </summary>
        None,

        /// <summary>
        /// Move enter.
        /// </summary>
        Enter
    }

    /// <summary>
    /// CostNode glyph.
    /// </summary>
    public class CostNodeGlyph : IGlyph
    {
        #region Fields

        private CostNode _costNode;

        private bool _selected;
        private Rectangle _rectangle = new Rectangle(0, 0, 30, 20);
        private MouseState _mouseState = MouseState.None;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="CostNodeGlyph"/> class.
        /// </summary>
        public CostNodeGlyph()
        {
            OnMouseMove = delegate
                {
                };

            OnRequestRate = delegate
                {
                };
        }

        #endregion

        #region Events

        /// <summary>
        /// On mouse move event.
        /// </summary>
        public event EventHandler<MouseEventArgs> OnMouseMove;

        /// <summary>
        /// On request rate event.
        /// </summary>
        public event EventHandler<RequestRateEventArgs> OnRequestRate;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether this node is selected or not.
        /// </summary>
        public bool Selected
        {
            get { return _selected; }
            set { _selected = value; }
        }

        /// <summary>
        /// Gets or sets CostNode.
        /// </summary>
        public CostNode CostNode
        {
            get
            {
                return _costNode;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _costNode = value;
            }
        }

        /// <summary>
        /// Gets or sets Mouse state of this node.
        /// </summary>
        public MouseState MouseState
        {
            get { return _mouseState; }
            set { _mouseState = value; }
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
        /// Gets or sets Location x of this glyph.
        /// </summary>
        public int X
        {
            get { return this.Rectangle.X; }
            set { this._rectangle.X = value; }
        }

        /// <summary>
        /// Gets or sets Location y of this glyph.
        /// </summary>
        public int Y
        {
            get { return this.Rectangle.Y; }
            set { this._rectangle.Y = value; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Convert this instance into string presentation.
        /// </summary>
        /// <returns>String presentation.</returns>
        public override string ToString()
        {
            if (_costNode != null)
            {
                return _costNode.ToString();
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region IGlyph Members

        /// <summary>
        /// Draw this instance on the Graphics.
        /// </summary>
        /// <param name="g">Graphics to draw with.</param>
        /// <param name="rect">Boundary to draw on.</param>
        /// <param name="font">Font to draw with.</param>
        /// <param name="brush">Brush to draw with.</param>
        /// <param name="selected">Is in selected mode.</param>
        public void Draw(Graphics g, Rectangle rect, Font font,
            Brush brush, bool selected)
        {
            if (g == null)
            {
                throw new ArgumentNullException("g");
            }

            // draw page border and shadow
            rect.Inflate(-2, -2);
            g.DrawRectangle(Pens.Purple, rect);
            g.FillRectangle(brush, rect);
            if (_mouseState == MouseState.Enter || _selected)
            {
                using (Pen pen = new Pen(Color.DarkBlue, 2))
                {
                    g.DrawRectangle(pen, rect);
                }
            }

            RequestRateEventArgs rateEvent = new RequestRateEventArgs(this, 0);

            OnRequestRate(this, rateEvent);
            if (rateEvent.Rate > 0)
            {
                using (Font fnt = new Font("Microsoft Sans Serif", 7.5F,
                    FontStyle.Bold, GraphicsUnit.Point))
                {
                    string label = rateEvent.Rate.ToString(CultureInfo.InvariantCulture);
                    SizeF sz = g.MeasureString(label, fnt);
                    g.DrawString(label, fnt, Brushes.White,
                        this.X + ((this.Size.Width - sz.Width) / 2),
                        this.Y + ((this.Size.Height - sz.Height) / 2));
                }
            }
        }

        /// <summary>
        /// Pre-calculate the layout of this instance.
        /// </summary>
        /// <param name="g">Graphics instance.</param>
        /// <param name="font">Font.</param>
        public void PrecalcLayout(Graphics g, Font font)
        {
        }

        #endregion

        #region Mouse Event Handler

        /// <summary>
        /// Handle DoMouseMove event.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        internal void DoMouseMove(Microsoft.Tts.Offline.Viterbi.ViterbiView sender,
            MouseEventArgs e)
        {
            OnMouseMove(sender, e);
        }

        /// <summary>
        /// Handle DoMouseEnter event.
        /// </summary>
        /// <param name="viterbiView">Event sender.</param>
        internal void DoMouseEnter(Microsoft.Tts.Offline.Viterbi.ViterbiView viterbiView)
        {
            this.MouseState = MouseState.Enter;
            viterbiView.Invalidate(this.Rectangle);
            viterbiView.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Handle DoMouseLeave event.
        /// </summary>
        /// <param name="viterbiView">Event sender.</param>
        internal void DoMouseLeave(Microsoft.Tts.Offline.Viterbi.ViterbiView viterbiView)
        {
            this.MouseState = MouseState.None;

            viterbiView.Invalidate(this.Rectangle);
            viterbiView.Cursor = Cursors.Default;
        }

        #endregion

        #region Operations

        /// <summary>
        /// Test this cost node is preceding of the given node.
        /// </summary>
        /// <param name="node">Cost node to test with.</param>
        /// <returns>True for yes, otherwise false.</returns>
        internal bool IsPreceed(CostNode node)
        {
            return this.CostNode.WaveUnit.SampleOffset
                + this.CostNode.WaveUnit.SampleLength
                == node.WaveUnit.SampleOffset;
        }

        #endregion
    }
}