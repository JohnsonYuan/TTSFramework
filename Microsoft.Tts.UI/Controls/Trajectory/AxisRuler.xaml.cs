//----------------------------------------------------------------------------
// <copyright file="AxisRuler.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     The code logic of AxisRuler
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Data;

    /// <summary>
    /// Interaction logic for AxisRuler.xaml.
    /// </summary>
    public partial class AxisRuler : UserControl
    {
        #region dependency property

        /// <summary>
        /// Min value of the graduation.
        /// </summary>
        public static readonly DependencyProperty MinProperty =
            DependencyProperty.Register("Min", typeof(double), typeof(AxisRuler),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Max value of the graduation.
        /// </summary>
        public static readonly DependencyProperty MaxProperty =
            DependencyProperty.Register("Max", typeof(double), typeof(AxisRuler),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Minor value of the graduation.
        /// </summary>
        public static readonly DependencyProperty MinorTickProperty =
            DependencyProperty.Register("MinorTick", typeof(double), typeof(AxisRuler),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Major step of the graduation.
        /// </summary>
        public static readonly DependencyProperty MajorStepProperty =
            DependencyProperty.Register("MajorStep", typeof(double), typeof(AxisRuler),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Whether the axis ruler is in the left.
        /// </summary>
        public static readonly DependencyProperty IsRulerLeftProperty =
            DependencyProperty.Register("IsRulerLeft", typeof(bool), typeof(AxisRuler),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Whether the axis ruler's width and height are limited by value range.
        /// </summary>
        public static readonly DependencyProperty IsBoxToValueProperty =
            DependencyProperty.Register("IsBoxToValue", typeof(bool), typeof(AxisRuler),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Ruler original offset.
        /// </summary>
        public static readonly DependencyProperty OriginOffsetProperty =
            DependencyProperty.Register("OriginOffset", typeof(double), typeof(AxisRuler),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Ruler height pixel.
        /// </summary>
        public static readonly DependencyProperty RulerHeightProperty =
            DependencyProperty.Register("RulerHeight", typeof(double), typeof(AxisRuler),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        #endregion

        #region fields

        /// <summary>
        /// Max ruler length.
        /// </summary>
        private const int MaxRulerLength = int.MaxValue;

        /// <summary>
        /// Ruler scale factor.
        /// </summary>
        private const double RulerScaleFactor = 0.2;

        /// <summary>
        /// Min render minor graduation.
        /// </summary>
        private const double MinRenderMinorTick = 0.00005;

        /// <summary>
        /// Ruler label font family name.
        /// </summary>
        private const string FontFamilyName = "Verdana";

        /// <summary>
        /// Width ratio of graduation to ruler.
        /// </summary>
        private const double MajorGraduationLength = 7;

        /// <summary>
        /// Width ratio of graduation to ruler.
        /// </summary>
        private const double MinorTickLength = 5;

        /// <summary>
        /// Width padding of selection area.
        /// </summary>
        private const double SelectionAreaWidthPadding = 2;

        /// <summary>
        /// Width padding of selection area.
        /// </summary>
        private const double RulerMargin = 5;

        /// <summary>
        /// Stroke pen of selection area.
        /// </summary>
        private Pen selectionAreaStroke = new Pen(Brushes.Blue, 1.0);

        /// <summary>
        /// Fill brush of selection area.
        /// </summary>
        private Brush selectionAreaFill = new SolidColorBrush(Colors.Blue)
        {
            Opacity = 0.1
        };

        /// <summary>
        /// Stroke pen of ruler.
        /// </summary>
        private Pen rulerStroke = new Pen(Brushes.Black, 1.0);

        /// <summary>
        /// Fill brush of ruler.
        /// </summary>
        private Brush rulerFill = Brushes.Transparent;

        /// <summary>
        /// Start selected value of the graduation.
        /// </summary>
        private double _startSelectionGradValue = double.NaN;

        /// <summary>
        /// Last mouse location.
        /// </summary>
        private Point _lastMouseLocation = new Point();

        /// <summary>
        /// Render minor graduation.
        /// </summary>
        private double _renderMinorTick = 1;

        #endregion

        #region contructor

        /// <summary>
        /// Initializes a new instance of the AxisRuler class.
        /// </summary>
        public AxisRuler()
        {
            InitializeComponent();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets min of graduation range.
        /// </summary>
        public double Min
        {
            get { return (double)this.GetValue(MinProperty); }
            set { this.SetValue(MinProperty, value); }
        }

        /// <summary>
        /// Gets or sets max of graduation range.
        /// </summary>
        public double Max
        {
            get { return (double)this.GetValue(MaxProperty); }
            set { this.SetValue(MaxProperty, value); }
        }

        /// <summary>
        /// Gets or sets minor tick.
        /// </summary>
        public double MinorTick
        {
            get { return (double)this.GetValue(MinorTickProperty); }
            set { this.SetValue(MinorTickProperty, value); }
        }

        /// <summary>
        /// Gets or sets graduation range.
        /// </summary>
        public double MajorStep
        {
            get { return (double)this.GetValue(MajorStepProperty); }
            set { this.SetValue(MajorStepProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the ruler is left.
        /// </summary>
        public bool IsRulerLeft
        {
            get { return (bool)this.GetValue(IsRulerLeftProperty); }
            set { this.SetValue(IsRulerLeftProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the axis ruler's width and height are limited by value range.
        /// </summary>
        public bool IsBoxToValue
        {
            get { return (bool)this.GetValue(IsBoxToValueProperty); }
            set { this.SetValue(IsBoxToValueProperty, value); }
        }

        /// <summary>
        /// Gets or sets graduation range.
        /// </summary>
        public double OriginOffset
        {
            get { return (double)this.GetValue(OriginOffsetProperty); }
            set { this.SetValue(OriginOffsetProperty, value); }
        }

        /// <summary>
        /// Gets or sets graduation range.
        /// </summary>
        public double RulerHeight
        {
            get { return (double)this.GetValue(RulerHeightProperty); }
            set { this.SetValue(RulerHeightProperty, value); }
        }

        /// <summary>
        /// Gets the label margin.
        /// </summary>
        private double LabelMargin
        {
            get
            {
                return MajorGraduationLength + 2;
            }
        }

        /// <summary>
        /// Gets the width of ruler.
        /// </summary>
        private double RulerWidth
        {
            get
            {
                return ActualWidth;
            }
        }

        /// <summary>
        /// Gets the graduation count.
        /// </summary>
        private int GraduationCount
        {
            get
            {
                int count = 0;
                if (_renderMinorTick != 0)
                {
                    count = checked((int)Math.Floor(GraduationRange / _renderMinorTick));
                }

                return count;
            }
        }

        /// <summary>
        /// Gets the height of each graduation.
        /// </summary>
        private double GraduationHeight
        {
            get
            {
                return RulerHeight / (double)GraduationCount;
            }
        }

        /// <summary>
        /// Gets the graduation range.
        /// </summary>
        private double GraduationRange
        {
            get
            {
                Debug.Assert(Max - Min >= 0, "Max should not be smaller than Min!");
                return Max - Min;
            }
        }

        /// <summary>
        /// Gets the clip rectangle.
        /// </summary>
        private RectangleGeometry ClipRect
        {
            get
            {
                return new RectangleGeometry(new Rect(-SelectionAreaWidthPadding, 0,
                    ActualWidth + (2 * SelectionAreaWidthPadding), ActualHeight));
            }
        }

        #endregion

        #region methods

        /// <summary>
        /// Set data context of the axis ruler.
        /// </summary>
        /// <param name="visualValueAxis">Axis ruler view data.</param>
        public void SetDataContext(VisualValueAxis visualValueAxis)
        {
            Debug.Assert(visualValueAxis != null, "Can't set a null data context to AxisRuler.");
            DataContext = visualValueAxis;
            Binding binding = new Binding("ValueRange.Min");
            binding.Source = visualValueAxis;
            binding.Mode = BindingMode.OneWay;
            SetBinding(AxisRuler.MinProperty, binding);

            binding = new Binding("ValueRange.Max");
            binding.Source = visualValueAxis;
            binding.Mode = BindingMode.OneWay;
            SetBinding(AxisRuler.MaxProperty, binding);

            binding = new Binding("MinorTick");
            binding.Source = visualValueAxis;
            binding.Mode = BindingMode.OneWay;
            SetBinding(AxisRuler.MinorTickProperty, binding);

            binding = new Binding("MajorStep");
            binding.Source = visualValueAxis;
            binding.Mode = BindingMode.OneWay;
            SetBinding(AxisRuler.MajorStepProperty, binding);
        }

        /// <summary>
        /// Reset ruler data.
        /// </summary>
        /// <param name="graduationMin">Graduation min value.</param>
        /// <param name="graduationMax">Graduation max value.</param>
        public void Reset(double graduationMin, double graduationMax)
        {
            double gradMax = AbsoluteLogicalPixelToGrad(RulerHeight + OriginOffset);
            double gradMin = AbsoluteLogicalPixelToGrad(RulerHeight + OriginOffset - ActualHeight);
            VisualValueAxis visualValueAxis = (VisualValueAxis)DataContext;
            visualValueAxis.ValueRange = new ValueAxisRange(gradMin, gradMax);
        }

        /// <summary>
        /// Override OnRenderSizeChanged methods to adjust ruler render size.
        /// </summary>
        /// <param name="sizeInfo">Size changed information.</param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            if (RulerHeight == 0)
            {
                RulerHeight = ActualHeight;
            }

            VisualValueAxis visualValueAxis = DataContext as VisualValueAxis;
            if (visualValueAxis != null)
            {
                ScaleToRenderRange(visualValueAxis.RenderRange.Min,
                    visualValueAxis.RenderRange.Max, 0, true);
            }
        }

        /// <summary>
        /// Overrides OnRender method to draw ruler.
        /// </summary>
        /// <param name="drawingContext">Drawing context.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (RulerHeight == 0 || ActualHeight < 1 || DataContext == null)
            {
                return;
            }

            drawingContext.PushClip(ClipRect);
            _rulerArea.Margin = new Thickness(0, 0, 0, 0);
            _renderMinorTick = MinorTick / ViewHelper.AdjustFactor(RulerHeight / ActualHeight);

            GuidelineSet gs = new GuidelineSet();
            double halfRulerPen = rulerStroke.Thickness / 2;
            double xOffset = IsRulerLeft ? RulerWidth : 0;
            gs.GuidelinesX.Add(xOffset - halfRulerPen);
            gs.GuidelinesX.Add(xOffset + halfRulerPen);
            drawingContext.PushGuidelineSet(gs.Clone());
            drawingContext.DrawLine(rulerStroke,
                new Point(xOffset, OriginOffset),
                new Point(xOffset, RulerHeight + OriginOffset));
            drawingContext.Pop();
            if (GraduationCount > 0)
            {
                int startGradIndex = checked((int)Math.Floor((RulerHeight + OriginOffset - ActualHeight) / GraduationHeight));
                int stopGradIndex = checked((int)Math.Floor((RulerHeight + OriginOffset) / GraduationHeight));

                // Draw scale and label.
                for (int gradIndex = startGradIndex; gradIndex <= stopGradIndex; ++gradIndex)
                {
                    double gradY = (GraduationHeight * gradIndex) - OriginOffset;
                    double gradValue = Min + (gradIndex * _renderMinorTick);
                    double graduationLength = MinorTickLength;

                    if ((GraduationCount - gradIndex) % MajorStep == 0)
                    {
                        graduationLength = MajorGraduationLength;
                        FormattedText text = new FormattedText(gradValue.ToString(),
                            CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                            new Typeface(FontFamilyName), 8, Brushes.Black);
                        Point lableLocation = new Point(LabelMargin,
                            RulerHeight - (gradY + (text.Height / 2)));
                        if (gradIndex == GraduationCount)
                        {
                            lableLocation.Y = OriginOffset;
                        }
                        else if (gradIndex == 0)
                        {
                            lableLocation.Y = OriginOffset + RulerHeight - text.Height - 1;
                        }

                        if (IsRulerLeft)
                        {
                            lableLocation.X = RulerWidth - text.Width - LabelMargin;
                        }

                        drawingContext.DrawText(text, lableLocation);
                    }

                    double lineHeight = RulerHeight - gradY;
                    gs.GuidelinesX.Clear();
                    gs.GuidelinesY.Clear();
                    gs.GuidelinesY.Add(lineHeight - halfRulerPen);
                    gs.GuidelinesY.Add(lineHeight + halfRulerPen);
                    drawingContext.PushGuidelineSet(gs.Clone());
                    if (IsRulerLeft)
                    {
                        drawingContext.DrawLine(rulerStroke,
                            new Point(ActualWidth, lineHeight),
                           new Point(RulerWidth - graduationLength, lineHeight));
                    }
                    else
                    {
                        drawingContext.DrawLine(rulerStroke, new Point(0, lineHeight),
                           new Point(graduationLength, lineHeight));
                    }

                    drawingContext.Pop();
                }
            }

            drawingContext.Pop();
        }

        #endregion

        #region UI events

        /// <summary>
        /// Event handler when mouse leaves ruler area.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse event arguments.</param>
        private void OnRulerAreaMouseLeave(object sender, MouseEventArgs e)
        {
            if (GraduationCount > 0)
            {
                if (e.RightButton == MouseButtonState.Pressed)
                {
                    EndSelection(e);
                }
            }
        }

        /// <summary>
        /// Event handler when mouse moves inside ruler area.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse event argument.</param>
        private void OnRulerAreaMouseMove(object sender, MouseEventArgs e)
        {
            if (GraduationCount > 0)
            {
                // Close pop-up to renew the pop-up position.
                Point mousePos = e.GetPosition(this as IInputElement);
                double minorTickValue = AbsoluteLogicalPixelToMinorTick(MouseOffsetYToLogicalY(mousePos.Y));

                if (e.RightButton == MouseButtonState.Pressed &&
                    !double.IsNaN(_startSelectionGradValue))
                {
                    _currentSelectionCanvas.Children.Clear();
                    _currentSelectionCanvas.Children.Add(CreateSelectionAreaFromMinorGrand(
                        _startSelectionGradValue, minorTickValue));
                }
                else if (e.LeftButton == MouseButtonState.Pressed)
                {
                    double deltaPixel = _lastMouseLocation.Y - mousePos.Y;

                    if (IsBoxToValue)
                    {
                        if (deltaPixel > 0 &&
                            OriginOffset + RulerHeight - deltaPixel < ActualHeight)
                        {
                            OriginOffset = ActualHeight - RulerHeight;
                        }
                        else if (deltaPixel < 0 &&
                            OriginOffset - deltaPixel > 0)
                        {
                            OriginOffset = 0;
                        }
                        else
                        {
                            OriginOffset -= deltaPixel;
                        }
                    }
                    else
                    {
                        OriginOffset -= deltaPixel;
                    }

                    _lastMouseLocation = mousePos;
                    NotifyRenderRangeChanged();
                }
            }
        }

        /// <summary>
        /// Event handler when mouse left button down inside ruler area.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse button event argument.</param>
        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (GraduationCount > 0)
            {
                Point mousePos = e.GetPosition(this as IInputElement);
                StartSelection(MouseOffsetYToLogicalY(mousePos.Y));
            }
        }

        /// <summary>
        /// Event handler when mouse left button up inside ruler area.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse button event argument.</param>
        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (GraduationCount > 0)
            {
                EndSelection(e);
            }
        }

        /// <summary>
        /// Event handler when mouse left button down inside ruler area.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse button event argument.</param>
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _lastMouseLocation = e.GetPosition(this as IInputElement);
        }

        /// <summary>
        /// Event handler when mouse wheeling.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse button event argument.</param>
        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0 && !IsValidRulerLength(RulerHeight * (1 + RulerScaleFactor)))
            {
                return;
            }

            Point position = e.GetPosition(this as IInputElement);
            double gradValue = AbsoluteLogicalPixelToGrad(MouseOffsetYToLogicalY(position.Y));

            double rulerHeight = RulerHeight;
            double originYOffset = OriginOffset;
            if (e.Delta > 0)
            {
                rulerHeight = RulerHeight * (1 + RulerScaleFactor);
            }
            else
            {
                rulerHeight = RulerHeight * (1 - RulerScaleFactor);
            }

            originYOffset = position.Y - (rulerHeight -
                (((gradValue - Min) * rulerHeight) / GraduationRange));

            if (IsBoxToValue)
            {
                if (originYOffset + rulerHeight < ActualHeight)
                {
                    originYOffset = ActualHeight - rulerHeight;
                }

                if (rulerHeight <= ActualHeight)
                {
                    rulerHeight = ActualHeight;
                    originYOffset = 0;
                }

                if (originYOffset > 0)
                {
                    originYOffset = 0;
                }
            }

            OriginOffset = originYOffset;
            RulerHeight = rulerHeight;
            NotifyRenderRangeChanged();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Scale the ruler to match with the render range.
        /// </summary>
        /// <param name="startGradValue">Start graduation value.</param>
        /// <param name="stopGradValue">Stop graduation value.</param>
        /// <param name="margin">Ruler margin.</param>
        /// <param name="notify">Whether notify ruler render changes.</param>
        private void ScaleToRenderRange(double startGradValue, double stopGradValue,
            double margin, bool notify)
        {
            double windowHeight = ActualHeight - (margin * 2);
            double rulerHeight = GraduationRange * windowHeight / (stopGradValue - startGradValue);
            if (rulerHeight < ActualHeight)
            {
                rulerHeight = ActualHeight;
            }

            double originYOffset = margin - (((Max - stopGradValue) * rulerHeight) / GraduationRange);

            if (IsBoxToValue)
            {
                if (originYOffset > 0)
                {
                    originYOffset = 0;
                }
                else if (originYOffset + rulerHeight < ActualHeight)
                {
                    originYOffset = ActualHeight - rulerHeight;
                }
            }

            if (IsValidRulerLength(rulerHeight))
            {
                RulerHeight = rulerHeight;
                OriginOffset = originYOffset;
                if (notify)
                {
                    NotifyRenderRangeChanged();
                }
            }
        }

        /// <summary>
        /// Scale ruler to match with render range after selection.
        /// </summary>
        /// <param name="e">Mouse event arguments.</param>
        private void EndSelection(MouseEventArgs e)
        {
            List<double> selectedGraduations = new List<double>();

            Point mousePos = e.GetPosition(this as IInputElement);
            if (_currentSelectionCanvas.Children.Count != 0)
            {
                _currentSelectionCanvas.Children.Clear();
                double endSelectionGradValue = AbsoluteLogicalPixelToMinorTick(MouseOffsetYToLogicalY(mousePos.Y));
                double startGradValue = Math.Floor(Math.Min(endSelectionGradValue, _startSelectionGradValue) /
                    _renderMinorTick) * _renderMinorTick;
                double stopGradValue = (Math.Floor(Math.Max(endSelectionGradValue, _startSelectionGradValue) /
                    _renderMinorTick) + 1) * _renderMinorTick;
                ScaleToRenderRange(startGradValue, stopGradValue, RulerMargin, true);
            }
        }

        /// <summary>
        /// Check if the ruler length is valid.
        /// </summary>
        /// <param name="rulerLength">Ruler length.</param>
        /// <returns>Whether the ruler length is valid.</returns>
        private bool IsValidRulerLength(double rulerLength)
        {
            double renderMinorTick = MinorTick / ViewHelper.AdjustFactor(rulerLength / ActualHeight);
            return rulerLength < MaxRulerLength && renderMinorTick > MinRenderMinorTick;
        }

        /// <summary>
        /// Convert mouse offset Y-axis to logical Y offset.
        /// </summary>
        /// <param name="mouseOffsetY">Mouse offset Y.</param>
        /// <returns>Logical offset Y.</returns>
        private double MouseOffsetYToLogicalY(double mouseOffsetY)
        {
            return RulerHeight - (mouseOffsetY - OriginOffset);
        }

        /// <summary>
        /// Convert a height to a graduation index.
        /// </summary>
        /// <param name="pixel">Vertical position of the pixel.</param>
        /// <returns>Graduation index.</returns>
        private double AbsoluteLogicalPixelToMinorTick(double pixel)
        {
            double gradValue = AbsoluteLogicalPixelToGrad(pixel);
            return Math.Floor(gradValue / _renderMinorTick) * _renderMinorTick;
        }

        /// <summary>
        /// Convert logical pixel to graduation.
        /// </summary>
        /// <param name="pixel">Logical pixel.</param>
        /// <returns>Graduation value.</returns>
        private double AbsoluteLogicalPixelToGrad(double pixel)
        {
            return (GraduationRange * pixel / RulerHeight) + Min;
        }

        /// <summary>
        /// Convert a height to a graduation index.
        /// </summary>
        /// <param name="graduation">Vertical graduation.</param>
        /// <returns>Graduation index.</returns>
        private double GradToLocationY(double graduation)
        {
            return RulerHeight - AbsoluteGradToPixel(graduation) + OriginOffset;
        }

        /// <summary>
        /// Convert a height to a graduation index.
        /// </summary>
        /// <param name="graduation">Absolute graduation to be converted.</param>
        /// <returns>Pixel length of the graduation.</returns>
        private double AbsoluteGradToPixel(double graduation)
        {
            return ((graduation - Min) * RulerHeight) / GraduationRange;
        }

        /// <summary>
        /// Convert a height to a graduation index.
        /// </summary>
        /// <param name="graduation">Graduation length.</param>
        /// <returns>Pixel length of the graduation.</returns>
        private double GradToPixel(double graduation)
        {
            return (graduation * RulerHeight) / GraduationRange;
        }

        /// <summary>
        /// Start selection.
        /// </summary>
        /// <param name="logicalY">Logical offset Y.</param>
        private void StartSelection(double logicalY)
        {
            double gradValue = AbsoluteLogicalPixelToMinorTick(logicalY);
            _currentSelectionCanvas.Children.Add(CreateSelectionArea(gradValue,
                gradValue + _renderMinorTick));
            _startSelectionGradValue = gradValue;
        }

        /// <summary>
        /// Convert a rectangle to a rect.
        /// </summary>
        /// <param name="rectangle">Rectangle to convert.</param>
        /// <returns>Converted rect.</returns>
        private Rect ConverToRect(Rectangle rectangle)
        {
            return new Rect(rectangle.Margin.Left, rectangle.Margin.Top,
                rectangle.Width, rectangle.Height);
        }

        /// <summary>
        /// Create a selection area.
        /// </summary>
        /// <param name="startGradIndex">Starting graduation index.</param>
        /// <param name="endGradIndex">Ending graduation index.</param>
        /// <returns>Generated area.</returns>
        private Rectangle CreateSelectionAreaFromMinorGrand(double startGradIndex,
            double endGradIndex)
        {
            return CreateSelectionArea(Math.Min(startGradIndex, endGradIndex),
                Math.Max(startGradIndex, endGradIndex) + _renderMinorTick);
        }

        /// <summary>
        /// Create a selection area.
        /// </summary>
        /// <param name="startGradValue">Starting graduation index.</param>
        /// <param name="endGradValue">Ending graduation index.</param>
        /// <returns>Generated area.</returns>
        private Rectangle CreateSelectionArea(
            double startGradValue, double endGradValue)
        {
            double maxGradValue = Math.Max(startGradValue, endGradValue);

            Rectangle rect = new Rectangle();
            rect.Margin = new Thickness(-SelectionAreaWidthPadding,
                GradToLocationY(maxGradValue), 0, 0);
            rect.Width = RulerWidth + (2.0 * SelectionAreaWidthPadding);
            rect.Height = GradToPixel(Math.Abs(endGradValue - startGradValue));
            if (rect.Margin.Top < 0)
            {
                rect.Height += rect.Margin.Top;
                rect.Margin = new Thickness(-SelectionAreaWidthPadding,
                    0, 0, 0);
            }

            if (rect.Margin.Top + rect.Height > ActualHeight)
            {
                rect.Height -= rect.Margin.Top + rect.Height - ActualHeight;
                rect.Margin = new Thickness(-SelectionAreaWidthPadding,
                    rect.Margin.Top - (rect.Margin.Top + rect.Height - ActualHeight),
                    0, 0);
            }

            rect.Stroke = selectionAreaStroke.Brush;
            rect.Fill = selectionAreaFill;
            rect.IsHitTestVisible = false;
            return rect;
        }

        /// <summary>
        /// Notify render range changed.
        /// </summary>
        private void NotifyRenderRangeChanged()
        {
            double gradMax = AbsoluteLogicalPixelToGrad(RulerHeight + OriginOffset);
            double gradMin = AbsoluteLogicalPixelToGrad(RulerHeight + OriginOffset - ActualHeight);
            VisualValueAxis visualValueAxis = (VisualValueAxis)DataContext;
            visualValueAxis.RenderRange = new ValueAxisRange(gradMin, gradMax);
        }

        #endregion
    }
}