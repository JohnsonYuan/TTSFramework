//----------------------------------------------------------------------------
// <copyright file="TtsMetaUnit.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements TTS Meta Unit class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// TTS meta-phone information.
    /// This phone should corresponding with TTS phone.
    /// </summary>
    public class TtsMetaPhone
    {
        #region Const fileds

        /// <summary>
        /// Match TTS tone regex.
        /// </summary>
        public const string TtsToneRegex = @"^ *(?<index>t[1-9][0-9]*) *$";

        #endregion

        #region Fields

        private Language _language;

        private string _name;

        // Context tone ID
        private int _tone;

        private int _tcgppScore;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsMetaPhone"/> class.
        /// </summary>
        /// <param name="language">Language of the TtsMetaPhone.</param>
        public TtsMetaPhone(Language language)
        {
            _language = language;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets TCGPP score of this phone.
        /// </summary>
        public int TcgppScore
        {
            get { return _tcgppScore; }
            set { _tcgppScore = value; }
        }

        /// <summary>
        /// Gets or sets Tone context ID of this phone.
        /// </summary>
        public int Tone
        {
            get { return _tone; }
            set { _tone = value; }
        }

        /// <summary>
        /// Gets Tone string of this phone.
        /// </summary>
        public string ToneString
        {
            get { return Helper.NeutralFormat("t{0}", _tone); }
        }

        /// <summary>
        /// Gets or sets Phone name.
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
        /// Gets Phone full name, in this format: phone+tone.
        /// </summary>
        public string FullName
        {
            get
            {
                string fullName = _name;
                if (_tone != ToneManager.NoneContextTone)
                {
                    ToneManager toneManager = Localor.GetPhoneme(_language).ToneManager;
                    fullName = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}",
                        _name,
                        TtsUnit.PhoneDelimiter,
                        toneManager.GetNameFromContextId(_tone));
                }

                return fullName;
            }
        }

        /// <summary>
        /// Gets Phone align name, in this format: phonetone. 
        /// This name is used for HTK model training and alignment.
        /// </summary>
        public string AlignName
        {
            get
            {
                string aligmName = _name;
                if (_tone != ToneManager.NoneContextTone)
                {
                    ToneManager toneManager = Localor.GetPhoneme(_language).ToneManager;
                    aligmName = string.Format(CultureInfo.InvariantCulture, "{0}{1}",
                        _name,
                        toneManager.GetNameFromContextId(_tone));
                }

                return aligmName;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get phone string of specified phones.
        /// </summary>
        /// <param name="separator">Separator for the joint phone.</param>
        /// <param name="phones">TtsPhones to be joint.</param>
        /// <param name="startIndex">Start index from where to join the phone.</param>
        /// <param name="count">Cont of the phone to be joint.</param>
        /// <returns>Full name of all the phones to be joint.</returns>
        public static string Join(string separator,
            TtsMetaPhone[] phones, int startIndex, int count)
        {
            if (string.IsNullOrEmpty(separator))
            {
                throw new ArgumentNullException("separator");
            }

            if (phones == null)
            {
                throw new ArgumentNullException("phones");
            }

            if (startIndex >= phones.Length || count <= 0 || startIndex + count - 1 >= phones.Length)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Invalid index/lenght parameter [phones.Length=" +
                    "{0}, startIndex={1}, count={2}", phones.Length, startIndex, count));
            }

            StringBuilder sb = new StringBuilder();
            for (int i = startIndex; i < startIndex + count; i++)
            {
                if (sb.Length > 0)
                {
                    sb.Append(separator);
                }

                sb.Append(phones[i].Name);
                if (phones[i].Tone != ToneManager.NoneContextTone)
                {
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "{0}{1}", separator, phones[i].ToneString));
                }
            }

            return sb.ToString();
        }
        #endregion
    }

    /// <summary>
    /// TTS meta-unit information.
    /// </summary>
    public class TtsMetaUnit
    {
        #region Fields

        private int _id;
        private string _name;
        private Language _language;
        private TtsMetaPhone[] _phones;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsMetaUnit"/> class.
        /// </summary>
        /// <param name="language">Language of the meta unit.</param>
        public TtsMetaUnit(Language language)
        {
            _language = language;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this unit is a special unit, 
        /// Which is beginning and ending with underscore.
        /// </summary>
        public bool Special
        {
            get
            {
                Match specialMatch = Regex.Match(Name, "^_(.*)_$");
                return specialMatch.Success;
            }
        }

        /// <summary>
        /// Gets or sets Language, which this unit belongs to.
        /// </summary>
        public Language Language
        {
            get { return _language; }
            set { _language = value; }
        }

        /// <summary>
        /// Gets or sets Name of this meta-unit. This also should be unique among all meta-unit in one language
        /// Name should be combinition of phone and tone, using " " or "+" as delimeter.
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

                // If reset the _name, need rebuild _phones.
                _phones = null;
                _name = value;
            }
        }

        /// <summary>
        /// Gets or sets Meta-unit identify.
        /// </summary>
        public int Id
        {
            get
            {
                return _id;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _id = value;
            }
        }

        /// <summary>
        /// Gets The most left phone of this slice.
        /// </summary>
        public string LeftPhone
        {
            get
            {
                if (_phones == null)
                {
                    _phones = BuildPhones(Language, Name);
                }

                return _phones[0].Name;
            }
        }

        /// <summary>
        /// Gets Phone array of this slice.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays", Justification = "Ignore.")]
        public TtsMetaPhone[] Phones
        {
            get
            {
                if (_phones == null)
                {
                    _phones = BuildPhones(Language, Name);
                }

                return _phones;
            }
        }

        /// <summary>
        /// Gets The most right phone of the slice.
        /// </summary>
        public string RightPhone
        {
            get
            {
                if (_phones == null)
                {
                    _phones = BuildPhones(Language, Name);
                }

                return _phones[_phones.Length - 1].Name;
            }
        }

        /// <summary>
        /// Gets The most right tone of the slice.
        /// </summary>
        public int RightTone
        {
            get
            {
                if (_phones == null)
                {
                    _phones = BuildPhones(Language, Name);
                }

                int tone = ToneManager.NoneContextTone;
                for (int i = _phones.Length - 1; i >= 0; i--)
                {
                    if (_phones[i].Tone != ToneManager.NoneContextTone)
                    {
                        tone = _phones[i].Tone;
                        break;
                    }
                }

                return tone;
            }
        }

        /// <summary>
        /// Gets The most left tone of the slice.
        /// </summary>
        public int LeftTone
        {
            get
            {
                if (_phones == null)
                {
                    _phones = BuildPhones(Language, Name);
                }

                int tone = ToneManager.NoneContextTone;
                for (int i = 0; i < _phones.Length; i++)
                {
                    if (_phones[i].Tone != ToneManager.NoneContextTone)
                    {
                        tone = _phones[i].Tone;
                        break;
                    }
                }

                return tone;
            }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Build phone names array of the unit, not contain tone.
        /// </summary>
        /// <param name="language">Language of the unit.</param>
        /// <param name="unit">Unit to be processed.</param>
        /// <returns>Phone name array, not contain tone.</returns>
        public static string[] BuildPhoneNames(Language language, string unit)
        {
            TtsMetaPhone[] ttsMetaPhones = BuildPhones(language, unit);
            return BuildPhoneNames(ttsMetaPhones);
        }

        /// <summary>
        /// Build phone names array of the unit, not contain tone.
        /// </summary>
        /// <param name="phones">TtsMetaPhone array to be processed.</param>
        /// <returns>Phone name array, not contain tone.</returns>
        public static string[] BuildPhoneNames(TtsMetaPhone[] phones)
        {
            if (phones == null || phones.Length == 0)
            {
                throw new ArgumentNullException("phones");
            }

            List<string> phonesString = new List<string>();
            foreach (TtsMetaPhone phone in phones)
            {
                phonesString.Add(phone.Name);
            }

            return phonesString.ToArray();
        }

        /// <summary>
        /// Build a phone array from a slice string.
        /// </summary>
        /// <param name="language">Language of the unit.</param>
        /// <param name="unit">Slice string, the text should only contain phone and tone,
        /// but not contain stress.</param>
        /// <returns>Phone array.</returns>
        public static TtsMetaPhone[] BuildPhones(Language language, string unit)
        {
            // should use mapping table for zh-CN
            // this is for en-US
            if (string.IsNullOrEmpty(unit))
            {
                throw new ArgumentNullException("unit");
            }

            List<TtsMetaPhone> phones = new List<TtsMetaPhone>();

            string[] elements = unit.Split(new char[]
                {
                    ' ', '+'
                },
                StringSplitOptions.RemoveEmptyEntries);
            ToneManager toneManager = Localor.GetPhoneme(language).ToneManager;

            for (int i = 0; i < elements.Length; i++)
            {
                TtsMetaPhone phone = new TtsMetaPhone(language);
                phone.Name = elements[i];

                // Check the next element is tone.
                if (i + 1 < elements.Length)
                {
                    Match match = Regex.Match(elements[i + 1], TtsMetaPhone.TtsToneRegex);
                    if (match.Success)
                    {
                        string toneString = match.Groups["index"].Value;
                        phone.Tone = toneManager.GetContextToneId(toneString);
                        i++;
                    }
                }

                phones.Add(phone);
            }

            if (phones.Count <= 0)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Invalid unit format, can't extract " +
                    "phone from unit [{0}]", unit));
            }

            return phones.ToArray();
        }

        /// <summary>
        /// Get phone names of the unit.
        /// </summary>
        /// <returns>String.</returns>
        public string[] GetPhonesName()
        {
            if (_phones == null)
            {
                _phones = BuildPhones(Language, Name);
            }

            return BuildPhoneNames(_phones);
        }

        #endregion
    }
}