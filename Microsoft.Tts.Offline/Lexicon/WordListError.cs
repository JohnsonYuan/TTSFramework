// -----------------------------------------------------------------------
// <copyright file="WordListError.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Core
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Tts.Offline.Common;

    /// <summary>
    /// Script processpr error definition.
    /// </summary>
    public enum WordListError
    {
        /// <summary>
        /// Invalid format in word list line.
        /// Parameters: 
        /// {0}: line id
        /// {1}: file path.
        /// </summary>
        [ErrorAttribute(Message = "Invalid format in word list line [{0}] of file [{1}].")]
        InvalidFormatInWordListFileError,

        /// <summary>
        /// Duplicate word in word list.
        /// Parameters: 
        /// {0}: line id
        /// {1}: file path.
        /// </summary>
        [ErrorAttribute(Message = "Duplicate word in word list line [{0}] of file [{1}].")]
        DuplicateWordInWordListFileError,
    }
}