//----------------------------------------------------------------------------
// <copyright file="SliceData.cs" company="MICROSOFT">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements SliceData
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
    using System.Xml;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Truncate phone on which side of the pronunciation string, to make
    /// The remaining phone sequence in the slice table.
    /// </summary>
    public enum TruncateSide
    {
        /// <summary>
        /// Left side of the pronunciation.
        /// </summary>
        Left,

        /// <summary>
        /// Right side of the pronunciation.
        /// </summary>
        Right
    }

    /// <summary>
    /// Slice type.
    /// </summary>
    public enum SliceType
    {
        /// <summary>
        /// Onset slice type.
        /// </summary>
        Onset,

        /// <summary>
        /// Nucleus slice type.
        /// </summary>
        Nucleus,

        /// <summary>
        /// Coda slice type.
        /// </summary>
        Coda,

        /// <summary>
        /// Special slice type.
        /// </summary>
        Special
    }

    /// <summary>
    /// Definition of necleus truncate rule.
    /// </summary>
    public class TruncateRule
    {
        #region Fields

        private TruncateSide _side;
        private string _phones;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="TruncateRule"/> class.
        /// </summary>
        /// <param name="side">Truncate side.</param>
        /// <param name="phones">Phones string to truncate.</param>
        public TruncateRule(TruncateSide side, string phones)
        {
            if (string.IsNullOrEmpty(phones))
            {
                throw new ArgumentNullException(phones);
            }

            _side = side;
            _phones = phones;
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets Phones, scope condition for this rule to apply.
        /// </summary>
        public string Phones
        {
            get
            {
                return _phones;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _phones = value;
                }
                else
                {
                    throw new ArgumentNullException("value");
                }
            }
        }

        /// <summary>
        /// Gets or sets Side, indicate which side(left/right) to apply this rule.
        /// </summary>
        public TruncateSide Side
        {
            get { return _side; }
            set { _side = value; }
        }
        #endregion
    }

    /// <summary>
    /// Truncate rule data.
    /// </summary>
    public class TruncateRuleData
    {
        #region Fields

        private Collection<TruncateRule> _nucleusTruncateRules =
                                   new Collection<TruncateRule>();

        private Language _language;

        #endregion

        #region Properties

        /// <summary>
        /// Gets Since the size of core nucleus set is always limited, so we need 
        /// Truncate phone into a core nucleus slice plus other marginal 
        /// Mono-phones
        ///  [sonorant *] core nucleus slice [sonorant *]
        /// <param />
        /// The rule will guide what kind of phone to truncate, and how it is 
        /// Truncated, like left-side first or right-side first.
        /// </summary>
        public Collection<TruncateRule> NucleusTruncateRules
        {
            get { return _nucleusTruncateRules; }
        }

        /// <summary>
        /// Gets or sets Language of the truncate rule.
        /// </summary>
        public Language Language
        {
            get { return _language; }
            set { _language = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Load slice data from file.
        /// </summary>
        /// <param name="filePath">Truncate rule data file path.</param>
        public void Load(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(filePath);
            }

            if (!File.Exists(filePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException), filePath);
            }

            using (StreamReader sr = new StreamReader(filePath))
            {
                Load(sr);
            }
        }

        /// <summary>
        /// Load unit table from stream.
        /// </summary>
        /// <param name="reader">Stream to read from.</param>
        public void Load(StreamReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            XmlHelper.Validate(reader.BaseStream, Localor.ConfigSchema);

            // Load dome
            XmlDocument dom = new XmlDocument();
            dom.Load(reader);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("tts", Localor.ConfigSchema.TargetNamespace);

            string truncateRulePath = Helper.NeutralFormat(@"tts:truncateRules[@lang=""{0}""]",
                Localor.LanguageToString(Language));
            XmlNode truncateRulesNode = dom.DocumentElement.SelectSingleNode(truncateRulePath, nsmgr);
            if (truncateRulesNode != null)
            {
                ParseTruncateRules(truncateRulesNode, nsmgr);
            }
        }

        /// <summary>
        /// Load truncation rules from XML format for 'tts' namespace.
        /// </summary>
        /// <param name="truncateRulesNode">Xml node of truncation rules.</param>
        /// <param name="nsmgr">Namespace of the truncation rules.</param>
        public void ParseTruncateRules(XmlNode truncateRulesNode, XmlNamespaceManager nsmgr)
        {
            List<string> invalidPhones = new List<string>();
            _nucleusTruncateRules.Clear();

            Phoneme phoneme = Localor.GetPhoneme(Language);
            XmlNodeList truncateRuleNodes = truncateRulesNode.SelectNodes(@"tts:truncateRule", nsmgr);

            if (truncateRuleNodes != null)
            {
                foreach (XmlNode ruleNode in truncateRuleNodes)
                {
                    TruncateSide side = (TruncateSide)Enum.Parse(typeof(TruncateSide),
                        ruleNode.SelectSingleNode(@"@side").Value);
                    List<string> phones = new List<string>();
                    foreach (XmlNode phoneValueNode in ruleNode.SelectNodes(@"tts:phone/@value", nsmgr))
                    {
                        string phone = phoneValueNode.InnerText.Trim();

                        if (phoneme.TtsSonorantPhones.IndexOf(phone) < 0)
                        {
                            invalidPhones.Add(phone);
                            continue;
                        }

                        phones.Add(phone);
                    }

                    if (phones.Count == 0)
                    {
                        string message = Helper.NeutralFormat("There is no phone in the truncation rule.");
                        throw new InvalidDataException(message);
                    }

                    TruncateRule rule = new TruncateRule(side, string.Join(" ", phones.ToArray()));

                    _nucleusTruncateRules.Add(rule);
                }

                if (invalidPhones.Count > 0)
                {
                    string message = Helper.NeutralFormat("The phones to truncate should be consonant sonorant." +
                        "These phones are [{0}].", string.Join(" ", invalidPhones.ToArray()));
                    throw new InvalidDataException(message);
                }
            }
        }

        /// <summary>
        /// Save truncation rules into given file.
        /// </summary>
        /// <param name="filePath">The location of the target file to save.</param>
        public void SaveTruncateRules(string filePath)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            settings.Indent = true;
            using (XmlWriter writer = XmlTextWriter.Create(filePath, settings))
            {
                writer.WriteStartDocument();

                writer.WriteStartElement("offline", Localor.ConfigSchema.TargetNamespace);

                SaveTruncateRules(writer);

                writer.WriteEndElement();

                writer.WriteEndDocument();
            }
        }

        /// <summary>
        /// Save truncation rules into Xml writer.
        /// </summary>
        /// <param name="writer">Xml writer to write out truncation rules.</param>
        public void SaveTruncateRules(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteStartElement("truncateRules");
            writer.WriteAttributeString("lang", Localor.LanguageToString(Language));

            foreach (TruncateRule rule in NucleusTruncateRules)
            {
                writer.WriteStartElement("truncateRule");
                writer.WriteAttributeString("side", rule.Side.ToString());

                string[] phones = rule.Phones.Split(new char[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);
                foreach (string phone in phones)
                {
                    writer.WriteStartElement("phone");
                    writer.WriteAttributeString("value", phone);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        #endregion
    }

    /// <summary>
    /// This syllable can be abstracted as a consonant-vowel-consonant
    /// Syllable, abbreviated CVC.In the one-syllable English word cat, 
    /// The nucleus is a, the onset c, the coda t, and the rime at. 
    /// <param />
    /// The basic idea of defining slice is to join some phone clusters, 
    /// Which are not easy to be separated or frequently used, into one unit.
    /// Such a joining operation is bounded to syllable boundaries. 
    /// <param />
    /// Slice is a set of atom units with the size between phone and syllable.
    /// <param />
    /// Here to address the problem, combining those phones hard to 
    /// Be separated or frequently used.
    ///     [sonorant *] xx [sonorant *]
    ///     xx denotes a vowel.
    /// </summary>
    public class SliceData
    {
        #region Fields, const, member variables, etc.

        // The unit string should be in format : phone+tone+phone+tone
        private Collection<string> _onsetSlices = new Collection<string>();
        private Collection<string> _nucleusSlices = new Collection<string>();
        private Collection<string> _codaSlices = new Collection<string>();

        private Dictionary<string, string> _specialSlices =
                                            new Dictionary<string, string>();

        private Language _language = Language.Neutral;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Language of this slice data for.
        /// </summary>
        public virtual Language Language
        {
            get { return _language; }
            set { _language = value; }
        }

        /// <summary>
        /// Gets Onset slice set
        /// The syllable onset is the sound or sounds occurring
        /// Before the nucleus.
        /// </summary>
        public Collection<string> OnsetSlices
        {
            get { return _onsetSlices; }
        }

        /// <summary>
        /// Gets Nucleus slice set
        /// The syllable nucleus is typically a sonorant, usually a vowel sound, 
        /// In the form of a monophthong, diphthong, or triphthong, 
        /// But sometimes sonorant consonants like [l] or [r].
        /// </summary>
        public Collection<string> NucleusSlices
        {
            get { return _nucleusSlices; }
        }

        /// <summary>
        /// Gets Coda slice set
        /// The syllable coda (literally 'tail') is the sound or sounds 
        /// That follow the nucleus. 
        /// </summary>
        public Collection<string> CodaSlices
        {
            get { return _codaSlices; }
        }

        /// <summary>
        /// Gets Onset slice full name list.
        /// </summary>
        public Collection<string> OnsetSlicesFullName
        {
            get
            {
                Collection<string> fullNames = new Collection<string>();

                foreach (string slice in _onsetSlices)
                {
                    fullNames.Add(SliceData.BuildFullUnitName(slice, SliceType.Onset));
                }

                return fullNames;
            }
        }

        /// <summary>
        /// Gets Nucleus slice full name list.
        /// </summary>
        public Collection<string> NucleusSlicesFullName
        {
            get
            {
                Collection<string> fullNames = new Collection<string>();

                foreach (string slice in _nucleusSlices)
                {
                    fullNames.Add(SliceData.BuildFullUnitName(slice, SliceType.Nucleus));
                }

                return fullNames;
            }
        }

        /// <summary>
        /// Gets Coda slice full name list.
        /// </summary>
        public Collection<string> CodaSlicesFullName
        {
            get
            {
                Collection<string> fullNames = new Collection<string>();

                foreach (string slice in _codaSlices)
                {
                    fullNames.Add(SliceData.BuildFullUnitName(slice, SliceType.Coda));
                }

                return fullNames;
            }
        }

        /// <summary>
        /// Gets Special slice, sometimes to address a special problem, 
        /// Like reading letters or common English word in non-English language,
        /// It would be fine to build a certain set of special units to 
        /// Cover those issure.
        /// <param />
        /// <example>
        ///     Letters.
        /// </example>
        /// </summary>
        public Dictionary<string, string> SpecialSlices
        {
            get { return _specialSlices; }
        }

        #endregion

        #region Static Operations

        /// <summary>
        /// Convert this unit to slide data.
        /// </summary>
        /// <param name="language">Language of the Slice data.</param>
        /// <param name="unitFullNames">Full unit name collections.</param>
        /// <returns>Converted result.</returns>
        public static SliceData ToSliceData(Language language, IEnumerable<string> unitFullNames)
        {
            if (unitFullNames == null)
            {
                throw new ArgumentNullException("unitFullNames");
            }

            SliceData sliceData = new SliceData();
            sliceData.Language = language;

            foreach (string name in unitFullNames)
            {
                sliceData.ParseUnit(name);
            }

            return sliceData;
        }

        /// <summary>
        /// Convert slice to full unit name.
        /// </summary>
        /// <param name="slice">
        ///     Slice string should be the same format with the ones in array:
        ///         _nucleusTruncateRules, _unsetTruncateRules, _codaTruncateRules.
        ///     For example : a+t1+b
        ///     The slice contains phone and tone.
        /// </param>
        /// <param name="type">Slice Type.</param>
        /// <returns>Full unit name.</returns>
        public static string BuildFullUnitName(string slice, SliceType type)
        {
            if (string.IsNullOrEmpty(slice))
            {
                throw new ArgumentNullException("slice");
            }

            string fullName = slice.Replace(" ", TtsUnit.PhoneDelimiter);
            switch (type)
            {
                case SliceType.Onset:
                    fullName = TtsUnit.OnsetPrefix + fullName;
                    break;
                case SliceType.Nucleus:
                    fullName = TtsUnit.NucleusPrefix + fullName;
                    break;
                case SliceType.Coda:
                    fullName = TtsUnit.CodaPrefix + fullName;
                    break;
                case SliceType.Special:
                    fullName = TtsUnit.NucleusPrefix + "_" + fullName + "_";
                    break;
                default:
                    Debug.Assert(false);
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "[{0}]: Invalid slice type. Only Onset, Nuclues, Coda and Speical are support",
                        type);
                    throw new NotSupportedException(message);
            }

            return fullName;
        }

        #endregion

        #region Operations

        /// <summary>
        /// Save Unit table into given file.
        /// </summary>
        /// <param name="filePath">The location of the target file to save.</param>
        public void SaveUnitTable(string filePath)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            settings.Indent = true;
            using (XmlWriter writer = XmlTextWriter.Create(filePath, settings))
            {
                writer.WriteStartDocument();

                writer.WriteStartElement("offline", Localor.ConfigSchema.TargetNamespace);

                SaveUnitTable(writer);

                writer.WriteEndElement();

                writer.WriteEndDocument();
            }
        }

        /// <summary>
        /// Save unit table into Xml writer.
        /// </summary>
        /// <param name="writer">Xml writer to write out unit table.</param>
        public void SaveUnitTable(XmlWriter writer)
        {
            writer.WriteStartElement("unitTable");
            writer.WriteAttributeString("lang", Localor.LanguageToString(Language));

            SaveUnit(writer, OnsetSlices, TtsUnit.OnsetPrefix);
            SaveUnit(writer, NucleusSlices, TtsUnit.NucleusPrefix);
            SaveUnit(writer, CodaSlices, TtsUnit.CodaPrefix);

            foreach (string key in SpecialSlices.Keys)
            {
                // No whitespace supported in the unit
                Debug.Assert(key.IndexOf(" ", StringComparison.Ordinal) < 0);

                writer.WriteStartElement("singleTokenUnit");
                string name = Helper.NeutralFormat("{0}_{1}_", TtsUnit.NucleusPrefix, key);
                writer.WriteAttributeString("name", name);
                writer.WriteAttributeString("pron", SpecialSlices[key]);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Indicating whether the unit list is empty.
        /// </summary>
        /// <returns>Whether the unit list is empty.</returns>
        public bool IsEmpty()
        {
            return _onsetSlices.Count == 0 && _nucleusSlices.Count == 0 &&
                _codaSlices.Count == 0 && _specialSlices.Count == 0;
        }

        /// <summary>
        /// Load offline language configuration file if UnitTable or TruncateRules exist.
        /// </summary>
        /// <param name="filePath">The location of the source file to load from.</param>
        /// <returns>Whether unit table loaded succesfully.</returns>
        public bool Load(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            bool succeeded = false;
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    succeeded = Load(reader);
                }
            }
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The configuration file [{0}] error is found.", filePath);
                throw new InvalidDataException(message, ide);
            }

            return succeeded;
        }

        /// <summary>
        /// Load unit table from stream.
        /// </summary>
        /// <param name="reader">Stream to read from.</param>
        /// <returns>Whether unit table loaded succesfully.</returns>
        public bool Load(StreamReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            XmlHelper.Validate(reader.BaseStream, Localor.ConfigSchema);

            // Load dome
            XmlDocument dom = new XmlDocument();
            dom.Load(reader);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("tts", Localor.ConfigSchema.TargetNamespace);

            string unitPath = Helper.NeutralFormat(@"tts:unitTable[@lang=""{0}""]",
                Localor.LanguageToString(Language));
            XmlNode unitTableNode = dom.DocumentElement.SelectSingleNode(unitPath, nsmgr);
            bool succeeded = false;
            if (unitTableNode != null)
            {
                ParseUnitTable(unitTableNode, nsmgr);
                succeeded = true;
            }

            return succeeded;
        }

        /// <summary>
        /// Load unit table from XML format for 'tts' namespace.
        /// </summary>
        /// <param name="unitTableNode">Unit table node to parse.</param>
        /// <param name="nsmgr">Namespace manager to look up during parsing.</param>
        public void ParseUnitTable(XmlNode unitTableNode, XmlNamespaceManager nsmgr)
        {
            OnsetSlices.Clear();
            NucleusSlices.Clear();
            CodaSlices.Clear();

            XmlNodeList nodeList = unitTableNode.SelectNodes("tts:unit", nsmgr);

            StringBuilder sb = new StringBuilder();
            foreach (XmlNode node in nodeList)
            {
                XmlElement ele = (XmlElement)node;
                string name = ele.GetAttribute("name");
                try
                {
                    ParseUnit(name);
                }
                catch (InvalidDataException e)
                {
                    sb.AppendLine(e.Message);
                }
            }

            // Throw InvalidDataException contains all invalid unit definition.
            if (sb.Length > 0)
            {
                throw new InvalidDataException(sb.ToString());
            }

            ParseSingleTokenUnit(unitTableNode, nsmgr);
        }

        /// <summary>
        /// Parse unit full name.
        /// </summary>
        /// <param name="unitFullName">Unit full name.</param>
        public void ParseUnit(string unitFullName)
        {
            if (unitFullName.StartsWith(TtsUnit.OnsetPrefix, StringComparison.Ordinal))
            {
                ParseUnit(unitFullName, TtsUnit.OnsetPrefix, OnsetSlices);
            }
            else if (unitFullName.StartsWith(TtsUnit.NucleusPrefix, StringComparison.Ordinal))
            {
                ParseUnit(unitFullName, TtsUnit.NucleusPrefix, NucleusSlices);
            }
            else if (unitFullName.StartsWith(TtsUnit.CodaPrefix, StringComparison.Ordinal))
            {
                ParseUnit(unitFullName, TtsUnit.CodaPrefix, CodaSlices);
            }
            else
            {
                string message = Helper.NeutralFormat("Unknown unit type found for [{0}]", unitFullName);
                throw new InvalidDataException(message);
            }
        }

        /// <summary>
        /// Tell a slice is nucleus, through checking whether
        /// 1) phone sequence already exists in the nucleus set
        /// 2) or there is any vowel in the phone set.
        /// </summary>
        /// <param name="ttsMetaUnit">TtsMetaUnit to test.</param>
        /// <returns>Ture if yes, otherwise false.</returns>
        public bool IsNucleus(TtsMetaUnit ttsMetaUnit)
        {
            if (ttsMetaUnit == null)
            {
                throw new ArgumentNullException("ttsMetaUnit");
            }

            // if the slice already exists in the nucleus set,
            // then return directly
            if (_nucleusSlices.IndexOf(ttsMetaUnit.Name) >= 0)
            {
                return true;
            }

            // else, check if there any vowel in the phone array
            Phoneme phoneme = Localor.GetPhoneme(Language);
            for (int i = 0; i < ttsMetaUnit.Phones.Length; i++)
            {
                if (phoneme.TtsVowelPhones.IndexOf(ttsMetaUnit.Phones[i].Name) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Parse unit.
        /// </summary>
        /// <param name="name">Name of unit.</param>
        /// <param name="prefix">Prefix with unit name.</param>
        /// <param name="units">Container of the result unit.</param>
        private static void ParseUnit(string name, string prefix, Collection<string> units)
        {
            string unit = name.Substring(prefix.Length);
            unit = unit.Replace(TtsUnit.PhoneDelimiter, " ");
            if (units.IndexOf(unit) >= 0)
            {
                string message = Helper.NeutralFormat("Duplicate unit type found for [{0}]", name);
                throw new InvalidDataException(message);
            }

            units.Add(unit);
        }

        /// <summary>
        /// Save unit out into writer.
        /// </summary>
        /// <param name="writer">Xml writer to write out unit.</param>
        /// <param name="units">Unit collection to write.</param>
        /// <param name="prefix">Prefix for these units.</param>
        private static void SaveUnit(XmlWriter writer, Collection<string> units, string prefix)
        {
            foreach (string unit in units)
            {
                writer.WriteStartElement("unit");
                string name = prefix + unit.Replace(" ", TtsUnit.PhoneDelimiter);
                writer.WriteAttributeString("name", name);
                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Parse single token unit.
        /// </summary>
        /// <param name="unitTableNode">Unit table node to read data from.</param>
        /// <param name="nsmgr">Namespace managemer to parse the data.</param>
        private void ParseSingleTokenUnit(XmlNode unitTableNode, XmlNamespaceManager nsmgr)
        {
            SpecialSlices.Clear();
            XmlNodeList nodeList = unitTableNode.SelectNodes(@"tts:singleTokenUnit", nsmgr);
            foreach (XmlNode node in nodeList)
            {
                XmlElement ele = (XmlElement)node;
                string name = ele.GetAttribute("name");

                if (!name.StartsWith(TtsUnit.NucleusPrefix, StringComparison.Ordinal))
                {
                    string message = Helper.NeutralFormat("Single token unit [{0}] should be nucleus, " +
                        "which starts with [{1}]", name, TtsUnit.NucleusPrefix);
                    throw new InvalidDataException(message);
                }

                name = name.Substring(TtsUnit.NucleusPrefix.Length);

                if (!name.StartsWith("_", StringComparison.Ordinal) || !name.EndsWith("_", StringComparison.Ordinal))
                {
                    string message = Helper.NeutralFormat("Single token unit [{1}{0}] should be " +
                        "with name like {1}_X_, which starts and ends with '_'.",
                        TtsUnit.NucleusPrefix, name);
                    throw new InvalidDataException(message);
                }

                // Remove heading and tailing underscore.
                name = name.Substring(1);
                name = name.Substring(0, name.Length - 1);

                if (SpecialSlices.ContainsKey(name))
                {
                    string message = Helper.NeutralFormat("Duplicate unit type found for [{0}]", name);
                    throw new InvalidDataException(message);
                }

                SpecialSlices.Add(name, ele.GetAttribute("pron"));
            }
        }

        #endregion
    }
}