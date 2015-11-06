//----------------------------------------------------------------------------
// <copyright file="LangDataCompiler.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      DataCompiler
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Xml;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Compiler.LanguageData;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Frontend;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider.LangData;
    using SP = Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// Data Compiler.
    /// </summary>
    public class DataCompiler
    {
        #region Fields
        private string _schemaFullName = "Lexical Attribute Schema";
        private Dictionary<string, LangDataObject> _moduleDataSet = new Dictionary<string, LangDataObject>();
        private string _toolDir = Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location);

        private Language _language = Language.Neutral;
        private DataHandlerList _dataHandlerList = new DataHandlerList(DomainItem.GeneralDomain);
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets DataHandlerList.
        /// </summary>
        public DataHandlerList DataHandlerList
        {
            get
            {
                return _dataHandlerList;
            }

            set
            {
                Helper.ThrowIfNull(value);
                _dataHandlerList = value;
            }
        }

        /// <summary>
        /// Gets ModuleDataSet contains the binary data which has been or will be generated.
        /// Value.Data == null means the binary data is under constructing, not available this moment.
        /// Value.Data != null means the binary data is built out and ready.
        /// </summary>
        public Dictionary<string, LangDataObject> ModuleDataSet
        {
            get
            {
                return _moduleDataSet;
            }
        }

        /// <summary>
        /// Gets or sets Tool Directory.
        /// </summary>
        public string ToolDir
        {
            get 
            { 
                return _toolDir; 
            }

            set 
            { 
                _toolDir = value; 
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Load Stream from file path.
        /// </summary>
        /// <param name="filePath">Path of file.</param>
        /// <param name="outputStream">OutputStream.</param>
        public static void LoadStream(string filePath, Stream outputStream)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            byte[] byteDatas = File.ReadAllBytes(filePath);
            outputStream.Write(byteDatas, 0, byteDatas.Length);
        }

        /// <summary>
        /// Set language.
        /// </summary>
        /// <param name="language">Language.</param>
        public void SetLanguage(Language language)
        {
            _language = language;
            _dataHandlerList.SetLanguage(language);
        }

        /// <summary>
        /// Get the Data Path according to the raw data name.
        /// </summary>
        /// <param name="name">Raw data name.</param>
        /// <returns>Data path.</returns>
        public string GetDataPath(string name)
        {
            if (_dataHandlerList.Datas.ContainsKey(name))
            {
                return _dataHandlerList.Datas[name].Path;
            }
            else
            {
                throw new ArgumentException(Helper.NeutralFormat(
                    "Could not find raw data {0} for LangDataCompiler", name), "name");
            }
        }

        /// <summary>
        /// Build the data.
        /// </summary>
        /// <param name="moduleDataName">Module data name.</param>
        /// <param name="outputStream">Output Stream.</param>
        /// <param name="isEnableValidate">Is enable data validate.</param>
        /// <param name="formatGuid">Format guid.</param>
        /// <returns>ErrorSet.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Postponed")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public ErrorSet Build(string moduleDataName, Stream outputStream, bool isEnableValidate, string formatGuid)
        {
            ////#region Check arguments
            if (string.IsNullOrEmpty(moduleDataName))
            {
                throw new ArgumentNullException("dataName");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }
            ////#endregion

            ErrorSet errorSet = new ErrorSet();
            ErrorSet subErrorSet = new ErrorSet();
            try
            {
                switch (moduleDataName)
                {
                    case ModuleDataName.PhoneSet:
                        TtsPhoneSet phoneSet = (TtsPhoneSet)GetObject(RawDataName.PhoneSet, errorSet);
                        if (!errorSet.Contains(ErrorSeverity.MustFix))
                        {
                            errorSet.Merge(PhoneSetCompiler.Compile(phoneSet, outputStream));
                        }

                        break;

                    case ModuleDataName.BackendPhoneSet:
                        phoneSet = (TtsPhoneSet)GetObject(RawDataName.BackendPhoneSet, errorSet);
                        if (!errorSet.Contains(ErrorSeverity.MustFix))
                        {
                            errorSet.Merge(PhoneSetCompiler.Compile(phoneSet, outputStream));
                        }

                        break;

                    case ModuleDataName.PosSet:
                        TtsPosSet posSet = (TtsPosSet)GetObject(RawDataName.PosSet, errorSet);
                        if (!errorSet.Contains(ErrorSeverity.MustFix))
                        {
                            errorSet.Merge(PosSetCompiler.Compile(posSet, outputStream));
                        }

                        break;

                    case ModuleDataName.PosTaggerPos:
                        LexicalAttributeSchema schema = (LexicalAttributeSchema)GetObject(
                            RawDataName.LexicalAttributeSchema, subErrorSet);
                        MergeDependencyError(errorSet, subErrorSet, _schemaFullName);
                        if (!subErrorSet.Contains(ErrorSeverity.MustFix))
                        {
                            TtsPosSet postaggingPosSet = TtsPosSet.LoadPosTaggingPosFromSchema(schema);
                            errorSet.Merge(PosSetCompiler.CompilePosTaggerPos(postaggingPosSet, outputStream));
                        }

                        break;

                    case ModuleDataName.Lexicon:
                        errorSet = CompileLexicon(outputStream);
                        break;

                    case ModuleDataName.CharTable:
                        ErrorSet charTableErrorSet = CompileCharTable(outputStream);
                        if (!isEnableValidate)
                        {
                            foreach (Error error in charTableErrorSet.Errors)
                            {
                                error.Severity = ErrorSeverity.Warning;
                            }
                        }

                        errorSet.Merge(charTableErrorSet);
                        break;

                    case ModuleDataName.SentenceSeparator:
                        string sentSepDataDir = _dataHandlerList.Datas[RawDataName.SentenceSeparatorDataPath].Path;
                        Collection<string> compiledSentenceSeparatorFiles = new Collection<string>();
                        errorSet = SentenceSeparatorCompiler.Compile(sentSepDataDir, outputStream, compiledSentenceSeparatorFiles);
                        if (errorSet.GetSeverityCount(ErrorSeverity.MustFix) == 0 &&
                            compiledSentenceSeparatorFiles.Count > 0)
                        {
                            errorSet.Add(ReportCompiledFiles("sentence separator", compiledSentenceSeparatorFiles));
                        }

                        break;

                    case ModuleDataName.WordBreaker:
                        {
                            System.IO.MemoryStream memStream = new MemoryStream();
                            string wordBreakerDataDir = _dataHandlerList.Datas[RawDataName.WordBreakerDataPath].Path;
                            Collection<string> compiledWordBreakerFiles = new Collection<string>();
                            errorSet = WordBreakerCompiler.Compile(wordBreakerDataDir, outputStream, compiledWordBreakerFiles, formatGuid);
                            if (errorSet.GetSeverityCount(ErrorSeverity.MustFix) == 0 && compiledWordBreakerFiles.Count > 0)
                            {
                                errorSet.Add(ReportCompiledFiles("word breaker", compiledWordBreakerFiles));
                            }
                        }

                        break;

                    case ModuleDataName.PostWordBreaker:
                        string postWordBreakerFilePath = _dataHandlerList.Datas[RawDataName.PostWordBreaker].Path;
                        errorSet = PostWordBreakerCompiler.Compile(postWordBreakerFilePath, outputStream);
                        break;

                    case ModuleDataName.ChineseTone:
                        string chineseToneFilePath = _dataHandlerList.Datas[RawDataName.ChineseTone].Path;
                        errorSet = ChineseToneCompiler.Compile(chineseToneFilePath, outputStream);
                        break;

                    case ModuleDataName.AcronymDisambiguation:
                        {
                            string acronymDisambiguationDataDir = _dataHandlerList.Datas[RawDataName.AcronymDisambiguation].Path;
                            Collection<string> compiledFiles = new Collection<string>();

                            errorSet = CrfModelCompiler.Compile(acronymDisambiguationDataDir, outputStream, compiledFiles, _language);
                            if (errorSet.GetSeverityCount(ErrorSeverity.MustFix) == 0 &&
                                compiledFiles.Count > 0)
                            {
                                errorSet.Add(ReportCompiledFiles("AcronymDisambiguation", compiledFiles));
                            }
                        }

                        break;

                    case ModuleDataName.NEDisambiguation:
                        {
                            string strNeDisambiguationDataDir = _dataHandlerList.Datas[RawDataName.NEDisambiguation].Path;
                            Collection<string> compiledFiles = new Collection<string>();
                            errorSet = CrfModelCompiler.Compile(strNeDisambiguationDataDir, outputStream, compiledFiles, _language);
                            if (errorSet.GetSeverityCount(ErrorSeverity.MustFix) == 0 &&
                                compiledFiles.Count > 0)
                            {
                                errorSet.Add(ReportCompiledFiles("NEDisambiguation", compiledFiles));
                            }
                        }

                        break;

                    case ModuleDataName.SyllabifyRule:
                        string syllabifyRuleFilePath = _dataHandlerList.Datas[RawDataName.SyllabifyRule].Path;
                        phoneSet = (TtsPhoneSet)GetObject(RawDataName.PhoneSet, subErrorSet);
                        MergeDependencyError(errorSet, subErrorSet, RawDataName.PhoneSet);
                        if (!subErrorSet.Contains(ErrorSeverity.MustFix))
                        {
                            errorSet.Merge(SyllabifyRuleCompiler.Compile(syllabifyRuleFilePath,
                                phoneSet, outputStream));
                        }

                        break;

                    case ModuleDataName.UnitGenerator:
                        string truncRuleFilePath = _dataHandlerList.Datas[RawDataName.TruncateRule].Path;
                        phoneSet = (TtsPhoneSet)GetObject(RawDataName.PhoneSet, subErrorSet);
                        MergeDependencyError(errorSet, subErrorSet, RawDataName.PhoneSet);
                        if (!subErrorSet.Contains(ErrorSeverity.MustFix))
                        {
                            errorSet.Merge(UnitGeneratorDataCompiler.Compile(truncRuleFilePath,
                                phoneSet, outputStream));
                        }

                        break;

                    case ModuleDataName.PolyphoneRule:
                        string generalRuleFilePath = _dataHandlerList.Datas[RawDataName.PolyphoneRule].Path;
                        phoneSet = (TtsPhoneSet)GetObject(RawDataName.PhoneSet, errorSet);
                        PolyphonyRuleFile polyRuleFile = new PolyphonyRuleFile();
                        ErrorSet polyErrorSet = polyRuleFile.Load(generalRuleFilePath, phoneSet);
                        if (!isEnableValidate)
                        {
                            foreach (Error error in polyErrorSet.Errors)
                            {
                                error.Severity = ErrorSeverity.Warning;
                            }
                        }

                        errorSet.Merge(polyErrorSet);
                        errorSet.Merge(CompileGeneralRule(generalRuleFilePath, outputStream));
                        break;

                    case ModuleDataName.BoundaryPronChangeRule:
                        generalRuleFilePath = _dataHandlerList.Datas[RawDataName.BoundaryPronChangeRule].Path;
                        errorSet = CompileGeneralRule(generalRuleFilePath, outputStream);
                        break;

                    case ModuleDataName.SentenceDetector:
                        generalRuleFilePath = _dataHandlerList.Datas[RawDataName.SentenceDetectRule].Path;
                        RuleFile ruleFile = new RuleFile();
                        List<string> dupKeys = ruleFile.GetDupKeys(generalRuleFilePath);
                        if (dupKeys.Count > 0)
                        {
                            foreach (string key in dupKeys)
                            {
                                errorSet.Add(new Error(DataCompilerError.DuplicateItemKey, key));
                            }
                        }
                        else
                        {
                            errorSet = CompileGeneralRule(generalRuleFilePath, outputStream);
                        }

                        break;

                    case ModuleDataName.QuotationMarkTable:
                        QuotationMarkTable quoteTable = QuotationMarkTable.Read(_dataHandlerList.Datas[RawDataName.QuotationMarkTable].Path);
                        errorSet = QuotationMarkCompiler.Compile(quoteTable, outputStream);
                        break;

                    case ModuleDataName.ParallelStructTable:
                        schema = (LexicalAttributeSchema)GetObject(
                            RawDataName.LexicalAttributeSchema, subErrorSet);
                        MergeDependencyError(errorSet, subErrorSet, _schemaFullName);
                        if (!subErrorSet.Contains(ErrorSeverity.MustFix))
                        {
                            TtsPosSet postaggingPosSet = TtsPosSet.LoadPosTaggingPosFromSchema(schema);

                            ParallelStructTable parallelStructTable = ParallelStructTable.Read(_dataHandlerList.Datas[RawDataName.ParallelStructTable].Path);
                            if (postaggingPosSet != null)
                            {
                                errorSet = ParallelStructCompiler.Compile(parallelStructTable, postaggingPosSet, outputStream);
                            }
                        }

                        break;

                    case ModuleDataName.WordFeatureSuffixTable:
                        schema = (LexicalAttributeSchema)GetObject(
                            RawDataName.LexicalAttributeSchema, subErrorSet);
                        MergeDependencyError(errorSet, subErrorSet, _schemaFullName);
                        if (!subErrorSet.Contains(ErrorSeverity.MustFix))
                        {
                            WordFeatureSuffixTable suffixTable = WordFeatureSuffixTable.Read(_dataHandlerList.Datas[RawDataName.WordFeatureSuffixTable].Path);
                            errorSet = WordFeatureSuffixCompiler.Compile(suffixTable, outputStream);
                        }

                        break;

                    case ModuleDataName.LtsRule:
                        string ltsRuleDataPath = _dataHandlerList.Datas[RawDataName.LtsRuleDataPath].Path;
                        errorSet = CompileLtsRule(ltsRuleDataPath, outputStream);
                        break;

                    case ModuleDataName.PhoneEventData:
                         PhoneConverterWrapper pcw = null;

                        // Check if the language has phone mapping data.
                        if (_moduleDataSet.ContainsKey(ModuleDataName.PhoneMappingRule))
                        {
                            // Check phone mapping binary data dependency.
                            if (_moduleDataSet[ModuleDataName.PhoneMappingRule].Data != null)
                            {
                                pcw = new PhoneConverterWrapper(_language, _moduleDataSet[ModuleDataName.PhoneMappingRule].Data);
                            }
                            else
                            {
                                errorSet.Add(DataCompilerError.DependenciesNotValid, "Please make sure that PhoneMappingRule has been compiled before PhoneEvent");
                            }
                        }

                        if (errorSet.GetSeverityCount(ErrorSeverity.MustFix) == 0)
                        {
                            phoneSet = (TtsPhoneSet)GetObject(RawDataName.PhoneSet, errorSet);
                            errorSet = PhoneEventCompiler.Compile(phoneSet, pcw, outputStream);
                        }

                        break;

                    case ModuleDataName.PosRule:
                        string lexicalRuleFilePath = _dataHandlerList.Datas[RawDataName.PosLexicalRule].Path;
                        string contextualRuleFilePath = _dataHandlerList.Datas[RawDataName.PosContextualRule].Path;
                        schema = (LexicalAttributeSchema)GetObject(
                            RawDataName.LexicalAttributeSchema, subErrorSet);
                        MergeDependencyError(errorSet, subErrorSet, _schemaFullName);
                        if (!subErrorSet.Contains(ErrorSeverity.MustFix))
                        {
                            posSet = TtsPosSet.LoadPosTaggingPosFromSchema(schema);
                            string posSetFilePath = Helper.GetTempFileName();
                            posSet.Save(posSetFilePath, Encoding.Unicode);
                            errorSet.Merge(CompilePosRule(lexicalRuleFilePath, contextualRuleFilePath,
                                posSetFilePath, outputStream));
                            File.Delete(posSetFilePath);
                        }

                        break;

                    case ModuleDataName.TnRule:
                        {
                            string tnmlRuleFilePath = _dataHandlerList.Datas[moduleDataName].Path;
                            string schemaFilePath = _dataHandlerList.Datas[RawDataName.LexicalAttributeSchema].Path;
                            errorSet = CompileTnml(tnmlRuleFilePath, schemaFilePath, outputStream, true);
                        }

                        break;

                    case ModuleDataName.FstNERule:
                        {
                            string fstNERuleFilePath = _dataHandlerList.Datas[moduleDataName].Path;
                            errorSet = CompileFstNE(fstNERuleFilePath, outputStream);
                        }

                        break;

                    case ModuleDataName.CompoundRule:
                        {
                            string tnmlRuleFilePath = _dataHandlerList.Datas[moduleDataName].Path;
                            string schemaFilePath = _dataHandlerList.Datas[RawDataName.LexicalAttributeSchema].Path;
                            phoneSet = (TtsPhoneSet)GetObject(RawDataName.PhoneSet, errorSet);
                            ErrorSet compundRuleError = DataFileValidator.ValidateCompoundRule(
                                    _dataHandlerList.Datas[moduleDataName].Path, phoneSet);

                            if (!isEnableValidate)
                            {
                                foreach (Error error in compundRuleError.Errors)
                                {
                                    error.Severity = ErrorSeverity.Warning;
                                }
                            }

                            errorSet.Merge(compundRuleError);
                            errorSet.Merge(CompileTnml(tnmlRuleFilePath, schemaFilePath, outputStream, false));
                        }

                        break;
                    case ModuleDataName.PhoneMappingRule:
                    case ModuleDataName.BackendPhoneMappingRule:
                    case ModuleDataName.FrontendBackendPhoneMappingRule:
                    case ModuleDataName.MixLingualPOSConverterData:
                        {
                            string tnmlRuleFilePath = _dataHandlerList.Datas[moduleDataName].Path;
                            string schemaFilePath = _dataHandlerList.Datas[RawDataName.LexicalAttributeSchema].Path;
                            errorSet = CompileTnml(tnmlRuleFilePath, schemaFilePath, outputStream, false);
                        }

                        break;
                    case ModuleDataName.ForeignLtsCollection:
                        errorSet = CompileForeignLtsCollection(_dataHandlerList.Datas[moduleDataName].Path, outputStream);
                        break;
                    case ModuleDataName.PolyphonyModel:
                        {
                            string polyphonyModelDataDir = _dataHandlerList.Datas[RawDataName.PolyphonyModel].Path;
                            Collection<string> compiledFiles = new Collection<string>();
                            errorSet = CrfModelCompiler.Compile(polyphonyModelDataDir, outputStream, compiledFiles, _language);
                            if (errorSet.GetSeverityCount(ErrorSeverity.MustFix) == 0 &&
                                compiledFiles.Count > 0)
                            {
                                errorSet.Add(ReportCompiledFiles("PolyphonyModel", compiledFiles));
                            }
                        }

                        break;
                    case ModuleDataName.RNNPolyphonyModel:
                        {
                            string polyphonyModelDataPath = _dataHandlerList.Datas[RawDataName.RNNPolyphonyModel].Path;
                            Collection<string> compiledFiles = new Collection<string>();
                            errorSet = RNNModelCompiler.Compile(polyphonyModelDataPath, outputStream, compiledFiles);
                            if (errorSet.GetSeverityCount(ErrorSeverity.MustFix) == 0 &&
                                compiledFiles.Count > 0)
                            {
                                errorSet.Add(ReportCompiledFiles("PolyphonyModel", compiledFiles));
                            }
                        }

                        break;
                    default:
                        errorSet.Add(DataCompilerError.InvalidModuleData, moduleDataName);
                        break;
                }
            }
            catch (Exception ex)
            {
                Type exceptionType = ex.GetType();
                if (exceptionType.Equals(typeof(FileNotFoundException)) ||
                    exceptionType.Equals(typeof(ArgumentNullException)) ||
                    exceptionType.Equals(typeof(XmlException)) ||
                    exceptionType.Equals(typeof(InvalidDataException)))
                {
                    errorSet.Add(DataCompilerError.RawDataNotFound, moduleDataName,
                        Helper.BuildExceptionMessage(ex));
                }
                else
                {
                    throw;
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Whether existing the module data.
        /// </summary>
        /// <param name="moduleDataName">ModuleDataName.</param>
        /// <returns>True for existing.</returns>
        public bool ExistModuleData(string moduleDataName)
        {
            return _moduleDataSet.ContainsKey(moduleDataName);
        }

        /// <summary>
        /// Compile the module data by data name.
        /// </summary>
        /// <param name="moduleDataName">Module Data Name.</param>
        /// <returns>Error set.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public ErrorSet Compile(string moduleDataName)
        {
            MemoryStream memoryStream = new MemoryStream();
            ErrorSet compilingErrorSet = Build(moduleDataName, memoryStream, false, string.Empty);
            string guid = LanguageDataHelper.GetReservedGuid(moduleDataName);
            if (!string.IsNullOrEmpty(guid))
            {
                ErrorSet errorSet = Compile(moduleDataName, memoryStream, guid, string.Empty);
                compilingErrorSet.Merge(errorSet);
            }

            return compilingErrorSet;
        }

        /// <summary>
        /// Compile the module data through data file path.
        /// </summary>
        /// <param name="moduleDataName">Module Data Name.</param>
        /// <param name="moduleDataPath">Module Data Path.</param>
        /// <returns>Error set.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public ErrorSet Compile(string moduleDataName, string moduleDataPath)
        {
            ErrorSet errorSet = null;
            string guid = LanguageDataHelper.GetReservedGuid(moduleDataName);
            if (!string.IsNullOrEmpty(guid))
            {
                MemoryStream memoryStream = new MemoryStream();
                LoadStream(moduleDataPath, memoryStream);
                errorSet = Compile(moduleDataName, memoryStream, guid, string.Empty);
            }
            else
            {
                errorSet = new ErrorSet();
                errorSet.Add(new Error(DataCompilerError.InvalidModuleData, moduleDataName));
            }

            return errorSet;
        }

        /// <summary>
        /// Compile the module data through data file path and with given guid string.
        /// </summary>
        /// <param name="moduleDataName">Module Data Name.</param>
        /// <param name="moduleDataPath">Module Data Path.</param>
        /// <param name="guidStr">Guid string.</param>
        /// <param name="formatGuidStr">Format guid string.</param>
        /// <returns>Error set.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public ErrorSet Compile(string moduleDataName, string moduleDataPath, string guidStr, string formatGuidStr)
        {
            MemoryStream memoryStream = new MemoryStream();
            LoadStream(moduleDataPath, memoryStream);
            return Compile(moduleDataName, memoryStream, guidStr, formatGuidStr);
        }

        /// <summary>
        /// Compile the module data through memory stream and with given guid string.
        /// </summary>
        /// <param name="moduleDataName">Module Data Name.</param>
        /// <param name="memoryStream">Memory stream.</param>
        /// <param name="guidStr">Guid string.</param>
        /// <param name="formatGuidStr">Format guid string.</param>
        /// <returns>Error set.</returns>
        public ErrorSet Compile(string moduleDataName, MemoryStream memoryStream, string guidStr, string formatGuidStr)
        {
            ErrorSet errorSet = new ErrorSet();

            if (memoryStream.Length <= 0)
            {
                errorSet.Add(new Error(DataCompilerError.ZeroModuleData, moduleDataName));
            }

            if (string.IsNullOrEmpty(guidStr))
            {
                errorSet.Add(new Error(DataCompilerError.InvalidModuleData, moduleDataName));
            }

            string currentGuidStr = guidStr;

            Guid guid = Guid.Empty;
            Guid formatGuid = Guid.Empty;

            try
            {
                guid = new Guid(currentGuidStr);

                currentGuidStr = formatGuidStr;
                if (!string.IsNullOrEmpty(currentGuidStr))
                {
                    formatGuid = new Guid(currentGuidStr);
                }
            }
            catch (FormatException ex)
            {
                errorSet.Add(new Error(DataCompilerError.InvalidGuidString,
                    moduleDataName, currentGuidStr, Helper.BuildExceptionMessage(ex)));
            }
            catch (OverflowException ex)
            {
                errorSet.Add(new Error(DataCompilerError.InvalidGuidString,
                    moduleDataName, currentGuidStr, Helper.BuildExceptionMessage(ex)));
            }

            if (errorSet.Count == 0)
            {
                byte[] data = memoryStream.ToArray();

                LangDataObject dataObject = new LangDataObject(
                    guid,
                    string.IsNullOrEmpty(formatGuidStr) ? SP.TtsDataTag.Find(guid) : formatGuid, 
                    data);
                if (_moduleDataSet.ContainsKey(moduleDataName))
                {
                    _moduleDataSet[moduleDataName] = dataObject;
                }
                else
                {
                    _moduleDataSet.Add(moduleDataName, dataObject);
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Create language data file.
        /// </summary>
        /// <param name="fileName">Language data file name.</param>
        /// <returns>Errors.</returns>
        public ErrorSet CombineDataFile(string fileName)
        {
            return CombineDataFile(fileName, DomainItem.GeneralDomain);
        }

        /// <summary>
        /// Create language data file.
        /// </summary>
        /// <param name="fileName">Language data file name.</param>
        /// <param name="domain">Domain.</param>
        /// <returns>Errors.</returns>
        public ErrorSet CombineDataFile(string fileName, string domain)
        {
            if (string.IsNullOrEmpty(domain) || string.IsNullOrEmpty(domain.Trim()))
            {
                domain = DomainItem.GeneralDomain;
            }

            ErrorSet errorSet = new ErrorSet();

            if (domain.Equals(DomainItem.GeneralDomain, StringComparison.OrdinalIgnoreCase))
            {
                errorSet = EnsureNecessaryData(this._moduleDataSet);
            }
            else if (this._moduleDataSet.Count == 0)
            {
                errorSet.Add(new Error(DataCompilerError.DomainDataMissing, domain));
            }

            if (!errorSet.Contains(ErrorSeverity.MustFix))
            {
                using (LangDataFile langDataFile = new LangDataFile())
                {
                    // Set file property
                    FileProperty fileProperty = new FileProperty();
                    fileProperty.Version = 1;
                    fileProperty.Build = 0;
                    fileProperty.LangID = (uint)_language;
                    langDataFile.FileProperties = fileProperty;

                    ArrayList sortedDataObjects = new ArrayList();
                    foreach (KeyValuePair<string, LangDataObject> obj in _moduleDataSet)
                    {
                        sortedDataObjects.Add(obj);
                    }

                    sortedDataObjects.Sort(new CompareLangDataObject());

                    // Set data objects
                    foreach (KeyValuePair<string, LangDataObject> obj in sortedDataObjects)
                    {
                        if (obj.Value.Data == null)
                        {
                            continue;
                        }

                        langDataFile.AddDataObject(obj.Value);
                        string message = Helper.NeutralFormat("Added {{{0}}} ({1}) data.",
                            obj.Value.Token.ToString(), obj.Key);
                        errorSet.Add(new Error(DataCompilerError.CompilingLog, message));
                    }

                    // Save as binary file
                    Helper.EnsureFolderExistForFile(fileName);
                    langDataFile.Save(fileName);
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Set the object for internal usage.
        /// </summary>
        /// <param name="name">The name string.</param>
        /// <param name="obj">The obj objetc.</param>
        public void SetObject(string name, object obj)
        {
            Helper.ThrowIfNull(_dataHandlerList);
            _dataHandlerList.SetObject(name, obj);
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Get the data object according to the name.
        /// </summary>
        /// <param name="name">Data name.</param>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>Object.</returns>
        internal object GetObject(string name, ErrorSet errorSet)
        {
            Debug.Assert(_dataHandlerList != null, "DataHandlerList is null");
            return _dataHandlerList.GetObject(name, errorSet);
        }

        #endregion

        #region private methods
        /// <summary>
        /// ReportComiledFiles.
        /// </summary>
        /// <param name="compileType">CompileType.</param>
        /// <param name="compiledFiles">CompiledFiles.</param>
        /// <returns>Error.</returns>
        private static Error ReportCompiledFiles(string compileType,
            Collection<string> compiledFiles)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < compiledFiles.Count; i++)
            {
                string compiledFile = compiledFiles[i];
                if (sb.Length == 0)
                {
                    sb.AppendLine();
                    sb.AppendLine(Helper.NeutralFormat(
                        "The following {0} files are compiled to binary:",
                        compileType));
                }

                sb.Append("\t" + compiledFile);
                if (i < compiledFiles.Count - 1)
                {
                    sb.AppendLine();
                }
            }

            return new Error(DataCompilerError.CompilingLogWithDataName, compileType, sb.ToString());
        }

        /// <summary>
        /// Merge compiling error into main error set.
        /// </summary>
        /// <param name="mainErrorSet">Main error set.</param>
        /// <param name="subErrorSet">Sub error set.</param>
        /// <param name="dataName">Data name.</param>
        private static void MergeDependencyError(ErrorSet mainErrorSet, ErrorSet subErrorSet, string dataName)
        {
            if (mainErrorSet == null)
            {
                throw new ArgumentNullException("mainErrorSet");
            }

            if (subErrorSet == null)
            {
                throw new ArgumentNullException("subErrorSet");
            }

            if (string.IsNullOrEmpty(dataName))
            {
                throw new ArgumentNullException("dataName");
            }

            if (subErrorSet.Contains(ErrorSeverity.MustFix))
            {
                mainErrorSet.Add(DataCompilerError.DependenciesNotValid, dataName);
            }

            foreach (Error error in subErrorSet.Errors)
            {
                if (error.Severity == ErrorSeverity.MustFix)
                {
                    mainErrorSet.Add(DataCompilerError.CompilingLogWithError, dataName, error.ToString());
                }
                else if (error.Severity == ErrorSeverity.Warning)
                {
                    mainErrorSet.Add(DataCompilerError.CompilingLogWithWarning, dataName, error.ToString());
                }
                else if (error.Severity == ErrorSeverity.NoError)
                {
                    mainErrorSet.Add(DataCompilerError.CompilingLogWithDataName, dataName, error.ToString());
                }
            }
        }

        /// <summary>
        /// Add compiling log into error set.
        /// </summary>
        /// <param name="errorSet">Error set.</param>
        /// <param name="dataName">Data name.</param>
        /// <param name="message">Message.</param>
        /// <param name="exitCode">Exit code.</param>
        private static void AddCompilingLog(ErrorSet errorSet, string dataName, string message,
            int exitCode)
        {
            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            if (!string.IsNullOrEmpty(message))
            {
                if (exitCode == 0)
                {
                    errorSet.Add(DataCompilerError.CompilingLogWithDataName, dataName, message);
                }
                else
                {
                    errorSet.Add(DataCompilerError.CompilingLogWithError, dataName, message);
                }
            }
        }

        /// <summary>
        /// Check the file path of raw data whether existed.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <param name="errorSet">Error set.</param>
        private static void CheckRawDataExists(string path, ErrorSet errorSet)
        {
            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            if (!File.Exists(path))
            {
                errorSet.Add(DataCompilerError.RawDataNotFound, Path.GetFileName(path), path);
            }
        }

        /// <summary>
        /// Check the file path of tool whether existed.
        /// </summary>
        /// <param name="path">File path of tool.</param>
        /// <param name="errorSet">Error set.</param>
        private static void CheckToolExists(string path, ErrorSet errorSet)
        {
            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            if (!File.Exists(path))
            {
                errorSet.Add(DataCompilerError.ToolNotFound, Path.GetFileName(path), path);
            }
        }

        /// <summary>
        /// Handle the command line and generate the output.
        /// </summary>
        /// <param name="processingName">Processing name.</param>
        /// <param name="command">Command.</param>
        /// <param name="arguments">Arguments.</param>
        /// <param name="outputPath">Output path.</param>
        /// <param name="outputStream">Output stream.</param>
        /// <param name="errorSet">Error set.</param>
        private static void HandleCommandLine(string processingName, string command, string arguments,
            string outputPath, Stream outputStream, ErrorSet errorSet)
        {
            Debug.Assert(errorSet != null);
            Debug.Assert(outputStream != null);
            Debug.Assert(!string.IsNullOrEmpty(processingName));
            Debug.Assert(!string.IsNullOrEmpty(command));
            Debug.Assert(!string.IsNullOrEmpty(arguments));
            Debug.Assert(!string.IsNullOrEmpty(outputPath));

            string message = string.Empty;
            int exitCode = CommandLine.RunCommandWithOutputAndError(
                command, arguments, Directory.GetCurrentDirectory(), ref message);
            if (!string.IsNullOrEmpty(message))
            {
                AddCompilingLog(errorSet, processingName, message, exitCode);
            }

            if (exitCode == 0)
            {
                LoadStream(outputPath, outputStream);
                File.Delete(outputPath);
            }
        }

        /// <summary>
        /// Ensure the necessary data is in the data objects.
        /// </summary>
        /// <param name="dataObjects">DataObjects.</param>
        /// <returns>Error Set.</returns>
        private ErrorSet EnsureNecessaryData(Dictionary<string, LangDataObject> dataObjects)
        {
            ErrorSet errorSet = new ErrorSet();
            string[] necessaryModuleDataNames = new string[] 
            {
                ModuleDataName.PhoneSet,
                ModuleDataName.PosTaggerPos,
                ModuleDataName.Lexicon,
                ModuleDataName.CharTable,
                ModuleDataName.UnitGenerator
            };

            foreach (string moduleDataName in necessaryModuleDataNames)
            {
                if (!_moduleDataSet.ContainsKey(moduleDataName))
                {
                    Error error = new Error(DataCompilerError.NecessaryDataMissing, moduleDataName);
                    errorSet.Add(error);
                    errorSet.Merge(Compile(moduleDataName));
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Compile the foreign LTS collection.
        /// </summary>
        /// <param name="configuration">Foreign LTS configuration.</param>
        /// <param name="outputStream">Output steam.</param>
        /// <returns>Error set.</returns>
        private ErrorSet CompileForeignLtsCollection(string configuration, Stream outputStream)
        {
            ErrorSet errorSet = new ErrorSet();

            // The configuration is written in
            // "originLanguageA : phonesetA ; RuleA ; originLanguageB: phonesetB ; RuleB"
            string[] phonesetLtsList = configuration.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            ushort count = Convert.ToUInt16(phonesetLtsList.Length / 2);
            Offline.Language[] languages = new Offline.Language[count];
            TtsPhoneSet[] phoneSets = new TtsPhoneSet[count];
            string[] ltsPaths = new string[count];

            // Load the phone sets
            for (ushort i = 0; i < count; i++)
            {
                languages[i] = Offline.Language.Neutral;
                string phoneSetPath = phonesetLtsList[i * 2].Trim();
                int languageSeparatorIndex = phoneSetPath.IndexOf(":");
                if (languageSeparatorIndex != -1)
                {
                    string language = phoneSetPath.Substring(0, languageSeparatorIndex).Trim();
                    languages[i] = Localor.StringToLanguage(language);
                    phoneSetPath = phoneSetPath.Substring(languageSeparatorIndex + 1, phoneSetPath.Length - languageSeparatorIndex - 1).Trim();
                }

                if (!Path.IsPathRooted(phoneSetPath))
                {
                    phoneSetPath = Path.Combine(_dataHandlerList.DataRoot, phoneSetPath);
                }

                phoneSets[i] = new TtsPhoneSet();
                phoneSets[i].Load(phoneSetPath);
                phoneSets[i].Validate();
                if (languages[i] == Offline.Language.Neutral)
                {
                    languages[i] = phoneSets[i].Language;
                }

                errorSet.Merge(phoneSets[i].ErrorSet);
                if (phoneSets[i].ErrorSet.Contains(ErrorSeverity.MustFix))
                {
                    phoneSets[i] = null;
                }
                else
                {
                    ltsPaths[i] = phonesetLtsList[(i * 2) + 1].Trim();
                    if (!Path.IsPathRooted(ltsPaths[i]))
                    {
                        ltsPaths[i] = Path.Combine(_dataHandlerList.DataRoot, ltsPaths[i]);
                    }
                }
            }

            if (!errorSet.Contains(ErrorSeverity.MustFix))
            {
                BinaryWriter bw = new BinaryWriter(outputStream);
                {
                    bw.Write((ushort)count);
                    for (ushort i = 0; i < count; i++)
                    {
                        bw.Write((ushort)languages[i]);
                        bw.Write((ushort)phoneSets[i].Language);
                    }

                    // Write phone set offset
                    long phoneSetOffset = bw.BaseStream.Position;
                    for (byte i = 0; i < count; i++)
                    {
                        bw.Write((uint)0);
                    }

                    // Write LTS offset
                    long ltsOffset = bw.BaseStream.Position;
                    for (byte i = 0; i < count; i++)
                    {
                        bw.Write((uint)0);
                    }

                    // Write phone set
                    for (byte i = 0; i < count; i++)
                    {
                        long offset = bw.BaseStream.Position;
                        bw.BaseStream.Seek(phoneSetOffset, SeekOrigin.Begin);
                        if (offset > uint.MaxValue)
                        {
                            throw new InvalidDataException(Helper.NeutralFormat(
                                "Foreign LTS collection size exceeds the maximal size {0}", uint.MaxValue));
                        }

                        bw.Write((uint)offset);
                        phoneSetOffset += sizeof(uint);
                        bw.BaseStream.Seek(offset, SeekOrigin.Begin);
                        errorSet.Merge(PhoneSetCompiler.Compile(phoneSets[i], bw.BaseStream));
                    }

                    // Write LTS
                    for (byte i = 0; i < count; i++)
                    {
                        long offset = bw.BaseStream.Position;
                        bw.BaseStream.Seek(ltsOffset, SeekOrigin.Begin);
                        if (offset > uint.MaxValue)
                        {
                            throw new InvalidDataException(Helper.NeutralFormat(
                                "Foreign LTS collection size exceeds the maximal size {0}", uint.MaxValue));
                        }

                        bw.Write((uint)offset);
                        ltsOffset += sizeof(uint);
                        bw.BaseStream.Seek(offset, SeekOrigin.Begin);
                        LoadStream(ltsPaths[i], bw.BaseStream);
                    }
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Compile Lexicon.
        /// </summary>
        /// <param name="outputStream">Output Stream.</param>
        /// <returns>ErrorSet.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Ignore.")]
        private ErrorSet CompileLexicon(Stream outputStream)
        {
            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            ErrorSet errorSet = new ErrorSet();

            ErrorSet subErrorSet = new ErrorSet();
            LexicalAttributeSchema schema = (LexicalAttributeSchema)GetObject(
                RawDataName.LexicalAttributeSchema, subErrorSet);
            MergeDependencyError(errorSet, subErrorSet, _schemaFullName);

            subErrorSet.Clear();
            TtsPhoneSet phoneSet = (TtsPhoneSet)GetObject(RawDataName.PhoneSet, subErrorSet);
            MergeDependencyError(errorSet, subErrorSet, RawDataName.PhoneSet);

            if (!errorSet.Contains(ErrorSeverity.MustFix))
            {
                Microsoft.Tts.Offline.Core.Lexicon lexicon = (Microsoft.Tts.Offline.Core.Lexicon)GetObject(RawDataName.Lexicon, errorSet);
                errorSet.Merge(lexicon.ErrorSet);

                // Change to case insensitive lexicon
                MemoryStream lexiconStream = new MemoryStream();
                using (XmlWriter xmlWriter = XmlWriter.Create(lexiconStream))
                {
                    Microsoft.Tts.Offline.Core.Lexicon.ContentControler lexiconControler = 
                        new Microsoft.Tts.Offline.Core.Lexicon.ContentControler();
                    lexiconControler.IsCaseSensitive = true;
                    lexicon.Save(xmlWriter, lexiconControler);
                }

                lexiconStream.Seek(0, SeekOrigin.Begin);
                Microsoft.Tts.Offline.Core.Lexicon caseInsensitiveLexicon = new Microsoft.Tts.Offline.Core.Lexicon();
                using (StreamReader sr = new StreamReader(lexiconStream))
                {
                    caseInsensitiveLexicon.Load(sr);
                }

                if (caseInsensitiveLexicon != null && !errorSet.Contains(ErrorSeverity.MustFix))
                {
                    caseInsensitiveLexicon.LexicalAttributeSchema = schema;

                    caseInsensitiveLexicon.PhoneSet = phoneSet;
                    caseInsensitiveLexicon.Validate();

                    // Set severity of errors only in case-insensitive lexicon to NoError for they're not treated as real error
                    caseInsensitiveLexicon.ErrorSet.SetSeverity(ErrorSeverity.NoError);

                    string vendorLexiconPath = Helper.GetTempFileName();

                    caseInsensitiveLexicon.SaveToVendorLexicon(vendorLexiconPath);

                    string toolFileName = ToolName.BldVendor2;
                    string binaryLexiconPath = Helper.GetTempFileName();

                    string compilingArguments = Helper.NeutralFormat("-v {0} V2 \"{1}\" \"{2}\" \"{3}\" TTS",
                        (int)_language, _dataHandlerList.Datas[RawDataName.LexicalAttributeSchema].Path,
                        vendorLexiconPath, binaryLexiconPath);
                    string toolPath = Path.Combine(ToolDir, toolFileName);

                    CheckToolExists(toolPath, errorSet);
                    if (!errorSet.Contains(ErrorSeverity.MustFix))
                    {
                        HandleCommandLine(ModuleDataName.Lexicon, toolPath, compilingArguments,
                            binaryLexiconPath, outputStream, errorSet);
                    }

                    File.Delete(vendorLexiconPath);

                    errorSet.Merge(caseInsensitiveLexicon.ErrorSet);
                }
                else if (lexicon == null)
                {
                    errorSet.Add(DataCompilerError.RawDataError, "Lexicon");
                }
                else
                {
                    errorSet.Merge(caseInsensitiveLexicon.ErrorSet);
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Compile LTS rule.
        /// </summary>
        /// <param name="ltsDataDir">Directory of LTS data.</param>
        /// <param name="outputStream">Output stream.</param>
        /// <returns>Error Set.</returns>
        private ErrorSet CompileLtsRule(string ltsDataDir, Stream outputStream)
        {
            ErrorSet errorSet = new ErrorSet();
            string toolFileName = ToolName.LtsCompiler;
            string letterSymPath = Path.Combine(ltsDataDir, "letter.sym");
            string phoneSymPath = Path.Combine(ltsDataDir, "phone.sym");
            string letterQPath = Path.Combine(ltsDataDir, "letter.q");
            string phoneQPath = Path.Combine(ltsDataDir, "phone.q");
            string ltsTreePath = Path.Combine(ltsDataDir, "tree.tree");
            string trainSmpPath = Path.Combine(ltsDataDir, "train.smp");
            CheckRawDataExists(letterSymPath, errorSet);
            CheckRawDataExists(phoneSymPath, errorSet);
            CheckRawDataExists(letterQPath, errorSet);
            CheckRawDataExists(phoneQPath, errorSet);
            CheckRawDataExists(ltsTreePath, errorSet);
            CheckRawDataExists(trainSmpPath, errorSet);

            string binaryLtsPath = Helper.GetTempFileName();
            string compilingArguments = Helper.NeutralFormat(
                "\"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\" {5} {6} {7} \"{8}\" \"{9}\"",
                letterSymPath, phoneSymPath, letterQPath, phoneQPath, ltsTreePath,
                0, 0, 0.00000001,
                trainSmpPath, binaryLtsPath);
            string toolPath = Path.Combine(ToolDir, toolFileName);
            CheckToolExists(toolPath, errorSet);

            if (!errorSet.Contains(ErrorSeverity.MustFix))
            {
                HandleCommandLine(ModuleDataName.LtsRule, toolPath, compilingArguments,
                    binaryLtsPath, outputStream, errorSet);
            }

            return errorSet;
        }

        /// <summary>
        /// Tnml rule compiler.
        /// </summary>
        /// <param name="tnmlRuleFilePath">Path of Tnml rule.</param>
        /// <param name="schemaFilePath">Path of Lexical Attribute Schema.</param>
        /// <param name="outputStream">Output stream.</param>
        /// <param name="isTNRule">Whether it's TN rule.</param>
        /// <returns>ErrorSet.</returns>
        private ErrorSet CompileTnml(string tnmlRuleFilePath, string schemaFilePath, Stream outputStream, bool isTNRule)
        {
            ErrorSet errorSet = new ErrorSet();
            string toolFileName = ToolName.TnmlCompiler;
            string binaryTnmlRulePath = Helper.GetTempFileName();
            string compilingArguments = Helper.NeutralFormat(
                "-lcid {0} -tnml \"{1}\" -schema \"{2}\" -tnbin \"{3}\"",
                (uint)_language, tnmlRuleFilePath, schemaFilePath, binaryTnmlRulePath);
            if (isTNRule)
            {
                compilingArguments += " -mode TTS -norulename FALSE";
            }

            string toolPath = Path.Combine(ToolDir, toolFileName);

            CheckToolExists(toolPath, errorSet);
            if (!File.Exists(tnmlRuleFilePath))
            {
                errorSet.Add(DataCompilerError.RawDataNotFound, "TNML rule", tnmlRuleFilePath);
            }

            if (!File.Exists(schemaFilePath))
            {
                errorSet.Add(DataCompilerError.RawDataNotFound, RawDataName.LexicalAttributeSchema, schemaFilePath);
            }

            if (!errorSet.Contains(ErrorSeverity.MustFix))
            {
                HandleCommandLine("TNML rule", toolPath, compilingArguments,
                    binaryTnmlRulePath, outputStream, errorSet);
            }

            return errorSet;
        }

        /// <summary>
        /// FstNE rule compiler.
        /// </summary>
        /// <param name="fstNERuleFilePath">Path of FstNE rule.</param>
        /// <param name="outputStream">Output stream.</param>
        /// <returns>ErrorSet.</returns>
        private ErrorSet CompileFstNE(string fstNERuleFilePath, Stream outputStream)
        {
            ErrorSet errorSet = new ErrorSet();
            string toolFileName = ToolName.FstNECompiler;
            string binaryFstNERulePath = Helper.GetTempFileName();

            string compilingArguments = Helper.NeutralFormat(
                "-lang {0} -intnml \"{1}\" -outfst \"{2}\"",
                Localor.LanguageToString(_language), fstNERuleFilePath, binaryFstNERulePath);

            string toolPath = Path.Combine(ToolDir, toolFileName);

            CheckToolExists(toolPath, errorSet);
            if (!File.Exists(fstNERuleFilePath))
            {
                errorSet.Add(DataCompilerError.RawDataNotFound, "FstNE rule", fstNERuleFilePath);
            }

            if (!errorSet.Contains(ErrorSeverity.MustFix))
            {
                HandleCommandLine("FstNE rule", toolPath, compilingArguments,
                    binaryFstNERulePath, outputStream, errorSet);
            }

            return errorSet;
        }

        /// <summary>
        /// Handle the command line and generate the output.
        /// </summary>
        /// <param name="lexicalRuleFilePath">Path of POS Lexical rule.</param>
        /// <param name="contextualRuleFilePath">Path of POS Contectual Rule.</param>
        /// <param name="posSetFilePath">Path of POS set.</param>
        /// <param name="outputStream">Output stream.</param>
        /// <returns>ErrorSet.</returns>
        private ErrorSet CompilePosRule(string lexicalRuleFilePath, string contextualRuleFilePath,
            string posSetFilePath, Stream outputStream)
        {
            ErrorSet errorSet = new ErrorSet();
            string toolFileName = ToolName.PosRuleCompiler;
            string binaryPosRulePath = Helper.GetTempFileName();
            string compilingArguments = Helper.NeutralFormat("\"{0}\" \"{1}\" \"{2}\" \"{3}\"",
                lexicalRuleFilePath, contextualRuleFilePath, posSetFilePath, binaryPosRulePath);
            string toolPath = Path.Combine(ToolDir, toolFileName);

            CheckToolExists(toolPath, errorSet);
            if (!File.Exists(lexicalRuleFilePath))
            {
                errorSet.Add(DataCompilerError.RawDataNotFound, RawDataName.PosLexicalRule, lexicalRuleFilePath);
            }
            
            if (!File.Exists(contextualRuleFilePath))
            {
                errorSet.Add(DataCompilerError.RawDataNotFound, RawDataName.PosContextualRule, contextualRuleFilePath);
            }
            
            if (!File.Exists(posSetFilePath))
            {
                errorSet.Add(DataCompilerError.RawDataNotFound, RawDataName.PosSet, posSetFilePath);
            }

            if (!errorSet.Contains(ErrorSeverity.MustFix))
            {
                HandleCommandLine(ModuleDataName.PosRule, toolPath, compilingArguments,
                    binaryPosRulePath, outputStream, errorSet);
            }

            return errorSet;
        }

        /// <summary>
        /// General Rule Compiler.
        /// </summary>
        /// <param name="txtPath">Path of txt formatted general rule.</param>
        /// <param name="outputStream">Output stream.</param>
        /// <returns>ErrorSet.</returns>
        private ErrorSet CompileGeneralRule(string txtPath, Stream outputStream)
        {
            ErrorSet errorSet = new ErrorSet();
            string toolFileName = ToolName.RuleCompiler;
            string binaryRulePath = Helper.GetTempFileName();
            string compilingArguments = Helper.NeutralFormat("\"{0}\" \"{1}\"", txtPath, binaryRulePath);
            string toolPath = Path.Combine(ToolDir, toolFileName);
            const string DataName = "General Rule";
            CheckToolExists(toolPath, errorSet);
            
            if (!File.Exists(txtPath))
            {
                errorSet.Add(DataCompilerError.RawDataNotFound, DataName, txtPath);
            }
            
            if (!errorSet.Contains(ErrorSeverity.MustFix))
            {
                HandleCommandLine(DataName, toolPath, compilingArguments,
                    binaryRulePath, outputStream, errorSet);
            }

            return errorSet;
        }

        /// <summary>
        /// Char table compiler.
        /// </summary>
        /// <param name="outputStream">Output Stream.</param>
        /// <returns>ErrorSet.</returns>
        private ErrorSet CompileCharTable(Stream outputStream)
        {
            ErrorSet errorSet = new ErrorSet();

            try
            {
                CharTable charTable = (CharTable)GetObject(RawDataName.CharTable, errorSet);

                ChartableValidator charTableValidator = new ChartableValidator();
                Microsoft.Tts.Offline.Core.Lexicon lexicon = (Microsoft.Tts.Offline.Core.Lexicon)GetObject(RawDataName.Lexicon, errorSet);
                TtsPhoneSet phoneSet = (TtsPhoneSet)GetObject(RawDataName.PhoneSet, errorSet);
                if (!errorSet.Contains(ErrorSeverity.MustFix))
                {
                    charTableValidator.Lexicon = lexicon;
                    charTableValidator.PhoneSet = phoneSet;
                    charTableValidator.EnsureInitialized();

                    if (charTable.Language != charTableValidator.Language)
                    {
                        throw new InvalidDataException("chartable language should match with lexicon or phoneset");
                    }

                    ErrorSet charTableErrors = charTableValidator.Validate(charTable, false, null);
                    foreach (Error error in charTableErrors.Errors)
                    {
                        if (error.Severity == ErrorSeverity.MustFix)
                        {
                            errorSet.Add(DataCompilerError.CompilingLogWithError,
                                RawDataName.CharTable, error.ToString());
                        }
                        else
                        {
                            errorSet.Add(DataCompilerError.CompilingLogWithWarning,
                                RawDataName.CharTable, error.ToString());
                        }
                    }

                    errorSet.Merge(CharTableCompiler.Compile(charTable, phoneSet, outputStream));
                }
            }
            catch (XmlException e)
            {
                errorSet.Add(DataCompilerError.RawDataError, e.Message);
            }

            return errorSet;
        }

        #endregion
    }

    /// <summary>
    /// Use this class to sort the lang data object.
    /// </summary>
    public class CompareLangDataObject : IComparer
    {
        /// <summary>
        /// Compare the LangDataObject.
        /// </summary>
        /// <param name="x">DataObject to compare with.</param>
        /// <param name="y">Other dataObject to compare with.</param>
        /// <returns>Comparing result.</returns>
        public int Compare(object x, object y)
        {
            KeyValuePair<string, LangDataObject> a = (KeyValuePair<string, LangDataObject>)x;
            KeyValuePair<string, LangDataObject> b = (KeyValuePair<string, LangDataObject>)y;

            int ret = StringComparer.OrdinalIgnoreCase.Compare(a.Value.Token.ToString(),
                b.Value.Token.ToString());

            return ret;
        }
    }

    /// <summary>
    /// A wrapper for the PhoneConverter.
    /// </summary>
    internal class PhoneConverterWrapper : IPhoneConverter, IDisposable
    {
        private SP::PhoneConverter _phoneConverter;

        public PhoneConverterWrapper(Language lang, byte[] data)
        {
            _phoneConverter = new SP::PhoneConverter(Convert.ToUInt16(lang), data);
        }

        public string TTS2SAPI(string ttsPhone)
        {
            return _phoneConverter.TTS2SAPI(ttsPhone);
        }

        public string TTS2UPS(string ttsPhone)
        {
            return _phoneConverter.TTS2UPS(ttsPhone);
        }

        #region IDisposable Members

        /// <summary>
        /// Disposes the resources used in this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the RewindableTextReader.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources;
        /// False to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != _phoneConverter)
                {
                    _phoneConverter.Dispose();
                }
            }
        }

        #endregion
    }
}