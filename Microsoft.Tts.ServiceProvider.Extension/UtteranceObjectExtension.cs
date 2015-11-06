//----------------------------------------------------------------------------
// <copyright file="UtteranceObjectExtension.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     This module extend object models in Utterance
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
    public static class UtteranceObjectExtension
    {
        public static List<TtsIntonationPhrase> GetIntonationPhrases(this SP.TtsSentence sent)
        {
            List<TtsIntonationPhrase> phrases = new List<TtsIntonationPhrase>();
            for (TtsIntonationPhrase phrase = sent.FirstIntonationPhrase;
                phrase != null;
                phrase = phrase.Next)
            {
                phrases.Add(phrase);
                if (phrase == sent.LastIntonationPhrase)
                {
                    break;
                }
            }

            return phrases;
        }
    }
}