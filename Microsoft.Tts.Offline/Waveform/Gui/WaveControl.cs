//----------------------------------------------------------------------------
// <copyright file="WaveControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements WaveControl
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Text;
    using System.Windows.Forms;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// WaveControl.
    /// </summary>
    public partial class WaveControl : UserControl
    {
        #region Fields

        private WaveFile _waveFile;

        private bool _showHorizontalScaleBar;
        private bool _showVerticalScaleBar = true;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveControl"/> class.
        /// </summary>
        public WaveControl()
        {
            InitializeComponent();

            ViewShift = delegate
                {
                };

            int minWidth = 200 + VerticalScaleBar.FixedWidth;
            int minHeight = 60 + HorizontalScaleBar.FixedHeight;

            MinimumSize = new Size(minWidth, minHeight);

            _waveformView.ViewUpdated += new EventHandler<ViewUpdateEventArgs>(OnWaveformViewUpdated);
            _horScaleBar.ViewShift += new EventHandler<ViewShiftEventArgs>(OnHorScaleBarViewShift);
            UpdateSize();
        }

        #endregion

        #region Events

        /// <summary>
        /// View shifted event.
        /// </summary>
        public event EventHandler<ViewShiftEventArgs> ViewShift;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets WaveFile.
        /// </summary>
        public WaveFile WaveFile
        {
            get
            {
                return _waveFile;
            }

            set 
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _waveFile = value;
                _waveformView.WaveFile = _waveFile;
                _horScaleBar.WaveFile = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Gets WaveformView.
        /// </summary>
        public WaveView WaveformView
        {
            get { return _waveformView; }
        }

        /// <summary>
        /// Gets VerticalScaleBar.
        /// </summary>
        public VerticalScaleBar VerticalScaleBar
        {
            get { return _verScaleBar; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether ShowVerScaleBar.
        /// </summary>
        public bool ShowVerticalScaleBar
        {
            get { return _showVerticalScaleBar; }
            set { _showVerticalScaleBar = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether ShowVerScaleBar.
        /// </summary>
        public bool ShowHorizontalScaleBar
        {
            get
            {
                return _showHorizontalScaleBar;
            }

            set
            {
                _showHorizontalScaleBar = value;
                UpdateSize();
            }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Reset.
        /// </summary>
        public void Reset()
        {
            _waveFile = null;
            _waveformView.Reset();
            _horScaleBar.Reset();
            Invalidate();
        }

        #endregion

        #region Override event handling

        /// <summary>
        /// Overrrid OnResize event of base class.
        /// </summary>
        /// <param name="e">EventArgs.</param>
        protected override void OnResize(EventArgs e)
        {
            UpdateSize();
        }

        #endregion

        #region Event handlers

        /// <summary>
        /// Handle horizon scale bar view shift.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">View shift event.</param>
        private void OnHorScaleBarViewShift(object sender, ViewShiftEventArgs e)
        {
            _waveformView.Shift(e.SampleShift);
            ViewShift(this, e);
        }

        /// <summary>
        /// Handle waveform view updated.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">View update event.</param>
        private void OnWaveformViewUpdated(object sender, ViewUpdateEventArgs e)
        {
            _horScaleBar.UpdateRange(e.ViewOffset, e.ViewLength);
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Update size.
        /// </summary>
        private void UpdateSize()
        {
            Rectangle rect = ClientRectangle;

            int waveviewWidth = _showVerticalScaleBar ?
                rect.Width - VerticalScaleBar.FixedWidth : rect.Width;
            int waveviewHeight = _showHorizontalScaleBar ?
                rect.Height - HorizontalScaleBar.FixedHeight : rect.Height;

            _waveformView.Bounds = new Rectangle(rect.Left, rect.Top,
                    waveviewWidth, waveviewHeight);

            _horScaleBar.Bounds = new Rectangle(_waveformView.Left, _waveformView.Bottom,
                _waveformView.Width, HorizontalScaleBar.FixedHeight);

            _verScaleBar.Bounds = new Rectangle(_waveformView.Right, _waveformView.Top,
                VerticalScaleBar.FixedWidth, _waveformView.Height);

            Invalidate();
        }

        #endregion
    }
}