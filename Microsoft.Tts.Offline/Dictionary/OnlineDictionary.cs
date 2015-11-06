//----------------------------------------------------------------------------
// <copyright file="OnlineDictionary.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     OnlineDictionary class. Interface of library.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Dictionary
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Enum of online dictionaries.
    /// </summary>
    public enum DictionaryName
    {
        /// <summary>
        /// Http://dictionary.reference.com/.
        /// </summary>
        [LanguageAttribute(Language.EnUS)]
        DictionaryReference,

        /// <summary>
        /// Http://www.merriam-webster.com/.
        /// </summary>
        [LanguageAttribute(Language.EnUS)]
        MerriamWebster,

        /// <summary>
        /// All dictionaries.
        /// </summary>
        [LanguageAttribute(Language.Neutral)]
        All
    }

    /// <summary>
    /// Language Name Attribute class.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class LanguageAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LanguageAttribute"/> class.
        /// </summary>
        /// <param name="lang">Lanuage name.</param>
        public LanguageAttribute(Language lang)
        {
            Language = lang;
        }

        /// <summary>
        /// Gets Language name.
        /// </summary>
        public Language Language
        {
            get;
            internal set;
        }
    }

    /// <summary>
    /// OnlineDictionary class. Interface of library.
    /// </summary>
    public class OnlineDictionary
    {
        #region Fields

        /// <summary>
        /// Language .
        /// </summary>
        private Language _language = Language.Neutral;

        /// <summary>
        /// Word field .
        /// </summary>
        private string _word = string.Empty;

        /// <summary>
        /// Dictionary instances .
        /// </summary>
        private Collection<DictionaryModel> _dics = new Collection<DictionaryModel>();

        /// <summary>
        /// Lexicon Item .
        /// </summary>
        private LexicalItem _item;

        /// <summary>
        /// Indicating whether it is a morphology .
        /// </summary>
        private bool _isMorphology = false;

        /// <summary>
        /// Store each dictionary result .
        /// </summary>
        private Collection<DicItem> _dicItems = new Collection<DicItem>();

        /// <summary>
        /// Online dictionary type array .
        /// </summary>
        private Type[] _dictionaryTypes = new Type[]
        {
            typeof(EnUS.DictionaryReference),
            typeof(EnUS.MerriamWebster)
        };

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the OnlineDictionary class .
        /// </summary>
        public OnlineDictionary()
        {
            _language = Language.EnUS;
            Initialize(_language);
        }

        /// <summary>
        /// Initializes a new instance of the OnlineDictionary class .
        /// </summary>
        /// <param name="language">Language .</param>
        public OnlineDictionary(Language language)
        {
            _language = language;
            Initialize(_language);
        }

        /// <summary>
        /// Initializes a new instance of the OnlineDictionary class with specified Dictionary.
        /// </summary>
        /// <param name="language">Language name.</param>
        /// <param name="dicName">Dictionary name.</param>
        public OnlineDictionary(Language language, DictionaryName dicName)
        {
            _language = language;
            Initialize(_language, dicName);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets lanuage .
        /// </summary>
        public Language Language
        {
            get
            {
                return _language;
            }
        }

        /// <summary>
        /// Gets word .
        /// </summary>
        public string Word
        {
            get
            {
                return _word;
            }
        }

        /// <summary>
        /// Gets lexicalItem with pronunciaton and POS .
        /// </summary>
        public LexicalItem Item
        {
            get
            {
                return _item;
            }
        }

        /// <summary>
        /// Gets a value indicating whether current word is a morphology word.
        /// </summary>
        public bool IsMorphology
        {
            get
            {
                return _isMorphology;
            }
        }

        /// <summary>
        /// Gets items from all dictionaries.
        /// </summary>
        public Collection<DicItem> DicItems
        {
            get
            {
                return _dicItems;
            }
        }

        #endregion

        #region Public Method

        /// <summary>
        /// Look up word from online dictionary .
        /// </summary>
        /// <param name="word">Word parameter .</param>
        /// <param name="isOOV">If the word is OOV.</param>
        /// <returns>Return a LexicalItem for each word .</returns>
        public LexicalItem Lookup(string word, ref bool isOOV)
        {
            _item = new LexicalItem(_language);
            _word = word.ToLower();
            _item.Grapheme = _word;
            bool flag = false;
            Dictionary<string, Collection<string>> prons = new Dictionary<string, Collection<string>>();
            isOOV = false;
            foreach (DictionaryModel dic in _dics)
            {
                Collection<string> pronunciations = dic.Lookup(word);
                isOOV = dic.IsOOV;
                if (!dic.IsOOV)
                {
                    if (string.IsNullOrEmpty(dic.Word))
                    {
                        throw new InvalidDataException(Helper.NeutralFormat(
                            "Error happened when processing word \"{0}\": The word parsed from web is empty", word));
                    }
                    else if (dic.Pronunciations.Count == 0)
                    {
                        throw new InvalidDataException(Helper.NeutralFormat(
                            "Error happened when processing word \"{0}\": The pronunciation parsed from web is empty", word));
                    }
                    else
                    {
                        DicItem item = new DicItem();
                        item.Word = dic.Word;
                        item.POS = dic.POS;
                        item.Pronunciations = dic.Pronunciations;
                        _dicItems.Add(item);
                        if (word.Equals(dic.Word, StringComparison.InvariantCultureIgnoreCase))
                        {
                            foreach (string pron in dic.Pronunciations)
                            {
                                if (!prons.Keys.Contains(pron))
                                {
                                    Collection<string> pos = new Collection<string>();
                                    pos.Add(dic.POS);
                                    prons.Add(pron, pos);
                                }
                                else
                                {
                                    if (!prons[pron].Contains(dic.POS))
                                    {
                                        prons[pron].Add(dic.POS);
                                    }
                                }
                            }
                        }
                        else
                        {
                            flag = true;
                        }
                    }
                }

                dic.Reset();
            }

            GenerateLexicalItem(prons);
            _isMorphology = flag;
            return Item;
        }

        #endregion

        #region Private Method

        /// <summary>
        /// Generate a LexicalItem from a dictionary.
        /// </summary>
        /// <param name="prons">Pronunciation dictionary.</param>
        private void GenerateLexicalItem(Dictionary<string, Collection<string>> prons)
        {
            Helper.ThrowIfNull(prons);

            foreach (string key in prons.Keys)
            {
                LexiconPronunciation pron = new LexiconPronunciation(_language);
                pron.Symbolic = key;
                foreach (string pos in prons[key])
                {
                    PosItem posItem = new PosItem(pos);
                    LexiconItemProperty property = new LexiconItemProperty(posItem);
                    pron.Properties.Add(property);
                }

                _item.Pronunciations.Add(pron);
            }
        }

        /// <summary>
        /// Initialize method .
        /// </summary>
        /// <param name="language">Language .</param>
        private void Initialize(Language language)
        {
            foreach (Type type in _dictionaryTypes)
            {
                System.Reflection.FieldInfo fieldInfo = type.GetField("Language");
                if ((Language)fieldInfo.GetValue(null) == language)
                {
                    DictionaryModel dic = (DictionaryModel)Activator.CreateInstance(type);
                    _dics.Add(dic);
                }
            }
        }

        /// <summary>
        /// Initialize method with specified dictionary.
        /// </summary>
        /// <param name="language">Language name.</param>
        /// <param name="dicName">Dictionary name.</param>
        private void Initialize(Language language, DictionaryName dicName)
        {
            Language lang = Language.Neutral;
            if (dicName != DictionaryName.All)
            {
                lang = GetDictionaryAttribute(dicName);
                if (lang != language)
                {
                    string message = Helper.NeutralFormat("{0} is not {1} dictionary.", dicName, language);
                    throw new ArgumentException(message);
                }
            }

            foreach (Type type in _dictionaryTypes)
            {
                System.Reflection.FieldInfo fieldInfo = type.GetField("Language");
                if ((Language)fieldInfo.GetValue(null) == language)
                {
                    DictionaryModel dic = (DictionaryModel)Activator.CreateInstance(type);
                    if (lang == Language.Neutral)
                    {
                        _dics.Add(dic);
                    }
                    else
                    {
                        if (dic.GetType().Name.Equals(dicName.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            _dics.Add(dic);
                        }
                    }
                }
            }

            if (_dics.Count == 0)
            {
                string message = Helper.NeutralFormat("Not supported dictionary {0} for {1}.", dicName, language);
                throw new NotSupportedException(message);
            }
        }

        /// <summary>
        /// Get dictionary name's language attribute.
        /// </summary>
        /// <param name="dicName">Dictionary name.</param>
        /// <returns>Language attribute value.</returns>
        private Language GetDictionaryAttribute(DictionaryName dicName)
        {
            System.Reflection.FieldInfo field = typeof(DictionaryName).GetField(dicName.ToString());
            object[] attributes = field.GetCustomAttributes(typeof(LanguageAttribute), true);
            return ((LanguageAttribute)attributes[0]).Language;
        }

        #endregion

        /// <summary>
        /// Result item from each online dictionary.
        /// </summary>
        public class DicItem
        {
            /// <summary>
            /// Gets or sets word .
            /// </summary>
            public string Word
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets POS .
            /// </summary>
            public string POS
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets pronunciations .
            /// </summary>
            public Collection<string> Pronunciations
            {
                get;
                set;
            }
        }
    }
}