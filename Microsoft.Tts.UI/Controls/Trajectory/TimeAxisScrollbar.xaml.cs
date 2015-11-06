namespace Microsoft.Tts.UI.Controls.Trajectory
{
    using System;
    using System.Collections.Generic;
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

    /// <summary>
    /// Interaction logic for TimeAxisScrollbar.xaml.
    /// </summary>
    public partial class TimeAxisScrollbar : UserControl
    {
        #region fields

        private VisualTimeAxis _timeAxis = new VisualTimeAxis();

        private double _lastValue = 0;

        #endregion

        public TimeAxisScrollbar()
        {
            InitializeComponent();
        }

        public void SetDataContext(VisualTimeAxis timeAxis)
        {
            DataContext = timeAxis;
            timeAxis.PropertyChanged += OnTimeAxisPropertyChanged;
            _timeAxis = timeAxis;
            ResetScrollar();
        }

        /// <summary>
        /// Update time axis.
        /// </summary>
        public void ResetScrollar()
        {
            double waveDuration = _timeAxis.Duration;
            if (_timeAxis.ZoomMode == ZoomMode.FrameLevel)
            {
                double length = ViewHelper.TimespanToPixel(waveDuration, _timeAxis.ZoomScale);
                _scrollBar.Maximum = length - ActualWidth;
                _scrollBar.Value = Math.Floor(ViewHelper.TimespanToPixel(_timeAxis.StartingTime, _timeAxis.ZoomScale));
                _scrollBar.SmallChange = ViewHelper.TimespanToPixel(_timeAxis.SampleInterval, _timeAxis.ZoomScale);
            }
            else
            {
                _scrollBar.Maximum = _scrollBar.ViewportSize;
            }
        }

        private void OnTimeAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ResetScrollar();
        }

        /// <summary>
        /// On scrollbar value changed.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OnScrollbarValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Math.Abs(e.NewValue - _lastValue) >=
                ViewHelper.TimespanToPixel(_timeAxis.SampleInterval, _timeAxis.ZoomScale))
            {
                _timeAxis.PropertyChanged -= OnTimeAxisPropertyChanged;
                _timeAxis.StartingTime =
                    Math.Floor(ViewHelper.PixelToTimeSpan(_scrollBar.Value, _timeAxis.ZoomScale) /
                    _timeAxis.SampleInterval) * _timeAxis.SampleInterval;
                _timeAxis.PropertyChanged += OnTimeAxisPropertyChanged;
                _lastValue = e.NewValue;
            }
        }

        /// <summary>
        /// Scroll by mouse wheel.
        /// </summary>
        /// <param name="sender">Object.</param>
        /// <param name="e">MouseWheelEventArgs.</param>
        private void OnHorScrollBarMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            _scrollBar.Value += _scrollBar.SmallChange * e.Delta;
        }
    }
}