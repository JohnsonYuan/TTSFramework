//----------------------------------------------------------------------------
// <copyright file="PhoneMap.cs" company="MICROSOFT">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Phoneme
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Phone mapping table, which could be one to many map, which is delimited by whitespace ' '.
    /// </summary>
    public class PhoneMap : XmlDataFile
    {
        #region Predefined phone mapping types

        /// <summary>
        /// TTS phone.
        /// </summary>
        public const string TtsPhone = "TtsPhone";

        /// <summary>
        /// TTS syllable.
        /// </summary>
        public const string TtsSyllable = "TtsSyllable";

        /// <summary>
        /// SAPI phone id.
        /// </summary>
        public const string SapiPhoneId = "SapiPhoneId";

        /// <summary>
        /// SAPI viseme id.
        /// </summary>
        public const string SapiVisemeId = "SapiVisemeId";

        /// <summary>
        /// SR phone.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        public const string SrPhone = "SrPhone";

        /// <summary>
        /// IPA phone.
        /// </summary>
        public const string IpaPhone = "IpaPhone";

        #endregion

        private static XmlSchema _schema;

        private string _source;
        private string _target;
        private Dictionary<string, string> _items = new Dictionary<string, string>(StringComparer.Ordinal);

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="PhoneMap"/> class.
        /// </summary>
        public PhoneMap()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PhoneMap"/> class.
        /// </summary>
        /// <param name="language">Language of this table.</param>
        /// <param name="source">Source phone type.</param>
        /// <param name="target">Target phone type.</param>
        public PhoneMap(Language language, string source, string target)
            : base(language)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException("source");
            }

            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException("target");
            }

            _source = source;
            _target = target;
        }

        #endregion

        /// <summary>
        /// PhoneMap error.
        /// </summary>
        public enum PhoneMapError
        {
            /// <summary>
            /// Duplicate item id.
            /// Parameters: 
            /// {0}: target phone which has more than one source phones.
            /// {1}: the first source phone used for reverting.
            /// {2}: the second source phone.
            /// </summary>
            [ErrorAttribute(Message =
                "Detected one target phone [{0}] mapped to more than one " +
                "source phone [{1}],[{2}] when revert mapping, use [{1}] " +
                "when revert mapping.",
                Severity = ErrorSeverity.Warning)]
            DuplicateTargetName
        }

        #region Properties

        /// <summary>
        /// Gets Configuration schema.
        /// </summary>
        public override XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.phonemap.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets or sets Source of this mapping.
        /// </summary>
        public string Source
        {
            get
            {
                return _source;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _source = value;
            }
        }

        /// <summary>
        /// Gets or sets Target of this mapping.
        /// </summary>
        public string Target
        {
            get
            {
                return _target;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _target = value;
            }
        }

        /// <summary>
        /// Gets Items.
        /// </summary>
        public Dictionary<string, string> Items
        {
            get { return _items; }
        }

        #endregion

        /// <summary>
        /// Create a phone map from file path.
        /// </summary>
        /// <param name="filePath">The filepath.</param>
        /// <returns>PhoneMap.</returns>
        public static PhoneMap CreatePhoneMap(string filePath)
        {
            PhoneMap phoneMap = new PhoneMap();
            phoneMap.Load(filePath);
            return phoneMap;
        }

        /// <summary>
        /// Reverse source and target in phonemap.
        /// </summary>
        /// <returns>ErrorSet.</returns>
        public ErrorSet Reverse()
        {
            ErrorSet errorSet = new ErrorSet();
            string sourceName = _source;
            _source = _target;
            _target = sourceName;
            Dictionary<string, string> revertedMap = new Dictionary<string, string>();
            foreach (string sourcePhone in _items.Keys)
            {
                if (!revertedMap.ContainsKey(_items[sourcePhone]))
                {
                    revertedMap.Add(_items[sourcePhone], sourcePhone);
                }
                else
                {
                    errorSet.Add(PhoneMapError.DuplicateTargetName,
                        _items[sourcePhone],
                        _items[sourcePhone],
                        sourcePhone);
                }
            }

            if (errorSet.GetSeverityCount(ErrorSeverity.MustFix) > 0)
            {
                throw new InvalidDataException(errorSet.ErrorsString());
            }
            else
            {
                _items.Clear();
                foreach (KeyValuePair<string, string> pair in revertedMap)
                {
                    _items.Add(pair.Key, pair.Value);
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Validate.
        /// </summary>
        public override void Validate()
        {
            Phoneme phoneme = Localor.GetPhoneme(Language);
            StringBuilder errorBuilder = new StringBuilder();
            if (Source == PhoneMap.TtsPhone)
            {
                foreach (string phone in phoneme.TtsPhones)
                {
                    if (!Phoneme.IsSilencePhone(phone) && !Phoneme.IsShortPausePhone(phone))
                    {
                        // Process only not silence, all phones except silence, short pause, and tone must contained in the map
                        if (!_items.ContainsKey(phone))
                        {
                            errorBuilder.AppendFormat(CultureInfo.InvariantCulture,
                                "TTS phone [{0}] should be included in map table as source phone. {1}",
                                phone, Environment.NewLine);
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, string> pair in _items)
            {
                if (StringComparer.Ordinal.Compare(Source, PhoneMap.TtsPhone) == 0)
                {
                    if (!phoneme.ContainsTtsPhone(pair.Key))
                    {
                        errorBuilder.AppendFormat(CultureInfo.InvariantCulture,
                            "Source phone [{0}] is invalid TTS phone. {1}",
                            pair.Key, Environment.NewLine);
                    }
                }

                if (StringComparer.Ordinal.Compare(Target, PhoneMap.TtsPhone) == 0)
                {
                    string[] items = pair.Value.Split(new char[] { ' ', '\t' },
                        StringSplitOptions.RemoveEmptyEntries);
                    foreach (string item in items)
                    {
                        if (!phoneme.ContainsTtsPhone(item))
                        {
                            errorBuilder.AppendFormat(CultureInfo.InvariantCulture,
                                "Target phone [{0}] is invalid TTS phone. {1}",
                                item, Environment.NewLine);
                        }
                    }
                }

                if (StringComparer.Ordinal.Compare(Source, PhoneMap.TtsSyllable) == 0)
                {
                    string[] items = pair.Key.Split(new char[] { ' ', '\t' },
                        StringSplitOptions.RemoveEmptyEntries);
                    foreach (string item in items)
                    {
                        if (!phoneme.ContainsTtsPhone(item))
                        {
                            errorBuilder.AppendFormat(CultureInfo.InvariantCulture,
                                "Source phone [{0}] is invalid TTS phone. {1}",
                                item, Environment.NewLine);
                        }
                    }
                }
            }

            if (errorBuilder.Length > 0)
            {
                // Error found
                throw new InvalidDataException(errorBuilder.ToString());
            }
        }

        /// <summary>
        /// Load XML file.
        /// </summary>
        /// <param name="xmlDoc">Xml document.</param>
        /// <param name="nsmgr">Nsmgr.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void Load(XmlDocument xmlDoc, XmlNamespaceManager nsmgr, object contentController)
        {
            if (xmlDoc == null)
            {
                throw new ArgumentNullException("xmlDoc");
            }

            if (nsmgr == null)
            {
                throw new ArgumentNullException("nsmgr");
            }

            Language = Localor.StringToLanguage(xmlDoc.DocumentElement.GetAttribute("lang"));
            _source = xmlDoc.DocumentElement.GetAttribute("source");
            _target = xmlDoc.DocumentElement.GetAttribute("target");

            XmlNodeList nodeList = xmlDoc.DocumentElement.SelectNodes("/tts:phoneMap/tts:item", nsmgr);
            _items.Clear();
            foreach (XmlNode xmlNode in nodeList)
            {
                XmlElement xmlEle = xmlNode as XmlElement;

                string from = xmlEle.GetAttribute("from");
                string to = xmlEle.GetAttribute("to");

                from = ReplaceHexString(from);
                to = ReplaceHexString(to);

                _items.Add(from, to);
            }
        }

        /// <summary>
        /// PerformanceSave.
        /// </summary>
        /// <param name="writer">Writer.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void PerformanceSave(XmlWriter writer, object contentController)
        {
            // Ensure consistence of this instance before saving into the stream.
            writer.WriteStartElement("phoneMap", Schema.TargetNamespace);
            writer.WriteAttributeString("lang", Localor.LanguageToString(Language));
            writer.WriteAttributeString("source", Source);
            writer.WriteAttributeString("target", Target);

            foreach (string item in _items.Keys)
            {
                writer.WriteStartElement("item");

                writer.WriteAttributeString("from", item);
                writer.WriteAttributeString("to", _items[item]);

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }
}