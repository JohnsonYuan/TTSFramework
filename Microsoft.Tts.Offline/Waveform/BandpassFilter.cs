//----------------------------------------------------------------------------
// <copyright file="BandpassFilter.cs" company="MICROSOFT">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements BandpassFilter
// </summary>
//----------------------------------------------------------------------------
namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// FIR Bandpass filter, filter waveform or EGG file.
    /// The filter which transmits energy in a specified wavelength band 
    /// But rejects energy above and below.
    /// The main function of such a filter in a transmitter is to limit the
    /// Bandwidth of the output signal to the minimum necessary to convey data
    /// At the desired speed and in the desired form.
    /// </summary>
    public class BandpassFilter
    {
        #region Private members

        private double _lowFreq;
        private double _highFreq;
        private int _order;
        private double[] _window;

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="BandpassFilter"/> class.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Thrown when the parameter
        /// Is invalid.</exception>
        /// <param name="lowFreq">Should not be negatived.</param>
        /// <param name="highFreq">Should not be less than lowFreq.</param>
        /// <param name="order">Should not be negative.</param>
        /// <param name="samplesPerSecond">Only support 16k.</param>
        public BandpassFilter(double lowFreq, double highFreq, int order, int samplesPerSecond)
        {
            Init(lowFreq, highFreq, order, WindowType.Kaiser, samplesPerSecond);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BandpassFilter"/> class.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Thrown when the parameter
        /// Is invalid.</exception>
        /// <param name="lowFreq">Should not be negative.</param>
        /// <param name="highFreq">Should not be less than lowFreq.</param>
        /// <param name="order">Should not be negatived.</param>
        /// <param name="windowType">Window used for filter design.</param>
        /// <param name="samplesPerSecond">Only support 16k.</param>
        public BandpassFilter(double lowFreq, double highFreq, int order, WindowType windowType, int samplesPerSecond)
        {
            Init(lowFreq, highFreq, order, windowType, samplesPerSecond);
        }

        #endregion

        #region Public property

        /// <summary>
        /// Gets Lowest frequency that estimate after filter.
        /// </summary>
        public double LowFreq
        {
            get { return _lowFreq; }
        }

        /// <summary>
        /// Gets Highest frequency that estimate after filter.
        /// </summary>
        public double HighFreq
        {
            get { return _highFreq; }
        }

        /// <summary>
        /// Gets Bandpass window's parameter.
        /// </summary>
        public int Order
        {
            get { return _order; }
        }

        #endregion

        #region Public method

        /// <summary>
        /// Filter soundData.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Thrown when
        /// The soundData parameter is null or doesn't contain
        /// Any elements.</exception>
        /// <exception cref="System.IO.InvalidDataException">Thrown when
        /// _window is null or doesn't contain any elements.</exception>
        /// <param name="soundData">Sound samples.</param>
        /// <returns>Filtered soundData.</returns>
        public short[] Filter(short[] soundData)
        {
            if (soundData == null || soundData.Length == 0)
            {
                throw new ArgumentNullException("soundData");
            }

            if (_window == null || _window.Length == 0)
            {
                string meesage = string.Format(CultureInfo.InvariantCulture,
                    "can't find valid bandpass filter array : _window ");
                throw new InvalidDataException(meesage);
            }

            short[] destData = new short[soundData.Length];

            for (int i = 0; i < soundData.Length; i++)
            {
                double sum = 0;
                for (int j = 0; j < _window.Length; j++)
                {
                    if ((i - ((_window.Length - 1) / 2) + j) >= 0 &&
                        (i - ((_window.Length - 1) / 2) + j) < soundData.Length)
                    {
                        sum += (double)(soundData[i - ((_window.Length - 1) / 2) + j] *
                            _window[j]);
                    }
                }

                destData[i] = NumberConverter.Double2Int16(sum);
            }

            return destData;
        }

        /// <summary>
        /// Filter one wave file.
        /// </summary>
        /// <exception cref="System.IO.FileNotFoundException">Thrown when
        /// Can't find file specified by sourceWaveFilePath.</exception>
        /// <exception cref="System.IO.InvalidDataException">Thrown when
        /// _window is null or doesn't contain any elements.</exception>
        /// <exception cref="System.ArgumentException">Thrown when
        /// The number of samples per second is not 16k.</exception>
        /// <param name="sourceWaveFilePath">Source waveform file to filter.</param>
        /// <param name="targetWaveFilePath">Target waveform file to save to filtering result.</param>
        public void Filter(string sourceWaveFilePath, string targetWaveFilePath)
        {
            if (string.IsNullOrEmpty(sourceWaveFilePath) ||
                !File.Exists(sourceWaveFilePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    sourceWaveFilePath);
            }

            if (_window == null || _window.Length == 0)
            {
                string meesage = string.Format(CultureInfo.InvariantCulture,
                    "can't find valid bandpass filter array : _window ");
                throw new InvalidDataException(meesage);
            }

            WaveFile sourceFile = new WaveFile();

            sourceFile.Load(sourceWaveFilePath);

            // wave filter only process 16k.
            if (sourceFile.Format.SamplesPerSecond != 16000)
            {
                string meesage = string.Format(CultureInfo.InvariantCulture,
                    "BandpassFilter only support 16k sample rate wave file. samplesPerSecond = {0}",
                    sourceFile.Format.SamplesPerSecond);
                throw new ArgumentException(meesage);
            }

            short[] filteredData = Filter(sourceFile.DataIn16Bits);

            Buffer.BlockCopy(filteredData, 0,
                sourceFile.GetSoundData(), 0,
                sourceFile.GetSoundData().Length);

            sourceFile.Save(targetWaveFilePath);
        }

        #endregion

        #region Private static method

        /// <summary>
        /// Build bandpass filter coefficients array with Kaiser window.
        /// </summary>
        /// <param name="lowFreq">Select frequency of low-pass band edge.</param>
        /// <param name="highFreq">Select frequency of high-pass band edge.</param>
        /// <param name="order">Filter order.</param>
        /// <param name="samplesPerSecond">Samples per second.</param>
        /// <returns>Filter.</returns>
        private static double[] BuildFilter(double lowFreq, double highFreq,
            int order, int samplesPerSecond)
        {
            return BuildFilter(lowFreq, highFreq, order, WindowType.Kaiser, samplesPerSecond);
        }

        /// <summary>
        /// Build bandpass filter coefficients array.
        /// </summary>
        /// <param name="lowFreq">Select frequency of low-pass band edge.</param>
        /// <param name="highFreq">Select frequency of high-pass band edge.</param>
        /// <param name="order">Filter order.</param>
        /// <param name="windowType">Window used for filter design.</param>
        /// <param name="samplesPerSecond">Samples per second.</param>
        /// <returns>Filter.</returns>
        private static double[] BuildFilter(double lowFreq,
            double highFreq,
            int order,
            WindowType windowType,
            int samplesPerSecond)
        {
            const double KaiserBeta = 16;
            double[] filters = new double[order + 1];

            double lowOmegaUnit = 2 * Math.PI * lowFreq / samplesPerSecond;
            double highOmegaUnit = 2 * Math.PI * highFreq / samplesPerSecond;

            for (int i = 0; i < filters.Length; i++)
            {
                float omega = (float)i - ((float)order / 2.0f);
                if (omega != 0)
                {
                    filters[i] = (Math.Sin(omega * highOmegaUnit) -
                        Math.Sin(omega * lowOmegaUnit)) / (Math.PI * omega);
                }
                else
                {
                    filters[i] = (highOmegaUnit - lowOmegaUnit) / Math.PI;
                }
            }

            double[] tempArray = null;

            switch (windowType)
            { 
                case WindowType.Bartlett:
                    tempArray = Window.Bartlett(filters.Length);
                    break;

                case WindowType.Kaiser:
                    tempArray = Window.Kaiser(filters.Length, KaiserBeta);
                    break;

                default:
                    throw new ArgumentException("Unsupported window type", "windowType");
            }

            for (int i = 0; i < filters.Length; i++)
            {
                filters[i] = filters[i] * tempArray[i];
            }

            return filters;
        }
        #endregion

        #region Private method

        /// <summary>
        /// The init function.
        /// </summary>
        /// <param name="lowFreq">Low frequency.</param>
        /// <param name="highFreq">High frequency.</param>
        /// <param name="order">The order.</param>
        /// <param name="windowType">Word type.</param>
        /// <param name="samplesPerSecond">Sample rate.</param>
        private void Init(double lowFreq, double highFreq, int order, WindowType windowType, int samplesPerSecond)
        {
            if (lowFreq < 0)
            {
                string meesage = string.Format(CultureInfo.InvariantCulture,
                    "Invalid low frequence parameter when build bandpass filter coefficient, low frequence must be positive : {0} ",
                    lowFreq);
                throw new ArgumentException(meesage, "lowFreq");
            }

            if (highFreq < lowFreq)
            {
                string meesage = string.Format(CultureInfo.InvariantCulture,
                    "Invalid low frequence parameter when build bandpass filter coefficient, high frequence must be bigger then low frequence : highFreq = {0} , lowFreq = {1} ",
                    highFreq, lowFreq);
                throw new ArgumentException(meesage);
            }

            if (order < 0 || order > 1000)
            {
                string meesage = string.Format(CultureInfo.InvariantCulture,
                    "Invalid low frequence parameter when build bandpass filter coefficient, high frequence must in the region :  [ 0 - 1000 ]: {0} ",
                    order);
                throw new ArgumentException(meesage, "order");
            }

            // wave filter only process 16k.
            if (samplesPerSecond != 16000)
            {
                string meesage = string.Format(CultureInfo.InvariantCulture,
                    "Only support 16k sample rate wave file. samplesPerSecond = {0}",
                    samplesPerSecond);
                throw new ArgumentException(meesage, "samplesPerSecond");
            }

            this._lowFreq = lowFreq;
            this._highFreq = highFreq;
            this._order = order;

            _window = BuildFilter(lowFreq, highFreq, order, windowType, samplesPerSecond);
        }
        #endregion
    }
}