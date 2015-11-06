//----------------------------------------------------------------------------
// <copyright file="LexiconSearcher.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements lexicon searcher class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// LexiconSearcher handle all the lexicon searching features.
    /// </summary>
    public class LexiconSearcher
    {
        private Lexicon _lex;

        /// <summary>
        /// Initializes a new instance of the <see cref="LexiconSearcher"/> class.
        /// </summary>
        /// <param name="lex">Host lexicon.</param>
        public LexiconSearcher(Lexicon lex)
        {
            _lex = lex;
        }

        /// <summary>
        /// Search with case insensitive.
        /// </summary>
        /// <param name="graphme">The praphme.</param>
        /// <returns>The lexical item.</returns>
        public LexicalItem Search(string graphme)
        {
            return _lex.Lookup(graphme, true);
        }

        /// <summary>
        /// SearchProns with case insensitive.
        /// </summary>
        /// <param name="graphme">The graphme.</param>
        /// <param name="pos">The pos parameter.</param>
        /// <returns>Lexicon pronunciation.</returns>
        public LexiconPronunciation[] SearchProns(string graphme, string pos)
        {
            List<LexiconPronunciation> retProns = new List<LexiconPronunciation>();
            LexicalItem lexItem = _lex.Lookup(graphme, true);
            if (lexItem != null)
            {
                foreach (LexiconPronunciation pron in lexItem.Pronunciations)
                {
                    // check the pos
                    foreach (LexiconItemProperty itemPro in pron.Properties)
                    {
                        if (itemPro.PartOfSpeech != null)
                        {
                            if (itemPro.PartOfSpeech.Value == pos)
                            {
                                retProns.Add(pron);
                                break;
                            }
                            else if (_lex.LexicalAttributeSchema != null)
                            {
                                string posTagPos = _lex.LexicalAttributeSchema.GetPosTaggingPos(
                                    itemPro.PartOfSpeech.Value);
                                if (posTagPos == pos)
                                {
                                    retProns.Add(pron);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return retProns.ToArray();
        }
    }
}