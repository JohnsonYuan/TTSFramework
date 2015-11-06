//----------------------------------------------------------------------------
// <copyright file="IProgressReporter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements progress reporter.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.UI
{
    using System;
    using System.Globalization;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Report progress interface.
    /// </summary>
    public interface IProgressReporter
    {
        /// <summary>
        /// On progress changed.
        /// </summary>
        /// <param name="text">Progress message.</param>
        /// <param name="progressPercentage">Progress percentage, range from 0 to 100.</param>
        void OnProgressChanged(string text, int progressPercentage);
    }

    /// <summary>
    /// Console progress reporter.
    /// </summary>
    public class ConsoleProgressReporter : IProgressReporter
    {
        /// <summary>
        /// On progress changed.
        /// </summary>
        /// <param name="text">Progress message.</param>
        /// <param name="progressPercentage">Progress percentage, range from 0 to 100.</param>
        public void OnProgressChanged(string text, int progressPercentage)
        {
            Console.WriteLine(Helper.NeutralFormat("{0}% processed :{1}",
                progressPercentage.ToString(CultureInfo.InvariantCulture), text));
        }
    }
}