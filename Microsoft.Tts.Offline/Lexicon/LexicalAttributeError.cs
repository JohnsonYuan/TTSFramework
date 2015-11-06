//----------------------------------------------------------------------------
// <copyright file="LexicalAttributeError.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements LexicalAttributeError class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Core
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Tts.Offline.Common;

    /// <summary>
    /// Word entry error.
    /// </summary>
    public enum LexicalAttributeError
    {
        /// <summary>
        /// Empty category.
        /// </summary>
        [ErrorAttribute(Message = "empty category is not allowed")]
        EmptyCategory,

        /// <summary>
        /// Empty value.
        /// </summary>
        [ErrorAttribute(Message = "empty value is not allowed")]
        EmptyValue,

        /// <summary>
        /// Invalid category
        /// Parameters:
        /// {0}: category.
        /// </summary>
        [ErrorAttribute(Message = "invalid category {0}")]
        InvalidCategory,

        /// <summary>
        /// Invalid category
        /// Parameters:
        /// {0}: value
        /// {1}: category.
        /// </summary>
        [ErrorAttribute(Message = "invalid value {0} under the category {1}")]
        InvalidValue,

        /// <summary>
        /// Invalid Definition For Pos
        /// Parameters:
        /// {0}: pos value.
        /// </summary>
        [ErrorAttribute(Message = "pos \"{0}\" should be defined inside \"pos\" node, but not attribute node")]
        InvalidDefinitionForPos,
    }
}