//----------------------------------------------------------------------------
// <copyright file="MetaFeature.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements MetaFeature
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Cart
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Feature metadata.
    /// </summary>
    public class MetaFeature
    {
        #region Fields

        private string _name;
        private Dictionary<int, string> _values = new Dictionary<int, string>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Feature name.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _name = value;
            }
        }

        /// <summary>
        /// Gets Feature values or scope.
        /// </summary>
        public Dictionary<int, string> Values
        {
            get { return _values; }
        }

        #endregion
    }
}