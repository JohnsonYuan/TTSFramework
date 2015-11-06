//----------------------------------------------------------------------------
// <copyright file="NumericStatistics.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements numeric statistics class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Research
{
    using System;

    /// <summary>
    /// Define the unary numeric statistics class.
    /// </summary>
    public class UnaryNumericStatistics
    {
        #region fields

        private int _count;
        private double _sum;
        private double _sumSquare;

        private double _lastSample;

        private double _min;
        private double _max;

        private double _minSuccessiveDiff;
        private double _maxSuccessiveDiff;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UnaryNumericStatistics"/> class.
        /// </summary>
        public UnaryNumericStatistics()
        {
            ResetStatistics();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets Sample count.
        /// </summary>
        public int Count
        {
            get
            {
                return _count;
            }
        }

        /// <summary>
        /// Gets Sum of samples.
        /// </summary>
        public double Sum
        {
            get
            {
                return _sum;
            }
        }

        /// <summary>
        /// Gets Sum of square of samples.
        /// </summary>
        public double SumSquare
        {
            get
            {
                return _sumSquare;
            }
        }

        /// <summary>
        /// Gets Min.
        /// </summary>
        public double Min
        {
            get
            {
                return _min;
            }
        }

        /// <summary>
        /// Gets Max.
        /// </summary>
        public double Max
        {
            get
            {
                return _max;
            }
        }

        /// <summary>
        /// Gets Range.
        /// </summary>
        public double Range
        {
            get
            {
                return _max - _min;
            }
        }

        /// <summary>
        /// Gets Mean.
        /// </summary>
        public double Mean
        {
            get
            {
                if (_count == 0)
                {
                    throw new InvalidOperationException("Can't calculate mean since no sample!");
                }

                double result = _sum / _count;
                return result;
            }
        }

        /// <summary>
        /// Gets Standard deviation.
        /// </summary>
        public double Std
        {
            get
            {
                if (_count == 0)
                {
                    throw new InvalidOperationException("Can't calculate standard deviation since no sample!");
                }

                double mean = Mean;
                double result = Math.Sqrt((_sumSquare / _count) - (mean * mean));
                return result;
            }
        }

        /// <summary>
        /// Gets Min succesive difference.
        /// </summary>
        public double MinSuccessiveDiff
        {
            get
            {
                return _minSuccessiveDiff;
            }
        }

        /// <summary>
        /// Gets Max succesive difference.
        /// </summary>
        public double MaxSuccessiveDiff
        {
            get
            {
                return _maxSuccessiveDiff;
            }
        }

        #endregion

        #region public operations

        /// <summary>
        /// Clear statistics.
        /// </summary>
        public void ResetStatistics()
        {
            _count = 0;
            _sum = 0F;
            _sumSquare = 0F;

            _lastSample = double.NaN;

            _min = double.MaxValue;
            _max = double.MinValue;

            _minSuccessiveDiff = double.MaxValue;
            _maxSuccessiveDiff = double.MinValue;
        }

        /// <summary>
        /// Add a sample point.
        /// </summary>
        /// <param name="sample">Value of the sample.</param>
        public void AddSample(double sample)
        {
            _count++;
            _sum += sample;
            _sumSquare += sample * sample;

            if (_min > sample)
            {
                _min = sample;
            }

            if (_max < sample)
            {
                _max = sample;
            }

            if (_count > 1)
            {
                double diff = sample - _lastSample;
                if (_minSuccessiveDiff > diff)
                {
                    _minSuccessiveDiff = diff;
                }

                if (_maxSuccessiveDiff < diff)
                {
                    _maxSuccessiveDiff = diff;
                }
            }

            _lastSample = sample;
        }

        #endregion
    }

    /// <summary>
    /// Define the binary numeric statistics class.
    /// </summary>
    public class BinaryNumericStatistics
    {
        #region fields

        private const double MinStdValue = 1e-100F;

        private UnaryNumericStatistics _statX;
        private UnaryNumericStatistics _statY;

        private int _count;
        private double _sumXY;

        private double _minBias;
        private double _maxBias;

        #endregion

        #region Consturctor

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryNumericStatistics"/> class.
        /// </summary>
        public BinaryNumericStatistics()
        {
            _statX = new UnaryNumericStatistics();
            _statY = new UnaryNumericStatistics();

            ResetStatistics();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets Unary statistics for X.
        /// </summary>
        public UnaryNumericStatistics StatX
        {
            get
            {
                return _statX;
            }
        }

        /// <summary>
        /// Gets Unary statistics for Y.
        /// </summary>
        public UnaryNumericStatistics StatY
        {
            get
            {
                return _statY;
            }
        }

        /// <summary>
        /// Gets Sample count.
        /// </summary>
        public int Count
        {
            get
            {
                return _count;
            }
        }

        /// <summary>
        /// Gets Sum of multiple of sample X and sample Y.
        /// </summary>
        public double SumXY
        {
            get
            {
                return _sumXY;
            }
        }

        /// <summary>
        /// Gets Bias (x - y).
        /// </summary>
        public double Bias
        {
            get
            {
                if (_count == 0)
                {
                    throw new InvalidOperationException("Can't calculate bias since no sample!");
                }

                double result = (_statX.Sum - _statY.Sum) / _count;
                return result;
            }
        }

        /// <summary>
        /// Gets RMSE.
        /// </summary>
        public double Rmse
        {
            get
            {
                if (_count == 0)
                {
                    throw new InvalidOperationException("Can't calculate RMSE since no sample!");
                }

                double result = Math.Sqrt((_statX.SumSquare + _statY.SumSquare - (2 * _sumXY)) / _count);
                return result;
            }
        }

        /// <summary>
        /// Gets Correlation.
        /// </summary>
        public double Correlation
        {
            get
            {
                if (_count == 0)
                {
                    throw new InvalidOperationException("Can't calculate correlation since no sample!");
                }

                double result;

                double stdX = _statX.Std;
                double stdY = _statY.Std;
                if (stdX <= MinStdValue || stdY <= MinStdValue)
                {
                    // make no sense in such case
                    result = 0;
                }
                else
                {
                    result = ((_sumXY / _count) - (_statX.Mean * _statY.Mean)) / (stdX * stdY);
                }

                return result;
            }
        }

        /// <summary>
        /// Gets Min bias.
        /// </summary>
        public double MinBias
        {
            get
            {
                return _minBias;
            }
        }

        /// <summary>
        /// Gets Max bias.
        /// </summary>
        public double MaxBias
        {
            get
            {
                return _maxBias;
            }
        }

        #endregion

        #region public operations

        /// <summary>
        /// Clear statistics.
        /// </summary>
        public void ResetStatistics()
        {
            _statX.ResetStatistics();
            _statY.ResetStatistics();

            _count = 0;
            _sumXY = 0F;

            _minBias = double.MaxValue;
            _maxBias = double.MinValue;
        }

        /// <summary>
        /// Add a sample pair.
        /// </summary>
        /// <param name="sampleX">Value of the sample X.</param>
        /// <param name="sampleY">Value of the sample Y.</param>
        public void AddSample(double sampleX, double sampleY)
        {
            _statX.AddSample(sampleX);
            _statY.AddSample(sampleY);

            _count++;
            _sumXY += sampleX * sampleY;

            double bias = sampleX - sampleY;
            if (_minBias > bias)
            {
                _minBias = bias;
            }

            if (_maxBias < bias)
            {
                _maxBias = bias;
            }
        }

        #endregion
    }
}