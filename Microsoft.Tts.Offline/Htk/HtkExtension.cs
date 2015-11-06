//----------------------------------------------------------------------------
// <copyright file="HtkExtension.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines a common library to manipulate constant and utility
//   funtionality for Htk.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    /// <summary>
    /// Helper class to hold some Htk constant and utility function.
    /// </summary>
    public static class HtkExtension
    {
        #region Methods

        /// <summary>
        /// Convert the windows path to Htk path format.
        /// </summary>
        /// <param name="path">The input windows path.</param>
        /// <returns>The path in Htk path format.</returns>
        public static string ToHtkPath(this string path)
        {
            return path.Replace('\\', '/');
        }

        #endregion
    }
}