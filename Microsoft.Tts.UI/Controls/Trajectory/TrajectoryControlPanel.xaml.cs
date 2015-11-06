//----------------------------------------------------------------------------
// <copyright file="TrajectoryControlPanel.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     The code logic of TrajectoryControlPanel
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls.Trajectory
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using Data;

    /// <summary>
    /// Interaction logic for TrajectoryControlPanel.xaml.
    /// </summary>
    public partial class TrajectoryControlPanel : UserControl
    {
        #region fields

        /// <summary>
        /// Time axis view data.
        /// </summary>
        private VisualTimeAxis _timeAxisViewData = null;

        /// <summary>
        /// Display view data.
        /// </summary>
        private VisualDisplayController _displayViewData = null;

        private bool _cachedModelDataShowed;
        private bool _cachedDeltaShowed;
        private bool _cachedAccelerationShowed;

        #endregion

        #region contructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TrajectoryControlPanel"/> class.
        /// </summary>
        public TrajectoryControlPanel()
        {
            InitializeComponent();
        }

        #endregion

        /// <summary>
        /// Gets the control panel's scroll bar.
        /// </summary>
        public TimeAxisScrollbar ScrollBar
        {
            get { return _timeScrollBar; }
        }

        #region methods

        /// <summary>
        /// Set data context.
        /// </summary>
        /// <param name="dataContext">Data context.</param>
        public void SetDataContext(VisualDimensionGraph dataContext)
        {
            Debug.Assert(dataContext != null,
                "Can't assign null data context to TrajectoryControlPanel.");

            DataContext = dataContext.DisplayController;
            _displayViewData = dataContext.DisplayController;
            _timeScrollBar.SetDataContext(dataContext.TimeAxis);
            _zoomControl.SetDataContext(dataContext.TimeAxis);
            _timeAxisViewData = dataContext.TimeAxis;
            dataContext.TimeAxis.PropertyChanged += new PropertyChangedEventHandler(TimeAxis_PropertyChanged);
        }

        private void TimeAxis_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ZoomMode")
            {
                SetZoomMode();
            }
        }

        /// <summary>
        /// Set zoom mode.
        /// </summary>
        private void SetZoomMode()
        {
            if (_timeAxisViewData.ZoomMode == ZoomMode.WholePicture)
            {
                _cachedModelDataShowed = _displayViewData.TrajectoryDisplayController.ModelDataShowed;
                _cachedDeltaShowed = _displayViewData.DeltaShowed;
                _cachedAccelerationShowed = _displayViewData.AccelerationShowed;

                // disables the visibility of frame line, model data, delta and acceleration.
                _timeAxisViewData.FramelineShowed = false;
                _displayViewData.TrajectoryDisplayController.ModelDataShowed = false;
                _displayViewData.DeltaShowed = false;
                _displayViewData.AccelerationShowed = false;

                _showModelDataCheckBox.IsEnabled = false;
                _showDeltaCheckBox.IsEnabled = false;
                _showAccelerationCheckBox.IsEnabled = false;
            }
            else
            {
                _showModelDataCheckBox.IsEnabled = true;
                _showDeltaCheckBox.IsEnabled = true;
                _showAccelerationCheckBox.IsEnabled = true;

                _displayViewData.TrajectoryDisplayController.ModelDataShowed = _cachedModelDataShowed;
                _displayViewData.DeltaShowed = _cachedDeltaShowed;
                _displayViewData.AccelerationShowed = _cachedAccelerationShowed;
            }
        }

        #endregion
    }
}