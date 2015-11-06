//----------------------------------------------------------------------------
// <copyright file="Pair.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Pair class.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// An extension class for Pair.
    /// </summary>
    public static class PairExtension
    {
        /// <summary>
        /// Gets a string to indicate a Pair IEnumerable object.
        /// </summary>
        /// <param name="pairs">The given Pair IEnumerable object.</param>
        /// <param name="getLeftString">The given function to get the string for left.</param>
        /// <param name="getRightString">The given function to get the string for right.</param>
        /// <typeparam name="TLeft">The left type.</typeparam>
        /// <typeparam name="TRight">The right type.</typeparam>
        /// <returns>The string to indicate a Pair IEnumerable object.</returns>
        public static string ToString<TLeft, TRight>(this IEnumerable<Pair<TLeft, TRight>> pairs, Func<TLeft, string> getLeftString, Func<TRight, string> getRightString)
        {
            StringBuilder builder = new StringBuilder();

            int count = 0;
            foreach (Pair<TLeft, TRight> pair in pairs)
            {
                if (count > 3)
                {
                    builder.Append(", ...");
                    break;
                }

                if (count > 0)
                {
                    builder.Append(", ");
                }

                builder.AppendFormat(CultureInfo.InvariantCulture, "<{0}, {1}>", getLeftString(pair.Left), getRightString(pair.Right));
                ++count;
            }

            return builder.ToString();
        }
    }

    /// <summary>
    /// This class defines a template for pair.
    /// </summary>
    /// <typeparam name="TLeft">Type of left element of the pair.</typeparam>
    /// <typeparam name="TRight">Type of right element of the pair.</typeparam>
    public class Pair<TLeft, TRight>
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Pair{TLeft, TRight}"/> class.
        /// </summary>
        /// <param name="left">Left element of the pair.</param>
        /// <param name="right">Right element of the pair.</param>
        public Pair(TLeft left, TRight right)
        {
            Left = left;
            Right = right;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the left element of the pair.
        /// </summary>
        public TLeft Left { get; set; }

        /// <summary>
        /// Gets or sets the right element of the pair.
        /// </summary>
        public TRight Right { get; set; }

        #endregion
    }
}