//----------------------------------------------------------------------------
// <copyright file="StemmerFile.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Miscellaneous file operation class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Compile common phrase error.
    /// </summary>
    public enum StemmerFileError
    {
        /// <summary>
        /// Duplicate item id.
        /// Parameters:
        /// {0}: One column line.
        /// {1}: Stemmer file path.
        /// </summary>
        [ErrorAttribute(Message = "Only one column detected for line [{0}] in file [{1}].",
            Severity = ErrorSeverity.Warning)]
        OneColumnLine,
    }

    /// <summary>
    /// Stemmer file operation class, stemmer file contains information of different forms of words.
    /// This file is plain text file with unicode encoding.
    /// Each line contains different forms of the word in the first column.
    /// The column separated by "tab" char ('\t').
    /// For example:
    ///     reach'\t'reaches'\t'reaching'\t'reached
    ///     react'\t'reacted'\t'reacting'\t'reacts.
    /// </summary>
    public static class StemmerFile
    {
        /// <summary>
        /// Load stemmer file.
        /// </summary>
        /// <param name="stemmerFilePath">Stemmer file path.</param>
        /// <param name="errorSet">Error set.</param>
        /// <returns>Loaded stemmer items.</returns>
        public static Dictionary<string, string> Read(string stemmerFilePath,
            ErrorSet errorSet)
        {
            Dictionary<string, string> stemmer = new Dictionary<string, string>();
            foreach (string line in Helper.FileLines(stemmerFilePath))
            {
                string[] items = line.Split(Delimitor.TabChars, StringSplitOptions.RemoveEmptyEntries);
                if (items.Length < 2)
                {
                    errorSet.Add(StemmerFileError.OneColumnLine, line, stemmerFilePath);
                    continue;
                }

                for (int i = 1; i < items.Length; i++)
                {
                    if (stemmer.ContainsKey(items[i]))
                    {
                        // Skips this one if there already has it.
                        continue;
                    }

                    stemmer.Add(items[i], items[0]);
                }
            }

            return stemmer;
        }
    }
}