//----------------------------------------------------------------------------
// <copyright file="ScriptFeatureImport.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script feature import
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.ScriptSynthesizer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Speech.Synthesis;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Research;
    using Microsoft.Tts.ServiceProvider;
    using Offline.Common;
    using Offline.Core;
    using Offline = Microsoft.Tts.Offline;
    using SP = Microsoft.Tts.ServiceProvider;
    using TtsTobiBoundaryTone = Microsoft.Tts.ServiceProvider.TtsTobiBoundaryTone;

    /// <summary>
    /// Script feature import.
    /// </summary>
    public class ScriptFeatureImport : IDisposable
    {
        #region Fields

        /// <summary>
        /// Valid minimum f0 value.
        /// </summary>
        private const float MinF0Value = 1e-3F;

        /// <summary>
        /// Indicate not update state.
        /// </summary>
        private const int NotUpdateState = -1;

        /// <summary>
        /// Service provider.
        /// </summary>
        private ServiceProvider _serviceProvider;

        /// <summary>
        /// Common config of script synthesizer.
        /// </summary>
        private ScriptSynthesizerCommonConfig _commonConfig;

        /// <summary>
        /// Config of script feature import.
        /// </summary>
        private ScriptFeatureImportConfig _config;

        /// <summary>
        /// Log writer.
        /// </summary>
        private TextLogger _logger;

        /// <summary>
        /// Current script item.
        /// </summary>
        private ScriptItem _curScriptItem;

        /// <summary>
        /// Current script sentence.
        /// </summary>
        private ScriptSentence _curScriptSentence;

        /// <summary>
        /// Invalid script item list.
        /// </summary>
        private List<string> _invalidItemList = new List<string>();

        /// <summary>
        /// Phone index of current sentence.
        /// </summary>
        private int[][,] _totalPhoneIndex;

        /// <summary>
        /// Flag of sentence match between runtime and script.
        /// </summary>
        private bool _sentenceMatch = false;

        /// <summary>
        /// Disposed flag.
        /// </summary>
        private bool _disposed = false;

        #endregion

        #region Constructor and destructor

        /// <summary>
        /// Initializes a new instance of the ScriptFeatureImport class.
        /// </summary>
        /// <param name="commonConfig">Common config of script synthesizer.</param>
        /// <param name="config">Config of script feature import.</param>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="logger">Log writer.</param>
        public ScriptFeatureImport(ScriptSynthesizerCommonConfig commonConfig,
            ScriptFeatureImportConfig config, ServiceProvider serviceProvider,
            TextLogger logger)
        {
            //// commonConfig can be null.

            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            if (!serviceProvider.Engine.IsHts)
            {
                string message = string.Format(CultureInfo.InvariantCulture, 
                    "Only support Hts engine.");
                throw new NotSupportedException(message);
            }

            _commonConfig = commonConfig;
            _config = config;

            if (_config.UpdateSilenceWord && _config.UpdateFixedSilenceDuration)
            {
                throw new InvalidDataException("Cannot implement updating script silence word and fixed silence duration together.");
            }

            if (!_config.UpdateSilenceWord && _config.UpdateScriptSilenceDuration)
            {
                throw new InvalidDataException("Cannot update script silence duration without update silence word.");
            }

            _serviceProvider = serviceProvider;
            if (_config.UpdateSilenceWord || _config.UpdateFixedSilenceDuration)
            {
                UpdateSilenceDurationsConfig();
            }

            _logger = logger;

            _serviceProvider.Engine.LinguisticProsodyTagger.Processing += new EventHandler<TtsModuleEventArgs>(OnLinguisticProcessing);
            _serviceProvider.Engine.LinguisticProsodyTagger.Processed += new EventHandler<TtsModuleEventArgs>(OnLinguisticProcessed);

            _serviceProvider.Engine.Processed += new EventHandler<TtsModuleEventArgs>(OnProcessed);
            _serviceProvider.Engine.AcousticProsodyTagger.Processed += new EventHandler<TtsModuleEventArgs>(OnAcousticProcessed);

            if (_commonConfig != null)
            {
                Helper.EnsureFolderExist(_commonConfig.OutputWaveDir);
            }
        }

        /// <summary>
        /// Finalizes an instance of the ScriptFeatureImport class.
        /// </summary>
        ~ScriptFeatureImport()
        {
            Dispose(false);
        }

        #endregion

        /// <summary>
        /// Acoustic update helper interface.
        /// </summary>
        private interface IUpdateHelper
        {
            /// <summary>
            /// Find the to be updated syllable/phone/state's position.
            /// </summary>
            /// <param name="stateIndex">State index.</param>
            /// <param name="phoneIndex">Phone index.</param>
            /// <param name="syllableIndex">Syllable index.</param>
            /// <param name="wordIndex">Word index.</param>
            /// <param name="totalPhoneIndex">Phone index array of sentence.</param>
            /// <returns>Certain syllable/phone/state's position.</returns>
            LayerIndex FindLayerIndex(int stateIndex, int phoneIndex, int syllableIndex,
                int wordIndex, int[][,] totalPhoneIndex);

            /// <summary>
            /// Trace log message to log file.
            /// </summary>
            /// <param name="acousticUpdated">Whether updated acoustic.</param>
            /// <param name="wordIndex">Word index.</param>
            /// <param name="syllableIndex">Syllable index.</param>
            /// <param name="phoneIndex">Phone index.</param>
            /// <param name="stateIndex">State index.</param>
            /// <param name="acousticTypeString">String of updated acoustic type.</param>
            /// <param name="logger">Text logger.</param>
            void LogMessage(bool acousticUpdated, int wordIndex, int syllableIndex,
                int phoneIndex, int stateIndex, string acousticTypeString, TextLogger logger);
        }

        #region Public operations

        /// <summary>
        /// Dispose routine.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Prepare for speak.
        /// </summary>
        /// <param name="scriptItem">Script item.</param>
        public void PrepareSpeak(ScriptItem scriptItem)
        {
            _curScriptItem = scriptItem;
            TraceLog(_logger, true, "Processing item: {0}", scriptItem.Id);

            SpeechSynthesizer synthesizer = _serviceProvider.SpeechSynthesizer;
            if (_commonConfig != null)
            {
                string outPutWaveFile = Path.Combine(_commonConfig.OutputWaveDir, Helper.NeutralFormat("{0}.wav", scriptItem.Id));
                synthesizer.SetOutputToWaveFile(outPutWaveFile, _serviceProvider.Engine.AudioFormatInfo);
            }
            else
            {
                synthesizer.SetOutputToNull();
            }
        }

        /// <summary>
        /// Initailize sentence to run.
        /// </summary>
        /// <param name="scriptSentence">Script sentence.</param>
        public void InitializeSentence(ScriptSentence scriptSentence)
        {
            _curScriptSentence = scriptSentence;
            _totalPhoneIndex = null;
            _sentenceMatch = false;
            TraceLog(_logger, true, "Processing sentence: {0}", _curScriptSentence.Text);
        }

        #endregion

        #region Private static operations

        /// <summary>
        /// Add line in log writer.
        /// </summary>
        /// <param name="logger">Log writer object.</param>
        private static void TraceLogLine(TextLogger logger)
        {
            if (logger != null)
            {
                logger.LogLine();
            }
        }

        /// <summary>
        /// Append log information to log writer.
        /// </summary>
        /// <param name="logger">Log writer object.</param>
        /// <param name="newLine">Flag of whether add a new line.</param>
        /// <param name="format">Format of the output.</param>
        /// <param name="list">Argument list of the output.</param>
        private static void TraceLog(TextLogger logger, bool newLine,
            string format, params object[] list)
        {
            if (logger != null)
            {
                if (newLine)
                {
                    logger.LogLine(format, list);
                }
                else
                {
                    logger.Log(format, list);
                }
            }
        }

        /// <summary>
        /// Check the word consistency.
        /// </summary>
        /// <param name="utt">Tts utterance.</param>
        /// <param name="scriptSentence">Script sentence.</param>
        private static void CheckWordConsistency(SP.TtsUtterance utt, ScriptSentence scriptSentence)
        {
            int wordIndex = 0;

            foreach (TtsWord uttWord in utt.Words)
            {
                if (uttWord.IsPronounceable)
                {
                    string wordText = utt.OriginalText.Substring((int)uttWord.TextOffset,
                        (int)uttWord.TextLength).ToLower(CultureInfo.CurrentCulture);
                    if (wordIndex < scriptSentence.PronouncedWords.Count &&
                        !wordText.Equals(scriptSentence.PronouncedWords[wordIndex].Description.ToLower(CultureInfo.CurrentCulture)))
                    {
                        string message = Helper.NeutralFormat("Runtime's word [{0}] " +
                            "and script word [{1}] has no consistence.", wordText,
                            scriptSentence.PronouncedWords[wordIndex].Description);
                        throw new InvalidDataException(message);
                    }

                    wordIndex++;
                }
            }

            if (wordIndex != scriptSentence.PronouncedWords.Count)
            {
                throw new InvalidDataException("Runtime's normal words' count must equal to the script's.");
            }
        }

        /// <summary>
        /// Get the orignal internal and external F0s.
        /// </summary>
        /// <param name="intUtt">Internal utterance.</param>
        /// <param name="extUvSeg">External uvSeg.</param>
        /// <param name="layerIndex">Certain syllable/phone/state's position.</param>
        /// <param name="extNotNullF0">Out float[], External F0s.</param> 
        /// <param name="intNotNullF0Position">Not null F0s' phones' position.</param>
        private static void GetF0(SP.TtsUtterance intUtt, ScriptUvSeg extUvSeg, LayerIndex layerIndex, 
            out float[] extNotNullF0, out List<int> intNotNullF0Position)
        {
            intNotNullF0Position = new List<int>();
            if (extUvSeg.SegType == ScriptUvSegType.Voiced)
            {
                extNotNullF0 = new float[extUvSeg.F0Contour.Contour.Count];
                for (int i = 0; i < extUvSeg.F0Contour.Contour.Count; i++)
                {
                    extNotNullF0[i] = extUvSeg.F0Contour.Contour[i];
                }
            }
            else if (extUvSeg.SegType == ScriptUvSegType.Mixed)
            {
                List<float> extNotNullF0List = new List<float>();
                for (int i = 0; i < extUvSeg.F0Contour.Contour.Count; i++)
                {
                    if (extUvSeg.F0Contour.Contour[i] != 0)
                    {
                        extNotNullF0List.Add(extUvSeg.F0Contour.Contour[i]);
                    }
                }

                extNotNullF0 = new float[extNotNullF0List.Count];
                extNotNullF0List.CopyTo(extNotNullF0);
            }
            else
            {
                extNotNullF0 = null;
            }

            int interF0Index = 0;
            for (int i = 0; i < layerIndex.StartPhone; i++)
            {
                for (int j = 0; j < (int)intUtt.Acoustic.Durations.Column; j++)
                {
                    interF0Index += (int)intUtt.Acoustic.Durations[i][j];
                }
            }

            for (int i = layerIndex.StartPhone; i < layerIndex.EndPhone; i++)
            {
                for (int j = 0; j < (int)intUtt.Acoustic.Durations.Column; j++)
                {
                    for (int k = 0; k < intUtt.Acoustic.Durations[i][j]; k++)
                    {
                        if (intUtt.Acoustic.F0s[interF0Index][0] != 0)
                        {
                            intNotNullF0Position.Add(interF0Index);
                        }

                        interF0Index++;
                    }
                }
            }
        }

        /// <summary>
        /// Check consistency of script and utterance.
        /// </summary>
        /// <param name="utt">Tts utterance.</param>
        /// <param name="scriptSentence">Script sentence.</param>
        /// <param name="option">Checking option.</param>
        private static void CheckConsistency(SP.TtsUtterance utt, ScriptSentence scriptSentence,
            ScriptFeatureImportConfig.CheckingOptions option)
        {
            if ((option & ScriptFeatureImportConfig.CheckingOptions.Word) ==
                ScriptFeatureImportConfig.CheckingOptions.Word)
            {
                CheckWordConsistency(utt, scriptSentence);
            }
        }

        /// <summary>
        /// Update silence words.
        /// </summary>
        /// <param name="utt">Engine TtsUtterance.</param>
        /// <param name="scriptSentence">Script sentence.</param>
        /// <param name="engine">Tts engine.</param>
        /// <param name="logger">Log writer object.</param>
        private static void UpdateSilenceWords(SP.TtsUtterance utt, ScriptSentence scriptSentence,
            TtsEngine engine, TextLogger logger)
        {
            // Gets phone set.
            TtsPhoneSet phoneSet = null;
            if (scriptSentence.ScriptItem != null && scriptSentence.ScriptItem.ScriptFile != null)
            {
                phoneSet = scriptSentence.ScriptItem.ScriptFile.PhoneSet;
            }

            if (phoneSet == null)
            {
                phoneSet = Localor.GetPhoneSet(scriptSentence.Language);
            }

            if (scriptSentence.ScriptItem != null && scriptSentence.ScriptItem.ScriptFile != null)
            {
                scriptSentence.ScriptItem.ScriptFile.PhoneSet = phoneSet;
            }

            int extWordIndex = 0;
            if (scriptSentence.Words[extWordIndex].WordType == WordType.Silence &&
                utt.Words[0].WordType != TtsWordType.WT_SILENCE)
            {
                string phone = scriptSentence.Words[extWordIndex].GetPronunciation(phoneSet);
                Debug.Assert(
                    Offline.Phoneme.IsSilenceFeature(phone), 
                    "Silence word should have only one phoneme - silence or short pause.");

                TtsWord silenceWord = utt.AddNewWord(utt.Words[0], InsertOptions.Before);
                ConfigSilenceWord(
                    engine.Phoneme.PronunciationToPhoneIds(Offline.Phoneme.ToRuntime(phone)),
                    silenceWord,
                    utt.Words[0].BreakLevel);
            }

            for (int uttWordIndex = 0; uttWordIndex < utt.Words.Count; uttWordIndex++)
            {
                TtsWord uttWord = utt.Words[uttWordIndex];
                if (uttWord.IsPronounceable)
                {
                    for (; extWordIndex < scriptSentence.Words.Count; extWordIndex++)
                    {
                        if (scriptSentence.Words[extWordIndex].IsPronounced)
                        {
                            extWordIndex++;
                            if (uttWord.BreakLevel < TtsBreakLevel.BK_IDX_INTERM_PHRASE)
                            {
                                if (extWordIndex < scriptSentence.Words.Count &&
                                    scriptSentence.Words[extWordIndex].WordType == WordType.Silence)
                                {
                                    string str1 = "Warning: Script xml has a silence word, ";
                                    string str2 = "but corresponding word[{0}] in engine has a break level ";
                                    string str3 = "less than BK_IDX_INTERM_PHRASE";
                                    TraceLog(logger, true, str1 + str2 + str3, uttWord.WordText);
                                }

                                if (uttWord.Next != null && uttWord.Next.WordType == TtsWordType.WT_SILENCE)
                                {
                                    utt.Delete(uttWord.Next);
                                }
                            }
                            else
                            {
                                if (extWordIndex < scriptSentence.Words.Count &&
                                    scriptSentence.Words[extWordIndex].WordType == WordType.Silence && 
                                    uttWord.Next.WordType != TtsWordType.WT_SILENCE)
                                {
                                    string phone = scriptSentence.Words[extWordIndex].GetPronunciation(phoneSet);
                                    Debug.Assert(
                                        Offline.Phoneme.IsSilenceFeature(phone),
                                        "Silence word should have only one phoneme - silence or short pause.");

                                    TtsWord silenceWord = utt.AddNewWord(uttWord, InsertOptions.After);
                                    ConfigSilenceWord(
                                        engine.Phoneme.PronunciationToPhoneIds(Offline.Phoneme.ToRuntime(phone)),
                                        silenceWord,
                                        uttWord.BreakLevel);
                                }
                                else if (uttWord.Next != null && uttWord.Next.WordType == TtsWordType.WT_SILENCE)
                                {
                                    utt.Delete(uttWord.Next);
                                }
                            }

                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update pronunciation.
        /// </summary>
        /// <param name="utt">Tts utterance.</param>
        /// <param name="scriptSentence">Script sentence.</param>
        /// <param name="engine">Tts engine.</param>
        private static void UpdatePronunciation(SP.TtsUtterance utt, ScriptSentence scriptSentence, TtsEngine engine)
        {
            int wordCount = 0;

            System.Console.WriteLine("warning: update the Pronunciation!");
            foreach (TtsWord uttWord in utt.Words)
            {
                if (uttWord.TextLength > 0)
                {
                    if (!string.IsNullOrEmpty(scriptSentence.Words[wordCount].Pronunciation) &&
                        !uttWord.Pronunciation.Equals(Offline.Core.Pronunciation.RemoveUnitBoundary(
                        scriptSentence.Words[wordCount].Pronunciation).ToUpper(CultureInfo.InvariantCulture)))
                    {
                        uttWord.PhoneIds = engine.Phoneme.PronunciationToPhoneIds(
                            Offline.Core.Pronunciation.RemoveUnitBoundary(scriptSentence.Words[wordCount].Pronunciation));
                    }

                    wordCount++;
                }
            }
        }

        /// <summary>
        /// Update PartOfSpeech.
        /// </summary>
        /// <param name="utt">Tts utterance.</param>
        /// <param name="scriptSentence">Script sentence.</param>
        /// <param name="engine">Tts engine.</param>
        private static void UpdatePartOfSpeech(SP.TtsUtterance utt, ScriptSentence scriptSentence, TtsEngine engine)
        {
            int wordCount = 0;

            System.Console.WriteLine("warning: update the PartOfSpeech!");
            foreach (TtsWord uttWord in utt.Words)
            {
                if (uttWord.TextLength > 0)
                {
                    uint posId = engine.PosTable.StringToId(scriptSentence.Words[wordCount].PosString);
                    uttWord.Pos = (ushort)posId;
                    wordCount++;
                }
            }
        }

        /// <summary>
        /// Update BreakLevel.
        /// </summary>
        /// <param name="utt">Tts utterance.</param>
        /// <param name="scriptSentence">Script sentence.</param>
        private static void UpdateBreak(SP.TtsUtterance utt, ScriptSentence scriptSentence)
        {
            int wordIndex = 0;

            System.Console.WriteLine("warning: update the BreakLevel!");
            foreach (TtsWord uttWord in utt.Words)
            {
                if (uttWord.IsPronounceable && wordIndex < scriptSentence.PronouncedWords.Count)
                {
                    int breaklevel = (int)scriptSentence.PronouncedWords[wordIndex].Break;
                    uttWord.BreakLevel = (SP.TtsBreakLevel)breaklevel;

                    wordIndex++;
                }
            }
        }

        /// <summary>
        /// Update BoundaryTone.
        /// </summary>
        /// <param name="utt">Tts utterance.</param>
        /// <param name="scriptSentence">Script sentence.</param>
        private static void UpdateBoundaryTone(SP.TtsUtterance utt, ScriptSentence scriptSentence)
        {
            int wordIndex = 0;

            System.Console.WriteLine("warning: update the BoundaryTone!");
            foreach (TtsWord uttWord in utt.Words)
            {
                if (uttWord.IsPronounceable && wordIndex < scriptSentence.PronouncedWords.Count)
                {
                    uttWord.ToBIFinalBoundaryTone = TtsTobiBoundaryTone.K_NOBND;
                    TobiLabel tobiLabel = scriptSentence.PronouncedWords[wordIndex].TobiFinalBoundaryTone;
                    if (tobiLabel != null)
                    {
                        uttWord.ToBIFinalBoundaryTone = StringToTobiBoundary(tobiLabel.Label);
                    }

                    wordIndex++;
                }
            }
        }

        /// <summary>
        /// Update emphasis.
        /// </summary>
        /// <param name="utt">Tts utterance.</param>
        /// <param name="scriptSentence">Script sentence.</param>
        private static void UpdateEmphasis(SP.TtsUtterance utt, ScriptSentence scriptSentence)
        {
            int wordIndex = 0;

            System.Console.WriteLine("warning: update the Emphasis!");
            foreach (TtsWord uttWord in utt.Words)
            {
                if (uttWord.IsPronounceable && wordIndex < scriptSentence.PronouncedWords.Count)
                {
                    int emphasis = (int)scriptSentence.PronouncedWords[wordIndex].Emphasis;
                    uttWord.Emphasis = (SP.TtsEmphasis)emphasis;

                    wordIndex++;
                }
            }
        }

        /// <summary>
        /// Update ToBIAccent.
        /// </summary>
        /// <param name="utt">Tts utterance.</param>
        /// <param name="scriptSentence">Script sentence.</param>
        private static void UpdateToBIAccent(SP.TtsUtterance utt, ScriptSentence scriptSentence)
        {
            int uttWordCount = 0;
            TtsTobiAccentSet accentSet = new TtsTobiAccentSet();

            System.Console.WriteLine("warning: update the ToBIAccent!");
            foreach (ScriptWord scriptWord in scriptSentence.Words)
            {
                if (scriptWord.IsPronounced)
                {
                    SP.TtsWord uttWord;
                    while (!(uttWord = utt.Words[uttWordCount++]).IsPronounceable)
                    {
                    }

                    Collection<ScriptSyllable> scriptSyllables = scriptWord.Syllables;
                    SP.TtsSyllable thisSyllable = uttWord.FirstSyllable;
                    foreach (ScriptSyllable scriptSyllable in scriptSyllables)
                    {
                        if (scriptSyllable.TobiPitchAccent != null)
                        {
                            thisSyllable.ToBIAccent = (SP.TtsTobiAccent)accentSet.Items[scriptSyllable.TobiPitchAccent.Label];
                        }

                        thisSyllable = thisSyllable.Next;
                    }
                }
            }
        }

        /// <summary>
        /// Transfer boundary string to tts tobi boundary.
        /// </summary>
        /// <param name="boundaryString">Boundary string.</param>
        /// <returns>Tts tobi boundary.</returns>
        private static TtsTobiBoundaryTone StringToTobiBoundary(string boundaryString)
        {
            TtsTobiBoundaryTone ttsTobiBoundary = TtsTobiBoundaryTone.K_NOBND;
            switch (boundaryString)
            {
                case "L-":
                    ttsTobiBoundary = TtsTobiBoundaryTone.K_LMINUS;
                    break;
                case "H-":
                    ttsTobiBoundary = TtsTobiBoundaryTone.K_HMINUS;
                    break;
                case "L-L%":
                    ttsTobiBoundary = TtsTobiBoundaryTone.K_LMINUSLPERC;
                    break;
                case "L-H%":
                    ttsTobiBoundary = TtsTobiBoundaryTone.K_LMINUSHPERC;
                    break;
                case "H-H%":
                    ttsTobiBoundary = TtsTobiBoundaryTone.K_HMINUSHPERC;
                    break;
                case "H-L%":
                    ttsTobiBoundary = TtsTobiBoundaryTone.K_HMINUSLPERC;
                    break;
                case "S-":
                    ttsTobiBoundary = TtsTobiBoundaryTone.K_SMINUS;
                    break;
                default:
                    throw new InvalidDataException(Helper.NeutralFormat("Invalid tobi " +
                        "boundary {0}.", boundaryString));
            }

            return ttsTobiBoundary;
        }

        /// <summary>
        /// Configure silence word to insert to engine words.
        /// </summary>
        /// <param name="phoneIds">Tts phone ids.</param>
        /// <param name="silenceWord">Tts silence word.</param>
        /// <param name="breakLevel">Tts break level.</param>
        private static void ConfigSilenceWord(string phoneIds, TtsWord silenceWord, TtsBreakLevel breakLevel)
        {
            silenceWord.WordText = " ";
            silenceWord.WordType = TtsWordType.WT_SILENCE;
            silenceWord.LangId = (ushort)SP.Language.EnUS;
            silenceWord.PhoneIds = phoneIds;
            silenceWord.BreakLevel = breakLevel;
            silenceWord.Emphasis = SP.TtsEmphasis.EMPH_NONE;
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Dispose managed resource.
        /// </summary>
        private void Close()
        {
            if (_serviceProvider != null)
            {
                _serviceProvider.Engine.LinguisticProsodyTagger.Processing -= new EventHandler<TtsModuleEventArgs>(OnLinguisticProcessing);
                _serviceProvider.Engine.LinguisticProsodyTagger.Processed -= new EventHandler<TtsModuleEventArgs>(OnLinguisticProcessed);

                _serviceProvider.Engine.Processed -= new EventHandler<TtsModuleEventArgs>(OnProcessed);
                _serviceProvider.Engine.AcousticProsodyTagger.Processed -= new EventHandler<TtsModuleEventArgs>(OnAcousticProcessed);
            }

            if (_commonConfig != null)
            {
                foreach (string invalidItem in _invalidItemList)
                {
                    Helper.ForcedDeleteFile(Path.Combine(_commonConfig.OutputWaveDir, Helper.NeutralFormat("{0}.wav", invalidItem)));
                    System.Console.WriteLine("Remove the bad wave file: {0}.wav", invalidItem);
                    TraceLog(_logger, true, "Remove the bad wave file: {0}.wav", invalidItem);
                }
            }
        }

        /// <summary>
        /// Do the dispose work here.
        /// </summary>
        /// <param name="disposing">Whether the functions is called by user's code (true), or by finalizer (false).</param>
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    Close();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.

                // Note disposing has been done.
                _disposed = true;
            }
        }

        /// <summary>
        /// Updates the silence duration configuration of service provider.
        /// </summary>
        private void UpdateSilenceDurationsConfig()
        {
            int durationCount = _serviceProvider.Engine.ProsodySetting.PauseDurations.Length;
            if (durationCount != (int)TtsPauseLevel.PAU_IDX_SENTENCE + 1)
            {
                throw new InvalidDataException("serviceProvider.Engine.ProsodySetting.PauseDurations.Length " +
                    "should equal with number of pauses in TtsPauseLevel.");
            }

            _serviceProvider.Engine.ProsodySetting.PauseDurations = _config.GetPauseLengths(durationCount);
            _serviceProvider.Engine.ProsodySetting.SpeakSessionEndPauseDuration =
                (uint)_serviceProvider.Engine.ProsodySetting.PauseDurations[(int)TtsPauseLevel.PAU_IDX_SENTENCE];
        }

        /// <summary>
        /// Service provider processing event handler.
        /// Do word update before linguist prosody tagger if necessary.
        /// </summary>
        /// <param name="sender">Sender of the event handler.</param>
        /// <param name="e">Event argument object.</param>
        private void OnLinguisticProcessing(object sender, TtsModuleEventArgs e)
        {
            if (e.ModuleType == TtsModuleType.TM_LPT_BREAK)
            {
                try
                {
                    CheckConsistency(e.Utterance, _curScriptSentence, _config.ConsistencyCheckOption);
                    _sentenceMatch = true;
                    UpdateWords(e.Utterance, _curScriptSentence, _serviceProvider.Engine);
                }
                catch (InvalidDataException exception)
                {
                    HandleException(exception);
                }
            }
        }

        /// <summary>
        /// Linguistic processing event handler.
        /// Do break, emphasis, boundary tone update after the right sub-level of acoustic prosody tagger.
        /// </summary>
        /// <param name="sender">Sender of the event handler.</param>
        /// <param name="e">Event argument object.</param>
        private void OnLinguisticProcessed(object sender, TtsModuleEventArgs e)
        {
            if (_sentenceMatch)
            {
                try
                {
                    if (e.ModuleType == SP.TtsModuleType.TM_LPT_BREAK)
                    {
                        if (_config.UpdateBreakLevel)
                        {
                            UpdateBreak(e.Utterance, _curScriptSentence);
                        }

                        if (_config.UpdateSilenceWord)
                        {
                            UpdateSilenceWords(e.Utterance, _curScriptSentence, _serviceProvider.Engine, _logger);
                        }
                    }

                    if (e.ModuleType == SP.TtsModuleType.TM_LPT_EMPHASIS)
                    {
                        if (_config.UpdateEmphasis)
                        {
                            UpdateEmphasis(e.Utterance, _curScriptSentence);
                        }
                    }

                    if (e.ModuleType == SP.TtsModuleType.TM_LPT_BOUNDARY_TONE)
                    {
                        if (_config.UpdateBoundaryTone)
                        {
                            UpdateBoundaryTone(e.Utterance, _curScriptSentence);
                        }
                    }
                }
                catch (InvalidDataException exception)
                {
                    HandleException(exception);
                }
            }
        }

        /// <summary>
        /// Service provider processed event handler.
        /// Do updates after the right process if necessary.
        /// </summary>
        /// <param name="sender">Sender of the event handler.</param>
        /// <param name="e">Event argument object.</param>
        private void OnProcessed(object sender, TtsModuleEventArgs e)
        {
            if (_sentenceMatch)
            {
                try
                {
                    if (e.ModuleType == TtsModuleType.TM_UNIT_GENERATION)
                    {
                        if (_config.UpdateToBIAccent)
                        {
                            UpdateToBIAccent(e.Utterance, _curScriptSentence);
                        }
                    }
                }
                catch (InvalidDataException exception)
                {
                    HandleException(exception);
                }
            }
        }

        /// <summary>
        /// Aoustic processing event handler.
        /// Do duration and F0 update after the right sub-level of acoustic prosody tagger.
        /// </summary>
        /// <param name="sender">Sender of the event handler.</param>
        /// <param name="e">Event argument object.</param>
        private void OnAcousticProcessed(object sender, TtsModuleEventArgs e)
        {
            if (e.ModuleType == TtsModuleType.TM_APT_DURATION && _sentenceMatch)
            {
                if (_config.UpdateNormalWordDuration || _config.UpdateF0 ||
                    _config.UpdateScriptSilenceDuration)
                {
                    try
                    {
                        GetPhonePosi(e.Utterance);

                        if (_config.UpdateNormalWordDuration || _config.UpdateF0)
                        {
                            CheckMatched(_curScriptSentence);
                        }

                        if (_config.UpdateNormalWordDuration || _config.UpdateScriptSilenceDuration)
                        {
                            DurationUpdate(e.Utterance, _curScriptSentence);
                        }

                        e.Utterance.Acoustic.RefreshFrameCount();
                    }
                    catch (InvalidDataException exception)
                    {
                        HandleException(exception);
                    }
                }
            }

            if (e.ModuleType == TtsModuleType.TM_APT_F0 && _sentenceMatch && _config.UpdateF0)
            {
                try
                {
                    F0Update(e.Utterance, _curScriptSentence);
                }
                catch (InvalidDataException exception)
                {
                    HandleException(exception);
                }
            }
        }

        /// <summary>
        /// Handle invalid data exception.
        /// </summary>
        /// <param name="exception">Invalid data exception.</param>
        private void HandleException(InvalidDataException exception)
        {
            _sentenceMatch = false;
            _invalidItemList.Add(_curScriptItem.Id);
            TraceLog(_logger, true, "Error: {0}", exception.Message);
        }

        /// <summary>
        /// Get internal normal words' phones' position.
        /// </summary>
        /// <param name="intUtt">Internal utterance.</param>
        [SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional",
            Justification = "it is not culture specific array")]
        private void GetPhonePosi(SP.TtsUtterance intUtt)
        {
            int normalWordCount = 0;
            int phoneCount = 0;

            foreach (TtsWord uttWord in intUtt.Words)
            {
                if (uttWord.IsPronounceable)
                {
                    normalWordCount++;
                }
            }

            _totalPhoneIndex = new int[normalWordCount][,];
            int normalWordIndex = 0;

            foreach (TtsWord uttWord in intUtt.Words)
            {
                int syllableCount = 0;
                if (!uttWord.IsPronounceable)
                {
                    TtsSyllable intSyllable = uttWord.FirstSyllable;
                    if (intSyllable != null)
                    {
                        while (intSyllable != uttWord.LastSyllable.Next)
                        {
                            TtsPhone intPhone = intSyllable.FirstPhone;
                            if (intPhone != null)
                            {
                                while (intPhone != intSyllable.LastPhone.Next)
                                {
                                    phoneCount++;
                                    intPhone = intPhone.Next;
                                }
                            }

                            intSyllable = intSyllable.Next;
                        }
                    }
                }
                else
                {
                    TtsSyllable intSyllable = uttWord.FirstSyllable;
                    if (intSyllable == null)
                    {
                        string message = Helper.NeutralFormat("Runtime's normal word [{0}] has no syllable.",
                                uttWord.WordText);
                        throw new InvalidDataException(message);
                    }

                    while (intSyllable != uttWord.LastSyllable.Next)
                    {
                        syllableCount++;
                        intSyllable = intSyllable.Next;
                    }

                    _totalPhoneIndex[normalWordIndex] = new int[syllableCount, 2];
                    int syllableIndex = 0;
                    intSyllable = uttWord.FirstSyllable;

                    while (intSyllable != uttWord.LastSyllable.Next)
                    {
                        TtsPhone intPhone = intSyllable.FirstPhone;
                        if (intPhone == null)
                        {
                            string message = Helper.NeutralFormat("Runtime's normal word [{0}]'s syllable [{1}] has no phone.",
                                    uttWord.WordText, intSyllable.Pronunciation);
                            throw new InvalidDataException(message);
                        }

                        _totalPhoneIndex[normalWordIndex][syllableIndex, 0] = phoneCount;
                        while (intPhone != intSyllable.LastPhone.Next)
                        {
                            phoneCount++;
                            intPhone = intPhone.Next;
                        }

                        _totalPhoneIndex[normalWordIndex][syllableIndex, 1] = phoneCount;

                        intSyllable = intSyllable.Next;
                        syllableIndex++;
                    }

                    normalWordIndex++;
                }
            }
        }

        /// <summary>
        /// Check whether the internal normal words matches the external normal words.
        /// </summary>
        /// <param name="extSentence">External script sentence.</param>
        private void CheckMatched(ScriptSentence extSentence)
        {
            if (_totalPhoneIndex.Length != extSentence.PronouncedWords.Count)
            {
                throw new InvalidDataException("Runtime's normal words' count must equal to the script's.");
            }

            for (int wordIndex = 0; wordIndex < _totalPhoneIndex.Length; wordIndex++)
            {
                int uttSyllableCount = _totalPhoneIndex[wordIndex].GetLength(0);
                if (uttSyllableCount != extSentence.PronouncedWords[wordIndex].Syllables.Count)
                {
                    string message = Helper.NeutralFormat("Script's normal word [{0}]'s syllables' count "
                        + "must equal to the runtime's.", extSentence.PronouncedWords[wordIndex].Pronunciation);
                    throw new InvalidDataException(message);
                }

                for (int syllableIndex = 0; syllableIndex < uttSyllableCount; syllableIndex++)
                {
                    int phoneCount = _totalPhoneIndex[wordIndex][syllableIndex, 1] - 
                        _totalPhoneIndex[wordIndex][syllableIndex, 0];
                    if (phoneCount != 
                        extSentence.PronouncedWords[wordIndex].Syllables[syllableIndex].Phones.Count)
                    {
                        string message = Helper.NeutralFormat("Script's normal word [{0}]'s " + 
                            "syllable [{1}]'s phones' count must equal to the runtime's.", 
                            extSentence.PronouncedWords[wordIndex].Pronunciation, 
                            extSentence.PronouncedWords[wordIndex].Syllables[syllableIndex].Text);
                        throw new InvalidDataException(message);
                    }
                }
            }
        }

        /// <summary>
        /// Do duration update if given a external duration.
        /// </summary>
        /// <param name="intUtt">Internal utterance.</param>
        /// <param name="extSentence">External script sentence.</param>
        private void DurationUpdate(SP.TtsUtterance intUtt, ScriptSentence extSentence)
        {
            // The length of each frame in millisecond.
            float frameLength = _serviceProvider.Engine.Config.SamplesPerFrame * 1000 /
                (float)_serviceProvider.Engine.Config.SamplesPerSecond;

            int normalWordIndex = 0;
            IUpdateHelper durationUpdater;
            TraceLog(_logger, true, "Updated Duration Position (Indicated by normal words index):");
            for (int wordIndex = 0; wordIndex < extSentence.Words.Count; wordIndex++)
            {
                ScriptWord scriptThisWord = extSentence.Words[wordIndex];
                if (scriptThisWord.IsPronounced)
                {
                    if (_config.UpdateNormalWordDuration)
                    {
                        int syllableIndex = 0;

                        foreach (ScriptSyllable extSyllable in scriptThisWord.Syllables)
                        {
                            int phoneIndex = 0;
                            durationUpdater = new SyllableUpdateHelper();
                            ProcessDurationUpdate(durationUpdater, intUtt, extSyllable,
                                0, phoneIndex, syllableIndex, normalWordIndex, frameLength);

                            foreach (ScriptPhone extPhone in extSyllable.Phones)
                            {
                                durationUpdater = new PhoneUpdateHelper();
                                ProcessDurationUpdate(durationUpdater, intUtt, extPhone,
                                    0, phoneIndex, syllableIndex, normalWordIndex, frameLength);

                                durationUpdater = new StateUpdateHelper();
                                UpdateStateDuration(durationUpdater, intUtt, extPhone,
                                    phoneIndex, syllableIndex, normalWordIndex, frameLength);
                                phoneIndex++;
                            }

                            syllableIndex++;
                        }
                    }

                    normalWordIndex++;
                }
                else if (scriptThisWord.WordType == WordType.Silence && _config.UpdateScriptSilenceDuration)
                {
                    durationUpdater = new SilenceUpdateHelper();
                    ProcessDurationUpdate(durationUpdater, intUtt, scriptThisWord.Syllables[0],
                        NotUpdateState, 0, 0, normalWordIndex - 1, frameLength);

                    ProcessDurationUpdate(durationUpdater, intUtt, scriptThisWord.Syllables[0].Phones[0],
                        NotUpdateState, 0, 0, normalWordIndex - 1, frameLength);

                    UpdateStateDuration(durationUpdater, intUtt, scriptThisWord.Syllables[0].Phones[0],
                        0, 0, normalWordIndex - 1, frameLength);
                }
            }

            TraceLogLine(_logger);
        }

        /// <summary>
        /// Update state duration.
        /// </summary>
        /// <param name="durationUpdater">Duration updater.</param>
        /// <param name="intUtt">Internal utterance.</param>
        /// <param name="scriptPhone">Script phone.</param>
        /// <param name="phoneIndex">External phone index.</param>
        /// <param name="syllableIndex">External syllable index.</param>
        /// <param name="wordIndex">External normal word index.</param>
        /// <param name="frameLength">The length of each frame in millisecond.</param>
        private void UpdateStateDuration(IUpdateHelper durationUpdater, SP.TtsUtterance intUtt,
            ScriptPhone scriptPhone, int phoneIndex, int syllableIndex, int wordIndex, 
            float frameLength)
        {
            int statesCount = scriptPhone.States.Count;
            if (statesCount != 0)
            {
                if (statesCount != intUtt.Acoustic.Durations.Column)
                {
                    throw new InvalidDataException("Script states' count must equal to the" + 
                        "engine's.");
                }

                for (int stateIndex = 0; stateIndex < statesCount; stateIndex++)
                {
                    ProcessDurationUpdate(durationUpdater, intUtt, scriptPhone.States[stateIndex], 
                        stateIndex, phoneIndex, syllableIndex, wordIndex, frameLength);
                }
            }
        }

        /// <summary>
        /// Do F0 update if given external F0s.
        /// </summary>
        /// <param name="intUtt">Internal utterance.</param>
        /// <param name="extSentence">External script sentence.</param>
        private void F0Update(SP.TtsUtterance intUtt, ScriptSentence extSentence)
        {
            int normalWordIndex = 0;
            TraceLog(_logger, true, "Updated F0 Position (Indicated by normal words index):");

            foreach (ScriptWord extWord in extSentence.PronouncedWords)
            {
                int syllableIndex = 0;
                IUpdateHelper f0Updater;
                foreach (ScriptSyllable extSyllable in extWord.Syllables)
                {
                    int phoneIndex = 0;
                    f0Updater = new SyllableUpdateHelper();
                    ProcessF0Update(f0Updater, intUtt, extSyllable, phoneIndex,
                        syllableIndex, normalWordIndex);

                    foreach (ScriptPhone extPhone in extSyllable.Phones)
                    {
                        f0Updater = new PhoneUpdateHelper();
                        ProcessF0Update(f0Updater, intUtt, extPhone, phoneIndex,
                            syllableIndex, normalWordIndex);

                        phoneIndex++;
                    }

                    syllableIndex++;
                }

                normalWordIndex++;
            }

            if (_config.FixF0NoConsistenceNum > 0)
            {
                FixF0NoConsistence(intUtt);
            }

            TraceLogLine(_logger);
        }

        /// <summary>
        /// Fix f0 no consistence.
        /// </summary>
        /// <param name="intUtt">Internal utterance.</param>
        private void FixF0NoConsistence(SP.TtsUtterance intUtt)
        {
            int enginePhoneIndex = 0;
            int f0Index = 0;
            foreach (TtsWord engineWord in intUtt.Words)
            {
                TtsSyllable intSyllable = engineWord.FirstSyllable;
                if (intSyllable != null)
                {
                    while (intSyllable != engineWord.LastSyllable.Next)
                    {
                        TtsPhone intPhone = intSyllable.FirstPhone;
                        if (intPhone != null)
                        {
                            while (intPhone != intSyllable.LastPhone.Next)
                            {
                                uint phoneDuration = 0;
                                for (int i = 0; i < intUtt.Acoustic.Durations.Column; i++)
                                {
                                    phoneDuration += intUtt.Acoustic.Durations[enginePhoneIndex][i];
                                }

                                int f0TempIndex = f0Index;
                                List<float> f0List = new List<float>();
                                for (int i = 0; i < phoneDuration; i++)
                                {
                                    f0List.Add(intUtt.Acoustic.F0s[f0Index][0]);
                                    f0Index++;
                                }

                                int zeroCount = 0;
                                for (int i = 0; i < f0List.Count; i++)
                                {
                                    if (f0List[i] == 0)
                                    {
                                        zeroCount++;
                                    }
                                }

                                if (zeroCount <= _config.FixF0NoConsistenceNum)
                                {
                                    if (f0List[0] == 0)
                                    {
                                        int notZeroIndex = 1;
                                        while (f0List[notZeroIndex] == 0)
                                        {
                                            notZeroIndex++;
                                        }

                                        intUtt.Acoustic.F0s[f0TempIndex][0] = f0List[notZeroIndex];
                                    }

                                    for (int i = 1; i < f0List.Count; i++)
                                    {
                                        if (f0List[i] == 0)
                                        {
                                            intUtt.Acoustic.F0s[f0TempIndex + i][0] = f0List[i - 1];
                                        }
                                    }
                                }
                                else if (zeroCount >= f0List.Count - _config.FixF0NoConsistenceNum)
                                {
                                    for (int i = 0; i < f0List.Count; i++)
                                    {
                                        if (f0List[i] != 0)
                                        {
                                            intUtt.Acoustic.F0s[f0TempIndex + i][0] = 0;
                                        }
                                    }
                                }

                                intPhone = intPhone.Next;
                                enginePhoneIndex++;
                            }
                        }

                        intSyllable = intSyllable.Next;
                    }
                }
            }
        }

        /// <summary>
        /// Process duration update.
        /// </summary>
        /// <param name="durationUpdater">Duration updater.</param>
        /// <param name="intUtt">Internal utterance.</param>
        /// <param name="holder">Script acoustic holder.</param>
        /// <param name="stateIndex">External state index.</param>
        /// <param name="phoneIndex">External phone index.</param>
        /// <param name="syllableIndex">External syllable index.</param>
        /// <param name="wordIndex">External normal word index.</param>
        /// <param name="frameLength">The length of each frame in millisecond.</param>
        private void ProcessDurationUpdate(IUpdateHelper durationUpdater, SP.TtsUtterance intUtt,
            ScriptAcousticsHolder holder, int stateIndex, int phoneIndex, int syllableIndex,
            int wordIndex, float frameLength)
        {
            if (holder.HasAcousticsValue && holder.Acoustics.HasDurationValue)
            {
                uint extDur = (uint)Math.Round(holder.Acoustics.Duration / frameLength);

                LayerIndex layerIndex = durationUpdater.FindLayerIndex(stateIndex, phoneIndex,
                    syllableIndex, wordIndex, _totalPhoneIndex);
                int phoneCount = layerIndex.EndPhone - layerIndex.StartPhone;
                if (!(layerIndex.State == NotUpdateState && (phoneCount == 0 || 
                    layerIndex.EndPhone > intUtt.Phones.Count)))
                {
                    bool durationUpdated = ExecuteUpdateDuration(intUtt, extDur, layerIndex);
                    durationUpdater.LogMessage(durationUpdated, wordIndex, syllableIndex,
                        phoneIndex, stateIndex, "duration", _logger);
                }
            }
        }

        /// <summary>
        /// Process f0 update.
        /// </summary>
        /// <param name="f0Updater">F0 updater.</param>
        /// <param name="intUtt">Internal utterance.</param>
        /// <param name="holder">Script acoustics holder.</param>
        /// <param name="phoneIndex">External phone index.</param>
        /// <param name="syllableIndex">External syllable index.</param>
        /// <param name="wordIndex">External normal word index.</param>
        private void ProcessF0Update(IUpdateHelper f0Updater, SP.TtsUtterance intUtt,
            ScriptAcousticsHolder holder, int phoneIndex, int syllableIndex, int wordIndex)
        {
            if (holder.HasAcousticsValue)
            {
                if (holder.Acoustics.UvSegs.Count > 1)
                {
                    throw new InvalidDataException("Do not support multiple UvSegs.");
                }
                else if (holder.Acoustics.UvSegs.Count == 1 && holder.Acoustics.UvSegs[0].HasF0ContourValue)
                {
                    LayerIndex layerIndex = f0Updater.FindLayerIndex(0, phoneIndex, syllableIndex,
                        wordIndex, _totalPhoneIndex);

                    float[] extNotNullF0;
                    List<int> intNotNullF0Position;
                    GetF0(intUtt, holder.Acoustics.UvSegs[0], layerIndex, out extNotNullF0,
                        out intNotNullF0Position);

                    if (extNotNullF0.Length > 0 && intNotNullF0Position.Count > 0)
                    {
                        bool f0Updated = ExecuteUpdateF0(intUtt, extNotNullF0, intNotNullF0Position);
                        f0Updater.LogMessage(f0Updated, wordIndex, syllableIndex, phoneIndex, 0,
                            "f0", _logger);
                    }
                }
            }
        }

        /// <summary>
        /// Execute update duration.
        /// </summary>
        /// <param name="intUtt">Internal utterance.</param>
        /// <param name="extTotalDur">Given exteranl duration.</param>
        /// <param name="layerIndex">Certain syllable/phone/state's position.</param>
        /// <returns>True if success, false if not.</returns>
        private bool ExecuteUpdateDuration(SP.TtsUtterance intUtt, uint extTotalDur,
            LayerIndex layerIndex)
        {
            int phoneCount = layerIndex.EndPhone - layerIndex.StartPhone;
            uint[] intDur;
            if (phoneCount == 0)
            {
                intDur = new uint[1];
                intDur[0] = intUtt.Acoustic.Durations[layerIndex.StartPhone][layerIndex.State];
            }
            else
            {
                intDur = new uint[phoneCount * (int)intUtt.Acoustic.Durations.Column];
                int duraIndex = 0;

                for (int i = layerIndex.StartPhone; i < layerIndex.EndPhone; ++i)
                {
                    for (int j = 0; j < (int)intUtt.Acoustic.Durations.Column; ++j)
                    {
                        intDur[duraIndex] = intUtt.Acoustic.Durations[i][j];
                        duraIndex++;
                    }
                }
            }

            uint exceptionTimes;
            bool calcuSuccess = AcousticReplacement.MapDuration(extTotalDur, ref intDur, out exceptionTimes);
            if (!calcuSuccess)
            {
                return false;
            }

            if (phoneCount == 0)
            {
                intUtt.Acoustic.Durations[layerIndex.StartPhone][layerIndex.State] = intDur[0];
            }
            else
            {
                int duraIndex = 0;
                for (int i = layerIndex.StartPhone; i < layerIndex.EndPhone; ++i)
                {
                    for (int j = 0; j < (int)intUtt.Acoustic.Durations.Column; ++j)
                    {
                        intUtt.Acoustic.Durations[i][j] = intDur[duraIndex];
                        duraIndex++;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Execute update f0.
        /// </summary>
        /// <param name="intUtt">Internal utterance.</param>
        /// <param name="extNotNullF0">External F0s.</param> 
        /// <param name="intNotNullF0Position">Not null F0s' phones' position.</param>
        /// <returns>Bool value indicating whether successfully update the internal F0s.</returns>
        private bool ExecuteUpdateF0(SP.TtsUtterance intUtt, float[] extNotNullF0,
            List<int> intNotNullF0Position)
        {
            float[] intNotNullF0;
            bool getNewIntF0 = AcousticReplacement.MapF0(intNotNullF0Position.Count,
                extNotNullF0, out intNotNullF0, AcousticReplacement.F0ExtrapolationMode.Copy);
            if (!getNewIntF0)
            {
                return false;
            }

            for (int i = 0; i < intNotNullF0Position.Count; i++)
            {
                if (intNotNullF0[i] <= MinF0Value)
                {
                    intUtt.Acoustic.F0s[intNotNullF0Position[i]][0] = 0;
                }
                else
                {
                    intUtt.Acoustic.F0s[intNotNullF0Position[i]][0] = (float)Math.Log(intNotNullF0[i]);
                }
            }

            return true;
        }

        /// <summary>
        /// Update word information.
        /// </summary>
        /// <param name="utt">Tts utterance.</param>
        /// <param name="scriptSentence">Script sentence.</param>
        /// <param name="engine">Tts engine.</param>
        private void UpdateWords(SP.TtsUtterance utt, ScriptSentence scriptSentence, TtsEngine engine)
        {
            if (_config.UpdatePronunciation)
            {
                UpdatePronunciation(utt, scriptSentence, engine);
            }

            if (_config.UpdatePartOfSpeech)
            {
                UpdatePartOfSpeech(utt, scriptSentence, engine);
            }
        }

        #endregion

        /// <summary>
        /// Indicate certain syllable/phone/state's position.
        /// </summary>
        public struct LayerIndex
        {
            /// <summary>
            /// The start phone's index of all phones.
            /// </summary>
            public int StartPhone;

            /// <summary>
            /// The end phone's index of all phones.
            /// </summary>
            public int EndPhone;

            /// <summary>
            /// The certain state's index of one phone's states.
            /// </summary>
            public int State;
        }

        #region Private visitor

        /// <summary>
        /// Syllable layer acoustic update helper.
        /// </summary>
        public class SyllableUpdateHelper : IUpdateHelper
        {
            /// <summary>
            /// Find the to be updated syllable/phone/state's position.
            /// </summary>
            /// <param name="stateIndex">State index.</param>
            /// <param name="phoneIndex">Phone index.</param>
            /// <param name="syllableIndex">Syllable index.</param>
            /// <param name="wordIndex">Word index.</param>
            /// <param name="totalPhoneIndex">Phone index array of sentence.</param>
            /// <returns>Certain syllable/phone/state's position.</returns>
            public LayerIndex FindLayerIndex(int stateIndex, int phoneIndex, int syllableIndex,
                int wordIndex, int[][,] totalPhoneIndex)
            {
                LayerIndex layerIndex = new LayerIndex();
                layerIndex.StartPhone = totalPhoneIndex[wordIndex][syllableIndex, 0];
                layerIndex.EndPhone = totalPhoneIndex[wordIndex][syllableIndex, 1];
                layerIndex.State = 0;

                return layerIndex;
            }

            /// <summary>
            /// Trace log message to log file.
            /// </summary>
            /// <param name="acousticUpdated">Whether updated acoustic.</param>
            /// <param name="wordIndex">Word index.</param>
            /// <param name="syllableIndex">Syllable index.</param>
            /// <param name="phoneIndex">Phone index.</param>
            /// <param name="stateIndex">State index.</param>
            /// <param name="acousticTypeString">String of updated acoustic type.</param>
            /// <param name="logger">Text logger.</param>
            public void LogMessage(bool acousticUpdated, int wordIndex, int syllableIndex,
                int phoneIndex, int stateIndex, string acousticTypeString, TextLogger logger)
            {
                if (acousticUpdated)
                {
                    TraceLog(logger, false, "{0},{1}\t", wordIndex, syllableIndex);
                }
                else
                {
                    string message = Helper.NeutralFormat("Error update " +
                        "PronouncedWords[{0}].Syllables[{1}] {2}", wordIndex, syllableIndex, acousticTypeString);
                    throw new InvalidDataException(message);
                }
            }
        }

        /// <summary>
        /// Phone layer acoustic update helper.
        /// </summary>
        public class PhoneUpdateHelper : IUpdateHelper
        {
            /// <summary>
            /// Find the to be updated syllable/phone/state's position.
            /// </summary>
            /// <param name="stateIndex">State index.</param>
            /// <param name="phoneIndex">Phone index.</param>
            /// <param name="syllableIndex">Syllable index.</param>
            /// <param name="wordIndex">Word index.</param>
            /// <param name="totalPhoneIndex">Phone index array of sentence.</param>
            /// <returns>Certain syllable/phone/state's position.</returns>
            public LayerIndex FindLayerIndex(int stateIndex, int phoneIndex, int syllableIndex,
                int wordIndex, int[][,] totalPhoneIndex)
            {
                LayerIndex layerIndex = new LayerIndex();
                layerIndex.StartPhone = totalPhoneIndex[wordIndex][syllableIndex, 0] + phoneIndex;
                layerIndex.EndPhone = layerIndex.StartPhone + 1;
                layerIndex.State = 0;

                return layerIndex;
            }

            /// <summary>
            /// Trace log message to log file.
            /// </summary>
            /// <param name="acousticUpdated">Whether updated acoustic.</param>
            /// <param name="wordIndex">Word index.</param>
            /// <param name="syllableIndex">Syllable index.</param>
            /// <param name="phoneIndex">Phone index.</param>
            /// <param name="stateIndex">State index.</param>
            /// <param name="acousticTypeString">String of updated acoustic type.</param>
            /// <param name="logger">Text logger.</param>
            public void LogMessage(bool acousticUpdated, int wordIndex, int syllableIndex,
                int phoneIndex, int stateIndex, string acousticTypeString, TextLogger logger)
            {
                if (acousticUpdated)
                {
                    TraceLog(logger, false, "{0},{1},{2}\t", wordIndex, syllableIndex, phoneIndex);
                }
                else
                {
                    string message = Helper.NeutralFormat("Error update " +
                        "PronouncedWords[{0}].Syllables[{1}].Phones[{2}] {3}", wordIndex,
                        syllableIndex, phoneIndex, acousticTypeString);
                    throw new InvalidDataException(message);
                }
            }
        }

        /// <summary>
        /// State layer acoustic update helper.
        /// </summary>
        public class StateUpdateHelper : IUpdateHelper
        {
            /// <summary>
            /// Find the to be updated syllable/phone/state's position.
            /// </summary>
            /// <param name="stateIndex">State index.</param>
            /// <param name="phoneIndex">Phone index.</param>
            /// <param name="syllableIndex">Syllable index.</param>
            /// <param name="wordIndex">Word index.</param>
            /// <param name="totalPhoneIndex">Phone index array of sentence.</param>
            /// <returns>Certain syllable/phone/state's position.</returns>
            public LayerIndex FindLayerIndex(int stateIndex, int phoneIndex, int syllableIndex,
                int wordIndex, int[][,] totalPhoneIndex)
            {
                LayerIndex layerIndex = new LayerIndex();
                layerIndex.StartPhone = totalPhoneIndex[wordIndex][syllableIndex, 0] + phoneIndex;
                layerIndex.EndPhone = layerIndex.StartPhone;
                layerIndex.State = stateIndex;

                return layerIndex;
            }

            /// <summary>
            /// Trace log message to log file.
            /// </summary>
            /// <param name="acousticUpdated">Whether updated acoustic.</param>
            /// <param name="wordIndex">Word index.</param>
            /// <param name="syllableIndex">Syllable index.</param>
            /// <param name="phoneIndex">Phone index.</param>
            /// <param name="stateIndex">State index.</param>
            /// <param name="acousticTypeString">String of updated acoustic type.</param>
            /// <param name="logger">Text logger.</param>
            public void LogMessage(bool acousticUpdated, int wordIndex, int syllableIndex,
                int phoneIndex, int stateIndex, string acousticTypeString, TextLogger logger)
            {
                if (acousticUpdated)
                {
                    TraceLog(logger, false, "{0},{1},{2},{3}\t", wordIndex, syllableIndex, phoneIndex,
                        stateIndex);
                }
                else
                {
                    string message = Helper.NeutralFormat("Error update " +
                        "PronouncedWords[{0}].Syllables[{1}].Phones[{2}].States[{3}] {4}", wordIndex,
                        syllableIndex, phoneIndex, stateIndex, acousticTypeString);
                    throw new InvalidDataException(message);
                }
            }
        }

        /// <summary>
        /// Silence layer acoustic update helper.
        /// </summary>
        public class SilenceUpdateHelper : IUpdateHelper
        {
            /// <summary>
            /// Find the to be updated syllable/phone/state's position.
            /// </summary>
            /// <param name="stateIndex">State index.</param>
            /// <param name="phoneIndex">Phone index.</param>
            /// <param name="syllableIndex">Syllable index.</param>
            /// <param name="wordIndex">Word index.</param>
            /// <param name="totalPhoneIndex">Phone index array of sentence.</param>
            /// <returns>Certain syllable/phone/state's position.</returns>
            public LayerIndex FindLayerIndex(int stateIndex, int phoneIndex, int syllableIndex,
                int wordIndex, int[][,] totalPhoneIndex)
            {
                LayerIndex layerIndex = new LayerIndex();
                if (wordIndex >= 0)
                {
                    layerIndex.StartPhone =
                        totalPhoneIndex[wordIndex][totalPhoneIndex[wordIndex].GetLength(0) - 1, 1];
                }

                if (wordIndex < totalPhoneIndex.GetLength(0) - 1)
                {
                    layerIndex.EndPhone = totalPhoneIndex[wordIndex + 1][0, 0];
                }
                else
                {
                    layerIndex.EndPhone = layerIndex.StartPhone + 1;
                }

                layerIndex.State = stateIndex;
                if (layerIndex.StartPhone == layerIndex.EndPhone)
                {
                    layerIndex.State = NotUpdateState;
                }
                else if (stateIndex != NotUpdateState)
                {
                    layerIndex.EndPhone = layerIndex.StartPhone;
                }

                return layerIndex;
            }

            /// <summary>
            /// Trace log message to log file.
            /// </summary>
            /// <param name="acousticUpdated">Whether updated acoustic.</param>
            /// <param name="wordIndex">Word index.</param>
            /// <param name="syllableIndex">Syllable index.</param>
            /// <param name="phoneIndex">Phone index.</param>
            /// <param name="stateIndex">State index.</param>
            /// <param name="acousticTypeString">String of updated acoustic type.</param>
            /// <param name="logger">Text logger.</param>
            public void LogMessage(bool acousticUpdated, int wordIndex, int syllableIndex,
                int phoneIndex, int stateIndex, string acousticTypeString, TextLogger logger)
            {
                if (acousticUpdated)
                {
                    if (stateIndex == NotUpdateState)
                    {
                        TraceLog(logger, false, "(sil){0},{1},{2}\t", wordIndex,
                            syllableIndex, phoneIndex);
                    }
                    else
                    {
                        TraceLog(logger, false, "(sil){0},{1},{2},{3}\t", wordIndex,
                            syllableIndex, phoneIndex, stateIndex);
                    }
                }
                else
                {
                    string message = Helper.NeutralFormat("Error update " +
                        "PronouncedWords[{0}]'s silence word's {1}", wordIndex,
                        acousticTypeString);
                    throw new InvalidDataException(message);
                }
            }
        }

        #endregion
    }
}
