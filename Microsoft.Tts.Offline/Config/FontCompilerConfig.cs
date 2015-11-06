//----------------------------------------------------------------------------
// <copyright file="FontCompilerConfig.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements VoiceFontConfig
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
    using System.Security.Permissions;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Waveform;

    /// <summary>
    /// Format version of voice font.
    /// </summary>
    public enum FormatVersion
    {
        /// <summary>
        /// Version undefined.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Version 3.0.
        /// </summary>
        Tts30 = 3
    }

    /// <summary>
    /// Voice font file tag.
    /// </summary>
    public enum FontSectionTag
    {
        /// <summary>
        /// Unknown section tag.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Weight Table File Tag:= "APL ",
        /// Its ASCII code: 0x41 0x50 0x4C 0x20.
        /// </summary>
        WeightTable = 0x204C5041,

        /// <summary>
        /// UNT File Tag:= "UNT ",
        /// Its ASCII code: 0x55, 0x4E, 0x54, 0x20.
        /// </summary>
        UnitInfo = 0x20544E55,

        /// <summary>
        /// NAM File Tag:="NAM ",
        /// Its ASCII code: 0x4E, 0x41,0x4D, 0x20.
        /// </summary>
        NameDomain = 0x204D414E,

        /// <summary>
        /// NUM File Tag:="NUM ",
        /// Its ASCII code: 0x4E, 0x55, 0x4D, 0x20.
        /// </summary>
        NumberDomain = 0x204D554E,

        /// <summary>
        /// LET File Tag:="LET ",
        /// Its ASCII code: 0x4C, 0x45, 0x54, 0x20.
        /// </summary>
        LetterDomain = 0x2054454C,

        /// <summary>
        /// LET File Tag:="ACR ",
        /// Its ASCII code: 0x41, 0x43, 0x52, 0x20.
        /// </summary>
        AcronymDomain = 0x20524341,

        /// <summary>
        /// APH File Tag :="APH ",
        /// Its ASCII code: 0x41 0x50 0x48 0x20.
        /// </summary>
        AnyPromptHelper = 0x20485041
    }

    /// <summary>
    /// Wave compress catalog.
    /// </summary>
    public enum WaveCompressCatalog
    {
        /// <summary>
        /// Uncompressed.
        /// </summary>
        Unc = 0,

        /// <summary>
        /// DirectX Model Object.
        /// </summary>
        Dmo = 3,

        /// <summary>
        /// Microsoft RTA codec.
        /// </summary>
        MSRTA = 4,

        /// <summary>
        /// SILK codec.
        /// </summary>
        SILK = 5,

        /// <summary>
        /// OpusSILK codec.
        /// </summary>
        OpusSILK = 6
    }

    /// <summary>
    /// Font build number.
    /// </summary>
    public class FontBuildNumber
    {
        #region Fields

        private int _majorBuildNumber;
        private int _minorBuildNumber;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FontBuildNumber"/> class.
        /// </summary>
        public FontBuildNumber()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FontBuildNumber"/> class.
        /// </summary>
        /// <param name="voicePath">Voice font path.</param>
        public FontBuildNumber(string voicePath)
        {
            if (string.IsNullOrEmpty(voicePath))
            {
                throw new ArgumentNullException("voicePath");
            }

            GetBuildNumber(voicePath);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FontBuildNumber"/> class.
        /// </summary>
        /// <param name="value">Value.</param>
        public FontBuildNumber(int value)
        {
            MinorBuildNumber = value & 0xFFFF;
            MajorBuildNumber = (int)((((uint)value) >> 16) & 0xFFFF);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Major build number.
        /// </summary>
        public int MajorBuildNumber
        {
            get
            {
                return _majorBuildNumber;
            }

            set
            {
                if (value < ushort.MinValue || value > ushort.MaxValue)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid major number {0}: it's not in the range [0, 65535]",
                        value);
                    throw new InvalidDataException(message);
                }

                _majorBuildNumber = value;
            }
        }

        /// <summary>
        /// Gets or sets Minor build number.
        /// </summary>
        public int MinorBuildNumber
        {
            get
            {
                return _minorBuildNumber;
            }

            set
            {
                if (value < ushort.MinValue || value > ushort.MaxValue)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid minor number {0}: it's not in the range [0, 65535]",
                        value);
                    throw new InvalidDataException(message);
                }

                _minorBuildNumber = value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the current build number.
        /// </summary>
        /// <returns>The current build number.</returns>
        public static FontBuildNumber GetCurrentBuildNumber()
        {
            const string BuildNumberKey = "FontBuildNumber";
            string value = Environment.GetEnvironmentVariable(BuildNumberKey, EnvironmentVariableTarget.Process);
            FontBuildNumber current = new FontBuildNumber();

            if (!string.IsNullOrEmpty(value))
            {
                // Gets the current build number from environment successfully.
                current.Parse(value);
            }
            else
            {
                // Fails to get the build number from environment setting, creates one according to the time.
                current.MajorBuildNumber = (((DateTime.Now.Year - 2000) % 5) * 10000) +
                    (DateTime.Now.Month * 100) + DateTime.Now.Day;
                current.MinorBuildNumber = (DateTime.Now.Hour * 100) + DateTime.Now.Minute;
            }

            return current;
        }

        /// <summary>
        /// Parse build number string to set build number information.
        /// </summary>
        /// <param name="buildNumber">Build number string.</param>
        public void Parse(string buildNumber)
        {
            if (string.IsNullOrEmpty(buildNumber))
            {
                throw new ArgumentNullException("buildNumber");
            }

            string[] numbers = buildNumber.Split(new char[] { '.' },
                StringSplitOptions.RemoveEmptyEntries);
            if (numbers.Length != 2)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "{0} is invalid build number.", buildNumber);
                throw new InvalidDataException(message);
            }

            int number;
            if (int.TryParse(numbers[0].Trim(), out number))
            {
                MajorBuildNumber = number;
            }
            else
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "{0} is invalid major build number.", numbers[0].Trim());
                throw new InvalidDataException(message);
            }

            if (int.TryParse(numbers[1].Trim(), out number))
            {
                MinorBuildNumber = number;
            }
            else
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "{0} is invalid minor build number.", numbers[1].Trim());
                throw new InvalidDataException(message);
            }
        }

        /// <summary>
        /// Convert to 32-bit integer number.
        /// </summary>
        /// <returns>Build number integer.</returns>
        public int ToInt32()
        {
            return (_majorBuildNumber << 16) | (_minorBuildNumber & 0xFFFF);
        }

        /// <summary>
        /// Convert to string presentation.
        /// </summary>
        /// <returns>Build number string.</returns>
        public override string ToString()
        {
            string message = string.Format(CultureInfo.InvariantCulture,
                "{0}.{1}", _majorBuildNumber, _minorBuildNumber);
            return message;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Get build number from voice font.
        /// </summary>
        /// <param name="voicePath">Voice path.</param>
        private void GetBuildNumber(string voicePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(voicePath));

            string aplFile = string.Format(CultureInfo.InvariantCulture,
                "{0}.APL", voicePath);
            FileStream fs = new FileStream(aplFile, FileMode.Open,
                FileAccess.Read, FileShare.Read);
            try
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    fs = null;
                    br.BaseStream.Seek(28, SeekOrigin.Begin);
                    MinorBuildNumber = br.ReadUInt16();
                    MajorBuildNumber = br.ReadUInt16();
                }
            }
            finally
            {
                if (null != fs)
                {
                    fs.Dispose();
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Font version.
    /// </summary>
    public class FontVersion
    {
        #region Fields

        private string _name;

        private int _samplesPerSecond;
        private int _bytesPerSample;
        private WaveFormatTag _pcmCategory;
        private WaveCompressCatalog _compressCatalog;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the FontVersion class.
        /// </summary>
        public FontVersion()
        {
        }

        /// <summary>
        /// Initializes a new instance of the FontVersion class.
        /// </summary>
        /// <param name="name">Voice font name.</param>
        /// <param name="compress">Compress mode.</param>
        /// <param name="encoding">Encoding.</param>
        /// <param name="samplesPerSecond">Samples per second.</param>
        /// <param name="bytesPerSample">Bytes per second.</param>
        public FontVersion(string name, WaveCompressCatalog compress, WaveFormatTag encoding,
            int samplesPerSecond, int bytesPerSample)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (samplesPerSecond != 16000 && samplesPerSecond != 8000)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Only 16k Hz (16000) and 8K hz (8000) sampling rate are supported");
                throw new ArgumentException(message);
            }

            if (bytesPerSample != 2 && bytesPerSample != 1)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Only 1 and 2 bytesPerSample are supported");
                throw new ArgumentException(message);
            }

            _name = name;
            _compressCatalog = compress;
            _pcmCategory = encoding;
            _samplesPerSecond = samplesPerSecond;
            _bytesPerSample = bytesPerSample;

            Validate();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the font version type of voice font.
        /// </summary>
        public string FontVersionType
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(_compressCatalog.ToString());
                sb.Append(_samplesPerSecond / 1000);
                sb.Append("K");
                sb.Append(_bytesPerSample * 8);
                sb.Append("Bit");
                sb.Append(_pcmCategory.ToString());

                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the CompressCatalog.
        /// </summary>
        public WaveCompressCatalog CompressCatalog
        {
            get { return _compressCatalog; }
            set { _compressCatalog = value; }
        }

        /// <summary>
        /// Gets or sets the FormatCategory.
        /// </summary>
        public WaveFormatTag PcmCategory
        {
            get { return _pcmCategory; }
            set { _pcmCategory = value; }
        }

        /// <summary>
        /// Gets or sets the BytesPerSecond.
        /// </summary>
        public int BytesPerSample
        {
            get { return _bytesPerSample; }
            set { _bytesPerSample = value; }
        }

        /// <summary>
        /// Gets or sets the SamplesPerSecond.
        /// </summary>
        public int SamplesPerSecond
        {
            get { return _samplesPerSecond; }
            set { _samplesPerSecond = value; }
        }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _name = value;
            }
        }
        #endregion

        #region Public methods

        /// <summary>
        /// Validate whether the config is supported or not.
        /// </summary>
        public void Validate()
        {
            if (PcmCategory == WaveFormatTag.Mulaw)
            {
                if (SamplesPerSecond != 8000)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Only supports 8000 SamplesPerSecond for Mulaw encoding.");
                    throw new NotSupportedException(message);
                }

                if (BytesPerSample != 1)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Only supports 1 BytesPerSample for Mulaw encoding.");
                    throw new NotSupportedException(message);
                }

                if (CompressCatalog != WaveCompressCatalog.Unc)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Only supports Unc (uncompress) for Mulaw encoding.");
                    throw new NotSupportedException(message);
                }
            }
            else if (PcmCategory == WaveFormatTag.Pcm)
            {
                if (BytesPerSample != 2)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Only supports 2 BytesPerSample for Mulaw encoding.");
                    throw new NotSupportedException(message);
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
        }

        #endregion
    }

    /// <summary>
    /// Voice font merge items.
    /// </summary>
    public class MergeItem
    {
        #region Fields

        private string _dir;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="MergeItem"/> class.
        /// </summary>
        /// <param name="dir">Merge item directory.</param>
        public MergeItem(string dir)
        {
            if (string.IsNullOrEmpty(dir))
            {
                throw new ArgumentNullException("dir");
            }

            Dir = dir;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets ScriptFile.
        /// </summary>
        public string ScriptFilePath
        {
            get { return Path.Combine(Dir, "script.xml"); }
        }

        /// <summary>
        /// Gets Filemap.
        /// </summary>
        public string FileMapFilePath
        {
            get { return Path.Combine(Dir, "Filemap.txt"); }
        }

        /// <summary>
        /// Gets Unit feature file path.
        /// </summary>
        public string UnitFeatureFilePath
        {
            get { return Path.Combine(Dir, "UnitFeature.xml"); }
        }

        /// <summary>
        /// Gets or sets DirPath.
        /// </summary>
        public string Dir
        {
            get
            {
                return _dir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _dir = value;
            }
        }

        #endregion

        #region Data validation

        /// <summary>
        /// Validation data alignment between feature file and script file.
        /// </summary>
        /// <param name="featureFile">Feature file.</param>
        /// <param name="scriptFile">Script file.</param>
        /// <param name="language">Language.</param>
        /// <returns>Data error set found.</returns>
        public static ErrorSet ValidateFeatureData(string featureFile,
            string scriptFile, Language language)
        {
            ErrorSet errorSet = new ErrorSet();

            TtsPhoneSet phoneSet = Localor.GetPhoneSet(language);
            XmlScriptValidateSetting validateSetting = new XmlScriptValidateSetting(phoneSet, null);
            XmlScriptFile script = XmlScriptFile.LoadWithValidation(scriptFile, validateSetting);
            if (script.ErrorSet.Count > 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "{0} error(s) found in the script file [{1}]",
                    script.ErrorSet.Count, scriptFile);

                throw new InvalidDataException(message);
            }

            XmlUnitFeatureFile unitFeatureFile = new XmlUnitFeatureFile(featureFile);
            if (unitFeatureFile.Units.Count <= 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Zero unit feature item in unit feature file {0}", featureFile);
                errorSet.Add(VoiceFontError.OtherErrors, message);

                throw new InvalidDataException(message);
            }

            if (unitFeatureFile.Language != language)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Different lanuage\r\nScript File {0}: lang = {1}\r\n Feature File {2}: lang = {3}",
                    scriptFile, Localor.LanguageToString(language),
                    featureFile, Localor.LanguageToString(unitFeatureFile.Language));

                throw new InvalidDataException(message);
            }

            foreach (string key in unitFeatureFile.Units.Keys)
            {
                UnitFeature unit = unitFeatureFile.Units[key];

                string sid = unit.SentenceId;
                int unitIndex = unit.Index;
                string unitName = unit.Name;

                if (unit.Index < 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "invalid unit index [{0}] found in feature file [{1}]. It should not be negative integer for unit indexing.",
                        unit.Index, featureFile);
                    errorSet.Add(VoiceFontError.OtherErrors, message);
                    continue;
                }

                try
                {
                    if (!script.ItemDic.ContainsKey(unit.SentenceId))
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "sentence id {0} in feature file [{1}] is not in script file [{2}]",
                            sid, featureFile, scriptFile);
                        errorSet.Add(ScriptError.OtherErrors, sid, message);
                        continue;
                    }

                    ScriptItem item = script.ItemDic[sid];
                    Phoneme phoneme = Localor.GetPhoneme(language);
                    SliceData sliceData = Localor.GetSliceData(language);
                    Collection<TtsUnit> itemUnits = item.GetUnits(phoneme, sliceData);
                    if (unitIndex >= itemUnits.Count)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "the {0}th unit [{1}] in sentence {2} of feature file [{3}] is out of range for sentence {2} in script file [{4}]",
                            unitIndex, unitName, sid, featureFile, scriptFile);
                        errorSet.Add(ScriptError.OtherErrors, sid, message);
                        continue;
                    }

                    TtsUnit ttsUnit = itemUnits[unitIndex];
                    string sliceName = ttsUnit.FullName.Replace(' ', '+');
                    if (sliceName != unitName)
                    {
                        string str1 = "the {0}th unit [{1}] in sentence {3} of feature file [{4}] ";
                        string str2 = "is not matched with {0}th unit [{2}] for sentence {3} in script file [{5}]";
                        string message = string.Format(CultureInfo.InvariantCulture,
                            str1 + str2,
                            unitIndex, unitName, sliceName, sid, featureFile, scriptFile);
                        errorSet.Add(ScriptError.OtherErrors, sid, message);
                        continue;
                    }
                }
                catch (InvalidDataException ide)
                {
                    errorSet.Add(ScriptError.OtherErrors, sid, Helper.BuildExceptionMessage(ide));
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Find unmatching sentences between filemap file and script file
        /// <param />
        /// This function should be merged with that in forcedalignment into common library.
        /// </summary>
        /// <param name="scriptFilePath">The location of script file.</param>
        /// <param name="language">Language of the script file.</param>
        /// <param name="mapFilePath">The location of file fist map path.</param>
        /// <returns>Unmatching sentence ids.</returns>
        public static ErrorSet FindUnmatchedSentences(string scriptFilePath,
            Language language, string mapFilePath)
        {
            ErrorSet errorSet = new ErrorSet();
            TtsPhoneSet phoneSet = Localor.GetPhoneSet(language);
            XmlScriptValidateSetting validateSetting = new XmlScriptValidateSetting(phoneSet, null);
            XmlScriptFile script = XmlScriptFile.LoadWithValidation(scriptFilePath, validateSetting);
            script.Remove(ScriptHelper.GetNeedDeleteItemIds(script.ErrorSet));

            Dictionary<string, string> map = Microsoft.Tts.Offline.FileListMap.ReadAllData(mapFilePath);
            errorSet.Merge(script.ErrorSet);
            foreach (string sid in script.ItemDic.Keys)
            {
                if (!map.ContainsKey(sid))
                {
                    string message = Helper.NeutralFormat(
                        "Script item {0} in script file but not in file list map file", sid);
                    errorSet.Add(ScriptError.OtherErrors, sid, message);
                }
            }

            foreach (string sid in map.Keys)
            {
                if (!script.ItemDic.ContainsKey(sid))
                {
                    string message = Helper.NeutralFormat(
                        "Script item {0} in file list map file but not in script file", sid);
                    errorSet.Add(ScriptError.OtherErrors, sid, message);
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Data validation.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <returns>Data error set found.</returns>
        public ErrorSet Validate(Language language)
        {
            // Files existance validation
            if (!Directory.Exists(Dir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException),
                    Dir);
            }

            if (!File.Exists(ScriptFilePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    ScriptFilePath);
            }

            if (!File.Exists(FileMapFilePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    FileMapFilePath);
            }

            if (!File.Exists(UnitFeatureFilePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    UnitFeatureFilePath);
            }

            ErrorSet errorSet = new ErrorSet();

            ErrorSet subErrorSet =
                FindUnmatchedSentences(ScriptFilePath, language, FileMapFilePath);
            errorSet.Merge(subErrorSet);

            subErrorSet = ValidateFeatureData(UnitFeatureFilePath, ScriptFilePath,
                language);
            errorSet.Merge(subErrorSet);

            return errorSet;
        }

        #endregion
    }

    /// <summary>
    /// VoiceFontConfig.
    /// </summary>
    [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]    
    public class FontCompilerConfig
    {
        #region Fields

        private static XmlSchema _schema;

        /// <summary>
        /// Tool work site folder, from where some dependent tools could be found.
        /// </summary>
        private Language _language;
        private string _workSiteDir;
        private VoiceCreationLanguageData _languageData = new VoiceCreationLanguageData();
        private string _voiceDataDir;
        private string _scriptPath;
        private string _filelistPath;
        private string _dropFileListPath;
        private string _domainListFilePath;
        private string _wave16kDir;
        private string _wave16kFilteredDir;
        private string _epochDir;
        private string _segmentDir;

        private List<string> _import = new List<string>();
        private string _weightTable = "WeightTable.txt";

        private string _dropUnitList;
        private string _holdUnitList;

        private string _outputDir;

        private int _cartMinCandidates = 50;
        private int _cartSplitLevel;

        private Collection<FontVersion> _versions = new Collection<FontVersion>();
        private Collection<MergeItem> _mergeItems = new Collection<MergeItem>();

        private DomainConfig _domainConfig;

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
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Config.FontCompiler.xsd");
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
        /// Gets Merge items.
        /// </summary>
        public Collection<MergeItem> MergeItems
        {
            get { return _mergeItems; }
        }

        /// <summary>
        /// Gets or sets Output directory.
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

        /// <summary>
        /// Gets or sets Hold unit list file.
        /// </summary>
        public string HoldUnitList
        {
            get
            {
                return _holdUnitList;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _holdUnitList = value;
            }
        }

        /// <summary>
        /// Gets or sets Drop unit list file.
        /// </summary>
        public string DropUnitList
        {
            get
            {
                return _dropUnitList;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _dropUnitList = value;
            }
        }

        /// <summary>
        /// Gets or sets Domain list file path.
        /// </summary>
        public string DomainListFilePath
        {
            get
            {
                return _domainListFilePath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _domainListFilePath = value;
            }
        }

        /// <summary>
        /// Gets Domain config.
        /// </summary>
        public DomainConfig DomainConfig
        {
            get
            {
                if (_domainConfig == null && !string.IsNullOrEmpty(DomainListFilePath))
                {
                    _domainConfig = new DomainConfig();
                    _domainConfig.Load(DomainListFilePath);
                }

                return _domainConfig;
            }
        }

        /// <summary>
        /// Gets Font versions.
        /// </summary>
        public Collection<FontVersion> Versions
        {
            get { return _versions; }
        }

        /// <summary>
        /// Gets or sets Alignment directory.
        /// </summary>
        public string AlignmentDir
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
        /// Gets or sets Epoch directory.
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
        /// Gets or sets Filtered 16k Hz waveform directory, filtered into 8k Hz.
        /// </summary>
        public string Wave16kFilteredDir
        {
            get
            {
                return _wave16kFilteredDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _wave16kFilteredDir = value;
            }
        }

        /// <summary>
        /// Gets or sets 16k Hz waveform directory.
        /// </summary>
        public string Wave16kDir
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
        /// Gets or sets File list path.
        /// </summary>
        public string FilelistPath
        {
            get
            {
                return _filelistPath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _filelistPath = value;
            }
        }

        /// <summary>
        /// Gets or sets Drop sentences file list.
        /// </summary>
        public string DropFileListPath
        {
            get
            {
                return _dropFileListPath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _dropFileListPath = value;
            }
        }

        /// <summary>
        /// Gets or sets Script path.
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
        /// Gets or sets Language.
        /// </summary>
        public Language Language
        {
            get { return _language; }
            set { _language = value; }
        }

        /// <summary>
        /// Gets Import files.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1002:DoNotExposeGenericLists", Justification = "Ignore.")]
        public List<string> Import
        {
            get { return _import; }
        }

        /// <summary>
        /// Gets or sets Weight table for phones, could be voice dependent.
        /// </summary>
        public string WeightTable
        {
            get
            {
                return Helper.GetFullPath(VoiceDataDir, _weightTable);
            }

            set
            {
                _weightTable = value;
            }
        }

        /// <summary>
        /// Gets Cart questions file.
        /// </summary>
        public string CartQuestions
        {
            get
            {
                string cartQuestions;
                if (_languageData.IsEmpty())
                {
                    // Stocked question set
                    cartQuestions = Helper.GetFullPath(Path.Combine(WorksiteDir, @"DataFiles\" + Localor.LanguageToString(Language)), "WholeCartQuestionSet.txt");
                }
                else
                {
                    cartQuestions = _languageData.CartQuestions;
                }

                return cartQuestions;
            }
        }

        /// <summary>
        /// Gets or sets Cart Minimial candidates in the leaf node.
        /// </summary>
        public int CartMinCandidates
        {
            get
            {
                return _cartMinCandidates;
            }

            set
            {
                if (value < 0)
                {
                    throw new System.ArgumentException("Candidate number cannot be negative");
                }

                _cartMinCandidates = value;
            }
        }

        /// <summary>
        /// Gets or sets Cart node split level.
        /// </summary>
        public int CartSplitLevel
        {
            get
            {
                return _cartSplitLevel;
            }

            set
            {
                if (value < 0)
                {
                    throw new System.ArgumentException("splitLevel cannot be negative");
                }

                _cartSplitLevel = value;
            }
        }

        /// <summary>
        /// Gets or sets Voice data directiory (weighttable etc).
        /// </summary>
        public string VoiceDataDir
        {
            get
            {
                string voiceDataDir = _voiceDataDir;
                if (string.IsNullOrEmpty(_voiceDataDir))
                {
                    voiceDataDir = Path.Combine(WorksiteDir, @"DataFiles\" + Localor.LanguageToString(Language));
                }

                return voiceDataDir;
            }

            set
            {
                _voiceDataDir = value;
            }
        }

        /// <summary>
        /// Gets or sets Work site dir.
        /// </summary>
        public string WorksiteDir
        {
            get
            {
                if (string.IsNullOrEmpty(_workSiteDir))
                {
                    string appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    _workSiteDir = Path.GetDirectoryName(appPath);
                }

                return _workSiteDir;
            }

            set
            {
                _workSiteDir = value;
            }
        }

        #endregion

        #region Public static methods

        /// <summary>
        /// Create versions xml element node .
        /// </summary>
        /// <param name="dom">Xml document.</param>
        /// <param name="xmlNamespace">Xml namespace.</param>
        /// <param name="versions">Versions collection to create Xml node.</param>
        /// <returns>Created versions xml element.</returns>
        public static XmlElement CreateVersionsXmlElement(XmlDocument dom, string xmlNamespace,
            Collection<FontVersion> versions)
        {
            XmlElement versionsEle = dom.CreateElement("versions", xmlNamespace);

            foreach (FontVersion version in versions)
            {
                XmlElement versionEle = dom.CreateElement("version", xmlNamespace);
                versionEle.SetAttribute("name", version.Name);
                versionEle.SetAttribute("compress", version.CompressCatalog.ToString());
                versionEle.SetAttribute("encoding", version.PcmCategory.ToString());
                versionEle.SetAttribute("samplesPerSecond",
                    version.SamplesPerSecond.ToString(CultureInfo.InvariantCulture));
                versionEle.SetAttribute("bytesPerSample",
                    version.BytesPerSample.ToString(CultureInfo.InvariantCulture));
                versionsEle.AppendChild(versionEle);
            }

            return versionsEle;
        }

        /// <summary>
        /// Parse version node list.
        /// </summary>
        /// <param name="versionsNodeList">Versions node list.</param>
        /// <param name="nsmgr">Xml namespace manager.</param>
        /// <param name="versions">Version collection.</param>
        public static void ParseVersions(XmlNodeList versionsNodeList,
            XmlNamespaceManager nsmgr, Collection<FontVersion> versions)
        {
            if (versionsNodeList == null)
            {
                throw new ArgumentNullException("versionsNodeList");
            }

            if (nsmgr == null)
            {
                throw new ArgumentNullException("nsmgr");
            }

            if (versions == null)
            {
                throw new ArgumentNullException("versions");
            }

            versions.Clear();
            foreach (XmlNode node in versionsNodeList)
            {
                XmlElement ele = (XmlElement)node;
                FontVersion fv = new FontVersion();

                fv.CompressCatalog = (WaveCompressCatalog)Enum.Parse(typeof(WaveCompressCatalog),
                    ele.GetAttribute("compress"));
                fv.PcmCategory = (WaveFormatTag)Enum.Parse(typeof(WaveFormatTag),
                    ele.GetAttribute("encoding"));
                fv.SamplesPerSecond = int.Parse(ele.GetAttribute("samplesPerSecond"),
                    CultureInfo.InvariantCulture);
                fv.BytesPerSample = int.Parse(ele.GetAttribute("bytesPerSample"),
                    CultureInfo.InvariantCulture);
                fv.Name = ele.GetAttribute("name");

                try
                {
                    fv.Validate();
                }
                catch (NotSupportedException nse)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Not support version config found {0}.",
                        ele.OuterXml);
                    throw new InvalidDataException(message, nse);
                }

                versions.Add(fv);
            }
        }

        #endregion

        #region Public instance methods

        /// <summary>
        /// Save FontCompiler's config data into XML file.
        /// </summary>
        /// <param name="filePath">Target file to save.</param>
        public void Save(string filePath)
        {
            XmlDocument dom = new XmlDocument();
            XmlSchema schema = FontCompilerConfig.ConfigSchema;
            dom.NameTable.Add(schema.TargetNamespace);

            // root element
            XmlElement ele = dom.CreateElement("fontCompiler", schema.TargetNamespace);
            if (!string.IsNullOrEmpty(WorksiteDir))
            {
                ele.SetAttribute("workSite", WorksiteDir);
            }

            // Optional language data path
            if (_languageData != null)
            {
                _languageData.SaveLanguageData(dom, schema, ele);
            }

            // speaker metadata
            XmlElement speakerEle = dom.CreateElement("speaker", schema.TargetNamespace);
            speakerEle.SetAttribute("language", Localor.LanguageToString(Language));
            ele.AppendChild(speakerEle);

            // version
            XmlElement versionsEle = CreateVersionsXmlElement(dom, schema.TargetNamespace, Versions);

            ele.AppendChild(versionsEle);

            // script
            XmlElement scriptDirEle = dom.CreateElement("scriptFile", schema.TargetNamespace);
            Debug.Assert(!string.IsNullOrEmpty(ScriptPath));
            scriptDirEle.SetAttribute("path", ScriptPath);
            ele.AppendChild(scriptDirEle);

            // file list map path
            XmlHelper.AppendElement(dom, ele, "fileList", "path", FilelistPath, schema);
            SaveSpeechData(dom, schema, ele);

            // drop sentence list
            XmlHelper.AppendElement(dom, ele, "dropFileList", "path", DropFileListPath, schema);

            // Domain list
            XmlHelper.AppendElement(dom, ele, "domainList", "path", DomainListFilePath, schema);

            // drop unit list
            XmlHelper.AppendElement(dom, ele, "dropUnitFile", "path", DropUnitList, schema);

            // hold unit list
            XmlHelper.AppendElement(dom, ele, "holdUnitFile", "path", HoldUnitList, schema);

            if (MergeItems.Count > 0)
            {
                XmlElement mergeItemsEle = dom.CreateElement("mergeItems", schema.TargetNamespace);
                foreach (MergeItem item in MergeItems)
                {
                    XmlHelper.AppendElement(dom, mergeItemsEle, "mergeItem", "path", item.Dir, schema);
                }

                ele.AppendChild(mergeItemsEle);
            }

            // import files
            XmlElement importEle = dom.CreateElement("import", schema.TargetNamespace);
            if (!string.IsNullOrEmpty(_voiceDataDir))
            {
                importEle.SetAttribute("path", VoiceDataDir);
            }

            XmlElement cartTrain = dom.CreateElement("cartTrain", schema.TargetNamespace);
            cartTrain.SetAttribute("twoPhaseMode", CartSplitLevel.ToString(CultureInfo.InvariantCulture));
            cartTrain.SetAttribute("minCandidates", CartMinCandidates.ToString(CultureInfo.InvariantCulture));

            Debug.Assert(!string.IsNullOrEmpty(_weightTable));
            XmlHelper.AppendElement(dom, importEle, "weightTable", "path", _weightTable, schema);
            if (Import.Count > 0)
            {
                foreach (string file in Import)
                {
                    XmlHelper.AppendElement(dom, importEle, "file", "path", file, schema);
                }
            }

            ele.AppendChild(importEle);

            // output directory
            XmlElement outputDirEle = dom.CreateElement("outputDir", schema.TargetNamespace);
            Debug.Assert(!string.IsNullOrEmpty(OutputDir));
            if (!Helper.IsValidPath(OutputDir))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "the OutputDir path [{0}] should be a valid path", OutputDir);
                throw new InvalidDataException(message);
            }

            outputDirEle.SetAttribute("path", OutputDir);
            ele.AppendChild(outputDirEle);

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
            Load(filePath, true);
        }

        /// <summary>
        /// Load configuration from file, for the import files allow relative path,
        /// And the relative path has dependency of FontCompiler assembly, so when
        /// Other program load config files using this method, should not check
        /// Import files.
        /// </summary>
        /// <param name="filePath">XML configuration file path.</param>
        /// <param name="checkImportFiles">Wheher check import files.</param>
        public void Load(string filePath, bool checkImportFiles)
        {
            Load(filePath, checkImportFiles, true);
        }

        /// <summary>
        /// Load configuration from file, for the import files allow relative path,
        /// And the relative path has dependency of FontCompiler assembly, so when
        /// Other program load config files using this method, should not check
        /// Import files.
        /// </summary>
        /// <param name="filePath">XML configuration file path.</param>
        /// <param name="checkImportFiles">Wheher check import files.</param>
        /// <param name="updateScriptPath">Whether update script path.</param>
        public void Load(string filePath, bool checkImportFiles, bool updateScriptPath)
        {
            // Check the configuration file first
            try
            {
                XmlHelper.Validate(filePath, ConfigSchema);
            }
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The configuration file [{0}] error is found.",
                    filePath);
                throw new InvalidDataException(message, ide);
            }

            // load configuration
            XmlDocument dom = new XmlDocument();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("tts", ConfigSchema.TargetNamespace);
            dom.Load(filePath);

            XmlNode node = dom.DocumentElement.SelectSingleNode(@"tts:speaker/@language", nsmgr);
            Language = Localor.StringToLanguage(node.InnerText);

            // test whether the namespace of the configuration file is designed
            if (string.Compare(dom.DocumentElement.NamespaceURI,
                ConfigSchema.TargetNamespace, StringComparison.OrdinalIgnoreCase) != 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The configuration xml file [{0}] must use the schema namespace [{1}]. Currently the config file uses namespace [{2}]",
                    filePath, ConfigSchema.TargetNamespace, dom.DocumentElement.NamespaceURI);
                throw new InvalidDataException(message);
            }

            if (dom.DocumentElement.HasAttribute("workSite"))
            {
                string workSite = dom.DocumentElement.GetAttribute("workSite");
                CheckPath(workSite, true, filePath, "workSite");
                WorksiteDir = workSite;
            }

            _languageData.ParseLanguageData(dom, nsmgr, "tts", true);
            _languageData.SetLanguageData(Language);

            ParseVersions(dom, nsmgr);

            ParseImports(dom, nsmgr, checkImportFiles);
            ParseCartTrain(dom, nsmgr);
            ParseSpeechData(dom, filePath, nsmgr);

            ParseDropSentenceList(dom, filePath, nsmgr);
            ParseDomainList(dom, filePath, nsmgr);
            ParseUnitList(dom, filePath, nsmgr);
            ParseOutputDir(dom, filePath, nsmgr);

            ParseMergeItems(dom, filePath, nsmgr);

            // script & filelist file parsing must follow merge items, since it will depend
            // on it to verify file existance.
            ParseFilelist(dom, filePath, nsmgr);
            ParseScriptFile(dom, filePath, nsmgr, updateScriptPath);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Check the directory specified in config file.
        /// </summary>
        /// <param name="path">Directory to check.</param>
        /// <param name="existing">If the directory is existing.</param>
        /// <param name="configPath">Config file.</param>
        /// <param name="pathName">Path name.</param>
        private static void CheckPath(string path, bool existing, string configPath, string pathName)
        {
            if (!Helper.IsValidPath(path))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "the {2} path [{0}] is invalid path, which is specified in config file [{1}]",
                    path, configPath, pathName);
                throw new InvalidDataException(message);
            }

            if (existing)
            {
                if (!Directory.Exists(path))
                {
                    throw Helper.CreateException(typeof(DirectoryNotFoundException),
                        path);
                }
            }
        }

        /// <summary>
        /// Save speech data locations.
        /// </summary>
        /// <param name="dom">XML document to save into.</param>
        /// <param name="schema">Schema of the configuration XML file.</param>
        /// <param name="ele">Element to append.</param>
        private void SaveSpeechData(XmlDocument dom, XmlSchema schema, XmlElement ele)
        {
            // wave 16k hz directory
            Debug.Assert(!string.IsNullOrEmpty(Wave16kDir));
            XmlHelper.AppendElement(dom, ele, "wave16kDir", "path", Wave16kDir, schema);

            // filtered wave 16k hz directory
            XmlHelper.AppendElement(dom, ele, "wave16kFilteredDir", "path", Wave16kFilteredDir, schema);

            // wave 16k hz directory
            Debug.Assert(!string.IsNullOrEmpty(EpochDir));
            XmlHelper.AppendElement(dom, ele, "epochDir", "path", EpochDir, schema);

            // alignment dir
            Debug.Assert(!string.IsNullOrEmpty(AlignmentDir));
            XmlHelper.AppendElement(dom, ele, "alignmentDir", "path", AlignmentDir, schema);
        }

        /// <summary>
        /// Parse XML document for output directory.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="filePath">XML configuration file path.</param>
        /// <param name="nsmgr">Namespace.</param>
        private void ParseOutputDir(XmlDocument dom, string filePath, XmlNamespaceManager nsmgr)
        {
            // OutputDir
            XmlNode node = dom.DocumentElement.SelectSingleNode(@"tts:outputDir/@path", nsmgr);
            CheckPath(node.InnerText, false, filePath, "OutputDir");
            OutputDir = node.InnerText;
        }

        /// <summary>
        /// Parse XML document for CART train configuration.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="nsmgr">Namespace.</param>
        private void ParseCartTrain(XmlDocument dom, XmlNamespaceManager nsmgr)
        {
            // cartTrain
            XmlNode node = dom.DocumentElement.SelectSingleNode(@"tts:cartTrain", nsmgr);
            if (node != null)
            {
                XmlAttribute mode = node.Attributes[@"splitLevel"];
                if (mode != null)
                {
                    CartSplitLevel = int.Parse(mode.Value, CultureInfo.InvariantCulture);
                }

                XmlAttribute candNum = node.Attributes[@"minCandidates"];
                if (candNum != null)
                {
                    CartMinCandidates = int.Parse(candNum.Value, CultureInfo.InvariantCulture);
                }
            }
        }

        /// <summary>
        /// Parse XML document for file list path.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="filePath">XML configuration file path.</param>
        /// <param name="nsmgr">Namespace.</param>
        private void ParseFilelist(XmlDocument dom, string filePath, XmlNamespaceManager nsmgr)
        {
            // FilelistPath
            XmlNode node = dom.DocumentElement.SelectSingleNode(@"tts:fileList/@path", nsmgr);
            if (node != null)
            {
                CheckPath(node.InnerText, false, filePath, "fileList");

                if (MergeItems.Count == 0 && !File.Exists(node.InnerText))
                {
                    // if merge enable, the filelist path will the target path
                    // this file will be the merge result of source filelist files
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Could not find the file list file [{0}], which is specified in config file [{1}]",
                        node.InnerText, filePath);
                    throw new FileNotFoundException(message);
                }

                FilelistPath = node.InnerText;
            }
            else
            {
                _filelistPath = null;
            }
        }

        /// <summary>
        /// Parse XML document for script file path.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="filePath">XML configuration file path.</param>
        /// <param name="nsmgr">Namespace.</param>
        /// <param name="updateScriptPath">Whether update script path.</param>
        private void ParseScriptFile(XmlDocument dom, string filePath, XmlNamespaceManager nsmgr,
            bool updateScriptPath)
        {
            // ScriptFilePath
            XmlNode node = dom.DocumentElement.SelectSingleNode(@"tts:scriptFile/@path", nsmgr);
            CheckPath(node.InnerText, false, filePath, "scriptFile");
            ScriptPath = node.InnerText;

            if (updateScriptPath && MergeItems.Count == 0)
            {
                // temporarily add codes to convert *.txt to *.xml 
                // will delete it when all scripts are converted to xml

                // Merge script if the input is a script folder
                ScriptPath = Path.Combine(OutputDir, @"Interm\MergedScript.xml");
                Helper.EnsureFolderExistForFile(ScriptPath);
                if (Directory.Exists(node.InnerText))
                {
                    string inputDir = node.InnerText;
                    if (Directory.GetFiles(inputDir, "*.xml", SearchOption.TopDirectoryOnly).Length == 0)
                    {
                        inputDir = Path.Combine(inputDir, "temp");
                        Helper.EnsureFolderExist(inputDir);
                        foreach (string input in Directory.GetFiles(
                            node.InnerText, "*.txt", SearchOption.TopDirectoryOnly))
                        {
                            string output = Path.Combine(inputDir, Path.GetFileNameWithoutExtension(input) + ".xml");
                            ScriptHelper.ConvertTwoLineScriptToXmlScript(input, output, Language);
                        }
                    }

                    TtsPhoneSet phoneSet = Localor.GetPhoneSet(Language);
                    XmlScriptValidateSetting validateSetting = new XmlScriptValidateSetting(phoneSet, null);
                    ScriptHelper.MergeScripts(inputDir, ScriptPath, false, validateSetting);

                    if (!inputDir.Equals(node.InnerText))
                    {
                        Directory.Delete(inputDir, true);
                    }
                }
                else if (File.Exists(node.InnerText))
                {
                    if (Path.GetExtension(node.InnerText).Equals(".txt"))
                    {
                        ScriptHelper.ConvertTwoLineScriptToXmlScript(node.InnerText, ScriptPath, Language);
                    }
                    else
                    {
                        File.Copy(node.InnerText, ScriptPath, true);
                    }
                }
                else if (!File.Exists(node.InnerText))
                {
                    // if merge enable, the script path will the target path
                    // this file will be the merge result of source script files
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Could not find the scriptFile file [{0}], which is specified in config file [{1}]",
                        node.InnerText, filePath);
                    throw new FileNotFoundException(message);
                }
            }
        }

        /// <summary>
        /// Parse XML document for drop sentence list.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="filePath">XML configuration file path.</param>
        /// <param name="nsmgr">Namespace.</param>
        private void ParseDropSentenceList(XmlDocument dom, string filePath, XmlNamespaceManager nsmgr)
        {
            XmlNode node = null;

            node = dom.DocumentElement.SelectSingleNode(@"tts:dropFileList/@path", nsmgr);
            if (node != null)
            {
                if (string.IsNullOrEmpty(node.InnerText) || !File.Exists(node.InnerText))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Could not find the drop sentence list file [{0}], which is specified in config file [{1}]",
                        node.InnerText, filePath);
                    throw new FileNotFoundException(message);
                }

                _dropFileListPath = node.InnerText;
                XmlHelper.Validate(DropFileListPath, SentenceList.ConfigSchema);
            }
            else
            {
                _dropFileListPath = null;
            }
        }

        /// <summary>
        /// Parse XML document for domain list.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="filePath">XML configuration file path.</param>
        /// <param name="nsmgr">Namespace.</param>
        private void ParseDomainList(XmlDocument dom, string filePath, XmlNamespaceManager nsmgr)
        {
            XmlNode node = null;

            node = dom.DocumentElement.SelectSingleNode(@"tts:domainList/@path", nsmgr);
            if (node != null)
            {
                if (string.IsNullOrEmpty(node.InnerText) || !File.Exists(node.InnerText))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Could not find the domain list file [{0}], which is specified in config file [{1}]",
                        node.InnerText, filePath);
                    throw new FileNotFoundException(message);
                }

                _domainListFilePath = node.InnerText;
                XmlHelper.Validate(DomainListFilePath, DomainConfig.Schema);
            }
            else
            {
                _domainListFilePath = null;
            }
        }

        /// <summary>
        /// Parse XML document for unit list.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="filePath">XML configuration file path.</param>
        /// <param name="nsmgr">Namespace.</param>
        private void ParseUnitList(XmlDocument dom, string filePath, XmlNamespaceManager nsmgr)
        {
            XmlNode node = null;

            // HoldUnitList
            node = dom.DocumentElement.SelectSingleNode(@"tts:holdUnitFile/@path", nsmgr);
            if (node != null)
            {
                if (string.IsNullOrEmpty(node.InnerText) || !File.Exists(node.InnerText))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Could not find the hold unit list file [{0}], which is specified in config file [{1}]",
                        node.InnerText, filePath);
                    throw new FileNotFoundException(message);
                }

                HoldUnitList = node.InnerText;
                XmlHelper.Validate(HoldUnitList, UnitListDictionary.ConfigSchema);
            }
            else
            {
                _holdUnitList = null;
            }

           // DropUnitList
            node = dom.DocumentElement.SelectSingleNode(@"tts:dropUnitFile/@path", nsmgr);
            if (node != null)
            {
                if (string.IsNullOrEmpty(node.InnerText) || !File.Exists(node.InnerText))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Could not find the drop unit list file [{0}], which is specified in config file [{1}]",
                        node.InnerText, filePath);
                    throw new FileNotFoundException(message);
                }

                DropUnitList = node.InnerText;
                XmlHelper.Validate(DropUnitList, UnitListDictionary.ConfigSchema);
            }
            else
            {
                _dropUnitList = null;
            }
        }

        /// <summary>
        /// Parse XML document for Speech Data.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="filePath">XML configuration file path.</param>
        /// <param name="nsmgr">Namespace.</param>
        private void ParseSpeechData(XmlDocument dom, string filePath, XmlNamespaceManager nsmgr)
        {
            XmlNode node;

            ParseWaveformData(dom, filePath, nsmgr);

            // Load epoch dir settting
            node = dom.DocumentElement.SelectSingleNode(@"tts:epochDir/@path", nsmgr);
            CheckPath(node.InnerText, true, filePath, "epochDir");
            EpochDir = node.InnerText;

            // Load segment settting
            node = dom.DocumentElement.SelectSingleNode(@"tts:alignmentDir/@path", nsmgr);
            CheckPath(node.InnerText, true, filePath, "alignmentDir");
            AlignmentDir = node.InnerText;
        }

        /// <summary>
        /// Parse XML document for waveform data.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="filePath">XML configuration file path.</param>
        /// <param name="nsmgr">Namespace.</param>
        private void ParseWaveformData(XmlDocument dom, string filePath, XmlNamespaceManager nsmgr)
        {
            bool build8kFont = false;
            foreach (FontVersion fv in Versions)
            {
                if (fv.SamplesPerSecond == 8000)
                {
                    build8kFont = true;
                }
            }

            XmlNode node = null;

           // Load wave 16k dir settting
            node = dom.DocumentElement.SelectSingleNode(@"tts:wave16kDir/@path", nsmgr);
            CheckPath(node.InnerText, true, filePath, "wave16kDir");
            Wave16kDir = node.InnerText;

            // Load wave 16k filtered dir settting
            node = dom.DocumentElement.SelectSingleNode(@"tts:wave16kFilteredDir/@path", nsmgr);
            if (node != null)
            {
                CheckPath(node.InnerText, true, filePath, "wave16kFilteredDir");
                Wave16kFilteredDir = node.InnerText;
            }
            else if (build8kFont)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The wave16kFilteredDir should be specified to build 8K hz voice font, which is not specified in config file [{0}]",
                    filePath);
                throw new InvalidDataException(message);
            }
        }

        /// <summary>
        /// Parse XML document for Font Versions.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="nsmgr">Namespace.</param>
        private void ParseVersions(XmlDocument dom, XmlNamespaceManager nsmgr)
        {
            XmlNodeList versionsNodeList = dom.DocumentElement.SelectNodes(@"tts:versions/tts:version", nsmgr);
            ParseVersions(versionsNodeList, nsmgr, Versions);
        }

        /// <summary>
        /// Parse XML document for Import files.
        /// </summary>
        /// <param name="dom">XmlDocument to be parsed.</param>
        /// <param name="nsmgr">XmlNameSpace.</param>
        /// <param name="checkImportFiles">Whether check import files.</param>
        private void ParseImports(XmlDocument dom, XmlNamespaceManager nsmgr, bool checkImportFiles)
        {
            XmlNode nodeImportPath = dom.DocumentElement.SelectSingleNode(@"tts:import/@path", nsmgr);
            XmlNodeList importsNodeList = dom.DocumentElement.SelectNodes(@"tts:import/tts:file", nsmgr);
            Import.Clear();

            if (nodeImportPath != null)
            {
                _voiceDataDir = nodeImportPath.InnerText;
            }

            foreach (XmlNode node in importsNodeList)
            {
                XmlElement ele = (XmlElement)node;
                string path = ele.GetAttribute("path");

                try
                {
                    if (checkImportFiles)
                    {
                        // Leverage Directory's search pattern
                        // Support name matching pattern in the path, which should be ended with '*'
                        string rootedPath = Helper.GetFullPath(VoiceDataDir, path);
                        
                        string[] files = Directory.GetFiles(Path.GetDirectoryName(rootedPath),
                            Path.GetFileName(rootedPath));
                        if (files.Length == 0)
                        {
                            string message = Helper.NeutralFormat(
                                "No import file is found at [{0}] to import. Config path is [{1}].", rootedPath, path);
                            throw new InvalidDataException(message);
                        }
                    }

                    if (string.CompareOrdinal(ele.Name, "weightTable") == 0)
                    {
                        WeightTable = path;
                    }
                    else
                    {
                        Import.Add(path);
                    }
                }
                catch (ArgumentException ae)
                {
                    string message = Helper.NeutralFormat(
                        "The path [{0}] to import is an invalid path", path);
                    throw new InvalidDataException(message, ae);
                }
            }
        }

        /// <summary>
        /// Parse XML document for MergeItems.
        /// </summary>
        /// <param name="dom">XML configuration document.</param>
        /// <param name="filePath">XML configuration file path.</param>
        /// <param name="nsmgr">Namespace.</param>
        private void ParseMergeItems(XmlDocument dom, string filePath, XmlNamespaceManager nsmgr)
        {
            XmlNodeList items = dom.DocumentElement.SelectNodes(@"tts:mergeItems/tts:mergeItem", nsmgr);
            MergeItems.Clear();
            if (items == null)
            {
                return;
            }

            foreach (XmlNode node in items)
            {
                XmlElement ele = (XmlElement)node;

                string mergePath = ele.GetAttribute("path");
                CheckPath(mergePath, true, filePath, "mergeItem");
                MergeItem mi = new MergeItem(mergePath);

                // check script and file list files
                ErrorSet errorSet = mi.Validate(this.Language);
                if (errorSet.Count > 0)
                {
                    errorSet.Export(Console.Out);
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid data found in the mergeItem path [{0}], which is specified in config file [{1}]",
                        mergePath, filePath);
                    throw new InvalidDataException(message);
                }

                MergeItems.Add(mi);
            }
        }

        #endregion
    }
}