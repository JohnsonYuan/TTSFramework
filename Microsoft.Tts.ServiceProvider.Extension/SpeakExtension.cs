//----------------------------------------------------------------------------
// <copyright file="SpeakExtension.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// SpeakExtension class.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.ServiceProvider.Extension
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// SpeakExtension class.
    /// </summary>
    public static class SpeakExtension
    {
        #region Field

        /// <summary>
        /// A fake word to speak, the pronunciation will be changed in processing.
        /// </summary>
        private const string FakeSingleWord = "test";

        #endregion

        #region Public Methods

        /// <summary>
        /// Speak pronunciation like "t eh s t".
        /// Call Example:
        /// TtsEngineSetting setting = new TtsEngineSetting(Language.EnUS);
        /// Setting.LangDataPath = "MSTTSLocEnUS.dat";
        /// Setting.VoicePath = "HelenT";
        /// ServiceProvider sp = new ServiceProvider(setting);
        /// Sp.SpeechSynthesizer.SetOutputToDefaultAudioDevice();
        /// Sp.SpeakPronunciaton("t eh s t");.
        /// </summary>
        /// <param name="sp">ServiceProvider instance.</param>
        /// <param name="pronunciation">Pronunciation parameter.</param>
        public static void SpeakPronunciation(this ServiceProvider sp, string pronunciation)
        {
            string phoneIds = string.Empty;
            try
            {
                phoneIds = sp.Engine.Phoneme.PronunciationToPhoneIds(pronunciation);
            }
            catch (EspException ex)
            {
                throw new InvalidDataException("Invalid Pronunciation: " + pronunciation, ex);
            }

            EventHandler<TtsModuleEventArgs> modifyPronunciationHandler
                = delegate(object sender, TtsModuleEventArgs e)
                {
                    if (e.ModuleType == TtsModuleType.TM_TEXT_ANALYZER && e.Utterance.Words.Count > 0)
                    {
                        TtsWord targetWord = GetFirstNonSilenceWord(e.Utterance);
                        Debug.Assert(targetWord != null, "TtsWord should not be null");
                        targetWord.PhoneIds = phoneIds;
                    }
                };

            try
            {
                sp.Engine.Processed += new EventHandler<TtsModuleEventArgs>(modifyPronunciationHandler);
                sp.SpeechSynthesizer.Speak(FakeSingleWord);
            }
            finally
            {
                sp.Engine.Processed -= new EventHandler<TtsModuleEventArgs>(modifyPronunciationHandler);
            }
        }

        public static UtteranceUpdatesSet SpeakUpdatedUtterance(
            this ServiceProvider sp, TtsUtterance utterance, UtteranceUpdatesSet updates)
        {
            UtteranceUpdatesSet replacedOldValues = new UtteranceUpdatesSet();
            EventHandler<TtsModuleEventArgs> acousticProsodyTaggerProcessedHandler
                    = delegate(object sender, TtsModuleEventArgs e)
                    {
                        TtsModuleType module = e.ModuleType;
                        if (updates.ContainsKey(module))
                        {
                            switch (module)
                            {
                                case TtsModuleType.TM_APT_DURATION:
                                    {
                                        MemoryMatrix<uint> durations = (MemoryMatrix<uint>)updates[TtsModuleType.TM_APT_DURATION];
                                        MemoryMatrix<uint> dstDurations = e.Utterance.Acoustic.Durations;
                                        replacedOldValues.Add(TtsModuleType.TM_APT_DURATION, dstDurations.Duplicate());
                                        Debug.Assert(
                                            durations.Column == dstDurations.Column &&
                                            durations.Row == dstDurations.Row);
                                        for (int i = 0; i < durations.Row; i++)
                                        {
                                            for (int j = 0; j < dstDurations.Column; j++)
                                            {
                                                if (durations[i][j] != uint.MaxValue)
                                                {
                                                    dstDurations[i][j] = durations[i][j];
                                                }
                                            }
                                        }

                                        updates[TtsModuleType.TM_APT_DURATION] = dstDurations.Duplicate();
                                        e.Utterance.Acoustic.RefreshFrameCount();
                                    }

                                    break;
                                case TtsModuleType.TM_APT_F0:
                                    {
                                        MemoryMatrix<float> f0s = (MemoryMatrix<float>)updates[TtsModuleType.TM_APT_F0];
                                        MemoryMatrix<float> dstF0s = e.Utterance.Acoustic.F0s;

                                        replacedOldValues.Add(TtsModuleType.TM_APT_F0, dstF0s.Duplicate());
                                        Debug.Assert(
                                            f0s.Column == dstF0s.Column &&
                                            f0s.Row == dstF0s.Row);
                                        for (int i = 0; i < f0s.Row; i++)
                                        {
                                            if (!float.IsNaN(f0s[i][0]))
                                            {
                                                dstF0s[i][0] = f0s[i][0];
                                            }
                                        }

                                        updates[TtsModuleType.TM_APT_F0] = dstF0s.Duplicate();
                                    }

                                    break;
                                case TtsModuleType.TM_APT_GAIN:
                                    {
                                        MemoryMatrix<float> gains = (MemoryMatrix<float>)updates[TtsModuleType.TM_APT_GAIN];
                                        MemoryMatrix<float> dstGains = e.Utterance.Acoustic.Gains;
                                        replacedOldValues.Add(TtsModuleType.TM_APT_GAIN, dstGains.Duplicate());
                                        Debug.Assert(
                                            gains.Column == dstGains.Column &&
                                            gains.Row == dstGains.Row);
                                        for (int i = 0; i < gains.Row; i++)
                                        {
                                            if (!float.IsNaN(gains[i][0]))
                                            {
                                                dstGains[i][0] = gains[i][0];
                                            }
                                        }

                                        updates[TtsModuleType.TM_APT_GAIN] = dstGains.Duplicate();
                                    }

                                    break;
                                case TtsModuleType.TM_APT_LSF:
                                    {
                                        MemoryMatrix<float> lsfs = (MemoryMatrix<float>)updates[TtsModuleType.TM_APT_LSF];
                                        MemoryMatrix<float> dstLsfs = e.Utterance.Acoustic.Lsfs;
                                        replacedOldValues.Add(TtsModuleType.TM_APT_LSF, dstLsfs.Duplicate());
                                        Debug.Assert(
                                            lsfs.Column == dstLsfs.Column &&
                                            lsfs.Row == dstLsfs.Row);
                                        for (int i = 0; i < lsfs.Row; i++)
                                        {
                                            for (int j = 0; j < lsfs.Column; j++)
                                            {
                                                if (!float.IsNaN(lsfs[i][j]))
                                                {
                                                    dstLsfs[i][j] = lsfs[i][j];
                                                }
                                            }
                                        }

                                        updates[TtsModuleType.TM_APT_LSF] = dstLsfs.Duplicate();
                                    }

                                    break;
                                default:
                                    throw new System.NotSupportedException("Not supported tts module type");
                            }
                        }
                    };

            UtteranceUpdatesSet deletedCandidates = new UtteranceUpdatesSet();
            EventHandler<TtsModuleEventArgs> unitSelectionHandler
                    = delegate(object sender, TtsModuleEventArgs e)
                    {
                        TtsModuleType module = e.ModuleType;
                        if (updates.ContainsKey(module))
                        {
                            switch (module)
                            {
                                case TtsModuleType.TM_UNIT_SELECTION:
                                    {
                                        TtsUnitLattice lattice = e.Utterance.UnitLattice;
                                        List<WaveUnitCostList> wucList = lattice.WucList;
                                        List<KeyValuePair<int, int>> deletedCandidatesInfoList = (List<KeyValuePair<int, int>>)updates[TtsModuleType.TM_UNIT_SELECTION];
                                        foreach (KeyValuePair<int, int> kvp in deletedCandidatesInfoList)
                                        {
                                            WaveUnitCostList unitCostlst = wucList[kvp.Key];
                                            unitCostlst.RemoveAt(kvp.Value);
                                        }
                                    }

                                    break;
                                default:
                                    throw new System.NotSupportedException("Not supported tts module type");
                            }
                        }
                    };
            try
            {
                sp.Engine.AcousticProsodyTagger.Processed += acousticProsodyTaggerProcessedHandler;
                sp.Engine.Processing += unitSelectionHandler;
                sp.SpeechSynthesizer.Speak(utterance.OriginalText);
            }
            finally
            {
                sp.Engine.AcousticProsodyTagger.Processed -= acousticProsodyTaggerProcessedHandler;
                sp.Engine.Processing -= unitSelectionHandler;
            }

            return replacedOldValues;
        }

        #endregion

        #region Private Method

        /// <summary>
        /// Get the first non silence word from utterance.
        /// </summary>
        /// <param name="utterance">TTSUtterance instance.</param>
        /// <returns>Non silence word.</returns>
        private static TtsWord GetFirstNonSilenceWord(TtsUtterance utterance)
        {
            if (utterance == null)
            {
                throw new ArgumentNullException("Utterance should not be null");
            }

            TtsWord firstNonSilenceWord = null;
            foreach (TtsWord word in utterance.Words)
            {
                if (!word.IsSilence)
                {
                    firstNonSilenceWord = word;
                    break;
                }
            }

            return firstNonSilenceWord;
        }

        #endregion
    }

    /// <summary>
    /// Just for typedef for Dictionary.
    /// </summary>
    [Serializable]
    public class UtteranceUpdatesSet : Dictionary<TtsModuleType, object>
    {
        public UtteranceUpdatesSet()
        {
        }

        protected UtteranceUpdatesSet(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}