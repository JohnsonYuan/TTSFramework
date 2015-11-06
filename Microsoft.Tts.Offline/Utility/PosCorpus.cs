//----------------------------------------------------------------------------
// <copyright file="PosCorpus.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements pos corpus.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// FrontEndCompilerError.
    /// </summary>
    public enum PosCorpusError
    {
        /// <summary>
        /// Empty Pos.
        /// Parameters: 
        /// {0}: word pos pair.
        /// </summary>
        [ErrorAttribute(Message = "Empty POS [{0}] in POS corpus.")]
        EmptyPos,

        /// <summary>
        /// Empty Word.
        /// Parameters: 
        /// {0}: word pos pair.
        /// </summary>
        [ErrorAttribute(Message = "Empty word [{0}] in POS corpus.")]
        EmptyWord,

        /// <summary>
        /// Invalid Format.
        /// Parameters: 
        /// {0}: word pos pair.
        /// </summary>
        [ErrorAttribute(Message = "Invalid format [{0}] in POS corpus.")]
        InvalidFormat,

        /// <summary>
        /// No PosTagging Pos.
        /// Parameters: 
        /// {0}: invalid POS.
        /// </summary>
        [ErrorAttribute(Message = "Can't find pos tagging pos [{0}] in schema.")]
        NoPosTaggingPos,

        /// <summary>
        /// Error With Line.
        /// Parameters: 
        /// {0}: error message.
        /// </summary>
        [ErrorAttribute(Message = "Error in line [{0}] :")]
        ErrorWithLine,
    }

    /// <summary>
    /// PosCorpus.
    /// </summary>
    public class PosCorpus
    {
        #region Fields

        private Collection<PosCorpusParagraph> _paragraphs = new Collection<PosCorpusParagraph>();
        private string _filePath;

        #endregion

        #region Properties

        /// <summary>
        /// Gets FilePath.
        /// </summary>
        public string FilePath
        {
            get { return _filePath; }
        }

        /// <summary>
        /// Gets Paragraphs.
        /// </summary>
        public Collection<PosCorpusParagraph> Paragraphs
        {
            get { return _paragraphs; }
        }

        #endregion

        /// <summary>
        /// Load.
        /// </summary>
        /// <param name="filePath">FilePath.</param>
        /// <returns>ErrorSet.</returns>
        public ErrorSet Load(string filePath)
        {
            return Load(filePath, null);
        }

        /// <summary>
        /// Load.
        /// </summary>
        /// <param name="filePath">FilePath.</param>
        /// <param name="attributeSchema">LexicalAttributeSchema.</param>
        /// <returns>The errotset.</returns>
        public ErrorSet Load(string filePath, LexicalAttributeSchema attributeSchema)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (!File.Exists(filePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException), filePath);
            }

            if (!Helper.IsUnicodeFile(filePath))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Invalid corpus file format(not UNICODE), should be UNICODE."));
            }

            _paragraphs.Clear();
            int lineNumber = 0;
            ErrorSet errorSetWithLine = new ErrorSet();

            foreach (string line in Helper.FileLines(filePath, Encoding.Unicode, false))
            {
                lineNumber ++;

                if (string.IsNullOrEmpty(line.Trim()))
                {
                    continue;
                }

                PosCorpusParagraph paragraph = new PosCorpusParagraph();
                ErrorSet errorSet = paragraph.Parse(line, attributeSchema);

                if (errorSet.Errors.Count == 0)
                {
                    Debug.Assert(paragraph.Words.Count > 0);
                    _paragraphs.Add(paragraph);
                }
                else
                {
                    foreach (Error error in errorSet.Errors)
                    {
                        errorSetWithLine.Add(PosCorpusError.ErrorWithLine,
                            error, lineNumber.ToString(CultureInfo.InvariantCulture));
                    }
                }
            }

            _filePath = filePath;
            return errorSetWithLine;
        }

        /// <summary>
        /// Save.
        /// </summary>
        /// <param name="filePath">FilePath.</param>
        public void Save(string filePath)
        {
            Helper.EnsureFolderExistForFile(filePath);
            using (StreamWriter cleanCorpusWriter = new StreamWriter(filePath, false, Encoding.Unicode))
            {
                foreach (PosCorpusParagraph paragraph in _paragraphs)
                {
                    cleanCorpusWriter.WriteLine(paragraph.ToString(true, false));
                }
            }

            _filePath = filePath;
        }

        /// <summary>
        /// Sub.
        /// </summary>
        /// <param name="posCorpus">PosCorpus.</param>
        /// <returns>Return PosCorpus.</returns>
        public PosCorpus Sub(PosCorpus posCorpus)
        {
            PosCorpus resultPosCorpus = new PosCorpus();

            foreach (PosCorpusParagraph paragraph in _paragraphs)
            {
                if (!posCorpus.Paragraphs.Contains(paragraph))
                {
                    resultPosCorpus.Paragraphs.Add(paragraph);
                }
            }

            return resultPosCorpus;
        }

        /// <summary>
        /// RandomDumpLines.
        /// </summary>
        /// <param name="percentage">Percentage.</param>
        /// <returns>PosCorpus.</returns>
        public PosCorpus RandomDumpLines(double percentage)
        {
            PosCorpus dumpedCorpus = new PosCorpus();
            Random random = new Random(_paragraphs.Count);
            foreach (PosCorpusParagraph paragraph in _paragraphs)
            {
                if (random.NextDouble() <= percentage)
                {
                    dumpedCorpus.Paragraphs.Add(paragraph);
                }
            }

            return dumpedCorpus;
        }

        /// <summary>
        /// ExportCorpus.
        /// </summary>
        /// <param name="targetFilePath">TargetFilePath.</param>
        /// <param name="withPos">WithPos.</param>
        /// <param name="toLower">ToLower.</param>
        public void ExportCorpus(string targetFilePath, bool withPos, bool toLower)
        {
            Helper.EnsureFolderExistForFile(targetFilePath);
            using (StreamWriter cleanCorpusWriter = new StreamWriter(targetFilePath, false, Encoding.Unicode))
            {
                foreach (PosCorpusParagraph paragraph in _paragraphs)
                {
                    cleanCorpusWriter.WriteLine(paragraph.ToString(withPos, toLower));
                }
            }
        }
    }

    /// <summary>
    /// PosCorpusParagraph.
    /// </summary>
    public class PosCorpusParagraph
    {
        #region Fileds

        private Collection<PosCorpusWord> _words = new Collection<PosCorpusWord>();
        private char[] _wordDelimeters = new char[] { '\t', ' ', '\n' };

        #endregion

        /// <summary>
        /// Gets Words.
        /// </summary>
        public Collection<PosCorpusWord> Words
        {
            get { return _words; }
        }

        /// <summary>
        /// Parse.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <param name="attributeSchema">LexicalAttributeSchema.</param>
        /// <returns>ErrorSet.</returns>
        public ErrorSet Parse(string line, LexicalAttributeSchema attributeSchema)
        {
            if (string.IsNullOrEmpty(line))
            {
                throw new ArgumentNullException("line");
            }

            ErrorSet errorSet = new ErrorSet();
            _words.Clear();
            string[] wordWithPosTags = line.Split(_wordDelimeters, StringSplitOptions.RemoveEmptyEntries);
            foreach (string wordWithPosTag in wordWithPosTags)
            {
                PosCorpusWord word = new PosCorpusWord();
                ErrorSet wordErrorSet = word.Parse(wordWithPosTag, attributeSchema);
                errorSet.AddRange(wordErrorSet);
                if (wordErrorSet.Count == 0)
                {
                    _words.Add(word);
                }
            }

            return errorSet;
        }

        /// <summary>
        /// ToString.
        /// </summary>
        /// <param name="withPos">WithPos.</param>
        /// <returns>String.</returns>
        public string ToString(bool withPos)
        {
            return this.ToString(withPos, true);
        }

        /// <summary>
        /// ToString.
        /// </summary>
        /// <param name="withPos">WithPos.</param>
        /// <param name="toLower">ToLower.</param>
        /// <returns>The string.</returns>
        public string ToString(bool withPos, bool toLower)
        {
            StringBuilder sb = new StringBuilder();
            foreach (PosCorpusWord word in _words)
            {
                if (sb.Length > 0)
                {
                    sb.Append(" ");
                }

                sb.Append(word.ToString(withPos, toLower));
            }

            return sb.ToString();
        }

        /// <summary>
        /// ToString.
        /// </summary>
        /// <returns>String.</returns>
        public override string ToString()
        {
            return this.ToString(true);
        }
    }

    /// <summary>
    /// PosCorpusWord.
    /// </summary>
    public class PosCorpusWord
    {
        #region Fileds

        private const char WordPosDelimeter = '/';
        private string _wordText;
        private string _pos;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Original word text.
        /// </summary>
        public string WordText
        {
            get
            {
                return _wordText;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _wordText = value;
            }
        }

        /// <summary>
        /// Gets Lower letters word text.
        /// </summary>
        public string LowerWordText
        {
            get
            {
                return _wordText.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Gets or sets Pos.
        /// </summary>
        public string Pos
        {
            get
            {
                return _pos;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _pos = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Parse word pos pair.
        /// </summary>
        /// <param name="wordPosPair">Word.</param>
        /// <param name="attributeSchema">AttributeSchema.</param>
        /// <returns>ErrorSet.</returns>
        public ErrorSet Parse(string wordPosPair, LexicalAttributeSchema attributeSchema)
        {
            ErrorSet errorSet = new ErrorSet();
            int slashIndex = wordPosPair.LastIndexOf(WordPosDelimeter);

            if (slashIndex < 0 || slashIndex > wordPosPair.Length - 1)
            {
                errorSet.Add(PosCorpusError.InvalidFormat, wordPosPair);
            }
            else if (slashIndex == 0)
            {
                errorSet.Add(PosCorpusError.EmptyWord, wordPosPair);
            }
            else if (slashIndex == wordPosPair.Length - 1)
            {
                errorSet.Add(PosCorpusError.EmptyPos, wordPosPair);
            }
            else
            {
                WordText = wordPosPair.Substring(0, slashIndex);
                string originalPos = wordPosPair.Substring(slashIndex + 1);
                if (attributeSchema != null)
                {
                    string posTaggingPos = attributeSchema.GetPosTaggingPos(originalPos);
                    if (string.IsNullOrEmpty(posTaggingPos))
                    {
                        errorSet.Add(PosCorpusError.NoPosTaggingPos, originalPos);
                    }
                    else
                    {
                        Pos = posTaggingPos;
                    }
                }
                else
                {
                    Pos = originalPos;
                }
            }

            return errorSet;
        }

        /// <summary>
        /// ToString.
        /// </summary>
        /// <returns>String.</returns>
        public override string ToString()
        {
            return this.ToString(true);
        }

        /// <summary>
        /// ToString.
        /// </summary>
        /// <param name="withPos">WithPos.</param>
        /// <param name="toLower">ToLower.</param>
        /// <returns>String.</returns>
        public string ToString(bool withPos, bool toLower)
        {
            if (string.IsNullOrEmpty(_wordText))
            {
                throw new InvalidDataException("_wordText should not be Empty");
            }

            string wordText = toLower ? WordText : _wordText;
            return withPos ? (wordText + WordPosDelimeter + _pos) : wordText;
        }

        /// <summary>
        /// ToString.
        /// </summary>
        /// <param name="withPos">WithPos.</param>
        /// <returns>String.</returns>
        public string ToString(bool withPos)
        {
            return this.ToString(withPos, true);
        }

        #endregion
    }
}