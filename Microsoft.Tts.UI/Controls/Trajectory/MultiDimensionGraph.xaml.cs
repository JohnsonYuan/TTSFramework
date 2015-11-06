//----------------------------------------------------------------------------
// <copyright file="MultiDimensionGraph.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     The code logic of MultiDimensionGraph
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using Data;

    /// <summary>
    /// Interaction logic for MultiDimensionGraph.xaml.
    /// </summary>
    public partial class MultiDimensionGraph : UserControl, IMultiFrameControler
    {
        #region fields

        /// <summary>
        /// View data.
        /// </summary>
        private VisualDimensionGraph _viewData = null;

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the MultiDimensionGraph class.
        /// </summary>
        public MultiDimensionGraph()
        {
            InitializeComponent();
            MultiSelectedFrames = new ObservableCollection<int>();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets multi selected frames.
        /// </summary>
        public ObservableCollection<int> MultiSelectedFrames { get; private set; }

        #endregion

        #region methods

        /// <summary>
        /// Set data context.
        /// </summary>
        /// <param name="dataContext">Data context.</param>
        public void SetDataContext(VisualDimensionGraph dataContext)
        {
            Debug.Assert(dataContext != null,
                "Can not assign a null data context to MultiDimensionGraph.");

            // set data context of control panels.
            _controlPanel.SetDataContext(dataContext);
            _controlPanel._waveformCheckBox.IsEnabled = false;
            dataContext.DisplayController.WaveformShowed = false;

            ObservableCollection<int> hoverFrames = new ObservableCollection<int>();

            // Set data context of sub graphs.
            Debug.Assert(dataContext.StaticTrajectory is VisualMultiTrajectory,
                "MultiDimensionGraph must be bind to VisualMultiTrajectory");
            _staticGraph.SetDataContext(dataContext,
                dataContext.StaticTrajectory as VisualMultiTrajectory, hoverFrames);
            Debug.Assert(dataContext.DeltaTrajectory is VisualMultiTrajectory,
                "MultiDimensionGraph must be bind to VisualMultiTrajectory");
            _deltaGraph.SetDataContext(dataContext,
                dataContext.DeltaTrajectory as VisualMultiTrajectory, hoverFrames);
            Debug.Assert(dataContext.AccelerationTrajectory is VisualMultiTrajectory,
                "MultiDimensionGraph must be bind to VisualMultiTrajectory");
            _accelerationGraph.SetDataContext(dataContext,
                dataContext.AccelerationTrajectory as VisualMultiTrajectory, hoverFrames);

            // set data context of segment graphs.
            _wordSegmentGraph.Segments = dataContext.WordSegments;
            _wordSegmentGraph.TimeAxis = dataContext.TimeAxis;
            _phoneSegmentGraph.Segments = dataContext.PhoneSegments;
            _phoneSegmentGraph.TimeAxis = dataContext.TimeAxis;

            _staticGraph.SetMultiSelectedFrames(MultiSelectedFrames);
            _deltaGraph.SetMultiSelectedFrames(MultiSelectedFrames);
            _accelerationGraph.SetMultiSelectedFrames(MultiSelectedFrames);

            _dimensionRuler.DataContext = dataContext;

            _errorDetectionPanel.SetData(dataContext, this);

            _viewData = dataContext;

            dataContext.DisplayController.PropertyChanged +=
                new PropertyChangedEventHandler(OnGraphVisibilityChanged);
            dataContext.DisplayController.TrajectoryDisplayController.ModelDataShowed = false;
            dataContext.DisplayController.DeltaShowed = false;
            dataContext.DisplayController.AccelerationShowed = false;

            Debug.Assert(dataContext.StaticTrajectory.YAxis != null, "YAxis of statistic trajectory should not be null!");
            _staticYAxis.SetDataContext(dataContext.StaticTrajectory.YAxis);
            Debug.Assert(dataContext.DeltaTrajectory.YAxis != null, "YAxis of delta trajectory should not be null!");
            _deltaYAxis.SetDataContext(dataContext.DeltaTrajectory.YAxis);
            Debug.Assert(dataContext.AccelerationTrajectory.YAxis != null, "YAxis of acceleration trajectory should not be null!");
            _accelerationYAxis.SetDataContext(dataContext.AccelerationTrajectory.YAxis);
        }

        /// <summary>
        /// Scroll to specific word.
        /// </summary>
        /// <param name="wordIndex">Index of the word.</param>
        public void ScrollToWord(int wordIndex)
        {
            Debug.Assert(wordIndex >= 0, "Word index should be larger or equal to zero.");
            _viewData.SetStartingTimeToWord(wordIndex, _staticGraph.ActualWidth);
        }

        /// <summary>
        /// Scroll to frame.
        /// </summary>
        /// <param name="frameIndex">Frame index.</param>
        public void ScrollToFrame(int frameIndex)
        {
            Debug.Assert(frameIndex >= 0, "Frame index should be larger or equal to zero.");
            _viewData.SetStartingTimeToFrame(frameIndex, _staticGraph.ActualWidth);
        }

        /// <summary>
        /// Reset control panel.
        /// </summary>
        public void ResetControlPanel()
        {
            VisualMultiTrajectory traj = _viewData.StaticTrajectory as VisualMultiTrajectory;
            _dimensionRuler.Reset(traj.Trajectories.Count);
            _controlPanel.ScrollBar.ResetScrollar();
        }

        /// <summary>
        /// Create trajectories.
        /// </summary>
        public void CreateTrajectories()
        {
            _staticGraph.CreateTrajectories();
            _deltaGraph.CreateTrajectories();
            _accelerationGraph.CreateTrajectories();
        }

        /// <summary>
        /// Event handler when graph size is changed.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OnGraphSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _controlPanel.ScrollBar.ResetScrollar();
        }

        /// <summary>
        /// Event handler when graph visibility changed.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OnGraphVisibilityChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("DeltaShowed") || e.PropertyName.Equals("AccelerationShowed"))
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
            }
        }

        #endregion
    }
}