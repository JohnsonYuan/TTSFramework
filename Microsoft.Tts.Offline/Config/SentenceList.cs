//----------------------------------------------------------------------------
// <copyright file="SentenceList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements SentenceList
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Config
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Drop sentence item.
    /// </summary>
    public class SentenceItem
    {
        #region Fields

        private string _sentenceId;
        private string _error;
        private string _description;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Sentence Id of the sentence.
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
        /// Gets or sets Error type of the sentence.
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
        /// Gets or sets Description of the drop sentence.
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

        #region XML operation

        /// <summary>
        /// Parse XmlNode to SentenceItem.
        /// </summary>
        /// <param name="node">XmlNode to be parsed.</param>
        /// <param name="nsmgr">Namespace manager of the node.</param>
        public void Parse(XmlNode node, XmlNamespaceManager nsmgr)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (nsmgr == null)
            {
                throw new ArgumentNullException("nsmgr");
            }

            XmlElement ele = (XmlElement)node;
            _sentenceId = node.Attributes["sid"].InnerText;

            if (node.Attributes["error"] != null)
            {
                _error = node.Attributes["error"].InnerText;
            }

            if (node.Attributes["desc"] != null)
            {
                _description = node.Attributes["desc"].InnerText;
            }
        }

        /// <summary>
        /// Create XmlNode for SentenceItem.
        /// </summary>
        /// <param name="dom">XmlDocument to which to create XmlNode.</param>
        /// <param name="xmlNamespace">Name space of the XmlElement.</param>
        /// <returns>Created SentenceItem XmlElement.</returns>
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

            XmlElement sentenceEle = dom.CreateElement("sentence", xmlNamespace);
            sentenceEle.SetAttribute("sid", _sentenceId);
            if (!string.IsNullOrEmpty(_error))
            {
                sentenceEle.SetAttribute("error", _error);
            }

            if (!string.IsNullOrEmpty(_description))
            {
                sentenceEle.SetAttribute("desc", _description);
            }

            return sentenceEle;
        }

        #endregion
    }

    /// <summary>
    /// Sentence list in this class will be dropped.
    /// </summary>
    public class SentenceList
    {
        #region Fields

        private static XmlSchema _schema;
        private Dictionary<string, SentenceItem> _sentencesDictionary = new Dictionary<string, SentenceItem>();

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
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Config.SentenceList.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets Sentence directory.
        /// </summary>
        public Dictionary<string, SentenceItem> SentencesDictionary
        {
            get { return _sentencesDictionary; }
        }

        #endregion

        #region XML operation

        /// <summary>
        /// Save to XML format file.
        /// </summary>
        /// <param name="filePath">File path to save to.</param>
        public void Save(string filePath)
        {
            XmlDocument dom = new XmlDocument();
            XmlSchema schema = ConfigSchema;
            dom.NameTable.Add(schema.TargetNamespace);

            // Root element
            XmlElement ele = dom.CreateElement("config", schema.TargetNamespace);

            XmlElement sentencesEle = dom.CreateElement("sentences", schema.TargetNamespace);
            ele.AppendChild(sentencesEle);

            foreach (string sentenceKey in _sentencesDictionary.Keys)
            {
                SentenceItem sentenceItem = _sentencesDictionary[sentenceKey];

                XmlElement sentenceEle = sentenceItem.CreateXmlElement(dom, schema.TargetNamespace);

                sentencesEle.AppendChild(sentenceEle);
            }

            dom.AppendChild(ele);
            dom.Save(filePath);

            // Performance compatibility format checking
            XmlHelper.Validate(filePath, ConfigSchema);
        }

        /// <summary>
        /// Load from XML file.
        /// </summary>
        /// <param name="filePath">File path to be load.</param>
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

            XmlNode sentencesNode = dom.DocumentElement.SelectSingleNode(@"tts:sentences", nsmgr);

            if (sentencesNode != null)
            {
                Parse(sentencesNode, nsmgr);
            }
        }

        /// <summary>
        /// Parse XmlNode to SentenceItem.
        /// </summary>
        /// <param name="node">XmlNode to be parsed.</param>
        /// <param name="nsmgr">Namespace manager of the node.</param>
        public void Parse(XmlNode node, XmlNamespaceManager nsmgr)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (nsmgr == null)
            {
                throw new ArgumentNullException("nsmgr");
            }

            XmlNodeList sentenceNodes = node.SelectNodes(@"tts:sentence", nsmgr);
            _sentencesDictionary.Clear();

            foreach (XmlNode sentenceNode in sentenceNodes)
            {
                SentenceItem item = new SentenceItem();

                item.Parse(sentenceNode, nsmgr);

                _sentencesDictionary.Add(item.SentenceId, item);
            }
        }

        /// <summary>
        /// Create XmlElement for the sentences.
        /// </summary>
        /// <param name="dom">XmlDocument on which to create XmlElement.</param>
        /// <param name="xmlNamespace">Name space of the XmlElement.</param>
        /// <returns>Created dropSentences XmlElement.</returns>
        public XmlElement CreateXmlElement(XmlDocument dom, string xmlNamespace)
        {
            XmlElement sentencesEle = null;

            if (_sentencesDictionary.Count > 0)
            {
                sentencesEle = dom.CreateElement("sentences", xmlNamespace);

                foreach (string key in _sentencesDictionary.Keys)
                {
                    SentenceItem item = _sentencesDictionary[key];
                    XmlElement sentenceEle = item.CreateXmlElement(dom, xmlNamespace);
                    sentencesEle.AppendChild(sentenceEle);
                }
            }

            return sentencesEle;
        }

        #endregion

        #region Operations

        /// <summary>
        /// Whether drop the whole sentence.
        /// </summary>
        /// <param name="sentenceId">Sentence ID to be checked.</param>
        /// <returns>Whether the sentence should be kept.</returns>
        public bool KeepSentence(string sentenceId)
        {
            return !_sentencesDictionary.ContainsKey(sentenceId);
        }

        #endregion
    }
}