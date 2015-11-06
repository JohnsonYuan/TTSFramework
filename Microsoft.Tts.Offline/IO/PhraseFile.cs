//----------------------------------------------------------------------------
// <copyright file="PhraseFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements class for phrase file operation.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.IO
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Phrase information in the phrase file, each phrase item represent one line in the phrase file.
    /// The columns are separated by tab ('\t')
    /// For example:
    ///     absence of menstruation'\t'noun
    ///     absence of middle ear'\t'noun
    ///     absence of mind'\t'noun,unknown.
    /// </summary>
    public class PhraseItem
    {
        /// <summary>
        /// Initializes a new instance of the PhraseItem class.
        /// </summary>
        public PhraseItem()
        {
            Poses = new Collection<string>();
        }

        /// <summary>
        /// Gets or sets phrase text.
        /// </summary>
        public string Phrase { get; set; }

        /// <summary>
        /// Gets POS list of the phrase.
        /// </summary>
        public Collection<string> Poses { get; private set; }
    }

    /// <summary>
    /// Phrase file operation class, phrase file contains information of phrases.
    /// This file is plain text file with unicode encoding.
    /// There are at least 2 columns in each line, the column separated by "tab" char ('\t').
    /// The first column is the phrase, the words of the phrase separated by blank space.
    /// The second column is the candidate POS information of the phrase, there may be multiple POS.
    /// The poses is combined with ",".
    /// For example:
    ///     absence of menstruation'\t'noun
    ///     absence of middle ear'\t'noun
    ///     absence of mind'\t'noun,unknown.
    /// </summary>
    public class PhraseFile
    {
        /// <summary>
        /// Load phrase from multi files.
        /// </summary>
        /// <param name="phraseFilePathes">Phrase files.</param>
        /// <returns>Loaded phrase list.</returns>
        public static Dictionary<string, PhraseItem> Read(string[] phraseFilePathes)
        {
            Dictionary<string, PhraseItem> allPhrases = new Dictionary<string, PhraseItem>();
            foreach (string phraseFilePath in phraseFilePathes)
            {
                Dictionary<string, PhraseItem> phrases = Read(phraseFilePath);
                phrases.Where(p => !allPhrases.ContainsKey(p.Key)).ForEach(
                    p => allPhrases.Add(p.Key, p.Value));
            }

            return allPhrases;
        }

        /// <summary>
        /// Load phrase files.
        /// </summary>
        /// <param name="phraseFilePath">Phrase file path.</param>
        /// <returns>Loaded phrase pair.</returns>
        public static Dictionary<string, PhraseItem> Read(string phraseFilePath)
        {
            Dictionary<string, PhraseItem> phraseItems = new Dictionary<string, PhraseItem>();
            foreach (string line in Helper.FileLines(phraseFilePath))
            {
                string[] items = line.Split(Delimitor.TabChars, StringSplitOptions.RemoveEmptyEntries);
                if (items.Length < 2)
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "The phrase file [{0}] is with invalid line [{1}], " +
                        "which should contain two columns separated by a tab character.",
                        phraseFilePath, line));
                }

                if (phraseItems.ContainsKey(items[0]))
                {
                    // Skips this one if there already has it.
                    continue;
                }

                string[] poses = items[1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                PhraseItem phraseItem = new PhraseItem()
                {
                    Phrase = items[0].ToLowerInvariant(),
                };

                poses.ForEach(p => phraseItem.Poses.Add(p));
                if (!phraseItems.ContainsKey(phraseItem.Phrase))
                {
                    phraseItems.Add(phraseItem.Phrase, phraseItem);
                }
            }

            return phraseItems;
        }
    }
}