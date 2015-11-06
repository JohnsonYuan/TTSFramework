//----------------------------------------------------------------------------
// <copyright file="TrajectoryFrameline.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     The code logic of TrajectoryFrameline
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Data;

    /// <summary>
    /// Interaction logic for TrajectoryFrameline.xaml.
    /// </summary>
    public partial class TrajectoryFrameline : UserControl
    {
        #region fields

        private UIElemHandlersStub _uiElemHanldersStub;
        
        private VisualTimeAxis _timeAxis;

        #endregion

        #region contructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TrajectoryFrameline"/> class.
        /// </summary>
        public TrajectoryFrameline()
        {
            InitializeComponent();
            _uiElemHanldersStub = new UIElemHandlersStub(this);
            TimeAxis = new VisualTimeAxis();
        }

        #endregion

        #region properties

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
        /// OnRender method.
        /// </summary>
        /// <param name="drawingContext">Drawing context.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (TimeAxis.ZoomMode == ZoomMode.FrameLevel)
            {
                GuidelineSet gs = new GuidelineSet();
                Pen pen = new Pen(Brushes.LightGray, 1);
                double halfPen = pen.Thickness / 2;
                
                // draw time line for each frame
                for (int i = 0; i <= ActualWidth / ViewHelper.TimespanToPixel(TimeAxis.SampleInterval, TimeAxis.ZoomScale); ++i)
                {
                    double x = ViewHelper.TimespanToPixel(TimeAxis.SampleInterval * (double)i, TimeAxis.ZoomScale);
                    gs.GuidelinesX.Clear();
                    gs.GuidelinesX.Add(x - halfPen);
                    gs.GuidelinesX.Add(x + halfPen);
                    drawingContext.PushGuidelineSet(gs.Clone());
                    drawingContext.DrawLine(pen, new Point(x, 0), new Point(x, Height));
                    drawingContext.Pop();
                }
            }
        }

        #endregion
    }
}