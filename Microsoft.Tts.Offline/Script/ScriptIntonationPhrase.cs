//----------------------------------------------------------------------------
// <copyright file="ScriptIntonationPhrase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script intonation phrase class.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Script
{
    using System.Collections.ObjectModel;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Script intonation phrase.
    /// </summary>
    public class ScriptIntonationPhrase
    {
        /// <summary>
        /// Initializes a new instance of the ScriptIntonationPhrase class.
        /// </summary>
        public ScriptIntonationPhrase()
        {
            IntermediatePhrases = new Collection<ScriptIntermediatePhrase>();
        }

        /// <summary>
        /// Gets or sets script sentence.
        /// </summary>
        public ScriptSentence Sentence { get; set; }

        /// <summary>
        /// Gets intermediate phrase list.
        /// </summary>
        public Collection<ScriptIntermediatePhrase> IntermediatePhrases { get; private set; }

        /// <summary>
        /// Gets all prosodic words in the intonation phrase.
        /// </summary>
        public Collection<ScriptProsodicWord> AllProsodicWord
        {
            get
            {
                Collection<ScriptProsodicWord> allProsodicWord = new Collection<ScriptProsodicWord>();
                foreach (ScriptIntermediatePhrase intermPhrase in IntermediatePhrases)
                {
                    Helper.AppendCollection<ScriptProsodicWord>(allProsodicWord,
                        intermPhrase.ProsodicWords);
                }

                return allProsodicWord;
            }
        }

        /// <summary>
        /// Get intermediate phrase of the word.
        /// </summary>
        /// <param name="word">Script word.</param>
        /// <returns>Intermediate phrase of the script word.</returns>
        public ScriptProsodicWord GetProsodicWord(ScriptWord word)
        {
            ScriptIntermediatePhrase intermediatePhrase = GetIntermediatePhrase(word);
            ScriptProsodicWord prosodicWord = intermediatePhrase.GetProsodicWord(word);
            Helper.ThrowIfNull(prosodicWord);
            return prosodicWord;
        }

        /// <summary>
        /// Get intermediate phrase of the word.
        /// </summary>
        /// <param name="word">Script word.</param>
        /// <returns>Intermediate phrase of the script word.</returns>
        public ScriptIntermediatePhrase GetIntermediatePhrase(ScriptWord word)
        {
            ScriptIntermediatePhrase intermediatePhrase = null;
            foreach (ScriptIntermediatePhrase phrase in IntermediatePhrases)
            {
                ScriptProsodicWord prosodicWord = phrase.GetProsodicWord(word);
                if (prosodicWord != null)
                {
                    intermediatePhrase = phrase;
                    break;
                }
            }

            return intermediatePhrase;
        }

        /// <summary>
        /// Parse words to intonation phrase.
        /// </summary>
        /// <param name="intonationWords">Words to be parsed.</param>
        public void Parse(Collection<ScriptWord> intonationWords)
        {
            IntermediatePhrases.Clear();
            Collection<ScriptWord> words = new Collection<ScriptWord>();
            for (int i = 0; i < intonationWords.Count; i++)
            {
                ScriptWord word = intonationWords[i];
                words.Add(word);
                if (word.IsPronouncableNormalWord &&
                    (word.Break >= TtsBreak.InterPhrase ||
                    intonationWords.IndexOf(word) == (intonationWords.Count - 1)))
                {
                    // Append non-normal word to the intonation break phrase.
                    for (int j = i + 1; j < intonationWords.Count; j++)
                    {
                        if (intonationWords[j].IsPronouncableNormalWord)
                        {
                            break;
                        }
                        else
                        {
                            words.Add(intonationWords[j]);
                            i = j;
                        }
                    }

                    ScriptIntermediatePhrase phrase = new ScriptIntermediatePhrase()
                    {
                        IntonationPhrase = this,
                    };

                    phrase.Parse(words);
                    IntermediatePhrases.Add(phrase);
                    words.Clear();
                }
            }

            if (words.Count > 0)
            {
                ScriptIntermediatePhrase phrase = new ScriptIntermediatePhrase()
                {
                    IntonationPhrase = this,
                };

                phrase.Parse(words);
                IntermediatePhrases.Add(phrase);
                words.Clear();
            }
        }

        /// <summary>
        /// Get string value of the prosodic word.
        /// </summary>
        /// <returns>String value of the prosodic word.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            IntermediatePhrases.ForEach(p =>
            {
                if (sb.Length > 0)
                {
                    sb.Append(" ");
                }

                sb.Append(p.ToString());
            });

            return sb.ToString();
        }
    }
}