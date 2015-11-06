//----------------------------------------------------------------------------
// <copyright file="ZhCNScriptItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements French script time
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// French script item.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
        "CA1706:ShortAcronymsShouldBeUppercase", Justification = "Ignore.")]
    public class ZhCNScriptItem : ScriptItem
    {
        #region Override properties

        /// <summary>
        /// Gets Language.
        /// </summary>
        public override Language Language
        {
            get { return Language.ZhCN; }
        }

        /// <summary>
        /// Gets Punctuation pattern for this language.
        /// </summary>
        public override string PunctuationPattern
        {
            get
            {
                // Puncuation "・" and "·" should not break words.
                // So zh-CN should not contain this punctuation.
                return @"(\""|/|\s'|\'$|,|\.\""$|\.$|\.\.\.|!|\?|_|;|:|\(|\)|\[|\]|\{|\}|" +
                    @"\xA1|\xBF| -| ?- |^-$|，|、|：|；|——|～|。|！|？|『|』|「|」|（|）|【|】" +
                    @"|〔|〕|《|》|〈|〉|“|”|\s‘|\‘$|\s’|\’$|¿|…" +
                    @"|…|／|－|！|［|］|．|‐)";
            }
        }

        #endregion
    }
}