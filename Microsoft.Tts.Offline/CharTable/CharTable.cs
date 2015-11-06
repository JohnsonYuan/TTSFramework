//----------------------------------------------------------------------------
// <copyright file="CharTable.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      char table object model
// </summary>
//--

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Compiler;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Char table error.
    /// </summary>
    public enum CharTableError
    {
        /// <summary>
        /// Empty symbol.
        /// </summary>
        [ErrorAttribute(Message = "Symbol cannot be empty",
            Severity = ErrorSeverity.MustFix)]
        EmptySymbol,

        /// <summary>
        /// Duplicate symbol.
        /// </summary>
        [ErrorAttribute(Message = "Duplicated symbols: [{0}]",
            Severity = ErrorSeverity.MustFix)]
        DuplicateSymbol,

        /// <summary>
        /// Uppercase and lowercase count doesn't match.
        /// </summary>
        /// <remarks>In languages such as deDE, frFR, jaJP, itIT, ruRU, uppercase and lowercase character counts are not same.</remarks>
        [ErrorAttribute(Message =
            "Uppercase [{0}] and lowercase [{1}] count doesn't match.",
            Severity = ErrorSeverity.Warning)]
        MismatchUpperAndLower,

        /// <summary>
        /// Digit count is not 10.
        /// </summary>
        /// <remarks>In some languages (daDK, zhHK, zhTW), digit count is more than 10 because we also support full-width chars.</remarks>
        [ErrorAttribute(Message = "Digit count is not 10.",
            Severity = ErrorSeverity.Warning)]
        ErrorDigitCount,

        /// <summary>
        /// Isolated symbol readout cannot be empty.
        /// </summary>
        [ErrorAttribute(Message =
            "Isolated symbol readout cannot be empty for no-alphabet char [{0}]",
            Severity = ErrorSeverity.MustFix)]
        EmptyIsolatedSymbol,

        /// <summary>
        /// No-alphabet should not have features.
        /// </summary>
        [ErrorAttribute(Message =
            "No-alphabet symbol [{0}] should not have features.",
            Severity = ErrorSeverity.MustFix)]
        NonAlphabetShouldNoFeatures,

        /// <summary>
        /// No-alphabet should not have pronunciation.
        /// </summary>
        [ErrorAttribute(Message =
            "No-alphabet symbol [{0}] should not have pronunciation.",
            Severity = ErrorSeverity.Warning)]
        NonAlphabetShouldNoPronunciation,

        /// <summary>
        /// Alphabet symbol must have pronunciation.
        /// </summary>
        [ErrorAttribute(Message =
            "Alphabet symbol [{0}] must have pronunciation.",
            Severity = ErrorSeverity.MustFix)]
        AlphabetNoPron,

        /// <summary>
        /// Invalid symbol pronunciation.
        /// </summary>
        [ErrorAttribute(Message =
            "Invalid symbol [{0}] pronunciation [{1}].",
            Severity = ErrorSeverity.MustFix)]
        AlphabetInvalidPron,

        /// <summary>
        /// Alphabet symbol must have feature.
        /// </summary>
        [ErrorAttribute(Message =
            "Alphabet symbol [{0}] must have feature.",
            Severity = ErrorSeverity.MustFix)]
        AlphabetNoFeatures,

        /// <summary>
        /// Alphabet symbol shoud not have expansion.
        /// </summary>
        [ErrorAttribute(Message =
            "Alphabet symbol [{0}] shoud not have expansion.",
            Severity = ErrorSeverity.MustFix)]
        AlphabetShouldNoExpansion,

        /// <summary>
        /// Expansion word not in lexicon.
        /// </summary>
        [ErrorAttribute(Message =
            "Expansion word [{0}] not in lexicon.",
            Severity = ErrorSeverity.MustFix)]
        ExpansionWordNotInLexicon,

        /// <summary>
        /// Symbol [{0}] in chartable not in lexicon.
        /// </summary>
        [ErrorAttribute(Message =
            "Symbol [{0}] in chartable not in lexicon.",
            Severity = ErrorSeverity.NoError)]
        SymbolNotInLexicon,

        /// <summary>
        /// Symbol [{0}] in chartable is not single char, only first char take effect.
        /// </summary>
        [ErrorAttribute(Message =
            "Symbol [{0}] in chartable is not single char, only first char take effect.",
            Severity = ErrorSeverity.MustFix)]
        SymbolNotSingleChar
    }

    /// <summary>
    /// Store char element from chartable.xml.
    /// </summary>
    public class CharElement
    {
        // Unicode range: U+0000...U+DFFF, U+E000...U+10FFFF
        // char use UTF-16 encoding.
        // Unicode value within 16-bit keep same after encoding.
        // Unicode value longer than 16-bit will be presented as 2 char (surrogate pair) after encoding.
        // surrogate pair: High D800...DBFF (str[0])
        //                 low  DC00...DFFF (str[1])
        private const char SURROGATE_HIGH_MIN = (char)0xD800;
        private const char SURROGATE_HIGH_MAX = (char)0xDBFF;
        private const char SURROGATE_LOW_MIN = (char)0xDC00;
        private const char SURROGATE_LOW_MAX = (char)0xDFFF;

        private string _symbol = string.Empty; 
        private string _contextualExpansion = string.Empty;
        private string _isolatedExpansion = string.Empty;
        private string _pron = string.Empty;
        private CharTableCompiler.CharFeature _feature = CharTableCompiler.CharFeature.None;
        private CharType _type = CharType.Symbol;

        /// <summary>
        /// CharType.
        /// </summary>
        public enum CharType
        {
            /// <summary>
            /// LowerCase.
            /// </summary>
            LowerCase,

            /// <summary>
            /// UpperCase.
            /// </summary>
            UpperCase,

            /// <summary>
            /// Digit.
            /// </summary>
            Digit,

            /// <summary>
            /// Symbol.
            /// </summary>
            Symbol
        }

        /// <summary>
        /// Gets or sets Char type.
        /// </summary>
        public CharType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// Gets or sets Char feature.
        /// </summary>
        public CharTableCompiler.CharFeature Feature
        {
            get { return _feature; }
            set { _feature = value; }
        }

        /// <summary>
        /// Gets or sets Symbol of the char element.
        /// </summary>
        public string Symbol
        {
            get { return _symbol; }
            set { _symbol = value; }
        }

        /// <summary>
        /// Gets encoded symbol in binary file.
        /// </summary>
        [CLSCompliantAttribute(false)]
        public uint EncodedSymbol
        {
            get
            {
                uint encoded = 0;

                if (string.IsNullOrEmpty(Symbol))
                {
                    // do nothing.
                }
                else if (Symbol.Length == 1)
                {
                    encoded = Symbol[0];
                }
                else if (Symbol.Length == 2 &&
                    SURROGATE_HIGH_MIN <= Symbol[0] && Symbol[0] <= SURROGATE_HIGH_MAX &&
                    SURROGATE_LOW_MIN <= Symbol[1] && Symbol[1] <= SURROGATE_LOW_MAX)
                {
                    encoded = Symbol[0];
                    encoded <<= 16;
                    encoded |= Symbol[1];
                }
                else
                {
                    // It's invalid symbol. Just keep first character.
                    encoded = Symbol[0];
                }

                return encoded;
            }
        }

        /// <summary>
        /// Gets or sets Expansion of the char element.
        /// </summary>
        public string ContextualExpansion
        {
            get { return _contextualExpansion; }
            set { _contextualExpansion = value; }
        }

        /// <summary>
        /// Gets or sets Isolated expansion.
        /// </summary>
        public string IsolatedExpansion
        {
            get { return _isolatedExpansion; }
            set { _isolatedExpansion = value; }
        }

        /// <summary>
        /// Gets or sets Pron of the char element.
        /// </summary>
        public string Pronunciation
        {
            get { return _pron; }
            set { _pron = value; }
        }

        /// <summary>
        /// Check whether input string is a single unicode character.
        /// Unicode character longer than 16-bit will be presented by a string with length 2.
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <returns>Return true: single character.</returns>
        public static bool IsSingleChar(string str)
        {
            bool singleChar = false;
            if (str.Length == 1)
            {
                singleChar = true;
            }
            else if (str.Length == 2)
            {
                if (SURROGATE_HIGH_MIN <= str[0] && str[0] <= SURROGATE_HIGH_MAX &&
                    SURROGATE_LOW_MIN <= str[1] && str[1] <= SURROGATE_LOW_MAX)
                {
                    singleChar = true;
                }
            }

            return singleChar;
        }

        /// <summary>
        /// Is alpha symbol.
        /// </summary>
        /// <returns>True/false.</returns>
        public bool IsAlphabet()
        {
            return Type == CharType.UpperCase || Type == CharType.LowerCase;
        }
    }

    /// <summary>
    /// Char table class.
    /// </summary>
    public class CharTable : XmlDataFile
    {
        #region Fields

        private static XmlSchema _schema;
        private Collection<CharElement> _charList = new Collection<CharElement>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets Configuration schema.
        /// </summary>
        public override XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.CharTable.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets the char list.
        /// </summary>
        public Collection<CharElement> CharList
        {
            get { return _charList; }
        }

        /// <summary>
        /// Gets all symbol list.
        /// </summary>
        public List<string> Symbols
        {
            get 
            {
                List<string> symbols = new List<string>();

                foreach (CharElement charElement in _charList)
                {
                    symbols.Add(charElement.Symbol);
                }

                return symbols;
            }
        }
        #endregion

        /// <summary>
        /// Extract the spell out words and those symbols with contextRead attribute.
        /// </summary>
        /// <returns>The word list.</returns>
        public List<string> ExtractExpansionWords()
        {
            Dictionary<string, object> words = new Dictionary<string, object>();
            foreach (CharElement elem in _charList)
            {
                if (!string.IsNullOrEmpty(elem.IsolatedExpansion))
                {
                    string[] expandedWords = elem.IsolatedExpansion.Split(" ,.".ToCharArray());
                    foreach (string word in expandedWords)
                    {
                        if (!words.ContainsKey(word))
                        {
                            words.Add(word, null);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(elem.ContextualExpansion))
                {
                    // add those symbols with contextRead attribute
                    if (!words.ContainsKey(elem.Symbol))
                    {
                        words.Add(elem.Symbol, null);
                    }
                }
            }

            return new List<string>(words.Keys);
        }

        /// <summary>
        /// Load XML file.
        /// </summary>
        /// <param name="xmlDoc">Xml document.</param>
        /// <param name="nsmgr">Nsmgr.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void Load(XmlDocument xmlDoc, XmlNamespaceManager nsmgr,
            object contentController)
        {
            if (xmlDoc == null)
            {
                throw new ArgumentNullException("xmlDoc");
            }

            if (nsmgr == null)
            {
                throw new ArgumentNullException("nsmgr");
            }

            Language = Localor.StringToLanguage(xmlDoc.DocumentElement.GetAttribute("lang"));
            XmlNodeList nodeList = xmlDoc.DocumentElement.SelectNodes("/tts:chartable/tts:char", nsmgr);

            _charList.Clear();

            foreach (XmlNode node in nodeList)
            {
                CharElement charElement = new CharElement();

                XmlNode attrNode = node.Attributes["symbol"];
                string symbolString = attrNode.Value.Trim();
                if (string.IsNullOrEmpty(symbolString))
                {
                    this.ErrorSet.Add(new Error(CharTableError.EmptySymbol, symbolString));
                }
                else
                {
                    if (!CharElement.IsSingleChar(symbolString))
                    {
                        this.ErrorSet.Add(new Error(CharTableError.SymbolNotSingleChar, symbolString));
                    }
                    
                    charElement.Symbol = symbolString;
                }

                attrNode = node.Attributes["isolatedSymbolReadout"];
                if (attrNode != null)
                {
                    charElement.IsolatedExpansion = Regex.Replace(attrNode.Value.Trim(), @" +", " ");
                }

                attrNode = node.Attributes["contextualSymbolReadout"];
                if (attrNode != null)
                {
                    charElement.ContextualExpansion = attrNode.Value.Trim();
                }

                attrNode = node.Attributes["pron"];
                if (attrNode != null)
                {
                    charElement.Pronunciation = attrNode.Value.Trim();
                }

                attrNode = node.Attributes["feature"];
                if (attrNode != null)
                {
                    string[] features = attrNode.Value.Trim().Split(new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries);
                    foreach (string feature in features)
                    {
                        charElement.Feature |= (CharTableCompiler.CharFeature)Enum.Parse(
                            typeof(CharTableCompiler.CharFeature), feature, true);
                    }
                }

                attrNode = node.Attributes["type"];

                // Default is symbol
                charElement.Type = CharElement.CharType.Symbol;
                if (attrNode != null)
                {
                    charElement.Type = (CharElement.CharType)Enum.Parse(
                        typeof(CharElement.CharType), attrNode.Value.Trim(),
                        true);
                }

                CharList.Add(charElement);
            }
        }

        /// <summary>
        /// PerformanceSave.
        /// </summary>
        /// <param name="writer">Writer.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void PerformanceSave(XmlWriter writer, object contentController)
        {
            writer.WriteStartElement("chartable", Schema.TargetNamespace);
            writer.WriteAttributeString("lang", Localor.LanguageToString(Language));

            foreach (CharElement charElem in CharList)
            {
                writer.WriteStartElement("char");
                writer.WriteAttributeString("symbol", charElem.Symbol);
                if (!string.IsNullOrEmpty(charElem.IsolatedExpansion))
                {
                    writer.WriteAttributeString("isolatedSymbolReadout", charElem.IsolatedExpansion);
                }

                if (!string.IsNullOrEmpty(charElem.ContextualExpansion))
                {
                    writer.WriteAttributeString("contextualSymbolReadout", charElem.ContextualExpansion);
                }

                if (!string.IsNullOrEmpty(charElem.Pronunciation))
                {
                    writer.WriteAttributeString("pron", charElem.Pronunciation);
                }

                if (charElem.Feature != CharTableCompiler.CharFeature.None)
                {
                    writer.WriteAttributeString("feature",
                        CharTableCompiler.FeaturesToString(charElem.Feature));
                }

                writer.WriteAttributeString("type", charElem.Type.ToString());
                writer.WriteEndElement();
            }
        }
    }

    /// <summary>
    /// Char table validator.
    /// </summary>
    public class ChartableValidator
    {
        #region Fields

        private const string IsolatedExpansionType = "ContextualSymbolReadout";
        private const string SpelloutExpansionType = "IsolatedSymbolReadout";
        private string _lexiconFilePath;
        private string _phonesetFilePath;
        private Lexicon _lexicon;
        private TtsPhoneSet _phoneset;
        private Language _language;

        #endregion

        #region Properties

        /// <summary>
        /// ChartableValidator's AttributeError.
        /// </summary>
        public enum AttributeError
        {
            /// <summary>
            /// Unknown type.
            /// </summary>
            [ErrorAttribute(Message = "Type is unknown: {0}", 
                Severity = ErrorSeverity.Warning)]
            UnknownType,

            /// <summary>
            /// Empty type.
            /// </summary>
            [ErrorAttribute(Message = "Type should not be empty.", 
                Severity = ErrorSeverity.Warning)]
            EmptyType,

            /// <summary>
            /// Invalid Type warning.
            /// </summary>
            [ErrorAttribute(Message = "symbol \"{0}\": Please assign \"LowerCase\", \"UpperCase\", \"Digit\" or \"Symbol\" for type.",
                Severity = ErrorSeverity.Warning)]
            InvalidTypeWarning,

            /// <summary>
            /// Unknown feature.
            /// </summary>
            [ErrorAttribute(Message = "Feature is unknown: {0}", 
                Severity = ErrorSeverity.MustFix)]
            UnknownFeature,

            /// <summary>
            /// OOV Symbol expansion.
            /// </summary>
            [ErrorAttribute(Message = "{0} has OOV word at symbol: {1}", 
                Severity = ErrorSeverity.Warning)]
            OOVSymbolExpansion,

            /// <summary>
            /// OOV Symbol expansion.
            /// </summary>
            [ErrorAttribute(Message = "{0} has OOV word at position: {1}", 
                Severity = ErrorSeverity.Warning)]
            OOVWordExpansion,

            /// <summary>
            /// Empty spell out expansion.
            /// </summary>
            [ErrorAttribute(Message = "Spell out expansion cannot be empty for char [{0}]", 
                Severity = ErrorSeverity.MustFix)]
            EmptySpellOutExpansion,

            /// <summary>
            /// Invalid symbol.
            /// </summary>
            [ErrorAttribute(Message = "Invalid symbol:{0}", 
                Severity = ErrorSeverity.MustFix)]
            InvalidSymbol,

            /// <summary>
            /// Duplicate symbol.
            /// </summary>
            [ErrorAttribute(Message = "Duplicate symbol: {0}", 
                Severity = ErrorSeverity.Warning)]
            DuplicateSymbol,

            /// <summary>
            /// Uppercase and lowercase count doesn't match.
            /// </summary>
            [ErrorAttribute(Message = "Uppercase and lowercase count doesn't match: {0} != {1}",
                Severity = ErrorSeverity.Warning)]
            SymbolCaseCountMismatch,

            /// <summary>
            /// Digit count is not 10.
            /// </summary>
            [ErrorAttribute(Message = "Digit count is not 10.",
                Severity = ErrorSeverity.Warning)]
            UnCompletedDigits,

            /// <summary>
            /// Alphabet symbol must have pronunciation.
            /// </summary>
            [ErrorAttribute(Message = "Symbol {0}: Alphabet symbol must have pronunciation.", 
                Severity = ErrorSeverity.MustFix)]
            AlphabetSymbolLackOfPron,

            /// <summary>
            /// Alphabet symbol must have feature.
            /// </summary>
            [ErrorAttribute(Message = "Symbol {0}: Alphabet symbol must have feature.", 
                Severity = ErrorSeverity.MustFix)]
            AlphabetSymbolLackOfFeature,

            /// <summary>
            /// Alphabet symbol should not have expansion.
            /// </summary>
            [ErrorAttribute(Message = "Symbol {0}: Alphabet symbol should not have expansion.",
                Severity = ErrorSeverity.Warning)]
            AlphabetSymbolRemoveExpansion,

            /// <summary>
            /// Non alphabet symbol should not have feature.
            /// </summary>
            [ErrorAttribute(Message = "Symbol {0}: Non alphabet symbol should not have feature.",
                Severity = ErrorSeverity.Warning)]
            NonAlphabetSymbolRemoveFeature,

            /// <summary>
            /// Non alphabet symbol should not have pronunciation.
            /// </summary>
            [ErrorAttribute(Message = "Symbol {0}: Non alphabet symbol should not have pronunciation.",
                Severity = ErrorSeverity.Warning)]
            NonAlphabetSymbolRemovePron,

            /// <summary>
            /// Isolated symbol has a different pronunciation in lexicon.
            /// </summary>
            [ErrorAttribute(Message = "Symbol {0}: Isolated symbol has a different pronunciation in lexicon.",
                Severity = ErrorSeverity.Warning)]
            SymbolDiffPronFromLex,

            /// <summary>
            /// Symbol already in lexicon.
            /// </summary>
            [ErrorAttribute(Message = "Symbol {0}: Isolated symbol with the pronunciation is already in lexicon.",
                Severity = ErrorSeverity.NoError)]
            InfoSymbolInLex,

            /// <summary>
            /// Failed to generate pronunciation for the specific symbol.
            /// </summary>
            [ErrorAttribute(Message = "Symbol {0}: Isolated symbol's pronunciation cannot be generated.",
                Severity = ErrorSeverity.MustFix)]
            SymbolPronGenError,
        }

        /// <summary>
        /// Gets or sets Language id.
        /// </summary>
        public Language Language
        {
            get { return _language; }
            set { _language = value; }
        }

        /// <summary>
        /// Gets or sets Lexicon.
        /// </summary>
        public Lexicon Lexicon
        {
            get { return _lexicon; }
            set { _lexicon = value; }
        }

        /// <summary>
        /// Gets or sets TTS Phone Set.
        /// </summary>
        public TtsPhoneSet PhoneSet
        {
            get { return _phoneset; }
            set { _phoneset = value; }
        }

        /// <summary>
        /// Gets or sets Phone set xml.
        /// </summary>
        public string PhoneSetFilePath
        {
            get { return _phonesetFilePath; }
            set { _phonesetFilePath = value; }
        }

        /// <summary>
        /// Gets or sets Lexicon xml.
        /// </summary>
        public string LexiconFilePath
        {
            get { return _lexiconFilePath; }
            set { _lexiconFilePath = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Validate char table.
        /// </summary>
        /// <param name="table">Char table.</param>
        /// <param name="shallow">Shallow validation.</param>
        /// <param name="wordsNotInLexicon">WordsNotInLexicon.</param>
        /// <returns>ErrorSet.</returns>
        public ErrorSet Validate(CharTable table,
            bool shallow, Collection<string> wordsNotInLexicon)
        {
            if (table == null)
            {
                throw new ArgumentNullException("table");
            }

            ErrorSet errorSet = new ErrorSet();
            int upperCaseNumber = 0;
            int lowerCaseNumber = 0;
            int digitNumber = 0;
            Collection<string> symbols = new Collection<string>();

            foreach (CharElement charElement in table.CharList)
            {
                if (charElement.Type == CharElement.CharType.UpperCase)
                {
                    upperCaseNumber++;
                }
                else if (charElement.Type == CharElement.CharType.LowerCase)
                {
                    lowerCaseNumber++;
                }
                else if (charElement.Type == CharElement.CharType.Digit)
                {
                    digitNumber++;
                }

                if (!symbols.Contains(charElement.Symbol))
                {
                    symbols.Add(charElement.Symbol);
                }
                else
                {
                    errorSet.Add(new Error(CharTableError.DuplicateSymbol,
                        charElement.Symbol));
                }

                if (!shallow)
                {
                    ValidateCharElement(charElement, errorSet, wordsNotInLexicon);
                }
            }

            if (upperCaseNumber != lowerCaseNumber)
            {
                errorSet.Add(new Error(CharTableError.MismatchUpperAndLower,
                    upperCaseNumber.ToString(CultureInfo.InvariantCulture),
                    lowerCaseNumber.ToString(CultureInfo.InvariantCulture)));
            }

            if (digitNumber != 10)
            {
                errorSet.Add(new Error(CharTableError.ErrorDigitCount));
            }

            return errorSet;
        }

        /// <summary>
        /// Validate char element.
        /// </summary>
        /// <param name="element">Char element.</param>
        /// <param name="errorSet">Errors.</param>
        /// <param name="wordsNotInLexicon">WordsNotInLexicon.</param>
        public void ValidateCharElement(CharElement element,
            ErrorSet errorSet, Collection<string> wordsNotInLexicon)
        {
            if (errorSet == null)
            {
                throw new ArgumentNullException("errors");
            }

            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            if (string.IsNullOrEmpty(element.Symbol))
            {
                errorSet.Add(new Error(CharTableError.EmptySymbol));
            }
            else
            {
                EnsureInitialized();
                if (_lexicon.Lookup(element.Symbol, true) == null)
                {
                    errorSet.Add(new Error(CharTableError.SymbolNotInLexicon,
                        element.Symbol));
                }

                if (!element.IsAlphabet())
                {
                    // IsolatedExpansion should not empty for no-alphabet
                    if (!string.IsNullOrEmpty(element.IsolatedExpansion))
                    {
                        ValidateExpansion(element.IsolatedExpansion,
                            "IsolatedSymbolReadout", errorSet, wordsNotInLexicon);
                    }
                    else
                    {
                        errorSet.Add(new Error(CharTableError.EmptyIsolatedSymbol,
                            element.Symbol));
                    }

                    // ContextualSymbolReadout is optional for no-alphabet
                    if (!string.IsNullOrEmpty(element.ContextualExpansion))
                    {
                        ValidateExpansion(element.ContextualExpansion,
                            "ContextualSymbolReadout", errorSet, wordsNotInLexicon);
                    }

                    // No-alphabet should not have features.
                    if (element.Feature != CharTableCompiler.CharFeature.None)
                    {
                        errorSet.Add(new Error(CharTableError.NonAlphabetShouldNoFeatures,
                            element.Symbol));
                    }

                    // No-alphabet should not have pronunciation.
                    if (!string.IsNullOrEmpty(element.Pronunciation))
                    {
                        errorSet.Add(new Error(CharTableError.NonAlphabetShouldNoPronunciation,
                            element.Symbol));
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(element.Pronunciation))
                    {
                        errorSet.Add(new Error(CharTableError.AlphabetNoPron,
                            element.Symbol));
                    }
                    else
                    {
                        ErrorSet pronErrorSet = Pronunciation.Validate(
                            element.Pronunciation, _phoneset);
                        foreach (Error error in pronErrorSet.Errors)
                        {
                            Error alphabetError = new Error(CharTableError.AlphabetInvalidPron,
                                error, element.Symbol, element.Pronunciation);

                            // Keep the same error level with pronunciation error.
                            alphabetError.Severity = error.Severity;
                            errorSet.Add(alphabetError);
                        }
                    }

                    if (element.Feature == CharTableCompiler.CharFeature.None)
                    {
                        errorSet.Add(new Error(CharTableError.AlphabetNoFeatures,
                            element.Symbol));
                    }

                    if (!string.IsNullOrEmpty(element.IsolatedExpansion) ||
                        !string.IsNullOrEmpty(element.ContextualExpansion))
                    {
                        errorSet.Add(new Error(CharTableError.AlphabetShouldNoExpansion,
                            element.Symbol));
                    }
                }
            }
        }

        /// <summary>
        /// Check and geneate isolated symbol lexion.
        /// </summary>
        /// <param name="chartable">Char table.</param>
        /// <param name="posSymbol">Pos of symbol.</param>
        /// <param name="lexiconOutput">Lexicon output.</param>
        /// <param name="errors">Errors.</param>
        public void CheckContextualSymbolInLexicon(CharTable chartable,
            string posSymbol, string lexiconOutput, Collection<string> errors)
        {
            if (chartable == null)
            {
                throw new ArgumentNullException("chartable");
            }

            if (errors == null)
            {
                throw new ArgumentNullException("errors");
            }

            if (posSymbol == null)
            {
                throw new ArgumentNullException("posSymbol");
            }

            Lexicon lexicon = new Lexicon(chartable.Language);
            Collection<string> polyWord = new Collection<string>();

            foreach (CharElement charElement in chartable.CharList)
            {
                LexicalItem symbolItem = _lexicon.Lookup(
                    charElement.Symbol.ToString(), true);
                LexicalItem lexiconItem = new LexicalItem(lexicon.Language);
                LexiconPronunciation lexiconPron = new LexiconPronunciation(
                    lexicon.Language);
                string pron = string.Empty;
                string expansion = charElement.ContextualExpansion;

                if (string.IsNullOrEmpty(expansion))
                {
                    continue;
                }

                lexiconItem.Grapheme = charElement.Symbol.ToString();
                Collection<string> errorStrings = new Collection<string>();
                bool hasError = _lexicon.GetPronunciationForWords(expansion, errorStrings, polyWord, ref pron);
                if (!hasError && !string.IsNullOrEmpty(pron))
                {
                    bool addWord = true;
                    if (symbolItem != null)
                    {
                        string[] prons = Pronunciation.SplitIntoPhones(pron);
                        foreach (LexiconPronunciation existPron in symbolItem.Pronunciations)
                        {
                            bool same = true;
                            string[] existProns = Pronunciation.SplitIntoPhones(existPron.Symbolic);
                            if (existProns.Length == prons.Length)
                            {
                                for (int i = 0; i < prons.Length; i++)
                                {
                                    if (existProns[i] != prons[i])
                                    {
                                        same = false;
                                        break;
                                    }
                                }

                                if (same)
                                {
                                    addWord = false;
                                    break;
                                }
                            }
                        }
                    }

                    // add the word if the symbol or pronunicaiton is not in lexicon
                    if (addWord)
                    {
                        lexiconPron.Symbolic = pron;
                        LexiconItemProperty lip = new LexiconItemProperty();
                        lip.PartOfSpeech = new PosItem(posSymbol);
                        lexiconPron.Properties.Add(lip);
                        lexiconItem.Pronunciations.Add(lexiconPron);
                        lexicon.Items.Add(lexiconItem.Grapheme, lexiconItem);
                        if (symbolItem != null)
                        {
                            errors.Add(AttributeError.SymbolDiffPronFromLex + charElement.Symbol.ToString());
                        }
                    }
                    else
                    {
                        errors.Add(AttributeError.InfoSymbolInLex + charElement.Symbol.ToString());
                    }
                }
                else
                {
                    errors.Add(AttributeError.SymbolPronGenError + charElement.Symbol.ToString());
                }
            }

            Lexicon.WriteAllData(lexiconOutput, lexicon, Encoding.Unicode);
        }

        /// <summary>
        /// Initialize the validator.
        /// </summary>
        public void EnsureInitialized()
        {
            Debug.Assert(LexiconFilePath != null || Lexicon != null);
            Debug.Assert(PhoneSetFilePath != null || PhoneSet != null);

            if (_lexicon == null)
            {
                _lexicon = new Lexicon();
                _lexicon.Load(LexiconFilePath);
            }

            if (_phoneset == null)
            {
                _phoneset = new TtsPhoneSet();
                _phoneset.Load(PhoneSetFilePath);
            }

            if (_phoneset.Language != _lexicon.Language)
            {
                string message = Utility.Helper.NeutralFormat(
                    "phoneset and lexicon language should match");
                throw new InvalidDataException(message);
            }

            _language = _lexicon.Language;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Check expansion type.
        /// </summary>
        /// <param name="expansion">Expansion words.</param>
        /// <param name="expansionType">Type string.</param>
        /// <param name="errorSet">Errors.</param>
        /// <param name="wordsNotInLexicon">WordsNotInLexicon.</param>
        private void ValidateExpansion(string expansion, string expansionType,
            ErrorSet errorSet, Collection<string> wordsNotInLexicon)
        {
            if (expansion == null)
            {
                throw new ArgumentNullException("expansion");
            }

            if (expansionType == null)
            {
                throw new ArgumentNullException("expansionType");
            }

            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            string[] arr = expansion.Split(new char[] { '\t', ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (string word in arr)
            {
                // skip validating ExpansionWordNotInLexicon chartable error for English readout in zhXX
                // because English word cannot be in lexicon of these languages
                if ((Language == Language.ZhCN || Language == Language.ZhTW || Language == Language.ZhHK)
                    && Helper.IsEnglishWord(word))
                {
                    continue;
                }

                if (_lexicon.Lookup(word, true) == null)
                {
                    errorSet.Add(new Error(CharTableError.ExpansionWordNotInLexicon,
                        word));
                    if (wordsNotInLexicon != null && !wordsNotInLexicon.Contains(word))
                    {
                        wordsNotInLexicon.Add(word);
                    }
                }
            }
        }

        #endregion
    }
}