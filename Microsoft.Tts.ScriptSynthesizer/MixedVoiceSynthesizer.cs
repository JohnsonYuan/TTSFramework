//----------------------------------------------------------------------------
// <copyright file="MixedVoiceSynthesizer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements mixed voice synthesizer
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.ScriptSynthesizer
{
    using System;
    using System.IO;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider;
    using Microsoft.Tts.ServiceProvider.Extension;

    /// <summary>
    /// Mixed voice synthesizer class.
    /// </summary>
    public class MixedVoiceSynthesizer : IDisposable
    {
        #region Fields

        /// <summary>
        /// Service provider - Donator.
        /// </summary>
        private ServiceProvider _donator;

        /// <summary>
        /// Service provider - Receiver.
        /// </summary>
        private ServiceProvider _receiver;

        /// <summary>
        /// Config object.
        /// </summary>
        private MixedVoiceSynthesizerConfig _config;

        /// <summary>
        /// Log writer.
        /// </summary>
        private TextLogger _logger;

        /// <summary>
        /// Current script item.
        /// </summary>
        private ScriptItem _curScriptItem;

        /// <summary>
        /// Current script sentence.
        /// </summary>
        private ScriptSentence _curScriptSententence;

        /// <summary>
        /// Matrix used to store donator's duration.
        /// </summary>
        private uint[,] _donatorDuration;

        /// <summary>
        /// Matrix used to store donator's f0.
        /// </summary>
        private float[,] _donatorF0;

        /// <summary>
        /// Disposed flag.
        /// </summary>
        private bool _disposed = false;

        #endregion

        #region Consturctor and destructor

        /// <summary>
        /// Initializes a new instance of the MixedVoiceSynthesizer class.
        /// </summary>
        /// <param name="donator">Service provider the donator.</param>
        /// <param name="receiver">Service provider the receiver.</param>
        /// <param name="config">Config object.</param>
        public MixedVoiceSynthesizer(ServiceProvider donator, ServiceProvider receiver,
            MixedVoiceSynthesizerConfig config)
        {
            if (donator == null)
            {
                throw new ArgumentNullException("donator");
            }

            if (receiver == null)
            {
                throw new ArgumentNullException("receiver");
            }

            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            _donator = donator;
            _receiver = receiver;

            _config = config;

            _donator.Engine.AcousticProsodyTagger.Processed +=
                new EventHandler<TtsModuleEventArgs>(OnDonatorAcousticProcessed);

            _receiver.Engine.AcousticProsodyTagger.Processed +=
                new EventHandler<TtsModuleEventArgs>(OnReceiverAcousticProcessed);

            if (!string.IsNullOrEmpty(config.LogFile))
            {
                _logger = new TextLogger(config.LogFile);
                _logger.Reset();
            }
        }

        /// <summary>
        /// Finalizes an instance of the MixedVoiceSynthesizer class.
        /// </summary>
        ~MixedVoiceSynthesizer()
        {
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        #endregion

        #region Public members

        /// <summary>
        /// Dispose routine.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Speak a script sentence in a script item.
        /// </summary>
        /// <param name="scriptItem">The script item.</param>
        /// <param name="scriptSentence">The script sentence.</param>
        public void Speak(ScriptItem scriptItem, ScriptSentence scriptSentence)
        {
            _curScriptItem = scriptItem;
            _curScriptSententence = scriptSentence;

            _donator.SpeechSynthesizer.Speak(_curScriptSententence.Text);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Dispose managed resource.
        /// </summary>
        private void Close()
        {
            _donator.Engine.AcousticProsodyTagger.Processed -=
                new EventHandler<TtsModuleEventArgs>(OnDonatorAcousticProcessed);

            _receiver.Engine.AcousticProsodyTagger.Processed -=
                new EventHandler<TtsModuleEventArgs>(OnReceiverAcousticProcessed);
        }

        /// <summary>
        /// Do the dispose work here.
        /// </summary>
        /// <param name="disposing">Whether the functions is called by user's code (true), or by finalizer (false).</param>
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    Close();
                }

                // Dispose unmanaged resources.

                // Note disposing has been done.
                _disposed = true;
            }
        }

        /// <summary>
        /// Aoustic processed event handler for the donator.
        /// Store duration and/or f0 of donator locally.
        /// </summary>
        /// <param name="sender">Sender of the event handler.</param>
        /// <param name="e">Event argument object.</param>
        private void OnDonatorAcousticProcessed(object sender, TtsModuleEventArgs e)
        {
            if (e.ModuleType == TtsModuleType.TM_APT_DURATION &&
                _config.DonateDuration)
            {
                _donatorDuration = e.Utterance.Acoustic.Durations.ToArray();
            }

            if (e.ModuleType == TtsModuleType.TM_APT_F0 &&
                _config.DonateF0)
            {
                _donatorF0 = e.Utterance.Acoustic.F0s.ToArray();
            }

            if (e.ModuleType == TtsModuleType.TM_APT_LSF)
            {
                _receiver.SpeechSynthesizer.Speak(_curScriptSententence.Text);
            }
        }

        /// <summary>
        /// Aoustic processed event handler for the receiver.
        /// Apply stored duration and/or f0 to receiver.
        /// </summary>
        /// <param name="sender">Sender of the event handler.</param>
        /// <param name="e">Event argument object.</param>
        private void OnReceiverAcousticProcessed(object sender, TtsModuleEventArgs e)
        {
            if (e.ModuleType == TtsModuleType.TM_APT_DURATION &&
                _config.DonateDuration)
            {
                if (!e.Utterance.Acoustic.Durations.DimensionIsMatched(_donatorDuration))
                {
                    ErrorLog("Number of phones don't match!");
                }
                else
                {
                    e.Utterance.Acoustic.Durations.CopyFrom(_donatorDuration);
                    e.Utterance.Acoustic.RefreshFrameCount();
                }
            }

            if (e.ModuleType == TtsModuleType.TM_APT_F0 &&
                _config.DonateF0)
            {
                if (!e.Utterance.Acoustic.F0s.DimensionIsMatched(_donatorF0))
                {
                    ErrorLog("Number of frames don't match!");
                }
                else
                {
                    e.Utterance.Acoustic.F0s.CopyFrom(_donatorF0);
                }
            }
        }

        /// <summary>
        /// Output error information to log.
        /// </summary>
        /// <param name="error">Error text.</param>
        private void ErrorLog(string error)
        {
            if (_logger != null)
            {
                _logger.LogLine("Error of script [{0}]: Sentence [{1}]",
                    _curScriptItem.Id, _curScriptSententence.Text);
                _logger.LogLine("\t- {0}", error);
            }
        }

        #endregion
    }
}
