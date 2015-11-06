//----------------------------------------------------------------------------
// <copyright file="ReportWriter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     report writer manages end to end reporting
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// A report writer class.
    /// </summary>
    public class ReportWriter : IDisposable
    {
        #region private members
        private TextWriter _writer;
        private Collection<string> _fieldNames = new Collection<string>();
        private Collection<string> _fieldValues = new Collection<string>();
        private StringBuilder _details = new StringBuilder();
        private string _summaryLine = "Summary";
        private string _detailLine = "Details";
        #endregion 

        #region constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ReportWriter"/> class.
        /// </summary>
        /// <param name="reportFile">Report file.</param>
        public ReportWriter(string reportFile)
        {
            _writer = new StreamWriter(reportFile, false, Encoding.Unicode);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportWriter"/> class.
        /// </summary>
        /// <param name="textWriter">Text writer.</param>
        public ReportWriter(TextWriter textWriter)
        {
            _writer = textWriter;
        }
        #endregion

        #region properties
        /// <summary>
        /// Gets or sets Line of summary.
        /// </summary>
        public string SummaryLine
        {
            get { return _summaryLine; }
            set { _summaryLine = value; }
        }

        #endregion

        #region public methods
        /// <summary>
        /// Write a summar field.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="value">Value.</param>
        public void WriteSummaryField(string name, object value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            _fieldNames.Add(name);
            _fieldValues.Add(value.ToString());
        }

        /// <summary>
        /// Write detail lines.
        /// </summary>
        /// <param name="format">Format.</param>
        /// <param name="arg">Arg.</param>
        public void WriteDetailLine(string format, params object[] arg)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            _details.AppendLine(Helper.NeutralFormat(format, arg));
        }

        /// <summary>
        /// Flush to output media.
        /// </summary>
        public void Flush()
        {
            Debug.Assert(_writer != null);
            _writer.WriteLine("{0}:", SummaryLine);

            Debug.Assert(_fieldNames.Count == _fieldValues.Count);
            for (int i = 0; i < _fieldNames.Count; i++)
            {
                _writer.WriteLine("\t{0}: {1}", _fieldNames[i], _fieldValues[i]);
            }

            _writer.WriteLine("\n{0}:", _detailLine);
            _writer.WriteLine(_details.ToString());
            _writer.Flush();
        }

        /// <summary>
        /// Dispose resource.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose this object.
        /// </summary>
        /// <param name="disposing">Flag indicating whether delete unmanaged resource.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_writer != null)
                {
                    _writer.Close();
                }
            }
        }
    }
    #endregion
}