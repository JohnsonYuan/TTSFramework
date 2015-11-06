//----------------------------------------------------------------------------
// <copyright file="Localor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Language localor functions
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Language type
    /// Reference: http://www.microsoft.com/globaldev/nlsweb/.
    /// </summary>
    public enum Language
    {
        /// <summary>
        /// Language Independent.
        /// </summary>
        Neutral = 0,

        /// <summary>
        /// Arabic (Egypt).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        ArEG = 3073,

        /// <summary>
        /// Catalan (Spain).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        CaES = 1027,

        /// <summary>
        /// Danish (Denmark).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        DaDK = 1030,

        /// <summary>
        /// German (Austria).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        DeAT = 3079,

        /// <summary>
        /// German (Switzerland).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        DeCH = 2055,

        /// <summary>
        /// German (Germany).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        DeDE = 1031,

        /// <summary>
        /// English (Australia).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        EnAU = 3081,

        /// <summary>
        /// English (Canada).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        EnCA = 4105,

        /// <summary>
        /// English (United Kingdom).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        EnGB = 2057,

        /// <summary>
        /// English (India).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        EnIN = 16393,

        /// <summary>
        /// English (United States).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        EnUS = 1033,

        /// <summary>
        /// Spanish (Spain).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        EsES = 3082,

        /// <summary>
        /// Spanish (Mexico).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        EsMX = 2058,

        /// <summary>
        /// Finnish (Finland).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        FiFI = 1035,

        /// <summary>
        /// French (Belgium).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        FrBE = 2060,

        /// <summary>
        /// French (Canada).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        FrCA = 3084,

        /// <summary>
        /// French (Switzerland).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        FrCH = 4108,

        /// <summary>
        /// French (France).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        FrFR = 1036,
        
        /// <summary>
        /// Hindi (India).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        HiIN = 1081,

        /// <summary>
        /// Italian (Italy).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        ItIT = 1040,

        /// <summary>
        /// Japanese (Japan).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        JaJP = 1041,

        /// <summary>
        /// Korean (Korea).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        KoKR = 1042,

        /// <summary>
        /// Norwegian, Bokmål (Norway).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        NbNO = 1044,

        /// <summary>
        /// Dutch (Belgium).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        NlBE = 2067,

        /// <summary>
        /// Dutch (Netherlands).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        NlNL = 1043,

        /// <summary>
        /// Polish (Poland).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        PlPL = 1045,

        /// <summary>
        /// Portuguese (Brazil).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        PtBR = 1046,

        /// <summary>
        /// Portuguese (Portugal).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        PtPT = 2070,

        /// <summary>
        /// Russian (Russia).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        RuRU = 1049,

        /// <summary>
        /// Swedish (Sweden).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        SvSE = 1053,

        /// <summary>
        /// Turkish (Turkey).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        TrTR = 1055,

        /// <summary>
        /// Chinese, Simplified (People's Republic of China).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        ZhCN = 2052,

        /// <summary>
        /// Chinese, Traditional (Hong Kong S.A.R.).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        ZhHK = 3076,

        /// <summary>
        /// Chinese, Traditional (Taiwan).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        ZhTW = 1028
    }

    /// <summary>
    /// Localor for language related functions.
    /// </summary>
    public static class Localor
    {
        #region Public const fields

        /// <summary>
        /// Unit table default file name.
        /// </summary>
        public const string UnitTableFileName = "UnitTable.xml";

        /// <summary>
        /// Phone truncation rules default file name.
        /// </summary>
        public const string TruncateRulesFileName = "TruncateRules.xml";

        /// <summary>
        /// TTS phoneset default file name.
        /// </summary>
        public const string PhoneSetFileName = "phoneset.xml";

        /// <summary>
        /// TTS lexicon default file name.
        /// </summary>
        public const string LexiconFileName = "Lexicon.xml";

        /// <summary>
        /// TTS schema default file name.
        /// </summary>
        public const string LexicalAttributeSchemaFileName = "schema.xml";

        /// <summary>
        /// TTS to SAPI viseme id mapping table default file name.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Viseme",
            Justification = "Viseme is a word")]
        public const string TtsToSapiVisemeIdFileName = "Tts2SapiVisemeId.xml";

        /// <summary>
        /// TTS to IPA phone mapping table default file name.
        /// </summary>
        public const string TtsToIpaPhoneFileName = "Tts2IpaPhone.xml";

        /// <summary>
        /// Font meta file.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "MetaFile", Justification = "this is not win32 metafile")]
        public const string FontMetaFileName = "FontMeta.xml";

        /// <summary>
        /// TTS to SR phone mapping table default file name.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        public const string TtsToSrPhoneFileName = "Tts2SrPhone.xml";

        /// <summary>
        /// Annotation regular express for script sentence
        /// 1. * emphasis
        /// 2. #\d break level.
        /// </summary>
        public const string AnnotationSymbols = @"((?:\*[234]?)|(?:#\d[FfcRr]?)|(?:#[FfcRr]))";

        /// <summary>
        /// Language naming pattern.
        /// </summary>
        public const string LanguageNamePattern = @"^([A-Z])([a-z])([A-Z][A-Z])$|^Neutral$";

        #endregion

        #region Private static fields

        private static Dictionary<int, Phoneme> _phonemeMaps =
                                        new Dictionary<int, Phoneme>();

        private static Dictionary<Language, SliceData> _sliceDataMaps =
                                        new Dictionary<Language, SliceData>();

        private static Dictionary<Language, TruncateRuleData> _truncateRuleDataMaps =
                                        new Dictionary<Language, TruncateRuleData>();

        private static Dictionary<Language, FeatureMeta> _featureMetaMaps =
                                        new Dictionary<Language, FeatureMeta>();

        private static Dictionary<Language, Dictionary<string, string>> _languageDataFiles =
            new Dictionary<Language, Dictionary<string, string>>();

        private static Dictionary<Language, TtsPhoneSet> _ttsPhoneSetMap = new Dictionary<Language, TtsPhoneSet>();

        private static Dictionary<Language, Lexicon> _ttsLexiconMap = new Dictionary<Language, Lexicon>();

        private static Dictionary<Language, LexicalAttributeSchema> _ttsLexicalAttributeSchemaMap = new Dictionary<Language, LexicalAttributeSchema>();

        private static XmlSchema _schema;

        #endregion

        /// <summary>
        /// Gets Configuration schema.
        /// </summary>
        public static XmlSchema ConfigSchema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Config.OfflineLanguageConfig.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Reset the language data in the global dictionary.
        /// </summary>
        /// <param name="language">Language.</param>
        public static void ResetLanguageSetting(Language language)
        {
            int key = ToLanguageKey(language, EngineType.Tts30);
            if (_phonemeMaps.ContainsKey(key))
            {
                _phonemeMaps.Remove(key);
            }

            if (_sliceDataMaps.ContainsKey(language))
            {
                _sliceDataMaps.Remove(language);
            }

            if (_featureMetaMaps.ContainsKey(language))
            {
                _featureMetaMaps.Remove(language);
            }

            if (_truncateRuleDataMaps.ContainsKey(language))
            {
                _truncateRuleDataMaps.Remove(language);
            }

            if (_ttsPhoneSetMap.ContainsKey(language))
            {
                _ttsPhoneSetMap.Remove(language);
            }

            if (_languageDataFiles.ContainsKey(language))
            {
                _languageDataFiles.Remove(language);
            }
        }

        /// <summary>
        /// Set language data files.
        /// </summary>
        /// <param name="language">Language to be set.</param>
        /// <param name="dataFileName">Data file name.</param>
        /// <param name="dataFilePath">Data file path.</param>
        public static void SetLanguageDataFile(Language language, string dataFileName, string dataFilePath)
        {
            if (string.IsNullOrEmpty(dataFileName))
            {
                throw new ArgumentNullException("dataFileName");
            }

            if (string.IsNullOrEmpty(dataFilePath))
            {
                throw new ArgumentNullException("dataFilePath");
            }

            if (!File.Exists(dataFilePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException), dataFilePath);
            }

            if (!_languageDataFiles.ContainsKey(language))
            {
                _languageDataFiles.Add(language, new Dictionary<string, string>());
            }

            Dictionary<string, string> dataFiles = _languageDataFiles[language];
            if (!dataFiles.ContainsKey(dataFileName))
            {
                dataFiles.Add(dataFileName, dataFilePath);
            }
            else
            {
                dataFiles[dataFileName] = dataFilePath;
            }

            Trace.WriteLine(Helper.NeutralFormat("Use language data {1} from [{0}]", dataFilePath, dataFileName));
        }

        /// <summary>
        /// Clear language data files.
        /// </summary>
        /// <param name="language">Language to be set.</param>
        /// <param name="dataFileName">Data file name.</param>
        public static void ClearLanguageDataFile(Language language, string dataFileName)
        {
            if (string.IsNullOrEmpty(dataFileName))
            {
                throw new ArgumentNullException("dataFileName");
            }

            if (!_languageDataFiles.ContainsKey(language))
            {
                _languageDataFiles.Add(language, new Dictionary<string, string>());
            }

            Dictionary<string, string> dataFiles = _languageDataFiles[language];
            if (dataFiles.ContainsKey(dataFileName))
            {
                dataFiles.Remove(dataFileName);
            }

            Trace.WriteLine(Helper.NeutralFormat("Clear language data from [{0}]", dataFileName));
        }

        /// <summary>
        /// Get user customized language data file.
        /// </summary>
        /// <param name="language">Language of the data file.</param>
        /// <param name="dataFileName">Data file name.</param>
        /// <returns>Data file path.</returns>
        public static string GetLanguageDataFile(Language language, string dataFileName)
        {
            string dataFilePath = string.Empty;

            if (_languageDataFiles.ContainsKey(language))
            {
                if (_languageDataFiles[language].ContainsKey(dataFileName))
                {
                    dataFilePath = _languageDataFiles[language][dataFileName];
                }
            }

            return dataFilePath;
        }

        /// <summary>
        /// Throw exception if stocked language data fails to load.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <param name="fileName">File name.</param>
        public static void ReportMissingStockedLanguageData(string language, string fileName)
        {
            string message = Helper.NeutralFormat(
                @"Cannot load stocked {1} data of Language {0}. " + 
                @"The file can be put into ToolCurrentFolder\DataFiles\LanguageName. " +
                @"For exmaple, for en-US, it is ToolCurrentFolder\DataFiles\en-US.",
                language, fileName);
            Trace.WriteLine(message);
            throw new NotSupportedException(message);
        }

        /// <summary>
        /// LoadResource will look for resource in the following order:
        /// <param />
        /// 1. Try to load resorce from customized path explicitly set by tools.
        /// <param />
        /// 2. Load embedded resource files, most of which are referenced to latest data path.
        /// <param />
        /// 3. Use stocked resources files as the last resort.
        /// <param />
        /// </summary>
        /// <param name="language">Language of resource to load.</param>
        /// <param name="fileName">Resource file name.</param>
        /// <returns>Stream reader pointing to the found resource, null if not found.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static StreamReader LoadResource(Language language, string fileName)
        {
            const string LanguageFolderName = "Language";

            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            StreamReader resourceStream = null;

            // Try to load resource from customized path.
            string dataFile = GetLanguageDataFile(language, fileName);

            if (!string.IsNullOrEmpty(dataFile) && File.Exists(dataFile))
            {
                string message = Helper.NeutralFormat("Localor: load [{0}] data from file [{1}].",
                    LanguageToString(language), dataFile);
                Trace.WriteLine(message);
                resourceStream = new StreamReader(dataFile);
            }

            // If above fails, try to load embedded resource.
            if (resourceStream == null)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = "Microsoft.Tts.Offline." + LanguageFolderName + "." +
                    LanguageToResourceString(language) + "." + fileName;

                Stream stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    string message = Helper.NeutralFormat("Localor: load [{0}] data from resource [{1}].",
                        LanguageToString(language), resourceName);
                    Trace.WriteLine(message);
                    resourceStream = new StreamReader(stream);
                }
            }

            // If above fails, try to use stocked resource such as DataFiles\en-US\phoneset.xml.
            if (resourceStream == null)
                {
                    string relativeDataFilePath = Path.Combine(LanguageToString(language), fileName);
                    relativeDataFilePath = Path.Combine("DataFiles", relativeDataFilePath);
                    dataFile = Helper.SearchFilePath(relativeDataFilePath);

                    if (!string.IsNullOrEmpty(dataFile) && File.Exists(dataFile))
                    {
                        string message = Helper.NeutralFormat("Localor: load [{0}] data from file [{1}].",
                            LanguageToString(language), dataFile);
                        Trace.WriteLine(message);
                        resourceStream = new StreamReader(dataFile);
                    }
                }

            return resourceStream;
        }

        /// <summary>
        /// Convert language name string to language type. (RFC 3066).
        /// </summary>
        /// <param name="languageName">Language string presentation.</param>
        /// <returns>Language enum.</returns>
        public static Language StringToLanguage(string languageName)
        {
            if (string.IsNullOrEmpty(languageName))
            {
                throw new ArgumentNullException("languageName");
            }

            if (languageName.Equals(Language.Neutral.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return Language.Neutral;
            }

            try
            {
                // CultureInfo ci = CultureInfo.GetCultureInfo(languageName);
                // Remove dash '-' in the name
                StringBuilder name = new StringBuilder(languageName.Replace("-", string.Empty));

                // Convert the first letter to upper case
                name[0] = char.ToUpper(name[0], CultureInfo.InvariantCulture);

                return (Language)Enum.Parse(typeof(Language), name.ToString(), true);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException(Helper.NeutralFormat(
                    "The language [{0}] is not supported. Please check the language name.", languageName));
            }
        }

        /// <summary>
        /// RFC 3066 style name.
        /// Http://msdn2.microsoft.com/en-us/library/system.globalization.cultureinfo.aspx.
        /// </summary>
        /// <param name="lang">Language enum.</param>
        /// <returns>Language string presentation.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Ignore.")]
        public static string LanguageToString(Language lang)
        {
            string name = Language.Neutral.ToString();

            Match match = Regex.Match(lang.ToString(), Localor.LanguageNamePattern);
            if (!match.Success)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Unsupported language name [{0}] found.", lang.ToString());
                throw new NotSupportedException(message);
            }

            if (match.Groups.Count != 1)
            {
                Debug.Assert(match.Groups.Count == 4);

                // match "^([A-Z])([a-z])([A-Z][A-Z])$"
                if (!string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    name = match.Groups[1].Value.ToLower(CultureInfo.InvariantCulture) +
                        match.Groups[2].Value +
                        "-" +
                        match.Groups[3];
                }
            }

            return name;
        }

        /// <summary>
        /// Get phoneme for specified language.
        /// </summary>
        /// <param name="language">Which language to get.</param>
        /// <param name="engine">Which engine to get.</param>
        /// <returns>Phoneme instance.</returns>
        public static Phoneme GetPhoneme(Language language, EngineType engine)
        {
            Phoneme phomeme = null;

            int key = ToLanguageKey(language, engine);
            string languageName = language.ToString();

            if (!_phonemeMaps.ContainsKey(key))
            {
                if (engine == EngineType.Tts20)
                {
                    languageName = languageName + EngineType.Tts20.ToString();
                }
                else
                {
                    phomeme = Phoneme.Create(language);
                }

                if (phomeme != null)
                {
                    _phonemeMaps.Add(key, phomeme);
                }
                else
                {
                    try
                    {
                        string typeName = "Microsoft.Tts.Offline." + languageName + "Phoneme";
                        Type phonemeType = typeof(Phoneme);
                        phomeme = (Phoneme)phonemeType.Assembly.CreateInstance(typeName);
                        if (phomeme != null)
                        {
                            _phonemeMaps.Add(key, phomeme);
                        }
                    }
                    catch (MissingMethodException mme)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "Language {0} is not implemented.", language);
                        throw new NotSupportedException(message, mme);
                    }
                }
            }
            else
            {
                phomeme = _phonemeMaps[key];
            }

            if (phomeme == null)
            {
                ReportMissingStockedLanguageData(LanguageToString(language), Localor.PhoneSetFileName);
            }

            return phomeme;
        }

        /// <summary>
        /// Get the default TTS phone set.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <returns>Phone set.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static TtsPhoneSet GetPhoneSet(Language language)
        {
            TtsPhoneSet phoneSet = null;
            if (_ttsPhoneSetMap.ContainsKey(language))
            {
                phoneSet = _ttsPhoneSetMap[language];
            }
            else
            {
                using (StreamReader reader = Localor.LoadResource(language, Localor.PhoneSetFileName))
                {
                    if (reader != null)
                    {
                        phoneSet = new TtsPhoneSet(language);
                        phoneSet.Load(reader);
                        _ttsPhoneSetMap[language] = phoneSet;
                    }
                }
            }

            return phoneSet;
        }

        /// <summary>
        /// Get Lexicon specific language.
        /// </summary>
        /// <param name="language">The language.</param>
        /// <returns>The lexicon.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static Lexicon GetLexicon(Language language)
        {
            Lexicon lexicon = null;
            if (_ttsLexiconMap.ContainsKey(language))
            {
                lexicon = _ttsLexiconMap[language];
            }
            else
            {
                using (StreamReader reader = Localor.LoadResource(language, Localor.LexiconFileName))
                {
                    if (reader != null)
                    {
                        lexicon = new Lexicon(language);
                        lexicon.Load(reader);
                        _ttsLexiconMap[language] = lexicon;
                    }
                }
            }

            return lexicon;
        }

        /// <summary>
        /// Get Lexicon specific language.
        /// </summary>
        /// <param name="language">The language.</param>
        /// <returns>The lexical attribute schema.</returns>
        public static LexicalAttributeSchema GetLexicalAttributeSchema(Language language)
        {
            LexicalAttributeSchema schema = null;
            if (_ttsLexicalAttributeSchemaMap.ContainsKey(language))
            {
                schema = _ttsLexicalAttributeSchemaMap[language];
            }
            else
            {
                using (StreamReader reader = Localor.LoadResource(language, Localor.LexicalAttributeSchemaFileName))
                {
                    if (reader != null)
                    {
                        schema = new LexicalAttributeSchema(language);
                        schema.Load(reader);
                        _ttsLexicalAttributeSchemaMap[language] = schema;
                    }
                }
            }

            return schema;
        }

        /// <summary>
        /// Get phoneme for specified language, default is for TTS 3.0.
        /// </summary>
        /// <param name="language">Which language to get.</param>
        /// <returns>Phoneme instance.</returns>
        public static Phoneme GetPhoneme(Language language)
        {
            return GetPhoneme(language, EngineType.Tts30);
        }

        /// <summary>
        /// Create script file instance for specified language.
        /// </summary>
        /// <param name="language">Which language to create for.</param>
        /// <returns>ScriptFile.</returns>
        public static ScriptFile CreateScriptFile(Language language)
        {
            return CreateScriptFile(language, EngineType.Tts30);
        }

        /// <summary>
        /// Create script file instance for specified language and engine type.
        /// </summary>
        /// <param name="language">Which language to create for.</param>
        /// <param name="engine">Engine type.</param>
        /// <returns>Script file instance.</returns>
        public static ScriptFile CreateScriptFile(Language language, EngineType engine)
        {
            if (language == Language.Neutral)
            {
                return new ScriptFile();
            }

            try
            {
                string typeName = "Microsoft.Tts.Offline." + language.ToString() + "ScriptFile";
                Type scriptFileType = typeof(ScriptFile);
                ScriptFile script = (ScriptFile)scriptFileType.Assembly.CreateInstance(typeName);

                if (script == null)
                {
                    // TODO: Enable logging here for easier diagnostics 
                    script = new ScriptFile(language);
                }

                script.EngineType = engine;
                return script;
            }
            catch (MissingMethodException mme)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Language {0} is not implemented.", language);
                throw new NotSupportedException(message, mme);
            }
        }

        /// <summary>
        /// Create script item instance for specified language and EngineType.Tts30.
        /// </summary>
        /// <param name="language">Which language to create for.</param>
        /// <returns>ScriptItem.</returns>
        public static ScriptItem CreateScriptItem(Language language)
        {
            return CreateScriptItem(language, EngineType.Tts30);
        }

        /// <summary>
        /// Create script item instance for specified language and engine.
        /// </summary>
        /// <param name="language">Which language to create for.</param>
        /// <param name="engine">Which Engine to create for.</param>
        /// <returns>ScriptItem.</returns>
        public static ScriptItem CreateScriptItem(Language language, EngineType engine)
        {
            if (language == Language.Neutral)
            {
                return new ScriptItem();
            }

            try
            {
                string typeName = "Microsoft.Tts.Offline." + language.ToString() + "ScriptItem";
                Type scriptItemType = typeof(ScriptItem);
                ScriptItem sciptItem = (ScriptItem)scriptItemType.Assembly.CreateInstance(typeName);

                if (sciptItem == null)
                {
                    // TODO: Enable logging here for easier diagnostics 
                    sciptItem = new ScriptItem(language);
                }

                sciptItem.Engine = engine;
                sciptItem.Language = language;
                return sciptItem;
            }
            catch (MissingMethodException mme)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Language {0} is not implemented.", language);
                throw new NotSupportedException(message, mme);
            }
        }

        /// <summary>
        /// Create slice data instance for specified language.
        /// </summary>
        /// <param name="language">Which language to create for.</param>
        /// <returns>SliceData.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static TruncateRuleData GetTruncateRuleData(Language language)
        {
            if (!_truncateRuleDataMaps.ContainsKey(language))
            {
                try
                {
                    TruncateRuleData truncateRuleData = LoadTruncateRuleData(language);
                    if (truncateRuleData != null)
                    {
                        _truncateRuleDataMaps.Add(language, truncateRuleData);
                    }
                    else
                    {
                        string typeName = "Microsoft.Tts.Offline." + language.ToString() + "TruncateRule";
                        Type slideDataType = typeof(SliceData);
                        truncateRuleData = (TruncateRuleData)slideDataType.Assembly.CreateInstance(typeName);
                        if (truncateRuleData != null)
                        {
                            _truncateRuleDataMaps.Add(language, truncateRuleData);
                        }
                        else
                        {
                            ReportMissingStockedLanguageData(LanguageToString(language), Localor.TruncateRulesFileName);
                        }
                    }
                }
                catch (MissingMethodException mme)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Language {0} is not implemented.", LanguageToString(language));
                    throw new NotSupportedException(message, mme);
                }
            }

            return _truncateRuleDataMaps[language];
        }

        /// <summary>
        /// Create slice data instance for specified language.
        /// </summary>
        /// <param name="language">Which language to create for.</param>
        /// <returns>SliceData.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public static SliceData GetSliceData(Language language)
        {
            if (!_sliceDataMaps.ContainsKey(language))
            {
                try
                {
                    SliceData sliceData = LoadSliceData(language);
                    if (sliceData != null)
                    {
                        _sliceDataMaps.Add(language, sliceData);
                    }
                    else
                    {
                        string typeName = "Microsoft.Tts.Offline." + language.ToString() + "SliceData";
                        Type slideDataType = typeof(SliceData);
                        sliceData = (SliceData)slideDataType.Assembly.CreateInstance(typeName);
                        if (sliceData != null)
                        {
                            _sliceDataMaps.Add(language, sliceData);
                        }
                        else
                        {
                            ReportMissingStockedLanguageData(LanguageToString(language), Localor.UnitTableFileName);
                        }
                    }
                }
                catch (MissingMethodException mme)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Language {0} is not implemented.", LanguageToString(language));
                    throw new NotSupportedException(message, mme);
                }
            }

            return _sliceDataMaps[language];
        }

        /// <summary>
        /// Create feature meta for speicified language.
        /// </summary>
        /// <param name="language">Which language to create for.</param>
        /// <returns>FeatureMeta.</returns>
        public static FeatureMeta GetFeatureMeta(Language language)
        {
            if (!_featureMetaMaps.ContainsKey(language))
            {
                try
                {
                    FeatureMeta featureMeta = new FeatureMeta(language);
                    _featureMetaMaps.Add(language, featureMeta);
                }
                catch (MissingMethodException mme)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Language {0} is not implemented.", language);
                    throw new NotSupportedException(message, mme);
                }
            }

            return _featureMetaMaps[language];
        }

        /// <summary>
        /// Map from integer language id to enum Language type data.
        /// </summary>
        /// <param name="languageId">Language id.</param>
        /// <returns>Language enum.</returns>
        public static Language MapLanguageId(int languageId)
        {
            Debug.Assert(Helper.IndexOf(((Language)languageId).ToString(),
                Enum.GetNames(typeof(Language))) >= 0);
            return (Language)languageId;
        }

        /// <summary>
        /// Map from enum Language type data to integer language id.
        /// </summary>
        /// <param name="language">Language enum.</param>
        /// <returns>Language id.</returns>
        public static int MapLanguageId(Language language)
        {
            return (int)language;
        }

        /// <summary>
        /// Map word type to break level.
        /// </summary>
        /// <param name="wordType">Word type.</param>
        /// <returns>Break level.</returns>
        public static TtsBreak MapWordType2Break(WordType wordType)
        {
            switch (wordType)
            {
                case WordType.Normal:
                    return TtsBreak.Word;
                case WordType.Period:
                    return TtsBreak.Sentence;
                case WordType.Exclamation:
                    return TtsBreak.Sentence;
                case WordType.Question:
                    return TtsBreak.Sentence;
                case WordType.OtherPunctuation:
                    return TtsBreak.Word;
                default:
                    Debug.Assert(false);
                    string str1 = "Only Normal, Period, Exclamation, Question or ";
                    string str2 = "OtherPunctuation is supported for mapping from word type to break level. ";
                    string str3 = "But word type [{0}] is found.";
                    string message = string.Format(CultureInfo.InvariantCulture,
                        str1 + str2 + str3,
                        wordType);
                    throw new NotSupportedException(message);
            }
        }

        /// <summary>
        /// Map punctuation to type.
        /// </summary>
        /// <param name="punctuation">String to test.</param>
        /// <param name="punctuationPattern">Punctuation pattern.</param>
        /// <returns>Word type.</returns>
        public static WordType MapPunctuation(string punctuation,
            string punctuationPattern)
        {
            if (string.IsNullOrEmpty(punctuation))
            {
                throw new ArgumentNullException("punctuation");
            }

            // TODO: Move punctation marks into data driven for languages
            switch (punctuation)
            {
                case ".":
                    return WordType.Period;
                case "!":
                    return WordType.Exclamation;
                case "?":
                case "？":
                    return WordType.Question;
                default:
                    Match puncMatch = Regex.Match(punctuation,
                        @"^" + punctuationPattern + "$");
                    if (puncMatch.Success)
                    {
                        return WordType.OtherPunctuation;
                    }
                    else
                    {
                        return WordType.Normal;
                    }
            }
        }

        /// <summary>
        /// Reverse map slice type (prefix of slice name) to position in syllable.
        /// </summary>
        /// <param name="sliceType">Prefix string of slice position.</param>
        /// <returns>Slice Position.</returns>
        public static PosInSyllable ReverseMapSlicePos(string sliceType)
        {
            if (string.IsNullOrEmpty(sliceType))
            {
                throw new ArgumentNullException("sliceType");
            }

            switch (sliceType)
            {
                case "os":
                    return PosInSyllable.Onset;
                case "cd":
                    return PosInSyllable.Coda;
                case "nc":
                    return PosInSyllable.NucleusInV;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Only Onset, Coda or Nucleus is supported for reverse mapping position to syllable. But string [{0}] is found.",
                        sliceType);
                    throw new NotSupportedException(message);
            }
        }

        /// <summary>
        /// Map slice position.
        /// </summary>
        /// <param name="slicePos">Slice Position.</param>
        /// <returns>Prefix string of slice position.</returns>
        public static string MapSlicePos(PosInSyllable slicePos)
        {
            switch (slicePos)
            {
                case PosInSyllable.Onset:
                case PosInSyllable.OnsetNext:
                    return "os_";
                case PosInSyllable.NucleusInV:
                case PosInSyllable.NucleusInVC:
                case PosInSyllable.NucleusInCV:
                case PosInSyllable.NucleusInCVC:
                    return "nc_";
                case PosInSyllable.CodaNext:
                case PosInSyllable.Coda:
                    return "cd_";
                default:
                    System.Diagnostics.Debug.Assert(false);
                    string str1 = "Only Onset, OnsetNext, NucleusInV, NucleusInVC, NucleusInCV, NucleusInCVC, CodaNext or Coda is ";
                    string str2 = "supported for mapping position to syllable to string. ";
                    string str3 = "But position to syllable [{0}] is found.";
                    string message = string.Format(CultureInfo.InvariantCulture, str1 + str2 + str3, slicePos);
                    throw new NotSupportedException(message);
            }
        }

        #region TTS 2.0 Mulan lexicon

        /// <summary>
        /// Map from POS of TTS 2.0 Mulan English lexicon to SAPI POS.
        /// </summary>
        /// <param name="pos">POS string to map.</param>
        /// <returns>PartOfSpeech.</returns>
        internal static PartOfSpeech MapPos(string pos)
        {
            if (string.IsNullOrEmpty(pos))
            {
                throw new ArgumentNullException("pos");
            }

            switch (pos.Trim())
            {
                case "verb":
                    return PartOfSpeech.Verb;
                case "noun":
                    return PartOfSpeech.Noun;
                case "pron":
                    return PartOfSpeech.Noun;
                case "objpron":
                    return PartOfSpeech.Noun;
                case "subjpron":
                    return PartOfSpeech.Noun;
                case "conj":
                    return PartOfSpeech.Noun;
                case "cconj":
                    return PartOfSpeech.Noun;
                case "prep":
                    return PartOfSpeech.Noun;
                case "adv":
                    return PartOfSpeech.Noun;
                case "adj":
                    return PartOfSpeech.Noun;
                case "det":
                    return PartOfSpeech.Noun;
                case "vaux":
                    return PartOfSpeech.Noun;
                case "contr":
                    return PartOfSpeech.Noun;
                case "interr":
                    return PartOfSpeech.Noun;
                case "interjection":
                    return PartOfSpeech.Interjection;
                default:
                    Debug.Assert(false);
                    string message = string.Format(CultureInfo.InvariantCulture,
                            "Un-supported POS string [{0}] is found.",
                            pos);
                    throw new NotSupportedException(message);
            }
        }

        #endregion

        /// <summary>
        /// To language key which is combined language and engine type.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <param name="engine">Engine type.</param>
        /// <returns>Language key.</returns>
        private static int ToLanguageKey(Language language, EngineType engine)
        {
            const int LanguageKeyBase = 100000;
            Debug.Assert((int)language < LanguageKeyBase);

            return ((int)engine * LanguageKeyBase) + (int)language;
        }

        /// <summary>
        /// Load slice data for given language.
        /// </summary>
        /// <param name="language">Language to load for.</param>
        /// <returns>SliceData if load.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        private static TruncateRuleData LoadTruncateRuleData(Language language)
        {
            TruncateRuleData truncateRule = null;

            using (StreamReader ruleReader = LoadResource(language, Localor.TruncateRulesFileName))
            {
                if (ruleReader != null)
                {
                    truncateRule = new TruncateRuleData();
                    truncateRule.Language = language;
                    truncateRule.Load(ruleReader);
                }
            }

            return truncateRule;
        }

        /// <summary>
        /// Load slice data for given language.
        /// </summary>
        /// <param name="language">Language to load for.</param>
        /// <returns>SliceData if load.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        private static SliceData LoadSliceData(Language language)
        {
            SliceData sliceData = null;

            using (StreamReader unitTableReader = LoadResource(language, Localor.UnitTableFileName))
            {
                if (unitTableReader != null)
                {
                    sliceData = new SliceData();
                    sliceData.Language = language;
                    sliceData.Load(unitTableReader);
                }
            }

            return sliceData;
        }

        /// <summary>
        /// Convert language to resource string, which uses '_' instead of '-'.
        /// </summary>
        /// <param name="language">Language type to convert.</param>
        /// <returns>Result string.</returns>
        private static string LanguageToResourceString(Language language)
        {
            return LanguageToString(language).Replace('-', '_');
        }
    }
}