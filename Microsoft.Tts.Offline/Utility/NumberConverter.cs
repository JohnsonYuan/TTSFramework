//----------------------------------------------------------------------------
// <copyright file="NumberConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This is a number conversing helper class.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Safe number convertion.
    /// </summary>
    public static class NumberConverter
    {
        /// <summary>
        /// Convert float value to short value.
        /// </summary>
        /// <param name="inputValue">Input float value.</param>
        /// <returns>Output short value.</returns>
        public static short Double2Int16(double inputValue)
        {
            double value = 0;

            if (inputValue >= 0)
            {
                value = inputValue + 0.5f;
                if (value > short.MaxValue)
                {
                    value = short.MaxValue;
                }
            }
            else
            {
                value = inputValue - 0.5f;
                if (value < short.MinValue)
                {
                    value = short.MinValue;
                }
            }

            return checked((short)value);
        }
    }
}