//----------------------------------------------------------------------------
// <copyright file="ChineseToneCompiler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Neutral Pattern List Compiler
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
    /// Chinese tone error definition.
    /// </summary>
    public enum ChineseToneCompilerError
    {
        /// <summary>
        /// Data file Not Found
        /// Parameters: 
        /// {0}: path of Chinese tone data file.
        /// </summary>
        [ErrorAttribute(Message = "ChineseTone data file '{0}' could not be found.")]
        DataFileNotFound,

        /// <summary>
        /// Invalid Line
        /// Parameters: 
        /// {0}: path of Chinese tone data file
        /// {1}: line number
        /// {2}: line content.
        /// </summary>
        [ErrorAttribute(Message = "Invalid line (line number: {1}) \"{2}\" in ChineseTone data file '{0}' : {4}",
            Severity = ErrorSeverity.MustFix)]
        InvalidLine,

        /// <summary>
        /// Invalid Pattern From Data
        /// Parameters:
        /// None.
        /// </summary>
        [ErrorAttribute(Message = "Invalid pattern form data.")]
        InvalidPatternFormData,
    }

    /// <summary>
    /// Compile Chinese tone data.
    /// </summary>
    public class ChineseToneCompiler
    {
        private const string PairSeparator = ",";

        /// <summary>
        /// Compile Chinese tone data table into binary file.
        /// </summary>
        /// <param name="chineseToneDataFile">Path of Chinese tone data file.</param>
        /// <param name="outputStream">Output Stream.</param>
        /// <returns>ErrorSet.</returns>
        public static ErrorSet Compile(string chineseToneDataFile, Stream outputStream)
        {
            if (string.IsNullOrEmpty(chineseToneDataFile))
            {
                throw new ArgumentNullException("chineseToneDataFile");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            ErrorSet errorSet = new ErrorSet();

            if (!File.Exists(chineseToneDataFile))
            {
                errorSet.Add(ChineseToneCompilerError.DataFileNotFound, chineseToneDataFile);
            }
            else
            {
                BinaryWriter outputBinaryWriter = new BinaryWriter(outputStream, Encoding.Unicode);
                List<string> fileLines = new List<string>(Helper.FileLines(chineseToneDataFile, Encoding.Unicode));

                List<string> wordListAABB = new List<string>();
                List<string> wordListAAB = new List<string>();

                int nTableIdx = -1;

                // Load words from raw data file to a word list
                for (int i = 0; i < fileLines.Count; ++i)
                {
                    string fileLine = fileLines[i];

                    if (fileLine.Contains("[ABAB]"))
                    {
                        nTableIdx = 0;
                        continue;
                    }
                    else if (fileLine.Contains("[AAB]"))
                    {
                        nTableIdx = 1;
                        continue;
                    }

                    if ((nTableIdx == 0 || nTableIdx == 1) && !string.IsNullOrWhiteSpace(fileLines[i]) && !fileLines[i].StartsWith("//"))
                    {
                        // Remove comment at the end of line
                        if (fileLine.Contains("//"))
                        {
                            fileLine = fileLine.Remove(fileLines[i].IndexOf("//"));
                        }

                        // Clean white spaces among the line
                        fileLine = fileLine.Trim();

                        string[] segments = fileLine.Split(
                            new string[] { PairSeparator }, StringSplitOptions.RemoveEmptyEntries);

                        if (segments.Length == 2)
                        {
                            segments[0] = segments[0].Trim();
                            segments[1] = segments[1].Trim();

                            switch (nTableIdx)
                            {
                                case 0:
                                    wordListAABB.AddRange(segments);
                                    wordListAABB.Add(string.Empty);
                                    break;
                                case 1:
                                    wordListAAB.AddRange(segments);
                                    wordListAAB.Add(string.Empty);
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            errorSet.Add(ChineseToneCompilerError.InvalidPatternFormData);
                        }
                    }
                }

                if (errorSet.Count == 0)
                {
                    // used for 4 bytes align.
                    const int ALIGNCOUNT = 4;
                    byte[] alignBytes = new byte[]
                    {
                        0, 0, 0, 0
                    };

                    // Start writing binary to output stream
                    // Write table count
                    outputBinaryWriter.Write((uint)2);

                    // Write word count in each table
                    outputBinaryWriter.Write((uint)wordListAABB.Count);
                    outputBinaryWriter.Write((uint)wordListAAB.Count);

                    List<int> offsetList = new List<int>();
                    using (StringPool stringPool = new StringPool())
                    {
                        // Put the words from word list to string pool
                        StringPool.WordsToStringPool(wordListAABB, stringPool, offsetList);

                        // Write the strings from string pool
                        byte[] stringBuffer = stringPool.ToArray();

                        int nWordByteSize = sizeof(int) * offsetList.Count;
                        int nBufferByteSize = stringBuffer.Length;

                        int nUnAlignSize = stringBuffer.Length % ALIGNCOUNT;
                        if (nUnAlignSize != 0)
                        {
                            nBufferByteSize += ALIGNCOUNT - nUnAlignSize;
                        }

                        int nTableByteSize = nWordByteSize + nBufferByteSize;

                        // Write offset of each table
                        outputBinaryWriter.Write((uint)nTableByteSize);

                        // Write offset of each word
                        offsetList.ForEach(x => outputBinaryWriter.Write((uint)x));

                        outputBinaryWriter.Write(stringBuffer, 0, stringBuffer.Length);

                        // Fill 0 to align the memory by 4 bytes
                        if (stringBuffer.Length % ALIGNCOUNT != 0)
                        {
                            outputBinaryWriter.Write(alignBytes, 0, ALIGNCOUNT - (stringBuffer.Length % ALIGNCOUNT));
                        }
                    }

                    offsetList = new List<int>();
                    using (StringPool stringPool = new StringPool())
                    {
                        // Put the words from word list to string pool
                        StringPool.WordsToStringPool(wordListAAB, stringPool, offsetList);

                        // Write the strings from string pool
                        byte[] stringBuffer = stringPool.ToArray();

                        int nWordByteSize = sizeof(int) * offsetList.Count;
                        int nBufferByteSize = stringBuffer.Length;

                        int nUnAlignSize = stringBuffer.Length % ALIGNCOUNT;
                        if (nUnAlignSize != 0)
                        {
                            nBufferByteSize += ALIGNCOUNT - nUnAlignSize;
                        }

                        int nTableByteSize = nWordByteSize + nBufferByteSize;

                        // Write offset of each table
                        outputBinaryWriter.Write((uint)nTableByteSize);

                        // Write offset of each word
                        offsetList.ForEach(x => outputBinaryWriter.Write((uint)x));

                        outputBinaryWriter.Write(stringBuffer, 0, stringBuffer.Length);

                        // Fill 0 to align the memory by 4 bytes
                        if (stringBuffer.Length % ALIGNCOUNT != 0)
                        {
                            outputBinaryWriter.Write(alignBytes, 0, ALIGNCOUNT - (stringBuffer.Length % ALIGNCOUNT));
                        }
                    }
                }
            }

            return errorSet;
        }
    }
}