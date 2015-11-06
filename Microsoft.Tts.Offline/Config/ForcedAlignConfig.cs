//----------------------------------------------------------------------------
// <copyright file="ForcedAlignConfig.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements forced alignment configuration
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Config
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Class presents forced alignment configuration for end user.
    /// </summary>
    public class ForcedAlignConfig
    {
        #region Fields

        private static XmlSchema _schema;

        private int _traceLevel = 0;
        private int _pruneLevel = 0;
        private int _minUttNumber = 500;
        private VoiceCreationLanguageData _languageData = new VoiceCreationLanguageData();
        private string _worksiteDir;
        private Speaker _speaker = new Speaker();
        private string _fileList;
        private string _scriptDir;
        private string _wave16kDir;

        private string _modelDir = string.Empty;
        private bool _rebuildModel;
        private bool _adaptModel = true;
        private bool _hybridSDModel = false;
        private bool _ignoreTone = true;
        private bool _keepSRPhones = false;
        private long _silenceDurationThresh = 400001;  // default: 40 ms

        private string _outputDir;

        #endregion

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
                    _schema = XmlHelper.LoadSchemaFromResource(assembly, "Microsoft.Tts.Offline.Config.ForcedAlign.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets or sets Value to indicate trace level of the HTK tools.
        /// <param />
        /// It is 0 by default.
        /// </summary>
        public int TraceLevel
        {
            get { return _traceLevel; }
            set { _traceLevel = value; }
        }

        /// <summary>
        /// Gets or sets Value to indicate -t level of the HVite.exe
        /// <param />
        /// It is 0 by default.
        /// <param />
        /// This property will define "-t" parameter for triphone HVite:
        /// Level -1 and 0: 400; level 1: 800; level 2: 1200
        /// In SD approach, this affects monophone HVite, too:
        /// Level -1: 1000; level 0: 2000; level 1 and 2: no -t.
        /// </summary>
        public int PruneLevel
        {
            get { return _pruneLevel; }
            set { _pruneLevel = value; }
        }

        /// <summary>
        /// Gets or sets The minimal utterances number required for SD model.
        /// </summary>
        public int MinUttNumber
        {
            get { return _minUttNumber; }
            set { _minUttNumber = value; }
        }

        /// <summary>
        /// Gets or sets The forced alignment tool depends this path to reach some extern 
        /// Tools and data, including:
        ///     1. Datafiles, for each language, with AM (acoustic model) files
        ///     2. Extern depend on extern tools, HTK.
        /// </summary>
        public string WorksiteDir
        {
            get
            {
                return _worksiteDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _worksiteDir = value;
            }
        }

        /// <summary>
        /// Gets or sets Speaker metadata of the data to align.
        /// </summary>
        public Speaker Speaker
        {
            get
            {
                return _speaker;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _speaker = value;
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
        /// Gets or sets A filelist file path. (with externsion .txt).
        /// </summary>
        public string FileList
        {
            get
            {
                return _fileList;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _fileList = string.Empty;
                }

                _fileList = value;
            }
        }

        /// <summary>
        /// Gets or sets A directory path, containing script files (with externsion .xml).
        /// </summary>
        public string ScriptDir
        {
            get
            {
                return _scriptDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _scriptDir = value;
            }
        }

        /// <summary>
        /// Gets or sets Waveform 16khz files directory.
        /// </summary>
        public string Wave16KDir
        {
            get
            {
                return _wave16kDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _wave16kDir = value;
            }
        }

        /// <summary>
        /// Gets or sets Speaker dependent acoustic model directory.
        /// </summary>
        public string ModelDir
        {
            get
            {
                return _modelDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _modelDir = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not rebuild the speaker 
        /// Dependent acoustic model.
        /// </summary>
        public bool RebuildModel
        {
            get { return _rebuildModel; }
            set { _rebuildModel = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether ignore tone in output.
        /// </summary>
        public bool IgnoreTone
        {
            get { return _ignoreTone; }
            set { _ignoreTone = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether keep SR phones in output.
        /// </summary>
        public bool KeepSRPhones
        {
            get { return _keepSRPhones; }
            set { _keepSRPhones = value; }
        }

        /// <summary>
        /// Gets or sets The silence duration threshold below which to be filtered.
        /// </summary>
        public long SilenceDurationThresh
        {
            get { return _silenceDurationThresh; }
            set { _silenceDurationThresh = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether adapts from speaker independent acoustic model 
        /// Or re-train the new acoustic model from scratch
        /// <param />
        /// It is true by default.
        /// </summary>
        public bool AdaptModel
        {
            get { return _adaptModel; }
            set { _adaptModel = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether using SR-SD combine model in speaker dependent acoustic model
        /// <param />
        /// It is false by default.
        /// </summary>
        public bool HybridSDModel
        {
            get { return _hybridSDModel; }
            set { _hybridSDModel = value; }
        }

        /// <summary>
        /// Gets or sets Output directory path, there will be some kind of output data generated.
        /// 1. Filemap.exe, this file list map is generated from the waveform 16k dir
        /// 2. Script.xml, this script file is generated from the script dir
        /// 3. SliceSegment directory, this directory contains the phonetic alignment result
        /// 4. Intermediate directory, this directory contains some internal data.
        /// </summary>
        public string OutputDir
        {
            get
            {
                return _outputDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _outputDir = value;
            }
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Save forced align config data into XML file.
        /// </summary>
        /// <param name="filePath">Target file path to save.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase",
            Justification = "it is not culture specific string")]
        public void Save(string filePath)
        {
            XmlDocument dom = new XmlDocument();
            dom.NameTable.Add(ConfigSchema.TargetNamespace);

            // root element
            XmlElement ele = dom.CreateElement("forcedAlign", ConfigSchema.TargetNamespace);
            if (!string.IsNullOrEmpty(WorksiteDir))
            {
                ele.SetAttribute("workSite", WorksiteDir);
            }

            // Optional language data path
            if (_languageData != null)
            {
                _languageData.SaveLanguageData(dom, ConfigSchema, ele);
            }

            // Trace level.
            XmlElement traceLevelEle = dom.CreateElement("traceLevel", ConfigSchema.TargetNamespace);
            traceLevelEle.SetAttribute("value", TraceLevel.ToString().ToLowerInvariant());
            ele.AppendChild(traceLevelEle);

            // speaker metadata
            XmlElement speakerEle = dom.CreateElement("speaker", ConfigSchema.TargetNamespace);
            speakerEle.SetAttribute("primaryLanguage",
                Localor.LanguageToString(this.Speaker.PrimaryLanguage));
            speakerEle.SetAttribute("gender", this.Speaker.Gender.ToString());
            ele.AppendChild(speakerEle);

            // Optional filelist file path.
            if (!string.IsNullOrEmpty(FileList))
            {
                XmlElement fileListEle = dom.CreateElement("fileList", ConfigSchema.TargetNamespace);
                fileListEle.SetAttribute("path", FileList);
                ele.AppendChild(fileListEle);
            }

            // script
            XmlElement scriptDirEle = dom.CreateElement("scriptDir", ConfigSchema.TargetNamespace);
            Debug.Assert(!string.IsNullOrEmpty(ScriptDir));
            scriptDirEle.SetAttribute("path", ScriptDir);
            ele.AppendChild(scriptDirEle);

            // wave 16k hz directory
            XmlElement wave16kDirEle = dom.CreateElement("wave16kDir", ConfigSchema.TargetNamespace);
            Debug.Assert(!string.IsNullOrEmpty(Wave16KDir));
            wave16kDirEle.SetAttribute("path", Wave16KDir);
            ele.AppendChild(wave16kDirEle);

            // forced Align model
            XmlElement adaptEle = dom.CreateElement("applySRModel", ConfigSchema.TargetNamespace);
            adaptEle.SetAttribute("value",
                AdaptModel.ToString().ToLower(CultureInfo.InvariantCulture));
            ele.AppendChild(adaptEle);

            // hybrid SD Align model
            XmlElement hybridSDEle = dom.CreateElement("hybridSDModel", ConfigSchema.TargetNamespace);
            hybridSDEle.SetAttribute("value",
                HybridSDModel.ToString().ToLower(CultureInfo.InvariantCulture));
            ele.AppendChild(hybridSDEle);

            if ((RebuildModel == false) && (AdaptModel == true))
            {
                XmlElement modelDirEle = dom.CreateElement("customizedModelDir", ConfigSchema.TargetNamespace);
                Debug.Assert(!string.IsNullOrEmpty(ModelDir));
                modelDirEle.SetAttribute("path", ModelDir);
                ele.AppendChild(modelDirEle);
            }

            // Advantage config.
            XmlElement advancedConfigureEle = dom.CreateElement("advancedConfigure", ConfigSchema.TargetNamespace);
            
            // ignore tone
            XmlElement ignoreToneEle = dom.CreateElement("ignoreTone", ConfigSchema.TargetNamespace);
            ignoreToneEle.SetAttribute("value", IgnoreTone.ToString().ToLowerInvariant());
            advancedConfigureEle.AppendChild(ignoreToneEle);
            
            // keep SR phones
            XmlElement keepSRPhonesEle = dom.CreateElement("keepSRPhones", ConfigSchema.TargetNamespace);
            keepSRPhonesEle.SetAttribute("value", KeepSRPhones.ToString().ToLowerInvariant());
            advancedConfigureEle.AppendChild(keepSRPhonesEle);
            
            // silence duration threshold
            XmlElement silenceDurationThreshEle = dom.CreateElement("silenceDurationThresh", ConfigSchema.TargetNamespace);
            silenceDurationThreshEle.SetAttribute("value", SilenceDurationThresh.ToString().ToLowerInvariant());
            advancedConfigureEle.AppendChild(silenceDurationThreshEle);

            ele.AppendChild(advancedConfigureEle);
            
            // SD Parameters.
            XmlElement sdParametersEle = dom.CreateElement("sdParameters", ConfigSchema.TargetNamespace);

            // Minimal utterances number required for SD model.
            XmlElement minUtteranceNumberEle = dom.CreateElement("minUtteranceNumber", ConfigSchema.TargetNamespace);
            minUtteranceNumberEle.SetAttribute("value", MinUttNumber.ToString().ToLowerInvariant());
            sdParametersEle.AppendChild(minUtteranceNumberEle);

            // Prune level
            XmlElement pruneLevelEle = dom.CreateElement("pruneLevel", ConfigSchema.TargetNamespace);
            pruneLevelEle.SetAttribute("level", PruneLevel.ToString().ToLowerInvariant());
            sdParametersEle.AppendChild(pruneLevelEle);

            ele.AppendChild(sdParametersEle);

            // output directory
            XmlElement outputDirEle = dom.CreateElement("outputDir", ConfigSchema.TargetNamespace);
            Debug.Assert(!string.IsNullOrEmpty(OutputDir));
            outputDirEle.SetAttribute("path", OutputDir);
            ele.AppendChild(outputDirEle);

            dom.AppendChild(ele);
            dom.Save(filePath);

            // performance compatability format checking
            XmlHelper.Validate(filePath, ConfigSchema);
        }

        /// <summary>
        /// Load Forced Align config from XML config file.
        /// </summary>
        /// <param name="filePath">Config filepath.</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public void Load(string filePath)
        {
            // check the configuration file first
            try
            {
                XmlHelper.Validate(filePath, ConfigSchema);
            }
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The configuration file [{0}] error is found. {1} {2}",
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
                string str1 = "The configuration xml file [{0}] must use the schema namespace [{1}]. ";
                string str2 = "Currently the config file uses namespace [{2}]";
                string message = string.Format(CultureInfo.InvariantCulture,
                    str1 + str2,
                    filePath, ConfigSchema.TargetNamespace, dom.DocumentElement.NamespaceURI);
                throw new InvalidDataException(message);
            }

            // Parse speaker metadata
            XmlNode node = dom.DocumentElement.SelectSingleNode(@"tts:speaker/@gender", nsmgr);
            Speaker.Gender = (Gender)Enum.Parse(typeof(Gender), node.InnerText);

            node = dom.DocumentElement.SelectSingleNode(@"tts:speaker/@primaryLanguage", nsmgr);
            Speaker.PrimaryLanguage = Localor.StringToLanguage(node.InnerText);

            _languageData.ParseLanguageData(dom, nsmgr, "tts", true);
            _languageData.SetLanguageData(Speaker.PrimaryLanguage);

            // parse basic data directory path
            ParseTraceLevel(dom, nsmgr);
            ParseWorkSiteDir(filePath, dom);
            ParseFileList(filePath, dom, nsmgr);
            ParseScriptDir(filePath, dom, nsmgr);
            ParseWave16kDir(filePath, dom, nsmgr);
            ParseMode(dom, nsmgr);
            ParseModelDir(filePath, dom, nsmgr);
            ParseOutputDir(filePath, dom, nsmgr);
            ParseAdvancedConfigure(dom, nsmgr);
            ParseSDParameters(dom, nsmgr);
        }

        /// <summary>
        /// Parse HVite.exe -t parameter level.
        /// </summary>
        /// <param name="dom">Configuration XML document.</param>
        /// <param name="nmgr">XML namespace manager.</param>
        private void ParseSDParameters(XmlDocument dom, XmlNamespaceManager nmgr)
        {
            XmlNode node = dom.DocumentElement.SelectSingleNode(@"tts:sdParameters/tts:pruneLevel/@level", nmgr);

            if (node != null)
            {
                string value = node.Value;
                PruneLevel = int.Parse(value);
            }
            else
            {
                PruneLevel = 0;
            }

            node = dom.DocumentElement.SelectSingleNode(@"tts:sdParameters/tts:minUtteranceNumber/@value", nmgr);

            if (node != null)
            {
                string value = node.Value;
                MinUttNumber = int.Parse(value);
            }
        }

        /// <summary>
        /// Parse trace level.
        /// </summary>
        /// <param name="dom">Configuration XML document.</param>
        /// <param name="nmgr">XML namespace manager.</param>
        private void ParseTraceLevel(XmlDocument dom, XmlNamespaceManager nmgr)
        {
            XmlNode node = dom.DocumentElement.SelectSingleNode(@"tts:traceLevel/@value", nmgr);

            if (node != null)
            {
                string value = node.Value;
                TraceLevel = int.Parse(value);
            }
            else
            {
                TraceLevel = 0;
            }
        }

        /// <summary>
        /// Forced alignment model path and control (rebuild).
        /// </summary>
        /// <param name="configFile">Configuration file path.</param>
        /// <param name="dom">Configuration XML document.</param>
        /// <param name="nmgr">XML namespace.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "it is not culture specific string")]
        private void ParseModelDir(string configFile, XmlDocument dom, XmlNamespaceManager nmgr)
        {
            if (AdaptModel == true)
            {
                XmlNode node = dom.DocumentElement.SelectSingleNode(@"tts:customizedModelDir/@path", nmgr);
                if (node == null)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                       "No customized model, the attached SR model will be adapted to do the forced alignment");
                    Console.WriteLine(message);
                    RebuildModel = true;
                }
                else if (!Helper.IsValidPath(node.InnerText))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "The model dir [{0}] is invalid directory path, which is specified in config file [{1}]",
                        node.InnerText, configFile);
                    throw new InvalidDataException(message);
                }
                else
                {
                    ModelDir = node.InnerText;
                    string alignModelName =
                        "mmf." + Speaker.Gender.ToString().Substring(0, 1).ToLowerInvariant();
                    string alignModelPath = Path.Combine(ModelDir, alignModelName);
                    if (!File.Exists(alignModelPath))
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "No model file [{0}] is found in the customized model dir [{1}]. Please check the file if you want to use the customized model.",
                            alignModelName, ModelDir);
                        throw new InvalidDataException(message);
                    }
                    else
                    {
                        RebuildModel = false;
                    }
                }
            }
            else
            {
                XmlNode node = dom.DocumentElement.SelectSingleNode(@"tts:customizedModelDir/@path", nmgr);
                if (node != null)
                {
                    string str1 = "SR model is not needed if the value of applySRModel is false. ";
                    string str2 = "The path of customized model [{0}] is ignored, speaker dependent model will be trained to do the alignment";
                    string message = string.Format(CultureInfo.InvariantCulture,
                       str1 + str2, node.InnerText);
                    Console.WriteLine(message);
                }

                RebuildModel = false;
            }
        }

        private void ParseMode(XmlDocument dom, XmlNamespaceManager nmgr)
        {
            XmlNode node = dom.DocumentElement.SelectSingleNode(@"tts:applySRModel/@value", nmgr);
            if (node != null)
            {
                AdaptModel = bool.Parse(node.InnerText);
            }

            node = dom.DocumentElement.SelectSingleNode(@"tts:hybridSDModel/@value", nmgr);
            if (node != null)
            {
                HybridSDModel = bool.Parse(node.InnerText);

                if (HybridSDModel && !AdaptModel)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                            "To use hybrid SD model, the applySRModel must be set true.");
                    throw new InvalidDataException(message);
                }
            }
        }

        /// <summary>
        /// Parse output file path.
        /// </summary>
        /// <param name="configFile">Configuration file path.</param>
        /// <param name="dom">Configuration XML document.</param>
        /// <param name="nmgr">XML namespace.</param>
        private void ParseOutputDir(string configFile, XmlDocument dom, XmlNamespaceManager nmgr)
        {
            XmlNode node = dom.DocumentElement.SelectSingleNode(@"tts:outputDir/@path", nmgr);
            if (!Helper.IsValidPath(node.InnerText))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "the output dir [{0}] is invalid directory path, which is specified in config file [{1}]",
                    node.InnerText, configFile);
                throw new InvalidDataException(message);
            }

            OutputDir = node.InnerText;
        }

        /// <summary>
        /// Parse advanced configure.
        /// </summary>
        /// <param name="dom">Configuration XML document.</param>
        /// <param name="nmgr">XML namespace.</param>
        private void ParseAdvancedConfigure(XmlDocument dom, XmlNamespaceManager nmgr)
        {
            XmlNode node = dom.DocumentElement.SelectSingleNode(@"tts:advancedConfigure/tts:ignoreTone/@value", nmgr);
            if (node != null)
            {
                IgnoreTone = bool.Parse(node.InnerText);
            }

            node = dom.DocumentElement.SelectSingleNode(@"tts:advancedConfigure/tts:keepSRPhones/@value", nmgr);
            if (node != null)
            {
                KeepSRPhones = bool.Parse(node.InnerText);
            }

            node = dom.DocumentElement.SelectSingleNode(@"tts:advancedConfigure/tts:silenceDurationThresh/@value", nmgr);
            if (node != null)
            {
                SilenceDurationThresh = long.Parse(node.InnerText);
            }
        }

        /// <summary>
        /// Parse waveform 16k hz files directory.
        /// </summary>
        /// <param name="configFile">Configuration file path.</param>
        /// <param name="dom">Configuration XML document.</param>
        /// <param name="nmgr">XML namespace.</param>
        private void ParseWave16kDir(string configFile, XmlDocument dom, XmlNamespaceManager nmgr)
        {
            XmlNode node = dom.DocumentElement.SelectSingleNode(@"tts:wave16kDir/@path", nmgr);
            if (!Helper.IsValidPath(node.InnerText))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid wave16kDir path [{0}], which is specified in config file [{1}]",
                    node.InnerText, configFile);
                throw new InvalidDataException(message);
            }

            if (!Directory.Exists(node.InnerText))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Could not find a part of the wave16kDir path [{0}], which is specified in config file [{1}]",
                    node.InnerText, configFile);
                throw new DirectoryNotFoundException(message);
            }

            Wave16KDir = node.InnerText;
        }

        /// <summary>
        /// Parse filelist file path.
        /// </summary>
        /// <param name="configFile">Configuration file path.</param>
        /// <param name="dom">Configuration XML document.</param>
        /// <param name="nmgr">XML namespace.</param>
        private void ParseFileList(string configFile, XmlDocument dom, XmlNamespaceManager nmgr)
        {
            XmlNode fileListPathNode =
                dom.DocumentElement.SelectSingleNode(@"tts:fileList/@path", nmgr);

            if (fileListPathNode != null)
            {
                _fileList = fileListPathNode.InnerText;

                if (!Helper.IsValidPath(_fileList))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "The fileList file path [{0}] is invalid file path, which is specified in config file [{1}]",
                        _fileList, configFile);
                    throw new InvalidDataException(message);
                }

                if (!File.Exists(_fileList))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Could not find a part of the fileList path [{0}], which is specified in config file [{1}]",
                        _fileList, configFile);
                    throw new DirectoryNotFoundException(message);
                }
            }
        }

        /// <summary>
        /// Parse sub-script files directory.
        /// </summary>
        /// <param name="configFile">Configuration file path.</param>
        /// <param name="dom">Configuration XML document.</param>
        /// <param name="nmgr">XML namespace.</param>
        private void ParseScriptDir(string configFile, XmlDocument dom, XmlNamespaceManager nmgr)
        {
            string tempDirString =
                dom.DocumentElement.SelectSingleNode(@"tts:scriptDir/@path", nmgr).InnerText;
            if (!Helper.IsValidPath(tempDirString))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The script dir [{0}] is invalid directory path, which is specified in config file [{1}]",
                    tempDirString, configFile);
                throw new InvalidDataException(message);
            }

            if (!Directory.Exists(tempDirString))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Could not find a part of the scriptDir path [{0}], which is specified in config file [{1}]",
                    tempDirString, configFile);
                throw new DirectoryNotFoundException(message);
            }

            if (Directory.GetFiles(tempDirString, "*.xml").Length == 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Cannot found script file (with .xml extension) under script dir [{0}] specified in the config file [{1}]",
                    tempDirString, configFile);
                throw new InvalidDataException(message);
            }

            ScriptDir = tempDirString;
        }

        /// <summary>
        /// Work site path parsing and verification.
        /// <param />
        /// The forced alignment tool depends this path to reach some extern 
        /// Tools and data, including:
        ///     1. Datafiles, for each language, with AM (acoustic model) files
        ///     2. Extern depend on extern tools, HTK.
        /// </summary>
        /// <param name="configFile">Configuration file path.</param>
        /// <param name="dom">Configuration XML document.</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        private void ParseWorkSiteDir(string configFile, XmlDocument dom)
        {
            // workSite is an optional attribute of the root element
            if (dom.DocumentElement.HasAttribute("workSite"))
            {
                // if "workSite" is specified in the config file, check it
                string siteDir = dom.DocumentElement.GetAttribute("workSite");
                if (string.IsNullOrEmpty(siteDir))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "workSite directory path [{0}] can not be empty in config file [{1}].",
                        siteDir, configFile);
                    throw new InvalidDataException(message);
                }

                if (!Helper.IsValidPath(siteDir))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid workSite path [{0}],which is specified in config file [{1}]",
                        siteDir, configFile);
                    throw new InvalidDataException(message);
                }

                if (!Directory.Exists(siteDir))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Could not find a part of the workSite path [{0}], which is specified in config file [{1}], ",
                        siteDir, configFile);
                    throw new DirectoryNotFoundException(message);
                }

                WorksiteDir = siteDir;
            }
            else
            {
                // if not specified, use default path, which is the same directory 
                // location as the application itself
                string appDir = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                WorksiteDir = Path.GetDirectoryName(appDir);
            }
        }

        #endregion
    }
}