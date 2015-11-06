//----------------------------------------------------------------------------
// <copyright file="WordFile.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      WordFile
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// WordFile Error definition.
    /// </summary>
    public enum WordFileError
    {
        /// <summary>
        /// ContainWhiteSpace
        /// Parameters: 
        /// {0}: word
        /// {1}: path of word file.
        /// </summary>
        [ErrorAttribute(Message = "Word of \"{0}\" contains invalid white space in file '{1}'.", 
            Severity = ErrorSeverity.MustFix)]
        ContainWhiteSpace,

        /// <summary>
        /// DuplicateWord
        /// Parameters: 
        /// {0}: word
        /// {1}: path of word file.
        /// </summary>
        [ErrorAttribute(Message = "Word of \"{0}\" in file '{1}' is duplicate in List.", 
            Severity = ErrorSeverity.Warning)]
        DuplicateWord,

        /// <summary>
        /// DuplicateWordsInOneFile
        /// Parameters: 
        /// {0}: word
        /// {1}: path of word file.
        /// </summary>
        [ErrorAttribute(Message = "There're duplicate Words of \"{0}\" in file '{1}'.",
            Severity = ErrorSeverity.MustFix)]
        DuplicateWordsInOneFile
    }

    /// <summary>
    /// Word File,which is of the following line format
    /// L"Adj.",.
    /// </summary>
    internal class WordFile
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="WordFile"/> class from being created.
        /// </summary>
        private WordFile()
        {
        }

        /// <summary>
        /// Collect the words in a file, which is of the following line format
        /// L"Adj.",.
        /// </summary>
        /// <param name="file">File containing words.</param>
        /// <param name="words">Word list.</param>
        /// <param name="sort">Whether sort the word list.</param>
        /// <returns>ErrorSet.</returns>
        public static ErrorSet LoadWordsIntoWordList(string file, List<string> words, bool sort)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("file");
            }

            if (words == null)
            {
                throw new ArgumentNullException("words");
            }

            ErrorSet errors = new ErrorSet();
            List<string> wordsInFile = new List<string>();

            if (File.Exists(file))
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line != null)
                        {
                            // remove comment.
                            string word = line.Split(new string[] { "//" }, StringSplitOptions.None)[0].Trim();

                            if (string.IsNullOrEmpty(word))
                            {
                                continue;
                            }

                            if (!wordsInFile.Contains(word))
                            {
                                wordsInFile.Add(word);
                            }
                            else
                            {
                                errors.Add(WordFileError.DuplicateWordsInOneFile, word, file);
                            }
                        }
                    }
                }
            }

            foreach (string word in wordsInFile)
            {
                if (!words.Contains(word))
                {
                    words.Add(word);
                }
                else
                {
                    errors.Add(WordFileError.DuplicateWord, word, file);
                }
            }

            if (sort)
            {
                words.Sort(StringComparer.Ordinal);
            }

            return errors;
        }

        /// <summary>
        /// Collect the words in the file and save them into string pool together with
        /// The offset list.
        /// </summary>
        /// <param name="filePath">File path containing words.</param>
        /// <param name="stringPool">String pool.</param>
        /// <param name="offsets">Offset list.</param>
        /// <param name="sort">Whether the word in the string pool are sorted.</param>
        /// <param name="errorSet">Error set.</param>
        /// <returns>Number of words.</returns>
        public static int LoadWordsIntoStringPool(string filePath, StringPool stringPool,
            ICollection<int> offsets, bool sort, ErrorSet errorSet)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (stringPool == null)
            {
                throw new ArgumentNullException("stringPool");
            }

            if (offsets == null)
            {
                throw new ArgumentNullException("offsets");
            }

            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            List<string> words = new List<string>();
            errorSet.Merge(LoadWordsIntoWordList(filePath, words, sort));
            StringPool.WordsToStringPool(words, stringPool, offsets);
            return words.Count;
        }
   }
}