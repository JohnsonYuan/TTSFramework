//----------------------------------------------------------------------------
// <copyright file="HmmNameEncoding.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements HMM name encoding
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Dynamic order of feature.
    /// </summary>
    public enum DynamicOrder
    {
        /// <summary>
        /// Undefined order.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Static feature.
        /// </summary>
        Static = 1,

        /// <summary>
        /// Delta feature.
        /// </summary>
        Delta = 2,

        /// <summary>
        /// Delta of delta.
        /// </summary>
        Acceleration = 3
    }

    /// <summary>
    /// HMM name encoding.
    /// </summary>
    public static class HmmNameEncoding
    {
        private static readonly Dictionary<string, HmmModelType> AcousticFeatureNames = new Dictionary<string, HmmModelType>()
            {
                { "lsp", HmmModelType.Lsp },
                { "logF0", HmmModelType.FundamentalFrequency },
                { "dur", HmmModelType.StateDuration },
                { "pdur", HmmModelType.PhoneDuration },
                { "mbe", HmmModelType.Mbe },
                { "pow", HmmModelType.Power },
                { "guidanceLsp", HmmModelType.GuidanceLsp },
            };

        #region Public operations

        /// <summary>
        /// Gets acoustic feature name string from HMMM model type.
        /// </summary>
        /// <param name="modelType">HMM model type.</param>
        /// <returns>Acoustic feature string.</returns>
        public static string GetAcousticFeatureName(HmmModelType modelType)
        {
            if (AcousticFeatureNames.ContainsValue(modelType))
            {
                return AcousticFeatureNames.Single(n => n.Value == modelType).Key;
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets HMM model type from acoustic feature name.
        /// </summary>
        /// <param name="featureName">Acoustic feature string.</param>
        /// <returns>HMM model type.</returns>
        public static HmmModelType GetModelType(string featureName)
        {
            Helper.ThrowIfNull(featureName);
            if (AcousticFeatureNames.ContainsKey(featureName))
            {
                return AcousticFeatureNames[featureName];
            }
            else
            {
                return HmmModelType.Invalid;
            }
        }

        /// <summary>
        /// Gets label string from HMMM model type.
        /// </summary>
        /// <param name="modelType">HMM model type.</param>
        /// <returns>Label string.</returns>
        public static string GetModelLabel(HmmModelType modelType)
        {
            string modelTag = string.Empty;
            switch (modelType)
            {
                case HmmModelType.Lsp:
                    modelTag = "LSP";
                    break;
                case HmmModelType.FundamentalFrequency:
                    modelTag = "LogF0";
                    break;
                case HmmModelType.StateDuration:
                    modelTag = "StateDuration";
                    break;
                case HmmModelType.PhoneDuration:
                    modelTag = "PhoneDuration";
                    break;
                case HmmModelType.Power:
                    modelTag = "Power";
                    break;
                case HmmModelType.Mbe:
                    modelTag = "MultiBandExcitation";
                    break;
                case HmmModelType.GuidanceLsp:
                    modelTag = "GuidanceLsp";
                    break;
            }

            return modelTag;
        }

        #endregion
    }
}