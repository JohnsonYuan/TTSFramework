//----------------------------------------------------------------------------
// <copyright file="ScriptFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements general format script file
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Engine Type.
    /// </summary>
    public enum EngineType
    {
        /// <summary>
        /// Mulan Engine, version 2.0.
        /// </summary>
        Tts20 = 0,

        /// <summary>
        /// ShanHai/ShenZhou Engine, version 3.0.
        /// </summary>
        Tts30 = 1
    }

    /// <summary>
    /// Script error collection.
    /// </summary>
    [Serializable]
    public class DataErrorSet
    {
        #region Fields

        private Collection<DataError> _errors = new Collection<DataError>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets Error collection in this set.
        /// </summary>
        public Collection<DataError> Errors
        {
            get { return _errors; }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Merge erros in errorSet to erros in this instance.
        /// </summary>
        /// <param name="errorSet">Source errors to copy.</param>
        public void Merge(DataErrorSet errorSet)
        {
            if (errorSet != null && errorSet.Errors != null)
            {
                foreach (DataError error in errorSet.Errors)
                {
                    _errors.Add(error);
                }
            }
        }

        /// <summary>
        /// Append this instance to text file.
        /// </summary>
        /// <param name="title">Title of this section.</param>
        /// <param name="filePath">Target file to append.</param>
        public void AppendSection(string title, string filePath)
        {
            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentNullException("title");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (Errors == null || Errors.Count == 0)
            {
                return;
            }

            Helper.EnsureFolderExistForFile(filePath);
            using (StreamWriter sw = new StreamWriter(filePath, true, Encoding.Unicode))
            {
                sw.Write("<");
                sw.Write(title);
                sw.WriteLine(">");

                foreach (DataError error in Errors)
                {
                    string message = error.ToString();
                    sw.WriteLine(message);
                }
            }
        }

        /// <summary>
        /// Fill sentence ids in the collection.
        /// </summary>
        /// <param name="ids">Id dictionary.</param>
        public void FillSentenceId(Dictionary<string, string> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException("ids");
            }

            foreach (DataError error in Errors)
            {
                if (string.IsNullOrEmpty(error.SentenceId))
                {
                    continue;
                }

                if (!ids.ContainsKey(error.SentenceId))
                {
                    ids.Add(error.SentenceId, error.SentenceId);
                }
            }
        }

        /// <summary>
        /// Dump to console.
        /// </summary>
        public void Dump2Console()
        {
            if (Errors.Count > 0)
            {
                foreach (DataError error in Errors)
                {
                    string message = error.ToString();
                    Console.Error.WriteLine(message);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Script error.
    /// </summary>
    [Serializable]
    public class DataError
    {
        #region Fields

        private string _sentenceId;
        private string _message;
        private string _filePath;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="DataError"/> class.
        /// </summary>
        public DataError()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataError"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        public DataError(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException("message");
            }

            Message = message;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataError"/> class.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <param name="message">Message.</param>
        public DataError(string path, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException("message");
            }

            if (!string.IsNullOrEmpty(path))
            {
                FilePath = path;
            }

            Message = message;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataError"/> class.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <param name="message">Message.</param>
        /// <param name="id">Id.</param>
        public DataError(string path, string message, string id)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException("message");
            }

            if (!string.IsNullOrEmpty(id))
            {
                SentenceId = id;
            }

            if (!string.IsNullOrEmpty(path))
            {
                FilePath = path;
            }

            Message = message;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Sentence identity of this error message.
        /// </summary>
        public string SentenceId
        {
            get
            {
                return _sentenceId;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _sentenceId = value;
            }
        }

        /// <summary>
        /// Gets or sets Detailed message about this error.
        /// </summary>
        public string Message
        {
            get
            {
                return _message;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _message = value;
            }
        }

        /// <summary>
        /// Gets or sets Script file path.
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

        #endregion

        #region Override object methods

        /// <summary>
        /// String presentation.
        /// </summary>
        /// <returns>String.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            // append sentence id
            if (!string.IsNullOrEmpty(SentenceId))
            {
                sb.Append(SentenceId);
            }

            // append filepath of that sentence
            if (!string.IsNullOrEmpty(FilePath))
            {
                if (sb.Length != 0)
                {
                    sb.Append(" ");
                }

                sb.AppendFormat(CultureInfo.InvariantCulture, "{0},", FilePath);
            }

            // append message
            if (sb.Length != 0)
            {
                sb.Append(" ");
            }

            sb.Append(Message);

            return sb.ToString();
        }

        #endregion
    }

    /// <summary>
    /// Definition of Script file. Data format:
    /// ((sentence id) (sentence content)(\n)(sentence pronunciation))+.
    /// <example>
    /// 104281 on wednesdays * after five p.m.
    ///       aa 1 n / w eh 1 n . z - d . ey . z / ae 1 . f - t . ax r / f . ay 1 . v / p . iy 1 / eh 1 m /.
    /// </example>
    /// </summary>
    public class ScriptFile
    {
        #region Const fields

        /// <summary>
        /// File extension for script file.
        /// </summary>
        public const string Extension = ".txt";

        /// <summary>
        /// The label for bad phone. E.g.  "a~" indicates that phone "a" is bad.
        /// </summary>
        public const string BadPhoneLabel = "~";

        #endregion

        #region Fields

        private string _filePath;
        private SortedDictionary<string, ScriptItem> _items = new SortedDictionary<string, ScriptItem>();

        private DataErrorSet _errorSet;
        private EngineType _engineType = EngineType.Tts30;

        private Language _language = Language.Neutral;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptFile"/> class.
        /// </summary>
        public ScriptFile()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptFile"/> class.
        /// </summary>
        /// <param name="language">Language.</param>
        public ScriptFile(Language language)
        {
            _language = language;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Data error set found in this script file.
        /// </summary>
        public DataErrorSet ErrorSet
        {
            get { return _errorSet; }
        }

        /// <summary>
        /// Gets Script entries of this script file.
        /// </summary>
        public SortedDictionary<string, ScriptItem> Items
        {
            get { return _items; }
        }

        /// <summary>
        /// Gets or sets File path of this script file.
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
        /// Gets or sets Language of this instance.
        /// </summary>
        public virtual Language Language
        {
            get { return _language; }
            set { _language = value; }
        }

        /// <summary>
        /// Gets or sets Engine type of this script file supports.
        /// </summary>
        public EngineType EngineType
        {
            get
            {
                return _engineType;
            }

            set
            {
                if (_engineType != value)
                {
                    Debug.Assert(_engineType == EngineType.Tts30, "Don't change EngineType two times.");
                    _engineType = value;
                }
            }
        }

        /// <summary>
        /// Gets Phoneme for this language script file.
        /// </summary>
        public virtual Phoneme Phoneme
        {
            get { return Localor.GetPhoneme(Language); }
        }

        #endregion

        #region Static operations

        /// <summary>
        /// Load script file into sentence id and script entry maped dictionary.
        /// </summary>
        /// <param name="scriptFilePath">Script file to read.</param>
        /// <param name="language">Language of the script.</param>
        /// <param name="engineType">Engine of the script to support.</param>
        /// <param name="outEntries">Output of script items.</param>
        /// <returns>Data error set found.</returns>
        public static DataErrorSet ReadAllData(string scriptFilePath,
            Language language, EngineType engineType,
            SortedDictionary<string, ScriptItem> outEntries)
        {
            if (string.IsNullOrEmpty(scriptFilePath))
            {
                throw new ArgumentNullException("scriptFilePath");
            }

            if (outEntries == null)
            {
                throw new ArgumentNullException("outEntries");
            }

            Collection<ScriptItem> entriesInCollection = new Collection<ScriptItem>();
            DataErrorSet errorSet = ReadAllData(scriptFilePath,
                language, engineType, entriesInCollection);

            foreach (ScriptItem entry in entriesInCollection)
            {
                if (outEntries.ContainsKey(entry.Id))
                {
                    DataError error = new DataError();
                    error.SentenceId = entry.Id;
                    error.Message = "Sentence id duplicated.";
                    error.FilePath = scriptFilePath;
                    errorSet.Errors.Add(error);
                    continue;
                }

                outEntries.Add(entry.Id, entry);
            }

            return errorSet;
        }

        /// <summary>
        /// Load script file into script entry collection, in order as in file.
        /// </summary>
        /// <param name="scriptFilePath">Script file to read.</param>
        /// <param name="outEntries">Output of script items.</param>
        /// <param name="withPron">Whether load script with pronunciation.</param>
        /// <param name="withSid">Whether load script with SID.</param>
        /// <returns>Data error set found.</returns>
        public static DataErrorSet ReadAllData(string scriptFilePath, Collection<ScriptItem> outEntries,
            bool withPron, bool withSid)
        {
            if (outEntries == null)
            {
                throw new ArgumentNullException("outEntries");
            }

            if (string.IsNullOrEmpty(scriptFilePath))
            {
                throw new ArgumentNullException("scriptFilePath");
            }

            return ReadAllData(scriptFilePath, Language.Neutral, EngineType.Tts30,
                outEntries, withPron, withSid, false);
        }

        /// <summary>
        /// Load script file into script entry collection, in order as in file.
        /// </summary>
        /// <param name="scriptFilePath">Script file to read.</param>
        /// <param name="language">Language of the script.</param>
        /// <param name="engineType">Engine of the script to support.</param>
        /// <param name="outEntries">Output of script items.</param>        
        /// <returns>Data error set found.</returns>
        public static DataErrorSet ReadAllData(string scriptFilePath,
            Language language, EngineType engineType,
            Collection<ScriptItem> outEntries)
        {
            if (outEntries == null)
            {
                throw new ArgumentNullException("outEntries");
            }

            if (string.IsNullOrEmpty(scriptFilePath))
            {
                throw new ArgumentNullException("scriptFilePath");
            }

            return ReadAllData(scriptFilePath, language, engineType, outEntries,
                true, true, true);
        }

        /// <summary>
        /// Load script file into script entry collection, in order as in file.
        /// </summary>
        /// <param name="scriptFilePath">Script file to read.</param>
        /// <param name="language">Language of the script.</param>
        /// <param name="engineType">Engine of the script to support.</param>
        /// <param name="outEntries">Output of script items.</param>
        /// <param name="withPron">Whether load script with pronunciation.</param>
        /// <param name="withSid">Whether load script with SID.</param>
        /// <param name="validate">Whether validate script item.</param>
        /// <returns>Data error set found.</returns>
        public static DataErrorSet ReadAllData(string scriptFilePath,
            Language language, EngineType engineType,
            Collection<ScriptItem> outEntries,
            bool withPron, bool withSid, bool validate)
        {
            if (outEntries == null)
            {
                throw new ArgumentNullException("outEntries");
            }

            if (string.IsNullOrEmpty(scriptFilePath))
            {
                throw new ArgumentNullException("scriptFilePath");
            }

            if (!File.Exists(scriptFilePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    scriptFilePath);
            }

            DataErrorSet errorSet = new DataErrorSet();

            using (StreamReader sr = new StreamReader(scriptFilePath))
            {
                try
                {
                    while (true)
                    {
                        ScriptItem scriptItem = Localor.CreateScriptItem(language, engineType);
                        DataError error = ReadOneScriptItem(sr, scriptItem, withPron, withSid, validate);

                        if (error != null)
                        {
                            // Attach file path information for the errors
                            error.FilePath = scriptFilePath;
                            errorSet.Errors.Add(error);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(scriptItem.Sentence))
                            {
                                // Succeed, and add loaded script item
                                outEntries.Add(scriptItem);
                            }
                            else
                            {
                                // End of stream reached
                                break;
                            }
                        }
                    }
                }
                catch (InvalidDataException ide)
                {
                    string message =
                        Helper.NeutralFormat("Failed to load script file [{0}]: {1}.", scriptFilePath, ide.Message);
                    throw new InvalidDataException(message, ide);
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Read one script item from the text stream reader.
        /// </summary>
        /// <param name="reader">Text stream to read out from.</param>
        /// <param name="scriptItem">Result script item.</param>
        /// <param name="withPron">Flag to indicate whether text stream is
        /// with pronunciation for each sentence.</param>
        /// <param name="withSid">Flag to indicate whether text stream is
        /// with sentence id for each sentence.</param>
        /// <param name="validate">Whether validate script item.</param>
        /// <returns>Data error found during reading, otherwise null returned.</returns>
        public static DataError ReadOneScriptItem(StreamReader reader, ScriptItem scriptItem,
            bool withPron, bool withSid, bool validate)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            if (reader.CurrentEncoding == null)
            {
                string message = Helper.NeutralFormat("reader.CurrentEncoding should not be null.");
                throw new ArgumentException(message);
            }

            if (scriptItem == null)
            {
                throw new ArgumentNullException("scriptItem");
            }

            string sentenceLine = null;
            string pronunciationLine = null;

            // Read the sentence content line
            while ((sentenceLine = reader.ReadLine()) != null)
            {
                // Skip empty line for sentence
                if (string.IsNullOrEmpty(sentenceLine))
                {
                    continue;
                }
                else
                {
                    if (reader.CurrentEncoding.CodePage != Encoding.Unicode.CodePage)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "The script file must be saved in Unicode.");
                        throw new InvalidDataException(message);
                    }

                    break;
                }
            }

            if (string.IsNullOrEmpty(sentenceLine))
            {
                // End of file reached
                return null;
            }

            if (withPron)
            {
                // Read the pronunciation line
                while ((pronunciationLine = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(pronunciationLine))
                    {
                        break;
                    }
                }

                if (string.IsNullOrEmpty(pronunciationLine))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid format, empty pronunciation string for sentence: '{0}', pronunciation: '{1}'.",
                        sentenceLine, pronunciationLine);
                    return new DataError(message);
                }
            }

            return ReadOneScriptItem(scriptItem, sentenceLine, pronunciationLine, withPron, withSid, validate);
        }

                /// <summary>
        /// Read one script item from the sentence content line and pronunciation line.
        /// </summary>
        /// <param name="scriptItem">Result script item.</param>
        /// <param name="sentenceLine">Sentence content line.</param>
        /// <param name="pronunciationLine">Pronunciation line.</param>
        /// <param name="withPron">Flag to indicate whether text stream is
        /// with pronunciation for each sentence.</param>
        /// <param name="withSid">Flag to indicate whether text stream is
        /// with sentence id for each sentence.</param>
        /// <returns>Data error found during reading, otherwise null returned.</returns>
        public static DataError ReadOneScriptItem(ScriptItem scriptItem,
            string sentenceLine, string pronunciationLine,
            bool withPron, bool withSid)
        {
            if (scriptItem == null)
            {
                throw new ArgumentNullException("scriptItem");
            }

            if (string.IsNullOrEmpty(sentenceLine))
            {
                throw new ArgumentNullException("sentenceLine");
            }

            if (withPron)
            {
                if (string.IsNullOrEmpty(pronunciationLine))
                {
                    throw new ArgumentNullException("pronunciationLine");
                }
            }

            return ReadOneScriptItem(scriptItem, sentenceLine, pronunciationLine,
                withPron, withSid, true);
        }

        /// <summary>
        /// Read one script item from the sentence content line and pronunciation line.
        /// </summary>
        /// <param name="scriptItem">Result script item.</param>
        /// <param name="sentenceLine">Sentence content line.</param>
        /// <param name="pronunciationLine">Pronunciation line.</param>
        /// <param name="withPron">Flag to indicate whether text stream is
        /// with pronunciation for each sentence.</param>
        /// <param name="withSid">Flag to indicate whether text stream is
        /// with sentence id for each sentence.</param>
        /// <param name="validate">Whether validate the script item.</param>
        /// <returns>Data error found during reading, otherwise null returned.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Ignore.")]
        public static DataError ReadOneScriptItem(ScriptItem scriptItem,
            string sentenceLine, string pronunciationLine,
            bool withPron, bool withSid, bool validate)
        {
            if (scriptItem == null)
            {
                throw new ArgumentNullException("scriptItem");
            }

            if (string.IsNullOrEmpty(sentenceLine))
            {
                throw new ArgumentNullException("sentenceLine");
            }

            sentenceLine = sentenceLine.Trim();
            if (withPron)
            {
                if (string.IsNullOrEmpty(pronunciationLine))
                {
                    throw new ArgumentNullException("pronunciationLine");
                }

                pronunciationLine = pronunciationLine.Trim();
            }

            if (withSid)
            {
                Match m = Regex.Match(sentenceLine, @"^([0-9a-zA-Z]+)[\t ]+(.+)$");
                if (!m.Success)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid format, no sentence id for sentence: '{0}', pronunciation: '{1}'.",
                        sentenceLine, withPron ? pronunciationLine : "null");
                    return new DataError(message);
                }

                scriptItem.Id = m.Groups[1].Value;
                scriptItem.Sentence = m.Groups[2].Value.Trim();
            }
            else
            {
                scriptItem.Sentence = sentenceLine;
            }

            if (withPron)
            {
                // Phone set is case insensitive, so convert pronunciation to lower letter.
                scriptItem.Pronunciation = pronunciationLine.ToLower(CultureInfo.InvariantCulture);
            }

            if (validate)
            {
                Phoneme phoneme = null;
                if (scriptItem.Language != Language.Neutral)
                {
                    phoneme = Localor.GetPhoneme(scriptItem.Language, scriptItem.Engine);
                }

                try
                {
                    // Check all phonemes, currently for DeDE and JaJP only 
                    if (phoneme != null &&
                        (scriptItem.Language == Language.DeDE || scriptItem.Language == Language.JaJP))
                    {
                        string[] phones = scriptItem.GetPhones();
                        foreach (string phone in phones)
                        {
                            phoneme.TtsPhone2Id(phone);
                        }
                    }

                    if (scriptItem.Language != Language.Neutral &&
                        (scriptItem.NormalWords == null || scriptItem.NormalWords.Count == 0))
                    {
                        string message = string.Format(CultureInfo.InvariantCulture,
                            "No normal word found in the sentence.");
                        return new DataError("null", message, scriptItem.Id);
                    }
                }
                catch (InvalidDataException ide)
                {
                    return new DataError("null", Helper.BuildExceptionMessage(ide), scriptItem.Id);
                }
            }

            return null;
        }

        /// <summary>
        /// Remove error sentence out of script file.
        /// </summary>
        /// <param name="errorSet">Data error set.</param>
        /// <param name="scriptFilePath">Script file path.</param>
        public static void RemoveErrorSentence(DataErrorSet errorSet, string scriptFilePath)
        {
            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            if (errorSet.Errors == null)
            {
                throw new ArgumentException("errorSet.Errors is null");
            }

            ScriptFile script = new ScriptFile();
            script.Load(scriptFilePath);

            foreach (DataError error in errorSet.Errors)
            {
                if (string.IsNullOrEmpty(error.SentenceId))
                {
                    continue;
                }

                if (script.Items.ContainsKey(error.SentenceId))
                {
                    script.Items.Remove(error.SentenceId);
                }
            }

            script.Save(scriptFilePath);
        }

        /// <summary>
        /// Concatenate sub script files in given directory into an unified sript file.
        /// </summary>
        /// <param name="language">Language of the script to process.</param>
        /// <param name="scriptDir">The source directory of sub script files.</param>
        /// <param name="targetScriptFilePath">The output script file path.</param>
        /// <returns>Data error set containing error found.</returns>
        public static DataErrorSet Concatenate(Language language,
            string scriptDir, string targetScriptFilePath)
        {
            if (string.IsNullOrEmpty(scriptDir))
            {
                throw new ArgumentNullException("scriptDir");
            }

            if (string.IsNullOrEmpty(targetScriptFilePath))
            {
                throw new ArgumentNullException("targetScriptFilePath");
            }

            if (!Directory.Exists(scriptDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException),
                    scriptDir);
            }

            return ConcatenateFiles(language, Directory.GetFiles(scriptDir, "*.txt"), false, targetScriptFilePath);
        }

        /// <summary>
        /// Concatenate script files in given file list into an unified sript file and reorder the sentence ID.
        /// </summary>
        /// <param name="language">Language of the script to process.</param>
        /// <param name="scriptListFile">A file list of the path of each source scripts, one path in one line.</param>
        /// <param name="targetScriptFilePath">The output script file path.</param>
        /// <returns>Data error set containing error found.</returns>
        public static DataErrorSet ConcatenateInOrder(Language language,
            string scriptListFile, string targetScriptFilePath)
        {
            if (string.IsNullOrEmpty(scriptListFile))
            {
                throw new ArgumentNullException("scriptListFile");
            }

            if (string.IsNullOrEmpty(targetScriptFilePath))
            {
                throw new ArgumentNullException("targetScriptFilePath");
            }

            scriptListFile = Path.GetFullPath(scriptListFile);
            if (!File.Exists(scriptListFile))
            {
                throw Helper.CreateException(typeof(FileNotFoundException),
                    scriptListFile);
            }

            string[] scriptFiles = File.ReadAllLines(scriptListFile);

            return ConcatenateFiles(language, scriptFiles, true, targetScriptFilePath);
        }

        /// <summary>
        /// Concatenate script files in given file list into an unified sript file and reorder the sentence ID.
        /// </summary>
        /// <param name="language">Language of the script to process.</param>
        /// <param name="fileList">A set of file names to be concatenated.</param>
        /// <param name="resetSid">A bool to indicate whether re-oreder the sentence ID of all scripts.</param>
        /// <param name="targetScriptFilePath">The output script file path.</param>
        /// <returns>Data error set containing error found.</returns>
        public static DataErrorSet ConcatenateFiles(Language language, IEnumerable<string> fileList, bool resetSid,
            string targetScriptFilePath)
        {
            if (string.IsNullOrEmpty(targetScriptFilePath))
            {
                throw new ArgumentNullException("targetScriptFilePath");
            }

            if (fileList == null)
            {
                throw new ArgumentNullException("fileList");
            }

            ScriptFile script = Localor.CreateScriptFile(language);
            DataErrorSet errorSet = new DataErrorSet();

            int count = 0;
            foreach (string file in fileList)
            {
                if (string.IsNullOrEmpty(file))
                {
                    continue;
                }

                ScriptFile subScript = Localor.CreateScriptFile(language);
                subScript.Load(file);
                errorSet.Merge(subScript.ErrorSet);

                foreach (string sid in subScript.Items.Keys)
                {
                    ScriptItem item = subScript.Items[sid];

                    item.Id = resetSid ? Helper.NeutralFormat("{0:D10}", ++count) : sid;

                    if (script.Items.ContainsKey(item.Id))
                    {
                        errorSet.Errors.Add(new DataError(file, "Sentence already exist", sid));
                        continue;
                    }

                    script.Items.Add(item.Id, item);
                }
            }

            Helper.EnsureFolderExistForFile(targetScriptFilePath);
            script.Save(targetScriptFilePath);

            return errorSet;
        }

        /// <summary>
        /// Validate the phone sequence in the script file. It will check:
        ///  1) word alignment with the pronunciation string. This means that
        ///     for each word it should have one and only one corresponding
        ///     pronunciation
        ///  2) the pronunciation should be syllabified, and for each syllbale
        ///     there is one and only one vowel. It can have one stress mark
        ///  3) each phones in the pronunciation string, should be valid in 
        ///     that langugage phoneme set.
        /// </summary>
        /// <param name="script">The script oebjct to be validated.</param>
        /// <returns>Errors/problems found in the script.</returns>
        public static DataErrorSet ValidatePronunciation(ScriptFile script)
        {
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

            DataErrorSet errorSet = new DataErrorSet();
            foreach (string sid in script.Items.Keys)
            {
                try
                {
                    ScriptItem item = script.Items[sid];
                    DataError subError = script.ProcessPronunciation(item);
                    if (subError != null)
                    {
                        errorSet.Errors.Add(subError);
                    }
                }
                catch (InvalidDataException ide)
                {
                    errorSet.Errors.Add(new DataError(script.FilePath,
                        Helper.BuildExceptionMessage(ide), sid));
                }
                catch (KeyNotFoundException knfe)
                {
                    errorSet.Errors.Add(new DataError(script.FilePath,
                        Helper.BuildExceptionMessage(knfe), sid));
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Validate the word pronunciations in sentence are valid or not.
        /// </summary>
        /// <param name="entry">Script item.</param>
        /// <returns>Data error found.</returns>
        public static DataError ValidatePronunciation(ScriptItem entry)
        {
            return ValidatePronunciation(entry, false);
        }

        /// <summary>
        /// Validate the word pronunciations in sentence are valid or not.
        /// </summary>
        /// <param name="entry">Script item.</param>
        /// <param name="isBadPhoneValid">True means that bad phone is valid.</param>
        /// <returns>Data error found.</returns>
        public static DataError ValidatePronunciation(ScriptItem entry, bool isBadPhoneValid)
        {
            DataError dataError = null;

            string[] words = entry.Pronunciation.Split(
                new string[] { entry.PronunciationSeparator.Word },
                StringSplitOptions.None);
            for (int i = 0; i < words.Length; i++)
            {
                if ((i == 0 || i == words.Length - 1) &&
                    string.IsNullOrEmpty(words[i]))
                {
                    // It makes sense for first or last one is with empty string
                    continue;
                }

                if (string.IsNullOrEmpty(words[i]))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "The word[{0}] pronunciation is empty by separator [{1}]",
                        i, entry.PronunciationSeparator.Word);
                    dataError = new DataError("null", message, entry.Id);
                    break;
                }

                // Check syllable's pronunciation
                string newWord = words[i];
                if (isBadPhoneValid)
                {
                    // remove the bad phone label from bad phone.
                    string pattern = @"([a-zA-Z]+)" + BadPhoneLabel;
                    string replacement = @"$1";
                    newWord = Regex.Replace(newWord, pattern, replacement, RegexOptions.CultureInvariant);
                }

                dataError = ValidateSyllables(entry, newWord);
                if (dataError != null)
                {
                    break;
                }
            }

            return dataError;
        }

        /// <summary>
        /// Validate the syllable in word pronunciation are valid or not.
        /// </summary>
        /// <param name="entry">Script item.</param>
        /// <param name="word">Pronunciation of word.</param>
        /// <returns>Data error found.</returns>
        public static DataError ValidateSyllables(ScriptItem entry, string word)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            if (entry.PronunciationSeparator == null)
            {
                string message = Helper.NeutralFormat("entry.PronunciationSeparator should not be null.");
                throw new ArgumentException(message);
            }

            if (string.IsNullOrEmpty(entry.PronunciationSeparator.Syllable))
            {
                string message = Helper.NeutralFormat("entry.PronunciationSeparator.Syllable should not be null.");
                throw new ArgumentException(message);
            }

            if (string.IsNullOrEmpty(word))
            {
                throw new ArgumentNullException("word");
            }

            Phoneme phoneme = Localor.GetPhoneme(entry.Language);

            DataError dataError = null;

            string[] syllables = word.Split(new string[] { entry.PronunciationSeparator.Syllable },
                StringSplitOptions.None);
            for (int j = 0; j < syllables.Length; j++)
            {
                string syllable = syllables[j].Trim();
                if (string.IsNullOrEmpty(syllable))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "The syllable[{0}] of word[{1}] pronunciation is empty by separator [{2}]",
                        j, word, entry.PronunciationSeparator.Syllable);
                    dataError = new DataError("null", message, entry.Id);
                    break;
                }

                if (Regex.Match(syllable, "^_(.*)_$").Success)
                {
                    // Special unit
                    continue;
                }

                string[] itmes = entry.PronunciationSeparator.SplitPhones(syllable);
                TtsMetaUnit ttsMetaUnit = new TtsMetaUnit(entry.Language);
                ttsMetaUnit.Name = string.Join(" ", itmes);
                string[] phones = ttsMetaUnit.GetPhonesName();

                // Tell whether is a valid nucleus, 
                // which could be syllable with no vowel in some languages, like fr-CA
                SliceData sliceData = Localor.GetSliceData(entry.Language);
                if (sliceData.NucleusSlices.IndexOf(ttsMetaUnit.Name) < 0)
                {
                    bool goodSyllable;

                    if (entry.Language == Language.EnUS)
                    {
                        // syllable that must have vowels
                        goodSyllable = IsGoodSyllableWithVowel(entry, phoneme, phones);
                    }
                    else if (entry.Language == Language.RuRU)
                    {
                        // A Russian syllable can have no sonorant
                        goodSyllable = IsSyllableWithLessVowel(entry, phoneme, phones);
                    }
                    else
                    {
                        // syllable that must have vowels or sonorants
                        goodSyllable = IsGoodSyllableWithSonorant(entry, phoneme, phones);
                    }

                    if (!goodSyllable)
                    {
                        int[] vowelIndexes = phoneme.GetVowelIndexes(phones);
                        string str1 = "There must be minimum {0} vowels or maximum {1} included in syllable ";
                        string str2 = "or the syllable should have one sonorant and more than one consonants, ";
                        string str3 = "but {2} vowels are found in syllable [{3}] of word [{4}].";
                        string message = string.Format(CultureInfo.InvariantCulture, str1 + str2 + str3, 
                            entry.MinVowelCountInSyllable, entry.MaxVowelCountInSyllable,
                            vowelIndexes.Length, syllables[j], word);
                        dataError = new DataError("null", message, entry.Id);
                        break;
                    }
                }

                // check slice's pronunciation
                dataError = ValidateSlices(entry, syllable);
                if (dataError != null)
                {
                    break;
                }
            }

            return dataError;
        }

        /// <summary>
        /// Validate the slices in syllable are valid or not.
        /// </summary>
        /// <param name="entry">Script item.</param>
        /// <param name="syllable">Pronunciation of syllable.</param>
        /// <returns>Data error found.</returns>
        public static DataError ValidateSlices(ScriptItem entry, string syllable)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            if (entry.PronunciationSeparator == null)
            {
                string message = Helper.NeutralFormat("entry.PronunciationSeparator should not be null.");
                throw new ArgumentException(message);
            }

            if (string.IsNullOrEmpty(entry.PronunciationSeparator.Slice))
            {
                string message = Helper.NeutralFormat("entry.PronunciationSeparator.Slice should not be null.");
                throw new ArgumentException(message);
            }

            if (string.IsNullOrEmpty(syllable))
            {
                throw new ArgumentNullException("syllable");
            }

            DataError dataError = null;

            string[] slices = syllable.Split(new string[] { entry.PronunciationSeparator.Slice },
                StringSplitOptions.None);
            for (int k = 0; k < slices.Length; k++)
            {
                if (string.IsNullOrEmpty(slices[k]))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "The slice[{0}] of syllable[{1}] is empty by separator [{2}]",
                        k, syllable, entry.PronunciationSeparator.Slice);
                    dataError = new DataError("null", message, entry.Id);
                    break;
                }

                // check phones
                dataError = ValidatePhones(entry, slices[k]);
                if (dataError != null)
                {
                    break;
                }
            }

            return dataError;
        }

        /// <summary>
        /// Validate the phones in slice are valid or not.
        /// </summary>
        /// <param name="entry">Script item.</param>
        /// <param name="slice">Pronunciation of slice.</param>
        /// <returns>Data error found.</returns>
        public static DataError ValidatePhones(ScriptItem entry, string slice)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            if (entry.PronunciationSeparator == null)
            {
                string message = Helper.NeutralFormat("entry.PronunciationSeparator should not be null.");
                throw new ArgumentException(message);
            }

            if (string.IsNullOrEmpty(entry.PronunciationSeparator.Phone))
            {
                string message = Helper.NeutralFormat("entry.PronunciationSeparator.Phone should not be null.");
                throw new ArgumentException(message);
            }

            if (string.IsNullOrEmpty(slice))
            {
                throw new ArgumentNullException("slice");
            }

            DataError error = null;

            Phoneme phoneme = Localor.GetPhoneme(entry.Language);
            string[] items = slice.Split(new string[] { entry.PronunciationSeparator.Phone },
                StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < items.Length; i++)
            {
                // TODO: PS#13181 Offline tools:Syllable veridation and pronunciation design
                if (items[i] == "1" || items[i] == "2" || items[i] == "3")
                {
                    continue;
                }

                if (items[i].StartsWith("_", StringComparison.Ordinal) && items[i].EndsWith("_", StringComparison.Ordinal))
                {
                    // special phone
                    continue;
                }

                if (phoneme.ToneManager.NameMap.ContainsKey(items[i]))
                {
                    // tone
                    continue;
                }

                if (phoneme.TtsPhones.IndexOf(items[i]) < 0)
                {
                    // invalid tts phone found
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "The phone[{0}] in slice[{1}] is invalid",
                        items[i], slice);
                    error = new DataError("null", message, entry.Id);
                    break;
                }
            }

            return error;
        }

        /// <summary>
        /// Merge a folder of scripts into one
        /// In wave normalization/force align/font building, we should discard error sentences.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <param name="scriptFolder">Script folder.</param>
        /// <param name="outputScript">Output script.</param>
        /// <returns>The data errors.</returns>
        public static DataErrorSet MergeScript(Language language, string scriptFolder, string outputScript)
        {
            ScriptFile script = Localor.CreateScriptFile(language);
            DataErrorSet errorSet = script.BuildScript(scriptFolder, outputScript);
            if (errorSet.Errors.Count > 0)
            {
                Console.Error.WriteLine("Data error is found while building " +
                    "merged script file from the script dir [{0}].",
                    scriptFolder);
                errorSet.Dump2Console();
            }

            return errorSet;
        }

        #endregion

        #region Serializations

        /// <summary>
        /// Initialize script file from a file.
        /// </summary>
        /// <param name="filePath">File to load script data.</param>
        /// <returns>Data error set found.</returns>
        public DataErrorSet Load(string filePath)
        {
            _items = new SortedDictionary<string, ScriptItem>();
            _errorSet = ReadAllData(filePath, Language, EngineType, _items);
            _filePath = filePath;

            return _errorSet;
        }

        /// <summary>
        /// Save script items into file.
        /// </summary>
        /// <param name="filePath">Target file to save.</param>
        public void Save(string filePath)
        {
            Save(filePath, true, true);
        }

        /// <summary>
        /// Save script items into file.
        /// </summary>
        /// <param name="filePath">Target file to save.</param>
        /// <param name="hasSid">Whether contains SID.</param>
        /// <param name="hasPron">Whether contains pronunciation.</param>
        public void Save(string filePath, bool hasSid, bool hasPron)
        {
            Helper.EnsureFolderExistForFile(filePath);
            using (StreamWriter sw = new StreamWriter(filePath,
                false, Encoding.Unicode))
            {
                foreach (string sid in Items.Keys)
                {
                    string sentence = Items[sid].ToString(hasSid, hasPron, true);
                    sw.WriteLine(sentence);
                }
            }
        }

        #endregion

        #region Build script file from pieces

        /// <summary>
        /// Build whole pronunciation script from script directory.
        /// </summary>
        /// <param name="phoneScriptFileDir">Script directory.</param>
        /// <param name="outFilePath">Pronunciation script file.</param>
        /// <returns>Data error set found.</returns>
        public DataErrorSet BuildScript(string phoneScriptFileDir,
            string outFilePath)
        {
            if (string.IsNullOrEmpty(outFilePath))
            {
                throw new ArgumentNullException("outFilePath");
            }

            if (!Directory.Exists(phoneScriptFileDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException),
                    phoneScriptFileDir);
            }

            Helper.EnsureFolderExistForFile(outFilePath);
            string[] subFilePaths = System.IO.Directory.GetFiles(phoneScriptFileDir,
                                                            "*" + ScriptFile.Extension);
            DataErrorSet errorSet = new DataErrorSet();
            if (subFilePaths != null)
            {
                errorSet = BuildScript(subFilePaths, outFilePath);
            }

            return errorSet;
        }

        /// <summary>
        /// Build pronunciation script from file list.
        /// </summary>
        /// <param name="scriptFilePaths">Hiragana script file list.</param>
        /// <param name="outFilePath">Romaji pronunciation script file.</param>
        /// <returns>Data error set found.</returns>
        public DataErrorSet BuildScript(string[] scriptFilePaths,
            string outFilePath)
        {
            if (scriptFilePaths == null)
            {
                throw new ArgumentNullException("scriptFilePaths");
            }

            DataErrorSet errorSet = new DataErrorSet();

            for (int i = 0; i < scriptFilePaths.Length; i++)
            {
                string scriptFilePath = scriptFilePaths[i];

                if (string.IsNullOrEmpty(scriptFilePath))
                {
                    throw new InvalidDataException("scriptFilePath");
                }

                if (!scriptFilePath.EndsWith(ScriptFile.Extension, StringComparison.Ordinal))
                {
                    continue;
                }

                // all script files should be saved in unicode
                if (!Helper.IsUnicodeFile(scriptFilePath))
                {
                    DataError error = new DataError(scriptFilePath,
                        "script file should be saved in Unicode.");
                    errorSet.Errors.Add(error);
                    continue;
                }

                // do appending
                DataErrorSet subErrorSet = AppendScript(scriptFilePath,
                    outFilePath, (i != 0));

                // merge error messages
                errorSet.Merge(subErrorSet);
            }

            return errorSet;
        }

        #endregion

        #region Public build mono-phone MLF file

        /// <summary>
        /// Based on script file, build a mono-phone MLF (See HTK document) file .
        /// </summary>
        /// <param name="scriptFilePath">Script file path.</param>
        /// <param name="outFilePath">Output Mlf file path.</param>
        /// <returns>Data error set found.</returns>
        public DataErrorSet BuildMonoMlf(string scriptFilePath,
            string outFilePath)
        {
            DataErrorSet errorSet = null;
            StreamWriter sw = null;

            try
            {
                if (!string.IsNullOrEmpty(outFilePath))
                {
                    Helper.EnsureFolderExistForFile(outFilePath);
                    sw = new StreamWriter(outFilePath, false, Encoding.ASCII);
                }

                errorSet = BuildMonoMlf(sw, scriptFilePath);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }

            // verify the file format while to witer out 
            Debug.Assert(string.IsNullOrEmpty(outFilePath)
                || HtkTool.VerifyMlfFormat(outFilePath));

            return errorSet;
        }

        /// <summary>
        /// Process pronunciaction for script entry before script building.
        /// </summary>
        /// <param name="entry">Script item.</param>
        /// <returns>Data error found.</returns>
        protected virtual DataError ProcessPronunciation(ScriptItem entry)
        {
            // This function to be hooked by the implementation of sub class
            return ValidatePronunciation(entry);
        }

        /// <summary>
        /// Build one sentence for mono MLF file .
        /// </summary>
        /// <param name="writer">Text writer to save MLF file.</param>
        /// <param name="entry">Script item.</param>
        protected virtual void BuildMonoMlf(TextWriter writer, ScriptItem entry)
        {
            // Go through each sentences
            if (entry.NormalWords.Count == 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "No normal word found in the sentence.");
                throw new InvalidDataException(message);
            }

            if (writer != null)
            {
                // write sentence header of MLF file
                writer.WriteLine("\"*/" + entry.Id + ".lab\"");
                writer.WriteLine(Phoneme.SilencePhone);
            }

            try
            {
                for (int i = 0; i < entry.NormalWords.Count; i++)
                {
                    // for each words
                    ScriptWord word = entry.NormalWords[i];

                    if (Phoneme.Tts2srMapType == Phoneme.TtsToSrMappingType.PhoneBased)
                    {
                        for (int j = 0; j < word.Units.Count; j++)
                        {
                            TtsUnit unit = word.Units[j];
                            BuildMonoMlf(writer, unit);
                        }
                    }
                    else if (Phoneme.Tts2srMapType == Phoneme.TtsToSrMappingType.SyllableBased)
                    {
                        Collection<TtsUnit> units = word.Units;
                        for (int j = 0; j < word.Syllables.Count; j++)
                        {
                            ScriptSyllable syllable = word.Syllables[j];
                            BuildMonoMlf(writer, syllable);
                        }
                    }

                    if (i + 1 < entry.NormalWords.Count)
                    {
                        // not last normal word in the sentence
                        if (writer != null)
                        {
                            writer.WriteLine(Phoneme.ShortPausePhone);
                        }
                    }
                }

                if (writer != null)
                {
                    writer.WriteLine(Phoneme.SilencePhone);
                    writer.WriteLine(".");  // end of sentence
                }
            }
            catch (InvalidDataException)
            {
                if (writer != null)
                {
                    writer.WriteLine(Phoneme.SilencePhone);
                    writer.WriteLine(".");  // end of sentence
                }

                throw;
            }
        }

        #endregion

        #region Private static methods

        /// <summary>
        /// Check if the syllable has valid vowel number.
        /// </summary>
        /// <param name="entry">Script entry.</param>
        /// <param name="phoneme">Phoneme.</param>
        /// <param name="phones">Phones of the syllable.</param>
        /// <returns>Bool.</returns>
        private static bool IsGoodSyllableWithVowel(ScriptItem entry,
                        Phoneme phoneme,
                        string[] phones)
        {
            bool goodSyllable = IsSyllableWithEnoughVowel(entry, phoneme, phones) &&
                                IsSyllableWithLessVowel(entry, phoneme, phones);

            return goodSyllable;
        }

        /// <summary>
        /// Check if the syllable has enough vowels.
        /// </summary>
        /// <param name="entry">Script entry.</param>
        /// <param name="phoneme">Phoneme.</param>
        /// <param name="phones">Phones of the syllable.</param>
        /// <returns>Bool.</returns>
        private static bool IsSyllableWithEnoughVowel(ScriptItem entry,
                        Phoneme phoneme,
                        string[] phones)
        {
            int[] vowelIndexes = phoneme.GetVowelIndexes(phones);
            return vowelIndexes.Length >= entry.MinVowelCountInSyllable;
        }

        /// <summary>
        /// Check if the syllable has too many vowels.
        /// </summary>
        /// <param name="entry">Script entry.</param>
        /// <param name="phoneme">Phoneme.</param>
        /// <param name="phones">Phones of the syllable.</param>
        /// <returns>True if not having too many.</returns>
        private static bool IsSyllableWithLessVowel(ScriptItem entry,
                        Phoneme phoneme,
                        string[] phones)
        {
            int[] vowelIndexes = phoneme.GetVowelIndexes(phones);
            return vowelIndexes.Length <= entry.MaxVowelCountInSyllable;
        }

        /// <summary>
        /// Check if the syllable has vowel or has a sonorant phoneme.
        /// </summary>
        /// <param name="entry">Script entry.</param>
        /// <param name="phoneme">Phoneme.</param>
        /// <param name="phones">Phones of the syllable.</param>
        /// <returns>Bool.</returns>
        private static bool IsGoodSyllableWithSonorant(ScriptItem entry,
                        Phoneme phoneme,
                        string[] phones)
        {
            bool goodSyllable = IsSyllableWithLessVowel(entry, phoneme, phones);

            if (goodSyllable)
            {
                if (!IsSyllableWithEnoughVowel(entry, phoneme, phones))
                {
                    if (phoneme.GetVowelIndexes(phones).Length == 0)
                    {
                        // no vowel, should have one sonorant and more than one consonants
                        int[] sonorantIndexes = phoneme.GetSonorantIndexes(phones);
                        if (sonorantIndexes.Length == 0 || phones.Length == 1)
                        {
                            goodSyllable = false;
                        }
                    }
                    else
                    {
                        goodSyllable = false;
                    }
                }
            }

            return goodSyllable;
        }

        #endregion

        #region Private build mono-phone MLF file

        /// <summary>
        /// Based on script file, build a mono-phone MLF (See HTK document) file .
        /// </summary>
        /// <param name="writer">Writer to write result out if not null.</param>
        /// <param name="scriptFilePath">Script file path.</param>
        /// <returns>Data error set found.</returns>
        private DataErrorSet BuildMonoMlf(TextWriter writer, string scriptFilePath)
        {
            Collection<ScriptItem> entries = new Collection<ScriptItem>();

            DataErrorSet errorSet = ReadAllData(scriptFilePath,
                Language, EngineType, entries);
            if (entries.Count == 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "There is no script sentence found in file [{0}].", scriptFilePath);
                throw new InvalidDataException(message);
            }

            if (writer != null)
            {
                // write MLF file header
                writer.WriteLine("#!MLF!#");
            }

            foreach (ScriptItem entry in entries)
            {
                try
                {
                    BuildMonoMlf(writer, entry);
                }
                catch (InvalidDataException ide)
                {
                    DataError error = new DataError(scriptFilePath,
                        Helper.BuildExceptionMessage(ide), entry.Id);
                    errorSet.Errors.Add(error);
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Build one unit for mono MLF file.
        /// </summary>
        /// <param name="writer">Text writer to save MLF file.</param>
        /// <param name="unit">Unit.</param>
        private void BuildMonoMlf(TextWriter writer, TtsUnit unit)
        {
            string[] srPhones = ConvertToSrPhone(unit);

            foreach (string srPhone in srPhones)
            {
                if (writer != null)
                {
                    writer.WriteLine(srPhone);
                }
            }
        }

        /// <summary>
        /// Build one syllable for mono MLF file.
        /// </summary>
        /// <param name="writer">Text writer to save MLF file.</param>
        /// <param name="syllable">Syllable.</param>
        private void BuildMonoMlf(TextWriter writer, ScriptSyllable syllable)
        {
            string[] srPhones = ConvertToSrPhone(syllable);

            foreach (string srPhone in srPhones)
            {
                if (writer != null)
                {
                    writer.WriteLine(srPhone);
                }
            }
        }

        #endregion

        #region Private instance methods

        /// <summary>
        /// Append a script into other script file.
        /// </summary>
        /// <param name="subScriptFilePath">Source script file.</param>
        /// <param name="outFilePath">Target script file.</param>
        /// <param name="append">Whether appending to target script file.</param>
        /// <returns>Invalid format script entry strings.</returns>
        private DataErrorSet AppendScript(string subScriptFilePath,
            string outFilePath, bool append)
        {
            DataErrorSet errorSet = new DataErrorSet();

            SortedDictionary<string, ScriptItem> existEntries = new SortedDictionary<string, ScriptItem>();
            if (append && File.Exists(outFilePath))
            {
                errorSet = ReadAllData(outFilePath, Language, EngineType, existEntries);
            }
            else
            {
                Helper.EnsureFolderExistForFile(outFilePath);
            }

            SortedDictionary<string, ScriptItem> subEntries = new SortedDictionary<string, ScriptItem>();
            DataErrorSet subErrorSet = ReadAllData(subScriptFilePath,
                 Language, EngineType, subEntries);
            errorSet.Merge(subErrorSet);

            using (StreamWriter sw = new StreamWriter(outFilePath, append, Encoding.Unicode))
            {
                foreach (string sid in subEntries.Keys)
                {
                    if (existEntries.ContainsKey(sid))
                    {
                        DataError error = new DataError(subScriptFilePath,
                            "Entry already exists in script file [" + outFilePath + "]", sid);
                        errorSet.Errors.Add(error);
                        continue;
                    }

                    // hook handling
                    DataError preAppendError = ProcessPronunciation(subEntries[sid]);
                    if (preAppendError != null)
                    {
                        errorSet.Errors.Add(preAppendError);
                        continue;
                    }

                    sw.WriteLine(subEntries[sid].ToString(true, true, true));
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Convert phones of TTS unit to SR phones.
        /// </summary>
        /// <param name="unit">TtsUnit to be processed.</param>
        /// <returns>SR phone array.</returns>
        private string[] ConvertToSrPhone(TtsUnit unit)
        {
            List<string> retPhones = new List<string>();

            // Go through each phone in this unit
            foreach (TtsMetaPhone phone in unit.MetaUnit.Phones)
            {
                // Map phone to Speech Recognition phone(s)
                string[] srPhones = Phoneme.Tts2SrPhones(phone.Name);
                if (srPhones == null)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid TTS phone[{0}], which can not be converted to Speech Recognition Phone.",
                        phone.Name);
                    throw new InvalidDataException(message);
                }

                retPhones.AddRange(srPhones);
            }

            return retPhones.ToArray();
        }

        /// <summary>
        /// Convert phones of TTS unit to SR phones.
        /// </summary>
        /// <param name="syllable">Syllable to be processed.</param>
        /// <returns>SR phone array.</returns>
        private string[] ConvertToSrPhone(ScriptSyllable syllable)
        {
            string syllableText = Pronunciation.CleanDecorate(syllable.Text.Trim());

            // Map phone to Speech Recognition phone(s)
            string[] srPhones = Phoneme.Tts2SrPhones(syllableText.Trim());
            if (srPhones == null)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid TTS syllable[{0}], which can not be converted to Speech Recognition Phone.",
                     syllableText);
                throw new InvalidDataException(message);
            }

            return srPhones;
        }

        #endregion
    }
}