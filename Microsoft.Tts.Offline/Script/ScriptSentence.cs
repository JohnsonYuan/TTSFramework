//----------------------------------------------------------------------------
// <copyright file="ScriptSentence.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script sentence class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Extensions;
    using Microsoft.Tts.Offline.Script;
    using Microsoft.Tts.Offline.Utility;
    using ScriptReviewer;

    /// <summary>
    /// Definition of script sentence types.
    /// </summary>
    public enum SentenceType
    {
        /// <summary>
        /// Unknown type.
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// Declarative.
        /// </summary>
        Declarative = 0,

        /// <summary>
        /// Yes/No question.
        /// </summary>
        YesNoQuestion,

        /// <summary>
        /// Who question.
        /// </summary>
        WhoQuestion,

        /// <summary>
        /// Exclamatory.
        /// </summary>
        Exclamatory,

        /// <summary>
        /// Imperative.
        /// </summary>
        Imperative,

        /// <summary>
        /// Hailing.
        /// </summary>
        Hailing,

        /// <summary>
        /// Single word question.
        /// </summary>
        SingleWordQuestion,

        /// <summary>
        /// Choice question.
        /// </summary>
        ChoiceQuestion,

        /// <summary>
        /// Cuteness sentence.
        /// </summary>
        Cuteness
    }

    /// <summary>
    /// The emotion category.
    /// </summary>
    [Flags]
    public enum EmotionCategory
    {
        /// <summary>
        /// Prase unknow.
        /// </summary>
        PraseUnknown = 0,

        /// <summary>
        /// Default value, does not specify the emotion.
        /// </summary>
        Neutral = (1 << 0),

        /// <summary>
        /// Sensitive emotion.
        /// </summary>
        Sensitive = (1 << 1),
       
        /// <summary>
        /// Abashed emotion, including shame, sorry, apologetic, guilt.
        /// </summary>
        Abashed = (1 << 2),
        
        /// <summary>
        /// Satisfied emotion.
        /// </summary>
        Satisfied = (1 << 3),
        
        /// <summary>
        /// Bouncy emotion.
        /// </summary>
        Bouncy = (1 << 4),
        
        /// <summary>
        /// Considerate emotion, including affectionate, kind, loving, sincere, sensitive, pity.
        /// </summary>
        Considerate = (1 << 5),
        
        /// <summary>
        /// Sorry emotion.
        /// </summary>
        Sorry = (1 << 6),
        
        /// <summary>
        /// Optimistic emotion.
        /// </summary>
        Optimistic = (1 << 7),
        
        /// <summary>
        /// Elated emotion.
        /// </summary>
        Elated = (1 << 8),
        
        /// <summary>
        /// Calm emotion.
        /// </summary>
        Calm = (1 << 9),
        
        /// <summary>
        /// Worry emotion, including afraid, anxiety, fear.
        /// </summary>
        Worry = (1 << 10),
        
        /// <summary>
        /// Happy emotion, including amuse, bouncy, excited, interested, pleased, satisfied, contentment, optimistic, elated, joy.
        /// </summary>
        Happy = (1 << 11),
        
        /// <summary>
        /// Angry emotion, including complaining.
        /// </summary>
        Angry = (1 << 12),
        
        /// <summary>
        /// Confident emotion, including resolute.
        /// </summary>
        Confident = (1 << 13),
        
        /// <summary>
        /// Disappointed emotion.
        /// </summary>
        Disappointed = (1 << 14),
        
        /// <summary>
        /// Disgust emotion, including bored, contempt, hate.
        /// </summary>
        Disgust = (1 << 15),
        
        /// <summary>
        /// Sad emotion.
        /// </summary>
        Sad = (1 << 16), 
    }

    /// <summary>
    /// Script class definition.
    /// </summary>
    public class ScriptSentence
    {
        #region Const fileds

        /// <summary>
        /// Deleted words status XML element name.
        /// </summary>
        public const string DeletedWordStatusName = "w";

        #endregion

        #region Fields

        private Language _language;
        private SentenceType _sentenceType = SentenceType.Unknown;
        private EmotionCategory _emotion = EmotionCategory.Neutral;
        private string _text;
        private Collection<ScriptWord> _words = new Collection<ScriptWord>();

        // One case can have multi accept results
        private Collection<List<ScriptWord>> _acceptSentences = new Collection<List<ScriptWord>>();
        private List<ScriptNamedEntity> _namedEntities = new List<ScriptNamedEntity>();
        private Dictionary<ScriptWord, TtsXmlStatus> _deletedWordsDict =
            new Dictionary<ScriptWord, TtsXmlStatus>();

        private Dictionary<ScriptWord, ScriptWord> _deletedWordAfterWordDict =
            new Dictionary<ScriptWord, ScriptWord>();

        private ScriptItem _scriptItem;
        private TtsXmlComments _ttsXmlComments = new TtsXmlComments();

        // Following are used for units
        private Collection<TtsUnit> _units = new Collection<TtsUnit>();

        private Collection<ScriptIntonationPhrase> _intonationPhrases =
            new Collection<ScriptIntonationPhrase>();

        private bool _needBuildUnits = true;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the ScriptSentence class.
        /// </summary>
        public ScriptSentence()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ScriptSentence class.
        /// </summary>
        /// <param name="language">Language of the sentence.</param>
        public ScriptSentence(Language language)
        {
            _language = language;
        }

        /// <summary>
        /// Initializes a new instance of the ScriptSentence class.
        /// </summary>
        /// <param name="language">Language of the sentence.</param>
        /// <param name="text">The text sentence.</param>
        public ScriptSentence(Language language, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException("text");
            }

            _language = language;
            Text = text;
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
                return SentenceType.IsQuestion();
            }
        }

        /// <summary>
        /// Gets the script phones this sentence has.
        /// </summary>
        public Collection<ScriptPhone> ScriptPhones
        {
            get
            {
                Collection<ScriptPhone> phones = new Collection<ScriptPhone>();

                foreach (ScriptWord word in Words)
                {
                    Collection<ScriptPhone> wordPhones = word.ScriptPhones;
                    foreach (ScriptPhone phone in wordPhones)
                    {
                        phones.Add(phone);
                    }
                }

                return phones;
            }
        }

        /// <summary>
        /// Gets deleted word after word dictionary, each deleted words has been attached to one
        /// Word in the script sentence, if revert the deleted word, use this dictionary to find
        /// The deleted word's postion to revert.
        /// </summary>
        public Dictionary<ScriptWord, ScriptWord> DeletedWordAndFollowingWordDict
        {
            get { return _deletedWordAfterWordDict; }
        }

        /// <summary>
        /// Gets deleted word dictionary.
        /// </summary>
        public Dictionary<ScriptWord, TtsXmlStatus> DeletedWordsDict
        {
            get { return _deletedWordsDict; }
        }

        /// <summary>
        /// Gets TTS xml comments.
        /// </summary>
        public TtsXmlComments TtsXmlComments
        {
            get { return _ttsXmlComments; }
        }

        /// <summary>
        /// Gets or sets language.
        /// </summary>
        public Language Language
        {
            get { return _language; }
            set { _language = value; }
        }

        /// <summary>
        /// Gets or sets the text of this Sentence.
        /// </summary>
        public string Text
        {
            get
            {
                return _text;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _text = value;
            }
        }

        /// <summary>
        /// Gets or sets sentence type.
        /// </summary>
        public SentenceType SentenceType
        {
            get { return _sentenceType; }
            set { _sentenceType = value; }
        }

        /// <summary>
        /// Gets or sets sentence emotion type.
        /// </summary>
        public EmotionCategory Emotion
        {
            get { return _emotion; }
            set { _emotion = value; }
        }

        /// <summary>
        /// Gets or sets the script item this sentence belongs to.
        /// </summary>
        public ScriptItem ScriptItem
        {
            get { return _scriptItem; }
            set { _scriptItem = value; }
        }

        /// <summary>
        /// Gets the word list this sentence contains.
        /// </summary>
        public Collection<ScriptWord> Words
        {
            get { return _words; }
        }

        /// <summary>
        /// Gets the acceptable results (contains several words) for this sentence.
        /// </summary>
        public Collection<List<ScriptWord>> AcceptSentences
        {
            get { return _acceptSentences; }
        }

        /// <summary>
        /// Gets the non-empty word list this sentence contains.
        /// </summary>
        public Collection<ScriptWord> TextWords
        {
            get
            {
                Helper.ThrowIfNull(_words);
                return new Collection<ScriptWord>(_words.Where(
                    w => w.IsTextWord).ToList());
            }
        }

        /// <summary>
        /// Gets the named entity list this sentence has.
        /// </summary>
        public List<ScriptNamedEntity> NamedEntities
        {
            get { return _namedEntities; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether need build units again.
        /// </summary>
        public bool NeedBuildUnits
        {
            get { return _needBuildUnits; }
            set { _needBuildUnits = value; }
        }

        /// <summary>
        /// Gets the normal words this sentence has.
        /// </summary>
        public Collection<ScriptWord> PronouncedNormalWords
        {
            get
            {
                return new Collection<ScriptWord>(Words.Where(s => s.IsPronouncableNormalWord).ToArray());
            }
        }

        /// <summary>
        /// Gets the pronounced and non-silence words this sentence has.
        /// </summary>
        public Collection<ScriptWord> PronouncedWords
        {
            get
            {
                Collection<ScriptWord> words = new Collection<ScriptWord>();
                foreach (ScriptWord word in Words)
                {
                    if (word.IsPronounced)
                    {
                        words.Add(word);
                    }
                }

                return words;
            }
        }

        /// <summary>
        /// Gets intonation phrases.
        /// </summary>
        public Collection<ScriptIntonationPhrase> IntonationPhrases
        {
            get
            {
                if (_intonationPhrases.Count() == 0)
                {
                    BuildIntonationPhrases();
                }

                return _intonationPhrases;
            }
        }

        #endregion

        #region public static operations

        /// <summary>
        /// Convert EmotionType to string used in script file.
        /// </summary>
        /// <param name="emotionType">EmotionType.</param>
        /// <returns> String representation of EmotionType.</returns>
        public static string EmotionTypeToString(EmotionCategory emotionType)
        {
            string emotionStr = string.Empty;

            switch (emotionType)
            {
                case EmotionCategory.Abashed:
                    emotionStr = "abashed";
                    break;
                case EmotionCategory.Angry:
                    emotionStr = "angry";
                    break;
                case EmotionCategory.Bouncy:
                    emotionStr = "bouncy";
                    break;
                case EmotionCategory.Calm:
                    emotionStr = "calm";
                    break;
                case EmotionCategory.Confident:
                    emotionStr = "confident";
                    break;
                case EmotionCategory.Considerate:
                    emotionStr = "considerate";
                    break;
                case EmotionCategory.Disappointed:
                    emotionStr = "disappointed";
                    break;
                case EmotionCategory.Disgust:
                    emotionStr = "disgust";
                    break;
                case EmotionCategory.Elated:
                    emotionStr = "elated";
                    break;
                case EmotionCategory.Happy:
                    emotionStr = "happy";
                    break;
                case EmotionCategory.Neutral:
                    break;
                case EmotionCategory.Optimistic:
                    emotionStr = "optimistic";
                    break;
                case EmotionCategory.PraseUnknown:
                    emotionStr = "praseUnknown";
                    break;
                case EmotionCategory.Sad:
                    emotionStr = "sad";
                    break;
                case EmotionCategory.Satisfied:
                    emotionStr = "satisfied";
                    break;
                case EmotionCategory.Sensitive:
                    emotionStr = "sensitive";
                    break;
                case EmotionCategory.Sorry:
                    emotionStr = "sorry";
                    break;
                case EmotionCategory.Worry:
                    emotionStr = "worry";
                    break;
                default:
                    string message = Helper.NeutralFormat("Unrecognized emotion type: \"{0}\"!", Convert.ToString(emotionType));
                    throw new InvalidDataException(message);
            }

            return emotionStr;
        }

        /// <summary>
        /// Convert string used in script file to EmotionType.
        /// </summary>
        /// <param name="emotionName">Emotion string used in script file.</param>
        /// <returns>Emotion type.</returns>
        public static EmotionCategory StringToEmotionType(string emotionName)
        {
            if (string.IsNullOrEmpty(emotionName))
            {
                throw new ArgumentNullException("emotion");
            }

            EmotionCategory emotionType = EmotionCategory.Neutral;
            switch (emotionName)
            {
                case "abashed":
                    emotionType = EmotionCategory.Abashed;
                    break;
                case "angry":
                    emotionType = EmotionCategory.Angry;
                    break;
                case "bouncy":
                    emotionType = EmotionCategory.Bouncy;
                    break;
                case "calm":
                    emotionType = EmotionCategory.Calm;
                    break;
                case "confident":
                    emotionType = EmotionCategory.Confident;
                    break;
                case "considerate":
                    emotionType = EmotionCategory.Considerate;
                    break;
                case "disappointed":
                    emotionType = EmotionCategory.Disappointed;
                    break;
                case "disgust":
                    emotionType = EmotionCategory.Disgust;
                    break;
                case "elated":
                    emotionType = EmotionCategory.Elated;
                    break;
                case "happy":
                    emotionType = EmotionCategory.Happy;
                    break;
                case "optimistic":
                    emotionType = EmotionCategory.Optimistic;
                    break;
                case "praseUnknown":
                    emotionType = EmotionCategory.PraseUnknown;
                    break;
                case "sad":
                    emotionType = EmotionCategory.Sad;
                    break;
                case "satisfied":
                    emotionType = EmotionCategory.Satisfied;
                    break;
                case "sensitive":
                    emotionType = EmotionCategory.Sensitive;
                    break;
                case "sorry":
                    emotionType = EmotionCategory.Sorry;
                    break;
                case "worry":
                    emotionType = EmotionCategory.Worry;
                    break;
                default:
                    string message = Helper.NeutralFormat("Unrecognized emotion type: \"{0}\"!", emotionName);
                    throw new InvalidDataException(message);
            }

            return emotionType;
        }

        /// <summary>
        /// Get the sentence type according to type name in script file.
        /// </summary>
        /// <param name="name">Type name.</param>
        /// <returns>SentenceType.</returns>
        public static SentenceType StringToSentenceType(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            SentenceType type = SentenceType.Unknown;
            switch (name)
            {
                case "declarative":
                    type = SentenceType.Declarative;
                    break;
                case "ynq":
                    type = SentenceType.YesNoQuestion;
                    break;
                case "whq":
                    type = SentenceType.WhoQuestion;
                    break;
                case "exclam":
                    type = SentenceType.Exclamatory;
                    break;
                case "imperative":
                    type = SentenceType.Imperative;
                    break;
                case "hail":
                    type = SentenceType.Hailing;
                    break;
                case "swq":
                    type = SentenceType.SingleWordQuestion;
                    break;
                case "choiceques":
                    type = SentenceType.ChoiceQuestion;
                    break;
                case "cuteness":
                    type = SentenceType.Cuteness;
                    break;
                default:
                    string message = Helper.NeutralFormat("Unrecognized sentence type name: \"{0}\"!", name);
                    throw new InvalidDataException(message);
            }

            return type;
        }

        /// <summary>
        /// Convert sentence type to string used in script file.
        /// </summary>
        /// <param name="type">Sentence type.</param>
        /// <returns>String representation of sentence type.</returns>
        public static string SentenceTypeToString(SentenceType type)
        {
            string name = string.Empty;

            switch (type)
            {
                case SentenceType.Declarative:
                    name = @"declarative";
                    break;
                case SentenceType.YesNoQuestion:
                    name = @"ynq";
                    break;
                case SentenceType.WhoQuestion:
                    name = @"whq";
                    break;
                case SentenceType.Exclamatory:
                    name = @"exclam";
                    break;
                case SentenceType.Imperative:
                    name = @"imperative";
                    break;
                case SentenceType.Hailing:
                    name = @"hail";
                    break;
                case SentenceType.SingleWordQuestion:
                    name = @"swq";
                    break;
                case SentenceType.ChoiceQuestion:
                    name = @"choiceques";
                    break;
                case SentenceType.Cuteness:
                    name = @"cuteness";
                    break;
            }

            return name;
        }

        #endregion

        #region public operations

        /// <summary>
        /// Get intonation phrase of the word.
        /// </summary>
        /// <param name="word">Script word.</param>
        /// <returns>Intonation phrase.</returns>
        public ScriptIntonationPhrase GetIntonationPhrase(ScriptWord word)
        {
            ScriptIntermediatePhrase intermediatePhrase = GetIntermediatePhrase(word);
            return intermediatePhrase == null ? null : intermediatePhrase.IntonationPhrase;
        }

        /// <summary>
        /// Get intermediate phrase.
        /// </summary>
        /// <param name="word">Script word.</param>
        /// <returns>Intermediate phrase.</returns>
        public ScriptIntermediatePhrase GetIntermediatePhrase(ScriptWord word)
        {
            ScriptIntermediatePhrase intermediatePhrase = null;
            foreach (ScriptIntonationPhrase phrase in IntonationPhrases)
            {
                intermediatePhrase = phrase.GetIntermediatePhrase(word);
                if (intermediatePhrase != null)
                {
                    break;
                }
            }

            return intermediatePhrase;
        }

        /// <summary>
        /// Build intonation phrases.
        /// </summary>
        public void BuildIntonationPhrases()
        {
            _intonationPhrases.Clear();
            Collection<ScriptWord> words = new Collection<ScriptWord>();
            for (int i = 0; i < _words.Count; i++)
            {
                ScriptWord word = _words[i];

                // Append non-normal word to the intonation break phrase.
                words.Add(word);
                if (word.IsPronouncableNormalWord &&
                    (word.Break >= TtsBreak.IntonationPhrase ||
                    _words.IndexOf(word) == (_words.Count - 1)))
                {
                    for (int j = i + 1; j < _words.Count; j++)
                    {
                        if (_words[j].IsPronouncableNormalWord)
                        {
                            break;
                        }
                        else
                        {
                            words.Add(_words[j]);
                            i = j;
                        }
                    }

                    ScriptIntonationPhrase phrase = new ScriptIntonationPhrase()
                    {
                        Sentence = this,
                    };

                    phrase.Parse(words);
                    _intonationPhrases.Add(phrase);
                    words.Clear();
                }
            }

            if (words.Count > 0)
            {
                ScriptIntonationPhrase phrase = new ScriptIntonationPhrase()
                {
                    Sentence = this,
                };

                phrase.Parse(words);
                _intonationPhrases.Add(phrase);
                words.Clear();
            }
        }

        /// <summary>
        /// Converts named entities to words.
        /// </summary>
        public void ConvertNamedEntityToWord()
        {
            RefreshNamedEntityRangeIndex();
            _namedEntities = new List<ScriptNamedEntity>(NamedEntities.SortBy(e => e.StartIndex));
            List<ScriptNamedEntity> conversionList = NamedEntities;
            while (IsNamedEntityOverlapping(conversionList))
            {
                Trace.WriteLine(Helper.NeutralFormat(
                    "Overlapped named entities are identified as [{0}].",
                    NamedEntities.Select(e => Helper.NeutralFormat("{0}, {1}, {2}", e.StartIndex, e.EndIndex, e.Text)).Concatenate(" / ")));

                conversionList = PickNoOverlapScriptNamedEntities(conversionList);
            }

            for (int i = conversionList.Count - 1; i >= 0; i--)
            {
                ScriptNamedEntity entity = conversionList[i];
                ScriptWord word = ToScriptWord(entity);

                Words.Insert(Words.IndexOf(entity.Start), word);
                for (int j = Words.IndexOf(entity.End) + 1; j > Words.IndexOf(entity.Start); j--)
                {
                    Words.RemoveAt(j);
                }

                NamedEntities.Remove(entity);
            }
        }

        /// <summary>
        /// Converts proper words in the word list into named entities.
        /// </summary>
        public void ConvertWordToNamedEntity()
        {
            for (int i = Words.Count - 1; i >= 0; i--)
            {
                ScriptWord currWord = Words[i];
                if (!string.IsNullOrEmpty(currWord.NamedEntityTypeString) &&
                    currWord.SubWords != null && currWord.SubWords.Count > 0)
                {
                    ScriptNamedEntity entity = ToScriptNamedEntity(currWord);
                    AddNamedEntity(entity);

                    ScriptWord lastSubWord = currWord.SubWords[currWord.SubWords.Count - 1];
                    lastSubWord.Break = currWord.Break;

                    for (int j = currWord.SubWords.Count - 1; j >= 0; j--)
                    {
                        Words.Insert(i, currWord.SubWords[j]);
                    }

                    Words.Remove(currWord);
                }
            }

            RefreshNamedEntityRangeIndex();
        }

        /// <summary>
        /// Delete word from the sentence.
        /// For example: A, B, C, D, E.
        /// Current status: C, and D have been deleted.then word after word dict store: C->D, D->E.
        /// After deleting B, then deleted word after word will store: B->C, C->D, D->E.
        /// </summary>
        /// <param name="scriptWord">Word to be deleted.</param>
        /// <returns>Deleted word position.</returns>
        public int DeleteWord(ScriptWord scriptWord)
        {
            if (scriptWord == null)
            {
                throw new ArgumentNullException("scriptWord");
            }

            int position = _words.IndexOf(scriptWord);

            if (position >= 0)
            {
                _words.Remove(scriptWord);
                _needBuildUnits = true;
            }

            return position;
        }

        /// <summary>
        /// Insert word to the position.
        /// </summary>
        /// <param name="scriptWord">Word to be insert.</param>
        /// <param name="position">Position to be inserted.</param>
        public void InsertWord(ScriptWord scriptWord, int position)
        {
            if (scriptWord == null)
            {
                throw new ArgumentNullException("scriptWord");
            }

            if (position < 0 || position > _words.Count)
            {
                throw new ArgumentException(Helper.NeutralFormat(
                    "Invalid position {0}, should between 0 and {1}.", position, _words.Count));
            }

            _words.Insert(position, scriptWord);
            scriptWord.Sentence = this;
            _needBuildUnits = true;
        }

        /// <summary>
        /// Adds a new named entity instance into the list, while avoid the duplicated ones.
        /// </summary>
        /// <param name="entity">The named entity instance to add.</param>
        /// <returns>Whether succeeded added.</returns>
        public bool AddNamedEntity(ScriptNamedEntity entity)
        {
            bool existed = false;
            foreach (var item in NamedEntities)
            {
                if (item.Equals(entity))
                {
                    existed = true;
                    break;
                }
            }

            if (!existed)
            {
                NamedEntities.Add(entity);
                _namedEntities = NamedEntities.SortBy(e => e.StartIndex).ToList();
            }

            return !existed;
        }

        /// <summary>
        /// Build sentence from word list.
        /// </summary>
        /// <returns>Sentence string.</returns>
        public string BuildTextFromWords()
        {
            StringBuilder sb = new StringBuilder();

            foreach (ScriptWord word in _words)
            {
                if (word.WordType == WordType.Normal || word.WordType == WordType.Punctuation)
                {
                    sb.AppendFormat("{0}{1}", " ", word.Grapheme);
                }
                else
                {
                    sb.Append(word.Grapheme);
                }
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Build pronunciation from sentence's word list.
        /// </summary>
        /// <returns>Pronunciation of the words.</returns>
        public string BuildPronFromWords()
        {
            StringBuilder sb = new StringBuilder();

            if (_words.Count > 0)
            {
                sb.Append(Core.Pronunciation.WordBoundaryString);
                foreach (ScriptWord word in _words)
                {
                    if (word.WordType == WordType.Normal)
                    {
                        // In current offline desing, we need support unit, so use "word.Pronunciation" to get pronunciation
                        // with unit boundary. We planned to remove unit boundary in the furture, when unit boundary removed,
                        // need call "word.GetPronunciation" to get the pronunciation, the pronunciation may come from syllable list
                        // or phone list.
                        if (string.IsNullOrEmpty(word.Pronunciation))
                        {
                            continue;
                        }

                        sb.Append(word.Pronunciation);
                        sb.Append(Core.Pronunciation.WordBoundaryString);
                    }
                }
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Get intermediate phrases of this sentence.
        /// </summary>
        /// <returns>The intermediate phrases this sentence has.</returns>
        public Collection<Collection<ScriptWord>> GetIntermediatePhrases()
        {
            Collection<Collection<ScriptWord>> intermediatePhrases = new Collection<Collection<ScriptWord>>();
            Collection<ScriptWord> intermediatePhrase = new Collection<ScriptWord>();
            foreach (ScriptWord word in PronouncedNormalWords)
            {
                intermediatePhrase.Add(word);
                if (word.Break >= TtsBreak.InterPhrase)
                {
                    intermediatePhrases.Add(intermediatePhrase);
                    intermediatePhrase = new Collection<ScriptWord>();
                }
            }

            if (intermediatePhrase.Count != 0)
            {
                intermediatePhrases.Add(intermediatePhrase);
            }

            return intermediatePhrases;
        }

        /// <summary>
        /// Get the Phones of this sentence.
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
            foreach (ScriptWord word in Words)
            {
                ErrorSet wordErrors = new ErrorSet();
                foreach (Phone phone in word.GetPhones(phoneSet, wordErrors))
                {
                    phones.Add(phone);
                }

                errors.Merge(wordErrors);
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
            foreach (ScriptWord word in Words)
            {
                ErrorSet wordErrors = new ErrorSet();
                foreach (string name in word.GetNormalPhoneNames(phoneSet, wordErrors))
                {
                    names.Add(name);
                }

                errors.Merge(wordErrors);
            }

            return names;
        }

        /// <summary>
        /// Get the syllable strings this sentence has
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

            foreach (ScriptWord word in Words)
            {
                foreach (string syllable in word.GetSyllables(phoneSet))
                {
                    syllables.Add(syllable);
                }
            }

            return syllables;
        }

        /// <summary>
        /// Get the unit list this sentence has.
        /// </summary>
        /// <param name="phoneme">Phoneme.</param>
        /// <param name="sliceData">Slice data.</param>
        /// <returns>Tts units.</returns>
        public Collection<TtsUnit> GetUnits(Phoneme phoneme, SliceData sliceData)
        {
            return GetUnits(phoneme, sliceData, true);
        }

        /// <summary>
        /// Get the unit list this sentence has.
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

            if (_needBuildUnits)
            {
                BuildUnits(phoneme, sliceData, buildUnitFeature);
                _needBuildUnits = false;
            }

            return _units;
        }

        /// <summary>
        /// Write sentence to xml.
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

            if (scriptContentController.SaveComments)
            {
                WriteDeletedWordsToComments(scriptLanguage);
            }

            // write <sent> node and its attributes
            writer.WriteStartElement("sent");

            string sentenceTypeName = SentenceTypeToString(SentenceType);
            if (!string.IsNullOrEmpty(sentenceTypeName))
            {
                writer.WriteAttributeString("type", sentenceTypeName);
            }

            string emotionType = EmotionTypeToString(Emotion);
            if (!string.IsNullOrEmpty(emotionType))
            {
                writer.WriteAttributeString("emotion", emotionType);
            }

            if (scriptContentController.SaveComments)
            {
                _ttsXmlComments.WriteToXml(writer);
            }

            // write <text> node and its content
            writer.WriteStartElement("text");
            writer.WriteString(Text);
            writer.WriteEndElement();

            // write words
            writer.WriteStartElement("words");
            foreach (ScriptWord word in Words)
            {
                word.WriteToXml(writer, scriptContentController, scriptLanguage);
            }

            writer.WriteEndElement();

            // write multi accept
            foreach (List<ScriptWord> acceptSent in AcceptSentences)
            {
                writer.WriteStartElement("accept");
                foreach (ScriptWord accept in acceptSent)
                {
                    accept.WriteToXml(writer, scriptContentController, scriptLanguage);
                }

                writer.WriteEndElement();
            }

            if (NamedEntities.Count > 0)
            {
                writer.WriteStartElement("nes");
                foreach (ScriptNamedEntity entity in NamedEntities)
                {
                    entity.WriteToXml(writer, scriptContentController);
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Remove the specified named entity from script sentence.
        /// </summary>
        /// <param name="namedEntityName">Named entity name to be removed.</param>
        public void RemoveNamedEntity(string namedEntityName)
        {
            Helper.ThrowIfNull(namedEntityName);
            List<ScriptNamedEntity> toRemoved =
                NamedEntities.Where(e => namedEntityName.Equals(
                e.Type, StringComparison.OrdinalIgnoreCase)).ToList();
            toRemoved.ForEach(e => NamedEntities.Remove(e));
        }

        #endregion

        #region private operation

        /// <summary>
        /// Pick no overlap ones.
        /// </summary>
        /// <param name="entities">The entities.</param>
        /// <returns>The script named entity list.</returns>
        private static List<ScriptNamedEntity> PickNoOverlapOnes(List<ScriptNamedEntity> entities)
        {
            List<ScriptNamedEntity> ret = new List<ScriptNamedEntity>();

            for (int i = 0; i < entities.Count - 2; i++)
            {
                if (entities[i].EndIndex >= entities[i + 1].StartIndex)
                {
                    if (entities[i].Count >= entities[i + 1].Count)
                    {
                        ret.Add(entities[i]);
                    }
                    else
                    {
                        ret.Add(entities[i + 1]);
                        i++;
                    }
                }
                else
                {
                    ret.Add(entities[i]);
                }
            }

            return ret;
        }

        /// <summary>
        /// Tells whether named entities in current list have overlapped word cover.
        /// </summary>
        /// <param name="entities">The entities.</param>
        /// <returns>True if finding overlap, false otherwise.</returns>
        private static bool IsNamedEntityOverlapping(List<ScriptNamedEntity> entities)
        {
            bool overlap = false;
            for (int i = 0; i < entities.Count - 2; i++)
            {
                if (entities[i].EndIndex >= entities[i + 1].StartIndex)
                {
                    overlap = true;
                    break;
                }
            }

            return overlap;
        }

        /// <summary>
        /// Pick no overlap named entities.
        /// </summary>
        /// <param name="entities">Script named entities.</param>
        /// <returns>Picked no overlap named entities.</returns>
        private static List<ScriptNamedEntity> PickNoOverlapScriptNamedEntities(List<ScriptNamedEntity> entities)
        {
            List<ScriptNamedEntity> ret = new List<ScriptNamedEntity>();

            for (int i = 0; i < entities.Count - 2; i++)
            {
                if (entities[i].EndIndex >= entities[i + 1].StartIndex)
                {
                    if (entities[i].Count >= entities[i + 1].Count)
                    {
                        ret.Add(entities[i]);
                    }
                    else
                    {
                        ret.Add(entities[i + 1]);
                        i++;
                    }
                }
                else
                {
                    ret.Add(entities[i]);
                }
            }

            return ret;
        }

        /// <summary>
        /// Converts one word instance to script named entity.
        /// </summary>
        /// <param name="word">The word instance to convert.</param>
        /// <returns>The converted script named entity instance.</returns>
        private ScriptNamedEntity ToScriptNamedEntity(ScriptWord word)
        {
            Helper.ThrowIfNull(word);
            if (string.IsNullOrEmpty(word.NamedEntityTypeString))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "The type of the named entity [{0}] should not be empty.", word.NamedEntityTypeString));
            }

            ScriptNamedEntity entity = new ScriptNamedEntity();

            entity.Text = word.Grapheme;
            entity.PosString = word.PosString;
            entity.Type = word.NamedEntityTypeString;

            entity.Start = word.SubWords[0];
            entity.End = word.SubWords[word.SubWords.Count - 1];

            return entity;
        }

        /// <summary>
        /// Converts one named entity to script word instance.
        /// </summary>
        /// <param name="entity">The named entity instance to convert.</param>
        /// <returns>The converted word instance.</returns>
        private ScriptWord ToScriptWord(ScriptNamedEntity entity)
        {
            Helper.ThrowIfNull(entity);
            if (string.IsNullOrEmpty(entity.Type))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "The type of the named entity [{0}] should not be empty.", entity.Text));
            }

            ScriptWord word = new ScriptWord(entity.Start.Language);

            word.Break = entity.End.Break;

            word.Grapheme = entity.Text;
            word.PosString = entity.PosString;

            if (word.PosString == ScriptNamedEntity.DefaultEmptyPosString)
            {
                word.PosString = ScriptNamedEntity.DefaultEntityPosString;
            }

            word.NamedEntityTypeString = entity.Type;
            word.Sentence = entity.Start.Sentence;

            word.SubWords = new Collection<ScriptWord>();
            StringBuilder pronunciation = new StringBuilder();
            for (int i = Words.IndexOf(entity.Start); i <= Words.IndexOf(entity.End); i++)
            {
                word.SubWords.Add(Words[i]);
                if (!string.IsNullOrEmpty(Words[i].Pronunciation))
                {
                    if (pronunciation.Length != 0)
                    {
                        pronunciation.AppendFormat(" {0} ", Pronunciation.WordPronBoundaryString);
                    }

                    pronunciation.Append(Words[i].Pronunciation);
                }
            }

            word.Pronunciation = pronunciation.ToString();

            return word;
        }

        /// <summary>
        /// Refreshes the range indexes of the named entities in this sentence.
        /// </summary>
        private void RefreshNamedEntityRangeIndex()
        {
            foreach (var entity in NamedEntities)
            {
                Debug.Assert(entity.Start != null, "Start word of the entity should not be null");
                Debug.Assert(entity.End != null, "End word of the entity should not be null");
                entity.Text = TextWords.Skip(entity.StartIndex)
                    .Take(entity.EndIndex - entity.StartIndex + 1).Select(w => w.Grapheme).Concatenate(" ");
            }
        }

        /// <summary>
        /// Write dleeted words to comments.
        /// </summary>
        /// <param name="scriptLanguage">Script language.</param>
        private void WriteDeletedWordsToComments(Language scriptLanguage)
        {
            if (TtsXmlComments.TtsXmlStatusDict.ContainsKey(DeletedWordStatusName))
            {
                TtsXmlComments.TtsXmlStatusDict.Remove(DeletedWordStatusName);
            }

            foreach (ScriptWord scriptWord in _deletedWordsDict.Keys)
            {
                scriptWord.TtsXmlComments.Reset();

                // Update deleted words' original content
                StringBuilder sb = new StringBuilder();

                scriptWord.TtsXmlComments.TtsXmlStatusDict.Clear();
                using (XmlWriter sw = XmlWriter.Create(sb))
                {
                    XmlScriptFile.ContentControler scriptContentController = new XmlScriptFile.ContentControler();
                    scriptContentController.SaveComments = true;
                    scriptWord.WriteToXml(sw, scriptContentController, scriptLanguage);
                    sw.Flush();
                    _deletedWordsDict[scriptWord].OriginalValue = sb.ToString();
                    _deletedWordsDict[scriptWord].Position = XmlScriptCommentHelper.GetDeletedWordPosition(scriptWord);
                    _deletedWordsDict[scriptWord].DelIndex = XmlScriptCommentHelper.GetDeletedWordIndex(scriptWord);

                    // Add deleted words to status list.
                    TtsXmlComments.AppendStatus(_deletedWordsDict[scriptWord], true);
                }
            }
        }

        /// <summary>
        /// Build units for this sentence.
        /// </summary>
        /// <param name="phoneme">Phoneme.</param>
        /// <param name="sliceData">Slice data.</param>
        /// <param name="buildUnitFeature">Whether build unit features.</param>
        private void BuildUnits(Phoneme phoneme, SliceData sliceData, bool buildUnitFeature)
        {
            Helper.ThrowIfNull(phoneme);
            Helper.ThrowIfNull(sliceData);

            _units.Clear();

            string punctuationPattern = ScriptItem.PunctuationPattern;
            for (int wordIndex = 0; wordIndex < Words.Count; wordIndex++)
            {
                ScriptWord word = Words[wordIndex];
                if (!word.IsPronouncableNormalWord ||
                    (!buildUnitFeature && string.IsNullOrEmpty(word.Pronunciation)))
                {
                    continue;
                }

                // look forward one item, test whether that is '?' mark
                WordType wordType = WordType.Normal;
                while (wordIndex < Words.Count - 1
                    && Words[wordIndex + 1].WordType != WordType.Normal)
                {
                    WordType nextType = Localor.MapPunctuation(Words[wordIndex + 1].Grapheme,
                        punctuationPattern);

                    // advance one more
                    if (nextType == WordType.OtherPunctuation)
                    {
                        wordType = nextType;
                    }
                    else
                    {
                        wordType = nextType;
                        break;
                    }

                    wordIndex++;
                }

                word.Units.Clear();
                word.BuildUnitWithoutFeature(sliceData, ScriptItem.PronunciationSeparator);
                foreach (TtsUnit unit in word.Units)
                {
                    unit.WordType = wordType;
                }

                Helper.AppendCollection<TtsUnit>(_units, word.Units);
            }

            if (buildUnitFeature)
            {
                BuildUnitFeatures(phoneme);
            }
        }

        /// <summary>
        /// Build unit features for this sentence.
        /// </summary>
        /// <param name="phoneme">Phoneme.</param>
        private void BuildUnitFeatures(Phoneme phoneme)
        {
            Helper.ThrowIfNull(phoneme);

            TtsUnit preUnit = null;
            ScriptSyllable preSyllable = null;
            ScriptWord preWord = null;
            TtsUnit nextUnit = null;

            for (int i = 0; i < _units.Count; i++)
            {
                TtsUnit unit = _units[i];

                ScriptSyllable syllable = (ScriptSyllable)unit.Tag;
                ScriptWord word = (ScriptWord)syllable.Tag;

                // Build context
                nextUnit = (i + 1 < _units.Count) ? _units[i + 1] : null;

                preUnit = (i > 0) ? _units[i - 1] : null;
                preSyllable = ScriptItem.FindPreviousSyllable(_units, i);
                preWord = ScriptItem.FindPreviousWord(Words, word);

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
                    (unitAtWordTail && ((int)word.Break >= (int)TtsBreak.InterPhrase)) ||
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
                unit.Feature.PosInSyllable = ScriptItem.CalculatePosInSyllable(preUnit, unit);

                // syllable position in word
                unit.Feature.PosInWord = ScriptItem.CalculatePosInWord(preSyllable, syllable);

                // word position in sentence
                unit.Feature.PosInSentence = ScriptItem.CalculatePosInSentence(preWord, word);
                if (unit.WordType == WordType.Question)
                {
                    unit.Feature.PosInSentence = PosInSentence.Quest;
                }

                // The unit in last syllable will get the same WordTone as the word.
                if (word.UnitSyllables.IndexOf(syllable) == word.UnitSyllables.Count - 1)
                {
                    unit.Feature.TtsWordTone = word.WordTone;
                }
                else
                {
                    unit.Feature.TtsWordTone = TtsWordTone.Continue;
                }
            }
        }

        #endregion
    }
}