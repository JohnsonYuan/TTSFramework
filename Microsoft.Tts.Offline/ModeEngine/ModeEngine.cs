//----------------------------------------------------------------------------
// <copyright file="ModeEngine.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines the ModeEngine class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.ModeEngine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// This abstract class is used to organize the mode engine of VoiceModelTrainer.
    /// Every step in ModeEngine will be extended from this class.
    /// </summary>
    public abstract class ModeEngine
    {
        #region private field

        /// <summary>
        /// The name of the mode.
        /// </summary>
        [CLSCompliant(false)]
        protected string _modeName = string.Empty;

        /// <summary>
        /// The location of config file.
        /// </summary>
        [CLSCompliant(false)]
        protected string _modeConfig = string.Empty;
        #endregion

        #region construtor

        /// <summary>
        /// Initializes a new instance of the <see cref="ModeEngine"/> class.
        /// </summary>
        /// <param name="modeName">The name of the mode.</param>
        /// <param name="modeConfig">The location of configure file.</param>
        public ModeEngine(string modeName, string modeConfig)
        {
            if (string.IsNullOrEmpty(modeName))
            {
                throw new ArgumentNullException(modeName);
            }

            if (string.IsNullOrEmpty(modeConfig))
            {
                throw new ArgumentNullException(modeConfig);
            }

            _modeName = modeName;
            _modeConfig = modeConfig;
        }
        #endregion

        /// <summary>
        /// The abstract method of execute.
        /// </summary>
        public abstract void Execute();
    }
}