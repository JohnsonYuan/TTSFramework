//----------------------------------------------------------------------------
// <copyright file="WordBreakerCompiler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Word Breaker Compiler
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
    public enum WordBreakerCompilerError
    {
        /// <summary>
        /// Data Folder Not Found
        /// Parameters: 
        /// {0}: path of word breaker data folder.
        /// </summary>
        [ErrorAttribute(Message = "WordBreaker data folder '{0}' could not be found.")]
        DataFolderNotFound,

        /// <summary>
        /// Invalid Line
        /// Parameters: 
        /// {0}: path of word breaker data file
        /// {1}: line number
        /// {2}: line content.
        /// </summary>
        [ErrorAttribute(Message = "Invalid line (line number: {1}) \"{2}\" in wordbreaker data file '{0}'.",
            Severity = ErrorSeverity.Warning)]
        InvalidLine,

        /// <summary>
        /// Basic Data Not Found
        /// Parameters: 
        /// {0}: path of necessary word breaker data file.
        /// </summary>
        [ErrorAttribute(Message = "Basic data could not be found for wordbreaker data: '{0}'.")]
        BasicDataNotFound,

        /// <summary>
        /// Not found word breaker files
        /// Parameters: 
        /// {0}: path of word breaker data file.
        /// </summary>
        [ErrorAttribute(Message = "Can't find word break file : [{0}].",
            Severity = ErrorSeverity.MustFix)]
        NotFindWordBreakerFile,

        /// <summary>
        /// Invalid format guid
        /// Parameters: 
        /// {0}: format guid of this word breaker.
        /// </summary>
        [ErrorAttribute(Message = "Invalid word breaker format guid : [{0}].")]
        InvalidFormatGuid
    }

    /// <summary>
    /// Compile word breaker data.
    /// </summary>
    public class WordBreakerCompiler
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="WordBreakerCompiler"/> class from being created.
        /// </summary>
        private WordBreakerCompiler()
        {
        }

        /// <summary>
        /// Compile word breaker data table into binary file.
        /// </summary>
        /// <param name="wordBreakerDataDir">Directory of word breaker data.</param>
        /// <param name="outputStream">Output Stream.</param>
        /// <param name="addedFileNames">Word breaker files added to binary.</param>
        /// <param name="formatGuid">Format Guid.</param>
        /// <returns>ErrorSet.</returns>
        public static ErrorSet Compile(string wordBreakerDataDir, Stream outputStream,
            Collection<string> addedFileNames, string formatGuid)
        {
            if (string.IsNullOrEmpty(wordBreakerDataDir))
            {
                throw new ArgumentNullException("wordBreakerDataDir");
            }

            if (addedFileNames == null)
            {
                throw new ArgumentNullException("addedFileNames");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            ErrorSet errorSet = new ErrorSet();
            string basicDataPath = Path.Combine(wordBreakerDataDir, "whitespacebreakingchar.txt");
            if (!Directory.Exists(wordBreakerDataDir))
            {
                errorSet.Add(WordBreakerCompilerError.DataFolderNotFound, wordBreakerDataDir);
            }
            else if (!File.Exists(basicDataPath))
            {
                errorSet.Add(WordBreakerCompilerError.BasicDataNotFound, basicDataPath);
            }
            else
            {
                BinaryWriter outputBinaryWriter = new BinaryWriter(outputStream, Encoding.Unicode);

                // Add breaking word list
                string[] wordBreakerFileNames = new string[]
                {
                    "whitespacebreakingchar.txt",
                    "emitbreakingchar.txt",
                    "boundarybreakingchar.txt"
                };

                foreach (string wordBreakerFileName in wordBreakerFileNames)
                {
                    string wordBreakerFilePath = Path.Combine(wordBreakerDataDir, wordBreakerFileName);
                    if (!File.Exists(wordBreakerFilePath))
                    {
                        // char number in the word breaker file.
                        outputBinaryWriter.Write(0);

                        // string termiator of the chars in the word breaker file.
                        outputBinaryWriter.Write('\0');

                        // used for 4 bytes align.
                        outputBinaryWriter.Write('\0');
                        errorSet.Add(new Error(WordBreakerCompilerError.NotFindWordBreakerFile, wordBreakerFileName));
                    }
                    else
                    {
                        addedFileNames.Add(wordBreakerFileName);
                        errorSet.Merge(WriteBreakingChar(outputBinaryWriter, wordBreakerFilePath));
                    }
                }

                outputBinaryWriter.Flush();

                // This guid is for the new format.
                if (formatGuid == "C4235FEF-CC38-4597-8928-ADD7CB186C79")
                {
                    // used for 4 bytes align.
                    const int ALIGNCOUNT = 4;
                    byte[] alignBytes = new byte[]
                    {
                        0, 0, 0, 0
                    };

                    // Add the special words not in the end of sentences
                    using (MemoryStream specialWordStream = new MemoryStream())
                    {
                        string[] specialWordFileNames = new string[]
                    {
                        "abbrev.txt",
                        "specialwords.txt",
                        "wordbreakerspecialwords.txt",
                        "titles.txt"
                    };

                        errorSet.Merge(WriteWordListIntoMemory(specialWordStream, wordBreakerDataDir, specialWordFileNames, addedFileNames));

                        if (errorSet.GetSeverityCount(ErrorSeverity.MustFix) == 0)
                        {
                            specialWordStream.Seek(0, SeekOrigin.Begin);
                            MemoryStream trieStream = Microsoft.Tts.ServiceProvider.BaseUtils.TrieTreeConverter.Convert(specialWordStream);

                            // Write the length of the trie tree.
                            int trieStringLength = (int)trieStream.Length;
                            outputBinaryWriter.Write(trieStringLength);
                            outputBinaryWriter.Flush();

                            // Write the data of the trie tree.
                            outputStream.Write(trieStream.ToArray(), 0, trieStringLength);

                            // Fill 0 to align the memory by 4 bytes
                            if (trieStringLength % ALIGNCOUNT != 0)
                            {
                                outputStream.Write(alignBytes, 0, ALIGNCOUNT - (trieStringLength % ALIGNCOUNT));
                            }

                            outputStream.Flush();
                        }

                        // Add the special words in the end of sentences
                        using (MemoryStream endSpecialWordStream = new MemoryStream())
                        {
                            string[] endSpecialWordFileNames = new string[]
                            {
                                "endabbr.txt",
                                "specialwords.txt",
                                "wordbreakerspecialwords.txt"
                            };

                            errorSet.Merge(WriteWordListIntoMemory(endSpecialWordStream, wordBreakerDataDir, endSpecialWordFileNames, addedFileNames));

                            if (errorSet.GetSeverityCount(ErrorSeverity.MustFix) == 0)
                            {
                                endSpecialWordStream.Seek(0, SeekOrigin.Begin);
                                MemoryStream trieStream = Microsoft.Tts.ServiceProvider.BaseUtils.TrieTreeConverter.Convert(endSpecialWordStream);

                                // Write the length of the trie tree.
                                int trieStringLength = (int)trieStream.Length;
                                outputBinaryWriter.Write(trieStringLength);
                                outputBinaryWriter.Flush();

                                // Write the data of the trie tree.
                                outputStream.Write(trieStream.ToArray(), 0, trieStringLength);
                                outputStream.Flush();
                            }
                        }
                    }      
                }
                else if (formatGuid == "86405BC7-8654-4cc5-82BD-19A220DBA0BA" || string.IsNullOrEmpty(formatGuid))
                {
                    // Add the special words not in the end of sentences
                    using (MemoryStream specialWordStream = new MemoryStream())
                    {
                        string[] specialWordFileNames = new string[]
                        {
                            "abbrev.txt",
                            "specialwords.txt",
                            "wordbreakerspecialwords.txt",
                            "titles.txt"
                        };

                        errorSet.Merge(WriteWordListIntoMemory(specialWordStream, wordBreakerDataDir, specialWordFileNames, addedFileNames));

                        if (errorSet.GetSeverityCount(ErrorSeverity.MustFix) == 0)
                        {
                            specialWordStream.Seek(0, SeekOrigin.Begin);
                            MemoryStream trieStream = Microsoft.Tts.ServiceProvider.BaseUtils.TrieTreeConverter.Convert(specialWordStream);

                            // Write the data of the trie tree.
                            int trieStringLength = (int)trieStream.Length;
                            outputStream.Write(trieStream.ToArray(), 0, trieStringLength);
                            outputStream.Flush();
                        }
                    }           
                }
                else
                {
                    errorSet.Add(new Error(WordBreakerCompilerError.InvalidFormatGuid, formatGuid));
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Write the word break data into memory.
        /// </summary>
        /// <param name="wordListStream">A memory stream storing the word list.</param>
        /// <param name="wordBreakerDataDir">Directory of word breaker data.</param>
        /// <param name="fileNames">Files of word breaker data.</param>
        /// <param name="addedFileNames">Word breaker files added to binary.</param>
        /// <returns>ErrorSet.</returns>
        private static ErrorSet WriteWordListIntoMemory(MemoryStream wordListStream, string wordBreakerDataDir,
            string[] fileNames, Collection<string> addedFileNames)
        {
            ErrorSet errorSet = new ErrorSet();
            
            BinaryWriter bw = new BinaryWriter(wordListStream, Encoding.Unicode);

            List<string> wordList = new List<string>();

            foreach (string fileName in fileNames)
            {
                string wordBreakerFilePath = Path.Combine(wordBreakerDataDir, fileName);
                if (!File.Exists(wordBreakerFilePath))
                {
                    errorSet.Add(new Error(WordBreakerCompilerError.NotFindWordBreakerFile, fileName));
                }
                else
                {
                    addedFileNames.Add(fileName);
                    errorSet.Merge(WordFile.LoadWordsIntoWordList(wordBreakerFilePath, wordList, false));
                }
            }

            LengthComparer comparer = new LengthComparer();
            wordList.Sort(comparer); // Sort by length in decrease

            using (StringPool sp = new StringPool())
            {
                Collection<int> offsets = new Collection<int>();
                StringPool.WordsToStringPool(wordList, sp, offsets);

                bw.Write(offsets.Count);
                foreach (int offset in offsets)
                {
                    bw.Write(offset);
                }

                byte[] pool = sp.ToArray();
                bw.Write(pool, 0, pool.Length);

                bw.Flush();

                return errorSet;
            }
        }

        /// <summary>
        /// Write the breaking char according to the file.
        /// </summary>
        /// <param name="bw">Binary writer.</param>
        /// <param name="filePath">File containing breaking characters.</param>
        /// <returns>Error Set.</returns>
        private static ErrorSet WriteBreakingChar(BinaryWriter bw, string filePath)
        {
            List<char> charList = new List<char>();
            ErrorSet errorSet = LoadHexWordFile(filePath, charList);
            charList.Sort();
            bw.Write(charList.Count);
            foreach (char ch in charList)
            {
                bw.Write(ch);
            }

            if (charList.Count % 2 == 0)
            {
                bw.Write('\0');
            }

            bw.Write('\0');
            return errorSet;
        }

        /// <summary>
        /// Collect the words in a file with the following line format:
        /// 0x0020,       // white space
        /// 0x0040,       // @
        /// 0x2022,       // � bullet.
        /// </summary>
        /// <param name="filePath">Path of char data.</param>
        /// <param name="charList">Char list.</param>
        /// <returns>Error Set.</returns>
        private static ErrorSet LoadHexWordFile(string filePath, ICollection<char> charList)
        {
            Debug.Assert(filePath != null, "file shouldn't be null.");
            Debug.Assert(charList != null, "charList shouldn't be null.");
            ErrorSet errorSet = new ErrorSet();
            if (charList == null)
            {
                throw new ArgumentNullException("charList");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (File.Exists(filePath))
            {
                // Regex regex = new System.Text.RegularExpressions.Regex(@"0[x]([0-9a-fA-F]{4})",
                //    RegexOptions.IgnoreCase);
                Regex regex = new System.Text.RegularExpressions.Regex(@"0[x]([0-9,a-f]{4})",
                    RegexOptions.IgnoreCase);
                using (StreamReader sr = new StreamReader(filePath))
                {
                    int lineNumber = 0;
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        lineNumber++;

                        // remove comment.
                        int endPos = line.IndexOf(@"//", StringComparison.OrdinalIgnoreCase);
                        if (endPos >= 0)
                        {
                            line = line.Substring(0, endPos);
                        }

                        line = line.Trim();

                        if (string.IsNullOrEmpty(line))
                        {
                            continue;
                        }

                        System.Text.RegularExpressions.Match m = regex.Match(line);
                        if (m.Success)
                        {
                            string charInHex = m.Groups[1].ToString();
                            charInHex = charInHex.Trim();
                            if (charInHex.Length > 0)
                            {
                                // convert Hex number in string to Char
                                ushort charInShort = Convert.ToUInt16(charInHex, 16);
                                char ch = Convert.ToChar(charInShort);

                                if (!charList.Contains(ch))
                                {
                                    charList.Add(ch);
                                }
                            }
                        }
                        else
                        {
                            errorSet.Add(WordBreakerCompilerError.InvalidLine,
                                filePath, lineNumber.ToString(CultureInfo.InvariantCulture), line);
                        }
                    }
                }
            }

            return errorSet;
        }
    }

    /// <summary>
    /// Length comparer.
    /// </summary>
    internal class LengthComparer : IComparer<string>
    {
        /// <summary>
        /// Compare function.
        /// </summary>
        /// <param name="x">Parameter x.</param>
        /// <param name="y">Y.</param>
        /// <returns>1 for the length of x less than the one of y.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1309:UseOrdinalStringComparison", MessageId = "System.string.Compare(System.string,System.string,System.StringComparison)", Justification = "Use ordinal currently will lead to unit test failure")]
        public int Compare(string x, string y)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (y == null)
            {
                throw new ArgumentNullException("y");
            }
            
            if (x.Length < y.Length)
            {
                return 1;
            }
            else if (x.Length == y.Length)
            {
                return string.Compare(x, y, StringComparison.InvariantCulture);
            }
            else
            {
                return -1;
            }
        }
    }
}