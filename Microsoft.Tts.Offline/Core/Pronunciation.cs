//----------------------------------------------------------------------------
// <copyright file="Pronunciation.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      class of Pronunciation including spliting, validation and etc.
// </summary>
//----------------------------------------------------------------------------
namespace Microsoft.Tts.Offline.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Enum of pronunciation error.
    /// </summary>
    public enum PronunciationError
    {
        /// <summary>
        /// Empty pronunciation string.
        /// </summary>
        [ErrorAttribute(Message = "Empty pronunciation", Severity = ErrorSeverity.MustFix)]
        EmptyPronunciation,

        /// <summary>
        /// Unrecognized phone
        /// Phones are expected to be separated by whitespace in the pronunciation string.
        /// </summary>
        [ErrorAttribute(Message = "Unrecognized phone \"{1}\" in pronunciation /{0}/",
            Severity = ErrorSeverity.MustFix)]
        UnrecognizedPhone,

        /// <summary>
        /// Empty syllable.
        /// </summary>
        [ErrorAttribute(Message = "Empty syllable in the pronunciation /{0}/")]
        EmptySyllable,

        /// <summary>
        /// Vowel plus sonorant count less than minimum
        /// {0} : pronunciation
        /// {1} : Minimum of vowel plus sonorant Count.
        /// </summary>
        /// <remarks>In lexicon pronunciation, some languages may have less than minimum vowel and sonorant in one syllable if the word has only one syllable
        /// , such as "h m" of word "hm" in enIN, "s" of prefix word "c'" in frCA
        /// , we'll not remove these words from lexicon, just report this issue as warning.</remarks>
        [ErrorAttribute(Message = "One syllable has incorrect number (minimum should be {1}) for vowel plus sonorant in pronunciation /{0}/",
            Severity = ErrorSeverity.Warning)]
        VowelAndSonorantCountLessThanMinimumForSingleSyllableWord,

        /// <summary>
        /// Vowel plus sonorant count less than minimum
        /// {0} : pronunciation.
        /// </summary>
        /// <remarks>In lexicon pronunciation, in some languages the pronunciation may made up by more than one word and a word is made up by only this one syllable
        /// , we'll not remove these words from lexicon, just report this issue as warning.</remarks>
        [ErrorAttribute(Message = "The pronunciation has more than one syllable, and one of the syllable has incorrect number (minimum should be {1}) for vowel plus sonorant in pronunciation /{0}/",
            Severity = ErrorSeverity.Warning)]
        VowelAndSonorantCountLessThanMinimumInTheSingleSyllableWord,

        /// <summary>
        /// Vowel plus sonorant count less than minimum
        /// {0} : pronunciation
        /// {1} : Minimum of vowel plus sonorant Count.
        /// </summary>
        [ErrorAttribute(Message = "One syllable has incorrect number (minimum should be {1}) for vowel plus sonorant in pronunciation /{0}/",
            Severity = ErrorSeverity.MustFix)]
        VowelAndSonorantCountLessThanMinimum,

        /// <summary>
        /// Vowel plus sonorant count greater than maximum
        /// {0} : pronunciation
        /// {1} : Maximum of vowel plus sonorant Count.
        /// </summary>
        [ErrorAttribute(Message = "One syllable has incorrect number (maximum should be {1}) for vowel plus sonorant in pronunciation /{0}/")]
        VowelAndSonorantCountGreaterThanMaximum,

        /// <summary>
        /// Vowel count less than minimum
        /// {0} : pronunciation
        /// {1} : Minimum of vowel count.
        /// </summary>
        [ErrorAttribute(Message = "One syllable has incorrect vowel number (minimum should be {1}) in pronunciation /{0}/",
            Severity = ErrorSeverity.MustFix)]
        VowelCountLessThanMinimum,

        /// <summary>
        /// Vowel count greater than maximum
        /// {0} : pronunciation
        /// {1} : Maximum of vowel count.
        /// </summary>
        [ErrorAttribute(Message = "One syllable has incorrect vowel number (maximum should be {1}) in pronunciation /{0}/",
            Severity = ErrorSeverity.MustFix)]
        VowelCountGreaterThanMaximum,

        /// <summary>
        /// Too many tones in one syllable
        /// {0} : pronunciation.
        /// </summary>
        [ErrorAttribute(Message = "There are too many tones in pronunciation /{0}/")]
        TooManyTonesInOneSyllable,

        /// <summary>
        /// Incorrect tone position
        /// {0} : pronunciation.
        /// </summary>
        [ErrorAttribute(Message = "Tone should be after vowel in pronunciation /{0}/")]
        IncorrectTonePosition,

        /// <summary>
        /// Incorrect stress position
        /// {0} : pronunciation.
        /// </summary>
        [ErrorAttribute(Message = "Stress should be after vowel in pronunciation /{0}/",
            Severity = ErrorSeverity.MustFix)]
        IncorrectStressPosition,

        /// <summary>
        /// Silence is not allowed in pronunciation string
        /// {0} : pronunciation.
        /// </summary>
        [ErrorAttribute(Message = "Silence phone should not appear in pronunciation /{0}/")]
        SilenceIsNotAllowedInPronunciation,

        /// <summary>
        /// Word pronunciation map error
        /// {0} : word text
        /// {1} : unit plain description.
        /// {2} : map source type.
        /// {3} : map target type.
        /// {4} : item id.
        /// </summary>
        [ErrorAttribute(Message = "Can't map word's [{0}] unit [{1}] from [{2}] to [{3}] in item [{4}], " +
            "item removed when enable mapping pronunciation.")]
        MapWordPronunciationError,

        /// <summary>
        /// The pronunciation of word contains invalid syllable, which can't be found in phone map.
        /// {0} : phone word of Lexicon.
        /// {1} : syllable to be mapped.
        /// {2} : mapping source type.
        /// </summary>
        [ErrorAttribute(Message = "The pronunciation of word [{0}] contains invalid syllable [{1}], which can't be found in phone map [{2}]")]
        CanNotFindSyllableInPhoneMap,
    }

    /// <summary>
    /// CountRange.
    /// </summary>
    public class CountRange
    {
        /// <summary>
        /// Not Applicable.
        /// </summary>
        public static int NotApplicable = -1;

        #region Fields
        private int _min;
        private int _max;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets Minimum.
        /// </summary>
        public int Min
        {
            get { return _min; }
            set { _min = value; }
        }

        /// <summary>
        /// Gets or sets Maximum.
        /// </summary>
        public int Max
        {
            get { return _max; }
            set { _max = value; }
        }
        #endregion
    }

    /// <summary>
    /// Syllable structure.
    /// </summary>
    public class SyllableStructure
    {
        #region Fields
        private CountRange _vowelCount = new CountRange();
        private CountRange _sonorantAndVowelCount = new CountRange();
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SyllableStructure"/> class.
        /// </summary>
        public SyllableStructure()
        {
            _vowelCount.Max = 1;
            _vowelCount.Min = 0;
            _sonorantAndVowelCount.Max = CountRange.NotApplicable;
            _sonorantAndVowelCount.Min = 1;
        }

        #region Properties
        /// <summary>
        /// Gets Vowel count.
        /// </summary>
        public CountRange VowelCount
        {
            get { return _vowelCount; }
        }

        /// <summary>
        /// Gets Sonorant and vowel count.
        /// </summary>
        public CountRange SonorantAndVowelCount
        {
            get { return _sonorantAndVowelCount; }
        }
        #endregion
    }

    /// <summary>
    /// Class to validate pronunciation.
    /// </summary>
    public class Pronunciation
    {
        /// <summary>
        /// Unit boundary string.
        /// </summary>
        public const string UnitBoundaryString = " . ";

        /// <summary>
        /// Syllable boundary string.
        /// </summary>
        public const string SyllableBoundaryString = " - ";

        /// <summary>
        /// Word boundary string.
        /// </summary>
        public const string WordPronBoundaryString = " & ";

        /// <summary>
        /// Word boundary string.
        /// </summary>
        public const string WordBoundaryString = " / ";
        private static char[] _pronSeparator = new char[] { ' ' };
        private static string[] _syllableBoundaryPattern = { SyllableBoundaryString, WordPronBoundaryString };
        private static string _unitBoundaryPattern = @" \. ";
        private static string _syllablePattern = @" [-|&] ";
        private static string _stressPattern = @"(?: [\d\?]$)|(?:^[\d\?] )|(?: [\d\?] )";
        private static string _tonePattern = @"(?: t[0-6]$)|(?:^t[0-6] )|(?: t[0-6] )";

        /// <summary>
        /// Prevents a default instance of the <see cref="Pronunciation"/> class from being created.
        /// </summary>
        private Pronunciation()
        {
        }

        #region Public static methods

        /// <summary>
        /// Compare two pronunciations.
        /// </summary>
        /// <param name="pronFirst">First pronunciation.</param>
        /// <param name="pronSecond">Second pronunciation.</param>
        /// <param name="onlyPhone">Comparison mode (only phones or all symbols.</param>
        /// <returns>Bool.</returns>
        public static bool Equals(string pronFirst, string pronSecond, bool onlyPhone)
        {
            if (string.IsNullOrEmpty(pronFirst))
            {
                throw new ArgumentNullException("pronFirst");
            }

            if (string.IsNullOrEmpty(pronSecond))
            {
                throw new ArgumentNullException("pronSecond");
            }

            string first = pronFirst.ToLowerInvariant();
            string second = pronSecond.ToLowerInvariant();

            if (onlyPhone)
            {
                first = CleanDecorate(first);
                second = CleanDecorate(second);

                // clean &amp; and '-'
                first = Regex.Replace(first, "[&-]", " ").Trim();
                second = Regex.Replace(second, "[&-]", " ").Trim();
            }

            first = Regex.Replace(first, @"\s+", " ").Trim();
            second = Regex.Replace(second, @"\s+", " ").Trim();

            bool isEqual = true;
            if (first != second)
            {
                isEqual = false;
            }

            return isEqual;
        }

        /// <summary>
        /// Standardize prununciation.
        /// </summary>
        /// <param name="pronunciation">Pronunciation to be fixed.</param>
        /// <param name="phoneset">Phoneset of the language.</param>
        /// <returns>Standardized pronunciation.</returns>
        public static string StandardizePronunciation(string pronunciation, TtsPhoneSet phoneset)
        {
            if (string.IsNullOrEmpty(pronunciation))
            {
                throw new ArgumentNullException("pronunciation");
            }

            StringBuilder sb = new StringBuilder();
            foreach (string syllableString in pronunciation.Split(
                new string[] { Pronunciation.SyllableBoundaryString },
                StringSplitOptions.RemoveEmptyEntries))
            {
                if (sb.Length > 0)
                {
                    sb.Append(Pronunciation.SyllableBoundaryString);
                }

                // Fix stress position in syllable.
                ScriptSyllable syllable = ScriptSyllable.ParseStringToSyllable(syllableString, phoneset);
                sb.Append(syllable.BuildTextFromPhones(phoneset));
            }

            return pronunciation = sb.ToString();
        }

        /// <summary>
        /// Clean decorate in the pronunciation.
        ///     1. stress mark, "1", "2", or "3"
        ///     2. liasion mark, "?".
        /// </summary>
        /// <param name="pronunciation">Pronunciation to be cleaned.</param>
        /// <returns>Cleanned pronunciation.</returns>
        public static string CleanDecorate(string pronunciation)
        {
            // This regular express pattern is for pronunciation decoration.
            // 1. stress mark, "1", "2", or "3"
            // 2. liasion mark, "?"
            const string PronunciationDecoratePattern = @"(?: [123\?]$)|(?:^[123\?] )|(?: [123\?] )";

            if (string.IsNullOrEmpty(pronunciation))
            {
                throw new ArgumentNullException("pronunciation");
            }

            pronunciation = Helper.RemoveDuplicateBlank(pronunciation);
            return Regex.Replace(pronunciation, PronunciationDecoratePattern, " ").Trim();
        }

        /// <summary>
        /// Clean the stress and syllable boundary in pronunciation.
        /// </summary>
        /// <param name="pronunciation">Pronunciation to be cleaned.</param>
        /// <returns>Cleanned pronunciation.</returns>
        public static string CleanStressSyllable(string pronunciation)
        {
            if (string.IsNullOrEmpty(pronunciation))
            {
                throw new ArgumentNullException("pronunciation");
            }

            return string.Join(" ", GetPurePhones(pronunciation));
        }

        /// <summary>
        /// Get the pure phones.
        /// </summary>
        /// <param name="pronunciation">Pronunciation.</param>
        /// <returns>Phones.</returns>
        public static string[] GetPurePhones(string pronunciation)
        {
            if (string.IsNullOrEmpty(pronunciation))
            {
                throw new ArgumentNullException("pronunciation");
            }

            return pronunciation.Split(new char[] { ' ', '-', '&', '1', '2', '3', '?' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Find stress level for pronunciation of syllable or slice.
        /// </summary>
        /// <param name="text">Prounciation to find stress feature.</param>
        /// <returns>Stress level of the pronunciation.</returns>
        public static TtsStress GetStress(string text)
        {
            const string PronunciationStressPattern = @"(?: [123]$)|(?: [123] )";

            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException("text");
            }

            // only one stress mark is allowed
            if (Regex.Split(text, PronunciationStressPattern).Length > 2)
            {
                throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture,
                    "Only one stress mark is allowed in one syllable [{0}]. You may need to syllabify it.",
                    text));
            }

            TtsStress stress = TtsStress.None;

            Match match = Regex.Match(text, "(" + PronunciationStressPattern + ")");
            if (match.Success)
            {
                string mark = match.Groups[1].Value.Trim();
                stress = (TtsStress)int.Parse(mark, CultureInfo.InvariantCulture);
                Debug.Assert(0 < (int)stress && (int)stress < Enum.GetValues(typeof(TtsStress)).Length);
            }

            return stress;
        }

        /// <summary>
        /// UnTag unit boundary on given pronunciation.
        /// </summary>
        /// <param name="pronunciation">The pronunciation with unit boundary tagged, like "IH . K - S . AY 1 - T . IH . D".</param>
        /// <returns>The pronunciation, like "IH K - S AY 1 - T IH D".</returns>
        public static string UnTagUnitBoundary(string pronunciation)
        {
            Helper.ThrowIfNull(pronunciation);
            string newPronunciation = pronunciation.Replace(UnitBoundaryString, " ");
            return newPronunciation;
        }

        /// <summary>
        /// Tag unit boundary on given pronunciation.
        /// </summary>
        /// <param name="pronunciation">The pronunciation, like "IH K - S AY 1 - T IH D".</param>
        /// <returns>
        /// The pronunciation with unit boundary tagged, like "IH . K - S . AY 1 - T . IH . D".
        /// </returns>
        public static string TagUnitBoundary(string pronunciation)
        {
            StringBuilder pronunciationBuilder = new StringBuilder();
            string[] syllables = SplitIntoSyllables(pronunciation.Trim());
            bool isFirstSyllable = true;
            foreach (string syllable in syllables)
            {
                if (isFirstSyllable)
                {
                    isFirstSyllable = false;
                }
                else
                {
                    pronunciationBuilder.Append(SyllableBoundaryString);
                }

                string[] phones = SplitIntoPhones(syllable);
                bool isFirstPhone = true;
                foreach (string phone in phones)
                {
                    if (isFirstPhone)
                    {
                        isFirstPhone = false;
                    }
                    else
                    {
                        if (Regex.IsMatch(phone, @"\d"))
                        {
                            pronunciationBuilder.Append(" ");
                        }
                        else
                        {
                            pronunciationBuilder.Append(UnitBoundaryString);
                        }
                    }

                    pronunciationBuilder.Append(phone);
                }
            }

            return pronunciationBuilder.ToString();
        }

        /// <summary>
        /// Split the pronunciation string into array of phone string, using the default separator of �space?.
        /// </summary>
        /// <param name="pron">Pronunciaiton.</param>
        /// <returns>Phone array.</returns>
        public static string[] SplitIntoPhones(string pron)
        {
            if (pron == null)
            {
                throw new ArgumentNullException("pron");
            }

            return pron.Split(_pronSeparator, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Split pronunciaiton into syllables.
        /// </summary>
        /// <param name="pronunciation">Pronunciaiton.</param>
        /// <returns>Syllable array.</returns>
        public static string[] SplitIntoSyllables(string pronunciation)
        {
            if (pronunciation == null)
            {
                throw new ArgumentNullException("pronunciation");
            }

            string[] syllables = pronunciation.Split(_syllableBoundaryPattern,
                StringSplitOptions.RemoveEmptyEntries);

            return RemoveEmptyItems(syllables);
        }

        /// <summary>
        /// Get the spelling out pronunciation.
        /// </summary>
        /// <param name="chartable">Chartable.</param>
        /// <param name="word">Word.</param>
        /// <returns>Spelt out pronunciation.</returns>
        public static string GetSpellingOutPron(CharTable chartable, string word)
        {
            if (chartable == null)
            {
                throw new ArgumentNullException("chartable");
            }

            if (string.IsNullOrEmpty(word))
            {
                throw new ArgumentNullException("word");
            }

            string pron = string.Empty;
            for (int index = 0; index < word.Length; index++)
            {
                string spellingPron = string.Empty;
                foreach (CharElement charElement in chartable.CharList)
                {
                    if (charElement.Symbol == word[index].ToString())
                    {
                        spellingPron = charElement.Pronunciation;
                    }
                }

                if (!string.IsNullOrEmpty(spellingPron))
                {
                    if (index != 0)
                    {
                        pron = pron + " - ";
                    }

                    pron = pron + spellingPron;
                }
            }

            return pron;
        }

        /// <summary>
        /// Validate the pronunciation string according to the phone set.
        /// </summary>
        /// <param name="pron">Pronunciation string.</param>
        /// <param name="ttsPhoneSet">Tts phone set.</param>
        /// <returns>ErrorSet.</returns>
        public static ErrorSet Validate(string pron, TtsPhoneSet ttsPhoneSet)
        {
            if (pron == null)
            {
                throw new ArgumentNullException("pron");
            }

            if (ttsPhoneSet == null)
            {
                throw new ArgumentNullException("ttsPhoneSet");
            }

            ErrorSet errorSet = new ErrorSet();
            string[] phones = SplitIntoPhones(pron);
            if (phones.Length == 0)
            {
                errorSet.Add(PronunciationError.EmptyPronunciation);
            }
            else
            {
                Collection<Phone> syllable = new Collection<Phone>();
                bool last = true;
                bool current = false;
                for (int i = 0; i < phones.Length; i++)
                {
                    Phone phone = ttsPhoneSet.GetPhone(phones[i]);
                    if (phone == null)
                    {
                        errorSet.Add(PronunciationError.UnrecognizedPhone, pron, phones[i]);
                        continue;
                    }

                    if (phone.HasFeature(PhoneFeature.Syllable))
                    {
                        current = phones[i].Equals("&") ? true : false;
                        errorSet.Merge(ValidateSyllable(syllable, pron, ttsPhoneSet.SyllableStructure, last && current));
                        last = current;
                        syllable.Clear();
                    }
                    else
                    {
                        syllable.Add(phone);
                    }
                }

                errorSet.Merge(ValidateSyllable(syllable, pron, ttsPhoneSet.SyllableStructure, last));
            }

            return errorSet;
        }

        /// <summary>
        /// Validate the syllable.
        /// </summary>
        /// <param name="syllable">Syllable.</param>
        /// <param name="pron">The whole pronunciation string.</param>
        /// <param name="syllableStructure">Syllable structure.</param>
        ///  <param name="isWordBoundarySyllable">Is Word Boundary Syllable.</param>
        /// <returns>ErrorSet.</returns>
        public static ErrorSet ValidateSyllable(Collection<Phone> syllable, string pron,
            SyllableStructure syllableStructure, bool isWordBoundarySyllable)
        {
            ErrorSet errorSet = new ErrorSet();
            if (syllable == null || syllable.Count == 0)
            {
                errorSet.Add(PronunciationError.EmptySyllable, pron);
            }
            else
            {
                int vowelCount = 0;
                int sonorantCount = 0;
                int toneCount = 0;
                foreach (Phone phone in syllable)
                {
                    if (phone.HasFeature(PhoneFeature.Vowel))
                    {
                        vowelCount++;
                    }

                    if (phone.HasFeature(PhoneFeature.Sonorant))
                    {
                        sonorantCount++;
                    }

                    if (phone.HasFeature(PhoneFeature.Tone))
                    {
                        toneCount++;
                        int index = syllable.IndexOf(phone);
                        if (index == 0 || !syllable[index - 1].HasFeature(PhoneFeature.Vowel))
                        {
                            errorSet.Add(PronunciationError.IncorrectTonePosition, pron);
                        }
                    }

                    if (phone.HasFeature(PhoneFeature.MainStress) ||
                        phone.HasFeature(PhoneFeature.SubStress))
                    {
                        int index = syllable.IndexOf(phone);
                        if (index == 0 || !syllable[index - 1].HasFeature(PhoneFeature.Vowel))
                        {
                            errorSet.Add(PronunciationError.IncorrectStressPosition, pron);
                        }
                    }

                    if (phone.HasFeature(PhoneFeature.Silence))
                    {
                        errorSet.Add(PronunciationError.SilenceIsNotAllowedInPronunciation, pron);
                    }
                }

                if (syllableStructure.VowelCount.Min != CountRange.NotApplicable &&
                    vowelCount < syllableStructure.VowelCount.Min)
                {
                    errorSet.Add(PronunciationError.VowelCountLessThanMinimum, pron,
                        syllableStructure.VowelCount.Min.ToString(CultureInfo.InvariantCulture));
                }

                if (syllableStructure.VowelCount.Max != CountRange.NotApplicable &&
                    vowelCount > syllableStructure.VowelCount.Max)
                {
                    errorSet.Add(PronunciationError.VowelCountGreaterThanMaximum, pron,
                        syllableStructure.VowelCount.Max.ToString(CultureInfo.InvariantCulture));
                }

                if (syllableStructure.SonorantAndVowelCount.Min != CountRange.NotApplicable &&
                    vowelCount + sonorantCount < syllableStructure.SonorantAndVowelCount.Min)
                {   // whether the pron has only one syllable
                    if (syllable.Count == SplitIntoPhones(pron).Length)
                    {
                        errorSet.Add(PronunciationError.VowelAndSonorantCountLessThanMinimumForSingleSyllableWord, pron,
                            syllableStructure.SonorantAndVowelCount.Min.ToString(CultureInfo.InvariantCulture));
                    }
                    else if (syllable.Count == 1 && isWordBoundarySyllable)
                    {
                        errorSet.Add(PronunciationError.VowelAndSonorantCountLessThanMinimumInTheSingleSyllableWord, pron,
                            syllableStructure.SonorantAndVowelCount.Min.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        errorSet.Add(PronunciationError.VowelAndSonorantCountLessThanMinimum, pron,
                            syllableStructure.SonorantAndVowelCount.Min.ToString(CultureInfo.InvariantCulture));
                    }
                }

                if (syllableStructure.SonorantAndVowelCount.Max != CountRange.NotApplicable &&
                    vowelCount + sonorantCount > syllableStructure.SonorantAndVowelCount.Max)
                {
                    errorSet.Add(PronunciationError.VowelAndSonorantCountGreaterThanMaximum, pron,
                        syllableStructure.SonorantAndVowelCount.Max.ToString(CultureInfo.InvariantCulture));
                }

                if (toneCount > 1)
                {
                    errorSet.Add(PronunciationError.TooManyTonesInOneSyllable, pron);
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Split the pronunciation string into Phone array.
        /// Each phone should be separated by space.
        /// If i-th phone can not be recognized, Phone[i] in the return value will be null.
        /// </summary>
        /// <param name="pron">Pronunciation string.</param>
        /// <param name="ttsPhoneSet">Tts phone set.</param>
        /// <param name="errorSet">Error set.</param>
        /// <returns>Phone list.</returns>
        public static Phone[] SplitIntoPhones(string pron, TtsPhoneSet ttsPhoneSet, ErrorSet errorSet)
        {
            if (pron == null)
            {
                throw new ArgumentNullException("pron");
            }

            if (ttsPhoneSet == null)
            {
                throw new ArgumentNullException("ttsPhoneSet");
            }

            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            string[] phones = SplitIntoPhones(pron);
            Phone[] ttsPhones = null;
            if (phones.Length == 0)
            {
                errorSet.Add(PronunciationError.EmptyPronunciation);
            }
            else
            {
                ttsPhones = new Phone[phones.Length];
                for (int i = 0; i < phones.Length; i++)
                {
                    ttsPhones[i] = ttsPhoneSet.GetPhone(phones[i]);
                    if (ttsPhones[i] == null)
                    {
                        errorSet.Add(PronunciationError.UnrecognizedPhone, pron, phones[i]);
                        break;
                    }
                }
            }

            return ttsPhones;
        }

        /// <summary>
        /// Convert the pronunciation string into hexical id string.
        /// </summary>
        /// <param name="pron">Pronunciation string.</param>
        /// <param name="ttsPhoneSet">TTS phone set.</param>
        /// <param name="errorSet">Error set.</param>
        /// <returns>Hexical id string.</returns>
        public static string ConvertIntoHexIds(string pron, TtsPhoneSet ttsPhoneSet, ErrorSet errorSet)
        {
            if (pron == null)
            {
                throw new ArgumentNullException("pron");
            }

            if (ttsPhoneSet == null)
            {
                throw new ArgumentNullException("ttsPhoneSet");
            }

            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            ErrorSet phoneErrorSet = new ErrorSet();
            Phone[] ttsPhones = SplitIntoPhones(pron, ttsPhoneSet, phoneErrorSet);
            errorSet.Merge(phoneErrorSet);
            string hexIds = string.Empty;
            if (phoneErrorSet.Errors.Count == 0)
            {
                foreach (Phone phone in ttsPhones)
                {
                    Debug.Assert(phone != null);
                    if (phone != null)
                    {
                        string hexId = Convert.ToString(phone.Id, 16);
                        if (!string.IsNullOrEmpty(hexIds))
                        {
                            hexIds += " ";
                        }

                        hexIds += hexId;
                    }
                }
            }

            return hexIds;
        }

        /// <summary>
        /// Clear the unit boundaries in pronunciation.
        /// </summary>
        /// <param name="pronunciation">Input pronunciation.</param>
        /// <returns>Pronunciation.</returns>
        public static string RemoveUnitBoundary(string pronunciation)
        {
            if (pronunciation == null)
            {
                throw new ArgumentNullException("pronunciation");
            }

            return Regex.Replace(pronunciation, _unitBoundaryPattern, " ");
        }

        /// <summary>
        /// Clear the stress mark in pronunciation.
        /// </summary>
        /// <param name="pronunciation">Input pronunciation.</param>
        /// <returns>Pronunciation.</returns>
        public static string RemoveStress(string pronunciation)
        {
            if (pronunciation == null)
            {
                throw new ArgumentNullException("pronunciation");
            }

            return Regex.Replace(pronunciation, _stressPattern, " ");
        }

        /// <summary>
        /// Hard-code to clear the Liaison mark in pronunciation.
        /// </summary>
        /// <param name="pronunciation">Input pronunciation.</param>
        /// <param name="language">Language.</param>
        /// <returns>Pronunciation.</returns>
        public static string RemoveLiaison(string pronunciation, Language language)
        {
            if (pronunciation == null)
            {
                throw new ArgumentNullException("pronunciation");
            }

            string newPron = pronunciation;
            if (language == Language.FrFR || language == Language.FrCA)
            {
                newPron = Regex.Replace(newPron, "(?i)(l_n|l_t|l_z|\\?)", " ");
            }

            newPron = Regex.Replace(newPron, "[ ]+", " ").Trim();
            return newPron;
        }

        /// <summary>
        /// Clear the tone mark in pronunciation.
        /// </summary>
        /// <param name="pronunciation">Input pronunciation.</param>
        /// <returns>Pronunciation.</returns>
        public static string RemoveTone(string pronunciation)
        {
            if (pronunciation == null)
            {
                throw new ArgumentNullException("pronunciation");
            }

            return Regex.Replace(pronunciation, _tonePattern, " ");
        }

        /// <summary>
        /// Clear the syllable mark in pronunciation.
        /// </summary>
        /// <param name="pronunciation">Input pronunciation.</param>
        /// <returns>Pronunciation.</returns>
        public static string RemoveSyllable(string pronunciation)
        {
            if (pronunciation == null)
            {
                throw new ArgumentNullException("pronunciation");
            }

            return Regex.Replace(pronunciation, _syllablePattern, " ");
        }

        /// <summary>
        /// Validate whether can map pronunciation using the phone map.
        /// </summary>
        /// <param name="unit">ScriptWord to be mapped.</param>
        /// <param name="phoneMap">Phone map.</param>
        /// <returns>Mapped pronunciation.</returns>
        public static string GetMappedPronunciation(TtsUnit unit, PhoneMap phoneMap)
        {
            string mappedUnit = string.Empty;
            if (phoneMap == null)
            {
                throw new ArgumentNullException("phoneMap");
            }

            if (phoneMap.Items.ContainsKey(unit.PlainDescription))
            {
                mappedUnit = phoneMap.Items[unit.PlainDescription];
            }
            else
            {
                throw new InvalidDataException(Helper.NeutralFormat("Can't find unit in phone map"));
            }

            return mappedUnit;
        }

        /// <summary>
        /// Validate whether can map pronunciation using the phone map.
        /// </summary>
        /// <param name="scriptWord">ScriptWord to be mapped.</param>
        /// <param name="phoneMap">Phone map.</param>
        /// <param name="errorSet">Error set.</param>
        /// <returns>Whether validate.</returns>
        public static string GetMappedPronunciation(ScriptWord scriptWord,
            PhoneMap phoneMap, ErrorSet errorSet)
        {
            if (scriptWord == null)
            {
                throw new ArgumentNullException("scriptWord");
            }

            if (phoneMap == null)
            {
                throw new ArgumentNullException("phoneMap");
            }

            Debug.Assert(scriptWord.Sentence != null);
            Debug.Assert(scriptWord.Sentence.ScriptItem != null);

            errorSet.Clear();
            Debug.Assert(scriptWord.GetUnits(Localor.GetPhoneme(scriptWord.Language),
                Localor.GetSliceData(scriptWord.Language), false).Count > 0);

            string wordPronun = scriptWord.Pronunciation;
            ScriptItem.AlignOffset(wordPronun, scriptWord.Units);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < scriptWord.Units.Count; i++)
            {
                if (i == 0 && scriptWord.Units[i].OffsetInString > 0)
                {
                    sb.Append(wordPronun.Substring(0, scriptWord.Units[i].OffsetInString));
                }
                else if (i > 0)
                {
                    int lastUnitEnd = scriptWord.Units[i - 1].OffsetInString + scriptWord.Units[i - 1].LengthInString;
                    int currUnitStart = scriptWord.Units[i].OffsetInString;
                    if (lastUnitEnd < currUnitStart)
                    {
                        sb.Append(wordPronun.Substring(lastUnitEnd, currUnitStart - lastUnitEnd));
                    }
                }

                if (!phoneMap.Items.ContainsKey(scriptWord.Units[i].PlainDescription))
                {
                    errorSet.Add(PronunciationError.MapWordPronunciationError,
                        scriptWord.Grapheme, scriptWord.Units[i].Description,
                        phoneMap.Source, phoneMap.Target, scriptWord.Sentence.ScriptItem.Id);
                    break;
                }
                else
                {
                    sb.Append(phoneMap.Items[scriptWord.Units[i].PlainDescription]);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Map pronunciation using phonemap.
        /// Only support map syllable based pronunciation.
        /// Todo: Support phone based, unit based pronunciation.
        /// </summary>
        /// <param name="pronunciation">Pronunciation to be mapped.</param>
        /// <param name="phoneMap">Phone map used to map pronunciation.</param>
        /// <returns>Mapped pronunciation.</returns>
        public static string GetMappedPronunciation(string pronunciation, PhoneMap phoneMap)
        {
            if (string.IsNullOrEmpty(pronunciation))
            {
                throw new ArgumentNullException("pronunciation");
            }

            if (phoneMap == null)
            {
                throw new ArgumentNullException("phoneMap");
            }

            ErrorSet errorSet = new ErrorSet();
            string mappedPron = GetMappedPronunciation(pronunciation, phoneMap, errorSet);
            return mappedPron;
        }

        /// <summary>
        /// Map pronunciation using phonemap.
        /// Only support map syllable based pronunciation.
        /// Todo: Support phone based, unit based pronunciation.
        /// </summary>
        /// <param name="word">Word corrsponding with pronunciation.</param>
        /// <param name="pronunciation">Pronunciation to be mapped.</param>
        /// <param name="phoneMap">Phone map used to map pronunciation.</param>
        /// <param name="errorSet">Error set.</param>
        /// <returns>Mapped pronunciation.</returns>
        public static string GetMappedPronunciation(string word, string pronunciation, PhoneMap phoneMap,
            ErrorSet errorSet)
        {
            if (string.IsNullOrEmpty(pronunciation))
            {
                throw new ArgumentNullException("pronunciation");
            }

            if (phoneMap == null)
            {
                throw new ArgumentNullException("phoneMap");
            }

            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            string[] syllables = Pronunciation.SplitIntoSyllables(pronunciation);
            StringBuilder mappedPronunciation = new StringBuilder();
            int lastSyllableEnd = 0;
            int currentSyllableStart = 0;

            foreach (string syllable in syllables)
            {
                currentSyllableStart = pronunciation.IndexOf(syllable, lastSyllableEnd, StringComparison.Ordinal);
                Debug.Assert(currentSyllableStart >= 0);
                mappedPronunciation.Append(pronunciation.Substring(lastSyllableEnd,
                    currentSyllableStart - lastSyllableEnd));
                if (phoneMap.Items.ContainsKey(syllable))
                {
                    mappedPronunciation.Append(phoneMap.Items[syllable]);
                }
                else
                {
                    errorSet.Add(PronunciationError.CanNotFindSyllableInPhoneMap, word,
                        syllable, phoneMap.Source);
                    break;
                }

                lastSyllableEnd = currentSyllableStart + syllable.Length;
            }

            return mappedPronunciation.ToString();
        }

        /// <summary>
        /// Map pronunciation using phonemap.
        /// Only support map syllable based pronunciation.
        /// Todo: Support phone based, unit based pronunciation.
        /// </summary>
        /// <param name="pronunciation">Pronunciation to be mapped.</param>
        /// <param name="phoneMap">Phone map used to map pronunciation.</param>
        /// <param name="errorSet">Error set.</param>
        /// <returns>Mapped pronunciation.</returns>
        public static string GetMappedPronunciation(string pronunciation, PhoneMap phoneMap,
            ErrorSet errorSet)
        {
            return GetMappedPronunciation(string.Empty, pronunciation, phoneMap, errorSet);
        }

        #endregion

        #region private operations

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
    }
}