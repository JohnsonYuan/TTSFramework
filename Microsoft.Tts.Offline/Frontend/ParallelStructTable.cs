//----------------------------------------------------------------------------
// <copyright file="ParallelStructTable.cs" company="Microsoft">
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
    /// Segment word.
    /// </summary>
    public class SegmentWord
    {
        /// <summary>
        /// Gets or sets the word text of the segment word.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the POS of the segment word.
        /// </summary>
        public string POS { get; set; }
    }

    /// <summary>
    /// Trigger word.
    /// </summary>
    public class TriggerWord
    {
        /// <summary>
        /// Gets or sets the word text of the trigger word.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the POS of the trigger word.
        /// </summary>
        public string POS { get; set; }
    }

    /// <summary>
    /// Parallel Struct table.
    /// </summary>
    public class ParallelStructTable : XmlDataFile
    {
        /// <summary>
        /// Schema information of parallel struct table.
        /// </summary>
        private static XmlSchema _schema;

        /// <summary>
        /// Segment words.
        /// </summary>
        private List<SegmentWord> _segmentItems = new List<SegmentWord>();

        /// <summary>
        /// Trigger words.
        /// </summary>
        private List<TriggerWord> _triggerItems = new List<TriggerWord>();

        #region Construction

        /// <summary>
        /// Initializes a new instance of the ParallelStructTable class.
        /// </summary>
        public ParallelStructTable()
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
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.ParallelStructTable.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets the segment word items.
        /// </summary>
        public List<SegmentWord> SegmentItems
        {
            get { return _segmentItems; }
        }

        /// <summary>
        /// Gets the trigger word items.
        /// </summary>
        public List<TriggerWord> TriggerItems
        {
            get { return _triggerItems; }
        }

        #endregion

        /// <summary>
        /// Creates a parallel struct table with given file path.
        /// </summary>
        /// <param name="filePath">The location of the parallel struct file to load left.</param>
        /// <returns>A new instance of ParallelStruct Table loaded from file.</returns>
        public static ParallelStructTable Read(string filePath)
        {
            ParallelStructTable table = new ParallelStructTable();
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

            XmlNodeList nodeList = xmlDoc.DocumentElement.SelectNodes("/tts:parallelStructTable/tts:segmentWords/tts:segmentWord", nsmgr);
            _segmentItems.Clear();
            foreach (XmlNode xmlNode in nodeList)
            {
                XmlElement xmlEle = xmlNode as XmlElement;

                string text = xmlEle.GetAttribute("text");
                string pos = xmlEle.GetAttribute("pos");

                SegmentWord segword = new SegmentWord();
                segword.Text = text;
                segword.POS = pos;

                _segmentItems.Add(segword);
            }

            _triggerItems.Clear();
            nodeList = xmlDoc.DocumentElement.SelectNodes("/tts:parallelStructTable/tts:triggerWords/tts:triggerWord", nsmgr);
            foreach (XmlNode xmlNode in nodeList)
            {
                XmlElement xmlEle = xmlNode as XmlElement;

                string text = xmlEle.GetAttribute("text");
                string pos = xmlEle.GetAttribute("pos");

                TriggerWord trgword = new TriggerWord();
                trgword.Text = text;
                trgword.POS = pos;
 
                _triggerItems.Add(trgword);
            }
        }

        /// <summary>
        /// Performances save operation.
        /// </summary>
        /// <param name="writer">The writer instance to write XML data out.</param>
        /// <param name="contentController">Content controller object.</param>
        protected override void PerformanceSave(XmlWriter writer, object contentController)
        {
            writer.WriteStartElement("parallelStructTable", Schema.TargetNamespace);
            writer.WriteAttributeString("lang", Localor.LanguageToString(Language));

            writer.WriteStartElement("segmentWords");
            foreach (var item in _segmentItems)
            {
                writer.WriteStartElement("segmentWord");
                writer.WriteAttributeString("text", item.Text.ToString());
                writer.WriteAttributeString("pos", item.POS.ToString());
                writer.WriteEndElement();
            }

            writer.WriteEndElement();

            writer.WriteStartElement("triggerWords");
            foreach (var item in _triggerItems)
            {
                writer.WriteStartElement("triggerWord");
                writer.WriteAttributeString("text", item.Text.ToString());
                writer.WriteAttributeString("pos", item.POS.ToString());
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }
}