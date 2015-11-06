//----------------------------------------------------------------------------
// <copyright file="DeDEScriptItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements German script item
//      It provides the function to parse the script lines of a sentence
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// German script item.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
        "CA1706:ShortAcronymsShouldBeUppercase", Justification = "Ignore.")]
    public class DeDEScriptItem : ScriptItem
    {
        #region Override properties

        /// <summary>
        /// Gets Language.
        /// </summary>
        public override Language Language
        {
            get { return Language.DeDE; }
        }

        /// <summary>
        /// Gets Separators for pronunciation string.
        /// </summary>
        public override PronunciationSeparator PronunciationSeparator
        {
            get { return new PronunciationSeparator("/", "-", ".", " "); }
        }

        /// <summary>
        /// Gets Punctuation pattern for this language.
        /// </summary>
        public override string PunctuationPattern
        {
            get
            {
                return @"(\""|,|\.\.\.|\.\""$|\.$|!|\?|_|;|:|\(|\)|\[|\]|\{|\}|\xA1|\xBF| -| ?- |^-$)";
            }
        }
        #endregion

        #region Operations

        #endregion

        #region Override methods

        /// <summary>
        /// Tokenize orthography sentence string
        /// Thr tokenized items are seperated by white-space.
        /// </summary>
        /// <param name="sentence">Sentence to tokenize.</param>
        /// <returns>Tokenized sentence string.</returns>
        protected override string TokenizeOrthographyString(string sentence)
        {
            if (Regex.Match(sentence, @"\S+/\S+").Success)
            {
                // well format
                return sentence;
            }

            Match endingAbbr = Regex.Match(sentence, @" (\S\.)(\S\.)+$");
            if (endingAbbr.Success)
            {
                // append ending "."
                sentence += " .";
            }

            sentence = Regex.Replace(sentence, Localor.AnnotationSymbols, " $1 ");
            sentence = Regex.Replace(sentence, PunctuationPattern, " $1 ");
            sentence = Regex.Replace(sentence, @"((?:\-\-))", " $1 ");
            sentence = Regex.Replace(sentence, @"\.\""", @". """);

            // # clean out extra spaces
            sentence = Regex.Replace(sentence, @"  *", @" ");
            sentence = Regex.Replace(sentence, @"^ *", string.Empty);

            return sentence;
        }

        #endregion
    }
}