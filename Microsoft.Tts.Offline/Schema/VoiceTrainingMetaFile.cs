//----------------------------------------------------------------------------
// <copyright file="VoiceTrainingMetaFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines the EnvironmentMeta class to operate the enviroment
// meta file.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Schema
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.FlowEngine;
    using Microsoft.Tts.Offline.IO;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Zip item type.
    /// </summary>
    public enum ZipItemType
    {
        /// <summary>
        /// None type.
        /// </summary>
        None,

        /// <summary>
        /// The zip item value is a file location.
        /// </summary>
        File,

        /// <summary>
        /// The zip item value is a directory location.
        /// </summary>
        Directory,

        /// <summary>
        /// The zip item value is a directory with file list folder structure.
        /// </summary>
        FileListDirectory,

        /// <summary>
        /// The zip item value is a file/directory location.
        /// </summary>
        Path,

        /// <summary>
        /// The zip item value is a directory location, don't zip files under the directory,
        /// Only save md5 of the file in that directory.
        /// </summary>
        DirectoryWithMD5,

        /// <summary>
        /// The zip item value is a directory location, don't zip files under the directory,
        /// Only save md5 of the file in that directory.
        /// The directory with file list folder structure.
        /// </summary>
        FileListDirectoryWithMD5,
    }

    /// <summary>
    /// Training meta file.
    /// </summary>
    public class VoiceTrainingMetaFile
    {
        #region Fields

        /// <summary>
        /// Training tool version.
        /// </summary>
        public const string ToolVersionKeyName = "ToolVersion";

        /// <summary>
        /// Training machine name.
        /// </summary>
        public const string TrainingMachineKeyName = "TrainingMachineName";

        /// <summary>
        /// Language input name.
        /// </summary>
        public const string LanguageInputName = "Language";

        /// <summary>
        /// Default training metafile name.
        /// </summary>
        public const string VoiceTrainingMetaFileName = "metafile.xml";

        /// <summary>
        /// Default training configuration file name.
        /// </summary>
        public const string VoiceModelTrainerConfigFileName = "VoiceModelTrainer.config";

        /// <summary>
        /// Training meta file schema.
        /// </summary>
        private static XmlSchema _schema;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceTrainingMetaFile"/> class.
        /// </summary>
        public VoiceTrainingMetaFile()
        {
            ZipItems = new Dictionary<string, TrainModelZipItem>();
            Parameters = new Dictionary<string, TrainMetaParameter>();
        }

        #region Properties

        /// <summary>
        /// Gets voice training meta file URI.
        /// </summary>
        public static string VoiceTrainingMetaFileUri
        {
            get
            {
                return ZipFile.UriPathDelimeter + VoiceTrainingMetaFileName;
            }
        }

        /// <summary>
        /// Gets voice model trainer configuration file URI.
        /// </summary>
        public static string VoiceModelTrainerConfigFileUri
        {
            get
            {
                return ZipFile.UriPathDelimeter + VoiceModelTrainerConfigFileName;
            }
        }

        /// <summary>
        /// Gets configuration schema.
        /// </summary>
        public static XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = new XmlSchema();
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    _schema = XmlHelper.LoadSchemaFromResource(assembly,
                        "Microsoft.Tts.Offline.Schema.VoiceTrainingMetaFile.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets sonfiguration schema.
        /// </summary>
        public static XmlSchemaInclude SchemaInclude
        {
            get
            {
                return new XmlSchemaInclude()
                {
                    Schema = Schema
                };
            }
        }

        /// <summary>
        /// Gets parameters directory.
        /// </summary>
        public Dictionary<string, TrainMetaParameter> Parameters { get; private set; }

        /// <summary>
        /// Gets zip items.
        /// </summary>
        public Dictionary<string, TrainModelZipItem> ZipItems { get; private set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Get parameter from the training meta file.
        /// </summary>
        /// <param name="keyName">The key name.</param>
        /// <returns>The parameter.</returns>
        public TrainMetaParameter GetParameter(string keyName)
        {
            Debug.Assert(Parameters != null);
            return Parameters.ContainsKey(keyName) ? Parameters[keyName] : null;
        }

        /// <summary>
        /// Reset the training meta file.
        /// </summary>
        public void Reset()
        {
            Parameters.Clear();
            ZipItems.Clear();
        }

        /// <summary>
        /// Save to file.
        /// </summary>
        /// <param name="filePath">File path to be saved to.</param>
        public void Save(string filePath)
        {
            Helper.EnsureFolderExistForFile(filePath);

            string xmlNamespace = Schema.TargetNamespace;
            XmlDocument dom = new XmlDocument();
            dom.NameTable.Add(xmlNamespace);
            XmlDeclaration declaration = dom.CreateXmlDeclaration("1.0", "utf-16", null);
            dom.AppendChild(declaration);

            XmlElement rootNode = dom.CreateElement("voiceTrainingMetaFile", xmlNamespace);
            dom.AppendChild(rootNode);

            if (Parameters.Count > 0)
            {
                XmlElement parasNode = dom.CreateElement("parameters", xmlNamespace);
                rootNode.AppendChild(parasNode);

                foreach (string paraKey in Parameters.Keys)
                {
                    XmlElement paraNode = dom.CreateElement("parameter", xmlNamespace);
                    paraNode.SetAttribute("name", paraKey);
                    paraNode.SetAttribute("value", Parameters[paraKey].Value);
                    paraNode.SetAttribute("reused", Parameters[paraKey].IsReused.ToString().ToLowerInvariant());
                    parasNode.AppendChild(paraNode);
                }
            }

            XmlElement zipItemsNode = dom.CreateElement("zipItems", xmlNamespace);
            rootNode.AppendChild(zipItemsNode);
            foreach (TrainModelZipItem zipItem in ZipItems.Values)
            {
                zipItemsNode.AppendChild(zipItem.CreateXmlNode(dom, xmlNamespace));
            }

            dom.Save(filePath);
        }

        /// <summary>
        /// Load from file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        public void Load(string filePath)
        {
            Helper.ThrowIfNull(filePath);

            XmlDocument dom = new XmlDocument();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("tts", Schema.TargetNamespace);

            // Validate
            if (!File.Exists(filePath))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The voice training meta file \"{0}\" not found",
                    filePath);
                throw new InvalidDataException(message);
            }

            try
            {
                XmlHelper.Validate(filePath, Schema);
            }
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Error found in the configuration file \"{0}\"",
                    filePath);
                throw new InvalidDataException(message, ide);
            }

            // Loads configuration.
            dom.Load(filePath);
            Reset();
            ParseVoiceTrainingMetaFile(nsmgr, dom.DocumentElement);
        }

        /// <summary>
        /// Parse from xml inside CDATA section.
        /// </summary>
        /// <param name="xmlContent">Inputed Xml content string.</param>
        public void LoadXml(string xmlContent)
        {
            Reset();

            // Validates the Xml string.
            XmlSchema schema = ConfigurationInput.ValidateCdataSection(xmlContent,
                VoiceTrainingMetaFile.SchemaInclude, "voiceTrainingMetaFileType");

            // Loads the Xml string.
            XmlDocument doc = new XmlDocument();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("tts", schema.TargetNamespace);
            doc.LoadXml(xmlContent);
            ParseVoiceTrainingMetaFile(nsmgr, doc.DocumentElement);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Parse training meta file.
        /// </summary>
        /// <param name="nsmgr">Namespace manager.</param>
        /// <param name="configNode">Xml node containing the config.</param>
        private void ParseVoiceTrainingMetaFile(XmlNamespaceManager nsmgr, XmlNode configNode)
        {
            // Load parameters element
            XmlNodeList parameterNodeList = configNode.SelectNodes(
                @"tts:parameters/tts:parameter", nsmgr);
            foreach (XmlNode node in parameterNodeList)
            {
                TrainMetaParameter parameter = new TrainMetaParameter();
                parameter.Parse(nsmgr, node);
                if (Parameters.ContainsKey(parameter.Name))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Duplicate paramter name {0}.", parameter.Name));
                }

                Parameters.Add(parameter.Name, parameter);
            }

            // Load zipItems element
            XmlNode zipItemsNode = configNode.SelectSingleNode(
                @"tts:zipItems", nsmgr);
            ParseZipItemsNode(nsmgr, zipItemsNode);
        }

        /// <summary>
        /// Load zip items from xml snippet.
        /// </summary>
        /// <param name="nsmgr">Namespace manager.</param>
        /// <param name="configNode">Xml node containing the config.</param>
        private void ParseZipItemsNode(XmlNamespaceManager nsmgr, XmlNode configNode)
        {
            // Argument checking
            if (nsmgr == null)
            {
                throw new ArgumentNullException("nsmgr");
            }

            if (configNode == null)
            {
                throw new ArgumentNullException("configNode");
            }

            if (configNode.Name != "zipItems")
            {
                throw new InvalidDataException("zipItems element expected!");
            }

            #endregion

            XmlNodeList parameterNodeList = configNode.SelectNodes(
                @"tts:input", nsmgr);

            foreach (XmlNode inputNode in parameterNodeList)
            {
                TrainModelZipItem zipItem = new TrainModelZipItem();
                zipItem.Parse(inputNode, nsmgr);
                ZipItems[zipItem.Reference.ToString()] = zipItem;
            }
        }
    }

    /// <summary>
    /// Training meta file parameter.
    /// </summary>
    public class TrainMetaParameter
    {
        /// <summary>
        /// Gets or sets name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets value of the parameter.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter is reused.
        /// </summary>
        public bool IsReused { get; set; }

        /// <summary>
        /// Load zip items from xml snippet.
        /// </summary>
        /// <param name="nsmgr">Namespace manager.</param>
        /// <param name="node">Xml node containing the config.</param>
        public void Parse(XmlNamespaceManager nsmgr, XmlNode node)
        {
            // Argument checking
            if (nsmgr == null)
            {
                throw new ArgumentNullException("nsmgr");
            }

            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (node.Name != "parameter")
            {
                throw new InvalidDataException("parameter element expected!");
            }

            Name = node.Attributes["name"].Value;
            if (node.Attributes["value"] != null)
            {
                Value = node.Attributes["value"].Value;
            }

            if (node.Attributes["reused"] != null)
            {
                IsReused = bool.Parse(node.Attributes["reused"].Value);
            }
        }
    }

    /// <summary>
    /// Train model ZIP item.
    /// </summary>
    public class TrainModelZipItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrainModelZipItem"/> class.
        /// </summary>
        public TrainModelZipItem()
        {
            Type = ZipItemType.None;
            Reference = new ConfigurationReference();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrainModelZipItem"/> class.
        /// </summary>
        /// <param name="moduleName">Module name.</param>
        /// <param name="parameterName">Parameter name.</param>
        public TrainModelZipItem(string moduleName, string parameterName)
        {
            Type = ZipItemType.None;
            Reference = new ConfigurationReference(moduleName, parameterName);
        }

        /// <summary>
        /// Gets reference of the zip item parameter name.
        /// </summary>
        public ConfigurationReference Reference { get; private set; }

        /// <summary>
        /// Gets or sets zip type.
        /// </summary>
        public ZipItemType Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the zip item is reused.
        /// </summary>
        public bool IsReused { get; set; }

        /// <summary>
        /// Gets or sets file extension of the file.
        /// </summary>
        public string FileExtension { get; set; }

        /// <summary>
        /// Gets or sets source of the parameter.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets relative local path with out file extension.
        /// </summary>
        public string RelativeLocalPathWithoutFileExtension
        {
            get
            {
                Helper.ThrowIfNull(Reference);
                string relativePath = Helper.NeutralFormat(@"{0}\{1}",
                    Reference.Module, Reference.Name);
                return relativePath;
            }
        }

        /// <summary>
        /// Gets relative local path.
        /// </summary>
        public string RelativeLocalPath
        {
            get
            {
                Helper.ThrowIfNull(Reference);
                string relativePath = RelativeLocalPathWithoutFileExtension;
                if ((Type == ZipItemType.File || Type == ZipItemType.DirectoryWithMD5 || Type == ZipItemType.FileListDirectoryWithMD5) &&
                    !string.IsNullOrEmpty(FileExtension))
                {
                    relativePath = relativePath.AppendExtensionName(FileExtension);
                }

                return relativePath;
            }
        }

        /// <summary>
        /// Gets relative zip uri path with out file extension.
        /// </summary>
        public string RelativeZipUriWithoutFileExtension
        {
            get
            {
                Helper.ThrowIfNull(Reference);
                string relativeUri = Helper.NeutralFormat("/{0}/{1}",
                    Reference.Module, Reference.Name);
                return relativeUri;
            }
        }

        /// <summary>
        /// Gets relative zip uri.
        /// </summary>
        public string RelativeZipUri
        {
            get
            {
                Helper.ThrowIfNull(Reference);
                string relativeUri = RelativeZipUriWithoutFileExtension;
                if ((Type == ZipItemType.File || Type == ZipItemType.DirectoryWithMD5 || Type == ZipItemType.FileListDirectoryWithMD5) &&
                    !string.IsNullOrEmpty(FileExtension))
                {
                    relativeUri = relativeUri.AppendExtensionName(FileExtension);
                }

                return relativeUri;
            }
        }

        /// <summary>
        /// Parse ZipItem node.
        /// </summary>
        /// <param name="node">Xml document.</param>
        /// <param name="nsmgr">Namespace manager.</param>
        public void Parse(XmlNode node, XmlNamespaceManager nsmgr)
        {
            Debug.Assert(node != null, "Dom should not be null");
            Debug.Assert(nsmgr != null, "Namespace manager should not be null");
            string fullName = node.Attributes["name"].Value;
            Debug.Assert(ConfigurationReference.IsReference(fullName), "Name should be referance in the zip items.");
            Reference = new ConfigurationReference();
            Reference.Parse(fullName);
            Type = (ZipItemType)Enum.Parse(typeof(ZipItemType), node.Attributes["type"].Value, true);

            IsReused = bool.Parse(node.Attributes["reused"].Value);
            Debug.Assert(!(Type == ZipItemType.DirectoryWithMD5 && !IsReused) &&
                !(Type == ZipItemType.FileListDirectoryWithMD5 && !IsReused));

            if (node.Attributes["fileExtension"] != null)
            {
                FileExtension = node.Attributes["fileExtension"].Value;
            }

            Debug.Assert(
                (Type == ZipItemType.Directory && string.IsNullOrEmpty(FileExtension)) ||
                (Type == ZipItemType.FileListDirectory && string.IsNullOrEmpty(FileExtension)) ||
                (Type == ZipItemType.DirectoryWithMD5 && !string.IsNullOrEmpty(FileExtension)) ||
                (Type == ZipItemType.FileListDirectoryWithMD5 && !string.IsNullOrEmpty(FileExtension)) ||
                (Type == ZipItemType.File && !string.IsNullOrEmpty(FileExtension)) ||
                (Type == ZipItemType.Path && !string.IsNullOrEmpty(FileExtension)));

            if (node.Attributes["source"] != null)
            {
                Source = node.Attributes["source"].Value;
            }
        }

        /// <summary>
        /// Create Zip item node.
        /// </summary>
        /// <param name="dom">Xml document.</param>
        /// <param name="xmlNamespace">Xml namespace.</param>
        /// <returns>Created XML node.</returns>
        public XmlElement CreateXmlNode(XmlDocument dom, string xmlNamespace)
        {
            Debug.Assert(dom != null, "Dom should not be null");
            Debug.Assert(xmlNamespace != null, "Namespace should not be null");

            XmlElement element = dom.CreateElement("input", xmlNamespace);
            Debug.Assert(!string.IsNullOrEmpty(Reference.ToString()));
            element.SetAttribute("name", Reference.ToString());
            Debug.Assert(Type != ZipItemType.None);
            element.SetAttribute("type", Type.ToString());
            element.SetAttribute("reused", IsReused.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
            if (!string.IsNullOrEmpty(FileExtension))
            {
                element.SetAttribute("fileExtension", FileExtension.ToString(CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrEmpty(Source))
            {
                element.SetAttribute("source", Source.ToString(CultureInfo.InvariantCulture));
            }

            return element;
        }
    }
}