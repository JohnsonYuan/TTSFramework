//----------------------------------------------------------------------------
// <copyright file="ScriptError.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script error class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Globalization;
    using System.Text;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Script error definition.
    /// </summary>
    public enum ScriptError
    {
        /// <summary>
        /// Duplicate item id.
        /// Parameters: 
        /// {0}: item id.
        /// </summary>
        [ErrorAttribute(Message = "Duplicated item id [{0}] found.")]
        DuplicateItemId,

        /// <summary>
        /// Pronunciation error
        /// Phones should be separated by whitespace and in phone set.
        /// Parameters: 
        /// {0}: pronunciation error
        /// {1}: item id
        /// {2}: word.
        /// </summary>
        [ErrorAttribute(Message = "Incorrect pronunciation format - {0} of word [{2}] in item id [{1}]",
            Manner = MessageCombine.Nested)]
        PronunciationError,

        /// <summary>
        /// Unrecognized POS.
        /// Parameters: 
        /// {0}: item id
        /// {1}: word
        /// {2}: pronunciation
        /// {3}: pos.
        /// </summary>
        [ErrorAttribute(Message = "Unrecognized POS \"{3}\" for the word [{1}] with pronunciation /{2}/ in item [{0}].")]
        UnrecognizedPos,

        /// <summary>
        /// Sentence separating error.
        /// Parameters: 
        /// {0}: item id
        /// {1}: file path
        /// {2}: sentence text.
        /// </summary>
        [ErrorAttribute(Message = "Wrong sentence separating for \"{2}\" in item [{0}] of file [{1}].")]
        SentenceSeparatingError,

        /// <summary>
        /// Word breaking error. 
        /// Parameters: 
        /// {0}: item id
        /// {1}: file path
        /// {2}: sentence text.
        /// </summary>
        [ErrorAttribute(Message = "Wrong word breaking in sentence \"{2}\" in item [{0}] of file [{1}].")]
        WordBreakingError,

        /// <summary>
        /// UV seg interval error.
        /// Parameters:
        /// {0}: item id
        /// {1}: error path
        /// {2}: interval begin
        /// {3}: interval end.
        /// </summary>
        [ErrorAttribute(Message = "Invalid UV seg interval [{2}, {3}) in the node path \"{1}\" in item [{0}].")]
        UvSegIntervalError,

        /// <summary>
        /// UV seg order error.
        /// Parameters:
        /// {0}: item id
        /// {1}: error path.
        /// </summary>
        [ErrorAttribute(Message = "Invalid order of UV seg interval in the node path \"{1}\" in item [{0}].")]
        UvSegOrderError,

        /// <summary>
        /// UV seg overlapping error.
        /// Parameters:
        /// {0}: item id
        /// {1}: error path.
        /// </summary>
        [ErrorAttribute(Message = "UV seg are overlapped with each other in the node path \"{1}\" in item [{0}].")]
        UvSegOverlappingError,

        /// <summary>
        /// Sanity of f0.
        /// Parameters:
        /// {0}: item id
        /// {1}: error path
        /// {2}: invalid f0 value.
        /// </summary>
        [ErrorAttribute(Message = "Invalid f0 value {2} in the node path \"{1}\" in item [{0}].")]
        F0Error,

        /// <summary>
        /// Inconsistency error between f0 values and UV type.
        /// Parameters:
        /// {0}: item id
        /// {1}: error path.
        /// </summary>
        [ErrorAttribute(Message = "Inconsistency between f0 values and UV type in the node path \"{1}\" in item [{0}].")]
        F0AndUvTypeError,

        /// <summary>
        /// Inconsistency error between duration value and UV segement interval.
        /// Parameters:
        /// {0}: item id
        /// {1}: error path.
        /// </summary>
        [ErrorAttribute(Message = "Inconsistency between duration value and UV segement interval in the node path \"{1}\" in item [{0}].")]
        DurationAndIntervalError,

        /// <summary>
        /// Element segment interval error.
        /// Parameters:
        /// {0}: item id
        /// {1}: error path
        /// {2}: interval begin
        /// {3}: interval end.
        /// </summary>
        [ErrorAttribute(Message = "Invalid element segment interval [{2}, {3}) in the node path \"{1}\" in item [{0}].")]
        SegmentIntervalError,

        /// <summary>
        /// Element segment interval error.
        /// Parameters:
        /// {0}: item id
        /// {1}: error path
        /// {2}: interval begin
        /// {3}: previous interval end.
        /// </summary>
        [ErrorAttribute(Message = "Invalid element segment begin {2} cf. previous segment end {3} in the node path \"{1}\" in item [{0}].")]
        SegmentSequenceError,

        /// <summary>
        /// Inconsistency error between segment duration value and UV segement interval.
        /// Parameters:
        /// {0}: item id
        /// {1}: error path.
        /// </summary>
        [ErrorAttribute(Message = "Inconsistency between segment duration value and UV segement interval in the node path \"{1}\" in item [{0}].")]
        SegmentDurationAndIntervalError,

        /// <summary>
        /// Inconsistency error between duration and segment.
        /// Parameters:
        /// {0}: item id
        /// {1}: error path
        /// {2}: duration
        /// {3}: segment begin
        /// {4}: segment end.
        /// </summary>
        [ErrorAttribute(Message = "Inconsistency between duration {2} and segment [{3}, {4}) in the node path \"{1}\" in item [{0}].")]
        DurationAndSegmentError,

        /// <summary>
        /// When load scripts in folder using ScriptFileCollection class, language doesn't match with the expected one.
        /// Parameters:
        /// {0}: invalid script language
        /// {1}: expected language
        /// {2}: script file name.
        /// </summary>
        [ErrorAttribute(Message = "Invalid script language [{0}] with expected language [{1}] in file [{2}]")]
        InvalidLanguage,

        /// <summary>
        /// Wrap the script errors in file when load scripts using ScriptFileCollection class
        /// Parameters:
        /// {0}: file name of the script contains error.
        /// {1}: errors of the script.
        /// </summary>
        [ErrorAttribute(Message = "Error in file [{0}] : [{1}]")]
        ScriptCollectionError,

        /// <summary>
        /// Other errors
        /// Parameters:
        /// {0}: item id
        /// {1}: normal word text.
        /// </summary>
        [ErrorAttribute(Message = "No pronunciation normal word '{1}' in script item {0}.",
            Severity = ErrorSeverity.Warning)]
        EmptyPronInNormalWord,

        /// <summary>
        /// Other errors
        /// Parameters:
        /// {0}: item id
        /// {1}: error description.
        /// </summary>
        [ErrorAttribute(Message = "Error(s) in item [{0}]: \"{1}\"")]
        OtherErrors,

        /// <summary>
        /// Invalid xml characters error
        /// Parameters:
        /// {0}: invalid line number
        /// {1}: invalid text.
        /// </summary>
        [ErrorAttribute(Message = "Invalid xml characters, line {0}: {1}")]
        InvalidXmlCharactersError
    }
}