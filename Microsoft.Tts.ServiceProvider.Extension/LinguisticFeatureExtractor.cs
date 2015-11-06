// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LinguisticFeatureExtractor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//   This module defines the LinguisticFeatureExtractor class to extract
//   linguistic features.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Tts.ServiceProvider.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Compiler;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Schema;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Waveform;
    using Microsoft.Tts.ServiceProvider;
    using Microsoft.Tts.ServiceProvider.FeatureExtractor;
    using Microsoft.TTS.ServiceProvider.Extension;
    using FeatureMeta = Microsoft.Tts.ServiceProvider.FeatureExtractor.FeatureMeta;
    using Language = Microsoft.Tts.ServiceProvider.Language;
    using Phoneme = Microsoft.Tts.ServiceProvider.Phoneme;
    using TtsEmphasis = Microsoft.Tts.ServiceProvider.TtsEmphasis;
    using TtsTobiAccent = Microsoft.Tts.ServiceProvider.TtsTobiAccent;
    using TtsTobiBoundaryTone = Microsoft.Tts.ServiceProvider.TtsTobiBoundaryTone;
    using TtsUtterance = Microsoft.Tts.ServiceProvider.TtsUtterance;
    using TtsWord = Microsoft.Tts.ServiceProvider.TtsWord;

    /// <summary>
    /// The class to extractor linguistic features.
    /// </summary>
    public class LinguistciFeatureExtractor : IDisposable
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="LinguistciFeatureExtractor"/> class.
        /// </summary>
        /// <param name="featureInfos">
        /// The feature infos.
        /// </param>
        /// <param name="phoneSet">
        /// The phone set.
        /// </param>
        /// <param name="posSet">
        /// The pos set.
        /// </param>
        /// <param name="manager">
        /// The customized feature plugin manager.
        /// </param>
        /// <param name="logger">
        /// The logger.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Exception.
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// Exception.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public LinguistciFeatureExtractor(IEnumerable<LinguisticFeatureInfo> featureInfos, TtsPhoneSet phoneSet,
            TtsPosSet posSet, CustomizedFeaturePluginManager manager, ILogger logger)
        {
            if (featureInfos == null)
            {
                throw new ArgumentNullException("featureInfos");
            }

            if (phoneSet == null)
            {
                throw new ArgumentNullException("phoneSet");
            }

            if (posSet == null)
            {
                throw new ArgumentNullException("posSet");
            }

            Logger = logger ?? new NullLogger();
            PhoneSet = phoneSet;
            PosSet = posSet;

            // Builds the name of feature name set.
            FeatureNameSetName = Helper.NeutralFormat("VoiceModelTrainer.{0}", PhoneSet.Language.ToString());

            try
            {
                // Creates a feature extration engine.
                ExtractionEngine = new FeatureExtractionEngine();

                // Creates the feature meta data.
                FeatureMetas = ExtractionEngine.Convert(LabelFeatureNameSet.MandatoryFeatureNames.ToList());
                FeatureInfos = new List<LinguisticFeatureInfo>();
                FeatureValueRecords = new List<FeatureValueRecord>();
                for (int i = 0; i < LabelFeatureNameSet.MandatoryFeatureNames.Length; ++i)
                {
                    FeatureInfos.Add(null);
                    FeatureValueRecords.Add(new FeatureValueRecord());
                }

                foreach (LinguisticFeatureInfo info in featureInfos)
                {
                    FeatureValueRecords.Add(new FeatureValueRecord());
                    int index = Array.IndexOf(LabelFeatureNameSet.MandatoryFeatureNames, info.Name);
                    if (index < 0)
                    {
                        FeatureMetas.Add(ExtractionEngine.Convert(info.Name, info.ExtendedProperty));
                        FeatureInfos.Add(info);
                    }
                    else
                    {
                        FeatureInfos[index] = info;
                    }
                }
            }
            catch (EspException e)
            {
                throw new InvalidDataException("Feature extraction engine error", e);
            }

            // Checks whether need pos and ToBI accent.
            for (int i = 0; i < FeatureMetas.Count; ++i)
            {
                if (!NeedPos &&
                    (FeatureMetas[i].Property == TtsFeatureProperty.TTS_FEATURE_PROPERTY_POS ||
                        FeatureMetas[i].Property == TtsFeatureProperty.TTS_FEATURE_PROPERTY_POSTAGGER_POS))
                {
                    NeedPos = true;
                }

                if (!NeedToBI &&
                    (FeatureMetas[i].Property == TtsFeatureProperty.TTS_FEATURE_PROPERTY_PRIMARY_ACCENT_POSITION ||
                        FeatureMetas[i].Property == TtsFeatureProperty.TTS_FEATURE_PROPERTY_SYLL_NUM_FROM_LEFT_ACCENT ||
                        FeatureMetas[i].Property == TtsFeatureProperty.TTS_FEATURE_PROPERTY_SYLL_NUM_TO_RIGHT_ACCENT ||
                        FeatureMetas[i].Property == TtsFeatureProperty.TTS_FEATURE_PROPERTY_ACCENTED_SYLL_NUM_BEFORE_CURR_SYLL ||
                        FeatureMetas[i].Property == TtsFeatureProperty.TTS_FEATURE_PROPERTY_ACCENTED_SYLL_NUM_AFTER_CURR_SYLL ||
                        FeatureMetas[i].Property == TtsFeatureProperty.TTS_FEATURE_PROPERTY_ACCENT ||
                        FeatureMetas[i].Property == TtsFeatureProperty.TTS_FEATURE_PROPERTY_TOBI_FINAL_BOUNDARY_TONE))
                {
                    NeedToBI = true;
                }
            }

            // Gets Phoneme according to phone set.
            MemoryStream phoneStream = new MemoryStream();
            ErrorSet errorSet = PhoneSetCompiler.Compile(phoneSet, phoneStream);
            if (errorSet.Count > 0)
            {
                foreach (Error error in errorSet.Errors)
                {
                    Logger.LogLine(error.ToString());
                }

                if (errorSet.Contains(ErrorSeverity.MustFix))
                {
                    throw new InvalidDataException("Error happens in tts phone set compiling");
                }
            }

            phoneStream.Seek(0, SeekOrigin.Begin);
            Phoneme = new Phoneme(phoneStream, (Language)phoneSet.Language);

            // Gets the utterance extenders.
            if (manager != null)
            {
                List<PluginInfo> pluginInfos = manager.GetPlugins(CustomizedFeaturePluginManager.AttachBeforeExtraction);
                if (pluginInfos != null)
                {
                    UtteranceExtenders = UtteranceExtenderFinder.LoadUtteranceExtenders(pluginInfos);
                }
            }

            // Initialize ZhToneIndexPlugin if the language is zh-CN
            if (Language.ZhCN == (Language)phoneSet.Language)
            {
                ChineseToneIndexExtractor = new ChineseToneIndexExtractor();
                ChineseToneIndexExtractor.Initialize(Phoneme);
            }

            // Creates feature name set.
            if (LabelFeatureNameSet.Exist(FeatureNameSetName))
            {
                FeatureNameSet = LabelFeatureNameSet.Query(FeatureNameSetName);
            }
            else
            {
                FeatureNameSet = LabelFeatureNameSet.Create(FeatureNameSetName,
                    FeatureMetas.Select(o => o.Name).ToList());
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets feature extraction engine.
        /// </summary>
        private FeatureExtractionEngine ExtractionEngine { get; set; }

        /// <summary>
        /// Gets or sets PosSet.
        /// </summary>
        private TtsPosSet PosSet { get; set; }

        /// <summary>
        /// Gets or sets PhoneSet.
        /// </summary>
        private TtsPhoneSet PhoneSet { get; set; }

        /// <summary>
        /// Gets or sets Phoneme.
        /// </summary>
        private Phoneme Phoneme { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether need Pos.
        /// </summary>
        private bool NeedPos { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether need ToBI accent.
        /// </summary>
        private bool NeedToBI { get; set; }

        /// <summary>
        /// Gets or sets utterance extenders.
        /// </summary>
        private List<IUtteranceExtender> UtteranceExtenders { get; set; }

        /// <summary>
        /// Gets or sets feature meta data.
        /// </summary>
        private List<FeatureMeta> FeatureMetas { get; set; }

        /// <summary>
        /// Gets or sets linguistic feature infos.
        /// </summary>
        private List<LinguisticFeatureInfo> FeatureInfos { get; set; }

        /// <summary>
        /// Gets or sets feature value records.
        /// </summary>
        private List<FeatureValueRecord> FeatureValueRecords { get; set; }

        /// <summary>
        /// Gets or sets feature name set.
        /// </summary>
        private LabelFeatureNameSet FeatureNameSet { get; set; }

        /// <summary>
        /// Gets or sets logger.
        /// </summary>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the name of feature name set.
        /// </summary>
        private string FeatureNameSetName { get; set; }

        /// <summary>
        /// Gets or sets ChineseToneIndexExtractor.
        /// </summary>
        private ChineseToneIndexExtractor ChineseToneIndexExtractor { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Extracts features from the given script.
        /// </summary>
        /// <param name="script">
        /// The xml script file.
        /// </param>
        /// <param name="fileListMap">
        /// The file list map.
        /// </param>
        /// <param name="alignmentDir">
        /// The alignment directory.
        /// </param>
        /// <param name="waveDir">
        /// The wave directory.
        /// </param>
        /// <returns>
        /// The extracted features in training sentence set.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Exception.
        /// </exception>
        public TrainingSentenceSet Extract(XmlScriptFile script, FileListMap fileListMap, string alignmentDir,
            string waveDir)
        {
            if (script == null)
            {
                throw new ArgumentNullException("script");
            }

            if (fileListMap == null)
            {
                throw new ArgumentNullException("fileListMap");
            }

            if (alignmentDir == null)
            {
                throw new ArgumentNullException("alignmentDir");
            }

            if (waveDir == null)
            {
                throw new ArgumentNullException("waveDir");
            }

            TrainingSentenceSet sentenceSet = new TrainingSentenceSet { FileListMap = fileListMap };
            List<string> errList = new List<string>();

            foreach (string sid in fileListMap.Map.Keys)
            {
                ScriptItem item = script.ItemDic[sid];

                try
                {
                    // Loads the segmentation file.
                    SegmentFile segmentFile = new SegmentFile();
                    segmentFile.Load(fileListMap.BuildPath(alignmentDir, sid, "txt"));

                    // Loads the waveform file to set the end time of the last segmentation.
                    WaveFile waveFile = new WaveFile();
                    waveFile.Load(fileListMap.BuildPath(waveDir, sid, FileExtensions.Waveform));
                    segmentFile.WaveSegments[segmentFile.WaveSegments.Count - 1].EndTime = waveFile.Duration;

                    // Extracts the single script item.
                    Sentence sentence = Extract(item, segmentFile);
                    sentence.TrainingSet = sentenceSet;
                    sentenceSet.Sentences.Add(sid, sentence);
                }
                catch (Exception e)
                {
                    if (!(e is InvalidDataException))
                    {
                        throw;
                    }

                    // Removes the error sentences.
                    Logger.Log(Helper.BuildExceptionMessage(e));
                    script.Remove(sid);
                    errList.Add(sid);
                }
            }

            fileListMap.RemoveItems(errList);
            return sentenceSet;
        }

        /// <summary>
        /// Extracts the features of the given utterance.
        /// </summary>
        /// <param name="sentId">
        /// Sentence id.
        /// </param>
        /// <param name="utterance">
        /// Service Provider utterance object.
        /// </param>
        /// <returns>
        /// The sentence contains all the features.
        /// </returns>
        /// <exception cref="InvalidDataException">
        /// Exception.
        /// </exception>
        public Sentence Extract(string sentId, TtsUtterance utterance)
        {
            List<FeatureVector> vectors;

            try
            {
                // Then, extracts the features.
                vectors = ExtractionEngine.Extract(utterance, FeatureMetas);
            }
            catch (EspException e)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Extract feature error on sentence \"{0}\"",
                    sentId), e);
            }

            // Validates the extracted vectors.
            if (vectors.Count != FeatureMetas.Count)
            {
                throw new InvalidDataException(
                    Helper.NeutralFormat("Length of result is mismatch on sentence \"{0}\"", sentId));
            }

            for (int i = 0; i < vectors.Count; i++)
            {
                if (vectors[i].Count != utterance.Phones.Count)
                {
                    throw new InvalidDataException(
                        Helper.NeutralFormat("Length of vector is mismatch on sentence \"{0}\"", sentId));
                }
            }

            // Creates a sentence to store all the features.
            Sentence sentence = new Sentence { Id = sentId };
            for (int i = 0; i < vectors[0].Count; ++i)
            {
                // Create candidates for each phoneme.
                PhoneSegment p = new PhoneSegment
                {
                    Sentence = sentence,
                    Index = i,
                    Features = vectors.Select(v => v[i])
                        .Skip(LabelFeatureNameSet.MandatoryFeatureNames.Length).ToArray(),
                };

                // Create the label to store the features.
                Label label = new Label(FeatureNameSet);
                for (int j = 0; j < vectors.Count; ++j)
                {
                    if (vectors[j][i].ValueType == FeatureValueType.FEATURE_VALUE_TYPE_UNKOWN)
                    {
                        label.SetFeatureValue(FeatureNameSet.FeatureNames[j], Label.NotApplicableFeatureValue);
                    }
                    else if (FeatureMetas[j].Property == TtsFeatureProperty.TTS_FEATURE_PROPERTY_PHONE_ID)
                    {
                        Phone phone = PhoneSet.GetPhone(vectors[j][i].IntValue);
                        label.SetFeatureValue(FeatureNameSet.FeatureNames[j], Offline.Phoneme.ToHtk(phone.Name));
                    }
                    else
                    {
                        label.SetFeatureValue(FeatureNameSet.FeatureNames[j],
                            vectors[j][i].IntValue.ToString(CultureInfo.InvariantCulture));
                    }

                    // Updates the corresponding value records.
                    FeatureValueRecords[j].Update(vectors[j][i]);
                }

                p.Label = label;
                sentence.PhoneSegments.Add(p);
            }

            return sentence;
        }

        /// <summary>
        /// Builds questions according to the history data of the feature extractor.
        /// </summary>
        /// <param name="phoneQuestionBuilder">
        /// The question builder to hold all phone question.
        /// </param>
        /// <param name="customizedQuestionBuilders">
        /// The customized question builders.
        /// </param>
        /// <param name="questionMode">
        /// The question building mode (Lsp, LogF0, Duration, Prosody, All).
        /// </param>
        /// <returns>
        /// The all questions listed in string.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Exception.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Exception.
        /// </exception>
        public List<string> BuildQuestions(QuestionBuilder phoneQuestionBuilder,
            Dictionary<string, QuestionBuilder[]> customizedQuestionBuilders,
            QuestionMode questionMode)
        {
            if (phoneQuestionBuilder == null)
            {
                throw new ArgumentNullException("phoneQuestionBuilder");
            }

            List<string> questions = new List<string>();

            for (int i = 0; i < FeatureMetas.Count; ++i)
            {
                string name = FeatureMetas[i].Name;
                string left = FeatureNameSet.GetLeftSeparator(name);
                string right = FeatureNameSet.GetRightSeparator(name);

                // Skip question building for the non-matched linguistic feature.
                if (FeatureInfos[i] != null && (FeatureInfos[i].QuestionMode & questionMode) == 0)
                {
                    continue;
                }

                if (FeatureMetas[i].Property == TtsFeatureProperty.TTS_FEATURE_PROPERTY_PHONE_ID)
                {
                    // Builds phone questions.
                    questions.AddRange(phoneQuestionBuilder.BuildQuestions(name, left, right));
                }
                else if (FeatureMetas[i].Property == TtsFeatureProperty.TTS_FEATURE_PROPERTY_POS)
                {
                    // Builds the pos questions.
                    questions.AddRange(
                        new QuestionBuilder(
                            FeatureValueRecords[i].Values.Cast<object>().ToDictionary(
                                o => PosSet.IdItems[(uint)((int)o)]))
                            .BuildQuestions(name, left, right));
                }
                else
                {
                    if (FeatureInfos[i] == null)
                    {
                        // Builds integer questions.
                        questions.AddRange(new QuestionBuilder(FeatureValueRecords[i].Values).BuildQuestions(name, left, right));
                    }
                    else if (string.IsNullOrEmpty(FeatureInfos[i].QuestionCategory))
                    {
                        if (FeatureInfos[i].ValueType == LingFeatureValueType.Null)
                        {
                            questions.AddRange(new QuestionBuilder(FeatureValueRecords[i].Values).BuildQuestions(name, left, right));
                        }
                        else
                        {
                            questions.AddRange(new QuestionBuilder(FeatureValueRecords[i].Values, FeatureInfos[i].MinValue,
                                FeatureInfos[i].MaxValue, FeatureInfos[i].ValueType).BuildQuestions(name, left, right));
                        }
                    }
                    else
                    {
                        // Builds customized questions.
                        if (customizedQuestionBuilders == null)
                        {
                            throw new ArgumentException("Custmized question builder expected");
                        }

                        if (!customizedQuestionBuilders.ContainsKey(FeatureInfos[i].QuestionCategory))
                        {
                            throw new ArgumentException(
                                Helper.NeutralFormat("Custmized question builder \"{0}\" not found",
                                    FeatureInfos[i].QuestionCategory));
                        }

                        QuestionBuilder[] builders = customizedQuestionBuilders[FeatureInfos[i].QuestionCategory];
                        foreach (QuestionBuilder builder in builders)
                        {
                            questions.AddRange(builder.BuildQuestions(name, left, right));
                        }
                    }
                }

                // Builds a not appliable feature question.
                questions.Add(QuestionBuilder.BuildNotApplicableFeatureQuestion(name, left, right));
            }

            return questions;
        }

        /// <summary>
        /// Disposes the all data.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the all data.
        /// </summary>
        /// <param name="disposing">Disposing flag.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (ExtractionEngine != null)
                {
                    ExtractionEngine.Dispose();
                }

                if (Phoneme != null)
                {
                    Phoneme.Dispose();
                }

                if (UtteranceExtenders != null)
                {
                    foreach (IUtteranceExtender extender in UtteranceExtenders)
                    {
                        extender.Dispose();
                    }
                }

                if (FeatureMetas != null)
                {
                    foreach (FeatureMeta featureMeta in FeatureMetas)
                    {
                        featureMeta.Dispose();
                    }
                }

                if (ChineseToneIndexExtractor != null)
                {
                    ChineseToneIndexExtractor.Dispose();
                }

                LabelFeatureNameSet.Remove(FeatureNameSetName);
            }
        }

        /// <summary>
        /// Extracts the features of the given script item.
        /// </summary>
        /// <param name="item">
        /// The script item.
        /// </param>
        /// <param name="segmentFile">
        /// The segmentation file.
        /// </param>
        /// <returns>
        /// The sentence contains all the features.
        /// </returns>
        /// <exception cref="InvalidDataException">
        /// Exception.
        /// </exception>
        private Sentence Extract(ScriptItem item, SegmentFile segmentFile)
        {
            UtteranceBuilder builder = new UtteranceBuilder(PhoneSet, PosSet, Phoneme)
            {
                NeedPos = NeedPos,
                NeedToBI = NeedToBI,
            };

            // Builds a utterance first.
            Sentence sentence = null;
            using (TtsUtterance utterance = builder.Build(item, segmentFile, false, -1))
            {
                // Extract ToneIndex if the language is zh-CN
                if (Language.ZhCN == (Language)PhoneSet.Language)
                {
                    ChineseToneIndexExtractor.Process(utterance, item);
                }

                if (UtteranceExtenders != null)
                {
                    // Uses the utterance extender here.
                    foreach (IUtteranceExtender extender in UtteranceExtenders)
                    {
                        extender.Process(utterance, item);
                    }
                }

                // Creates a sentence to store all the features.
                sentence = Extract(item.Id, utterance);

                for (int i = 0; i < sentence.PhoneSegments.Count; ++i)
                {
                    // Create candidates for each phoneme.
                    sentence.PhoneSegments[i].StartTimeInSecond = (float)segmentFile.WaveSegments[i].StartTime;
                    sentence.PhoneSegments[i].EndTimeInSecond = (float)segmentFile.WaveSegments[i].EndTime;
                }
            }

            return sentence;
        }

        #endregion
    }

    /// <summary>
    /// The utterance builder.
    /// </summary>
    public class UtteranceBuilder
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UtteranceBuilder"/> class.
        /// </summary>
        /// <param name="phoneSet">
        /// The phone set.
        /// </param>
        /// <param name="posSet">
        /// The pos set.
        /// </param>
        /// <param name="phoneme">
        /// The phoneme.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Exception.
        /// </exception>
        public UtteranceBuilder(TtsPhoneSet phoneSet, TtsPosSet posSet, Phoneme phoneme)
        {
            if (phoneSet == null)
            {
                throw new ArgumentNullException("phoneSet");
            }

            if (posSet == null)
            {
                throw new ArgumentNullException("posSet");
            }

            if (phoneme == null)
            {
                throw new ArgumentNullException("phoneme");
            }

            PhoneSet = phoneSet;
            PosSet = posSet;
            Phoneme = phoneme;
            NeedPunctuation = false;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether need punctuation.
        /// </summary>
        public bool NeedPunctuation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether need Pos.
        /// </summary>
        public bool NeedPos { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether need ToBI accent.
        /// </summary>
        public bool NeedToBI { get; set; }

        /// <summary>
        /// Gets or sets PosSet.
        /// </summary>
        private TtsPosSet PosSet { get; set; }

        /// <summary>
        /// Gets or sets Phoneme.
        /// </summary>
        private Phoneme Phoneme { get; set; }

        /// <summary>
        /// Gets or sets PhoneSet.
        /// </summary>
        private TtsPhoneSet PhoneSet { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Builds utterance according to the given script item.
        /// </summary>
        /// <param name="item">
        /// The script item.
        /// </param>
        /// <param name="segmentFile">
        /// The segmentation file.
        /// </param>
        /// <param name="buildAllWords">
        /// Whether build all words in ScriptItem into utterance.
        /// </param>
        /// <param name="subSentenceIndex">
        /// Which sub sentence is used to build into utterance, if value is -1, then use all the sub sentences.
        /// </param>
        /// <returns>
        /// The built utterance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Exception.
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// Exception.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public TtsUtterance Build(ScriptItem item, SegmentFile segmentFile, bool buildAllWords, int subSentenceIndex)
        {
            Helper.ThrowIfNull(item);

            TtsUtterance utterance = new TtsUtterance();
            int phoneIndex = 0;
            try
            {
                // Silence indicates a silence word.
                if (segmentFile != null &&
                    segmentFile.WaveSegments[phoneIndex].IsSilenceFeature)
                {
                    phoneIndex += AppendSilenceWord(utterance, segmentFile.WaveSegments[phoneIndex].Label);
                }

                // Creates a words map for ToBI accent.
                Dictionary<ScriptWord, TtsWord> mapWords = new Dictionary<ScriptWord, TtsWord>();

                int sentenceIndex = 0;
                foreach (ScriptSentence scriptSentence in item.Sentences)
                {
                    // Only add certain sentence in the scriptItem.
                    if (subSentenceIndex != -1 && sentenceIndex++ != subSentenceIndex)
                    {
                        continue;
                    }

                    // Treats unkown sentence type as declarative.
                    if (scriptSentence.SentenceType != SentenceType.Unknown)
                    {
                        utterance.SentenceType = (TtsSentenceType)scriptSentence.SentenceType;
                    }
                    else
                    {
                        utterance.SentenceType = (TtsSentenceType)SentenceType.Declarative;
                    }

                    utterance.SentenceEmotionType = (EmotionmlCategory)scriptSentence.Emotion;

                    // Converts each word in script sentence.
                    foreach (ScriptWord scriptWord in scriptSentence.Words)
                    {
                        if (buildAllWords || scriptWord.IsPronouncableNormalWord)
                        {
                            phoneIndex += AppendNormalWord(utterance, scriptWord);

                            // Adds into words map.
                            mapWords.Add(scriptWord, utterance.Words[utterance.Words.Count - 1]);

                            // Breaks if meets the end of the utterance.
                            if (segmentFile != null &&
                                phoneIndex >= segmentFile.WaveSegments.Count)
                            {
                                break;
                            }

                            if (segmentFile != null &&
                                segmentFile.WaveSegments[phoneIndex].IsSilenceFeature)
                            {
                                phoneIndex += AppendSilenceWord(utterance, segmentFile.WaveSegments[phoneIndex].Label);
                            }
                        }
                        else if (buildAllWords || (NeedPunctuation && scriptWord.WordType == WordType.Punctuation))
                        {
                            phoneIndex += AppendPunctuationWord(utterance, scriptWord);
                        }
                    }
                }

                // Builds phone list.
                int[] pauseDurations = new int[(int)TtsPauseLevel.PAU_IDX_SENTENCE + 1];
                Array.Clear(pauseDurations, 0, pauseDurations.Length);
                utterance.BuildPhoneList(Phoneme, pauseDurations, 0, 0);

                // Builds ToBI accent, which should be happened after phone list built.
                BuildToBIInformation(mapWords);

                // Builds phrase list.
                utterance.BuildPhraseList();

                // Builds character list.
                utterance.BuildContextCharacters();

                return utterance;
            }
            catch (EspException e)
            {
                throw new InvalidDataException(
                    Helper.NeutralFormat("Build utterance error on sentence \"{0}\"", item.Id), e);
            }
        }

        /// <summary>
        /// Appends a silence word in then end of given utterance.
        /// </summary>
        /// <param name="utterance">
        /// The given utterance.
        /// </param>
        /// <param name="phone">
        /// The phone name.
        /// </param>
        /// <returns>
        /// The phoneme count of the silence word.
        /// </returns>
        private int AppendSilenceWord(TtsUtterance utterance, string phone)
        {
            Debug.Assert(Offline.Phoneme.IsSilenceFeature(phone), "Silence word should have phone: short pause or silence");

            TtsWord word = utterance.AppendNewWord();
            word.PhoneIds = Phoneme.PronunciationToPhoneIds(Offline.Phoneme.ToRuntime(phone));
            word.LangId = (ushort)PhoneSet.Language;
            word.WordType = TtsWordType.WT_SILENCE;
            word.Pos = 0;

            // Modify the silence word's break level to make it consistent with runtime engine
            if (word.Previous != null)
            {
                word.BreakLevel = word.Previous.BreakLevel;
            }
            else
            {
                word.BreakLevel = TtsBreakLevel.BK_IDX_SENTENCE;
            }

            return 1;
        }

        /// <summary>
        /// Appends a punctuation word in the end of given utterance.
        /// </summary>
        /// <param name="utterance">
        /// The given utterance.
        /// </param>
        /// <param name="scriptWord">
        /// The script word.
        /// </param>
        /// <returns>
        /// The phoneme count of the given word.
        /// </returns>
        /// <exception cref="InvalidDataException">
        /// Exception.
        /// </exception>
        private int AppendPunctuationWord(TtsUtterance utterance, ScriptWord scriptWord)
        {
            TtsWord word = utterance.AppendNewWord();
            word.LangId = (ushort)scriptWord.Language;
            word.BreakLevel = (TtsBreakLevel)scriptWord.Break;
            word.Emphasis = (TtsEmphasis)scriptWord.Emphasis;
            word.WordText = scriptWord.Grapheme;
            word.NETypeText = scriptWord.NETypeText;
            word.WordType = TtsWordType.WT_PUNCTUATION;

            // There is no phoneme for punctuation word.
            return 0;
        }

        /// <summary>
        /// Appends a normal word in the end of given utterance.
        /// </summary>
        /// <param name="utterance">
        /// The given utterance.
        /// </param>
        /// <param name="scriptWord">
        /// The script word.
        /// </param>
        /// <returns>
        /// The phoneme count of the given word.
        /// </returns>
        /// <exception cref="InvalidDataException">
        /// Exception.
        /// </exception>
        private int AppendNormalWord(TtsUtterance utterance, ScriptWord scriptWord)
        {
            TtsWord word = utterance.AppendNewWord();
            word.LangId = (ushort)scriptWord.Language;
            word.BreakLevel = (TtsBreakLevel)scriptWord.Break;
            word.Emphasis = (TtsEmphasis)scriptWord.Emphasis;
            word.WordText = scriptWord.Grapheme;
            word.NETypeText = scriptWord.NETypeText;
            word.WordRegularText = scriptWord.RegularText;
            word.WordType = TtsWordType.WT_NORMAL;
            word.AcousticDomain = DomainExtension.MapToEnum(scriptWord.AcousticDomainTag);
            word.WordExpansion = scriptWord.Expansion;
            word.ReadablePronunciation = scriptWord.Pronunciation;
            if (!string.IsNullOrEmpty(scriptWord.Pronunciation))
            {
                word.PhoneIds = Phoneme.PronunciationToPhoneIds(Pronunciation.RemoveUnitBoundary(scriptWord.Pronunciation));
            }

            if (NeedPos)
            {
                // Checks pos.
                if (string.IsNullOrEmpty(scriptWord.PosString))
                {
                    throw new InvalidDataException(
                        Helper.NeutralFormat("No POS found in sentence \"{0}\" for word \"{1}\"",
                            scriptWord.Sentence.ScriptItem.Id, scriptWord.Grapheme));
                }

                // Sets pos value.
                word.Pos = (ushort)PosSet.Items[scriptWord.PosString];
                string taggingPos = PosSet.CategoryTaggingPOS[scriptWord.PosString];
                word.POSTaggerPos = (ushort)PosSet.Items[taggingPos];
            }

            // Gets the normal phoneme count.
            ErrorSet errorSet = new ErrorSet();
            int count = scriptWord.GetNormalPhoneNames(PhoneSet, errorSet).Count;
            if (errorSet.Count > 0)
            {
                throw new InvalidDataException(
                    Helper.NeutralFormat("Invalid phone found in sentence \"{0}\" for word \"{1}\"",
                        scriptWord.Sentence.ScriptItem.Id, scriptWord.Grapheme));
            }

            return count;
        }

        /// <summary>
        /// Builds ToBI informaiton.
        /// </summary>
        /// <param name="mapWords">The map of script word and tts word.</param>
        private void BuildToBIInformation(Dictionary<ScriptWord, TtsWord> mapWords)
        {
            if (NeedToBI)
            {
                TtsTobiAccentSet accentSet = new TtsTobiAccentSet();
                TtsTobiBoundaryToneSet boundaryToneSet = new TtsTobiBoundaryToneSet();

                foreach (KeyValuePair<ScriptWord, TtsWord> pair in mapWords)
                {
                    // Builds ToBI accent if needs.
                    Collection<ScriptSyllable> scriptSyllables = pair.Key.Syllables;
                    TtsSyllable syllable = pair.Value.FirstSyllable;

                    foreach (ScriptSyllable scriptSyllable in scriptSyllables)
                    {
                        if (scriptSyllable.TobiPitchAccent != null)
                        {
                            syllable.ToBIAccent = (TtsTobiAccent)accentSet.Items[scriptSyllable.TobiPitchAccent.Label];
                        }

                        syllable = syllable.Next;
                    }

                    // Build ToBI boudary tone
                    // pair.ScriptWord.TobiFinalBoundaryTone
                    if (pair.Key.TobiFinalBoundaryTone != null)
                    {
                        pair.Value.ToBIFinalBoundaryTone = (TtsTobiBoundaryTone)boundaryToneSet.Items[pair.Key.TobiFinalBoundaryTone.Label];
                    }
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// The class to record the all feature values.
    /// </summary>
    internal class FeatureValueRecord
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureValueRecord"/> class.
        /// </summary>
        public FeatureValueRecord()
        {
            Values = new HashSet<int>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the all feature values.
        /// </summary>
        public HashSet<int> Values { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Updates values.
        /// </summary>
        /// <param name="value">
        /// The given feature value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Exception.
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// Exception.
        /// </exception>
        public void Update(FeatureValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (value.ValueType != FeatureValueType.FEATURE_VALUE_TYPE_UNKOWN)
            {
                if (value.ValueType == FeatureValueType.FEATURE_VALUE_TYPE_STRING)
                {
                    throw new InvalidDataException("Cannot support string type linguistic feature");
                }

                if (!Values.Contains(value.IntValue))
                {
                    Values.Add(value.IntValue);
                }
            }
        }

        #endregion
    }
}