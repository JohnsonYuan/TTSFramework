//----------------------------------------------------------------------------
// <copyright file="PostWordBreakerCompiler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Post Word Breaker Compiler
// </summary>
//----------------------------------------------------------------------------
namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Word breaker error definition.
    /// </summary>
    public enum PostWordBreakerCompilerError
    {
        /// <summary>
        /// Data file Not Found
        /// Parameters: 
        /// {0}: path of post word breaker data file.
        /// </summary>
        [ErrorAttribute(Message = "PostWordBreaker data file '{0}' could not be found.")]
        DataFileNotFound,

        /// <summary>
        /// Invalid Line
        /// Parameters: 
        /// {0}: path of post word breaker data file
        /// {1}: line number
        /// {2}: line content.
        /// </summary>
        [ErrorAttribute(Message = "Invalid line (line number: {1}) \"{2}\" in PostWordBreaker data file '{0}' : {4}",
            Severity = ErrorSeverity.MustFix)]
        InvalidLine,
    }

    /// <summary>
    /// Compile post word breaker data.
    /// </summary>
    public class PostWordBreakerCompiler
    {
        private const int MaxGrams = 5;
        private const string PairSeparator = "=>";
        private const string WordListSeparator = ",";

        /// <summary>
        /// Prevents a default instance of the <see cref="PostWordBreakerCompiler"/> class from being created.
        /// </summary>
        private PostWordBreakerCompiler()
        {
        }

        /// <summary>
        /// Compile post word breaker data table into binary file.
        /// </summary>
        /// <param name="postWordBreakerDataFile">Path of post word breaker data file.</param>
        /// <param name="outputStream">Output Stream.</param>
        /// <returns>ErrorSet.</returns>
        public static ErrorSet Compile(string postWordBreakerDataFile, Stream outputStream)
        {
            if (string.IsNullOrEmpty(postWordBreakerDataFile))
            {
                throw new ArgumentNullException("postWordBreakerDataFile");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            ErrorSet errorSet = new ErrorSet();
            if (!File.Exists(postWordBreakerDataFile))
            {
                errorSet.Add(PostWordBreakerCompilerError.DataFileNotFound, postWordBreakerDataFile);
            }
            else
            {
                BinaryWriter outputBinaryWriter = new BinaryWriter(outputStream, Encoding.Unicode);
                List<string> fileLines = new List<string>(Helper.FileLines(postWordBreakerDataFile, Encoding.Unicode));
                List<string> wordList = new List<string>();

                // Load words from raw data file to a word list
                for (int i = 0; i < fileLines.Count; ++i)
                {
                    if (!string.IsNullOrWhiteSpace(fileLines[i]) && !fileLines[i].StartsWith("//"))
                    {
                        string fileLine = fileLines[i];

                        // Remove comment at the end of line
                        if (fileLine.Contains("//"))
                        {
                            fileLine = fileLine.Remove(fileLines[i].IndexOf("//"));
                        }

                        // Clean white spaces among the line
                        fileLine = fileLine.Trim();

                        // Start parsing the line
                        if (fileLine.Contains(PairSeparator))
                        {
                            string[] segments = fileLine.Split(
                                new string[] { PairSeparator }, StringSplitOptions.RemoveEmptyEntries);
                            if (segments.Length == 2)
                            {
                                segments[0] = segments[0].Trim();
                                segments[1] = segments[1].Trim();

                                string[] patternWords = segments[0].Split(
                                    new string[] { WordListSeparator }, StringSplitOptions.RemoveEmptyEntries);
                                string[] replacementWords = segments[1].Split(
                                    new string[] { WordListSeparator }, StringSplitOptions.RemoveEmptyEntries);

                                if (patternWords.Length == 1 && replacementWords.Length == 1)
                                {
                                    // It's invalid if both pattern and replacement contain only one word
                                    errorSet.Add(PostWordBreakerCompilerError.InvalidLine,
                                        postWordBreakerDataFile, i.ToString(), fileLines[i],
                                        "Both pattern and replacement contain only one word.");
                                }
                                else if (patternWords.Length > MaxGrams || replacementWords.Length > MaxGrams)
                                {
                                    // It's invalid if either pattern or replacement contain more than MaxGrams words
                                    errorSet.Add(PostWordBreakerCompilerError.InvalidLine,
                                        postWordBreakerDataFile, i.ToString(), fileLines[i],
                                        "Either pattern or replacement contain more than 5 words.");
                                }
                                else
                                {
                                    bool spaceInsideWord = false;
                                    for (int j = 0; j < patternWords.Length; ++j)
                                    {
                                        patternWords[j] = patternWords[j].Trim();
                                        if (patternWords[j].Contains(" "))
                                        {
                                            spaceInsideWord = true;
                                            break;
                                        }
                                    }

                                    if (!spaceInsideWord)
                                    {
                                        for (int j = 0; j < replacementWords.Length; ++j)
                                        {
                                            replacementWords[j] = replacementWords[j].Trim();
                                            if (replacementWords[j].Contains(" "))
                                            {
                                                spaceInsideWord = true;
                                                break;
                                            }
                                        }
                                    }

                                    if (!spaceInsideWord)
                                    {
                                        // Pattern and replacement must have same count of characters
                                        if (string.Join(string.Empty, patternWords) == string.Join(string.Empty, replacementWords))
                                        {
                                            wordList.AddRange(patternWords);
                                            wordList.Add(string.Empty);

                                            // Replace wild characters (*, ?) in replacement words to placeholders (/1, /2)
                                            WildCharToPlaceholder(replacementWords);
                                            wordList.AddRange(replacementWords);
                                            wordList.Add(string.Empty);
                                        }
                                        else
                                        {
                                            errorSet.Add(PostWordBreakerCompilerError.InvalidLine,
                                                postWordBreakerDataFile, i.ToString(), fileLines[i],
                                                "Pattern and replacement must have same content.");
                                        }
                                    }
                                    else
                                    {
                                        errorSet.Add(PostWordBreakerCompilerError.InvalidLine,
                                            postWordBreakerDataFile, i.ToString(), fileLines[i],
                                            "White space is not allowed to be inside a word.");
                                    }
                                }
                            }
                            else
                            {
                                errorSet.Add(PostWordBreakerCompilerError.InvalidLine,
                                    postWordBreakerDataFile, i.ToString(), fileLines[i],
                                    "More than 2 word lists found.");
                            }
                        }
                        else
                        {
                            errorSet.Add(PostWordBreakerCompilerError.InvalidLine,
                                postWordBreakerDataFile, i.ToString(), fileLines[i],
                                "No word list separator (=>) found.");
                        }
                    }
                }

                List<int> offsetList = new List<int>();
                using (StringPool stringPool = new StringPool())
                {
                    // Put the words from word list to string pool
                    StringPool.WordsToStringPool(wordList, stringPool, offsetList);

                    // Start writing binary to output stream

                    // Write table count
                    outputBinaryWriter.Write((uint)1);

                    // Write word count in each table
                    outputBinaryWriter.Write((uint)wordList.Count);

                    // Write offset of each word
                    offsetList.ForEach(x => outputBinaryWriter.Write((uint)x));

                    // Write the strings from string pool
                    byte[] stringBuffer = stringPool.ToArray();
                    outputBinaryWriter.Write(stringBuffer, 0, stringBuffer.Length);
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Replace the wild characters (*, ?) in given string array to placeholders (/1, /2).
        /// </summary>
        /// <param name="words">Input string array, also the output string array.</param>
        private static void WildCharToPlaceholder(string[] words)
        {
            int wildCharOrder = 0;
            for (int i = 0; i < words.Length; ++i)
            {
                string updatedWord = string.Empty;
                foreach (char character in words[i])
                {
                    if (character == '*' || character == '?')
                    {
                        ++wildCharOrder;
                        updatedWord += "/" + wildCharOrder;
                    }
                    else
                    {
                        updatedWord += character;
                    }
                }

                words[i] = updatedWord;
            }
        }
    }
}