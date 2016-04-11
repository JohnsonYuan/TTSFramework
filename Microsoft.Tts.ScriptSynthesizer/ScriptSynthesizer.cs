//----------------------------------------------------------------------------
// <copyright file="ScriptSynthesizer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script synthesizer
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.ScriptSynthesizer
{
    using System;
    using System.Globalization;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider;
    using Microsoft.Tts.ServiceProvider.Extension;

    /// <summary>
    /// Script synthesizer.
    /// </summary>
    public class ScriptSynthesizer : IDisposable
    {
        #region Fields

        /// <summary>
        /// Service provider.
        /// </summary>
        private ServiceProvider _serviceProvider;

        /// <summary>
        /// Config of script feature import.
        /// </summary>
        private ScriptFeatureImportConfig _scriptFeatureImportConfig;

        /// <summary>
        /// Script feature import object.
        /// </summary>
        private ScriptFeatureImport _scriptFeatureImport;

        /// <summary>
        /// Customized feature extraction object.
        /// </summary>
        private CustomizedFeatureExtraction _customizedFeatureExtraction;

        /// <summary>
        /// Log writer.
        /// </summary>
        private TextLogger _logger;

        /// <summary>
        /// Disposed flag.
        /// </summary>
        private bool _disposed = false;

        #endregion

        #region Consturctor and destructor

        /// <summary>
        /// Initializes a new instance of the ScriptSynthesizer class.
        /// </summary>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="commonConfig">Common config of script synthesizer.</param>
        /// <param name="scriptFeatureImportConfig">Config of script feature import.</param>
        /// <param name="customizedFeaturePluginManager">Plugin manager of customized feature extraction.</param>
        public ScriptSynthesizer(ServiceProvider serviceProvider,
            ScriptSynthesizerCommonConfig commonConfig,
            ScriptFeatureImportConfig scriptFeatureImportConfig,
            CustomizedFeaturePluginManager customizedFeaturePluginManager)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            // commonConfig can be null.
            // scriptFeatureImportConfig can be null.
            // customizedFeaturePluginManager can be null.
            _serviceProvider = serviceProvider;

            _scriptFeatureImportConfig = scriptFeatureImportConfig;

            if (commonConfig != null)
            {
                _logger = new TextLogger(commonConfig.TraceLogFile);
                _logger.Reset();
            }

            if (_scriptFeatureImportConfig != null)
            {
                _scriptFeatureImport = new ScriptFeatureImport(commonConfig,
                    _scriptFeatureImportConfig, _serviceProvider, _logger);
            }

            if (customizedFeaturePluginManager != null)
            {
                _customizedFeatureExtraction = new CustomizedFeatureExtraction(_serviceProvider,
                    customizedFeaturePluginManager);
            }
        }

        /// <summary>
        /// Finalizes an instance of the ScriptSynthesizer class.
        /// </summary>
        ~ScriptSynthesizer()
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
        /// Prepare for speak.
        /// </summary>
        /// <param name="scriptItem">Script item.</param>
        public void PrepareSpeak(ScriptItem scriptItem)
        {
            if (_scriptFeatureImportConfig != null)
            {
                _scriptFeatureImport.PrepareSpeak(scriptItem);
            }

            if (_customizedFeatureExtraction != null)
            {
                _customizedFeatureExtraction.PrepareSpeak(scriptItem);
            }
        }

        /// <summary>
        /// Initailize sentence to run.
        /// </summary>
        /// <param name="scriptSentence">Script sentence.</param>
        public void InitializeSentence(ScriptSentence scriptSentence)
        {
            if (_scriptFeatureImportConfig != null)
            {
                _scriptFeatureImport.InitializeSentence(scriptSentence);
            }
        }

        #endregion

        #region Private methods

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
                }

                // Dispose unmanaged resources.
                if (_scriptFeatureImport != null)
                {
                    _scriptFeatureImport.Dispose();
                }

                // Dispose unmanaged resources.
                if (_customizedFeatureExtraction != null)
                {
                    _customizedFeatureExtraction.Dispose();
                }

                // Note disposing has been done.
                _disposed = true;
            }
        }

        #endregion
    }
}
