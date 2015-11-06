//----------------------------------------------------------------------------
// <copyright file="IUtteranceExtender.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     IUtteranceExtender interface definition
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.ServiceProvider.Extension
{
    using System;
    using Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// Interface to extend custom utterance properties.
    /// </summary>
    public interface IUtteranceExtender : IDisposable
    {
        /// <summary>
        /// Initialize with configuration string.
        /// </summary>
        /// <param name="configuration">The configuration string.</param>
        void Initialize(string configuration);

        /// <summary>
        /// Process each utterance.
        /// </summary>
        /// <param name="utterance">The utterance object to extend.</param>
        /// <param name="scriptItem">The script item utterance associated with.</param>
        void Process(TtsUtterance utterance, Offline.ScriptItem scriptItem);
    }
}