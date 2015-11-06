//----------------------------------------------------------------------------
// <copyright file="ScriptIntermediatePhrase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script intermediate phrase class.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Script
{
    using System.Collections.ObjectModel;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Script prosodic word.
    /// </summary>
    public class ScriptProsodicWord
    {
        /// <summary>
        /// Initializes a new instance of the ScriptProsodicWord class.
        /// </summary>
        public ScriptProsodicWord()
        {
            Words = new Collection<ScriptWord>();
        }

        /// <summary>
        /// Gets or sets intermediate phrase.
        /// </summary>
        public ScriptIntermediatePhrase IntermediatePhrase { get; set; }

        /// <summary>
        /// Gets words in the intermediate phrase.
        /// </summary>
        public Collection<ScriptWord> Words { get; private set; }

        /// <summary>
        /// Get string value of the prosodic word.
        /// </summary>
        /// <returns>String value of the prosodic word.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            Words.ForEach(w =>
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(" ");
                    }

                    sb.Append(w.Grapheme);
                });

            return sb.ToString();
        }
    }

    /// <summary>
    /// Script intermediate phrase.
    /// </summary>
    public class ScriptIntermediatePhrase
    {
        /// <summary>
        /// Initializes a new instance of the ScriptIntermediatePhrase class.
        /// </summary>
        public ScriptIntermediatePhrase()
        {
            ProsodicWords = new Collection<ScriptProsodicWord>();
        }

        /// <summary>
        /// Gets or sets intonation phrase.
        /// </summary>
        public ScriptIntonationPhrase IntonationPhrase { get; set; }

        /// <summary>
        /// Gets prosodic words in the intermediate phrase.
        /// </summary>
        public Collection<ScriptProsodicWord> ProsodicWords { get; private set; }

        /// <summary>
        /// Gets all words in the intermediate phrase.
        /// </summary>
        public Collection<ScriptWord> AllWords
        {
            get
            {
                Collection<ScriptWord> allWord = new Collection<ScriptWord>();
                foreach (ScriptProsodicWord prosodicWord in ProsodicWords)
                {
                    Helper.AppendCollection<ScriptWord>(allWord,
                        prosodicWord.Words);
                }

                return allWord;
            }
        }

        /// <summary>
        /// Parse words to intermediate phrase.
        /// </summary>
        /// <param name="intermediateWords">Words to be parsed.</param>
        public void Parse(Collection<ScriptWord> intermediateWords)
        {
            ProsodicWords.Clear();
            Collection<ScriptWord> words = new Collection<ScriptWord>();
            for (int i = 0; i < intermediateWords.Count; i++)
            {
                ScriptWord word = intermediateWords[i];

                // Append non-normal word to the intermediate break phrase.
                words.Add(word);
                if (word.IsPronouncableNormalWord &&
                    (word.Break >= TtsBreak.Word ||
                    intermediateWords.IndexOf(word) == (intermediateWords.Count - 1)))
                {
                    for (int j = i + 1; j < intermediateWords.Count; j++)
                    {
                        if (intermediateWords[j].IsPronouncableNormalWord)
                        {
                            break;
                        }
                        else
                        {
                            words.Add(intermediateWords[j]);
                            i = j;
                        }
                    }

                    ScriptProsodicWord prosodicWord = new ScriptProsodicWord()
                    {
                        IntermediatePhrase = this,
                    };

                    Helper.AppendCollection<ScriptWord>(prosodicWord.Words, words);
                    ProsodicWords.Add(prosodicWord);
                    words.Clear();
                }
            }

            if (words.Count > 0)
            {
                ScriptProsodicWord prosodicWord = new ScriptProsodicWord()
                {
                    IntermediatePhrase = this,
                };

                Helper.AppendCollection<ScriptWord>(prosodicWord.Words, words);
                ProsodicWords.Add(prosodicWord);
                words.Clear();
            }
        }

        /// <summary>
        /// Get prosodic word contains this word.
        /// </summary>
        /// <param name="scriptWord">Script word.</param>
        /// <returns>Prosodic word contained this word.</returns>
        public ScriptProsodicWord GetProsodicWord(ScriptWord scriptWord)
        {
            Helper.ThrowIfNull(ProsodicWords);
            Helper.ThrowIfNull(scriptWord);
            ScriptProsodicWord foundProsodicWord = null;
            foreach (ScriptProsodicWord prosodicWord in ProsodicWords)
            {
                if (prosodicWord.Words.Contains(scriptWord))
                {
                    foundProsodicWord = prosodicWord;
                    break;
                }
            }

            return foundProsodicWord;
        }

        /// <summary>
        /// Get string value of the prosodic word.
        /// </summary>
        /// <returns>String value of the prosodic word.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            ProsodicWords.ForEach(w =>
            {
                if (sb.Length > 0)
                {
                    sb.Append(" ");
                }

                sb.Append(w.ToString());
            });

            return sb.ToString();
        }
    }
}