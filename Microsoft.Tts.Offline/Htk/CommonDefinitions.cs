//----------------------------------------------------------------------------
// <copyright file="CommonDefinitions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines some common classes/enums for HTK
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>
    /// HMM model types.
    /// </summary>
    public enum HmmModelType
    {
        /// <summary>
        /// Invalid model type.
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// LSP model.
        /// </summary>
        Lsp = 1,

        /// <summary>
        /// F0 model.
        /// </summary>
        FundamentalFrequency = 2,

        /// <summary>
        /// State-based duration model.
        /// </summary>
        StateDuration = 3,

        /// <summary>
        /// Unvoiced/voiced model.
        /// </summary>
        VoicedUnvoiced = 4,

        /// <summary>
        /// Gain model.
        /// </summary>
        Gain = 5,

        /// <summary>
        /// Phone-based duration model.
        /// </summary>
        PhoneDuration = 6,

        /// <summary>
        /// Multi-Band Excitation model.
        /// </summary>
        Mbe = 7,

        /// <summary>
        /// Power model.
        /// </summary>
        Power = 8,

        /// <summary>
        /// CodecLSP model.
        /// </summary>
        GuidanceLsp = 9,

        /// <summary>
        /// Pitch marker mdoel.
        /// </summary>
        PitchMarker = 10,
    }

    /// <summary>
    /// Modeling configuration of statistics parameter speech synthesis.
    /// </summary>
    public static class SpsModeling
    {
        /// <summary>
        /// Default state count for SPS model.
        /// </summary>
        public const int DefaultStateCount = 5;
    }
}