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
    using Microsoft.Tts.ServiceProvider;
    using Microsoft.Tts.ServiceProvider.BaseUtils;

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
        [ErrorAttribute(Message = "Invalid line (line number: {1}) \"{2}\" in PostWordBreaker data file '{0}' : {3}",
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
        private const int TrieAlign = 4;

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
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
                List<string> patternWordList = new List<string>();
                Dictionary<string, string> pattern2Replace = new Dictionary<string, string>();

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
                                segments[0] = segments[0].Replace(" ", string.Empty);
                                segments[1] = segments[1].Replace(" ", string.Empty);

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
                                    patternWordList.Add(segments[0]);

                                    // Replace wild characters (*, ?) in replacement words to placeholders (/1, /2)
                                    WildCharToPlaceholder(ref segments[1]);
                                    pattern2Replace.Add(segments[0], segments[1]);
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

                List<int> replaceWordOffsetList = new List<int>();
                TrieTree trieTree = new TrieTree(patternWordList);

                // sorted by trie id
                List<string> sortedReplaceWordList = SortReplaceWordList(pattern2Replace, trieTree);

                using (StringPool stringPool = new StringPool())
                {
                    // Put the words from word list to string pool
                    StringPool.WordsToStringPool(sortedReplaceWordList, stringPool, replaceWordOffsetList);

                    // Start writing binary to output stream
                    uint trieOffset = 0;
                    uint trieSize = 0;

                    // Write TrieTree offset and size 
                    outputBinaryWriter.Write(trieOffset);
                    outputBinaryWriter.Write(trieSize);

                    // Write replace word count 
                    outputBinaryWriter.Write((uint)sortedReplaceWordList.Count);

                    // Write offset of each word
                    replaceWordOffsetList.ForEach(x => outputBinaryWriter.Write((uint)x));

                    // Write the strings from string pool
                    byte[] stringBuffer = stringPool.ToArray();
                    outputBinaryWriter.Write(stringBuffer, 0, stringBuffer.Length);

                    PadBytes(outputBinaryWriter, TrieAlign);

                    trieOffset = (uint)outputBinaryWriter.BaseStream.Position;
                    outputBinaryWriter.Write(trieTree.GetTrieData());
                    trieSize = (uint)(outputBinaryWriter.BaseStream.Position - trieOffset);

                    outputBinaryWriter.Seek(0, SeekOrigin.Begin);
                    outputBinaryWriter.Write(trieOffset);
                    outputBinaryWriter.Write(trieSize);

                    outputBinaryWriter.Flush();
                }
            }

            return errorSet;
        }

        private static int PadBytes(BinaryWriter writer, int alignment)
        {
            int padCount = 0;
            var position = writer.BaseStream.Position;

            if (position % alignment != 0)
            {
                padCount = (int)(alignment - (position % alignment));
                writer.Write(new byte[padCount]);
            }

            return padCount;
        }

        /// <summary>
        /// Replace the wild characters (*, ?) in given string array to placeholders (/1, /2).
        /// </summary>
        /// <param name="word">Input string array, also the output string array.</param>
        private static void WildCharToPlaceholder(ref string word)
        {
            int wildCharOrder = 0;

            string updatedWord = string.Empty;
            foreach (char character in word)
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

            word = updatedWord;
        }

        /// <summary>
        /// Sort the replace word list using Trie Tree id.
        /// </summary>
        /// <param name="pattern2Repalce">PatternWord:key, ReplaceWord:Value.</param>
        /// <param name="trieTree">Trie tree build by pattern word.</param>
        /// <returns>SortedReplaceWordList.</returns>
        private static List<string> SortReplaceWordList(Dictionary<string, string> pattern2Repalce, TrieTree trieTree)
        {
            List<string> sortedRepalceWordList = new List<string>();

            for (int i = 0; i < pattern2Repalce.Count; i++)
            {
               string word = trieTree.ID2Word(i);
               if (pattern2Repalce.ContainsKey(word))
               {
                   sortedRepalceWordList.Add(pattern2Repalce[word]);
               }
            }

            return sortedRepalceWordList;
        }
    }
}