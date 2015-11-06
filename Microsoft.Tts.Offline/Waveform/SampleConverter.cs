//----------------------------------------------------------------------------
// <copyright file="SampleConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Class to do conversion of sample between standards
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Waveform
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Waveform sample value converter.
    /// </summary>
    public static class SampleConverter
    {
        #region Private fields

        /// <summary>
        /// Lookups table for fast linear-to-ulaw mapping.
        /// </summary>
        private static int[] _ulawLookUpTable = new int[]
        {
            0, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
            5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
        };

        #endregion

        #region Public static methods

        /// <summary>
        /// Converts the value of sample from signed 16 bit linear sample to ulaw sample.
        /// </summary>
        /// <param name="sample">The source sample value in linear format.</param>
        /// <returns>The converted ulaw sample value.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1009:ClosingParenthesisMustBeSpacedCorrectly", Justification = "Ignored.")]
        public static byte LinearToUlaw(short sample)
        {
            // define the add-in bias for 16 bit samples
            const short Bias = 0x84;
            const short Clip = 32635;

            // get the sample into sign-magnitude.
            // set aside the sign
            int sign = (sample >> 8) & 0x80;

            if (sign != 0)
            {
                // get magnitude
                sample = (short)-sample;
            }

            if (sample > Clip)
            {
                // clip the magnitude 
                sample = Clip;
            }

            // convert from 16 bit linear to ulaw
            sample = (short)(sample + Bias);
            int exponent = _ulawLookUpTable[(sample >> 7) & 0xFF];
            int mantissa = (sample >> (exponent + 3)) & 0x0F;
            byte ulawByte = (byte) ~(sign | (exponent << 4) | mantissa);

#if ZEROTRAP
            if ( ulawbyte == 0 )
            {
                // optional CCITT trap
                ulawbyte = 0x02;
            }
#endif

            return ulawByte;
        }

        #endregion
    }
}