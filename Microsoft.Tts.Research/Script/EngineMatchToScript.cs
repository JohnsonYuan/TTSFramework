//----------------------------------------------------------------------------
// <copyright file="EngineMatchToScript.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements EngineMatchToScript, make the words 
// consistent between engine and script.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider;
    using SP = Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// EngineMatchToScript class.
    /// </summary>
    public static class EngineMatchToScript
    {
        /// <summary>
        /// Copy engine's silence words to xml script.
        /// </summary>
        /// <param name="utterance">Tts utterance.</param>
        /// <param name="scriptSentence">Script sentence.</param>
        public static void CopySilenceTo(this SP.TtsUtterance utterance,
            ScriptSentence scriptSentence)
        {
            CheckPronouncedWordsMatched(scriptSentence, utterance);

            int scriptWordIndex = 0;
            foreach (TtsWord uttWord in utterance.Words)
            {
                if (uttWord.IsPronounceable)
                {
                    if (scriptWordIndex < scriptSentence.Words.Count &&
                        scriptSentence.Words[scriptWordIndex].WordType == WordType.Silence)
                    {
                        scriptSentence.Words.RemoveAt(scriptWordIndex);
                    }
                }
                else if (uttWord.IsSilence && ((scriptWordIndex < scriptSentence.Words.Count &&
                    scriptSentence.Words[scriptWordIndex].WordType != WordType.Silence) ||
                    scriptWordIndex == scriptSentence.Words.Count))
                {
                    if (uttWord.FirstSyllable.FirstPhone.PhoneID == Phoneme.SilencePhoneId)
                    {
                        InsertSilenceWord(scriptSentence, scriptWordIndex, Phoneme.SilencePhone);
                    }
                    else
                    {
                        Debug.Assert(uttWord.FirstSyllable.FirstPhone.IsShortPauseSupported, "Short pause should be supported.");
                        InsertSilenceWord(scriptSentence, scriptWordIndex, Phoneme.ShortPausePhone);
                    }
                }

                scriptWordIndex++;
            }

            if (utterance.Words.Count != scriptWordIndex)
            {
                throw new InvalidDataException("Runtime's words' count must equal to " +
                    "the script's.");
            }
        }

        /// <summary>
        /// Check the consistence of pronounced words between engine and script.
        /// </summary>
        /// <param name="scriptSentence">Script sentence.</param>
        /// <param name="utt">Tts utterance.</param>
        public static void CheckPronouncedWordsMatched(ScriptSentence scriptSentence, 
            SP.TtsUtterance utt)
        {
            int wordIndex = 0;
            int phoneIndex = 0;

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

                    foreach (ScriptSyllable scriptSyllable in 
                        scriptSentence.PronouncedWords[wordIndex].Syllables)
                    {
                        foreach (ScriptPhone scriptPhone in scriptSyllable.Phones)
                        {
                            string uttPhoneText = string.Empty;
                            if (phoneIndex < utt.Phones.Count)
                            {
                                string[] items = 
                                    utt.Phones[phoneIndex].Pronunciation.Split(new char[] { ' ' });
                                uttPhoneText = items[0].ToLower(CultureInfo.CurrentCulture);
                            }

                            if (!uttPhoneText.Equals(scriptPhone.Name.ToLower(CultureInfo.CurrentCulture)))
                            {
                                string message = Helper.NeutralFormat("Runtime's phone [{0}] " +
                                    "and script phone [{1}] has no consistence.", uttPhoneText,
                                    scriptPhone.Name);
                                throw new InvalidDataException(message);
                            }

                            phoneIndex++;
                        }
                    }

                    wordIndex++;
                }
                else if (uttWord.IsSilence)
                {
                    phoneIndex++;
                }
            }

            if (wordIndex != scriptSentence.PronouncedWords.Count)
            {
                throw new InvalidDataException("Runtime's normal words' count must " + 
                    "equal to the script's.");
            }
        }

        /// <summary>
        /// Insert silence word to script.
        /// </summary>
        /// <param name="scriptSentence">Script sentence.</param>
        /// <param name="wordIndex">To be insert word's position.</param>
        /// <param name="phoneme">The phoneme string.</param>
        public static void InsertSilenceWord(ScriptSentence scriptSentence, int wordIndex, string phoneme)
        {
            Debug.Assert(Phoneme.IsSilenceFeature(phoneme), "The phoneme should have silence feature");

            ScriptWord silenceWord = new ScriptWord();
            silenceWord.WordType = WordType.Silence;
            silenceWord.Pronunciation = Phoneme.ToRuntime(phoneme);
            silenceWord.Sentence = scriptSentence;
            ScriptSyllable silenceSyllable = new ScriptSyllable();
            silenceSyllable.Word = silenceWord;
            silenceWord.Syllables.Add(silenceSyllable);
            ScriptPhone silencePhone = new ScriptPhone(phoneme);
            silencePhone.Syllable = silenceSyllable;
            silenceWord.Syllables[0].Phones.Add(silencePhone);

            scriptSentence.Words.Insert(wordIndex, silenceWord);
        }
    }
}