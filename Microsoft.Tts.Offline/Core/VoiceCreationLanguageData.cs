//----------------------------------------------------------------------------
// <copyright file="VoiceCreationLanguageData.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      This module implements language data object for voice creation.
//      The file paths are specified in xml configuration file.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// VoiceCreationLanguageData error.
    /// </summary>
    public enum VoiceCreationLanguageDataError
    {
        /// <summary>
        /// Language data file language mismatch.
        /// </summary>
        [ErrorAttribute(Message = "Need [{0}] but find [{1}] in language data file [{2}] : [{3}]")]
        MismatchLanguage,

        /// <summary>
        /// Can't find data in language data file.
        /// </summary>
        [ErrorAttribute(Message = "Empty data for language [{0}] in data file [{1}] : [{2}].")]
        EmptyLanguageDataFile,

        /// <summary>
        /// Can Not Create Phomeme.
        /// </summary>
        [ErrorAttribute(Message = "Can't create phomeme for language [{0}]")]
        CanNotCreatePhomeme
    }

    /// <summary>
    /// In voice creation related tools, it needs to load some language data to do data validation
    /// This class is a helper to manage language data in those tools.
    /// </summary>
    public class VoiceCreationLanguageData
    {
        #region private variables
        private Language _language;             // data language
        private string _phoneSet;               // phone set
        private string _posTable;               // pos table
        private string _unitTable;              // unit table
        private string _truncateRule;           // truncate rule
        private string _ttsToSapiVisemeId;      // tts to sapi viseme map
        private string _ttsToIpaPhone;          // tts to sr phone map
        private string _ttsToSrPhone;           // tts to sr phone map
        private string _dataDir;                // data directory
        private string _cartQuestions;          // cart questions
        private string _fontMeta;               // unit selection font meta
        private string _srModelDir;             // sr's speaker independent model
        private string _phoneQuestions;         // phone question for unit selection used by speaker dependent align
        private string _lexicalAttributeSchema; // lexical attribute schema

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceCreationLanguageData"/> class.
        /// </summary>
        public VoiceCreationLanguageData()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Data language.
        /// </summary>
        public Language Language
        {
            get
            {
                return _language;
            }

            set
            {
                _language = value;
            }
        }

        /// <summary>
        /// Gets or sets Phoneset file.
        /// </summary>
        public string PhoneSet
        {
            get
            {
                return GetLanguageDataFileFullPath(_phoneSet);
            }

            set
            {
                _phoneSet = value;
            }
        }

        /// <summary>
        /// Gets or sets Postable file.
        /// </summary>
        public string PosTable
        {
            get
            {
                return GetLanguageDataFileFullPath(_posTable);
            }

            set
            {
                _posTable = value;
            }
        }

        /// <summary>
        /// Gets or sets Unit table file.
        /// </summary>
        public string UnitTable
        {
            get
            {
                return GetLanguageDataFileFullPath(_unitTable);
            }

            set
            {
                _unitTable = value;
            }
        }

        /// <summary>
        /// Gets or sets Truncate rule.
        /// </summary>
        public string TruncateRule
        {
            get
            {
                return GetLanguageDataFileFullPath(_truncateRule);
            }

            set
            {
                _truncateRule = value;
            }
        }

        /// <summary>
        /// Gets or sets Tts to sapi viseme id file.
        /// </summary>
        public string TtsToSapiVisemeId
        {
            get
            {
                return GetLanguageDataFileFullPath(_ttsToSapiVisemeId);
            }

            set
            {
                _ttsToSapiVisemeId = value;
            }
        }

        /// <summary>
        /// Gets or sets Tts to SR phone map file.
        /// </summary>
        public string TtsToSrPhone
        {
            get
            {
                return GetLanguageDataFileFullPath(_ttsToSrPhone);
            }

            set
            {
                _ttsToSrPhone = value;
            }
        }

        /// <summary>
        /// Gets or sets Tts to IPA phone map file.
        /// </summary>
        public string TtsToIpaPhone
        {
            get
            {
                return GetLanguageDataFileFullPath(_ttsToIpaPhone);
            }

            set
            {
                _ttsToIpaPhone = value;
            }
        }

        /// <summary>
        /// Gets or sets Cart questions for unit selection font building.
        /// </summary>
        public string CartQuestions
        {
            get
            {
                return GetLanguageDataFileFullPath(_cartQuestions);
            }

            set
            {
                _cartQuestions = value;
            }
        }

        /// <summary>
        /// Gets or sets Tts phone questions.
        /// </summary>
        public string PhoneQuestions
        {
            get
            {
                return GetLanguageDataFileFullPath(_phoneQuestions);
            }

            set
            {
                _phoneQuestions = value;
            }
        }

        /// <summary>
        /// Gets or sets Sr model directory.
        /// </summary>
        public string SrModelDir
        {
            get
            {
                return GetLanguageDataFileFullPath(_srModelDir);
            }

            set
            {
                _srModelDir = value;
            }
        }

        /// <summary>
        /// Gets or sets Tts font meta (unit selection, seems it is better to be
        /// A voice data).
        /// </summary>
        public string FontMeta
        {
            get
            {
                return GetLanguageDataFileFullPath(_fontMeta);
            }

            set
            {
                _fontMeta = value;
            }
        }

        /// <summary>
        /// Gets or sets Language data directory.
        /// </summary>
        public string DataDir
        {
            get
            {
                return _dataDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _dataDir = value;
            }
        }

        /// <summary>
        /// Gets or sets Lexical attribute schema.
        /// </summary>
        public string LexicalAttributeSchema
        {
            get
            {
                return GetLanguageDataFileFullPath(_lexicalAttributeSchema);
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _lexicalAttributeSchema = value;
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Validate language data files.
        /// </summary>
        /// <param name="language">Language of the data files.</param>
        /// <returns>Error set.</returns>
        public ErrorSet ValidateLanguageData(Language language)
        {
            ErrorSet errorSet = new ErrorSet();
            if (!IsEmpty())
            {
                if (!string.IsNullOrEmpty(_phoneSet))
                {
                    TtsPhoneSet ttsPhoneSet = new TtsPhoneSet();
                    ttsPhoneSet.Load(PhoneSet);
                    if (ttsPhoneSet.Language != language)
                    {
                        errorSet.Add(new Error(VoiceCreationLanguageDataError.MismatchLanguage,
                            Localor.LanguageToString(language),
                            Localor.LanguageToString(ttsPhoneSet.Language),
                            Localor.PhoneSetFileName, PhoneSet));
                    }
                }

                if (!string.IsNullOrEmpty(_unitTable))
                {
                    SliceData sliceData = new SliceData();
                    sliceData.Language = language;
                    sliceData.Load(UnitTable);
                    if (sliceData.IsEmpty())
                    {
                        errorSet.Add(new Error(VoiceCreationLanguageDataError.EmptyLanguageDataFile,
                            Localor.LanguageToString(language),
                            Localor.UnitTableFileName, UnitTable));
                    }
                }

                if (!string.IsNullOrEmpty(_lexicalAttributeSchema))
                {
                    LexicalAttributeSchema lexicalAttributeSchema = new LexicalAttributeSchema();
                    lexicalAttributeSchema.Load(LexicalAttributeSchema);
                    if (lexicalAttributeSchema.Language != language)
                    {
                        errorSet.Add(new Error(VoiceCreationLanguageDataError.MismatchLanguage,
                            Localor.LanguageToString(language),
                            Localor.LanguageToString(lexicalAttributeSchema.Language),
                            Localor.PhoneSetFileName, LexicalAttributeSchema));
                    }
                }

                if (!string.IsNullOrEmpty(_truncateRule))
                {
                    TruncateRuleData truncateRuleData = new TruncateRuleData();
                    truncateRuleData.Load(TruncateRule);
                    if (truncateRuleData.Language != language)
                    {
                        errorSet.Add(new Error(VoiceCreationLanguageDataError.MismatchLanguage,
                            Localor.LanguageToString(language),
                            Localor.LanguageToString(truncateRuleData.Language),
                            Localor.TruncateRulesFileName, TruncateRule));
                    }
                }

                if (!string.IsNullOrEmpty(_ttsToSapiVisemeId))
                {
                    PhoneMap phoneMap = PhoneMap.CreatePhoneMap(TtsToSapiVisemeId);
                    if (phoneMap.Language != language)
                    {
                        errorSet.Add(new Error(VoiceCreationLanguageDataError.MismatchLanguage,
                            Localor.LanguageToString(language),
                            Localor.LanguageToString(phoneMap.Language),
                            Localor.TtsToSapiVisemeIdFileName, TtsToSapiVisemeId));
                    }
                }

                if (!string.IsNullOrEmpty(_ttsToSrPhone))
                {
                    PhoneMap phoneMap = PhoneMap.CreatePhoneMap(TtsToSrPhone);
                    if (phoneMap.Language != language)
                    {
                        errorSet.Add(new Error(VoiceCreationLanguageDataError.MismatchLanguage,
                            Localor.LanguageToString(language),
                            Localor.LanguageToString(phoneMap.Language),
                            Localor.TtsToSrPhoneFileName, TtsToSrPhone));
                    }
                }

                if (!string.IsNullOrEmpty(_ttsToIpaPhone))
                {
                    PhoneMap phoneMap = PhoneMap.CreatePhoneMap(TtsToIpaPhone);
                    if (phoneMap.Language != language)
                    {
                        errorSet.Add(new Error(VoiceCreationLanguageDataError.MismatchLanguage,
                            Localor.LanguageToString(language),
                            Localor.LanguageToString(phoneMap.Language),
                            Localor.TtsToIpaPhoneFileName, TtsToIpaPhone));
                    }
                }

                if (!string.IsNullOrEmpty(_fontMeta))
                {
                    PhoneMap phoneMap = PhoneMap.CreatePhoneMap(FontMeta);
                    if (phoneMap.Language != language)
                    {
                        errorSet.Add(new Error(VoiceCreationLanguageDataError.MismatchLanguage,
                            Localor.LanguageToString(language),
                            Localor.LanguageToString(phoneMap.Language),
                            Localor.FontMetaFileName, FontMeta));
                    }
                }
            }
            else
            {
                Trace.WriteLine("Using stocked language data with tools...");
            }

            return errorSet;
        }

        /// <summary>
        /// Set language data file path to localor globals
        /// The principle for using localor:
        /// 1. data object model don't use Localor.GetXXX function direclty.
        /// </summary>
        /// <param name="language">The language.</param>
        public void SetLanguageData(Language language)
        {
            if (!IsEmpty())
            {
                Trace.WriteLine("Using explicit language data specified by user...");
                Localor.ResetLanguageSetting(language);

                if (!string.IsNullOrEmpty(_phoneSet))
                {
                    Localor.SetLanguageDataFile(language, Localor.PhoneSetFileName, PhoneSet);
                }

                if (!string.IsNullOrEmpty(_unitTable))
                {
                    Localor.SetLanguageDataFile(language, Localor.UnitTableFileName, UnitTable);
                }

                if (!string.IsNullOrEmpty(_lexicalAttributeSchema))
                {
                    Localor.SetLanguageDataFile(language, Localor.LexicalAttributeSchemaFileName, LexicalAttributeSchema);
                }

                if (!string.IsNullOrEmpty(_posTable))
                {
                    Localor.SetLanguageDataFile(language, Localor.LexicalAttributeSchemaFileName, PosTable);
                }

                if (!string.IsNullOrEmpty(_truncateRule))
                {
                    Localor.SetLanguageDataFile(language, Localor.TruncateRulesFileName,
                        TruncateRule);
                }

                if (!string.IsNullOrEmpty(_ttsToSapiVisemeId))
                {
                    Localor.SetLanguageDataFile(language, Localor.TtsToSapiVisemeIdFileName,
                            TtsToSapiVisemeId);
                }

                if (!string.IsNullOrEmpty(_ttsToSrPhone))
                {
                    Localor.SetLanguageDataFile(language, Localor.TtsToSrPhoneFileName,
                            TtsToSrPhone);
                }

                if (!string.IsNullOrEmpty(_ttsToIpaPhone))
                {
                    Localor.SetLanguageDataFile(language, Localor.TtsToIpaPhoneFileName,
                            TtsToIpaPhone);
                }

                if (!string.IsNullOrEmpty(_fontMeta))
                {
                    Localor.SetLanguageDataFile(language, Localor.FontMetaFileName,
                            FontMeta);
                }
            }
            else
            {
                Trace.WriteLine("Using stocked language data with tools...");
            }
        }

        /// <summary>
        /// Parse XML document for language data information.
        /// In language expansion scenerio, language data are explictly provided by langDev.
        /// While in voice expansion scenerio, language data are deployed together with tools.
        /// </summary>
        /// <param name="dom">XmlDocument to be parsed.</param>
        /// <param name="nsmgr">XmlNameSpace.</param>
        /// <param name="prefix">Prefix.</param>
        /// <param name="checkFiles">Whether check import files.</param>
        public void ParseLanguageData(XmlDocument dom, XmlNamespaceManager nsmgr, string prefix,
                                 bool checkFiles)
        {
            if (dom == null)
            {
                throw new ArgumentNullException("dom");
            }

            if (nsmgr == null)
            {
                throw new ArgumentNullException("nsmgr");
            }

            if (prefix == null)
            {
                throw new ArgumentNullException("prefix");
            }

            XmlElement eleLangData = dom.DocumentElement.SelectSingleNode(prefix + @":languageData", nsmgr) as XmlElement;
            ParseLanguageDataFromXmlElement(checkFiles, eleLangData);
        }

        /// <summary>
        /// Parse language data from xml element.
        /// </summary>
        /// <param name="checkFiles">CheckFiles.</param>
        /// <param name="eleLangData">EleLangData.</param>
        public void ParseLanguageDataFromXmlElement(bool checkFiles, XmlElement eleLangData)
        {
            if (eleLangData != null)
            {
                string dataDir = eleLangData.GetAttribute("path");
                if (dataDir == null)
                {
                    dataDir = string.Empty;
                }

                _dataDir = dataDir;

                _language = Localor.StringToLanguage(eleLangData.GetAttribute("language"));

                foreach (XmlNode node in eleLangData.ChildNodes)
                {
                    XmlElement ele = node as XmlElement;
                    if (ele == null)
                    {
                        // skip non element node
                        continue;
                    }

                    string eleName = ele.Name;
                    string path = ele.GetAttribute("path");

                    if (checkFiles)
                    {
                        string rootedPath = Helper.GetFullPath(dataDir, path);
                        if (!File.Exists(rootedPath) && !Directory.Exists(rootedPath))
                        {
                            string message = Helper.NeutralFormat("The {1} full path [{0}] doesn't exist", rootedPath, eleName);
                            throw new InvalidDataException(message);
                        }
                    }

                    if (string.CompareOrdinal(eleName, "phoneSet") == 0)
                    {
                        _phoneSet = path;
                    }
                    else if (string.CompareOrdinal(eleName, "posTable") == 0)
                    {
                        _posTable = path;
                    }
                    else if (string.CompareOrdinal(eleName, "unitTable") == 0)
                    {
                        _unitTable = path;
                    }
                    else if (string.CompareOrdinal(eleName, "ttsToSapiVisemeId") == 0)
                    {
                        _ttsToSapiVisemeId = path;
                    }
                    else if (string.CompareOrdinal(eleName, "ttsToIpaPhone") == 0)
                    {
                        _ttsToIpaPhone = path;
                    }
                    else if (string.CompareOrdinal(eleName, "ttsToSrPhone") == 0)
                    {
                        _ttsToSrPhone = path;
                    }
                    else if (string.CompareOrdinal(eleName, "cartQuestions") == 0)
                    {
                        _cartQuestions = path;
                    }
                    else if (string.CompareOrdinal(eleName, "fontMeta") == 0)
                    {
                        _fontMeta = path;
                    }
                    else if (string.CompareOrdinal(eleName, "phoneQuestions") == 0)
                    {
                        _phoneQuestions = path;
                    }
                    else if (string.CompareOrdinal(eleName, "srModelDir") == 0)
                    {
                        _srModelDir = path;
                    }
                    else if (string.CompareOrdinal(eleName, "schema") == 0)
                    {
                        _lexicalAttributeSchema = path;
                    }
                    else
                    {
                        string message = Helper.NeutralFormat("Unsupported language data for voice creation: {1} {0}", path, eleName);
                        throw new InvalidDataException(message);
                    }
                }
            }
        }

        /// <summary>
        /// Save language data information.
        /// </summary>
        /// <param name="dom">Xml dom document.</param>
        /// <param name="schema">Xml schema.</param>
        /// <param name="parentEle">Parent element.</param>
        public void SaveLanguageData(XmlDocument dom, XmlSchema schema, XmlElement parentEle)
        {
            if (dom == null)
            {
                throw new ArgumentNullException("dom");
            }

            if (schema == null)
            {
                throw new ArgumentNullException("schema");
            }

            if (parentEle == null)
            {
                throw new ArgumentNullException("parentEle");
            }

            if (!IsEmpty())
            { 
                XmlElement eleLangData = XmlHelper.AppendElement(dom, parentEle, "languageData", "path", _dataDir, schema, true);
                eleLangData.SetAttribute("language", Localor.LanguageToString(Language));

                XmlHelper.AppendElement(dom, eleLangData, "phoneSet", "path", _phoneSet, schema);
                XmlHelper.AppendElement(dom, eleLangData, "posTable", "path", _posTable, schema);
                XmlHelper.AppendElement(dom, eleLangData, "unitTable", "path", _unitTable, schema);
                XmlHelper.AppendElement(dom, eleLangData, "ttsToSapiVisemeId", "path", _ttsToSapiVisemeId, schema);
                XmlHelper.AppendElement(dom, eleLangData, "ttsToSrPhone", "path", _ttsToSrPhone, schema);
                XmlHelper.AppendElement(dom, eleLangData, "cartQuestions", "path", _cartQuestions, schema);
                XmlHelper.AppendElement(dom, eleLangData, "fontMeta", "path", _fontMeta, schema);
                XmlHelper.AppendElement(dom, eleLangData, "phoneQuestions", "path", _phoneQuestions, schema);
                XmlHelper.AppendElement(dom, eleLangData, "srModelDir", "path", _srModelDir, schema);
                XmlHelper.AppendElement(dom, eleLangData, "schema", "path", _lexicalAttributeSchema, schema);
            }
        }

        /// <summary>
        /// Save language data information.
        /// </summary>
        /// <param name="xmlWriter">Xml writer.</param>
        public void SaveLanguageData(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("languageData");
            xmlWriter.WriteAttributeString("language", Localor.LanguageToString(_language));
            xmlWriter.WriteAttributeString("path", _dataDir);

            if (!string.IsNullOrEmpty(_phoneSet))
            {
                xmlWriter.WriteStartElement("phoneSet");
                xmlWriter.WriteAttributeString("path", _phoneSet);
                xmlWriter.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(_unitTable))
            {
                xmlWriter.WriteStartElement("unitTable");
                xmlWriter.WriteAttributeString("path", _unitTable);
                xmlWriter.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(_posTable))
            {
                xmlWriter.WriteStartElement("posTable");
                xmlWriter.WriteAttributeString("path", _posTable);
                xmlWriter.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(_ttsToSapiVisemeId))
            {
                xmlWriter.WriteStartElement("ttsToSapiVisemeId");
                xmlWriter.WriteAttributeString("path", _ttsToSapiVisemeId);
                xmlWriter.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(_ttsToSrPhone))
            {
                xmlWriter.WriteStartElement("ttsToSrPhone");
                xmlWriter.WriteAttributeString("path", _ttsToSrPhone);
                xmlWriter.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(_cartQuestions))
            {
                xmlWriter.WriteStartElement("cartQuestions");
                xmlWriter.WriteAttributeString("path", _cartQuestions);
                xmlWriter.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(_fontMeta))
            {
                xmlWriter.WriteStartElement("fontMeta");
                xmlWriter.WriteAttributeString("path", _fontMeta);
                xmlWriter.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(_phoneQuestions))
            {
                xmlWriter.WriteStartElement("phoneQuestions");
                xmlWriter.WriteAttributeString("path", _phoneQuestions);
                xmlWriter.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(_srModelDir))
            {
                xmlWriter.WriteStartElement("srModelDir");
                xmlWriter.WriteAttributeString("path", _srModelDir);
                xmlWriter.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(_lexicalAttributeSchema))
            {
                xmlWriter.WriteStartElement("schema");
                xmlWriter.WriteAttributeString("path", _lexicalAttributeSchema);
                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
        }

        /// <summary>
        /// Tell if the file paths are empty.
        /// </summary>
        /// <returns>True/false.</returns>
        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(_phoneSet) &&
                   string.IsNullOrEmpty(_posTable) &&
                   string.IsNullOrEmpty(_unitTable) &&
                   string.IsNullOrEmpty(_ttsToSapiVisemeId) &&
                   string.IsNullOrEmpty(_ttsToSrPhone) &&
                   string.IsNullOrEmpty(_cartQuestions) &&
                   string.IsNullOrEmpty(_fontMeta) && 
                   string.IsNullOrEmpty(_phoneQuestions) &&
                   string.IsNullOrEmpty(_srModelDir);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Get language data file full path.
        /// </summary>
        /// <param name="filePath">Language file path.</param>
        /// <returns>Language data full path.</returns>
        private string GetLanguageDataFileFullPath(string filePath)
        {
            string fullPath = filePath;
            if (!string.IsNullOrEmpty(filePath))
            {
                fullPath = Helper.GetFullPath(_dataDir, filePath);
            }

            return fullPath;
        }

        #endregion 
    }
}