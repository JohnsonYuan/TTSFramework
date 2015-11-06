//----------------------------------------------------------------------------
// <copyright file="SegmentGraph.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     The code logic of SegmentGraph
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Data;

    /// <summary>
    /// Interaction logic for SegmentGraph.xaml.
    /// </summary>
    public partial class SegmentGraph : UserControl
    {
        #region fields

        /// <summary>
        /// Pen to draw lines.
        /// </summary>
        private static Pen linePen = new Pen(Brushes.Blue, 1.0);

        /// <summary>
        /// The ui element handler stub.
        /// </summary>
        private UIElemHandlersStub _uiElemHanldersStub;

        /// <summary>
        /// The time axis of the graph.
        /// </summary>
        private VisualTimeAxis _timeAxis;

        #endregion

        #region contructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SegmentGraph"/> class.
        /// </summary>
        public SegmentGraph()
        {
            InitializeComponent();
            _uiElemHanldersStub = new UIElemHandlersStub(this);
            Segments = null;
            TimeAxis = new VisualTimeAxis();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets segments.
        /// </summary>
        public Collection<VisualSegment> Segments { get; set; }

        /// <summary>
        /// Gets or sets the time axis.
        /// </summary>
        public VisualTimeAxis TimeAxis
        {
            get 
            {
                return _timeAxis; 
            }

            set
            {
                _uiElemHanldersStub.InstallUnInstallRenderHandler(value, _timeAxis);
                _timeAxis = value;
                InvalidateVisual();
            }
        }

        #endregion

        #region methods

        /// <summary>
        /// Override OnRender method.
        /// </summary>
        /// <param name="drawingContext">Drawing context.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (TimeAxis.ZoomMode == ZoomMode.FrameLevel && Segments != null && Segments.Count > 0)
            {
                GuidelineSet gs = new GuidelineSet();
                double startingX = 0.0;
                int startingIndex = (int)Math.Ceiling(TimeAxis.StartingTime / TimeAxis.SampleInterval);
                int currentSegIndex = -1;
                VisualSegment segment = TrajectoryHelper.FindSegment(Segments, startingIndex,
                    out currentSegIndex);
                if (segment == null)
                {
                    return;
                }

                Debug.Assert(currentSegIndex >= 0, "Can't find the specified segment.");

                double halfLinePen = linePen.Thickness / 2;
                for (int index = startingIndex; currentSegIndex < Segments.Count; ++index)
                {
                    double x = ViewHelper.TimespanToPixel(((index + 1) * TimeAxis.SampleInterval) - TimeAxis.StartingTime,
                        TimeAxis.ZoomScale);
                    segment = Segments[currentSegIndex];

                    // don't draw segment for punctuation, 
                    //   whose start frame index is larger than end frame index.
                    if (segment.StartFrameIndex <= segment.EndFrameIndex)
                    {
                        if (x > ActualWidth)
                        {
                            DrawSegmentText(drawingContext, startingX, Width, segment.Text);
                            break;
                        }

                        if (index == segment.EndFrameIndex)
                        {
                            DrawSegmentText(drawingContext, startingX, x, segment.Text);
                            gs.GuidelinesX.Clear();
                            gs.GuidelinesX.Add(x - halfLinePen);
                            gs.GuidelinesX.Add(x + halfLinePen);
                            drawingContext.PushGuidelineSet(gs.Clone());
                            drawingContext.DrawLine(linePen, new Point(x, 0), new Point(x, Height));
                            drawingContext.Pop();
                            startingX = x;
                            currentSegIndex++;
                        }
                    }
                    else
                    {
                        currentSegIndex++;
                    }
                }
            }
        }

        /// <summary>
        /// Notify render to redraw.
        ///    // TODO PS#77549: I tried to use FrameworkPropertyMetadataOptions.AffectsRender to make
        ///         property changes affects render but it doesn't work here. This is a walk around.
        /// </summary>
        /// <param name="property">Dependency property.</param>
        /// <param name="args">Event args.</param>
        private static void RenderChanged(DependencyObject property, DependencyPropertyChangedEventArgs args)
        {
            SegmentGraph graph = property as SegmentGraph;
            graph.InvalidateVisual();
        }

        /// <summary>
        /// Draw segment text.
        /// </summary>
        /// <param name="drawingContext">Drawing context.</param>
        /// <param name="leftBoundary">Left boundary.</param>
        /// <param name="rightBoundary">Right boundary.</param>
        /// <param name="text">Segment text.</param>
        private void DrawSegmentText(DrawingContext drawingContext, double leftBoundary, 
            double rightBoundary, string text)
        {
            Point middlePoint = new Point((leftBoundary + rightBoundary) / 2, 0);
            FormattedText formattedText = new FormattedText(text, CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, new Typeface("Verdana"), 12, Brushes.Black);
            formattedText.TextAlignment = TextAlignment.Center;

            drawingContext.DrawText(formattedText, middlePoint);
        }

        #endregion
    }
}