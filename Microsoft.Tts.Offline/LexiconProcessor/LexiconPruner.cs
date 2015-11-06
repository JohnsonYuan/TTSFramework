//----------------------------------------------------------------------------
// <copyright file="LexiconPruner.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     Implementation lexicon pruning process
//      1. LTS based pruning
//      2. Freq based pruning
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.LexiconProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Security.Permissions;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Compiler;
    using Microsoft.Tts.Offline.Compiler.LanguageData;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.FlowEngine;
    using Microsoft.Tts.Offline.Utility;
    using SP = Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// Lexicon prune mode.
    /// </summary>
    [Flags]
    public enum LexiconPruneMode
    {
        /// <summary>
        /// Prune lexicon based on LTS rule.
        /// </summary>
        LTS,

        /// <summary>
        /// Prune lexicon based on word frequency.
        /// </summary>
        Freq,

        /// <summary>
        /// Pruning Correct.
        /// </summary>
        PruningCorrect,
    }

    /// <summary>
    /// Pronunciation comparison mode.
    /// </summary>
    public enum PronComparisonMode
    {
        /// <summary>
        /// Compare stress and phone.
        /// </summary>
        StressPhone = 0,

        /// <summary>
        /// Only compare phone.
        /// </summary>
        OnlyPhone = 1,
    }

    /// <summary>
    /// LexiconPruner.
    /// </summary>
    public class LexiconPruner : FlowHandler
    {
        #region Fields
        private Lexicon _outLexicon = null;
        private Lexicon _outLtsRemovedLexicon = null;
        private string _outEngineData;

        // cut off frequency for each domain
        private List<CutoffWordlist> _cutoffWordlist = new List<CutoffWordlist>();

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="LexiconPruner"/> class.
        /// </summary>
        /// <param name="name">Name.</param>
        public LexiconPruner(string name)
            : base(name)
        {
            Description = "Lexicon Pruning";
        }
        #endregion

        #region Inputs and Outputs
        /// <summary>
        /// Gets or sets Mode of Lexicon Pruner.
        /// </summary>
        public LexiconPruneMode InMode { get; set; }

        /// <summary>
        /// Gets or sets Locale Handler Dir.
        /// </summary>
        public string InLocaleHandlerDir { get; set; }

        /// <summary>
        /// Gets or sets Input Lexicon.
        /// </summary>
        public Lexicon InLexicon { get; set; }

        /// <summary>
        /// Gets or sets Input Removed Lexicon for pruning correct.
        /// </summary>
        public Lexicon InRemovedLexicon { get; set; }

        /// <summary>
        /// Gets or sets Exception word file.
        /// </summary>
        public string InExceptionWordFile { get; set; }

        /// <summary>
        /// Gets or sets Language Data File for Lts Mode.
        /// </summary>
        public string InLangDataFile { get; set; }

        /// <summary>
        /// Gets or sets Exception word list, when it is set, InEceptionWordFile will be disable.
        /// </summary>
        public IDictionary<string, int> InExceptionWordList { get; set; }

        /// <summary>
        /// Gets or sets The object of Compiler.
        /// </summary>
        public DataCompiler InCompiler { get; set; }

        /// <summary>
        /// Gets or sets POS filter.
        /// </summary>
        public string InPosFilter { get; set; }

        /// <summary>
        /// Gets or sets Pronunciation Comparison mode.
        /// </summary>
        public PronComparisonMode InPronComparisonMode { get; set; }

        /// <summary>
        /// Gets or sets  the output lexicon path.
        /// </summary>
        public string InSetOutLexiconPath { get; set; }

        /// <summary>
        /// Gets or sets  the output removed lexicon path.
        /// </summary>
        public string InSetOutRemovedLexiconPath { get; set; }

        /// <summary>
        /// Gets or sets Voice Font Path.
        /// </summary>
        public string InVoiceFont { get; set; }

        /// <summary>
        /// Gets or sets Extra LangData.
        /// </summary>
        public string InExtraDAT { get; set; }

        /// <summary>
        /// Gets Output Pruned lexicon.
        /// </summary>
        public Lexicon OutPrunedLexicon
        {
            get { return _outLexicon; }
        }

        /// <summary>
        /// Gets Output Removed Lexicon by LTS Pruning.
        /// </summary>
        public Lexicon OutLtsRemovedLexicon
        {
            get { return _outLtsRemovedLexicon; }
        }

        /// <summary>
        /// Gets Output Engine Data.
        /// </summary>
        public string OutEngineData
        {
            get { return _outEngineData; }
        }

        #endregion

        #region Abstract Methods
        /// <summary>
        /// Validate Arguments.
        /// </summary>
        protected override void ValidateArguments()
        {
            EnsureNotNull(this.InLexicon, "In Lexicon should be valid");
            if (this.InMode == LexiconPruneMode.Freq)
            {
                foreach (CutoffWordlist domainWordList in _cutoffWordlist)
                {
                    string wordlistPath = Directory.GetParent(this.InWorkingDirectory).FullName;
                    wordlistPath = Path.Combine(wordlistPath, domainWordList.Path);

                    if (!File.Exists(wordlistPath))
                    {
                        throw new FileNotFoundException(wordlistPath);
                    }
                }
            }
            else if (this.InMode == LexiconPruneMode.LTS)
            {
                if (string.IsNullOrEmpty(InLangDataFile))
                {
                    EnsureNotNull(this.InCompiler, "Compiler should be valid");
                }

                EnsureNotNull(this.InPosFilter, "Part of Speech should be valid");
            }
            else if (this.InMode == LexiconPruneMode.PruningCorrect)
            {
                EnsureNotNull(this.InRemovedLexicon, "Removed Lexicon should be valid");
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
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        protected override void Execute()
        {
            if (this.InMode == LexiconPruneMode.Freq)
            {
                PruneLexiconWithWordFrequency();
            }
            else if (this.InMode == LexiconPruneMode.LTS)
            {
                Log("Do LTS Pruning");

                _outEngineData = InLangDataFile;

                // if InLangDataFile is set, InCompiler will be disable
                if (string.IsNullOrEmpty(_outEngineData))
                {
                    bool ltsValid = true;

                    // Firstly, check LTS rule in compiler
                    if (!InCompiler.ExistModuleData(ModuleDataName.LtsRule))
                    {
                        ErrorSet errors = InCompiler.Compile(ModuleDataName.LtsRule);
                        if (errors.Contains(ErrorSeverity.MustFix))
                        {
                            ltsValid = false;
                        }
                    }

                    if (ltsValid)
                    {
                        // Firstly, Prune by LTS rule
                        _outEngineData = Path.Combine(this.IntermediateDataDirectory, "MSTTSLocXxXX.dat");
                        if (!CompileEngineData(_outEngineData, InLexicon))
                        {
                            Log("Compile Engine Data fail & Lexicon Pruning fail");
                            _outEngineData = string.Empty;
                        }
                    }

                    if (!ltsValid)
                    {
                        Log("LTS data not valid for Engine data, Pruning by LTS stop.");
                    }
                }

                if (!string.IsNullOrEmpty(_outEngineData) && File.Exists(_outEngineData))
                {
                    // Load Exception word list
                    IDictionary<string, int> exceptionWords = GetExceptionWordList();

                    // Set output lexicon
                    _outLexicon = new Lexicon();
                    _outLexicon.Language = InLexicon.Language;
                    _outLexicon.Encoding = InLexicon.Encoding;

                    // Set removed lexicon
                    _outLtsRemovedLexicon = new Lexicon();
                    _outLtsRemovedLexicon.Language = InLexicon.Language;
                    _outLtsRemovedLexicon.Encoding = InLexicon.Encoding;

                    SP.TtsEngineSetting setting = InitializeEngineSetting(
                        InLexicon.Language, _outEngineData);
                    using (SP.ServiceProvider sp = new SP.ServiceProvider(setting))
                    {
                        PruneByLtsRule(sp, InLexicon, _outLexicon, _outLtsRemovedLexicon, InPosFilter,
                            InPronComparisonMode, exceptionWords);
                    }
                }

                if (!string.IsNullOrEmpty(InSetOutRemovedLexiconPath) && OutLtsRemovedLexicon != null)
                {
                    OutLtsRemovedLexicon.Save(GetOutPathUnderResultDirectory(InSetOutRemovedLexiconPath));
                }
            }
            else if (this.InMode == LexiconPruneMode.PruningCorrect)
            {
                // Secondly, Do pruning correction
                SP.TtsEngineSetting setting = null;
                int backEntriesCount;
                int times = 0;
                _outLexicon = InLexicon;
                _outEngineData = Path.Combine(this.IntermediateDataDirectory, "MSTTSLocXxXX.dat");
                if (!string.IsNullOrEmpty(InExtraDAT))
                {
                    try
                    {
                        // Extra DAT should be in same folder with native DAT
                        string destFileName = Path.Combine(IntermediateDataDirectory, Path.GetFileName(InExtraDAT));
                        File.Copy(InExtraDAT, destFileName, true);
                    }
                    catch
                    {
                        Log("Copy extra DAT file {0} failed!", InExtraDAT);
                        return;
                    }
                }

                do
                {
                    times++;
                    Log("Do pruning correction for {0} times:", times);
                    if (!CompileEngineData(_outEngineData, _outLexicon))
                    {
                        Log("Compile Engine Data fail & Lexicon Pruning fail");
                        break;
                    }
                    else
                    {
                        if (setting == null)
                        {
                            setting = InitializeEngineSetting(
                            InLexicon.Language, _outEngineData);
                        }

                        using (SP.ServiceProvider sp = new SP.ServiceProvider(setting))
                        {
                            backEntriesCount = PruningCorrect(sp, _outLexicon, InRemovedLexicon, InPronComparisonMode);
                        }
                    }
                }
                while (backEntriesCount != 0);

                // Delete extra DAT and domain DAT file copied
                foreach (string file in Directory.GetFiles(IntermediateDataDirectory))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.IsReadOnly)
                    {
                        Helper.ForcedDeleteFile(file);
                    }
                }
            }

            if (!string.IsNullOrEmpty(InSetOutLexiconPath) && _outLexicon != null)
            {
                _outLexicon.Save(GetOutPathUnderResultDirectory(InSetOutLexiconPath));
            }
        }

        /// <summary>
        /// Validate Results.
        /// </summary>
        /// <param name="enable">Indicator to whether flow is enabled.</param>
        protected override void ValidateResults(bool enable)
        {
            if (enable && OutPrunedLexicon == null)
            {
                throw new InvalidDataException("Pruning failed");
            }
        }

        #endregion

        #region Static Private Methods

        /// <summary>
        /// Whether the pronunciation of this lexicon item can be correctly predicted with LTS rule.
        /// </summary>
        /// <param name="item">Lexicon item.</param>
        /// <param name="pronouncer">TTS pronouncer.</param>
        /// <param name="phoneTable">Phone table.</param>
        /// <param name="onlyPhone">Whether only compare phones or not.</param>
        /// <returns>Bool.</returns>
        private static bool IsAvailableToPruneWithLTSRule(LexicalItem item,
            SP.Pronouncer pronouncer, SP.Phoneme phoneTable, bool onlyPhone)
        {
            Debug.Assert(item != null && pronouncer != null && phoneTable != null);

            bool available = false;

            // Pronunciation in the lexicon data
            string pronInLex = item.Pronunciations[0].Symbolic;

            // Pronunciation created from LTS rule
            string pronLTS = GetPronunciationWithLTSRule(item.Grapheme.Trim(),
                pronouncer, phoneTable);

            if (!string.IsNullOrEmpty(pronLTS))
            {
                if (IsEqualPronunciation(pronInLex, pronLTS, onlyPhone))
                {
                    // Drop this item since its pronunciation can be correctly
                    // predicted by LTS rule
                    available = true;
                }
            }

            return available;
        }

        /// <summary>
        /// Compare two pronunciations.
        /// </summary>
        /// <param name="pronFirst">First pronunciation.</param>
        /// <param name="pronSecond">Second pronunciation.</param>
        /// <param name="onlyPhone">Comparison mode (only phones or all symbols.</param>
        /// <returns>Bool.</returns>
        private static bool IsEqualPronunciation(string pronFirst, string pronSecond, bool onlyPhone)
        {
            Debug.Assert(!string.IsNullOrEmpty(pronFirst));
            Debug.Assert(!string.IsNullOrEmpty(pronSecond));

            string first = pronFirst.ToLowerInvariant();
            string second = pronSecond.ToLowerInvariant();

            if (onlyPhone)
            {
                first = Regex.Replace(first, @" [1-9]", string.Empty);
                second = Regex.Replace(second, @" [1-9]", string.Empty);
            }

            first = Regex.Replace(first, @"\s+", " ");
            second = Regex.Replace(second, @"\s+", " ");

            bool isEqual = true;
            if (first != second)
            {
                isEqual = false;
            }

            return isEqual;
        }

        /// <summary>
        /// Get pronunciation with LTS rules.
        /// </summary>
        /// <param name="grapheme">Word grapheme.</param>
        /// <param name="pronouncer">TTS pronouncer.</param>
        /// <param name="phoneTable">Phoneme table.</param>
        /// <returns>Pronunciation.</returns>
        private static string GetPronunciationWithLTSRule(string grapheme,
            SP.Pronouncer pronouncer, SP.Phoneme phoneTable)
        {
            Debug.Assert(!string.IsNullOrEmpty(grapheme));
            Debug.Assert(pronouncer != null);
            Debug.Assert(phoneTable != null);

            string pronLTS = string.Empty;

            SP.TtsPronSource pronSource = SP.TtsPronSource.PS_NONE;
            string predictedPhoneIds = pronouncer.WordPronouncer.GetPronunciation(
                grapheme, SP.TtsPronGenerationType.PG_LTS, SP.TtsDomain.TTS_DOMAIN_GENERAL, ref pronSource);

            if (!string.IsNullOrEmpty(predictedPhoneIds))
            {
                Debug.Assert(pronSource == SP::TtsPronSource.PS_LTS);
                string phoneIds = pronouncer.Syllabifier.Syllabify(predictedPhoneIds);
                pronLTS = phoneTable.PhoneIdsToPronunciation(phoneIds);
            }

            return pronLTS;
        }

        /// <summary>
        /// End to end check the OOV word by the tts engine front end.
        /// If it will have a different pronunciation according to the removed lexicon pronunciation.
        /// </summary>
        /// <param name="item">Lexicon item.</param>
        /// <param name="engine">TTS engine.</param>
        /// <param name="onlyPhone">Whether only compare phones or not.</param>
        /// <returns>Bool.</returns>
        private static bool IsWrongPronunciationByEndToEndFrontEnd(LexicalItem item,
            SP.TtsEngine engine, bool onlyPhone)
        {
            Debug.Assert(item != null && engine != null && engine.Phoneme != null);

            bool wrong = false;

            // Pronunciation in the lexicon data
            string pronInLex = item.Pronunciations[0].Symbolic;

            SP.Phoneme phoneTable = engine.Phoneme;
            using (SP.TtsUtterance utt = new SP.TtsUtterance(), uttUpperCase = new SP.TtsUtterance())
            {
                string word = item.Grapheme.Trim();
                engine.SetSpeakText(word);
                engine.TextProcessor.Reset();
                engine.TextProcessor.Process(utt);

                // Some words capital form will be spelled out if they are pruned like "auto" "oak", check it here
                engine.SetSpeakText(word.ToUpperInvariant());
                engine.TextProcessor.Reset();
                engine.TextProcessor.Process(uttUpperCase);

                // make sure the word grapheme is not changed by front end process
                if (utt.Words.Count == 1 && utt.Words[0].WordText == word)
                {
                    string phoneIds = utt.Words[0].PhoneIds;
                    if (!string.IsNullOrEmpty(phoneIds))
                    {
                        string pronUpperCase = phoneTable.PhoneIdsToPronunciation(uttUpperCase.Words[0].PhoneIds);
                        string pronActual = phoneTable.PhoneIdsToPronunciation(phoneIds);
                        wrong = !Pronunciation.Equals(pronInLex, pronActual, onlyPhone) || !Pronunciation.Equals(pronActual, pronUpperCase, onlyPhone);
                    }
                    else if (!string.IsNullOrEmpty(pronInLex))
                    {
                        wrong = true;
                    }
                }
            }

            return wrong;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Compile engine data.
        /// </summary>
        /// <param name="engineDataPath">Engine data path.</param>
        /// <param name="lexicon">The lexicon.</param>
        /// <returns>If the compile successed.</returns>
        private bool CompileEngineData(string engineDataPath, Lexicon lexicon)
        {
            ErrorSet errors = new ErrorSet();
            Lexicon oriLexicon = InCompiler.GetObject(RawDataName.Lexicon, errors) as Lexicon;
            InCompiler.SetObject(RawDataName.Lexicon, lexicon);
            errors.Merge(InCompiler.Compile(ModuleDataName.Lexicon));
            errors.Merge(InCompiler.CombineDataFile(engineDataPath));

            // Restore the original lexicon
            InCompiler.SetObject(RawDataName.Lexicon, oriLexicon);
            return !errors.Contains(ErrorSeverity.MustFix);
        }

        /// <summary>
        /// Initialize Engine setting.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <param name="engineDataPath">EngineDataPath.</param>
        /// <returns>Engine setting.</returns>
        private SP.TtsEngineSetting InitializeEngineSetting(Language language, string engineDataPath)
        {
            SP.Language langId = (SP.Language)language;
            SP.TtsEngineSetting setting = new SP.TtsEngineSetting(langId);
            setting.PipelineMode = SP.ModulePipelineMode.PM_TEXT_ANALYSIS;
            setting.LangDataPath = engineDataPath;
            if (!string.IsNullOrEmpty(InLocaleHandlerDir))
            {
                setting.LangDllDir = InLocaleHandlerDir;
            }

            // Only PruningCorrect mode use the voice font
            if (InMode == LexiconPruneMode.PruningCorrect && !string.IsNullOrEmpty(InVoiceFont))
            {
                setting.VoicePath = InVoiceFont;
                string fontFolder = Path.GetDirectoryName(InVoiceFont);

                // Some voice font have domain DAT, copy it to make sure initialize engine successfully
                CopyDomainDAT(fontFolder, IntermediateDataDirectory, language);
            }

            return setting;
        }

        /// <summary>
        /// Copy domain data file.
        /// </summary>
        /// <param name="sourceDir">Source directory.</param>
        /// <param name="destDir">Target directory.</param>
        /// <param name="language">Language.</param>
        private void CopyDomainDAT(string sourceDir, string destDir, Language language)
        {
            if (Directory.Exists(sourceDir))
            {
                string domainFilePattern = language.ToString() + ".+" + "\\.dat";
                foreach (string file in Directory.GetFiles(sourceDir))
                {
                    string fileName = Path.GetFileName(file);
                    if (Regex.Match(fileName, domainFilePattern, RegexOptions.IgnoreCase).Success)
                    {
                        try
                        {
                            string sourceFile = Path.Combine(sourceDir, fileName);
                            string destFile = Path.Combine(destDir, fileName);
                            File.Copy(sourceFile, destFile, true);
                        }
                        catch
                        {
                            Log("Copy domain DAT file {0} failed!", file);
                        }
                    }
                }
            }
        }

        #endregion

        #region Prune Lexicon Based on Word Frequency

        /// <summary>
        /// Parse cutoff frequency string.
        /// </summary>
        /// <param name="cutOffFreq">Cutoff frequency string in config.</param>
        private void ParseCutoffFreq(string cutOffFreq)
        {
            // parse xml
            XElement element = XElement.Parse(cutOffFreq);

            foreach (XElement ele in element.Elements("wordlist"))
            {
                string path = string.Empty;
                double fcs = 1.0;

                // get attribute path
                XAttribute pathAttr = ele.Attribute("path");

                if (pathAttr == null)
                {
                    throw new InvalidDataException("Cannot find attribute [path] in config.");
                }

                path = pathAttr.Value;

                // get attribute fcs
                double result = 0.0;

                if (double.TryParse(ele.Value, out result))
                {
                    fcs = double.Parse(ele.Value);
                }

                // add to _cutoffWordlist
                CutoffWordlist cutoffWordlist = new CutoffWordlist();
                cutoffWordlist.Fcs = fcs;
                cutoffWordlist.Path = path;

                _cutoffWordlist.Add(cutoffWordlist);
            }
        }

        /// <summary>
        /// Prune lexicon with word frequency:
        /// We set cut off frequency as cutOffFreq. If the accumulated frequency sum is larger than
        /// Predefined threshold, we stop the pruning process and only high frequency words are kept.
        /// </summary>
        private void PruneLexiconWithWordFrequency()
        {
            // return domain word list with fcs <= user specifed fcs.
            Dictionary<string, int> cutoffWords = CombineWordlistWithFCS(_cutoffWordlist);

            // words in none prune word list shouldn't be removed during frequency pruning.
            IDictionary<string, int> exceptionWords = GetExceptionWordList();
            foreach (string word in exceptionWords.Keys)
            {
                if (!cutoffWords.ContainsKey(word))
                {
                    cutoffWords.Add(word);
                }
            }

            // for each word in lexicon, if it is in any domain words, keep it in lexicon, otherwise, remove from lexicon.
            Debug.Assert(cutoffWords != null);

            if (cutoffWords.Count > 0)
            {
                // Set output lexicon
                _outLexicon = new Lexicon();
                _outLexicon.Language = this.InLexicon.Language;
                _outLexicon.Encoding = InLexicon.Encoding;

                // Extracting words
                Log("Extracting...");
                foreach (LexicalItem item in InLexicon.Items.Values)
                {
                    // Because lexicon item could be case sensitive. However, the words in word frequency list is in lower case.
                    string word = item.Grapheme.ToLowerInvariant();

                    // if word is in domain wordlist, then add to output lexicon.
                    if (cutoffWords.ContainsKey(word))
                    {
                        _outLexicon.Items.Add(item.Grapheme, item);
                    }
                }

                Log("Finished pruning lexicon with word frequency: {0} words are remained!", _outLexicon.Items.Count);
            }
        }

        /// <summary>
        /// Extract words from each wordlist with fcs less equal than user specified,
        /// Then combine extracted words into dictionary.
        /// </summary>
        /// <param name="cutoffWordlist">Mutil wordlists are used to extract words with fcs.</param>
        /// <returns>Extract words which fcs less equal than user specified fcs.</returns>
        private Dictionary<string, int> CombineWordlistWithFCS(List<CutoffWordlist> cutoffWordlist)
        {
            Dictionary<string, int> cutoffWords = new Dictionary<string, int>();

            // load wordlist for each domain
            foreach (CutoffWordlist domainWordList in cutoffWordlist)
            {
                // load wordlist
                string wordlistPath = Directory.GetParent(this.InWorkingDirectory).FullName;
                wordlistPath = Path.Combine(wordlistPath, domainWordList.Path);

                WordList domainWords = WordList.LoadWordListFromWordListFile(wordlistPath);

                // Collect words according cut off frequency
                foreach (WordListItem item in domainWords.Words.Values)
                {
                    // if word's fcs <= user specified fcs, save to result
                    if (item.Fcs <= domainWordList.Fcs)
                    {
                        if (!cutoffWords.ContainsKey(item.Grapheme))
                        {
                            cutoffWords.Add(item.Grapheme);
                        }
                    }
                    else
                    {
                        // as wordlist order is asc, here should break when word fcs > user's fcs.
                        break;
                    }
                }
            }

            return cutoffWords;
        }

        #endregion

        #region Prune Lexicon Based on LTS Rule

        /// <summary>
        /// Prune word entries in filters with LTS rule:
        /// We only process word entries which have one kind of POS values in the filers, e.g.,
        /// If "noun" is in the filters, only word entries with POS="noun" will be processed.
        /// Just like in PruneAllWordsWithLTSRule, word entries which pronunciations can be correctly
        /// Predicted by LTS rule will be dropped.
        /// </summary>
        /// <param name="sp">ServiceProvider.</param>
        /// <param name="mainLexicon">Main lexicon.</param>
        /// <param name="outLexicon">Out lexicon.</param>
        /// <param name="ltsRemovedLexicon">Lts removed lexicon.</param>
        /// <param name="posFilter">Pos filter.</param>
        /// <param name="pronComparisonMode">Pronunciation comparison mode.</param>
        /// <param name="exceptionWords">Exception word list.</param>
        private void PruneByLtsRule(SP.ServiceProvider sp, Lexicon mainLexicon, Lexicon outLexicon, Lexicon ltsRemovedLexicon,
            string posFilter, PronComparisonMode pronComparisonMode, IDictionary<string, int> exceptionWords)
        {
            SP.Pronouncer pronouncer = sp.Engine.TextProcessor.SentenceAnalyzer.Pronouncer;
            SP.Phoneme phoneTable = sp.Engine.Phoneme;
            bool onlyPhone = pronComparisonMode.Equals(PronComparisonMode.OnlyPhone);

            int leftCount = 0;
            int prunedCount = 0;
            int meetFilterRequirementCount = 0;
            int polyphoneCount = 0;

            // Prune word entries which have one kind of POS values
            // in filters in the lexicon data
            foreach (LexicalItem item in mainLexicon.Items.Values)
            {
                // Keep current item into pruned lexicon data
                bool left = true;
                LexicalItem queriedItem = mainLexicon.Lookup(item.Grapheme, true);

                if (queriedItem.Pronunciations.Count == 1)
                {
                    if (queriedItem.Pronunciations[0].Properties.Count == 1)
                    {
                        // Get POS value
                        string posValue =
                            item.Pronunciations[0].Properties[0].PartOfSpeech.Value;

                        // none prune word list is case insensitive
                        if (posFilter.Equals(posValue, StringComparison.InvariantCulture) &&
                            !exceptionWords.ContainsKey(item.Grapheme.ToLower()))
                        {
                            meetFilterRequirementCount++;
                            if (IsAvailableToPruneWithLTSRule(item, pronouncer, phoneTable, onlyPhone))
                            {
                                left = false;
                                prunedCount++;
                            }
                        }
                    }
                }
                else
                {
                    polyphoneCount++;
                }

                if (left)
                {
                    // Write current item into the pruned lexicon data
                    outLexicon.Items.Add(item.Grapheme, item);
                    ++leftCount;
                }
                else
                {
                    ltsRemovedLexicon.Items.Add(item.Grapheme, item);
                }
            }

            Log("LTS pruning Successful ...");
            Log("Total Lexicon Entries = {0}", mainLexicon.Items.Count);
            Log("Total Polyphony Entries (case insensitive)= {0}", polyphoneCount);
            Log("Left Lexicon Entries = {0}", leftCount);
            Log("Removed Lexicon Entries = {0}", prunedCount);
            Log("==================================================================");
            Log("POS = {0}, Total Entries = {1}, Left Entries = {2}, Pruned Entries= {3}",
                InPosFilter, meetFilterRequirementCount, meetFilterRequirementCount - prunedCount, prunedCount);
        }

        private Dictionary<string, int> LoadNonPruneWordsFromText(string nonPruneWordlistPath)
        {
            Dictionary<string, int> nonPruneWords = new Dictionary<string, int>();

            // add words in nonprunwordlist.txt to exceptionWords
            using (StreamReader sr = new StreamReader(nonPruneWordlistPath))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();

                    if (!string.IsNullOrEmpty(line))
                    {
                        // none prune word list is case insensitive
                        string word = line.Trim().ToLower();
                        if (!nonPruneWords.ContainsKey(word))
                        {
                            nonPruneWords.Add(word);
                        }
                    }
                }
            }

            return nonPruneWords;
        }

        private Dictionary<string, int> LoadNonPruneWordsFromXML(string nonPruneConfigPath)
        {
            Dictionary<string, int> nonPruneWords = new Dictionary<string, int>();

            XDocument xmlDoc = XDocument.Load(nonPruneConfigPath);

            XElement rootElement = xmlDoc.Element("NonPruneWordlists");

            string inWorkDirPath = Directory.GetParent(InWorkingDirectory).FullName;

            foreach (XElement wordlistElement in rootElement.Elements("wordlist"))
            {
                // how much words user would get, -1 represents closed wordlist.
                int topCount = -1;

                // if user don't set frequency, get all words.
                if (!int.TryParse(wordlistElement.Value, out topCount))
                {
                    topCount = -1;
                }

                // load for each wordlist
                XAttribute pathAttr = wordlistElement.Attribute("path");
                if (pathAttr == null)
                {
                    throw new InvalidDataException("Cannot find attribute [path] in config.");
                }

                string wordlistPath = Path.Combine(inWorkDirPath, pathAttr.Value);

                WordList wordlist = WordList.LoadWordListFromWordListFile(wordlistPath);

                int loopCount = 0;

                // add to exceptionWords with case insensitive
                foreach (string word in wordlist.Words.Keys)
                {
                    if (!nonPruneWords.ContainsKey(word.ToLower()))
                    {
                        nonPruneWords.Add(word.ToLower());
                    }

                    if (topCount != -1 && ++loopCount >= topCount)
                    {
                        break;
                    }
                }
            }

            // get nonPruneWordlist txt file.
            XElement nonPrunedWordFileElement = rootElement.Element("NonPruneWordlistFile");

            if (nonPrunedWordFileElement != null)
            {
                string nonPruneWordlistTxtFile = Path.Combine(inWorkDirPath, nonPrunedWordFileElement.Value);

                Dictionary<string, int> nonPrunedWordsFromTxt = LoadNonPruneWordsFromText(nonPruneWordlistTxtFile);

                // add to resut.
                foreach (string word in nonPrunedWordsFromTxt.Keys)
                {
                    if (!nonPruneWords.ContainsKey(word.ToLower()))
                    {
                        nonPruneWords.Add(word.ToLower());
                    }
                }
            }

            return nonPruneWords;
        }

        /// <summary>
        /// Get the exception word list
        /// Use dictionary for performance.
        /// </summary>
        /// <returns>IDictionary.</returns>
        private IDictionary<string, int> GetExceptionWordList()
        {
            IDictionary<string, int> exceptionWords = null;
            if (InExceptionWordList != null)
            {
                exceptionWords = InExceptionWordList;
            }
            else
            {
                exceptionWords = new Dictionary<string, int>();
                if (!string.IsNullOrEmpty(InExceptionWordFile))
                {
                    // if nonPruneWordlist file is xml, parse it.
                    if (Path.GetExtension(InExceptionWordFile).ToLower().Equals(".xml"))
                    {
                        // xml is config that include some wordlists.
                        exceptionWords = LoadNonPruneWordsFromXML(InExceptionWordFile);
                    }
                    else if (Path.GetExtension(InExceptionWordFile).ToLower().Equals(".txt"))
                    {
                        exceptionWords = LoadNonPruneWordsFromText(InExceptionWordFile);
                    }
                }

                if (InCompiler != null)
                {
                    // load wordbreaker words.
                    string wordBreakerDataDir = InCompiler.DataHandlerList.Datas[RawDataName.WordBreakerDataPath].Path;
                    string[] wordBreakerFileNames = new string[]
                    {
                        "abbrev.txt",
                        "specialwords.txt",
                        "wordbreakerspecialwords.txt",
                        "titles.txt"
                    };

                    List<string> wordList = new List<string>();
                    ErrorSet errors = new ErrorSet();

                    foreach (string wordBreakerFileName in wordBreakerFileNames)
                    {
                        string wordBreakerFilePath = Path.Combine(wordBreakerDataDir, wordBreakerFileName);
                        if (File.Exists(wordBreakerFilePath))
                        {
                            ErrorSet wordBreakerErrors = WordFile.LoadWordsIntoWordList(wordBreakerFilePath, wordList, false);
                            errors.AddRange(wordBreakerErrors);
                        }
                    }

                    // if load words correctly, add them to none-prunned word list.
                    if (errors.GetSeverityCount(ErrorSeverity.MustFix) == 0)
                    {
                        foreach (string word in wordList)
                        {
                            // none prune word list is case insensitive
                            string lowerWord = word.ToLower();
                            if (!exceptionWords.ContainsKey(lowerWord))
                            {
                                exceptionWords.Add(lowerWord);
                            }
                        }
                    }

                    // add TN rule words to none prune word list.
                    string tnRulePath = InCompiler.DataHandlerList.Datas[RawDataName.TnRule].Path;
                    List<string> tnWords = ExtracWordsFromTNML(tnRulePath);

                    foreach (string word in tnWords)
                    {
                        // none prune word list is case insensitive
                        string lowerWord = word.ToLower();
                        if (!exceptionWords.ContainsKey(lowerWord))
                        {
                            exceptionWords.Add(lowerWord);
                        }
                    }

                    // add Chartable symbol to none prune word list.
                    string charTablePath = InCompiler.DataHandlerList.Datas[RawDataName.CharTable].Path;
                    List<string> chartableWords = ExtractWordsFromCharTable(charTablePath);

                    foreach (string word in chartableWords)
                    {
                        // none prune word list is case insensitive
                        string lowerWord = word.ToLower();
                        if (!exceptionWords.ContainsKey(lowerWord))
                        {
                            exceptionWords.Add(lowerWord);
                        }
                    }
                }

                InExceptionWordList = exceptionWords;
            }

            return exceptionWords;
        }
        #endregion

        /// <summary>
        /// Extract words from tnml.
        /// </summary>
        /// <param name="strTNMLFile">Tnml file.</param>
        /// <returns>The words.</returns>
        private List<string> ExtracWordsFromTNML(string strTNMLFile)
        {
            List<string> words = new List<string>();
            System.Xml.XmlDocument tnmlDoc = new System.Xml.XmlDocument();
            tnmlDoc.Load(strTNMLFile);
            System.Xml.XmlNodeList outNodes = tnmlDoc.SelectNodes(".//out");
            foreach (System.Xml.XmlNode node in outNodes)
            {
                List<string> outWords =
                    new List<string>(node.InnerText.Split(" ,.".ToCharArray()));
                System.Xml.XmlAttribute attr = node.Attributes["action"];
                if (attr != null && attr.Value == "glue-LR")
                {
                    // remove the first word
                    if (outWords.Count > 0)
                    {
                        outWords.RemoveAt(0);
                    }

                    // remove the last word
                    if (outWords.Count > 0)
                    {
                        outWords.RemoveAt(outWords.Count - 1);
                    }
                }

                words.AddRange(outWords);
            }

            return words;
        }

        /// <summary>
        /// Extract words from chartable.
        /// </summary>
        /// <param name="strCharTable">Chartable file.</param>
        /// <returns>Word list.</returns>
        private List<string> ExtractWordsFromCharTable(string strCharTable)
        {
            CharTable charTable = new CharTable();
            charTable.Load(strCharTable);

            List<string> expanedWords = charTable.ExtractExpansionWords();
            List<string> symbolWords = charTable.Symbols;

            expanedWords.AddRange(symbolWords);

            return expanedWords;
        }

        #region Pruning Correction

        /// <summary>
        /// PruningCorrect.
        /// </summary>
        /// <param name="sp">ServiceProvider.</param>
        /// <param name="mainLexicon">Main lexicon.</param>
        /// <param name="removedLexicon">Removed lexicon.</param>
        /// <param name="pronComparisonMode">Pronunciation comparison mode.</param>
        /// <returns>Count of entries put back to lexicon.</returns>
        private int PruningCorrect(SP.ServiceProvider sp, Lexicon mainLexicon, Lexicon removedLexicon,
            PronComparisonMode pronComparisonMode)
        {
            SP.Pronouncer pronouncer = sp.Engine.TextProcessor.SentenceAnalyzer.Pronouncer;
            SP.Phoneme phoneTable = sp.Engine.Phoneme;
            bool onlyPhone = pronComparisonMode.Equals(PronComparisonMode.OnlyPhone);

            int backEntriesCount = 0;
            int remainRemovedCount = 0;
            int totalCount = 0;

            // Prune word entries which have one kind of POS values
            // in filters in the lexicon data
            foreach (LexicalItem item in removedLexicon.Items.Values)
            {
                totalCount++;
                if (totalCount % 5000 == 0)
                {
                    Log("Handled {0} words.", totalCount);
                }

                if (mainLexicon.Items.ContainsKey(item.Grapheme))
                {
                    continue;
                }

                // Keep current item not to be put into pruned lexicon data
                bool back = true;
                Debug.Assert(item.Pronunciations.Count > 0);
                if (item.Pronunciations.Count == 1)
                {
                    back = IsWrongPronunciationByEndToEndFrontEnd(item, sp.Engine, onlyPhone);
                }

                if (back)
                {
                    mainLexicon.Items.Add(item.Grapheme, item);
                    Logger.LogLine("[{0}] was back to lexicon", item.Grapheme);
                    ++backEntriesCount;
                }
                else
                {
                    ++remainRemovedCount;
                }
            }

            Log("Pruning correction Successful ...");
            Log("Total Lexicon Entries = {0}", mainLexicon.Items.Count);
            Log("Back to Lexicon Entries = {0}", backEntriesCount);
            Log("Remain removed Entries = {0}", remainRemovedCount);
            return backEntriesCount;
        }
        #endregion

        #region Struct

        /// <summary>
        /// Save the cut off word list.
        /// </summary>
        public class CutoffWordlist
        {
            /// <summary>
            /// Gets or sets Fcs.
            /// </summary>
            public double Fcs { get; set; }

            /// <summary>
            /// Gets or sets Word list path.
            /// </summary>
            public string Path { get; set; }
        }

        #endregion
    }
}