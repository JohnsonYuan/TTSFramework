//----------------------------------------------------------------------------
// <copyright file="SingleTrajectoryGraph.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     The code logic of SingleTrajectoryGraph
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using Data;

    /// <summary>
    /// Interaction logic for SingleTrajectoryGraph.xaml.
    /// </summary>
    public partial class SingleTrajectoryGraph : UserControl
    {
        #region fields

        /// <summary>
        /// Graph view data.
        /// </summary>
        private VisualSingleTrajectory _trajectoryData = null;

        /// <summary>
        /// Display contoller.
        /// </summary>
        private VisualTrajectoryDisplayController _displayController;

        #endregion

        #region contructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleTrajectoryGraph"/> class.
        /// </summary>
        public SingleTrajectoryGraph()
        {
            InitializeComponent();
            DisplayController = new VisualTrajectoryDisplayController();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets ParameterGraph.
        /// </summary>
        public IntervalLinerGraph ParameterGraph
        {
            get
            {
                return _parameterGraph;
            }
        }

        /// <summary>
        /// Gets AuxiliaryParameterGraph.
        /// </summary>
        public IntervalLinerGraph AuxiliaryParameterGraph
        {
            get
            {
                return _auxiliaryParameterGraph;
            }
        }

        /// <summary>
        /// Gets MeansGraph.
        /// </summary>
        public IntervalConstantGraph MeansGraph
        {
            get
            {
                return _meanGraph;
            }
        }

        /// <summary>
        /// Gets or sets the display controller.
        /// </summary>
        public VisualTrajectoryDisplayController DisplayController
        {
            get
            { 
                return _displayController; 
            }

            set
            {
                if (value != null)
                {
                    value.PropertyChanged += OnDisplayPropertyChanged;
                }

                if (_displayController != null)
                {
                    _displayController.PropertyChanged -= OnDisplayPropertyChanged;
                }

                _displayController = value;
                OnDisplayPropertyChanged(this, new PropertyChangedEventArgs("DisplayController"));
            }
        }

        #endregion

        #region methods

        /// <summary>
        /// Set data context.
        /// </summary>
        /// <param name="trajectoryData">Trajectory data.</param>
        public void SetDataContext(VisualSingleTrajectory trajectoryData)
        {
            DataContext = trajectoryData;

            VisualConstantSamples visualConstantSamples = new VisualConstantSamples();
            visualConstantSamples.Samples = trajectoryData.Means;
            visualConstantSamples.Deviations = trajectoryData.StandardDeviations;
            visualConstantSamples.TimeAxis = trajectoryData.TimeAxis;
            visualConstantSamples.YAxis = trajectoryData.YAxis;
            _meanGraph.SetDataContext(visualConstantSamples);

            VisualLinerSamples visualLinerSamples = new VisualLinerSamples();
            visualLinerSamples.Samples = trajectoryData.AuxiliaryParameters;
            visualLinerSamples.TimeAxis = trajectoryData.TimeAxis;
            visualLinerSamples.YAxis = trajectoryData.YAxis;
            _auxiliaryParameterGraph.SetDataContext(visualLinerSamples);

            visualLinerSamples = new VisualLinerSamples();
            visualLinerSamples.Samples = trajectoryData.GeneratedParameters;
            visualLinerSamples.TimeAxis = trajectoryData.TimeAxis;
            visualLinerSamples.YAxis = trajectoryData.YAxis;
            _parameterGraph.SetDataContext(visualLinerSamples);

            _hightlighFrames.SelectedFrames = trajectoryData.SelectedFrameIndexes;
            _hightlighFrames.HighlightFrames = trajectoryData.HoverFrameIndexes;
            _hightlighFrames.TimeAxis = trajectoryData.TimeAxis;

            _frameline.TimeAxis = trajectoryData.TimeAxis;

            _trajectoryData = trajectoryData;
        }

        private void OnDisplayPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _meanGraph.Visibility = _displayController.ModelDataShowed ? Visibility.Visible : Visibility.Hidden;
            _parameterGraph.Visibility = _displayController.TrajectoryShowed ? Visibility.Visible : Visibility.Hidden;
            _auxiliaryParameterGraph.Visibility = _displayController.AuxiliaryTrajectoryShowed ? Visibility.Visible : Visibility.Hidden;
        }

        #endregion
    }
}