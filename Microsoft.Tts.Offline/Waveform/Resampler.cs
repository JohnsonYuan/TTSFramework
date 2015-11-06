//----------------------------------------------------------------------------
// <copyright file="Resampler.cs" company="MICROSOFT">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      This module implements ResampleFilter.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Resample wave44k to wave16k.
    /// </summary>
    public class ResampleFilter
    {
        #region Public const fields

        /// <summary>
        /// The bits per sample of the source waveform file supported.
        /// </summary>
        public const short SupportedBitsPerSample = (int)WaveBitsPerSample.Sixteen;

        /// <summary>
        /// The channel number of the source waveform file supported.
        /// </summary>
        public const short SupportedChannels = (int)WaveChannel.Mono;

        #endregion

        #region Private fields

        private const float HalfFilterLength = 0.0005f;
        private static readonly int[] PIPrimes = new int[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 };

        private int _upFactor;
        private int _filterHalf;
        private int _downFactor;
        private int _filterLength;
        private int _bufferLength;
        private float[] _filterCoeff;

        private float[] _leftMemory;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResampleFilter"/> class.
        /// </summary>
        /// <param name="inputSamplesPerSecond">Samples per second of input waveform file.</param>
        /// <param name="outputSamplesPerSecond">Samples per second of output waveform file.</param>
        public ResampleFilter(int inputSamplesPerSecond,
            int outputSamplesPerSecond)
        {
            // Check if we can deal with the format
            if (inputSamplesPerSecond <= 0 ||
                outputSamplesPerSecond <= 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid sampling rate found: input {0}, output {1}.",
                    inputSamplesPerSecond, outputSamplesPerSecond);
                throw new ArgumentOutOfRangeException(message);
            }

            if (inputSamplesPerSecond == outputSamplesPerSecond)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Input and output samples per second are the same: input {0}, output {1}.",
                    inputSamplesPerSecond, outputSamplesPerSecond);
                throw new ArgumentException(message);
            }

            int limitFactor = 0;

            FindResampleFactors(inputSamplesPerSecond, outputSamplesPerSecond);
            limitFactor = (_upFactor > _downFactor) ? _upFactor : _downFactor;

            _filterHalf = checked((int)(inputSamplesPerSecond * limitFactor * HalfFilterLength));
            _filterLength = (2 * _filterHalf) + 1;

            _filterCoeff = WindowedLowPass(.5f / checked((float)limitFactor),
                checked((float)_upFactor));

            _bufferLength = checked((int)(checked((float)_filterLength) /
                checked((float)_upFactor)));

            _leftMemory = new float[_bufferLength];
        }

        #endregion

        #region Public static methods

        /// <summary>
        /// Resample the source waveform file to 16k Hz waveform file.
        /// </summary>
        /// <param name="sourceFile">Location of source waveform file.</param>
        /// <param name="targetFile">Location of target waveform file.</param>
        /// <param name="targetSamplesPerSecond">Samples per second of the target waveform file.</param>
        public static void Resample(string sourceFile, string targetFile, int targetSamplesPerSecond)
        {
            if (string.IsNullOrEmpty(sourceFile))
            {
                throw new ArgumentNullException("sourceFile");
            }

            if (string.IsNullOrEmpty(targetFile))
            {
                throw new ArgumentNullException("targetFile");
            }

            WaveFormat format = WaveFile.ReadFormat(sourceFile);
            if (format.SamplesPerSecond < targetSamplesPerSecond)
            {
                throw new NotSupportedException(Helper.NeutralFormat(
                    "Resampling tool will introduce obvious aliasing " +
                    "noise when upsampling from [{0}] to [[1}], refer to bug #12628",
                    format.SamplesPerSecond, targetSamplesPerSecond));
            }

            WaveFile waveFile = new WaveFile();
            waveFile.Load(sourceFile);

            Resample(waveFile, targetSamplesPerSecond);

            Helper.EnsureFolderExistForFile(targetFile);
            waveFile.Save(targetFile);
        }

        /// <summary>
        /// Convert the WaveFile instance into another samples per second.
        /// </summary>
        /// <param name="waveFile">Waveform instance to resample.</param>
        /// <param name="targetSamplesPerSecond">Samples per second of the target waveform file.</param>
        public static void Resample(WaveFile waveFile, int targetSamplesPerSecond)
        {
            if (waveFile == null)
            {
                throw new ArgumentNullException("waveFile");
            }

            if (waveFile.Riff == null)
            {
                string message = Helper.NeutralFormat("The Riff of wave file should not bu null.");
                throw new ArgumentNullException("waveFile", message);
            }

            if (waveFile.DataIn16Bits == null)
            {
                string message = Helper.NeutralFormat("The DataIn16Bits of wave file should not bu null.");
                throw new ArgumentNullException("waveFile", message);
            }

            if (waveFile.Format.BitsPerSample != SupportedBitsPerSample)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Only {0}bit waveform file supported for resampling.",
                    SupportedBitsPerSample);
                throw new NotSupportedException(message);
            }

            if (waveFile.Format.Channels != SupportedChannels)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Only {0} channel waveform file supported for resampling.",
                    SupportedChannels);
                throw new NotSupportedException(message);
            }

            // Do nothing if both samples per second are the same
            if (waveFile.Format.SamplesPerSecond != targetSamplesPerSecond)
            {
                // If both samples per second are not the same

                // Validate cache data encoded in Short
                if (waveFile.DataIn16Bits.Length != waveFile.GetSoundData().Length / sizeof(short))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "The Data in 16 bits buffer is not updated with the sound data.");
                    Debug.Assert(false, message);
                    throw new InvalidDataException(message);
                }

                ResampleFilter resample = new ResampleFilter(waveFile.Format.SamplesPerSecond,
                    targetSamplesPerSecond);

                // Re-sample
                short[] targetSamples = resample.Resample(waveFile.DataIn16Bits);

                // Update the target sound data into the WaveFile instance
                RiffChunk dataChunk = waveFile.Riff.GetChunk(Riff.IdData);
                dataChunk.SetData(ArrayHelper.BinaryConvertArray(targetSamples));

                WaveFormat format = waveFile.Format;
                format.SamplesPerSecond = targetSamplesPerSecond;
                format.AverageBytesPerSecond =
                    format.SamplesPerSecond * waveFile.Format.BitsPerSample / 8;

                waveFile.Format = format;
            }
        }

        #endregion

        #region Public instance methods

        /// <summary>
        /// Do resamples, only support 44.2k to 16k.
        /// </summary>
        /// <param name="inputSamples">Input samples.</param>
        /// <returns>Output samples.</returns>
        public short[] Resample(short[] inputSamples)
        {
            if (inputSamples == null)
            {
                throw new ArgumentNullException("inputSamples");
            }

            // Add half window length.
            const int PlusHalfWindowCount = 1;
            long outSampleNumber = 0;
            long phase = 0;
            long offset = 0;

            // check interger overflow here, OverflowException exception may throw
            outSampleNumber = checked((inputSamples.LongLength * (long)_upFactor) - _filterHalf) / _downFactor;

            if (outSampleNumber < 0)
            {
                outSampleNumber = 0;
            }

            short[] outSamples = new short[outSampleNumber];

            for (long i = 0; i < outSampleNumber; i++)
            {
                double accumulation = 0.0;

                offset = ((i * _downFactor) - (PlusHalfWindowCount * (long)_filterHalf)) / _upFactor;
                phase = (i * _downFactor) - ((offset * _upFactor) + (PlusHalfWindowCount * (long)_filterHalf));

                for (long j = 0; j < _filterLength / _upFactor; j++)
                {
                    if (_upFactor * j > phase)
                    {
                        if (offset + j >= 0 && offset + j < inputSamples.LongLength)
                        {
                            accumulation += (double)inputSamples[offset + j] * _filterCoeff[(_upFactor * j) - phase];
                        }
                        else if (offset + j < 0)
                        {
                            accumulation += (double)_leftMemory[_bufferLength + offset + j] * _filterCoeff[(_upFactor * j) - phase];
                        }
                    }
                }

                outSamples[i] = NumberConverter.Double2Int16(accumulation);
            }

            // Store samples into buffer
            offset = inputSamples.LongLength - _bufferLength;

            for (int i = 0; i < _bufferLength; i++)
            {
                if (offset >= 0)
                {
                    _leftMemory[i] = inputSamples[offset++];
                }
                else
                {
                    offset++;
                    _leftMemory[i] = 0.0f;
                }
            }

            return outSamples;
        }

        #endregion

        #region Private static methods

        /// <summary>
        /// Returns a vector with a Blackman window of the specified length.
        /// </summary>
        /// <param name="length">Window length.</param>
        /// <param name="symmetric">Whether is symmetric.</param>
        /// <returns>Blackman window.</returns>
        private static float[] Blackman(int length, bool symmetric)
        {
            float[] window = new float[length];
            double arg = 0.0;
            double arg2 = 0.0;

            arg = 2.0 * Math.PI;
            if (symmetric)
            {
                arg /= (float)(length - 1);
            }
            else
            {
                arg /= (float)length;
            }

            arg2 = 2.0 * arg;

            for (int i = 0; i < length; i++)
            {
                window[i] = (float)(0.42 - (0.5 * Math.Cos(arg * i)) + (0.08 * Math.Cos(arg2 * i)));
            }

            return window;
        }

        #endregion

        #region Private instance methods

        /// <summary>
        /// Creates a low pass filter using the windowing method.
        /// DCutOff is spec. in normalized frequency.
        /// </summary>
        /// <param name="cutOff">Cut off value.</param>
        /// <param name="gain">Gain value.</param>
        /// <returns>Low passed window data.</returns>
        private float[] WindowedLowPass(float cutOff, float gain)
        {
            float[] coeffs = null;
            float[] window = null;
            double arg = 0;
            double sinc = 0;

            System.Diagnostics.Debug.Assert(cutOff > 0.0 && cutOff < 0.5);

            window = Blackman(_filterLength, true);

            coeffs = new float[_filterLength];

            arg = 2.0f * Math.PI * cutOff;

            coeffs[_filterHalf] = (float)(gain * 2.0 * cutOff);

            for (long i = 1; i <= _filterHalf; i++)
            {
                sinc = gain * Math.Sin(arg * i) / (Math.PI * i) * window[_filterHalf - i];
                coeffs[_filterHalf + i] = (float)sinc;
                coeffs[_filterHalf - i] = (float)sinc;
            }

            return coeffs;
        }

        /// <summary>
        /// Find resample factors.
        /// </summary>
        /// <param name="inputSamplesPerSecond">Input samples per second.</param>
        /// <param name="outputSamplesPerSecond">Output samples per second.</param>
        private void FindResampleFactors(int inputSamplesPerSecond,
            int outputSamplesPerSecond)
        {
            int div = 1;

            while (div != 0)
            {
                div = 0;
                for (int i = 0; i < PIPrimes.Length; i++)
                {
                    if ((inputSamplesPerSecond % PIPrimes[i]) == 0 &&
                        (outputSamplesPerSecond % PIPrimes[i]) == 0)
                    {
                        inputSamplesPerSecond /= PIPrimes[i];
                        outputSamplesPerSecond /= PIPrimes[i];
                        div = 1;
                        break;
                    }
                }
            }

            _upFactor = outputSamplesPerSecond;
            _downFactor = inputSamplesPerSecond;
        }

        #endregion
    }
}