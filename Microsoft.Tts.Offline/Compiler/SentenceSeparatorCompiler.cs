//----------------------------------------------------------------------------
// <copyright file="SentenceSeparatorCompiler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Sentence Separator Compiler
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Sentence separator compiler error.
    /// </summary>
    public enum SentenceSeparatorCompilerError
    {
        /// <summary>
        /// Not found word breaker files
        /// Parameters: 
        /// {0}: path of word breaker data file.
        /// </summary>
        [ErrorAttribute(Message = "Can't find sentence separator file : [{0}].",
            Severity = ErrorSeverity.Warning)]
        NotFindSentenceSeparatorFile
    }

    /// <summary>
    /// Sentence Separator Compiler.
    /// </summary>
    public class SentenceSeparatorCompiler
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="SentenceSeparatorCompiler"/> class from being created.
        /// </summary>
        private SentenceSeparatorCompiler()
        {
        }

        /// <summary>
        /// Compile the binary.
        /// </summary>
        /// <param name="sentSepDataDir">Directory of sentence separator data.</param>
        /// <param name="outputStream">OutputStream.</param>
        /// <param name="addedFileNames">Added sentence separator file names.</param>
        /// <returns>ErrorSet.</returns>
        public static ErrorSet Compile(string sentSepDataDir, Stream outputStream,
            Collection<string> addedFileNames)
        {
            if (string.IsNullOrEmpty(sentSepDataDir))
            {
                throw new ArgumentNullException("sentSepDataDir");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            ErrorSet errorSet = new ErrorSet();
            string[] sentSetDataFiles = new string[] 
            {
                "abbrev.txt",
                "bible.txt",
                "conjunct.txt",
                "endabbr.txt",
                "frstwrds.txt",
                "notend.txt",
                "numbers.txt",
                "numintro.txt",
                "smtm.txt",
                "specialwords.txt",
                "titles.txt",
                "wordend.txt"
            };

            if (!errorSet.Contains(ErrorSeverity.MustFix))
            {
                BinaryWriter bw = new BinaryWriter(outputStream);
                Collection<int> words = new Collection<int>();
                Collection<int> offsets = new Collection<int>();
                using (StringPool sp = new StringPool())
                {
                    // Files are ordered alphabetically
                    foreach (string dependency in sentSetDataFiles)
                    {
                        string path = Path.Combine(sentSepDataDir, dependency);
                        if (File.Exists(path))
                        {
                            addedFileNames.Add(dependency);
                            words.Add(WordFile.LoadWordsIntoStringPool(path, sp, offsets, true, errorSet));
                        }
                        else
                        {
                            errorSet.Add(new Error(SentenceSeparatorCompilerError.NotFindSentenceSeparatorFile,
                                dependency));

                            // zero count of word
                            words.Add(0);
                        }
                    }

                    // Write to file
                    bw.Write(words.Count);
                    int total = 0;
                    foreach (int count in words)
                    {
                        bw.Write(count);
                        total += count;
                    }

                    Debug.Assert(total == offsets.Count);
                    foreach (int offset in offsets)
                    {
                        bw.Write(offset);
                    }

                    byte[] pool = sp.ToArray();
                    bw.Write(pool, 0, pool.Length);
                }           
            }

            return errorSet;
        }
    }
}