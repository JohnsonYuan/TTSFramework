//----------------------------------------------------------------------------
// <copyright file="EngineVocoder.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements the tts engine vocoder class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Research
{
    using System;
    using System.Globalization;
    using Microsoft.Tts.ServiceProvider;
    using Offline = Microsoft.Tts.Offline;

    /// <summary>
    /// Engine vocoder, call service provider to do the synthesis.
    /// </summary>
    public class EngineVocoder : IVocoder, IDisposable
    {
        private ServiceProvider _serviceProvider;

        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="EngineVocoder"/> class.
        /// </summary>
        /// <param name="language">Language of the engine.</param>
        /// <param name="voicePath">Voice font path of the engine.</param>
        /// <param name="localePath">Locale handler of the engine.</param>
        public EngineVocoder(string language, string voicePath, string localePath)
        {
            Offline.Language languageID = Offline.Localor.StringToLanguage(language);
            int langID = (int)languageID;
            _serviceProvider = new ServiceProvider((Language)langID, voicePath, localePath);

            if (_serviceProvider == null)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "ServiceProvider construction error: language is \"{0}\", voicePath is \"{1}\", localePath is \"{2}\"",
                    language, voicePath, localePath);
                throw new ArgumentException(message);
            }
        }

        #region IDisposalbe and destructor

        // Use C# destructor syntax for finalization code.
        // This destructor will run only if the Dispose method
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide destructors in types derived from this class.

        /// <summary>
        /// Finalizes an instance of the <see cref="EngineVocoder"/> class..
        /// </summary>
        ~EngineVocoder()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        
        /// <summary>
        /// Dispose routine.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// Using f0, gain and lsp vector to generate speech data.
        /// </summary>
        /// <param name="f0Vector">F0 vector, value in linear Hz domain.</param>
        /// <param name="lspVector">LSP vector, vlaue in interval [0, 0.5).</param>
        /// <param name="gainVector">Gain vector.</param>
        /// <param name="samplingRate">Sampling rate.</param>
        /// <param name="framePeriod">Frame period, in seconds.</param>
        /// <returns>Resultant wave data, in short.</returns>
        public short[] LspExcite(float[] f0Vector, float[,] lspVector, float[] gainVector,
            int samplingRate, float framePeriod)
        {
            uint frameSamples = (uint)Math.Round(samplingRate * framePeriod);
            short[] waveData = _serviceProvider.Engine.Vocoder.SynthesizeLSF(f0Vector, lspVector, gainVector, frameSamples);
            return waveData;
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        
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

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                if (_serviceProvider != null)
                {
                    _serviceProvider.Dispose();
                    _serviceProvider = null;
                }

                // Note disposing has been done.
                _disposed = true;
            }
        }
    }
}