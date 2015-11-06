//----------------------------------------------------------------------------
// <copyright file="LexiconProcessor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     Implementation LexiconProcessor.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.LexiconProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.FlowEngine;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// LexiconProcessorMode.
    /// </summary>
    [Flags]
    public enum LexiconProcessorMode
    {
        /// <summary>
        /// Merge two TTS xml Lexicon.
        /// </summary>
        Merge,

        /// <summary>
        /// Extract domain lexicon from reviewed script with pronunciation.
        /// </summary>
        ExtractDomainLexicon,

        /// <summary>
        /// Extract a sub lexicon from main lexicon by the words in a corpus file.
        /// </summary>
        ExtractSubLexicon,

        /// <summary>
        /// Extract the word List from Lexicon.
        /// </summary>
        ExtractWordListFromLexicon,
    }

    /// <summary>
    /// The merge mode for lexicon merging when facing the same word.
    /// </summary>
    public enum MergeMode
    {
        /// <summary>
        /// Keep all pronunciation .
        /// </summary>
        KeepAll,

        /// <summary>
        /// Keep the the pronunciation in the last lexicon.
        /// </summary>
        KeepLastOne,

        /// <summary>
        /// Keep the the pronunciation in the first lexicon.
        /// </summary>
        KeepFirstOne,
    }

    /// <summary>
    /// Lexicon Processor.
    /// </summary>
    public class LexiconProcessor : FlowHandler
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="LexiconProcessor"/> class.
        /// </summary>
        /// <param name="name">Name.</param>
        public LexiconProcessor(string name)
            : base(name)
        {
            Description = "Lexicon Processor";
        }

        #endregion

        #region Inputs and Outputs
        /// <summary>
        /// Gets or sets Mode of Lexicon Processor.
        /// </summary>
        public LexiconProcessorMode InMode { get; set; }

        /// <summary>
        /// Gets or sets Main Lexicon.
        /// </summary>
        public Lexicon InMainLexicon { get; set; }

        /// <summary>
        /// Gets or sets Additional Lexicon.
        /// </summary>
        public Lexicon InAdditionalLexicon { get; set; }

        /// <summary>
        /// Gets or sets Merge Mode.
        /// </summary>
        public MergeMode InMergeMode { get; set; }

        /// <summary>
        /// Gets or sets Phone Set.
        /// </summary>
        public TtsPhoneSet InPhoneSet { get; set; }

        /// <summary>
        /// Gets or sets Attribute Schema.
        /// </summary>
        public LexicalAttributeSchema InAttribSchema { get; set; }

        /// <summary>
        /// Gets or sets Script Folder.
        /// </summary>
        public string InScriptFolder { get; set; }

        /// <summary>
        /// Gets or sets Domain List File.
        /// </summary>
        public string InDomainListFile { get; set; }

        /// <summary>
        /// Gets or sets Default Part of Speech.
        /// </summary>
        public string InPos { get; set; }

        /// <summary>
        /// Gets or sets Corpus Type.
        /// </summary>
        public string InCorpusType { get; set; }

        /// <summary>
        /// Gets or sets Corpus File.
        /// </summary>
        public string InCorpusFile { get; set; }

        /// <summary>
        /// Gets or sets the out lexicon path.
        /// </summary>
        public string InSetOutLexiconPath { get; set; }

        /// <summary>
        /// Gets or sets Output Lexicon.
        /// </summary>
        public Lexicon OutLexicon { get; set; }

        /// <summary>
        /// Gets or sets Out Word List.
        /// </summary>
        public IList<string> OutWordList { get; set; }

        #endregion

        #region Abstract Methods
        /// <summary>
        /// Validate Arguments.
        /// </summary>
        protected override void ValidateArguments()
        {
            if (this.InMode != LexiconProcessorMode.ExtractDomainLexicon)
            {
                EnsureNotNull(this.InMainLexicon, "In Main Lexicon should be valid");
            }

            if (this.InMode == LexiconProcessorMode.Merge)
            {
                ////EnsureNotNull(this.InAdditionalLexicon, "Additional Lexicon should be valid");
            }
            else if (this.InMode == LexiconProcessorMode.ExtractDomainLexicon)
            {
                if (!File.Exists(this.InDomainListFile))
                {
                    throw new FileNotFoundException(this.InDomainListFile);
                }

                if (!Directory.Exists(this.InScriptFolder))
                {
                    throw new DirectoryNotFoundException(this.InScriptFolder);
                }

                if (string.IsNullOrEmpty(this.InPos))
                {
                    throw new ArgumentNullException(this.InPos);
                }
            }
            else if (this.InMode == LexiconProcessorMode.ExtractSubLexicon)
            {
                if (string.IsNullOrEmpty(this.InCorpusType))
                {
                    throw new ArgumentNullException(this.InCorpusType);
                }

                if (string.Compare(InCorpusType, "TNML", true) != 0 &&
                    string.Compare(InCorpusType, "CHARTABLE", true) != 0 &&
                    string.Compare(InCorpusType, "WORDLIST", true) != 0)
                {
                    throw new InvalidDataException("Unsupported corpus type");
                }

                if (!File.Exists(this.InCorpusFile))
                {
                    throw new FileNotFoundException(this.InCorpusFile);
                }
            }
            else if (this.InMode == LexiconProcessorMode.ExtractWordListFromLexicon)
            {
                // nothing to check except main lexicon
            }
            else
            {
                throw new NotSupportedException(
                    Helper.NeutralFormat("The mode of \"{0}\" is not supported", this.InMode.ToString()));
            }
        }

        /// <summary>
        /// Execute.
        /// </summary>
        protected override void Execute()
        {
            if (this.InMode == LexiconProcessorMode.Merge)
            {
                Log("Merge Lexicon");
                OutLexicon = new Lexicon();
                OutLexicon.Language = InMainLexicon.Language;
                OutLexicon.Encoding = InMainLexicon.Encoding;
                foreach (KeyValuePair<string, LexicalItem> item in InMainLexicon.Items)
                {
                    OutLexicon.Items.Add(item.Key, item.Value);
                }

                if (InAdditionalLexicon != null)
                {
                    MergeLexicon(OutLexicon, InAdditionalLexicon, InMergeMode);
                }
            }
            else if (this.InMode == LexiconProcessorMode.ExtractDomainLexicon)
            {
                Log("Extract Domain Lexicon");
                OutLexicon = ExtractDomainLexicon(InScriptFolder, InDomainListFile,
                    InMainLexicon, InPos, InMergeMode,
                    InPhoneSet, InAttribSchema);
            }
            else if (this.InMode == LexiconProcessorMode.ExtractSubLexicon)
            {
                Log("Extract Sub Lexicon");
                OutLexicon = ExtractSubLexicon(InCorpusType, InCorpusFile, InMainLexicon);
            }
            else if (this.InMode == LexiconProcessorMode.ExtractWordListFromLexicon)
            {
                Log("Extract Word List from Lexicon");
                OutWordList = InMainLexicon.ListWords();
            }

            if (!string.IsNullOrEmpty(InSetOutLexiconPath) && this.InMode != LexiconProcessorMode.ExtractWordListFromLexicon
                && OutLexicon != null)
            {
                OutLexicon.Save(GetOutPathUnderResultDirectory(InSetOutLexiconPath));
            }
        }

        /// <summary>
        /// Validate Results.
        /// </summary>
        /// <param name="enable">Indicator to whether flow is enabled.</param>
        protected override void ValidateResults(bool enable)
        {
            if (InMode == LexiconProcessorMode.ExtractWordListFromLexicon)
            {
                if (OutWordList == null)
                {
                    throw new InvalidDataException("Lexicon Processing of Extracting word list failed");
                }
            }
            else
            {
                if (InMode == LexiconProcessorMode.Merge && InAdditionalLexicon == null)
                {
                    OutLexicon = InMainLexicon;
                }

                if (enable && OutLexicon == null)
                {
                    throw new InvalidDataException("Lexicon Processing failed");
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Extract Domain Lexicon from script.
        /// </summary>
        /// <param name="scriptFolder">Script Folder.</param>
        /// <param name="domainListFile">Domain List File.</param>
        /// <param name="inMainLex">Input Main Lexicon.</param>
        /// <param name="defaultPartOfSpeech">Default Part of Speech.</param>
        /// <param name="mergeMode">Merging Mode for Lexicon.</param>
        /// <param name="phoneSet">Phone set.</param>
        /// <param name="attribSchema">Lexical attribute schema.</param>
        /// <returns>Lexicon.</returns>
        private Lexicon ExtractDomainLexicon(string scriptFolder, string domainListFile,
            Lexicon inMainLex, string defaultPartOfSpeech, MergeMode mergeMode,
            TtsPhoneSet phoneSet, LexicalAttributeSchema attribSchema)
        {
            if (attribSchema != null)
            {
                if (PosItem.Validate(defaultPartOfSpeech, null, attribSchema).Count > 0)
                {
                    Log("Default Part of speech {0} is unrecognized according to attribute schema, extraction breaks",
                        defaultPartOfSpeech);
                    return null;
                }
            }

            Lexicon outLex = null;
            foreach (string domainName in Helper.FileLines(domainListFile))
            {
                string domainFilePath = Path.Combine(scriptFolder, domainName);
                XmlScriptFile scriptFile = new XmlScriptFile();
                scriptFile.Load(domainFilePath);
                if (outLex != null && outLex.Language != scriptFile.Language)
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Found inconsistent language \"{0}\" against previous one \"{1}\" in the file of \"{2}\"",
                        scriptFile.Language.ToString(),
                        outLex.Language.ToString(), domainFilePath));
                }

                Lexicon lexicon = Lexicon.CreateFromXmlScriptFile(scriptFile, defaultPartOfSpeech, inMainLex);
                if (phoneSet != null && attribSchema != null)
                {
                    lexicon.Validate(phoneSet, attribSchema);
                    if (lexicon.ErrorSet.Count > 0)
                    {
                        Console.Error.WriteLine("The script file {0} contains {1} errors, skip!",
                            domainFilePath, lexicon.ErrorSet.Count);
                        Log("The script file {0} contains {1} errors:",
                            domainFilePath, lexicon.ErrorSet.Count);
                        foreach (Error error in lexicon.ErrorSet.Errors)
                        {
                            Log(error.ToString());
                        }

                        // Skip this domain lexicon
                        continue;
                    }
                }

                if (outLex == null)
                {
                    outLex = lexicon;
                }
                else
                {
                    MergeLexicon(outLex, lexicon, mergeMode);
                }
            }

            if (outLex.Items.Count == 0)
            {
                Log("The final lexicon is empty.");
            }
            
            return outLex;
        }

        /// <summary>
        /// Merge lexicons.
        /// </summary>
        /// <param name="mergedLexicon">Lexicon to be merged to.</param>
        /// <param name="subLexicon">Lexicon to be merged.</param>
        /// <param name="mergeMode">MergeMode.</param>
        private void MergeLexicon(Lexicon mergedLexicon, Lexicon subLexicon, MergeMode mergeMode)
        {
            switch (mergeMode)
            {
                case MergeMode.KeepAll:
                    MergeLexiconWithKeepAll(mergedLexicon, subLexicon);
                    break;
                case MergeMode.KeepLastOne:
                    MergeLexiconWithKeepLastOne(mergedLexicon, subLexicon);
                    break;
                case MergeMode.KeepFirstOne:
                    MergeLexiconWithKeepFirstOne(mergedLexicon, subLexicon);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Merge lexicon, when facing conflict word, keep all attributes.
        /// </summary>
        /// <param name="mergedLexicon">Main lexicon and merged lexicon.</param>
        /// <param name="subLexicon">Sub lexicon.</param>
        private void MergeLexiconWithKeepAll(Lexicon mergedLexicon, Lexicon subLexicon)
        {
            Collection<string> addedPronMessage = new Collection<string>();
            Collection<string> addedPropertyMessage = new Collection<string>();
            Collection<string> duplicateWordMessage = new Collection<string>();
            int addedWord = 0;

            foreach (KeyValuePair<string, LexicalItem> subLexiconItem in subLexicon.Items)
            {
                string word = subLexiconItem.Key;
                if (!mergedLexicon.Items.ContainsKey(word))
                {
                    mergedLexicon.Items.Add(subLexiconItem.Key, subLexiconItem.Value);
                    addedWord++;
                    continue;
                }

                LexicalItem mergedItem = mergedLexicon.Items[word];
                bool theSameWord = true;
                foreach (LexiconPronunciation subItemPron in subLexiconItem.Value.Pronunciations)
                {
                    LexiconPronunciation mergedItemPron = null;

                    // Find subLexiconItem's pronunciation in the merged item.
                    foreach (LexiconPronunciation itemPron in mergedItem.Pronunciations)
                    {
                        if (itemPron.Symbolic.Equals(subItemPron.Symbolic, StringComparison.OrdinalIgnoreCase))
                        {
                            mergedItemPron = itemPron;
                            break;
                        }
                    }

                    // If the pronunciation doesn't exist in merged item, then add it.
                    if (mergedItemPron == null)
                    {
                        mergedItem.Pronunciations.Add(subItemPron);
                        theSameWord = false;
                        addedPronMessage.Add(Helper.NeutralFormat(
                            "Pronunciation [{0}] has been added to word [{1}]",
                            subItemPron.Symbolic, mergedItem.Grapheme));
                    }
                    else
                    {
                        foreach (LexiconItemProperty subItemProperty in subItemPron.Properties)
                        {
                            bool hasProperty = false;

                            // Find subLexiconItemPron's property in the merged item.
                            foreach (LexiconItemProperty itemProperty in mergedItemPron.Properties)
                            {
                                if (itemProperty.Equals(subItemProperty))
                                {
                                    hasProperty = true;
                                    break;
                                }
                            }

                            // Add the property if doesn't contains it.
                            if (!hasProperty)
                            {
                                mergedItemPron.Properties.Add(subItemProperty);
                                theSameWord = false;
                                addedPropertyMessage.Add(Helper.NeutralFormat(
                                    "Property has been added to word [{0}]'s pronunciation [{1}] : [{2}]",
                                    mergedItem.Grapheme, subItemPron.Symbolic, subItemProperty.ToString()));
                            }
                        }
                    }
                }

                if (theSameWord)
                {
                    duplicateWordMessage.Add(Helper.NeutralFormat(
                        "Word [{0}] has been dropped because of duplication.", subLexiconItem.Key));
                }
            }

            // Log the message
            {
                Log("---------------------------------");
                Log("Totally:");
                Log("[{0}] words have been dropped because of duplication.", duplicateWordMessage.Count);
                Log("[{0}] pronunciations and [{1}] properties have been added.",
                    addedPronMessage.Count, addedPropertyMessage.Count);
            }
        }

        /// <summary>
        /// Merge lexicon, when facing conflict word, use the attributes in the last lexicon.
        /// </summary>
        /// <param name="mergedLexicon">Main lexicon and merged lexicon.</param>
        /// <param name="subLexicon">Sub lexicon.</param>
        private void MergeLexiconWithKeepLastOne(Lexicon mergedLexicon, Lexicon subLexicon)
        {
            Collection<string> replacedPronMessage = new Collection<string>();
            Collection<string> replacedPropertyMessage = new Collection<string>();
            Collection<string> existedWords = new Collection<string>();
            Collection<string> existedWordsInLower = new Collection<string>();

            // Dump the conflict pronunciations or properties from the merged lexicon
            foreach (KeyValuePair<string, LexicalItem> mergedLexiconItem in mergedLexicon.Items)
            {
                string word = mergedLexiconItem.Key;
                LexicalItem newItem = subLexicon.Lookup(word, true);

                // If the sub lexicon contain the same grapheme, then delete the one in original lexicon.
                if (newItem != null)
                {
                    existedWords.Add(word);
                    existedWordsInLower.Add(word.ToLowerInvariant());
                    foreach (LexiconPronunciation originalPron in mergedLexiconItem.Value.Pronunciations)
                    {
                        LexiconPronunciation existedPron = newItem.FindPronunciation(originalPron.Symbolic);
                        if (existedPron == null)
                        {
                            replacedPronMessage.Add(Helper.NeutralFormat(
                                "Pronunciation for word [{0}] has been removed: [{1}]",
                                mergedLexiconItem.Key, originalPron.Symbolic));
                        }
                        else
                        {
                            foreach (LexiconItemProperty subItemProperty in originalPron.Properties)
                            {
                                bool hasProperty = false;

                                // Find old properties in new(sub) item.
                                foreach (LexiconItemProperty itemProperty in existedPron.Properties)
                                {
                                    if (itemProperty.Equals(subItemProperty))
                                    {
                                        hasProperty = true;
                                        break;
                                    }
                                }

                                // Add the property if doesn't contains it.
                                if (!hasProperty)
                                {
                                    replacedPropertyMessage.Add(Helper.NeutralFormat(
                                        "Property has been replaced for word [{0}]'s pronunciation [{1}] : [{2}]",
                                        word, originalPron.Symbolic, subItemProperty.ToString()));
                                }
                            }
                        }
                    }
                }
            }

            // Remove the duplicate word entries
            foreach (string word in existedWords)
            {
                mergedLexicon.Items.Remove(word);
            }

            // Add new word entries into merged lexicon.
            int newWord = 0;
            foreach (KeyValuePair<string, LexicalItem> subLexiconItem in subLexicon.Items)
            {
                mergedLexicon.Items.Add(subLexiconItem.Key, subLexiconItem.Value);
                if (!existedWordsInLower.Contains(subLexiconItem.Key.ToLowerInvariant()))
                {
                    newWord++;
                }
            }

            // Log the Message
            {
                Log("---------------------------------");
                Log("Totally:");
                Log(Helper.NeutralFormat("[{0}] words have been replaced by the latter lexicon", replacedPronMessage.Count));
                Log("[{0}] properties have been replaced.", replacedPropertyMessage.Count);
                Log(Helper.NeutralFormat("[{0}] new words have been added by the latter lexicon", newWord));
            }
        }

        /// <summary>
        /// Merge lexicon. when facing conflict word, use the attributes in the first lexicon.
        /// </summary>
        /// <param name="mergedLexicon">Main lexicon and merged lexicon.</param>
        /// <param name="subLexicon">Sub lexicon.</param>
        private void MergeLexiconWithKeepFirstOne(Lexicon mergedLexicon, Lexicon subLexicon)
        {
            Collection<string> skippedPronMessage = new Collection<string>();
            int addedWord = 0;

            foreach (KeyValuePair<string, LexicalItem> subLexiconItem in subLexicon.Items)
            {
                string word = subLexiconItem.Key;

                // If the sub lexicon item doesn't exist in merged lexicon, then add it.
                LexicalItem originalItem = mergedLexicon.Lookup(word, true);
                if (originalItem == null)
                {
                    mergedLexicon.Items.Add(subLexiconItem.Key, subLexiconItem.Value);
                    addedWord++;
                    continue;
                }

                foreach (LexiconPronunciation newPron in subLexiconItem.Value.Pronunciations)
                {
                    if (!originalItem.ContainsPronunciation(newPron.Symbolic))
                    {
                        skippedPronMessage.Add(Helper.NeutralFormat(
                            "Pronunciation for word [{0}] has been skipped: [{1}]",
                            subLexiconItem.Key, newPron.Symbolic));
                    }
                }
            }

            // Log the message
            {
                Log("---------------------------------");
                Log("Totally:");
                Log("[{0}] pronunciations have been skipped.",
                    skippedPronMessage.Count);
                Log(Helper.NeutralFormat("[{0}] new words have been added by the latter lexicon", addedWord));
            }
        }

        /// <summary>
        /// ExtractSubLexicon.
        /// </summary>
        /// <param name="corpusType">Corpus type.</param>
        /// <param name="corpusFile">Corpus file.</param>
        /// <param name="mainLexicon">Main lexicon.</param>
        /// <returns>Lexicon.</returns>
        private Lexicon ExtractSubLexicon(string corpusType, string corpusFile,
            Lexicon mainLexicon)
        {
            List<string> words = null;
            if (string.Compare(corpusType, "WORDLIST", true) == 0)
            {
                words = ExtractWordsFromWordList(corpusFile);
            }
            else
            {
                throw new InvalidDataException("Unsupported corpus type");
            }

            List<string> missedLexWords = new List<string>();
            Lexicon newLex = mainLexicon.ExtractSubLexicon(words, missedLexWords);
            foreach (string word in missedLexWords)
            {
                if (word.IndexOf("[break=") == -1)
                {
                    string logWord = word.Replace("{", "{{");
                    logWord = logWord.Replace("}", "}}");
                    Log("[" + logWord + "] not in main lexicon!");
                }
            }

            return newLex;
        }

        /// <summary>
        /// Extract words from word list.
        /// </summary>
        /// <param name="wordListFile">Word list file.</param>
        /// <returns>Word list.</returns>
        private List<string> ExtractWordsFromWordList(string wordListFile)
        {
            List<string> words = new List<string>();
            using (StreamReader sr = new StreamReader(wordListFile))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    line = line.Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    string[] items = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (items.Length == 1 || items.Length == 2)
                    {
                        if (!words.Contains(items[0]))
                        {
                            words.Add(items[0]);
                        }
                    }
                    else
                    {
                        Log("wrong line:{0}", line);
                    }
                }
            }

            return words;
        }

        #endregion
    }
}