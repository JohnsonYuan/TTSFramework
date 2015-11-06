//----------------------------------------------------------------------------
// <copyright file="Speaker.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements speaker information
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Gender.
    /// </summary>
    public enum Gender
    {
        /// <summary>
        /// Male.
        /// </summary>
        Male,

        /// <summary>
        /// Female.
        /// </summary>
        Female,

        /// <summary>
        /// Child.
        /// </summary>
        Child,

        /// <summary>
        /// Neutral.
        /// </summary>
        Neutral
    }

    /// <summary>
    /// Speaker to descript speaker information.
    /// </summary>
    public class Speaker
    {
        #region Fields

        private Gender _gender;
        private Language _primaryLanguage;
        private string _name;

        #endregion

        #region Properties
        
        /// <summary>
        /// Gets or sets Name.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets or sets Language.
        /// </summary>
        public Language PrimaryLanguage
        {
            get { return _primaryLanguage; }
            set { _primaryLanguage = value; }
        }

        /// <summary>
        /// Gets or sets Gender.
        /// </summary>
        public Gender Gender
        {
            get { return _gender; }
            set { _gender = value; }
        }

        /// <summary>
        /// Gets Gender flag.
        /// </summary>
        public string GenderLetter
        {
            get
            {
                string genderLetter = string.Empty;
                if (_gender == Gender.Neutral)
                {
                    genderLetter = "i";
                }
                else
                {
                    genderLetter = _gender.ToString().Substring(0, 1).ToLower(CultureInfo.CurrentCulture);
                }

                return genderLetter;
            }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Clone.
        /// </summary>
        /// <returns>Speaker instance cloned.</returns>
        public Speaker Clone()
        {
            Speaker other = new Speaker();
            other.Gender = this.Gender;
            other.PrimaryLanguage = this.PrimaryLanguage;
            other.Name = this.Name;

            return other;
        }

        #endregion
    }
}