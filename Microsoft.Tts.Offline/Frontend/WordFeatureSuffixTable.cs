//----------------------------------------------------------------------------
// <copyright file="WordFeatureSuffixTable.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Class defines parallel struct table.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Noun suffix.
    /// </summary>
    public class NounSuffix
    {
        /// <summary>
        /// Gets or sets the word text of the noun suffix.
        /// </summary>
        public string Text { get; set; }
    }

    /// <summary>
    /// Adjective suffix.
    /// </summary>
    public class AdjSuffix
    {
        /// <summary>
        /// Gets or sets the word text of the adjective suffix.
        /// </summary>
        public string Text { get; set; }
    }

    /// <summary>
    /// Verb suffix.
    /// </summary>
    public class VerbSuffix
    {
        /// <summary>
        /// Gets or sets the word text of the verb suffix.
        /// </summary>
        public string Text { get; set; }
    }

    /// <summary>
    /// Separator character.
    /// </summary>
    public class SeparatorChar
    {
        /// <summary>
        /// Gets or sets the word text of the separator character.
        /// </summary>
        public string Text { get; set; }
    }

    /// <summary>
    /// Word feature suffix table.
    /// </summary>
    public class WordFeatureSuffixTable : XmlDataFile
    {
        /// <summary>
        /// Schema information of parallel struct table.
        /// </summary>
        private static XmlSchema _schema;

        /// <summary>
        /// Noun suffixes.
        /// </summary>
        private List<NounSuffix> _nounItems = new List<NounSuffix>();

        /// <summary>
        /// Adjective suffixes.
        /// </summary>
        private List<AdjSuffix> _adjItems = new List<AdjSuffix>();

        /// <summary>
        /// Verb suffixes.
        /// </summary>
        private List<VerbSuffix> _verbItems = new List<VerbSuffix>();

        /// <summary>
        /// Separator characters.
        /// </summary>
        private List<SeparatorChar> _separatorItems = new List<SeparatorChar>();

        #region Construction

        /// <summary>
        /// Initializes a new instance of the WordFeatureSuffixTable class.
        /// </summary>
        public WordFeatureSuffixTable()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the configuration schema.
        /// </summary>
        public override XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.WordFeatureSuffixTable.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets the noun suffix items.
        /// </summary>
        public List<NounSuffix> NounItems
        {
            get { return _nounItems; }
        }

        /// <summary>
        /// Gets the adjective suffix items.
        /// </summary>
        public List<AdjSuffix> AdjItems
        {
            get { return _adjItems; }
        }

        /// <summary>
        /// Gets the verb suffix items.
        /// </summary>
        public List<VerbSuffix> VerbItems
        {
            get { return _verbItems; }
        }

        /// <summary>
        /// Gets the separator character items.
        /// </summary>
        public List<SeparatorChar> SeparatorItems
        {
            get { return _separatorItems; }
        }

        #endregion

        /// <summary>
        /// Creates a word feature suffix table with given file path.
        /// </summary>
        /// <param name="filePath">The location of the suffix file to load left.</param>
        /// <returns>A new instance of WordFeatureSuffix Table loaded from file.</returns>
        public static WordFeatureSuffixTable Read(string filePath)
        {
            WordFeatureSuffixTable table = new WordFeatureSuffixTable();
            table.Load(filePath);
            return table;
        }

        /// <summary>
        /// Validate the state of this instance.
        /// </summary>
        public override void Validate()
        {
        }

        /// <summary>
        /// Load data from XML document instance into this instance.
        /// </summary>
        /// <param name="xmlDoc">Xml document instance.</param>
        /// <param name="nsmgr">Namespace instance.</param>
        /// <param name="contentController">Content controller object.</param>
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

            XmlNodeList nodeList = xmlDoc.DocumentElement.SelectNodes("/tts:wordFeatureSuffixTable/tts:nounSuffixes/tts:nounSuffix", nsmgr);
            _nounItems.Clear();
            foreach (XmlNode xmlNode in nodeList)
            {
                XmlElement xmlEle = xmlNode as XmlElement;

                string text = xmlEle.GetAttribute("text");

                NounSuffix nounSuffix = new NounSuffix();
                nounSuffix.Text = text;

                _nounItems.Add(nounSuffix);
            }

            nodeList = xmlDoc.DocumentElement.SelectNodes("/tts:wordFeatureSuffixTable/tts:adjSuffixes/tts:adjSuffix", nsmgr);
            _adjItems.Clear();
            foreach (XmlNode xmlNode in nodeList)
            {
                XmlElement xmlEle = xmlNode as XmlElement;

                string text = xmlEle.GetAttribute("text");

                AdjSuffix adjSuffix = new AdjSuffix();
                adjSuffix.Text = text;

                _adjItems.Add(adjSuffix);
            }

            nodeList = xmlDoc.DocumentElement.SelectNodes("/tts:wordFeatureSuffixTable/tts:verbSuffixes/tts:verbSuffix", nsmgr);
            _verbItems.Clear();
            foreach (XmlNode xmlNode in nodeList)
            {
                XmlElement xmlEle = xmlNode as XmlElement;

                string text = xmlEle.GetAttribute("text");

                VerbSuffix verbSuffix = new VerbSuffix();
                verbSuffix.Text = text;

                _verbItems.Add(verbSuffix);
            }

            nodeList = xmlDoc.DocumentElement.SelectNodes("/tts:wordFeatureSuffixTable/tts:separatorChars/tts:separatorChar", nsmgr);
            _separatorItems.Clear();
            foreach (XmlNode xmlNode in nodeList)
            {
                XmlElement xmlEle = xmlNode as XmlElement;

                string text = xmlEle.GetAttribute("text");

                SeparatorChar separatorChar = new SeparatorChar();
                separatorChar.Text = text;

                _separatorItems.Add(separatorChar);
            }
        }

        /// <summary>
        /// Performances save operation.
        /// </summary>
        /// <param name="writer">The writer instance to write XML data out.</param>
        /// <param name="contentController">Content controller object.</param>
        protected override void PerformanceSave(XmlWriter writer, object contentController)
        {
            writer.WriteStartElement("wordFeatureSuffixTable", Schema.TargetNamespace);
            writer.WriteAttributeString("lang", Localor.LanguageToString(Language));

            writer.WriteStartElement("nounSuffixes");
            foreach (var item in _nounItems)
            {
                writer.WriteStartElement("nounSuffix");
                writer.WriteAttributeString("text", item.Text.ToString());
                writer.WriteEndElement();
            }

            writer.WriteEndElement();

            writer.WriteStartElement("adjSuffixes");
            foreach (var item in _adjItems)
            {
                writer.WriteStartElement("adjSuffix");
                writer.WriteAttributeString("text", item.Text.ToString());
                writer.WriteEndElement();
            }

            writer.WriteEndElement();

            writer.WriteStartElement("verbSuffixes");
            foreach (var item in _verbItems)
            {
                writer.WriteStartElement("verbSuffix");
                writer.WriteAttributeString("text", item.Text.ToString());
                writer.WriteEndElement();
            }

            writer.WriteEndElement();

            writer.WriteStartElement("separatorChars");
            foreach (var item in _separatorItems)
            {
                writer.WriteStartElement("separatorChar");
                writer.WriteAttributeString("text", item.Text.ToString());
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }
}