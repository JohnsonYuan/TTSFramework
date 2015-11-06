//----------------------------------------------------------------------------
// <copyright file="ConfigurationInput.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements the ConfigurationInput class.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.FlowEngine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Class to process the input element in configuration file.
    /// </summary>
    public class ConfigurationInput
    {
        #region Fields

        /// <summary>
        /// The name of input element.
        /// </summary>
        public const string InputElementName = "input";

        /// <summary>
        /// The name of the "name" attribute.
        /// </summary>
        public const string InputElementNameAttribute = "name";

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of the input item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the input item.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the value is a CData section.
        /// </summary>
        public bool IsCdataSection { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Validates the CDATA section.
        /// </summary>
        /// <param name="cdata">The Xml string in CDATA section.</param>
        /// <param name="include">The schema should be included.</param>
        /// <param name="type">The type name in the included schema to validate the CDATA.</param>
        /// <returns>The schema which validated the CDATA.</returns>
        public static XmlSchema ValidateCdataSection(string cdata, XmlSchemaInclude include, string type)
        {
            if (string.IsNullOrEmpty(cdata))
            {
                throw new ArgumentNullException("cdata");
            }

            if (include == null)
            {
                throw new ArgumentNullException("include");
            }

            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentNullException("type");
            }

            // Creates a XmlDocument to load the cdata, to get a the document element.
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(cdata);
            }
            catch (XmlException e)
            {
                throw new ArgumentException(Helper.NeutralFormat("Input data is not a Xml document - \"{0}\"", cdata), e);
            }

            if (doc.DocumentElement == null)
            {
                throw new ArgumentException("Input data have no document element - \"{0}\"", cdata);
            }

            // Generates a fake document by adding a root element.
            string xml = GenerateDocumentForCDataSection(cdata);

            // Creates a stream to save the document.
            using (MemoryStream stream = new MemoryStream())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(xml);
                stream.Write(bytes, 0, bytes.Length);
                stream.Seek(0, SeekOrigin.Begin);

                // Creates a schema to validate the document.
                XmlSchema schema = GenerateSchemaForCDataSection(include, doc.DocumentElement.Name, type);

                try
                {
                    // Validates the stream.
                    XmlHelper.Validate(stream, schema);
                }
                catch (InvalidDataException ide)
                {
                    throw new InvalidDataException(
                        Helper.NeutralFormat("Validation failed in input data \"{0}\"", cdata), ide);
                }

                return schema;
            }
        }

        /// <summary>
        /// Loads the configuration input element from a given XmlElement.
        /// </summary>
        /// <param name="element">The given XmlElement to load.</param>
        public void Load(XmlElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            if (element.LocalName != InputElementName)
            {
                throw new ArgumentException(Helper.NeutralFormat("\"{0}\" expected by \"{1}\" inputed", InputElementName,
                    element.Name));
            }

            Name = element.Attributes[InputElementNameAttribute].Value;
            if (string.IsNullOrEmpty(Name))
            {
                throw new ArgumentException(Helper.NeutralFormat("\"{0}\" element has an empty attribute \"{1}\"",
                    InputElementName, InputElementNameAttribute));
            }

            IEnumerable<XmlNode> childNodes = element.ChildNodes.Cast<XmlNode>().Where(n => !(n is XmlComment));
            int childCount = childNodes.Count();
            if (childCount > 1)
            {
                throw new ArgumentException(Helper.NeutralFormat("\"{0}\" element has multiple child element",
                    InputElementName));
            }

            if (childCount == 0)
            {
                IsCdataSection = false;
                Value = string.Empty;
            }
            else
            {
                XmlNode node = childNodes.Single();
                if (node is XmlCDataSection)
                {
                    IsCdataSection = true;
                }
                else if (node is XmlText)
                {
                    IsCdataSection = false;
                }
                else
                {
                    throw new ArgumentException(
                        Helper.NeutralFormat("\"{0}\" element can only contain Text or CDataSection node",
                            InputElementName));
                }

                Value = node.InnerText;
            }
        }

        /// <summary>
        /// Converts the configuration input element to XmlElement.
        /// </summary>
        /// <param name="doc">The given XmlDocument who will be the document of returned XmlElement.</param>
        /// <param name="schema">The given XmlSchema.</param>
        /// <returns>The XmlElement holds this input element.</returns>
        public XmlElement ToElement(XmlDocument doc, XmlSchema schema)
        {
            if (doc == null)
            {
                throw new ArgumentNullException("doc");
            }

            if (schema == null)
            {
                throw new ArgumentNullException("schema");
            }

            XmlElement e = doc.CreateElement(InputElementName, schema.TargetNamespace);
            e.SetAttribute(InputElementNameAttribute, Name);

            if (IsCdataSection)
            {
                XmlCDataSection section = doc.CreateCDataSection(Value);
                e.AppendChild(section);
            }
            else
            {
                XmlText text = doc.CreateTextNode(Value);
                e.AppendChild(text);
            }

            return e;
        }

        /// <summary>
        /// Generates an integrate document for CDATA section.
        /// </summary>
        /// <param name="cdata">The given CDATA section.</param>
        /// <returns>The generated document.</returns>
        private static string GenerateDocumentForCDataSection(string cdata)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            builder.AppendLine("<cdataSection xmlns=\"http://schemas.microsoft.com/tts/toolsuite\">");
            builder.AppendLine(cdata);
            builder.AppendLine("</cdataSection>");
            return builder.ToString();
        }

        /// <summary>
        /// Generates a schema to validate the generated document.
        /// </summary>
        /// <param name="include">The schema should be included.</param>
        /// <param name="name">The name of the CDATA section.</param>
        /// <param name="type">The type name in the included schema to validate the CDATA.</param>
        /// <returns>The generated schema.</returns>
        private static XmlSchema GenerateSchemaForCDataSection(XmlSchemaInclude include, string name, string type)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            builder.AppendLine("<xs:schema attributeFormDefault=\"unqualified\" elementFormDefault=\"qualified\"");
            builder.AppendLine(" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"");
            builder.AppendLine(" targetNamespace=\"http://schemas.microsoft.com/tts/toolsuite\"");
            builder.AppendLine(" xmlns=\"http://schemas.microsoft.com/tts/toolsuite\">");
            builder.AppendLine("<xs:element name=\"cdataSection\">");
            builder.AppendLine("<xs:complexType>");
            builder.AppendLine("<xs:sequence>");
            builder.AppendFormat("<xs:element name=\"{0}\" type=\"{1}\" />", name, type);
            builder.AppendLine("</xs:sequence>");
            builder.AppendLine("</xs:complexType>");
            builder.AppendLine("</xs:element>");
            builder.Append("</xs:schema>");

            using (TextReader reader = new StringReader(builder.ToString()))
            {
                XmlSchema schema = XmlSchema.Read(reader, null);
                schema.Includes.Add(include);
                return schema;
            }
        }

        #endregion
    }
}