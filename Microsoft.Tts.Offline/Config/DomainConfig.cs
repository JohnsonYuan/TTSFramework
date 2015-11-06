//----------------------------------------------------------------------------
// <copyright file="DomainConfig.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements DomainConfig
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Config
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Definition of tts domain.
    /// </summary>
    [Flags]
    public enum ScriptDomain
    {
        /// <summary>
        /// Normal tts domain.
        /// </summary>
        Normal = 0x0001,

        /// <summary>
        /// Number domain.
        /// </summary>
        Number = 0x0002,

        /// <summary>
        /// PersonName domain.
        /// </summary>
        PersonName = 0x0004,

        /// <summary>
        /// Letter domain.
        /// </summary>
        Letter = 0x0008,

        /// <summary>
        /// Acronym domain.
        /// </summary>
        Acronym = 0x0010,

        /// <summary>
        /// All above.
        /// </summary>
        All = ScriptDomain.Normal | ScriptDomain.Number | ScriptDomain.PersonName |
            ScriptDomain.Letter | ScriptDomain.Acronym
    }

    /// <summary>
    /// Script range item.
    /// </summary>
    public struct ScriptRangeItem
    {
        /// <summary>
        /// StartSentenceId;.
        /// </summary>
        public string StartSentenceId;

        /// <summary>
        /// EndSentenceId;.
        /// </summary>
        public string EndSentenceId;

        /// <summary>
        /// Domain;.
        /// </summary>
        public ScriptDomain Domain;
    }

    /// <summary>
    /// Domain config list.
    /// </summary>
    public class DomainConfigList
    {
        #region Fields

        private Collection<ScriptRangeItem> _items = new Collection<ScriptRangeItem>();
        private ScriptDomain _domain;
        private string _fontExtName;
        private FontSectionTag _fontTag;
        private bool _sharedWithNormalDomain = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets Script range items.
        /// </summary>
        public Collection<ScriptRangeItem> Items
        {
            get
            {
                return _items; 
            }
        }

        /// <summary>
        /// Gets or sets Domain Type.
        /// </summary>
        public ScriptDomain Domain
        {
            get 
            { 
                return _domain; 
            }

            set 
            {
                _domain = value; 
            }
        }

        /// <summary>
        /// Gets or sets Font extension name.
        /// </summary>
        public string FontExtName
        {
            get
            {
                return _fontExtName;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _fontExtName = value;
            }
        }

        /// <summary>
        /// Gets or sets Font tag.
        /// </summary>
        public FontSectionTag FontTag
        {
            get
            { 
                return _fontTag; 
            }

            set 
            {
                _fontTag = value; 
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether could be shared with Normal Domain.
        /// </summary>
        public bool SharedWithNormalDomain
        {
            get
            {
                return _sharedWithNormalDomain; 
            }

            set
            { 
                _sharedWithNormalDomain = value; 
            }
        }

        #endregion

        #region Public Operations

        /// <summary>
        /// Is given sentenceId in the domain list.
        /// </summary>
        /// <param name="sentenceId">Given sentence Id.</param>
        /// <returns>Bool.</returns>
        public bool Contains(string sentenceId)
        {
            bool contained = false;

            foreach (ScriptRangeItem item in _items)
            {
                if (string.Compare(item.StartSentenceId, sentenceId, StringComparison.Ordinal) <= 0 &&
                    string.Compare(item.EndSentenceId, sentenceId, StringComparison.Ordinal) >= 0)
                {
                    contained = true;
                    break;
                }
            }

            return contained;
        }

        #endregion
    }

    /// <summary>
    /// Digital word.
    /// </summary>
    public class DigitalWordItem
    {
        #region Fields

        private string _word;
        private int _group;
        private string _sentenceId;
        private int _wordIndex;

        #endregion

        #region Properties

        /// <summary>
        /// Gets SentenceId + wordIndex.
        /// </summary>
        public string Id
        {
            get 
            {
                return GetKey(_sentenceId, _wordIndex); 
            }
        }

        /// <summary>
        /// Gets or sets Word text.
        /// </summary>
        public string Word
        {
            get
            {
                return _word;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _word = value;
            }
        }

        /// <summary>
        /// Gets or sets Group index.
        /// </summary>
        public int Group
        {
            get 
            {
                return _group;
            }

            set
            {
                _group = value;
            }
        }

        /// <summary>
        /// Gets or sets Setnece Id.
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
        /// Gets or sets Word index.
        /// </summary>
        public int WordIndex
        {
            get 
            {
                return _wordIndex; 
            }

            set
            {
                _wordIndex = value; 
            }
        }

        #endregion

        #region Public Operations

        /// <summary>
        /// Compile unit feature key.
        /// </summary>
        /// <param name="sentenceId">Sentence id.</param>
        /// <param name="index">Word index in the sentece.</param>
        /// <returns>Key of digital word item.</returns>
        public static string GetKey(string sentenceId, int index)
        {
            if (string.IsNullOrEmpty(sentenceId))
            {
                throw new ArgumentNullException("sentenceId");
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return string.Format(CultureInfo.InvariantCulture, "{0} {1:0000}",
                sentenceId, index);
        }

        /// <summary>
        /// Implements ToString() method used for debugging and logging.
        /// </summary>
        /// <returns>String value of the object.</returns>
        public override string ToString()
        {
            return Helper.NeutralFormat("DigitalWordItem: sid={0},word={1},group={2},wordIndex={3}",
                _sentenceId, _word, _group, _wordIndex);
        }

        #endregion
    }

    /// <summary>
    /// Domain config list for number domain.
    /// </summary>
    public class NumberDomainConfigList : DomainConfigList
    {
        #region Fields

        private Dictionary<string, DigitalWordItem> _digitals =
            new Dictionary<string, DigitalWordItem>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets Digital words.
        /// </summary>
        public Dictionary<string, DigitalWordItem> Digitals
        {
            get
            {
                return _digitals;
            }
        }

        #endregion
    }

    /// <summary>
    /// Acronym word.
    /// </summary>
    public class AcronymWordItem
    {
        #region Fields

        private string _word;
        private int _group;
        private string _sentenceId;
        private int _wordIndex;

        #endregion

        #region Properties

        /// <summary>
        /// Gets SentenceId + wordIndex.
        /// </summary>
        public string Id
        {
            get 
            {
                return GetKey(_sentenceId, _wordIndex); 
            }
        }

        /// <summary>
        /// Gets or sets Word text.
        /// </summary>
        public string Word
        {
            get
            {
                return _word;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _word = value;
            }
        }

        /// <summary>
        /// Gets or sets Group index.
        /// </summary>
        public int Group
        {
            get 
            {
                return _group;
            }

            set
            { 
                _group = value;
            }
        }

        /// <summary>
        /// Gets or sets Setnece Id.
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
        /// Gets or sets Word index.
        /// </summary>
        public int WordIndex
        {
            get 
            { 
                return _wordIndex; 
            }

            set
            { 
                _wordIndex = value;
            }
        }

        #endregion

        #region Public Operations

        /// <summary>
        /// Compile unit feature key.
        /// </summary>
        /// <param name="sentenceId">Sentence id.</param>
        /// <param name="index">Word index in the sentece.</param>
        /// <returns>Key of acronym word item.</returns>
        public static string GetKey(string sentenceId, int index)
        {
            if (string.IsNullOrEmpty(sentenceId))
            {
                throw new ArgumentNullException("sentenceId");
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return string.Format(CultureInfo.InvariantCulture, "{0} {1:0000}",
                sentenceId, index);
        }

        /// <summary>
        /// Implements ToString() method used for debugging and logging.
        /// </summary>
        /// <returns>String value of the object.</returns>
        public override string ToString()
        {
            return Helper.NeutralFormat("AcronymWordItem: sid={0},word={1},group={2},wordIndex={3}",
                _sentenceId, _word, _group, _wordIndex);
        }

        #endregion
    }

    /// <summary>
    /// Domain config list for acronym domain.
    /// </summary>
    public class AcronymDomainConfigList : DomainConfigList
    {
        #region Fields

        private Dictionary<string, AcronymWordItem> _acronyms =
            new Dictionary<string, AcronymWordItem>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets Acronym words.
        /// </summary>
        public Dictionary<string, AcronymWordItem> Acronyms
        {
            get 
            { 
                return _acronyms;
            }
        }

        #endregion
    }

    /// <summary>
    /// Domain config class.
    /// </summary>
    public class DomainConfig
    {
        #region Fields

        private static Dictionary<ScriptDomain, string> _fontExtNames;
        private static Dictionary<ScriptDomain, FontSectionTag> _fontTags;

        private static XmlSchema _schema;
        private string _filePath;

        private Dictionary<ScriptDomain, DomainConfigList> _domainLists =
            new Dictionary<ScriptDomain, DomainConfigList>();

        #endregion

        #region Constructions

        /// <summary>
        /// Initializes static members of the <see cref="DomainConfig"/> class.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "Performance is not concern here")]
        static DomainConfig()
        {
            _fontExtNames = new Dictionary<ScriptDomain, string>();
            _fontExtNames.Add(ScriptDomain.Normal, "CRT");
            _fontExtNames.Add(ScriptDomain.Number, "NUM");
            _fontExtNames.Add(ScriptDomain.PersonName, "NAM");
            _fontExtNames.Add(ScriptDomain.Letter, "LET");
            _fontExtNames.Add(ScriptDomain.Acronym, "ACR");

            _fontTags = new Dictionary<ScriptDomain, FontSectionTag>();
            _fontTags.Add(ScriptDomain.Normal, FontSectionTag.Unknown);
            _fontTags.Add(ScriptDomain.Number, FontSectionTag.NumberDomain);
            _fontTags.Add(ScriptDomain.PersonName, FontSectionTag.NameDomain);
            _fontTags.Add(ScriptDomain.Letter, FontSectionTag.LetterDomain);
            _fontTags.Add(ScriptDomain.Acronym, FontSectionTag.AcronymDomain);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Domain config schema.
        /// </summary>
        public static XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    _schema =
                        XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Config.DomainConfig.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets Domain config file path.
        /// </summary>
        public string FilePath
        {
            get 
            { 
                return _filePath;
            }
        }

        /// <summary>
        /// Gets Domain lists.
        /// </summary>
        public Dictionary<ScriptDomain, DomainConfigList> DomainLists
        {
            get 
            { 
                return _domainLists; 
            }
        }

        #endregion

        #region Public Operations

        /// <summary>
        /// Load from XML file.
        /// </summary>
        /// <param name="filePath">File path to be load.</param>
        public void Load(string filePath)
        {
            // Check the configuration file first
            try
            {
                XmlHelper.Validate(filePath, Schema);
            }
            catch (InvalidDataException ide)
            {
                string message = Helper.NeutralFormat(
                    "The configuration file [{0}] error is found.",
                    filePath);
                throw new InvalidDataException(message, ide);
            }

            _domainLists.Clear();
            _filePath = filePath;

            // Load domain lists
            XmlDocument dom = new XmlDocument();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("tts", Schema.TargetNamespace);
            dom.Load(filePath);

            XmlNodeList domainNodes = dom.DocumentElement.SelectNodes(@"tts:domain", nsmgr);
            foreach (XmlNode node in domainNodes)
            {
                XmlElement domainEle = node as XmlElement;
                string domainTypeText = domainEle.GetAttribute("type");
                if (string.IsNullOrEmpty(domainTypeText))
                {
                    string message = Helper.NeutralFormat("Domain type is not defined");
                    throw new InvalidDataException(message);
                }

                ScriptDomain domainType = (ScriptDomain)Enum.Parse(typeof(ScriptDomain), domainTypeText);

                if (!Enum.IsDefined(typeof(ScriptDomain), domainType))
                {
                    string message = Helper.NeutralFormat("Undefined domain type: [{0}]",
                        domainType);
                    throw new InvalidDataException(message);
                }

                DomainConfigList domainList;
                if (domainType == ScriptDomain.Number)
                {
                    domainList = ParseNumberDomain(domainEle, nsmgr);
                }
                else if (domainType == ScriptDomain.Acronym)
                {
                    domainList = ParseAcronymDomain(domainEle, nsmgr);
                }
                else
                {
                    domainList = ParseDomain(domainEle, domainType, nsmgr);
                }

                // Set domain index file extension name
                if (!_fontExtNames.ContainsKey(domainType))
                {
                    string message = Helper.NeutralFormat("Undefined domain file extension: [{0}]",
                        domainType);
                    throw new InvalidDataException(message);
                }

                domainList.FontExtName = _fontExtNames[domainType];

                // Set font tag of domain index file
                if (!_fontTags.ContainsKey(domainType))
                {
                    string message = Helper.NeutralFormat("Undefined domain font tag: [{0}]",
                        domainType);
                    throw new InvalidDataException(message);
                }

                domainList.FontTag = _fontTags[domainType];

                _domainLists.Add(domainType, domainList);
            }
        }

        /// <summary>
        /// Save as xml file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        public void Save(string filePath)
        {
            XmlDocument dom = new XmlDocument();
            dom.NameTable.Add(Schema.TargetNamespace);
            XmlDeclaration declaration = dom.CreateXmlDeclaration("1.0", "utf-8", null);

            // Root element
            XmlElement rootEle = dom.CreateElement("domains", Schema.TargetNamespace);

            // domin elements
            foreach (ScriptDomain domainType in _domainLists.Keys)
            {
                XmlElement domainEle = dom.CreateElement("domain", Schema.TargetNamespace);
                domainEle.SetAttribute("type", Enum.GetName(typeof(ScriptDomain), domainType));

                DomainConfigList domainList = _domainLists[domainType];
                XmlElement scriptsEle = GetScriptSection(dom, domainList.Items);
                domainEle.AppendChild(scriptsEle);

                if (domainType == ScriptDomain.Number)
                {
                    NumberDomainConfigList numberDomainList = domainList as NumberDomainConfigList;
                    XmlElement digitalsEle = GetDigitalSection(dom, numberDomainList.Digitals);
                    domainEle.AppendChild(digitalsEle);
                }
                else if (domainType == ScriptDomain.Acronym)
                {
                    AcronymDomainConfigList acronymDomainList = domainList as AcronymDomainConfigList;
                    XmlElement acronymsEle = GetAcronymSection(dom, acronymDomainList.Acronyms);
                    domainEle.AppendChild(acronymsEle);
                }

                rootEle.AppendChild(domainEle);
            }

            dom.AppendChild(declaration);
            dom.AppendChild(rootEle);
            dom.Save(filePath);

            // Performance compatibility format checking
            XmlHelper.Validate(filePath, Schema);
        }

        #endregion

        #region Private Operations

        /// <summary>
        /// Parse number domain.
        /// </summary>
        /// <param name="domainEle">Domain element.</param>
        /// <param name="nsmgr">Xml name space manager.</param>
        /// <returns>Domain list.</returns>
        private static NumberDomainConfigList ParseNumberDomain(XmlElement domainEle, XmlNamespaceManager nsmgr)
        {
            NumberDomainConfigList domainList = new NumberDomainConfigList();
            domainList.Domain = ScriptDomain.Number;

            // Parse scripts section
            XmlNode scriptsNode = domainEle.SelectSingleNode(@"tts:scripts", nsmgr);
            ParseScriptSection(scriptsNode, ScriptDomain.Number, domainList.Items);

            // Parse digitals section
            XmlNode digitalsNode = domainEle.SelectSingleNode(@"tts:digitals", nsmgr);
            ParseDigitalSection(digitalsNode, domainList.Digitals);

            return domainList;
        }

        /// <summary>
        /// Parse acronym domain.
        /// </summary>
        /// <param name="domainEle">Domain element.</param>
        /// <param name="nsmgr">Xml name space manager.</param>
        /// <returns>Domain list.</returns>
        private static AcronymDomainConfigList ParseAcronymDomain(XmlElement domainEle, XmlNamespaceManager nsmgr)
        {
            AcronymDomainConfigList domainList = new AcronymDomainConfigList();
            domainList.Domain = ScriptDomain.Acronym;

            // Parse scripts section
            XmlNode scriptsNode = domainEle.SelectSingleNode(@"tts:scripts", nsmgr);
            ParseScriptSection(scriptsNode, ScriptDomain.Number, domainList.Items);

            // Parse digitals section
            XmlNode acronymsNode = domainEle.SelectSingleNode(@"tts:acronyms", nsmgr);
            ParseAcronymSection(acronymsNode, domainList.Acronyms);

            return domainList;
        }

        /// <summary>
        /// Parse domain element.
        /// </summary>
        /// <param name="domainEle">Domain element.</param>
        /// <param name="domainType">Domain type.</param>
        /// <param name="nsmgr">Xml name space manager.</param>
        /// <returns>Domain list.</returns>
        private static DomainConfigList ParseDomain(XmlElement domainEle, ScriptDomain domainType,
            XmlNamespaceManager nsmgr)
        {
            DomainConfigList domainList = new DomainConfigList();
            domainList.Domain = domainType;

            XmlNode scriptsNode = domainEle.SelectSingleNode(@"tts:scripts", nsmgr);
            ParseScriptSection(scriptsNode, domainType, domainList.Items);
            
            if (domainEle.HasAttribute("sharedWithNormalDomain"))
            {
                domainList.SharedWithNormalDomain = bool.Parse(domainEle.GetAttribute("sharedWithNormalDomain"));
            }

            return domainList;
        }

        /// <summary>
        /// Parse scripts element.
        /// </summary>
        /// <param name="scriptsNode">Xml node.</param>
        /// <param name="domainType">Domain type.</param>
        /// <param name="items">Script range items.</param>
        private static void ParseScriptSection(XmlNode scriptsNode, ScriptDomain domainType,
            Collection<ScriptRangeItem> items)
        {
            Debug.Assert(scriptsNode != null);
            Debug.Assert(items != null);

            foreach (XmlNode node in scriptsNode.ChildNodes)
            {
                XmlElement scriptEle = node as XmlElement;
                
                string startSentenceId = scriptEle.GetAttribute("from");
                if (string.IsNullOrEmpty(startSentenceId))
                {
                    string message = Helper.NeutralFormat("Empty start sentence id");
                    throw new InvalidDataException(message);
                }

                string endSentenceId = scriptEle.GetAttribute("to");
                if (string.IsNullOrEmpty(endSentenceId))
                {
                    string message = Helper.NeutralFormat("Empty end sentence id");
                    throw new InvalidDataException(message);
                }

                if (string.Compare(startSentenceId, endSentenceId, StringComparison.Ordinal) > 0)
                {
                    string message = Helper.NeutralFormat("Start Sentence Id = [{0}] is not " +
                        "less than End Sentence Id [{1}]", startSentenceId, endSentenceId);
                    throw new InvalidDataException(message);
                }

                ScriptRangeItem item;
                item.StartSentenceId = startSentenceId;
                item.EndSentenceId = endSentenceId;
                item.Domain = domainType;

                items.Add(item);
            }
        }

        /// <summary>
        /// Parse digitals element.
        /// </summary>
        /// <param name="digitalsNode">Xml node.</param>
        /// <param name="items">Digital Word items.</param>
        private static void ParseDigitalSection(XmlNode digitalsNode,
            Dictionary<string, DigitalWordItem> items)
        {
            Debug.Assert(digitalsNode != null);
            Debug.Assert(items != null);

            foreach (XmlNode node in digitalsNode.ChildNodes)
            {
                XmlElement digitalEle = node as XmlElement;

                string word = digitalEle.GetAttribute("word");
                if (string.IsNullOrEmpty(word))
                {
                    string message = Helper.NeutralFormat("Empty word");
                    throw new InvalidDataException(message);
                }

                string sentenceId = digitalEle.GetAttribute("sentenceId");
                if (string.IsNullOrEmpty(sentenceId))
                {
                    string message = Helper.NeutralFormat("Empty sentence id");
                    throw new InvalidDataException(message);
                }

                string wordIndexText = digitalEle.GetAttribute("wordIndex");
                if (string.IsNullOrEmpty(wordIndexText))
                {
                    string message = Helper.NeutralFormat("Empty word index");
                    throw new InvalidDataException(message);
                }

                int wordIndex = int.Parse(wordIndexText, CultureInfo.InvariantCulture);

                string groupText = digitalEle.GetAttribute("group");
                if (string.IsNullOrEmpty(groupText))
                {
                    string message = Helper.NeutralFormat("Empty group");
                    throw new InvalidDataException(message);
                }

                int group = int.Parse(groupText, CultureInfo.InvariantCulture);

                DigitalWordItem item = new DigitalWordItem();
                item.Word = word;
                item.WordIndex = wordIndex;
                item.SentenceId = sentenceId;
                item.Group = group;

                if (items.ContainsKey(item.Id))
                {
                    string message = Helper.NeutralFormat("Found duplicate word: " +
                        "id = {0}, text = {1}", item.Id, item.Word);
                    throw new InvalidDataException(message);
                }

                items.Add(item.Id, item);
            }
        }

        /// <summary>
        /// Parse acronyms element.
        /// </summary>
        /// <param name="acronymsNode">Xml node.</param>
        /// <param name="items">Acronym Word items.</param>
        private static void ParseAcronymSection(XmlNode acronymsNode,
            Dictionary<string, AcronymWordItem> items)
        {
            Debug.Assert(acronymsNode != null);
            Debug.Assert(items != null);

            foreach (XmlNode node in acronymsNode.ChildNodes)
            {
                XmlElement acronymEle = node as XmlElement;

                string word = acronymEle.GetAttribute("word");
                if (string.IsNullOrEmpty(word))
                {
                    string message = Helper.NeutralFormat("Empty word");
                    throw new InvalidDataException(message);
                }

                string sentenceId = acronymEle.GetAttribute("sentenceId");
                if (string.IsNullOrEmpty(sentenceId))
                {
                    string message = Helper.NeutralFormat("Empty sentence id");
                    throw new InvalidDataException(message);
                }

                string wordIndexText = acronymEle.GetAttribute("wordIndex");
                if (string.IsNullOrEmpty(wordIndexText))
                {
                    string message = Helper.NeutralFormat("Empty word index");
                    throw new InvalidDataException(message);
                }

                int wordIndex = int.Parse(wordIndexText, CultureInfo.InvariantCulture);

                string groupText = acronymEle.GetAttribute("group");
                if (string.IsNullOrEmpty(groupText))
                {
                    string message = Helper.NeutralFormat("Empty group");
                    throw new InvalidDataException(message);
                }

                int group = int.Parse(groupText, CultureInfo.InvariantCulture);

                AcronymWordItem item = new AcronymWordItem();
                item.Word = word;
                item.WordIndex = wordIndex;
                item.SentenceId = sentenceId;
                item.Group = group;

                if (items.ContainsKey(item.Id))
                {
                    string message = Helper.NeutralFormat("Found duplicate word: " +
                        "id = {0}, text = {1}", item.Id, item.Word);
                    throw new InvalidDataException(message);
                }

                items.Add(item.Id, item);
            }
        }

        /// <summary>
        /// Get scripts xml element.
        /// </summary>
        /// <param name="dom">Xml document.</param>
        /// <param name="items">Script range items.</param>
        /// <returns>Scripts xml element.</returns>
        private static XmlElement GetScriptSection(XmlDocument dom,
            Collection<ScriptRangeItem> items)
        {
            XmlElement scriptsEle = dom.CreateElement("scripts",
                Schema.TargetNamespace);

            foreach (ScriptRangeItem item in items)
            {
                XmlElement scriptEle = dom.CreateElement("script",
                    Schema.TargetNamespace);
                scriptEle.SetAttribute("from", item.StartSentenceId);
                scriptEle.SetAttribute("to", item.EndSentenceId);

                scriptsEle.AppendChild(scriptEle);
            }

            return scriptsEle;
        }

        /// <summary>
        /// Create digitals xml element.
        /// </summary>
        /// <param name="dom">Xml document.</param>
        /// <param name="items">Digital word items.</param>
        /// <returns>Digitals xml element.</returns>
        private static XmlElement GetDigitalSection(XmlDocument dom,
            Dictionary<string, DigitalWordItem> items)
        {
            XmlElement digitalsEle = dom.CreateElement("digitals",
                Schema.TargetNamespace);

            foreach (string key in items.Keys)
            {
                DigitalWordItem item = items[key];

                XmlElement digitalEle = dom.CreateElement("digital",
                    Schema.TargetNamespace);
                digitalEle.SetAttribute("word", item.Word);
                digitalEle.SetAttribute("group", item.Group.ToString(CultureInfo.InvariantCulture));
                digitalEle.SetAttribute("sentenceId", item.SentenceId.ToString());
                digitalEle.SetAttribute("wordIndex", item.WordIndex.ToString(CultureInfo.InvariantCulture));

                digitalsEle.AppendChild(digitalEle);
            }

            return digitalsEle;
        }

        /// <summary>
        /// Create acronyms xml element.
        /// </summary>
        /// <param name="dom">Xml document.</param>
        /// <param name="items">Acronym word items.</param>
        /// <returns>Acronyms xml element.</returns>
        private static XmlElement GetAcronymSection(XmlDocument dom,
            Dictionary<string, AcronymWordItem> items)
        {
            XmlElement acronymsEle = dom.CreateElement("acronyms",
                Schema.TargetNamespace);

            foreach (string key in items.Keys)
            {
                AcronymWordItem item = items[key];

                XmlElement acronymEle = dom.CreateElement("acronym",
                    Schema.TargetNamespace);
                acronymEle.SetAttribute("word", item.Word);
                acronymEle.SetAttribute("group", item.Group.ToString(CultureInfo.InvariantCulture));
                acronymEle.SetAttribute("sentenceId", item.SentenceId.ToString());
                acronymEle.SetAttribute("wordIndex", item.WordIndex.ToString(CultureInfo.InvariantCulture));

                acronymsEle.AppendChild(acronymEle);
            }

            return acronymsEle;
        }

        #endregion
    }
}