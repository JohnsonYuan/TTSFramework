//----------------------------------------------------------------------------
// <copyright file="UnitListDictionary.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements UnitList
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Config
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Drop/Hold unit item.
    /// </summary>
    public class UnitItem
    {
        #region Fileds

        private string _sentenceId;
        private int _indexInSentence;
        private string _name;
        private string _error;
        private string _description;

        #endregion

        #region Properties

        /// <summary>
        /// Gets Id of the UnitItem.
        /// </summary>
        public string Id
        {
            get { return GetKey(_sentenceId, _indexInSentence); }
        }

        /// <summary>
        /// Gets or sets Sentence Id of the unit.
        /// </summary>
        public string SentenceId
        {
            get
            {
                return _sentenceId;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _sentenceId = value;
            }
        }

        /// <summary>
        /// Gets or sets Unit index in sentence.
        /// </summary>
        public int IndexInSentence
        {
            get { return _indexInSentence; }
            set { _indexInSentence = value; }
        }

        /// <summary>
        /// Gets or sets Unit name.
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
        /// Gets or sets Unit item error type.
        /// </summary>
        public string Error
        {
            get
            {
                return _error;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _error = value;
            }
        }

        /// <summary>
        /// Gets or sets Unit list item description.
        /// </summary>
        public string Description
        {
            get
            {
                return _description;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _description = value;
            }
        }

        #endregion

        #region public static method.

        /// <summary>
        /// Get key of the unit in UnitList.
        /// </summary>
        /// <param name="sentenceId">Sentence ID of the unit.</param>
        /// <param name="indexInSentence">Unit index in the sentence..</param>
        /// <returns>Generated UnitItem key.</returns>
        public static string GetKey(string sentenceId, int indexInSentence)
        {
            if (string.IsNullOrEmpty(sentenceId))
            {
                throw new ArgumentNullException("sentenceId");
            }

            if (indexInSentence < 0)
            {
                string message = Helper.NeutralFormat("Index of unit in sentence should not be negative.");
                throw new ArgumentException(message, "indexInSentence");
            }

            return string.Format(CultureInfo.InvariantCulture,
                "{0}_{1}", sentenceId, indexInSentence.ToString(CultureInfo.InvariantCulture));
        }

        #endregion

        #region Xml operation.

        /// <summary>
        /// Create XmlNode for UnitItem.
        /// </summary>
        /// <param name="dom">XmlDocument to which to create XmlNode.</param>
        /// <param name="xmlNamespace">Name space of the XmlElement.</param>
        /// <returns>Created UnitItem XmlElement.</returns>
        public XmlElement CreateXmlElement(XmlDocument dom, string xmlNamespace)
        {
            if (dom == null)
            {
                throw new ArgumentNullException("dom");
            }

            if (string.IsNullOrEmpty(xmlNamespace))
            {
                throw new ArgumentNullException("xmlNamespace");
            }

            XmlElement unitEle = dom.CreateElement("unit", xmlNamespace);
            unitEle.SetAttribute("sid", _sentenceId);
            unitEle.SetAttribute("index", _indexInSentence.ToString(CultureInfo.InvariantCulture));
            unitEle.SetAttribute("name", _name);
            if (!string.IsNullOrEmpty(_error))
            {
                unitEle.SetAttribute("error", _error);
            }

            if (!string.IsNullOrEmpty(_description))
            {
                unitEle.SetAttribute("desc", _description);
            }

            return unitEle;
        }

        /// <summary>
        /// Parse XmlNode to UnitItem.
        /// </summary>
        /// <param name="node">XmlNode to be parsed.</param>
        public void Parse(XmlNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            XmlElement ele = (XmlElement)node;
            _sentenceId = ele.GetAttribute("sid");
            _indexInSentence = int.Parse(ele.GetAttribute("index"), CultureInfo.InvariantCulture);
            _name = ele.GetAttribute("name");

            XmlNode errorNode = node.Attributes["error"];
            if (errorNode != null)
            {
                _error = errorNode.InnerText;
            }

            XmlNode descNode = node.Attributes["desc"];
            if (descNode != null)
            {
                _description = descNode.InnerText;
            }
        }

        #endregion
    }

    /// <summary>
    /// UnitList class, currently has two types: Hold, Drop.
    /// One UnitList have could contains some UnitItems,
    /// Each UnitItem presents one unit in sentence.
    /// </summary>
    public class UnitList
    {
        #region Fields

        private Language _language;
        private UnitListType _type;
        private Dictionary<string, UnitItem> _units = new Dictionary<string, UnitItem>();

        #endregion

        #region enum

        /// <summary>
        /// Unit list type.
        /// </summary>
        public enum UnitListType
        {
            /// <summary>
            /// None of the following types.
            /// </summary>
            None = 0,

            /// <summary>
            /// Drop unit list.
            /// </summary>
            Drop = 1,

            /// <summary>
            /// Hold unit list.
            /// </summary>
            Hold = 2
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets UnitList Id.
        /// </summary>
        public string Id
        {
            get { return GetKey(_language, _type); }
        }

        /// <summary>
        /// Gets or sets UnitList type: Drop/Hold.
        /// </summary>
        public UnitListType UnitType
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// Gets or sets Unit list language.
        /// </summary>
        public Language Language
        {
            get { return _language; }
            set { _language = value; }
        }

        /// <summary>
        /// Gets Unit list.
        /// </summary>
        public Dictionary<string, UnitItem> Units
        {
            get
            {
                return _units;
            }
        }

        #endregion

        #region public static method

        /// <summary>
        /// Get key of the UnitList.
        /// </summary>
        /// <param name="language">Language of the UnitList.</param>
        /// <param name="type">Type of the UnitList.</param>
        /// <returns>Key of the UnitList.</returns>
        public static string GetKey(Language language, UnitListType type)
        {
            return Localor.LanguageToString(language) + "_" + type.ToString();
        }

        #endregion

        #region public operation method

        /// <summary>
        /// Add Unit to UnitList.
        /// </summary>
        /// <param name="unitItem">UnitItem to be added.</param>
        public void AddUnit(UnitItem unitItem)
        {
            if (unitItem == null)
            {
                throw new ArgumentNullException("unitItem");
            }

            if (!_units.ContainsKey(unitItem.Id))
            {
                _units.Add(unitItem.Id, unitItem);
            }
            else
            {
                _units[unitItem.Id] = unitItem;
            }
        }

        /// <summary>
        /// Remove unit from UnitList.
        /// </summary>
        /// <param name="sentenceId">Sentence ID of the unit to be remove.</param>
        /// <param name="indexInSentence">Unit index in sentence.</param>
        /// <returns>Removed unit.</returns>
        public UnitItem RemoveUnit(string sentenceId, int indexInSentence)
        {
            UnitItem unitItem = null;
            string unitKey = UnitItem.GetKey(sentenceId, indexInSentence);

            if (_units.ContainsKey(unitKey))
            {
                unitItem = _units[unitKey];
                _units.Remove(unitKey);
            }

            return unitItem;
        }

        /// <summary>
        /// Append UnitList to an existing UnitList.
        /// </summary>
        /// <param name="unitList">UnitList to be appended.</param>
        public void Append(UnitList unitList)
        {
            if (unitList == null)
            {
                throw new ArgumentNullException("unitList");
            }

            if (unitList.Units == null)
            {
                string message = Helper.NeutralFormat("The Units of unit list should not be null.");
                throw new ArgumentNullException("unitList", message);
            }

            if (unitList.Units.Keys == null)
            {
                string message = Helper.NeutralFormat("The Units.Keys of unit list should not be null.");
                throw new ArgumentNullException("unitList", message);
            }

            if (unitList.Language != _language || unitList.UnitType != _type)
            {
                return;
            }

            foreach (string unitKey in unitList.Units.Keys)
            {
                UnitItem unitItem = unitList.Units[unitKey];
                AddUnit(unitItem);
            }
        }

        #endregion

        #region public XML operation method.

        /// <summary>
        /// Create XmlElement for the UnitList.
        /// </summary>
        /// <param name="dom">XmlDocument on which to create XmlElement.</param>
        /// <param name="xmlNamespace">Name space of the XmlElement.</param>
        /// <returns>Created UnitList XmlElement.</returns>
        public XmlElement CreateXmlElement(XmlDocument dom, string xmlNamespace)
        {
            if (dom == null)
            {
                throw new ArgumentNullException("dom");
            }

            if (string.IsNullOrEmpty(xmlNamespace))
            {
                throw new ArgumentNullException("xmlNamespace");
            }

            XmlElement unitListEle = null;

            if (_units.Count > 0)
            {
                unitListEle = dom.CreateElement("unitList", xmlNamespace);

                unitListEle.SetAttribute("language", Localor.LanguageToString(_language));
                unitListEle.SetAttribute("type", _type.ToString());

                XmlElement unitsEle = dom.CreateElement("units", xmlNamespace);
                unitListEle.AppendChild(unitsEle);

                foreach (string key in _units.Keys)
                {
                    UnitItem item = _units[key];
                    XmlElement unitEle = item.CreateXmlElement(dom, xmlNamespace);
                    unitsEle.AppendChild(unitEle);
                }
            }

            return unitListEle;
        }

        /// <summary>
        /// Parse XmlNode to UnitList.
        /// </summary>
        /// <param name="node">XmlNode to be parsed.</param>
        /// <param name="nsmgr">XmlNamespaceManager.</param>
        public void Parse(XmlNode node, XmlNamespaceManager nsmgr)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (node.Attributes == null)
            {
                string message = Helper.NeutralFormat("the Attributes of node should not be null.");
                throw new ArgumentNullException("node", message);
            }

            XmlNode languageNode = node.Attributes["language"];
            XmlNode typeNode = node.Attributes["type"];
            if (languageNode == null || typeNode  == null)
            {
                throw new InvalidDataException("unitList XmlNode " +
                    "should have the following two properties: language, type.");
            }

            _language = Localor.StringToLanguage(languageNode.InnerText);
            _type = (UnitListType)Enum.Parse(typeof(UnitListType), typeNode.InnerText);

            XmlNodeList unitNodes = node.SelectNodes(@"tts:units/tts:unit", nsmgr);
            _units.Clear();

            foreach (XmlNode unitNode in unitNodes)
            {
                UnitItem item = new UnitItem();

                item.Parse(unitNode);

                _units.Add(item.Id, item);
            }
        }

        #endregion
    }

    /// <summary>
    /// Unit list dictionary.
    /// </summary>
    public class UnitListDictionary
    {
        #region Fields

        private static XmlSchema _schema;
        private Dictionary<string, UnitList> _unitListMap = new Dictionary<string, UnitList>();
        private SentenceList _dropSentenceList = new SentenceList();

        #endregion

        #region Properties

        /// <summary>
        /// Gets UnitListDictionary schema.
        /// </summary>
        public static XmlSchema ConfigSchema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Config.UnitListDictionary.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets Unit list map, using combination of language and type as key,
        /// For example : en-US_Hold.
        /// </summary>
        public Dictionary<string, UnitList> UnitListMap
        {
            get { return _unitListMap; }
        }

        /// <summary>
        /// Gets Drop sentence list.
        /// </summary>
        public SentenceList DropSentenceList
        {
            get { return _dropSentenceList; }
        }

        #endregion

        #region I/O

        /// <summary>
        /// Save unit list data into XML file.
        /// </summary>
        /// <param name="filePath">Target file to save.</param>
        public void Save(string filePath)
        {
            XmlDocument dom = new XmlDocument();
            XmlSchema schema = UnitListDictionary.ConfigSchema;
            dom.NameTable.Add(schema.TargetNamespace);

            // Root element
            XmlElement ele = dom.CreateElement("config", schema.TargetNamespace);

            foreach (string unitListKey in _unitListMap.Keys)
            {
                UnitList unitList = _unitListMap[unitListKey];

                XmlElement unitListEle = unitList.CreateXmlElement(dom, schema.TargetNamespace);

                ele.AppendChild(unitListEle);
            }

            XmlElement dropSentenceEle = _dropSentenceList.CreateXmlElement(dom, schema.TargetNamespace);
            if (dropSentenceEle != null)
            {
                ele.AppendChild(dropSentenceEle);
            }

            dom.AppendChild(ele);
            dom.Save(filePath);

            // Performance compatibility format checking
            XmlHelper.Validate(filePath, ConfigSchema);
        }

        /// <summary>
        /// Load configuration from file.
        /// </summary>
        /// <param name="filePath">XML configuration file path.</param>
        public void Load(string filePath)
        {
            // Check the configuration file first
            try
            {
                XmlHelper.Validate(filePath, ConfigSchema);
            }
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The configuration file [{0}] error is found.",
                    filePath);
                throw new InvalidDataException(message, ide);
            }

            // Load configuration
            XmlDocument dom = new XmlDocument();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("tts", ConfigSchema.TargetNamespace);
            dom.Load(filePath);

            XmlNodeList unitListNodes = dom.DocumentElement.SelectNodes(@"tts:unitList", nsmgr);

            _unitListMap.Clear();
            foreach (XmlNode unitListNode in unitListNodes)
            {
                UnitList unitList = new UnitList();
                unitList.Parse(unitListNode, nsmgr);
                _unitListMap.Add(unitList.Id, unitList);
            }

            XmlNode dropSentencesNode = dom.DocumentElement.SelectSingleNode(@"tts:dropSentences", nsmgr);

            if (dropSentencesNode != null)
            {
                _dropSentenceList.Parse(dropSentencesNode, nsmgr);
            }
        }

        #endregion

        #region operation

        /// <summary>
        /// Merge all unit list files.
        /// </summary>
        /// <param name="filePaths">File path array to be merged.</param>
        public void Merge(string[] filePaths)
        {
            if (filePaths == null)
            {
                throw new ArgumentNullException("filePaths");
            }

            foreach (string filePath in filePaths)
            {
                if (!File.Exists(filePath))
                {
                    throw Helper.CreateException(typeof(FileNotFoundException), filePath);
                }

                UnitListDictionary unitListDictionary = new UnitListDictionary();
                unitListDictionary.Load(filePath);
                Append(unitListDictionary);
            }
        }

        /// <summary>
        /// Check whether the unit should be kept.
        /// </summary>
        /// <param name="language">Language of the unit to be checked.</param>
        /// <param name="sentenceId">Sentence ID of the unit to be checked.</param>
        /// <param name="indexInSentence">Unit index in sentence to be checked.</param>
        /// <returns>Whether the unit should be kept.</returns>
        public bool KeepUnit(Language language, string sentenceId, int indexInSentence)
        {
            if (string.IsNullOrEmpty(sentenceId))
            {
                throw new ArgumentNullException("sentenceId");
            }

            if (indexInSentence < 0)
            {
                throw new ArgumentException("indexInSentence is less than 0");
            }

            bool keep = true;

            foreach (string unitListKey in _unitListMap.Keys)
            {
                UnitList unitList = _unitListMap[unitListKey];

                if (unitList.Language != language)
                {
                    continue;
                }

                string unitKey = UnitItem.GetKey(sentenceId, indexInSentence);

                if (unitList.UnitType == UnitList.UnitListType.Drop)
                {
                    if (unitList.Units.ContainsKey(unitKey))
                    {
                        keep = false;
                        break;
                    }
                }
                else if (unitList.UnitType == UnitList.UnitListType.Hold)
                {
                    // If unitList.Unit.Count == 0, we treate as all unit are contained in the HoldUnitList.
                    if (unitList.Units.Count > 0 &&
                        !unitList.Units.ContainsKey(unitKey))
                    {
                        keep = false;
                        break;
                    }
                }
            }

            return keep;
        }

        /// <summary>
        /// Append unit in one UnitListDictionary to an existing one.
        /// </summary>
        /// <param name="unitListDic">UnitListDictionary to be added.</param>
        public void Append(UnitListDictionary unitListDic)
        {
            if (unitListDic == null)
            {
                throw new ArgumentNullException("unitListDic");
            }

            if (unitListDic.UnitListMap == null)
            {
                string message =
                    Helper.NeutralFormat("The UnitListMap of unit list dictionary should not be null.");
                throw new ArgumentNullException("unitListDic", message);
            }

            if (unitListDic.UnitListMap.Keys == null)
            {
                string message =
                    Helper.NeutralFormat("The UnitListMap.Keys of unit list dictionary should not be null.");
                throw new ArgumentNullException("unitListDic", message);
            }

            foreach (string unitListKey in unitListDic.UnitListMap.Keys)
            {
                UnitList unitList = unitListDic.UnitListMap[unitListKey];
                if (!_unitListMap.ContainsKey(unitListKey))
                {
                    _unitListMap.Add(unitListKey, unitList);
                }
                else
                {
                    _unitListMap[unitListKey].Append(unitList);
                }
            }
        }

        /// <summary>
        /// Whether unit list map contains the unit list with the specified language and type.
        /// </summary>
        /// <param name="language">Language of the UnitList.</param>
        /// <param name="type">Type of the UnitList.</param>
        /// <param name="unitKey">UnitKey of the UnitItem.</param>
        /// <returns>Whether contains the specified unit.</returns>
        public bool ContainsKey(Language language, UnitList.UnitListType type, string unitKey)
        {
            string unitListKey = UnitList.GetKey(language, type);

            if (_unitListMap.ContainsKey(unitListKey) &&
                _unitListMap[unitListKey].Units.ContainsKey(unitKey))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Add unit to unit list map.
        /// </summary>
        /// <param name="language">Language of the unit.</param>
        /// <param name="type">Type of the unit.</param>
        /// <param name="unit">Unit to be added.</param>
        public void AddUnit(Language language, UnitList.UnitListType type,
            UnitItem unit)
        {
            string unitListKey = UnitList.GetKey(language, type);

            if (!_unitListMap.ContainsKey(unitListKey))
            {
                UnitList unitList = new UnitList();
                unitList.Language = language;
                unitList.UnitType = type;
                _unitListMap.Add(unitListKey, unitList);

                unitList.AddUnit(unit);
            }
            else
            {
                UnitList unitList = _unitListMap[unitListKey];
                unitList.AddUnit(unit);
            }
        }

        /// <summary>
        /// Remove unit from unit list map.
        /// </summary>
        /// <param name="language">Language of the unit.</param>
        /// <param name="type">Type of the unit.</param>
        /// <param name="sentenceId">Sentence ID of the unit.</param>
        /// <param name="indexInSentence">Unit index in sentence.</param>
        /// <returns>Removed UnitItem.</returns>
        public UnitItem RemoveUnit(Language language, UnitList.UnitListType type,
            string sentenceId, int indexInSentence)
        {
            string uniListKey = UnitList.GetKey(language, type);
            if (_unitListMap.ContainsKey(uniListKey))
            {
                UnitList unitList = _unitListMap[uniListKey];
                return unitList.RemoveUnit(sentenceId, indexInSentence);
            }

            return null;
        }

        #endregion
    }
}