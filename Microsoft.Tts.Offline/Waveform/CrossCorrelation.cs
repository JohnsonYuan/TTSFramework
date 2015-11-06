//----------------------------------------------------------------------------
// <copyright file="CrossCorrelation.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Cross Correlation
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// CrossCorrelation.
    /// </summary>
    public static class CrossCorrelation
    {
        #region Calculate Cross Correlation

        /// <summary>
        /// Calculate the Cross Correlation of two short arrays.
        /// </summary>
        /// <param name="arrayA">Array A.</param>
        /// <param name="arrayB">Array B.</param>
        /// <param name="normalize">If normalize or not.</param>
        /// The Formulae is result[2N-1] = IFFT(FFT(arrayA)*Conjucate(FFT(arrayB)))
        /// <returns>Cross Correlation result.</returns>
        public static float[] CalcCorrelation(short[] arrayA, short[] arrayB, bool normalize)
        {
            if (arrayA.Length != arrayB.Length)
            {
                throw new GeneralException("CrossCorrlation.CalcCorrelation: The size of arrayA and arrayB is not match");
            }

            int length = arrayA.Length;
            int expandedLength = Expand(length);

            short[] fullArrayA = new short[expandedLength];
            short[] fullArrayB = new short[expandedLength];

            // Short is 2 bytes
            int bytesToCopy = length * 2;
            Buffer.BlockCopy(arrayA, 0, fullArrayA, 0, bytesToCopy);
            Buffer.BlockCopy(arrayB, 0, fullArrayB, 0, bytesToCopy);

            Complex[] complexesA = new Complex[expandedLength];
            Complex[] complexesB = new Complex[expandedLength];
            Complex[] complexesResult = new Complex[expandedLength];

            Complex.Short2Complex(fullArrayA, ref complexesA, true);
            Complex.Short2Complex(fullArrayB, ref complexesB, true);

            Fft.Transfer(false, ref complexesA);
            Fft.Transfer(false, ref complexesB);

            Complex.Conjugate(ref complexesB);

            for (int i = 0; i < expandedLength; i++)
            {
                complexesResult[i] = complexesA[i] * complexesB[i];
            }

            Fft.Transfer(true, ref complexesResult);

            // Copy result to a float array. 
            // The last N-1 values in cResult copy to fResult[0...N-2]
            // The first N values in cResult copy to fResult[N-1...2N-2]
            float[] result = new float[(length * 2) - 1];

            for (int i = 0; i < length - 1; i++)
            {
                result[i] = complexesResult[expandedLength - length + 1 + i].Real;
            }

            for (int i = length - 1; i < (length * 2) - 1; i++)
            {
                result[i] = complexesResult[i - length + 1].Real;
            }

            // Normalize the result
            // The result should divide the number of samples that used to calculate the cross correlation
            if (normalize)
            {
                for (int i = 0; i < length; i++)
                {
                    result[i] /= 1 + i;
                }

                for (int i = length; i < (length * 2) - 1; i++)
                {
                    result[i] /= (length * 2) - i - 1;
                }
            }

            return result;
        }

        /// <summary>
        /// Expand the length of original array to the least 2's power that greater than (2 * length - 1).
        /// </summary>
        /// <param name="length">Input length.</param>
        /// <returns>The least 2's power greater than length.</returns>
        public static int Expand(int length)
        {
            if (length < 0)
            {
                throw new GeneralException("CrossCorrlation.Expand: The length to expand can not be negative. ");
            }

            int logLength = (int)Math.Ceiling(Math.Log(((length * 2) - 1), 2));
            int newLength = (int)Math.Pow(2, logLength);
            return newLength;
        }
        #endregion
    }
}