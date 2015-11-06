//----------------------------------------------------------------------------
// <copyright file="ILexicon.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Lexicon interface
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Core
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Lexicon interface.
    /// </summary>
    public interface ILexicon
    {
        /// <summary>
        /// Gets Items the lexicon has.
        /// </summary>
        Dictionary<string, LexicalItem> Items
        {
            get;
        }

        /// <summary>
        /// Search for item according to grapheme.
        /// </summary>
        /// <param name="grapheme">Graphme of the word to lookup.</param>
        /// <returns>Lexicon item found, null if not.</returns>
        LexicalItem Lookup(string grapheme);
    }
}