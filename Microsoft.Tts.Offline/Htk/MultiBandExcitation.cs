// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MultiBandExcitation.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//   This module defines a common library to manipulate multi-band excitation file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// The class to manipulate the multi-band excitation file.
    /// </summary>
    public class MbeFile
    {
        /// <summary>
        /// The file name of this multi-band excitation file.
        /// </summary>
        private string _fileName;

        /// <summary>
        /// The data to store the data of multi-band excitation file.
        /// </summary>
        private float[,] _data = null;

        /// <summary>
        /// Gets data of the multi-band excitation file.
        /// </summary>
        public float[,] Data
        {
            get { return _data; }
        }

        /// <summary>
        /// Loads the data from the given file.
        /// </summary>
        /// <param name="file">
        /// The given file name.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Exception.
        /// </exception>
        public void Load(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("file");
            }

            using (StreamReader sr = new StreamReader(file))
            {
                string line = sr.ReadLine().Trim();
                string[] size = line.Split(new char[] { ' ', '\t' });
                Debug.Assert(size.Length == 2);
                int column = int.Parse(size[0]);
                int row = int.Parse(size[1]);

                _data = new float[row, column];

                for (int i = 0; i < row; ++i)
                {
                    line = sr.ReadLine().Trim();
                    string[] mbeValue = line.Split(new char[] { ' ', '\t' });
                    Debug.Assert(mbeValue.Length == column);

                   for (int j = 0; j < column; ++j)
                   {
                       _data[i, j] = float.Parse(mbeValue[j]);
                   }
                }
            }

            _fileName = file;
        }      
    }
}