//----------------------------------------------------------------------------
// <copyright file="IntervalConstantGraph.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     The code logic of IntervalConstantGraph
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using Data;

    /// <summary>
    /// Interaction logic for IntervalConstantGraph.xaml.
    /// </summary>
    public partial class IntervalConstantGraph : UserControl
    {
        #region dependency properties

        /// <summary>
        /// Curve stroke thickness.
        /// </summary>
        public static readonly DependencyProperty CurveStrokeThicknessProperty = DependencyProperty.Register(
            "CurveStrokeThickness", typeof(double), typeof(IntervalConstantGraph), new FrameworkPropertyMetadata(1.0,
            FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Curve stroke brush.
        /// </summary>
        public static readonly DependencyProperty CurveStrokeProperty = DependencyProperty.Register(
            "CurveStroke", typeof(Brush), typeof(IntervalConstantGraph), new FrameworkPropertyMetadata(new SolidColorBrush(Colors.Black),
            FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Deviation fill brush.
        /// </summary>
        public static readonly DependencyProperty DeviationFillProperty =
            DependencyProperty.Register("DeviationFill", typeof(Brush), typeof(IntervalConstantGraph),
            new FrameworkPropertyMetadata(new SolidColorBrush(Colors.LightBlue),
            FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Deviation stroke brush.
        /// </summary>
        public static readonly DependencyProperty DeviationStokeProperty =
            DependencyProperty.Register("DeviationStoke", typeof(Brush), typeof(IntervalConstantGraph),
            new FrameworkPropertyMetadata(new SolidColorBrush(Colors.Blue),
            FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Upper bound of y-axis value. Sample values larger than it will be dropped.
        /// </summary>
        public static readonly DependencyProperty UpperboundProperty = DependencyProperty.Register(
            "Upperbound", typeof(double), typeof(IntervalConstantGraph), new FrameworkPropertyMetadata(DefaultUpperBound,
            FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Upper bound of y-axis value. Sample values less than it will be dropped.
        /// </summary>
        public static readonly DependencyProperty LowerboundProperty = DependencyProperty.Register(
            "Lowerbound", typeof(double), typeof(IntervalConstantGraph), new FrameworkPropertyMetadata(DefaultLowerBound,
            FrameworkPropertyMetadataOptions.AffectsRender));

        #endregion

        #region fields

        /// <summary>
        /// Minimum interval (in pixel) of adjacent points.
        /// Resample according to this value to make sure each pixel
        ///     in horizontal orientation will have one sample point.
        /// </summary>
        public const double MinPointInterval = 1.0;

        /// <summary>
        /// Padding ratio in vertical orientation.
        /// </summary>
        public const double VerticalPaddingPercentage = 0.05;

        /// <summary>
        /// Default upper bound.
        /// </summary>
        public static double DefaultUpperBound = 1e9;

        /// <summary>
        /// Default lower bound.
        /// </summary>
        public static double DefaultLowerBound = -1e9;

        /// <summary>
        /// Horizontal scale.
        /// </summary>
        protected double _horizontalScale = 1.0;

        private VisualConstantSamples _samples;
        private UIElemHandlersStub _uiElemHanldersStub;
        private int _hoverFrame = 0;

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the IntervalConstantGraph class.
        /// </summary>
        public IntervalConstantGraph()
        {
            _uiElemHanldersStub = new UIElemHandlersStub(this);
            InitializeComponent();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets curve stroke thickness.
        /// </summary>
        public double CurveStrokeThickness
        {
            get { return (double)this.GetValue(CurveStrokeThicknessProperty); }
            set { this.SetValue(CurveStrokeThicknessProperty, value); }
        }

        /// <summary>
        /// Gets or sets brush to draw curve stroke.
        /// </summary>
        public Brush CurveStroke
        {
            get { return (Brush)this.GetValue(CurveStrokeProperty); }
            set { this.SetValue(CurveStrokeProperty, value); }
        }

        /// <summary>
        /// Gets or sets brush to draw border of deviations.
        /// </summary>
        public Brush DeviationStroke
        {
            get { return (Brush)this.GetValue(DeviationStokeProperty); }
            set { this.SetValue(DeviationStokeProperty, value); }
        }

        /// <summary>
        /// Gets or sets brush to fill deviations.
        /// </summary>
        public Brush DeviationFill
        {
            get { return (Brush)this.GetValue(DeviationFillProperty); }
            set { this.SetValue(DeviationFillProperty, value); }
        }

        /// <summary>
        /// Gets or sets upper bound of y-axis value.
        ///     Sample values larger than it will be dropped.
        /// </summary>
        public double Upperbound
        {
            get { return (double)this.GetValue(UpperboundProperty); }
            set { this.SetValue(UpperboundProperty, value); }
        }

        /// <summary>
        /// Gets or sets lower bound of y-axis value.
        ///     Sample values less than it will be dropped.
        /// </summary>
        public double Lowerbound
        {
            get { return (double)this.GetValue(LowerboundProperty); }
            set { this.SetValue(LowerboundProperty, value); }
        }

        /// <summary>
        /// Gets the vertical padding pixel.
        /// </summary>
        public double VerticalPaddingPixel
        {
            get
            {
                return ActualHeight * VerticalPaddingPercentage;
            }
        }

        /// <summary>
        /// Gets the render window height pixel.
        /// </summary>
        public double RenderHeightPixel
        {
            get
            {
                return ActualHeight - (2 * VerticalPaddingPixel);
            }
        }

        /// <summary>
        /// Gets the canvas whole height.
        /// </summary>
        public double CanvasHeight
        {
            get
            {
                return RenderHeightPixel * (_samples.YAxis.ValueRange.Max - _samples.YAxis.ValueRange.Min) / (_samples.YAxis.RenderRange.Max - _samples.YAxis.RenderRange.Min);
            }
        }

        /// <summary>
        /// Gets the canvas whole height.
        /// </summary>
        public double CanvasOffset
        {
            get
            {
                return CanvasHeight * (((_samples.YAxis.RenderRange.Max - _samples.YAxis.ValueRange.Min) / (_samples.YAxis.ValueRange.Max - _samples.YAxis.ValueRange.Min)) - 1);
            }
        }

        #endregion

        #region methods

        public void SetDataContext(VisualConstantSamples samples)
        {
            _uiElemHanldersStub.InstallUnInstallRenderHandler(samples, _samples);
            _samples = samples;
            samples.Samples.CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e)
            {
                InvalidateVisual();
            };
            DataContext = samples;
        }

        /// <summary>
        /// Calculate sample rate.
        /// </summary>
        /// <returns>Sample rate.</returns>
        public int CalculateSampleRate()
        {
            double intervalLength = ViewHelper.TimespanToPixel(_samples.TimeAxis.SampleInterval, _horizontalScale);
            return (int)Math.Ceiling(1.0 / intervalLength);
        }

        /// <summary>
        /// Re-sample values.
        /// </summary>
        /// <param name="sampleValues">Sample values.</param>
        /// <param name="sampleRate">Sample rate.</param>
        /// <returns>Re-sampled values.</returns>
        protected Collection<double> Resample(Collection<double> sampleValues, int sampleRate)
        {
            Collection<double> selectedValues = new Collection<double>();
            for (int sampleIndex = 0; sampleIndex < sampleValues.Count / sampleRate; ++sampleIndex)
            {
                selectedValues.Add(sampleValues[sampleRate * sampleIndex]);
            }

            return selectedValues;
        }

        /// <summary>
        /// Create a point according to specific sample value.
        /// </summary>
        /// <param name="sampleIndex">Sample index.</param>
        /// <param name="sampleRate">Sample rate.</param>
        /// <param name="sampleValue">Sample value.</param>
        /// <returns>Generated point.</returns>
        protected Point CreatePoint(int sampleIndex, int sampleRate, double sampleValue)
        {
            Point point = new Point();
            double offset = ((double)sampleIndex * (double)sampleRate * _samples.TimeAxis.SampleInterval) - _samples.TimeAxis.StartingTime;
            point.X = ViewHelper.TimespanToPixel(offset, _horizontalScale);
            point.Y = (RenderHeightPixel * (_samples.YAxis.ValueRange.Max - sampleValue) / (_samples.YAxis.RenderRange.Max - _samples.YAxis.RenderRange.Min)) +
                CanvasOffset + VerticalPaddingPixel;
            return point;
        }

        /// <summary>
        /// Create a sample according to specific mouse point.
        /// </summary>
        /// <param name="point">Point.</param>
        /// <param name="sampleRate">SampleRate.</param>
        /// <param name="sampleIndex">SampleIndex.</param>
        /// <param name="sampleValue">SampleValue.</param>
        protected void CreateSample(Point point, int sampleRate, out int sampleIndex, out double sampleValue)
        {
            double offset = ViewHelper.PixelToTimeSpan(point.X, _horizontalScale);
            sampleIndex = (int)((offset + _samples.TimeAxis.StartingTime) / _samples.TimeAxis.SampleInterval / sampleRate);
            sampleValue = _samples.YAxis.ValueRange.Max - ((point.Y - VerticalPaddingPixel - CanvasOffset) * (_samples.YAxis.RenderRange.Max - _samples.YAxis.RenderRange.Min) / RenderHeightPixel);
        }

        /// <summary>
        /// Judge whether a Y-axis value is out of a valid range.
        /// </summary>
        /// <param name="value">Y-axis value.</param>
        /// <returns>True if it's valid.</returns>
        protected bool IsValidSampleValue(double value)
        {
            return value > Lowerbound && value < Upperbound;
        }

        #endregion

        #region methods

        /// <summary>
        /// Overrides OnRender and use drawing context to draw trajectories.
        /// </summary>
        /// <param name="drawingContext">Drawing context.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (_samples != null)
            {
                Collection<double> sampleValues = _samples.Samples;
                if (sampleValues != null && sampleValues.Count > 0)
                {
                    if (RenderHeightPixel > 0)
                    {
                        _horizontalScale = _samples.TimeAxis.ZoomScale;

                        // Recount horizontal scale when in whole picture zoom mode.
                        if (_samples.TimeAxis.ZoomMode == ZoomMode.WholePicture)
                        {
                            double duration = _samples.TimeAxis.SampleInterval * (double)sampleValues.Count;
                            _horizontalScale = ActualWidth / ViewHelper.TimespanToPixel(duration, 1.0);
                        }

                        // Draw wave
                        if (sampleValues != null)
                        {
                            // Clip the trajectory which is out of graph scope.
                            drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
                            RenderConstantInterpolation(drawingContext);
                            drawingContext.Pop();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Render in constant interpolation.
        /// </summary>
        /// <param name="drawingContext">Drawing context.</param>
        private void RenderConstantInterpolation(DrawingContext drawingContext)
        {
            int sampleRate = CalculateSampleRate();
            Collection<double> sampleValue = Resample(_samples.Samples, sampleRate);
            Collection<double> deviations = null;
            if (_samples.Deviations != null)
            {
                deviations = Resample(_samples.Deviations, sampleRate);
            }

            Pen pen = new Pen(CurveStroke, CurveStrokeThickness);
            Pen rectPen = new Pen(DeviationStroke, CurveStrokeThickness);
            double halfPen = CurveStrokeThickness / 2;
            GuidelineSet gs = new GuidelineSet();
            for (int pointIndex = (int)Math.Ceiling(_samples.TimeAxis.StartingTime / _samples.TimeAxis.SampleInterval);
                pointIndex < sampleValue.Count; ++pointIndex)
            {
                double value = sampleValue[pointIndex];
                if (IsValidSampleValue(value))
                {
                    Point startPoint = CreatePoint(pointIndex, sampleRate, value);
                    Point endPoint = CreatePoint(pointIndex + 1, sampleRate, value);
                    endPoint.Y = startPoint.Y;
                    if (startPoint.X > Width)
                    {
                        break;
                    }

                    if (deviations != null)
                    {
                        value = deviations[pointIndex];
                        if (IsValidSampleValue(value))
                        {
                            Rect rect = new Rect();
                            rect.Width = ViewHelper.TimespanToPixel(_samples.TimeAxis.SampleInterval * (double)sampleRate, _horizontalScale);
                            double deviation = value * RenderHeightPixel / (_samples.YAxis.RenderRange.Max - _samples.YAxis.RenderRange.Min);
                            rect.Height = deviation * 2;
                            rect.X = startPoint.X;
                            rect.Y = startPoint.Y - deviation;
                            gs.GuidelinesX.Clear();
                            gs.GuidelinesX.Add(rect.X - halfPen);
                            gs.GuidelinesX.Add(rect.X + halfPen);
                            gs.GuidelinesX.Add(rect.Right - halfPen);
                            gs.GuidelinesX.Add(rect.Right + halfPen);
                            gs.GuidelinesY.Clear();
                            gs.GuidelinesY.Add(rect.Y - halfPen);
                            gs.GuidelinesY.Add(rect.Y + halfPen);
                            gs.GuidelinesY.Add(rect.Bottom - halfPen);
                            gs.GuidelinesY.Add(rect.Bottom + halfPen);
                            drawingContext.PushGuidelineSet(gs.Clone());
                            if (pointIndex == _hoverFrame)
                            {
                                drawingContext.DrawRectangle(Brushes.Yellow, new Pen(Brushes.Red, CurveStrokeThickness), rect);
                            }
                            else
                            {
                                drawingContext.DrawRectangle(DeviationFill, rectPen, rect);
                            }

                            drawingContext.Pop();
                        }
                    }

                    gs.GuidelinesX.Clear();
                    gs.GuidelinesY.Clear();
                    gs.GuidelinesY.Add(startPoint.Y - halfPen);
                    gs.GuidelinesY.Add(startPoint.Y + halfPen);
                    drawingContext.PushGuidelineSet(gs.Clone());
                    drawingContext.DrawLine(pen, startPoint, endPoint);
                    drawingContext.Pop();
                }
            }
        }

        #endregion
       
        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Point point = e.GetPosition(this);
            _hoverFrame = TrajectoryHelper.GetFrameIndex(point.X, _samples.TimeAxis);
            InvalidateVisual();
        }
    }
}