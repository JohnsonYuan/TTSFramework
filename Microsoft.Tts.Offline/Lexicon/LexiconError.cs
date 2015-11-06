//----------------------------------------------------------------------------
// <copyright file="LexiconError.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements lexicon error class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Core
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Globalization;
    using System.Text;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Word entry error.
    /// </summary>
    public enum WordEntryError
    {
        /// <summary>
        /// Empty word entry or word only contain space or tab.
        /// </summary>
        [ErrorAttribute(Message = "empty word or word only containing space or tab is not allowed")]
        EmptyWord,

        /// <summary>
        /// Word with leading or trailing space.
        /// </summary>
        [ErrorAttribute(Message = "leading or trailing spaces in a word is not allowed")]
        LeadingOrTrailingSpace,

        /// <summary>
        /// Word containing Tab or multiple spaces.
        /// </summary>
        [ErrorAttribute(Message = "word containing Tab or numtiple space is not allowed")]
        ContainingTabOrMultipleSpaces
    }

    /// <summary>
    /// Lexicon error definition.
    /// </summary>
    public enum LexiconError
    {
        /// <summary>
        /// Duplicate word entry.
        /// </summary>
        [ErrorAttribute(Message = "Duplicated words with the same grapheme [{0}] found.", 
            Severity = ErrorSeverity.MustFix)]
        DuplicateWordEntry,

        /// <summary>
        /// Pronunciation error
        /// Phones should be separated by whitespace and in phone set.
        /// </summary>
        [ErrorAttribute(Message = "Incorrect pronunciation format - {0} of word [{1}]", 
            Severity = ErrorSeverity.Warning, Manner = MessageCombine.Nested)]
        PronunciationError, 

        /// <summary>
        /// Duplicate pronunciation node.
        /// </summary>
        [ErrorAttribute(Message = "Duplicate pronunciation /{1}/ in word [{0}]",
            Severity = ErrorSeverity.MustFix)]
        DuplicatePronunciationNode,

        /// <summary>
        /// Pronunciation is a new one for unified lexicon when import from domain lexicon.
        /// </summary>
        [ErrorAttribute(Message = "New domain pronunciation /{2}/ of word [{1}] in domain [{0}] is not imported.",
            Severity = ErrorSeverity.Warning)]
        NewDomainPronunciation,

        /// <summary>
        /// Unrecognized POS.
        /// </summary>
        [ErrorAttribute(Message = "Unrecognized POS \"{2}\" for the word [{0}] with pronunciation /{1}/.",
            Severity = ErrorSeverity.Warning)]
        UnrecognizedPos,

        /// <summary>
        /// Duplicate POS.
        /// </summary>
        [ErrorAttribute(Message = "Duplicate property for the word [{0}] with pronunciation /{1}/.", 
            Severity = ErrorSeverity.MustFix)]
        DuplicateProperty,

        /// <summary>
        /// Invalid word entry: nested error with WordEntryError.
        /// </summary>
        [ErrorAttribute(Message = "Invalid word entry with grapheme [{0}]",
            Manner = MessageCombine.Concatenate, ConcateString = ":",
            Severity = ErrorSeverity.MustFix)]
        InvalidWordEntry,

        /// <summary>
        /// Case error.
        /// </summary>
        [ErrorAttribute(Message = "Incorrect case - {0} of word [{1}]",
            Severity = ErrorSeverity.Warning, Manner = MessageCombine.Nested)]
        CaseError,

        /// <summary>
        /// Gender error.
        /// </summary>
        [ErrorAttribute(Message = "Incorrect gender - {0} of word [{1}]",
            Severity = ErrorSeverity.Warning, Manner = MessageCombine.Nested)]
        GenderError,

        /// <summary>
        /// Number error.
        /// </summary>
        [ErrorAttribute(Message = "Incorrect number - {0} of word [{1}]",
            Severity = ErrorSeverity.Warning, Manner = MessageCombine.Nested)]
        NumberError,

        /// <summary>
        /// Domain error
        /// Parameters:
        /// {0}: Nested error
        /// {1}: word
        /// {2}: pronunciation.
        /// </summary>
        [ErrorAttribute(Message = "Incorrect domain - {0} of word [{1}] with pronunciation /{2}/",
            Severity = ErrorSeverity.Warning, Manner = MessageCombine.Nested)]
        DomainError,

        /// <summary>
        /// Attribute error
        /// Parameters:
        /// {0}: Nested error
        /// {1}: word
        /// {2}: pronunciation.
        /// </summary>
        [ErrorAttribute(Message = "Incorrect attribute - {0} of word [{1}] with pronunciation /{2}/",
            Severity = ErrorSeverity.Warning, Manner = MessageCombine.Nested)]
        AttributeError,

        /// <summary>
        /// Invalid Dependent Data.
        /// </summary>
        [ErrorAttribute(Message = "Invalid Dependent data \"{0}\".")]
        InvalidDependentData,

        /// <summary>
        /// Empty lexicon.
        /// </summary>
        [ErrorAttribute(Message = "Empty lexicon ")]
        EmptyLexicon,

        /// <summary>
        /// Mixed property definition.
        /// </summary>
        [ErrorAttribute(Message = "Incorrect property definition for word {0} with [{1}]: There is not allowed to Mixed use property and attribute definition for case, gender or number",
            Severity = ErrorSeverity.Warning)]
        MixedPropertyDefinition,

        /// <summary>
        /// Lack of pronunciation for general domain.
        /// </summary>
        [ErrorAttribute(Message = "Lack of pronunciation for general domain of lexicon word [{0}].",
            Severity = ErrorSeverity.MustFix)]
        LackGeneralDomainPronError,
    }

    /// <summary>
    /// Lexicon Compiler error definition.
    /// </summary>
    public enum LexiconCompilerError
    {
        /// <summary>
        /// Remove invalid word
        /// Parameters:
        /// {0}: word.
        /// </summary>
        [ErrorAttribute(Message = "Remove invalid word [{0}] (in lowercase) as it contains error in it.",
            Severity = ErrorSeverity.MustFix)]
        RemoveInvalidWord,

        /// <summary>
        /// Remove invalid pronunciation
        /// Parameters:
        /// {0}: word
        /// {1}: pronunciation.
        /// </summary>
        [ErrorAttribute(Message = "Remove invalid pronunciation /{1}/ for word [{0}] (in lowercase) as it contains error in it.",
            Severity = ErrorSeverity.MustFix)]
        RemoveInvalidPronunciation,

        /// <summary>
        /// Remove invalid property
        /// Parameters:
        /// {0}: word
        /// {1}: pronunciation
        /// {2}: part of speech.
        /// </summary>
        [ErrorAttribute(Message = "Remove invalid property with POS of \"{2}\" for the pronunciation of /{1}/ in word [{0}] (in lowercase) as it contains error in it.",
            Severity = ErrorSeverity.Warning)]
        RemoveInvalidProperty
    }

    /// <summary>
    /// Lexical item error defenition.
    /// </summary>
    public enum LexicalItemError
    {
        /// <summary>
        /// More than one perfered pronunciation of specific domain.
        /// Parameters:
        /// {0}: pronunciation string
        /// {1}: domain name.
        /// </summary>
        [ErrorAttribute(Message = "There are more than one prefered pronunciation \"{0}\" of domain \"{1}\"",
            Severity = ErrorSeverity.MustFix)]
        MoreThanOnePreferedPronOfSpecificDomain,
    }
}