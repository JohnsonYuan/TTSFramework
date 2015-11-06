//----------------------------------------------------------------------------
// <copyright file="TimeMark.cs" company="MICROSOFT">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements TimeMark
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Drawing;
    using System.IO;
    using System.Text;

    /// <summary>
    /// TimeMark.
    /// </summary>
    public class TimeMark : IDisposable
    {
        #region Fields

        private float _offset;
        private Pen _pen;
        private string _label;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeMark"/> class.
        /// </summary>
        /// <param name="label">Label of this time mark.</param>
        /// <param name="offset">Sample offset.</param>
        /// <param name="pen">Pen to draw this mark.</param>
        public TimeMark(string label, float offset, Pen pen)
        {
            if (string.IsNullOrEmpty(label))
            {
                throw new ArgumentNullException("label");
            }

            if (pen == null)
            {
                throw new ArgumentNullException("pen");
            }

            _label = label;
            _offset = offset;
            _pen = pen;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Label.
        /// </summary>
        public string Label
        {
            get
            {
                return _label;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _label = value;
            }
        }

        /// <summary>
        /// Gets or sets Pen.
        /// </summary>
        public Pen Pen
        {
            get
            {
                return _pen;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _pen = value;
            }
        }

        /// <summary>
        /// Gets or sets Offset.
        /// </summary>
        public float Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Releases all resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by this instance.
        /// </summary>
        /// <param name="disposing">Release unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_pen != null)
                {
                    _pen.Dispose();
                }
            }
        }

        #endregion
    }
}