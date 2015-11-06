//----------------------------------------------------------------------------
// <copyright file="WordList.cs" company="MICROSOFT">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      This module implements WordList.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Word list class, it is composed of word list line, each line is a WordListItem.
    /// <example>
    ///  to   7973445 0.0687747986353419 Examples: Turn right on to Factoria Blvd SE
    ///  on   7477083 0.133268236252926  Examples: Turn right on to Factoria Blvd SE
    ///  right    5465478 0.180410613092033  Examples: Turn right on to Factoria Blvd SE
    ///  Turn 5404304  0.227025334754272  Examples: Turn left onto Alexander St.
    /// </example>
    /// </summary>
    public class WordList
    {
        #region Fields

        private ErrorSet _errorSet = new ErrorSet();
        private Dictionary<string, WordListItem> _words = new Dictionary<string, WordListItem>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets Error set.
        /// </summary>
        public ErrorSet ErrorSet
        {
            get { return _errorSet; }
        }

        /// <summary>
        /// Gets Words.
        /// </summary>
        public Dictionary<string, WordListItem> Words
        {
            get { return _words; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Load WordList from script folder.
        /// </summary>
        /// <param name="sourceDir">Source script folder.</param>
        /// <returns>WordList.</returns>
        public static WordList LoadWordListFromScriptDir(string sourceDir)
        {
            Helper.ThrowIfDirectoryNotExist(sourceDir);
            WordList list = new WordList();
            foreach (string relativeFilePath in Helper.GetSubFilesRelativePath(sourceDir, "*.xml"))
            {
                string filePath = Path.Combine(sourceDir, relativeFilePath);

                XmlScriptFile xmlScriptFile = new XmlScriptFile(filePath);
                foreach (ScriptItem item in xmlScriptFile.Items)
                {
                    foreach (ScriptSentence sentence in item.Sentences)
                    {
                        foreach (ScriptWord word in sentence.Words)
                        {
                            string wordLowerCase = word.Grapheme.ToLower();
                            if (WordListItem.IsValidGrapheme(wordLowerCase))
                            {
                                WordListItem wordListItem = null;
                                if (list.Words.ContainsKey(wordLowerCase))
                                {
                                    wordListItem = list.Words[wordLowerCase];

                                    wordListItem.Frequency += item.Frequency;
                                }
                                else
                                {
                                    wordListItem = new WordListItem(wordLowerCase);

                                    wordListItem.Frequency = item.Frequency;

                                    list.Words.Add(wordLowerCase, wordListItem);
                                }

                                wordListItem.AddExample(word.Grapheme, sentence.Text);

                                if (!wordListItem.PronSources.Contains(word.PronSource))
                                {
                                    wordListItem.PronSources.Add(word.PronSource);
                                }

                                if (!string.IsNullOrEmpty(word.Pronunciation))
                                {
                                    if (wordListItem.Pronunciations.ContainsKey(word.Pronunciation))
                                    {
                                        wordListItem.Pronunciations[word.Pronunciation]++;
                                    }
                                    else
                                    {
                                        wordListItem.Pronunciations.Add(word.Pronunciation, 1);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Load WordList from word list file.
        /// </summary>
        /// <param name="sourceFilePath">Source word list file path.</param>
        /// <returns>WordList.</returns>
        public static WordList LoadWordListFromWordListFile(string sourceFilePath)
        {
            Helper.ThrowIfFileNotExist(sourceFilePath);
            WordList list = new WordList();
            ErrorSet duplicateWordsErrorSet = new ErrorSet();
            ErrorSet invalidFormatErrorSet = new ErrorSet();
            int lineCount = 0;
            foreach (string text in Helper.FileLines(sourceFilePath))
            {
                lineCount++;
                WordListItem wordListItem = WordListItem.ParseLine(text);
                if (wordListItem != null)
                {
                    if (!list.Words.ContainsKey(wordListItem.Grapheme))
                    {
                        list.Words.Add(wordListItem.Grapheme, wordListItem);
                    }
                    else
                    {
                        Error error = new Error(WordListError.DuplicateWordInWordListFileError,
                            lineCount.ToString(), sourceFilePath);
                        duplicateWordsErrorSet.Add(error);
                    }
                }
                else
                {
                    Error error = new Error(WordListError.InvalidFormatInWordListFileError,
                        lineCount.ToString(), sourceFilePath);
                    invalidFormatErrorSet.Add(error);
                }
            }

            list.ErrorSet.Merge(invalidFormatErrorSet);
            list.ErrorSet.Merge(duplicateWordsErrorSet);
            return list;
        }

        /// <summary>
        /// Import word list.
        /// </summary>
        /// <param name="otherWordList">Other word list.</param>
        public void ImportWordList(WordList otherWordList)
        {
            Dictionary<string, WordListItem> otherWordListDictionary = otherWordList.Words;

            foreach (var otherWordListPair in otherWordListDictionary)
            {
                if (!Words.ContainsKey(otherWordListPair.Key))
                {
                    Words.Add(otherWordListPair.Key, otherWordListPair.Value);
                }
                else
                {
                    WordListItem thisItem = Words[otherWordListPair.Key];
                    WordListItem otherItem = otherWordListDictionary[otherWordListPair.Key];

                    // merge examples
                    thisItem.ImportExamples(otherItem);

                    // merge prons
                    foreach (var pronunciationsPair in otherItem.Pronunciations)
                    {
                        if (!thisItem.Pronunciations.ContainsKey(pronunciationsPair.Key))
                        {
                            thisItem.Pronunciations.Add(pronunciationsPair.Key);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save function.
        /// </summary>
        /// <param name="targetFilePath">Target file path.</param>
        /// <param name="resetFcs">Whether reset fcs.</param>
        public void Save(string targetFilePath, bool resetFcs)
        {
            if (resetFcs)
            {
                Sort();
                ResetFcs();
            }

            Save(targetFilePath);
        }

        /// <summary>
        /// Save function.
        /// </summary>
        /// <param name="targetFilePath">Target file path.</param>
        public void Save(string targetFilePath)
        {
            Helper.ThrowIfNull(targetFilePath);
            Helper.EnsureFolderExistForFile(targetFilePath);
            using (StreamWriter sw = new StreamWriter(targetFilePath, false, Encoding.Unicode))
            {
                foreach (KeyValuePair<string, WordListItem> pair in _words)
                {
                    sw.WriteLine(pair.Value.ToString());
                }
            }
        }

        /// <summary>
        /// Get high frequency words with fcs corpus coverage.
        /// </summary>
        /// <param name="fcs">Fcs for filter words.</param>
        /// <returns>Word list in which the word fcs is less than or equal to specified fcs.</returns>
        public WordList GetHighFrequencyWords(double fcs)
        {
            WordList wordList = new WordList();
            foreach (WordListItem wordItem in _words.Values)
            {
                if (wordItem.Fcs <= fcs)
                {
                    wordList.Words.Add(wordItem.Grapheme, wordItem);
                }
            }

            return wordList;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Sort word list by frequency.
        /// </summary>
        private void Sort()
        {
            Dictionary<string, WordListItem> sortedWords = new Dictionary<string, WordListItem>();
            foreach (KeyValuePair<string, WordListItem> pair in _words.SortBy(e => e.Value.Frequency).Reverse())
            {
                sortedWords.Add(pair.Key, pair.Value);
            }

            _words.Clear();
            _words = sortedWords;
        }

        /// <summary>
        /// Set fcs in all words.
        /// </summary>
        private void ResetFcs()
        {
            long totalFrequency = _words.Values.Sum(e => e.Frequency);
            if (totalFrequency != 0)
            {
                long accumulatedFreqeuncy = 0;
                foreach (WordListItem word in _words.Values)
                {
                    accumulatedFreqeuncy += word.Frequency;
                    word.Fcs = (double)accumulatedFreqeuncy / totalFrequency;
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// WordListItem class
    /// It represents a line in wordlist.
    /// </summary>
    public class WordListItem
    {
        #region Fields

        private string _grapheme = string.Empty;
        private Collection<string> _examples = new Collection<string>();

        // Used for script to word list conversion, in order to keep example for different case.
        // 1st string is case sensitive word.
        // 2nd string is example
        // e.g.
        // US, He is in a trip to US.
        // us, Will you go with us?
        private Dictionary<string, string> _exampleDictionary = null;

        private Collection<TtsPronSource> _pronSources = new Collection<TtsPronSource>();
        private Dictionary<string, int> _pronunciations = new Dictionary<string, int>();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WordListItem"/> class.
        /// </summary>
        /// <param name="grapheme">Grapheme.</param>
        public WordListItem(string grapheme)
            : this(grapheme, 1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WordListItem"/> class.
        /// </summary>
        /// <param name="grapheme">Grapheme.</param>
        /// <param name="frequency">Frequency.</param>
        public WordListItem(string grapheme, long frequency)
        {
            Helper.ThrowIfNull(grapheme);
            Grapheme = grapheme;
            Frequency = frequency;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Word frequency.
        /// </summary>
        public long Frequency { get; set; }

        /// <summary>
        /// Gets or sets Accumulated corpus coverage.
        /// </summary>
        public double Fcs { get; set; }

        /// <summary>
        /// Gets or sets Word grapheme
        /// It's case insenstive. Always in lower case.
        /// </summary>
        public string Grapheme
        {
            get
            {
                return _grapheme;
            }

            set
            {
                _grapheme = value.ToLower();
            }
        }

        /// <summary>
        /// Gets Exmaples extracted from script.
        /// </summary>
        public ICollection<string> Examples
        {
            get
            {
                if (_exampleDictionary == null)
                {
                    return _examples;
                }
                else
                {
                    return _exampleDictionary.Values;
                }
            }
        }

        /// <summary>
        /// Gets or sets Comment including Frequency, FCS, Examples, Pronunciations in one line.
        /// When this property is set, save function will only save Grapheme + Comment.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Gets Pronunciation sources.
        /// </summary>
        public Collection<TtsPronSource> PronSources
        {
            get { return _pronSources; }
        }

        /// <summary>
        /// Gets All pronunciations and corresponding frequency for this word.
        /// </summary>
        public Dictionary<string, int> Pronunciations
        {
            get { return _pronunciations; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Parse one line in word list file.
        /// </summary>
        /// <param name="line">Line in word list file.</param>
        /// <returns>WordListItem.</returns>
        public static WordListItem ParseLine(string line)
        {
            WordListItem wordListItem = null;
            string[] lineInfo = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (lineInfo.Length >= 2)
            {
                string word = lineInfo[0].Trim();
                string comment = string.Join("\t", lineInfo, 1, lineInfo.Length - 1).Trim();

                if (!string.IsNullOrEmpty(word) && !string.IsNullOrEmpty(comment))
                {
                    wordListItem = new WordListItem(word);
                    wordListItem.Comment = comment;

                    // get examples
                    string exampleString = GetSubstring(line, "<Examples>: ", "\t<Prons>|$");

                    if (!string.IsNullOrEmpty(exampleString))
                    {
                        string[] examples = exampleString.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string example in examples)
                        {
                            wordListItem._examples.Add(example);
                        }
                    }

                    // get prons
                    string pronunciationString = GetSubstring(line, "<Prons>: ", "$");

                    if (!string.IsNullOrEmpty(pronunciationString))
                    {
                        string[] pronunciations = pronunciationString.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string pronunciation in pronunciations)
                        {
                            if (pronunciation.Length > 2 &&
                                pronunciation.StartsWith("/") &&
                                pronunciation.EndsWith("/"))
                            {
                                string pron = pronunciation.Substring(1, pronunciation.Length - 2);
                                wordListItem.Pronunciations.Add(pron);
                            }
                        }
                    }

                    // get fcs and frequence
                    int frequency = 0;
                    if (lineInfo.Length > 1 &&
                        int.TryParse(lineInfo[1], out frequency))
                    {
                        wordListItem.Frequency = frequency;
                    }

                    double fcs = 0.0;
                    if (lineInfo.Length > 2 &&
                        double.TryParse(lineInfo[2], out fcs))
                    {
                        wordListItem.Fcs = fcs;
                    }
                }
            }
            else if (lineInfo.Length == 1)
            {
                string word = lineInfo[0].Trim();
                wordListItem = new WordListItem(word);
                wordListItem.Frequency = -1;
                wordListItem.Fcs = -1.0;
            }

            return wordListItem;
        }

        /// <summary>
        /// Check whether grapheme is valid.
        /// For example:
        /// We needn't put pure digits or pure symbols such as ",", "123" into word list.
        /// Word with number (such as Win7) is also invaid, because it should be handled by TN pattern. 
        /// Word with symbol (such as it's ad-Hoc) is valid. 
        /// </summary>
        /// <param name="grapheme">Grapheme.</param>
        /// <returns>Whether valid.</returns>
        public static bool IsValidGrapheme(string grapheme)
        {
            bool valid = false;
            if (Helper.ContainsLetter(grapheme) && !Helper.ContainsNumber(grapheme))
            {
                valid = true;
            }

            return valid;
        }

        /// <summary>
        /// Return a line string in word list.
        /// </summary>
        /// <returns>String.</returns>
        public override string ToString()
        {
            StringBuilder wordListLine = new StringBuilder();

            wordListLine.AppendFormat("{0}\t{1}\t{2}\t", Grapheme, Frequency, Fcs);

            string resultExamples = string.Empty;

            if (Examples.Count > 0)
            {
                wordListLine.Append("<Examples>: ");
                foreach (string example in Examples)
                {
                    wordListLine.Append(example);
                    wordListLine.Append('\t');
                }
            }

            if (Pronunciations.Count > 0)
            {
                wordListLine.Append("<Prons>: ");
                foreach (string pron in Pronunciations.Keys)
                {
                    wordListLine.AppendFormat("/{0}/\t", pron);
                }
            }

            return wordListLine.ToString().Trim();
        }

        /// <summary>
        /// Return word list item description in word list.
        /// </summary>
        /// <returns>String.</returns>
        public string GetDescription()
        {
            StringBuilder description = new StringBuilder();

            if (Frequency > 0)
            {
                description.AppendFormat("Frequency:{0}", Frequency);
            }

            if (Fcs > 0)
            {
                description.AppendFormat("\r\nFCS:{0}", Fcs);
            }

            string resultExamples = string.Empty;

            if (Examples.Count > 0)
            {
                description.Append("\r\n<Examples>: ");
                foreach (string example in Examples)
                {
                    description.Append("\r\n");
                    description.Append(example);
                }
            }

            if (Pronunciations.Count > 0)
            {
                description.Append("\r\n<Prons>: ");
                foreach (string pron in Pronunciations.Keys)
                {
                    description.AppendFormat("/{0}/\t", pron);
                }
            }

            return description.ToString().Trim();
        }

        /// <summary>
        /// Whether this word is an OOV word.
        /// </summary>
        /// <returns>If the word is oov.</returns>
        public bool IsOovWord()
        {
            bool isOov = false;

            if (PronSources.Contains(TtsPronSource.LTS) ||
                PronSources.Contains(TtsPronSource.Spelling) ||
                PronSources.Contains(TtsPronSource.Compound) ||
                PronSources.Contains(TtsPronSource.ExtraLanguage) ||
                PronSources.Contains(TtsPronSource.OovLochandler) ||
                PronSources.Contains(TtsPronSource.ForeignName) ||
                PronSources.Contains(TtsPronSource.Other))
            {
                isOov = true;
            }

            return isOov;
        }

        /// <summary>
        /// Whether this word is an ambiguous word,
        /// Including more than one pronunciation whose stress and syllable are removed.
        /// </summary>
        /// <returns>If the word is an ambiguous word.</returns>
        public bool IsAmbiguousWord()
        {
            bool isAmbiguous = false;
            if (Pronunciations.Count > 1)
            {
                string firstPron = null;
                foreach (string pron in Pronunciations.Keys)
                {
                    string fixedPron = Pronunciation.RemoveStress(pron);
                    fixedPron = Pronunciation.RemoveSyllable(fixedPron);
                    if (firstPron == null)
                    {
                        firstPron = fixedPron;
                    }
                    else if (!firstPron.Equals(fixedPron, StringComparison.OrdinalIgnoreCase))
                    {
                        isAmbiguous = true;
                        break;
                    }
                }
            }

            return isAmbiguous;
        }

        /// <summary>
        /// Add example.
        /// </summary>
        /// <param name="word">Key word, case sensitvie.</param>
        /// <param name="example">Example.</param>
        public void AddExample(string word, string example)
        {
            if (_examples.Count > 0)
            {
                throw new InvalidOperationException("Shouldn't Call AddExample when there's example in _examples.");
            }

            if (_exampleDictionary == null)
            {
                _exampleDictionary = new Dictionary<string, string>();
            }

            if (!_exampleDictionary.ContainsKey(word))
            {
                _exampleDictionary.Add(word, example);
            }
        }

        /// <summary>
        /// Import examples from another word list item.
        /// </summary>
        /// <param name="otherItem">Another word list item.</param>
        public void ImportExamples(WordListItem otherItem)
        {
            if (_exampleDictionary != null)
            {
                throw new InvalidOperationException("Shouldn't call ImportExamples when there's example in _exampleDictionary.");
            }

            foreach (string example in otherItem.Examples)
            {
                _examples.Add(example);
            }
        }

        /// <summary>
        /// Extract middle string between start and end.
        /// </summary>
        /// <param name="source">Source sentence.</param>
        /// <param name="start">Start.</param>
        /// <param name="end">End.</param>
        /// <returns>Extract string.</returns>
        private static string GetSubstring(string source, string start, string end)
        {
            Regex reg = new Regex(
                "(?<=(" + start + "))[.\\s\\S]*?(?=(" + end + "))",
                RegexOptions.Multiline | RegexOptions.Singleline);

            return reg.Match(source).Value;
        }

        #endregion
    }
}