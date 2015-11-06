namespace Microsoft.Tts.UI.Controls.Acoustic
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
    using Microsoft.Tts.UI.Controls.Data;
    using Microsoft.Tts.UI.Controls.Trajectory.Data;

    /// <summary>
    /// Interaction logic for WaveFormViewer.xaml.
    /// </summary>
    public partial class WaveFormViewer : UserControl, IDisposable
    {
        private ViewDataPropertyBinder _timeAxisPropertiesBinder = null;

        private VisualTimeAxis _internalTimeAxis = new VisualTimeAxis();

        public WaveFormViewer()
        {
            InitializeComponent();
        }

        public VisualTimeAxis InternalTimeAxis
        {
            get
            {
                return _internalTimeAxis;
            }
        }

        public void SetDataContext(VisualWaveForm visualWaveForm)
        {
            SetDataContext(visualWaveForm, true);
        }

        public void SetDataContext(VisualWaveForm visualWaveForm, bool isBinding)
        {
            VisualLinerSamples samples = new VisualLinerSamples();
            samples.TimeAxis = _internalTimeAxis;
            samples.YAxis = visualWaveForm.YAxis;
            samples.Samples = visualWaveForm.WaveSamples;
            _linerGraph.SetDataContext(samples);

            _internalTimeAxis.CopyPropertiesFrom(visualWaveForm.TimeAxis);
            _internalTimeAxis.SampleInterval = (double)1000 / visualWaveForm.Format.SamplesPerSecond;
            if (isBinding)
            {
                visualWaveForm.PropertyChanged += OnWaveFormPropertyChanged;
                _timeAxisPropertiesBinder = new ViewDataPropertyBinder(_internalTimeAxis, visualWaveForm.TimeAxis);
                _timeAxisPropertiesBinder.ExcludedProperties.Add("SampleInterval");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_timeAxisPropertiesBinder != null)
                {
                    _timeAxisPropertiesBinder.Dispose();
                }
            }
        }

        private void OnWaveFormPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Format")
            {
                VisualWaveForm waveForm = (VisualWaveForm)sender;
                _internalTimeAxis.SampleInterval = (double)1000 / waveForm.Format.SamplesPerSecond;
            }
        }
    }
}