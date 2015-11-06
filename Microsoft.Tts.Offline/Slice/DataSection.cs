//----------------------------------------------------------------------------
// <copyright file="DataSection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements data section in file storage
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;

    /// <summary>
    /// Data section definition.
    /// </summary>
    public class DataSection
    {
        #region Fields

        private string _name;
        private int _number;
        private int _cumulation;
        private Collection<string> _lines = new Collection<string>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets String lines of this data section.
        /// </summary>
        public Collection<string> Lines
        {
            get { return _lines; }
        }

        /// <summary>
        /// Gets or sets Cumulation.
        /// </summary>
        public int Cumulation
        {
            get { return _cumulation; }
            set { _cumulation = value; }
        }

        /// <summary>
        /// Gets or sets Number of this data section.
        /// </summary>
        public int Number
        {
            get { return _number; }
            set { _number = value; }
        }

        /// <summary>
        /// Gets or sets Label of this data section.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _name = value;
                }
                else
                {
                    throw new ArgumentNullException("value");
                }
            }
        }
        #endregion
    }
}