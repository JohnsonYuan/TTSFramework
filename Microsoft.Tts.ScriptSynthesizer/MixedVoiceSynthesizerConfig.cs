//----------------------------------------------------------------------------
// <copyright file="MixedVoiceSynthesizerConfig.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements mixed voice synthesizer config
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.ScriptSynthesizer
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Mixed voice synthesizer config.
    /// </summary>
    public class MixedVoiceSynthesizerConfig : IConfigSnippet
    {
        #region Fields

        /// <summary>
        /// Donate duration flag.
        /// </summary>
        private bool _donateDuration;

        /// <summary>
        /// Donate f0 flag.
        /// </summary>
        private bool _donateF0;

        /// <summary>
        /// Path of log file.
        /// </summary>
        private string _logFile;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether donate duration flag.
        /// </summary>
        public bool DonateDuration
        {
            get { return _donateDuration; }
            set { _donateDuration = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether donate f0 flag.
        /// </summary>
        public bool DonateF0
        {
            get { return _donateF0; }
            set { _donateF0 = value; }
        }

        /// <summary>
        /// Gets or sets path of log file.
        /// </summary>
        public string LogFile
        {
            get { return _logFile; }
            set { _logFile = value; }
        }

        #endregion

        #region Public static members

        /// <summary>
        /// Parse config snippet and create the config object.
        /// </summary>
        /// <param name="nsmgr">Namespace manager.</param>
        /// <param name="configNode">Xml node containing the config.</param>
        /// <returns>The config object.</returns>
        public static MixedVoiceSynthesizerConfig ParseConfig(XmlNamespaceManager nsmgr, XmlNode configNode)
        {
            MixedVoiceSynthesizerConfig config = null;
            if (nsmgr != null && configNode != null)
            {
                config = new MixedVoiceSynthesizerConfig();
                config.Load(nsmgr, configNode);
            }

            return config;
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

            if (configNode.Name != "mixedVoiceSynthesizerConfig")
            {
                throw new InvalidDataException("mixedVoiceSynthesizerConfig element expected!");
            }

            XmlNode node = configNode.SelectSingleNode(@"tts:donateDuration/@enable", nsmgr);
            _donateDuration = bool.Parse(node.InnerText);

            node = configNode.SelectSingleNode(@"tts:donateF0/@enable", nsmgr);
            _donateF0 = bool.Parse(node.InnerText);

            node = configNode.SelectSingleNode(@"tts:logFile/@path", nsmgr);
            _logFile = node.InnerText;
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

            // root element of this snippet
            XmlElement root = dom.CreateElement("mixedVoiceSynthesizerConfig", schema.TargetNamespace);
            parent.AppendChild(root);

            XmlHelper.AppendElement(dom, root, "donateDuration", "enable",
                _donateDuration.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture),
                schema);

            XmlHelper.AppendElement(dom, root, "donateF0", "enable",
                _donateF0.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture),
                schema);

            XmlHelper.AppendElement(dom, root, "logFile", "path", _logFile, schema);
        }

        #endregion
    }
}
