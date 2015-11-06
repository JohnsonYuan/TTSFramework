//----------------------------------------------------------------------------
// <copyright file="TestDataLocator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements TestDataLocator
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Test
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Test data path locator.
    /// </summary>
    public static class TestDataLocator
    {
        private static readonly Collection<Language> SupportedLanguages = new Collection<Language>
            {
                Language.EnUS,
                Language.FrFR,
                Language.ZhCN,
                Language.ZhTW,
                Language.ZhHK
            };

        /// <summary>
        /// Get phone set file path for the language.
        /// </summary>
        /// <param name="language">Language of the phoneset.xml file.</param>
        /// <returns>Phone set file path.</returns>
        public static string GetPhoneSetFilePath(Language language)
        {
            CheckLanguageSupported(language);
            return Path.Combine(Helper.FindTestDataPath(),
                Helper.NeutralFormat(@"{0}\LangData\phoneset.xml", Localor.LanguageToString(language)));
        }

        /// <summary>
        /// Get phone set file path for the language.
        /// </summary>
        /// <param name="language">Language of the schema.xml file.</param>
        /// <returns>Phone set file path.</returns>
        public static string GetSchemaFilePath(Language language)
        {
            CheckLanguageSupported(language);
            return Path.Combine(Helper.FindTestDataPath(),
                Helper.NeutralFormat(@"{0}\LangData\schema.xml", Localor.LanguageToString(language)));
        }

        /// <summary>
        /// Get phone based unit table file path for the language.
        /// </summary>
        /// <param name="language">Language of the phone based unit table file.</param>
        /// <returns>Phone based unit table file path.</returns>
        public static string GetPhoneBasedUnitTableFilePath(Language language)
        {
            CheckLanguageSupported(language);
            return Path.Combine(Helper.FindTestDataPath(),
                Helper.NeutralFormat(@"{0}\LangData\PhoneBasedUnitTable.xml", Localor.LanguageToString(language)));
        }

        /// <summary>
        /// Get Tts2SapiVisemeId file path for the language.
        /// </summary>
        /// <param name="language">Language of the Tts2SapiVisemeId file.</param>
        /// <returns>Tts2SapiVisemeId file path.</returns>
        public static string GetTts2SapiVisemeIdFilePath(Language language)
        {
            CheckLanguageSupported(language);
            return Path.Combine(Helper.FindTestDataPath(),
                Helper.NeutralFormat(@"{0}\LangData\Tts2SapiVisemeId.xml", Localor.LanguageToString(language)));
        }

        /// <summary>
        /// Check whether the language is support for getting language data file.
        /// </summary>
        /// <param name="language">Language to be checked..</param>
        private static void CheckLanguageSupported(Language language)
        {
            if (!SupportedLanguages.Contains(language))
            {
                throw new NotSupportedException(Helper.NeutralFormat(
                    "Doesn't support get phone set path for language [{0}].",
                    language));
            }
        }
    }
}