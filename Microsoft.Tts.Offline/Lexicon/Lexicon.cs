//----------------------------------------------------------------------------
// <copyright file="Lexicon.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements lexicon class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;
#if COREXTBRANCH
    using SP = Microsoft.Tts.ServiceProvider;
#endif

    /// <summary>
    /// Lexicon.
    /// </summary>
    /// <example>
    /// <code lang="C#" title="The following code example demonstrates the usage of Lexicon class.">
    /// Using System;
    /// Using System.Collections.Generic;
    /// Using System.Text;
    /// Using Microsoft.Tts.Offline;
    /// Using Microsoft.Tts.Offline.Utility;
    /// Using Microsoft.Tts.Offline.Core;
    /// Namespace FrameworkSample
    /// {
    ///     class Program
    ///     {
    ///         private static void Main(string[] args)
    ///         {
    ///             Lexicon lexicon = new Lexicon();
    ///             lexicon.Load(@"\\tts\ShanHai\TTSData\ttsdata\en-GB\Language\TAData\Lexicon\lexicon.xml");
    ///             foreach (LexicalItem item in lexicon.Items.Values)
    ///             {
    ///                 StringBuilder sb = new StringBuilder();
    ///                 sb.AppendLine(Helper.NeutralFormat("WordText={0}", item.Grapheme));
    ///                 foreach (LexiconPronunciation pron in item.Pronunciations)
    ///                 {
    ///                     sb.AppendLine(Helper.NeutralFormat("\tPron={0}", pron.Symbolic));
    ///                     foreach (LexiconItemProperty property in pron.Properties)
    ///                     {
    ///                         sb.Append(Helper.NeutralFormat("\t\tPOS={0}", property.PartOfSpeech.Value));
    ///                         foreach (string attrName in property.AttributeSet.Keys)
    ///                         {
    ///                             sb.AppendFormat(",{0}={1}", attrName, property.AttributeSet[attrName]);
    ///                         }
    ///                         sb.AppendLine();
    ///                     }
    ///                 }
    ///                 Console.WriteLine(sb.ToString());
    ///             }
    ///             lexicon.Save(@"D:\lexicon.xml");
    ///         }
    ///     }
    /// }.
    /// </code>
    /// </example>
    public class Lexicon : XmlDataFile, ILexicon
    {
        #region Fields

        private static XmlSchema _schema;

        /// <summary>
        /// This field is case insensitive.
        /// </summary>
        private string _domainTag;
        private Dictionary<string, LexicalItem> _items = new Dictionary<string, LexicalItem>();
        private TtsPhoneSet _ttsPhoneSet;
        private TtsPosSet _ttsPosSet;
        private LexicalAttributeSchema _attributeSchema;
        private bool _isBaseline = false;

        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Lexicon"/> class.
        /// </summary>
        /// <param name="language">Language.</param>
        public Lexicon(Language language)
            : base(language)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Lexicon"/> class.
        /// </summary>
        public Lexicon()
        {
        }

        #endregion

        #region Enums

        /// <summary>
        /// LexiconStatus.
        /// </summary>
        public enum LexiconStatus
        {
            /// <summary>
            /// Original state.
            /// </summary>
            Original = 0,

            /// <summary>
            /// Changed state.
            /// </summary>
            Changed = 1,

            /// <summary>
            /// Deleted state.
            /// </summary>
            Deleted = 2,

            /// <summary>
            /// Added state.
            /// </summary>
            Added = 3,

            /// <summary>
            /// Checked state.
            /// </summary>
            Checked = 4
        }

        /// <summary>
        /// LexiconOrigin.
        /// </summary>
        public enum LexiconOrigin
        {
            /// <summary>
            /// Current origin, also known as sibling.
            /// </summary>
            Current = 0,

            /// <summary>
            /// Baseline origin.
            /// </summary>
            Baseline = 1
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Lexicon domain.
        /// </summary>
        public string DomainTag
        {
            get
            {
                return _domainTag;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException();
                }

                _domainTag = value.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Gets Items.
        /// </summary>
        public Dictionary<string, LexicalItem> Items
        {
            get { return _items; }
        }

        /// <summary>
        /// Gets Schema of lexicon.xml.
        /// </summary>
        public override System.Xml.Schema.XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.lexicon.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets or sets TTS phone set.
        /// </summary>
        public TtsPhoneSet PhoneSet
        {
            get { return _ttsPhoneSet; }
            set { _ttsPhoneSet = value; }
        }

        /// <summary>
        /// Gets or sets TTS POS set.
        /// </summary>
        public TtsPosSet PosSet
        {
            get { return _ttsPosSet; }
            set { _ttsPosSet  = value; }
        }

        /// <summary>
        /// Gets or sets Lexical Attribute Schema.
        /// </summary>
        public LexicalAttributeSchema LexicalAttributeSchema
        {
            get { return _attributeSchema; }
            set { _attributeSchema = value; }
        }

        /// <summary>
        /// Gets or sets base lexicon relative file path.
        /// </summary>
        public string BaseLexiconRelativeFilePath { get; set; }

        #endregion

        #region Public static methods

        /// <summary>
        /// Write all lexicon information into file.
        /// </summary>
        /// <param name="lexiconFilePath">The location of the target lexicon to write.</param>
        /// <param name="lexicon">Lexicon information to write out.</param>
        /// <param name="encoding">Encoding of the lexicon file.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
            Justification = "use lexicon as type is more clear")]
        public static void WriteAllData(string lexiconFilePath, Lexicon lexicon, Encoding encoding)
        {
            lexicon.Save(lexiconFilePath, encoding);
        }

        /// <summary>
        /// Read all lexicon items from XML lexicon file.
        /// </summary>
        /// <param name="lexiconFilePath">XML lexicon filepath.</param>
        /// <returns>Lexicon.</returns>
        public static Lexicon ReadAllData(string lexiconFilePath)
        {
            if (string.IsNullOrEmpty(lexiconFilePath))
            {
                throw new ArgumentNullException("lexiconFilePath");
            }

            Lexicon lexicon = new Lexicon();
            lexicon.Load(lexiconFilePath);
            return lexicon;
        }

        /// <summary>
        /// Create the lexicon from Xml Script file.
        /// </summary>
        /// <param name="scriptFile">Xml script file.</param>
        /// <param name="defaultPos">Part of Speech String.</param>
        /// <param name="mainLexicon">MainLexicon.</param>
        /// <returns>Lexicon.</returns>
        public static Lexicon CreateFromXmlScriptFile(XmlScriptFile scriptFile, string defaultPos, Lexicon mainLexicon)
        {
            if (scriptFile == null)
            {
                throw new ArgumentNullException("scriptFile");
            }

            if (string.IsNullOrEmpty(defaultPos))
            {
                throw new ArgumentNullException("defaultPos");
            }

            Lexicon lexicon = new Lexicon(scriptFile.Language);
            foreach (ScriptItem item in scriptFile.Items)
            {
                foreach (ScriptWord scriptWord in item.AllPronouncedWords)
                {
                    string word = scriptWord.Grapheme;

                    // Create LexiconPronunciaton Node
                    LexiconPronunciation pron = new LexiconPronunciation(lexicon.Language);
                    pron.Symbolic = scriptWord.Pronunciation;

                    if (mainLexicon != null)
                    {
                        LexicalItem mainLexiconItem = mainLexicon.Lookup(word, true);
                        if (mainLexiconItem != null)
                        {
                            LexiconPronunciation lexPron = mainLexiconItem.FindPronunciation(pron.Symbolic, true);
                            if (lexPron != null)
                            {
                                pron.Symbolic = lexPron.Symbolic;
                            }
                        }
                    }

                    LexiconItemProperty property = new LexiconItemProperty();
                    if (string.IsNullOrEmpty(scriptWord.PosString))
                    {
                        property.PartOfSpeech = new PosItem(defaultPos);
                    }
                    else
                    {
                        property.PartOfSpeech = new PosItem(scriptWord.PosString);
                    }

                    pron.Properties.Add(property);
                    
                    if (!lexicon.Items.ContainsKey(word))
                    {
                        LexicalItem lexicalItem = new LexicalItem(lexicon.Language);
                        lexicalItem.Grapheme = word;
                        lexicalItem.Pronunciations.Add(pron);
                        lexicon.Items.Add(word, lexicalItem);
                    }
                    else
                    {
                        bool needAdd = true;
                        foreach (LexiconPronunciation pronunciation in lexicon.Items[word].Pronunciations)
                        {
                            if (pronunciation.Symbolic.Equals(pron.Symbolic, StringComparison.InvariantCultureIgnoreCase))
                            {
                                needAdd = false;
                                if (!pronunciation.Properties.Contains(property))
                                {
                                    pronunciation.Properties.Add(property);
                                }
                            }
                        }

                        if (needAdd)
                        {
                            lexicon.Items[word].Pronunciations.Add(pron);
                        }
                    }
                }
            }
            
            return lexicon;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Validate Lexicon according to pos set and phone set.
        /// </summary>
        /// <param name="ttsPhoneSet">TTS phone set.</param>
        /// <param name="ttsPosSet">TTS POS set.</param>
        public void Validate(TtsPhoneSet ttsPhoneSet, TtsPosSet ttsPosSet)
        {
            if (ttsPosSet == null)
            {
                throw new ArgumentNullException("ttsPosSet");
            }

            if (ttsPhoneSet == null)
            {
                throw new ArgumentNullException("ttsPhoneSet");
            }

            if (!ttsPosSet.Language.Equals(Language))
            {
                throw new InvalidDataException(Error.BuildMessage(CommonError.NotConsistentLanguage,
                    Language.ToString(), "lexicon", ttsPosSet.Language.ToString(), "pos set"));
            }

            if (!ttsPhoneSet.Language.Equals(Language))
            {
                throw new InvalidDataException(Error.BuildMessage(CommonError.NotConsistentLanguage,
                    Language.ToString(), "lexicon", ttsPhoneSet.Language.ToString(), "phone set"));
            }

            Validate(ttsPhoneSet, ttsPosSet, null);
        }

        /// <summary>
        /// Validate Lexicon according to phone set and lexical attribute schema.
        /// </summary>
        /// <param name="ttsPhoneSet">TTS phone set.</param>
        /// <param name="attributeSchema">TTS attribute schema.</param>
        public void Validate(TtsPhoneSet ttsPhoneSet, LexicalAttributeSchema attributeSchema)
        {
            if (attributeSchema == null)
            {
                throw new ArgumentNullException("attributeSchema");
            }

            if (ttsPhoneSet == null)
            {
                throw new ArgumentNullException("ttsPhoneSet");
            }

            if (!attributeSchema.Language.Equals(Language))
            {
                throw new InvalidDataException(Error.BuildMessage(CommonError.NotConsistentLanguage,
                    Language.ToString(), "lexicon", attributeSchema.Language.ToString(), 
                    "lexical attribute Schema"));
            }

            if (!ttsPhoneSet.Language.Equals(Language))
            {
                throw new InvalidDataException(Error.BuildMessage(CommonError.NotConsistentLanguage,
                    Language.ToString(), "lexicon", ttsPhoneSet.Language.ToString(), "phone set"));
            }

            Validate(ttsPhoneSet, null, attributeSchema);
        }

        /// <summary>
        /// Lookup lexicon item for grapheme. If ignoring case, only below types of 
        /// Graphme will be searched:
        /// 1. original grapheme
        /// 2. all letter in grapheme in upper case
        /// 3. all letter in grapheme in lower case
        /// 4. In Pascal style: first letter is in upper case and rest letters are in lower case.
        /// </summary>
        /// <param name="grapheme">Graphme of the word.</param>
        /// <param name="ignoreCase">Ignore the case of the graphme.</param>
        /// <returns>Lexicon item found.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Ignore.")]
        public LexicalItem Lookup(string grapheme, bool ignoreCase)
        {
            if (string.IsNullOrEmpty(grapheme))
            {
                throw new ArgumentNullException("grapheme");
            }

            LexicalItem foundItem = null;
            if (!ignoreCase)
            {
                foundItem = Lookup(grapheme);
            }
            else
            {
                foundItem = new LexicalItem(this.Language);
                foundItem.Grapheme = grapheme;
                foundItem.AddRange(Lookup(grapheme));

                CultureInfo cultureInfo = CultureInfo.GetCultureInfo(Localor.LanguageToString(this.Language));

                string word = grapheme.ToLower(cultureInfo);
                if (!word.Equals(grapheme, StringComparison.Ordinal))
                {
                    foundItem.AddRange(Lookup(word));
                }

                LexicalItem newItem = Lookup(grapheme.ToUpper(cultureInfo));
                foundItem.AddRange(newItem);

                if (grapheme.Length > 1)
                {
                    string pascalWord = string.Concat(grapheme.Substring(0, 1).ToUpper(cultureInfo),
                        grapheme.Substring(1).ToLower(cultureInfo));
                    newItem = Lookup(pascalWord);
                    foundItem.AddRange(newItem);
                }

                foundItem.MergeDuplicatePronunciation();
                if (foundItem.Pronunciations.Count == 0)
                {
                    foundItem = null;
                }
            }

            return foundItem;
        }

        /// <summary>
        /// Extract a sub lexicon from a word list.
        /// </summary>
        /// <param name="words">Words list to extract.</param>
        /// <returns>New sub lexicon.</returns>
        public Lexicon ExtractSubLexicon(List<string> words)
        {
            return ExtractSubLexicon(words, null);
        }

        /// <summary>
        /// Extract a sub lexicon from a word list and return those words which not in the main lexicon.
        /// </summary>
        /// <param name="words">Words list to extract.</param>
        /// <param name="missedLexWords">Words that not in the main lexicon.</param>
        /// <returns>New sub lexicon.</returns>
        public Lexicon ExtractSubLexicon(List<string> words, List<string> missedLexWords)
        {
            Lexicon newLex = new Lexicon();
            newLex.Language = Language;
            newLex.Encoding = Encoding;
            newLex.PhoneSet = PhoneSet;
            newLex.PosSet = PosSet;
            Dictionary<string, object> missedWords = null;
            if (missedLexWords != null)
            {
                missedLexWords.Clear();
                missedWords = new Dictionary<string, object>(StringComparer.InvariantCulture);
            }

            foreach (string word in words)
            {
                if (string.IsNullOrEmpty(word) || newLex.Items.ContainsKey(word))
                {
                    continue;
                }

                // First do case sensitive lookup; if not found, do case insensitive lookup.
                LexicalItem wordItem = newLex.Lookup(word);
                if (wordItem == null)
                {
                    wordItem = Lookup(word, true);
                }

                if (wordItem != null)
                {
                    newLex.Items.Add(word, wordItem);
                }
                else
                {
                    if (missedWords != null && !missedWords.ContainsKey(word))
                    {
                        missedWords.Add(word, null);
                    }
                }
            }

            if (missedLexWords != null)
            {
                missedLexWords.AddRange(missedWords.Keys);
            }

            return newLex;
        }

        /// <summary>
        /// List all words in the lexicon.
        /// </summary>
        /// <returns>Word list.</returns>
        public IList<string> ListWords()
        {
            IList<string> wordList = new List<string>();
            foreach (LexicalItem lexItem in Items.Values)
            {
                wordList.Add(lexItem.Grapheme);
            }

            return wordList;
        }

        /// <summary>
        /// List all of the phone frequency in this lexicon. No validation of phone is performed.
        /// </summary>
        /// <returns>Sorted phone listed with frequency.</returns>
        public SortedDictionary<string, int> ListPhones()
        {
            ScriptItem scriptItem = Localor.CreateScriptItem(Language);

            SortedDictionary<string, int> phoneFreq = new SortedDictionary<string, int>();

            foreach (LexicalItem lexItem in Items.Values)
            {
                foreach (LexiconPronunciation lexPron in lexItem.Pronunciations)
                {
                    string pron = Pronunciation.CleanDecorate(lexPron.Symbolic);
                    string[] units = scriptItem.PronunciationSeparator.SplitSlices(pron);

                    foreach (string unit in units)
                    {
                        foreach (string phone in TtsMetaUnit.BuildPhoneNames(this.Language, unit))
                        {
                            if (phoneFreq.ContainsKey(phone))
                            {
                                phoneFreq[phone]++;
                            }
                            else
                            {
                                phoneFreq[phone] = 1;
                            }
                        }
                    }
                }
            }

            return phoneFreq;
        }

        /// <summary>
        /// List all of the POS frequency in this lexicon. No validation of POS is performed.
        /// </summary>
        /// <returns>Sorted POS listed with frequency.</returns>
        public SortedDictionary<string, int> ListPoses()
        {
            SortedDictionary<string, int> posFreq = new SortedDictionary<string, int>();
            foreach (LexicalItem lexItem in Items.Values)
            {
                foreach (LexiconPronunciation lexPron in lexItem.Pronunciations)
                {
                    foreach (LexiconItemProperty property in lexPron.Properties)
                    {
                        if (posFreq.ContainsKey(property.PartOfSpeech.Value))
                        {
                            posFreq[property.PartOfSpeech.Value]++;
                        }
                        else
                        {
                            posFreq[property.PartOfSpeech.Value] = 1;
                        }
                    }
                }
            }

            return posFreq;
        }

        /// <summary>
        /// Save the Lexicon into vendor Lexicon for BldVendor2.
        /// Should use Validate(PhoneSet, LexicalAttributeSchema) first.
        /// Will skip the error data.
        /// </summary>
        /// <param name="vendorLexiconFileName">Name of vendor lexicon.</param>
        public void SaveToVendorLexicon(string vendorLexiconFileName)
        {
            if (PhoneSet == null)
            {
                throw new InvalidOperationException("Please set Phone Set first " +
                    "and phone set should not be null");
            }

            if (LexicalAttributeSchema == null)
            {
                throw new InvalidOperationException("Please set Lexical Attribute Schema first " +
                    "and LexicalAttribute Schema should not be null");
            }

            if (GetAllDomainsCountInfo().Count > 1)
            {
                throw new InvalidOperationException("This function should only work for domain lexicon " +
                    "and don't work for combined lexicon.");
            }

            if (!LexicalAttributeSchema.Language.Equals(Language))
            {
                throw new InvalidDataException(Error.BuildMessage(CommonError.NotConsistentLanguage,
                    Language.ToString(), "lexicon",
                    LexicalAttributeSchema.Language.ToString(), "attribute schema"));
            }

            if (!PhoneSet.Language.Equals(Language))
            {
                throw new InvalidDataException(Error.BuildMessage(CommonError.NotConsistentLanguage,
                    Language.ToString(), "lexicon", PhoneSet.Language.ToString(), "phone set"));
            }

            using (TextWriter tw = new StreamWriter(vendorLexiconFileName, false, Encoding.Unicode))
            {
                foreach (string graphme in Items.Keys)
                {
                    LexicalItem lexItem = Items[graphme];
                    if (!lexItem.Valid)
                    {
                        this.ErrorSet.Add(LexiconCompilerError.RemoveInvalidWord, graphme);
                        continue;
                    }

                    Debug.Assert(lexItem.Pronunciations.Count > 0);
                    SavePronunciation(tw, graphme, lexItem);
                }
            }
        }

        /// <summary>
        /// Get lexicon pronunciation for words (separated by spaces).
        /// </summary>
        /// <param name="expansion">Expansion.</param>
        /// <param name="errors">Errors met.</param>
        /// <param name="polyWord">Polyphone words met.</param>
        /// <param name="pron">Pron.</param>
        /// <returns>Whether has error.</returns>
        public bool GetPronunciationForWords(string expansion, Collection<string> errors,
                    Collection<string> polyWord, ref string pron)
        {
            string[] arr = expansion.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            bool hasError = false;
            foreach (string word in arr)
            {
                LexicalItem lookup = Lookup(word, true);
                if (lookup != null)
                {
                    int count = lookup.Pronunciations.Count;
                    if (count >= 1)
                    {
                        if (!string.IsNullOrEmpty(pron))
                        {
                            pron += " - ";
                        }

                        if (count > 1)
                        {
                            if (!polyWord.Contains(word))
                            {
                                errors.Add("Lexicon have more than one pronunciation for expansion word: " + word);
                                polyWord.Add(word);
                            }
                        }

                        pron += lookup.Pronunciations[0].Symbolic;
                    }
                    else
                    {
                        errors.Add("Lexicon doesn't have pronunciation for expansion word: " + word);
                        hasError = true;
                        break;
                    }
                }
                else
                {
                    errors.Add("Lexicon doesn't have expansion word: " + word);
                    hasError = true;
                    break;
                }
            }
         
            return hasError;
        }

        /// <summary>
        /// Save the Lexicon into vendor 1 Lexicon for bldVendor1.
        /// Should use Validate(PhoneSet, LexicalAttributeSchema) first.
        /// Will skip the error data.
        /// </summary>
        /// <param name="vendorLexFileName">Name of vendor lexicon.</param>
        public void SaveToVendor1Lex(string vendorLexFileName)
        {
            if (PhoneSet == null)
            {
                throw new ArgumentException("Please set Phone Set first " +
                    "and phone set should not be null");
            }

            if (PosSet == null)
            {
                throw new ArgumentException("Please set POS set first and POS set should not be null");
            }

            if (GetAllDomainsCountInfo().Count > 1)
            {
                throw new InvalidOperationException("This function should only work for domain lexicon " +
                    "and don't work for combined lexicon.");
            }

            if (!PosSet.Language.Equals(Language))
            {
                throw new InvalidDataException(Error.BuildMessage(CommonError.NotConsistentLanguage,
                    Language.ToString(), "lexicon",
                    PosSet.Language.ToString(), "POS Set"));
            }

            if (!PhoneSet.Language.Equals(Language))
            {
                throw new InvalidDataException(Error.BuildMessage(CommonError.NotConsistentLanguage,
                    Language.ToString(), "lexicon", PhoneSet.Language.ToString(), "phone set"));
            }

            using (TextWriter tw = new StreamWriter(vendorLexFileName, false, Encoding.Unicode))
            {
                foreach (string graphme in Items.Keys)
                {
                    LexicalItem lexItem = Items[graphme];
                    if (!lexItem.Valid)
                    {
                        continue;
                    }

                    Debug.Assert(lexItem.Pronunciations.Count > 0);
                    int pronIndex = 0;
                    foreach (LexiconPronunciation lexPron in lexItem.Pronunciations)
                    {
                        if (!lexPron.Valid)
                        {
                            continue;
                        }

                        ErrorSet phoneConvertErrorSet = new ErrorSet();
                        string hexIds = Pronunciation.ConvertIntoHexIds(lexPron.Symbolic,
                            _ttsPhoneSet, phoneConvertErrorSet);
                        if (string.IsNullOrEmpty(hexIds))
                        {
                            continue;
                        }

                        tw.WriteLine("Word {0}", graphme);
                        tw.WriteLine("Pronunciation{0} {1}", pronIndex++, hexIds);

                        Debug.Assert(lexPron.Symbolic != null);
                        Debug.Assert(lexPron.Properties != null && lexPron.Properties.Count > 0);
                        int propertyIndex = 0;
                        foreach (LexiconItemProperty pr in lexPron.Properties)
                        {
                            if (!pr.Valid)
                            {
                                continue;
                            }

                            int posID = 0;
                            int genderID = 0;
                            int caseID = 0;
                            int numberID = 0;
                            Debug.Assert(pr.PartOfSpeech != null && !string.IsNullOrEmpty(pr.PartOfSpeech.Value));
                            if (pr.PartOfSpeech != null && !string.IsNullOrEmpty(pr.PartOfSpeech.Value))
                            {
                                posID = (int)PosSet.Items[pr.PartOfSpeech.Value];
                            }

                            if (pr.Case != null && !string.IsNullOrEmpty(pr.Case.Value))
                            {
                                ErrorSet.Merge(CaseItem.StringToId(pr.Case.Value, out caseID));
                            }

                            if (pr.Gender != null && !string.IsNullOrEmpty(pr.Gender.Value))
                            {
                                ErrorSet.Merge(GenderItem.StringToId(pr.Gender.Value, out genderID));
                            }

                            if (pr.Number != null && !string.IsNullOrEmpty(pr.Number.Value))
                            {
                                ErrorSet.Merge(NumberItem.StringToId(pr.Number.Value, out numberID));
                            }

                            // Currently we encode gender before POS field
                            // miscValue is of type of 32 bit
                            // low word (16 bit) is used for POS
                            // In high word, gender uses 3 bits
                            // case uses 4 bits and number uses 2 bits
                            int miscValue = posID + (genderID << 16) + (caseID << 19) + (numberID << 23);
                            tw.WriteLine("POS{0} {1}",  propertyIndex++, miscValue);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Import domain lexicon into current lexicon.
        /// </summary>
        /// <param name="domainLex">Domain lexicon.</param>
        /// <param name="trustDomainLexicon">Whether domain lexion is trusting.</param>
        public void ImportDomainLexicon(Lexicon domainLex, bool trustDomainLexicon)
        {
            if (!string.IsNullOrEmpty(_domainTag))
            {
                throw new InvalidDataException(
                    string.Format("Target lexicon is not a unified lexicon, it is in \"{0}\" domain.", _domainTag));
            }

            if (!ValidateDomainLexicon(domainLex))
            {
                throw new InvalidDataException("The lexicon to import is not a domain lexicon.");
            }

            foreach (LexicalItem domainLexItem in domainLex.Items.Values)
            {
                if (_items.ContainsKey(domainLexItem.Grapheme))
                {
                    ErrorSet importError = _items[domainLexItem.Grapheme].ImportDomainLexicalItem(domainLexItem, domainLex.DomainTag, trustDomainLexicon);

                    ErrorSet.Merge(importError);
                }
                else
                {
                    LexicalItem clonedItem = domainLexItem.Clone();
                    clonedItem.Frequency = 0;
                    foreach (LexiconPronunciation pron in clonedItem.Pronunciations)
                    {
                        pron.Frequency = 0;
                    }

                    _items.Add(domainLexItem.Grapheme, clonedItem);
                }
            }
        }

#if COREXTBRANCH

        /// <summary>
        /// Split unified lexicon into domain lexicons.
        /// </summary>
        /// <param name="sp">ServiceProvider.</param>
        /// <param name="errorSet">The errorset.</param>
        /// <returns>Domain Lexicon array.</returns>
        [CLSCompliant(false)]
        public Lexicon[] SplitIntoDomainLexicons(SP.ServiceProvider sp, ErrorSet errorSet)
        {
            // Dictionary key="domain tag string", value="Lexicon instance"
            Dictionary<string, Lexicon> domainLexicons = new Dictionary<string, Lexicon>();
            Lexicon generalLexicon = new Lexicon(this.Language);
            generalLexicon.Encoding = Encoding;
            generalLexicon.DomainTag = DomainItem.GeneralDomain;
            domainLexicons.Add(generalLexicon.DomainTag, generalLexicon);

            foreach (KeyValuePair<string, LexicalItem> pair in this.Items)
            {
                Dictionary<string, LexicalItem> domainLexItems = pair.Value.SplitToDomainLexicalItems();
                bool same = ArePronsSameForAllDomains(domainLexItems);
                bool added = false;

                bool isExpandedWords = false;

                // check if has pronunciation is expaned.
                // for chinese/japanese/korean have no expanded words, they needn't to check if is expanded word.
                if (Language != Language.ZhCN &&
                    Language != Language.ZhHK &&
                    Language != Language.ZhTW &&
                    Language != Language.JaJP &&
                    Language != Language.KoKR)
                {
                    foreach (LexiconPronunciation pron in pair.Value.Pronunciations)
                    {
                        if (LexicalItem.IsExpandedWord(pair.Value.Grapheme, pair.Value.Language, pron, sp))
                        {
                            isExpandedWords = true;
                            break;
                        }
                    }
                }

                // for zh-XX, if there is any english word, the word must not be regularly.
                if ((Language == Language.ZhCN || Language == Language.ZhTW || Language == Language.ZhHK) 
                    && Helper.IsEnglishWord(pair.Value.Grapheme))
                {
                    isExpandedWords = true;
                }

                // word is not expaneded
                if (!isExpandedWords)
                {
                    if (same || domainLexItems.Count == 1)
                    {
                        LexicalItem newLexItem = pair.Value.Clone();
                        newLexItem.CleanAllDomainTags();
                        FillDomainLexicalItem(domainLexicons, DomainItem.GeneralDomain, newLexItem);
                        added = true;
                    }
                    else
                    {
                        CheckGeneralPronExist(domainLexItems, errorSet, pair.Value.Grapheme);
                    }
                }

                if (!added)
                {
                    foreach (KeyValuePair<string, LexicalItem> lexItemPair in domainLexItems)
                    {
                        FillDomainLexicalItem(domainLexicons, lexItemPair.Key, lexItemPair.Value);
                    }
                }
            }

            return domainLexicons.Values.ToArray();
        }

        /// <summary>
        /// Convert to new attribute format lexicon.
        /// </summary>
        public void ToNewAttributeFormatLexicon()
        {
            foreach (LexicalItem item in _items.Values)
            {
                item.ToNewAttributeFormatLexicalItem(ErrorSet);
            }
        }
#endif
        #endregion

        #region XmlDataFile public members

        /// <summary>
        /// Validate function.
        /// </summary>
        public override void Validate()
        {
            if (PhoneSet == null)
            {
                throw new InvalidOperationException("Please set Phone Set first and phone set should not be null");
            }

            if (PosSet == null && LexicalAttributeSchema == null)
            {
                throw new InvalidOperationException("Please set Lexical Attribute Schema (or POS Set) first " +
                    "and LexicalAttribute Schema (or POS set) should not be null");
            }

            if (LexicalAttributeSchema != null)
            {
                Validate(PhoneSet, LexicalAttributeSchema);
            }
            else
            {
                Validate(PhoneSet, PosSet);
            }
        }

        #endregion

        #region ILexicon Members

        /// <summary>
        /// Lookup lexicon item for grapheme.
        /// </summary>
        /// <param name="grapheme">Grapheme of the word.</param>
        /// <returns>Lexicon item found.</returns>
        public LexicalItem Lookup(string grapheme)
        {
            if (string.IsNullOrEmpty(grapheme))
            {
                throw new ArgumentNullException("grapheme");
            }

            LexicalItem item = null;
            if (_items != null && _items.ContainsKey(grapheme))
            {
                item = _items[grapheme];
            }

            return item;
        }

        #endregion

        #region XmlDataFile protected Members

        /// <summary>
        /// Load XML file.
        /// </summary>
        /// <param name="xmlDoc">XmlDoc.</param>
        /// <param name="nsmgr">Nsmgr.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void Load(XmlDocument xmlDoc, XmlNamespaceManager nsmgr, object contentController)
        {
            ContentControler lexiconContentController = contentController as ContentControler;
            Debug.Assert(contentController == null || lexiconContentController != null);
            if (lexiconContentController == null)
            {
                lexiconContentController = new ContentControler();
            }

            Language language = Localor.StringToLanguage(xmlDoc.DocumentElement.Attributes["lang"].InnerText);
            if (!Language.Equals(Language.Neutral) && !language.Equals(Language))
            {
                ErrorSet.Add(CommonError.NotConsistentLanguage,
                    Language.ToString(), "initial one", language.ToString(), "lexicon");
            }

            Language = language;
            if (xmlDoc.DocumentElement.Attributes["domain"] != null)
            {
                string domainTag = xmlDoc.DocumentElement.Attributes["domain"].InnerText;
                if (!string.IsNullOrEmpty(domainTag))
                {
                    DomainTag = domainTag;
                }
            }

            // Load current lexicon
            _items.Clear();
            XmlNodeList wordNodes = xmlDoc.DocumentElement.SelectNodes("tts:w", nsmgr);
            foreach (XmlNode wordNode in wordNodes)
            {
                LoadLexicalItem(this, wordNode, nsmgr, lexiconContentController);
            }

            // Get baseline lexicon file path
            string baseLexiconFilePath = string.Empty;
            if (xmlDoc.DocumentElement.FirstChild != null &&
                xmlDoc.DocumentElement.FirstChild.LocalName == "include" &&
                xmlDoc.DocumentElement.FirstChild.Attributes["href"] != null)
            {
                BaseLexiconRelativeFilePath = xmlDoc.DocumentElement.FirstChild.Attributes["href"].InnerText;
                if (!string.IsNullOrEmpty(BaseLexiconRelativeFilePath))
                {
                    baseLexiconFilePath = Helper.GetFullPath(Path.GetDirectoryName(this.FilePath), BaseLexiconRelativeFilePath);
                }
            }

            if (!string.IsNullOrEmpty(baseLexiconFilePath) && File.Exists(baseLexiconFilePath))
            {
                Lexicon baseLexicon = new Lexicon();
                baseLexicon._isBaseline = true;

                // Load baseline lexicon
                baseLexicon.Load(baseLexiconFilePath, lexiconContentController);

                // Merge current lexicon and baseline lexicon
                foreach (var baseItem in baseLexicon.Items)
                {
                    // We drop those items if they have "deleted" status when LoadLexicalItem(),
                    // so there's no deleted words in both lexicons.

                    // if this item isn't in current lexicon, add it into current lexicon
                    if (!_items.ContainsKey(baseItem.Key))
                    {
                        _items.Add(baseItem.Key, baseItem.Value);
                    }
                    //// if this item is already in current lexicon, keep current word item
                    /*else
                    {

                    } */
                }
            }
        }

        /// <summary>
        /// Save lexicon into Xml writer.
        /// </summary>
        /// <param name="writer">Writer file to save into.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void PerformanceSave(XmlWriter writer, object contentController)
        {
            ContentControler lexiconContentController = contentController as ContentControler;
            if (lexiconContentController == null)
            {
                lexiconContentController = new ContentControler();
            }

            writer.WriteStartElement("lexiconWords", "http://schemas.microsoft.com/tts");
            writer.WriteAttributeString("lang", Localor.LanguageToString(Language));

            if (!string.IsNullOrEmpty(DomainTag))
            {
                writer.WriteAttributeString("domain", DomainTag);
            }

            if (!string.IsNullOrEmpty(BaseLexiconRelativeFilePath))
            {
                writer.WriteStartElement("include");
                writer.WriteAttributeString("href", BaseLexiconRelativeFilePath);
                writer.WriteEndElement();
            }

            IEnumerable<LexicalItem> lexiconItems = _items.Values;

            if (lexiconContentController.DontSaveBaselineLexicon)
            {
                lexiconItems = _items.Values.Where(p => p.Origin == LexiconOrigin.Current);
            }

            // Go through each lexicon item in the lexicon
            foreach (LexicalItem lexiconItem in lexiconItems)
            {
                if (!string.IsNullOrEmpty(DomainTag))
                {
                    lexiconItem.CleanAllDomainTags();
                }

                lexiconItem.WriteToXml(writer);
            }

            writer.WriteEndElement();
        }

        #endregion

        #region Private static methods

        /// <summary>
        /// Validate domain lexicon, and check whether it only contains one domain tag.
        /// </summary>
        /// <param name="domainLex">Domain Lexicon.</param>
        /// <returns>Whether valid.</returns>
        private static bool ValidateDomainLexicon(Lexicon domainLex)
        {
            Helper.ThrowIfNull(domainLex);

            bool valid = true;
            if (string.IsNullOrEmpty(domainLex.DomainTag))
            {
                valid = false;
            }
            else
            {
                foreach (LexicalItem domainLexItem in domainLex.Items.Values)
                {
                    if (!domainLexItem.OnlyContainsOneDomain(domainLex.DomainTag))
                    {
                        valid = false;
                        break;
                    }
                }
            }

            return valid;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Load LexicalItem from XmlNode.
        /// </summary>
        /// <param name="parentLexicon">Lexicon.</param>
        /// <param name="wordNode">XmlNode.</param>
        /// <param name="nsmgr">XmlNamespaceManager.</param>
        /// <param name="contentController">Object.</param>
        private void LoadLexicalItem(Lexicon parentLexicon, XmlNode wordNode, XmlNamespaceManager nsmgr, Lexicon.ContentControler contentController)
        {
            LexicalItem lexiconItem = LexicalItem.Load(parentLexicon, wordNode, nsmgr, contentController, ErrorSet);

            // If no pronunciation at last, we drop the word item.
            if (lexiconItem != null && lexiconItem.Pronunciations.Count > 0)
            {
                if (_items.ContainsKey(lexiconItem.Grapheme))
                {
                    ErrorSet.Add(LexiconError.DuplicateWordEntry, lexiconItem.Grapheme);
                    foreach (LexiconPronunciation pronunciation in lexiconItem.Pronunciations)
                    {
                        pronunciation.Parent = _items[lexiconItem.Grapheme];
                        _items[lexiconItem.Grapheme].Pronunciations.Add(pronunciation);
                    }
                }
                else
                {
                    if (parentLexicon._isBaseline)
                    {
                        lexiconItem.Origin = LexiconOrigin.Baseline;
                    }
                    else
                    {
                        lexiconItem.Origin = LexiconOrigin.Current;
                    }
                    
                    _items.Add(lexiconItem.Grapheme, lexiconItem);
                }
            }
        }

        /// <summary>
        /// Validate Lexicon according to TTS phone set and pos set or lexical attribute schema.
        /// </summary>
        /// <param name="ttsPhoneSet">TTS phone set.</param>
        /// <param name="ttsPosSet">TTS POS set.</param>
        /// <param name="attributeSchema">Lexical attribute Schema.</param>
        private void Validate(TtsPhoneSet ttsPhoneSet, TtsPosSet ttsPosSet,
            LexicalAttributeSchema attributeSchema)
        {
            Debug.Assert(ttsPhoneSet != null);
            Debug.Assert(ttsPosSet != null || attributeSchema != null);
            bool dependentDataValid = true;
            ttsPhoneSet.Validate();
            if (ttsPhoneSet.ErrorSet.Contains(ErrorSeverity.MustFix))
            {
                ErrorSet.Add(LexiconError.InvalidDependentData, "Phone set");
                dependentDataValid = false;
            }

            if (ttsPosSet != null)
            {
                ttsPosSet.Validate();
                if (ttsPosSet.ErrorSet.Contains(ErrorSeverity.MustFix))
                {
                    ErrorSet.Add(LexiconError.InvalidDependentData, "POS set");
                    dependentDataValid = false;
                }
            }

            if (attributeSchema != null)
            {
                attributeSchema.Validate();
                if (attributeSchema.ErrorSet.Contains(ErrorSeverity.MustFix))
                {
                    ErrorSet.Add(LexiconError.InvalidDependentData, "Lexical Attribute Schema");
                    dependentDataValid = false;
                }
            }

            if (dependentDataValid)
            {
                bool containValidItem = false;
                foreach (LexicalItem lexItem in Items.Values)
                {
                    ErrorSet errorSet = lexItem.Validate(ttsPhoneSet, ttsPosSet, attributeSchema);
                    ErrorSet.Merge(errorSet);
                    containValidItem = containValidItem || lexItem.Valid;
                }

                if (!containValidItem)
                {
                    ErrorSet.Add(LexiconError.EmptyLexicon);
                }
            }

            validated = true;
        }

        /// <summary>
        /// Save pronunciations.
        /// </summary>
        /// <param name="tw">Text writer.</param>
        /// <param name="graphme">Word graphme.</param>
        /// <param name="lexItem">Lexicon items.</param>
        private void SavePronunciation(TextWriter tw, string graphme, LexicalItem lexItem)
        {
            foreach (LexiconPronunciation lexPron in lexItem.Pronunciations)
            {
                if (!lexPron.Valid)
                {
                    this.ErrorSet.Add(LexiconCompilerError.RemoveInvalidPronunciation, graphme, lexPron.Symbolic);
                    continue;
                }

                ErrorSet phoneConvertErrorSet = new ErrorSet();
                string hexIds = Pronunciation.ConvertIntoHexIds(lexPron.Symbolic,
                    _ttsPhoneSet, phoneConvertErrorSet);
                if (string.IsNullOrEmpty(hexIds))
                {
                    continue;
                }

                string firstHalf = Helper.NeutralFormat("{0}\t{1}\t{2}",
                    graphme, hexIds, "sppos=noncontent");
                Collection<string> attributeStringList = new Collection<string>();

                Debug.Assert(lexPron.Symbolic != null);
                Debug.Assert(lexPron.Properties != null && lexPron.Properties.Count > 0);
                SaveProperty(graphme, lexPron, attributeStringList);

                foreach (string attributeString in attributeStringList)
                {
                    tw.WriteLine(firstHalf + attributeString);
                }
            }
        }

        /// <summary>
        /// Save lexicon properties.
        /// </summary>
        /// <param name="graphme">Word graphme.</param>
        /// <param name="lexPron">Lexicon pronunciation.</param>
        /// <param name="attributeStringList">Attribute string list.</param>
        private void SaveProperty(string graphme, LexiconPronunciation lexPron, Collection<string> attributeStringList)
        {
            foreach (LexiconItemProperty pr in lexPron.Properties)
            {
                if (!pr.Valid)
                {
                    this.ErrorSet.Add(LexiconCompilerError.RemoveInvalidProperty,
                        graphme, lexPron.Symbolic, pr.PartOfSpeech.Value);
                    continue;
                }

                List<ArrayList> attributes = new List<ArrayList>();

                Debug.Assert(pr.PartOfSpeech != null && !string.IsNullOrEmpty(pr.PartOfSpeech.Value));
                if (pr.PartOfSpeech != null && !string.IsNullOrEmpty(pr.PartOfSpeech.Value))
                {
                    ArrayList attrbuteList = new ArrayList();

                    attrbuteList.Add(
                        _attributeSchema.GenerateString("POS", pr.PartOfSpeech.Value));

                    attributes.Add(attrbuteList);
                }

                if (pr.Case != null && !string.IsNullOrEmpty(pr.Case.Value))
                {
                    ArrayList attrbuteList = new ArrayList();

                    ArrayList valueList = CaseItem.ConvertIntoArray(pr.Case.Value, ErrorSet);

                    for (int i = 0; i < valueList.Count; i++)
                    {
                        attrbuteList.Add(
                            _attributeSchema.GenerateString("F_CASE", valueList[i].ToString()));
                    }

                    attributes.Add(attrbuteList);
                }

                if (pr.Gender != null && !string.IsNullOrEmpty(pr.Gender.Value))
                {
                    ArrayList attrbuteList = new ArrayList();

                    ArrayList valueList = GenderItem.ConvertIntoArray(pr.Gender.Value, ErrorSet);

                    for (int i = 0; i < valueList.Count; i++)
                    {
                        attrbuteList.Add(
                            _attributeSchema.GenerateString("F_GENDER", valueList[i].ToString()));
                    }

                    attributes.Add(attrbuteList);
                }

                // Write out number information if present
                if (pr.Number != null && !string.IsNullOrEmpty(pr.Number.Value))
                {
                    ArrayList attrbuteList = new ArrayList();

                    ArrayList valueList = NumberItem.ConvertIntoArray(pr.Number.Value, ErrorSet);

                    for (int i = 0; i < valueList.Count; i++)
                    {
                        attrbuteList.Add(
                            _attributeSchema.GenerateString("F_NUMBER", valueList[i].ToString()));
                    }

                    attributes.Add(attrbuteList);
                }

                foreach (KeyValuePair<string, List<AttributeItem>> pair in pr.AttributeSet)
                {
                    ArrayList attrbuteList = new ArrayList();

                    foreach (AttributeItem attr in pair.Value)
                    {
                        string attribute = LexicalAttributeSchema.GenerateString(pair.Key, attr.Value);

                        if (!string.IsNullOrEmpty(attribute))
                        {
                            attrbuteList.Add(attribute);
                        }
                    }

                    attributes.Add(attrbuteList);
                }

                // fill a terminal null in end of the list
                attributes.Add(null);

                BuildAttributeStringList(attributeStringList, string.Empty, attributes.ToArray(), 0);
            }
        }

        /// <summary>
        /// Build the attribute string list.
        /// </summary>
        /// <param name="attributeStringList">Attribute strings.</param>
        /// <param name="previousString">Previous string.</param>
        /// <param name="attributes">Attributes.</param>
        /// <param name="index">Index of the next attribute.</param>
        private void BuildAttributeStringList(Collection<string> attributeStringList, 
            string previousString, ArrayList[] attributes, int index)
        {
            const string ConcatenatedString = "\t";
            if (attributes[index] == null)
            {
                if (!attributeStringList.Contains(previousString))
                {
                    attributeStringList.Add(previousString);
                }
            }
            else
            {
                foreach (object obj in attributes[index])
                {
                    BuildAttributeStringList(attributeStringList,
                        previousString + ConcatenatedString + (string)obj, attributes, index + 1);
                }
            }
        }

        /// <summary>
        /// Add domain specified LexicalItem into dictionary.
        /// </summary>
        /// <param name="domainLexicons">Dictionary.</param>
        /// <param name="domainTag">Domain tag.</param>
        /// <param name="lexItem">LexicalItem.</param>
        private void FillDomainLexicalItem(Dictionary<string, Lexicon> domainLexicons, string domainTag, LexicalItem lexItem)
        {
            Helper.ThrowIfNull(domainLexicons);
            Helper.ThrowIfNull(domainTag);
            Helper.ThrowIfNull(lexItem);

            if (domainLexicons.ContainsKey(domainTag))
            {
                if (!domainLexicons[domainTag].Items.ContainsKey(lexItem.Grapheme))
                {
                    domainLexicons[domainTag].Items.Add(lexItem.Grapheme, lexItem);
                }
                else
                {
                    throw new InvalidDataException(
                        string.Format("Duplicate lexicon word \"{0}\" in \"{1}\" domain.", lexItem.Grapheme, domainTag));
                }
            }
            else
            {
                Lexicon newLexicon = new Lexicon(Language);
                newLexicon.Encoding = Encoding;
                newLexicon.DomainTag = domainTag;
                newLexicon.Items.Add(lexItem.Grapheme, lexItem);
                domainLexicons.Add(domainTag, newLexicon);
            }
        }

        /// <summary>
        /// Check pronunciation in different LexicalItem has same pronunciation text.
        /// </summary>
        /// <param name="domainLexItems">Domain LexicalItem dictionary.</param>
        /// <returns>Whether same.</returns>
        private bool ArePronsSameForAllDomains(Dictionary<string, LexicalItem> domainLexItems)
        {
            if (domainLexItems == null)
            {
                throw new ArgumentNullException();
            }

            bool same = true;
            string previousPron = null;

            foreach (LexicalItem lexItem in domainLexItems.Values)
            {
                // To compare pronunciation without duplicate, using dictionary to save pronunciation.
                // If word with same pronunciation in all domain but different pronunciation order, word is still spilted to differnt lexicon.
                Dictionary<string, int> currentPronDictionary = new Dictionary<string, int>();

                foreach (LexiconPronunciation pron in lexItem.Pronunciations)
                {
                    // add to dictionary
                    if (!currentPronDictionary.ContainsKey(pron.Symbolic))
                    {
                        currentPronDictionary.Add(pron.Symbolic);
                    }
                }

                // Concate pronunciation text with delimter "/"
                string currentPron = currentPronDictionary.Keys.Concatenate("/");

                if (previousPron == null)
                {
                    previousPron = currentPron.ToString();
                }
                else if (previousPron != currentPron.ToString())
                {
                    same = false;
                    break;
                }
            }

            return same;
        }

        /// <summary>
        /// Get dictionary about all domains information
        /// Key is domain name and value is domain count.
        /// </summary>
        /// <returns>Dictionary about domain information.</returns>
        private Dictionary<string, int> GetAllDomainsCountInfo()
        {
            Dictionary<string, int> domains = new Dictionary<string, int>();
            foreach (LexicalItem lexItem in _items.Values)
            {
                foreach (LexiconPronunciation pron in lexItem.Pronunciations)
                {
                    foreach (LexiconItemProperty property in pron.Properties)
                    {
                        foreach (DomainItem domain in property.Domains.Values)
                        {
                            if (domains.ContainsKey(domain.Value))
                            {
                                domains[domain.Value]++;
                            }
                            else
                            {
                                domains.Add(domain.Value, 1);
                            }
                        }
                    }
                }
            }

            return domains;
        }

        /// <summary>
        /// Check if the pronunciation for general domain exist.
        /// We check general domain pronunciation to avoid the case that word have address domain pronunciation, but got LTSed in general domain.
        /// </summary>
        /// <param name="domainLexItems">Domain lexiconItems.</param>
        /// <param name="errorSet">The errorSet.</param>
        /// <param name="word">The current word.</param>
        private void CheckGeneralPronExist(Dictionary<string, LexicalItem> domainLexItems,
            ErrorSet errorSet, string word)
        {
            Helper.ThrowIfNull(domainLexItems);
            Helper.ThrowIfNull(errorSet);

            if (!domainLexItems.ContainsKey(DomainItem.GeneralDomain))
            {
                errorSet.Add(LexiconError.LackGeneralDomainPronError, word);
            }
        }

        #endregion

        /// <summary>
        /// Lexicon content controler.
        /// </summary>
        public class ContentControler
        {
            /// <summary>
            /// Gets or sets a value indicating whether case sensitive.
            /// </summary>
            public bool IsCaseSensitive { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether Speicfy that all lexicon change history need be kept.
            /// </summary>
            public bool IsHistoryCheckingMode { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether save baseline lexicon items as well.
            /// True : don't save baseline origin lexicon items.
            /// False: save baseline origin lexicon items.
            /// </summary>
            public bool DontSaveBaselineLexicon { get; set; }
        }
    }
}