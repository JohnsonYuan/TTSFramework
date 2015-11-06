//----------------------------------------------------------------------------
// <copyright file="LexicalAttribute.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements lexicon attribute class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Core
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Lexical Attribute which are defined in Lexical Attribute schema.
    /// </summary>
    public class LexicalAttribute
    {
        #region Fields
        private string _category;
        private string _value;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="LexicalAttribute"/> class.
        /// </summary>
        /// <param name="category">Category.</param>
        /// <param name="value">Value.</param>
        public LexicalAttribute(string category, string value)
        {
            if (string.IsNullOrEmpty(category))
            {
                throw new ArgumentNullException("category");
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException("category");
            }

            _category = category;
            _value = value;
        }

        #region Properties
        /// <summary>
        /// Gets Category name.
        /// </summary>
        public string Category
        {
            get { return _category; }
        }

        /// <summary>
        /// Gets Value name.
        /// </summary>
        public string Value
        {
            get { return _value; }
        }

        #endregion
    }
}