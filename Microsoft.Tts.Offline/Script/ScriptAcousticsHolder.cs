//----------------------------------------------------------------------------
// <copyright file="ScriptAcousticsHolder.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script acoustics holder
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    /// <summary>
    /// Script Acoustics holder.
    /// </summary>
    public class ScriptAcousticsHolder
    {
        #region Fields

        private ScriptAcoustics _acoustics;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this object has acoustics value or not.
        /// </summary>
        public bool HasAcousticsValue
        {
            get
            {
                return _acoustics != null;
            }
        }

        /// <summary>
        /// Gets or sets Acoustics.
        /// </summary>
        public ScriptAcoustics Acoustics
        {
            get
            {
                return _acoustics;
            }

            set
            {
                _acoustics = value;
            }
        }

        #endregion

        #region public operations

        /// <summary>
        /// Remove the acoustics vlaue.
        /// </summary>
        public void RemoveAcousticsValue()
        {
            _acoustics = null;
        }

        #endregion
    }
}