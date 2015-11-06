//----------------------------------------------------------------------------
// <copyright file="TrajectoryHighlightFrame.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     The code logic of TrajectoryHighlightFrame
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory
{
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using Data;

    /// <summary>
    /// Interaction logic for TrajectoryHighlightFrame.xaml.
    /// </summary>
    public partial class TrajectoryHighlightFrame : UserControl
    {
        #region dependency properties

        /// <summary>
        /// Deviation fill brush.
        /// </summary>
        public static readonly DependencyProperty HighlightFillProperty =
            DependencyProperty.Register("HighlightFill", typeof(Brush), typeof(TrajectoryHighlightFrame),
            new FrameworkPropertyMetadata(new SolidColorBrush(Colors.Yellow) { Opacity = 0.5 },
            FrameworkPropertyMetadataOptions.AffectsRender));

        #endregion

        #region fields
        
        private UIElemHandlersStub _uiElemHanldersStub;

        private VisualTimeAxis _timeAxis;

        private ObservableCollection<int> _highlightFrames;

        private ObservableCollection<int> _selectedFrames;

        private Collection<int> _firstSelectedFrames;

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TrajectoryHighlightFrame"/> class.
        /// </summary>
        public TrajectoryHighlightFrame()
        {
            _uiElemHanldersStub = new UIElemHandlersStub(this);
            InitializeComponent();
            TimeAxis = new VisualTimeAxis();
            HighlightFrames = new ObservableCollection<int>();
            SelectedFrames = new ObservableCollection<int>();
            _firstSelectedFrames = new Collection<int>();
            IsHoverSelect = false;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets Highlight fill style.
        /// </summary>
        public Brush HighlightFill
        {
            get { return (Brush)this.GetValue(HighlightFillProperty); }
            set { this.SetValue(HighlightFillProperty, value); }
        }

        /// <summary>
        /// Gets or sets The time axis.
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

        /// <summary>
        /// Gets or sets Highlight frames.
        /// </summary>
        public ObservableCollection<int> HighlightFrames
        {
            get
            {
                return _highlightFrames;
            }

            set
            {
                _uiElemHanldersStub.InstallUnInstallRenderHandler((INotifyCollectionChanged)value, (INotifyCollectionChanged)_highlightFrames);
                _highlightFrames = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets Highlight frames.
        /// </summary>
        public ObservableCollection<int> SelectedFrames
        {
            get
            {
                return _selectedFrames;
            }

            set
            {
                _uiElemHanldersStub.InstallUnInstallRenderHandler((INotifyCollectionChanged)value, (INotifyCollectionChanged)_selectedFrames);
                _selectedFrames = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether we do the selection while hover.
        /// </summary>
        public bool IsHoverSelect
        {
            get;
            set;
        }

        #endregion

        #region methods

        /// <summary>
        /// On render.
        /// </summary>
        /// <param name="drawingContext">Drawing context.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (TimeAxis.ZoomMode == ZoomMode.FrameLevel)
            {
                if (HighlightFrames != null)
                {
                    foreach (int highlightIndex in HighlightFrames)
                    {
                        DrawFrame(drawingContext, highlightIndex);
                    }
                }

                if (SelectedFrames != null)
                {
                    foreach (int highlightIndex in SelectedFrames)
                    {
                        DrawFrame(drawingContext, highlightIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Draw the frame.
        /// </summary>
        /// <param name="drawingContext">DrawingContext.</param>
        /// <param name="index">Index.</param>
        protected void DrawFrame(DrawingContext drawingContext, int index)
        {
            double x = ViewHelper.TimespanToPixel((TimeAxis.SampleInterval * (double)index) - TimeAxis.StartingTime, TimeAxis.ZoomScale);
            double width = ViewHelper.TimespanToPixel(TimeAxis.SampleInterval, TimeAxis.ZoomScale);
            width = x + width > ActualWidth ? ActualWidth - x : width;
            double y = 0;
            double height = ActualHeight;
            if (x >= 0 && width > 0)
            {
                drawingContext.DrawRectangle(HighlightFill, null, new Rect(x, y, width, height));
            }
        }

        private void DoSelectionTo(Point point)
        {
            Collection<int> selectedFrames = new Collection<int>();
            TrajectoryHelper.GenerateSelectedFrameIndexes(point.X, TimeAxis, selectedFrames);
            selectedFrames.Add(_firstSelectedFrames);
            int min = int.MaxValue;
            int max = int.MinValue;
            foreach (int n in selectedFrames)
            {
                if (n < min)
                {
                    min = n;
                }

                if (n > max)
                {
                    max = n;
                }
            }

            max++;
            SelectedFrames.Clear();
            while (min < max)
            {
                SelectedFrames.Add(min++);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point mousePoint = e.GetPosition(this);
            TrajectoryHelper.GenerateSelectedFrameIndexes(mousePoint.X, TimeAxis, HighlightFrames);
            if (IsHoverSelect && e.LeftButton == MouseButtonState.Pressed)
            {
                DoSelectionTo(mousePoint);
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            Point mousePoint = e.GetPosition(this);
            if (Keyboard.IsKeyDown(Key.LeftShift) ||
                Keyboard.IsKeyDown(Key.RightShift))
            {
                DoSelectionTo(mousePoint);
            }
            else
            {
                TrajectoryHelper.GenerateSelectedFrameIndexes(mousePoint.X, TimeAxis, _firstSelectedFrames);
                _firstSelectedFrames.DuplicateTo(SelectedFrames);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A &&
                (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                // Select all frames.
                int nFrames = (int)(TimeAxis.Duration / TimeAxis.SampleInterval);
                SelectedFrames.Clear();
                for (int i = 0; i < nFrames; i++)
                {
                    SelectedFrames.Add(i);
                }
            }
        }

        #endregion
    }
}