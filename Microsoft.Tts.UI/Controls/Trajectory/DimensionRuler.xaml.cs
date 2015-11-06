//----------------------------------------------------------------------------
// <copyright file="DimensionRuler.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     The code logic of DimensionRuler
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Shapes;

    /// <summary>
    /// Interaction logic for DimensionRuler.xaml.
    /// </summary>
    public partial class DimensionRuler : UserControl
    {
        #region dependency property

        /// <summary>
        /// Graduation range.
        /// </summary>
        public static readonly DependencyProperty GraduationRangeProperty =
            DependencyProperty.Register("GraduationRange", typeof(int), typeof(DimensionRuler),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SelectedGraduationsProperty =
            DependencyProperty.Register("SelectedGraduations", typeof(ObservableCollection<int>), typeof(DimensionRuler),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnSelectedGraduationsChanged)));

        #endregion

        #region fields

        /// <summary>
        /// Ruler name.
        /// </summary>
        private static string rulerName = "Dimensions";

        /// <summary>
        /// Width ratio of label text to whole user control.
        /// </summary>
        private static double labelWidthRatio = 0.4;

        /// <summary>
        /// Width ratio of graduation to ruler.
        /// </summary>
        private static double gradiationRatio = 0.3;

        /// <summary>
        /// Width padding of selection area.
        /// </summary>
        private static double selectionAreaWidthPadding = 2;

        /// <summary>
        /// Height padding of selection area.
        /// </summary>
        private static double selectionAreaHeightPadding = 1;

        /// <summary>
        /// Stroke pen of selection area.
        /// </summary>
        private static Pen selectionAreaStroke = new Pen(Brushes.Blue, 1.0);

        /// <summary>
        /// Fill brush of selection area.
        /// </summary>
        private static Brush selectionAreaFill = new SolidColorBrush(Colors.Blue)
        {
            Opacity = 0.3
        };

        /// <summary>
        /// Stroke pen of ruler.
        /// </summary>
        private static Pen rulerStroke = new Pen(Brushes.Black, 1.0);

        /// <summary>
        /// Fill brush of ruler.
        /// </summary>
        private static Brush rulerFill = Brushes.Yellow;

        /// <summary>
        /// Label interval.
        /// </summary>
        private static int labelInterval = 5;

        /// <summary>
        /// Start index of selection area.
        /// </summary>
        private int _selectionStartIndex = -1;

        #endregion

        #region contructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DimensionRuler"/> class.
        /// </summary>
        public DimensionRuler()
        {
            InitializeComponent();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets the list of selected graduations.
        /// </summary>
        public ObservableCollection<int> SelectedGraduations
        {
            get
            {
                return (ObservableCollection<int>)this.GetValue(SelectedGraduationsProperty);
            }

            set
            {
                ObservableCollection<int> grads = value;
                this.SetValue(SelectedGraduationsProperty, grads);
                grads.CollectionChanged +=
                    delegate(object sender, NotifyCollectionChangedEventArgs e)
                    {
                        InvalidateVisual();
                    };
            }
        }

        /// <summary>
        /// Gets or sets graduation range.
        /// </summary>
        public int GraduationRange
        {
            get { return (int)this.GetValue(GraduationRangeProperty); }
            set { this.SetValue(GraduationRangeProperty, value); }
        }

        /// <summary>
        /// Gets X of ruler origin.
        /// </summary>
        private double RulerOriginX
        {
            get
            {
                return ActualWidth * labelWidthRatio;
            }
        }

        /// <summary>
        /// Gets width of ruler.
        /// </summary>
        private double RulerWidth
        {
            get
            {
                return ActualWidth * (1.0 - labelWidthRatio);
            }
        }

        /// <summary>
        /// Gets height of ruler.
        /// </summary>
        private double RulerHeight
        {
            get
            {
                return ActualHeight;
            }
        }

        /// <summary>
        /// Gets height of each graduation.
        /// </summary>
        private double GraduationHeight
        {
            get
            {
                return ActualHeight / (double)GraduationRange;
            }
        }

        #endregion

        #region methods

        /// <summary>
        /// Reset data.
        /// </summary>
        /// <param name="graduationCount">Graduation count.</param>
        public void Reset(int graduationCount)
        {
            GraduationRange = graduationCount;
            SelectedGraduations.Clear();
            for (int index = 1; index <= graduationCount; ++index)
            {
                SelectedGraduations.Add(index);
            }
        }

        /// <summary>
        /// Overrides OnRender method to draw ruler.
        /// </summary>
        /// <param name="drawingContext">Drawing context.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            _selectionCanvas.Children.Clear();
            _rulerArea.Margin = new Thickness(RulerOriginX, 0, 0, 0);
            _rulerArea.Height = RulerHeight;
            _rulerArea.Width = RulerWidth;

            FormattedText rulerText = new FormattedText(rulerName, CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, new Typeface("Verdana"), 10, Brushes.Black);
            Transform transform = new RotateTransform(270, ActualWidth / 2.0, ActualHeight / 2.0);
            drawingContext.PushTransform(transform);
            drawingContext.DrawText(rulerText,
                new Point(0, ((ActualHeight - rulerText.Height - ActualWidth) / 2.0) + 5.0));
            drawingContext.Pop();

            // draw ruler.
            Rect rect = new Rect(RulerOriginX, 0, RulerWidth, RulerHeight);
            drawingContext.DrawRectangle(rulerFill, rulerStroke, rect);

            if (GraduationRange > 0)
            {
                // draw scale and label.
                for (int gradIndex = 1; gradIndex <= GraduationRange; ++gradIndex)
                {
                    double gradY = GraduationHeight * gradIndex;

                    if ((GraduationRange - gradIndex) % labelInterval == 0)
                    {
                        FormattedText text = new FormattedText(gradIndex.ToString(),
                            CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                            new Typeface("Verdana"), 8, Brushes.Black);
                        double textX = ((RulerWidth - text.Width) / 2.0) + RulerOriginX;
                        drawingContext.DrawText(text,
                            new Point(textX, RulerHeight - gradY + ((GraduationHeight - text.Height) / 2.0)));
                    }

                    drawingContext.DrawLine(rulerStroke, new Point(ActualWidth, gradY),
                       new Point(RulerOriginX + (RulerWidth * (1.0 - gradiationRatio)), gradY));
                }

                // draw selection area.
                if (SelectedGraduations.Count > 0)
                {
                    int startGrad = SelectedGraduations[0];
                    int endGrad = SelectedGraduations[0];
                    foreach (int grad in SelectedGraduations)
                    {
                        if (grad - endGrad > 1)
                        {
                            drawingContext.DrawRectangle(selectionAreaFill, selectionAreaStroke,
                                ConverToRect(CreateSelectionArea(startGrad, endGrad)));
                            startGrad = grad;
                        }

                        endGrad = grad;
                    }

                    drawingContext.DrawRectangle(selectionAreaFill, selectionAreaStroke,
                        ConverToRect(CreateSelectionArea(startGrad, endGrad)));
                }
            }
        }

        private static void OnSelectedGraduationsChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            Debug.Assert(dependencyObject != null && dependencyObject is DimensionRuler);
            Debug.Assert(e != null);
            Debug.Assert(e.NewValue != null && e.NewValue is ObservableCollection<int>);
            DimensionRuler ruler = dependencyObject as DimensionRuler;

            ruler.SelectedGraduations = e.NewValue as ObservableCollection<int>;
        }

        #endregion

        #region UI events

        /// <summary>
        /// Event handler when mouse enters ruler area.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse event args.</param>
        private void OnRulerAreaMouseEnter(object sender, MouseEventArgs e)
        {
            if (GraduationRange > 0)
            {
                _labelPopup.IsOpen = true;
            }
        }

        /// <summary>
        /// Event handler when mouse leaves ruler area.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse event args.</param>
        private void OnRulerAreaMouseLeave(object sender, MouseEventArgs e)
        {
            if (GraduationRange > 0)
            {
                _labelPopup.IsOpen = false;
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    EndSelection(e);
                }
            }
        }

        /// <summary>
        /// Event handler when mouse moves inside ruler area.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse event args.</param>
        private void OnRulerAreaMouseMove(object sender, MouseEventArgs e)
        {
            if (GraduationRange > 0)
            {
                // close popup to renew the popup position.
                _labelPopup.IsOpen = false;
                Point mousePos = e.GetPosition(this as IInputElement);
                int gradIndex = PixelToGraduation(mousePos.Y);
                _popupText.Text = gradIndex.ToString();
                _labelPopup.IsOpen = true;

                if (e.LeftButton == MouseButtonState.Pressed &&
                    _selectionStartIndex > 0)
                {
                    _currentSelectionCanvas.Children.Clear();
                    _currentSelectionCanvas.Children.Add(CreateSelectionArea(_selectionStartIndex, gradIndex));
                }
            }
        }

        /// <summary>
        /// Event handler when mouse left button down inside ruler area.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse button event args.</param>
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (GraduationRange > 0)
            {
                Point mousePos = e.GetPosition(this as IInputElement);
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    StartSelection(mousePos.Y);
                }
                else
                {
                    SelectedGraduations.Clear();
                    _currentSelectionCanvas.Children.Clear();
                    StartSelection(mousePos.Y);
                }
            }
        }

        /// <summary>
        /// Event handler when mouse left button up inside ruler area.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse button event args.</param>
        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (GraduationRange > 0)
            {
                EndSelection(e);
            }
        }

        private void EndSelection(MouseEventArgs e)
        {
            List<int> selectedGraduations = new List<int>(SelectedGraduations);

                Point mousePos = e.GetPosition(this as IInputElement);
                if (_currentSelectionCanvas.Children.Count != 0)
                {
                    _currentSelectionCanvas.Children.Clear();
                    int gradIndex = PixelToGraduation(mousePos.Y);
                    int startIndex = Math.Min(gradIndex, _selectionStartIndex);
                    int endIndex = Math.Max(gradIndex, _selectionStartIndex);
                    for (int index = startIndex; index <= endIndex; ++index)
                    {
                    if (!selectedGraduations.Contains(index))
                    {
                        selectedGraduations.Add(index);
                    }
                }

                selectedGraduations.Sort();
            }

            SelectedGraduations.Clear();
            foreach (int grad in selectedGraduations)
            {
                SelectedGraduations.Add(grad);
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Convert a height to a graduation index.
        /// </summary>
        /// <param name="verticalPos">Vertical position of the pixel.</param>
        /// <returns>Graduation index.</returns>
        private int PixelToGraduation(double verticalPos)
        {
            int index = (int)((RulerHeight - verticalPos) / GraduationHeight) + 1;
            index = Math.Min(index, GraduationRange);
            return index;
        }

        /// <summary>
        /// Start selection.
        /// </summary>
        /// <param name="mousePos">Mouse position.</param>
        private void StartSelection(double mousePos)
        {
            int gradIndex = PixelToGraduation(mousePos);
            _currentSelectionCanvas.Children.Add(CreateSelectionArea(gradIndex, gradIndex));
            _selectionStartIndex = gradIndex;
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
        /// <param name="startIndex">Starting graduation index.</param>
        /// <param name="endIndex">Ending graduation index.</param>
        /// <returns>Generated area.</returns>
        private Rectangle CreateSelectionArea(int startIndex, int endIndex)
        {
            int maxIndex = Math.Max(startIndex, endIndex);
            Rectangle rect = new Rectangle();
            rect.Margin = new Thickness(RulerOriginX - selectionAreaWidthPadding,
                ((double)(GraduationRange - maxIndex) * GraduationHeight) - selectionAreaHeightPadding,
                0, 0);
            rect.Width = RulerWidth + (2.0 * selectionAreaWidthPadding);
            rect.Height = (GraduationHeight * (double)(Math.Abs(endIndex - startIndex) + 1))
                + (2.0 * selectionAreaHeightPadding);
            rect.Stroke = selectionAreaStroke.Brush;
            rect.Fill = selectionAreaFill;
            rect.IsHitTestVisible = false;
            return rect;
        }

        #endregion
    }
}