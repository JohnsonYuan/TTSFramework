//----------------------------------------------------------------------------
// <copyright file="WaveView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements WaveView
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.IO;
    using System.Text;
    using System.Windows.Forms;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// WaveView.
    /// </summary>
    public partial class WaveView : Control
    {
        #region Const variables

        /// <summary>
        /// The bigger the worse, the samllest is 1.
        /// </summary>
        private const int PictureQuality = 1;

        private const int CacheMapWidth = 4096;
        private const int CacheMapHeight = 480;

        #endregion

        #region Fields

        private bool _spectrum;
        private bool _showMarkLabel = true;

        private WaveFile _waveFile;

        private Collection<TimeMark> _timeMarks = new Collection<TimeMark>();

        private float _zoomX = 1.0f;
        private float _positionRatioX;

        private int _viewSampleOffset;
        private int _viewSampleLength;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveView"/> class.
        /// </summary>
        public WaveView()
        {
            InitializeComponent();
            ViewUpdated = delegate
                {
                };
        }

        #endregion

        #region Events

        /// <summary>
        /// View updated event.
        /// </summary>
        public event EventHandler<ViewUpdateEventArgs> ViewUpdated;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether ShowMarkLabel.
        /// </summary>
        public bool ShowMarkLabel
        {
            get { return _showMarkLabel; }
            set { _showMarkLabel = value; }
        }

        /// <summary>
        /// Gets or sets CenterRatioX.
        /// </summary>
        public float PositionRatioX
        {
            get
            {
                return _positionRatioX;
            }

            set
            {
                 _positionRatioX = value;
                 UpdateViewSampleWindow();
            }
        }

        /// <summary>
        /// Gets or sets ZoomX.
        /// </summary>
        public float ZoomX
        {
            get
            {
                return _zoomX;
            }

            set
            {
                _zoomX = Math.Min(value, 20);
                _zoomX = Math.Max(_zoomX, 1);

                UpdateViewSampleWindow();

                Invalidate();
            }
        }

        /// <summary>
        /// Gets TimeMarks.
        /// </summary>
        public Collection<TimeMark> TimeMarks
        {
            get { return _timeMarks; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether IsSpectrum.
        /// </summary>
        public bool IsSpectrum
        {
            get { return _spectrum; }
            set { _spectrum = value; }
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

                if (_waveFile != value)
                {
                    // wavefile data changed
                    _waveFile = value;

                    int[] range = EstimateViewWindow();
                    if (range != null)
                    {
                        int offset = range[0];
                        int length = range[1];

                        PositionRatioX = (float)offset / (_waveFile.DataIn16Bits.Length - length);
                        ZoomX = _waveFile.DataIn16Bits.Length / length;
                    }

                    // update view request
                    Invalidate();
                }
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
            _timeMarks.Clear();
            _zoomX = 1.0f;
            _positionRatioX = 0;

            _viewSampleOffset = 0;
            _viewSampleLength = 0;

            Invalidate();
        }

        /// <summary>
        /// Shift.
        /// </summary>
        /// <param name="sampleShift">Sample number to shift.</param>
        public void Shift(int sampleShift)
        {
            if (_waveFile == null)
            {
                return;
            }

            _viewSampleOffset += sampleShift;
            _viewSampleOffset = Math.Min(_waveFile.DataIn16Bits.Length - _viewSampleLength,
                _viewSampleOffset);
            _viewSampleOffset = Math.Max(0, _viewSampleOffset);

            System.Diagnostics.Trace.WriteLine(_viewSampleLength);
            Invalidate();
        }

        #endregion

        #region Control events

        /// <summary>
        /// Override OnPaint.
        /// </summary>
        /// <param name="e">PaintEventArgs.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (_waveFile == null)
            {
                return;
            }

            Bitmap cachedMap = null;

            ViewUpdated(this, new ViewUpdateEventArgs(_waveFile.DataIn16Bits.Length,
                _viewSampleOffset, _viewSampleLength));

            if (_spectrum)
            {
                cachedMap = DrawSpectrum(_viewSampleOffset, _viewSampleLength, e.ClipRectangle);
            }
            else
            {
                cachedMap = DrawWaveform(_viewSampleOffset, _viewSampleLength, e.ClipRectangle);
            }

            if (cachedMap == null)
            {
                return;
            }

            e.Graphics.DrawImage(cachedMap, 0, 0);
            cachedMap.Dispose();

            DrawTimeMarks(e.Graphics, e.ClipRectangle);

            base.OnPaint(e);
        }

        /// <summary>
        /// Override Resize.
        /// </summary>
        /// <param name="e">EventArgs.</param>
        protected override void OnResize(EventArgs e)
        {
            this.Invalidate();
            base.OnResize(e);
        }

        /// <summary>
        /// Update view sample window.
        /// </summary>
        private void UpdateViewSampleWindow()
        {
            if (_waveFile == null)
            {
                return;
            }

            // _prevCenterRatioX * (1 - 1.0f / _prevScaleX) + (float)_centerRatioX / _prevScaleX;
            float currentRatio = _positionRatioX;

            _viewSampleLength = (int)(_waveFile.DataIn16Bits.Length / _zoomX);
            _viewSampleOffset = (int)((currentRatio * _waveFile.DataIn16Bits.Length) -
                (currentRatio * _viewSampleLength));
            _viewSampleLength -= _viewSampleLength % Fft.WindowIncrement;
            _viewSampleOffset -= _viewSampleOffset % Fft.WindowIncrement;
        }

        /// <summary>
        /// Draw time marks.
        /// </summary>
        /// <param name="graphics">Graphics.</param>
        /// <param name="clip">Clip.</param>
        private void DrawTimeMarks(Graphics graphics, Rectangle clip)
        {
            float offsetY = 4;

            using (Font font = new Font("Microsoft Sans Serif", 7.5F, FontStyle.Regular, GraphicsUnit.Point))
            {
                foreach (TimeMark timeMark in _timeMarks)
                {
                    int sampleOffset = (int)(timeMark.Offset * _waveFile.Format.SamplesPerSecond);
                    int offsetX = (int)((float)(sampleOffset - _viewSampleOffset) /
                        _viewSampleLength * clip.Width);

                    graphics.DrawLine(timeMark.Pen, offsetX, 0, offsetX, clip.Height);

                    if (ShowMarkLabel)
                    {
                        SizeF sz = graphics.MeasureString(timeMark.Label, font);
                        graphics.DrawString(timeMark.Label, font, Brushes.Black, offsetX + 2, offsetY);
                        offsetY += sz.Height;
                    }
                }
            }
        }

        /// <summary>
        /// Draw waveform view.
        /// </summary>
        /// <param name="sampleOffset">Sample offset.</param>
        /// <param name="sampleLength">Sample length.</param>
        /// <param name="clip">Clip.</param>
        /// <returns>Bitmap created.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        private Bitmap DrawWaveform(int sampleOffset, int sampleLength, Rectangle clip)
        {
            ArbgPixel waveColor = new ArbgPixel(255, 0, 255, 0);

            if (_waveFile == null)
            {
                return null;
            }

            if (clip.Width == 0 || clip.Height == 0)
            {
                return null;
            }

            Bitmap waveform = new Bitmap(clip.Width, clip.Height);

            using (UnsafeBitmap unsafeMap = new UnsafeBitmap(waveform))
            {
                Graphics graphics = Graphics.FromImage(unsafeMap.Bitmap);
                graphics.FillRectangle(Brushes.WhiteSmoke, graphics.VisibleClipBounds);

                unsafeMap.LockBitmap();

                float stepY = (float)clip.Height / 2 / short.MaxValue;
                float stepX = (float)clip.Width / sampleLength;

                //// #region Draw middle line

                int middleY = (int)clip.Height / 2;

                for (int x = 0; x < clip.Width; x++)
                {
                    unsafeMap.SetAt(x, middleY, 0, 255, 0, 0);
                }

                //// #endregion

                short[] data = _waveFile.DataIn16Bits;
                int sampleEndIndex = sampleOffset + sampleLength;

                Point prevPoint = new Point(0, middleY);
                Point currPoint = new Point(0, middleY);

                int prevY = int.MinValue;
                for (int sampleIndex = sampleOffset; sampleIndex < sampleEndIndex; sampleIndex++)
                {
                    currPoint.X = (int)(((sampleIndex - sampleOffset) * stepX) + 0.5);
                    currPoint.Y = middleY - (int)((data[sampleIndex] * stepY) + 0.5);

                    unsafeMap.SetAt((int)currPoint.X, (int)currPoint.Y, waveColor);

                    if (currPoint.X != prevPoint.X)
                    {
                        // connect two points in line
                        float tag = (float)(currPoint.Y - prevPoint.Y) / (currPoint.X - prevPoint.X);

                        for (int interX = prevPoint.X; interX < currPoint.X; interX++)
                        {
                            int interY = (int)(((interX - prevPoint.X) * tag) + 0.5) + prevPoint.Y;

                            if (prevY != interY && prevY != int.MinValue)
                            {
                                int insertDirect = Math.Sign(interY - prevY);
                                int insertLength = Math.Abs(interY - prevY);
                                for (int insertY = 1; insertY < insertLength; insertY++)
                                {
                                    unsafeMap.SetAt(interX, prevY + (insertY * insertDirect), waveColor);
                                }
                            }

                            unsafeMap.SetAt(interX, interY, waveColor);

                            prevY = interY;
                        }
                    }
                    else
                    {
                        if (prevPoint.Y != currPoint.Y)
                        {
                            int insertDirect = Math.Sign(currPoint.Y - prevPoint.Y);
                            int insertLength = Math.Abs(currPoint.Y - prevPoint.Y);
                            for (int insertY = 1; insertY < insertLength; insertY++)
                            {
                                unsafeMap.SetAt(prevPoint.X, prevPoint.Y + (insertY * insertDirect),
                                    waveColor);
                            }
                        }
                    }

                    prevPoint = currPoint;
                }

                unsafeMap.UnlockBitmap();
                waveform = unsafeMap.Detach();
            }

            return waveform;
        }

        /// <summary>
        /// Draw spectrum view.
        /// </summary>
        /// <param name="smapleOffset">Sample offset.</param>
        /// <param name="sampleLength">Sample length.</param>
        /// <param name="clip">Clip.</param>
        /// <returns>Bitmap created.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        private Bitmap DrawSpectrum(int smapleOffset, int sampleLength, Rectangle clip)
        {
            if (_waveFile == null)
            {
                return null;
            }

            if (clip.Width == 0 || clip.Height == 0)
            {
                return null;
            }

            Bitmap spectrum = new Bitmap(clip.Width, clip.Height);

            using (UnsafeBitmap unsafeMap = new UnsafeBitmap(spectrum))
            {
                Graphics graphics = Graphics.FromImage(unsafeMap.Bitmap);
                graphics.FillRectangle(Brushes.WhiteSmoke, graphics.VisibleClipBounds);

                int windowCount = (sampleLength - Fft.WindowSize) / Fft.WindowIncrement;
                int windowOffset = smapleOffset / Fft.WindowIncrement;
                int windowEndIndex = windowOffset + windowCount;

                unsafeMap.LockBitmap();

                float stepX = (float)clip.Width / windowCount;
                float stepY = (float)clip.Height / Fft.FftWidth;

                float offsetX = stepX / 2;
                for (int windowIndex = windowOffset; windowIndex < windowEndIndex; windowIndex++)
                {
                    float[] data = _waveFile.Spectrum[windowIndex];
                    if (data == null)
                    {
                        System.Diagnostics.Debug.Assert(false);
                        continue;
                    }

                    int length = data.Length;

                    float x = (windowIndex - windowOffset) * stepX;

                    for (int j = 0; j < length; ++j)
                    {
                        float value = data[j];
                        float y = ((length - j) * stepY) - 1;

                        int i = (int)(value / 18);
                        i = (i > 256) ? 255 : i;

                        byte color = (byte)(128 - (i / 2));
                        for (float rx = -stepX / 2; rx < stepX / 2; rx++)
                        {
                            for (float ry = -stepY / 2; ry < stepY / 2; ry++)
                            {
                                int px = (int)(x + rx + offsetX);
                                int py = (int)(y + ry);
                                double distance = Math.Sqrt((rx * rx) + (ry * ry));
                                byte alpha = (distance > 256) ? (byte)255 : (byte)distance;
                                if (px >= 0 && px < clip.Width
                                    && py >= 0 && py < clip.Height)
                                {
                                    unsafeMap.SetAt((int)px, (int)py, (byte)i, color, 0, alpha);
                                }
                            }
                        }

                        unsafeMap.SetAt((int)(x + offsetX), (int)y, (byte)i, (byte)(128 - (i / 2)), 0, 0);
                    }
                }

                unsafeMap.UnlockBitmap();

                spectrum = unsafeMap.Detach();
            }

            return spectrum;
        }

        /// <summary>
        /// Estimate the view window.
        /// </summary>
        /// <returns>Int [] {offset, length}.</returns>
        private int[] EstimateViewWindow()
        {
            if (_waveFile == null)
            {
                return null;
            }

            int min = int.MaxValue;
            int max = int.MinValue;
            foreach (TimeMark mark in _timeMarks)
            {
                int temp = (int)(Math.Max(mark.Offset, 0) * _waveFile.Format.SamplesPerSecond);
                min = Math.Min(min, temp);

                temp = (int)(Math.Min(mark.Offset, _waveFile.Duration) * _waveFile.Format.SamplesPerSecond);
                max = Math.Max(max, temp);
            }

            int pendingSample = (int)(0.3f * _waveFile.Format.SamplesPerSecond);
            min = Math.Max(min - pendingSample, 0);
            max = Math.Min(max + pendingSample, _waveFile.DataIn16Bits.Length);
            return new int[] { min, max - min };
        }
        #endregion
    }
}