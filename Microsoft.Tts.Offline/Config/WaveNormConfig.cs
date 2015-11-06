//----------------------------------------------------------------------------
// <copyright file="WaveNormConfig.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements WaveNormConfig
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Config
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Load Configures form xml.
    /// </summary>
    public class WaveNormConfig
    {
        #region Fields

        private static XmlSchema _schema;

        private VoiceCreationLanguageData _languageData = new VoiceCreationLanguageData();
        private string _scriptPath;
        private string _fileListMap;
        private string _epochDir;
        private string _segmentDir;
        private string _waveDir;
        private Language _language;
        private string _waveNormDir;
        private string _midtermDir;
        private string _overflowAllowed;

        #endregion Private Fields

        #region Properties

        /// <summary>
        /// Gets Configuration schema.
        /// </summary>
        public static XmlSchema ConfigSchema
        {
            get
            {
                if (_schema == null)
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    _schema = XmlHelper.LoadSchemaFromResource(assembly, "Microsoft.Tts.Offline.Config.WaveNorm.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets Language data being used.
        /// </summary>
        public VoiceCreationLanguageData LanguageData
        {
            get { return _languageData; }
        }

        /// <summary>
        /// Gets or sets If the overflow is allowed in wave normalization.
        /// </summary>
        public string OverflowAllowed
        {
            get
            {
                return _overflowAllowed;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _overflowAllowed = value;
            }
        }

        /// <summary>
        /// Gets or sets Process file list file.
        /// </summary>
        public string FileListMap
        {
            get
            {
                return _fileListMap;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _fileListMap = value;
            }
        }

        /// <summary>
        /// Gets or sets Process file list file.
        /// </summary>
        public string ScriptPath
        {
            get
            {
                return _scriptPath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _scriptPath = value;
            }
        }

        /// <summary>
        /// Gets or sets Epoch data dir.
        /// </summary>
        public string EpochDir
        {
            get
            {
                return _epochDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _epochDir = value;
            }
        }

        /// <summary>
        /// Gets or sets Segment mark dir.
        /// </summary>
        public string SegmentDir
        {
            get
            {
                return _segmentDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _segmentDir = value;
            }
        }

        /// <summary>
        /// Gets or sets Original wave dir.
        /// </summary>
        public string WaveDir
        {
            get
            {
                return _waveDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _waveDir = value;
            }
        }

        /// <summary>
        /// Gets or sets Language type.
        /// </summary>
        public Language Language
        {
            get { return _language; }
            set { _language = value; }
        }

        /// <summary>
        /// Gets or sets Language type name for serialize.
        /// </summary>
        public string LanguageName
        {
            get
            {
                return Localor.LanguageToString(_language);
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _language = Localor.StringToLanguage(value);
            }
        }

        /// <summary>
        /// Gets Process log file, null -> log to console.
        /// </summary>
        public string LogFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_waveNormDir))
                {
                    throw new InvalidOperationException("_waveNormDir is null");
                }

                return Path.Combine(_midtermDir, "WaveNorm.log");
            }
        }

        /// <summary>
        /// Gets or sets Output dir, normalization final result dir.
        /// </summary>
        public string WaveNormDir
        {
            get
            {
                return _waveNormDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _waveNormDir = value;
            }
        }

        /// <summary>
        /// Gets or sets Save all midterm result dir.
        /// </summary>
        public string MidtermDir
        {
            get
            {
                return _midtermDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _midtermDir = value;
            }
        }

        #endregion Public Properties

        #region Serialization

        /// <summary>
        /// Save wavenorm config data into XML file.
        /// </summary>
        /// <param name="filePath">Target file path to save.</param>
        public void Save(string filePath)
        {
            XmlDocument dom = new XmlDocument();
            dom.NameTable.Add(ConfigSchema.TargetNamespace);

            // root element
            XmlElement rootEle = dom.CreateElement("waveNormConfig", ConfigSchema.TargetNamespace);
            XmlElement ele;

            ele = dom.CreateElement("scriptPath", ConfigSchema.TargetNamespace);
            ele.InnerText = ScriptPath;
            rootEle.AppendChild(ele);

            ele = dom.CreateElement("fileListMap", ConfigSchema.TargetNamespace);
            ele.InnerText = FileListMap;
            rootEle.AppendChild(ele);

            ele = dom.CreateElement("epochDir", ConfigSchema.TargetNamespace);
            ele.InnerText = EpochDir;
            rootEle.AppendChild(ele);

            ele = dom.CreateElement("segmentDir", ConfigSchema.TargetNamespace);
            ele.InnerText = SegmentDir;
            rootEle.AppendChild(ele);

            ele = dom.CreateElement("waveDir", ConfigSchema.TargetNamespace);
            ele.InnerText = WaveDir;
            rootEle.AppendChild(ele);

            ele = dom.CreateElement("language", ConfigSchema.TargetNamespace);
            ele.InnerText = LanguageName;
            rootEle.AppendChild(ele);

            // Optional language data path
            if (_languageData != null)
            {
                _languageData.Language = Localor.StringToLanguage(LanguageName);
                _languageData.SaveLanguageData(dom, ConfigSchema, rootEle);
            }

            ele = dom.CreateElement("waveNormDir", ConfigSchema.TargetNamespace);
            ele.InnerText = WaveNormDir;
            rootEle.AppendChild(ele);

            ele = dom.CreateElement("midtermDir", ConfigSchema.TargetNamespace);
            ele.InnerText = MidtermDir;
            rootEle.AppendChild(ele);

            ele = dom.CreateElement("overflowAllowed", ConfigSchema.TargetNamespace);
            ele.InnerText = OverflowAllowed.ToString();
            rootEle.AppendChild(ele);

            dom.AppendChild(rootEle);
            dom.Save(filePath);

            // performance compatability format checking
            XmlHelper.Validate(filePath, ConfigSchema);
        }

        /// <summary>
        /// Load wavenorm config from XML config file.
        /// </summary>
        /// <param name="filePath">Config filepath.</param>
        public void Load(string filePath)
        {
            // check the configuration file first
            try
            {
                XmlHelper.Validate(filePath, ConfigSchema);
            }
            catch (InvalidDataException ide)
            {
                string message = Helper.NeutralFormat("The configuration file [{0}] error is found. {1} {2}",
                    filePath, System.Environment.NewLine, ide.Message);
                throw new InvalidDataException(message, ide);
            }

            // load configuration
            XmlDocument dom = new XmlDocument();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("tts", ConfigSchema.TargetNamespace);
            dom.Load(filePath);

            // test whether the namespace of the configuration file is designed
            if (string.Compare(dom.DocumentElement.NamespaceURI,
                ConfigSchema.TargetNamespace, StringComparison.OrdinalIgnoreCase) != 0)
            {
                string message = Helper.NeutralFormat(
                    "The configuration xml file [{0}] must use the schema namespace [{1}]. " +
                    "Currently the config file uses namespace [{2}]",
                    filePath, ConfigSchema.TargetNamespace, dom.DocumentElement.NamespaceURI);
                throw new InvalidDataException(message);
            }

            XmlNode node;
            node = dom.DocumentElement.SelectSingleNode(@"tts:scriptPath", nsmgr);
            ScriptPath = node.InnerText;

            node = dom.DocumentElement.SelectSingleNode(@"tts:fileListMap", nsmgr);
            FileListMap = node.InnerText;

            node = dom.DocumentElement.SelectSingleNode(@"tts:epochDir", nsmgr);
            EpochDir = node.InnerText;

            node = dom.DocumentElement.SelectSingleNode(@"tts:segmentDir", nsmgr);
            SegmentDir = node.InnerText;

            node = dom.DocumentElement.SelectSingleNode(@"tts:waveDir", nsmgr);
            WaveDir = node.InnerText;

            node = dom.DocumentElement.SelectSingleNode(@"tts:language", nsmgr);
            Language = Localor.StringToLanguage(node.InnerText);

            node = dom.DocumentElement.SelectSingleNode(@"tts:waveNormDir", nsmgr);
            WaveNormDir = node.InnerText;

            _languageData.ParseLanguageData(dom, nsmgr, "tts", true);
            _languageData.SetLanguageData(Language);

            node = dom.DocumentElement.SelectSingleNode(@"tts:midtermDir", nsmgr);
            MidtermDir = node.InnerText;

            node = dom.DocumentElement.SelectSingleNode(@"tts:overflowAllowed", nsmgr);
            OverflowAllowed = node.InnerText;
        }

        #endregion 
    }
}