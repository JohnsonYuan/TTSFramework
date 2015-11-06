//----------------------------------------------------------------------------
// <copyright file="ViterbiView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements ViterbiView
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Viterbi
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.Threading;
    using System.Windows.Forms;
    using Microsoft.Tts.Offline.Viterbi;

    /// <summary>
    /// ViterbiView.
    /// </summary>
    public partial class ViterbiView : Panel
    {
        #region Fields

        private TtsUtterance _utterance;

        private bool _nodeFolded = true;
        private bool _dataReady;

        private int _shownRouteCount = 1;
        private Dictionary<string, CostNodeGlyph> _shownNodeGlyphs;
        private Collection<CostNodeClusterGlyph> _costNodeClusterGlyphs;
        private Collection<WordGlyph> _wordGlyphs;

        private CostNodeGlyph _selectedNode;
        private CostNodeGlyph _prevMouseOverNode;
        private Mutex _mutex = new Mutex();

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ViterbiView"/> class.
        /// </summary>
        public ViterbiView()
        {
            OnShowNode = delegate
                {
                };

            OnNodeClick = delegate
                {
                };

            OnWordClick = delegate
                {
                };

            OnClusterClick = delegate
                {
                };

            OnNodeContextMenu = delegate
                {
                };

            InitializeComponent();
            this.DoubleBuffered = true;
            this.Controls.Add(_dummyButton);
        }

        #endregion

        #region Events

        /// <summary>
        /// OnShowNode.
        /// </summary>
        public event EventHandler<NodeEventArgs> OnShowNode;

        /// <summary>
        /// OnNodeClick.
        /// </summary>
        public event EventHandler<NodeEventArgs> OnNodeClick;

        /// <summary>
        /// OnWordClick.
        /// </summary>
        public event EventHandler<WordEventArgs> OnWordClick;

        /// <summary>
        /// OnClusterClick.
        /// </summary>
        public event EventHandler<ClusterEventArgs> OnClusterClick;

        /// <summary>
        /// OnNodeContextMenu.
        /// </summary>
        public event EventHandler<NodeEventArgs> OnNodeContextMenu;

        #endregion

        #region Properties

        /// <summary>
        /// Gets All node clusters for each unit's specification.
        /// </summary>
        public Collection<CostNodeClusterGlyph> CostNodeClusterGlyphs
        {
            get { return _costNodeClusterGlyphs; }
        }

        /// <summary>
        /// Gets Visible Shown Node Glyphs.
        /// </summary>
        public Dictionary<string, CostNodeGlyph> ShownNodeGlyphs
        {
            get { return _shownNodeGlyphs; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Specify nodes is folded in view.
        /// </summary>
        public bool NodeFolded
        {
            get { return _nodeFolded; }
            set { _nodeFolded = value; }
        }

        /// <summary>
        /// Gets or sets Selected CostNode glyph.
        /// </summary>
        public CostNodeGlyph SelectedNode
        {
            get
            {
                return _selectedNode;
            }

            set
            {
                SelectNode(value);
            }
        }

        /// <summary>
        /// Gets or sets Utterance associated with this view.
        /// </summary>
        public TtsUtterance Utterance
        {
            get
            {
                return _utterance;
            }

            set
            {
                _selectedNode = null;
                _prevMouseOverNode = null;
                if (value == null)
                {
                    _utterance = null;
                    return;
                }

                // value is not null
                _utterance = value;
            }
        }
        #endregion

        #region Operations

        /// <summary>
        /// Performance select a cluster.
        /// </summary>
        /// <param name="cluster">Cluster to select.</param>
        public void Select(CostNodeClusterGlyph cluster)
        {
            try
            {
                using (Graphics g = Graphics.FromHwnd(this.Handle))
                {
                    g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

                    for (int i = 0; i < CostNodeClusterGlyphs.Count; i++)
                    {
                        CostNodeClusterGlyph cnc = CostNodeClusterGlyphs[i];
                        if (cluster != cnc)
                        {
                            cnc.Selected = false;
                        }
                        else
                        {
                            cnc.Selected = true;
                        }

                        cnc.Draw(g, cnc.Rectangle, this.Font, Brushes.LightSalmon, cnc.Selected);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }

        /// <summary>
        /// Make sure cluster is visible on screen.
        /// </summary>
        /// <param name="cluster">Cluster to performance.</param>
        public void MakeSureVisible(CostNodeCluster cluster)
        {
            if (CostNodeClusterGlyphs == null || CostNodeClusterGlyphs.Count == 0)
            {
                return;
            }

            foreach (CostNodeClusterGlyph cnc in CostNodeClusterGlyphs)
            {
                if (cnc.CostNodeCluster == cluster)
                {
                    if (cnc.Rectangle.Left < this.HorizontalScroll.Value ||
                        cnc.Rectangle.Left > (this.HorizontalScroll.Value + this.Width))
                    {
                        // unvisible now
                        int locate = cnc.Rectangle.Left - (this.Width / 2);
                        locate = Math.Max(locate, 0);
                        if (locate != 0)
                        {
                            locate += cnc.Rectangle.Width;
                        }

                        this.HorizontalScroll.Value = Math.Min(locate, HorizontalScroll.Maximum);
                        this.Refresh();

                        // this.HorizontalScroll.Value = Math.Min(locate, HorizontalScroll.Maximum);
                        // this.Refresh();
                        // Application.DoEvents();
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// Build shown node lattice view.
        /// </summary>
        /// <param name="percent">How many percent of routes would be shown.</param>
        public void BuildShownNodeLattice(float percent)
        {
            if (Utterance == null)
            {
                return;
            }

            if (Utterance.Viterbi == null)
            {
                return;
            }

            BuildShownNodeLattice((int)(Utterance.Viterbi.NodeRoutes.Count * percent));
        }

        /// <summary>
        /// Build shown node lattice view.
        /// </summary>
        /// <param name="shownRouteCount">How many routes would be shown.</param>
        public void BuildShownNodeLattice(int shownRouteCount)
        {
            _dataReady = false;

            if (Utterance == null)
            {
                return;
            }

            if (Utterance.Viterbi == null)
            {
                return;
            }

            if (Utterance.Viterbi.NodeRoutes == null)
            {
                return;
            }

            int routeCount = Utterance.Viterbi.NodeRoutes.Count;
            if (routeCount == 0)
            {
                return;
            }

            // build word glyphs
            _wordGlyphs = new Collection<WordGlyph>();
            foreach (ScriptWord word in Utterance.Script.Words)
            {
                if (word.WordType == WordType.Normal)
                {
                    WordGlyph wordGlyph = new WordGlyph();
                    wordGlyph.Word = word;
                    _wordGlyphs.Add(wordGlyph);
                }
            }

            _shownRouteCount = shownRouteCount;
            _shownRouteCount = Math.Abs(_shownRouteCount)
                < routeCount ? _shownRouteCount : routeCount;
            _shownRouteCount = Math.Abs(_shownRouteCount) < 1 ?
                1 : _shownRouteCount;

            // build shown clusters
            BuildShownClusters();

            // build show nodes
            foreach (NodeRoute route in Utterance.Viterbi.NodeRoutes)
            {
                route.Visible = false;
            }

            // build shown routes
            BuildShownRoutes(routeCount);

            // build cost node glyphs
            foreach (CostNodeClusterGlyph cluster in CostNodeClusterGlyphs)
            {
                cluster.BuildGlyphs(_shownNodeGlyphs, NodeFolded);
            }

            // hood events
            int lastClusterIndex = Utterance.Viterbi.CostNodeClusters.Count - 1;
            CostNodeClusterGlyph lastCluster = CostNodeClusterGlyphs[lastClusterIndex];
            foreach (CostNodeGlyph costNodeGlyph in lastCluster.CostNodeGlyphs)
            {
                costNodeGlyph.OnRequestRate += 
                    new EventHandler<RequestRateEventArgs>(NeedCostRateNode_OnRequestRate);
            }

            if (Utterance.Viterbi != null
                && Utterance.Viterbi.SelectedRoute != null)
            {
                int lastNodeIndex = Utterance.Viterbi.SelectedRoute.CostNodes.Count - 1;
                CostNode lastNode = Utterance.Viterbi.SelectedRoute.CostNodes[lastNodeIndex];
                SelectedNode = CostNodeClusterGlyphs[lastNode.ClusterIndex].CostNodeGlyphs[lastNode.Index];
                OnShowNode(this, new NodeEventArgs(SelectedNode));
            }

            DoResize();
            _dataReady = true;
            Invalidate();
        }

        /// <summary>
        /// Recalcualte layout.
        /// </summary>
        /// <param name="rect">Rectangle to calculate layout in.</param>
        public void RecalcLayout(Rectangle rect)
        {
            if (Utterance != null && CostNodeClusterGlyphs != null)
            {
                int clusterCount = CostNodeClusterGlyphs.Count;
                if (clusterCount == 0)
                {
                    return;
                }

                int wordHeight = 0;
                int step = rect.Width / clusterCount;
                int maxHeight = 0;
                int maxWidth = 0;
                int maxClusterWidth = 0;

                using (Graphics g = Graphics.FromHwnd(this.Handle))
                {
                    for (int i = 0; i < _wordGlyphs.Count; i++)
                    {
                        WordGlyph wordGlyph = _wordGlyphs[i];
                        wordGlyph.PrecalcLayout(g, this.Font);

                        wordHeight = Math.Max(wordHeight, wordGlyph.Rectangle.Height);
                    }

                    for (int i = 0; i < clusterCount; i++)
                    {
                        CostNodeClusterGlyph cluster = CostNodeClusterGlyphs[i];
                        cluster.PrecalcLayout(g, this.Font);
                        maxClusterWidth = Math.Max(maxClusterWidth, cluster.Size.Width);
                    }
                }

                step = (int)Math.Max(maxClusterWidth * 1.2, step);

                for (int i = 0; i < clusterCount; i++)
                {
                    CostNodeClusterGlyph cluster = CostNodeClusterGlyphs[i];

                    int x = (step * i) + (step / 2) - (cluster.Size.Width / 2);
                    Debug.Assert(i == 0 || x - CostNodeClusterGlyphs[i - 1].X < step + 100);
                    int y = 10 + 5 + wordHeight;
                    cluster.Rectangle = new Rectangle(x, y,
                        cluster.Size.Width, cluster.Size.Height);
                    cluster.RecalcLayout();
                    maxHeight = Math.Max(cluster.Rectangle.Height, maxHeight);

                    maxWidth = x + (step / 2) + (cluster.Size.Width / 2);
                }

                for (int i = 0; i < _wordGlyphs.Count; i++)
                {
                    WordGlyph wordGlyph = _wordGlyphs[i];
                    int y = 10;

                    int unitMinIndex = Utterance.Script.Units.IndexOf(wordGlyph.Word.Units[0]);
                    int unitMaxIndex =
                        Utterance.Script.Units.IndexOf(wordGlyph.Word.Units[wordGlyph.Word.Units.Count - 1]);

                    int x = CostNodeClusterGlyphs[unitMinIndex].X;
                    int width = CostNodeClusterGlyphs[unitMaxIndex].Rectangle.Right -
                        CostNodeClusterGlyphs[unitMinIndex].Rectangle.Left;

                    wordGlyph.Size = new Size(width, wordGlyph.Size.Height);
                    wordGlyph.X = x;
                    wordGlyph.Y = y;
                }

                _dummyButton.Location = new Point(maxWidth - this.HorizontalScroll.Value,
                    maxHeight + CostNodeClusterGlyphs[0].Rectangle.Top + 10 - this.VerticalScroll.Value);
            }
        }

        /// <summary>
        /// Find a NodeRoute which is shown and includes the cost node.
        /// </summary>
        /// <param name="costNode">Cost noe to find route on.</param>
        /// <returns>Found node route.</returns>
        public NodeRoute FindRoute(CostNode costNode)
        {
            if (Utterance == null || Utterance.Viterbi == null)
            {
                return null;
            }

            foreach (NodeRoute route in Utterance.Viterbi.NodeRoutes)
            {
                if (route.Visible
                    && route.CostNodes.Contains(costNode))
                {
                    return route;
                }
            }

            return null;
        }

        #endregion

        #region Window events

        /// <summary>
        /// Hanlde OnPaint event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (!_dataReady)
            {
                return;
            }

            if (CostNodeClusterGlyphs == null)
            {
                return;
            }

            if (Utterance == null)
            {
                return;
            }

            if (Utterance.Viterbi == null)
            {
                return;
            }

            int routeCount = Utterance.Viterbi.NodeRoutes.Count;
            if (routeCount == 0)
            {
                return;
            }

            DoubleBuffered = true;
            e.Graphics.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

            for (int i = 0; i < _wordGlyphs.Count; i++)
            {
                _wordGlyphs[i].Draw(e.Graphics, _wordGlyphs[i].Rectangle, this.Font,
                    Brushes.LightSalmon, false);
            }

            foreach (CostNodeClusterGlyph cluster in CostNodeClusterGlyphs)
            {
                cluster.Draw(e.Graphics, cluster.Rectangle, this.Font,
                    Brushes.LightSalmon, false);
            }

            for (int i = 0; i < Math.Abs(_shownRouteCount); i++)
            {
                NodeRoute route = null;
                if (_shownRouteCount > 0)
                {
                    route = Utterance.Viterbi.NodeRoutes[i];
                }
                else
                {
                    route = Utterance.Viterbi.NodeRoutes[routeCount - 1 - i];
                }

                CostNode prevNode = null;
                foreach (CostNode node in route.CostNodes)
                {
                    if (prevNode == null)
                    {
                        prevNode = node;
                        continue;
                    }

                    // TODO
                    // ConnectNodes(e.Graphics, prevNode, node, Pens.LightBlue);
                    prevNode = node;
                }
            }

            if (_prevMouseOverNode != this.SelectedNode)
            {
                HighLightRoutesOnNode(_prevMouseOverNode, e.Graphics);
            }

            HighLightRoutesOnNode(this.SelectedNode, e.Graphics);
        }

        /// <summary>
        /// Hanlde OnMouseMove event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            Point logic = e.Location;
            logic.Offset(this.HorizontalScroll.Value, this.VerticalScroll.Value);
            IGlyph glyph = HitTest(logic);

            if (glyph is WordGlyph)
            {
                return;
            }

            CostNodeGlyph node = glyph as CostNodeGlyph;

            if (_prevMouseOverNode == null)
            {
                if (node != null)
                {
                    _prevMouseOverNode = node;
                    Invalidate();
                    _prevMouseOverNode.DoMouseEnter(this);
                    OnShowNode(this, new NodeEventArgs(node));
                }
            }
            else
            {
                if (node != _prevMouseOverNode)
                {
                    _prevMouseOverNode.DoMouseLeave(this);
                    _prevMouseOverNode = node;
                    Invalidate();
                    if (node != null)
                    {
                        _prevMouseOverNode.DoMouseEnter(this);
                        OnShowNode(this, new NodeEventArgs(node));
                    }
                }
            }

            if (node != null)
            {
                node.DoMouseMove(this, e);
            }
        }

        /// <summary>
        /// Hanlde OnResize event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnResize(EventArgs e)
        {
            if (this.Parent != null)
            {
                DoResize();
            }
        }

        /// <summary>
        /// Hanlde OnScroll event.
        /// </summary>
        /// <param name="se">Event arguments.</param>
        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            this.Invalidate();
        }

        /// <summary>
        /// Hanlde OnMouseClick event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnMouseClick(MouseEventArgs e)
        {
            Point logic = e.Location;
            logic.Offset(this.HorizontalScroll.Value, this.VerticalScroll.Value);
            IGlyph glyph = HitTest(logic);
            CostNodeGlyph costNodeGlyph = glyph as CostNodeGlyph;

            switch (e.Button)
            {
                case MouseButtons.Left:
                    CostNodeClusterGlyph cluster = glyph as CostNodeClusterGlyph;
                    WordGlyph wordGlyph = glyph as WordGlyph;
                    if (cluster != null)
                    {
                        OnClusterClick(this, new ClusterEventArgs(cluster));
                    }
                    else if (wordGlyph != null)
                    {
                        OnWordClick(this, new WordEventArgs(wordGlyph));
                    }
                    else
                    {
                        SelectedNode = costNodeGlyph;
                        OnNodeClick(this, new NodeEventArgs(SelectedNode));
                    }

                    break;
                case MouseButtons.Right:
                    if (costNodeGlyph != null)
                    {
                        OnNodeContextMenu(this, new NodeEventArgs(costNodeGlyph, e));
                    }

                    break;
                default:
                    break;
            }

            Invalidate();
        }

        /// <summary>
        /// Execute resize actions.
        /// </summary>
        private void DoResize()
        {
            _mutex.WaitOne();
            RecalcLayout(this.ClientRectangle);
            _mutex.ReleaseMutex();
            Invalidate();
        }
        #endregion

        #region Private operations

        /// <summary>
        /// Build shown clusters.
        /// </summary>
        private void BuildShownClusters()
        {
            _shownNodeGlyphs = new Dictionary<string, CostNodeGlyph>();

            _costNodeClusterGlyphs = new Collection<CostNodeClusterGlyph>();
            foreach (CostNodeCluster cluster in Utterance.Viterbi.CostNodeClusters)
            {
                CostNodeClusterGlyph clusterGlyph = new CostNodeClusterGlyph();
                clusterGlyph.CostNodeCluster = cluster;

                clusterGlyph.CostNodeGlyphs.Clear();
                foreach (CostNode node in cluster.CostNodes)
                {
                    CostNodeGlyph nodeGlyph = new CostNodeGlyph();
                    nodeGlyph.CostNode = node;
                    clusterGlyph.AddNode(nodeGlyph);
                }

                _costNodeClusterGlyphs.Add(clusterGlyph);
            }
        }

        /// <summary>
        /// Build shown routes.
        /// </summary>
        /// <param name="routeCount">Route count to show.</param>
        private void BuildShownRoutes(int routeCount)
        {
            for (int i = 0; i < Math.Abs(_shownRouteCount); i++)
            {
                NodeRoute route = null;
                if (_shownRouteCount > 0)
                {
                    route = Utterance.Viterbi.NodeRoutes[i];
                }
                else
                {
                    route = Utterance.Viterbi.NodeRoutes[routeCount - 1 - i];
                }

                route.Visible = true;

                foreach (CostNode node in route.CostNodes)
                {
                    CostNodeGlyph glyph = new CostNodeGlyph();
                    glyph.CostNode = node;
                    string key = node.Key.ToString(CultureInfo.InvariantCulture)
                        + " " + node.ClusterIndex.ToString(CultureInfo.InvariantCulture);
                    if (!_shownNodeGlyphs.ContainsKey(key))
                    {
                        _shownNodeGlyphs.Add(key, glyph);
                    }
                }
            }
        }

        /// <summary>
        /// Connect two cost node glyph.
        /// </summary>
        /// <param name="g">Graphics to draw on.</param>
        /// <param name="previous">Previous cost node glyph.</param>
        /// <param name="current">Current cost node glyph.</param>
        /// <param name="pen">Pen to draw with.</param>
        private void ConnectNodes(Graphics g, CostNodeGlyph previous,
            CostNodeGlyph current, Pen pen)
        {
            string prevKey = previous.CostNode.Key.ToString(CultureInfo.InvariantCulture)
                + " " + previous.CostNode.ClusterIndex.ToString(CultureInfo.InvariantCulture);
            string currKey = current.CostNode.Key.ToString(CultureInfo.InvariantCulture)
                + " " + current.CostNode.ClusterIndex.ToString(CultureInfo.InvariantCulture);

            if ((!_shownNodeGlyphs.ContainsKey(prevKey) || !_shownNodeGlyphs.ContainsKey(currKey)) &&
                this.NodeFolded)
            {
                return;
            }

            // page border and shadow
            Point prevPoint = new Point(previous.Rectangle.Right,
                previous.Rectangle.Top + (previous.Rectangle.Height / 2));
            Point currPoint = new Point(current.Rectangle.Left,
                current.Rectangle.Top + (current.Rectangle.Height / 2));
            g.DrawLine(pen, prevPoint, currPoint);
        }

        /// <summary>
        /// Tell which glyph is hit.
        /// </summary>
        /// <param name="point">Point to test.</param>
        /// <returns>Glyph instance.</returns>
        private IGlyph HitTest(Point point)
        {
            if (CostNodeClusterGlyphs == null)
            {
                return null;
            }

            if (Utterance == null)
            {
                return null;
            }

            IGlyph glyph = null;
            foreach (CostNodeClusterGlyph cluster in CostNodeClusterGlyphs)
            {
                glyph = cluster.HitTest(point);

                if (glyph != null)
                {
                    return glyph;
                }
            }

            foreach (WordGlyph wordGlyph in _wordGlyphs)
            {
                if (wordGlyph.Rectangle.Contains(point))
                {
                    return wordGlyph;
                }
            }

            return glyph;
        }

        /// <summary>
        /// De-select a costnode glyph.
        /// </summary>
        /// <param name="node">Node to deselect.</param>
        private void DeselectNode(CostNodeGlyph node)
        {
            if (node != null)
            {
                node.Selected = false;
                if (SelectedNode == node)
                {
                    _selectedNode = null;
                }

                _utterance.Viterbi.SelectedRoute = _utterance.Viterbi.NodeRoutes[0];
            }
        }

        /// <summary>
        /// Select a cost node.
        /// </summary>
        /// <param name="node">Node to select.</param>
        private void SelectNode(CostNodeGlyph node)
        {
            if (node == null)
            {
                DeselectNode(this.SelectedNode);
            }
            else if (node != _selectedNode)
            {
                DeselectNode(_selectedNode);
                _selectedNode = node;
                _selectedNode.Selected = true;
                _utterance.Viterbi.SelectedRoute = FindRoute(node.CostNode);
            }
            else
            {
                _selectedNode.Selected = true;
            }
        }

        /// <summary>
        /// Highlight all the routes including the costnode.
        /// </summary>
        /// <param name="node">Node to highlight routes on it.</param>
        /// <param name="g">Graphics to draw on.</param>
        private void HighLightRoutesOnNode(CostNodeGlyph node, Graphics g)
        {
            if (node == null)
            {
                return;
            }

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int highLighted = 0;
            foreach (NodeRoute route in Utterance.Viterbi.NodeRoutes)
            {
                if (route.CostNodes.Contains(node.CostNode))
                {
                    HighLightRoute(route, g);
                    highLighted += 1;
                }
            }

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
        }

        /// <summary>
        /// Hightlight a route.
        /// </summary>
        /// <param name="route">Route to highlight.</param>
        /// <param name="g">Graphics to draw on.</param>
        private void HighLightRoute(NodeRoute route, Graphics g)
        {
            if (!route.Visible && this.NodeFolded)
            {
                return;
            }

            CostNodeGlyph prevNode = null;
            Brush brush = Brushes.LightSeaGreen;
            foreach (CostNode node in route.CostNodes)
            {
                CostNodeGlyph glyph =
                    CostNodeClusterGlyphs[node.ClusterIndex].CostNodeGlyphs[node.Index];
                if (prevNode == null || !prevNode.IsPreceed(node))
                {
                    if (brush == Brushes.LightSeaGreen)
                    {
                        brush = Brushes.LightGreen;
                    }
                    else
                    {
                        brush = Brushes.LightSeaGreen;
                    }
                }

                glyph.Draw(g, glyph.Rectangle, null, brush, false);
                if (prevNode == null)
                {
                    prevNode = glyph;
                    continue;
                }

                ConnectNodes(g, prevNode, glyph, Pens.Blue);
                prevNode = glyph;
            }
        }

        #endregion

        #region Events handling

        /// <summary>
        /// Hanlde OnRequestRate event of needCostRateNode.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void NeedCostRateNode_OnRequestRate(object sender, RequestRateEventArgs e)
        {
            NodeRoute route = Utterance.Viterbi.FindRoute(e.CostNodeGlyph.CostNode);
            if (route == null)
            {
                e.Rate = 0;
                return;
            }

            e.Rate = Utterance.Viterbi.NodeRoutes.IndexOf(route);
            e.Rate = e.Rate + 1;
        }

        #endregion
    }
}