//----------------------------------------------------------------------------
// <copyright file="SsftData.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements class for SSFT data
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// SSFT data file manager.
    /// </summary>
    public static class SsftData
    {
        #region Public static operations

        /// <summary>
        /// Read lexicon data from file.
        /// </summary>
        /// <param name="language">Language of the lexicon to read.</param>
        /// <param name="filePath">Lexicon data file path.</param>
        /// <returns>Lexicon instance.</returns>
        public static Lexicon ReadLexicon(Language language, string filePath)
        {
            switch (language)
            {
                case Language.EnUS:
                    return ReadAllEnUSLexicon(filePath);
                default:
                    System.Diagnostics.Debug.Assert(false);
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Only {0} language is supported for lexicon loading. But {1} language is found to load for.",
                        Language.EnUS, language);
                    throw new NotSupportedException(message);
            }
        }

        #endregion

        #region Private static operations

        /// <summary>
        /// Load SSFT format english lexicon from text file.
        /// <example>
        /// Word abstract
        /// Pronunciation0 ae 1 b - s t r ae k t
        /// POS0 adj
        /// POS1 noun
        /// Pronunciation1 ax b - s t r ae 1 k t
        /// POS0 verb.
        /// </example>
        /// </summary>
        /// <param name="filePath">Text file to load.</param>
        /// <returns>Lexicon.</returns>
        private static Lexicon ReadAllEnUSLexicon(string filePath)
        {
            Lexicon lexicon = new Lexicon(Language.EnUS);
            string line = null;
            using (TextReader tr = new StreamReader(filePath))
            {
                LexicalItem item = null;
                LexiconPronunciation pronun = null;

                while ((line = tr.ReadLine()) != null)
                {
                    if (line.StartsWith("Word", StringComparison.Ordinal))
                    {
                        Match m = Regex.Match(line, @"Word\s+(.*)\s*");
                        System.Diagnostics.Debug.Assert(m.Success);

                        item = new LexicalItem(Language.EnUS);
                        item.Grapheme = m.Groups[1].Value;

                        if (!lexicon.Items.ContainsKey(item.Grapheme))
                        {
                            lexicon.Items.Add(item.Grapheme, item);
                        }
                        else
                        {
                            System.Diagnostics.Trace.WriteLine(item.ToString());
                        }
                    }
                    else if (line.StartsWith("Pronunciation", StringComparison.Ordinal))
                    {
                        Match pronunMatch = Regex.Match(line, @"Pronunciation\d\s+(.*)\s*");
                        System.Diagnostics.Debug.Assert(pronunMatch.Success);

                        pronun = new LexiconPronunciation(Language.EnUS);
                        pronun.Symbolic = pronunMatch.Groups[1].Value;

                        item.Pronunciations.Add(pronun);
                    }
                    else if (line.StartsWith("POS", StringComparison.Ordinal))
                    {
                        Match posMatch = Regex.Match(line, @"POS\d\s+(.*)\s*");
                        System.Diagnostics.Debug.Assert(posMatch.Success);

                        PosItem pi = new PosItem(posMatch.Groups[1].Value);
                        pi.Pos = Localor.MapPos(pi.Value);
                        pronun.Properties.Add(new LexiconItemProperty(pi));
                    }
                    else if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false);
                        string message = string.Format(CultureInfo.InvariantCulture,
                        "Only Word, Pronunciation or POS is supported for information of lexicon entry. But {0} is found to load from file [{1}].",
                        line, filePath);
                        throw new NotSupportedException(message);
                    }
                }
            }

            return lexicon;
        }

        #endregion
    }
}