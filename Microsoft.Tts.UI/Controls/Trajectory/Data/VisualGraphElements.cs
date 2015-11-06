//----------------------------------------------------------------------------
// <copyright file="VisualGraphElements.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      class of visual graph elements
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory.Data
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Waveform;
    using Microsoft.Tts.UI.Controls.Data;
    using Microsoft.Tts.UI.Controls.Trajectory;

    /// <summary>
    /// The class of VisualSpectrumForm.
    /// </summary>
    public class VisualSpectrumForm : VisualTrajectoryBase
    {
        private double[][] _spectrum;

        /// <summary>
        /// Gets or sets spectrum.
        /// </summary>
        public double[][] Spectrum
        {
            get
            {
                return _spectrum;
            }

            set
            {
                _spectrum = value;
                NotifyPropertyChanged("Spectrum");
            }
        }
    }

    /// <summary>
    /// The class of VisualWaveForm.
    /// </summary>
    public class VisualWaveForm : VisualTrajectoryBase
    {
        #region fields

        private WaveFormat _waveFormat = new WaveFormat();

        private TransactionObservableCollection<double> _waveSamples;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualWaveForm"/> class.
        /// </summary>
        public VisualWaveForm()
        {
            WaveSamples = new TransactionObservableCollection<double>();
        }

        /// <summary>
        /// Gets or sets the wave format.
        /// </summary>
        public WaveFormat Format
        {
            get 
            { 
                return _waveFormat; 
            }

            set
            {
                _waveFormat = value;
                NotifyPropertyChanged("Format");
            }
        }

        /// <summary>
        /// Gets or sets the wave samples.
        /// </summary>
        public TransactionObservableCollection<double> WaveSamples
        {
            get 
            { 
                return _waveSamples; 
            }

            set
            {
                if (value != null)
                {
                    value.TransactionChangeHandler += OnWavesTransactionChangeHandler;
                }

                if (_waveSamples != null)
                {
                    _waveSamples.TransactionChangeHandler -= OnWavesTransactionChangeHandler;
                }

                _waveSamples = value;
                NotifyPropertyChanged("WaveSamples");
            }
        }

        /// <summary>
        /// Create a visual wave instance from the wave stream.
        /// </summary>
        /// <param name="waveStream">Stream.</param>
        /// <returns>VisualWaveForm.</returns>
        public static VisualWaveForm CreateFromStream(Stream waveStream)
        {
            VisualWaveForm waveForm = new VisualWaveForm();
            WaveFile waveFile = new WaveFile();
            waveFile.Load(waveStream);
            waveForm.Format = waveFile.Format;
            TransactionObservableCollection<double> samples = waveForm.WaveSamples;
            foreach (short sample in waveFile.DataIn16Bits)
            {
                samples.Add((double)sample);
            }

            waveForm.YAxis.Reset(samples, 0);
            return waveForm;
        }

        private void OnWavesTransactionChangeHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsResetTimeAxisAutomatically)
            {
                TimeAxis.Duration = TimeAxis.SampleInterval * WaveSamples.Count;
            }

            if (IsResetYAxisAutomatically)
            {
                YAxis.Reset(WaveSamples, 0);
            }
        }
    }

    /// <summary>
    /// The class of VisualDurations.
    /// </summary>
    public class VisualDurations : ViewDataBase
    {
        #region fields

        private TransactionObservableCollection<double> _durations;

        private VisualTimeAxis _timeAxis;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualDurations"/> class.
        /// </summary>
        public VisualDurations()
        {
            Durations = new TransactionObservableCollection<double>();
            TimeAxis = new VisualTimeAxis();
            IsResetTimeAxisAutomatically = false;
        }

        /// <summary>
        /// Gets or sets durations.
        /// </summary>
        public TransactionObservableCollection<double> Durations
        {
            get 
            { 
                return _durations;
            }

            set
            {
                if (value != null)
                {
                    value.TransactionChangeHandler += OnDurationTransactionChangeHandler;
                }

                if (_durations != null)
                {
                    _durations.TransactionChangeHandler -= OnDurationTransactionChangeHandler;
                }

                _durations = value;
                NotifyPropertyChanged("Durations");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether reset the time axis according to the duration changes.
        /// </summary>
        public bool IsResetTimeAxisAutomatically
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets X-axis view data.
        /// </summary>
        public VisualTimeAxis TimeAxis
        {
            get 
            { 
                return _timeAxis; 
            }

            set
            {
                if (_timeAxis != null)
                {
                    _timeAxis.PropertyChanged -= OnTimeAxisPropertyChanged;
                }

                if (value != null)
                {
                    value.PropertyChanged += OnTimeAxisPropertyChanged;
                }

                _timeAxis = value;
                NotifyPropertyChanged("TimeAxis");
            }
        }

        private void OnDurationTransactionChangeHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsResetTimeAxisAutomatically)
            {
                double total = 0;
                foreach (double duration in Durations)
                {
                    total += duration;
                }

                TimeAxis.Duration = total;
            }
        }

        private void OnTimeAxisPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("TimeAxis");
        }
    }

    /// <summary>
    /// The class of VisualLinerSamples.
    /// </summary>
    public class VisualLinerSamples : VisualTrajectoryBase
    {
        #region fields
        
        private TransactionObservableCollection<double> _samples;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualLinerSamples"/> class.
        /// </summary>
        public VisualLinerSamples()
        {
            Samples = new TransactionObservableCollection<double>();
        }

        /// <summary>
        /// Gets or sets samples.
        /// </summary>
        public TransactionObservableCollection<double> Samples
        {
            get
            {
                return _samples;
            }

            set
            {
                if (_samples != null)
                {
                    _samples.TransactionChangeHandler -= OnSamplesTransactionChangeHandler;
                }

                if (value != null)
                {
                    value.TransactionChangeHandler += OnSamplesTransactionChangeHandler;
                }

                _samples = value;
                NotifyPropertyChanged("Samples");
            }
        }

        private void OnSamplesTransactionChangeHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsResetTimeAxisAutomatically)
            {
                TimeAxis.Duration = TimeAxis.SampleInterval * Samples.Count;
            }

            if (IsResetYAxisAutomatically)
            {
                YAxis.Reset(Samples);
            }
        }

        /// <summary>
        /// The class of InvalidSampleException.
        /// </summary>
        [Serializable]
        public class InvalidSampleException : Exception
        {
        }
    }

    /// <summary>
    /// Class of edit controlling.
    /// </summary>
    public class VisualLinerEditControl
    {
        public VisualLinerEditControl()
        {
            Mode = EditMode.Free;
        }

        public enum EditMode
        {
            /// <summary>
            /// Free.
            /// </summary>
            Free,

            /// <summary>
            /// Line.
            /// </summary>
            Line,

            /// <summary>
            /// Sin.
            /// </summary>
            Sin,

            /// <summary>
            /// Cos.
            /// </summary>
            Cos,

            /// <summary>
            /// Custom.
            /// </summary>
            Custom
        }

        public EditMode Mode
        {
            get;
            set;
        }

        public VisualLinerSamples EditingSamples
        {
            get;
            set;
        }
    }

    /// <summary>
    /// The class of VisualConstantSamples.
    /// </summary>
    public class VisualConstantSamples : VisualTrajectoryBase
    {
        #region fields

        private TransactionObservableCollection<double> _samples;

        private TransactionObservableCollection<double> _deviations;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualConstantSamples"/> class.
        /// </summary>
        public VisualConstantSamples()
        {
            Samples = new TransactionObservableCollection<double>();
            Deviations = new TransactionObservableCollection<double>();
        }

        /// <summary>
        /// Gets or sets the samples.
        /// </summary>
        public TransactionObservableCollection<double> Samples
        {
            get 
            {
                return _samples; 
            }

            set
            {
                _samples = value;
                NotifyPropertyChanged("Samples");
            }
        }

        /// <summary>
        /// Gets or sets the deviations.
        /// </summary>
        public TransactionObservableCollection<double> Deviations
        {
            get
            { 
                return _deviations; 
            }

            set
            {
                _deviations = value;
                NotifyPropertyChanged("Deviations");
            }
        }
    }

    /// <summary>
    /// Class of VisualSegment.
    /// </summary>
    public class VisualSegment : ViewDataBase
    {
        #region fields

        /// <summary>
        /// Text of word or phoneme.
        /// </summary>
        private string _text = string.Empty;

        /// <summary>
        /// Start frame index.
        /// </summary>
        private int _startFrameIndex = 0;

        /// <summary>
        /// End frame index.
        /// </summary>
        private int _endFrameIndex = 0;

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets text.
        /// </summary>
        public string Text
        {
            get
            {
                return _text;
            }

            set
            {
                _text = value;
                NotifyPropertyChanged("Grapheme");
            }
        }

        /// <summary>
        /// Gets or sets start frame index.
        /// </summary>
        public int StartFrameIndex
        {
            get
            {
                return _startFrameIndex;
            }

            set
            {
                _startFrameIndex = value;
                NotifyPropertyChanged("StartFrameIndex");
            }
        }

        /// <summary>
        /// Gets or sets end frame index.
        /// </summary>
        public int EndFrameIndex
        {
            get
            {
                return _endFrameIndex;
            }

            set
            {
                _endFrameIndex = value;
                NotifyPropertyChanged("EndFrameIndex");
            }
        }

        #endregion
    }

    /// <summary>
    /// Value axis range.
    /// </summary>
    public class ValueAxisRange : ViewDataBase
    {
        /// <summary>
        /// Min value of the range.
        /// </summary>
        private double _min;

        /// <summary>
        /// Max value of the range.
        /// </summary>
        private double _max;

        /// <summary>
        /// Initializes a new instance of the ValueAxisRange class.
        /// </summary>
        /// <param name="min">Min value of the range.</param>
        /// <param name="max">Max value of the range.</param>
        public ValueAxisRange(double min, double max)
        {
            Set(min, max);
        }

        /// <summary>
        /// Gets the min value of the range.
        /// </summary>
        public double Min
        {
            get
            {
                return _min;
            }

            private set
            {
                _min = value;
                NotifyPropertyChanged("Min");
            }
        }

        /// <summary>
        /// Gets max value of the range.
        /// </summary>
        public double Max
        {
            get
            {
                return _max;
            }

            private set
            {
                _max = value;
                NotifyPropertyChanged("Max");
            }
        }

        /// <summary>
        /// Gets the range value.
        /// </summary>
        public double Range
        {
            get
            {
                Validate();
                return Max - Min;
            }
        }

        /// <summary>
        /// Set min and max value of the range.
        /// </summary>
        /// <param name="min">Min value of the range.</param>
        /// <param name="max">Max value of the range.</param>
        public void Set(double min, double max)
        {
            Min = min;
            Max = max;
            Validate();
        }

        /// <summary>
        /// Validate value of the range.
        /// </summary>
        private void Validate()
        {
            if (Max < Min)
            {
                throw new InvalidDataException(string.Format(
                    "Max [{0}] should not be smaller than Min [{1}].",
                    Max, Min));
            }
        }
    }

    /// <summary>
    /// Class of VisualValueAxis.
    /// </summary>
    public class VisualValueAxis : ViewDataBase
    {
        #region fields

        /// <summary>
        /// Default min value of the axis.
        /// </summary>
        private const double DefaultMin = 0;

        /// <summary>
        /// Default max value of the axis.
        /// </summary>
        private const double DefaultMax = 10;

        /// <summary>
        /// Default minor graduation count in the axis.
        /// </summary>
        private const double DefaultMinorCount = 10;

        /// <summary>
        /// Default major step of the graduation count in the axis.
        /// </summary>
        private const int DefaultMajorStep = 2;

        /// <summary>
        /// Value range of the axis.
        /// </summary>
        private ValueAxisRange _valueRange;

        /// <summary>
        /// Render range of the axis.
        /// </summary>
        private ValueAxisRange _renderRange;

        /// <summary>
        /// Minor graduation.
        /// </summary>
        private double _minorTick;

        /// <summary>
        /// Major step of the graduation.
        /// </summary>
        private int _majorStep;

        #endregion

        /// <summary>
        /// Initializes a new instance of the VisualValueAxis class.
        /// </summary>
        public VisualValueAxis()
        {
            ValueRange = new ValueAxisRange(DefaultMin, DefaultMax);
            RenderRange = new ValueAxisRange(DefaultMin, DefaultMax);
            Reset(DefaultMin, DefaultMax);
        }

        #region properties

        /// <summary>
        /// Gets or sets value range.
        /// </summary>
        public ValueAxisRange ValueRange
        {
            get
            {
                return _valueRange;
            }

            set
            {
                if (_valueRange != null)
                {
                    _valueRange.PropertyChanged -= ValueAxisValueRangeChanged;
                }

                if (value != null)
                {
                    value.PropertyChanged += ValueAxisValueRangeChanged;
                }

                _valueRange = value;
                NotifyPropertyChanged("ValueRange");
            }
        }

        /// <summary>
        /// Gets or sets render range.
        /// </summary>
        public ValueAxisRange RenderRange
        {
            get
            {
                return _renderRange;
            }

            set
            {
                if (_renderRange != null)
                {
                    _renderRange.PropertyChanged -= ValueAxisRenderRangeChanged;
                }

                if (value != null)
                {
                    value.PropertyChanged += ValueAxisRenderRangeChanged;
                }

                _renderRange = value;
                NotifyPropertyChanged("RenderRange");
            }
        }

        /// <summary>
        /// Gets or sets minor graduation.
        /// </summary>
        public double MinorTick
        {
            get
            {
                return _minorTick;
            }

            set
            {
                _minorTick = value;
                NotifyPropertyChanged("MinorTick");
            }
        }

        /// <summary>
        /// Gets or sets major step of the graduation.
        /// </summary>
        public int MajorStep
        {
            get
            {
                return _majorStep;
            }

            set
            {
                _majorStep = value;
                NotifyPropertyChanged("MajorStep");
            }
        }

        #endregion

        /// <summary>
        /// Reset min and max of the range.
        /// </summary>
        /// <param name="min">Min of the range.</param>
        /// <param name="max">Max of the range.</param>
        public void Reset(double min, double max)
        {
            double delta = Math.Abs(max - min);
            double scaleBase = Math.Pow(10, Math.Floor(Math.Log10(delta)));
            double adjustedMin = Math.Floor(min / scaleBase) * scaleBase;
            double adjustedMax = Math.Ceiling(max / scaleBase) * scaleBase;

            _valueRange.Set(adjustedMin, adjustedMax);
            _renderRange.Set(adjustedMin, adjustedMax);
            MinorTick = ViewHelper.AdjustFactor((adjustedMax - adjustedMin) / DefaultMinorCount);
            MajorStep = DefaultMajorStep;
        }

        public void Reset(Collection<double> samples)
        {
            Reset(samples, double.NaN);
        }

        /// <summary>
        /// Reset by a collection.
        /// </summary>
        /// <param name="samples">Collection.</param>
        /// <param name="symmetric">Symmetric.</param>
        public void Reset(Collection<double> samples, double symmetric)
        {
            if (samples.Count > 0)
            {
                double min = double.MaxValue, max = double.MinValue;
                foreach (double d in samples)
                {
                    if (d > double.MinValue && d < double.MaxValue)
                    {
                        min = Math.Min(min, d);
                        max = Math.Max(max, d);
                    }
                }

                if (min < symmetric && max > symmetric)
                {
                    double lenMax = max - symmetric;
                    double lenMin = symmetric - min;
                    if (lenMax > lenMin)
                    {
                        min = symmetric - lenMax;
                    }
                    else
                    {
                        max = symmetric + lenMin;
                    }
                }

                Reset(min, max);
            }
        }

        private void ValueAxisValueRangeChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("ValueRange");
        }

        private void ValueAxisRenderRangeChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("RenderRange");
        }
    }

    /// <summary>
    /// Class of VisualTimeAxis.
    /// </summary>
    public class VisualTimeAxis : ViewDataBase
    {
        #region fields

        /// <summary>
        /// Sample interval in millisecond.
        /// </summary>
        private double _sampleInterval = 5.0;

        /// <summary>
        /// Wave duration.
        /// </summary>
        private double _duration = 0.0;

        /// <summary>
        /// Starting time to show.
        /// </summary>
        private double _startingTime;

        /// <summary>
        /// Whether to show frame line.
        /// </summary>
        private bool _framelineShowed;

        /// <summary>
        /// Zoom scale.
        /// </summary>
        private double _zoomScale;

        /// <summary>
        /// Zoom mode.
        /// </summary>
        private ZoomMode _zoomMode = ZoomMode.FrameLevel;

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the VisualTimeAxis class.
        /// </summary>
        public VisualTimeAxis()
        {
            Reset();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets sample interval in millisecond.
        /// </summary>
        public double SampleInterval
        {
            get
            {
                return _sampleInterval;
            }

            set
            {
                _sampleInterval = value;
                NotifyPropertyChanged("SampleInterval");
            }
        }

        /// <summary>
        /// Gets or sets wave duration.
        /// </summary>
        public double Duration
        {
            get
            {
                return _duration;
            }

            set
            {
                _duration = value;
                NotifyPropertyChanged("Duration");
            }
        }

        /// <summary>
        /// Gets or sets starting time.
        /// </summary>
        public double StartingTime
        {
            get
            {
                return _startingTime;
            }

            set
            {
                _startingTime = value;
                NotifyPropertyChanged("StartingTime");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show frame line.
        /// </summary>
        public bool FramelineShowed
        {
            get
            {
                return _framelineShowed;
            }

            set
            {
                _framelineShowed = value;
                NotifyPropertyChanged("FramelineShowed");
            }
        }

        /// <summary>
        /// Gets or sets zoom scale.
        /// </summary>
        public double ZoomScale
        {
            get
            {
                return _zoomScale;
            }

            set
            {
                _zoomScale = value;
                NotifyPropertyChanged("ZoomScale");
            }
        }

        /// <summary>
        /// Gets or sets zoom mode.
        /// </summary>
        public ZoomMode ZoomMode
        {
            get
            {
                return _zoomMode;
            }

            set
            {
                _zoomMode = value;
                NotifyPropertyChanged("ZoomMode");
            }
        }

        #endregion

        #region methods

        /// <summary>
        /// Reset data.
        /// </summary>
        public void Reset()
        {
            FramelineShowed = true;
            StartingTime = 0.0;
            ZoomScale = 1.0;
            ZoomMode = ZoomMode.FrameLevel;
            Duration = 0.0;
        }

        /// <summary>
        /// Set duration.
        /// </summary>
        /// <param name="frameCount">Frame count.</param>
        public void SetDuration(int frameCount)
        {
            Debug.Assert(frameCount >= 0, "Invalid frame number.");
            Duration = SampleInterval * frameCount;
        }

        #endregion
    }

    /// <summary>
    /// Class of VisualDisplayController.
    /// </summary>
    public class VisualDisplayController : ViewDataBase
    {
        #region fields

        /// <summary>
        /// Indicates whether to enable show auxiliary trajectory.
        /// </summary>
        private bool _auxiliaryTrajectoryEnabled;

        /// <summary>
        /// Indicates whether to show delta graph.
        /// </summary>
        private bool _deltaShowed = true;

        /// <summary>
        /// Indicates whether to show acceleration graph.
        /// </summary>
        private bool _accelerationShowed = true;

        /// <summary>
        /// Indicates whether to show waveform graph.
        /// </summary>
        private bool _waveformShowed = true;

        /// <summary>
        /// The display controller of trajectory.
        /// </summary>
        private VisualTrajectoryDisplayController _trajectoryDisplayController = new VisualTrajectoryDisplayController();

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualDisplayController"/> class.
        /// </summary>
        public VisualDisplayController()
        {
            _trajectoryDisplayController.PropertyChanged += OnTrajectoryDisplayControllerPropertyChanged;
        }

        #region properties

        /// <summary>
        /// Gets the trajectory display controller.
        /// </summary>
        public VisualTrajectoryDisplayController TrajectoryDisplayController
        {
            get { return _trajectoryDisplayController; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to enable show auxiliary trajectory.
        /// </summary>
        public bool AuxiliaryTrajectoryEnabled
        {
            get
            {
                return _auxiliaryTrajectoryEnabled;
            }

            set
            {
                _auxiliaryTrajectoryEnabled = value;
                NotifyPropertyChanged("AuxiliaryTrajectoryEnabled");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show delta graph.
        /// </summary>
        public bool DeltaShowed
        {
            get
            {
                return _deltaShowed;
            }

            set
            {
                _deltaShowed = value;
                NotifyPropertyChanged("DeltaShowed");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show acceleration graph.
        /// </summary>
        public bool AccelerationShowed
        {
            get
            {
                return _accelerationShowed;
            }

            set
            {
                _accelerationShowed = value;
                NotifyPropertyChanged("AccelerationShowed");
            }
        }

        public bool WaveformShowed
        {
            get
            {
                return _waveformShowed;
            }

            set
            {
                _waveformShowed = value;
                NotifyPropertyChanged("WaveformShowed");
            }
        }

        #endregion

        private void OnTrajectoryDisplayControllerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(e.PropertyName);
        }
    }
}