//----------------------------------------------------------------------------
// <copyright file="IntervalLinerGraph.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     The code logic of IntervalLinerGraph
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
    using Microsoft.Tts.UI.Controls;
    using Microsoft.Tts.UI.Controls.Acoustic.Data;

    /// <summary>
    /// Interaction logic for IntervalLinerGraph.xaml.
    /// </summary>
    public partial class IntervalLinerGraph : UserControl
    {
        #region dependency properties

        /// <summary>
        /// Curve stroke thickness.
        /// </summary>
        public static readonly DependencyProperty CurveStrokeThicknessProperty = DependencyProperty.Register(
            "CurveStrokeThickness", typeof(double), typeof(IntervalLinerGraph), new FrameworkPropertyMetadata(1.0,
            FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Curve stroke brush.
        /// </summary>
        public static readonly DependencyProperty CurveStrokeProperty = DependencyProperty.Register(
            "CurveStroke", typeof(Brush), typeof(IntervalLinerGraph), new FrameworkPropertyMetadata(new SolidColorBrush(Colors.Black),
            FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Curve stroke brush.
        /// </summary>
        public static readonly DependencyProperty CurveHitStrokeProperty = DependencyProperty.Register(
            "CurveHitStroke", typeof(Brush), typeof(IntervalLinerGraph), new FrameworkPropertyMetadata(new SolidColorBrush(Colors.Yellow),
            FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Upper bound of y-axis value. Sample values larger than it will be dropped.
        /// </summary>
        public static readonly DependencyProperty UpperboundProperty = DependencyProperty.Register(
            "Upperbound", typeof(double), typeof(IntervalLinerGraph), new FrameworkPropertyMetadata(DefaultUpperBound,
            FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Upper bound of y-axis value. Sample values less than it will be dropped.
        /// </summary>
        public static readonly DependencyProperty LowerboundProperty = DependencyProperty.Register(
            "Lowerbound", typeof(double), typeof(IntervalLinerGraph), new FrameworkPropertyMetadata(DefaultLowerBound,
            FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Indicate that if the values can be tuned by UI.
        /// </summary>
        public static readonly DependencyProperty IsEditableProperty = DependencyProperty.Register(
            "IsEditable", typeof(bool), typeof(IntervalLinerGraph), new FrameworkPropertyMetadata(false,
            FrameworkPropertyMetadataOptions.None));

        /// <summary>
        /// If force update visual when samples changed.
        /// </summary>
        public static readonly DependencyProperty IsForceUpdateSamplesProperty = DependencyProperty.Register(
            "IsForceUpdateSamples", typeof(bool), typeof(IntervalLinerGraph), new FrameworkPropertyMetadata(false,
            FrameworkPropertyMetadataOptions.None));

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

        /// <summary>
        /// Capture mouse for canvas or not.
        /// </summary>
        private bool _canvasMouse = false;

        private VisualLinerSamples _samples;

        private int _lastModifiedIndex = -1;

        private UIElemHandlersStub _uiElemHanldersStub;

        private bool _fDragging = false;
        private bool _fIsHit = false;
        private Brush _currBrush = null;

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the IntervalLinerGraph class.
        /// </summary>
        public IntervalLinerGraph()
        {
            InitializeComponent();
            _uiElemHanldersStub = new UIElemHandlersStub(this);
            CanvasMouseCapture = false;
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
        /// Gets or sets brush to draw curve stroke.
        /// </summary>
        public Brush CurveHitStroke
        {
            get { return (Brush)this.GetValue(CurveHitStrokeProperty); }
            set { this.SetValue(CurveHitStrokeProperty, value); }
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
        /// Gets or sets a value indicating whether the values can be tuned by UI.
        /// </summary>
        public bool IsEditable
        {
            get 
            {
                return (bool)this.GetValue(IsEditableProperty); 
            }

            set 
            {
                this.SetValue(IsEditableProperty, value);
                if (value)
                {
                    if (CanvasMouseCapture)
                    {
                        Cursor = Cursors.Pen;
                    }
                    else
                    {
                        Cursor = Cursors.Hand;
                    }
                }
                else
                {
                    Cursor = Cursors.Arrow;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether force update visual when samples changed.
        /// </summary>
        public bool IsForceUpdateSamples
        {
            get { return (bool)this.GetValue(IsForceUpdateSamplesProperty); }
            set { this.SetValue(IsForceUpdateSamplesProperty, value); }
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

        /// <summary>
        /// Gets or sets a value indicating whether Capture mouse for canvas or not.
        /// </summary>
        public bool CanvasMouseCapture
        {
            get
            {
                return _canvasMouse;
            }

            set
            {
                if (value)
                {
                    Cursor = Cursors.Pen;
                }
                else
                {
                    Cursor = Cursors.Wait;
                }

                _rect.IsHitTestVisible = value;
                _canvasMouse = value;
            }
        }

        #endregion

        #region methods

        public void SetDataContext(VisualLinerSamples samples)
        {
            _uiElemHanldersStub.InstallUnInstallRenderHandler(samples, _samples);
            _samples = samples;
            samples.Samples.CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (_fDragging || IsForceUpdateSamples)
                {
                    InvalidateVisual();
                }
            };
            samples.Samples.TransactionChangeHandler += delegate(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (!_fDragging)
                {
                    InvalidateVisual();
                }
            };
            _currBrush = CurveStroke;
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
        /// Create a sample according to specific mouse point.
        /// </summary>
        /// <param name="point">Point.</param>
        /// <param name="sampleIndex">SampleIndex.</param>
        /// <param name="sampleValue">SampleValue.</param>
        public void CreateSample(Point point, out int sampleIndex, out double sampleValue)
        {
            CreateSample(point, CalculateSampleRate(), out sampleIndex, out sampleValue);
        }

        /// <summary>
        /// Judge whether a Y-axis value is out of a valid range.
        /// </summary>
        /// <param name="value">Y-axis value.</param>
        /// <returns>True if it's valid.</returns>
        public bool IsValidSampleValue(double value)
        {
            return value > Lowerbound && value < Upperbound;
        }

        #endregion

        #region methods

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
            point.Y = ((RenderHeightPixel * (_samples.YAxis.ValueRange.Max - sampleValue)) / (_samples.YAxis.RenderRange.Max - _samples.YAxis.RenderRange.Min)) +
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
            if (sampleIndex >= _samples.Samples.Count)
            {
                sampleIndex = _samples.Samples.Count - 1;
            }

            if (sampleValue >= VisualF0.MaxF0Value - 1)
            {
                sampleValue = VisualF0.MaxF0Value - 1;
            }
            else if (sampleValue <= (-1 * VisualF0.MaxF0Value) + 1)
            {
                sampleValue = (float)((-1 * VisualF0.MaxF0Value) + 1);
            }
        }

        /// <summary>
        /// Overrides OnRender and use drawing context to draw trajectories.
        /// </summary>
        /// <param name="drawingContext">Drawing context.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (_samples != null)
            {
                drawingContext.PushGuidelineSet(new GuidelineSet());
                _currBrush = _fIsHit && !CanvasMouseCapture ? CurveHitStroke : CurveStroke;
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
                            RenderLinearInterpolation(drawingContext);
                            drawingContext.Pop();
                        }
                    }
                }

                drawingContext.Pop();
            }
        }

        /// <summary>
        /// Render in linear interpolation.
        /// </summary>
        /// <param name="drawingContext">Drawing context.</param>
        private void RenderLinearInterpolation(DrawingContext drawingContext)
        {
            int sampleRate = CalculateSampleRate();
            Collection<double> sampleValue = Resample(_samples.Samples, sampleRate);

            double sampleInterval = _samples.TimeAxis.SampleInterval * sampleRate;
            Point prevPoint = new Point();
            for (int pointIndex = (int)Math.Ceiling(_samples.TimeAxis.StartingTime / sampleInterval);
                pointIndex < sampleValue.Count; ++pointIndex)
            {
                double curValue = sampleValue[pointIndex];
                if (IsValidSampleValue(curValue))
                {
                    int prevIndex = pointIndex - 1;
                    if (prevIndex >= 0)
                    {
                        double prevValue = sampleValue[prevIndex];
                        if (IsValidSampleValue(prevValue))
                        {
                            prevPoint = CreatePoint(prevIndex, sampleRate, prevValue);
                            if (prevPoint.X > ActualWidth)
                            {
                                break;
                            }

                            if (prevPoint.X >= 0)
                            {
                                Point curPoint = CreatePoint(pointIndex, sampleRate, curValue);
                                drawingContext.DrawLine(new Pen(_currBrush, CurveStrokeThickness), prevPoint, curPoint);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        private void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsEditable && _samples != null && _samples.Samples.Count > 0)
            {
                CaptureMouse();
                _lastModifiedIndex = -1;
                _fDragging = true;
                _samples.Samples.StartTransaction();
            }
        }

        private void OnMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_fDragging)
            {
                Cursor old = Cursor;
                Cursor = Cursors.Wait;
                _samples.Samples.EndTransaction();
                Cursor = old;
                _fDragging = false;
                ReleaseMouseCapture();
            }
        }

        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_fDragging)
            {
                Point mousePoint = e.GetPosition(this);
                int nSampleRate = CalculateSampleRate();
                int nSampleIndex;
                double dSampleValue;
                CreateSample(mousePoint, nSampleRate, out nSampleIndex, out dSampleValue);
                if (nSampleIndex < 0)
                {
                    return;
                }

                // If the sample is not a valid sample, we cannot tune it.
                if (IsValidSampleValue(_samples.Samples[nSampleIndex]))
                {
                    double old = _samples.Samples[nSampleIndex];
                    bool fInvalidSample = false;
                    try
                    {
                        _samples.Samples[nSampleIndex] = dSampleValue;
                    }
                    catch (VisualLinerSamples.InvalidSampleException)
                    {
                        // If the new sample is invalide, we restore old value.
                        _samples.Samples[nSampleIndex] = old;
                        dSampleValue = old;
                        fInvalidSample = true;
                    }

                    if (_lastModifiedIndex != -1)
                    {
                        _samples.Samples.LinerTransform(_lastModifiedIndex, nSampleIndex,
                            delegate(double value)
                            {
                                return !IsValidSampleValue(value);
                            });
                    }

                    if ((dSampleValue > _samples.YAxis.ValueRange.Max ||
                        dSampleValue < _samples.YAxis.ValueRange.Min) && 
                        _lastModifiedIndex != nSampleIndex)
                    {
                        _samples.YAxis.Reset(_samples.Samples);
                    }

                    if (!fInvalidSample)
                    {
                        _lastModifiedIndex = nSampleIndex;
                    }
                }
            }
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            _fIsHit = true;
            InvalidateVisual();
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            _fIsHit = false;
            InvalidateVisual();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (IsEditable)
            {
                IsEditable = IsEditable;
            }
        }
    }
}