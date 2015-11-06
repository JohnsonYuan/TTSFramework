//----------------------------------------------------------------------------
// <copyright file="TtsUnit.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements class for TTS unit
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Class for TTS unit.
    /// </summary>
    public class TtsUnit
    {
        #region Const fields

        /// <summary>
        /// Phone connector pattern, @"\s|\+".
        /// </summary>
        public const string PhoneConnectorPattern = @"\s|\+";

        /// <summary>
        /// Prefix of Onset unit name.
        /// </summary>
        public const string OnsetPrefix = "os_";

        /// <summary>
        /// Prefix of Nucleus unit name.
        /// </summary>
        public const string NucleusPrefix = "nc_";

        /// <summary>
        /// Prefix of Coda unit name.
        /// </summary>
        public const string CodaPrefix = "cd_";

        /// <summary>
        /// Delimiter used to seperate phones in the unit name.
        /// </summary>
        public const string PhoneDelimiter = "+";

        /// <summary>
        /// Delimiter used to seperate units in the unit group.
        /// </summary>
        public const string UnitDelimiter = ".";

        #endregion

        #region Fields

        private TtsMetaUnit _metaUnit;
        private TtsUnitFeature _feature;

        private TtsBreak _ttsBreak = TtsBreak.Phone;
        private WordType _wordType = WordType.Normal;
        private ScriptWord _word;

        private Language _language;
        private Collection<Language> _languages = new Collection<Language>();

        private int _offsetInString;
        private int _lengthInString;

        private object _tag;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsUnit"/> class.
        /// </summary>
        /// <param name="language">Language for this unit.</param>
        public TtsUnit(Language language)
        {
            _language = language;
            _languages.Add(_language);
            Feature = new TtsUnitFeature();
            MetaUnit = new TtsMetaUnit(language);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets All languages.
        /// </summary>
        public Collection<Language> Languages
        {
            get 
            {
                return _languages;
            }

            set
            {
                _languages = value;
            }
        }

        /// <summary>
        /// Gets Language.
        /// </summary>
        public Language Language
        {
            get 
            { 
                return _language; 
            }
        }

        /// <summary>
        /// Gets Name leading with "os_", "nc_" or "cd_".
        /// </summary>
        public string FullName
        {
            get { return BuildUnitFullName(MetaUnit.Name, Feature.PosInSyllable); }
        } 

        /// <summary>
        /// Gets or sets LengthInString.
        /// </summary>
        public int LengthInString
        {
            get { return _lengthInString; }
            set { _lengthInString = value; }
        }

        /// <summary>
        /// Gets or sets OffsetInString.
        /// </summary>
        public int OffsetInString
        {
            get { return _offsetInString; }
            set { _offsetInString = value; }
        }

        /// <summary>
        /// Gets or sets Word.
        /// </summary>
        public ScriptWord Word
        {
            get
            {
                return _word;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _word = value;
            }
        }

        /// <summary>
        /// Gets or sets Tag.
        /// </summary>
        public object Tag
        {
            get
            {
                return _tag;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _tag = value;
            }
        }

        /// <summary>
        /// Gets or sets Word type, mainly for punctuation.
        /// </summary>
        public WordType WordType
        {
            get { return _wordType; }
            set { _wordType = value; }
        }

        /// <summary>
        /// Gets or sets Break level of this unit, this is used during calculation, 
        /// Which will be transfered to PosInWord, or PosInSentence.
        /// </summary>
        public TtsBreak TtsBreak
        {
            get { return _ttsBreak; }
            set { _ttsBreak = value; }
        }

        /// <summary>
        /// Gets or sets Meta data of this unit.
        /// </summary>
        public TtsMetaUnit MetaUnit
        {
            get
            {
                return _metaUnit;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _metaUnit = value;
            }
        }

        /// <summary>
        /// Gets or sets Feature of this unit instance.
        /// </summary>
        public TtsUnitFeature Feature
        {
            get
            {
                return _feature;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _feature = value;
            }
        }

        /// <summary>
        /// Gets Plain description string of this unit instance, use blank space as phone and tone delimeter.
        /// </summary>
        public string PlainDescription
        {
            get
            {
                return Description.Replace("+", " ");
            }
        }

        /// <summary>
        /// Gets Description string of this unit instance, use "+" as phone and tone delimeter.
        /// </summary>
        public string Description
        {
            get
            {
                StringBuilder description = new StringBuilder();

                Collection<Phoneme> phonemes = new Collection<Phoneme>();

                foreach (Language language in Languages)
                {
                    phonemes.Add(Localor.GetPhoneme(language));
                }

                int stressPosition = -1;
                for (int i = 0; i < MetaUnit.Phones.Length; i++)
                {
                    string phone = MetaUnit.Phones[i].FullName;
                    if (description.Length == 0)
                    {
                        description.Append(phone);
                    }
                    else
                    {
                        description.AppendFormat(CultureInfo.InvariantCulture,
                            " {0}", phone);
                    }

                    // If have mult-vowel(ja-JP), should put stress after the last vowel.
                    foreach (Phoneme phoneme in phonemes)
                    {
                        if (phoneme.TtsVowelPhones.IndexOf(phone) >= 0 &&
                            Feature.TtsStress != TtsStress.None)
                        {
                            stressPosition = description.Length;
                            break;
                        }
                    }
                }

                if (stressPosition > -1)
                {
                    description.Insert(stressPosition, Helper.NeutralFormat(" {0}", ((int)Feature.TtsStress).ToString(CultureInfo.InvariantCulture)));
                }

                return description.ToString();
            }
        }

        /// <summary>
        /// Gets Linguistic feature string of this unit.
        /// </summary>
        public string LinguisticFeatureString
        {
            get
            {
                if (Feature == null || MetaUnit == null)
                {
                    return null;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(CultureInfo.InvariantCulture, " {0,2}", (int)Feature.PosInSentence);
                sb.AppendFormat(CultureInfo.InvariantCulture, " {0,2}", (int)Feature.PosInWord);
                sb.AppendFormat(CultureInfo.InvariantCulture, " {0,2}", (int)Feature.PosInSyllable);
                sb.AppendFormat(CultureInfo.InvariantCulture, " {0,2}", Feature.LeftContextPhone);
                sb.AppendFormat(CultureInfo.InvariantCulture, " {0,2}", Feature.RightContextPhone);
                sb.AppendFormat(CultureInfo.InvariantCulture, " {0,2}", Feature.LeftContextTone);
                sb.AppendFormat(CultureInfo.InvariantCulture, " {0,2}", Feature.RightContextTone);
                sb.AppendFormat(CultureInfo.InvariantCulture, " {0,2}", (int)Feature.TtsStress);
                sb.AppendFormat(CultureInfo.InvariantCulture, " {0,2}", (int)Feature.TtsEmphasis);
                sb.AppendFormat(CultureInfo.InvariantCulture, " {0,2}", (int)Feature.TtsWordTone);
                sb.AppendFormat(CultureInfo.InvariantCulture, " {0,2}", (int)this.TtsBreak);
                if (this.WordType == WordType.Normal)
                {
                    sb.Append("  0");
                }
                else if (this.WordType == WordType.OtherPunctuation)
                {
                    // not support yet
                    sb.Append("  0");
                }
                else
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture,
                        " {0,2}", (int)this.WordType);
                }

                sb.AppendFormat(CultureInfo.InvariantCulture, " {0}", FullName);

                return sb.ToString();
            }
        }

        #endregion

        #region Static operations

        /// <summary>
        /// Read all tts unit from Unit Linguistic FeatureVector file.
        /// </summary>
        /// <param name="filePath">Unit Linguistic FeatureVector file.</param>
        /// <param name="language">Language of the unit file.</param>
        /// <returns>Unit dictionary, indexing by (sentence id + index in sentence).</returns>
        public static Dictionary<string, TtsUnit> ReadAllData(string filePath,
            Language language)
        {
            Dictionary<string, TtsUnit> units = new Dictionary<string, TtsUnit>();

            using (StreamReader sr = new StreamReader(filePath))
            {
                string line = null;
                while (string.IsNullOrEmpty(line = sr.ReadLine()) != true)
                {
                    TtsUnit unit = new TtsUnit(language);

                    string[] items = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    unit.Feature = new TtsUnitFeature();
                    unit.Feature.Parse(items, 2);
                    unit.MetaUnit = new TtsMetaUnit(language);
                    unit.MetaUnit.Name = items[items.Length - 1];
                    string key = items[0] + " " + items[1];

                    units.Add(key, unit);
                }
            }

            return units;
        }

        /// <summary>
        /// Build unit full name, this is, prefix "os_", "nc_" or "cd_" to the unit name.
        /// </summary>
        /// <param name="name">Unit name.</param>
        /// <param name="slicePos">Position in syllable feature of the unit.</param>
        /// <returns>Full name string of the unit.</returns>
        public static string BuildUnitFullName(string name, PosInSyllable slicePos)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (name.StartsWith("os_", StringComparison.Ordinal) || 
                        name.StartsWith("nc_", StringComparison.Ordinal) || 
                        name.StartsWith("cd_", StringComparison.Ordinal))
            {
                throw new ArgumentException("unit name [{0}] should be start with os_, nc_ or cd_", name);
            }

            switch (slicePos)
            {
                case PosInSyllable.Onset:
                case PosInSyllable.OnsetNext:
                    return "os_" + name;
                case PosInSyllable.NucleusInV:
                case PosInSyllable.NucleusInVC:
                case PosInSyllable.NucleusInCV:
                case PosInSyllable.NucleusInCVC:
                    return "nc_" + name;
                case PosInSyllable.CodaNext:
                case PosInSyllable.Coda:
                    return "cd_" + name;
                default:
                    Debug.Assert(false);
                    string str1 = "Only Onset, OnsetNext, NucleusInV, NucleusInVC, NucleusInCV, NucleusInCVC, CodaNext or Code is ";
                    string str2 = "supported for unit full name generation. ";
                    string str3 = "But position in syllable [{0}] is found.";
                    string message = string.Format(CultureInfo.InvariantCulture, str1 + str2 + str3, slicePos);
                    throw new NotSupportedException(message);
            }
        }

        /// <summary>
        /// Convert unit full name to unit name, reomve prefix of
        /// Unit full name (os_, nc_, or cd_).
        /// </summary>
        /// <param name="fullName">Unit full name.</param>
        /// <returns>Unit name.</returns>
        public static string BuildUnitName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentNullException("fullName");
            }

            if ((fullName.Length > 3) && (fullName.StartsWith("os_", StringComparison.Ordinal) ||
                fullName.StartsWith("nc_", StringComparison.Ordinal) || fullName.StartsWith("cd_", StringComparison.Ordinal)))
            {
                return fullName.Substring(3);
            }
            else
            {
                string message = Helper.NeutralFormat(
                    "[{0}]: Invalid unit full name",
                    fullName);
                throw new ArgumentException(message);
            }
        }

        #endregion

        #region Presentation operations

        /// <summary>
        /// Converts the value of this instance to a System.string.
        /// </summary>
        /// <returns>A string whose value is the same as name of this instance.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(MetaUnit.Name);

            return sb.ToString();
        }

        #endregion
    }
}