// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PitchMarkerFile.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//   This module defines a common library to manipulate text pitch marker file.
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
    /// The class to manipulate the text pitch marker file.
    /// </summary>
    public class PitchMarkerFile
    {
        private const int ColumnNumbers = 3;
        private const int FrameSamplePoint = 80;

        /// <summary>
        /// The file name of this pitch marker file.
        /// </summary>
        private string _fileName;

        /// <summary>
        /// The data to store the data of pitch marker file.
        /// </summary>
        private List<int[]> _data;

        /// <summary>
        /// The data store the frame offset and period.
        /// </summary>
        private List<int[]> _dataWithinFrame;

        private int _frameNumber = 0;

        /// <summary>
        /// Gets data of the pitch marker file.
        /// </summary>
        public List<int[]> Data
        {
            get { return _data; }
        }

        /// <summary>
        /// Gets data of the pitch marker within frame.
        /// </summary>
        public List<int[]> DataWithInFrame
        {
            get { return _dataWithinFrame; }
        }

        /// <summary>
        /// Loads the data from the given file.
        /// </summary>
        /// <param name="file">
        /// The given file name.
        /// </param>
        /// <param name="frameNumber">
        /// The number of frame.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Exception.
        /// </exception>
        public void Load(string file, int frameNumber)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("file");
            }

            _data = new List<int[]>();
            _dataWithinFrame = new List<int[]>();
            _frameNumber = frameNumber;

            // Loads data from text file.
            foreach (string line in Helper.FileLines(file))
            {
                string[] splitedLine = line.Split(new char[2] { ' ', '\t' });

                if (splitedLine.Length != ColumnNumbers)
                {
                    throw new Exception(string.Format("Incorrect format of pitch marker file"));
                }

                int[] valueInline = new int[ColumnNumbers];
                int index = 0;
                foreach (var splittedValue in splitedLine)
                {
                    valueInline[index++] = int.Parse(splittedValue.Trim(), CultureInfo.InvariantCulture);
                }

                _data.Add(valueInline);
            }

            AlignWithFrame();

            _fileName = file;
        }

        /// <summary>
        /// Saves the file into text file.
        /// </summary>
        /// <param name="file">
        /// The given file name to store the pitch marker data.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Exception.
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// Exception.
        /// </exception>
        public void Save(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("file");
            }

            if (_data == null)
            {
                throw new InvalidDataException("No pitch marker data found");
            }

            // Saves pitch marker data into text file.
            using (StreamWriter writer = new StreamWriter(file, false, Encoding.ASCII))
            {
                foreach (var element in _data)
                {
                    if (element.Length != ColumnNumbers)
                    {
                        throw new Exception("The data format of pitch marker is incorrect");
                    }
                    else
                    {
                        for (int i = 0; i < ColumnNumbers; i++)
                        {
                            writer.Write("{0} ", element[i]);
                        }

                        writer.WriteLine();
                    }
                }
            }

            _fileName = file;
        }

        private void AlignWithFrame()
        {
            int j = 0;

            for (int i = 0; i < _frameNumber; i++)
            {
                var frameStartSample = i * FrameSamplePoint;
                var frameEndSample = ((i + 1) * FrameSamplePoint) - 1;
                int[] data = new int[2];
                bool getTheFirstStartingSample = true;
                while (frameStartSample <= _data[j][0] && frameEndSample >= _data[j][0])
                {
                    if (getTheFirstStartingSample)
                    {
                        // Get the offset position.
                        data[0] = _data[j][0] - frameStartSample;

                        // Get the period.
                        data[1] = _data[j][2];
                    }

                    getTheFirstStartingSample = false;
                    j++;
                }

                if (getTheFirstStartingSample)
                {
                    data[0] = -1;
                    data[1] = -1;
                }

                _dataWithinFrame.Add(data);
            }
        }
    }
}