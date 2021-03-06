//----------------------------------------------------------------------------
// <copyright file="ScriptItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script entry
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
    using System.Text.RegularExpressions;
    using System.Xml;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Definition of script domain types.
    /// </summary>
    public enum ScriptDomainType
    {
        /// <summary>
        /// Unknown type.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Normal.
        /// </summary>
        Normal,

        /// <summary>
        /// Digit.
        /// </summary>
        Digit,

        /// <summary>
        /// Name.
        /// </summary>
        Name
    }

    /// <summary>
    /// Pronunciation separator.
    /// </summary>
    public class PronunciationSeparator
    {
        #region Fields

        private const string ReservedSyllableSeparator = "&";

        private string _word;
        private string _syllable;
        private string _slice;
        private string _phone;

        private string[] _allForSyllable;
        private string[] _allForSlice;
        private string[] _allForPhone;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="PronunciationSeparator"/> class.
        /// Build pronunciation separator from all kinds of separator.
        /// </summary>
        /// <param name="word">Word separator.</param>
        /// <param name="syllable">Syllable separator.</param>
        /// <param name="slice">Slice separator.</param>
        /// <param name="phone">Phone separator.</param>
        public PronunciationSeparator(string word, string syllable,
            string slice, string phone)
        {
            if (string.IsNullOrEmpty(word))
            {
                throw new ArgumentNullException("word");
            }

            if (string.IsNullOrEmpty(syllable))
            {
                throw new ArgumentNullException("syllable");
            }

            if (string.IsNullOrEmpty(slice))
            {
                throw new ArgumentNullException("slice");
            }

            if (string.IsNullOrEmpty(phone))
            {
                throw new ArgumentNullException("phone");
            }

            Word = word;
            Syllable = syllable;
            Slice = slice;
            Phone = phone;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Phone separator.
        /// </summary>
        public string Phone
        {
            get
            {
                return _phone;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _phone = value;
            }
        }

        /// <summary>
        /// Gets or sets Slice separator.
        /// </summary>
        public string Slice
        {
            get
            {
                return _slice;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _slice = value;
            }
        }

        /// <summary>
        /// Gets or sets Syllable separator.
        /// </summary>
        public string Syllable
        {
            get
            {
                return _syllable;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _syllable = value;
            }
        }

        /// <summary>
        /// Gets or sets Word separator.
        /// </summary>
        public string Word
        {
            get
            {
                return _word;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _word = value;
            }
        }

        #endregion

        #region Public static methods

        /// <summary>
        /// Get all syllable in the pronunciation.
        /// </summary>
        /// <param name="pronunciation">Pronunciation to be processed.</param>
        /// <returns>Splitted syllables array.</returns>
        public string[] SplitSyllables(string pronunciation)
        {
            if (pronunciation == null)
            {
                throw new ArgumentNullException("pronunciation");
            }

            string[] syllables = pronunciation.Split(GetAllForSyllable(),
                StringSplitOptions.RemoveEmptyEntries);

            // Remove empty syllable like the one after "ao 1":
            //      /r aa 2 - s t . ax n - k . ao 1 . -  /
            return RemoveEmptyItems(syllables);
        }

        /// <summary>
        /// Get all slices in the pronunciation.
        /// </summary>
        /// <param name="pronunciation">Pronunciation to be processed.</param>
        /// <returns>Splitted slices array.</returns>
        public string[] SplitSlices(string pronunciation)
        {
            if (pronunciation == null)
            {
                throw new ArgumentNullException("pronunciation");
            }

            string[] units = pronunciation.Split(GetAllForSlice(),
                StringSplitOptions.RemoveEmptyEntries);

            // Avoid empty unit like the one after "ao 1":
            //      /r aa 2 - s t . ax n - k . ao 1 . - s k . iy/
            return RemoveEmptyItems(units);
        }

        /// <summary>
        /// Get all phones in the pronunciation, this methods will not delete tone.
        /// </summary>
        /// <param name="pronunciation">Pronunciation to be processed.</param>
        /// <returns>Splitted phoens array, tones are not deleted in the array.</returns>
        public string[] SplitPhones(string pronunciation)
        {
            if (pronunciation == null)
            {
                throw new ArgumentNullException("pronunciation");
            }

            string[] phones = pronunciation.Split(GetAllForPhone(),
                StringSplitOptions.RemoveEmptyEntries);

            return RemoveEmptyItems(phones);
        }

        #endregion

        #region Private static methods

        /// <summary>
        /// Remove empty items.
        /// </summary>
        /// <param name="items">Items to be processed.</param>
        /// <returns>Non empty items array.</returns>
        private static string[] RemoveEmptyItems(string[] items)
        {
            List<string> nonEmptyItems = new List<string>();
            foreach (string item in items)
            {
                string trimedItem = item.Trim();
                if (!string.IsNullOrEmpty(trimedItem))
                {
                    nonEmptyItems.Add(trimedItem);
                }
            }

            return nonEmptyItems.ToArray();
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Get all phone level separators.
        /// </summary>
        /// <returns>Phone separators.</returns>
        private string[] GetAllForPhone()
        {
            if (_allForPhone == null)
            {
                _allForPhone = new string[] { Word, ReservedSyllableSeparator, Syllable, Slice, Phone };
            }

            return _allForPhone;
        }

        /// <summary>
        /// Get all slice level separators.
        /// </summary>
        /// <returns>Slice separators.</returns>
        private string[] GetAllForSlice()
        {
            if (_allForSlice == null)
            {
                _allForSlice = new string[] { Word, ReservedSyllableSeparator, Syllable, Slice };
            }

            return _allForSlice;
        }

        /// <summary>
        /// Get all syllable level separators.
        /// </summary>
        /// <returns>Syllable separators.</returns>
        private string[] GetAllForSyllable()
        {
            if (_allForSyllable == null)
            {
                _allForSyllable = new string[] { Word, ReservedSyllableSeparator, Syllable };
            }

            return _allForSyllable;
        }

        #endregion
    }

    /// <summary>
    /// Class represents one script item in transcript file
    /// This class is compatible of the two-line script
    ///  An example of two-line item is:
    /// <example>
    /// 100006 this was a good deal * #2 for citibank * #2 and for germany.
    ///        dh . ih 1 . s / w . ax 1 . z / ax / g . uh 1 . d / d . iy 1 l 
    /// / f . ao 1 r / s . ih 1 - t . iy - b . ae 2 ng . k / ae 1 n . d 
    /// / f . ao 1 r / jh . er 1 r - m ax - n iy /.
    /// </example>
    /// </summary>
    public class ScriptItem
    {
        #region Const fields

        /// <summary>
        /// Word tone labels.
        /// </summary>
        public const string WordToneLabels = "cRrFfRF";

        /// <summary>
        /// The appended string following the sentence ID.
        /// </summary>
        public const string SentenceIdDelimiter = "\t";

        /// <summary>
        /// The appended string following the sentence
        /// ATTENTION: Do NOT use Environment.NewLine as the line break!!!
        /// Because it (\r\n) will be converted to "\n" in RichTextBox, and this
        /// Leads to the mismatch between ScriptItem.cs and ScriptItemBox.cs.
        /// </summary>
        public const string SentenceDelimiter = "\n";

        /// <summary>
        /// Append \r\n after sentence if to write the string to file.
        /// </summary>
        public const string SentenceDelimiterInFile = "\r\n";

        /// <summary>
        /// Item ID pattern.
        /// </summary>
        public const string ItemIdPattern = "[a-zA-Z0-9_]+";

        /// <summary>
        /// Item id Length.
        /// </summary>
        public const int ItemIdLength = 10;

        /// <summary>
        /// Default frequency.
        /// </summary>
        public const int DefaultFrequency = 1;

        private const double DefaultReadingDifficulty = -1;

        #endregion

        #region Static data tables

        private static PosInWord[][] _posInWordTrans = new PosInWord[][]
        {
            // previous   \     current syllable pause step
            // Syll pos
            //                             1                 >1        
            /*  1 */ new PosInWord[] { PosInWord.Middle, PosInWord.Tail },
            /* >1 */ new PosInWord[] { PosInWord.Head, PosInWord.Mono }
        };

        private static PosInSentence[][] _posInSentenceTrans = new PosInSentence[][]
        {
            // previous    \    current pause step
            //                           2                  3                   4                   5
            /* 2 */ new PosInSentence[] 
                    {
                        PosInSentence.L1R1,
                        PosInSentence.L1R2,
                        PosInSentence.L1R3,
                        PosInSentence.L1R4 
                    },
            /* 3 */ new PosInSentence[]
                    {
                        PosInSentence.L2R1,
                        PosInSentence.L2R2,
                        PosInSentence.L2R3,
                        PosInSentence.L2R4
                    },
            /* 4 */ new PosInSentence[]
                    {
                        PosInSentence.L34R1,
                        PosInSentence.L34R2,
                        PosInSentence.L34R34,
                        PosInSentence.L34R34
                    },
            /* 5 */ new PosInSentence[]
                    {
                        PosInSentence.L34R1,
                        PosInSentence.L34R2,
                        PosInSentence.L34R34,
                        PosInSentence.L34R34
                    }
        };

        #endregion

        #region Fields

        private string _sentence;
        private string _pronunciation;
        private int _sentenceOffsetInString;
        private int _pronunciationOffsetInString;
        private bool _needUpdateSentOffset = true;
        private bool _needUpdatePronOffset = true;
        private Collection<ScriptWord> _words = new Collection<ScriptWord>();
        private Collection<TtsUnit> _units = new Collection<TtsUnit>();

        // this design is to achive better performance,
        // since for any Sentence and Pronunciation string set operations,
        // it is not supposed to build the detailed data structures 
        // for Words and Units each time, especially when caller does not intent 
        // to call the Words and Units properties of this object.
        private bool _needRebuildWords = true;
        private bool _needRebuildUnits = true;
        private EngineType _engine = EngineType.Tts30;

        private double _readingDifficulty = DefaultReadingDifficulty;
        private Language _language = Language.Neutral;
        private string _id = string.Empty;
        private string _text;
        private int _frequency = DefaultFrequency;
        private ScriptDomainType _domain = ScriptDomainType.Unknown;
        private XmlScriptFile _scriptFile;
        private Collection<ScriptSentence> _sentences = new Collection<ScriptSentence>();
        private TtsXmlComments _ttsXmlComments = new TtsXmlComments();

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptItem"/> class.
        /// </summary>
        public ScriptItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptItem"/> class.
        /// </summary>
        /// <param name="language">Language.</param>
        public ScriptItem(Language language)
        {
            _language = language;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the sentence is question sentence.
        /// </summary>
        public bool IsQuestion
        {
            get
            {
                Helper.ThrowIfNull(Sentences);
                bool isQuestion = false;
                Sentences.ForEach(s =>
                {
                    if (s.IsQuestion)
                    {
                        isQuestion = true;
                    }
                });

                return isQuestion;
            }
        }

        /// <summary>
        /// Gets Tts xml comments.
        /// </summary>
        public TtsXmlComments TtsXmlComments
        {
            get { return _ttsXmlComments; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether text is SSML format.
        /// </summary>
        public bool IsSsml { get; set; }

        /// <summary>
        /// Gets or sets SententStartPos.
        /// </summary>
        public int PronunciationOffsetInString
        {
            get
            {
                if (_needUpdatePronOffset)
                {
                    CalculatePronunciationOffset();
                    _needUpdatePronOffset = false;
                }

                return _pronunciationOffsetInString;
            }

            set
            {
                _pronunciationOffsetInString = value;
            }
        }

        /// <summary>
        /// Gets or sets SententStartPos.
        /// </summary>
        public int SententOffsetInString
        {
            get
            {
                if (_needUpdateSentOffset)
                {
                    CalculateSentenceOffset();
                    _needUpdateSentOffset = false;
                }

                return _sentenceOffsetInString;
            }

            set
            {
                _sentenceOffsetInString = value;
            }
        }

        /// <summary>
        /// Gets or sets Sentence content for this sentence
        /// Set:
        ///     content should not be null.
        /// </summary>
        public string Sentence
        {
            get
            {
                return _sentence;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _sentence = value.Trim();
                    _needRebuildWords = true;
                    _needRebuildUnits = true;
                }
                else
                {
                    throw new ArgumentNullException("value");
                }
            }
        }

        /// <summary>
        /// Gets Plain sentence content, which is without prosody annotation, 
        /// No POS tag and only one whitespace separated words.
        /// </summary>
        public string PlainSentence
        {
            get
            {
                string plainSentence = _sentence;
                if (!IsSsml)
                {
                    plainSentence = GetCleanSetnence(_sentence);
                }

                return plainSentence;
            }
        }

        /// <summary>
        /// Gets or sets Pronunciation string for this sentence
        /// Set:
        ///     pronunciation should not be null.
        /// </summary>
        public string Pronunciation
        {
            get
            {
                return _pronunciation;
            }

            set
            {
                _needRebuildWords = true;
                _needRebuildUnits = true;
                if (!string.IsNullOrEmpty(value))
                {
                    _pronunciation = value.Trim();
                }
                else
                {
                    _units.Clear();
                    _pronunciation = string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets Unit list of the script item
        /// Exceptions:
        ///     get:
        ///         System.IO.InvalidDataException.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "Too much dependency and it is much clear here")]
        public Collection<TtsUnit> Units
        {
            get
            {
                RefreshUnits();
                return _units;
            }
        }

        /// <summary>
        /// Gets Word list of this script entry
        /// Exceptions:
        ///     get:
        ///         System.IO.InvalidDataException.
        /// </summary>
        public Collection<ScriptWord> Words
        {
            get
            {
                RefreshWords();
                return _words;
            }
        }

        /// <summary>
        /// Gets Words of all the sentences
        /// TODO: will rename it to Words when two-line script are removed.
        /// </summary>
        public Collection<ScriptWord> AllWords
        {
            get
            {
                Collection<ScriptWord> words = new Collection<ScriptWord>();
                foreach (ScriptSentence sentence in Sentences)
                {
                    foreach (ScriptWord word in sentence.Words)
                    {
                        words.Add(word);
                    }
                }

                return words;
            }
        }

        /// <summary>
        /// Gets Words type is normal of all the sentences
        /// TODO: will rename it to WithNormalWords when two-line script are removed.
        /// </summary>
        public Collection<ScriptWord> AllWordsWithNormal
        {
            get
            {
                Collection<ScriptWord> words = new Collection<ScriptWord>();
                foreach (ScriptSentence sentence in Sentences)
                {
                    foreach (ScriptWord word in sentence.Words)
                    {
                        if (word.WordType == Microsoft.Tts.Offline.WordType.Normal && !Regex.IsMatch(word.RegularText, @"[^\da-zA-Z\u4E00-\u9FFF\+\-\*\/\=]")) 
                        {
                            words.Add(word);
                        }
                    }
                }

                return words;
            }
        }

        /// <summary>
        /// Gets Normal word collection
        /// Exceptions:
        ///     get:
        ///         System.IO.InvalidDataException.
        /// </summary>
        public Collection<ScriptWord> NormalWords
        {
            get
            {
                Collection<ScriptWord> words = new Collection<ScriptWord>();
                foreach (ScriptWord word in Words)
                {
                    if (word.WordType == WordType.Normal)
                    {
                        words.Add(word);
                    }
                }

                return words;
            }
        }

        /// <summary>
        /// Gets Normal words of all the sentences
        /// TODO: will rename it to NormalWords when two-line script are removed.
        /// </summary>
        public Collection<ScriptWord> AllPronouncedNormalWords
        {
            get
            {
                Collection<ScriptWord> words = new Collection<ScriptWord>();
                foreach (ScriptSentence sentence in Sentences)
                {
                    foreach (ScriptWord word in sentence.PronouncedNormalWords)
                    {
                        words.Add(word);
                    }
                }

                return words;
            }
        }

        /// <summary>
        /// Gets Pronounced and non-silence words of all the sentences.
        /// </summary>
        public Collection<ScriptWord> AllPronouncedWords
        {
            get
            {
                Collection<ScriptWord> words = new Collection<ScriptWord>();
                foreach (ScriptSentence sentence in Sentences)
                {
                    foreach (ScriptWord word in sentence.PronouncedWords)
                    {
                        words.Add(word);
                    }
                }

                return words;
            }
        }

        /// <summary>
        /// Gets or sets Script item id
        /// Id should not be null.
        /// </summary>
        public string Id
        {
            get
            {
                return _id;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _id = value;
            }
        }

        /// <summary>
        /// Gets or sets The plain text.
        /// </summary>
        public string Text
        {
            get
            {
                return _text;
            }

            set
            {
                if (string.IsNullOrEmpty("value"))
                {
                    throw new ArgumentNullException("value");
                }

                _text = value;
            }
        }

        /// <summary>
        /// Gets or sets The domain type.
        /// </summary>
        public ScriptDomainType Domain
        {
            get { return _domain; }
            set { _domain = value; }
        }

        /// <summary>
        /// Gets or sets The reading difficulty score.
        /// </summary>
        public double ReadingDifficulty
        {
            get { return _readingDifficulty; }
            set { _readingDifficulty = value; }
        }

        /// <summary>
        /// Gets or sets The XmlScriptFile this script item belongs to.
        /// </summary>
        public XmlScriptFile ScriptFile
        {
            get { return _scriptFile; }
            set { _scriptFile = value; }
        }

        /// <summary>
        /// Gets Sentences this script item has.
        /// </summary>
        public Collection<ScriptSentence> Sentences
        {
            get { return _sentences; }
        }

        /// <summary>
        /// Gets or sets Frequency of the script item: how many times it occurs.
        /// </summary>
        public int Frequency
        {
            get
            {
                return _frequency;
            }

            set
            {
                _frequency = value;
            }
        }

        /// <summary>
        /// Gets or sets tag of the script item, this property is used for attach data to script item.
        /// </summary>
        public object Tag { get; set; }

        #endregion

        #region Virtual properties

        /// <summary>
        /// Gets or sets What kind of language is this script item.
        /// </summary>
        public virtual Language Language
        {
            get { return _language; }
            set { _language = value; }
        }

        /// <summary>
        /// Gets or sets Engine type of this script item supported.
        /// </summary>
        public EngineType Engine
        {
            get { return _engine; }
            set { _engine = value; }
        }

        /// <summary>
        /// Gets Punctuation pattern for this langauge.
        /// </summary>
        public virtual string PunctuationPattern
        {
            get
            {
                return @"(\""|/|\s'|\'$|,|\.\""$|\.$|\.\.\.|!|\?|_|;|:|\(|\)|\[|\]|\{|\}|" +
                    @"\xA1|\xBF| -| ?- |^-$|，|、|：|；|——|～|。|！|？|『|』|「|」|（|）|【|】" +
                    @"|〔|〕|《|》|〈|〉|“|”|\s‘|\‘$|\s’|\’$|¿|…|·" +
                    @"|…|／|－|！|［|］|・|．|‐)";
            }
        }

        /// <summary>
        /// Gets Pronunciation separator which this script item uses.
        /// </summary>
        public virtual PronunciationSeparator PronunciationSeparator
        {
            get { return new PronunciationSeparator("/", "-", ".", " "); }
        }

        /// <summary>
        /// Gets Minimux number of vowel phone should be included in one syllable.
        /// </summary>
        public virtual int MinVowelCountInSyllable
        {
            get
            {
                Debug.Assert(Localor.GetPhoneSet(_language) != null);
                return Localor.GetPhoneSet(_language).SyllableStructure.VowelCount.Min;
            }
        }

        /// <summary>
        /// Gets Maximun number of vowel phone should be included in one syllable.
        /// </summary>
        public virtual int MaxVowelCountInSyllable
        {
            get
            {
                Debug.Assert(Localor.GetPhoneSet(_language) != null);
                return Localor.GetPhoneSet(_language).SyllableStructure.VowelCount.Max;
            }
        }

        #endregion

        #region Word tone feature convert

        /// <summary>
        /// Get clean sentence.
        /// This assumes script is in text format.
        /// </summary>
        /// <param name="sentence">Sentence to be cleaned.</param>
        /// <returns>Cleaned sentence.</returns>
        public static string GetCleanSetnence(string sentence)
        {
            if (!string.IsNullOrEmpty(sentence))
            {
                // Remove prosody symbols if existing
                sentence = Regex.Replace(sentence, Localor.AnnotationSymbols, string.Empty);

                // Remove POS tag if existing
                sentence = Regex.Replace(sentence, @"/([a-zA-Z_]+|[0-9]+)\b", string.Empty);
                sentence = Regex.Replace(sentence, @"\s+", " ");
            }

            return sentence;
        }

        /// <summary>
        /// Convert label (single letter, [c|R|r|F|f]) to TtsWordTone.
        /// </summary>
        /// <param name="label">Label to convert.</param>
        /// <returns>TtsWordTone of the label.</returns>
        public static TtsWordTone LabelToWordTone(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                throw new ArgumentNullException("label");
            }

            Debug.Assert(Enum.GetNames(typeof(TtsWordTone)).Length == WordToneLabels.Length);
            int index = WordToneLabels.IndexOf(label, StringComparison.Ordinal);

            if (index < 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Not support word tone label [{0}] found.", label);
                throw new NotSupportedException(message);
            }

            return (TtsWordTone)index;
        }

        /// <summary>
        /// Convert TtsWordTone to label (single letter, [c|R|r|F|f]).
        /// </summary>
        /// <param name="wordTone">WordTone to convert.</param>
        /// <returns>Label of the TtsWordTone.</returns>
        public static string WordToneToLabel(TtsWordTone wordTone)
        {
            Debug.Assert(Enum.GetNames(typeof(TtsWordTone)).Length == WordToneLabels.Length);

            return WordToneLabels[(int)wordTone].ToString();
        }

        #endregion

        #region Public static methods

        /// <summary>
        /// Align offset of each unit against the given pronunciation string.
        /// </summary>
        /// <param name="pronunciation">Pronunciation to align.</param>
        /// <param name="units">Unit collection to update offset.</param>
        public static void AlignOffset(string pronunciation, Collection<TtsUnit> units)
        {
            if (string.IsNullOrEmpty(pronunciation))
            {
                throw new ArgumentNullException("pronunciation");
            }

            if (units == null)
            {
                throw new ArgumentNullException("units");
            }

            int index = 0;
            for (int i = 0; i < units.Count; i++)
            {
                TtsUnit unit = units[i];
                index = pronunciation.IndexOf(unit.MetaUnit.LeftPhone, index, StringComparison.Ordinal);
                if (index < 0)
                {
                    throw new InvalidDataException("AlignOffset failed");
                }

                unit.OffsetInString = index;
                unit.LengthInString = unit.Description.Length;
                index += unit.LengthInString;
            }
        }

        /// <summary>
        /// This method is extracted from the old method ReverseBuildSentence()
        /// Attention: Only using it for two-line scripts.
        /// </summary>
        /// <param name="words">Words to build from.</param>
        /// <returns>The built sentence.</returns>
        public static string ReverseBuildSentence(Collection<ScriptWord> words)
        {
            if (words == null)
            {
                throw new ArgumentNullException("words");
            }

            StringBuilder sb = new StringBuilder();
            foreach (ScriptWord word in words)
            {
                // Skip un-normal words
                if (word.WordType == WordType.Normal)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, " {0}", word.Description);

                    if (word.Emphasis == TtsEmphasis.Yes)
                    {
                        if (!string.IsNullOrEmpty(word.EmphasisTag))
                        {
                            sb.Append(" " + word.EmphasisTag);
                        }
                        else
                        {
                            sb.Append(" *");
                        }
                    }

                    // Not tagging default word break level or continued tone
                    if (word.Break != TtsBreak.Word || word.WordTone != TtsWordTone.Continue)
                    {
                        if (!string.IsNullOrEmpty(word.BreakTag) || !string.IsNullOrEmpty(word.WordToneTag))
                        {
                            string breakLevel = string.Empty;
                            string wordTone = string.Empty;

                            if (!string.IsNullOrEmpty(word.BreakTag))
                            {
                                Match breakMatch = Regex.Match(word.BreakTag, @"^#([0|1|2|3|4])([FfcRr]?)$");
                                Debug.Assert(breakMatch.Success);
                                breakLevel = breakMatch.Groups[1].Value;
                                wordTone = breakMatch.Groups[2].Value;
                            }
                            else if (word.Break != TtsBreak.Word)
                            {
                                breakLevel = string.Format(CultureInfo.InvariantCulture,
                                    "{0}", (int)word.Break - (int)TtsBreak.Word + 1);
                            }

                            if (!string.IsNullOrEmpty(word.WordToneTag))
                            {
                                Match wordToneMatch = Regex.Match(word.WordToneTag, @"^#([FfcRr])$");
                                if (wordToneMatch.Success)
                                {
                                    wordTone = wordToneMatch.Groups[1].Value;
                                }
                            }

                            sb.AppendFormat(" #{0}{1}", breakLevel, wordTone);
                        }
                        else
                        {
                            sb.AppendFormat(CultureInfo.InvariantCulture,
                                " #{0}", (int)word.Break - (int)TtsBreak.Word + 1);

                            if (word.WordTone != TtsWordTone.Continue)
                            {
                                sb.AppendFormat(CultureInfo.InvariantCulture,
                                    "{0}", ScriptItem.WordToneToLabel(word.WordTone));
                            }
                        }
                    }
                }
                else
                {
                    // Punctuation
                    if (!string.IsNullOrEmpty(word.PosTag))
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture,
                            " {0}/{1}", word.Grapheme, word.PosTag);
                    }
                    else
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture,
                            " {0}", word.Grapheme);
                    }
                }
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Build pronunciation string from ScriptWord collection
        /// This method is extracted from the old method ReverseBuildPronunciation()
        /// Attention: Only using it for two-line scripts.
        /// </summary>
        /// <param name="words">Words to build from.</param>
        /// <param name="useUnits">Whether usin units to build pronunciation.</param>
        /// <returns>String.</returns>
        public static string ReverseBuildPronunciation(Collection<ScriptWord> words, bool useUnits)
        {
            if (words == null)
            {
                throw new ArgumentNullException("words");
            }

            StringBuilder sb = new StringBuilder();
            foreach (ScriptWord word in words)
            {
                // skip un-normal words
                if (word.WordType != WordType.Normal)
                {
                    continue;
                }

                if (useUnits)
                {
                    word.ReverseBuildPronunciation();
                }

                if (word.Liaison == TtsLiaison.Labelled)
                {
                    sb.Append("?");
                    sb.Append(" ");
                }

                sb.Append(word.Pronunciation);
                sb.Append(Core.Pronunciation.WordBoundaryString);
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Calculate PosInSyllable feature for a given unit
        /// Change it to public for code re-use in script sentence.
        /// </summary>
        /// <param name="preUnit">Previous unit of target unit to calculate.</param>
        /// <param name="unit">Target unit to calculate.</param>
        /// <returns>PosInSyllable feature.</returns>
        public static PosInSyllable CalculatePosInSyllable(TtsUnit preUnit, TtsUnit unit)
        {
            PosInSyllable pis = PosInSyllable.Coda;

            if (unit.Feature.PosInSyllable == PosInSyllable.Onset)
            {
                pis = PosInSyllable.Onset;
                if (preUnit != null && (int)preUnit.TtsBreak <= (int)TtsBreak.Phone)
                {
                    pis = PosInSyllable.OnsetNext;
                }
            }
            else if (unit.Feature.PosInSyllable == PosInSyllable.OnsetNext)
            {
                pis = PosInSyllable.OnsetNext;
            }
            else if (unit.Feature.PosInSyllable == PosInSyllable.Coda)
            {
                pis = PosInSyllable.Coda;
                if ((int)unit.TtsBreak <= (int)TtsBreak.Phone)
                {
                    pis = PosInSyllable.CodaNext;
                }
            }
            else if (unit.Feature.PosInSyllable == PosInSyllable.CodaNext)
            {
                pis = PosInSyllable.CodaNext;
            }
            else if (unit.Feature.PosInSyllable == PosInSyllable.NucleusInV ||
                unit.Feature.PosInSyllable == PosInSyllable.NucleusInVC ||
                unit.Feature.PosInSyllable == PosInSyllable.NucleusInCV ||
                unit.Feature.PosInSyllable == PosInSyllable.NucleusInCVC)
            {
                if (preUnit != null && (int)preUnit.TtsBreak <= (int)TtsBreak.Phone)
                {
                    if ((int)unit.TtsBreak <= (int)TtsBreak.Phone)
                    {
                        pis = PosInSyllable.NucleusInCVC;
                    }
                    else
                    {
                        pis = PosInSyllable.NucleusInCV;
                    }
                }
                else
                {
                    if ((int)unit.TtsBreak <= (int)TtsBreak.Phone)
                    {
                        pis = PosInSyllable.NucleusInVC;
                    }
                    else
                    {
                        pis = PosInSyllable.NucleusInV;
                    }
                }
            }

            if (unit.MetaUnit.Special)
            {
                pis = PosInSyllable.Onset;
            }

            return pis;
        }

        /// <summary>
        /// Calculate PosInWord feature for a given syllable
        /// Change it to public for code re-use in script sentence.
        /// </summary>
        /// <param name="preSyllable">Previous syllable of target syllable to calculate.</param>
        /// <param name="syllable">Target syllable to calculate.</param>
        /// <returns>PosInWord feature.</returns>
        public static PosInWord CalculatePosInWord(ScriptSyllable preSyllable,
            ScriptSyllable syllable)
        {
            int row = (preSyllable == null ||
                (int)preSyllable.TtsBreak > (int)TtsBreak.Syllable) ? 1 : 0;
            int column = ((int)syllable.TtsBreak > (int)TtsBreak.Syllable) ? 1 : 0;
            return _posInWordTrans[row][column];
        }

        /// <summary>
        /// Calculate PosInSentence feature for a given word
        /// Change it to public for code re-use in script sentence.
        /// </summary>
        /// <param name="preWord">Previous word of target word to calculate.</param>
        /// <param name="word">Target word to calculate.</param>
        /// <returns>PosInSentence feature.</returns>
        public static PosInSentence CalculatePosInSentence(ScriptWord preWord, ScriptWord word)
        {
            int row = (int)((preWord == null) ? TtsBreak.Sentence : preWord.Break) - (int)TtsBreak.Word;
            int column = (int)word.Break - (int)TtsBreak.Word;

            // #0, a word is connected with the word following it. Like the liason in French
            // Here treat it as word boundary
            column = column < 0 ? 0 : column;
            row = row < 0 ? 0 : row;

            PosInSentence pis = _posInSentenceTrans[row][column];

            return pis;
        }

        /// <summary>
        /// Find previos word, which is normal word
        /// Change it to public for code re-use in script sentence.
        /// </summary>
        /// <param name="words">Word collection to search.</param>
        /// <param name="word">Word to find previous word for.</param>
        /// <returns>Found word.</returns>
        public static ScriptWord FindPreviousWord(Collection<ScriptWord> words, ScriptWord word)
        {
            if (words == null)
            {
                throw new ArgumentNullException("words");
            }

            if (word == null)
            {
                throw new ArgumentNullException("word");
            }

            int index = words.IndexOf(word);
            if (index == -1)
            {
                throw new ArgumentOutOfRangeException("word");
            }

            while (index - 1 >= 0)
            {
                if (words[index - 1].WordType == WordType.Normal)
                {
                    return words[index - 1];
                }

                --index;
            }

            return null;
        }

        /// <summary>
        /// Find previos syllable for a given unit through indexing
        /// Change it to public for code re-use in script sentence.
        /// </summary>
        /// <param name="units">Unit collection.</param>
        /// <param name="index">Index of the unit to find previous syllable.</param>
        /// <returns>Found syllanle.</returns>
        public static ScriptSyllable FindPreviousSyllable(Collection<TtsUnit> units, int index)
        {
            if (units == null)
            {
                throw new ArgumentNullException("units");
            }

            if (index < 0 || index >= units.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            ScriptSyllable syllable = (ScriptSyllable)units[index].Tag;
            ScriptSyllable prevSyllable = null;
            int temp = index - 1;
            while (temp >= 0)
            {
                prevSyllable = (ScriptSyllable)units[temp].Tag;
                if (prevSyllable == syllable)
                {
                    --temp;
                }
                else
                {
                    break;
                }
            }

            if (temp < 0)
            {
                prevSyllable = null;
            }

            return prevSyllable;
        }

        /// <summary>
        /// Check whether a script item is valid
        /// We don't check schema here
        /// Validation conditions: 
        /// 1. Normal word should have pronunciation 
        /// 2. Pronunciation should be good
        /// 3. POS should be in POS set
        /// We could use some flag to control the validation conditions
        /// When we need flexible control.
        /// </summary>
        /// <param name="item">The item to be checked.</param>
        /// <param name="errors">Errors if item is invalid.</param>
        /// <param name="validateSetting">Validation data set.</param>
        /// <returns>True is valid.</returns>
        public static bool IsValidItem(ScriptItem item, ErrorSet errors, XmlScriptValidateSetting validateSetting)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (errors == null)
            {
                throw new ArgumentNullException("errors");
            }

            if (validateSetting == null)
            {
                throw new ArgumentNullException("validateSetting");
            }

            validateSetting.VerifySetting();

            XmlScriptValidationScope scope = validateSetting.ValidationScope;

            bool valid = true;
            errors.Clear();

            int sentIndex = 0;
            foreach (ScriptSentence sentence in item.Sentences)
            {
                int wordIndex = 0;
                foreach (ScriptWord word in sentence.Words)
                {
                    if ((scope & XmlScriptValidationScope.Pronunciation) == XmlScriptValidationScope.Pronunciation)
                    {
                        // check pronunciation
                        string pron = null;
                        if (word.WordType == WordType.Normal)
                        {
                            pron = word.GetPronunciation(validateSetting.PhoneSet);
                        }

                        if (!string.IsNullOrEmpty(pron))
                        {
                            ErrorSet pronErrors = Core.Pronunciation.Validate(pron, validateSetting.PhoneSet);
                            foreach (Error error in pronErrors.Errors)
                            {
                                errors.Add(ScriptError.PronunciationError, error, item.Id, word.Grapheme);
                            }
                        }
                        else if (word.WordType == WordType.Normal)
                        {
                            // Pronunciation is optional for normal word, will give warning if empty pronunciation for normal word.
                            errors.Add(ScriptError.EmptyPronInNormalWord, item.Id, word.Grapheme);
                        }
                    }

                    if ((scope & XmlScriptValidationScope.POS) == XmlScriptValidationScope.POS)
                    {
                        // check pos name
                        if (!string.IsNullOrEmpty(word.PosString) &&
                            !validateSetting.PosSet.Items.ContainsKey(word.PosString))
                        {
                            errors.Add(ScriptError.UnrecognizedPos, item.Id, word.Grapheme,
                                word.Pronunciation, word.PosString);
                        }
                    }

                    string nodePath = string.Format(CultureInfo.InvariantCulture, "Sentence[{0}].Word[{1}]",
                        sentIndex, wordIndex);
                    word.IsValid(item.Id, nodePath, scope, errors);

                    wordIndex++;
                }

                sentIndex++;
            }

            if ((scope & XmlScriptValidationScope.SegmentSequence) == XmlScriptValidationScope.SegmentSequence)
            {
                CheckSegments(item, errors);
            }

            if (errors.Count > 0)
            {
                valid = false;
            }

            return valid;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Validate Item id patter.
        /// </summary>
        /// <param name="itemId">Item ID to be checked.</param>
        /// <returns>Whether item ID is valid.</returns>
        public static bool IsValidItemId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            return Regex.Match(itemId, ItemIdPattern).Success;
        }

        /// <summary>
        /// Get the domain type from domain string name.
        /// </summary>
        /// <param name="domain">Domain string name.</param>
        /// <returns>ScriptDomainType.</returns>
        public static ScriptDomainType StringToDomainType(string domain)
        {
            if (string.IsNullOrEmpty(domain))
            {
                throw new ArgumentNullException("domain");
            }

            ScriptDomainType type = ScriptDomainType.Unknown;
            switch (domain)
            {
                case "normal":
                    type = ScriptDomainType.Normal;
                    break;
                case "digit":
                    type = ScriptDomainType.Digit;
                    break;
                case "name":
                    type = ScriptDomainType.Name;
                    break;
                default:
                    string message = Helper.NeutralFormat("Unrecognized script domain name: \"{0}\"!", domain);
                    throw new InvalidDataException(message);
            }

            return type;
        }

        /// <summary>
        /// Convert script domain type to string used in script file.
        /// </summary>
        /// <param name="type">Sctipt domain type.</param>
        /// <returns>
        /// String representation of script domain type.
        /// Return empty for ScriptDomainType.Unknown.
        /// </returns>
        public static string DomainTypeToString(ScriptDomainType type)
        {
            string name = string.Empty;

            switch (type)
            {
                case ScriptDomainType.Normal:
                    name = @"normal";
                    break;
                case ScriptDomainType.Digit:
                    name = @"digit";
                    break;
                case ScriptDomainType.Name:
                    name = @"name";
                    break;
            }

            return name;
        }

        #region public operations of XML script item

        /// <summary>
        /// Write the item to xml writer.
        /// </summary>
        /// <param name="writer">XmlWriter.</param>
        /// <param name="scriptContentController">XmlScriptFile ContentControler.</param>
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

            // write <si> node and its attributes
            writer.WriteStartElement("si");
            writer.WriteAttributeString("id", Id);

            string domainName = DomainTypeToString(Domain);
            if (!string.IsNullOrEmpty(domainName))
            {
                writer.WriteAttributeString("domain", domainName);
            }

            if (Frequency != DefaultFrequency)
            {
                writer.WriteAttributeString("frequency", Frequency.ToString());
            }

            if (ReadingDifficulty > DefaultReadingDifficulty)
            {
                // We use Flesh Score ranging from 0 to 100 now.
                writer.WriteAttributeString("difficulty", string.Format(CultureInfo.InvariantCulture, "{0:F4}", ReadingDifficulty));
            }

            if (scriptContentController.SaveComments)
            {
                _ttsXmlComments.WriteToXml(writer);
            }

            // write <text> node and its content
            writer.WriteStartElement("text");

            if (IsSsml)
            {
                writer.WriteCData(Text);
            }
            else
            {
                writer.WriteString(Text);
            }

            writer.WriteEndElement();

            // write sentences
            foreach (ScriptSentence sentence in Sentences)
            {
                sentence.WriteToXml(writer, scriptContentController, scriptLanguage);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Get the Phones of this item.
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
            Collection<Phone> phones = new Collection<Phone>();
            foreach (ScriptSentence sentence in Sentences)
            {
                ErrorSet sentenceErrors = new ErrorSet();
                foreach (Phone phone in sentence.GetPhones(phoneSet, sentenceErrors))
                {
                    phones.Add(phone);
                }

                errors.Merge(sentenceErrors);
            }

            return phones;
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
            Collection<string> names = new Collection<string>();
            foreach (ScriptSentence sentence in Sentences)
            {
                ErrorSet sentenceErrors = new ErrorSet();
                foreach (string name in sentence.GetNormalPhoneNames(phoneSet, sentenceErrors))
                {
                    names.Add(name);
                }

                errors.Merge(sentenceErrors);
            }

            return names;
        }

        /// <summary>
        /// Get the syllable strings this item has
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

            Collection<string> syllables = new Collection<string>();

            foreach (ScriptSentence sentence in Sentences)
            {
                foreach (string syllable in sentence.GetSyllables(phoneSet))
                {
                    syllables.Add(syllable);
                }
            }

            return syllables;
        }

        /// <summary>
        /// Get the unit list this item has.
        /// </summary>
        /// <param name="phoneme">Phoneme.</param>
        /// <param name="sliceData">Slice data.</param>
        /// <returns>Tts units.</returns>
        public Collection<TtsUnit> GetUnits(Phoneme phoneme, SliceData sliceData)
        {
            if (phoneme == null)
            {
                throw new ArgumentNullException("phoneme");
            }

            if (sliceData == null)
            {
                throw new ArgumentNullException("sliceData");
            }

            Collection<TtsUnit> units = new Collection<TtsUnit>();
            foreach (ScriptSentence sentence in Sentences)
            {
                foreach (TtsUnit unit in sentence.GetUnits(phoneme, sliceData))
                {
                    units.Add(unit);
                }
            }

            return units;
        }

        /// <summary>
        /// Get sentence ID, sentence ID informat of: "itemID-sentenceIndex", start from 1.
        /// </summary>
        /// <param name="scriptSentence">Script sentence to get ID.</param>
        /// <returns>String.</returns>
        public string GetSentenceId(ScriptSentence scriptSentence)
        {
            if (scriptSentence == null)
            {
                throw new ArgumentNullException("scriptSentence");
            }

            if (scriptSentence.ScriptItem == null)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Empty script item detected, script sentence should belongs to one script item"));
            }

            int sentenceIndex = Sentences.IndexOf(scriptSentence);
            if (sentenceIndex < 0)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Can't find sentence in item"));
            }

            return Helper.NeutralFormat("{0}-{1}", scriptSentence.ScriptItem.Id, sentenceIndex + 1);
        }

        #endregion

        /// <summary>
        /// Parse script entry string.
        /// </summary>
        /// <param name="entry">Entry string.</param>
        public void Parse(string entry)
        {
            // entry should not be null
            if (string.IsNullOrEmpty(entry))
            {
                throw new ArgumentNullException("entry");
            }

            // only two lines are allowed. The first line contains entry id and sentence,
            // and the sencond line contains pronunciation string
            string[] lines = entry.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length != 2)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Script item is malformed, which should contain two lines, first for id and sentence; second for pronunciation.");
                throw new InvalidDataException(message);
            }

            Match m = Regex.Match(lines[0], @"(\S+)[ \t]+(.*)");
            if (!m.Success)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "The first line [{0}] of script item should contain id and sentence, for example, \"10001 Hello word.\"",
                    lines[0]);
                throw new InvalidDataException(message);
            }

            this.Id = m.Groups[1].Value;
            this.Sentence = m.Groups[2].Value;
            this.Pronunciation = lines[1];
        }

        /// <summary>
        /// Clear the intermediate data, words and units.
        /// </summary>
        public void Clear()
        {
            Words.Clear();
            Units.Clear();

            _needRebuildWords = true;
            _needRebuildUnits = true;
        }

        /// <summary>
        /// Build text from sentences.
        /// </summary>
        /// <returns>Built text.</returns>
        public string BuildTextFromSentences()
        {
            StringBuilder sb = new StringBuilder();
            foreach (ScriptSentence scriptSentence in _sentences)
            {
                if (sb.Length > 0)
                {
                    sb.Append(" ");
                }

                sb.Append(scriptSentence.Text);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Build text from all the words.
        /// </summary>
        /// <returns>Built text.</returns>
        public string BuildTextFromAllWords()
        {
            return BuildTextFromAllWords(false);
        }

        /// <summary>
        /// Build text from all the words.
        /// </summary>
        /// <param name="hasBreak">Whether text contains break.</param>
        /// <returns>Built text.</returns>
        public string BuildTextFromAllWords(bool hasBreak)
        {
            StringBuilder sb = new StringBuilder();
            foreach (ScriptWord word in AllWords)
            {
                string trimedGrapheme = string.IsNullOrEmpty(word.Grapheme) ?
                    string.Empty : word.Grapheme.Trim();
                if (string.IsNullOrEmpty(trimedGrapheme))
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append(" ");
                }

                sb.Append(trimedGrapheme);
                if (hasBreak)
                {
                    sb.Append(word.BreakSuffix);
                }
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Build pronunciation from all the words.
        /// </summary>
        /// <returns>Built pronunciation.</returns>
        public string BuildPronunciationFromAllWords()
        {
            StringBuilder sb = new StringBuilder();
            foreach (ScriptWord word in AllWords)
            {
                string trimedPron = string.IsNullOrEmpty(word.Pronunciation) ?
                    string.Empty : word.Pronunciation.Trim();
                if (string.IsNullOrEmpty(trimedPron))
                {
                    continue;
                }

                sb.Append(Core.Pronunciation.WordBoundaryString);
                sb.Append(trimedPron);
            }

            if (sb.Length > 0)
            {
                sb.Append(Core.Pronunciation.WordBoundaryString);
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Build pronunciation string from ScriptWord collection
        /// Using units to build the pronunciation.
        /// </summary>
        public void ReverseBuildPronunciation()
        {
            _pronunciation = ReverseBuildPronunciation(Words, true);
            _needRebuildUnits = true;
        }

        /// <summary>
        /// Build sentence string from ScriptWord collection.
        /// </summary>
        public void ReverseBuildSentence()
        {
            // clear this
            if (Words.Count == 0)
            {
                return;
            }

            _sentence = ReverseBuildSentence(Words);

            AlignOffset(_sentence, Words);
        }

        /// <summary>
        /// Builds in the named entities into the word list or vise versa.
        /// </summary>
        /// <param name="reverse">Flag indicating whether to reverse from word into named enitty or not.</param>
        public void BuildInNamedEntity(bool reverse)
        {
            if (reverse)
            {
                foreach (ScriptSentence sentence in Sentences)
                {
                    sentence.ConvertWordToNamedEntity();
                }
            }
            else
            {
                foreach (ScriptSentence sentence in Sentences)
                {
                    foreach (ScriptWord word in sentence.Words)
                    {
                        if (!string.IsNullOrEmpty(word.Grapheme) &&
                            word.Grapheme.IndexOfAny(" \t".ToCharArray()) >= 0)
                        {
                            throw new InvalidDataException(Helper.NeutralFormat(
                                "There should contain no whitespace in word grapheme." +
                                " However one is found in word [{0}] in script item [{1}].",
                                    word.Grapheme, Id));
                        }
                    }

                    sentence.ConvertNamedEntityToWord();
                    foreach (var word in sentence.Words)
                    {
                        // No revert back.
                        word.Grapheme = word.Grapheme.Replace(' ', '_');
                    }
                }
            }
        }

        /// <summary>
        /// Validate matching between sentence content and pronunciation line.
        /// </summary>
        public void Validate()
        {
            RefreshUnits();
        }

        #endregion

        #region Virtual methods

        /// <summary>
        /// Get all phones for this sentence's pronunciation string.
        /// </summary>
        /// <returns>Phone string collection.</returns>
        public virtual string[] GetPhones()
        {
            if (Pronunciation == null)
            {
                return null;
            }

            // Remove primary or second stress mark here
            List<string> phones = new List<string>();
            string[] slices = PronunciationSeparator.SplitSlices(Core.Pronunciation.CleanDecorate(Pronunciation));

            foreach (string slice in slices)
            {
                phones.AddRange(TtsMetaUnit.BuildPhoneNames(Language,
                    Core.Pronunciation.CleanDecorate(slice)));
            }

            return phones.ToArray();
        }

        /// <summary>
        /// Get all syllables for this sentence's pronunciation string.
        /// </summary>
        /// <returns>Syllable string array.</returns>
        public virtual string[] GetSyllables()
        {
            if (Pronunciation == null)
            {
                return null;
            }

            // Remove primary or second stress mark here
            string nonMarkedPronun = Core.Pronunciation.CleanDecorate(Pronunciation);

            return PronunciationSeparator.SplitSyllables(nonMarkedPronun);
        }

        /// <summary>
        /// Given one sentence's pronunciation string, convert
        /// One Phone-based segment file to Unit-based segment file.
        /// </summary>
        /// <param name="pronunciation">Pronunciation string.</param>
        /// <param name="filePath">Phone-based segment file.</param>
        /// <param name="targetFilePath">Unit-based segment file.</param>
        /// <returns>Data error found.</returns>
        public virtual DataError CombinePhone(string pronunciation,
            string filePath, string targetFilePath)
        {
            pronunciation = Core.Pronunciation.CleanDecorate(pronunciation);
            string[] slices = PronunciationSeparator.SplitSlices(pronunciation);
            Collection<WaveSegment> phoneSegs = SegmentFile.ReadAllData(filePath);

            DataError dataError = null;
            using (StreamWriter sw = new StreamWriter(targetFilePath))
            {
                int sliceIndex = 0;
                StringBuilder slice = new StringBuilder();
                for (int i = 0; i < phoneSegs.Count;)
                {
                    if (phoneSegs[i].IsSilenceFeature)
                    {
                        sw.WriteLine(phoneSegs[i].ToString());
                        i++;
                        continue;
                    }

                    if (sliceIndex >= slices.Length)
                    {
                        string sid = Path.GetFileNameWithoutExtension(filePath);
                        dataError = new DataError(filePath,
                            "Data does not align between phone segmentation and pronunciation in CombinePhone", sid);
                        break;
                    }

                    TtsMetaUnit ttsMetaUnit = new TtsMetaUnit(Language);
                    ttsMetaUnit.Name = slices[sliceIndex];
                    sliceIndex++;

                    // Clear first
                    slice.Remove(0, slice.Length);
                    foreach (TtsMetaPhone phone in ttsMetaUnit.Phones)
                    {
                        if (string.IsNullOrEmpty(phone.Name))
                        {
                            continue;
                        }

                        if (slice.Length > 0)
                        {
                            slice.Append("+");
                        }

                        slice.Append(phone.FullName);
                    }

                    if (slice.Length == 0)
                    {
                        continue;
                    }

                    sw.Write(phoneSegs[i].StartTime.ToString("F5", CultureInfo.InvariantCulture));
                    sw.WriteLine(" " + slice.ToString());
                    i += ttsMetaUnit.Phones.Length;
                }
            }

            if (dataError != null)
            {
                try
                {
                    File.Delete(targetFilePath);
                }
                catch (IOException ioe)
                {
                    Console.WriteLine(ioe.Message);
                }
            }

            return dataError;
        }

        #endregion

        #region ICloneable Members

        /// <summary>
        /// Clone the script item.
        /// </summary>
        /// <returns>Cloned script item.</returns>
        public ScriptItem Clone()
        {
            StringBuilder sb = new StringBuilder();
            using (XmlWriter sw = XmlWriter.Create(sb))
            {
                XmlScriptFile.ContentControler contentControler = new XmlScriptFile.ContentControler();
                contentControler.SaveComments = true;
                WriteToXml(sw, contentControler, Language);
                sw.Flush();
            }

            ScriptItem clonedScriptItem = null;
            StringReader sr = new StringReader(sb.ToString());
            try
            {
                using (XmlTextReader xtr = new XmlTextReader(sr))
                {
                    sr = null;

                    while (xtr.Read())
                    {
                        if (xtr.NodeType == XmlNodeType.Element && xtr.Name == "si")
                        {
                            clonedScriptItem = XmlScriptFile.LoadItem(xtr, null, Language);
                        }
                    }
                }
            }
            finally
            {
                if (null != sr)
                {
                    sr.Dispose();
                }
            }

            return clonedScriptItem;
        }

        #endregion

        /// <summary>
        /// Refresh sentence and pronunciation.
        /// </summary>
        public void Refresh()
        {
            if (Words.Count > 0)
            {
                ReverseBuildSentence();
            }

            if (Units.Count > 0)
            {
                ReverseBuildPronunciation();
            }
        }

        /// <summary>
        /// Convert a script item to string.
        /// </summary>
        /// <param name="hasSid">Whether has sentence ID.</param>
        /// <param name="hasPron">Whether has pronunciation.</param>
        /// <param name="writeToFile">Whether write to file.</param>
        /// <returns>String.</returns>
        public string ToString(bool hasSid, bool hasPron, bool writeToFile)
        {
            StringBuilder sb = new StringBuilder();
            if (hasSid)
            {
                if (string.IsNullOrEmpty(Id))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Empty sentence ID detected sentence [{0}], pronunciation [{1}].",
                        Sentence, Pronunciation));
                }

                sb.Append(Id + SentenceIdDelimiter);
            }

            if (string.IsNullOrEmpty(Sentence))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Empty sentence Sentence content detected SID [{0}], pronunciation [{1}].",
                    Id, Pronunciation));
            }

            sb.Append(Sentence);

            if (hasPron)
            {
                if (string.IsNullOrEmpty(Pronunciation))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Empty pronunciation detected SID [{0}] sentence [{1}].",
                        Id, Sentence));
                }

                if (writeToFile)
                {
                    sb.Append(SentenceDelimiterInFile);
                }
                else
                {
                    sb.Append(SentenceDelimiter);
                }

                if (hasSid)
                {
                    sb.Append(' ', Id.Length);
                    sb.Append(SentenceIdDelimiter);
                }

                sb.Append(Pronunciation);
            }

            return sb.ToString();
        }

        #region Override methods

        /// <summary>
        /// In string presentation.
        /// </summary>
        /// <returns>String presentation of the script item.</returns>
        public override string ToString()
        {
            return ToString(true, true, false);
        }

        #endregion

        #region Syntax building

        /// <summary>
        /// Build Word list for one sentence and its pronunciation.
        /// </summary>
        /// <param name="sentence">Sentence string.</param>
        /// <param name="pronunciation">Pronunciation string.</param>
        /// <returns>Word list.</returns>
        public Collection<ScriptWord> BuildWords(string sentence, string pronunciation)
        {
            if (string.IsNullOrEmpty(sentence))
            {
                throw new ArgumentNullException("sentence");
            }

            Collection<ScriptWord> words = new Collection<ScriptWord>();

            try
            {
                // Build work token list
                PreBuildWords(words, sentence, Language);
                AlignOffset(sentence, words);

                if (!string.IsNullOrEmpty(pronunciation))
                {
                    // Apply pronunciation string to each words
                    PostBuildWords(words, pronunciation);
                }
            }
            catch (InvalidDataException ide)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid data found while build words from sentence [{0}] and pronunciation string [{1}].",
                    sentence, pronunciation);

                throw new InvalidDataException(message, ide);
            }

            _needRebuildWords = false;

            return words;
        }

        /// <summary>
        /// Tokenize orthography sentence string
        /// Thr tokenized items are seperated by white-space.
        /// </summary>
        /// <param name="sentence">Sentence to tokenize.</param>
        /// <returns>Tokenized sentence string.</returns>
        protected virtual string TokenizeOrthographyString(string sentence)
        {
            if (Regex.Match(sentence, @"\S+/\S+").Success)
            {
                // Well format
                return sentence;
            }

            Match abbrEnding = Regex.Match(sentence, @" (\S\.)(\S\.)+$");
            if (abbrEnding.Success)
            {
                // Append ending "."
                sentence += " .";
            }

            sentence = Regex.Replace(sentence, Localor.AnnotationSymbols, " $1 ");
            sentence = Regex.Replace(sentence, PunctuationPattern, " $1 ");
            sentence = Regex.Replace(sentence, @"(?:\-\-)", " $1 ");
            sentence = Regex.Replace(sentence, @"\.\""", @". """);
            sentence = Regex.Replace(sentence, @"([^\.\s]+\S)\.", @"$1 .");

            // # Clean out extra spaces
            sentence = Regex.Replace(sentence, @"  *", @" ");
            sentence = Regex.Replace(sentence, @"^ *", string.Empty);

            return sentence;
        }

        #region Private static methods

        /// <summary>
        /// Check for segment error in a script item.
        /// </summary>
        /// <param name="item">Script item.</param>
        /// <param name="errors">Error list.</param>
        private static void CheckSegments(ScriptItem item, ErrorSet errors)
        {
            int preSegEndWord = 0;
            int preSegEndSyllable = 0;
            int preSegEndPhone = 0;

            int sentIndex = 0;
            foreach (ScriptSentence sentence in item.Sentences)
            {
                int wordIndex = 0;
                foreach (ScriptWord word in sentence.Words)
                {
                    string wordPath = string.Format(CultureInfo.InvariantCulture, "Sentence[{0}].Word[{1}]",
                        sentIndex, wordIndex);
                    if (word.HasAcousticsValue && word.Acoustics.HasSegmentInterval)
                    {
                        word.Acoustics.SegmentIntervals.ForEach(seg => CheckSegment(errors, item.Id, wordPath, seg, ref preSegEndWord));
                    }

                    int syllableIndex = 0;
                    foreach (ScriptSyllable syllable in word.Syllables)
                    {
                        string syllablePath = string.Format(CultureInfo.InvariantCulture, "{0}.Syllable[{1}]", wordPath, syllableIndex);
                        if (syllable.HasAcousticsValue && syllable.Acoustics.HasSegmentInterval)
                        {
                            syllable.Acoustics.SegmentIntervals.ForEach(seg => CheckSegment(errors, item.Id, syllablePath, seg, ref preSegEndSyllable));
                        }

                        int phoneIndex = 0;
                        foreach (ScriptPhone phone in syllable.Phones)
                        {
                            if (phone.HasAcousticsValue && phone.Acoustics.HasSegmentInterval)
                            {
                                string phonePath = string.Format(CultureInfo.InvariantCulture, "{0}.Phone[{1}]", syllablePath, phoneIndex);
                                phone.Acoustics.SegmentIntervals.ForEach(seg => CheckSegment(errors, item.Id, phonePath, seg, ref preSegEndPhone));
                            }

                            phoneIndex++;
                        }

                        syllableIndex++;
                    }

                    wordIndex++;
                }

                sentIndex++;
            }
        }

        /// <summary>
        /// Check for segment error for a specific segment.
        /// </summary>
        /// <param name="errors">Error list.</param>
        /// <param name="itemID">Script ID.</param>
        /// <param name="nodePath">Path of the node.</param>
        /// <param name="segmentInterval">Segment interval.</param>
        /// <param name="preSegEnd">Preivous segment end point.</param>
        private static void CheckSegment(ErrorSet errors, string itemID, string nodePath,
            SegmentInterval segmentInterval, ref int preSegEnd)
        {
            if (segmentInterval.Begin < preSegEnd)
            {
                string errorPath = string.Format(CultureInfo.InvariantCulture, "{0}.Acoustics", nodePath);
                errors.Add(ScriptError.SegmentSequenceError, itemID, errorPath,
                    segmentInterval.Begin.ToString(CultureInfo.InvariantCulture),
                    preSegEnd.ToString(CultureInfo.InvariantCulture));
            }

            preSegEnd = segmentInterval.End;
        }

        #endregion

        /// <summary>
        /// Align offset of each word against the given sentence string.
        /// </summary>
        /// <param name="sentence">Sentence string to align with.</param>
        /// <param name="words">Word collection to update offset.</param>
        private static void AlignOffset(string sentence, Collection<ScriptWord> words)
        {
            int index = 0;
            for (int i = 0; i < words.Count; i++)
            {
                ScriptWord word = words[i];
                if (word.WordType != WordType.Normal)
                {
                    continue;
                }

                index = sentence.IndexOf(word.Grapheme, index, StringComparison.Ordinal);
                if (index < 0)
                {
                    throw new InvalidDataException("AlignOffset words failed");
                }

                word.OffsetInString = index;
                word.LengthInString = word.Grapheme.Length;
                index += word.LengthInString;
            }
        }

        /// <summary>
        /// Adjust the normal word property with the property of
        /// The following punctuation .
        /// </summary>
        /// <param name="words">Word collection of the sentence.</param>
        private static void AdjustPunctuationProperty(Collection<ScriptWord> words)
        {
            for (int i = 1; i < words.Count; i++)
            {
                if (words[i].WordType != WordType.Normal)
                {
                    ScriptWord normalWord = FindPreviousWord(words, words[i]);
                    if (normalWord == null)
                    {
                        continue;
                    }

                    Debug.Assert(normalWord.WordType == WordType.Normal);

                    // if preview word uses default values, then apply new values
                    if ((int)normalWord.Break < (int)words[i].Break)
                    {
                        normalWord.Break = words[i].Break;
                    }

                    if ((int)normalWord.Emphasis < (int)words[i].Emphasis)
                    {
                        normalWord.Emphasis = words[i].Emphasis;
                    }
                }
            }
        }

        /// <summary>
        /// Fix the ending punctuation of the sentence, this is, to make sure 
        /// The sentence end with Period punctuation.
        /// </summary>
        /// <param name="words">Word collection of the sentence.</param>
        /// <param name="language">Language of the sentence.</param>
        private static void FixEndingPunctuation(Collection<ScriptWord> words, Language language)
        {
            if (words.Count > 0)
            {
                ScriptWord lastWord = words[words.Count - 1];
                if (lastWord.WordType == WordType.Normal)
                {
                    ScriptWord word = new ScriptWord(language);
                    word.Grapheme = ".";
                    word.WordType = WordType.Period;
                    word.Break = Localor.MapWordType2Break(word.WordType);
                    words.Add(word);
                }
                else if (lastWord.WordType == WordType.OtherPunctuation)
                {
                    // upgrade this word break level
                    lastWord.WordType = WordType.Period;
                    lastWord.Break = Localor.MapWordType2Break(lastWord.WordType);
                }
            }
        }

        /// <summary>
        /// Build syllable collection for a given word.
        /// </summary>
        /// <param name="word">Word to process.</param>
        private static void BuildSyllables(ScriptWord word)
        {
            if (word == null)
            {
                throw new ArgumentNullException("word");
            }

            if (word.Pronunciation == null)
            {
                throw new ArgumentException("word.Pronunciation is null");
            }

            word.Syllables.Clear();

            string[] syllableTexts = Core.Pronunciation.SplitIntoSyllables(word.Pronunciation);
            for (int syllableIndex = 0; syllableIndex < syllableTexts.Length; syllableIndex++)
            {
                ScriptSyllable syllable = new ScriptSyllable();

                syllable.Text = syllableTexts[syllableIndex];

                syllable.TtsBreak = (syllableIndex == syllableTexts.Length - 1) ?
                    word.Break : TtsBreak.Syllable;
                syllable.Stress = Core.Pronunciation.GetStress(syllable.Text);
                syllable.TtsEmphasis = (syllable.Stress != TtsStress.None) ?
                    word.Emphasis : TtsEmphasis.None;

                word.Syllables.Add(syllable);
            }
        }

        /// <summary>
        /// If need, rebuild unit collection from pronunciation string.
        /// </summary>
        private void RefreshUnits()
        {
            if (_needRebuildUnits)
            {
                _needRebuildUnits = false;
                _units = BuildUnits(_pronunciation, Words);
            }
        }

        /// <summary>
        /// If need, rebuild word collection from sentence and pronunciation string.
        /// </summary>
        private void RefreshWords()
        {
            if (_needRebuildWords)
            {
                _needRebuildWords = false;
                _words = BuildWords(_sentence, _pronunciation);
                _needRebuildUnits = true;
            }
        }

        /// <summary>
        /// Build unit list basing on pronunciation string and word collection.
        /// </summary>
        /// <param name="pronunciation">Pronunciation string of the script item.</param>
        /// <param name="words">Pre-built word collection.</param>
        /// <returns>Built unit collection.</returns>
        private Collection<TtsUnit> BuildUnits(string pronunciation,
            Collection<ScriptWord> words)
        {
            Collection<TtsUnit> units = new Collection<TtsUnit>();

            if (string.IsNullOrEmpty(pronunciation) != true
                && words != null && words.Count > 0)
            {
                PreBuildUnits(words, units);

                BuildUnitFeatures(words, units);

                AlignOffset(pronunciation, units);
            }
            else
            {
                // return empty units
            }

            _needRebuildUnits = false;

            return units;
        }

        /// <summary>
        /// Build unit feature for each unit.
        /// </summary>
        /// <param name="words">Word list.</param>
        /// <param name="units">Unit list .</param>
        private void BuildUnitFeatures(Collection<ScriptWord> words, Collection<TtsUnit> units)
        {
            Phoneme phoneme = Localor.GetPhoneme(Language, Engine);

            TtsUnit preUnit = null;
            ScriptSyllable preSyllable = null;
            ScriptWord preWord = null;
            TtsUnit nextUnit = null;

            for (int i = 0; i < units.Count; i++)
            {
                TtsUnit unit = units[i];

                ScriptSyllable syllable = (ScriptSyllable)unit.Tag;
                ScriptWord word = (ScriptWord)syllable.Tag;

                nextUnit = (i + 1 < units.Count) ? units[i + 1] : null;

                preUnit = (i > 0) ? units[i - 1] : null;
                preSyllable = FindPreviousSyllable(units, i);
                preWord = FindPreviousWord(words, word);

                bool unitAtWordHead = preUnit == null ||
                    word != (ScriptWord)((ScriptSyllable)preUnit.Tag).Tag;
                bool unitAtWordTail = nextUnit == null ||
                    word != (ScriptWord)((ScriptSyllable)nextUnit.Tag).Tag;

                if (preUnit == null ||
                    (unitAtWordHead && preWord != null && ((int)preWord.Break >= (int)TtsBreak.InterPhrase)) ||
                    preUnit.MetaUnit.Special)
                {
                    unit.Feature.LeftContextPhone = phoneme.TtsPhone2Id(Phoneme.SilencePhone);
                    unit.Feature.LeftContextTone = ToneManager.NoneContextTone;
                }
                else
                {
                    unit.Feature.LeftContextPhone = phoneme.TtsPhone2Id(preUnit.MetaUnit.RightPhone);
                    unit.Feature.LeftContextTone = preUnit.MetaUnit.RightTone;
                }

                if (nextUnit == null ||
                    (unitAtWordTail
                    && ((int)word.Break >= (int)TtsBreak.InterPhrase)) ||
                    nextUnit.MetaUnit.Special)
                {
                    unit.Feature.RightContextPhone = phoneme.TtsPhone2Id(Phoneme.SilencePhone);
                    unit.Feature.RightContextTone = ToneManager.NoneContextTone;
                }
                else
                {
                    unit.Feature.RightContextPhone = phoneme.TtsPhone2Id(nextUnit.MetaUnit.LeftPhone);
                    unit.Feature.RightContextTone = nextUnit.MetaUnit.LeftTone;
                }

                // adjust position in syllable
                unit.Feature.PosInSyllable = CalculatePosInSyllable(preUnit, unit);

                // syllable position in word
                unit.Feature.PosInWord = CalculatePosInWord(preSyllable, syllable);

                // word position in sentence
                unit.Feature.PosInSentence = CalculatePosInSentence(preWord, word);
                if (unit.WordType == WordType.Question)
                {
                    unit.Feature.PosInSentence = PosInSentence.Quest;
                }

                // The unit in last syllable will get the same WordTone as the word.
                if (word.Syllables.IndexOf(syllable) == word.Syllables.Count - 1)
                {
                    unit.Feature.TtsWordTone = word.WordTone;
                }
                else
                {
                    unit.Feature.TtsWordTone = TtsWordTone.Continue;
                }
            }
        }

        /// <summary>
        /// Prepare units building from given word collection.
        /// </summary>
        /// <param name="words">Word collection to build units from.</param>
        /// <param name="units">Built unit collection.</param>
        private void PreBuildUnits(Collection<ScriptWord> words, Collection<TtsUnit> units)
        {
            for (int wordIndex = 0; wordIndex < words.Count; wordIndex++)
            {
                ScriptWord word = words[wordIndex];
                if (!word.IsPronouncableNormalWord)
                {
                    continue;
                }

                // look forward one item, test whether that is '?' mark
                WordType wordType = WordType.Normal;
                while (wordIndex < words.Count - 1
                    && words[wordIndex + 1].WordType != WordType.Normal)
                {
                    // advance one more
                    if (words[wordIndex + 1].WordType == WordType.OtherPunctuation)
                    {
                        wordType = words[wordIndex + 1].WordType;
                        wordIndex++;
                    }
                    else
                    {
                        wordType = words[wordIndex + 1].WordType;
                        wordIndex++;
                        break;
                    }
                }

                word.Units.Clear();
                BuildSyllables(word);

                for (int syllableIndex = 0; syllableIndex < word.Syllables.Count; syllableIndex++)
                {
                    ScriptSyllable syllable = word.Syllables[syllableIndex];
                    syllable.Tag = word;

                    Collection<TtsUnit> syllableUnits = BuildUnits(syllable);
                    for (int i = 0; i < syllableUnits.Count; i++)
                    {
                        syllableUnits[i].WordType = wordType;
                        syllableUnits[i].Tag = syllable;
                        syllableUnits[i].Word = word;

                        word.Units.Add(syllableUnits[i]);
                        units.Add(syllableUnits[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Build unit collection for a given syllable.
        /// </summary>
        /// <param name="syllable">Syllable to process.</param>
        /// <returns>Unit collection.</returns>
        private Collection<TtsUnit> BuildUnits(ScriptSyllable syllable)
        {
            string syllableText = Core.Pronunciation.CleanDecorate(syllable.Text.Trim());

            string[] slices = PronunciationSeparator.SplitSlices(syllableText);

            PosInSyllable[] pis = EstimatePosInSyllable(slices);

            Collection<TtsUnit> units = new Collection<TtsUnit>();
            int vowelPhoneCount = 0;
            for (int sliceIndex = 0; sliceIndex < slices.Length; sliceIndex++)
            {
                string slice = slices[sliceIndex].Trim();
                if (string.IsNullOrEmpty(slice))
                {
                    continue;
                }

                TtsUnit unit = new TtsUnit(Language);

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

                Phoneme phoneme = Localor.GetPhoneme(unit.Language);
                foreach (TtsMetaPhone phone in unit.MetaUnit.Phones)
                {
                    if (phoneme.TtsVowelPhones.IndexOf(phone.Name) >= 0)
                    {
                        vowelPhoneCount++;
                    }
                }

                units.Add(unit);
            }

            if (vowelPhoneCount > MaxVowelCountInSyllable)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "There are more than {0} vowel phone in this syllable [{1}], which is supposed to contain no more than one vowel phone",
                    MaxVowelCountInSyllable, syllable.Text);
                throw new InvalidDataException(message);
            }

            return units;
        }

        /// <summary>
        /// Estimate position in syllable for each slice in a slice set.
        /// </summary>
        /// <param name="slices">Slice collection to estimate.</param>
        /// <returns>Estimated result of position in syllable.</returns>
        private PosInSyllable[] EstimatePosInSyllable(string[] slices)
        {
            PosInSyllable[] pis = new PosInSyllable[slices.Length];
            int nucleusIndex = -1;

            for (int sliceIndex = 0; sliceIndex < slices.Length; sliceIndex++)
            {
                SliceData slicedata = Localor.GetSliceData(this.Language);
                TtsMetaUnit ttsMetaUnit = new TtsMetaUnit(this.Language);
                ttsMetaUnit.Name = slices[sliceIndex];

                if (slicedata.IsNucleus(ttsMetaUnit))
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
                pis[sliceIndex] = PosInSyllable.Onset;
            }

            for (int sliceIndex = nucleusIndex + 1; sliceIndex < slices.Length; sliceIndex++)
            {
                pis[sliceIndex] = PosInSyllable.Coda;
            }

            return pis;
        }

        /// <summary>
        /// Post words build, attaching pronunciation for each word.
        /// </summary>
        /// <param name="words">Word list to process.</param>
        /// <param name="pronunciation">Sentence pronunciation string.</param>
        private void PostBuildWords(Collection<ScriptWord> words, string pronunciation)
        {
            int wordCount = 0;
            foreach (ScriptWord word in words)
            {
                if (word.WordType == WordType.Normal)
                {
                    wordCount++;
                }
            }

            // TODO : this.PronunciationSeparator.Word
            string[] pronunArray = pronunciation.Split(new string[] { PronunciationSeparator.Word },
                                        StringSplitOptions.RemoveEmptyEntries);
            pronunArray = Helper.RemoveEmptyItems(pronunArray);
            if (pronunArray.Length != wordCount)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Token misalignment, {0} pronunciation items against {1} sentence words",
                    pronunArray.Length, wordCount);
                throw new InvalidDataException(message);
            }

            int pronIndex = 0;
            for (int i = 0; i < words.Count; i++)
            {
                if (words[i].WordType != WordType.Normal)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(pronunArray[pronIndex].Trim()))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Empty pronunciation for {0}th word", i);
                    throw new InvalidDataException(message);
                }

                words[i].Pronunciation = pronunArray[pronIndex].Trim();

                if (words[i].Pronunciation.StartsWith("?", StringComparison.Ordinal))
                {
                    words[i].Liaison = TtsLiaison.Labelled;
                    words[i].Pronunciation = words[i].Pronunciation.Substring(1);
                }

                pronIndex++;
            }
        }

        /// <summary>
        /// Previos words building, performance word tokenization.
        /// </summary>
        /// <param name="words">Result word list.</param>
        /// <param name="sentence">Sentence content string.</param>
        /// <param name="language">Language of the sentence.</param>
        private void PreBuildWords(Collection<ScriptWord> words,
            string sentence, Language language)
        {
            if (words == null)
            {
                throw new ArgumentNullException("words");
            }

            if (string.IsNullOrEmpty(sentence))
            {
                throw new ArgumentNullException("sentence");
            }

            words.Clear();
            string strTokens = TokenizeOrthographyString(sentence);

            // Support case:
            // This/DET
            // This
            Match tokenMatch = Regex.Match(strTokens, @"((\S+)/(\S+))|(\S+)");

            int wordIndex = -1;
            while (tokenMatch.Success)
            {
                wordIndex = PreBuildWord(words, language, tokenMatch, wordIndex);

                tokenMatch = tokenMatch.NextMatch();
            }

            // fix ending punctuations
            FixEndingPunctuation(words, language);

            // apply punctuation property to preview word
            AdjustPunctuationProperty(words);
        }

        /// <summary>
        /// Previos word building, process one token of word.
        /// </summary>
        /// <param name="words">Result word list.</param>
        /// <param name="language">Language of the sentence.</param>
        /// <param name="tokenMatch">Token matched.</param>
        /// <param name="wordIndex">Word index of current word.</param>
        /// <returns>Word index of next word.</returns>
        private int PreBuildWord(Collection<ScriptWord> words, Language language,
            Match tokenMatch, int wordIndex)
        {
            bool tagged = true;

            // 0 is the whole "((\S+)/(\S+))|(\S+)"
            // 1 is "((\S+)/(\S+))|"
            // 2 is "(\S+)/"
            // 3 is "/(\S+)"
            // 4 is "|(\S+)"
            string content = tokenMatch.Groups[1].Value;

            if (string.IsNullOrEmpty(content))
            {
                tagged = false;
                content = tokenMatch.Groups[4].Value;
            }
            else
            {
                content = tokenMatch.Groups[2].Value;
            }

            Match breakMatch = Regex.Match(content, @"^#([0|1|2|3|4])([FfcRr]?)$");
            Match wordToneMatch = Regex.Match(content, @"^#([FfcRr])$");
            Match emphasisMatch = Regex.Match(content, @"^\*[234]?$");
            Match puncMatch = Regex.Match(content, @"^" + PunctuationPattern + "$");

            if (emphasisMatch.Success)
            {
                if (wordIndex >= 0)
                {
                    words[wordIndex].Emphasis = TtsEmphasis.Yes;
                    words[wordIndex].EmphasisTag = content;
                }
            }
            else if (wordToneMatch.Success)
            {
                if (wordIndex >= 0)
                {
                    words[wordIndex].WordTone =
                        ScriptItem.LabelToWordTone(wordToneMatch.Groups[1].Value);
                    words[wordIndex].WordToneTag = content;
                }
            }
            else if (breakMatch.Success)
            {
                if (wordIndex >= 0)
                {
                    // Upgrade one level for #1 for word break level.
                    words[wordIndex].Break =
                        (TtsBreak)(int.Parse(breakMatch.Groups[1].Value,
                                   CultureInfo.InvariantCulture) + 1);
                    words[wordIndex].BreakTag = content;

                    if (!string.IsNullOrEmpty(breakMatch.Groups[2].Value))
                    {
                        words[wordIndex].WordTone =
                            ScriptItem.LabelToWordTone(breakMatch.Groups[2].Value);
                        words[wordIndex].WordToneTag = content;
                    }
                }
            }
            else if (puncMatch.Success)
            {
                ScriptWord word = new ScriptWord(language);
                word.Grapheme = content;
                word.WordType = Localor.MapPunctuation(puncMatch.Groups[1].Value,
                    PunctuationPattern);
                word.Break = Localor.MapWordType2Break(word.WordType);
                if (tagged)
                {
                    word.PosTag = tokenMatch.Groups[3].Value;
                }

                words.Add(word);
            }
            else
            {
                ScriptWord word = new ScriptWord(language);
                word.Grapheme = content;
                if (tagged)
                {
                    word.PosTag = tokenMatch.Groups[3].Value;
                }

                word.AccessingUnits += delegate
                {
                    RefreshUnits();
                };

                words.Add(word);
                wordIndex = words.Count - 1;
            }

            return wordIndex;
        }

        #endregion

        #region other private methods

        /// <summary>
        /// Get the sentence offset in the result of ToString().
        /// </summary>
        private void CalculateSentenceOffset()
        {
            _sentenceOffsetInString = 0;
            if (!string.IsNullOrEmpty(_id))
            {
                _sentenceOffsetInString += _id.Length + SentenceIdDelimiter.Length;
            }
        }

        /// <summary>
        /// Get the pronunciation offset in the result of ToString().
        /// </summary>
        private void CalculatePronunciationOffset()
        {
            _pronunciationOffsetInString = 0;
            if (!string.IsNullOrEmpty(_id))
            {
                _pronunciationOffsetInString += (_id.Length + SentenceIdDelimiter.Length) * 2;
            }

            _pronunciationOffsetInString += _sentence.Length + SentenceDelimiter.Length;
        }

        #endregion
    }
}