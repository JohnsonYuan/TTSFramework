//----------------------------------------------------------------------------
// <copyright file="TtsPos.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements TTS POS
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Part of speech.
    /// </summary>
    public enum PartOfSpeech
    {
        /// <summary>
        /// Not override POS.
        /// </summary>
        NotOverride = -1,

        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Noun.
        /// </summary>
        Noun = 0x1000,

        /// <summary>
        /// Verb.
        /// </summary>
        Verb = 0x2000,

        /// <summary>
        /// Modifier.
        /// </summary>
        Modifier = 0x3000,

        /// <summary>
        /// Function.
        /// </summary>
        Function = 0x4000,

        /// <summary>
        /// Interjection.
        /// </summary>
        Interjection = 0x5000,

        /// <summary>
        /// SuppressWord.
        /// </summary>
        SuppressWord = 0xf000
    }
}