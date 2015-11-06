namespace Microsoft.Tts.UI.Controls.Acoustic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using Data;

    /// <summary>
    /// Interaction logic for AcousticEditor.xaml.
    /// </summary>
    public partial class AcousticEditor : UserControl
    {
        public AcousticEditor()
        {
            InitializeComponent();
        }

        public void SetDataContext(VisualAcousticSpace visualAcoustic)
        {
            _f0Editor.SetDataContext(visualAcoustic.F0);
            _f0YAxis.SetDataContext(visualAcoustic.F0.YAxis);
            _gainEditor.SetDataContext(visualAcoustic.Gain);
            _gainYAxis.SetDataContext(visualAcoustic.Gain.YAxis);
            _durations.SetDataContext(visualAcoustic.Durations);
            _waveForm.SetDataContext(visualAcoustic.WaveForm);
            _waveformYAxis.SetDataContext(visualAcoustic.WaveForm.YAxis);

            _phoneSegmentGraph.Segments = visualAcoustic.PhoneSegments;
            _phoneSegmentGraph.TimeAxis = visualAcoustic.F0.TimeAxis;

            _wordSegmentGraph.Segments = visualAcoustic.WordSegments;
            _wordSegmentGraph.TimeAxis = visualAcoustic.F0.TimeAxis;

            _timeAxisScallbar.SetDataContext(visualAcoustic.F0.TimeAxis);
            _zoomControl.SetDataContext(visualAcoustic.F0.TimeAxis);
        }
    }
}