// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeatureMetaExtension.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     This module defines the FeatureMetaExtension class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Tts.ServiceProvider.Extension
{
    using Microsoft.Tts.ServiceProvider;
    using Microsoft.Tts.ServiceProvider.FeatureExtractor;

    /// <summary>
    /// Feature meta extension.
    /// </summary>
    public static class FeatureMetaExtension
    {
        /// <summary>
        /// Gets feature meta top level in the location.
        /// </summary>
        /// <param name="featureMeta">Feature meta.</param>
        /// <returns>Get feature meta top level in the location.</returns>
        public static TtsFeatureLevel GetTopLevel(this FeatureMeta featureMeta)
        {
            TtsFeatureLevel topLevel = TtsFeatureLevel.TTS_FEATURE_LEVEL_DUMMY;
            foreach (FeatureLocation location in featureMeta.Locations)
            {
                if (location.Level > topLevel)
                {
                    topLevel = location.Level;
                }
            }

            return topLevel;
        }

        /// <summary>
        /// Gets feature meta top level step.
        /// </summary>
        /// <param name="featureMeta">Feature meta.</param>
        /// <returns>Get feature meta top level step.</returns>
        public static int GetTopLevelStep(this FeatureMeta featureMeta)
        {
            TtsFeatureLevel topLevel = featureMeta.GetTopLevel();
            int step = 0;
            foreach (FeatureLocation location in featureMeta.Locations)
            {
                if (location.Level == topLevel)
                {
                    step += location.Step;
                }
            }

            return step;
        }
    }
}