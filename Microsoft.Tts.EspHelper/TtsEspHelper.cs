//----------------------------------------------------------------------------
// <copyright file="TtsEspHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements UnitList.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.EspHelper
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Config;
    using Microsoft.Tts.Offline.Utility;
    using SP = Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// TTS ESP Helper.
    /// </summary>
    public class TtsEspHelper : IDisposable
    {
        #region Fileds

        private SP.TtsEngine _engine;
        private SP.ServiceProvider _serviceProvider;
        private Language _language;
        private ProcessMode _mode;

        #endregion

        #region Constructions

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsEspHelper"/> class.
        /// </summary>
        /// <param name="serviceProvider">ServiceProvider.</param>
        /// <param name="language">Language.</param>
        /// <param name="mode">Process mode.</param>
        public TtsEspHelper(SP.ServiceProvider serviceProvider, Language language, ProcessMode mode)
        {
            Helper.ThrowIfNull(serviceProvider);
            _serviceProvider = serviceProvider;
            _engine = serviceProvider.Engine;
            _language = language;
            _mode = mode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsEspHelper"/> class.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <param name="voicePath">Voice font path.</param>
        /// <param name="mode">Process mode.</param>
        public TtsEspHelper(Language language, string voicePath, ProcessMode mode) :
            this(language, voicePath, string.Empty, string.Empty, mode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsEspHelper"/> class.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <param name="voicePath">Voice font path.</param>
        /// <param name="langDllPath">Language dll path.</param>
        /// <param name="langDataPath">Language data path.</param>
        /// <param name="mode">Process mode.</param>
        public TtsEspHelper(Language language, string voicePath, string langDllPath,
            string langDataPath, ProcessMode mode)
        {
            if (string.IsNullOrEmpty(voicePath))
            {
                voicePath = null;
            }

            if (string.IsNullOrEmpty(langDllPath))
            {
                langDllPath = null;
            }

            if (string.IsNullOrEmpty(langDataPath))
            {
                langDataPath = null;
            }

            _language = language;
            if (string.IsNullOrEmpty(langDllPath) && string.IsNullOrEmpty(langDataPath))
            {
                _engine = new SP.TtsEngine((SP.Language)language, voicePath);
            }
            else if (string.IsNullOrEmpty(langDataPath))
            {
                _engine = new SP.TtsEngine((SP.Language)language, voicePath, langDllPath);
            }
            else
            {
                _engine = new SP.TtsEngine((SP.Language)language, voicePath, langDllPath, langDataPath);
            }

            _mode = mode;
        }

        #endregion

        #region Enum

        /// <summary>
        /// Process node.
        /// </summary>
        [Flags]
        public enum ProcessMode
        {
            /// <summary>
            /// Do text process.
            /// </summary>
            TextProcess = 0x00000001,

            /// <summary>
            /// Do tag prosody.
            /// </summary>
            ProsodyTag = 0x00000002,

            /// <summary>
            /// Do unit generate.
            /// </summary>
            UnitGenerate = 0x00000004,

            /// <summary>
            /// Do unit lattice generate.
            /// </summary>
            UnitLatticeGenerate = 0x00000010,

            /// <summary>
            /// Do unit selection.
            /// </summary>
            UnitSelect = 0x00000020,

            /// <summary>
            /// Do wave generation.
            /// </summary>
            WaveGenerate = 0x00000040,
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets TTS engine.
        /// </summary>
        public SP.TtsEngine Engine
        {
            get { return _engine; }
        }

        /// <summary>
        /// Gets serviceProvider.
        /// </summary>
        public SP.ServiceProvider ServiceProvider
        {
            get { return _serviceProvider; }
        }

        #endregion

        #region Public static methods

        /// <summary>
        /// Build unit pronunciation.
        /// </summary>
        /// <param name="unit">Unit to be build pronunciation.</param>
        /// <returns>Pronunciation string.</returns>
        public static string BuildPronunciation(SP.TtsUnit unit)
        {
            StringBuilder sb = new StringBuilder();

            // If unit index equals zero, it is sil.
            if (unit.UnitIndex != 0)
            {
                sb.Append(unit.UnitText);
                sb.Append(" ");

                uint positionInSyllable = unit.FeatureVector.Features[(int)SP.UnitFeatureId.FEATURE_POS_IN_SYLLABLE].IntValue;
                if (positionInSyllable == (uint)SP.TtsUnitPosInSyl.UNIT_POS_IN_SYL_NUCLEUS_V ||
                    positionInSyllable == (uint)SP.TtsUnitPosInSyl.UNIT_POS_IN_SYL_NUCLEUS_VC ||
                    positionInSyllable == (uint)SP.TtsUnitPosInSyl.UNIT_POS_IN_SYL_NUCLEUS_CV ||
                    positionInSyllable == (uint)SP.TtsUnitPosInSyl.UNIT_POS_IN_SYL_NUCLEUS_CVC)
                {
                    switch (unit.FeatureVector.Features[(int)SP.UnitFeatureId.FEATURE_STRESS].IntValue)
                    {
                        case (int)SP.TtsStress.STRESS_PRIMARY:
                            sb.Append("1 ");
                            break;

                        case (int)SP.TtsStress.STRESS_SECONDARY:
                            sb.Append("2 ");
                            break;

                        case (int)SP.TtsStress.STRESS_TERTIARY:
                            sb.Append("3 ");
                            break;
                    }
                }

                switch (unit.BreakLevel)
                {
                    case SP.TtsBreakLevel.BK_IDX_PHONE:
                        sb.Append(". ");
                        break;

                    case SP.TtsBreakLevel.BK_IDX_SYLLABLE:
                        sb.Append("- ");
                        break;

                    default:
                        sb.Append("/ ");
                        break;
                }
            }

            sb.Replace('+', ' ');

            return sb.ToString();
        }

        /// <summary>
        /// Build sentence from word list.
        /// </summary>
        /// <param name="wordList">Esp word list.</param>
        /// <returns>Sentence string.</returns>
        public static string BuildSentence(ReadOnlyCollection<SP.TtsWord> wordList)
        {
            StringBuilder sb = new StringBuilder();
            foreach (SP.TtsWord word in wordList)
            {
                if (!string.IsNullOrEmpty(word.WordText))
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(" ");
                    }

                    sb.Append(word.WordText);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Check whether the word is OOV.
        /// </summary>
        /// <param name="word">Word to be checked.</param>
        /// <returns>Whether the word is OOV.</returns>
        public static bool IsOov(SP.TtsWord word)
        {
            bool isOov = false;
            if (word.WordType == SP.TtsWordType.WT_NORMAL &&
                ((word.PronSource == SP.TtsPronSource.PS_LTS ||
                    word.PronSource == SP.TtsPronSource.PS_SPELLING) &&
                !string.IsNullOrEmpty(word.PhoneIds)))
            {
                isOov = true;
            }

            return isOov;
        }

        /// <summary>
        /// Whether this is a common word.
        /// </summary>
        /// <param name="word">Word to be checked.</param>
        /// <returns>True if is a common word.</returns>
        public static bool IsCommonWord(SP.TtsWord word)
        {
            return word.WordType == SP.TtsWordType.WT_NORMAL ||
                word.WordType == SP.TtsWordType.WT_PUNCTUATION ||
                word.WordType == SP.TtsWordType.WT_SPELLOUT;
        }

        /// <summary>
        /// Whether this is a common word except punctuation.
        /// </summary>
        /// <param name="word">Word to be checked.</param>
        /// <returns>True if is a common word.</returns>
        public static bool IsNonPuncCommonWord(SP.TtsWord word)
        {
            return word.WordType == SP.TtsWordType.WT_NORMAL ||
                word.WordType == SP.TtsWordType.WT_SPELLOUT;
        }

        /// <summary>
        /// Map language IDs.
        /// </summary>
        /// <param name="language">Language ID used in offline.</param>
        /// <returns>Language ID used in serviceprovider.</returns>
        public static SP.Language MapLanguage(Language language)
        {
            string languageString = Enum.GetName(typeof(SP.Language), language);

            return (SP.Language)Enum.Parse(typeof(SP.Language), languageString);
        }

        /// <summary>
        /// Separate word with service provider.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <param name="languageDataPath">Input data path.</param>
        /// <param name="sentence">Input sentence.</param>
        /// <param name="words">W.</param>
        public static void BreakWordsWithESP(Microsoft.Tts.Offline.Language language,
                string languageDataPath, string sentence, Collection<string> words)
        {
            using (TtsEspHelper espHelper = new TtsEspHelper(language, string.Empty,
                string.Empty, languageDataPath, TtsEspHelper.ProcessMode.TextProcess))
            {
                espHelper.BreakWordsWithESP(sentence, words);
            }
        }

        /// <summary>
        /// Normalize text with service provider.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <param name="languageDataPath">Input data path.</param>
        /// <param name="speakStr">Input sentence.</param>
        /// <param name="sayAs">Say as string.</param>
        /// <param name="words">Words.</param>
        public static void NormalizeWithESP(Microsoft.Tts.Offline.Language language,
                string languageDataPath, string speakStr, string sayAs, Collection<string> words)
        {
            using (TtsEspHelper espHelper = new TtsEspHelper(language, string.Empty, string.Empty,
                languageDataPath, TtsEspHelper.ProcessMode.TextProcess))
            {
                foreach (SP.TtsUtterance utt in espHelper.EspUtterances(speakStr, sayAs))
                {
                    using (utt)
                    {
                        foreach (SP.TtsWord ttsWord in utt.Words)
                        {
                            if (IsCommonWord(ttsWord))
                            {
                                if (!string.IsNullOrEmpty(ttsWord.WordText))
                                {
                                    words.Add(ttsWord.WordText);
                                }

                                if (ttsWord.TNBreak > 0)
                                {
                                    words.Add("<break>");
                                }
                            }
                        }
                    }
                }                    
            }
        }

        /// <summary>
        /// Separate sentence with ESP.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <param name="languageDataPath">Input data path.</param>
        /// <param name="paragraph">Input paragraph.</param>
        /// <param name="result">Result sentence.</param>
        public static void SeparateSentenceWithESP(Language language,
                string languageDataPath, string paragraph, Collection<string> result)
        {
            using (TtsEspHelper espHelper = new TtsEspHelper(language, string.Empty, string.Empty,
                languageDataPath, TtsEspHelper.ProcessMode.TextProcess))
            {
                espHelper.SeparateSentenceWithESP(paragraph, result);
            }
        }

        #endregion

        #region Public instance methods

        /// <summary>
        /// Get script words from utterance.
        /// </summary>
        /// <param name="utt">Utterance.</param>
        /// <param name="isOovWord">Return whether each word is oov.</param>
        /// <returns>Script words.</returns>
        public Collection<ScriptWord> GetScriptWords(SP.TtsUtterance utt, Collection<bool> isOovWord)
        {
            if (utt == null)
            {
                throw new ArgumentNullException("utt");
            }

            Collection<ScriptWord> words = new Collection<ScriptWord>();
            for (int i = 0; i < utt.Words.Count; ++i)
            {
                SP.TtsWord word = utt.Words[i];
                if (IsCommonWord(word))
                {
                    ScriptWord scriptWord = new ScriptWord();

                    // Currently runtime will append a punctuation word for the sentence that has no 
                    // punctuation at the end.
                    if (string.IsNullOrEmpty(word.WordText) && (i == utt.Words.Count - 1) &&
                        word.WordType == SP.TtsWordType.WT_PUNCTUATION)
                    {
                        // Don't add the word to keep consistence with original input text.
                        continue;
                    }

                    scriptWord.Grapheme = word.WordText;
                    if (!string.IsNullOrEmpty(word.Pronunciation))
                    {
                        scriptWord.Pronunciation = word.Pronunciation.ToLowerInvariant();
                    }

                    ushort posTaggerPos = _engine.PosTable.GetPOSTaggerPOS(checked((ushort)word.Pos));
                    scriptWord.PosString = _engine.PosTable.IdToString(posTaggerPos);
                    scriptWord.DetailedPosString = _engine.PosTable.IdToString(checked((ushort)word.Pos));
                    scriptWord.NETypeText = word.NETypeText;
                    scriptWord.OffsetInString = (int)word.TextOffset;
                    scriptWord.LengthInString = (int)word.TextLength;
                    scriptWord.PronSource = (TtsPronSource)word.PronSource;
                    scriptWord.RegularText = word.WordRegularText;

                    switch (word.WordType)
                    {
                        case SP.TtsWordType.WT_NORMAL:
                        case SP.TtsWordType.WT_SPELLOUT:
                            scriptWord.WordType = WordType.Normal;
                            break;
                        case SP.TtsWordType.WT_PUNCTUATION:
                            scriptWord.WordType = WordType.Punctuation;
                            break;
                    }

                    switch (word.Emphasis)
                    {
                        case SP.TtsEmphasis.EMPH_YES:
                            scriptWord.Emphasis = TtsEmphasis.Yes;
                            break;
                        case SP.TtsEmphasis.EMPH_NONE:
                            scriptWord.Emphasis = TtsEmphasis.None;
                            break;
                    }

                    switch (word.BreakLevel)
                    {
                        case SP.TtsBreakLevel.BK_IDX_SYLLABLE:
                            scriptWord.Break = TtsBreak.Syllable;
                            break;
                        case SP.TtsBreakLevel.BK_IDX_INTERM_PHRASE:
                            scriptWord.Break = TtsBreak.InterPhrase;
                            break;
                        case SP.TtsBreakLevel.BK_IDX_INTONA_PHRASE:
                            scriptWord.Break = TtsBreak.IntonationPhrase;
                            break;
                        case SP.TtsBreakLevel.BK_IDX_SENTENCE:
                            scriptWord.Break = TtsBreak.Sentence;
                            break;
                    }

                    if (word.ToBIFinalBoundaryTone != (uint)TtsTobiBoundary.NoBoundaryTone)
                    {
                        Offline.TtsTobiBoundaryToneSet boundarySet = new Offline.TtsTobiBoundaryToneSet();
                        scriptWord.TobiFinalBoundaryTone = TobiLabel.Create(boundarySet.IdItems[(uint)word.ToBIFinalBoundaryTone]);
                    }

                    words.Add(scriptWord);
                    isOovWord.Add(IsOov(word));
                }
            }

            return words;
        }

        /// <summary>
        /// Get utterance generated by ESP.
        /// </summary>
        /// <param name="content">Content to be spoken.</param>
        /// <returns>Utterance enum.</returns>
        public IEnumerable<SP.TtsUtterance> EspUtterances(string content)
        {
            return EspUtterances(content, string.Empty);
        }

        /// <summary>
        /// Get utterance generated by ESP.
        /// </summary>
        /// <param name="content">Content to be spoken.</param>
        /// <param name="sayas">Sayas used by ESP.</param>
        /// <returns>Utterance enum.</returns>
        public IEnumerable<SP.TtsUtterance> EspUtterances(string content, string sayas)
        {
            if (_engine == null)
            {
                throw new ArgumentNullException("_engine");
            }

            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentNullException("content");
            }

            if (string.IsNullOrEmpty(sayas))
            {
                _engine.SetSpeakText(content);
            }
            else
            {
                _engine.SetSpeakText(content, sayas);
            }

            if ((_mode & ProcessMode.TextProcess) != 0)
            {
                _engine.TextProcessor.Reset();
            }

            if ((_mode & ProcessMode.ProsodyTag) != 0)
            {
                _engine.LinguisticProsodyTagger.Reset();
            }

            if ((_mode & ProcessMode.UnitGenerate) != 0)
            {
                _engine.UnitGenerator.Reset();
            }

            if ((_mode & ProcessMode.UnitLatticeGenerate) != 0)
            {
                _engine.UnitLatticeGenerator.Reset();
            }

            if ((_mode & ProcessMode.UnitSelect) != 0)
            {
                _engine.UnitSelector.Reset();
            }

            if ((_mode & ProcessMode.WaveGenerate) != 0)
            {
                _engine.WaveGenerator.Reset();
            }

            while (true)
            {
                SP.TtsUtterance utterance = new SP.TtsUtterance();

                if ((_mode & ProcessMode.TextProcess) != 0 &&
                    !_engine.TextProcessor.Process(utterance))
                {
                    break;
                }

                if ((_mode & ProcessMode.ProsodyTag) != 0 &&
                    !_engine.LinguisticProsodyTagger.Process(utterance))
                {
                    break;
                }

                if ((_mode & ProcessMode.UnitGenerate) != 0 &&
                    !_engine.UnitGenerator.Process(utterance))
                {
                    break;
                }

                if ((_mode & ProcessMode.UnitLatticeGenerate) != 0 &&
                    !_engine.UnitLatticeGenerator.Process(utterance))
                {
                    break;
                }

                if ((_mode & ProcessMode.UnitSelect) != 0 &&
                    !_engine.UnitSelector.Process(utterance))
                {
                    break;
                }

                if ((_mode & ProcessMode.WaveGenerate) != 0 &&
                    !_engine.WaveGenerator.Process(utterance))
                {
                    break;
                }

                yield return utterance;
            }
        }

        /// <summary>
        /// Break words with ESP.
        /// </summary>
        /// <param name="sentence">Sentence to be processed.</param>
        /// <param name="words">Output words.</param>
        public void BreakWordsWithESP(string sentence, Collection<string> words)
        {
            foreach (SP.TtsUtterance utt in EspUtterances(sentence))
            {
                using (utt)
                {
                    int lastOffset = -1;
                    foreach (SP.TtsWord ttsWord in utt.Words)
                    {
                        if (ttsWord.TextOffset != lastOffset)
                        {
                            string word = sentence.Substring((int)ttsWord.TextOffset,
                                (int)ttsWord.TextLength);
                            words.Add(word);
                            lastOffset = (int)ttsWord.TextOffset;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Separate sentence with ESP.
        /// </summary>
        /// <param name="paragraph">Input paragraph.</param>
        /// <param name="result">Result sentence.</param>
        public void SeparateSentenceWithESP(string paragraph, Collection<string> result)
        {
            foreach (SP.TtsUtterance utt in EspUtterances(paragraph))
            {
                using (utt)
                {
                    uint sentenceStart = 0;
                    uint sentenceEnd = 0;
                    bool firstWord = true;
                    foreach (SP.TtsWord ttsWord in utt.Words)
                    {
                        if (firstWord)
                        {
                            sentenceStart = ttsWord.TextOffset;
                            sentenceEnd = ttsWord.TextOffset + ttsWord.TextLength;
                            firstWord = false;
                        }
                        else
                        {
                            sentenceEnd = ttsWord.TextOffset + ttsWord.TextLength;
                        }
                    }

                    if (sentenceEnd != 0)
                    {
                        string sentence = paragraph.Substring((int)sentenceStart,
                            (int)sentenceEnd - (int)sentenceStart);
                        result.Add(sentence);
                    }
                }
            }
        }

        /// <summary>
        /// Get original unit list used by ESP when speak the content.
        /// </summary>
        /// <param name="content">Content to be spoken.</param>
        /// <param name="waveUnits">Unit dictionary.</param>
        /// <returns>Unit list used by ESP to speak the content.</returns>
        public UnitListDictionary GetOriginalUnitList(string content, Dictionary<long, WaveUnit> waveUnits)
        {
            UnitListDictionary unitListDictionary = new UnitListDictionary();
            foreach (SP.TtsUtterance uttr in EspUtterances(content))
            {
                using (uttr)
                {
                    for (int index = 0; index < uttr.Segments.Count; index++)
                    {
                        WaveUnit waveUnit = waveUnits[uttr.Segments[index].InventoryWaveStartPosition];
                        UnitItem unitItem = new UnitItem();
                        unitItem.SentenceId = waveUnit.SentenceId;
                        unitItem.IndexInSentence = waveUnit.IndexInSentence;
                        unitItem.Name = waveUnit.Name;
                        unitListDictionary.AddUnit(_language, UnitList.UnitListType.Hold, unitItem);
                    }
                }
            }

            return unitListDictionary;
        }

        /// <summary>
        /// List the best unit and unit candidates number used by ESP when speak the content.
        /// </summary>
        /// <param name="content">Content to be spoken.</param>
        /// <param name="waveUnits">Unit dictionary.</param>
        /// <param name="candCounter">Candidates number per unit.</param>
        /// <returns>Unit list used by ESP to speak the content.</returns>
        public Collection<UnitItem> GenUnitList(string content,
            Dictionary<long, WaveUnit> waveUnits, out Collection<int> candCounter)
        {
            Collection<UnitItem> unitList = new Collection<UnitItem>();
            candCounter = new Collection<int>();
            foreach (SP.TtsUtterance uttr in EspUtterances(content))
            {
                using (uttr)
                {
                    int candNum = 0;
                    for (int index = 0; index < uttr.Segments.Count; index++)
                    {
                        if (uttr.Segments[index].Unit.IsSilence)
                        {
                            continue;
                        }

                        WaveUnit waveUnit = waveUnits[uttr.Segments[index].InventoryWaveStartPosition];
                        UnitItem unitItem = new UnitItem();
                        unitItem.SentenceId = waveUnit.SentenceId;
                        unitItem.IndexInSentence = waveUnit.IndexInSentence;
                        unitItem.Name = waveUnit.Name;
                        unitList.Add(unitItem);
                        candCounter.Add(uttr.UnitLattice.WucList[candNum++].WucNodeList.Count);
                    }
                }
            }

            return unitList;
        }

        /// <summary>
        /// Generate script item from raw text(only generate to word level).
        /// </summary>
        /// <param name="text">Plain text.</param>
        /// <returns>ScriptItem.</returns>
        public ScriptItem GenerateScriptItem(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException("text");
            }

            // this function should contain "ProcessMode.TextProcess"
            if ((_mode & ProcessMode.TextProcess) == 0)
            {
                throw new InvalidOperationException("Process mode can only be ProcessMode.TextProcess");
            }

            ScriptItem item = new ScriptItem();
            item.Text = text;

            foreach (SP.TtsUtterance utt in EspUtterances(text))
            {
                using (utt)
                {
                    if (utt.Words.Count == 0)
                    {
                        continue;
                    }

                    ScriptSentence sentence = new ScriptSentence();
                    foreach (SP.TtsWord word in utt.Words)
                    {
                        if (!string.IsNullOrEmpty(word.WordText))
                        {
                            ScriptWord scriptWord = new ScriptWord();
                            scriptWord.Grapheme = word.WordText;

                            if (!string.IsNullOrEmpty(word.Pronunciation))
                            {
                                scriptWord.Pronunciation = word.Pronunciation.ToLowerInvariant();
                            }

                            scriptWord.WordType = WordType.Normal;
                            if (word.WordType == SP.TtsWordType.WT_PUNCTUATION)
                            {
                                scriptWord.WordType = WordType.Punctuation;
                            }

                            scriptWord.PronSource = (TtsPronSource)word.PronSource;

                            sentence.Words.Add(scriptWord);
                        }
                    }

                    sentence.Text = sentence.BuildTextFromWords();
                    item.Sentences.Add(sentence);
                }
            }

            return item;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose this object.
        /// </summary>
        /// <param name="disposing">Flag indicating whether delete unmanaged resource.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != _engine)
                {
                    _engine.Dispose();
                }
            }
        }

        #endregion
    }
}
