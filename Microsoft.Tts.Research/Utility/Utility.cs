//----------------------------------------------------------------------------
// <copyright file="Utility.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements rude compare class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Research
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Define a rude comparer for Int16 which use absolute delta.
    /// </summary>
    public class Int16AbsRudeCompare : IEqualityComparer<short>
    {
        /// <summary>
        /// Delta for comparison.
        /// </summary>
        private short _delta;

        /// <summary>
        /// Initializes a new instance of the <see cref="Int16AbsRudeCompare"/> class.
        /// </summary>
        /// <param name="delta">Delta for comparison.</param>
        public Int16AbsRudeCompare(short delta)
        {
            _delta = delta;
        }

        /// <summary>
        /// Override Equals().
        /// </summary>
        /// <param name="x">The left-hand-side.</param>
        /// <param name="y">The right-hand-side.</param>
        /// <returns>True if equal, false if not.</returns>
        public bool Equals(short x, short y)
        {
            bool result;

            if (x == y)
            {
                result = true;
            }
            else if (Math.Abs(x - y) <= _delta)
            {
                result = true;
            }
            else
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Override GetHashCode();.
        /// </summary>
        /// <param name="obj">Short.</param>
        /// <returns>Hash code.</returns>
        public int GetHashCode(short obj)
        {
            return obj.GetHashCode();
        }
    }

    /// <summary>
    /// Define a rude comparer for float.
    /// </summary>
    public class SingleRudeCompare : IEqualityComparer<float>
    {
        /// <summary>
        /// Delta for comparison.
        /// </summary>
        private float _delta;

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleRudeCompare"/> class.
        /// </summary>
        /// <param name="delta">Delta for comparison.</param>
        public SingleRudeCompare(float delta)
        {
            _delta = delta;
        }

        /// <summary>
        /// Override Equals().
        /// </summary>
        /// <param name="x">The left-hand-side.</param>
        /// <param name="y">The right-hand-side.</param>
        /// <returns>True if equal, false if not.</returns>
        public bool Equals(float x, float y)
        {
            bool result;

            if (x == y)
            {
                result = true;
            }
            else if (2 * Math.Abs((x - y) / (x + y)) < _delta)
            {
                result = true;
            }
            else
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Override GetHashCode();.
        /// </summary>
        /// <param name="obj">Float.</param>
        /// <returns>Hash code.</returns>
        public int GetHashCode(float obj)
        {
            return obj.GetHashCode();
        }
    }

    /// <summary>
    /// Define a rude comparer for double.
    /// </summary>
    public class DoubleRudeCompare : IEqualityComparer<double>
    {
        /// <summary>
        /// Delta for comparison.
        /// </summary>
        private double _delta;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleRudeCompare"/> class.
        /// </summary>
        /// <param name="delta">Delta for comparison.</param>
        public DoubleRudeCompare(double delta)
        {
            _delta = delta;
        }

        /// <summary>
        /// Override Equals().
        /// </summary>
        /// <param name="x">The left-hand-side.</param>
        /// <param name="y">The right-hand-side.</param>
        /// <returns>True if equal, false if not.</returns>
        public bool Equals(double x, double y)
        {
            bool result;

            if (x == y)
            {
                result = true;
            }
            else if (2 * Math.Abs((x - y) / (x + y)) < _delta)
            {
                result = true;
            }
            else
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Override GetHashCode();.
        /// </summary>
        /// <param name="obj">Double.</param>
        /// <returns>Hash code.</returns>
        public int GetHashCode(double obj)
        {
            return obj.GetHashCode();
        }
    }
}