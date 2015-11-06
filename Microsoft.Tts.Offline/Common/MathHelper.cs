//-------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="MathHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This file contains the common functions related to mathimatic operation.
// </summary>
//--------------------------------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.Win32;

    /// <summary>
    /// Common math functions.
    /// </summary>
    public class MathHelper
    {
        /// <summary>
        /// The class for all kinds of Log scale functions.
        /// </summary>
        public class LogFunctions
        {
            /// <summary>
            /// The minimal log value (corresponds to 0.0).
            /// </summary>
            public static double LogMin = -1.0E5;

            /// <summary>
            /// Log Add function.
            /// </summary>
            /// <param name="logNum1">The fist number to be added.</param>
            /// <param name="logNum2">The second number to be added.</param>
            /// <returns>The log scale sum value.</returns>
            public static double Add(double logNum1, double logNum2)
            {
                if (logNum1 < logNum2)
                {
                    double temp = logNum1;
                    logNum1 = logNum2;
                    logNum2 = temp;
                }

                double logSum = LogMin;
                if (logNum1 < LogMin)
                {
                    logSum = LogMin;
                }
                else if (logNum2 < LogMin)
                {
                    logSum = logNum1;
                }
                else
                {
                    logSum = logNum1 + Math.Log(1.0 + Math.Exp(logNum2 - logNum1));
                }

                return logSum;
            }

            /// <summary>
            /// Log Multiply function.
            /// </summary>
            /// <param name="logNum1">The multiplier 1.</param>
            /// <param name="logNum2">The multiplier 2.</param>
            /// <returns>The log scale product value.</returns>
            public static double Multiply(double logNum1, double logNum2)
            {
                return logNum1 + logNum2;
            }

            /// <summary>
            /// Log Average function.
            /// </summary>
            /// <param name="logNums">The log scale values.</param>
            /// <returns>The log scale average values.</returns>
            public static double Average(ICollection<double> logNums)
            {
                double average = LogMin;
                foreach (double logNum in logNums)
                {
                    average = Add(average, logNum);
                }

                if (logNums.Count > 0 && average > LogMin)
                {
                    average -= Math.Log(logNums.Count * 1.0);
                }

                return average;
            }
        }

        /// <summary>
        /// The Range class.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        public class Range<T>
        {
            /// <summary>
            /// The minimal value.
            /// </summary>
            public T Min;

            /// <summary>
            /// The mean value.
            /// </summary>
            public T Mean;

            /// <summary>
            /// The median value.
            /// </summary>
            public T Median;

            /// <summary>
            /// The maximum value.
            /// </summary>
            public T Max;
        }

        /// <summary>
        /// The least square fitter class.
        /// </summary>
        public class LeastSquaresFitter
        {
            /// <summary>
            /// Perform linear fitting on the inputing vectors.
            /// </summary>
            /// <param name="variableXs">The independent variable vector.</param>
            /// <param name="variableYs">The dependent variable vector.</param>
            /// <returns>The linear fitting result.</returns>
            public static LinearFittingResult DoLinearFitting(IList<double> variableXs, IList<double> variableYs)
            {
                if (variableXs.Count != variableYs.Count || variableXs.Count <= 0)
                {
                    throw new Exception("The dimentions of input variable vectors do not match or the input variable vectors are empty");
                }

                double averageX = 0.0;
                double averageY = 0.0;
                for (int i = 0; i < variableXs.Count; i++)
                {
                    averageX += variableXs[i];
                    averageY += variableYs[i];
                }

                averageX /= variableXs.Count * 1.0;
                averageY /= variableYs.Count * 1.0;

                double sumSquaresXX = 0.0;
                double sumSquaresYY = 0.0;
                double sumSquaresXY = 0.0;
                for (int i = 0; i < variableXs.Count; i++)
                {
                    sumSquaresXX += Math.Pow(variableXs[i] - averageX, 2.0);
                    sumSquaresYY += Math.Pow(variableYs[i] - averageY, 2.0);
                    sumSquaresXY += (variableXs[i] - averageX) * (variableYs[i] - averageY);
                }

                LinearFittingResult linearFittingResult = new LinearFittingResult()
                {
                    Offset = averageY - ((sumSquaresXY / sumSquaresXX) * averageX),
                    Slope = sumSquaresXY / sumSquaresXX,
                    CorrelationCoefficient = Math.Sqrt(Math.Pow(sumSquaresXY, 2.0) / sumSquaresXX / sumSquaresYY),
                    AverageX = averageX,
                    AverageY = averageY
                };

                return linearFittingResult;
            }

            /// <summary>
            /// The least square fitting result structure
            /// Y = aX + b
            /// A: the fitting slope
            /// B: the offset.
            /// </summary>
            public struct LinearFittingResult
            {
                /// <summary>
                /// The offset value.
                /// </summary>
                public double Offset;

                /// <summary>
                /// The slope of fitting line.
                /// </summary>
                public double Slope;

                /// <summary>
                /// The correlation coefficient.
                /// </summary>
                public double CorrelationCoefficient;

                /// <summary>
                /// The average value of X.
                /// </summary>
                public double AverageX;

                /// <summary>
                /// The average value of Y.
                /// </summary>
                public double AverageY;
            }
        }
    }
}