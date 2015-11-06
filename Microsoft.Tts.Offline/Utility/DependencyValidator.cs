//----------------------------------------------------------------------------
// <copyright file="DependencyValidator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This is a executable file dependency validation static class.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Dependency validation class.
    /// </summary>
    public static class DependencyValidator
    {
        /// <summary>
        /// Validate tool dependencies.
        /// </summary>
        /// <param name="toolPath">Tool path to be checked.</param>
        /// <param name="dependenciesRelativPath">Tool dependencies list.</param>
        public static void Check(string toolPath, string[] dependenciesRelativPath)
        {
            Helper.ThrowIfFileNotExist(toolPath);
            Helper.ThrowIfNull(dependenciesRelativPath);

            string toolDir = Path.GetDirectoryName(toolPath);
            foreach (string dependencyRelativPath in dependenciesRelativPath)
            {
                string dependencyPath = Path.Combine(toolDir, dependencyRelativPath);
                Helper.ThrowIfFileNotExist(dependencyPath);
            }
        }
    }
}