//----------------------------------------------------------------------------
// <copyright file="RewindableTextReader.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Rewindable TextReader
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// The class to represent Rewindable TextReader.
    /// </summary>
    public class RewindableTextReader : IDisposable
    {
        #region Fields

        /// <summary>
        /// The composited text reader instance.
        /// </summary>
        private TextReader _reader;

        /// <summary>
        /// The cached line of string for next read line operation.
        /// </summary>
        private string _cachedLine;

        /// <summary>
        /// Flag to indicate whether return cached data or not.
        /// </summary>
        private bool _rewindable;

        #endregion

        /// <summary>
        /// Initializes a new instance of the RewindableTextReader class.
        /// </summary>
        /// <param name="reader">The reader instance to use for reading.</param>
        public RewindableTextReader(TextReader reader)
        {
            Helper.ThrowIfNull(reader);

            _reader = reader;
        }

        /// <summary>
        /// Closes the System.IO.TextReader and releases any system resources associated with the TextReader.
        /// </summary>
        public void Close()
        {
            _reader.Close();
        }

        /// <summary>
        /// Peek a line of characters from the current stream and returns the data as a string.
        /// </summary>
        /// <returns>The next line from the input stream, or null if all characters have been read.</returns>
        public string PeekLine()
        {
            string line = ReadLine();
            RewindLine();
            return line;
        }

        /// <summary>
        /// Reads a line of characters from the current stream and returns the data as a string.
        /// </summary>
        /// <returns>The next line from the input stream, or null if all characters have been read.</returns>
        public string ReadLine()
        {
            if (!_rewindable)
            {
                _cachedLine = _reader.ReadLine();
            }

            _rewindable = false;
            return _cachedLine;
        }

        /// <summary>
        /// Rewinds a line of characters which will be re-return from ReadLine.
        /// </summary>
        public void RewindLine()
        {
            if (_rewindable)
            {
                throw new NotSupportedException("Only single line rewinding is supported");
            }

            _rewindable = true;
        }

        /// <summary>
        /// Reads lines from the text reader and stop if the indicator is found.
        /// </summary>
        /// <param name="stoppers">The indicators to stop current read operation.</param>
        /// <param name="inclusive">Flag to indicate whether including the line of indicator.</param>
        /// <returns>Lines read from the stream.</returns>
        public IEnumerable<string> ReadLines(IEnumerable<string> stoppers, bool inclusive)
        {
            Helper.ThrowIfNull(stoppers);

            string line = null;
            while ((line = ReadLine()) != null)
            {
                if (stoppers.Any(s => line.StartsWith(s)))
                {
                    if (inclusive)
                    {
                        yield return line;
                    }
                    else
                    {
                        RewindLine();
                    }

                    break;
                }

                yield return line;
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Disposes the resources used in this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);         
        }

        /// <summary>
        /// Releases the unmanaged resources used by the RewindableTextReader.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources;
        /// False to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_reader != null)
                {
                    _reader.Dispose();
                }
            }
        }

        #endregion
    }
}