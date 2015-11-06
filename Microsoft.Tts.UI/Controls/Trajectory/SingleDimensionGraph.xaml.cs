//----------------------------------------------------------------------------
// <copyright file="SingleDimensionGraph.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     The code logic of SingleDimensionGraph
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory
{
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using Data;

    /// <summary>
    /// Interaction logic for SingleDimensionGraph.xaml.
    /// </summary>
    public partial class SingleDimensionGraph : UserControl
    {
        #region fields

        /// <summary>
        /// View data.
        /// </summary>
        private VisualDimensionGraph _viewData = null;

        /// <summary>
        /// The trajectory info shown in the popup UI.
        /// </summary>
        private VisualTrajectoryInfo _trajectoryInfo = new VisualTrajectoryInfo();

        /// <summary>
        /// Tracking the mouse events from those controls on which the popup UI will display.
        /// </summary>
        private Collection<FrameworkElement> _mouseTrackedControls = new Collection<FrameworkElement>();
        private int _trackingCount = 0;
        private VisualSingleTrajectory _trackingTrajectoryData;

        #endregion

        #region contructor

        /// <summary>
        /// Initializes a new instance of the SingleDimensionGraph class.
        /// </summary>
        public SingleDimensionGraph()
        {
            InitializeComponent();
            AddMouseTracking(_staticGraph.ParameterGraph);
            AddMouseTracking(_staticGraph.MeansGraph);
            AddMouseTracking(_deltaGraph.ParameterGraph);
            AddMouseTracking(_deltaGraph.MeansGraph);
            AddMouseTracking(_accelerationGraph.ParameterGraph);
            AddMouseTracking(_accelerationGraph.MeansGraph);
        }

        #endregion

        #region methods

        /// <summary>
        /// Set data context.
        /// </summary>
        /// <param name="dataContext">Data context.</param>
        public void SetDataContext(VisualDimensionGraph dataContext)
        {
            Debug.Assert(dataContext != null, "Can't set a null data context to SingleDimensionGraph.");
            DataContext = dataContext;

            _controlPanel.SetDataContext(dataContext);

            dataContext.DisplayController.PropertyChanged +=
                new PropertyChangedEventHandler(OnGraphVisibilityChanged);

            ObservableCollection<int> hoverFrames = new ObservableCollection<int>();
            Debug.Assert(dataContext.StaticTrajectory is VisualSingleTrajectory,
                "SingleDimensionGraph must be bind to VisualSingleTrajectory");
            VisualSingleTrajectory visualSingleTrajectory = dataContext.StaticTrajectory as VisualSingleTrajectory;
            visualSingleTrajectory.SelectedFrameIndexes = dataContext.SelectedFrameIndexes;
            visualSingleTrajectory.HoverFrameIndexes = hoverFrames;
            _staticGraph.SetDataContext(visualSingleTrajectory);
            _staticGraph.DisplayController = dataContext.DisplayController.TrajectoryDisplayController;

            Debug.Assert(dataContext.DeltaTrajectory is VisualSingleTrajectory,
                "SingleDimensionGraph must be bind to VisualSingleTrajectory");
            visualSingleTrajectory = dataContext.DeltaTrajectory as VisualSingleTrajectory;
            visualSingleTrajectory.SelectedFrameIndexes = dataContext.SelectedFrameIndexes;
            visualSingleTrajectory.HoverFrameIndexes = hoverFrames;
            _deltaGraph.SetDataContext(visualSingleTrajectory);
            _deltaGraph.DisplayController = dataContext.DisplayController.TrajectoryDisplayController;

            Debug.Assert(dataContext.AccelerationTrajectory is VisualSingleTrajectory,
                "SingleDimensionGraph must be bind to VisualSingleTrajectory");
            visualSingleTrajectory = dataContext.AccelerationTrajectory as VisualSingleTrajectory;
            visualSingleTrajectory.SelectedFrameIndexes = dataContext.SelectedFrameIndexes;
            visualSingleTrajectory.HoverFrameIndexes = hoverFrames;
            _accelerationGraph.SetDataContext(visualSingleTrajectory);
            _accelerationGraph.DisplayController = dataContext.DisplayController.TrajectoryDisplayController;

            _waveformGraph.SetDataContext(dataContext.WaveSamples);

            _durationGraph.SetDataContext(dataContext.PhoneDurations);

            _wordSegmentGraph.Segments = dataContext.WordSegments;
            _wordSegmentGraph.TimeAxis = dataContext.TimeAxis;

            _phoneSegmentGraph.Segments = dataContext.PhoneSegments;
            _phoneSegmentGraph.TimeAxis = dataContext.TimeAxis;

            _trajectoryInfoTable.DataContext = _trajectoryInfo;

            _viewData = dataContext;

            Debug.Assert(dataContext.StaticTrajectory.YAxis != null, "YAxis of statistic trajectory should not be null!");
            _staticYAxis.SetDataContext(dataContext.StaticTrajectory.YAxis);
            Debug.Assert(dataContext.DeltaTrajectory.YAxis != null, "YAxis of delta trajectory should not be null!");
            _deltaYAxis.SetDataContext(dataContext.DeltaTrajectory.YAxis);
            Debug.Assert(dataContext.AccelerationTrajectory.YAxis != null, "YAxis of acceleration trajectory should not be null!");
            _accelerationYAxis.SetDataContext(dataContext.AccelerationTrajectory.YAxis);
            Debug.Assert(dataContext.WaveSamples.YAxis != null, "YAxis of wave form should not be null!");
            _waveformYAxis.SetDataContext(dataContext.WaveSamples.YAxis);
        }

        /// <summary>
        /// Scroll to word.
        /// </summary>
        /// <param name="wordIndex">Word index.</param>
        public void ScrollToWord(int wordIndex)
        {
            Debug.Assert(wordIndex >= 0, "Word index should be larger or equal to zero.");
            _viewData.SetStartingTimeToWord(wordIndex, _staticGraph.ActualWidth);
        }

        /// <summary>
        /// Reset control panel.
        /// </summary>
        public void ResetControlPanel()
        {
            _controlPanel.ScrollBar.ResetScrollar();
        }

        /// <summary>
        /// On graph size changed.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OnGraphSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _controlPanel.ScrollBar.ResetScrollar();
        }

        /// <summary>
        /// On graph visibility changed.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OnGraphVisibilityChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("DeltaShowed") ||
                e.PropertyName.Equals("AccelerationShowed") ||
                e.PropertyName.Equals("WaveformShowed"))
            {
                if (_viewData.DisplayController.DeltaShowed)
                {
                    _deltaGraphRow.Height = new GridLength(1.0, GridUnitType.Star);
                    _deltaGraphRowPadding.Height = new GridLength(5, GridUnitType.Pixel);
                }
                else
                {
                    _deltaGraphRow.Height = new GridLength(0.0, GridUnitType.Pixel);
                    _deltaGraphRowPadding.Height = new GridLength(0, GridUnitType.Pixel);
                }

                if (_viewData.DisplayController.AccelerationShowed)
                {
                    _accelerationGraphRow.Height = new GridLength(1.0, GridUnitType.Star);
                    _accelerationGraphRowPadding.Height = new GridLength(5, GridUnitType.Pixel);
                }
                else
                {
                    _accelerationGraphRow.Height = new GridLength(0.0, GridUnitType.Pixel);
                    _accelerationGraphRowPadding.Height = new GridLength(0, GridUnitType.Pixel);
                }

                if (_viewData.DisplayController.WaveformShowed)
                {
                    _waveformGraphRow.Height = new GridLength(1.0, GridUnitType.Star);
                    _waveformGraphRowPadding.Height = new GridLength(5, GridUnitType.Pixel);
                }
                else
                {
                    _waveformGraphRow.Height = new GridLength(0.0, GridUnitType.Pixel);
                    _waveformGraphRowPadding.Height = new GridLength(0, GridUnitType.Pixel);
                }
            }
        }

        private void AddMouseTracking(FrameworkElement trackedControl)
        {
            trackedControl.MouseEnter += new MouseEventHandler(OnTrackedControlMouseEnter);
            trackedControl.MouseLeave += new MouseEventHandler(OnTrackedControlMouseLeave);
            trackedControl.MouseMove += new MouseEventHandler(OnTrackedControlMouseMove);
        }

        private void OnTrackedControlMouseMove(object sender, MouseEventArgs e)
        {
            if (_trackingTrajectoryData != null)
            {
                Point mousePoint = e.GetPosition((IInputElement)sender);
                TrajectoryHelper.BuildTrajectoryInfo(_viewData, mousePoint.X,
                    _trackingTrajectoryData.GeneratedParameters, _trackingTrajectoryData.AuxiliaryParameters,
                    _trackingTrajectoryData.Means, _trackingTrajectoryData.StandardDeviations, _trajectoryInfo);
            }
        }

        private void OnTrackedControlMouseLeave(object sender, MouseEventArgs e)
        {
            _trackingCount--;
            if (_trackingCount == 0)
            {
                _dataPopup.IsOpen = false;
            }
        }

        private void OnTrackedControlMouseEnter(object sender, MouseEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)((FrameworkElement)sender).Parent;
            while (element != null)
            {
                SingleTrajectoryGraph singleTrajectory = element as SingleTrajectoryGraph;
                if (singleTrajectory != null)
                {
                    _trackingTrajectoryData = (VisualSingleTrajectory)singleTrajectory.DataContext;
                }

                element = (FrameworkElement)element.Parent;
            }

            _trackingCount++;
            _dataPopup.IsOpen = true;
        }

        #endregion
    }
}