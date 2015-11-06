//----------------------------------------------------------------------------
// <copyright file="LspErrorDetectionPanel.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     The code logic of LspErrorDectectionPanel
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
    using System.Windows.Documents;
    using Data;

    /// <summary>
    /// Interaction logic for LspErrorDectectionUserControl.xaml.
    /// </summary>
    public partial class LspErrorDetectionPanel : UserControl, IDisposable
    {
        #region fields

        private VisualLspErrorData _errorData = new VisualLspErrorData();
        private VisualMultiTrajectory _trajectoryData = null;
        private VisualDimensionGraph _graphData = null;
        private BackgroundWorker _loadDataWorker = null;
        private IMultiFrameControler _parent = null;
        private ObservableCollection<int> _multiHighlightFrames = new ObservableCollection<int>();

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="LspErrorDetectionPanel"/> class.
        /// </summary>
        public LspErrorDetectionPanel()
        {
            InitializeComponent();
            DataContext = _errorData;
            InitializeBackgroundWorker();
        }

        #endregion

        #region methods

        /// <summary>
        /// Set data.
        /// </summary>
        /// <param name="data">Graph data.</param>
        /// <param name="parent">Parent graph.</param>
        public void SetData(VisualDimensionGraph data, IMultiFrameControler parent)
        {
            Debug.Assert(data != null && parent != null);
            _graphData = data;
            _parent = parent;
            Debug.Assert(data.StaticTrajectory is VisualMultiTrajectory);
            _trajectoryData = data.StaticTrajectory as VisualMultiTrajectory;
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_loadDataWorker != null)
                {
                    _loadDataWorker.Dispose();
                }
            }
        }

        /// <summary>
        /// On find button click event handler.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OnFindButtonClick(object sender, RoutedEventArgs e)
        {
            int interval = -1;
            double threshold = double.NaN;
            bool succeeded = int.TryParse(_dimensionIntervalTextBox.Text, out interval);
            if (!succeeded)
            {
                _errorTextBlock.Text = "Interval must be integer";
            }

            if (succeeded)
            {
                _errorData.DimensionInterval = interval;
                if (!string.IsNullOrEmpty(_minThresholdTextBox.Text))
                {
                    succeeded = double.TryParse(_minThresholdTextBox.Text, out threshold);
                    if (!succeeded)
                    {
                        _errorTextBlock.Text = "Min threshold must be integer";
                    }
                }
            }

            if (succeeded)
            {
                _errorData.MinThreshold = threshold;
                threshold = double.NaN;
                if (!string.IsNullOrEmpty(_maxThresholdTextBox.Text))
                {
                    succeeded = double.TryParse(_maxThresholdTextBox.Text, out threshold);
                    if (!succeeded)
                    {
                        _errorTextBlock.Text = "Max threshold must be integer";
                    }
                }
            }

            if (succeeded)
            {
                _errorData.MaxThreshold = threshold;
                _graphData.SelectedFrameIndexes.Clear();
                _errorData.LoadingData = true;
                _loadDataWorker.RunWorkerAsync();
            }
        }

        /// <summary>
        /// On error data selection changed event handler.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OnErrorDataSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.Assert(e != null && e.AddedItems != null);

            if (e.AddedItems.Count == 1)
            {
                Debug.Assert(e.AddedItems[0] is VisualLspErrorData.VisualErrorData);
                VisualLspErrorData.VisualErrorData errorData =
                    e.AddedItems[0] as VisualLspErrorData.VisualErrorData;
                _graphData.SelectedDimensions.Clear();
                _graphData.SelectedDimensions.Add(errorData.StartDimensionIndex);
                _graphData.SelectedDimensions.Add(errorData.EndDimensionIndex);

                _graphData.SelectedFrameIndexes.Clear();
                _graphData.SelectedFrameIndexes.Add(errorData.FrameIndex);

                _parent.ScrollToFrame(errorData.FrameIndex);
            }
        }

        /// <summary>
        /// Initialize background worker.
        /// </summary>
        private void InitializeBackgroundWorker()
        {
            _loadDataWorker = new BackgroundWorker();
            _loadDataWorker.DoWork += (sender, e) =>
            {
                Debug.Assert(_errorData != null);
                _multiHighlightFrames.Clear();
                _errorData.BuildErrorData(_trajectoryData.Trajectories);
            };

            _loadDataWorker.RunWorkerCompleted += (sender, e) =>
            {
                _graphData.SelectedFrameIndexes.Clear();
                _graphData.SelectedDimensions.Clear();
                _findButton.IsEnabled = true;
                _errorData.LoadingData = false;

                if (e.Error == null)
                {
                    _errorData.FetchErrorData("1");
                    _parent.MultiSelectedFrames.Clear();
                    _errorData.ErrorFrames.ForEach(index =>
                        _parent.MultiSelectedFrames.Add(index));
                    VisualMultiTrajectory traj = _graphData.StaticTrajectory as VisualMultiTrajectory;
                    for (int i = 1; i <= traj.Trajectories.Count; ++i)
                    {
                        _graphData.SelectedDimensions.Add(i);
                    }

                    _errorTextBlock.Text = null;
                }
                else
                {
                    _errorTextBlock.Text = e.Error.Message;
                }
            };
        }

        /// <summary>
        /// On page navigate event handler.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OnPageNavigate(object sender, RoutedEventArgs e)
        {
            Debug.Assert(e.Source is Hyperlink);
            _errorData.FetchErrorData((e.Source as Hyperlink).TargetName);
        }

        /// <summary>
        /// On reset button click event handler.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OnResetButtonClick(object sender, RoutedEventArgs e)
        {
            _errorData.LoadingData = false;
            _graphData.SelectedFrameIndexes.Clear();
            _findButton.IsEnabled = true;
            _errorData.Reset();
            _parent.MultiSelectedFrames.Clear();
        }

        #endregion
    }
}