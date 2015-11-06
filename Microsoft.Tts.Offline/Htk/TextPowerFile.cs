// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextPowerFile.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//   This module defines a common library to manipulate text power file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// The class to manipulate the text power file.
    /// </summary>
    public class TextPowerFile
    {
        #region Private fileds
        /// <summary>
        /// The file name of this power file.
        /// </summary>
        private string _fileName;

        /// <summary>
        /// The data to store the data of power file.
        /// </summary>
        private List<float> _data;

        #endregion

        #region Properties

        /// <summary>
        /// Gets data of the power file.
        /// </summary>
        public List<float> Data
        {
            get { return _data; }
        }

        #endregion

        /// <summary>
        /// Loads the data from the given file.
        /// </summary>
        /// <param name="file">
        /// The given file name.
        /// </param>
        /// <exception cref="ArgumentNullException">ArgumentNullException.
        /// </exception>
        public void Load(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("file");
            }

            _data = new List<float>();

            // Loads data from text file.
            foreach (string line in Helper.FileLines(file))
            {
                _data.Add(float.Parse(line.Trim(), CultureInfo.InvariantCulture));
            }

            _fileName = file;
        }

        /// <summary>
        /// Saves the file into text file.
        /// </summary>
        /// <param name="file">
        /// The given file name to store the power data.
        /// </param>
        /// <exception cref="ArgumentNullException">ArgumentNullException.
        /// </exception>
        /// <exception cref="InvalidDataException">InvalidDataException.
        /// </exception>
        public void Save(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("file");
            }

            if (_data == null)
            {
                throw new InvalidDataException("No power data found");
            }

            // Saves power data into text file.
            using (StreamWriter writer = new StreamWriter(file, false, Encoding.ASCII))
            {
                for (int i = 0; i < _data.Count; ++i)
                {
                    if (_data[i] > 0)
                    {
                        writer.WriteLine("{0:f6}", _data[i]);
                    }
                    else
                    {
                        writer.WriteLine(_data[i]);
                    }
                }
            }

            _fileName = file;
        }

        /// <summary>
        /// Smooths power data using median filter.
        /// </summary>
        /// <param name="windowLength">
        /// The window length of the median filter.
        /// </param>
        /// <param name="iteration">
        /// The iteration number of the median filter.
        /// </param>
        /// <param name="minValue">
        /// The min value of the power data.
        /// </param>
        /// <param name="maxValue">
        /// The max value of the power data.
        /// </param>
        /// <exception cref="ArgumentException">ArgumentException.
        /// </exception>
        /// <exception cref="InvalidDataException">InvalidDataException.
        /// </exception>
        public void Smooth(int windowLength, int iteration, float minValue, float maxValue)
        {
            if (windowLength <= 1)
            {
                throw new ArgumentException(
                    Helper.NeutralFormat("window length should be greater than 1, but \"{0}\" given", windowLength));
            }

            if (_data == null)
            {
                throw new InvalidDataException("No power data found");
            }

            MedianFilter medianFilter = new MedianFilter(windowLength);
            for (int i = 0; i < iteration; ++i)
            {
                medianFilter.Filter(_data, e => e > minValue && e < maxValue);
            }
        }

        /// <summary>
        /// Converts the power data to logarithmic data.
        /// </summary>
        public void ConvertToLog()
        {
            for (int i = 0; i < _data.Count; ++i)
            {
                _data[i] = _data[i] > 0.1f ? (float)Math.Log(_data[i]) : (float)-1e+010;
            }
        }

        /// <summary>
        /// Zeros power in the silence frames.
        /// </summary>
        /// <param name="alignmentFile">
        /// The alignment file.
        /// </param>
        /// <param name="secondsPerFrame">
        /// The seconds per frame.
        /// </param>
        /// <exception cref="ArgumentException">ArgumentException.
        /// </exception>
        /// <exception cref="InvalidDataException">InvalidDataException.
        /// </exception>
        public void ZeroSilence(string alignmentFile, float secondsPerFrame)
        {
            if (!File.Exists(alignmentFile))
            {
                throw new FileNotFoundException(Helper.NeutralFormat("Alignment file \"{0}\" not found", alignmentFile));
            }

            // Loads alginment file.
            SegmentFile segmentFile = new SegmentFile();
            segmentFile.Load(alignmentFile);

            // Gets the last segmentation.
            WaveSegment last = segmentFile.WaveSegments[segmentFile.WaveSegments.Count - 1];

            // Gets the silence frame and end frame.
            if ((int)(last.StartTime / secondsPerFrame) > _data.Count)
            {
                throw new InvalidDataException(
                    Helper.NeutralFormat(
                        "The start frame {0} of last phone [{1}] is larger than power length {2} in file \"{3}\", remove this sentence from training set.",
                        (int)(last.StartTime / secondsPerFrame), last.Label, _data.Count, _fileName));
            }

            List<int> silenceFrames = new List<int>();
            for (int i = 0; i < segmentFile.WaveSegments.Count; ++i)
            {
                if (segmentFile.WaveSegments[i].IsSilenceFeature)
                {
                    silenceFrames.Add((int)(segmentFile.WaveSegments[i].StartTime / secondsPerFrame));
                    if (i < segmentFile.WaveSegments.Count - 1)
                    {
                        silenceFrames.Add((int)(segmentFile.WaveSegments[i + 1].StartTime / secondsPerFrame));
                    }
                    else
                    {
                        silenceFrames.Add(_data.Count);
                    }
                }
            }

            // Zeros the power data at silence frames.
            for (int i = 0; i < silenceFrames.Count; i += 2)
            {
                for (int j = silenceFrames[i]; j < silenceFrames[i + 1]; ++j)
                {
                    _data[j] = 0.0f;
                }
            }
        }

        /// <summary>
        /// Check whether all data is zero.
        /// </summary>
        /// <returns>Whether all data is zero.</returns>
        public bool IsAllDataZero()
        {
            if (_data == null)
            {
                throw new InvalidDataException("No power data found");
            }

            bool allZero = true;
            foreach (float data in _data)
            {
                if (data != 0)
                {
                    allZero = false;
                    break;
                }
            }

            return allZero;
        }
    }
}