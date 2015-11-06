//----------------------------------------------------------------------------
// <copyright file="HorizontalScaleBar.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements HorizontalScaleBar
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Globalization;
    using System.Text;
    using System.Windows.Forms;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// Horizontal scale bar.
    /// </summary>
    public partial class HorizontalScaleBar : Control
    {
        #region Fields

        private WaveFile _waveFile;

        private int _sampleOffset;
        private int _sampleLength;

        private bool _movingMode;
        private Point _previousLocation;
        private int _cumulateShift;

        private int[] _units = new int[] { 100, 400, 1000, 2000, 4000, 8000, 16000, 32000 };

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="HorizontalScaleBar"/> class.
        /// </summary>
        public HorizontalScaleBar()
        {
            InitializeComponent();

            ViewShift = delegate
                {
                };
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
        /// Gets Fixed height.
        /// </summary>
        public static int FixedHeight
        {
            get { return 25; }
        }

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
                _sampleOffset = 0;
                _sampleLength = _waveFile.DataIn16Bits.Length;
            }
        }

        /// <summary>
        /// Gets or sets SampleLength.
        /// </summary>
        public int SampleLength
        {
            get { return _sampleLength; }
            set { _sampleLength = value; }
        }

        /// <summary>
        /// Gets or sets SampleOffset.
        /// </summary>
        public int SampleOffset
        {
            get { return _sampleOffset; }
            set { _sampleOffset = value; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Reset.
        /// </summary>
        public void Reset()
        {
            _waveFile = null;

            _sampleOffset = 0;
            _sampleLength = 0;

            _movingMode = false;
            _cumulateShift = 0;
            Invalidate();
        }

        #endregion

        #region Operations

        /// <summary>
        /// Update range.
        /// </summary>
        /// <param name="offset">New sample offset.</param>
        /// <param name="length">New sample length.</param>
        internal void UpdateRange(int offset, int length)
        {
            _sampleOffset = offset;
            _sampleLength = length;

            Invalidate();
        }

        #endregion

        #region Override event handling

        /// <summary>
        /// Overrrid OnMouseDown event of base class.
        /// </summary>
        /// <param name="e">MouseEventArgs.</param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _movingMode = true;
                _cumulateShift = 0;
                Cursor = Cursors.Hand;
                _previousLocation = e.Location;
            }
        }

        /// <summary>
        /// Overrrid OnMouseUp event of base class.
        /// </summary>
        /// <param name="e">MouseEventArgs.</param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            _movingMode = false;
            Cursor = Cursors.Arrow;
            _cumulateShift = 0;
        }

        /// <summary>
        /// Overrrid OnMouseMove event of base class.
        /// </summary>
        /// <param name="e">MouseEventArgs.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_movingMode && e.Button == MouseButtons.Left)
            {
                int shiftX = _previousLocation.X - e.X;

                int sampleShift = (int)((float)shiftX / ClientRectangle.Width * SampleLength);
                sampleShift -= _cumulateShift;
                ViewShift(this, new ViewShiftEventArgs(sampleShift));
                _cumulateShift += sampleShift;
            }
        }

        /// <summary>
        /// Override OnPaint event of base class.
        /// </summary>
        /// <param name="e">PaintEventArgs.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle rect = e.ClipRectangle;
            rect.Inflate(-1, -1);
            e.Graphics.DrawRectangle(Pens.GreenYellow, rect);
            e.Graphics.FillRectangle(Brushes.LightBlue, rect);

            if (_waveFile != null)
            {
                int unitStep = PredictSuitableUnitLevel(rect.Width);
                int stepX = (int)(rect.Width / ((float)SampleLength / unitStep));

                using (Font font =
                    new Font("Microsoft Sans Serif", 6.5F, FontStyle.Regular, GraphicsUnit.Point))
                {
                    int mark = _sampleOffset;
                    for (int x = 0; x < rect.Width; x += stepX)
                    {
                        e.Graphics.DrawLine(Pens.Red, x, 0, x, 3);
                        float timestamp = (float)mark / _waveFile.Format.SamplesPerSecond;
                        string label = timestamp.ToString("F3", CultureInfo.InvariantCulture);

                        SizeF sz = e.Graphics.MeasureString(label, font);
                        if (x - (sz.Width / 2) >= 0)
                        {
                            e.Graphics.DrawString(label, font, Brushes.Black,
                                x - (sz.Width / 2), 4);
                        }

                        mark += unitStep;
                    }
                }
            }

            base.OnPaint(e);
        }

        /// <summary>
        /// Override OnResize event of base class.
        /// </summary>
        /// <param name="e">EventArgs.</param>
        protected override void OnResize(EventArgs e)
        {
            this.Invalidate();
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Predict stuitable unit level.
        /// </summary>
        /// <param name="width">Total width.</param>
        /// <returns>Unit level.</returns>
        private int PredictSuitableUnitLevel(int width)
        {
            foreach (int level in _units)
            {
                int step = (int)(width / ((float)SampleLength / level));
                if (step > 40)
                {
                    return level;
                }
            }

            return _units[_units.Length - 1];
        }

        #endregion
    }
}