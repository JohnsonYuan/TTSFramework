//----------------------------------------------------------------------------
// <copyright file="HtsQuestionSet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements HTS question set object model
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Font.Hts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.Offline.Htk;

    /// <summary>
    /// Question set header of HTS font.
    /// </summary>
    public class HtsQuestionSetHeader
    {
        /// <summary>
        /// Initializes a new instance of the HtsQuestionSetHeader class.
        /// </summary>
        public HtsQuestionSetHeader()
        {
            HasQuestionName = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether current font has question names.
        /// </summary>
        public bool HasQuestionName
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Question set.
    /// </summary>
    public class HtsQuestionSet
    {
        #region Fields
        private HtsQuestionSetHeader _header = new HtsQuestionSetHeader();
        private HashSet<string> _customFeatures = new HashSet<string>();
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets question set header.
        /// </summary>
        public HtsQuestionSetHeader Header
        {
            get { return _header; }
            set { _header = value; }
        }

        /// <summary>
        /// Gets or sets the customized feature list.
        /// </summary>
        public HashSet<string> CustomFeatures
        {
            get { return _customFeatures; }
            set { _customFeatures = value; }
        }

        /// <summary>
        /// Gets or sets question items.
        /// </summary>
        public IEnumerable<Question> Items
        {
            get;
            set;
        }

        #endregion
    }
}