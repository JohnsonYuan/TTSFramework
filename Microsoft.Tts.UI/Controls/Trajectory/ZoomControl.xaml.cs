namespace Microsoft.Tts.UI.Controls.Trajectory
{
    using System;
    using System.Collections.Generic;
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
    /// Interaction logic for ZoomControl.xaml.
    /// </summary>
    public partial class ZoomControl : Slider
    {
        private const double _ScaleMaxRatio = 10;

        private VisualTimeAxis _timeAxis;

        public ZoomControl()
        {
            InitializeComponent();
        }

        public void SetDataContext(VisualTimeAxis timeAxis)
        {
            DataContext = timeAxis;
            _timeAxis = timeAxis;
            Maximum = 10;
            Minimum = 1;
            SmallChange = 0.5;
            Binding binding = new Binding("ZoomScale");
            binding.Source = timeAxis;
            binding.Converter = new ZoomScaleConverter();
            binding.Mode = BindingMode.TwoWay;
            SetBinding(ValueProperty, binding);
        }

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            if (Value == Minimum)
            {
                _timeAxis.ZoomMode = ZoomMode.WholePicture;
            }
            else if (_timeAxis.ZoomMode != ZoomMode.FrameLevel)
            {
                _timeAxis.ZoomMode = ZoomMode.FrameLevel;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta > 0)
            {
                Value += SmallChange;
            }
            else
            {
                Value -= SmallChange;
            }
        }

        public class ZoomScaleConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                double zoomScale = (double)value;
                if (zoomScale > 1.0)
                {
                    zoomScale += 4;
                }
                else
                {
                    zoomScale = 6 - (1 / zoomScale);
                }

                return zoomScale;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                double sliderValue = (double)value;
                if (sliderValue > 5)
                {
                    sliderValue = sliderValue - 4;
                }
                else
                {
                    sliderValue = 1 / (6 - sliderValue);
                }

                return sliderValue;
            }
        }
    }
}