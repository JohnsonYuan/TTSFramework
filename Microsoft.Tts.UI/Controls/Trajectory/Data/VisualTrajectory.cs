//----------------------------------------------------------------------------
// <copyright file="VisualTrajectory.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      class of visual trajectory related.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.UI.Controls.Data;
    using Microsoft.Tts.UI.Controls.Trajectory;

    /// <summary>
    /// Abstract class of VisualTrajectory.
    /// </summary>
    public abstract class VisualTrajectoryBase : ViewDataBase
    {
        #region fields

        private VisualTimeAxis _timeAxis;

        private VisualValueAxis _yAxis;

        #endregion

        /// <summary>
        /// Initializes a new instance of the VisualTrajectoryBase class.
        /// </summary>
        public VisualTrajectoryBase()
        {
            YAxis = new VisualValueAxis();
            TimeAxis = new VisualTimeAxis();
            IsResetTimeAxisAutomatically = false;
            IsResetYAxisAutomatically = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether reset the time axis according to the data changes.
        /// </summary>
        public bool IsResetTimeAxisAutomatically
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether reset the value axis according to the data changes.
        /// </summary>
        public bool IsResetYAxisAutomatically
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Y-axis view data.
        /// </summary>
        public VisualValueAxis YAxis
        {
            get 
            { 
                return _yAxis; 
            }

            set
            {
                if (_yAxis != null)
                {
                    _yAxis.PropertyChanged -= OnYAxisPropertyChanged;
                }

                if (value != null)
                {
                    value.PropertyChanged += OnYAxisPropertyChanged;
                }

                _yAxis = value;
                NotifyPropertyChanged("YAxis");
            }
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
        
        #region methods

        /// <summary>
        /// Calculate value range. It's a virtual method.
        /// </summary>
        public virtual void CalculateValueRange()
        {
        }

        /// <summary>
        /// Clear the data.
        /// </summary>
        public virtual void Clear()
        {
        }

        /// <summary>
        /// Judge if the value is out of valid range.
        /// </summary>
        /// <param name="value">Value to judge.</param>
        /// <returns>True if valid.</returns>
        protected static bool IsValidValue(double value)
        {
            return value > IntervalLinerGraph.DefaultLowerBound &&
                value < IntervalLinerGraph.DefaultUpperBound;
        }

        #endregion

        private void OnYAxisPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("YAxis");
        }

        private void OnTimeAxisPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("TimeAxis");
        }
    }

    /// <summary>
    /// Class of VisualMultiTrajectory.
    /// </summary>
    public class VisualMultiTrajectory : VisualTrajectoryBase
    {
        #region contructor

        /// <summary>
        /// Initializes a new instance of the VisualMultiTrajectory class.
        /// </summary>
        /// <param name="dimensions">Dimensions of the VisualMultiTrajectory.</param>
        public VisualMultiTrajectory(ObservableCollection<int> dimensions)
            : base()
        {
            Helper.ThrowIfNull(dimensions);
            Trajectories = new List<VisualSingleTrajectory>();
            SelectedDimensions = dimensions;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets trajectories. VisualMultiTrajectory contains a collection of single trajectories.
        /// </summary>
        public List<VisualSingleTrajectory> Trajectories { get; private set; }

        /// <summary>
        /// Gets selected dimension list.
        /// </summary>
        public ObservableCollection<int> SelectedDimensions { get; private set; }

        #endregion

        #region methods

        /// <summary>
        /// Link all single trajectory's time and value axis.
        /// </summary>
        public void LinkAxis()
        {
            foreach (VisualSingleTrajectory st in Trajectories)
            {
                st.TimeAxis = TimeAxis;
                st.YAxis = YAxis;
            }
        }

        /// <summary>
        /// Calculate value range.
        /// </summary>
        public override void CalculateValueRange()
        {
            CalculateValueRange(null);
        }

        /// <summary>
        /// Calculate value range of specific dimensions.
        /// </summary>
        /// <param name="dimensions">Dimensions specified. Null means no specific dimension.</param>
        public void CalculateValueRange(Collection<int> dimensions)
        {
            double maxValue = IntervalLinerGraph.DefaultLowerBound;
            double minValue = IntervalLinerGraph.DefaultUpperBound;
            bool updated = false;

            for (int index = 0; index < Trajectories.Count; ++index)
            {
                if (dimensions == null || dimensions.Contains(index + 1))
                {
                    updated = true;
                    Trajectories[index].CalculateValueRange();
                    maxValue = Math.Max(maxValue, Trajectories[index].YAxis.ValueRange.Max);
                    minValue = Math.Min(minValue, Trajectories[index].YAxis.ValueRange.Min);
                }
            }

            if (updated)
            {
                YAxis.Reset(minValue, maxValue);
            }
        }

        /// <summary>
        /// Clear data.
        /// </summary>
        public override void Clear()
        {
            Trajectories.Clear();
        }

        #endregion
    }

    /// <summary>
    /// Class of VisualDisplayController.
    /// </summary>
    public class VisualTrajectoryDisplayController : ViewDataBase
    {
        #region fields

        /// <summary>
        /// Indicates whether to show auxiliary trajectory.
        /// </summary>
        private bool _auxiliaryTrajectoryShowed;

        /// <summary>
        /// Indicates whether to show trajectory.
        /// </summary>
        private bool _trajectoryShowed = true;

        /// <summary>
        /// Indicates whether to show model data.
        /// </summary>
        private bool _modelDataShowed = true;

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets a value indicating whether to show trajectory.
        /// </summary>
        public bool TrajectoryShowed
        {
            get
            {
                return _trajectoryShowed;
            }

            set
            {
                _trajectoryShowed = value;
                NotifyPropertyChanged("TrajectoryShowed");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show auxiliary trajectory.
        /// </summary>
        public bool AuxiliaryTrajectoryShowed
        {
            get
            {
                return _auxiliaryTrajectoryShowed;
            }

            set
            {
                _auxiliaryTrajectoryShowed = value;
                NotifyPropertyChanged("AuxiliaryTrajectoryShowed");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show model data.
        /// </summary>
        public bool ModelDataShowed
        {
            get
            {
                return _modelDataShowed;
            }

            set
            {
                _modelDataShowed = value;
                NotifyPropertyChanged("ModelDataShowed");
            }
        }

        #endregion
    }

    /// <summary>
    /// Class of VisualSingleTrajectory.
    /// </summary>
    public class VisualSingleTrajectory : VisualTrajectoryBase
    {
        #region constructor

        /// <summary>
        /// Initializes a new instance of the VisualSingleTrajectory class.
        /// </summary>
        public VisualSingleTrajectory()
            : base()
        {
            GeneratedParameters = new TransactionObservableCollection<double>();
            AuxiliaryParameters = new TransactionObservableCollection<double>();
            Means = new TransactionObservableCollection<double>();
            StandardDeviations = new TransactionObservableCollection<double>();
            SelectedFrameIndexes = new ObservableCollection<int>();
            HoverFrameIndexes = new ObservableCollection<int>();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets generated parameter values.
        /// </summary>
        public TransactionObservableCollection<double> GeneratedParameters { get; private set; }

        /// <summary>
        /// Gets auxiliary parameter values.
        /// </summary>
        public TransactionObservableCollection<double> AuxiliaryParameters { get; private set; }

        /// <summary>
        /// Gets mean values.
        /// </summary>
        public TransactionObservableCollection<double> Means { get; private set; }

        /// <summary>
        /// Gets standard deviation values.
        /// </summary>
        public TransactionObservableCollection<double> StandardDeviations { get; private set; }

        /// <summary>
        /// Gets or sets Selected frame index.
        /// </summary>
        public ObservableCollection<int> SelectedFrameIndexes { get; set; }

        /// <summary>
        /// Gets or sets Hover frame index.
        /// </summary>
        public ObservableCollection<int> HoverFrameIndexes { get; set; }

        #endregion

        #region methods

        /// <summary>
        /// Calculate value range.
        /// </summary>
        public override void CalculateValueRange()
        {
            Debug.Assert((GeneratedParameters.Count == 0 || GeneratedParameters.Count == Means.Count) &&
                (AuxiliaryParameters.Count == 0 || GeneratedParameters.Count == AuxiliaryParameters.Count) &&
                Means.Count == StandardDeviations.Count, 
                "The number of GeneratedParameters shall be zero or equal to mean and deviation");

            double maxValue = IntervalLinerGraph.DefaultLowerBound;
            double minValue = IntervalLinerGraph.DefaultUpperBound;
            bool updated = false;

            for (int index = 0; index < Means.Count; ++index)
            {
                if (GeneratedParameters.Count > 0 && 
                    VisualTrajectoryBase.IsValidValue(GeneratedParameters[index]))
                {
                    maxValue = Math.Max(maxValue, GeneratedParameters[index]);
                    minValue = Math.Min(minValue, GeneratedParameters[index]);
                    updated = true;
                }

                if (AuxiliaryParameters.Count > 0 &&
                    VisualTrajectoryBase.IsValidValue(AuxiliaryParameters[index]))
                {
                    maxValue = Math.Max(maxValue, AuxiliaryParameters[index]);
                    minValue = Math.Min(minValue, AuxiliaryParameters[index]);
                    updated = true;
                }
            }

            if (updated)
            {
                YAxis.Reset(minValue, maxValue);
            }
        }

        /// <summary>
        /// Clear data.
        /// </summary>
        public override void Clear()
        {
            GeneratedParameters.Clear();
            AuxiliaryParameters.Clear();
            Means.Clear();
            StandardDeviations.Clear();
        }

        #endregion
    }
}