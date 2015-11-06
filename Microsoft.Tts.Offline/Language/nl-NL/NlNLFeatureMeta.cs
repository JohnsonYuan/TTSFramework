//----------------------------------------------------------------------------
// <copyright file="NlNLFeatureMeta.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements nl-NL feature Meta.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Nl-NL feature meta data.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
        "CA1706:ShortAcronymsShouldBeUppercase")]
    public class NlNLFeatureMeta : FeatureMeta
    {
        #region Fields

        /// <summary>
        /// Nl-NL feature Meta List.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1814:PreferJaggedArraysOverMultidimensional")]
        private static readonly int[,] _metaDataList = new int[,]
        {
            // {FeatureID,BitWidth}
            { (int)TtsFeature.PosInSentence, DefaultBitWidth },
            { (int)TtsFeature.PosInWord, DefaultBitWidth },
            { (int)TtsFeature.PosInSyllable, DefaultBitWidth },
            { (int)TtsFeature.LeftContextPhone, DefaultBitWidth },
            { (int)TtsFeature.RightContextPhone, DefaultBitWidth },
            { (int)TtsFeature.TtsStress, DefaultBitWidth },
            { (int)TtsFeature.TtsEmphasis, DefaultBitWidth },
            { (int)TtsFeature.TtsNeighborPrev, DefaultBitWidth },
            { (int)TtsFeature.TtsEnergy, sizeof(float) * DefaultBitWidth }
        };

        #endregion

        #region Construction
        /// <summary>
        /// Construction.
        /// </summary>
        public NlNLFeatureMeta()
        {
            SetFeatureMeta(_metaDataList);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Language of this instance.
        /// </summary>
        public override Language Language
        {
            get { return Language.NlNL; }
        }

        #endregion
    }
}