//----------------------------------------------------------------------------
// <copyright file="XmlScriptFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements XML script entry
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
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// XML Script file class.
    /// </summary>
    /// <example>
    ///     <code lang="C#" title="The following code example demonstrates the usage of XmlScriptFile class.">
    /// Using System;
    /// Using System.Collections.Generic;
    /// Using System.Text;
    /// Using Microsoft.Tts.Offline;
    /// Using Microsoft.Tts.Offline.Utility;
    /// Namespace FrameworkSample
    /// {
    ///     class Program
    ///     {
    ///         private static int Main(string[] args)
    ///         {
    ///             XmlScriptFile xmlScriptFile = new XmlScriptFile();
    ///             xmlScriptFile.Load(@"\\tts\shanhai\TTSData\ttsdata\en-GB\Language\Scripts\Scripts_0322326_0410275.xml");
    ///             foreach (ScriptItem item in xmlScriptFile.Items)
    ///             {
    ///                 StringBuilder sb = new StringBuilder();
    ///                 sb.AppendLine(Helper.NeutralFormat("ItemID={0}", item.Id));
    ///                 sb.AppendLine(Helper.NeutralFormat("ItemText={0}", item.Text));
    ///                 foreach (ScriptSentence sentence in item.Sentences)
    ///                 {
    ///                     sb.AppendLine(Helper.NeutralFormat("\tSentenceIndex={0}", item.Sentences.IndexOf(sentence)));
    ///                     sb.AppendLine(Helper.NeutralFormat("\tSentenceText={0}", sentence.Text));
    ///                     foreach (ScriptWord word in sentence.Words)
    ///                     {
    ///                         sb.AppendLine(Helper.NeutralFormat("\t\tWordText={0}, Pron={1}, WordType={2}, Pos={3}",
    ///                             word.Grapheme, word.Pronunciation, word.WordType.ToString(), word.Pos));
    ///                     }
    ///                 }
    ///                 Console.WriteLine(sb.ToString());
    ///             }
    ///             xmlScriptFile.Save(@"D:\script.xml");
    ///         }
    ///     }
    /// }.
    ///     </code>
    /// </example>
    public class XmlScriptFile : XmlDataFile
    {
        #region Public fields

        /// <summary>
        /// The extension of file.
        /// </summary>
        public const string Extension = @".xml";

        /// <summary>
        /// Deleted sentence item status XML element name.
        /// </summary>
        public const string DeletedItemStatusName = "si";

        #endregion

        #region Private fields

        private static XmlSchema _schema;

        private string _version = string.Empty;
        private string _sayAs = string.Empty;
        private Collection<ScriptItem> _items = new Collection<ScriptItem>();
        private TtsPhoneSet _phoneSet;
        private TtsPosSet _posSet;
        private TtsXmlComments _ttsXmlComments = new TtsXmlComments();
        private Dictionary<string, ScriptItem> _itemDic = new Dictionary<string, ScriptItem>();
        private Dictionary<ScriptItem, TtsXmlStatus> _deletedItemsDict = new Dictionary<ScriptItem, TtsXmlStatus>();

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the XmlScriptFile class.
        /// </summary>
        /// <param name="language">Language name.</param>
        public XmlScriptFile(Language language)
            : base(language)
        {
        }

        /// <summary>
        /// Initializes a new instance of the XmlScriptFile class.
        /// </summary>
        public XmlScriptFile()
        {
        }

        /// <summary>
        /// Initializes a new instance of the XmlScriptFile class.
        /// </summary>
        /// <param name="filePath">The location of the script file to load.</param>
        public XmlScriptFile(string filePath)
        {
            Load(filePath);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the script text is ssml.
        /// </summary>
        public bool IsSsml { get; set; }

        /// <summary>
        /// Gets Tts XML comments.
        /// </summary>
        public TtsXmlComments TtsXmlComments
        {
            get { return _ttsXmlComments; }
        }

        /// <summary>
        /// Gets Deleted items.
        /// </summary>
        public Dictionary<ScriptItem, TtsXmlStatus> DeletedItemsDict
        {
            get { return _deletedItemsDict; }
        }

        /// <summary>
        /// Gets or sets Script version.
        /// </summary>
        public string Version
        {
            get { return _version; }
            set { _version = value; }
        }

        /// <summary>
        /// Gets or sets Say-as type the script applied.
        /// </summary>
        public string SayAs
        {
            get { return _sayAs; }
            set { _sayAs = value; }
        }

        /// <summary>
        /// Gets or sets Category: Lexicon, spell, Lts, etc.
        /// </summary>
        public string Category
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Phone set.
        /// </summary>
        public TtsPhoneSet PhoneSet
        {
            get { return _phoneSet; }
            set { _phoneSet = value; }
        }

        /// <summary>
        /// Gets or sets Pos set.
        /// </summary>
        public TtsPosSet PosSet
        {
            get { return _posSet; }
            set { _posSet = value; }
        }

        /// <summary>
        /// Gets Schema of script.xml.
        /// </summary>
        public override XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.script.xsd");
                }

                _schema.Includes.Clear();
                XmlSchemaInclude posIncluded = new XmlSchemaInclude();
                posIncluded.Schema =
                    XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.PosTable.xsd");
                _schema.Includes.Add(posIncluded);

                XmlSchemaInclude phoneIncluded = new XmlSchemaInclude();
                phoneIncluded.Schema =
                    XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.phoneset.xsd");
                _schema.Includes.Add(phoneIncluded);

                XmlSchemaInclude commentsIncluded = new XmlSchemaInclude();
                commentsIncluded.Schema =
                    XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.ttsxmlcomments.xsd");
                _schema.Includes.Add(commentsIncluded);

                return _schema;
            }
        }

        /// <summary>
        /// Gets The script items this file contains.
        /// </summary>
        public Collection<ScriptItem> Items
        {
            get { return _items; }
        }

        /// <summary>
        /// Gets The script item key-value pairs.
        /// </summary>
        public Dictionary<string, ScriptItem> ItemDic
        {
            get { return _itemDic; }
        }

        #endregion

        #region public static methods

        /// <summary>
        /// Load script and check it.
        /// </summary>
        /// <param name="scriptFile">File to be loaded.</param>
        /// <param name="validateSetting">Validation data set.</param>
        /// <returns>Script loaded.</returns>
        public static XmlScriptFile LoadWithValidation(string scriptFile, XmlScriptValidateSetting validateSetting)
        {
            if (string.IsNullOrEmpty(scriptFile))
            {
                throw new ArgumentNullException("scriptFile");
            }

            if (validateSetting == null)
            {
                throw new ArgumentNullException("validateSetting");
            }

            validateSetting.VerifySetting();

            XmlScriptFile script = new XmlScriptFile();
            script.Load(scriptFile);

            script.PhoneSet = validateSetting.PhoneSet;
            script.PosSet = validateSetting.PosSet;
            script.Validate(validateSetting);

            return script;
        }

        /// <summary>
        /// Check whether an item is compliant with schema.
        /// </summary>
        /// <param name="item">Item to be checked.</param>
        public static void CheckSchema(ScriptItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            // currently disable id format checking, we will enable it when
            // all script id are re-set
            // if (!Regex.IsMatch(item.Id, @"^[0-9]{10}$"))
            if (string.IsNullOrEmpty(item.Id))
            {
                string message = Helper.NeutralFormat("Script id [{0}] is invalid.", item.Id);
                throw new InvalidDataException(message);
            }

            if (string.IsNullOrEmpty(item.Text))
            {
                string message = Helper.NeutralFormat("Script id [{0}] is invalid.", item.Id);
                throw new InvalidDataException(message);
            }

            foreach (ScriptSentence sentence in item.Sentences)
            {
                if (string.IsNullOrEmpty(sentence.Text))
                {
                    string message = Helper.NeutralFormat("Sentence text in item [{0}] is empty.", item.Id);
                    throw new InvalidDataException(message);
                }

                foreach (ScriptWord word in sentence.Words)
                {
                    if (string.IsNullOrEmpty(word.Grapheme) && word.WordType != WordType.Silence)
                    {
                        string message = Helper.NeutralFormat("word in item [{0}] is empty.", item.Id);
                        throw new InvalidDataException(message);
                    }

                    foreach (ScriptSyllable syllable in word.Syllables)
                    {
                        foreach (ScriptPhone phone in syllable.Phones)
                        {
                            if (string.IsNullOrEmpty(phone.Name))
                            {
                                string message = Helper.NeutralFormat("phone in item [{0}] is empty.",
                                    item.Id);
                                throw new InvalidDataException(message);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Load one script item from the xmltextreader.
        /// </summary>
        /// <param name="reader">XmlTextReader.</param>
        /// <param name="contentController">ContentControler.</param>
        /// <param name="language">The language of the script.</param>
        /// <returns>ScriptItem that read.</returns>
        public static ScriptItem LoadItem(XmlTextReader reader, object contentController, Language language)
        {
            Debug.Assert(reader != null);

            ContentControler scriptContentController = new ContentControler();
            if (contentController is ContentControler)
            {
                scriptContentController = contentController as ContentControler;
            }
            else if (contentController != null)
            {
                throw new ArgumentException("Invalid contentController type");
            }

            ScriptItem item = new ScriptItem(language);

            // get id, domain and reading difficulty
            if (!string.IsNullOrEmpty(reader.GetAttribute("id")))
            {
                item.Id = reader.GetAttribute("id");
            }
            else 
            {
                string message = "Script id value cannot be null.";
                throw new ArgumentException(message);
            }

            string domain = reader.GetAttribute("domain");
            if (!string.IsNullOrEmpty(domain))
            {
                item.Domain = ScriptItem.StringToDomainType(domain);
            }

            string frequency = reader.GetAttribute("frequency");
            if (!string.IsNullOrEmpty(frequency))
            {
                item.Frequency = int.Parse(frequency);
            }

            string score = reader.GetAttribute("difficulty");
            if (!string.IsNullOrEmpty(score))
            {
                item.ReadingDifficulty = double.Parse(score, CultureInfo.InvariantCulture);
            }

            // get the text and sentences
            if (!reader.IsEmptyElement)
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "sent")
                    {
                        ScriptSentence sentence = LoadSentence(reader, scriptContentController, language);
                        sentence.ScriptItem = item;
                        item.Sentences.Add(sentence);
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.Name == "text")
                    {
                        reader.Read();
                        item.Text = reader.Value;
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.Name == "comments")
                    {
                        if (scriptContentController.LoadComments)
                        {
                            item.TtsXmlComments.Parse(reader);
                            item.TtsXmlComments.Tag = item;
                        }
                        else
                        {
                            reader.Skip();
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "si")
                    {
                        break;
                    }
                }
            }

            return item;
        }

        /// <summary>
        /// Load one script word from the xmltextreader.
        /// </summary>
        /// <param name="reader">XmlTextReader.</param>
        /// <param name="contentController">ContentControler.</param>
        /// <param name="language">The language of the script.</param>
        /// <returns>ScriptWord that read.</returns>
        public static ScriptWord LoadWord(XmlTextReader reader, object contentController, Language language)
        {
            Debug.Assert(reader != null);

            ContentControler scriptContentController = new ContentControler();
            if (contentController is ContentControler)
            {
                scriptContentController = contentController as ContentControler;
            }
            else if (contentController != null)
            {
                throw new ArgumentException("Invalid contentController type");
            }

            ScriptWord word = new ScriptWord(language);

            // load attributes
            LoadWordAttributes(word, reader, language);

            // load syllables
            // remember that word can have no syllable list
            if (!reader.IsEmptyElement)
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "syls")
                    {
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "syl")
                            {
                                ScriptSyllable syllable = LoadSyllable(reader, language);
                                syllable.Word = word;
                                word.Syllables.Add(syllable);
                            }
                            else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "syls")
                            {
                                break;
                            }
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.Name == "acoustics")
                    {
                        word.Acoustics = new ScriptAcoustics();
                        word.Acoustics.ParseFromXml(reader);
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.Name == "comments")
                    {
                        if (scriptContentController.LoadComments)
                        {
                            word.TtsXmlComments.Parse(reader);
                            word.TtsXmlComments.Tag = word;
                        }
                        else
                        {
                            reader.Skip();
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "w")
                    {
                        break;
                    }
                }
            }

            return word;
        }

        /// <summary>
        /// Load one script named entity from the xml text reader.
        /// </summary>
        /// <param name="reader">The XML reader instance to read data from.</param>
        /// <param name="sentence">Script sentence.</param>
        /// <param name="scriptContentController">ContentControler.</param>
        /// <returns>ScriptNamedEntity instance that read.</returns>
        public static ScriptNamedEntity LoadNamedEntity(XmlTextReader reader,
            ScriptSentence sentence, ContentControler scriptContentController)
        {
            Debug.Assert(reader != null);
            Debug.Assert(scriptContentController != null);
            ScriptNamedEntity entity = new ScriptNamedEntity();
            entity.Type = reader.GetAttribute("type");
            entity.Text = reader.GetAttribute("v");
            string pos = reader.GetAttribute("pos");
            if (!string.IsNullOrEmpty(pos))
            {
                entity.PosString = pos;
            }

            Debug.Assert(sentence.Words.Count > 0);
            int startIndex = int.Parse(reader.GetAttribute("s"), CultureInfo.InvariantCulture);
            int endIndex = int.Parse(reader.GetAttribute("e"), CultureInfo.InvariantCulture);

            Collection<ScriptWord> graphemeWords = sentence.TextWords;
            if (startIndex < 0 && startIndex >= graphemeWords.Count)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Invalid start index for sentence [{0}] : [{1}]",
                    sentence.ScriptItem.GetSentenceId(sentence), startIndex));
            }

            entity.Start = graphemeWords[startIndex];

            if (endIndex < 0 || endIndex >= graphemeWords.Count)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Invalid end index for sentence [{0}] : [{1}]",
                    sentence.ScriptItem.GetSentenceId(sentence), endIndex));
            }

            entity.End = graphemeWords[endIndex];
            return entity;
        }

        #endregion

        #region public override operations

        /// <summary>
        /// Validate 
        /// Note that the items to be validated are compatible with script schema
        /// If user doesn't want to check POS, he/she should set _phoneSet = null.
        /// </summary>
        /// <param name="setting">Validation setting.</param>
        public override void Validate(XmlValidateSetting setting)
        {
            if (setting == null)
            {
                throw new ArgumentNullException("setting");
            }

            XmlScriptValidateSetting validateSetting = setting as XmlScriptValidateSetting;
            validateSetting.VerifySetting();

            XmlScriptValidationScope scope = validateSetting.ValidationScope;

            if (scope != XmlScriptValidationScope.None)
            {
                foreach (ScriptItem item in Items)
                {
                    ErrorSet errors = new ErrorSet();
                    ScriptItem.IsValidItem(item, errors, validateSetting);
                    ErrorSet.Merge(errors);
                }
            }
        }

        #endregion

        #region public operations

        /// <summary>
        /// Get item by using item ID, if don't search deleted items can get it from ItemDict directly,
        /// When need search deleted items, need call this function to find the item..
        /// </summary>
        /// <param name="itemId">ID of the item to be found.</param>
        /// <param name="searchDeletedItem">Whether search deleted items.</param>
        /// <returns>Founded script item.</returns>
        public ScriptItem GetItem(string itemId, bool searchDeletedItem)
        {
            ScriptItem scriptItem = null;

            if (ItemDic.ContainsKey(itemId))
            {
                scriptItem = ItemDic[itemId];
            }
            else if (searchDeletedItem)
            {
                foreach (ScriptItem deletedScriptItem in DeletedItemsDict.Keys)
                {
                    if (deletedScriptItem.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase))
                    {
                        scriptItem = deletedScriptItem;
                        break;
                    }
                }
            }

            return scriptItem;
        }

        /// <summary>
        /// Get script sentence from script file.
        /// </summary>
        /// <param name="sentenceId">Sentence ID to be found.</param>
        /// <param name="searchDeletedSentence">Whether search deleted sentences.</param>
        /// <returns>Founded script sentence.</returns>
        public ScriptSentence GetSentence(string sentenceId, bool searchDeletedSentence)
        {
            if (string.IsNullOrEmpty(sentenceId))
            {
                throw new ArgumentNullException("sentenceId");
            }

            int sentenceIndex = 0;
            string itemId = ScriptHelper.GetItemIdFromSentenceId(sentenceId, ref sentenceIndex);
            ScriptItem scriptItem = GetItem(itemId, searchDeletedSentence);
            ScriptSentence scriptSentence = null;
            if (scriptItem != null && sentenceIndex - 1 < scriptItem.Sentences.Count)
            {
                scriptSentence = scriptItem.Sentences[sentenceIndex - 1];
            }

            return scriptSentence;
        }

        /// <summary>
        /// Indicating whether the item has been deleted.
        /// </summary>
        /// <param name="itemId">Item ID to be justified..</param>
        /// <returns>Whether the item has been deleted.</returns>
        public bool IsDeletedItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            bool isDeletedItem = false;
            foreach (ScriptItem item in _deletedItemsDict.Keys)
            {
                if (itemId.Equals(item.Id, StringComparison.OrdinalIgnoreCase))
                {
                    isDeletedItem = true;
                    break;
                }
            }

            return isDeletedItem;
        }

        /// <summary>
        /// Add one item to script file.
        /// This method will check whether the item is balid before adding.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        /// <param name="errors">The errors if failed to add.</param>
        /// <param name="validate">Whether validate schema and content.</param>
        /// <param name="sort">Whether insert the script item in the sort position.</param>
        /// <returns>True if successfully added.</returns>
        public bool Add(ScriptItem item, ErrorSet errors, bool validate, bool sort)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (errors == null)
            {
                throw new ArgumentNullException("errors");
            }

            // check schema, should throw exception if invalid
            CheckSchema(item);

            bool added = true;
            errors.Clear();

            // content checking, should add to errors if invalid
            if (_itemDic.ContainsKey(item.Id))
            {
                errors.Add(ScriptError.DuplicateItemId, item.Id);
            }

            if (validate)
            {
                ErrorSet contentErrors = new ErrorSet();
                XmlScriptValidateSetting validateSetting = new XmlScriptValidateSetting(PhoneSet, PosSet);
                ScriptItem.IsValidItem(item, contentErrors, validateSetting);
                errors.Merge(contentErrors);
            }

            if (errors.Count > 0)
            {
                added = false;
            }

            if (added)
            {
                _itemDic.Add(item.Id, item);
                if (sort)
                {
                    bool inserted = false;
                    for (int i = 0; i < _items.Count; i++)
                    {
                        if (string.Compare(item.Id, _items[i].Id, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            _items.Insert(i, item);
                            inserted = true;
                            break;
                        }
                    }

                    if (!inserted)
                    {
                        _items.Add(item);
                    }
                }
                else
                {
                    _items.Add(item);
                }
            }

            return added;
        }

        /// <summary>
        /// Add one item to script file.
        /// This method will check whether the item is balid before adding.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        /// <param name="errors">The errors if failed to add.</param>
        /// <param name="validate">Whether validate schema and content.</param>
        /// <returns>True if successfully added.</returns>
        public bool Add(ScriptItem item, ErrorSet errors, bool validate)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (errors == null)
            {
                throw new ArgumentNullException("errors");
            }

            return Add(item, errors, validate, false);
        }

        /// <summary>
        /// Remove item from file.
        /// </summary>
        /// <param name="itemId">Item ID to be removed.</param>
        public void Remove(string itemId)
        {
            Collection<string> itemIds = new Collection<string>();
            itemIds.Add(itemId);
            Remove(itemIds);
        }

        /// <summary>
        /// Remove items from file.
        /// </summary>
        /// <param name="itemIds">Ids of items to be removed.</param>
        public void Remove(IEnumerable<string> itemIds)
        {
            if (itemIds == null)
            {
                throw new ArgumentNullException("itemIds");
            }

            Dictionary<string, string> ids = new Dictionary<string, string>();
            foreach (string id in itemIds)
            {
                if (!ids.ContainsKey(id))
                {
                    ids.Add(id, id);
                }
            }

            if (ids.Count > 0)
            {
                Collection<ScriptItem> leftItems = new Collection<ScriptItem>();
                _itemDic.Clear();
                foreach (ScriptItem item in _items)
                {
                    if (!ids.ContainsKey(item.Id))
                    {
                        leftItems.Add(item);
                        _itemDic.Add(item.Id, item);
                    }
                }

                _items = leftItems;
            }
        }

        #endregion

        #region protected override operations

        /// <summary>
        /// Performance loading.
        /// </summary>
        /// <param name="reader">Stream reader.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void PerformanceLoad(StreamReader reader, object contentController)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            ContentControler scriptContentController = new ContentControler();
            if (contentController is ContentControler)
            {
                scriptContentController = contentController as ContentControler;
            }
            else if (contentController != null)
            {
                throw new ArgumentException("Invalid contentController type");
            }

            XmlTextReader xmlTextReader = new XmlTextReader(reader);
            while (xmlTextReader.Read())
            {
                if (xmlTextReader.NodeType == XmlNodeType.Element && xmlTextReader.Name == "script")
                {
                    Language = Localor.StringToLanguage(xmlTextReader.GetAttribute("language"));
                    string version = xmlTextReader.GetAttribute("version");
                    if (version != null)
                    {
                        _version = version;
                    }

                    string sayAs = xmlTextReader.GetAttribute("say-as");
                    if (sayAs != null)
                    {
                        _sayAs = sayAs;
                    }

                    string isSsml = xmlTextReader.GetAttribute("isssml");
                    if (!string.IsNullOrEmpty(isSsml))
                    {
                        IsSsml = bool.Parse(isSsml);
                    }
                }
                else if (xmlTextReader.NodeType == XmlNodeType.Element && xmlTextReader.Name == "comments")
                {
                    if (scriptContentController.LoadComments)
                    {
                        _ttsXmlComments.Parse(xmlTextReader);
                        _ttsXmlComments.Tag = this;
                    }
                    else
                    {
                        xmlTextReader.Skip();
                    }
                }
                else if (xmlTextReader.NodeType == XmlNodeType.Element && xmlTextReader.Name == "si")
                {
                    ScriptItem scriptItem = null;
                    try
                    {
                        scriptItem = LoadItem(xmlTextReader, scriptContentController, Language);
                        scriptItem.ScriptFile = this;
                        scriptItem.IsSsml = IsSsml;
                        if (_itemDic.ContainsKey(scriptItem.Id))
                        {
                            // don't allow duplicate ID 
                            ErrorSet.Add(ScriptError.DuplicateItemId, scriptItem.Id);
                        }
                        else
                        {
                            _items.Add(scriptItem);
                            _itemDic.Add(scriptItem.Id, scriptItem);
                        }
                    }
                    catch (InvalidDataException ex)
                    {
                        ErrorSet.Add(ScriptError.OtherErrors, "null", Helper.BuildExceptionMessage(ex));
                    }
                }
            }

            if (scriptContentController.LoadComments)
            {
                ParseDeletedItemsFromComments(Language);
            }
        }

        /// <summary>
        /// Save script into Xml writer.
        /// </summary>
        /// <param name="writer">Writer file to save into.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void PerformanceSave(XmlWriter writer, object contentController)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            ContentControler scriptContentController = new ContentControler();
            if (contentController is ContentControler)
            {
                scriptContentController = contentController as ContentControler;
            }
            else if (contentController != null)
            {
                throw new ArgumentException("Invalid contentController type");
            }

            WriteTo(writer, scriptContentController);
        }

        #endregion

        #region private operations

        /// <summary>
        /// Load phone from XmlTextReader.
        /// </summary>
        /// <param name="reader">XmlTextReader.</param>
        /// <returns>ScriptPhone.</returns>
        private static ScriptPhone LoadPhone(XmlTextReader reader)
        {
            Debug.Assert(reader != null);

            ScriptPhone phone = new ScriptPhone(reader.GetAttribute("v"));

            string valid = reader.GetAttribute("valid");
            if (!string.IsNullOrEmpty(valid))
            {
                phone.Valid = bool.Parse(valid);
            }

            string tone = reader.GetAttribute("tone");
            if (!string.IsNullOrEmpty(tone))
            {
                phone.Tone = tone;
            }

            string stress = reader.GetAttribute("stress");
            if (!string.IsNullOrEmpty(stress))
            {
                phone.Stress = ScriptSyllable.StringToStress(stress);
            }

            string sentenceID = reader.GetAttribute("sentenceID");
            if (!string.IsNullOrEmpty(sentenceID))
            {
                phone.SentenceId = sentenceID;
            }

            string unitIndex = reader.GetAttribute("unitIndex");
            if (!string.IsNullOrEmpty(unitIndex))
            {
                phone.UnitIndex = int.Parse(unitIndex);
            }

            if (!reader.IsEmptyElement)
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "states")
                    {
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "state")
                            {
                                ScriptState state = LoadState(reader);
                                state.Phone = phone;
                                phone.States.Add(state);
                            }
                            else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "states")
                            {
                                break;
                            }
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.Name == "acoustics")
                    {
                        phone.Acoustics = new ScriptAcoustics();
                        phone.Acoustics.ParseFromXml(reader);
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "ph")
                    {
                        break;
                    }
                }
            }

            return phone;
        }

        /// <summary>
        /// Load state from XmlTextReader.
        /// </summary>
        /// <param name="reader">Xml text reader.</param>
        /// <returns>Script state.</returns>
        private static ScriptState LoadState(XmlTextReader reader)
        {
            Debug.Assert(reader != null);
            ScriptState state = new ScriptState();

            if (!reader.IsEmptyElement)
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "acoustics")
                    {
                        state.Acoustics = new ScriptAcoustics();
                        state.Acoustics.ParseFromXml(reader);
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "state")
                    {
                        break;
                    }
                }
            }

            return state;
        }

        /// <summary>
        /// Load the attributes for a given word.
        /// </summary>
        /// <param name="word">ScriptWord.</param>
        /// <param name="reader">XmlTextReader.</param>
        /// <param name="scriptLanguage">The language of the script.</param>
        private static void LoadWordAttributes(ScriptWord word, XmlTextReader reader,
            Language scriptLanguage)
        {
            Debug.Assert(word != null);
            Debug.Assert(reader != null);

            string wordLanguage = reader.GetAttribute("language");
            if (!string.IsNullOrEmpty(wordLanguage) &&
                Localor.StringToLanguage(wordLanguage) != scriptLanguage)
            {
                word.Language = Localor.StringToLanguage(wordLanguage);
            }

            word.Grapheme = reader.GetAttribute("v");

            string pron = reader.GetAttribute("p");
            if (!string.IsNullOrEmpty(pron))
            {
                word.Pronunciation = pron;
            }

            word.AcceptGrapheme = reader.GetAttribute("av");
            string acceptPron = reader.GetAttribute("ap");
            if (!string.IsNullOrEmpty(acceptPron))
            {
                word.AcceptPronunciation = acceptPron;
            }

            string type = reader.GetAttribute("type");
            if (!string.IsNullOrEmpty(type))
            {
                word.WordType = ScriptWord.StringToWordType(type);
            }

            if (word.Grapheme == null || word.Grapheme.Length == 0)
            {
                if (word.WordType != WordType.Silence &&
                    word.WordType != WordType.Punctuation)
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Line [{0}]: only silence word, prosody boundary or punctuation can have null/empty word grapheme",
                        reader.LineNumber));
                }
            }
            else
            {
                if (word.WordType == WordType.Silence)
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Line [{0}]: silence word or prosody boundary should have empty word grapheme",
                        reader.LineNumber));
                }
            }

            string pos = reader.GetAttribute("pos");
            if (!string.IsNullOrEmpty(pos))
            {
                word.PosString = pos;
            }

            string expansion = reader.GetAttribute("exp");
            if (!string.IsNullOrEmpty(expansion))
            {
                word.Expansion = expansion;
            }

            string emphasis = reader.GetAttribute("em");
            if (!string.IsNullOrEmpty(emphasis))
            {
                word.Emphasis = ScriptWord.StringToEmphasis(emphasis);
            }

            string breakLevel = reader.GetAttribute("br");
            if (!string.IsNullOrEmpty(breakLevel))
            {
                word.Break = ScriptWord.StringToBreak(breakLevel);
            }

            string breakAskLevel = reader.GetAttribute("bra");
            if (!string.IsNullOrEmpty(breakAskLevel))
            {
                word.BreakAsk = ScriptWord.StringToBreak(breakAskLevel);
            }
            else
            {
                word.BreakAsk = ScriptWord.UndefinedBreakAsk;
            }

            string breakProb = reader.GetAttribute("brp");
            if (!string.IsNullOrEmpty(breakProb))
            {
                word.BreakProb = float.Parse(breakProb, CultureInfo.InvariantCulture);
            }
            else
            {
                word.BreakProb = ScriptWord.DefaultProbability;
            }

            string wordTone = reader.GetAttribute("wt");
            if (!string.IsNullOrEmpty(wordTone))
            {
                word.WordTone = ScriptWord.StringToWordTone(wordTone);
            }

            string tobiibt = reader.GetAttribute("tobiibt");
            if (!string.IsNullOrEmpty(tobiibt))
            {
                word.TobiInitialBoundaryTone = new TobiLabel(tobiibt);
            }

            string tobifbt = reader.GetAttribute("tobifbt");
            if (!string.IsNullOrEmpty(tobifbt))
            {
                word.TobiFinalBoundaryTone = new TobiLabel(tobifbt);
            }

            string domain = reader.GetAttribute("domain");
            if (!string.IsNullOrEmpty(domain))
            {
                word.AcousticDomainTag = domain;
            }

            string nusTag = reader.GetAttribute("nus");
            if (!string.IsNullOrEmpty(nusTag))
            {
                word.NusTag = nusTag;
            }

            string regularText = reader.GetAttribute("regularText");
            if (!string.IsNullOrEmpty(regularText))
            {
                word.RegularText = regularText;
            }

            string sp = reader.GetAttribute("sp");
            if (!string.IsNullOrEmpty(sp))
            {
                word.ShallowParseTag = sp;
            }

            string tcgppScore = reader.GetAttribute("tcgppScore");
            if (!string.IsNullOrEmpty(tcgppScore))
            {
                word.TcgppScores = tcgppScore;
            }

            string pronSource = reader.GetAttribute("pronSource");
            if (!string.IsNullOrEmpty(pronSource))
            {
                word.PronSource = ScriptWord.StringToPronSource(pronSource);
            }

            string netype = reader.GetAttribute("netype");
            if (!string.IsNullOrEmpty(netype))
            {
                word.NETypeText = netype;
            }

            if (word.WordType != WordType.Silence)
            {
                string offset = reader.GetAttribute("offset");
                if (!string.IsNullOrEmpty(offset))
                {
                    word.OffsetInString = int.Parse(offset);
                }

                string length = reader.GetAttribute("length");
                if (!string.IsNullOrEmpty(length))
                {
                    word.LengthInString = int.Parse(length);
                }
            }
        }

        /// <summary>
        /// Load one sentence from the xml text reader.
        /// </summary>
        /// <param name="reader">XmlTextReader.</param>
        /// <param name="scriptContentController">ContentControler.</param>
        /// <param name="language">The language of the script.</param>
        /// <returns>Sentence that read.</returns>
        private static ScriptSentence LoadSentence(XmlTextReader reader, ContentControler scriptContentController, Language language)
        {
            Debug.Assert(reader != null);
            Debug.Assert(scriptContentController != null);
            ScriptSentence sentence = new ScriptSentence(language);

            // get sentence type
            string type = reader.GetAttribute("type");
            if (!string.IsNullOrEmpty(type))
            {
                sentence.SentenceType = ScriptSentence.StringToSentenceType(type);
            }
            
            // get sentence emotion type
            string emotion = reader.GetAttribute("emotion");
            if (!string.IsNullOrEmpty(emotion))
            {
                sentence.Emotion = ScriptSentence.StringToEmotionType(emotion);
            }

            // get the text and word list
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "text")
                {
                    reader.Read();
                    sentence.Text = reader.Value;
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "words")
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "w")
                        {
                            ScriptWord word = LoadWord(reader, scriptContentController, language);
                            word.Sentence = sentence;
                            sentence.Words.Add(word);
                        }
                        else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "words")
                        {
                            break;
                        }
                    }
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "accept")
                {
                    List<ScriptWord> acceptSent = new List<ScriptWord>();
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "w")
                        {
                            ScriptWord acceptWord = LoadWord(reader, scriptContentController, language);
                            acceptWord.Sentence = sentence;
                            acceptSent.Add(acceptWord);
                        }
                        else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "accept")
                        {
                            break;
                        }
                    }

                    sentence.AcceptSentences.Add(acceptSent);
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "nes")
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "ne")
                        {
                            ScriptNamedEntity entity = LoadNamedEntity(reader, sentence, scriptContentController);
                            sentence.NamedEntities.Add(entity);
                        }
                        else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "nes")
                        {
                            break;
                        }
                    }
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "comments")
                {
                    if (scriptContentController.LoadComments)
                    {
                        sentence.TtsXmlComments.Parse(reader);
                        sentence.TtsXmlComments.Tag = sentence;
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "sent")
                {
                    break;
                }
            }

            if (scriptContentController.LoadComments)
            {
                ParseDeletedWordsFromComments(sentence, language);
            }

            return sentence;
        }

        /// <summary>
        /// Parse deleted words from comments.
        /// </summary>
        /// <param name="scriptSentence">Script sentence to be parse.</param>
        /// <param name="scriptLanguage">The language of the script.</param>
        private static void ParseDeletedWordsFromComments(ScriptSentence scriptSentence, Language scriptLanguage)
        {
            scriptSentence.DeletedWordsDict.Clear();
            scriptSentence.DeletedWordAndFollowingWordDict.Clear();

            if (scriptSentence.TtsXmlComments.TtsXmlStatusDict.ContainsKey(ScriptSentence.DeletedWordStatusName))
            {
                SortedDictionary<int, SortedDictionary<int, ScriptWord>> deletedWordDict =
                    new SortedDictionary<int, SortedDictionary<int, ScriptWord>>();

                foreach (TtsXmlStatus status in scriptSentence.TtsXmlComments.TtsXmlStatusDict[ScriptSentence.DeletedWordStatusName])
                {
                    using (StringReader sr = new StringReader(status.OriginalValue))
                    {
                        XmlTextReader xtr = new XmlTextReader(sr);
                        if (!xtr.IsEmptyElement)
                        {
                            while (xtr.Read())
                            {
                                if (xtr.NodeType == XmlNodeType.Element && xtr.Name == "w")
                                {
                                    ScriptWord word = LoadWord(xtr, null, scriptLanguage);
                                    word.Sentence = scriptSentence;
                                    scriptSentence.DeletedWordsDict.Add(word, status);
                                    if (status.Position == TtsXmlStatus.UnsetPosition)
                                    {
                                        status.Position = scriptSentence.Words.Count;
                                    }

                                    if (status.DelIndex == TtsXmlStatus.UnsetPosition)
                                    {
                                        status.DelIndex = 0;
                                    }

                                    if (!deletedWordDict.ContainsKey(status.Position))
                                    {
                                        deletedWordDict.Add(status.Position, new SortedDictionary<int, ScriptWord>());
                                    }

                                    // To keep compatable with old format(which doesn't contains this parameter), need automatically
                                    // update del index.
                                    while (deletedWordDict[status.Position].ContainsKey(status.DelIndex))
                                    {
                                        status.DelIndex++;
                                    }

                                    deletedWordDict[status.Position].Add(status.DelIndex, word);
                                }
                            }
                        }
                    }                    
                }

                foreach (int position in deletedWordDict.Keys)
                {
                    List<ScriptWord> deletedWordInTheSamePosition = new List<ScriptWord>();
                    foreach (int delIndex in deletedWordDict[position].Keys)
                    {
                        deletedWordInTheSamePosition.Add(deletedWordDict[position][delIndex]);
                    }

                    if (deletedWordInTheSamePosition.Count > 0)
                    {
                        for (int i = 0; i < deletedWordInTheSamePosition.Count - 1; i++)
                        {
                            scriptSentence.DeletedWordAndFollowingWordDict.Add(deletedWordInTheSamePosition[i + 1],
                                deletedWordInTheSamePosition[i]);
                        }

                        ScriptWord nextWord = position < scriptSentence.Words.Count ?
                            scriptSentence.Words[position] : null;
                        scriptSentence.DeletedWordAndFollowingWordDict.Add(
                                deletedWordInTheSamePosition[0], nextWord);
                    }
                }
            }
        }

        /// <summary>
        /// Load syllable from XmlTextReader.
        /// </summary>
        /// <param name="reader">XmlTextReader.</param>
        /// <param name="language">The language of the script.</param>
        /// <returns>ScriptSyllable.</returns>
        private static ScriptSyllable LoadSyllable(XmlTextReader reader, Language language)
        {
            Debug.Assert(reader != null);
            ScriptSyllable syllable = new ScriptSyllable(language);

            // load attributes
            string stress = reader.GetAttribute("stress");
            if (!string.IsNullOrEmpty(stress))
            {
                syllable.Stress = ScriptSyllable.StringToStress(stress);
            }

            string tobipa = reader.GetAttribute("tobipa");
            syllable.TobiPitchAccent = TobiLabel.Create(tobipa);

            // load phone
            if (!reader.IsEmptyElement)
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "phs")
                    {
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "ph")
                            {
                                ScriptPhone phone = LoadPhone(reader);
                                phone.Syllable = syllable;
                                syllable.Phones.Add(phone);
                            }
                            else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "phs")
                            {
                                break;
                            }
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.Name == "acoustics")
                    {
                        syllable.Acoustics = new ScriptAcoustics();
                        syllable.Acoustics.ParseFromXml(reader);
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "syl")
                    {
                        break;
                    }
                }
            }

            return syllable;
        }

        /// <summary>
        /// Parse deleted items from comments.
        /// </summary>
        /// <param name="scriptLanguage">Script language.</param>
        private void ParseDeletedItemsFromComments(Language scriptLanguage)
        {
            DeletedItemsDict.Clear();
            if (_ttsXmlComments.TtsXmlStatusDict.ContainsKey(DeletedItemStatusName))
            {
                foreach (TtsXmlStatus status in _ttsXmlComments.TtsXmlStatusDict[DeletedItemStatusName])
                {
                    using (StringReader sr = new StringReader(status.OriginalValue))
                    {
                        XmlTextReader xtr = new XmlTextReader(sr);

                        if (!xtr.IsEmptyElement)
                        {
                            while (xtr.Read())
                            {
                                if (xtr.NodeType == XmlNodeType.Element && xtr.Name == "si")
                                {
                                    XmlScriptFile.ContentControler controler = new ContentControler();
                                    controler.LoadComments = true;
                                    ScriptItem scriptItem = LoadItem(xtr, controler, scriptLanguage);
                                    scriptItem.ScriptFile = this;
                                    DeletedItemsDict.Add(scriptItem, status);
                                }
                            }
                        }
                    }        
                }
            }
        }

        /// <summary>
        /// Write deleted item to comments.
        /// </summary>
        private void WriteDeletedItemToComments()
        {
            if (TtsXmlComments.TtsXmlStatusDict.ContainsKey(DeletedItemStatusName))
            {
                TtsXmlComments.TtsXmlStatusDict.Remove(DeletedItemStatusName);
            }

            foreach (ScriptItem scriptItem in _deletedItemsDict.Keys)
            {
                scriptItem.TtsXmlComments.Reset();
                foreach (ScriptSentence sentence in scriptItem.Sentences)
                {
                    sentence.TtsXmlComments.Reset();
                    sentence.DeletedWordsDict.Clear();
                    foreach (ScriptWord word in sentence.Words)
                    {
                        word.TtsXmlComments.Reset();
                    }
                }

                // Delete the sentence.
                StringBuilder sb = new StringBuilder();
                using (XmlWriter sw = XmlWriter.Create(sb))
                {
                    XmlScriptFile.ContentControler contentControler = new ContentControler();
                    contentControler.SaveComments = true;
                    scriptItem.WriteToXml(sw, contentControler, Language);
                    sw.Flush();
                    _deletedItemsDict[scriptItem].OriginalValue = sb.ToString();

                    TtsXmlComments.AppendStatus(_deletedItemsDict[scriptItem], true);
                }
            }
        }

        /// <summary>
        /// Write script to xml.
        /// </summary>
        /// <param name="writer">XmlWriter.</param>
        /// <param name="scriptContentController">Content controller.</param>
        private void WriteTo(XmlWriter writer, ContentControler scriptContentController)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            if (scriptContentController == null)
            {
                throw new ArgumentNullException("scriptContentController");
            }

            writer.WriteStartElement("script", "http://schemas.microsoft.com/tts");
            writer.WriteAttributeString("language", Localor.LanguageToString(Language));
            if (!string.IsNullOrEmpty(Version))
            {
                writer.WriteAttributeString("version", Version);
            }

            if (!string.IsNullOrEmpty(SayAs))
            {
                writer.WriteAttributeString("say-as", SayAs);
            }

            if (!string.IsNullOrEmpty(Category))
            {
                writer.WriteAttributeString("category", Category);
            }

            if (IsSsml)
            {
                writer.WriteAttributeString("isssml", IsSsml.ToString().ToLowerInvariant());
            }

            if (scriptContentController.SaveComments)
            {
                WriteDeletedItemToComments();
                _ttsXmlComments.WriteToXml(writer);
            }

            Collection<string> invalidTexts = new Collection<string>();

            for (int i = 0; i < Items.Count; ++i)
            {
                ScriptItem item = Items[i];
                item.IsSsml = IsSsml;

                if (!string.IsNullOrEmpty(item.Text))
                {
                    if (XmlHelper.IsValidXMLText(item.Text))
                    {
                        item.WriteToXml(writer, scriptContentController, Language);
                    }
                    else
                    {
                        // Save invalid text.
                        ErrorSet.Add(ScriptError.InvalidXmlCharactersError, (i + 1).ToString(), item.Text);
                    }
                }
            }

            writer.WriteEndElement();
        }

        #endregion

        /// <summary>
        /// XmlScriptFile content controler.
        /// </summary>
        public class ContentControler
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ContentControler"/> class.
            /// </summary>
            public ContentControler()
            {
                SaveComments = true;
                SavePronSource = true;
            }

            /// <summary>
            /// Gets or sets a value indicating whether save comment.
            /// </summary>
            public bool SaveComments { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether load comment.
            /// </summary>
            public bool LoadComments { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether save pronunciation source.
            /// </summary>
            public bool SavePronSource { get; set; }
        }
    }
}