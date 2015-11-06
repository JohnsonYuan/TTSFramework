//----------------------------------------------------------------------------
// <copyright file="ScriptReviewDocument.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Manage the actions on script file.
// </summary>
//----------------------------------------------------------------------------

namespace ScriptReviewer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Script;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Script file collection operation error.
    /// </summary>
    public enum XmlScriptReviewerDocumentError
    {
        /// <summary>
        /// Empty normal word pronunciation:
        /// {0} : Word text.
        /// {1} : Item ID.
        /// </summary>
        [ErrorAttribute(Message = "Empty normal word's [{0}] pronunciation in item [{1}], " +
            "item removed when enable mapping pronunciation.")]
        EmptyNormalWordPronunciation,

        /// <summary>
        /// Empty normal word's original pronunciation:
        /// {0} : Word text.
        /// {1} : Item ID.
        /// </summary>
        [ErrorAttribute(Message = "Empty normal word's [{0}] original pronunciation in item [{1}], " +
            "item removed when enable mapping pronunciation.")]
        EmptyNormalWordOriginalPronunciation,

        /// <summary>
        /// Invalid normal word's pronunciation:
        /// {0} : Word text.
        /// {1} : Item ID.
        /// </summary>
        [ErrorAttribute(Message = "Invalid normal word's [{0}] pronunciation in item [{1}], " +
            "item removed when enable mapping pronunciation.")]
        InvalidNormalWordPronunciation,

        /// <summary>
        /// Invalid normal word's original pronunciation:
        /// {0} : Word text.
        /// {1} : Item ID.
        /// </summary>
        [ErrorAttribute(Message = "Invalid normal word's [{0}] original pronunciation [{1}] in item [{2}], " +
            "item removed when enable mapping pronunciation.")]
        InvalidNormalWordOriginalPronunciation,

        /// <summary>
        /// Sentence not in script error.
        /// {0} : item ID.
        /// </summary>
        [ErrorAttribute(Message = "Item [{0}] in file list but not in script.")]
        SentenceNotInScriptError
    }

    /// <summary>
    /// Event arguments.
    /// </summary>
    /// <typeparam name="T">Type of the event argument.</typeparam>
    public class EventArgs<T> : EventArgs
    {
        private T _item;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventArgs{T}"/> class.
        /// </summary>
        /// <param name="item">Data item of type T.</param>
        public EventArgs(T item)
        {
            _item = item;
        }

        /// <summary>
        /// Gets Item value.
        /// </summary>
        public T Item
        {
            get { return _item; }
        }
    }

    /// <summary>
    /// ReviewDocument class manages actions on script file.
    /// </summary>
    public class XMLScriptReviewDocument
    {
        #region Fields

        private static XmlSchema _schema;

        private Language _language = Language.Neutral;
        private string _fileListFilePath = string.Empty;
        private string _scriptPath = string.Empty;

        private string _waveDir = string.Empty;

        private string _segmentDir = string.Empty;
        private string _modelToUiPhoneMapFilePath = string.Empty;

        private Dictionary<string, List<ScriptWordComment>> _sidCommentMap =
            new Dictionary<string, List<ScriptWordComment>>();

        private Dictionary<ScriptWord, ScriptWordComment> _wordCommentMap =
            new Dictionary<ScriptWord, ScriptWordComment>();

        private Dictionary<ScriptItem, ScriptWordComment> _itemCommentMap =
            new Dictionary<ScriptItem, ScriptWordComment>();

        private Dictionary<ScriptWord, TtsXmlStatus> _deletedWordDict =
            new Dictionary<ScriptWord, TtsXmlStatus>();

        private ScriptFileCollection _scriptFileCollection = new ScriptFileCollection(true);

        private PhoneMap _modelToUiPhoneMap;
        private PhoneMap _uiToModelPhoneMap;

        /// <summary>
        /// Flag whether need further saving.
        /// </summary>
        private bool _needSave;
        private bool _updateSentence = true;

        /// <summary>
        /// Whether need map segment file's pronunciation.
        /// </summary>
        private bool _mappedSegment = true;

        private List<string> _filteredItemIds = new List<string>();
        private List<string> _domainNames = new List<string>();

        private FileListMap _waveFileList;
        private FileListMap _segFileList;

        private Collection<VoiceCreationLanguageData> _languageDataCollection = new Collection<VoiceCreationLanguageData>();
        private Language[] _languages;
        
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="XMLScriptReviewDocument"/> class.
        /// </summary>
        public XMLScriptReviewDocument()
        {
            CommentAdded = delegate
            {
            };

            CommentUpdated = delegate
            {
            };

            CommentRemoved = delegate
            {
            };
        }

        /// <summary>
        /// Event while a new comment is added.
        /// </summary>
        public event EventHandler<EventArgs<ScriptWordComment>> CommentAdded;

        /// <summary>
        /// Event while a new comment is updated.
        /// </summary>
        public event EventHandler<EventArgs<ScriptWordComment>> CommentUpdated;

        /// <summary>
        /// Event while a new comment is removed.
        /// </summary>
        public event EventHandler<EventArgs<Collection<ScriptWordComment>>> CommentRemoved;

        #region Properties

        /// <summary>
        /// Gets UnitListDictionary schema.
        /// </summary>
        public static XmlSchema ConfigSchema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.ScriptReviewConfig.xsd");
                }

                _schema.Includes.Clear();
                XmlSchemaInclude ttsCommonIncluded = new XmlSchemaInclude();
                ttsCommonIncluded.Schema =
                    XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.ttscommon.xsd");
                _schema.Includes.Add(ttsCommonIncluded);

                return _schema;
            }
        }

        /// <summary>
        /// Gets LanguageData collection.
        /// </summary>
        public Collection<VoiceCreationLanguageData> LanguageDataCollection
        {
            get 
            {
                return _languageDataCollection;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether update sentence.
        /// </summary>
        public bool UpdateSentence
        {
            get { return _updateSentence; }
            set { _updateSentence = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether map segment using the phonemap.
        /// </summary>
        public bool MappedSegment
        {
            get { return _mappedSegment; }
            set { _mappedSegment = value; }
        }

        /// <summary>
        /// Gets Script path.
        /// </summary>
        public PhoneMap ModelToUiPhoneMap
        {
            get { return _modelToUiPhoneMap; }
        }

        /// <summary>
        /// Gets Script path.
        /// </summary>
        public PhoneMap UiToModelPhoneMap
        {
            get { return _uiToModelPhoneMap; }
        }

        /// <summary>
        /// Gets or sets Script path.
        /// </summary>
        public string ScriptPath
        {
            get { return _scriptPath; }
            set { _scriptPath = value; }
        }

        /// <summary>
        /// Gets or sets Language of the config file.
        /// </summary>
        public Language Language
        {
            get
            {
                return _language;
            }

            set
            {
                _language = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether File list of this review document.
        /// </summary>
        public bool NeedSave
        {
            get { return _needSave; }
            set { _needSave = value; }
        }

        /// <summary>
        /// Gets Filtered item id list.
        /// </summary>
        public List<string> FilteredItemIds
        {
            get { return _filteredItemIds; }
        }

        /// <summary>
        /// Gets Domain names.
        /// </summary>
        public List<string> DomainNames
        {
            get { return _domainNames; }
        }

        /// <summary>
        /// Gets segment file list.
        /// </summary>
        public FileListMap SegmentFileList
        {
            get { return _segFileList; }
        }

        /// <summary>
        /// Gets Wave file list.
        /// </summary>
        public FileListMap WaveFileList
        {
            get { return _waveFileList; }
        }

        /// <summary>
        /// Gets or sets Wave file path.
        /// </summary>
        public string WaveDir
        {
            get
            {
                return _waveDir;
            }

            set
            {
                _waveDir = value;
            }
        }

        /// <summary>
        /// Gets or sets Segment file path.
        /// </summary>
        public string SegmentDir
        {
            get
            {
                return _segmentDir;
            }

            set
            {
                _segmentDir = value;
            }
        }

        /// <summary>
        /// Gets Create the List of ScriptItemComments 
        /// And get the list by the string of sentence id.
        /// </summary>
        public Dictionary<string, List<ScriptWordComment>> SidCommentMap
        {
            get { return _sidCommentMap; }
        }

        /// <summary>
        /// Gets Create the List of comments of one ScriptItem 
        /// And get it by the ScriptItem.
        /// </summary>
        public Dictionary<ScriptItem, ScriptWordComment> ItemCommentMap
        {
            get { return _itemCommentMap; }
        }

        /// <summary>
        /// Gets Create the List of comments of one ScriptWord 
        /// And get it by the ScriptWord.
        /// </summary>
        public Dictionary<ScriptWord, ScriptWordComment> WordCommentMap
        {
            get { return _wordCommentMap; }
        }

        /// <summary>
        /// Gets Tts.Offline class.
        /// </summary>
        public ScriptFileCollection ScriptFileCollection
        {
            get { return _scriptFileCollection; }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Validate specified wordPron with multi-language.
        /// </summary>
        /// <param name="pronunciation">A word pronunciation to be validate.</param>
        /// <param name="languages">Phone map array.</param>
        /// <returns>Error set.</returns>
        public static ErrorSet ValidatePronunciation(string pronunciation, Language[] languages)
        {
            if (string.IsNullOrEmpty(pronunciation))
            {
                throw new ArgumentNullException("pronunciation");
            }

            pronunciation = Pronunciation.RemoveUnitBoundary(pronunciation);

            ErrorSet errorSet = new ErrorSet();

            // Validate specified wordPron with multi-language, if any language is right, then clear errorSet and return.
            foreach (Language language in languages)
            {
                ErrorSet validError = Pronunciation.Validate(pronunciation, Localor.GetPhoneSet(language));

                if (validError.Count == 0)
                {
                    errorSet.Clear();
                    break;
                }
                else
                {
                    errorSet.Merge(validError);
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Validate pronunciation.
        /// </summary>
        /// <param name="pronunciation">Pronunciation to be validate.</param>
        /// <param name="phoneMap">Phone map.</param>
        /// <param name="phoneSet">Phone set.</param>
        /// <returns>Error set.</returns>
        public static ErrorSet ValidatePronunciation(string pronunciation,
            PhoneMap phoneMap, TtsPhoneSet phoneSet)
        {
            if (string.IsNullOrEmpty(pronunciation))
            {
                throw new ArgumentNullException("pronunciation");
            }

            if (phoneMap == null)
            {
                throw new ArgumentNullException("phoneMap");
            }

            if (phoneSet == null)
            {
                throw new ArgumentNullException("phoneSet");
            }

            pronunciation = Pronunciation.RemoveUnitBoundary(pronunciation);

            ErrorSet errorSet = new ErrorSet();
            pronunciation = Pronunciation.GetMappedPronunciation(pronunciation, phoneMap, errorSet);
            if (errorSet.Count == 0)
            {
                errorSet.AddRange(Pronunciation.Validate(pronunciation, phoneSet));
            }

            return errorSet;
        }

        /// <summary>
        /// Validate config file schema.
        /// </summary>
        /// <param name="filePath">Xml file to be validate.</param>
        public static void Validate(string filePath)
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
        }

        /// <summary>
        /// Validate pronunciation.
        /// </summary>
        /// <param name="pronunciation">Pronunciation to be validate.</param>
        /// <param name="isUIPronunciation">Whether is UI pronuncion.</param>
        /// <returns>Error set.</returns>
        public ErrorSet ValidatePronunciation(string pronunciation, bool isUIPronunciation)
        {
            ErrorSet errorSet = new ErrorSet();
            if (isUIPronunciation)
            {
                if (_uiToModelPhoneMap != null)
                {
                    errorSet.AddRange(ValidatePronunciation(pronunciation,
                        _uiToModelPhoneMap, Localor.GetPhoneSet(_language)));
                }
                else
                {
                    errorSet.AddRange(ValidatePronunciation(pronunciation, new Language[] { _language }));
                }
            }
            else
            {
                errorSet.AddRange(ValidatePronunciation(pronunciation, _languages));
            }

            return errorSet;
        }

        /// <summary>
        /// Validate script script reviewer's config data.
        /// </summary>
        public void Validate()
        {
            ErrorSet errorSet = new ErrorSet();

            foreach (VoiceCreationLanguageData languageData in _languageDataCollection)
            {
                ErrorSet validError = languageData.ValidateLanguageData(languageData.Language);

                if (validError.Count == 0)
                {
                    errorSet.Clear();
                }
                else
                {
                    errorSet.AddRange(validError);
                }
            }

            if (errorSet.Errors.Count > 0)
            {
                throw new InvalidDataException(errorSet.ErrorsString(true));
            }

            if (!string.IsNullOrEmpty(WaveDir) && !Directory.Exists(WaveDir))
            {
                string message = Helper.NeutralFormat("WaveDir can not be found at [{0}].",
                    WaveDir);
                throw new InvalidDataException(message);
            }

            if (!string.IsNullOrEmpty(_fileListFilePath) && !File.Exists(_fileListFilePath))
            {
                string message = Helper.NeutralFormat("Filelist can not be found at [{0}].",
                    _fileListFilePath);
                throw new InvalidDataException(message);
            }

            if (!string.IsNullOrEmpty(SegmentDir) && !Directory.Exists(SegmentDir) && MappedSegment)
            {
                string message = Helper.NeutralFormat("SegmentDir can not be found at [{0}].",
                    SegmentDir);
                throw new InvalidDataException(message);
            }

            if (string.IsNullOrEmpty(_scriptPath) ||
                (!File.Exists(_scriptPath) && !Directory.Exists(_scriptPath)))
            {
                string message = Helper.NeutralFormat("ScriptPath can not be found at [{0}].",
                    _scriptPath);
                throw new InvalidDataException(message);
            }
        }

        /// <summary>
        /// Load settings form config.xml.
        /// </summary>
        /// <param name="filePath">String type.</param>
        public void Load(string filePath)
        {
            Validate(filePath);

            // Load dom
            XmlDocument dom = new XmlDocument();
            XmlNamespaceManager nm = new XmlNamespaceManager(dom.NameTable);
            nm.AddNamespace("tts", ConfigSchema.TargetNamespace);

            Reset();

            dom.Load(filePath);

            // Load language files
            ParseLanguageFiles(dom, nm);

            _updateSentence = true;
            XmlNode xmlNode = dom.DocumentElement.SelectSingleNode("@updateSentence", nm);
            if (xmlNode != null)
            {
                _updateSentence = bool.Parse(xmlNode.InnerText);
            }

            // Load wave directory
            xmlNode = dom.DocumentElement.SelectSingleNode("tts:waveDir/@path", nm);
            if (xmlNode != null)
            {
                WaveDir = xmlNode.InnerText;
            }

            // Load file list.
            xmlNode = dom.DocumentElement.SelectSingleNode("tts:filelist/@path", nm);
            if (xmlNode != null)
            {
                _fileListFilePath = xmlNode.InnerText;
            }

            // Load segment mapped
            _mappedSegment = false;
            xmlNode = dom.DocumentElement.SelectSingleNode("tts:alignmentDir/@mapped", nm);
            if (xmlNode != null)
            {
                _mappedSegment = bool.Parse(xmlNode.InnerText);
            }

            // Load segment directory
            SegmentDir = string.Empty;
            if (_mappedSegment)
            {
                xmlNode = dom.DocumentElement.SelectSingleNode("tts:alignmentDir/@path", nm);
                if (xmlNode != null)
                {
                    SegmentDir = xmlNode.InnerText;
                }
            }

            // Read script file path
            xmlNode = dom.DocumentElement.SelectSingleNode("tts:scriptFile/@path", nm);
            if (xmlNode != null)
            {
                _scriptPath = xmlNode.InnerText;
            }

            // Read script file path
            xmlNode = dom.DocumentElement.SelectSingleNode("tts:scriptFile/@mapPath", nm);
            if (xmlNode != null)
            {
                _modelToUiPhoneMapFilePath = xmlNode.InnerText;
            }

            // Read script file path
            XmlNodeList xmlNodes = dom.DocumentElement.SelectNodes("tts:domains/tts:domain", nm);
            if (xmlNodes != null)
            {
                foreach (XmlNode node in xmlNodes)
                {
                    _domainNames.Add(node.InnerText);
                }
            }
        }

        /// <summary>
        /// Get script items the tool loaded.
        /// </summary>
        /// <returns>Enumerator of the lines in the given file.</returns>
        public IEnumerable<ScriptItem> ScriptItems()
        {
            foreach (ScriptItem scriptItem in _scriptFileCollection.ScriptItems(true))
            {
                if (_filteredItemIds.Contains(scriptItem.Id))
                {
                    yield return scriptItem;
                }
            }
        }

        /// <summary>
        /// Get script items the tool loaded.
        /// </summary>
        /// <returns>Enumerator of the lines in the given file.</returns>
        public IEnumerable<ScriptSentence> ScriptSentences()
        {
            foreach (ScriptItem scriptItem in _scriptFileCollection.ScriptItems(true))
            {
                if (_filteredItemIds.Contains(scriptItem.Id))
                {
                    foreach (ScriptSentence scriptSentence in scriptItem.Sentences)
                    {
                        yield return scriptSentence;
                    }
                }
            }
        }

        /// <summary>
        /// Load config data: language data, filelist and script.
        /// </summary>
        /// <param name="recoverDir">If not string.Empty, recover the scritp from this dir.</param>
        /// <returns>ErrorSet.</returns>
        public ErrorSet LoadConfigData(string recoverDir)
        {
            ErrorSet errorSet = new ErrorSet();

            foreach (VoiceCreationLanguageData languageData in _languageDataCollection)
            {
                languageData.SetLanguageData(languageData.Language);
            }

            errorSet.AddRange(LoadScriptCollection(recoverDir));
            errorSet.AddRange(PrepareFilteredItemId());

            LoadPhoneMap();

            if (_modelToUiPhoneMap != null)
            {
                errorSet.AddRange(RemoveEmptyPronunciationItems());
                errorSet.AddRange(RemoveErrorMapPronunciationItems());
            }

            bool updateSentence = _updateSentence;

            // Disable update sentence to raise performance.
            _updateSentence = false;
            LoadScriptComments();
            _updateSentence = updateSentence;

            // Automatically update all item text.
            foreach (ScriptItem scriptItem in ScriptItems())
            {
                SyncItemTextFromWordList(scriptItem);
            }

            return errorSet;
        }

        /// <summary>
        /// Sync item text from word text.
        /// </summary>
        /// <param name="scriptWord">Script word to be synced.</param>
        public void SyncWordChangesToItem(ScriptWord scriptWord)
        {
            if (_updateSentence)
            {
                ScriptHelper.SyncWordChangesToItem(scriptWord);
            }
        }

        /// <summary>
        /// Sync item text from word text.
        /// </summary>
        /// <param name="scriptItem">Script word to be synced.</param>
        public void SyncItemTextFromWordList(ScriptItem scriptItem)
        {
            if (_updateSentence)
            {
                ScriptHelper.SyncItemTextFromWordList(scriptItem);
            }
        }

        /// <summary>
        /// Save comments into the given location in a XML file type.
        /// </summary>
        /// <param name="filePath">The location of the comment file to save.</param>
        public void Save(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (_scriptFileCollection.XmlScriptFiles.Count == 0)
            {
                return;
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.Unicode;
            settings.Indent = true;
            using (XmlWriter writer = XmlWriter.Create(filePath, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("scriptComments", "http://schemas.microsoft.com/tts");
                writer.WriteAttributeString("lang", Localor.LanguageToString(_language));

                writer.WriteStartElement("scriptFile");
                writer.WriteAttributeString("path", _scriptPath);
                if (!string.IsNullOrEmpty(_modelToUiPhoneMapFilePath))
                {
                    writer.WriteAttributeString("mapPath", _modelToUiPhoneMapFilePath);
                }

                writer.WriteEndElement();

                writer.WriteStartElement("languagesData");

                foreach (VoiceCreationLanguageData languageData in _languageDataCollection)
                {
                    languageData.SaveLanguageData(writer);
                }

                writer.WriteEndElement();

                if (_fileListFilePath != null && !string.IsNullOrEmpty(_fileListFilePath))
                {
                    writer.WriteStartElement("filelist");
                    writer.WriteAttributeString("path", _fileListFilePath);
                    writer.WriteEndElement();
                }

                if (!string.IsNullOrEmpty(_waveDir))
                {
                    writer.WriteStartElement("waveDir");
                    writer.WriteAttributeString("path", _waveDir);
                    writer.WriteEndElement();
                }

                if (!string.IsNullOrEmpty(_segmentDir))
                {
                    writer.WriteStartElement("alignmentDir");
                    writer.WriteAttributeString("path", _segmentDir);
                    writer.WriteEndElement();
                }

                if (_domainNames.Count > 0)
                {
                    writer.WriteStartElement("domains");
                    foreach (string domainName in _domainNames)
                    {
                        writer.WriteStartElement("domain");
                        writer.WriteString(domainName);
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            NeedSave = false;
            Validate(filePath);
        }

        /// <summary>
        /// Remove comment.
        /// </summary>
        /// <returns>Can not reverted comments.</returns>
        /// <param name="comment">The comment.</param>
        public Collection<ScriptWordComment> RemoveComment(ScriptWordComment comment)
        {
            Collection<ScriptWordComment> comments = new Collection<ScriptWordComment>();
            comments.Add(comment);
            return RemoveComments(comments);
        }

        /// <summary>
        /// Remove comment.
        /// </summary>
        /// <param name="comments">Comment to remove.</param>
        /// <returns>Can not reverted comments.</returns>
        public Collection<ScriptWordComment> RemoveComments(Collection<ScriptWordComment> comments)
        {
            if (comments == null)
            {
                throw new ArgumentNullException("comments");
            }

            Collection<ScriptWordComment> canNotRevertComments = new Collection<ScriptWordComment>();

            foreach (ScriptWordComment comment in comments)
            {
                NeedSave = true;

                if (comment.ScriptWord != null)
                {
                    if (!SidCommentMap.ContainsKey(comment.Sid))
                    {
                        throw new InvalidDataException(
                            Helper.NeutralFormat("Can't find sentence [{0}] for comment", comment.Sid));
                    }

                    if (comment.ScriptWord.Sentence.Words.Count == 1 &&
                        comment.ScriptWord.Sentence.Words[0] == comment.ScriptWord &&
                        comment.WordEditStatus == TtsXmlStatus.EditStatus.Add)
                    {
                        canNotRevertComments.Add(comment);
                    }
                    else
                    {
                        SidCommentMap[comment.Sid].Remove(comment);
                        if (SidCommentMap[comment.Sid].Count == 0)
                        {
                            SidCommentMap.Remove(comment.Sid);
                        }

                        WordCommentMap.Remove(comment.ScriptWord);
                        XmlScriptCommentHelper.UndoChangeInScript(comment.ScriptWord);
                        SyncWordChangesToItem(comment.ScriptWord);
                    }
                }
                else if (comment.ScriptItem != null)
                {
                    ItemCommentMap.Remove(comment.ScriptItem);
                    XmlScriptCommentHelper.UndoChangeInScript(comment.ScriptItem);
                }
            }

            foreach (ScriptWordComment comment in canNotRevertComments)
            {
                comments.Remove(comment);
            }

            CommentRemoved(this, new EventArgs<Collection<ScriptWordComment>>(comments));
            return canNotRevertComments;
        }

        /// <summary>
        /// Get wave file path via sentence id.
        /// </summary>
        /// <param name="sid">Sentence id.</param>
        /// <returns>Path of the wave file.</returns>
        public string GetWaveFilePath(string sid)
        {
            string filePath = null;
            if (!string.IsNullOrEmpty(_waveDir) && _waveFileList != null)
            {
                if (_waveFileList.Map.ContainsKey(sid))
                {
                    filePath = Path.Combine(_waveDir, _waveFileList.Map[sid] + ".wav");
                }
            }

            return filePath;
        }

        /// <summary>
        /// Get wave segment path via sentence id.
        /// </summary>
        /// <param name="sid">Sentence id.</param>
        /// <returns>Path of the segment file.</returns>
        public string GetSegmentFilePath(string sid)
        {
            string filePath = null;
            if (!string.IsNullOrEmpty(_segmentDir) && _segFileList != null)
            {
                if (_segFileList.Map.ContainsKey(sid))
                {
                    filePath = Path.Combine(_segmentDir, _segFileList.Map[sid] + ".txt");
                }
            }

            return filePath;
        }

        /// <summary>
        /// Find words.
        /// </summary>
        /// <param name="searchWordText">Word text to be found.</param>
        /// <param name="searchWordPron">Word pronunciation to be found.</param>
        /// <param name="startId">Start sentence Id.</param>
        /// <param name="endId">End sentence Id.</param>
        /// <param name="matchCase">Whether matching case when searching word.</param>
        /// <param name="matchWholeWord">Whether matching whole words.</param>
        /// <param name="findWords">Found words.</param>
        public void FindWord(string searchWordText, string searchWordPron,
            string startId, string endId, bool matchCase, bool matchWholeWord,
            Collection<ScriptWord> findWords)
        {
            if (findWords == null)
            {
                throw new ArgumentNullException("findWords");
            }

            if (string.IsNullOrEmpty(searchWordText) && string.IsNullOrEmpty(searchWordPron))
            {
                throw new ArgumentException("wordText and wordPron should not be both empty!");
            }

            if (!string.IsNullOrEmpty(startId) && !string.IsNullOrEmpty(endId) &&
                ScriptHelper.CompareItemId(startId, endId) > 0)
            {
                throw new ArgumentException("Start Id should no bigger than end Id.");
            }

            // Also update the deleted items.
            foreach (ScriptItem scriptItem in ScriptItems())
            {
                if (!string.IsNullOrEmpty(startId) && ScriptHelper.CompareItemId(startId, scriptItem.Id) > 0)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(endId) && ScriptHelper.CompareItemId(endId, scriptItem.Id) < 0)
                {
                    continue;
                }

                foreach (ScriptSentence sentence in scriptItem.Sentences)
                {
                    foreach (ScriptWord scriptWord in sentence.Words)
                    {
                        if (IsMatchWord(scriptWord, searchWordText, searchWordPron, matchCase, matchWholeWord))
                        {
                            findWords.Add(scriptWord);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Replace words.
        /// </summary>
        /// <param name="searchWordText">Word to be replaced.</param>
        /// <param name="searchWordPron">Pronunciation to be repalced.</param>
        /// <param name="fixedWordText">Word replaced with.</param>
        /// <param name="fixedWordPron">Pronunciation repalced with.</param>
        /// <param name="startId">Start sentence Id.</param>
        /// <param name="endId">End sentence Id.</param>
        /// <param name="matchCase">Whether matching case when searching word.</param>
        /// <param name="matchWholeWord">Whether matching whole words.</param>
        /// <param name="replacedWords">Replaced words.</param>
        /// <returns>Error message.</returns>
        public string ReplaceWord(string searchWordText, string searchWordPron, string fixedWordText,
            string fixedWordPron, string startId, string endId, bool matchCase, bool matchWholeWord,
            Collection<ScriptWord> replacedWords)
        {
            if (replacedWords == null)
            {
                throw new ArgumentNullException("replacedWords");
            }

            if (string.IsNullOrEmpty(fixedWordText) && string.IsNullOrEmpty(fixedWordPron))
            {
                throw new ArgumentException("fixedWordText and fixedWordPron can't be both empty.");
            }

            StringBuilder errorMessage = new StringBuilder();
            Collection<ScriptWord> foundWords = new Collection<ScriptWord>();

            FindWord(searchWordText, searchWordPron, startId, endId,
                matchCase, matchWholeWord, foundWords);
            foreach (ScriptWord scriptWord in foundWords)
            {
                string fixedWholeWordText = fixedWordText;
                string fixedWholeWordPron = fixedWordPron;
                if (!matchWholeWord)
                {
                    if (!string.IsNullOrEmpty(searchWordText))
                    {
                        fixedWholeWordText = Regex.Replace(scriptWord.Grapheme, searchWordText, fixedWordText);
                    }

                    if (!string.IsNullOrEmpty(searchWordPron))
                    {
                        string originalWholeWordPron = scriptWord.Pronunciation;
                        if (!string.IsNullOrEmpty(originalWholeWordPron) && _modelToUiPhoneMap != null)
                        {
                            originalWholeWordPron = Pronunciation.GetMappedPronunciation(
                                originalWholeWordPron, _modelToUiPhoneMap);
                        }

                        fixedWholeWordPron = Regex.Replace(originalWholeWordPron, searchWordPron, fixedWordPron);
                    }

                    if (!string.IsNullOrEmpty(fixedWholeWordPron))
                    {
                        ErrorSet errorSet = ValidatePronunciation(fixedWholeWordPron, true);
                        if (errorSet.Errors.Count > 0)
                        {
                            string originalWholeWordPron = scriptWord.Pronunciation;
                            if (!string.IsNullOrEmpty(originalWholeWordPron) && _modelToUiPhoneMap != null)
                            {
                                originalWholeWordPron = Pronunciation.GetMappedPronunciation(
                                     originalWholeWordPron, _modelToUiPhoneMap);
                            }

                            errorMessage.AppendLine(Helper.NeutralFormat(
                                "Can't replace word's [{0}] pronunciation [{1}] to [{2}] : [{3}]",
                                scriptWord.Grapheme, originalWholeWordPron, fixedWholeWordPron,
                                errorSet.ErrorsString()));
                            continue;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(fixedWholeWordPron) && _uiToModelPhoneMap != null)
                {
                    fixedWholeWordPron = Pronunciation.GetMappedPronunciation(
                        fixedWholeWordPron, _uiToModelPhoneMap);
                }

                bool updated = false;
                if (!string.IsNullOrEmpty(fixedWholeWordText) &&
                    !fixedWholeWordText.Equals(scriptWord.Grapheme, StringComparison.Ordinal) &&
                    XmlScriptCommentHelper.ModifyWordText(scriptWord, fixedWholeWordText))
                {
                    SyncWordChangesToItem(scriptWord);
                    updated = true;
                }

                if (!string.IsNullOrEmpty(fixedWholeWordPron) &&
                    !fixedWholeWordPron.Equals(scriptWord.Pronunciation, StringComparison.Ordinal) &&
                    XmlScriptCommentHelper.ModifyPronText(scriptWord, fixedWholeWordPron))
                {
                    updated = true;
                }

                if (updated)
                {
                    replacedWords.Add(scriptWord);
                    OverwriteComment(scriptWord);
                }
            }

            return errorMessage.ToString();
        }

        /// <summary>
        /// Add new comment to the document.
        /// </summary>
        /// <param name="comment">Comment to add.</param>
        /// <param name="raiseEvent">Falg to indicate whether or not to raise CommentAdded event.</param>
        public void AddComment(ScriptWordComment comment, bool raiseEvent)
        {
            Debug.Assert(comment.ScriptWord != null || comment.ScriptItem != null);

            if (comment.ScriptWord != null)
            {
                if (!_sidCommentMap.ContainsKey(comment.Sid))
                {
                    _sidCommentMap.Add(comment.Sid, new List<ScriptWordComment>());
                }

                _sidCommentMap[comment.Sid].Add(comment);

                if (_wordCommentMap.ContainsKey(comment.ScriptWord))
                {
                    _wordCommentMap[comment.ScriptWord] = comment;
                }
                else
                {
                    _wordCommentMap.Add(comment.ScriptWord, comment);
                }

                SyncWordChangesToItem(comment.ScriptWord);
            }
            else if (comment.ScriptItem != null)
            {
                if (_itemCommentMap.ContainsKey(comment.ScriptItem))
                {
                    _itemCommentMap[comment.ScriptItem] = comment;
                }
                else
                {
                    _itemCommentMap.Add(comment.ScriptItem, comment);
                }
            }

            NeedSave = true;
            if (raiseEvent)
            {
                CommentAdded(this, new EventArgs<ScriptWordComment>(comment));
            }
        }

        /// <summary>
        /// Reset.
        /// </summary>
        public void Reset()
        {
            _waveDir = string.Empty;
            _segmentDir = string.Empty;
            _scriptPath = string.Empty;
            _fileListFilePath = string.Empty;
            _modelToUiPhoneMapFilePath = string.Empty;
            _needSave = false;
            _segFileList = null;
            _waveFileList = null;
            _uiToModelPhoneMap = null;
            _modelToUiPhoneMap = null;

            _scriptFileCollection.Reset();
            _filteredItemIds.Clear();
            _sidCommentMap.Clear();
            _wordCommentMap.Clear();
            _itemCommentMap.Clear();
            _languageDataCollection.Clear();
        }

        /// <summary>
        /// Overwrite word comment.
        /// </summary>
        /// <param name="scriptItem">Script word whose comment to be updated.</param>
        public void OverwriteComment(ScriptItem scriptItem)
        {
            if (ItemCommentMap.ContainsKey(scriptItem))
            {
                Debug.Assert(ItemCommentMap.ContainsKey(scriptItem), "Should call AddComment.");
                ScriptWordComment existComment = ItemCommentMap[scriptItem];
                NeedSave = true;
                SyncItemTextFromWordList(scriptItem);
                CommentUpdated(this, new EventArgs<ScriptWordComment>(existComment));
            }
            else
            {
                ScriptWordComment comment = new ScriptWordComment(scriptItem);
                AddComment(comment, true);
            }
        }

        /// <summary>
        /// Overwrite word comment.
        /// </summary>
        /// <param name="scriptWord">Script word whose comment to be updated.</param>
        public void OverwriteComment(ScriptWord scriptWord)
        {
            if (WordCommentMap.ContainsKey(scriptWord))
            {
                Debug.Assert(WordCommentMap.ContainsKey(scriptWord), "Should call AddComment.");
                ScriptWordComment existComment = WordCommentMap[scriptWord];
                Debug.Assert(existComment.ScriptWord != null);
                if (existComment.WordEditStatus == TtsXmlStatus.EditStatus.Original)
                {
                    RemoveComment(existComment);
                }
                else
                {
                    CommentUpdated(this, new EventArgs<ScriptWordComment>(existComment));
                }

                NeedSave = true;
                SyncWordChangesToItem(scriptWord);
            }
            else
            {
                ScriptWordComment comment = new ScriptWordComment(scriptWord, _modelToUiPhoneMap);
                AddComment(comment, true);
            }
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Check whether the value match search value.
        /// </summary>
        /// <param name="textValue">Text value to be check.</param>
        /// <param name="searchValue">Search value.</param>
        /// <param name="matchCase">Whether match case.</param>
        /// <param name="matchWholeWord">Whether match whole word.</param>
        /// <returns>Whether the value matched.</returns>
        private static bool IsMatchValue(string textValue, string searchValue,
            bool matchCase, bool matchWholeWord)
        {
            if (string.IsNullOrEmpty(searchValue))
            {
                throw new ArgumentNullException("searchValue");
            }

            bool match = false;
            if (!string.IsNullOrEmpty(textValue))
            {
                if (matchCase && matchWholeWord)
                {
                    match = textValue.Equals(searchValue, StringComparison.Ordinal);
                }
                else if (!matchCase && matchWholeWord)
                {
                    match = textValue.Equals(searchValue, StringComparison.OrdinalIgnoreCase);
                }
                else if (matchCase && !matchWholeWord)
                {
                    match = textValue.IndexOf(searchValue, 0, StringComparison.Ordinal) >= 0;
                }
                else if (!matchCase && !matchWholeWord)
                {
                    match = textValue.IndexOf(searchValue, 0, StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }

            return match;
        }

        /// <summary>
        /// Remove error mapped pronunciation items.
        /// </summary>
        /// <returns>The errorset.</returns>
        private ErrorSet RemoveErrorMapPronunciationItems()
        {
            ErrorSet errorSet = new ErrorSet();
            foreach (ScriptItem item in ScriptItems())
            {
                bool validItem = true;
                foreach (ScriptWord scriptWord in item.AllWords)
                {
                    if (scriptWord.WordType == WordType.Normal)
                    {
                        ErrorSet mapWordPronErrorSet = new ErrorSet();
                        Pronunciation.GetMappedPronunciation(scriptWord, _modelToUiPhoneMap,
                            mapWordPronErrorSet);
                        if (mapWordPronErrorSet.Errors.Count > 0)
                        {
                            errorSet.AddRange(mapWordPronErrorSet);
                            validItem = false;
                        }

                        if (validItem)
                        {
                            TtsXmlStatus status = XmlScriptCommentHelper.GetWordCommentStatus(
                                scriptWord, XmlScriptCommentHelper.WordPronStatusName);
                            if (status != null && status.Status == TtsXmlStatus.EditStatus.Modify)
                            {
                                Debug.Assert(!string.IsNullOrEmpty(status.OriginalValue));

                                mapWordPronErrorSet.Clear();
                                Pronunciation.GetMappedPronunciation(status.OriginalValue, _modelToUiPhoneMap,
                                    mapWordPronErrorSet);
                                foreach (Error error in mapWordPronErrorSet.Errors)
                                {
                                    errorSet.Add(new Error(XmlScriptReviewerDocumentError.InvalidNormalWordOriginalPronunciation,
                                        error, scriptWord.Grapheme, status.OriginalValue, item.Id));
                                    validItem = false;
                                }
                            }
                        }
                    }
                }

                if (!validItem)
                {
                    _filteredItemIds.Remove(item.Id);
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Remove empty pronunciation items.
        /// </summary>
        /// <returns>The errorset.</returns>
        private ErrorSet RemoveEmptyPronunciationItems()
        {
            ErrorSet errorSet = new ErrorSet();
            foreach (ScriptItem item in ScriptItems())
            {
                bool validItem = true;
                foreach (ScriptWord scriptWord in item.AllWords)
                {
                    if (scriptWord.WordType == WordType.Normal)
                    {
                        if (string.IsNullOrEmpty(scriptWord.Pronunciation))
                        {
                            errorSet.Add(XmlScriptReviewerDocumentError.EmptyNormalWordPronunciation,
                                scriptWord.Grapheme, item.Id);
                            validItem = false;
                        }

                        if (validItem)
                        {
                            TtsXmlStatus status = XmlScriptCommentHelper.GetWordCommentStatus(
                                scriptWord, XmlScriptCommentHelper.WordPronStatusName);
                            if (status != null && status.Status == TtsXmlStatus.EditStatus.Modify)
                            {
                                if (string.IsNullOrEmpty(status.OriginalValue))
                                {
                                    errorSet.Add(XmlScriptReviewerDocumentError.EmptyNormalWordOriginalPronunciation,
                                        scriptWord.Grapheme, item.Id);
                                    validItem = false;
                                }
                            }
                        }
                    }
                }

                if (!validItem)
                {
                    _filteredItemIds.Remove(item.Id);
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Load phoen map.
        /// </summary>
        private void LoadPhoneMap()
        {
            if (!string.IsNullOrEmpty(_modelToUiPhoneMapFilePath))
            {
                _modelToUiPhoneMap = PhoneMap.CreatePhoneMap(_modelToUiPhoneMapFilePath);
                _uiToModelPhoneMap = new PhoneMap(_modelToUiPhoneMap.Language,
                    _modelToUiPhoneMap.Target, _modelToUiPhoneMap.Source);
                foreach (string sourcePhone in _modelToUiPhoneMap.Items.Keys)
                {
                    if (!_uiToModelPhoneMap.Items.ContainsKey(_modelToUiPhoneMap.Items[sourcePhone]))
                    {
                        _uiToModelPhoneMap.Items.Add(_modelToUiPhoneMap.Items[sourcePhone], sourcePhone);
                    }
                }
            }
        }

        /// <summary>
        /// Prepare filtered item id.
        /// </summary>
        /// <returns>Error set.</returns>
        private ErrorSet PrepareFilteredItemId()
        {
            ErrorSet errorSet = new ErrorSet();
            if (!string.IsNullOrEmpty(_fileListFilePath))
            {
                _waveFileList = FileListMap.CreateInstance(FileListMap.ReadAllData(_fileListFilePath, false));
                _segFileList = FileListMap.CreateInstance(_waveFileList.Map);

                foreach (string itemId in _waveFileList.Map.Keys)
                {
                    if (_scriptFileCollection.FindItem(itemId, true) != null)
                    {
                        _filteredItemIds.Add(itemId);
                    }
                }

                foreach (string sid in _waveFileList.Map.Keys)
                {
                    if (!_filteredItemIds.Contains(sid))
                    {
                        errorSet.Add(XmlScriptReviewerDocumentError.SentenceNotInScriptError, sid);
                    }
                }
            }
            else
            {
                foreach (ScriptItem item in _scriptFileCollection.ScriptItems(true))
                {
                    _filteredItemIds.Add(item.Id);
                }

                if (!string.IsNullOrEmpty(_waveDir))
                {
                    _waveFileList = FileListMap.CreateInstance(_waveDir, ".wav");
                }

                if (!string.IsNullOrEmpty(_segmentDir))
                {
                    _segFileList = FileListMap.CreateInstance(_segmentDir, ".txt");
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Load script collection.
        /// </summary>
        /// <param name="recoverDir">Recover dir.</param>
        /// <returns>ErrorSet.</returns>
        private ErrorSet LoadScriptCollection(string recoverDir)
        {
            _scriptFileCollection.Language = _language;
            ErrorSet errorSet = new ErrorSet();
            if (!string.IsNullOrEmpty(recoverDir))
            {
                if (!Directory.Exists(recoverDir))
                {
                    throw Helper.CreateException(typeof(DirectoryNotFoundException), recoverDir);
                }

                errorSet.AddRange(_scriptFileCollection.Load(recoverDir));
                if (Directory.Exists(_scriptPath))
                {
                    foreach (XmlScriptFile xmlScriptFile in _scriptFileCollection.XmlScriptFiles)
                    {
                        xmlScriptFile.FilePath = Path.Combine(_scriptPath,
                            Path.GetFileName(xmlScriptFile.FilePath));
                    }
                }
                else if (File.Exists(_scriptPath))
                {
                    Debug.Assert(_scriptFileCollection.XmlScriptFiles.Count == 1);
                    _scriptFileCollection.XmlScriptFiles[0].FilePath = _scriptPath;
                }
            }
            else
            {
                errorSet.AddRange(_scriptFileCollection.Load(_scriptPath));
            }

            return errorSet;
        }

        /// <summary>
        /// Check whether the script word match the search condition.
        /// </summary>
        /// <param name="scriptWord">Script word.</param>
        /// <param name="searchWordText">Search word text.</param>
        /// <param name="searchWordPron">Search word pron.</param>
        /// <param name="matchCase">If match case.</param>
        /// <param name="matchWholeWord">If match whole word.</param>
        /// <returns>If it is match word.</returns>
        private bool IsMatchWord(ScriptWord scriptWord, string searchWordText, string searchWordPron,
            bool matchCase, bool matchWholeWord)
        {
            if (scriptWord == null)
            {
                throw new ArgumentNullException("scriptWord");
            }

            if (string.IsNullOrEmpty(searchWordText) && string.IsNullOrEmpty(searchWordPron))
            {
                throw new ArgumentException("wordText and wordPron can't be both empty");
            }

            bool match = false;
            string wordPron = scriptWord.Pronunciation;
            if (!string.IsNullOrEmpty(wordPron) && _modelToUiPhoneMap != null)
            {
                wordPron = Pronunciation.GetMappedPronunciation(wordPron, _modelToUiPhoneMap);
            }

            if (!string.IsNullOrEmpty(searchWordText) && !string.IsNullOrEmpty(searchWordPron) &&
                IsMatchValue(scriptWord.Grapheme, searchWordText, matchCase, matchWholeWord) &&
                IsMatchValue(wordPron, searchWordPron, matchCase, matchWholeWord))
            {
                match = true;
            }
            else if (!string.IsNullOrEmpty(searchWordText) && string.IsNullOrEmpty(searchWordPron) &&
                IsMatchValue(scriptWord.Grapheme, searchWordText, matchCase, matchWholeWord))
            {
                match = true;
            }
            else if (string.IsNullOrEmpty(searchWordText) && !string.IsNullOrEmpty(searchWordPron) &&
                IsMatchValue(wordPron, searchWordPron, matchCase, matchWholeWord))
            {
                match = true;
            }

            return match;
        }

        /// <summary>
        /// Load word comments.
        /// </summary>
        private void LoadScriptComments()
        {
            foreach (ScriptSentence sentence in ScriptSentences())
            {
                foreach (ScriptWord word in sentence.Words)
                {
                    if (XmlScriptCommentHelper.GetScriptWordEditStatus(word) != TtsXmlStatus.EditStatus.Original)
                    {
                        ScriptWordComment comment = new ScriptWordComment(word, _modelToUiPhoneMap);
                        AddComment(comment, false);
                    }
                }

                foreach (ScriptWord word in sentence.DeletedWordsDict.Keys)
                {
                    ScriptWordComment comment = new ScriptWordComment(word, _modelToUiPhoneMap);
                    AddComment(comment, false);
                }
            }

            foreach (ScriptItem scriptItem in ScriptItems())
            {
                string timeStamp;
                XmlScriptCommentHelper.ScriptItemStatus status = XmlScriptCommentHelper.GetScriptItemStatus(
                    scriptItem, out timeStamp);
                if (status != XmlScriptCommentHelper.ScriptItemStatus.Original)
                {
                    ScriptWordComment comment = new ScriptWordComment(scriptItem);
                    AddComment(comment, false);
                }
            }
        }

        /// <summary>
        /// Parse language files.
        /// </summary>
        /// <param name="dom">Config document.</param>
        /// <param name="nm">Namespace.</param>
        private void ParseLanguageFiles(XmlDocument dom, XmlNamespaceManager nm)
        {
            XmlNode xmlNode = dom.DocumentElement.SelectSingleNode("@lang", nm);

            if (xmlNode == null)
            {
                string message = Helper.NeutralFormat(@"No ""scriptFile"" element with attribute ""lang"" defined.");
                throw new InvalidDataException(message);
            }

            _language = Localor.StringToLanguage(xmlNode.InnerText);

            XmlNodeList xmlNodeList = dom.DocumentElement.SelectNodes(@"tts:languagesData/tts:languageData", nm);

            _languages = new Language[xmlNodeList.Count];

            int languageCount = 0;
            foreach (XmlNode languageNode in xmlNodeList)
            {
                VoiceCreationLanguageData languageData = new VoiceCreationLanguageData();

                languageData.ParseLanguageDataFromXmlElement(true, languageNode as XmlElement);

                _languageDataCollection.Add(languageData);

                _languages[languageCount++] = languageData.Language;
            }
        }

        #endregion
    }
}