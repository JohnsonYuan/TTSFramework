//----------------------------------------------------------------------------
// <copyright file="FontsCombinerConfig.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements FontsCombinerConfig.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Config
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// FontsCombinerConfig.
    /// </summary>
    public class FontsCombinerConfig
    {
        #region Const fields

        private const string PhoneSetFileItem = "PhoneSetFile";
        private const string LexiconSchemaFileItem = "LexiconSchemaFile";
        private const string FontFileForF0Item = "FontFileForF0";
        private const string FontFileForLspItem = "FontFileForLsp";
        private const string FontFileForPhoneDurationItem = "FontFileForPhoneDuration";
        private const string FontFileForStateDurationItem = "FontFileForStateDuration";
        private const string FontFileForMbeItem = "FontFileForMbe";
        private const string TargetFontFileItem = "TargetFontFile";

        #endregion

        #region Fields

        private static XmlSchema _schema;
        private string _language;
        private string _phoneSetFile;
        private string _lexiconSchemaFile;
        private string _fontFileForF0;
        private string _fontFileForLsp;
        private string _fontFileForPhoneDuration;
        private string _fontFileForStateDuration;
        private string _fontFileForMbe;
        private List<string> _inputFontFiles = new List<string>();
        private string _targetFontFile;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the FontsCombinerConfig class.
        /// </summary>
        public FontsCombinerConfig()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets configuration schema.
        /// </summary>
        public static XmlSchema ConfigSchema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Config.FontsCombinerConfig.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets the phone set.
        /// </summary>
        public TtsPhoneSet PhoneSet { get; private set; }

        /// <summary>
        /// Gets the lexicon schema.
        /// </summary>
        public TtsPosSet PosSet { get; private set; }

        /// <summary>
        /// Gets the language for fonts combiner.
        /// For example, the value can be "en-US", "zh-CN" etc.
        /// </summary>
        public Language FontLanguage { get; private set; }

        /// <summary>
        /// Gets or sets language.
        /// </summary>
        public string Language
        {
            get
            {
                return _language;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("Language is null.");
                }

                _language = value;
            }
        }

        /// <summary>
        /// Gets or sets phoneset file.
        /// </summary>
        public string PhoneSetFile
        {
            get
            {
                return _phoneSetFile;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("Phone Set File path is null.");
                }

                _phoneSetFile = value;
            }
        }

        /// <summary>
        /// Gets or sets lexicon schema file.
        /// </summary>
        public string LexiconSchemaFile
        {
            get
            {
                return _lexiconSchemaFile;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("Lexicon Schema File path is null.");
                }

                _lexiconSchemaFile = value;
            }
        }

        /// <summary>
        /// Gets or sets font file for f0.
        /// </summary>
        public string FontFileForF0
        {
            get
            {
                return _fontFileForF0;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("Font File For F0 path is null.");
                }

                _fontFileForF0 = value;
            }
        }

        /// <summary>
        /// Gets or sets font file for lsp.
        /// </summary>
        public string FontFileForLsp
        {
            get
            {
                return _fontFileForLsp;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("Font File for Lsp path is null.");
                }

                _fontFileForLsp = value;
            }
        }

        /// <summary>
        /// Gets or sets font file for state duration.
        /// </summary>
        public string FontFileForStateDuration
        {
            get
            {
                return _fontFileForStateDuration;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("Font File for State Duration path is null.");
                }

                _fontFileForStateDuration = value;
            }
        }

        /// <summary>
        /// Gets or sets font file for phone duration, optional.
        /// </summary>
        public string FontFileForPhoneDuration
        {
            get { return _fontFileForPhoneDuration; }
            set { _fontFileForPhoneDuration = value; }
        }

        /// <summary>
        /// Gets or sets font file for mbe, optional.
        /// </summary>
        public string FontFileForMbe
        {
            get { return _fontFileForMbe; }
            set { _fontFileForMbe = value; }
        }

        /// <summary>
        /// Gets all the input voice font files.
        /// </summary>
        public List<string> InputFontFiles
        {
            get { return _inputFontFiles; }
        }

        /// <summary>
        /// Gets or sets target font file.
        /// </summary>
        public string TargetFontFile
        {
            get
            {
                return _targetFontFile;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("Target Font File path is null");
                }

                _targetFontFile = value;
            }
        }

        #endregion

        #region Public instance methods

        /// <summary>
        /// Save FontsCombiner's config data into XML file.
        /// </summary>
        /// <param name="filePath">Target file to save.</param>
        public void Save(string filePath)
        {
            XmlDocument dom = new XmlDocument();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("tts", ConfigSchema.TargetNamespace);
            dom.NameTable.Add(ConfigSchema.TargetNamespace);

            XmlDeclaration declaration = dom.CreateXmlDeclaration("1.0", "utf-8", null);
            dom.AppendChild(declaration);

            // root element.
            XmlElement ele = dom.CreateElement("FontsCombiner", ConfigSchema.TargetNamespace);

            // Language.
            XmlElement languageEle = dom.CreateElement("Language", ConfigSchema.TargetNamespace);
            Helper.ThrowIfNull(Language);
            languageEle.SetAttribute("name", Language);
            ele.AppendChild(languageEle);

            // Phone Set File path.
            AddFilePathItem(dom, ConfigSchema, ele, PhoneSetFileItem, PhoneSetFile);

            // Lexicon Schema File path.
            AddFilePathItem(dom, ConfigSchema, ele, LexiconSchemaFileItem, LexiconSchemaFile);

            // The path of Font File (for F0).
            AddFilePathItem(dom, ConfigSchema, ele, FontFileForF0Item, FontFileForF0);
            
            // The path of Font File (for Lsp).
            AddFilePathItem(dom, ConfigSchema, ele, FontFileForLspItem, FontFileForLsp);

            // The path of Font File (for StateDuration).
            AddFilePathItem(dom, ConfigSchema, ele, FontFileForStateDurationItem, FontFileForStateDuration);

            // The path of Font File (for PhoneDuration).
            if (!string.IsNullOrEmpty(FontFileForPhoneDuration))
            {
                AddFilePathItem(dom, ConfigSchema, ele, FontFileForPhoneDurationItem, FontFileForPhoneDuration);
            }

            // The path of Font File (for Mbe).
            if (!string.IsNullOrEmpty(FontFileForMbe))
            {
                AddFilePathItem(dom, ConfigSchema, ele, FontFileForMbeItem, FontFileForMbe);
            }

            // The target font file path.
            AddFilePathItem(dom, ConfigSchema, ele, TargetFontFileItem, TargetFontFile);

            dom.AppendChild(ele);
            dom.Save(filePath);

            // performance compatibility format checking
            XmlHelper.Validate(filePath, ConfigSchema);
        }

        /// <summary>
        /// Load configuration from file.
        /// </summary>
        /// <param name="filePath">XML configuration file path.</param>
        public void Load(string filePath)
        {
            // Check the configuration file first.
            try
            {
                XmlHelper.Validate(filePath, ConfigSchema);
            }
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The configuration file \"{0}\" error is found.",
                    filePath);
                throw new InvalidDataException(message, ide);
            }

            // Load configuration.
            XmlDocument dom = new XmlDocument();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("tts", ConfigSchema.TargetNamespace);
            dom.Load(filePath);

            // Test whether the namespace of the configuration file is designed.
            if (string.Compare(dom.DocumentElement.NamespaceURI,
                ConfigSchema.TargetNamespace, StringComparison.OrdinalIgnoreCase) != 0)
            {
                string str1 = "The configuration xml file \"{0}\" must use the schema namespace [{1}]. ";
                string str2 = "Currently the config file uses namespace [{2}]";
                string message = string.Format(CultureInfo.InvariantCulture,
                    str1 + str2,
                    filePath, ConfigSchema.TargetNamespace, dom.DocumentElement.NamespaceURI);
                throw new InvalidDataException(message);
            }

            // Language.
            ParseLanguage(dom, nsmgr);

            // PhoneSet.
            ParsePhoneSet(dom, nsmgr);

            // Lexicon Schema.
            ParseLexiconSchema(dom, nsmgr);

            // Font File path for F0 data.
            FontFileForF0 = ParseFilePath(dom, nsmgr, FontFileForF0Item);
            InputFontFiles.Add(FontFileForF0);

            // Font File path for Lsp data.
            FontFileForLsp = ParseFilePath(dom, nsmgr, FontFileForLspItem);
            if (!InputFontFiles.Contains(FontFileForLsp))
            {
                InputFontFiles.Add(FontFileForLsp);
            }

            // Font File path for State Duration data.
            FontFileForStateDuration = ParseFilePath(dom, nsmgr, FontFileForStateDurationItem);
            if (!InputFontFiles.Contains(FontFileForStateDuration))
            {
                InputFontFiles.Add(FontFileForStateDuration);
            }

            // Font File path for Phone Duration data.
            FontFileForPhoneDuration = ParsePhoneDurationMbePath(dom, nsmgr, FontFileForPhoneDurationItem);
            if (!string.IsNullOrEmpty(FontFileForPhoneDuration) && !InputFontFiles.Contains(FontFileForPhoneDuration))
            {
                InputFontFiles.Add(FontFileForPhoneDuration);
            }

            // Font File path for Mbe data.
            FontFileForMbe = ParsePhoneDurationMbePath(dom, nsmgr, FontFileForMbeItem);
            if (!string.IsNullOrEmpty(FontFileForMbe) && !InputFontFiles.Contains(FontFileForMbe))
            {
                InputFontFiles.Add(FontFileForMbe);
            }

            // Target Font File path.
            ParseTargetFontFile(dom, nsmgr);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Add File Path Item to config file.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="schema">Schema.</param>
        /// <param name="ele">Root element.</param>
        /// <param name="elementName">The element need to add.</param>
        /// <param name="filePath">The file path need to add.</param>
        private static void AddFilePathItem(XmlDocument dom, XmlSchema schema, XmlElement ele, string elementName, string filePath)
        {
            if (!Helper.IsValidPath(filePath))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "the path \"[{0}]\" should be a valid path", filePath);
                throw new InvalidDataException(message);
            }

            XmlElement fileEle = dom.CreateElement(elementName, schema.TargetNamespace);
            fileEle.SetAttribute("path", filePath);
            ele.AppendChild(fileEle);
        }

        /// <summary>
        /// Parse XML document for Target Font File.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="nsmgr">Namespace.</param>
        private void ParseTargetFontFile(XmlDocument dom, XmlNamespaceManager nsmgr)
        {
            XmlNode node = dom.DocumentElement.SelectSingleNode(
                string.Format(CultureInfo.InvariantCulture, @"tts:{0}/@path", TargetFontFileItem), nsmgr);

            if (node == null)
            {
                throw new ArgumentNullException(string.Format(CultureInfo.InvariantCulture,
                    "The argument {0} should be in the config file, but can't find it", TargetFontFileItem));
            }

            TargetFontFile = node.InnerText;
            if (!Helper.IsValidPath(TargetFontFile))
            {
                throw new ArgumentNullException(string.Format(CultureInfo.InvariantCulture,
                    "Target Font File path \"[{0}]\" is invalid", TargetFontFile));
            }
        }

        /// <summary>
        /// Parse XML document for language.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="nsmgr">Namespace.</param>
        private void ParseLanguage(XmlDocument dom, XmlNamespaceManager nsmgr)
        {
            XmlNode node = dom.DocumentElement.SelectSingleNode(@"tts:Language/@name", nsmgr);

            if (node == null)
            {
                throw new ArgumentNullException(
                    "The language item hould be in the config file, but can't find it");
            }

            Helper.ThrowIfNull(node.InnerText);
            FontLanguage = Localor.StringToLanguage(node.InnerText);
        }

        /// <summary>
        /// Parse XML document for Phone Set File path.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="nsmgr">Namespace.</param>
        private void ParsePhoneSet(XmlDocument dom, XmlNamespaceManager nsmgr)
        {
            PhoneSetFile = ParseFilePath(dom, nsmgr, PhoneSetFileItem);

            if (!Helper.FileValidExists(PhoneSetFile))
            {
                throw new FileNotFoundException(
                    string.Format(CultureInfo.InvariantCulture,
                    "Phone set file \"{0}\" not found.", PhoneSetFile));
            }

            PhoneSet = new TtsPhoneSet();
            PhoneSet.Load(PhoneSetFile);
        }

        /// <summary>
        /// Parse XML document for Lexicon Schema File path.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="nsmgr">Namespace.</param>
        private void ParseLexiconSchema(XmlDocument dom, XmlNamespaceManager nsmgr)
        {
            LexiconSchemaFile = ParseFilePath(dom, nsmgr, LexiconSchemaFileItem);

            if (!Helper.FileValidExists(LexiconSchemaFile))
            {
                throw new FileNotFoundException(
                    string.Format(CultureInfo.InvariantCulture,
                    "Lexicon schema file \"{0}\" not found", LexiconSchemaFile));
            }

            LexicalAttributeSchema attributeSchema = new LexicalAttributeSchema(FontLanguage);
            attributeSchema.Load(LexiconSchemaFile);
            attributeSchema.Validate();
            if (attributeSchema.ErrorSet.Contains(ErrorSeverity.MustFix))
            {
                attributeSchema.ErrorSet.Export(Console.Error);
                throw new InvalidDataException(
                    string.Format(CultureInfo.InvariantCulture,
                    "Please fix the error of lexicon schema file \"{0}\"", LexiconSchemaFile));
            }

            PosSet = TtsPosSet.LoadFromSchema(LexiconSchemaFile);
        }

        /// <summary>
        /// Parse XML document for PhoneDuration and Mbe file path.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="nsmgr">Namespace.</param>
        /// <param name="elementName">The element name need to parse.</param>
        /// <returns>The file name.</returns>
        private string ParsePhoneDurationMbePath(XmlDocument dom, XmlNamespaceManager nsmgr, string elementName)
        {
            XmlNode node = dom.DocumentElement.SelectSingleNode(
                string.Format(CultureInfo.InvariantCulture, @"tts:{0}/@path", elementName), nsmgr);

            string filePath = string.Empty;

            if (node != null)
            {
                filePath = node.InnerText;
                Helper.ThrowIfFileNotExist(filePath);
            }

            return filePath;
        }

        /// <summary>
        /// Parse XML document for file path.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="nsmgr">Namespace.</param>
        /// <param name="elementName">The element name need to parse.</param>
        /// <returns>The file name.</returns>
        private string ParseFilePath(XmlDocument dom, XmlNamespaceManager nsmgr, string elementName)
        {
            XmlNode node = dom.DocumentElement.SelectSingleNode(
                string.Format(CultureInfo.InvariantCulture, @"tts:{0}/@path", elementName), nsmgr);

            if (node == null)
            {
                throw new ArgumentNullException(string.Format(CultureInfo.InvariantCulture,
                    "The argument {0} should be in the config file, but can't find it", elementName));
            }

            Helper.ThrowIfFileNotExist(node.InnerText);
            return node.InnerText;
        }

        #endregion
    }
}