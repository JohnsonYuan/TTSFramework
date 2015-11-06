//----------------------------------------------------------------------------
// <copyright file="UtteranceToXML.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     This module dump the data in the TTSUtterance to the xml
// </summary>
//----------------------------------------------------------------------------
namespace Microsoft.Tts.ServiceProvider.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;
    using SP = Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// Define the method to dump the data in the utterance to XmlDocument.
    /// </summary>
    public static class UtteranceExtension
    {
        /// <summary>
        /// The begin id of the script items.
        /// </summary>
        private const ulong BeginId = 1;

        /// <summary>
        /// The id length of the script item.
        /// </summary>
        private const uint IdLength = 10;

        /// <summary>
        /// The duration time (millisecond) in one frame.
        /// </summary>
        private const int MillisecondsPerFrame = 5;

        /// <summary>
        /// The pronunciation of the silence.
        /// </summary>
        private const string PronOfSilence = "-SIL-";

        /// <summary>
        /// The pronunciation of the short pause.
        /// </summary>
        private const string PronOfShortPause = "-SP-";

        /// <summary>
        /// Define the method to dump the utterance to XML.
        /// </summary>
        /// <param name="utt">The utterance for dumpping.</param>
        /// <param name="ttsEngine">The object ttsEngine to help to convert the Pos and get sentence id.</param>
        /// <returns>An XmlScriptFile object.</returns>
        /// <exception cref="InvalideDataException">Empty utt word text.</exception>
        public static XmlScriptFile ToXml(this SP.TtsUtterance utt, SP.TtsEngine ttsEngine)
        {
            if (ttsEngine == null)
            {
                throw new ArgumentNullException("ttsEngine");
            }

            XmlScriptFile script = new XmlScriptFile();
            script.Encoding = Encoding.Unicode;
            script.Language = GetLanguage(utt);
            ScriptItem item = utt.ToScriptItem(ttsEngine);
            script.Items.Add(item);
            return script;
        }

        /// <summary>
        /// Dump the data in the utterance to a script item.
        /// </summary>
        /// <param name="utt">The utterance for dumpping.</param>
        /// <param name="ttsEngine">The object ttsEngine to help to convert the Pos and get sentence id.</param>
        /// <returns>A script item object.</returns>
        public static ScriptItem ToScriptItem(this SP.TtsUtterance utt, SP.TtsEngine ttsEngine)
        {
            if (ttsEngine == null)
            {
                throw new ArgumentNullException("ttsEngine");
            }

            ScriptItem item = new ScriptItem();
            item.Text = utt.OriginalText;
            item.Id = Helper.NeutralFormat("{0:D" + IdLength + "}", BeginId);
            ScriptSentence sentence = new ScriptSentence();
            sentence.SentenceType = (SentenceType)utt.SentenceType;
            DumpWords(sentence, utt, ttsEngine, GetLanguage(utt));
            sentence.Text = sentence.BuildTextFromWords();
            item.Sentences.Add(sentence);
            return item;
        }

        /// <summary>
        /// Dump the data in the words.
        /// </summary>
        /// <param name="sentence">The script sentence which to store the data dumped from the words.</param>
        /// <param name="utt">The utterance.</param>
        /// <param name="ttsEngine">The object ttsEngine to help to convert the Pos and get sentence id.</param>
        /// <param name="scriptLanguage">The language of the script.</param>
        private static void DumpWords(ScriptSentence sentence, SP.TtsUtterance utt,
            SP.TtsEngine ttsEngine, Language scriptLanguage)
        {
            Debug.Assert(sentence != null, "Sentence should not be null");
            Debug.Assert(utt != null, "Utt should not be null");
            Debug.Assert(ttsEngine != null, "ttsEngine should not be null");

            // Phone index to mark the phone in the Utt.Phones
            int phoneIndex = 0;

            // F0 index to mark the start position in the Utt.Sccoustic.F0s
            int f0StartIndex = 0;

            // Unit index to mark the unit in the Utt.Units
            int unitIndex = 0;

            // Word index to mark the position in the Utt.Words
            int wordIndex = 0;

            foreach (SP.TtsWord word in utt.Words)
            {
                if (word.WordText != null)
                {
                    ScriptWord scriptWord = new ScriptWord();

                    // Tag the language to the word level if there is not single language in the utt.
                    // The major language (the most word count with this language) will be tag on the 
                    // script level, others tag on the word level. 
                    if ((Language)word.LangId != scriptLanguage)
                    {
                        scriptWord.Language = (Language)word.LangId;
                    }

                    // According to the schema, if the word is "silence", there should be not
                    // value in the scriptWord pronunciation. Means: <w v=""
                    if (word.WordType != TtsWordType.WT_SILENCE)
                    {
                        scriptWord.Grapheme = word.WordText;
                    }

                    if (!string.IsNullOrEmpty(word.Pronunciation))
                    {
                        scriptWord.Pronunciation = word.Pronunciation.ToLowerInvariant();
                    }

                    scriptWord.WordType = ConvertWordType(word);

                    // Dump the Part-Of-Speech.
                    // If the word is "sil", the word text is " ", the pos id is 65535, out of boundary.
                    // In this case, will not dump the pos.
                    if (!string.IsNullOrEmpty(word.WordText.Trim()))
                    {
                        scriptWord.PosString = ttsEngine.PosTable.IdToString(word.Pos);
                    }

                    scriptWord.Break = (TtsBreak)word.BreakLevel;
                    scriptWord.Emphasis = (TtsEmphasis)word.Emphasis;
                    scriptWord.TobiFinalBoundaryTone = ConvertTobiFBT(word.ToBIFinalBoundaryTone);
                    scriptWord.PronSource = (TtsPronSource)word.PronSource;
                    scriptWord.OffsetInString = (int)word.TextOffset;
                    scriptWord.LengthInString = (int)word.TextLength;
                    DumpSyllables(scriptWord, utt, word, ref phoneIndex, ref unitIndex, ref f0StartIndex, ttsEngine);
                    sentence.Words.Add(scriptWord);
                }
                else
                {
                    string message = Helper.NeutralFormat("The word text of word [{0}]: \"{1}\" in the" +
                        "utterance is empty.", wordIndex, word.WordText);
                    throw new InvalidDataException(message);
                }

                wordIndex++;
            }
        }

        /// <summary>
        /// Dump the data in the syllable.
        /// </summary>
        /// <param name="scriptWord">The script word to store the data dumped from the syllables.</param>
        /// <param name="utt">The utterance.</param>
        /// <param name="word">The word which contains the these syllables.</param>
        /// <param name="phoneIndex">Phone index to mark the phone in the Utt.Phones.</param>
        /// <param name="unitIndex">Unit index to mark the unit in the Utt.Units.</param>
        /// <param name="f0StartIndex">F0 index to mark the start position in the F0s.</param>
        /// <param name="ttsEngine">The object ttsEngine to help to convert the Pos and get sentence id.</param>
        private static void DumpSyllables(ScriptWord scriptWord, SP.TtsUtterance utt,
            SP.TtsWord word, ref int phoneIndex, ref int unitIndex, ref int f0StartIndex, SP.TtsEngine ttsEngine)
        {
            Debug.Assert(scriptWord != null, "ScriptWord should not be null");
            Debug.Assert(utt != null, "Utt should not be null");
            Debug.Assert(word != null, "Word should not be null");
            Debug.Assert(phoneIndex >= 0, "PhoneIndex should not be less than 0");
            Debug.Assert(f0StartIndex >= 0, "f0StartIndex should not be less than 0");
            Debug.Assert(ttsEngine != null, "ttsEngine should not be null");

            // Go through each syllable in the word.
            SP.TtsSyllable syllable = word.FirstSyllable;
            while (syllable != null)
            {
                ScriptSyllable scriptSyllable = new ScriptSyllable();
                TtsTobiAccentSet tobiAccentSet = new TtsTobiAccentSet();
                if (syllable.ToBIAccent != SP.TtsTobiAccent.K_NOACC)
                {
                    scriptSyllable.TobiPitchAccent = TobiLabel.Create(tobiAccentSet.IdItems[(uint)syllable.ToBIAccent]);
                }

                scriptSyllable.Stress = (TtsStress)syllable.Stress;
                DumpPhones(scriptSyllable, utt, syllable, ref phoneIndex, ref unitIndex, ref f0StartIndex, ttsEngine);
                scriptWord.Syllables.Add(scriptSyllable);
                if (syllable == word.LastSyllable)
                {
                    break;
                }

                syllable = syllable.Next;
            }
        }

        /// <summary>
        /// Dump the data in the phone.
        /// </summary>
        /// <param name="scriptSyllable">The script syllable to store the data dumped from the phones.</param>
        /// <param name="utt">The utterance.</param>
        /// <param name="syllable">The syllable which contains these phones.</param>
        /// <param name="phoneIndex">Phone index to mark the phone in the Utt.Phones.</param>
        /// <param name="unitIndex">Unit index to mark the unit in the Utt.Units.</param>
        /// <param name="f0StartIndex">F0 index to mark the start position in the F0s.</param>
        /// <param name="ttsEngine">The object ttsEngine to help to convert the Pos and get sentence id.</param>
        private static void DumpPhones(ScriptSyllable scriptSyllable, SP.TtsUtterance utt,
            SP.TtsSyllable syllable, ref int phoneIndex, ref int unitIndex, ref int f0StartIndex, SP.TtsEngine ttsEngine)
        {
            Debug.Assert(scriptSyllable != null, "ScriptSyllable should not be null");
            Debug.Assert(utt != null, "Utt should not be null");
            Debug.Assert(syllable != null, "Syllable should not be null");
            Debug.Assert(phoneIndex >= 0, "PhoneIndex should not be less than 0");
            Debug.Assert(f0StartIndex >= 0, "f0StartIndex should not be less than 0");
            Debug.Assert(ttsEngine != null, "ttsEngine should not be null");

            WuiManager wuiManager = null;
            if (utt.Segments.Count > 0)
            {
                int bestNodeIndex = (int)utt.UnitLattice.WucList[unitIndex].BestNodeIndex;
                wuiManager = ttsEngine.RUSVoiceDataManager.GetWuiManagerByUnitCostNode(utt.UnitLattice.WucList[unitIndex].WucNodeList[bestNodeIndex]);
            }

            // Go through each phone in the syllable.
            SP.TtsPhone phone = syllable.FirstPhone;
            while (phone != null)
            {
                // Dump the pronunciation of the phone.
                string phonePronunciation = Pronunciation.RemoveStress(phone.Pronunciation.ToLowerInvariant()).Trim();

                // Remove the tone from the phone pronunciation if it exist.
                if (phone.Tone != 0)
                {
                    phonePronunciation = Pronunciation.RemoveTone(phonePronunciation).Trim();
                }

                ScriptPhone scriptPhone = new ScriptPhone(phonePronunciation);
                scriptPhone.Tone = phone.Tone.ToString();
                scriptPhone.Stress = (TtsStress)phone.Stress;

                if (phone.Pronunciation != PronOfSilence)
                {
                    if (wuiManager != null)
                    {
                        scriptPhone.SentenceId = wuiManager.GetSentenceId(utt.Segments[unitIndex].WaveUnitInfo);
                    }

                    if (phone.Unit != null)
                    {
                        scriptPhone.UnitIndex = (int)phone.Unit.UnitIndex;
                    }
                }

                scriptPhone.Acoustics = new ScriptAcoustics();

                // Dump the segments.
                if (utt.Segments.Count > 0 && !utt.Segments[unitIndex].Unit.UnitText.Equals(PronOfSilence)
                    && !utt.Segments[unitIndex].Unit.UnitText.Equals(PronOfShortPause))
                {
                    scriptPhone.Acoustics.Duration = (int)utt.Segments[unitIndex].WaveUnitInfo.WaveLength + (int)utt.Segments[unitIndex + 1].WaveUnitInfo.WaveLength;
                    int segStart = (int)utt.Segments[unitIndex].WaveUnitInfo.RecordingWaveStartPosition;
                    int segEnd = segStart + (int)utt.Segments[unitIndex].WaveUnitInfo.WaveLength;
                    scriptPhone.Acoustics.SegmentIntervals.Add(new SegmentInterval(segStart, segEnd));
                    segStart = (int)utt.Segments[unitIndex + 1].WaveUnitInfo.RecordingWaveStartPosition;
                    segEnd = segStart + (int)utt.Segments[unitIndex + 1].WaveUnitInfo.WaveLength;
                    scriptPhone.Acoustics.SegmentIntervals.Add(new SegmentInterval(segStart, segEnd));
                }

                // Relative begin position of the uvsegment interval.
                int relativeBegin = 0;

                // Relative end position of the uvsegment interval.
                int relativeEnd = 0;

                // When go through the F0 values, this valuie to identify if meet the first voiced segment. 
                bool reBeginPositionFindOut = false;

                // Check if all the F0 values in one state are equals to 0. If yes, don't write down the uvseg.
                bool isF0ValueExist = false;

                // Dump the durations and F0s in each state. 
                if (utt.Acoustic.Durations != null)
                {
                    for (int i = 0; i < utt.Acoustic.Durations[phoneIndex].Length; ++i)
                    {
                        ScriptState scriptState = new ScriptState();

                        // Dump duration
                        int durationInFrame = (int)utt.Acoustic.Durations[phoneIndex][i];
                        scriptState.Acoustics = new ScriptAcoustics(durationInFrame * MillisecondsPerFrame);

                        // Dump F0s
                        if (utt.Acoustic.F0s != null)
                        {
                            ScriptUvSeg scriptUvSeg = GetF0Contour(utt, f0StartIndex, durationInFrame, ScriptAcousticChunkEncoding.Text,
                                ref relativeBegin, ref relativeEnd, ref reBeginPositionFindOut, ref isF0ValueExist);
                            if (isF0ValueExist == true)
                            {
                                scriptState.Acoustics.UvSegs.Add(scriptUvSeg);
                            }

                            f0StartIndex += durationInFrame;
                        }

                        scriptPhone.States.Add(scriptState);
                    }
                }

                // Dump the uvsegment relative interval.
                if (utt.Acoustic.F0s != null && !phone.Pronunciation.Equals(PronOfSilence)
                    && !phone.Pronunciation.Equals(PronOfShortPause))
                {
                    ScriptUvSeg uvSegForRelativeInterval = new ScriptUvSeg(ScriptUvSegType.Mixed);
                    uvSegForRelativeInterval.Interval = new ScriptUvSegInterval(relativeBegin * 5, relativeEnd * 5);
                    scriptPhone.Acoustics.UvSegs.Add(uvSegForRelativeInterval);
                }

                phoneIndex++;
                unitIndex++;
                if (wuiManager != null &&
                    !phone.Pronunciation.Equals(PronOfSilence) &&
                    !phone.Pronunciation.Equals(PronOfShortPause))
                {
                    // if it is not an silence phone, the according unit must be an half phone unit, 
                    // we need skip the right half phone to move next phone's unit
                    unitIndex++;
                }

                scriptSyllable.Phones.Add(scriptPhone);

                if (phone == syllable.LastPhone)
                {
                    break;
                }

                phone = phone.Next;
            }
        }

        /// <summary>
        /// Get the language from the utterance.
        /// </summary>
        /// <param name="utt">The utterance.</param>
        /// <returns>Enum: Microsoft.Tts.Offline.language.</returns>
        private static Language GetLanguage(SP.TtsUtterance utt)
        {
            Debug.Assert(utt != null, "Utt should not be null");

            Language language = Language.Neutral;

            if (utt.Words.Count > 0)
            {
                ushort langId = utt.Words[0].LangId;

                // Save the language and the count of the word with this language to a dictionary.
                Dictionary<ushort, int> langDic = new Dictionary<ushort, int>();
                foreach (SP.TtsWord word in utt.Words)
                {
                    if (langDic.ContainsKey(word.LangId))
                    {
                        langDic[word.LangId]++;
                    }
                    else
                    {
                        langDic.Add(word.LangId, 1);
                    }
                }

                // Mutiple language in the utterance, save the major language(with the most word count) as 
                // script language, tag the other language on the word attribute. 
                if (langDic.Count > 1)
                {
                    int maxWordLangCount = 0;
                    foreach (KeyValuePair<ushort, int> lang in langDic)
                    {
                        if (lang.Value > maxWordLangCount)
                        {
                            maxWordLangCount = lang.Value;
                            langId = lang.Key;
                        }
                    }
                }

                language = (Language)langId;
            }

            return language;
        }

        /// <summary>
        /// Convert the word type from SP.TtsWord.WordType to Offline.WordType.
        /// </summary>
        /// <param name="ttsword">The SP.TtsWord object.</param>
        /// <returns>Enum: Offline.WordType.</returns>
        private static WordType ConvertWordType(SP.TtsWord ttsword)
        {
            Debug.Assert(ttsword != null, "Ttsword should not be null");

            WordType wordType;
            switch (ttsword.WordType)
            {
                case SP.TtsWordType.WT_PUNCTUATION:
                    wordType = WordType.Punctuation;
                    break;
                case SP.TtsWordType.WT_SILENCE:
                    wordType = WordType.Silence;
                    break;
                case SP.TtsWordType.WT_SPELLOUT:
                    wordType = WordType.Spell;
                    break;
                case SP.TtsWordType.WT_BOOKMARK:
                    wordType = WordType.Bookmark;
                    break;
                default:
                    wordType = WordType.Normal;
                    break;
            }

            return wordType;
        }

        /// <summary>
        /// Get the F0s in the state.
        /// </summary>
        /// <param name="utt">Utterance which will provide the F0 values.</param>
        /// <param name="f0StartIndex">The start index to get the F0.</param>
        /// <param name="duration">The duration value in the state.</param>
        /// <param name="f0EncodingMode">The F0 encoding mode, like "text", "hexBinary", etc.</param>
        /// <param name="relativeBegin">The begin position of the voice segment.</param>
        /// <param name="relativeEnd">The end position of the voice segment.</param>
        /// <param name="reBeginPositionFindOut">The bool value to mark if arrive the first voice segment.</param>
        /// <param name="isF0ValueExist">The bool value to mark if the F0 value exist, means not all equal to 0.</param>
        /// <returns>Object ScriptUvSeg.</returns>
        private static ScriptUvSeg GetF0Contour(SP.TtsUtterance utt, int f0StartIndex,
            int duration, ScriptAcousticChunkEncoding f0EncodingMode, ref int relativeBegin,
            ref int relativeEnd, ref bool reBeginPositionFindOut, ref bool isF0ValueExist)
        {
            Debug.Assert(utt != null, "Utt should not be null");
            Debug.Assert(f0StartIndex >= 0, "f0StartIndex should not be less than 0");
            Debug.Assert(duration > 0, "Duration should not be less than 0");
            Debug.Assert(relativeBegin >= 0, "relativeBegin should not be less than 0");
            Debug.Assert(relativeEnd >= 0, "relativeEnd should not be less than 0");

            ScriptUvSeg scriptUvSeg = new ScriptUvSeg();
            scriptUvSeg.SegType = ScriptUvSegType.Mixed;
            scriptUvSeg.F0Contour = new ScriptF0Contour();
            scriptUvSeg.F0Contour.ChunkEncoding = f0EncodingMode;
            int f0EndIndex = f0StartIndex + duration;

            for (int i = f0StartIndex; i < f0EndIndex; i++)
            {
                float f0 = utt.Acoustic.F0s[i][0];

                if (f0 == 0)
                {
                    if (reBeginPositionFindOut == false)
                    {
                        relativeBegin++;
                        relativeEnd++;
                    }
                }
                else
                {
                    isF0ValueExist = true;
                    reBeginPositionFindOut = true;
                    relativeEnd++;
                    scriptUvSeg.F0Contour.Contour.Add(f0);
                }
            }

            return scriptUvSeg;
        }

        /// <summary>
        /// Get the tobi final boundary tone tag in the script.
        /// </summary>
        /// <param name="tobiBoundary">SP.TtsTobiBoundaryTone tobiBoundary.</param>
        /// <returns>Final boundary tone ToBI label.</returns>
        private static TobiLabel ConvertTobiFBT(SP.TtsTobiBoundaryTone tobiBoundary)
        {
            string tobiFinalBoundaryTone = string.Empty;
            switch (tobiBoundary)
            {
                case SP.TtsTobiBoundaryTone.K_LMINUS:
                    tobiFinalBoundaryTone = "L-";
                    break;
                case SP.TtsTobiBoundaryTone.K_HMINUS:
                    tobiFinalBoundaryTone = "H-";
                    break;
                case SP.TtsTobiBoundaryTone.K_LMINUSLPERC:
                    tobiFinalBoundaryTone = "L-L%";
                    break;
                case SP.TtsTobiBoundaryTone.K_LMINUSHPERC:
                    tobiFinalBoundaryTone = "L-H%";
                    break;
                case SP.TtsTobiBoundaryTone.K_HMINUSHPERC:
                    tobiFinalBoundaryTone = "H-H%";
                    break;
                case SP.TtsTobiBoundaryTone.K_HMINUSLPERC:
                    tobiFinalBoundaryTone = "H-L%";
                    break;
                case SP.TtsTobiBoundaryTone.K_SMINUS:
                    tobiFinalBoundaryTone = "S-";
                    break;
            }

            return TobiLabel.Create(tobiFinalBoundaryTone);
        }
    }
}