//----------------------------------------------------------------------------
// <copyright file="MedianFilter.cs" company="MICROSOFT">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements MedianFilter
// </summary>
//----------------------------------------------------------------------------
namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// The class to implement median filter.
    /// </summary>
    public class MedianFilter
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MedianFilter class.
        /// </summary>
        /// <param name="windowLength">The window length of this filter.</param>
        public MedianFilter(int windowLength)
        {
            WindowLength = windowLength;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the window length of this filter.
        /// </summary>
        public int WindowLength { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Filters the data.
        /// </summary>
        /// <param name="data">The given data will be filtered.</param>
        public void Filter(IList<float> data)
        {
            Filter(data, null, null);
        }

        /// <summary>
        /// Filters the data.
        /// </summary>
        /// <param name="data">The given data will be filtered.</param>
        /// <param name="predicate">The predication to indicate the value is an invalidate value or not.</param>
        /// <param name="uvFile">Uv file.</param>
        public void Filter(IList<float> data, Func<float, bool> predicate, string uvFile = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            List<bool> uvData = null;
            if (uvFile != null)
            {
                Helper.ThrowIfFileNotExist(uvFile);
                uvData = new List<bool>();
                foreach (string line in Helper.FileLines(uvFile))
                {
                    uvData.Add(int.Parse(line.Trim()) > 0);
                }

                if (uvData.Count != data.Count)
                {
                    throw new ArgumentException("uv length doesn't match with data length");
                }
            }

            Collection<float> save = new Collection<float>();
            for (int i = 0; i < data.Count; ++i)
            {
                int left = (i - WindowLength) >= 0 ? (i - WindowLength) : 0;
                int right = (i + WindowLength) <= (data.Count - 1) ? i + WindowLength : data.Count - 1;

                List<float> windows = new List<float>();
                for (int j = left; j <= right; ++j)
                {
                    windows.Add(data[j]);
                }

                windows.Sort();
                save.Add(windows[WindowLength]);
            }

            for (int i = 0; i < save.Count; ++i)
            {
                // check whether it's voiced
                if ((predicate == null && uvData == null) ||
                    (uvData == null && predicate != null && predicate(save[i])) ||
                    (uvData != null && uvData[i]))
                {
                    int left = (i - WindowLength) >= 0 ? (i - WindowLength) : 0;
                    int right = (i + WindowLength) <= (data.Count - 1) ? i + WindowLength : data.Count - 1;

                    float sum = 0.0f;
                    int count = 0;
                    for (int j = left; j <= right; ++j)
                    {
                        if ((predicate == null) || (predicate != null && predicate(save[j])))
                        {
                            sum += save[j];
                            ++count;
                        }
                    }

                    if (count > 0)
                    {
                        data[i] = sum / count;
                    }
                }
                else
                {
                    data[i] = 0.0f;
                }
            }
        }

        #endregion
    }
}