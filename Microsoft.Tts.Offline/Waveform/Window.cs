//----------------------------------------------------------------------------
// <copyright file="Window.cs" company="MICROSOFT">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements DSP window functions
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;

    /// <summary>
    /// Window type enum.
    /// </summary>
    public enum WindowType
    {
        /// <summary>
        /// Bartlett (triangular) window.
        /// </summary>
        Bartlett,

        /// <summary>
        /// Kaiser window.
        /// </summary>
        Kaiser
    }

    /// <summary>
    /// Build window vectors.
    /// </summary>
    public class Window
    {
        /// <summary>
        /// Build the symetric Bartlett (triangular) window, with the peak as 1 and two ends as 0.
        /// </summary>
        /// <param name="length">Window length.</param>
        /// <returns>Window vector.</returns>
        public static double[] Bartlett(int length)
        {
            if (length < 2)
            {
                throw new ArgumentOutOfRangeException("length", "length must be greater than 1");
            }

            double[] window = new double[length];
            int order = length - 1;

            for (int i = 0; i < window.Length; i++)
            {
                window[i] = 1.0 - ((2.0 * Math.Abs(i - (order / 2.0))) / order);
            }

            return window;
        }

        /// <summary>
        /// Build symetric Kaiser window, with peak as 1.
        /// </summary>
        /// <param name="length">The Length.</param>
        /// <param name="beta">The beta value.</param>
        /// <returns>The kaiser double value.</returns>
        public static double[] Kaiser(int length, double beta)
        {
            if (length < 2)
            {
                throw new ArgumentOutOfRangeException("length", "length must be greater than 1");
            }

            double[] window = new double[length];
            int order = length - 1;

            double scale = Math.Abs(BesselI0(beta));
            for (int i = 0; i < window.Length; i++)
            {
                window[i] = BesselI0(beta * Math.Sqrt(1 - Math.Pow(((2.0 * i) / order) - 1, 2)));
                window[i] /= scale;
            }

            return window;
        }

        /// <summary>
        /// The 0th order modified Bessel funtion
        /// Reference: http://mathworld.wolfram.com/ModifiedBesselFunctionoftheFirstKind.html.
        /// </summary>
        /// <param name="x">The input x.</param>
        /// <returns>The output double value.</returns>
        public static double BesselI0(double x)
        {
            double termK = 1;
            double sum = 0;
            double lastSum = 0;
            int k = 1;

            do
            {
                lastSum = sum;
                termK *= Math.Pow((x / 2) / k, 2);
                sum += termK;
                k++;
            }
            while (sum != lastSum);

            return sum;
        }
    }
}