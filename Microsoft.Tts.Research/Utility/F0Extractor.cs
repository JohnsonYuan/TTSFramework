//----------------------------------------------------------------------------
// <copyright file="F0Extractor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     Extract f0 data from epoch data
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Research
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// F0 extractor class.
    /// </summary>
    public class F0Extractor
    {
        #region Public static operations

        /// <summary>
        /// Extract f0s from epoch data.
        /// </summary>
        /// <param name="epochData">Epoch data.</param>
        /// <param name="sampleRate">Sample rate.</param>
        /// <param name="frameSize">Frame size in second.</param>
        /// <param name="beginTime">Begin time of f0s in second.</param>
        /// <param name="endTime">End time of f0s in second.</param>
        /// <param name="searchIndex">The index to start search from.</param>
        /// <returns>F0 contour.</returns>
        public static int[] ExtractF0FromEpoch(int[] epochData, int sampleRate,
            double frameSize, double beginTime, double endTime, ref int searchIndex)
        {
            if (epochData == null || epochData.Length == 0)
            {
                throw new ArgumentNullException("epochData");
            }

            if (sampleRate <= 0)
            {
                throw new ArgumentException("sampleRate should be larger than 0.");
            }

            if (frameSize <= 0)
            {
                throw new ArgumentException("frameSize should be larger than 0.");
            }

            if (beginTime < 0)
            {
                throw new ArgumentException("beginTime should not be negative.");
            }

            if (searchIndex < 0 || searchIndex >= epochData.Length)
            {
                throw new InvalidDataException("searchIndex invalid.");
            }

            int[] epochOffset = EpochFile.EpochToOffset(epochData);
            var query = from epoch in epochOffset
                        select Math.Abs(epoch) / (double)sampleRate;
            double[] epochTime = query.ToArray();
            
            if (endTime > epochTime[epochTime.Length - 1] || endTime <= beginTime)
            {
                throw new InvalidDataException("endTime invalid.");
            }

            int[] f0Contour = ExtractF0Process(epochData, epochTime, frameSize, 
                sampleRate, beginTime, endTime, ref searchIndex);
            return f0Contour;
        }

        /// <summary>
        /// Extract f0s from epoch data      .
        /// </summary>
        /// <param name="epochData">Epoch data.</param>
        /// <param name="sampleRate">Sample rate.</param>
        /// <param name="numToExtract">Number of f0s to extract.</param>
        /// <param name="beginTime">Begin time of f0s in second.</param>
        /// <param name="endTime">End time of f0s in second.</param>
        /// <param name="searchIndex">The index to start search from.</param>
        /// <returns>F0 contour.</returns>
        public static int[] ExtractF0FromEpoch(int[] epochData, int sampleRate,
            int numToExtract, double beginTime, double endTime, ref int searchIndex)
        {
            if (numToExtract <= 0)
            {
                throw new ArgumentException("numToExtract should be larger than 0.");
            }

            double frameSize = (endTime - beginTime) / numToExtract;

            int[] f0Contour = ExtractF0FromEpoch(epochData, sampleRate, frameSize,
                beginTime, endTime, ref searchIndex);
            Debug.Assert(numToExtract == f0Contour.Length);

            return f0Contour;
        }

        #endregion

        #region Private static operations

        /// <summary>
        /// Extract f0s process.
        /// </summary>
        /// <param name="epochData">Epoch data.</param>
        /// <param name="epochTime">Epoch data in time.</param>
        /// <param name="frameSize">Frame size in second.</param>
        /// <param name="sampleRate">Sample rate.</param>
        /// <param name="beginTime">Begin time of f0s in second.</param>
        /// <param name="endTime">End time of f0s in second.</param>
        /// <param name="searchIndex">The index to start search from.</param>
        /// <returns>F0 contour.</returns>
        private static int[] ExtractF0Process(int[] epochData, double[] epochTime,
            double frameSize, int sampleRate, double beginTime, double endTime,
            ref int searchIndex)
        {         
            if (searchIndex != 0 && beginTime < epochTime[searchIndex - 1])
            {
                throw new InvalidDataException("Wrong searchIndex.");
            }

            Collection<int> f0s = new Collection<int>();
            double timePosition = beginTime + (frameSize / 2);
            if (timePosition >= endTime)
            {
                for (; searchIndex < epochTime.Length; searchIndex++)
                {
                    if (epochTime[searchIndex] > beginTime)
                    {
                        if (epochData[searchIndex] <= 0)
                        {
                            f0s.Add(0);
                        }
                        else
                        {
                            f0s.Add((int)Math.Round((double)sampleRate /
                                epochData[searchIndex]));
                        }

                        break; 
                    }
                }
            }

            while (timePosition < endTime)
            {
                for (; searchIndex < epochTime.Length; searchIndex++)
                {
                    if (epochTime[searchIndex] > timePosition)
                    {
                        if (epochData[searchIndex] <= 0)
                        {
                            f0s.Add(0);
                        }
                        else
                        { 
                            f0s.Add((int)Math.Round((double)sampleRate / 
                                epochData[searchIndex]));
                        }

                        timePosition = timePosition + frameSize;
                        break; 
                    }
                }
            }

            if (f0s.Count == 0)
            {
                f0s.Add(0);
            }

            int[] f0Contour = new int[f0s.Count];
            f0s.CopyTo(f0Contour, 0);           

            return f0Contour;
        }

        #endregion
    }
}