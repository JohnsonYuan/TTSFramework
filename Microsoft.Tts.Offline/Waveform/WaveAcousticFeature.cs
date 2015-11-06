//----------------------------------------------------------------------------
// <copyright file="WaveAcousticFeature.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This class get the Root-Mean-Square from a wave file.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Calculate the wave acoustic features.
    /// </summary>
    public class WaveAcousticFeature
    {
        #region Private field

        private string _filePath;
        private short[] _waveData;
        private int _samplesPerSecond;

        #endregion

        #region Proterties

        /// <summary>
        /// Gets Total sample number of wave file.
        /// </summary>
        public int SampleNumber
        {
            get { return _waveData.Length; }
        }

        /// <summary>
        /// Gets The sampling rate of the wave file.
        /// </summary>
        public int SamplesPerSecond
        {
            get { return _samplesPerSecond; }
        }

        /// <summary>
        /// Gets File path of the wave file.
        /// </summary>
        public string FilePath
        {
            get { return _filePath; }
        }

        #endregion

        #region Pulic Methods

        /// <summary>
        /// Load a wave file for processing.
        /// </summary>
        /// <param name="filePath">Waveform file path.</param>
        public void Load(string filePath)
        {
            _filePath = filePath;
            WaveFile waveFile = new WaveFile();
            waveFile.Load(filePath);
            switch (waveFile.Format.BitsPerSample)
            {
                case (int)WaveBitsPerSample.Sixteen:
                    _waveData = ArrayHelper.BinaryConvertArray(waveFile.GetSoundData());
                    break;
                default:
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Only {0} bits per sample waveform is supported. But it is {1} bits per sample of waveform file [{2}].",
                        (int)WaveBitsPerSample.Sixteen, waveFile.Format.BitsPerSample, filePath);
                    throw new NotSupportedException(message);
            }

            _samplesPerSecond = waveFile.Format.SamplesPerSecond;
        }

        /// <summary>
        /// Load a wave file for processing.
        /// </summary>
        /// <param name="waveFile">Wave file.</param>
        public void Load(WaveFile waveFile)
        {
            _filePath = string.Empty;
            switch (waveFile.Format.BitsPerSample)
            {
                case (int)WaveBitsPerSample.Sixteen:
                    _waveData = ArrayHelper.BinaryConvertArray(waveFile.GetSoundData());
                    break;
                default:
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Only {0} bits per sample waveform is supported. But it is {1} bits per sample of waveform stream.",
                        (int)WaveBitsPerSample.Sixteen, waveFile.Format.BitsPerSample);
                    throw new NotSupportedException(message);
            }

            _samplesPerSecond = waveFile.Format.SamplesPerSecond;
        }

        /// <summary>
        /// Root means quare from time.
        /// </summary>
        /// <param name="fromTime">The start postion of the wave range to calculation, in second.</param>
        /// <param name="duration">The duration or length of the wave range, in second.</param>
        /// <returns>RMS of samples in the given range.</returns>
        public float CalculateRms(float fromTime, float duration)
        {
            return CalculateRms(fromTime * _samplesPerSecond, duration * _samplesPerSecond);
        }

        /// <summary>
        /// Root means quare from sample.
        /// </summary>
        /// <param name="fromSample">The start postion of the wave range to calculation, in sample.</param>
        /// <param name="sampleNumber">The duration or length of the wave range, in sample.</param>
        /// <returns>RMS of samples in the given range.</returns>
        public float CalculateRms(int fromSample, int sampleNumber)
        {
            if (fromSample < 0)
            {
                throw new ArgumentOutOfRangeException("fromSample");
            }

            if (sampleNumber < 0)
            {
                throw new ArgumentOutOfRangeException("sampleNumber");
            }

            if (fromSample + sampleNumber > SampleNumber)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The end of the sample range should not be bigger than total sample count.");
                throw new ArgumentOutOfRangeException("fromSample", message);
            }

            const int WindowLength = 320;

            int frameCount = 0;
            double rms = 0.0;
            int endSample = fromSample + sampleNumber - WindowLength;
            for (int i = fromSample; i < endSample; i += WindowLength)
            {
                rms += CalculateWindowRms(i, WindowLength);

                ++frameCount;
            }

            // Pay attention to the calculation of energy. Hu Peng
            if (frameCount > 0)
            {
                rms /= frameCount;
            }

            return (float)rms;
        }

        /// <summary>
        /// Root mean square for energy feature.
        /// </summary>
        /// <param name="fromSample">The start postion of the wave range to calculation, in sample.</param>
        /// <param name="sampleNumber">The duration or length of the wave range, in sample.</param>
        /// <returns>RMS of samples in the given range.</returns>
        public float CalculateEnergyRms(int fromSample, int sampleNumber)
        {
            if (fromSample < 0)
            {
                throw new ArgumentOutOfRangeException("fromSample");
            }

            if (sampleNumber < 0)
            {
                throw new ArgumentOutOfRangeException("sampleNumber");
            }

            if (fromSample + sampleNumber > SampleNumber)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The end of the sample range should not be bigger than total sample count.");
                throw new ArgumentOutOfRangeException("fromSample", message);
            }

            double rms = 0.0;
            if (sampleNumber > 0)
            {
                rms = CalculateWindowRms(fromSample, sampleNumber);
                Debug.Assert(rms >= 0.0);
                if (rms > 0.0)
                {
                    rms = Math.Log10(rms);
                }
            }

            return (float)rms;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Calculate root mean square based on window.
        /// </summary>
        /// <param name="fromSample">The duration or length of the wave range, in sample.</param>
        /// <param name="windowWidth">The sample number in the window.</param>
        /// <returns>RMS of the samples in the window.</returns>
        private double CalculateWindowRms(int fromSample, int windowWidth)
        {
            Debug.Assert(fromSample >= 0);
            Debug.Assert(windowWidth >= 0);
            Debug.Assert(_waveData.Length >= fromSample + windowWidth);

            double energy = 0.0;
            for (int i = fromSample; i < fromSample + windowWidth; ++i)
            {
                energy += _waveData[i] * _waveData[i]; // short * short => int
            }

            // xwhan: I think the return mush be Single/double, but the original code
            // use short for return.
            return Math.Sqrt(energy / windowWidth);
        }

        #endregion
    }
}