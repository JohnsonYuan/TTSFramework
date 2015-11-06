//----------------------------------------------------------------------------
// <copyright file="QuotationMarkTable.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Class defines quotation mark table.
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
    /// Quotation direction.
    /// </summary>
    public enum QuotationDirect
    {
        /// <summary>
        /// Direct free quotation mark.
        /// </summary>
        Neutral,

        /// <summary>
        /// Oriented quotation mark.
        /// </summary>
        Oriented,
    }

    /// <summary>
    /// Quotation Mark Pair.
    /// </summary>
    public class QuotationMark
    {
        /// <summary>
        /// Gets or sets the left character of this quotation mark pair.
        /// </summary>
        public char Left { get; set; }

        /// <summary>
        /// Gets or sets the right character of this quotation mark pair.
        /// </summary>
        public char Right { get; set; }

        /// <summary>
        /// Gets or sets the direction type of this quotation mark.
        /// </summary>
        public QuotationDirect Direct { get; set; }
    }

    /// <summary>
    /// Quotation mark table.
    /// </summary>
    public class QuotationMarkTable : XmlDataFile
    {
        /// <summary>
        /// Schema information of quotation mark table.
        /// </summary>
        private static XmlSchema _schema;

        /// <summary>
        /// Quotation marks.
        /// </summary>
        private List<QuotationMark> _items = new List<QuotationMark>();

        #region Construction

        /// <summary>
        /// Initializes a new instance of the QuotationMarkTable class.
        /// </summary>
        public QuotationMarkTable()
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
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.QuotationMarkTable.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets the quotation mark items.
        /// </summary>
        public List<QuotationMark> Items
        {
            get { return _items; }
        }

        #endregion

        /// <summary>
        /// Creates a quotation mark table with given file path.
        /// </summary>
        /// <param name="filePath">The location of the quotation mark file to load left.</param>
        /// <returns>A new instance of Quotation Mark Table loaded from file.</returns>
        public static QuotationMarkTable Read(string filePath)
        {
            QuotationMarkTable table = new QuotationMarkTable();
            table.Load(filePath);
            return table;
        }

        /// <summary>
        /// Validate the state of this instance.
        /// </summary>
        public override void Validate()
        {
            StringBuilder eb = new StringBuilder();
            foreach (var item in _items)
            {
                if (item.Direct == QuotationDirect.Neutral)
                {
                    if (item.Left != item.Right)
                    {
                        eb.AppendLine(Helper.NeutralFormat("The direction neutral quotation mark should have " +
                            "the same left and right symbols. But it is with left [{0]] and right [[1]].{2}",
                            item.Left, item.Right, Environment.NewLine));
                    }
                }
            }

            foreach (var item in _items.GroupBy(i => i.Left).Where(g => g.Count() > 1))
            {
                eb.AppendLine(Helper.NeutralFormat(
                    "Left side quotation mark [{0}] is duplicated {1} times in the quotation mark table.",
                    item.Key, item.Count()));
            }

            foreach (var item in _items.GroupBy(i => i.Right).Where(g => g.Count() > 1))
            {
                eb.AppendLine(Helper.NeutralFormat(
                    "Right side quotation mark [{0}] is duplicated {1} times in the quotation mark table.",
                    item.Key, item.Count()));
            }

            if (eb.Length > 0)
            {
                throw new System.IO.InvalidDataException(eb.ToString());
            }
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

            XmlNodeList nodeList = xmlDoc.DocumentElement.SelectNodes("/tts:quotationMarkTable/tts:mark", nsmgr);
            _items.Clear();
            foreach (XmlNode xmlNode in nodeList)
            {
                XmlElement xmlEle = xmlNode as XmlElement;

                string left = xmlEle.GetAttribute("left");
                string right = xmlEle.GetAttribute("right");

                left = ReplaceHexString(left).Trim();
                right = ReplaceHexString(right).Trim();

                QuotationMark mark = new QuotationMark();
                if (left.Length != 1)
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Invalid left side quotation mark [[0]] is find, which should be single character.", left));
                }
                else
                {
                    mark.Left = left[0];
                }

                if (right.Length != 1)
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Invalid right side quotation mark [[0]] is find, which should be single character.", right));
                }
                else
                {
                    mark.Right = right[0];
                }

                mark.Direct = (QuotationDirect)Enum.Parse(typeof(QuotationDirect), xmlEle.GetAttribute("direct"));

                _items.Add(mark);
            }
        }

        /// <summary>
        /// Performances save operation.
        /// </summary>
        /// <param name="writer">The writer instance to write XML data out.</param>
        /// <param name="contentController">Content controller object.</param>
        protected override void PerformanceSave(XmlWriter writer, object contentController)
        {
            writer.WriteStartElement("quotationMarkTable", Schema.TargetNamespace);
            writer.WriteAttributeString("lang", Localor.LanguageToString(Language));

            foreach (var item in _items)
            {
                writer.WriteStartElement("mark");

                writer.WriteAttributeString("left", item.Left.ToString());
                writer.WriteAttributeString("right", item.Right.ToString());
                writer.WriteAttributeString("direct", item.Direct.ToString());

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }
}