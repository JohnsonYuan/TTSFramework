//----------------------------------------------------------------------------
// <copyright file="VoiceFontError.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements voice font error class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using Microsoft.Tts.Offline.Common;

    /// <summary>
    /// Voice font error definition.
    /// </summary>
    public enum VoiceFontError
    {
        /// <summary>
        /// Other errors
        /// Parameters: 
        /// {0}: error description.
        /// </summary>
        [ErrorAttribute(Message = "Error(s): \"{0}\"")]
        OtherErrors,
    }
}