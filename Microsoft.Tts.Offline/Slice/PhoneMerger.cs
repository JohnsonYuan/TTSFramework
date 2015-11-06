//----------------------------------------------------------------------------
// <copyright file="PhoneMerger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements phoneme merger
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
    using System.Resources;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Class to merge phones into bigger level slices/units
    /// Note: For XML script, the unit boundary is tagged in the word's "p"(pronunciation) attribute.
    /// </summary>
    public class PhoneMerger
    {
        #region Fields

        // for support common words as whole unit
        private Microsoft.Tts.Offline.SliceData _sliceData;
        private Microsoft.Tts.Offline.TruncateRuleData _truncateRuleData;
        private Microsoft.Tts.Offline.TtsPhoneSet _phoneSet;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="PhoneMerger"/> class.
        /// </summary>
        /// <param name="phoneSet">Phone set.</param>
        /// <param name="sliceData">Slice data.</param>
        /// <param name="truncRule">Truncate rule data.</param>
        public PhoneMerger(TtsPhoneSet phoneSet, SliceData sliceData, TruncateRuleData truncRule)
        {
            if (phoneSet == null)
            {
                throw new ArgumentNullException("phoneSet");
            }

            if (sliceData == null)
            {
                throw new ArgumentNullException("sliceData");
            }

            if (truncRule == null)
            {
                throw new ArgumentNullException("truncRule");
            }

            _phoneSet = phoneSet;
            _sliceData = sliceData;
            _truncateRuleData = truncRule;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Slice data.
        /// </summary>
        public SliceData SliceData
        {
            get { return _sliceData; }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Remove all slice boundaries in the source script file
        /// And save it to target script file.
        /// </summary>
        /// <param name="sourceScriptFilePath">Source script file.</param>
        /// <param name="targetScriptFilePath">Target script file.</param>
        /// <returns>Data error set found.</returns>
        public static ErrorSet RemoveSliceBoundary(string sourceScriptFilePath,
            string targetScriptFilePath)
        {
            // Parameters validation
            if (string.IsNullOrEmpty(sourceScriptFilePath))
            {
                throw new ArgumentNullException("sourceScriptFilePath");
            }

            if (string.IsNullOrEmpty(targetScriptFilePath))
            {
                throw new ArgumentNullException("targetScriptFilePath");
            }

            XmlScriptFile script = new XmlScriptFile();

            // Keep comments in XmlScript file
            XmlScriptFile.ContentControler controler = new XmlScriptFile.ContentControler();
            controler.LoadComments = true;
            script.Load(sourceScriptFilePath, controler);

            foreach (ScriptItem item in script.Items)
            {
                foreach (ScriptWord word in item.AllWords)
                {
                    if (!string.IsNullOrEmpty(word.Pronunciation))
                    {
                        word.Pronunciation = Pronunciation.RemoveUnitBoundary(word.Pronunciation);
                    }
                }
            }

            // Save comments in XmlScript file
            script.Save(targetScriptFilePath, Encoding.Unicode);

            return script.ErrorSet;
        }

        /// <summary>
        /// Set nucleus vowel stress mark.
        /// </summary>
        /// <param name="phoneme">Phoneme of the language to process.</param>
        /// <param name="pronunciation">Pronunciation to set pronunciation.</param>
        /// <param name="stress">Stress mark to set for the vowel in the pronunciation.</param>
        /// <returns>Pronunciation with stress.</returns>
        public static string SetVowelStress(Phoneme phoneme, string pronunciation, TtsStress stress)
        {
            if (phoneme == null)
            {
                throw new ArgumentNullException("phoneme");
            }

            if (string.IsNullOrEmpty(pronunciation))
            {
                return null;
            }

            if (stress > TtsStress.None)
            {
                string[] phones = pronunciation.Split(new char[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);
                int vowelIndex = phoneme.GetFirstVowelIndex(phones);
                if (vowelIndex < 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "There is no vowel found in the syllable pronunciation [{0}]",
                        phones);
                    throw new InvalidDataException(message);
                }

                phones[vowelIndex] = string.Format(CultureInfo.InvariantCulture,
                    "{0} {1}", phones[vowelIndex], (int)stress);

                return string.Join(" ", phones);
            }
            else
            {
                return pronunciation;
            }
        }

        /// <summary>
        /// Truncate one phone from nucleus.
        /// </summary>
        /// <param name="phoneme">Phoneme of the language to process.</param>
        /// <param name="rules">Truncation rules.</param>
        /// <param name="nucleus">CVC source to truncate.</param>
        /// <returns>Result: left part + right part.</returns>
        public static string[] TruncateOnePhoneFromNucleus(Phoneme phoneme,
            Collection<TruncateRule> rules, string nucleus)
        {
            if (phoneme == null)
            {
                throw new ArgumentNullException("phoneme");
            }

            if (rules == null)
            {
                throw new ArgumentNullException("rules");
            }

            if (string.IsNullOrEmpty(nucleus))
            {
                throw new ArgumentNullException("nucleus");
            }

            TtsMetaUnit ttsMetaUnit = new TtsMetaUnit(phoneme.Language);
            ttsMetaUnit.Name = nucleus;
            string[] phoneNames = ttsMetaUnit.GetPhonesName();
            string leftPart = null;
            string rightPart = null;

            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i] == null)
                {
                    string message = Helper.NeutralFormat("rules[{0}] should not be null.", i);
                    throw new ArgumentException(message);
                }

                if (rules[i].Side == TruncateSide.Right)
                {
                    Match m = Regex.Match(rules[i].Phones,
                        @"\b" + phoneNames[phoneNames.Length - 1] + @"\b");
                    if (m.Success)
                    {
                        leftPart = TtsMetaPhone.Join(" ", ttsMetaUnit.Phones, 0, phoneNames.Length - 1);
                        rightPart = ttsMetaUnit.Phones[phoneNames.Length - 1].Name;
                        break;
                    }
                }
                else if (rules[i].Side == TruncateSide.Left)
                {
                    Match m = Regex.Match(rules[i].Phones,
                        @"\b" + phoneNames[0] + @"\b");
                    if (m.Success)
                    {
                        leftPart = ttsMetaUnit.Phones[0].Name;
                        rightPart = TtsMetaPhone.Join(" ", ttsMetaUnit.Phones, 1, phoneNames.Length - 1);
                        break;
                    }
                }
                else
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Truncating side [{0}] is not supported.",
                        rules[i].Side);
                    Debug.Assert(false, message);
                    throw new NotSupportedException(message);
                }
            }

            if (string.IsNullOrEmpty(leftPart) || string.IsNullOrEmpty(rightPart))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Nucleus [{0}] has empty left phone or right phone after truncating.",
                    nucleus);
                Trace.WriteLine(message);
                leftPart = TtsMetaPhone.Join(" ", ttsMetaUnit.Phones, 0, phoneNames.Length - 1);
                rightPart = ttsMetaUnit.Phones[phoneNames.Length - 1].Name;
            }

            return new string[] { leftPart, rightPart };
        }

        /// <summary>
        /// Format phone string in Word to slice string.
        /// </summary>
        /// <param name="sliceData">Slice data.</param>
        /// <param name="wordPron">Word pronunciation to convert.</param>
        /// <returns>Word pronunciation string in slice.</returns>
        public static string RewritePhones2Units(SliceData sliceData,
            string wordPron)
        {
            if (sliceData == null)
            {
                throw new ArgumentNullException("sliceData");
            }

            if (string.IsNullOrEmpty(wordPron))
            {
                throw new ArgumentNullException("wordPron");
            }

            string[] syllables = Regex.Split(wordPron, @"\s*[&|\-]\s*");
            List<string> tgtSylls = new List<string>();
            for (int i = 0; i < syllables.Length; i++)
            {
                TtsStress nucleusStress = Pronunciation.GetStress(syllables[i]);

                string[] units = BuildUnits(Localor.GetPhoneme(sliceData.Language),
                    sliceData, syllables[i]);

                string tgtslice = string.Join(" . ", units);
                tgtslice = tgtslice.Replace(TtsUnit.OnsetPrefix, string.Empty);
                tgtslice = tgtslice.Replace(TtsUnit.NucleusPrefix, string.Empty);
                tgtslice = tgtslice.Replace(TtsUnit.CodaPrefix, string.Empty);
                tgtslice = tgtslice.Replace(TtsUnit.PhoneDelimiter, " ");

                if (nucleusStress != TtsStress.None)
                {
                    tgtslice = SetVowelStress(Localor.GetPhoneme(sliceData.Language),
                        tgtslice, nucleusStress);
                }

                tgtSylls.Add(tgtslice);
            }

            return string.Join(" - ", tgtSylls.ToArray());
        }

        /// <summary>
        /// Build units for syllbale pronunciation,
        /// And the units are concatenated together in the string and seperated by ".".
        /// </summary>
        /// <param name="phoneme">Phoneme of the language to process with.</param>
        /// <param name="sliceData">Slice data to process.</param>
        /// <param name="syllable">Syllables to process.</param>
        /// <returns>Best unit list.</returns>
        public static string[] BuildUnits(Phoneme phoneme,
            SliceData sliceData, string syllable)
        {
            if (phoneme == null)
            {
                throw new ArgumentNullException("phoneme");
            }

            if (phoneme.TtsSonorantPhones == null)
            {
                string message = Helper.NeutralFormat("phoneme.TtsSonorantPhones should not be null.");
                throw new ArgumentException(message);
            }

            if (string.IsNullOrEmpty(syllable))
            {
                throw new ArgumentNullException("syllable");
            }

            if (sliceData == null)
            {
                throw new ArgumentNullException("sliceData");
            }

            if (sliceData.OnsetSlices == null)
            {
                string message = Helper.NeutralFormat("sliceData.OnsetSlices should not be null.");
                throw new ArgumentException(message);
            }

            if (sliceData.NucleusSlices == null)
            {
                string message = Helper.NeutralFormat("sliceData.NucleusSlices should not be null.");
                throw new ArgumentException(message);
            }

            List<string> slicedUnits = new List<string>();

            string unstressedSyllable = Pronunciation.RemoveStress(syllable);

            ScriptItem scriptItem = new ScriptItem(phoneme.Language);

            // items contains phone and tone.
            string[] items = scriptItem.PronunciationSeparator.SplitPhones(unstressedSyllable);

            // Treate all syllable as one unit at first.
            TtsMetaUnit ttsMetaUnit = new TtsMetaUnit(phoneme.Language);
            ttsMetaUnit.Name = string.Join(" ", items);
            string[] phones = ttsMetaUnit.GetPhonesName();

            // Treat all phones in this syllable as a whole unit
            if (sliceData.NucleusSlices.IndexOf(ttsMetaUnit.Name) >= 0)
            {
                // If it is alread defined in the predefined unit collection, return it
                slicedUnits.Add(TtsUnit.NucleusPrefix + ttsMetaUnit.Name.Replace(" ", TtsUnit.PhoneDelimiter));
                return slicedUnits.ToArray();
            }

            int vowelIndex = phoneme.GetFirstVowelIndex(phones);
            if (vowelIndex < 0)
            {
                // If no vowel in the syllable, treat all phones in this syllable as a unit if it is in unit table
                if (sliceData.OnsetSlices.IndexOf(ttsMetaUnit.Name) >= 0)
                {
                    slicedUnits.Add(TtsUnit.OnsetPrefix + ttsMetaUnit.Name.Replace(" ", TtsUnit.PhoneDelimiter));
                }
                else if (sliceData.CodaSlices.IndexOf(ttsMetaUnit.Name) >= 0)
                {
                    slicedUnits.Add(TtsUnit.CodaPrefix + ttsMetaUnit.Name.Replace(" ", TtsUnit.PhoneDelimiter));
                }
                else
                {
                    // otherwise, treat each phone as a coda unit
                    foreach (string phone in phones)
                    {
                        slicedUnits.Add(TtsUnit.CodaPrefix + phone);
                    }
                }

                return slicedUnits.ToArray();
            }

            // Search first cosonant sonarant from the left side of the vowel font in the syllable
            int firstSonarantIndex = vowelIndex;
            for (int i = vowelIndex - 1; i >= 0; i--)
            {
                if (phoneme.TtsSonorantPhones.IndexOf(phones[i]) >= 0)
                {
                    firstSonarantIndex = i;
                }
            }

            // Search last cosonant sonarant from the right side of the vowel font in the syllable
            int lastSonarantIndex = vowelIndex;
            for (int i = vowelIndex + 1; i <= phones.Length - 1; i++)
            {
                if (phoneme.TtsSonorantPhones.IndexOf(phones[i]) >= 0)
                {
                    lastSonarantIndex = i;
                }
            }

            // Treat all vowel and surrounding sonarant consonants as the nucleus unit first
            string nucleus = TtsMetaPhone.Join(" ", ttsMetaUnit.Phones,
                firstSonarantIndex, lastSonarantIndex - firstSonarantIndex + 1);

            TruncateRuleData truncateRuleData = Localor.GetTruncateRuleData(phoneme.Language);

            // Refine nucleus according to the predefined unit table
            while (lastSonarantIndex - firstSonarantIndex > 0 && sliceData.NucleusSlices.IndexOf(nucleus) <= 0)
            {
                // If the unit candidate is not listed in the predefined unit list, try to truncate it
                string[] leftRight =
                    PhoneMerger.TruncateOnePhoneFromNucleus(phoneme, truncateRuleData.NucleusTruncateRules,
                    nucleus);

                if (phoneme.TtsPhones.IndexOf(leftRight[0]) >= 0)
                {
                    Debug.Assert(phoneme.TtsPhones.IndexOf(leftRight[0]) >= 0);
                    firstSonarantIndex++;
                }
                else
                {
                    Debug.Assert(phoneme.TtsPhones.IndexOf(leftRight[1]) >= 0);
                    lastSonarantIndex--;
                }

                // Re-define the remaining nucleus unit
                nucleus = TtsMetaPhone.Join(" ", ttsMetaUnit.Phones,
                    firstSonarantIndex, lastSonarantIndex - firstSonarantIndex + 1);
            }

            slicedUnits.Add(TtsUnit.NucleusPrefix + nucleus.Replace(" ", TtsUnit.PhoneDelimiter));

            // Refine onset
            for (int index = firstSonarantIndex - 1; index >= 0; index--)
            {
                string onset = TtsMetaPhone.Join(TtsUnit.PhoneDelimiter, ttsMetaUnit.Phones, 0, index + 1);
                if (sliceData.OnsetSlices.IndexOf(onset.Replace(TtsUnit.PhoneDelimiter, " ")) >= 0)
                {
                    slicedUnits.Insert(0, TtsUnit.OnsetPrefix + onset);

                    // Remove the number of added phones,
                    // except current phone itself which will be recuded by index--
                    index -= index;
                }
                else
                {
                    // Treat it as a single phone unit
                    slicedUnits.Insert(0,
                        TtsUnit.OnsetPrefix + TtsMetaPhone.Join(TtsUnit.PhoneDelimiter, ttsMetaUnit.Phones, index, 1));
                }
            }

            // Refine coda, matching from right to left
            BuildCodaUnits(sliceData, ttsMetaUnit.Phones, lastSonarantIndex + 1, slicedUnits);

            return slicedUnits.ToArray();
        }

        /// <summary>
        /// Slice the pronunciation of each script item in the script file.
        /// </summary>
        /// <param name="script">Script file to slice.</param>
        /// <returns>Data error found during the slicing.</returns>
        public ErrorSet Slice(XmlScriptFile script)
        {
            if (script == null)
            {
                throw new ArgumentNullException("script");
            }

            ErrorSet errorSet = new ErrorSet();
            foreach (ScriptItem entry in script.Items)
            {
                try
                {
                    Slice(entry);
                }
                catch (InvalidDataException ide)
                {
                    string message = Helper.NeutralFormat("Error in item {0} of file {1}: {2}", 
                        entry.Id, script.FilePath, Helper.BuildExceptionMessage(ide)); 
                    errorSet.Add(ScriptError.OtherErrors, entry.Id, message);
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Slice the pronunciation of one script file.
        /// </summary>
        /// <param name="scriptFilePath">Source file.</param>
        /// <param name="targetFilePath">Target file.</param>
        /// <returns>Data error set found.</returns>
        public ErrorSet Slice(string scriptFilePath, string targetFilePath)
        {
            XmlScriptFile script = new XmlScriptFile();
            XmlScriptFile.ContentControler controler = new XmlScriptFile.ContentControler();
            controler.LoadComments = true;
            script.Load(scriptFilePath, controler);

            ErrorSet errorSet = Slice(script);
            script.Save(targetFilePath);

            errorSet.Merge(script.ErrorSet);

            return errorSet;
        }

        /// <summary>
        /// Syllabify one tts word entry.
        /// </summary>
        /// <param name="word">Word to syllabify.</param>
        /// <returns>Sliced word pronunciation.</returns>
        public string SliceWord(ScriptWord word)
        {
            if (word == null)
            {
                throw new ArgumentNullException("word");
            }

            if (string.IsNullOrEmpty(word.Grapheme))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "word.Grapheme should not be null or empty.");
                throw new ArgumentNullException("word", message);
            }

            string pron = word.GetPronunciation(_phoneSet);
            string slicePron = string.Empty;
            if (!string.IsNullOrEmpty(pron))
            {
                slicePron = RewritePhones2Units(_sliceData, pron);
                if (!string.IsNullOrEmpty(slicePron))
                {
                    slicePron = Regex.Replace(slicePron, @"\.\s+\.", ".");
                }
            }

            return slicePron;
        }

        /// <summary>
        /// Slice pronunciation of one script entry into sliced units.
        /// </summary>
        /// <param name="item">Entry to generate slices.</param>
        public void Slice(ScriptItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            foreach (ScriptWord word in item.AllPronouncedNormalWords)
            {
                string slicePronunciation = SliceWord(word);
                if (!string.IsNullOrEmpty(slicePronunciation))
                {
                    word.Pronunciation = slicePronunciation;
                }
            }
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Build coda units from the phone list.
        /// </summary>
        /// <param name="sliceData">Slice data.</param>
        /// <param name="phones">Phones to process.</param>
        /// <param name="codaOffset">The offset of the first phone in coda group.</param>
        /// <param name="slicedUnits">Unit container to append result coda units.</param>
        private static void BuildCodaUnits(SliceData sliceData,
            TtsMetaPhone[] phones, int codaOffset, List<string> slicedUnits)
        {
            int remainPhoneCount = phones.Length - codaOffset;
            int codaUnitOffset = slicedUnits.Count;

            // t w ih 1 k s t
            if (remainPhoneCount > 0)
            {
                int codaStartCursor = codaOffset;
                while (remainPhoneCount > 0)
                {
                    int phoneCount = remainPhoneCount - (codaStartCursor - codaOffset);
                    string tentativeCoda =
                        TtsMetaPhone.Join(TtsUnit.PhoneDelimiter, phones,
                        codaStartCursor, phoneCount);
                    if (remainPhoneCount != 1 &&
                        sliceData.CodaSlices.IndexOf(tentativeCoda.Replace(TtsUnit.PhoneDelimiter, " ")) < 0 &&
                        phoneCount != 1)
                    {
                        codaStartCursor++;
                    }
                    else
                    {
                        // Left single phone will be treated as coda unit
                        slicedUnits.Insert(codaUnitOffset, TtsUnit.CodaPrefix + tentativeCoda);
                        remainPhoneCount = codaStartCursor - codaOffset;
                        codaStartCursor = codaOffset;
                    }
                }
            }
        }

        #endregion
    }
}