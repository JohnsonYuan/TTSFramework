//----------------------------------------------------------------------------
// <copyright file="Pronunciation.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     This module implements prununciation extension
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.ServiceProvider.Extension
{
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// ServiceProvider prununciation operation class.
    /// </summary>
    public static class TtsPronSourceExtension
    {
        private static Dictionary<TtsPronSource, string> _ttsProns;

        /// <summary>
        /// Initializes static members of the <see cref="TtsPronSourceExtension"/> class.
        /// </summary>
        static TtsPronSourceExtension()
        {
            _ttsProns = new Dictionary<TtsPronSource, string>()
            {
                { TtsPronSource.PS_NONE, "None" },
                { TtsPronSource.PS_MAIN_LEXICON, "Main lexicon" },
                { TtsPronSource.PS_MORPHOLOGY, "Morphology lexicon" },
                { TtsPronSource.PS_EXTRALANGUAGE, "Extra language" },
                { TtsPronSource.PS_COMPOUND, "Compound rule" },
                { TtsPronSource.PS_SPELLING, "Spell out" },
                { TtsPronSource.PS_LTS, "LTS" },
                { TtsPronSource.PS_PRONCHANGE, "Boundary change" },
                { TtsPronSource.PS_OOV_LOCHANDLER, "OOV locale handler" },
                { TtsPronSource.PS_CUSTOM_LEXICON, "Custom lexicon" },
                { TtsPronSource.PS_POSTPRON_LOCHANDLER, "Postpron locale handler" },
                { TtsPronSource.PS_XMLTAG, "Xml tag" },
                { TtsPronSource.PS_VOICE_LEXICON, "Voice specific lexicon" },
                { TtsPronSource.PS_DOMAIN_LEXICON, "Domain lexicon" },
                { TtsPronSource.PS_MAIN_POLYPHONY, "Main polyphony rule" },
                { TtsPronSource.PS_DOMAIN_POLYPHONY, "Domain polyphony rule" },
                { TtsPronSource.PS_POLYPHONY_RNN_MODEL, "RNN polyphony Model" },
                { TtsPronSource.PS_POLYPHONY_CRF_MODEL, "CRF polyphony Model" },
                { TtsPronSource.PS_OTHER, "Others" },
            };
        }

        /// <summary>
        /// Gets display string of the TtsPronSource enum.
        /// </summary>
        /// <param name="pronSource">TtsPronSource value.</param>
        /// <returns>Display string of the TtsPronSource enum.</returns>
        public static string ToDisplayString(this TtsPronSource pronSource)
        {
            if (!_ttsProns.ContainsKey(pronSource))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Please add the TtsPronSource display string for [{0}] in : Microsoft.Tts.ServiceProvider.Extension.Pronunciation._ttsProns",
                    pronSource.ToString()));
            }

            return _ttsProns[pronSource];
        }
    }
}