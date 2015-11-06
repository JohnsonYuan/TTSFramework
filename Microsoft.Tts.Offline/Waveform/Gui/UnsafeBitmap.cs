//----------------------------------------------------------------------------
// <copyright file="UnsafeBitmap.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements UnsafeBitmap
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Security.Permissions;

    /// <summary>
    /// Arbg (Alpha Red Green Blue) Pixel.
    /// </summary>
    internal struct ArbgPixel
    {
        #region Fields

        internal byte Blue;
        internal byte Green;
        internal byte Red;
        internal byte Alpha;

        #endregion

        #region Contruction

        /// <summary>
        /// Initializes a new instance of the <see cref="ArbgPixel"/> struct.
        /// </summary>
        /// <param name="alpha">Alpha.</param>
        /// <param name="red">Red.</param>
        /// <param name="green">Green.</param>
        /// <param name="blue">Blue.</param>
        internal ArbgPixel(byte alpha, byte red, byte green, byte blue)
        {
            Alpha = alpha;
            Red = red;
            Green = green;
            Blue = blue;
        }

        #endregion

        #region Public static operations

        /// <summary>
        /// Operator ==.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public static bool operator ==(ArbgPixel left, ArbgPixel right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Operator !=.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if not equal, otherwise false.</returns>
        public static bool operator !=(ArbgPixel left, ArbgPixel right)
        {
            return !left.Equals(right);
        }

        #endregion

        #region Operations

        /// <summary>
        /// Test equal with other instance.
        /// </summary>
        /// <param name="obj">Other instance.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is ArbgPixel))
            {
                return false;
            }

            ArbgPixel other = (ArbgPixel)obj;
            return Alpha == other.Alpha && Red == other.Red &&
                Green == other.Green && Blue == other.Blue;
        }

        /// <summary>
        /// Get hash code.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }

    /// <summary>
    /// Unsafe Bitmap.
    /// </summary>
    /// <SecurityNote>
    ///     Critical: This code accesses an unsafe code block
    /// </SecurityNote>
    [System.Security.SecurityCritical]
    internal unsafe class UnsafeBitmap : IDisposable
    {
        #region Fields

        private Bitmap _bitmap;
        private BitmapData _bitmapData;
        private int _width;
        private byte* _base;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsafeBitmap"/> class.
        /// </summary>
        /// <param name="bitmap">Bitmap.</param>
        internal UnsafeBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException("bitmap");
            }

            _bitmap = bitmap;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Bitmap.
        /// </summary>
        internal Bitmap Bitmap
        {
            get { return _bitmap; }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Releases all resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Lock bitmap.
        /// </summary>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        internal void LockBitmap()
        {
            GraphicsUnit unit = GraphicsUnit.Pixel;
            RectangleF boundsF = _bitmap.GetBounds(ref unit);
            Rectangle bounds = new Rectangle((int)boundsF.X, (int)boundsF.Y,
                (int)boundsF.Width, (int)boundsF.Height);

            _width = (int)boundsF.Width * sizeof(ArbgPixel);
            _bitmapData = _bitmap.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            _base = (byte*)_bitmapData.Scan0.ToPointer();
        }

        /// <summary>
        /// Set ARBG at given point.
        /// </summary>
        /// <param name="x">X.</param>
        /// <param name="y">Y.</param>
        /// <param name="alpha">Alpha.</param>
        /// <param name="red">Red.</param>
        /// <param name="green">Green.</param>
        /// <param name="blue">Blue.</param>
        internal void SetAt(int x, int y, byte alpha, byte red, byte green, byte blue)
        {
            System.Diagnostics.Debug.Assert(_base != null);
            System.Diagnostics.Debug.Assert(_width > 0);

            ArbgPixel* pixel = (ArbgPixel*)(_base + (y * _width) + (x * sizeof(ArbgPixel)));

            pixel->Alpha = alpha;
            pixel->Red = red;
            pixel->Green = green;
            pixel->Blue = blue;
        }

        /// <summary>
        /// Set ArbgPixel at given point.
        /// </summary>
        /// <param name="x">X.</param>
        /// <param name="y">Y.</param>
        /// <param name="waveColor">Color.</param>
        internal void SetAt(int x, int y, ArbgPixel waveColor)
        {
            System.Diagnostics.Debug.Assert(_base != null);
            System.Diagnostics.Debug.Assert(_width > 0);

            ArbgPixel* pixel = (ArbgPixel*)(_base + (y * _width) + (x * sizeof(ArbgPixel)));

            *pixel = waveColor;
        }

        /// <summary>
        /// Unlock bitmap.
        /// </summary>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        internal void UnlockBitmap()
        {
            _bitmap.UnlockBits(_bitmapData);
            _bitmapData = null;
            _base = null;
        }

        /// <summary>
        /// Detach.
        /// </summary>
        /// <returns>Bitmap.</returns>
        internal Bitmap Detach()
        {
            System.Diagnostics.Debug.Assert(_bitmapData == null);
            System.Diagnostics.Debug.Assert(_base == null);

            Bitmap cache = _bitmap;
            _bitmap = null;

            return cache;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Releases all resources used by this instance.
        /// </summary>
        /// <param name="disposing">Release unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_bitmap != null)
                {
                    _bitmap.Dispose();
                }
            }
        }

        #endregion
    }
}