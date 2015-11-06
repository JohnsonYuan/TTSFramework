//----------------------------------------------------------------------------
// <copyright file="UnitLatticeSentenceSet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements unit lattice sentences class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Unit lattice sentence set class.
    /// </summary>
    public class UnitLatticeSentenceSet : XmlDataFile
    {
        #region Private Fields

        /// <summary>
        /// XML schema.
        /// </summary>
        private static XmlSchema _schema;

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the UnitLatticeSentenceSet class.
        /// </summary>
        public UnitLatticeSentenceSet()
        {
            this.Sentences = new Collection<UnitLatticeSentence>();
        }

        #endregion
    
        #region Properties

        /// <summary>
        /// Gets schema of latticefile.xml.
        /// </summary>
        public override XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.UnitLattice.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets or sets the sentences in the file.
        /// </summary>
        public Collection<UnitLatticeSentence> Sentences { get; set; }

        #endregion

        #region public operations

        /// <summary>
        /// Add new sentence but kick off previous one, use to handle backoff.
        /// </summary>
        /// <param name="sentence">Sentence.</param>
        public void AddwithOverwrite(UnitLatticeSentence sentence)
        {
            foreach (UnitLatticeSentence sent in Sentences)
            {
                if (sent.Id == sentence.Id)
                {
                    Sentences.Remove(sent);
                    break;
                }
            }

            Sentences.Add(sentence);
        }
        #endregion 

        #region protected override operations

        /// <summary>
        /// Performance loading.
        /// </summary>
        /// <param name="reader">Stream reader.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void PerformanceLoad(StreamReader reader, object contentController)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            XmlTextReader xmlTextReader = new XmlTextReader(reader);
            while (xmlTextReader.Read())
            {
                if (xmlTextReader.NodeType == XmlNodeType.Element && xmlTextReader.Name == "sentences")
                {
                    Language = Localor.StringToLanguage(xmlTextReader.GetAttribute("lang"));
                }
                else if (xmlTextReader.NodeType == XmlNodeType.Element && xmlTextReader.Name == "sentence")
                {
                    UnitLatticeSentence sentence = new UnitLatticeSentence();
                    sentence.Load(xmlTextReader, contentController);

                    this.Sentences.Add(sentence);
                }
            }
        }

        /// <summary>
        /// Saves script into Xml writer.
        /// </summary>
        /// <param name="writer">Writer file to save into.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void PerformanceSave(XmlWriter writer, object contentController)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            this.WriteTo(writer, contentController);
        }

        #endregion

        #region private operations
        /// <summary>
        /// Writes unit lattice sentences to xml.
        /// </summary>
        /// <param name="writer">XmlWriter to save XML file.</param>
        /// <param name="contentController">Content controller.</param>
        private void WriteTo(XmlWriter writer, object contentController)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteStartElement("sentences", "http://schemas.microsoft.com/tts");
            writer.WriteAttributeString("lang", Localor.LanguageToString(Language));

            foreach (UnitLatticeSentence sentence in this.Sentences)
            {
                sentence.WriteToXml(writer, null);
            }

            writer.WriteEndElement();
        }

        #endregion
    }
}