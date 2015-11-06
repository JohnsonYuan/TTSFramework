namespace Microsoft.Tts.UI.Controls.Trajectory
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using Data;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Interaction logic for DurationGraph.xaml.
    /// </summary>
    public partial class DurationGraph : UserControl
    {
        #region dependency properties

        /// <summary>
        /// Top margin.
        /// </summary>
        public static readonly DependencyProperty TopMarginProperty = DependencyProperty.Register(
            "TopMargin", typeof(double), typeof(DurationGraph), new FrameworkPropertyMetadata(0.0,
            FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Bottom margin.
        /// </summary>
        public static readonly DependencyProperty BottomMarginProperty = DependencyProperty.Register(
            "BottomMargin", typeof(double), typeof(DurationGraph), new FrameworkPropertyMetadata(0.0,
            FrameworkPropertyMetadataOptions.AffectsRender));

        #endregion

        private UIElemHandlersStub _uiElemHanldersStub;

        private VisualDurations _durations = null;

        private bool _fDragging = false;

        private double _lineWidth = 1;
        private double _selectedPixelOffset;
        private double _selectedDuration;
        private int _selectedIndex;

        public DurationGraph()
        {
            _uiElemHanldersStub = new UIElemHandlersStub(this);
            InitializeComponent();
        }

        #region properties

        public double TopMargin
        {
            get { return (double)this.GetValue(TopMarginProperty); }
            set { this.SetValue(TopMarginProperty, value); }
        }

        public double BottomMargin
        {
            get { return (double)this.GetValue(BottomMarginProperty); }
            set { this.SetValue(BottomMarginProperty, value); }
        }

        #endregion

        public void SetDataContext(VisualDurations durations)
        {
            _uiElemHanldersStub.InstallUnInstallRenderHandler(durations, _durations);
            DataContext = durations;
            _durations = durations;
            durations.Durations.CollectionChanged += OnDurationsCollectionChanged;
            durations.Durations.TransactionChangeHandler += OnDurationsTransactionChanged;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (_durations != null && _durations.TimeAxis.ZoomMode == ZoomMode.FrameLevel)
            {
                GuidelineSet gs = new GuidelineSet();
                Point start = new Point();
                Point end = new Point();
                start.Y = TopMargin;
                end.Y = ActualHeight - BottomMargin;
                Pen pen = new Pen(Brushes.Blue, _lineWidth);
                pen.DashStyle = DashStyles.Dash;
                double halfPen = pen.Thickness / 2;
                int n = _durations.Durations.Count;
                double offset = -ViewHelper.TimespanToPixel(_durations.TimeAxis.StartingTime, _durations.TimeAxis.ZoomScale);
                for (int i = 0; i < n - 1; i++)
                {
                    offset += ViewHelper.TimespanToPixel(_durations.Durations[i], _durations.TimeAxis.ZoomScale);
                    if (offset > 0 && offset < ActualWidth)
                    {
                        start.X = end.X = offset;
                        gs.GuidelinesX.Clear();
                        gs.GuidelinesX.Add(start.X - halfPen);
                        gs.GuidelinesX.Add(start.X + halfPen);
                        drawingContext.PushGuidelineSet(gs.Clone());
                        drawingContext.DrawLine(pen, start, end);
                        drawingContext.Pop();
                    }
                }
            }
        }

        private void OnDurationsTransactionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_fDragging == false)
            {
                InvalidateVisual();
            }
        }

        private void OnDurationsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_fDragging)
            {
                InvalidateVisual();
            }
        }

        private int GetDurIndex(double timePos)
        {
            int n = 0;
            double time = 0;
            timePos -= ViewHelper.PixelToTimeSpan(_lineWidth, _durations.TimeAxis.ZoomScale);
            if (timePos < 0)
            {
                timePos = 0;
            }

            foreach (double dur in _durations.Durations)
            {
                time += dur;
                if (timePos < time)
                {
                    break;
                }
                else
                {
                    n++;
                }
            }

            return n;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _fDragging = true;
            _durations.Durations.StartTransaction();
            
            // get the selected duration
            Point point = e.GetPosition(this);
            _selectedPixelOffset = point.X;
            double pos = ViewHelper.PixelToTimeSpan(point.X, _durations.TimeAxis.ZoomScale) + _durations.TimeAxis.StartingTime;
            _selectedIndex = GetDurIndex(pos);
            _selectedDuration = _durations.Durations[_selectedIndex];
            CaptureMouse();
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Cursor cur = Cursor;
            Cursor = Cursors.Wait;
            Point point = e.GetPosition(this);
            double align = ViewHelper.TimespanToPixel(_durations.TimeAxis.SampleInterval, _durations.TimeAxis.ZoomScale);
            point.X = (int)(point.X / align) * align;
            MoveToPoint(point);
            _durations.Durations.EndTransaction();
            _fDragging = false;
            Cursor = cur;
            ReleaseMouseCapture();
        }

        private void MoveToPoint(Point point)
        {
            double newDur = ViewHelper.PixelToTimeSpan(point.X - _selectedPixelOffset, _durations.TimeAxis.ZoomScale) + _selectedDuration;
            if (newDur > 0)
            {
                _durations.Durations[_selectedIndex] = newDur;
            }
            else
            {
                _durations.Durations[_selectedIndex] = 0;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_fDragging)
            {
                MoveToPoint(e.GetPosition(this));
            }
        }
    }
}