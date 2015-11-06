//----------------------------------------------------------------------------
// <copyright file="ZhToneIndexExtractor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements ZhToneIndexPlugin VoiceModelTrainer plugin, which sets custom
//     ChineseToneIndex property by calling an exposed SP class ChineseToneHandler.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.ServiceProvider.Extension
{
    using System;
    using System.IO;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Compiler;
    using SP = Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// Implements the ChineseToneIndexExtractor.
    /// </summary>
    public sealed class ChineseToneIndexExtractor : IDisposable
    {
        /// <summary>
        /// ChineseToneHandler.
        /// </summary>
        private SP.ChineseToneHandler _zhtone = null;

        #region Method
       
        /// <summary>
        /// Initialize with phoneset.
        /// </summary>
        /// <param name="phoneme">
        /// Represents the phoneme.
        /// </param>
        public void Initialize(SP.Phoneme phoneme)
        {
            _zhtone = new ChineseToneHandler(phoneme);
        }

        /// <summary>
        /// Update Chinese tone index by calling SP.ChineseToneHandler.
        /// </summary>
        /// <param name="utterance">
        /// Represents the utterance object to extract linguistic feature from.
        /// </param>
        /// <param name="scriptItem">
        /// Represents the script item related to the utterance object.
        /// </param>
        public void Process(Microsoft.Tts.ServiceProvider.TtsUtterance utterance,
            Microsoft.Tts.Offline.ScriptItem scriptItem)
        {
            for (int wordIndex = 0; wordIndex < utterance.Words.Count; wordIndex++)
            {
                _zhtone.UpdateToneIndex(utterance.Words[wordIndex]);
            }
        }

        /// <summary>
        /// Dispose phoneme and zhtone.
        /// </summary>
        public void Dispose()
        {
            if (_zhtone != null)
            {
                _zhtone.Dispose();
            }

            return;
        }

        #endregion
    }
}
