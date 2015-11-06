//----------------------------------------------------------------------------
// <copyright file="CostNodeClusterGlyph.cs" company="MICROSOFT">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements CostNodeClusterGlyph
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
    using System.Xml;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Viterbi;

    /// <summary>
    /// CostNodeClusterGlyph.
    /// </summary>
    public class CostNodeGroupGlyph : IGlyph
    {
        #region Fields

        private Rectangle _rectangle = new Rectangle(0, 0, 30, 5);
        private Collection<CostNodeGlyph> _costNodes = new Collection<CostNodeGlyph>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets Cost nodes of this group.
        /// </summary>
        public Collection<CostNodeGlyph> CostNodes
        {
            get { return _costNodes; }
        } 

        #endregion

        #region IGlyph Members

        /// <summary>
        /// Gets or sets X position of this instance.
        /// </summary>
        public int X
        {
            get { return this.Rectangle.X; }
            set { this._rectangle.X = value; }
        }

        /// <summary>
        /// Gets or sets Y position of this instance.
        /// </summary>
        public int Y
        {
            get { return this.Rectangle.Y; }
            set { this._rectangle.Y = value; }
        }

        /// <summary>
        /// Gets or sets Size of this instance.
        /// </summary>
        public Size Size
        {
            get { return _rectangle.Size; }
            set { _rectangle.Size = value; }
        }

        /// <summary>
        /// Gets or sets Ractange bound of this instance.
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
            // empty body
        }

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
            g.FillRectangle(Brushes.LightGray, rect);
        }

        #endregion
    }

    /// <summary>
    /// CostNodeClusterGlyph.
    /// </summary>
    public class CostNodeClusterGlyph : IGlyph
    {
        #region Fields

        private Collection<CostNodeGlyph> _costNodeGlyphs =
                                        new Collection<CostNodeGlyph>();

        private Dictionary<long, CostNodeGlyph> _indexedNodeGlyphs =
                                        new Dictionary<long, CostNodeGlyph>();

        private Rectangle _rectangle;
        private Size _size;
        private Size _headerSize = new Size(0, 15);

        private Collection<IGlyph> _glyphs = new Collection<IGlyph>();

        private CostNodeCluster _costNodeCluster;

        private bool _selected;

        /// <summary>
        /// Gets or sets a value indicating whether this glyph is selected.
        /// </summary>
        public bool Selected
        {
            get { return _selected; }
            set { _selected = value; }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Cost node cluster associated with this glyph instance.
        /// </summary>
        public CostNodeCluster CostNodeCluster
        {
            get
            {
                return _costNodeCluster;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _costNodeCluster = value;
            }
        }

        /// <summary>
        /// Gets All glyphs in this cluster.
        /// </summary>
        public Collection<IGlyph> Glyphs
        {
            get { return _glyphs; }
        }

        /// <summary>
        /// Gets Costnode glyph grouped by indexing.
        /// </summary>
        public Dictionary<long, CostNodeGlyph> IndexedNodes
        {
            get { return _indexedNodeGlyphs; }
        }

        /// <summary>
        /// Gets All costnode glyph.
        /// </summary>
        public Collection<CostNodeGlyph> CostNodeGlyphs
        {
            get { return _costNodeGlyphs; }
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
        /// Gets or sets Size of this instance.
        /// </summary>
        public Size Size
        {
            get
            {
                return _size;
            }

            set
            {
                _size = value;
                _headerSize.Width = _size.Width;
            }
        }

        /// <summary>
        /// Gets or sets Ractange bound of this instance.
        /// </summary>
        public Rectangle Rectangle
        {
            get { return _rectangle; }
            set { _rectangle = value; }
        }

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
            DrawBorder(g, rect);
            DrawHeader(g, font);

            foreach (IGlyph glyph in Glyphs)
            {
                glyph.Draw(g, glyph.Rectangle, font, brush, false);
            }
        }

        /// <summary>
        /// Pre-calculate the layout of this instance.
        /// </summary>
        /// <param name="g">Graphics instance.</param>
        /// <param name="font">Font.</param>
        public void PrecalcLayout(Graphics g, Font font)
        {
            foreach (IGlyph glyph in this.Glyphs)
            {
                glyph.PrecalcLayout(g, font);
            }

            Size size = g.MeasureString(CostNodeCluster.TtsUnit.MetaUnit.Name, font).ToSize();

            // add margin
            size.Height += 4;
            size.Width += 4;

            _headerSize = size;

            foreach (IGlyph glyph in this.Glyphs)
            {
                if (glyph.Size.Width > size.Width)
                {
                    size.Width = glyph.Size.Width;
                }

                size.Height += glyph.Size.Height;
            }

            // more margin
            size.Width += 4;
            this.Size = size;
        }

        /// <summary>
        /// Re-calculate the layout of this glyph instance.
        /// </summary>
        public void RecalcLayout()
        {
            Size size = new Size();
            size.Height += _headerSize.Height;

            foreach (IGlyph glyph in this.Glyphs)
            {
                glyph.X = this.Rectangle.X +
                    ((this.Rectangle.Width - glyph.Rectangle.Width) / 2);
                glyph.Y = this.Rectangle.Y + size.Height;

                size.Height += glyph.Size.Height;
            }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Build this cluster's glyph.
        /// </summary>
        /// <param name="shownNodes">Shown node collection.</param>
        /// <param name="folded">Flag indicating whether drawing in folded mode.</param>
        public void BuildGlyphs(Dictionary<string, CostNodeGlyph> shownNodes,
            bool folded)
        {
            if (shownNodes == null)
            {
                throw new ArgumentNullException("shownNodes");
            }

            _glyphs = new Collection<IGlyph>();

            int continuedNotShown = 0;
            foreach (CostNodeGlyph node in _costNodeGlyphs)
            {
                string key = node.CostNode.Key.ToString(CultureInfo.InvariantCulture)
                    + " " + node.CostNode.ClusterIndex.ToString(CultureInfo.InvariantCulture);
                if (shownNodes.ContainsKey(key))
                {
                    if (continuedNotShown > 0 && folded)
                    {
                        int popout = continuedNotShown;
                        CostNodeGroupGlyph group = new CostNodeGroupGlyph();
                        while (popout > 0)
                        {
                            group.CostNodes.Add((CostNodeGlyph)_glyphs[_glyphs.Count - 1]);
                            _glyphs.RemoveAt(_glyphs.Count - 1);
                            popout--;
                        }

                        _glyphs.Add(group);
                    }

                    continuedNotShown = 0;
                }
                else
                {
                    continuedNotShown++;
                }

                _glyphs.Add(node);
            }

            if (continuedNotShown > 0 && folded)
            {
                int popout = continuedNotShown;
                CostNodeGroupGlyph group = new CostNodeGroupGlyph();
                while (popout > 0)
                {
                    group.CostNodes.Add((CostNodeGlyph)_glyphs[_glyphs.Count - 1]);
                    _glyphs.RemoveAt(_glyphs.Count - 1);
                    popout--;
                }

                _glyphs.Add(group);
            }
        }

        /// <summary>
        /// Add a costnode glyph to this cluster.
        /// </summary>
        /// <param name="node">Node to add.</param>
        public void AddNode(CostNodeGlyph node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (node.CostNode == null)
            {
                throw new ArgumentException("node.CostNode is null");
            }

            IndexedNodes.Add(node.CostNode.Key, node);
            CostNodeGlyphs.Add(node);
        }

        #endregion

        #region Internal operations

        /// <summary>
        /// Hit testing on given point for a glyph.
        /// </summary>
        /// <param name="point">Point to test.</param>
        /// <returns>Glyph instance found, null for nothing found.</returns>
        internal IGlyph HitTest(Point point)
        {
            foreach (IGlyph glyph in Glyphs)
            {
                if (glyph.Rectangle.Contains(point))
                {
                    return glyph;
                }
            }

            Rectangle header = new Rectangle(this.X, this.Y, _headerSize.Width, _headerSize.Height);
            if (header.Contains(point))
            {
                return this;
            }

            return null;
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Draw header of cluster glyph.
        /// </summary>
        /// <param name="g">Graphics to draw on.</param>
        /// <param name="font">Font to draw with.</param>
        private void DrawHeader(Graphics g, Font font)
        {
            string label = CostNodeCluster.TtsUnit.MetaUnit.Name;
            SizeF sz = g.MeasureString(label, font);

            Brush brush = Brushes.DarkBlue;
            if (_selected)
            {
                brush = Brushes.Red;
            }

            g.FillRectangle(brush,
                this.X, this.Y, _headerSize.Width, _headerSize.Height);

            g.DrawString(label, font, Brushes.White,
                this.X + ((_headerSize.Width - sz.Width) / 2),
                this.Y + ((_headerSize.Height - sz.Height) / 2));
        }

        /// <summary>
        /// Draw border of the cluster glyph.
        /// </summary>
        /// <param name="g">Graphics to draw on.</param>
        /// <param name="rect">Rect to draw in.</param>
        private void DrawBorder(Graphics g, Rectangle rect)
        {
            Color colorShadow = Color.Gray;
            if (_selected)
            {
                colorShadow = Color.Red;
            }

            using (Pen penBlack = new Pen(Color.Black, 1))
            using (Pen penShadow = new Pen(colorShadow, 2))
            {
                // draw page border and shadow
                g.DrawRectangle(penBlack, rect);
                rect.Inflate(-1, -1);
                g.DrawLine(penShadow, rect.Right + 3, rect.Top + 1,
                    rect.Right + 2, rect.Bottom + 3);
                g.DrawLine(penShadow, rect.Left + 1, rect.Bottom + 3,
                    rect.Right + 2, rect.Bottom + 3);
            }
        }

        #endregion
    }
}