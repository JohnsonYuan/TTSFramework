//----------------------------------------------------------------------------
// <copyright file="IVocoder.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements IVocoder interface
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Research
{
    using System;

    /// <summary>
    /// Interface for vocoder.
    /// </summary>
    public interface IVocoder
    {
        /// <summary>
        /// Using f0, gain and lsp vector to generate speech data.
        /// </summary>
        /// <param name="f0Vector">F0 vector, value in linear Hz domain.</param>
        /// <param name="lspVector">LSP vector, vlaue in interval [0, 0.5).</param>
        /// <param name="gainVector">Gain vector.</param>
        /// <param name="samplingRate">Sampling rate.</param>
        /// <param name="framePeriod">Frame period, in seconds.</param>
        /// <returns>Resultant wave data, in short.</returns>
        short[] LspExcite(float[] f0Vector, float[,] lspVector, float[] gainVector,
            int samplingRate, float framePeriod);
    }
}