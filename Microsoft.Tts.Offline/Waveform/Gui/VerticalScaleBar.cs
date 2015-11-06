//----------------------------------------------------------------------------
// <copyright file="VerticalScaleBar.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements VerticalScaleBar
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

    /// <summary>
    /// Vertical scale bar.
    /// </summary>
    public partial class VerticalScaleBar : Control
    {
        #region Fields

        private bool _spectrum;

        private int[] _spectrumUnits = new int[] { 1000, 2000, 4000 };
        private int[] _waveformUnits = new int[] { 10000, 20000, 30000 };

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="VerticalScaleBar"/> class.
        /// </summary>
        public VerticalScaleBar()
        {
            InitializeComponent();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Fixed width.
        /// </summary>
        public static int FixedWidth
        {
            get { return 45; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Is spectrum view.
        /// </summary>
        public bool IsSpectrum
        {
            get { return _spectrum; }
            set { _spectrum = value; }
        }

        #endregion

        #region Override event handling

        /// <summary>
        /// Overrrid OnPaint event of base class.
        /// </summary>
        /// <param name="e">PaintEventArgs.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle rect = e.ClipRectangle;
            rect.Inflate(-1, -1);
            e.Graphics.DrawRectangle(Pens.LightSalmon, rect);
            e.Graphics.FillRectangle(Brushes.LightBlue, rect);

            if (IsSpectrum)
            {
                DrawSpectrumScale(e, rect);
            }
            else
            {
                DrawWaveformScale(e, rect);
            }

            base.OnPaint(e);
        }

        /// <summary>
        /// Overrrid OnResize event of base class.
        /// </summary>
        /// <param name="e">EventArgs.</param>
        protected override void OnResize(EventArgs e)
        {
            this.Invalidate();
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Draw waveform scale.
        /// </summary>
        /// <param name="pe">Event.</param>
        /// <param name="rect">Region to draw on.</param>
        private void DrawWaveformScale(PaintEventArgs pe, Rectangle rect)
        {
            int unitStep = PredictWaveformUnit(rect.Height / 2);
            int stepY = (int)(rect.Height / 2 / ((float)short.MaxValue / unitStep));

            using (Font font = new Font("Microsoft Sans Serif", 6.5F, FontStyle.Regular, GraphicsUnit.Point))
            {
                int mark = 0;
                for (int y = 0; y < rect.Height / 2; y += stepY)
                {
                    pe.Graphics.DrawLine(Pens.Red, 0, (rect.Height / 2) - y, 3, (rect.Height / 2) - y);
                    pe.Graphics.DrawLine(Pens.Red, 0, (rect.Height / 2) + y, 3, (rect.Height / 2) + y);

                    string label = mark.ToString(CultureInfo.InvariantCulture);
                    SizeF sz = pe.Graphics.MeasureString(label, font);
                    if ((rect.Height / 2) - y - (sz.Height / 2) >= 0)
                    {
                        pe.Graphics.DrawString(label, font, Brushes.Black,
                            4, (rect.Height / 2) - y - (sz.Height / 2));
                    }

                    label = (-mark).ToString(CultureInfo.InvariantCulture);
                    sz = pe.Graphics.MeasureString(label, font);
                    if ((rect.Height / 2) + y - (sz.Height / 2) >= 0)
                    {
                        pe.Graphics.DrawString(label, font, Brushes.Black,
                            4, (rect.Height / 2) + y - (sz.Height / 2));
                    }

                    mark += unitStep;
                }
            }
        }

        /// <summary>
        /// Draw spectrum scale.
        /// </summary>
        /// <param name="pe">Event.</param>
        /// <param name="rect">Region to draw on.</param>
        private void DrawSpectrumScale(PaintEventArgs pe, Rectangle rect)
        {
            int unitStep = PredictSpectrumUnit(rect.Height);
            int stepY = (int)(rect.Height / ((float)8000 / unitStep));

            using (Font font = new Font("Microsoft Sans Serif", 6.5F, FontStyle.Regular, GraphicsUnit.Point))
            {
                int mark = 0;
                for (int y = 0; y < rect.Height; y += stepY)
                {
                    pe.Graphics.DrawLine(Pens.Red, 0, rect.Height - y, 3, rect.Height - y);
                    string label = mark.ToString(CultureInfo.InvariantCulture);

                    SizeF sz = pe.Graphics.MeasureString(label, font);
                    if (y - (sz.Height / 2) >= 0)
                    {
                        pe.Graphics.DrawString(label, font, Brushes.Black,
                            4, rect.Height - y - (sz.Height / 2));
                    }

                    mark += unitStep;
                }
            }
        }

        /// <summary>
        /// Predict spectrum unit.
        /// </summary>
        /// <param name="width">Width of the pexil.</param>
        /// <returns>Spectrul unit.</returns>
        private int PredictSpectrumUnit(int width)
        {
            foreach (int level in _spectrumUnits)
            {
                int step = (int)(width / ((float)8000 / level));
                if (step > 20)
                {
                    return level;
                }
            }

            return _spectrumUnits[_spectrumUnits.Length - 1];
        }

        /// <summary>
        /// Predict Waveform Unit.
        /// </summary>
        /// <param name="width">Width of the pexil.</param>
        /// <returns>Waveform unit.</returns>
        private int PredictWaveformUnit(int width)
        {
            foreach (int level in _waveformUnits)
            {
                int step = (int)(width / ((float)short.MaxValue / level));
                if (step > 20)
                {
                    return level;
                }
            }

            return _waveformUnits[_waveformUnits.Length - 1];
        }

        #endregion
    }
}