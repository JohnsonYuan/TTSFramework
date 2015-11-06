//----------------------------------------------------------------------------
// <copyright file="UnitFeatureFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements class UnitFeatureFile
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Config;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// Unit feature file.
    /// </summary>
    public class UnitFeatureFile
    {
        #region Fields

        private static XmlSchema _schema;
        private string _filePath;
        private Language _language;

        private SortedDictionary<string, UnitFeature> _units =
            new SortedDictionary<string, UnitFeature>();

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitFeatureFile"/> class.
        /// </summary>
        public UnitFeatureFile()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitFeatureFile"/> class.
        /// </summary>
        /// <param name="filePath">Unit feature file path.</param>
        public UnitFeatureFile(string filePath)
        {
            Load(filePath);
        }

        #endregion

        #region Deletegates

        /// <summary>
        /// Dictionary key.
        /// </summary>
        /// <typeparam name="T1">Type used by dictionary.</typeparam>
        /// <param name="t">Dictionary.</param>
        /// <returns>Key string.</returns>
        private delegate string GetKey<T1>(T1 t);

        /// <summary>
        /// Dictionary value.
        /// </summary>
        /// <typeparam name="T1">Type used by dictionary.</typeparam>
        /// <typeparam name="T2">Tyep used by dictionary value.</typeparam>
        /// <param name="t">Dictionary.</param>
        /// <returns>Value.</returns>
        private delegate T2 GetValue<T1, T2>(T1 t);

        /// <summary>
        /// Set value.
        /// </summary>
        /// <typeparam name="T1">Type for target variable.</typeparam>
        /// <typeparam name="T2">Type for input variable.</typeparam>
        /// <param name="t1">Target variable.</param>
        /// <param name="t2">Input variable.</param>
        private delegate void SetValue<T1, T2>(T1 t1, T2 t2);

        #endregion

        #region Properties

        /// <summary>
        /// Gets Unit feature schema.
        /// </summary>
        public static XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.IO.UnitFeatureFile.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets or sets Unit feature file path.
        /// </summary>
        public string FilePath
        {
            get
            {
                return _filePath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _filePath = value;
            }
        }

        /// <summary>
        /// Gets or sets Language.
        /// </summary>
        public Language Language
        {
            get { return _language; }
            set { _language = value; }
        }

        /// <summary>
        /// Gets Unit feature collection.
        /// </summary>
        public SortedDictionary<string, UnitFeature> Units
        {
            get { return _units; }
        }

        /// <summary>
        /// Gets Unit feature collection indexed by sentence id.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Ignore.")]
        public SortedDictionary<string, List<UnitFeature>> SentIndexedUnits
        {
            get
            {
                // Use sentence id as index
                return ToIndexed<UnitFeature, UnitFeature>(_units.Values,
                    delegate(UnitFeature unit)
                    {
                        if (unit == null)
                        {
                            throw new ArgumentNullException("unit");
                        }

                        return unit.SentenceId;
                    },
                    delegate(UnitFeature unit)
                    {
                        return unit;
                    });
            }
        }

        /// <summary>
        /// Gets Unit feature collection indexed by unit name.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Ignore.")]
        public SortedDictionary<string, List<UnitFeature>> NameIndexedUnits
        {
            get
            {
                // Use unit name as index
                return ToIndexed<UnitFeature, UnitFeature>(_units.Values,
                    delegate(UnitFeature unit) { return unit.Name; },
                    delegate(UnitFeature unit) { return unit; });
            }
        }

        #endregion

        #region Static Operations

        /// <summary>
        /// Extract all linguistic and acoustic features.
        /// </summary>
        /// <param name="script">Script file.</param>
        /// <param name="fileMapFilePath">File list map path.</param>
        /// <param name="segmentDir">Alignment directory.</param>
        /// <param name="wave16kDir">16 kHz waveform directory.</param>
        /// <param name="epochDir">Epoch directory.</param>
        /// <param name="featureFile">Unit feature file.</param>
        /// <returns>Error information set.</returns>
        public static DataErrorSet ExtractFeature(ScriptFile script,
            string fileMapFilePath, string segmentDir, string wave16kDir,
            string epochDir, UnitFeatureFile featureFile)
        {
            if (string.IsNullOrEmpty(fileMapFilePath))
            {
                throw new ArgumentNullException("fileMapFilePath");
            }

            FileListMap fileMap = new FileListMap();
            fileMap.Load(fileMapFilePath);

            return ExtractFeature(script, fileMap, segmentDir, wave16kDir,
                epochDir, featureFile);
        }

        /// <summary>
        /// Extract all linguistic and acoustic features.
        /// </summary>
        /// <param name="script">Script file.</param>
        /// <param name="fileMap">File list map.</param>
        /// <param name="segmentDir">Alignment directory.</param>
        /// <param name="wave16kDir">16 kHz waveform directory.</param>
        /// <param name="epochDir">Epoch directory.</param>
        /// <param name="units">Unit feature collection.</param>
        /// <returns>Error information set.</returns>
        public static DataErrorSet ExtractFeature(ScriptFile script,
            FileListMap fileMap, string segmentDir, string wave16kDir,
            string epochDir, SortedDictionary<string, UnitFeature> units)
        {
            DataErrorSet errorSet = ExtractLinguistic(script, units);

            DataErrorSet subErrors = ExtractAcoustic(script, fileMap, segmentDir,
                wave16kDir, epochDir, units);
            errorSet.Merge(subErrors);

            return errorSet;
        }

        /// <summary>
        /// Extract all linguistic and acoustic features.
        /// </summary>
        /// <param name="script">Script file.</param>
        /// <param name="fileMap">File list map.</param>
        /// <param name="segmentDir">Alignment directory.</param>
        /// <param name="wave16kDir">16 kHz waveform directory.</param>
        /// <param name="epochDir">Epoch directory.</param>
        /// <param name="featureFile">Unit feature file.</param>
        /// <returns>Error information set.</returns>
        public static DataErrorSet ExtractFeature(ScriptFile script,
            FileListMap fileMap, string segmentDir, string wave16kDir,
            string epochDir, UnitFeatureFile featureFile)
        {
            if (featureFile == null)
            {
                throw new ArgumentNullException("featureFile");
            }

            return ExtractFeature(script, fileMap, segmentDir, wave16kDir,
                epochDir, featureFile.Units);
        }

        /// <summary>
        /// Extract all linguistic and acoustic features.
        /// </summary>
        /// <param name="scriptFilePath">Script file path.</param>
        /// <param name="language">Language.</param>
        /// <param name="engine">Engine type.</param>
        /// <param name="fileMapFilePath">File list map path.</param>
        /// <param name="segmentDir">Alignment directory.</param>
        /// <param name="wave16kDir">16 kHz waveform directory.</param>
        /// <param name="epochDir">Epoch directory.</param>
        /// <param name="featureFile">Unit feature file.</param>
        /// <returns>Error information set.</returns>
        public static DataErrorSet ExtractFeature(string scriptFilePath, Language language,
            EngineType engine, string fileMapFilePath, string segmentDir, string wave16kDir,
            string epochDir, UnitFeatureFile featureFile)
        {
            if (string.IsNullOrEmpty(scriptFilePath))
            {
                throw new ArgumentNullException("scriptFilePath");
            }

            if (string.IsNullOrEmpty(fileMapFilePath))
            {
                throw new ArgumentNullException("fileMapFilePath");
            }

            ScriptFile script = Localor.CreateScriptFile(language, engine);
            script.Load(scriptFilePath);
            System.Diagnostics.Debug.Assert(script.ErrorSet.Errors.Count == 0);
            if (script.ErrorSet.Errors.Count > 0)
            {
                return script.ErrorSet;
            }

            FileListMap fileMap = new FileListMap();
            fileMap.Load(fileMapFilePath);

            return ExtractFeature(script, fileMap, segmentDir, wave16kDir,
                epochDir, featureFile);
        }

        /// <summary>
        /// Extract all linguistic and acoustic features.
        /// </summary>
        /// <param name="scriptFilePath">Script file path.</param>
        /// <param name="language">Language.</param>
        /// <param name="engine">Engine type.</param>
        /// <param name="fileMapFilePath">File list map path.</param>
        /// <param name="segmentDir">Alignment directory.</param>
        /// <param name="wave16kDir">16 kHz waveform directory.</param>
        /// <param name="epochDir">Epoch directory.</param>
        /// <param name="unitFeatureFilePath">Unit feature file path.</param>
        /// <returns>Error information set.</returns>
        public static DataErrorSet ExtractFeature(string scriptFilePath, Language language,
            EngineType engine, string fileMapFilePath, string segmentDir, string wave16kDir,
            string epochDir, string unitFeatureFilePath)
        {
            if (string.IsNullOrEmpty(scriptFilePath))
            {
                throw new ArgumentNullException("scriptFilePath");
            }

            if (string.IsNullOrEmpty(fileMapFilePath))
            {
                throw new ArgumentNullException("fileMapFilePath");
            }

            if (string.IsNullOrEmpty(unitFeatureFilePath))
            {
                throw new ArgumentNullException("unitFeatureFilePath");
            }

            ScriptFile script = Localor.CreateScriptFile(language, engine);
            script.Load(scriptFilePath);
            
            FileListMap fileMap = new FileListMap();
            fileMap.Load(fileMapFilePath);

            UnitFeatureFile featureFile = new UnitFeatureFile();
            DataErrorSet errorSet = ExtractFeature(script, fileMap, segmentDir, wave16kDir,
                epochDir, featureFile);
            featureFile.Save(unitFeatureFilePath, language);

            return errorSet;
        }

        /// <summary>
        /// Extract linguistic feature.
        /// </summary>
        /// <param name="script">Script file.</param>
        /// <param name="units">Unit feature collection.</param>
        /// <returns>Error information set.</returns>
        public static DataErrorSet ExtractLinguistic(ScriptFile script,
            SortedDictionary<string, UnitFeature> units)
        {
            // Validate Parameters
            if (script == null)
            {
                throw new ArgumentNullException("script");
            }

            if (script.Items == null)
            {
                throw new ArgumentException("script.Items is null");
            }

            if (script.Items.Keys == null)
            {
                throw new ArgumentException("script.Items.Keys is null");
            }

            if (units == null)
            {
                throw new ArgumentNullException("units");
            }

            DataErrorSet errorSet = new DataErrorSet();

            foreach (string sid in script.Items.Keys)
            {
                try
                {
                    ExtractLinguistic(script.Items[sid], units);
                }
                catch (InvalidDataException ide)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Failed to extract linguistic afeature for sentence [{0}]. {1}",
                        sid, Helper.BuildExceptionMessage(ide));
                    errorSet.Errors.Add(new DataError(script.FilePath, message, sid));
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Extract linguistic feature and save to unit feature file.
        /// </summary>
        /// <param name="script">Script file.</param>
        /// <param name="featureFile">Unit feature file.</param>
        /// <returns>Error information set.</returns>
        public static DataErrorSet ExtractLinguistic(ScriptFile script, UnitFeatureFile featureFile)
        {
            if (featureFile == null)
            {
                throw new ArgumentNullException("featureFile");
            }

            return ExtractLinguistic(script, featureFile.Units);
        }

        /// <summary>
        /// Extract acoustic feature.
        /// </summary>
        /// <param name="script">Script file.</param>
        /// <param name="fileMap">File list map.</param>
        /// <param name="segmentDir">Alignment directory.</param>
        /// <param name="wave16kDir">16 kHz waveform directory.</param>
        /// <param name="epochDir">Epoch directory.</param>
        /// <param name="units">Unit feature collections.</param>
        /// <returns>Error information set.</returns>
        public static DataErrorSet ExtractAcoustic(ScriptFile script, FileListMap fileMap,
            string segmentDir, string wave16kDir, string epochDir,
            SortedDictionary<string, UnitFeature> units)
        {
            // Validate Parameters
            if (script == null)
            {
                throw new ArgumentNullException("script");
            }

            if (script.Items == null)
            {
                throw new ArgumentException("script.Items is null");
            }

            if (script.Items.Keys == null)
            {
                throw new ArgumentException("script.Items.Keys is null");
            }

            if (fileMap == null)
            {
                throw new ArgumentNullException("fileMap");
            }

            if (fileMap.Map == null)
            {
                throw new ArgumentException("fileMap.Map is null");
            }

            if (fileMap.Map.Keys == null)
            {
                throw new ArgumentException("fileMap.Map.Keys is null");
            }

            if (units == null)
            {
                throw new ArgumentNullException("units");
            }

            if (string.IsNullOrEmpty(segmentDir))
            {
                throw new ArgumentNullException("segmentDir");
            }

            if (string.IsNullOrEmpty(wave16kDir))
            {
                throw new ArgumentNullException("wave16kDir");
            }

            if (string.IsNullOrEmpty(epochDir))
            {
                throw new ArgumentNullException("epochDir");
            }

            if (!Directory.Exists(segmentDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException),
                    segmentDir);
            }

            if (!Directory.Exists(wave16kDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException),
                    wave16kDir);
            }

            if (!Directory.Exists(epochDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException),
                    epochDir);
            }

            DataErrorSet errorSet = new DataErrorSet();

            foreach (string sid in script.Items.Keys)
            {
                try
                {
                    ExtractAcoustic(script.Items[sid], fileMap, segmentDir,
                        wave16kDir, epochDir, units);
                }
                catch (InvalidDataException ide)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Failed to extract acoustic feature for sentence [{0}]. {1}",
                        sid, Helper.BuildExceptionMessage(ide));
                    errorSet.Errors.Add(new DataError(script.FilePath,
                        message, sid));
                }
            }

            // Post Process
            // Some acoustic features need post processing, i.e. energy feature, epoch feature
            CalculateEnergyFeature(script.Language, units);

            CalculateEpochFeature(script.Language, units);

            return errorSet;
        }

        /// <summary>
        /// Extract acoustic feature.
        /// </summary>
        /// <param name="script">Script file.</param>
        /// <param name="fileMap">File list map.</param>
        /// <param name="segmentDir">Alignment directory.</param>
        /// <param name="wave16kDir">16 kHz waveform directory.</param>
        /// <param name="epochDir">Epoch directory.</param>
        /// <param name="featureFile">Unit feature file.</param>
        /// <returns>Error information set.</returns>
        public static DataErrorSet ExtractAcoustic(ScriptFile script, FileListMap fileMap,
            string segmentDir, string wave16kDir, string epochDir, UnitFeatureFile featureFile)
        {
            if (featureFile == null)
            {
                throw new ArgumentNullException("featureFile");
            }

            return ExtractAcoustic(script, fileMap, segmentDir, wave16kDir,
                epochDir, featureFile.Units);
        }

        /// <summary>
        /// Build domain info into unit feature.
        /// </summary>
        /// <param name="units">Unit feature collection.</param>
        /// <param name="domainConfig">Domain config.</param>
        /// <param name="language">Language.</param>
        public static void BuildDomainInfo(IDictionary<string, UnitFeature> units,
            DomainConfig domainConfig, Language language)
        {
            if (units == null)
            {
                throw new ArgumentNullException("units");
            }

            if (domainConfig == null)
            {
                throw new ArgumentNullException("domainConfig");
            }

            foreach (string key in units.Keys)
            {
                UnitFeature unit = units[key];

                int type = 0;
                foreach (ScriptDomain domainType in domainConfig.DomainLists.Keys)
                {
                    DomainConfigList domainList = domainConfig.DomainLists[domainType];

                    if (domainList.Contains(unit.SentenceId))
                    {
                        type |= (int)domainList.Domain;
                    }
                }

                if (type != 0)
                {
                    unit.Domain = type;
                }
            }

            // Update energy feature
            CalculateEnergyFeature(language, units);
        }

        /// <summary>
        /// Get units in given domain.
        /// </summary>
        /// <param name="units">Given unit collection.</param>
        /// <param name="domain">Given domain.</param>
        /// <returns>Units in given domain.</returns>
        public static SortedDictionary<string, UnitFeature> GetDomainUnits(
            SortedDictionary<string, UnitFeature> units, int domain)
        {
            if (units == null)
            {
                throw new ArgumentNullException("units");
            }

            SortedDictionary<string, UnitFeature> domainUnits = new SortedDictionary<string, UnitFeature>();
            foreach (string key in units.Keys)
            {
                UnitFeature unit = units[key];
                if ((domain & unit.Domain) != 0)
                {
                    if (domainUnits.ContainsKey(key))
                    {
                        string message = Helper.NeutralFormat(
                            "Find duplicate unit [{0}]",
                            unit.Id);
                        throw new InvalidDataException(message);
                    }

                    domainUnits.Add(key, unit);
                }
            }

            return domainUnits;
        }

        /// <summary>
        /// Build wave unit info feature file.
        /// </summary>
        /// <param name="featureFile">Unit feature file.</param>
        /// <param name="waveUnitInfoFilePath">Wave unit info file path.</param>
        public static void BuildWaveUnitInfo(UnitFeatureFile featureFile, string waveUnitInfoFilePath)
        {
            // Validate Parameters
            if (featureFile == null)
            {
                throw new ArgumentNullException("featureFile");
            }

            if (featureFile.Units == null)
            {
                throw new ArgumentException("featureFile.Units is null");
            }

            if (featureFile.Units.Keys == null)
            {
                throw new ArgumentException("featureFile.Units.Keys is null");
            }

            if (string.IsNullOrEmpty(waveUnitInfoFilePath))
            {
                throw new ArgumentNullException("waveUnitInfoFilePath");
            }

            using (StreamWriter wuiWriter = new StreamWriter(waveUnitInfoFilePath, false, Encoding.ASCII))
            {
                foreach (string key in featureFile.Units.Keys)
                {
                    UnitFeature unit = featureFile.Units[key];

                    wuiWriter.Write("{0,7}", unit.SentenceId);
                    wuiWriter.Write(" {0,3}", unit.Index);

                    // Linguistic items.
                    for (int i = 0; i <= (int)TtsFeature.TtsWordTone; i++)
                    {
                        wuiWriter.Write(" {0,2}", unit.LingusitcFeature[i]);
                    }

                    // Acoustic items.
                    wuiWriter.Write(" {0,8}", unit.AcousticFeature.SampleOffset);
                    wuiWriter.Write(" {0,6}", unit.AcousticFeature.SampleLength);
                    wuiWriter.Write(" {0,6}", unit.AcousticFeature.EpochOffset);
                    wuiWriter.Write(" {0,4}", unit.AcousticFeature.EpochLength);
                    wuiWriter.Write(" {0,4}", unit.AcousticFeature.Epoch16KCompressLength);
                    wuiWriter.Write(" {0,4}", unit.AcousticFeature.Epoch8KCompressLength);

                    wuiWriter.WriteLine(" {0,8}", unit.Name);
                }
            }
        }

        /// <summary>
        /// Build wave segment sequence file.
        /// </summary>
        /// <param name="featureFile">Unit feature file.</param>
        /// <param name="mapFilePath">File list file path.</param>
        /// <param name="waveSegmentSequenceFilePath">Wave segment file path.</param>
        public static void BuildWaveSegmentSequence(UnitFeatureFile featureFile, string mapFilePath, string waveSegmentSequenceFilePath)
        {
            // Validate Parameters
            if (featureFile == null)
            {
                throw new ArgumentNullException("featureFile");
            }

            if (featureFile.Units == null)
            {
                throw new ArgumentException("featureFile.Units is null");
            }

            if (featureFile.Units.Keys == null)
            {
                throw new ArgumentException("featureFile.Units.Keys is null");
            }

            if (string.IsNullOrEmpty(mapFilePath))
            {
                throw new ArgumentNullException("mapFilePath");
            }

            if (string.IsNullOrEmpty(waveSegmentSequenceFilePath))
            {
                throw new ArgumentNullException("waveSegmentSequenceFilePath");
            }

            Dictionary<string, string> maps = FileListMap.ReadAllData(mapFilePath);

            using (StreamWriter waveSequenceWriter = new StreamWriter(waveSegmentSequenceFilePath))
            {
                foreach (string key in featureFile.Units.Keys)
                {
                    UnitFeature unit = featureFile.Units[key];

                    TtsAcousticFeature acousFeature = unit.AcousticFeature;

                    waveSequenceWriter.Write("{0,7}", unit.SentenceId);
                    waveSequenceWriter.Write(" {0,3}", unit.Index);
                    waveSequenceWriter.Write(" {0,8}", acousFeature.SampleOffset);
                    waveSequenceWriter.Write(" {0,6}", acousFeature.SampleLength);
                    waveSequenceWriter.Write(" {0,6}", acousFeature.EpochOffset);
                    waveSequenceWriter.Write(" {0,4}", acousFeature.EpochLength);
                    waveSequenceWriter.Write(" {0,4}", acousFeature.Epoch16KCompressLength);
                    waveSequenceWriter.Write(" {0,4}", acousFeature.Epoch8KCompressLength);
                    waveSequenceWriter.Write(" {0,8}", maps[unit.SentenceId]);
                    waveSequenceWriter.WriteLine(" {0,8}", unit.Name);
                }
            }
        }

        /// <summary>
        /// Filter vector data according to units file.
        /// </summary>
        /// <param name="units">Unit feature collection.</param>
        /// <param name="language">Language.</param>
        /// <param name="filteringUnitsFile">Filtering unit file path.</param>
        /// <param name="unitListType">
        ///     UnitList.UnitListType.Hold: only units in the filteringUnitsFile will be kept
        ///     UnitList.UnitListType.Drop: only units in the filteringUnitsFile will be removed.
        /// </param>
        public static void FilterFeatureData(IDictionary<string, UnitFeature> units, Language language,
            string filteringUnitsFile, UnitList.UnitListType unitListType)
        {
            if (string.IsNullOrEmpty(filteringUnitsFile))
            {
                throw new ArgumentNullException("filteringUnitsFile");
            }

            UnitListDictionary unitListDict = new UnitListDictionary();
            unitListDict.Load(filteringUnitsFile);

            UnitList filteringUnitList =
                unitListDict.UnitListMap[UnitList.GetKey(language, unitListType)];
            FilterFeatureData(units, language, filteringUnitList);
        }

        /// <summary>
        /// Filter unit feature data according to given filtering units.
        /// </summary>
        /// <param name="units">Unit feature collection.</param>
        /// <param name="language">Language.</param>
        /// <param name="filteringUnitList">Give filtering unit collection.</param>
        public static void FilterFeatureData(IDictionary<string, UnitFeature> units,
            Language language, UnitList filteringUnitList)
        {
            if (units == null)
            {
                throw new ArgumentNullException("units");
            }

            if (units.Keys == null)
            {
                throw new ArgumentException("units.Keys is null");
            }

            if (filteringUnitList == null)
            {
                throw new ArgumentNullException("filteringUnitList");
            }

            if (filteringUnitList.Units.Count > 0)
            {
                Collection<string> removedKeys = new Collection<string>();
                foreach (string id in units.Keys)
                {
                    UnitFeature unit = units[id];
                    string key = UnitItem.GetKey(unit.SentenceId, unit.Index);
                    if ((filteringUnitList.UnitType == UnitList.UnitListType.Hold &&
                        !filteringUnitList.Units.ContainsKey(key)) ||
                        (filteringUnitList.UnitType == UnitList.UnitListType.Drop &&
                        filteringUnitList.Units.ContainsKey(key)))
                    {
                        removedKeys.Add(id);
                    }
                }

                foreach (string key in removedKeys)
                {
                    units.Remove(key);
                }

                // Update energy feature
                CalculateEnergyFeature(language, units);

                // Update epoch feature
                CalculateEpochFeature(language, units);
            }
        }

        /// <summary>
        /// Merge two unit feature file.
        /// </summary>
        /// <param name="sourceFile">Source unit feature file.</param>
        /// <param name="targetFile">Target unit feature file.</param>
        public static void Merge(UnitFeatureFile sourceFile, UnitFeatureFile targetFile)
        {
            // Validate Parameters
            if (sourceFile == null)
            {
                throw new ArgumentNullException("sourceFile");
            }

            if (sourceFile.Units == null)
            {
                throw new ArgumentException("sourceFile.Units is null");
            }

            if (sourceFile.Units.Keys == null)
            {
                throw new ArgumentException("sourceFile.Units.Keys is null");
            }

            if (targetFile == null)
            {
                throw new ArgumentNullException("targetFile");
            }

            if (targetFile.Units == null)
            {
                throw new ArgumentException("targetFile.Units is null");
            }

            if (targetFile.Language != sourceFile.Language)
            {
                string message = Helper.NeutralFormat("Source file and target file " +
                    "with different language: Source[{0}], Target[{1}]",
                    Localor.LanguageToString(sourceFile.Language),
                    Localor.LanguageToString(targetFile.Language));
                throw new ArgumentException(message);
            }

            foreach (string id in sourceFile.Units.Keys)
            {
                UnitFeature unit = sourceFile.Units[id];
                if (!targetFile.Units.ContainsKey(id))
                {
                    targetFile.Units.Add(id, unit);
                }
            }

            // Calculate energy feature
            CalculateEnergyFeature(targetFile.Language, targetFile.Units);

            // Calculate epoch feature
            CalculateEpochFeature(targetFile.Language, targetFile.Units);
        }

        /// <summary>
        /// Calculate energy fature for a group of units.
        /// </summary>
        /// <param name="units">Given unit list.</param>
        public static void CalculateEnergyFeature(ICollection<TtsAcousticFeature> units)
        {
            CalculateNormalizedFeature<TtsAcousticFeature>(units,
                delegate(TtsAcousticFeature acousFeature)
                {
                    if (acousFeature == null)
                    {
                        throw new ArgumentNullException("acousFeature");
                    }

                    return acousFeature.EnergyRms;
                },
                delegate(TtsAcousticFeature acousFeature, float value)
                {
                    if (acousFeature == null)
                    {
                        throw new ArgumentNullException("acousFeature");
                    }

                    acousFeature.Energy = value;
                });
        }

        /// <summary>
        /// Calculate epoch feature for a group of units.
        /// </summary>
        /// <param name="units">Ginven unit list.</param>
        public static void CalculateEpochFeature(ICollection<TtsAcousticFeature> units)
        {
            if (units == null)
            {
                throw new ArgumentNullException("units");
            }

            if (units.Count > 0)
            {
                // Firstly calculate mean.
                double mean = 0;
                int count = 0;
                foreach (TtsAcousticFeature unit in units)
                {
                    if (unit.AverageEpoch > 0)
                    {
                        mean += unit.AverageEpoch;
                        count++;
                    }
                }

                if (count > 0)
                {
                    mean /= count;
                }

                // Secondly calculate deviation.
                double variance = 0;
                foreach (TtsAcousticFeature unit in units)
                {
                    if (unit.AverageEpoch > 0)
                    {
                        double diff = unit.AverageEpoch - mean;
                        variance += diff * diff;
                    }
                }

                if (count > 0)
                {
                    variance = Math.Sqrt(variance / count);
                }

                // At last calculate normalized epoch.
                foreach (TtsAcousticFeature unit in units)
                {
                    if (variance == 0.0)
                    {
                        unit.NormalizedEpoch = 0.0f;
                    }
                    else
                    {
                        unit.NormalizedEpoch = (float)((unit.AverageEpoch - mean) / variance);
                    }
                }
            }
        }

        /// <summary>
        /// Calculate energy feature.
        /// </summary>
        /// <param name="units">Unit feature collection.</param>
        /// <param name="domain">Given script domain type.</param>
        public static void CalculateEnergyFeature(IDictionary<string, UnitFeature> units, int domain)
        {
            if (units == null)
            {
                throw new ArgumentNullException("units");
            }

            if (units.Count > 0)
            {
                // According to energy algorithm, the index is unit name but not ID
                SortedDictionary<string, List<TtsAcousticFeature>> nameIndexedUnits =
                    new SortedDictionary<string, List<TtsAcousticFeature>>();

                foreach (string key in units.Keys)
                {
                    UnitFeature unit = units[key];

                    if ((domain & unit.Domain) == 0)
                    {
                        unit.AcousticFeature.Energy = 0.0f;
                        continue;
                    }

                    if (!nameIndexedUnits.ContainsKey(unit.Name))
                    {
                        nameIndexedUnits.Add(unit.Name, new List<TtsAcousticFeature>());
                    }

                    nameIndexedUnits[unit.Name].Add(unit.AcousticFeature);
                }

                // Calculate energy feature
                foreach (string name in nameIndexedUnits.Keys)
                {
                    CalculateEnergyFeature(nameIndexedUnits[name]);
                }
            }
        }

        /// <summary>
        /// Calculate epoch feature.
        /// </summary>
        /// <param name="units">Unit feature collection.</param>
        /// <param name="domain">Given script domain type.</param>
        public static void CalculateEpochFeature(IDictionary<string, UnitFeature> units, int domain)
        {
            if (units == null)
            {
                throw new ArgumentNullException("units");
            }

            if (units.Count > 0)
            {
                // According to energy algorithm, the index is unit name but not ID
                SortedDictionary<string, List<TtsAcousticFeature>> nameIndexedUnits =
                    new SortedDictionary<string, List<TtsAcousticFeature>>();

                foreach (string key in units.Keys)
                {
                    UnitFeature unit = units[key];

                    if ((domain & unit.Domain) == 0)
                    {
                        unit.AcousticFeature.NormalizedEpoch = 0.0f;
                        continue;
                    }

                    if (!nameIndexedUnits.ContainsKey(unit.Name))
                    {
                        nameIndexedUnits.Add(unit.Name, new List<TtsAcousticFeature>());
                    }

                    nameIndexedUnits[unit.Name].Add(unit.AcousticFeature);
                }

                foreach (string name in nameIndexedUnits.Keys)
                {
                    CalculateEpochFeature(nameIndexedUnits[name]);
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Load unit feature file.
        /// </summary>
        /// <param name="filePath">Unit feature file path.</param>
        public void Load(string filePath)
        {
            _units.Clear();

            FilePath = filePath;
            Language = LoadFile(filePath, _units);
        }

        /// <summary>
        /// Save as unit feature file.
        /// </summary>
        public void Save()
        {
            Save(_filePath, _language);
        }

        /// <summary>
        /// Save as unit feature file.
        /// </summary>
        /// <param name="filePath">Unit feature file path.</param>
        public void Save(string filePath)
        {
            Save(filePath, _language);
        }

        /// <summary>
        /// Save as unit feature file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="language">Language.</param>
        public void Save(string filePath, Language language)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            Helper.TestWritable(filePath);

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";
            using (XmlWriter writer = XmlWriter.Create(filePath, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("unitFeatures", "http://schemas.microsoft.com/tts");
                writer.WriteAttributeString("lang", Localor.LanguageToString(language));

                // Write all unit features
                foreach (string key in _units.Keys)
                {
                    _units[key].ToXml(writer);
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            // Performance compatibility format checking
            XmlHelper.Validate(filePath, Schema);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Create dictionary from given collection.
        /// </summary>
        /// <typeparam name="T1">Type used by given collection.</typeparam>
        /// <typeparam name="T2">Type used by dictionary value.</typeparam>
        /// <param name="units">Given collection.</param>
        /// <param name="getKey">Given dictionary key.</param>
        /// <param name="getValue">Given dictionary value.</param>
        /// <returns>Created Dictionary.</returns>
        private static SortedDictionary<string, List<T2>> ToIndexed<T1, T2>(
            ICollection<T1> units, GetKey<T1> getKey, GetValue<T1, T2> getValue)
        {
            SortedDictionary<string, List<T2>> ret = new SortedDictionary<string, List<T2>>();

            foreach (T1 unit in units)
            {
                if (!ret.ContainsKey(getKey(unit)))
                {
                    ret.Add(getKey(unit), new List<T2>());
                }

                ret[getKey(unit)].Add(getValue(unit));
            }

            return ret;
        }

        /// <summary>
        /// Calculate normalized feature (suce as energy feature).
        /// </summary>
        /// <typeparam name="T1">Type of items.</typeparam>
        /// <param name="items">Item collection.</param>
        /// <param name="getValue">Given source feature.</param>
        /// <param name="setValue">Given target feature.</param>
        private static void CalculateNormalizedFeature<T1>(ICollection<T1> items,
            GetValue<T1, float> getValue, SetValue<T1, float> setValue)
        {
            Debug.Assert(items != null);

            if (items.Count > 0)
            {
                // Firstly calculate average.
                double sum = 0;
                foreach (T1 item in items)
                {
                    sum += getValue(item);
                }

                double average = sum / items.Count;

                // Secondly calculate deviation.
                sum = 0;
                foreach (T1 item in items)
                {
                    double diff = getValue(item) - average;
                    sum += diff * diff;
                }

                double deviation = Math.Sqrt(sum / items.Count);

                // At last calculate normalized average
                foreach (T1 item in items)
                {
                    if (deviation == 0.0)
                    {
                        setValue(item, 0);
                    }
                    else
                    {
                        setValue(item, (float)((getValue(item) - average) / deviation));
                    }
                }
            }
        }

        /// <summary>
        /// Load unit feature file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="units">Unit feature collection.</param>
        /// <returns>Lanuage used by unit features.</returns>
        private static Language LoadFile(string filePath, SortedDictionary<string, UnitFeature> units)
        {
            Language language = Language.Neutral;

            try
            {
                using (XmlTextReader reader = new XmlTextReader(filePath))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name == "unitFeatures")
                            {
                                language = Localor.StringToLanguage(reader.GetAttribute("lang").Trim());
                            }
                            else if (reader.Name == "unit")
                            {
                                UnitFeature unit = LoadUnit(reader);

                                if (!units.ContainsKey(unit.Id))
                                {
                                    units.Add(unit.Id, unit);
                                }
                                else
                                {
                                    string message = string.Format(CultureInfo.InvariantCulture,
                                        "Find duplication unit feature item: {0}", unit.Id);
                                    throw new InvalidDataException(message);
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Xml.XmlException xmlExp)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Xml format error: {0}", xmlExp.Message);
                throw new InvalidDataException(message);
            }

            return language;
        }

        /// <summary>
        /// Load one unit feature item from unit feature file.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        /// <returns>Unit feature item.</returns>
        private static UnitFeature LoadUnit(XmlTextReader reader)
        {
            Debug.Assert(reader != null);

            UnitFeature unit = new UnitFeature();
            unit.SentenceId = GetAttribute(reader, "sId");
            unit.Index = int.Parse(GetAttribute(reader, "index"), CultureInfo.InvariantCulture);
            unit.Name = GetAttribute(reader, "name");

            string domainText = reader.GetAttribute("domain");
            if (!string.IsNullOrEmpty(domainText))
            {
                unit.Domain = int.Parse(domainText, CultureInfo.InvariantCulture);
            }

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "lingF")
                    {
                        LoadLinguisticFeature(reader, unit);
                    }
                    else if (reader.Name == "acousF")
                    {
                        LoadAcousticFeature(reader, unit);
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement &&
                    reader.Name == "unit")
                {
                    break;
                }
            }

            if (unit.LingusitcFeature == null && unit.AcousticFeature == null)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Linguistic features and acoustic features are all empty");
                throw new ArgumentException(message);
            }

            return unit;
        }

        /// <summary>
        /// Load linguistic features.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        /// <param name="unit">Unit feature item.</param>
        private static void LoadLinguisticFeature(XmlTextReader reader, UnitFeature unit)
        {
            Debug.Assert(reader != null);
            Debug.Assert(unit != null);

            unit.LingusitcFeature = new TtsUnitFeature();

            unit.LingusitcFeature.PosInSentence = (PosInSentence)GetIntegerAttribute(reader, "pInS");
            unit.LingusitcFeature.PosInWord = (PosInWord)GetIntegerAttribute(reader, "pInW");
            unit.LingusitcFeature.PosInSyllable = (PosInSyllable)GetIntegerAttribute(reader, "pInSyl");
            unit.LingusitcFeature.LeftContextPhone = GetIntegerAttribute(reader, "lPh");
            unit.LingusitcFeature.RightContextPhone = GetIntegerAttribute(reader, "rPh");
            unit.LingusitcFeature.LeftContextTone = GetIntegerAttribute(reader, "lTone");
            unit.LingusitcFeature.RightContextTone = GetIntegerAttribute(reader, "rTone");
            unit.LingusitcFeature.TtsEmphasis = (TtsEmphasis)GetIntegerAttribute(reader, "emph");
            unit.LingusitcFeature.TtsStress = (TtsStress)GetIntegerAttribute(reader, "stress");
            unit.LingusitcFeature.TtsWordTone = (TtsWordTone)GetIntegerAttribute(reader, "wTone");
            unit.Break = (TtsBreak)GetIntegerAttribute(reader, "break");
            unit.WordType = (WordType)GetIntegerAttribute(reader, "wType");
        }

        /// <summary>
        /// Load acoustic features.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        /// <param name="unit">Unit feature item.</param>
        private static void LoadAcousticFeature(XmlTextReader reader, UnitFeature unit)
        {
            Debug.Assert(reader != null);
            Debug.Assert(unit != null);

            unit.AcousticFeature = new TtsAcousticFeature();

            // startTime and duration (s)
            unit.AcousticFeature.StartTime = GetFloatAttribute(reader, "tOffset");
            unit.AcousticFeature.Duration = GetFloatAttribute(reader, "tLen");

            // sample offset and length (sample rate = 16k)
            unit.AcousticFeature.SampleOffset = GetIntegerAttribute(reader, "sOffset");
            unit.AcousticFeature.SampleLength = GetIntegerAttribute(reader, "sLen");

            // sample length (sample rate = 8k)
            unit.AcousticFeature.Sample8KLength = GetIntegerAttribute(reader, "sLen8k");
            
            // epoch offset and length
            unit.AcousticFeature.EpochOffset = GetIntegerAttribute(reader, "eOffset");
            unit.AcousticFeature.EpochLength = GetIntegerAttribute(reader, "eLen");

            // compressed epoch length for sample rate = 16k and 8k
            unit.AcousticFeature.Epoch16KCompressLength = GetIntegerAttribute(reader, "ecLen16k");
            unit.AcousticFeature.Epoch8KCompressLength = GetIntegerAttribute(reader, "ecLen8k");

            // root mean square energy for building cart tree
            unit.AcousticFeature.CartRms = GetFloatAttribute(reader, "rmsCart");

            // root mean square energy and energy feature
            unit.AcousticFeature.EnergyRms = GetFloatAttribute(reader, "rmsE");
            unit.AcousticFeature.Energy = GetFloatAttribute(reader, "energy");

            // average pitch and pitch range
            unit.AcousticFeature.AveragePitch = GetFloatAttribute(reader, "pAvg");
            unit.AcousticFeature.PitchRange = GetFloatAttribute(reader, "pRange");

            // average epoch and normalized epoch feature
            unit.AcousticFeature.AverageEpoch = GetFloatAttribute(reader, "eAvg");
            unit.AcousticFeature.NormalizedEpoch = GetFloatAttribute(reader, "eNormAvg");
        }

        /// <summary>
        /// Get attribute of xml element.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        /// <param name="attrib">Attribute name string.</param>
        /// <returns>Attribute value string.</returns>
        private static string GetAttribute(XmlTextReader reader, string attrib)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!string.IsNullOrEmpty(attrib));

            string attribValue = reader.GetAttribute(attrib);

            if (string.IsNullOrEmpty(attribValue))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "cannot find attribute {0} in element {1}",
                    attrib, reader.Name);
                throw new InvalidDataException(message);
            }

            return attribValue;
        }

        /// <summary>
        /// Get integer attribute value of xml element.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>Attribute value.</returns>
        private static int GetIntegerAttribute(XmlTextReader reader, string attribName)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!string.IsNullOrEmpty(attribName));

            int attribValue;

            string attribValueText = reader.GetAttribute(attribName);
            if (!string.IsNullOrEmpty(attribValueText))
            {
                if (!int.TryParse(attribValueText, NumberStyles.Any,
                    CultureInfo.InvariantCulture.NumberFormat, out attribValue))
                {
                    string message = Helper.NeutralFormat("[{0}]: Invalid attribute value [{1}]",
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
        /// Get float attribute value of xml reader.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>Attribute value.</returns>
        private static float GetFloatAttribute(XmlTextReader reader, string attribName)
        {
            Debug.Assert(reader != null);
            Debug.Assert(!string.IsNullOrEmpty(attribName));

            float attribValue;

            string attribValueText = reader.GetAttribute(attribName);
            if (!string.IsNullOrEmpty(attribValueText))
            {
                if (!float.TryParse(attribValueText, NumberStyles.Any, 
                    CultureInfo.InvariantCulture.NumberFormat, out attribValue))
                {
                    string message = Helper.NeutralFormat("[{0}]: Invalid attribute value [{1}]",
                        attribName, attribValueText);
                    throw new InvalidDataException(message);
                }
            }
            else
            {
                attribValue = 0.0f;
            }

            return attribValue;
        }

        /// <summary>
        /// Extract linguistic feature for one script item.
        /// </summary>
        /// <param name="scriptItem">Script item.</param>
        /// <param name="units">Unit feature collection.</param>
        private static void ExtractLinguistic(ScriptItem scriptItem,
            SortedDictionary<string, UnitFeature> units)
        {
            Debug.Assert(scriptItem != null);
            Debug.Assert(units != null);

            for (int i = 0; i < scriptItem.Units.Count; i++)
            {
                UnitFeature unit = GetUnitFeature(scriptItem.Id, i, scriptItem.Units[i].FullName, units);
                unit.LingusitcFeature = scriptItem.Units[i].Feature;
                unit.Break = scriptItem.Units[i].TtsBreak;
                unit.WordType = scriptItem.Units[i].WordType;
            }
        }

        /// <summary>
        /// Extract acoustic feature for one script item.
        /// </summary>
        /// <param name="scriptItem">Script item.</param>
        /// <param name="fileMap">File list map.</param>
        /// <param name="segmentDir">Segment directory.</param>
        /// <param name="wave16kDir">16 Khz Waveform directory.</param>
        /// <param name="epochDir">Epoch directory.</param>
        /// <param name="units">Unit feature collection.</param>
        private static void ExtractAcoustic(ScriptItem scriptItem,
            FileListMap fileMap, string segmentDir, string wave16kDir,
            string epochDir, SortedDictionary<string, UnitFeature> units)
        {
            Debug.Assert(scriptItem != null);
            Debug.Assert(fileMap != null);
            Debug.Assert(units != null);

            string sid = scriptItem.Id;

            if (!fileMap.Map.ContainsKey(sid))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Sentence [{0}] does not exist in file list map [{1}].",
                    sid, fileMap.FilePath);
                throw new InvalidDataException(message);
            }

            // Find the absolute file paths for each kind data file, and load data files
            SegmentFile segFile = new SegmentFile();
            segFile.Load(fileMap.BuildPath(segmentDir, sid, ".txt"));

            EggAcousticFeature eggFile = new EggAcousticFeature();
            eggFile.LoadEpoch(fileMap.BuildPath(epochDir, sid, ".epoch"));

            WaveAcousticFeature waveFile = new WaveAcousticFeature();
            waveFile.Load(fileMap.BuildPath(wave16kDir, sid, ".wav"));

            // Calculate acoustic features for each segments in the files
            int totalCount = segFile.NonSilenceWaveSegments.Count;
            Debug.Assert(scriptItem.Units.Count == totalCount);
            if (scriptItem.Units.Count != totalCount)
            {
                string str1 = "Unit number mis-matched in sentence [{0}] between ";
                string str2 = "script file and the alignment file [{1}]. ";
                string str3 = "There are {2} units in script but {3} units in alignment.";
                string message = string.Format(CultureInfo.InvariantCulture,
                    str1 + str2 + str3,
                    scriptItem.Id, segFile.FilePath, scriptItem.Units.Count, totalCount);
                throw new InvalidDataException(message);
            }

            for (int i = 0; i < scriptItem.Units.Count; i++)
            {
                UnitFeature unit = GetUnitFeature(sid, i, scriptItem.Units[i].FullName, units);
                unit.AcousticFeature =
                    ExtractAcoustic(waveFile, eggFile, segFile.NonSilenceWaveSegments[i]);
            }
        }

        /// <summary>
        /// Extract acoustic feature for one unit.
        /// </summary>
        /// <param name="waveFile">Waveform file.</param>
        /// <param name="eggFile">Epoch file.</param>
        /// <param name="segment">Wave segment.</param>
        /// <returns>Acoustic feature.</returns>
        private static TtsAcousticFeature ExtractAcoustic(WaveAcousticFeature waveFile,
            EggAcousticFeature eggFile, WaveSegment segment)
        {
            Debug.Assert(waveFile != null);
            Debug.Assert(eggFile != null);
            Debug.Assert(segment != null);

            TtsAcousticFeature acousticFeature = new TtsAcousticFeature();

            // Get unit sample scope.
            int sampleOffset = (int)(segment.StartTime * waveFile.SamplesPerSecond);
            int sampleLength = (int)(segment.Duration * waveFile.SamplesPerSecond);
            int sampleEnd = sampleOffset + sampleLength;

            int epochOffset = 0;
            int epochEnd = 0;

            // Calculate average pitch, pitch average.
            float averagePitch, pitchRange;
            eggFile.GetPitchAndRange(sampleOffset, sampleLength, out averagePitch, out pitchRange);
            segment.AveragePitch = averagePitch;
            segment.PitchRange = pitchRange;

            // Calculate average epoch.
            float averageEpoch = eggFile.GetAverageEpoch(sampleOffset, sampleLength);

            // Calculate root mean square: before that ajust the segment alignment with
            // the epoch data
            epochOffset = eggFile.AdjustAlignment(ref sampleOffset);
            epochEnd = eggFile.AdjustAlignment(ref sampleEnd);

            if (epochOffset > epochEnd)
            {
                string info = string.Format(CultureInfo.InvariantCulture,
                    "epochOffset[{0}] should not be bigger than epochEnd[{1}]",
                    epochOffset, epochEnd);
                throw new InvalidDataException(info);
            }

            if (sampleEnd > waveFile.SampleNumber)
            {
                string str1 = "Mis-match found between alignment segment and waveform file [{0}], ";
                string str2 = "for the end sample of alignment is [{1}] but";
                string str3 = " the total sample number of waveform file is [{2}].";
                string info = string.Format(CultureInfo.InvariantCulture,
                    str1 + str2 + str3,
                    waveFile.FilePath, epochEnd, waveFile.SampleNumber);
                throw new InvalidDataException(info);
            }

            segment.RootMeanSquare = waveFile.CalculateRms(sampleOffset, sampleEnd - sampleOffset);

            // calculate epoch
            int epoch16KCompressLength = EpochFile.CompressEpoch(eggFile.Epoch,
                epochOffset, epochEnd - epochOffset, null);
            int epoch8KCompressLength = EpochFile.CompressEpoch(eggFile.Epoch8k,
                epochOffset, epochEnd - epochOffset, null);

            acousticFeature.StartTime = (float)segment.StartTime;
            acousticFeature.Duration = (float)segment.Duration;
            acousticFeature.SampleOffset = sampleOffset;
            acousticFeature.SampleLength = sampleEnd - sampleOffset;
            acousticFeature.EpochOffset = epochOffset;
            acousticFeature.EpochLength = epochEnd - epochOffset;
            acousticFeature.CartRms = segment.RootMeanSquare;
            acousticFeature.EnergyRms = waveFile.CalculateEnergyRms(sampleOffset,
                sampleEnd - sampleOffset);
            acousticFeature.AveragePitch = segment.AveragePitch;
            acousticFeature.PitchRange = segment.PitchRange;
            acousticFeature.Epoch16KCompressLength = epoch16KCompressLength;
            acousticFeature.Epoch8KCompressLength = epoch8KCompressLength;
            acousticFeature.Sample8KLength = (sampleEnd / 2) - (sampleOffset / 2);
            acousticFeature.AverageEpoch = averageEpoch;

            return acousticFeature;
        }

        /// <summary>
        /// Add a new unit feature into unit feature collection.
        /// If exited same on in the collection, return it .
        /// </summary>
        /// <param name="sid">Sentence Id.</param>
        /// <param name="index">Unit Index.</param>
        /// <param name="name">Unit name.</param>
        /// <param name="units">Unit feature collection.</param>
        /// <returns>Unit feature.</returns>
        private static UnitFeature GetUnitFeature(string sid, int index, string name,
            SortedDictionary<string, UnitFeature> units)
        {
            Debug.Assert(!string.IsNullOrEmpty(sid));
            Debug.Assert(index >= 0);
            Debug.Assert(!string.IsNullOrEmpty(name));

            UnitFeature unit = new UnitFeature(sid, index);
            if (units.ContainsKey(unit.Id))
            {
                unit = units[unit.Id];
                Debug.Assert(unit.Name == name);
                if (unit.Name != name)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Unit {0} - Name mismatch: current unit name {1}, unit name in unit list {2}",
                        unit.Id, name, unit.Name);
                    throw new InvalidDataException(message);
                }
            }
            else
            {
                unit.Name = name;
                units.Add(unit.Id, unit);
            }

            return unit;
        }

        /// <summary>
        /// Calculate energy feature.
        /// </summary>
        /// <param name="lang">Language.</param>
        /// <param name="units">Unit feature collection.</param>
        private static void CalculateEnergyFeature(Language lang, IDictionary<string, UnitFeature> units)
        {
            Debug.Assert(units != null);
            Debug.Assert(units.Count > 0);

            FeatureMeta featureMeta = Localor.GetFeatureMeta(lang);
            Debug.Assert(featureMeta != null);

            if (featureMeta.Metadata.ContainsKey(TtsFeature.TtsEnergy))
            {
                CalculateEnergyFeature(units, (int)ScriptDomain.Normal);
            }
        }

        /// <summary>
        /// Try to calculate epoch feature.
        /// </summary>
        /// <param name="lang">Language.</param>
        /// <param name="units">Unit feature collection.</param>
        private static void CalculateEpochFeature(Language lang, IDictionary<string, UnitFeature> units)
        {
            Debug.Assert(units != null);
            Debug.Assert(units.Count > 0);

            FeatureMeta featureMeta = Localor.GetFeatureMeta(lang);
            Debug.Assert(featureMeta != null);

            if (featureMeta.Metadata.ContainsKey(TtsFeature.TtsNormalizedEpoch))
            {
                CalculateEpochFeature(units, (int)ScriptDomain.Normal);
            }
        }

        #endregion
    }
}