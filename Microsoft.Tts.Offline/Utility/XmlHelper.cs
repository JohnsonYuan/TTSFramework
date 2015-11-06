//----------------------------------------------------------------------------
// <copyright file="XmlHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     Implement a class for common xml operation.
//
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// XmlHelper help operate Xml file.
    /// </summary>
    public sealed class XmlHelper
    {
        /// <summary>
        /// Schema Resource Prefix "Microsoft.Tts.Offline.".
        /// </summary>
        public static string SchemaResourcePrefix = "Microsoft.Tts.Offline.";

        /// <summary>
        /// Invalid XML characters the range 0x-0x1F (excluding white space characters 0x9, 0xA, and 0xD).
        /// </summary>
        private const int InvalidXmlCharStart = 0x0;
        private const int InvalidXmlCharEnd = 0x1f;
        private static readonly int[] ExcludeInvalidXmlChar = { 0x9, 0xA, 0xD };

        /// <summary>
        /// Prevents a default instance of the <see cref="XmlHelper"/> class from being created.
        /// XmlHelper only contain static methods, add a private default constructor
        /// To exclude warning CA1053 (Static holder types should not have constructors).
        /// </summary>
        private XmlHelper()
        {
        }

        /// <summary>
        /// Append an element with attribute to a parent element.
        /// </summary>
        /// <param name="dom">Dom.</param>
        /// <param name="elem">Parent element.</param>
        /// <param name="childElemName">Child element name.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <param name="attribValue">Attribute value.</param>
        /// <param name="schema">Schema value.</param>
        /// <returns>New create element.</returns>
        public static XmlElement AppendElement(XmlDocument dom, XmlElement elem,
            string childElemName, string attribName, string attribValue, XmlSchema schema)
        {
            return AppendElement(dom, elem, childElemName, attribName, attribValue, schema, false);
        }

        /// <summary>
        /// Append an element with attribute to a parent element.
        /// </summary>
        /// <param name="dom">Dom.</param>
        /// <param name="elem">Parent element.</param>
        /// <param name="childElemName">Child element name.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <param name="attribValue">Attribute value.</param>
        /// <param name="schema">Schema value.</param>
        /// <param name="alwaysCreateElem">Alreays create element.</param>
        /// <returns>New create element.</returns>
        public static XmlElement AppendElement(XmlDocument dom, XmlElement elem,
            string childElemName, string attribName, string attribValue,
            XmlSchema schema, bool alwaysCreateElem)
        {
            if (dom == null)
            {
                throw new ArgumentNullException("dom");
            }

            if (elem == null)
            {
                throw new ArgumentNullException("elem");
            }

            if (childElemName == null)
            {
                throw new ArgumentNullException("childElemName");
            }

            if (attribName == null)
            {
                throw new ArgumentNullException("attribName");
            }

            if (schema == null)
            {
                throw new ArgumentNullException("schema");
            }

            XmlElement childElem = null;
            if (alwaysCreateElem || !string.IsNullOrEmpty(attribValue))
            {
                childElem = dom.CreateElement(childElemName, schema.TargetNamespace);
                if (!string.IsNullOrEmpty(attribValue))
                {
                    childElem.SetAttribute(attribName, attribValue);
                }

                elem.AppendChild(childElem);
            }

            return childElem;
        }

        /// <summary>
        /// Load schema from resource in the assembly.
        /// </summary>
        /// <param name="assembly">Assembly form which to read schema.</param>
        /// <param name="resourceName">Resource to be loaded.</param>
        /// <returns>Loaded XmlSchema.</returns>
        public static XmlSchema LoadSchemaFromResource(Assembly assembly, string resourceName)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            if (string.IsNullOrEmpty(resourceName))
            {
                throw new ArgumentNullException("resourceName");
            }

            XmlSchema configSchema = null;

            Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using (StreamReader configSchemaStream = new StreamReader(stream))
                {
                    XmlTextReader xtrConfigSchema = new XmlTextReader(configSchemaStream);
                    configSchema = XmlSchema.Read(xtrConfigSchema, null);
                }
            }

            return configSchema;
        }

        /// <summary>
        /// Load schema from resource.
        /// </summary>
        /// <param name="resourceName">Resource to be loaded.</param>
        /// <returns>Loaded XmlSchema.</returns>
        public static XmlSchema LoadSchemaFromResource(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string fullResourceName = resourceName;
            bool needPrefix = !string.IsNullOrEmpty(resourceName) && !resourceName.StartsWith(
                    SchemaResourcePrefix, StringComparison.OrdinalIgnoreCase);
            if (needPrefix)
            {
                fullResourceName = SchemaResourcePrefix + "Schema." + resourceName;
            }

            XmlSchema schema = LoadSchemaFromResource(assembly, fullResourceName);
            if (schema == null && needPrefix)
            {
                fullResourceName = SchemaResourcePrefix + "Config." + resourceName;
                schema = LoadSchemaFromResource(assembly, fullResourceName);
                if (schema == null)
                {
                    schema = LoadSchemaFromResource(assembly, resourceName);
                }
            }

            LoadSchemaIncludes(schema);
             
            return schema;
        }

        /// <summary>
        /// Check if input is valid xml text.
        /// </summary>
        /// <param name="text">Input text.</param>
        /// <returns>If input is valid xml text.</returns>
        public static bool IsValidXMLText(string text)
        {
            bool result = true;
            foreach (char currentCharacter in text)
            {
                if ((currentCharacter >= InvalidXmlCharStart && currentCharacter <= InvalidXmlCharEnd) &&
                    (!ExcludeInvalidXmlChar.Contains(currentCharacter)))
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Load schema includes.
        /// </summary>
        /// <param name="schema">Schema.</param>
        public static void LoadSchemaIncludes(XmlSchema schema)
        {
            if (schema != null)
            {
                foreach (XmlSchemaObject schemaObject in schema.Includes)
                {
                    if (schemaObject.GetType() == typeof(XmlSchemaInclude))
                    {
                        XmlSchemaInclude schemaInclude = schemaObject as XmlSchemaInclude;
                        if (schemaInclude.Schema == null && !string.IsNullOrEmpty(schemaInclude.SchemaLocation))
                        {
                            schemaInclude.Schema = XmlHelper.LoadSchemaFromResource(schemaInclude.SchemaLocation);
                        }
                    }
                    else if (schemaObject.GetType() == typeof(XmlSchemaImport))
                    {
                        XmlSchemaImport schemaImport = schemaObject as XmlSchemaImport;
                        if (schemaImport.Schema == null && !string.IsNullOrEmpty(schemaImport.SchemaLocation))
                        {
                            schemaImport.Schema = XmlHelper.LoadSchemaFromResource(schemaImport.SchemaLocation);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Valid file with existing schema.
        /// </summary>
        /// <param name="filePath">File path of the config file.</param>
        /// <param name="schema">XmlSchema of the config file.</param>
        public static void Validate(string filePath, XmlSchema schema)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (schema == null)
            {
                throw new ArgumentNullException("schema");
            }

            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    Validate(reader.BaseStream, schema);
                }
            }
            catch (InvalidDataException ide)
            {
                string message = Helper.NeutralFormat("Failed to load file [{0}]", filePath);
                throw new InvalidDataException(message, ide);
            }
        }

        /// <summary>
        /// Valid XDocument with the specified XmlSchema.
        /// </summary>
        /// <param name="source">XDocument to be validate.</param>
        /// <param name="schema">XmlSchema used for validating.</param>
        public static void ValidateXDocument(XDocument source, XmlSchema schema)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (schema == null)
            {
                throw new ArgumentNullException("schema");
            }

            MemoryStream mem = new MemoryStream();
            try
            {
                using (var writer = new StreamWriter(mem))
                {
                    mem = null;

                    source.Save(writer);

                    writer.BaseStream.Seek(0, SeekOrigin.Begin);
                    XmlHelper.Validate(writer.BaseStream, schema);
                }
            }
            finally
            {
                if (null != mem)
                {
                    mem.Dispose();
                }
            }
        }

        /// <summary>
        /// Valid xml file with the specified XmlSchema.
        /// </summary>
        /// <param name="stream">Stream to be validate.</param>
        /// <param name="schema">XmlSchema used for validating.</param>
        public static void Validate(Stream stream, XmlSchema schema)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (schema == null)
            {
                throw new ArgumentNullException("schema");
            }

            try
            {
                long position = stream.Position;

                // Set the validation options
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.Schemas.Add(schema);
                foreach (XmlSchemaObject schemaObject in schema.Includes)
                {
                    if (schemaObject.GetType() == typeof(XmlSchemaInclude))
                    {
                        settings.Schemas.Add((schemaObject as XmlSchemaInclude).Schema);
                    }
                    else if (schemaObject.GetType() == typeof(XmlSchemaImport))
                    {
                        settings.Schemas.Add((schemaObject as XmlSchemaImport).Schema);
                    }
                }

                settings.ValidationType = ValidationType.Schema;

                System.Xml.XmlReader xmlReader = System.Xml.XmlReader.Create(new StreamReader(stream), settings);

                // Go through the whole xml file to validate the data
                while (xmlReader.Read())
                {
                }

                stream.Seek(position, SeekOrigin.Begin);
            }
            catch (System.Xml.Schema.XmlSchemaValidationException xsdExp)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Data format error in the line {0} : {1}", xsdExp.LineNumber, xsdExp.Message);
                throw new InvalidDataException(message);
            }
            catch (System.Xml.XmlException xmlExp)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Xml format error: {0}", xmlExp.Message);
                throw new InvalidDataException(message);
            }
            catch (System.UriFormatException uriExp)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid path: {0}.", uriExp.Message);
                throw new InvalidDataException(message);
            }
        }

        /// <summary>
        /// Get the encoding for the XML file.
        /// </summary>
        /// <param name="filePath">XML file.</param>
        /// <returns>Encoding.</returns>
        public static Encoding GetEncoding(string filePath)
        {
            Encoding encoding = Encoding.Default;

            using (XmlTextReader xmlReader = new XmlTextReader(filePath))
            {
                // Assume the first line contains the Declaration.
                if (xmlReader.Read())
                {
                    if (xmlReader.NodeType.Equals(XmlNodeType.XmlDeclaration))
                    {
                        encoding = xmlReader.Encoding;
                    }
                }
            }

            return encoding;
        }

        /// <summary>
        /// Get the line number from the XmlNode.
        /// </summary>
        /// <param name="node">XmlNode to be checked.</param>
        /// <returns>Node line number in XML file.</returns>
        public static int GetXmlNodeLineNumber(XmlNode node)
        {
            IXmlLineInfo lineInfo = node as IXmlLineInfo;
            if (lineInfo != null)
            {
                return lineInfo.LineNumber;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Get the line position from the Xml Node.
        /// </summary>
        /// <param name="node">XmlNode to be checked.</param>
        /// <returns>Xml node line position.</returns>
        public static int GetXmlNodeLinePosition(XmlNode node)
        {
            IXmlLineInfo lineInfo = node as IXmlLineInfo;
            if (lineInfo != null)
            {
                return lineInfo.LinePosition;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Get attribute of xml element.
        /// </summary>
        /// <param name="ele">Xml element.</param>
        /// <param name="attrib">Attribute name string.</param>
        /// <returns>Attribute value string.</returns>
        public static string GetAttributeString(XmlElement ele, string attrib)
        {
            if (ele == null)
            {
                throw new ArgumentNullException("ele");
            }

            if (string.IsNullOrEmpty(attrib))
            {
                throw new ArgumentNullException("attrib");
            }

            string attribValue = ele.GetAttribute(attrib);

            if (string.IsNullOrEmpty(attribValue))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "cannot find attribute {0} in element {1}",
                    attrib, ele.Name);
                throw new InvalidDataException(message);
            }

            return attribValue;
        }

        /// <summary>
        /// Get integer attribute value of xml element.
        /// </summary>
        /// <param name="ele">Xml element.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>Attribute value.</returns>
        public static int GetAttributeNumber(XmlElement ele, string attribName)
        {
            if (ele == null)
            {
                throw new ArgumentNullException("ele");
            }

            if (string.IsNullOrEmpty(attribName))
            {
                throw new ArgumentNullException("attribName");
            }

            int attribValue;

            string attribValueText = ele.GetAttribute(attribName);
            if (!string.IsNullOrEmpty(attribValueText))
            {
                if (!int.TryParse(attribValueText, NumberStyles.Any,
                    CultureInfo.InvariantCulture.NumberFormat, out attribValue))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "[{0}]: Invalid attribute value [{1}]",
                        attribName, attribValueText);
                    throw new InvalidDataException(message);
                }
            }
            else
            {
                attribValue = 0;
            }

            return attribValue;
        }

        /// <summary>
        /// Get date time attribute value of xml element.
        /// </summary>
        /// <param name="ele">Xml element.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>Attribute value.</returns>
        public static DateTime GetAttributeDateTime(XmlElement ele, string attribName)
        {
            if (ele == null)
            {
                throw new ArgumentNullException("ele");
            }

            if (string.IsNullOrEmpty(attribName))
            {
                throw new ArgumentNullException("attribName");
            }

            DateTime attribValue;

            string attribValueText = ele.GetAttribute(attribName);
            if (!string.IsNullOrEmpty(attribValueText))
            {
                if (!DateTime.TryParse(attribValueText,
                    CultureInfo.InvariantCulture.DateTimeFormat,
                    DateTimeStyles.None, out attribValue))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "[{0}]: Invalid attribute value [{1}]",
                        attribName, attribValueText);
                    throw new InvalidDataException(message);
                }
            }
            else
            {
                attribValue = DateTime.Now;
            }

            return attribValue;
        }

        /// <summary>
        /// Write element attribute.
        /// </summary>
        /// <param name="writer">Xml writer.</param>
        /// <param name="nameSpace">Name space.</param>
        /// <param name="elementName">Element name.</param>
        /// <param name="attributeName">Attribute name.</param>
        /// <param name="attributeValue">Attribute value.</param>
        public static void WriteElementAttribute(XmlWriter writer, string nameSpace,
            string elementName, string attributeName, string attributeValue)
        {
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(nameSpace);
            Helper.ThrowIfNull(elementName);
            Helper.ThrowIfNull(attributeName);
            Helper.ThrowIfNull(attributeValue);

            writer.WriteStartElement(elementName, nameSpace);
            writer.WriteAttributeString(attributeName, attributeValue);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Compare the two element.
        /// </summary>
        /// <param name="firstElement">First xml element doc.</param>
        /// <param name="secondElement">Second xml element doc.</param>
        /// <returns>Whether both xml element equal.</returns>
        public static bool XmlElementEqual(XmlElement firstElement, XmlElement secondElement)
        {
            if (firstElement == secondElement)
            {
                return true;
            }

            if (firstElement == null || secondElement == null)
            {
                return false;
            }

            return firstElement.OuterXml.Equals(secondElement.OuterXml);
        }
    }

    /// <summary>
    /// XmlFileElement which can get the line number.
    /// </summary>
    public class XmlFileElement : XmlElement, IXmlLineInfo
    {
        #region Fields
        private int lineNumber;
        private int linePosition;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="XmlFileElement"/> class.
        /// </summary>
        /// <param name="prefix">Prefix.</param>
        /// <param name="localName">Local name.</param>
        /// <param name="namespaceStr">Namespace URI.</param>
        /// <param name="xmlDoc">Xml document.</param>
        public XmlFileElement(string prefix, string localName,
            string namespaceStr, XmlDocument xmlDoc)
            : this(prefix, localName, namespaceStr, xmlDoc, 0, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlFileElement"/> class.
        /// </summary>
        /// <param name="prefix">Prefix.</param>
        /// <param name="localName">Local name.</param>
        /// <param name="namespaceStr">Namespace URI.</param>
        /// <param name="xmlDoc">Xml document.</param>
        /// <param name="lineNumber">Line number.</param>
        /// <param name="linePosition">Line position.</param>
        public XmlFileElement(string prefix, string localName,
            string namespaceStr, XmlDocument xmlDoc,
            int lineNumber, int linePosition)
            : base(prefix, localName, namespaceStr, xmlDoc)
        {
            this.lineNumber = lineNumber;
            this.linePosition = linePosition;
        }
        #endregion

        #region IXmlLineInfo Properties

        /// <summary>
        /// Gets Line number.
        /// </summary>
        public int LineNumber
        {
            get { return lineNumber; }
        }

        /// <summary>
        /// Gets Line position.
        /// </summary>
        public int LinePosition
        {
            get { return linePosition; }
        }

        #endregion

        #region IXmlLineInfo methods

        /// <summary>
        /// Whether has line infomation.
        /// </summary>
        /// <returns>Whether has line info.</returns>
        public bool HasLineInfo()
        {
            return true;
        }

        #endregion
    }

    /// <summary>
    /// XmlFileDocument which support line number.
    /// </summary>
    public class XmlFileDocument : XmlDocument
    {
        private IXmlLineInfo _lineInfo;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="XmlFileDocument"/> class.
        /// </summary>
        public XmlFileDocument()
            : base()
        {
        }
        #endregion

        #region Methods
        /// <summary>
        /// Create element.
        /// </summary>
        /// <param name="prefix">Prefix.</param>
        /// <param name="localName">Local name.</param>
        /// <param name="namespaceURI">Namespace URI.</param>
        /// <returns>Xml element.</returns>
        public override XmlElement CreateElement(string prefix,
            string localName, string namespaceURI)
        {
            return (_lineInfo != null)
                ? new XmlFileElement(prefix, localName, namespaceURI, this,
                                     _lineInfo.LineNumber, _lineInfo.LinePosition)
                : new XmlFileElement(prefix, localName, namespaceURI, this);
        }

        /// <summary>
        /// Load from the XmlReader.
        /// </summary>
        /// <param name="reader">Xml Reader.</param>
        public override void Load(XmlReader reader)
        {
            _lineInfo = reader as IXmlLineInfo;
            base.Load(reader);
            _lineInfo = null;
        }
        #endregion
    } 
}