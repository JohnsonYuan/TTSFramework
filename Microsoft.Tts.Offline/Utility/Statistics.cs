//----------------------------------------------------------------------------
// <copyright file="Statistics.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements statistics related functions
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// This class containing various statistics related functions.
    /// </summary>
    public static class Statistics
    {
        /// <summary>
        /// Sort a data set, and then select the element on given percentile.
        /// </summary>
        /// <typeparam name="T">The type of elements in the data set.</typeparam>
        /// <param name="dataSet">The data set.</param>
        /// <param name="percentile">The percentile.</param>
        /// <returns>The selected element.</returns>
        public static T ValueOnPercentile<T>(Collection<T> dataSet, double percentile) where T : IComparable
        {
            return ValueOnPercentile<T>(dataSet, percentile, null);
        }

        /// <summary>
        /// Sort a data set, and then select the element on given percentile.
        /// </summary>
        /// <typeparam name="T">The type of elements in the data set.</typeparam>
        /// <param name="dataSet">The data set.</param>
        /// <param name="percentile">The percentile.</param>
        /// <param name="comparer">
        /// The customized comparer, can be null.
        /// If this is set to null, default comparer will be used.
        /// </param>
        /// <returns>The selected element.</returns>
        public static T ValueOnPercentile<T>(Collection<T> dataSet, double percentile, IComparer<T> comparer)
            where T : IComparable
        {
            if (dataSet == null || dataSet.Count == 0)
            {
                throw new ArgumentException("The data set should not be null or empty.", "dataSet");
            }

            if (percentile < 0.0 || percentile > 1.0)
            {
                throw new ArgumentOutOfRangeException(
                    "percentile", "This argument should be on interval [0, 1].");
            }

            List<T> dataList = new List<T>(dataSet);
            if (comparer == null)
            {
                dataList.Sort();
            }
            else
            {
                dataList.Sort(comparer);
            }

            int targetIndex = (int)((percentile * (dataSet.Count - 1)) + 0.5);
            return dataList[targetIndex];
        }

        /// <summary>
        /// Get the distribution for a data set, using the given interval size.
        /// </summary>
        /// <param name="dataSet">The data set.</param>
        /// <param name="intervalSize">The interval size.</param>
        /// <returns>
        /// The distribution, stored in a dictionary.
        /// The key is the distribution interval, and the value is frequence.
        /// </returns>
        public static Dictionary<int, int> Distribution(Collection<int> dataSet, int intervalSize)
        {
            if (dataSet == null)
            {
                throw new ArgumentNullException("dataSet");
            }

            Collection<double> dataSetAsDouble = new Collection<double>();
            dataSet.ForEach(x => dataSetAsDouble.Add((double)x));
            Dictionary<int, int> distribution = new Dictionary<int, int>();
            Distribution(dataSetAsDouble, (double)intervalSize).ForEach(
                x => distribution.Add((int)(x.Key + (0.5 * Math.Sign(x.Key))), x.Value));

            return distribution;
        }

        /// <summary>
        /// Get the distribution for a data set, using the given interval size.
        /// </summary>
        /// <param name="dataSet">The data set.</param>
        /// <param name="intervalSize">The interval size.</param>
        /// <returns>
        /// The distribution, stored in a dictionary.
        /// The key is the distribution interval, and the value is frequence.
        /// </returns>
        public static Dictionary<double, int> Distribution(Collection<double> dataSet, double intervalSize)
        {
            if (dataSet == null || dataSet.Count == 0)
            {
                throw new ArgumentException("The data set should not be null or empty.", "dataSet");
            }

            if (intervalSize <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "interval", "This argument should be greater than 0.");
            }

            double minValue = dataSet[dataSet.MinIndex()];
            double maxValue = dataSet[dataSet.MaxIndex()];
            double minBoundaryPoint = Math.Floor(minValue / intervalSize) * intervalSize;
            double maxBoundaryPoint = Math.Floor(maxValue / intervalSize) * intervalSize;
            if (maxBoundaryPoint <= maxValue)
            {
                maxBoundaryPoint += intervalSize;
            }

            Collection<double> boundaryPoints = new Collection<double>();
            int boundaryPointCount = (int)(((maxBoundaryPoint - minBoundaryPoint) / intervalSize) + 0.5) + 1;
            for (double i = 0; i < boundaryPointCount; ++i)
            {
                boundaryPoints.Add(
                    minBoundaryPoint + (((maxBoundaryPoint - minBoundaryPoint) / (boundaryPointCount - 1)) * i));
            }

            return Distribution<double>(dataSet, boundaryPoints);
        }

        /// <summary>
        /// Get the distribution for a data set, using the given distribution boundary points.
        /// </summary>
        /// <typeparam name="T">The type of elements in the data set.</typeparam>
        /// <param name="dataSet">The data set.</param>
        /// <param name="boundaryPoints">The distribution boundary points.</param>
        /// <returns>
        /// The distribution, stored in a dictionary.
        /// The key is the distribution interval, and the value is frequence.
        /// </returns>
        public static Dictionary<T, int> Distribution<T>(Collection<T> dataSet, Collection<T> boundaryPoints)
            where T : IComparable
        {
            if (dataSet == null || dataSet.Count == 0)
            {
                throw new ArgumentException("The data set should not be null or empty.", "dataSet");
            }

            if (boundaryPoints == null)
            {
                throw new ArgumentNullException("boundaryPoints");
            }

            if (boundaryPoints.Count < 2)
            {
                throw new ArgumentException(
                    "The boundary points count shouldn't be less than 2.", "boundaryPoints");
            }

            Dictionary<T, int> distribution = new Dictionary<T, int>();
            for (int i = 0; i < boundaryPoints.Count - 1; ++i)
            {
                distribution.Add(boundaryPoints[i], 0);
                foreach (T dataItem in dataSet)
                {
                    if (dataItem.CompareTo(boundaryPoints[i]) >= 0 &&
                        dataItem.CompareTo(boundaryPoints[i + 1]) < 0)
                    {
                        ++distribution[boundaryPoints[i]];
                    }
                }
            }

            distribution.Add(boundaryPoints[boundaryPoints.Count - 1], 0);
            return distribution;
        }
    }
}