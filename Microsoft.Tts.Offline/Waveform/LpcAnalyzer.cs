//----------------------------------------------------------------------------
// <copyright file="LpcAnalyzer.cs" company="MICROSOFT">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements LpcAnalyzer
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

    /// <summary>
    /// LPC analyzer.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
        "CA1053:StaticHolderTypesShouldNotHaveConstructors", Justification = "Ignore.")]
    public class LpcAnalyzer
    {
        #region Const definition

        /// <summary>
        /// Parameter used to compute LPC parameters of desired region.
        /// </summary>
        public const double PerceptualBandwidth = 14.05;

        #endregion

        #region Public static method

        /// <summary>
        /// Compute LPC parameters for a region of the input waveform.
        /// </summary>
        /// <param name="epochs">Epoch data.</param>
        /// <param name="epochOffset">Epoch offset point in the data.</param>
        /// <param name="epochNumber">Epoch number.</param>
        /// <param name="window">Filter window.</param>
        /// <param name="frameSize">Frame size.</param>
        /// <param name="samples">Waveform samples.</param>
        /// <param name="sampleNumber">Sample number.</param>
        /// <param name="samplesPerSecond">Samples per second.</param>
        /// <param name="lpcFrames">LPC frames.</param>
        /// <param name="lpcOrder">LPC order.</param>
        public static void Analyze(float[] epochs, int epochOffset,
            int epochNumber, float[] window, int frameSize,
            float[] samples, int sampleNumber, int samplesPerSecond,
            float[] lpcFrames, int lpcOrder)
        {
            ////#region Argument validate

            if (epochs == null || epochs.Length == 0)
            {
                throw new ArgumentNullException("epochs");
            }

            if (window == null || window.Length == 0)
            {
                throw new ArgumentNullException("window");
            }

            if (samples == null || samples.Length == 0)
            {
                throw new ArgumentNullException("samples");
            }

            ////#endregion

            int sampleOffset = 0;
            for (int i = 0; i < epochOffset; i++)
            {
                sampleOffset += checked((int)Math.Abs(epochs[i]));
            }

            int epochEnd = epochOffset + epochNumber;

            // compute LPC parameters of desired region
            float alpha = (float)Math.Exp(-Math.PI * PerceptualBandwidth /
                samplesPerSecond);

            /////#region Identify the desired beginning
            int desiredEpochOffset = epochOffset;
            int sampleIndex = sampleOffset - (frameSize / 2);
            for (; sampleIndex < 0; desiredEpochOffset++)
            {
                sampleIndex += checked((int)Math.Abs(epochs[desiredEpochOffset]));
            }
            ////#endregion

            int currSampleOffset = sampleIndex;
            int lpcFrameOffset = (desiredEpochOffset - epochOffset) *
                (1 + lpcOrder);

            ////#region Identify the desired ending
            int desiredEpochEnd = epochEnd;
            sampleIndex = sampleOffset + sampleNumber + (frameSize / 2);
            for (; sampleIndex > samples.Length; desiredEpochEnd--)
            {
                sampleIndex -= checked((int)Math.Abs(epochs[desiredEpochEnd - 1]));
            }
            ////#endregion

            for (int i = desiredEpochOffset; i < desiredEpochEnd; i++)
            {
                // if Durbin recursion ends prematurely at frame continue process
                AnalyzeLpcFrame(samples, currSampleOffset, window, lpcOrder,
                    alpha, lpcFrames, lpcFrameOffset);
                currSampleOffset += checked((int)Math.Abs(epochs[i]));
                lpcFrameOffset += lpcOrder + 1;
            }

            // copy first LPC frame (epoch) from desired first one if out of range
            lpcFrameOffset = 0;
            for (int i = epochOffset; i < desiredEpochOffset; i++)
            {
                for (int j = 0; j <= lpcOrder; j++)
                {
                    lpcFrames[lpcFrameOffset + j] = lpcFrames[(desiredEpochOffset * (1 + lpcOrder)) + j];
                }

                lpcFrameOffset += lpcOrder + 1;
            }

            // copy last LPC frame (epoch) from desired last one if out of range
            lpcFrameOffset = desiredEpochEnd * (1 + lpcOrder);
            for (int i = desiredEpochEnd; i < epochEnd; i++)
            {
                for (int j = 0; j <= lpcOrder; j++)
                {
                    lpcFrames[lpcFrameOffset + j] =
                        lpcFrames[((desiredEpochEnd - 1) * (1 + lpcOrder)) + j];
                }

                lpcFrameOffset += 1 + lpcOrder;
            }
        }

        /// <summary>
        /// Generates a Hamming window with given window size.
        /// </summary>
        /// <param name="size">Windows size.</param>
        /// <returns>Filter window.</returns>
        public static float[] CreateHammingWindow(int size)
        {
            float[] window = new float[size];

            double alpha = 2.0 * Math.PI / size;

            for (int i = 0; i < size; i++)
            {
                window[i] = (float)(0.54 - (0.46 * Math.Cos(alpha * i)));
            }

            return window;
        }

        /// <summary>
        /// Change sign of the waveform.
        /// </summary>
        /// <param name="samples">Waveform samples.</param>
        public static void Invert(float[] samples)
        {
            if (samples == null || samples.Length == 0)
            {
                throw new ArgumentNullException("samples");
            }

            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = -samples[i];
            }
        }

        /// <summary>
        /// Preprocess input waveform samples. remove DC (direct current).
        /// </summary>
        /// <param name="samples">Waveform samples.</param>
        /// <returns>Waveform samples with direct current removed.</returns>
        public static float[] RemoveDirectCurrent(short[] samples)
        {
            if (samples == null || samples.Length == 0)
            {
                throw new ArgumentNullException("samples");
            }

            float[] ret = new float[samples.Length];

            // compute DC component for the whole speech data
            float dc = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                dc += (float)samples[i];
            }

            dc /= samples.Length;

            // remove DC component
            for (int i = 0; i < samples.Length; i++)
            {
                ret[i] = samples[i] - dc;
            }

            return ret;
        }

        /// <summary>
        /// Finds exact pitch-synchronous frames to cut for all the samples
        /// That has the epochs.
        /// </summary>
        /// <param name="epochs">Epoch data.</param>
        /// <param name="epochStartIndex">Start index.</param>
        /// <param name="epochStopIndex">Stop index.</param>
        public static void AdjustStartStop(float[] epochs, ref int epochStartIndex,
            ref int epochStopIndex)
        {
            if (epochs == null || epochs.Length == 0)
            {
                throw new ArgumentNullException("epochs");
            }

            int sampleNumber = 0;
            for (int i = 0; i < epochs.Length; i++)
            {
                sampleNumber += checked((int)Math.Abs(epochs[i]));
            }

            FindBoundryEpoch(0, sampleNumber, epochs, ref epochStartIndex, ref epochStopIndex);
        }

        /// <summary>
        /// Finds exact pitch-synchronous frames to cut between startSample
        /// And stopSample.
        /// </summary>
        /// <param name="startSample">Start sample index.</param>
        /// <param name="stopSample">Stop sample index.</param>
        /// <param name="epochs">Epoch data.</param>
        /// <param name="epochStartIndex">Start epoch index.</param>
        /// <param name="epochStopIndex">Stop epoch index.</param>
        public static void FindBoundryEpoch(int startSample, int stopSample,
            float[] epochs, ref int epochStartIndex, ref int epochStopIndex)
        {
            if (epochs == null || epochs.Length == 0)
            {
                throw new ArgumentNullException("epochs");
            }

            if ((0 > startSample) || (startSample >= stopSample))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid startSample/stopSample when AdjustStartStop epoch" + "position, startSample = {0} , stopSample = {1} .",
                    startSample, stopSample);
                throw new InvalidDataException(message);
            }

            // find the sample number for the first frame to be included
            // two possible start points are selected around StartSample
            // sampleStartMinus <= StartSample <= SampleStartPlus
            int sampleStartPlus = 0;
            while (epochStartIndex < epochs.Length && sampleStartPlus < startSample)
            {
                sampleStartPlus += checked((int)Math.Abs(epochs[epochStartIndex]));
                epochStartIndex++;
            }

            int sampleStartMinus = sampleStartPlus;
            if (epochStartIndex > 0)
            {
                sampleStartMinus -= checked((int)Math.Abs(epochs[epochStartIndex - 1]));
            }

            // start points to sampleStartPlus
            // find the milliseconds for the last frame to be included
            // two possible stop points are selected around StopSample
            // sampleStopMinus <= StopSample <= SampleStopPlus
            epochStopIndex = epochStartIndex;
            int sampleStopPlus = sampleStartPlus;
            while (epochStopIndex < epochs.Length && sampleStopPlus < stopSample)
            {
                sampleStopPlus += checked((int)Math.Abs(epochs[epochStopIndex]));
                epochStopIndex++;
            }

            int sampleStopMinus = sampleStopPlus;
            if (epochStopIndex > 0)
            {
                sampleStopMinus -= checked((int)Math.Abs(epochs[epochStopIndex - 1]));
                --epochStopIndex;
            }

            int closestStartSample = 0;

            // epochStopIndex now points to the first frame *excluded* i.e.
            // sampleStopMinus, Choose start point closest to that requested
            if ((startSample - sampleStartMinus) < (sampleStartPlus - startSample))
            {
                --epochStartIndex;
                closestStartSample = sampleStartMinus;
            }
            else
            {
                closestStartSample = sampleStartPlus;
            }

            // now choose the stopping point based on
            // minimizing the error on unit duration
            int duration = stopSample - startSample;
            if (Math.Abs(duration - (sampleStopMinus - closestStartSample)) >
                Math.Abs(duration - (sampleStopPlus - closestStartSample)))
            {
                ++epochStopIndex;
            }

            // check that region contains at least 1 epoch
            if (epochStopIndex <= epochStartIndex)
            {
                epochStopIndex = epochStartIndex + 1;
            }
        }

        /// <summary>
        /// FIR Filter.
        /// This routine computes the residual signal.
        /// PFilter[0] is supposed to contain the filter gain.
        /// </summary>
        /// <param name="waveSamples">Waveform samples.</param>
        /// <param name="sampleOffset">Sample offset.</param>
        /// <param name="sampleNumber">Sample number.</param>
        /// <param name="lpcFrames">LPC frames.</param>
        /// <param name="lpcFrameOffset">LPC frame offset.</param>
        /// <param name="lpcOrder">LPC order.</param>
        /// <param name="residualSamples">Residual samples.</param>
        /// <param name="residualSampleOffset">Residual sample offset.</param>
        public static void InverseLpcFilter(float[] waveSamples, int sampleOffset,
            int sampleNumber, float[] lpcFrames, int lpcFrameOffset, int lpcOrder,
            float[] residualSamples, int residualSampleOffset)
        {
            ////#region Parameter validate

            if (waveSamples == null || waveSamples.Length == 0)
            {
                throw new ArgumentNullException("waveSamples");
            }

            if (residualSamples == null || residualSamples.Length == 0)
            {
                throw new ArgumentNullException("residualSamples");
            }

            if (lpcFrames == null || lpcFrames.Length == 0)
            {
                throw new ArgumentNullException("lpcFrames");
            }

            ////#endregion

            // inverse LPC filtering
            float gain = 1.0f / lpcFrames[0];

            for (int i = 0; i < sampleNumber; i++)
            {
                float residual = waveSamples[sampleOffset + i];
                for (int j = 1; j <= lpcOrder; j++)
                {
                    if (sampleOffset + i - j >= 0)
                    {
                        residual += lpcFrames[lpcFrameOffset + j] *
                            waveSamples[sampleOffset + i - j];
                    }
                }

                residualSamples[residualSampleOffset + i] = gain * residual;
            }
        }

        #endregion

        #region Private static method

        /// <summary>
        /// Analyzes a frame of speech, decomposing it into excitation and
        /// Linear filter. In this case we use LPC parameters.
        /// </summary>
        /// <param name="samples">Waveform samples.</param>
        /// <param name="sampleOffset">Sample offset.</param>
        /// <param name="window">Filter window.</param>
        /// <param name="lpcOrder">LPC order.</param>
        /// <param name="alpha">Alpha .</param>
        /// <param name="lpc">LPC frames.</param>
        /// <param name="lpcOffset">LPC frame offset.</param>
        /// <returns>false : Durbin recursion ends prematurely at frame,
        /// it doesn't affect the process.</returns>
        private static bool AnalyzeLpcFrame(float[] samples, int sampleOffset,
            float[] window, int lpcOrder, float alpha, float[] lpc,
            int lpcOffset)
        {
            // window input data
            const int MaxWindowLength = 1000;
            const int MaxOrder = 30;

            if (window.Length >= MaxWindowLength)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "window's size must be less than {0}, current window's size is {1}",
                    MaxWindowLength, window.Length);

                throw new ArgumentException(message, "window");
            }

            if (lpcOrder >= MaxOrder)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "lpcOrder must be less than {0}, current lpcOrder is {1}",
                    MaxOrder, lpcOrder);
                throw new ArgumentException(message, "lpcOrder");
            }

            // window the input speech sample data
            float[] windowedSamples = new float[samples.Length];
            for (int i = 0; i < window.Length; i++)
            {
                windowedSamples[i] = samples[sampleOffset + i] * window[i];
            }

            // compute autocorrelation
            double[] autos = CalcAcc(windowedSamples, window.Length, lpcOrder);

            if (autos.Length <= 1)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "autocorrelation coefficients number is too little for " + "further processing, the number is {0}.",
                    autos.Length);
                throw new InvalidDataException(message);
            }

            // high Frequency compensation
            autos[0] *= 1.0f;
            double frameGain;
            float[] inputLpc;

            // compute LPC parameters, inLpc is the same length with autos
            if (!Durbin(autos, out inputLpc, out frameGain))
            {
                return false;
            }

            // bandwidth expansion for increased LPC stability for female speech
            PoleExpansion(inputLpc, alpha, lpc, lpcOffset);

            lpc[lpcOffset] = 1.0f;
            return true;
        }

        /// <summary>
        /// Expands bandwidth of all poles in an LPC system.
        /// </summary>
        /// <param name="inputLPC">Input LPC.</param>
        /// <param name="alpha">Alpha.</param>
        /// <param name="outputLpc">Output LPC.</param>
        /// <param name="outputLpcOffset">Offset of output LPC.</param>
        private static void PoleExpansion(float[] inputLPC, float alpha,
            float[] outputLpc, int outputLpcOffset)
        {
            float gamma = 1.0f;
            outputLpc[outputLpcOffset] = 1.0f;
            for (int j = 1; j < inputLPC.Length; j++)
            {
                gamma *= alpha;
                outputLpc[outputLpcOffset + j] = inputLPC[j] * gamma;
            }
        }

        /// <summary>
        /// Classical Durbin recursion on the autocorrelation coefficients
        /// To compute the linear prediction coefficients. The reflection
        /// Coefficients are also computed.
        /// The sign convention used defines the first reflection coefficient
        /// As the normalized first autocorrelation coefficient, which results
        /// In positive values of rc[0] for voiced speech.
        /// </summary>
        /// <param name="autos">Autos.</param>
        /// <param name="lpc">LPC.</param>
        /// <param name="resid">Residual.</param>
        /// <returns>True if succeeded, otherwise false.</returns>
        private static bool Durbin(double[] autos, out float[] lpc, out double resid)
        {
            lpc = null;
            resid = 0;

            // frame energy has to be positive
            if (autos[0] <= 0)
            {
                return false;
            }

            // normalize autocorrelation coefficients
            double value = 1.0f / autos[0];
            for (int i = 1; i < autos.Length; i++)
            {
                autos[i] *= value;
            }

            lpc = new float[autos.Length];

            // initialize variables
            value = autos[1];
            lpc[1] = -(float)value;
            lpc[0] = 1.0f;
            resid = 1.0 - (value * value);

            // do recursion
            for (int i = 2; i < autos.Length; i++)
            {
                value = autos[i];
                for (int j = 1; j < i; j++)
                {
                    value += lpc[j] * autos[i - j];
                }

                value /= resid;

                // check if reflection coefficient is within boundaries
                if (value <= -1.0 || value >= 1.0)
                {
                    for (int j = i; j < autos.Length; j++)
                    {
                        lpc[j] = 0.0f;
                    }

                    return false;
                }

                lpc[i] = -(float)value;
                for (int j = 1; 2 * j <= i; j++)
                {
                    double alfsave = lpc[j];
                    lpc[j] = (float)(alfsave - (value * lpc[i - j]));
                    if (2 * j != i)
                    {
                        lpc[i - j] -= (float)(value * alfsave);
                    }
                }

                resid *= 1.0 - (value * value);
            }

            return true;
        }

        /// <summary>
        /// Calculate
        ///     from input signal x[0], x[1], ..., x[N - 1]
        ///     to autocorrelation coefficients (ACC) R[0], R[1], ..., R[p].
        /// </summary>
        /// <param name="samples">Samples.</param>
        /// <param name="windowLength">Window length.</param>
        /// <param name="order">Predictor order.</param>
        /// <returns>Autocorrelation coefficients.</returns>
        private static double[] CalcAcc(float[] samples, int windowLength,
            int order)
        {
            int pos1 = 0;
            int pos2 = 0;
            double sum = 0;
            double[] outputAutos = new double[order + 1];

            for (int i = 0; i < outputAutos.Length; i++)
            {
                pos1 = 0;
                pos2 = i;
                sum = 0.0;

                for (int j = 0; j < windowLength - i; j++)
                {
                    sum += samples[pos1] * samples[pos2];
                    pos1++;
                    pos2++;
                }

                outputAutos[i] = sum;
            }

            return outputAutos;
        }

        #endregion
    }
}