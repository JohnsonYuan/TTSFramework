//----------------------------------------------------------------------------
// <copyright file="AcousticViewData.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      class of visual graph elements
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Acoustic.Data
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.UI.Controls.Data;
    using Microsoft.Tts.UI.Controls.Trajectory;
    using Trajectory.Data;

    /// <summary>
    /// Class of VisualF0.
    /// </summary>
    public class VisualF0 : VisualLinerSamples
    {
        public static double MaxF0Value = 8000;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualF0"/> class.
        /// </summary>
        public VisualF0()
        {
            PropertyChanged += OnVisualF0PropertyChanged;
            Samples.CollectionChanged += OnSamplesCollectionChanged;
            IsResetYAxisAutomatically = true;
            GuideLineSamples = new TransactionObservableCollection<double>();
        }

        /// <summary>
        /// Gets or sets The F0 guideline samples.
        /// </summary>
        public TransactionObservableCollection<double> GuideLineSamples
        {
            get;
            set;
        }

        private void OnVisualF0PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Samples")
            {
                Samples.CollectionChanged += OnSamplesCollectionChanged;
            }
        }

        private void OnSamplesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Reset)
            {
                int nChanged = e.NewItems.Count;
                for (int i = 0; i < nChanged; i++)
                {
                    if (!double.IsNaN(Samples[e.NewStartingIndex + i]))
                    {
                        // Check F0 according to runtime assumption.
                        if (Samples[e.NewStartingIndex + i] > MaxF0Value)
                        {
                            throw new VisualLinerSamples.InvalidSampleException();
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Class of VisualGain.
    /// </summary>
    public class VisualGain : VisualLinerSamples
    {
        public static double MaxGainValue = 10.0;

        public static double MinGainValue = -10; // in theory, the minimal of log(gain) can be -∞, while in practice it was rare.

        public VisualGain()
        {
            PropertyChanged += OnVisualGainPropertyChanged;
            Samples.CollectionChanged += OnSamplesCollectionChanged;
            IsResetYAxisAutomatically = true;
            GuideLineSamples = new TransactionObservableCollection<double>();
        }

        /// <summary>
        /// Gets or sets The Gain guideline samples.
        /// </summary>
        public TransactionObservableCollection<double> GuideLineSamples
        {
            get;
            set;
        }

        private void OnVisualGainPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Samples")
            {
                Samples.CollectionChanged += OnSamplesCollectionChanged;
            }
        }

        private void OnSamplesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Reset)
            {
                int nChanged = e.NewItems.Count;
                for (int i = 0; i < nChanged; i++)
                {
                    if (!double.IsNaN(Samples[e.NewStartingIndex + i]))
                    {
                        // Check F0 according to runtime assumption.
                        if (Samples[e.NewStartingIndex + i] > MaxGainValue || Samples[e.NewStartingIndex + i] < MinGainValue)
                        {
                            throw new VisualLinerSamples.InvalidSampleException();
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// The class of DurationDataController.
    /// This class will handle the duration change logic.
    /// </summary>
    public class DurationDataController
    {
        /// <summary>
        /// Visual F0s.
        /// </summary>
        private Collection<double> _f0s;

        /// <summary>
        /// Backup of the original F0s, guideline to detect changes.
        /// </summary>
        private double[] _f0sGuideline;

        /// <summary>
        /// Visual Gains.
        /// </summary>
        private Collection<double> _gains;

        /// <summary>
        /// Backup of the original Gains, guideline to detect changes.
        /// </summary>
        private double[] _gainsGuideline;

        /// <summary>
        /// Visual durations.
        /// </summary>
        private TransactionObservableCollection<double> _durations;

        /// <summary>
        /// Backup of the original durations, guideline to detect the changes.
        /// </summary>
        private double[] _durationsGuideline;

        /// <summary>
        /// Indicate if the guideline is ready or not.
        /// </summary>
        private bool _fNeedResetGuideline = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="DurationDataController"/> class.
        /// </summary>
        public DurationDataController()
        {
            IsBlind = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether blinding property.
        /// </summary>
        public bool IsBlind
        {
            get;
            set;
        }

        /// <summary>
        /// Gets F0s guideline.
        /// </summary>
        public Collection<double> F0sGuideLine
        {
            get { return new Collection<double>(_f0sGuideline); }
        }

        /// <summary>
        /// Gets Gains guideline.
        /// </summary>
        public Collection<double> GainsGuideLine
        {
            get { return new Collection<double>(_gainsGuideline); }
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
                    value.TransactionChangeHandler += OnDurationsTransactionChangeHandler;
                }

                if (_durations != null)
                {
                    _durations.TransactionChangeHandler -= OnDurationsTransactionChangeHandler;
                }

                _durations = value;
            }
        }

        /// <summary>
        /// Gets or sets F0s.
        /// </summary>
        public Collection<double> F0s
        {
            get { return _f0s; }
            set { _f0s = value; }
        }

        /// <summary>
        /// Gets or sets Gains.
        /// </summary>
        public Collection<double> Gains
        {
            get { return _gains; }
            set { _gains = value; }
        }

        /// <summary>
        /// Gets or sets time axis.
        /// </summary>
        public VisualTimeAxis TimeAxis
        {
            get;
            set;
        }

        /// <summary>
        /// Backup original data and setup guidlines.
        /// </summary>
        /// <param name="f0s">F0s.</param>
        /// <param name="gains">Gains.</param>
        /// <param name="durations">Durations.</param>
        public void SetupGuideline(
            Collection<double> f0s, Collection<double> gains, Collection<double> durations)
        {
            _f0sGuideline = new double[f0s.Count];
            f0s.CopyTo(_f0sGuideline, 0);

            _gainsGuideline = new double[gains.Count];
            gains.CopyTo(_gainsGuideline, 0);

            _durationsGuideline = new double[durations.Count];
            durations.CopyTo(_durationsGuideline, 0);

            _fNeedResetGuideline = false;
        }

        /// <summary>
        /// Mark all unchanged F0s to NaN.
        /// </summary>
        /// <param name="f0s">F0s.</param>
        /// <returns>Collection.</returns>
        public Collection<double> MarkUnchangedF0s(Collection<double> f0s)
        {
            Collection<double> mark = new Collection<double>();
            for (int i = 0; i < _f0sGuideline.Length; i++)
            {
                mark.Add(f0s[i]);
            }

            return mark;
        }

        /// <summary>
        /// Mark all unchanged Gains to NaN.
        /// </summary>
        /// <param name="gains">Gains.</param>
        /// <returns>Collection.</returns>
        public Collection<double> MarkUnchangedGains(Collection<double> gains)
        {
            Collection<double> mark = new Collection<double>();
            for (int i = 0; i < _gainsGuideline.Length; i++)
            {
                if (_gainsGuideline[i] == gains[i])
                {
                    mark.Add(double.NaN);
                }
                else
                {
                    mark.Add(gains[i]);
                }
            }

            return mark;
        }

        private void OnDurationsTransactionChangeHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsBlind)
            {
                return;
            }

            // Check if the guide line is ready or not
            if (!_fNeedResetGuideline)
            {
                Collection<double> markedF0s = MarkUnchangedF0s(_f0s);
                Collection<double> markedGains = MarkUnchangedGains(_gains);
                
                // Do scaling based on duration one by one
                int nIndex = 0;
                _f0s.Clear();
                _gains.Clear();
                for (int i = 0; i < _durationsGuideline.Length; i++)
                {
                    int nSamples = (int)Math.Round(_durationsGuideline[i] / TimeAxis.SampleInterval);
                    double ratio = Durations[i] / _durationsGuideline[i];
                    _f0s.Add(markedF0s.Resample(ratio, nIndex, nIndex + nSamples));
                    _gains.Add(markedGains.Resample(ratio, nIndex, nIndex + nSamples));
                    nIndex += nSamples;
                }
            }

            _fNeedResetGuideline = true;
        }
    }

    /// <summary>
    /// Class of VisualAcousticSpace.
    /// </summary>
    public class VisualAcousticSpace
    {
        /// <summary>
        /// The common time axis.
        /// </summary>
        private VisualTimeAxis _timeAxis = new VisualTimeAxis();

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualAcousticSpace"/> class.
        /// </summary>
        public VisualAcousticSpace()
        {
            F0 = new VisualF0();
            Gain = new VisualGain();
            Durations = new VisualDurations();
            WaveForm = new VisualWaveForm();
            WordSegments = new Collection<VisualSegment>();
            PhoneSegments = new Collection<VisualSegment>();
            LinkTimeAxis(F0.TimeAxis);
        }

        /// <summary>
        /// Gets or sets F0.
        /// </summary>
        public VisualF0 F0
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Gain.
        /// </summary>
        public VisualGain Gain
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Duration.
        /// </summary>
        public VisualDurations Durations
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets wave form.
        /// </summary>
        public VisualWaveForm WaveForm
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets word segments.
        /// </summary>
        public Collection<VisualSegment> WordSegments
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets phone segments.
        /// </summary>
        public Collection<VisualSegment> PhoneSegments
        {
            get;
            set;
        }

        /// <summary>
        /// Link all components' time axis to specific time axis.
        /// </summary>
        /// <param name="timeAxis">VisualTimeAxis.</param>
        public void LinkTimeAxis(VisualTimeAxis timeAxis)
        {
            F0.TimeAxis = timeAxis;
            Gain.TimeAxis = timeAxis;
            Durations.TimeAxis = timeAxis;
            using (ViewDataPropertyBinder propertyBinder =
                new ViewDataPropertyBinder(WaveForm.TimeAxis, timeAxis))
            {
                propertyBinder.ExcludedProperties.Add("SampleInterval");
                _timeAxis = timeAxis;
            }
        }

        /// <summary>
        /// Scoll to specific word.
        /// </summary>
        /// <param name="wordIndex">WordIndex.</param>
        public void SetStartingTimeToWord(int wordIndex)
        {
            if (_timeAxis.ZoomMode == ZoomMode.FrameLevel)
            {
                int frameIndex = WordSegments[wordIndex].StartFrameIndex;
                double startingTime = _timeAxis.SampleInterval * frameIndex;
                startingTime = Math.Floor(startingTime / _timeAxis.SampleInterval) * _timeAxis.SampleInterval;
                _timeAxis.StartingTime = startingTime;
            }
        }
    }
}