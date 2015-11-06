//----------------------------------------------------------------------------
// <copyright file="PosError.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements POS error
// </summary>
//----------------------------------------------------------------------------
namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// POS Error definition.
    /// </summary>
    public enum PosErrorType
    {
        /// <summary>
        /// Duplicate pos name.
        /// </summary>
        [ErrorAttribute(Message = "Duplicate POS name \"{0}\" (Id = \"{1}\")")]
        DuplicatePosName,

        /// <summary>
        /// Duplicate pos id.
        /// </summary>
        [ErrorAttribute(Message = "Duplicate POS id \"{1}\" (name = \"{0}\"")]
        DuplicatePosId,
    }
}