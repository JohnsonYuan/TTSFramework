//----------------------------------------------------------------------------
// <copyright file="DataHandlerList.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Data Handler List for Raw data
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Xml;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Compiler.LanguageData;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Frontend;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// DataHandlerList class.
    /// </summary>
    public class DataHandlerList
    {
        #region Fields

        private Dictionary<string, DataHandler> _datas = new Dictionary<string, DataHandler>();
        private string _domain = DomainItem.GeneralDomain;
        private string _dataRoot = Directory.GetCurrentDirectory();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataHandlerList"/> class.
        /// </summary>
        /// <param name="domain">Domain.</param>
        public DataHandlerList(string domain)
        {
            if (string.IsNullOrEmpty(domain))
            {
                throw new ArgumentNullException("domain");
            }

            _domain = domain.ToLowerInvariant();
            Initialize();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Datas.
        /// </summary>
        public Dictionary<string, DataHandler> Datas
        {
            get
            { 
                return _datas; 
            }
        }

        /// <summary>
        /// Gets or sets Data root dir.
        /// </summary>
        public string DataRoot
        {
            get 
            {
                return _dataRoot; 
            }

            set 
            {
                _dataRoot = value; 
            }
        }

        /// <summary>
        /// Gets Domain of the handler list.
        /// </summary>
        public string Domain
        {
            get 
            { 
                return _domain; 
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Set language.
        /// </summary>
        /// <param name="language">Language.</param>
        public void SetLanguage(Language language)
        {
            foreach (KeyValuePair<string, DataHandler> pair in _datas)
            {
                pair.Value.SetLanguage(language);
            }
        }

        /// <summary>
        /// Prepare all data path.
        /// </summary>
        /// <param name="rawRootDir">RawRootDir.</param>
        /// <param name="rawDataList">RawDataList.</param>
        public void PrepareDataPath(string rawRootDir, Dictionary<string, string> rawDataList)
        {
            Helper.ThrowIfNull(rawDataList);

            SetDataDir(rawRootDir);

            // Set the path for raw data
            foreach (KeyValuePair<string, string> pair in rawDataList)
            {
                string path = pair.Value;
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        SetRawDataPath(pair.Key, path);
                    }
                    catch (ArgumentException)
                    {
                        Error error = new Error(DataCompilerError.InvalidRawData, pair.Key);
                        Helper.PrintColorMessage(error.Severity, error.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Set the object for internal usage.
        /// </summary>
        /// <param name="name">The name string.</param>
        /// <param name="obj">The obj objetc.</param>
        public void SetObject(string name, object obj)
        {
            if (_datas.ContainsKey(name))
            {
                _datas[name].SetObject(obj);
            }
            else
            {
                throw new ArgumentException(
                    Helper.NeutralFormat("Invalid object for \"{0}\"", name), name);
            }
        }

        /// <summary>
        /// Initialize all the data.
        /// </summary>
        public void Initialize()
        {
            Type[] dataHandlers = new Type[]
            {
                typeof(LexiconData),
                typeof(SchemaData),
                typeof(PosSetData),
                typeof(PhoneSetData),
                typeof(BackendPhoneSetData),
                typeof(CharTableData),
                typeof(SyllabifyRuleData),
                typeof(TruncateRuleData),
                typeof(PauseLengthData),
                typeof(PolyphoneRuleData),
                typeof(SentenceDetectData),
                typeof(QuotationDetectorData),
                typeof(ParallelStructDetectorData),
                typeof(TextRegularizerData),
                typeof(PosLexicalRuleData),
                typeof(PosContextualRuleData),
                typeof(TnRuleData),
                typeof(FstNERuleData),
                typeof(CompoundRuleData),
                typeof(LtsRuleDataPath),
                typeof(WordBreakerDataPath),
                typeof(ChineseToneData),
                typeof(PostWordBreakerData),
                typeof(SentenceSeparatorDataPath),
                typeof(BoundaryPronChangeRuleData),
                typeof(PhoneMappingRuleData),
                typeof(BackendPhoneMappingRuleData),
                typeof(FrontendBackendPhoneMappingRuleData),
                typeof(MixLingualPOSConverterData),
                typeof(ForeignLtsCollection),
                typeof(WordFrequencyData),
                typeof(ExtraDomainLexiconData),
                typeof(RegressionLexiconData),
                typeof(DomainScriptFolderData),
                typeof(DomainListFileData),
                typeof(NonPrunedWordListFileData),
                typeof(AcronymDisambiguationData),
                typeof(NEDisambiguationData),
                typeof(VoiceFontData),
                typeof(ExtraDATData),
                typeof(PolyphonyModelData),
                typeof(RNNPolyphonyModelData),
            };

            foreach (Type type in dataHandlers)
            {
                Debug.Assert(type.BaseType.Equals(typeof(DataHandler)));
                if (type.BaseType.Equals(typeof(DataHandler)))
                {
                    DataHandler dataHandler = (DataHandler)Activator.CreateInstance(type);
                    _datas[dataHandler.Name] = dataHandler;
                }
            }
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// Get the data object according to the name.
        /// </summary>
        /// <param name="name">Data name.</param>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>Error.</returns>
        internal object GetObject(string name, ErrorSet errorSet)
        {
            object obj = null;
            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            if (_datas.ContainsKey(name))
            {
                obj = _datas[name].GetObject(errorSet);
            }
            else
            {
                errorSet.Add(DataCompilerError.InvalidModuleData);
            }

            return obj;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Set the data directory and also set the data directory for all data.
        /// </summary>
        /// <param name="dataDir">Data directory.</param>
        private void SetDataDir(string dataDir)
        {
            _dataRoot = dataDir;
            foreach (KeyValuePair<string, DataHandler> pair in _datas)
            {
                if (string.IsNullOrEmpty(pair.Value.Path) &&
                    !string.IsNullOrEmpty(pair.Value.RelativePath))
                {
                    pair.Value.Path = Path.Combine(dataDir, pair.Value.RelativePath);
                }
            }
        }

        /// <summary>
        /// SetRawDataPath.
        /// </summary>
        /// <param name="rawDataName">RawDataName.</param>
        /// <param name="path">Path.</param>
        private void SetRawDataPath(string rawDataName, string path)
        {
            DataHandler dataHandler = null;
            foreach (KeyValuePair<string, DataHandler> pair in _datas)
            {
                if (pair.Key.Equals(rawDataName, StringComparison.OrdinalIgnoreCase))
                {
                    dataHandler = pair.Value;
                    break;
                }
            }

            if (dataHandler != null)
            {
                dataHandler.Path = path;
                dataHandler.RelativePath = null;
            }
            else
            {
                throw new ArgumentException("There is no such raw data", "rawDataName");
            }
        }

        #endregion
    }
}