//----------------------------------------------------------------------------
// <copyright file="CustomizedFeatureExtraction.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements customized feature extraction
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.ScriptSynthesizer
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.ServiceProvider;
    using Microsoft.Tts.ServiceProvider.Extension;

    /// <summary>
    /// Customized feature extraction.
    /// </summary>
    public class CustomizedFeatureExtraction : IDisposable
    {
        #region Private fields

        /// <summary>
        /// Service provider.
        /// </summary>
        private ServiceProvider _serviceProvider;

        /// <summary>
        /// The plugin manager.
        /// </summary>
        private CustomizedFeaturePluginManager _customizedFeaturePluginManager;

        /// <summary>
        /// Utterance extenders.
        /// </summary>
        private List<IUtteranceExtender> _utteranceExtenders = new List<IUtteranceExtender>();

        /// <summary>
        /// Current script item.
        /// </summary>
        private ScriptItem _curScriptItem;

        /// <summary>
        /// Disposed flag.
        /// </summary>
        private bool _disposed = false;

        #endregion

        #region Constructor and destructor

        /// <summary>
        /// Initializes a new instance of the CustomizedFeatureExtraction class.
        /// </summary>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="customizedFeaturePluginManager">Plugin manager.</param>
        public CustomizedFeatureExtraction(ServiceProvider serviceProvider,
            CustomizedFeaturePluginManager customizedFeaturePluginManager)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            if (customizedFeaturePluginManager == null)
            {
                throw new ArgumentNullException("customizedFeaturePluginManager");
            }

            _customizedFeaturePluginManager = customizedFeaturePluginManager;

            List<PluginInfo> pluginInfos = _customizedFeaturePluginManager.GetPlugins(
                CustomizedFeaturePluginManager.AttachBeforeExtraction);
            if (pluginInfos != null)
            {
                _utteranceExtenders = UtteranceExtenderFinder.LoadUtteranceExtenders(pluginInfos);
            }

            _serviceProvider = serviceProvider;

            _serviceProvider.Engine.AcousticProsodyTagger.Processing += new EventHandler<TtsModuleEventArgs>(OnAcousticProcessing);
        }

        /// <summary>
        /// Finalizes an instance of the CustomizedFeatureExtraction class.
        /// </summary>
        ~CustomizedFeatureExtraction()
        {
            Dispose(false);
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Dispose routine.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Prepare for the speak.
        /// </summary>
        /// <param name="scriptItem">Script item.</param>
        public void PrepareSpeak(ScriptItem scriptItem)
        {
            _curScriptItem = scriptItem;
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Dispose managed resource.
        /// </summary>
        private void Close()
        {
            if (_serviceProvider != null)
            {
                _serviceProvider.Engine.AcousticProsodyTagger.Processing -= new EventHandler<TtsModuleEventArgs>(OnAcousticProcessing);
            }
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

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                foreach (IUtteranceExtender extender in _utteranceExtenders)
                {
                    extender.Dispose();
                }

                // Note disposing has been done.
                _disposed = true;
            }
        }

        /// <summary>
        /// Aoustic processing event handler.
        /// Call process of utterance extenders to process the utterance.
        /// </summary>
        /// <param name="sender">Sender of the event handler.</param>
        /// <param name="e">Event argument object.</param>
        private void OnAcousticProcessing(object sender, TtsModuleEventArgs e)
        {
            if (e.ModuleType == TtsModuleType.TM_APT_MODEL_FINDER)
            {
                foreach (IUtteranceExtender extender in _utteranceExtenders)
                {
                    extender.Process(e.Utterance, _curScriptItem);
                }
            }
        }

        #endregion
    }
}
