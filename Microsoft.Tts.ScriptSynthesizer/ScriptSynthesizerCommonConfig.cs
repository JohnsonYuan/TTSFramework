//----------------------------------------------------------------------------
// <copyright file="ScriptSynthesizerCommonConfig.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script synthesizer common configuration
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.ScriptSynthesizer
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Interface of config snippet.
    /// </summary>
    public interface IConfigSnippet
    {
        /// <summary>
        /// Load configuration from xml snippet.
        /// </summary>
        /// <param name="nsmgr">Namespace manager.</param>
        /// <param name="configNode">Xml node containing the config.</param>
        void Load(XmlNamespaceManager nsmgr, XmlNode configNode);

        /// <summary>
        /// Save configuration file as a xml snippet.
        /// </summary>
        /// <param name="dom">Xml document to be saved into.</param>
        /// <param name="parent">Xml parent element to be saved into.</param>
        /// <param name="schema">Xml schema.</param>
        void Save(XmlDocument dom, XmlElement parent, XmlSchema schema);
    }

    /// <summary>
    /// Script synthesizer common Config.
    /// </summary>
    public class ScriptSynthesizerCommonConfig : IConfigSnippet
    {
        #region Fields

        /// <summary>
        /// Output wave path.
        /// </summary>
        private string _outputWaveDir;

        /// <summary>
        /// Output trace path.
        /// </summary>
        private string _traceLogFile;

        #endregion

        #region Public static properties

        /// <summary>
        /// Gets configuration schema.
        /// </summary>
        public static XmlSchemaInclude SchemaInclude
        {
            get
            {
                XmlSchemaInclude included = new XmlSchemaInclude();
                included.Schema =
                    XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.ScriptSynthesizer.xsd");

                return included;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets output wave path.
        /// </summary>
        public string OutputWaveDir
        {
            get
            {
                return _outputWaveDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _outputWaveDir = value;
            }
        }

        /// <summary>
        /// Gets or sets output trace path.
        /// </summary>
        public string TraceLogFile
        {
            get
            {
                return _traceLogFile;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _traceLogFile = value;
            }
        }

        #endregion

        #region Public static members

        /// <summary>
        /// Parse config snippet and create the config object.
        /// </summary>
        /// <param name="nsmgr">Namespace manager.</param>
        /// <param name="configNode">Xml node containing the config.</param>
        /// <returns>The config object.</returns>
        public static ScriptSynthesizerCommonConfig ParseConfig(XmlNamespaceManager nsmgr, XmlNode configNode)
        {
            ScriptSynthesizerCommonConfig commonConfig = null;
            if (nsmgr != null && configNode != null)
            {
                commonConfig = new ScriptSynthesizerCommonConfig();
                commonConfig.Load(nsmgr, configNode);
            }

            return commonConfig;
        }

        #endregion

        #region IConfigSnippet methods

        /// <summary>
        /// Load configuration from xml snippet.
        /// </summary>
        /// <param name="nsmgr">Namespace manager.</param>
        /// <param name="configNode">Xml node containing the config.</param>
        public void Load(XmlNamespaceManager nsmgr, XmlNode configNode)
        {
            if (nsmgr == null)
            {
                throw new ArgumentNullException("nsmgr");
            }

            if (configNode == null)
            {
                throw new ArgumentNullException("configNode");
            }

            if (configNode.Name != "scriptSynthesizerCommonConfig")
            {
                throw new InvalidDataException("scriptSynthesizerCommonConfig element expected!");
            }

            XmlNode node = configNode.SelectSingleNode(@"tts:outputWaveDir/@path", nsmgr);
            _outputWaveDir = node.InnerText;

            node = configNode.SelectSingleNode(@"tts:traceLogFile/@path", nsmgr);
            _traceLogFile = node.InnerText;
        }

        /// <summary>
        /// Save configuration file as a xml snippet.
        /// </summary>
        /// <param name="dom">Xml document to be saved into.</param>
        /// <param name="parent">Xml parent element to be saved into.</param>
        /// <param name="schema">Xml schema.</param>
        public void Save(XmlDocument dom, XmlElement parent, XmlSchema schema)
        {
            if (dom == null)
            {
                throw new ArgumentNullException("dom");
            }

            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            if (schema == null)
            {
                throw new ArgumentNullException("schema");
            }

            // root element of this snippet.
            XmlElement root = dom.CreateElement("scriptSynthesizerCommonConfig", schema.TargetNamespace);
            parent.AppendChild(root);

            // Output wave directory.
            XmlHelper.AppendElement(dom, root, "outputWaveDir", "path", _outputWaveDir, schema);

            // TraceLogFile path.
            XmlHelper.AppendElement(dom, root, "traceLogFile", "path", _traceLogFile, schema);
        }

        #endregion
    }

    /// <summary>
    /// Voice setup config.
    /// </summary>
    public class VoiceSetupConfig
    {
        #region Fields

        /// <summary>
        /// Offline language.
        /// </summary>
        private Language _language;

        /// <summary>
        /// Voice font path.
        /// </summary>
        private string _fontPath;

        /// <summary>
        /// Locale handler path.
        /// </summary>
        private string _localeHandlerDir;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets offline language.
        /// </summary>
        public Language Language
        {
            get
            {
                return _language;
            }

            set
            {
                _language = value;
            }
        }

        /// <summary>
        /// Gets or sets voice font path.
        /// </summary>
        public string FontPath
        {
            get
            {
                return _fontPath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _fontPath = value;
            }
        }

        /// <summary>
        /// Gets or sets locale handler path.
        /// </summary>
        public string LocaleHandlerDir
        {
            get
            {
                return _localeHandlerDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _localeHandlerDir = value;
            }
        }

        #endregion

        #region Public static members

        /// <summary>
        /// Parse config snippet and create the manager.
        /// </summary>
        /// <param name="nsmgr">Namespace manager.</param>
        /// <param name="configNode">Xml node containing the config.</param>
        /// <returns>The config object.</returns>
        public static VoiceSetupConfig ParseConfig(XmlNamespaceManager nsmgr, XmlNode configNode)
        {
            VoiceSetupConfig config = null;
            if (nsmgr != null && configNode != null)
            {
                config = new VoiceSetupConfig();
                config.ParseSettings(nsmgr, configNode);
            }

            return config;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Load configuration from xml snippet.
        /// </summary>
        /// <param name="nsmgr">Namespace manager.</param>
        /// <param name="configNode">Xml node containing the config.</param>
        public void ParseSettings(XmlNamespaceManager nsmgr, XmlNode configNode)
        {
            if (nsmgr == null)
            {
                throw new ArgumentNullException("nsmgr");
            }

            if (configNode == null)
            {
                throw new ArgumentNullException("configNode");
            }

            if (configNode.Name != "voiceFont")
            {
                throw new InvalidDataException("voiceFont element expected!");
            }

            XmlNode node = configNode.SelectSingleNode(@"tts:language/@name", nsmgr);
            _language = Localor.StringToLanguage(node.InnerText);

            node = configNode.SelectSingleNode(@"tts:fontPath/@path", nsmgr);
            _fontPath = node.InnerText;

            node = configNode.SelectSingleNode(@"tts:localeHandlerDir/@path", nsmgr);
            _localeHandlerDir = node.InnerText;
        }

        /// <summary>
        /// Save configuration file as a xml snippet.
        /// </summary>
        /// <param name="dom">Xml document to be saved into.</param>
        /// <param name="parent">Xml parent element to be saved into.</param>
        /// <param name="schema">Xml schema.</param>
        public void SaveSettings(XmlDocument dom, XmlElement parent, XmlSchema schema)
        {
            if (dom == null)
            {
                throw new ArgumentNullException("dom");
            }

            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            if (schema == null)
            {
                throw new ArgumentNullException("schema");
            }

            // root element of this snippet.
            XmlElement root = dom.CreateElement("voiceFont", schema.TargetNamespace);
            parent.AppendChild(root);

            XmlHelper.AppendElement(dom, root, "language", "name", Localor.LanguageToString(_language), schema);

            XmlHelper.AppendElement(dom, root, "fontPath", "path", _fontPath, schema);

            XmlHelper.AppendElement(dom, root, "localeHandlerDir", "path", _localeHandlerDir, schema);
        }

        #endregion
    }
}
