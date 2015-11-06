//----------------------------------------------------------------------------
// <copyright file="ScriptWord.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script Word class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Serialization;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Definition of script word types.
    /// </summary>
    public enum WordType
    {
        /// <summary>
        /// Normal word entry type.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Period .
        /// </summary>
        Period,

        /// <summary>
        /// Exclamation !.
        /// </summary>
        Exclamation,

        /// <summary>
        /// Question ?.
        /// </summary>
        Question,

        /// <summary>
        /// Other punctuation.
        /// </summary>
        OtherPunctuation,

        /// <summary>
        /// Punctuation.
        /// </summary>
        Punctuation,

        /// <summary>
        /// Silence.
        /// </summary>
        Silence,

        /// <summary>
        /// Spell out to match with the ESP.WT_SPELLOUT.
        /// </summary>
        Spell,

        /// <summary>
        /// Bookmark to match with the ESP.WT_BOOKMARK.
        /// </summary>
        Bookmark
    }

    /// <summary>
    /// How the word should be processed: word, spell or expand.
    /// </summary>
    public enum PType
    {
        /// <summary>
        /// Not applied.
        /// </summary>
        NAN = 0,

        /// <summary>
        /// It should read out as a word.
        /// </summary>
        Word,

        /// <summary>
        /// It should spell out.
        /// </summary>
        Spell,

        /// <summary>
        /// It should be expanded.
        /// </summary>
        Expand
    }

    /// <summary>
    /// Definition of script word data structure.
    /// </summary>
    public class ScriptWord : ScriptAcousticsHolder
    {
        #region

        /// <summary>
        /// Default break for word.
        /// </summary>
        public const TtsBreak DefaultBreak = TtsBreak.Word;

        /// <summary>
        /// Undefined value for break ask for word.
        /// </summary>
        public const TtsBreak UndefinedBreakAsk = TtsBreak.Phone;

        /// <summary>
        /// Default word tone for word.
        /// </summary>
        public const TtsWordTone DefaultWordTone = TtsWordTone.Continue;

        /// <summary>
        /// Default emphasis for word.
        /// </summary>
        public const TtsEmphasis DefaultEmphasis = TtsEmphasis.None;

        /// <summary>
        /// Default pronunciation source for word.
        /// </summary>
        public const TtsPronSource DefaultPronSource = TtsPronSource.MainLexicon;

        /// <summary>
        /// Tcgpp score delimeter.
        /// </summary>
        public const char TcgppScoreDelimeter = ' ';

        /// <summary>
        /// Default probability value.
        /// </summary>
        public const float DefaultProbability = 0.0f;

        #endregion

        #region Fields

        // Following fields are for the compatible of two-line script
        private PartOfSpeech _pos = PartOfSpeech.Unknown;
        private TtsLiaison _liaison = TtsLiaison.Default;
        private int _offsetInString;
        private int _lengthInString;
        private string _posTag;
        private string _detailedPosString;
        private string _emphasisTag;
        private string _breakTag;
        private string _wordToneTag;
        private Collection<TtsUnit> _units = new Collection<TtsUnit>();

        // used only for unit generation
        private Collection<ScriptSyllable> _unitSyllables = new Collection<ScriptSyllable>();

        private string _grapheme;
        private string _pronunciation;
        private string _expansion;
        private WordType _wordType = WordType.Normal;
        private TtsBreak _break = ScriptWord.DefaultBreak;
        private TtsBreak _breakAsk = ScriptWord.UndefinedBreakAsk;
        private float _breakProb = DefaultProbability;
        private TtsWordTone _wordTone = ScriptWord.DefaultWordTone;
        private TtsEmphasis _emphasis = ScriptWord.DefaultEmphasis;
        private TtsPronSource _pronSource = ScriptWord.DefaultPronSource;
        private Language _language;
        private string _posString;
        private string _namedEntityTypeString;
        private TobiLabel _tobiInitialBoundaryTone;
        private TobiLabel _tobiFinalBoundaryTone;
        private string _shallowParseTag;
        private string _acousticDomainTag;
        private string _nusTag;
        private string _regularText;
        private ScriptSentence _sentence;
        private Collection<ScriptSyllable> _syllables = new Collection<ScriptSyllable>();

        private TtsXmlComments _ttsXmlComments = new TtsXmlComments();
        private string _tcgppScores;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptWord"/> class.
        /// </summary>
        public ScriptWord()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptWord"/> class.
        /// Construction of word for a specified language.
        /// </summary>
        /// <param name="language">Language of the word to create.</param>
        public ScriptWord(Language language)
        {
            _language = language;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptWord"/> class.
        /// Construction of word for a specified language.
        /// </summary>
        /// <param name="language">Language of the word to create.</param>
        /// <param name="text">The word.</param>
        public ScriptWord(Language language, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException("text");
            }

            _language = language;
            Grapheme = text;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptWord"/> class.
        /// Construction of word for a specified language.
        /// </summary>
        /// <param name="language">Language of the word to create.</param>
        /// <param name="text">The word.</param>
        /// <param name="pronunciation">Pronunciation, can contain.</param>
        public ScriptWord(Language language, string text, string pronunciation)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException("text");
            }

            if (string.IsNullOrEmpty(pronunciation))
            {
                throw new ArgumentNullException("pronunciation");
            }

            _language = language;
            Grapheme = text;
            Pronunciation = pronunciation;
        }

        #endregion

        #region Public events

        /// <summary>
        /// Event to indicate Units are accessed.
        /// </summary>
        public event EventHandler<EventArgs> AccessingUnits;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets TCGPP phones scores of the word.
        /// </summary>
        public string TcgppScores
        {
            get
            {
                return _tcgppScores;
            }

            set
            {
                _tcgppScores = value;
                if (!string.IsNullOrEmpty(_tcgppScores) && Units.Count > 0)
                {
                    // Parse TCGPP score to TtsMetaPhone
                    string[] tcgppScores = _tcgppScores.Split(new char[] { TcgppScoreDelimeter },
                        StringSplitOptions.RemoveEmptyEntries);
                    int index = 0;

                    int phoneCount = 0;
                    foreach (TtsUnit unit in Units)
                    {
                        foreach (TtsMetaPhone phone in unit.MetaUnit.Phones)
                        {
                            phoneCount++;
                        }
                    }

                    if (phoneCount != tcgppScores.Length)
                    {
                        throw new InvalidDataException(Helper.NeutralFormat(
                            "Invalid TCGPP score format [{0}], expected phone count [{1}].",
                            _tcgppScores, phoneCount));
                    }

                    foreach (TtsUnit unit in Units)
                    {
                        foreach (TtsMetaPhone phone in unit.MetaUnit.Phones)
                        {
                            int phoneTcgppScore = 0;
                            if (!int.TryParse(tcgppScores[index], out phoneTcgppScore))
                            {
                                StringBuilder sb = new StringBuilder();
                                sb.Append(Helper.NeutralFormat(
                                    "Invalid TCGPP score format [{0}] in word [{1}]",
                                    TcgppScores, Grapheme));

                                if (_sentence != null && _sentence.ScriptItem != null)
                                {
                                    sb.Append(Helper.NeutralFormat(" in item [{0}]", _sentence.ScriptItem.Id));
                                }

                                throw new InvalidDataException(sb.ToString());
                            }

                            phone.TcgppScore = phoneTcgppScore;
                            index++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets Tts XML comments.
        /// </summary>
        public TtsXmlComments TtsXmlComments
        {
            get { return _ttsXmlComments; }
        }

        /// <summary>
        /// Gets or sets BreakTag.
        /// </summary>
        public string BreakTag
        {
            get
            {
                return _breakTag;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _breakTag = value;
            }
        }

        /// <summary>
        /// Gets or sets WordToneTag.
        /// </summary>
        public string WordToneTag
        {
            get
            {
                return _wordToneTag;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _wordToneTag = value;
            }
        }

        /// <summary>
        /// Gets or sets EmphasisTag.
        /// </summary>
        public string EmphasisTag
        {
            get
            {
                return _emphasisTag;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _emphasisTag = value;
            }
        }

        /// <summary>
        /// Gets or sets Tag.
        /// </summary>
        public string PosTag
        {
            get
            {
                return _posTag;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _posTag = value;
            }
        }

        /// <summary>
        /// Gets or sets the detailed POS string.
        /// </summary>
        public string DetailedPosString
        {
            get
            {
                return _detailedPosString;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _detailedPosString = value;
            }
        }

        /// <summary>
        /// Gets or sets LengthInString.
        /// </summary>
        public int LengthInString
        {
            get { return _lengthInString; }
            set { _lengthInString = value; }
        }

        /// <summary>
        /// Gets or sets OffsetInString.
        /// </summary>
        public int OffsetInString
        {
            get { return _offsetInString; }
            set { _offsetInString = value; }
        }

        /// <summary>
        /// Gets a value indicating whether current word is a sentence mark.
        /// </summary>
        public bool IsSentenceMark
        {
            get
            {
                switch (WordType)
                {
                    case WordType.Normal:
                        return false;
                    case WordType.Period:
                        return true;
                    case WordType.Exclamation:
                        return true;
                    case WordType.Question:
                        return true;
                    case WordType.OtherPunctuation:
                        return false;
                    default:
                        System.Diagnostics.Debug.Assert(false);
                        return false;
                }
            }
        }

        /// <summary>
        /// Gets or sets POS.
        /// </summary>
        public PartOfSpeech Pos
        {
            get { return _pos; }
            set { _pos = value; }
        }

        /// <summary>
        /// Gets or sets Liaison mark for this word.
        /// </summary>
        public TtsLiaison Liaison
        {
            get { return _liaison; }
            set { _liaison = value; }
        }

        /// <summary>
        /// Gets Description string of this word.
        /// </summary>
        public string Description
        {
            get
            {
                if (string.IsNullOrEmpty(PosTag))
                {
                    return _grapheme;
                }
                else
                {
                    return _grapheme + "/" + PosTag;
                }
            }
        }

        /// <summary>
        /// Gets Units.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "Too much dependency and it is much clear here")]
        public Collection<TtsUnit> Units
        {
            get
            {
                if (AccessingUnits != null)
                {
                    AccessingUnits(this, null);
                }

                return _units;
            }
        }

        /// <summary>
        /// Gets Syllables used for unit generation.
        /// </summary>
        public Collection<ScriptSyllable> UnitSyllables
        {
            get
            {
                return _unitSyllables;
            }
        }

        // The following attributes are reserved for script object model

        /// <summary>
        /// Gets or sets MyProperty.
        /// </summary>
        public Language Language
        {
            get { return _language; }
            set { _language = value; }
        }

        /// <summary>
        /// Gets or sets Boolean, specify whether this word is marked emphasis.
        /// </summary>
        public TtsEmphasis Emphasis
        {
            get { return _emphasis; }
            set { _emphasis = value; }
        }

        /// <summary>
        /// Gets or sets Break level, appending this word.
        /// </summary>
        public TtsBreak Break
        {
            get { return _break; }
            set { _break = value; }
        }

        /// <summary>
        /// Gets or sets Word pronunciation sourcem appending this word.
        /// </summary>
        public TtsPronSource PronSource
        {
            get { return _pronSource; }
            set { _pronSource = value; }
        }

        /// <summary>
        /// Gets or sets the requirement of the break level for this work.
        /// </summary>
        public TtsBreak BreakAsk
        {
            get { return _breakAsk; }
            set { _breakAsk = value; }
        }

        /// <summary>
        /// Gets or sets the probability of the break level, appending this word.
        /// </summary>
        public float BreakProb
        {
            get { return _breakProb; }
            set { _breakProb = value; }
        }

        /// <summary>
        /// Gets or sets Word tone, appending this word.
        /// </summary>
        public TtsWordTone WordTone
        {
            get { return _wordTone; }
            set { _wordTone = value; }
        }

        /// <summary>
        /// Gets or sets Word type, specify what kind of word this is.
        /// </summary>
        public WordType WordType
        {
            get { return _wordType; }
            set { _wordType = value; }
        }

        /// <summary>
        /// Gets a value indicating whether the word is text word.
        /// </summary>
        public bool IsTextWord
        {
            get
            {
                return !string.IsNullOrEmpty(_grapheme);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the word is pronouncable normal word.
        /// </summary>
        public bool IsPronouncableNormalWord
        {
            get
            {
                return _wordType == WordType.Normal &&
                    !string.IsNullOrEmpty(_pronunciation);
            }
        }

        /// <summary>
        /// Gets or sets Word string, could be string.Empty for silence words.
        /// </summary>
        public string Grapheme
        {
            get
            {
                return _grapheme;
            }

            set
            {
                // don't throw an exception when the string is empty, because "silence" word has empty grapheme but not null
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _grapheme = value;
            }
        }

        /// <summary>
        /// Gets or sets Pronunciation string of this word
        /// This property is left for two-line script using
        /// For the XML script, call GetPronunciation(TtsPhoneset).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "Too much dependency and it is much clear here")]
        public string Pronunciation
        {
            get
            {
                return _pronunciation;
            }

            set
            {
                // None normal words' may not contains pronunciation.
                _units.Clear();
                _pronunciation = value;
                if (Sentence != null)
                {
                    Sentence.NeedBuildUnits = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets Acceptable word string, could be string.Empty for silence words.
        /// </summary>
        public string AcceptGrapheme
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Acceptable pronunciation string of this word.
        /// </summary>
        public string AcceptPronunciation
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Word expansion.
        /// </summary>
        public string Expansion
        {
            get
            {
                return _expansion;
            }

            set
            {
                _expansion = value;
            }
        }

        /// <summary>
        /// Gets or sets Process type: how the word should be processed. example: word, spell or expand.
        /// </summary>
        public PType ProcessType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets The sentence this word belongs to.
        /// </summary>
        public ScriptSentence Sentence
        {
            get { return _sentence; }
            set { _sentence = value; }
        }

        /// <summary>
        /// Gets or sets Shallow parsing tag.
        /// </summary>
        public string ShallowParseTag
        {
            get
            {
                return _shallowParseTag;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _shallowParseTag = value;
            }
        }

        /// <summary>
        /// Gets or sets the acoustic domain tag.
        /// </summary>
        public string AcousticDomainTag
        {
            get
            {
                return _acousticDomainTag;
            }

            set
            {
                _acousticDomainTag = value;
            }
        }

        /// <summary>
        /// Gets or sets the NUS tag.
        /// </summary>
        public string NusTag
        {
            get
            {
                return _nusTag;
            }

            set
            {
                _nusTag = value;
            }
        }

        /// <summary>
        /// Gets or sets Tobi Initial Boundary Tone.
        /// </summary>
        public TobiLabel TobiInitialBoundaryTone
        {
            get
            {
                return _tobiInitialBoundaryTone;
            }

            set
            {
                _tobiInitialBoundaryTone = value;
            }
        }

        /// <summary>
        /// Gets or sets Tobi Final Boundary Tone.
        /// </summary>
        public TobiLabel TobiFinalBoundaryTone
        {
            get
            {
                return _tobiFinalBoundaryTone;
            }

            set
            {
                _tobiFinalBoundaryTone = value;
            }
        }

        /// <summary>
        /// Gets or sets Regular Text.
        /// </summary>
        public string RegularText
        {
            get
            {
                return _regularText;
            }

            set
            {
                _regularText = value;
            }
        }

        /// <summary>
        /// Gets or sets Part of speech string.
        /// </summary>
        public string PosString
        {
            get
            {
                return _posString;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _posString = value;
            }
        }

        /// <summary>
        /// Gets or sets the type string of named entity of this instance.
        /// </summary>
        public string NamedEntityTypeString
        {
            get { return _namedEntityTypeString; }
            set { _namedEntityTypeString = value; }
        }

        /// <summary>
        /// Gets or sets the sub word list of this script word.
        /// </summary>
        public Collection<ScriptWord> SubWords { get; set; }

        /// <summary>
        /// Gets Syllable.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "Too much dependency and it is much clear here")]
        public Collection<ScriptSyllable> Syllables
        {
            get
            {
                return _syllables;
            }
        }

        /// <summary>
        /// Gets the script phones this word has.
        /// </summary>
        public Collection<ScriptPhone> ScriptPhones
        {
            get
            {
                Collection<ScriptPhone> phones = new Collection<ScriptPhone>();

                foreach (ScriptSyllable syllable in Syllables)
                {
                    foreach (ScriptPhone phone in syllable.Phones)
                    {
                        phones.Add(phone);
                    }
                }

                return phones;
            }
        }

        /// <summary>
        /// Gets a value indicating whether is pronounced and non-silence word, silence word has pronunciation "-sil-".
        /// </summary>
        public bool IsPronounced
        {
            get
            {
                return !string.IsNullOrEmpty(_pronunciation) && _wordType != WordType.Silence;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the word is last non silence word.
        /// </summary>
        public bool IsLastNonSilenceWord
        {
            get
            {
                Helper.ThrowIfNull(Sentence);
                bool isLastNonSilenceWord = IsPronouncableNormalWord;
                int index = Sentence.Words.IndexOf(this);
                if (index < 0)
                {
                    Helper.ThrowIfNull(Sentence.ScriptItem);
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Can't find word [{0}] in item [{1}] word list.",
                        Grapheme, Sentence.ScriptItem.Id));
                }

                for (int i = index + 1; i < Sentence.Words.Count; i++)
                {
                    if (Sentence.Words[i].IsPronounced)
                    {
                        isLastNonSilenceWord = false;
                        break;
                    }
                }

                return isLastNonSilenceWord;
            }
        }

        /// <summary>
        /// Gets or sets NE type text.
        /// </summary>
        public string NETypeText { get; set; }

        /// <summary>
        /// Gets word break suffix:
        ///     1) have break level bigger than #1 (word level break)
        ///     2) there has explicit break ask > #1, which word break is #1.
        /// </summary>
        public string BreakSuffix
        {
            get
            {
                const string WorkBreakTextFormat = " #{0}";
                const string WorkBreakAskTextFormat = "/#{0}";

                StringBuilder sb = new StringBuilder();
                if ((int)Break >= (int)TtsBreak.InterPhrase || BreakAsk == TtsBreak.InterPhrase)
                {
                    sb.AppendFormat(WorkBreakTextFormat, ScriptWord.BreakToString(Break, true));
                    if (BreakAsk != ScriptWord.UndefinedBreakAsk &&
                        ((BreakAsk == TtsBreak.Word && Break != BreakAsk) ||
                        (BreakAsk == TtsBreak.InterPhrase && (int)Break < (int)BreakAsk)))
                    {
                        sb.AppendFormat(WorkBreakAskTextFormat, ScriptWord.BreakToString(BreakAsk, true));
                    }
                }

                return sb.ToString();
            }
        }

        #endregion

        #region public static operations

        /// <summary>
        /// Build script syllables from a word's pronunciation.
        /// </summary>
        /// <param name="pronunciation">The word's pronunciation.</param>
        /// <param name="phoneSet">TtsPhoneSet.</param>
        /// <returns>The built syllables.</returns>
        public static Collection<ScriptSyllable> ParsePronunciationToSyllables(string pronunciation,
            TtsPhoneSet phoneSet)
        {
            if (string.IsNullOrEmpty(pronunciation))
            {
                throw new ArgumentNullException("pronunciation");
            }

            if (phoneSet == null)
            {
                throw new ArgumentNullException("phoneSet");
            }

            // check whether the pronunciation is valid
            // only need to throw exception for invalid pronunciation
            ErrorSet errors = Core.Pronunciation.Validate(pronunciation, phoneSet);

            if (errors.Count > 0)
            {
                string message = Helper.NeutralFormat("Invalid pronunciation.");
                throw new InvalidDataException(message);
            }

            string[] syllables = Core.Pronunciation.SplitIntoSyllables(pronunciation);
            Collection<ScriptSyllable> scriptSyllables = new Collection<ScriptSyllable>();
            foreach (string syllable in syllables)
            {
                scriptSyllables.Add(ScriptSyllable.ParseStringToSyllable(syllable, phoneSet));
            }

            return scriptSyllables;
        }

        /// <summary>
        /// Get the word type according to type name in script file.
        /// </summary>
        /// <param name="name">Type name.</param>
        /// <returns>WordType.</returns>
        public static WordType StringToWordType(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            WordType type = WordType.Normal;
            switch (name)
            {
                case "normal":
                    type = WordType.Normal;
                    break;
                case "punc":
                    type = WordType.Punctuation;
                    break;
                case "silence":
                    type = WordType.Silence;
                    break;
                case "spell":
                    type = WordType.Spell;
                    break;
                case "bookmark":
                    type = WordType.Bookmark;
                    break;
                default:
                    string message = Helper.NeutralFormat("Unrecognized word type name: \"{0}\"!", name);
                    throw new InvalidDataException(message);
            }

            return type;
        }

        /// <summary>
        /// Convert word type to string used in script file.
        /// </summary>
        /// <param name="type">Word type.</param>
        /// <returns>
        /// String representation of word type.
        /// </returns>
        public static string WordTypeToString(WordType type)
        {
            string name = string.Empty;

            switch (type)
            {
                case WordType.Normal:
                    name = @"normal";
                    break;
                case WordType.Punctuation:
                    name = @"punc";
                    break;
                case WordType.Silence:
                    name = @"silence";
                    break;
                case WordType.Spell:
                    name = @"spell";
                    break;
                case WordType.Bookmark:
                    name = @"bookmark";
                    break;
            }

            return name;
        }

        /// <summary>
        /// Get the emphasis according to emphasis name in script file.
        /// </summary>
        /// <param name="name">Emphasis name.</param>
        /// <returns>TtsEmphasis.</returns>
        public static TtsEmphasis StringToEmphasis(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            TtsEmphasis emphasis = TtsEmphasis.None;
            switch (name)
            {
                case "0":
                    break;
                case "1":
                case "2":
                case "3":
                case "4":
                    emphasis = TtsEmphasis.Yes;
                    break;
                default:
                    string message = Helper.NeutralFormat("Unrecognized emphasis name: \"{0}\"!", name);
                    throw new InvalidDataException(message);
            }

            return emphasis;
        }

        /// <summary>
        /// Convert TtsEmphasis to string used in script file.
        /// </summary>
        /// <param name="emphasis">TtsEmphasis.</param>
        /// <returns>
        /// String representation of TtsEmphasis.
        /// </returns>
        public static string EmphasisToString(TtsEmphasis emphasis)
        {
            string name = string.Empty;

            switch (emphasis)
            {
                case TtsEmphasis.Yes:
                    name = @"1";
                    break;
            }

            return name;
        }

        /// <summary>
        /// Get the break according to break name in script file.
        /// </summary>
        /// <param name="name">Break name.</param>
        /// <returns>TtsBreak.</returns>
        public static TtsBreak StringToBreak(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            TtsBreak breakLevel = TtsBreak.Word;
            switch (name)
            {
                case "0":
                    breakLevel = TtsBreak.Syllable;
                    break;
                case "1":
                    break;
                case "2":
                    breakLevel = TtsBreak.InterPhrase;
                    break;
                case "3":
                    breakLevel = TtsBreak.IntonationPhrase;
                    break;
                case "4":
                    breakLevel = TtsBreak.Sentence;
                    break;
                default:
                    string message = Helper.NeutralFormat("Unrecognized break name: \"{0}\"!", name);
                    throw new InvalidDataException(message);
            }

            return breakLevel;
        }

        /// <summary>
        /// Convert TtsBreak to string used in script file.
        /// </summary>
        /// <param name="breakLevel">TtsBreak.</param>
        /// <returns>
        /// String representation of TtsBreak.
        /// </returns>
        public static string BreakToString(TtsBreak breakLevel)
        {
            return BreakToString(breakLevel, false);
        }

        /// <summary>
        /// Convert TtsBreak to string used in script file.
        /// </summary>
        /// <param name="breakLevel">TtsBreak.</param>
        /// <param name="exportDefault">Whether export default break.</param>
        /// <returns>
        /// String representation of TtsBreak.
        /// </returns>
        public static string BreakToString(TtsBreak breakLevel, bool exportDefault)
        {
            string name = string.Empty;

            // For work level break, don't display the string, so return empty string.
            switch (breakLevel)
            {
                case TtsBreak.Syllable:
                    name = @"0";
                    break;
                case TtsBreak.Word:
                    name = @"1";
                    break;
                case TtsBreak.InterPhrase:
                    name = @"2";
                    break;
                case TtsBreak.IntonationPhrase:
                    name = @"3";
                    break;
                case TtsBreak.Sentence:
                    name = @"4";
                    break;
            }

            if (!exportDefault && breakLevel == DefaultBreak)
            {
                name = string.Empty;
            }

            return name;
        }

        /// <summary>
        /// Get the WordTone according to WordTone name in script file.
        /// </summary>
        /// <param name="name">WordTone name.</param>
        /// <returns>TtsWordTone.</returns>
        public static TtsWordTone StringToWordTone(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            TtsWordTone wordTone = TtsWordTone.Continue;
            switch (name)
            {
                case "r":
                    wordTone = TtsWordTone.MinorRise;
                    break;
                case "R":
                    wordTone = TtsWordTone.FullRise;
                    break;
                case "F":
                    wordTone = TtsWordTone.FullFall;
                    break;
                case "f":
                    wordTone = TtsWordTone.MinorFall;
                    break;
                case "c":
                    break;
                default:
                    string message = Helper.NeutralFormat("Unrecognized WordTone name: \"{0}\"!", name);
                    throw new InvalidDataException(message);
            }

            return wordTone;
        }

        /// <summary>
        /// Convert WordTone to string used in script file.
        /// </summary>
        /// <param name="wordTone">TtsWordTone.</param>
        /// <returns>
        /// String representation of WordTone.
        /// </returns>
        public static string WordToneToString(TtsWordTone wordTone)
        {
            string name = string.Empty;

            switch (wordTone)
            {
                case TtsWordTone.MinorRise:
                    name = @"r";
                    break;
                case TtsWordTone.FullRise:
                    name = @"R";
                    break;
                case TtsWordTone.FullFall:
                    name = @"F";
                    break;
                case TtsWordTone.MinorFall:
                    name = @"f";
                    break;
            }

            return name;
        }

        /// <summary>
        /// Get the PronSource according to PronSource name in script file.
        /// </summary>
        /// <param name="name">PronSource name.</param>
        /// <returns>TtsPronSource.</returns>
        public static TtsPronSource StringToPronSource(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            TtsPronSource pronSource = TtsPronSource.None;
            try
            {
                pronSource = (TtsPronSource)Enum.Parse(
                    typeof(TtsPronSource), name, true);
            }
            catch (ArgumentException ae)
            {
                string message = Helper.NeutralFormat(
                    "Unrecognized PronSource name: \"{0}\"!", name);
                throw new InvalidDataException(message, ae);
            }

            return pronSource;
        }

        /// <summary>
        /// Convert PronSource to string used in script file.
        /// </summary>
        /// <param name="pronSource">TtsPronSource.</param>
        /// <returns>
        /// String representation of PronSource.
        /// </returns>
        public static string PronSourceToString(TtsPronSource pronSource)
        {
            string name = string.Empty;

            if (pronSource != TtsPronSource.None)
            {
                name = CodeIdentifier.MakeCamel(pronSource.ToString());
            }

            return name;
        }

        #endregion

        #region public Operations

        /// <summary>
        /// Sync TCGPP score from phone to word.
        /// </summary>
        public void SyncTcgppScoreFromPhoneToWord()
        {
            StringBuilder sb = new StringBuilder();
            foreach (TtsUnit unit in GetUnits(Localor.GetPhoneme(Language),
                Localor.GetSliceData(Language), false))
            {
                foreach (TtsMetaPhone phone in unit.MetaUnit.Phones)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(" ");
                    }

                    sb.Append(phone.TcgppScore.ToString(CultureInfo.InvariantCulture));
                }
            }

            _tcgppScores = sb.ToString();
        }

        /// <summary>
        /// Get the pronunciation
        /// If there exist syllable list, build pronunciation from it
        /// Otherwise, return the pronunciation of word's attribute without unit boundary.
        /// Note: return empty string if this word doesn't have pronunciation(e.g. punctuation).
        /// </summary>
        /// <param name="phoneSet">Phone set.</param>
        /// <returns>Pronunciation string.</returns>
        public string GetPronunciation(TtsPhoneSet phoneSet)
        {
            string pronunciation = string.Empty;

            // Build the pronunciation when syllables and phones exist
            if (Syllables.Count > 0 && Syllables.Any(syl => syl.Phones.Count > 0))
            {
                StringBuilder sb = new StringBuilder();
                foreach (ScriptSyllable syllable in Syllables)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(Core.Pronunciation.SyllableBoundaryString);
                    }

                    sb.Append(syllable.BuildTextFromPhones(phoneSet));
                }

                pronunciation = sb.ToString();
            }
            else
            {
                // phoneSet can be null here
                if (!string.IsNullOrEmpty(_pronunciation))
                {
                    pronunciation = Core.Pronunciation.RemoveUnitBoundary(_pronunciation);
                }
            }

            return pronunciation;
        }

        /// <summary>
        /// Check whether the element is valid or not.
        /// </summary>
        /// <param name="itemID">ID of the script item.</param>
        /// <param name="nodePath">Path of the node.</param>
        /// <param name="scope">The validation setting.</param>
        /// <param name="errors">Contains errors found at present.</param>
        /// <returns>Valid or not. Always true if no validation is performed.</returns>
        public bool IsValid(string itemID, string nodePath, XmlScriptValidationScope scope, ErrorSet errors)
        {
            bool valid = true;

            for (int i = 0; i < _syllables.Count; i++)
            {
                ScriptSyllable syllable = _syllables[i];
                string path = string.Format(CultureInfo.InvariantCulture, "{0}.Syllable[{1}]", nodePath, i);
                if (!syllable.IsValid(itemID, path, scope, errors))
                {
                    valid = false;
                }
            }

            if (HasAcousticsValue)
            {
                string path = string.Format(CultureInfo.InvariantCulture, "{0}.Acoustics", nodePath);
                if (!Acoustics.IsValid(itemID, path, scope, errors))
                {
                    valid = false;
                }
            }

            return valid;
        }

        /// <summary>
        /// Write word to xml.
        /// </summary>
        /// <param name="writer">XmlWriter.</param>
        /// <param name="scriptContentController">XmlScriptFile.ContentControler.</param>
        /// <param name="scriptLanguage">The language of the script.</param>
        public void WriteToXml(XmlWriter writer, XmlScriptFile.ContentControler scriptContentController, Language scriptLanguage)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            if (scriptContentController == null)
            {
                throw new ArgumentNullException("scriptContentController");
            }

            // write <w> node and its attributes
            writer.WriteStartElement("w");

            if (Language != Language.Neutral && Language != scriptLanguage)
            {
                writer.WriteAttributeString("language", Localor.LanguageToString(Language));
            }

            writer.WriteAttributeString("v", Grapheme);

            if (!string.IsNullOrEmpty(Pronunciation))
            {
                writer.WriteAttributeString("p", Pronunciation);
            }

            if (!string.IsNullOrEmpty(AcceptGrapheme))
            {
                writer.WriteAttributeString("av", AcceptGrapheme);
            }

            if (!string.IsNullOrEmpty(AcceptPronunciation))
            {
                writer.WriteAttributeString("ap", AcceptPronunciation);
            }

            writer.WriteAttributeString("type", WordTypeToString(WordType));

            if (!string.IsNullOrEmpty(PosString))
            {
                writer.WriteAttributeString("pos", PosString);
            }

            if (!string.IsNullOrEmpty(Expansion))
            {
                writer.WriteAttributeString("exp", Expansion);
            }

            string emphasisName = EmphasisToString(Emphasis);
            if (!string.IsNullOrEmpty(emphasisName))
            {
                writer.WriteAttributeString("em", emphasisName);
            }

            if (Break != DefaultBreak)
            {
                string breakName = BreakToString(Break);
                writer.WriteAttributeString("br", breakName);
            }

            if (BreakAsk != UndefinedBreakAsk)
            {
                string breakName = BreakToString(BreakAsk, true);
                writer.WriteAttributeString("bra", breakName);
            }

            if (BreakProb != DefaultProbability)
            {
                writer.WriteAttributeString("brp", BreakProb.ToString("0.000", CultureInfo.InvariantCulture));
            }

            if (TobiFinalBoundaryTone != null)
            {
                writer.WriteAttributeString("tobifbt", TobiFinalBoundaryTone.ToString());
            }

            if (!string.IsNullOrEmpty(AcousticDomainTag))
            {
                writer.WriteAttributeString("domain", AcousticDomainTag);
            }

            if (!string.IsNullOrEmpty(NusTag))
            {
                writer.WriteAttributeString("nus", NusTag);
            }

            if (TobiInitialBoundaryTone != null)
            {
                writer.WriteAttributeString("tobiibt", TobiInitialBoundaryTone.ToString());
            }

            if (!string.IsNullOrEmpty(ShallowParseTag))
            {
                writer.WriteAttributeString("sp", ShallowParseTag);
            }

            string wordToneName = WordToneToString(WordTone);
            if (!string.IsNullOrEmpty(wordToneName))
            {
                writer.WriteAttributeString("wt", wordToneName);
            }

            if (!string.IsNullOrEmpty(_tcgppScores))
            {
                writer.WriteAttributeString("tcgppScore", _tcgppScores);
            }

            if (!string.IsNullOrEmpty(NETypeText))
            {
                writer.WriteAttributeString("netype", NETypeText);
            }

            if (!string.IsNullOrEmpty(RegularText))
            {
                writer.WriteAttributeString("regularText", RegularText.ToString());
            }

            if (PronSource != DefaultPronSource && scriptContentController.SavePronSource)
            {
                string pronSourceName = PronSourceToString(PronSource);
                if (!string.IsNullOrEmpty(pronSourceName))
                {
                    writer.WriteAttributeString("pronSource", pronSourceName);
                }
            }

            if (WordType != WordType.Silence)
            {
                if (OffsetInString > 0)
                {
                    writer.WriteAttributeString("offset", OffsetInString.ToString(CultureInfo.InvariantCulture));
                }

                if (LengthInString > 0)
                {
                    writer.WriteAttributeString("length", LengthInString.ToString(CultureInfo.InvariantCulture));
                }
            }

            if (ProcessType != PType.NAN)
            {
                switch (ProcessType)
                {
                    case PType.Word:
                        writer.WriteAttributeString("processType", "word");
                        break;
                    case PType.Spell:
                        writer.WriteAttributeString("processType", "spell");
                        break;
                    case PType.Expand:
                        writer.WriteAttributeString("processType", "expand");
                        break;
                }
            }

            if (scriptContentController.SaveComments)
            {
                _ttsXmlComments.WriteToXml(writer);
            }

            // write syllables
            if (Syllables.Count != 0)
            {
                writer.WriteStartElement("syls");
                foreach (ScriptSyllable syllable in Syllables)
                {
                    syllable.WriteToXml(writer);
                }

                writer.WriteEndElement();
            }

            if (HasAcousticsValue)
            {
                Acoustics.WriteToXml(writer);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Get the Phones of this word.
        /// </summary>
        /// <param name="phoneSet">Phone set.</param>
        /// <param name="errors">Errors if having invalid phone.</param>
        /// <returns>The phones.</returns>
        public Collection<Phone> GetPhones(TtsPhoneSet phoneSet, ErrorSet errors)
        {
            if (phoneSet == null)
            {
                throw new ArgumentNullException("phoneSet");
            }

            if (errors == null)
            {
                throw new ArgumentNullException("errors");
            }

            errors.Clear();
            string pronunciation = GetPronunciation(phoneSet);
            Collection<Phone> phoneColl = new Collection<Phone>();

            // Note: for punctucations should return empty phone collection
            if (WordType == WordType.Normal)
            {
                Phone[] phones = Core.Pronunciation.SplitIntoPhones(pronunciation, phoneSet, errors);
                if (phones != null)
                {
                    phoneColl = new Collection<Phone>(phones);
                }
            }

            return phoneColl;
        }

        /// <summary>
        /// Get the normal phones' names.
        /// </summary>
        /// <param name="phoneSet">Phone set.</param>
        /// <param name="errors">Errors is having.</param>
        /// <returns>The pohne names.</returns>
        public Collection<string> GetNormalPhoneNames(TtsPhoneSet phoneSet, ErrorSet errors)
        {
            if (phoneSet == null)
            {
                throw new ArgumentNullException("phoneSet");
            }

            if (errors == null)
            {
                throw new ArgumentNullException("errors");
            }

            errors.Clear();
            Collection<Phone> phones = GetPhones(phoneSet, errors);
            Collection<string> names = new Collection<string>();
            if (errors.Count == 0)
            {
                foreach (Phone phone in phones)
                {
                    if (phone.IsNormal)
                    {
                        names.Add(phone.Name);
                    }
                }
            }

            return names;
        }

        /// <summary>
        /// Get the syllable strings this word has
        /// The syllable contains stress but doesn't contain unit boundaries.
        /// </summary>
        /// <param name="phoneSet">Phone set.</param>
        /// <returns>Syllable strings.</returns>
        public Collection<string> GetSyllables(TtsPhoneSet phoneSet)
        {
            if (phoneSet == null)
            {
                throw new ArgumentNullException("phoneSet");
            }

            string[] syllables = Core.Pronunciation.SplitIntoSyllables(GetPronunciation(phoneSet));

            return new Collection<string>(syllables);
        }

        /// <summary>
        /// Build the pronunciation string of word from the units of current word.
        /// </summary>
        public void ReverseBuildPronunciation()
        {
            if (_units.Count == 0)
            {
                return;
            }

            _pronunciation = null;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Units.Count; i++)
            {
                TtsUnit unit = Units[i];
                if (i > 0)
                {
                    if ((int)Units[i - 1].TtsBreak >= (int)TtsBreak.Syllable)
                    {
                        sb.Append(Core.Pronunciation.SyllableBoundaryString);
                    }
                    else
                    {
                        sb.Append(Core.Pronunciation.UnitBoundaryString);
                    }

                    sb.Append(unit.PlainDescription);
                }
                else
                {
                    sb.Append(unit.PlainDescription);
                }
            }

            _pronunciation = sb.ToString();
        }

        /// <summary>
        /// Get the unit list this word has.
        /// </summary>
        /// <param name="phoneme">Phoneme.</param>
        /// <param name="sliceData">Slice data.</param>
        /// <returns>Tts units.</returns>
        public Collection<TtsUnit> GetUnits(Phoneme phoneme, SliceData sliceData)
        {
            return GetUnits(phoneme, sliceData, true);
        }

        /// <summary>
        /// Get the unit list this word has.
        /// </summary>
        /// <param name="phoneme">Phoneme.</param>
        /// <param name="sliceData">Slice data.</param>
        /// <param name="buildUnitFeature">Whether build unit features.</param>
        /// <returns>Tts units.</returns>
        public Collection<TtsUnit> GetUnits(Phoneme phoneme, SliceData sliceData,
            bool buildUnitFeature)
        {
            if (phoneme == null)
            {
                throw new ArgumentNullException("phoneme");
            }

            if (sliceData == null)
            {
                throw new ArgumentNullException("sliceData");
            }

            if (WordType == WordType.Normal && _units.Count == 0)
            {
                if (Sentence == null)
                {
                    throw new InvalidDataException(Helper.NeutralFormat("word should belong to a sentence."));
                }

                Sentence.GetUnits(phoneme, sliceData, buildUnitFeature);
            }

            return _units;
        }

        /// <summary>
        /// Reset Prosody tags and set them to default value,
        /// Including emphasis, break level and word tone.
        /// </summary>
        public void ClearTag()
        {
            _breakTag = null;
            _emphasisTag = null;
            _wordToneTag = null;

            _break = ScriptWord.DefaultBreak;
            _breakAsk = ScriptWord.UndefinedBreakAsk;
            _breakProb = ScriptWord.DefaultProbability;
            _emphasis = ScriptWord.DefaultEmphasis;
            _wordTone = ScriptWord.DefaultWordTone;
        }

        /// <summary>
        /// Build word unit without filling features.
        /// </summary>
        /// <param name="sliceData">Slice data.</param>
        /// <param name="pronunciationSeparator">Pronunciation separator.</param>
        public void BuildUnitWithoutFeature(SliceData sliceData,
            PronunciationSeparator pronunciationSeparator)
        {
            if (Units.Count > 0)
            {
                return;
            }

            UpdateUnitSyllables();

            for (int syllableIndex = 0; syllableIndex < UnitSyllables.Count; syllableIndex++)
            {
                ScriptSyllable syllable = UnitSyllables[syllableIndex];
                syllable.Tag = this;

                Collection<TtsUnit> syllableUnits = BuildUnitsForSyllable(syllable, sliceData, pronunciationSeparator);
                for (int i = 0; i < syllableUnits.Count; i++)
                {
                    syllableUnits[i].WordType = WordType;
                    syllableUnits[i].Tag = syllable;
                    syllableUnits[i].Word = this;

                    Units.Add(syllableUnits[i]);
                }
            }

            // Parse TCGPP score to TtsMetaPhone
            if (!string.IsNullOrEmpty(_tcgppScores))
            {
                string[] tcgppScores = _tcgppScores.Split(new char[] { TcgppScoreDelimeter },
                    StringSplitOptions.RemoveEmptyEntries);
                int index = 0;
                foreach (TtsUnit unit in Units)
                {
                    foreach (TtsMetaPhone phone in unit.MetaUnit.Phones)
                    {
                        if (index >= tcgppScores.Length)
                        {
                            throw new InvalidDataException(Helper.NeutralFormat(
                                "Invalid TCGPP score format [{0}]", _tcgppScores));
                        }

                        phone.TcgppScore = int.Parse(tcgppScores[index]);
                        index++;
                    }
                }
            }
        }

        /// <summary>
        /// Get next non-silence word in the sentence.
        /// </summary>
        /// <returns>Next non-silence word in the sentence.</returns>
        public ScriptWord NextNonSilenceWord()
        {
            ScriptSentence sentence = Sentence;
            Debug.Assert(Sentence != null, "Script sentence should not be null");

            int wordIndex = sentence.Words.IndexOf(this);
            Debug.Assert(wordIndex >= 0, "Word should be in sentence.");

            int offset = 1;
            while (wordIndex + offset < sentence.Words.Count &&
                sentence.Words[wordIndex + offset].WordType == WordType.Silence)
            {
                offset++;
            }

            ScriptWord nextNonSilenceWord = null;
            if (wordIndex + offset < sentence.Words.Count)
            {
                nextNonSilenceWord = sentence.Words[wordIndex + offset];
            }

            return nextNonSilenceWord;
        }

        #endregion

        #region Override methods

        /// <summary>
        /// Returns a System.string that represents the current ScriptWord.
        /// </summary>
        /// <returns>A System.string that represents the current ScriptWord.</returns>
        public override string ToString()
        {
            StringBuilder ret = new StringBuilder();
            ret.Append(Grapheme);
            ret.Append(" Break:");
            ret.Append(Break.ToString());
            ret.Append(" Type:");
            ret.Append(WordType.ToString());
            ret.Append(" Emphasis:");
            ret.Append(Emphasis.ToString());

            return ret.ToString();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Build units from syllable.
        /// </summary>
        /// <param name="syllable">Syllable.</param>
        /// <param name="sliceData">Slice data.</param>
        /// <param name="pronunciationSeparator">Pronunciation separator.</param>
        /// <returns>Units.</returns>
        private static Collection<TtsUnit> BuildUnitsForSyllable(ScriptSyllable syllable,
            SliceData sliceData, PronunciationSeparator pronunciationSeparator)
        {
            Debug.Assert(syllable != null);
            Debug.Assert(sliceData != null);

            string syllableText = Core.Pronunciation.RemoveStress(syllable.Text.Trim());
            string[] slices = pronunciationSeparator.SplitSlices(syllableText);

            PosInSyllable[] pis = EstimatePosInSyllable(slices, sliceData);

            Collection<TtsUnit> units = new Collection<TtsUnit>();
            for (int sliceIndex = 0; sliceIndex < slices.Length; sliceIndex++)
            {
                string slice = slices[sliceIndex].Trim();
                if (string.IsNullOrEmpty(slice))
                {
                    continue;
                }

                TtsUnit unit = new TtsUnit(sliceData.Language);

                // break level
                unit.TtsBreak = (sliceIndex == slices.Length - 1) ? syllable.TtsBreak : TtsBreak.Phone;

                // pos in syllable
                unit.Feature.PosInSyllable = pis[sliceIndex];

                // NONE: punctuation type

                // emphasis
                unit.Feature.TtsEmphasis = syllable.TtsEmphasis;

                // stress mark
                unit.Feature.TtsStress = syllable.Stress;

                // fill unit name
                // remove stress mark and replace white space with '+' for unit name
                unit.MetaUnit.Name = Regex.Replace(slice, " +", @"+");
                unit.MetaUnit.Language = unit.Language;

                units.Add(unit);
            }

            return units;
        }

        /// <summary>
        /// Estimate pos in syllable for each slice.
        /// </summary>
        /// <param name="slices">Slices.</param>
        /// <param name="sliceData">Slice data table.</param>
        /// <returns>PosInSyllable list.</returns>
        private static PosInSyllable[] EstimatePosInSyllable(string[] slices, SliceData sliceData)
        {
            PosInSyllable[] pis = new PosInSyllable[slices.Length];
            int nucleusIndex = -1;

            for (int sliceIndex = 0; sliceIndex < slices.Length; sliceIndex++)
            {
                TtsMetaUnit ttsMetaUnit = new TtsMetaUnit(sliceData.Language);
                ttsMetaUnit.Name = slices[sliceIndex];

                if (sliceData.IsNucleus(ttsMetaUnit))
                {
                    if (sliceIndex == 0)
                    {
                        if (sliceIndex == slices.Length - 1)
                        {
                            pis[sliceIndex] = PosInSyllable.NucleusInV;
                        }
                        else
                        {
                            pis[sliceIndex] = PosInSyllable.NucleusInVC;
                        }
                    }
                    else
                    {
                        if (sliceIndex == slices.Length - 1)
                        {
                            pis[sliceIndex] = PosInSyllable.NucleusInCV;
                        }
                        else
                        {
                            pis[sliceIndex] = PosInSyllable.NucleusInCVC;
                        }
                    }

                    nucleusIndex = sliceIndex;
                    break;
                }
            }

            for (int sliceIndex = 0; sliceIndex < nucleusIndex; sliceIndex++)
            {
                if (sliceIndex == 0)
                {
                    pis[sliceIndex] = PosInSyllable.Onset;
                }
                else
                {
                    pis[sliceIndex] = PosInSyllable.OnsetNext;
                }
            }

            for (int sliceIndex = nucleusIndex + 1; sliceIndex < slices.Length; sliceIndex++)
            {
                if (sliceIndex == slices.Length - 1)
                {
                    pis[sliceIndex] = PosInSyllable.Coda;
                }
                else
                {
                    pis[sliceIndex] = PosInSyllable.CodaNext;
                }
            }

            return pis;
        }

        /// <summary>
        /// Update the syllables for the word.
        /// </summary>
        private void UpdateUnitSyllables()
        {
            if (Pronunciation == null)
            {
                throw new InvalidDataException(Helper.NeutralFormat("word {0}'s has no pronunciation",
                    Grapheme));
            }

            string[] syllableTexts = Core.Pronunciation.SplitIntoSyllables(Pronunciation);
            UnitSyllables.Clear();
            for (int syllableIndex = 0; syllableIndex < syllableTexts.Length; syllableIndex++)
            {
                ScriptSyllable syllable = new ScriptSyllable();
                syllable.Text = syllableTexts[syllableIndex];
                syllable.TtsBreak = (syllableIndex == syllableTexts.Length - 1) ?
                    Break : TtsBreak.Syllable;
                syllable.Stress = Core.Pronunciation.GetStress(syllable.Text);
                syllable.TtsEmphasis = (syllable.Stress != TtsStress.None) ?
                    Emphasis : TtsEmphasis.None;

                UnitSyllables.Add(syllable);
            }
        }

        #endregion
    }
}