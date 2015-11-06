//----------------------------------------------------------------------------
// <copyright file="ScriptHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements XML script helper class
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
    using System.Xml.Schema;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// ScriptHelper class .
    /// </summary>
    public static class ScriptHelper
    {
        #region public operations

        #region Script sentence ID helper

        /// <summary>
        /// Compare item ID.
        /// </summary>
        /// <param name="itemIdA">Item ID A.</param>
        /// <param name="itemIdB">Item ID B.</param>
        /// <returns>1: ItemIdA bigger than ItemIdB;
        /// -1: ItemIdA smaller than ItemIdB;
        /// 0: ItemIdA equal to ItemIdB.</returns>
        public static int CompareItemId(string itemIdA, string itemIdB)
        {
            if (!ScriptItem.IsValidItemId(itemIdA))
            {
                throw new ArgumentException("itemIdA is invalid");
            }

            if (!ScriptItem.IsValidItemId(itemIdB))
            {
                throw new ArgumentException("itemIdB is invalid");
            }

            return string.Compare(itemIdA, itemIdB, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check whether the sentence ID format is valid.
        /// </summary>
        /// <param name="sentenceId">Sentence ID to be validated.</param>
        /// <returns>Whether the sentence ID is valid.</returns>
        public static bool IsValidSentenceId(string sentenceId)
        {
            if (string.IsNullOrEmpty(sentenceId))
            {
                throw new ArgumentNullException("sentenceId");
            }

            bool validate = true;
            int dashIndex = sentenceId.IndexOf('-');
            if (dashIndex < 0)
            {
                validate = false;
            }

            if (validate)
            {
                validate = ScriptItem.IsValidItemId(sentenceId.Substring(0, dashIndex));
            }

            if (validate)
            {
                int sentenceIndex = 0;
                validate = int.TryParse(sentenceId.Substring(dashIndex + 1), out sentenceIndex);
            }

            return validate;
        }

        /// <summary>
        /// Get item ID from sentence ID.
        /// </summary>
        /// <param name="sentenceId">Sentence ID.</param>
        /// <param name="sentenceIndex">Sentence index, start from 1.</param>
        /// <returns>Item id for the sentence.</returns>
        public static string GetItemIdFromSentenceId(string sentenceId, ref int sentenceIndex)
        {
            if (string.IsNullOrEmpty(sentenceId))
            {
                throw new ArgumentNullException("sentenceId");
            }

            if (!IsValidSentenceId(sentenceId))
            {
                throw new InvalidDataException(Helper.NeutralFormat("Invalid sentence ID format [{0}]", sentenceId));
            }

            int dashIndex = sentenceId.IndexOf('-');
            if (dashIndex < 0)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Invalid sentence Id format : {0}", sentenceId));
            }

            sentenceIndex = int.Parse(sentenceId.Substring(dashIndex + 1), CultureInfo.InvariantCulture);
            return sentenceId.Substring(0, dashIndex);
        }

        /// <summary>
        /// Get item ID from sentence ID.
        /// </summary>
        /// <param name="sentenceId">Sentence ID.</param>
        /// <returns>Item id for the sentence.</returns>
        public static string GetItemIdFromSentenceId(string sentenceId)
        {
            if (string.IsNullOrEmpty(sentenceId))
            {
                throw new ArgumentNullException("sentenceId");
            }

            int sentenceIndex = 0;
            return GetItemIdFromSentenceId(sentenceId, ref sentenceIndex);
        }

        #endregion

        /// <summary>
        /// Scritp word to be displayed.
        /// </summary>
        /// <param name="word">Script word.</param>
        /// <returns>String.</returns>
        public static string BuildDisplayedWordText(ScriptWord word)
        {
            if (word == null)
            {
                throw new ArgumentNullException("word");
            }

            if (string.IsNullOrEmpty(word.Grapheme))
            {
                throw new ArgumentException("word.Grapheme");
            }

            StringBuilder displayedWordText = new StringBuilder();
            displayedWordText.AppendFormat("{0}", word.Grapheme);
            if (word.Emphasis == TtsEmphasis.Yes)
            {
                displayedWordText.Append(" *");
            }

            string breakString = ScriptWord.BreakToString(word.Break);
            if (!string.IsNullOrEmpty(breakString))
            {
                if (word.Break == TtsBreak.Syllable)
                {
                    displayedWordText.Append(" _");
                }
                else
                {
                    displayedWordText.Append(" #");
                }

                displayedWordText.Append(breakString);
            }

            return displayedWordText.ToString();
        }

        /// <summary>
        /// Sync word text to item text.
        /// </summary>
        /// <param name="scriptWord">Script word to be synced.</param>
        public static void SyncWordChangesToItem(ScriptWord scriptWord)
        {
            if (scriptWord == null)
            {
                throw new ArgumentNullException("scriptWord");
            }

            if (scriptWord.Sentence == null)
            {
                throw new ArgumentException("scriptWord.Sentence is null");
            }

            if (scriptWord.Sentence.ScriptItem == null)
            {
                throw new ArgumentException("scriptWord.Sentence.ScriptItem is null");
            }

            SyncItemTextFromWordList(scriptWord.Sentence.ScriptItem);
        }

        /// <summary>
        /// Sync item text from word list.
        /// </summary>
        /// <param name="scriptItem">Script item.</param>
        public static void SyncItemTextFromWordList(ScriptItem scriptItem)
        {
            if (scriptItem == null)
            {
                throw new ArgumentNullException("scriptItem");
            }

            foreach (ScriptSentence scriptSentence in scriptItem.Sentences)
            {
                scriptSentence.Text = scriptSentence.BuildTextFromWords();
            }

            scriptItem.Text = scriptItem.BuildTextFromSentences();
        }

        /// <summary>
        /// Map pronunciation using phonemap.
        /// Only support map syllable based pronunciation.
        /// Todo: Support phone based, unit based pronunciation.
        /// </summary>
        /// <param name="pronunciation">Pronunciation to be mapped.</param>
        /// <param name="phoneMap">Phone map used to map pronunciation.</param>
        /// <returns>Mapped pronunciation.</returns>
        public static string MapPronunciation(string pronunciation, PhoneMap phoneMap)
        {
            if (string.IsNullOrEmpty(pronunciation))
            {
                throw new ArgumentNullException("pronunciation");
            }

            string[] syllables = Pronunciation.SplitIntoSyllables(pronunciation);
            StringBuilder mappedPronunciation = new StringBuilder();
            int lastSyllableEnd = 0;
            int currentSyllableStart = 0;
            foreach (string syllable in syllables)
            {
                currentSyllableStart = pronunciation.IndexOf(syllable, StringComparison.Ordinal);
                Debug.Assert(currentSyllableStart >= 0);
                mappedPronunciation.Append(pronunciation.Substring(lastSyllableEnd,
                    currentSyllableStart - lastSyllableEnd));
                if (phoneMap.Items.ContainsKey(syllable))
                {
                    mappedPronunciation.Append(phoneMap.Items[syllable]);
                }
                else
                {
                    mappedPronunciation.Length = 0;
                    break;
                }

                lastSyllableEnd = currentSyllableStart + syllable.Length;
            }

            return mappedPronunciation.ToString();
        }

        /// <summary>
        /// Merge scripts in a folder into a script file
        /// Error items are removed from the output file.
        /// </summary>
        /// <param name="scriptDir">Dir conataining script file.</param>
        /// <param name="targetFile">Merged file.</param>
        /// <param name="resetId">True means resetting id.</param>
        /// <param name="validateSetting">Validation setting.</param>
        /// <returns>ErrorSet.</returns>
        public static ErrorSet MergeScripts(string scriptDir, string targetFile, bool resetId, XmlScriptValidateSetting validateSetting)
        {
            XmlScriptFile.ContentControler controler = new XmlScriptFile.ContentControler();
            controler.SaveComments = false;
            return MergeScripts(scriptDir, targetFile, resetId, validateSetting, controler);
        }

        /// <summary>
        /// Merge scripts in a folder into a script file.
        /// Error items are removed from the output file.
        /// </summary>
        /// <param name="scriptDir">Dir conataining script file.</param>
        /// <param name="errorSet">Error set.</param>
        /// <param name="resetId">True means resetting id.</param>
        /// <param name="validateSetting">Validation setting.</param>
        /// <param name="contentController">Contenct controller.</param>
        /// <returns>Xml script file.</returns>
        public static XmlScriptFile MergeScripts(string scriptDir, ErrorSet errorSet,
            bool resetId, XmlScriptValidateSetting validateSetting, object contentController)
        {
            if (string.IsNullOrEmpty(scriptDir))
            {
                throw new ArgumentNullException("scriptDir");
            }

            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            if (validateSetting == null)
            {
                throw new ArgumentNullException("validateSetting");
            }

            if (!Directory.Exists(scriptDir))
            {
                throw new DirectoryNotFoundException(scriptDir);
            }

            validateSetting.VerifySetting();
            
            XmlScriptValidationScope scope = validateSetting.ValidationScope;

            string[] subFiles = Directory.GetFiles(
                scriptDir, "*" + XmlScriptFile.Extension, SearchOption.AllDirectories);
            XmlScriptFile mergedScript = new XmlScriptFile();

            long id = 0;
            foreach (string file in subFiles)
            {
                XmlScriptFile script = new XmlScriptFile();
                script.Load(file, contentController);
                if (mergedScript.Language == Language.Neutral)
                {
                    mergedScript.Language = script.Language;
                }
                else if (mergedScript.Language != script.Language)
                {
                    throw new InvalidDataException(Helper.NeutralFormat("Inconsistent langage in {0}", file));
                }

                if (scope != XmlScriptValidationScope.None)
                {
                    script.PosSet = validateSetting.PosSet;
                    script.PhoneSet = validateSetting.PhoneSet;

                    script.Validate(validateSetting);
                    script.Remove(GetNeedDeleteItemIds(script.ErrorSet));
                }

                errorSet.Merge(script.ErrorSet);
                foreach (ScriptItem item in script.Items)
                {
                    item.Id = resetId ? Helper.NeutralFormat("{0:D10}", ++id) : item.Id;

                    ErrorSet addErrors = new ErrorSet();
                    if (!mergedScript.Add(item, addErrors, false))
                    {
                        // Added failed
                        errorSet.Merge(addErrors);
                    }
                }
            }

            return mergedScript;
        }

        /// <summary>
        /// Merge scripts in a folder into a script file.
        /// Error items are removed from the output file.
        /// </summary>
        /// <param name="scriptDir">Dir conataining script file.</param>
        /// <param name="targetFile">Target file path.</param>
        /// <param name="resetId">True means resetting id.</param>
        /// <param name="validateSetting">Validation setting.</param>
        /// <param name="contentController">Contenct controller.</param>
        /// <returns>Error set.</returns>
        public static ErrorSet MergeScripts(string scriptDir, string targetFile, bool resetId, XmlScriptValidateSetting validateSetting, object contentController)
        {
            ErrorSet errorSet = new ErrorSet();
            XmlScriptFile mergedScript = MergeScripts(scriptDir, errorSet, resetId, validateSetting, contentController);
            Helper.EnsureFolderExistForFile(targetFile);
            mergedScript.Save(targetFile, Encoding.Unicode, contentController);
            return errorSet;
        }

        /// <summary>
        /// Get the ids of items that need to be deleted.
        /// </summary>
        /// <param name="errors">The error set.</param>
        /// <returns>The ids.</returns>
        public static Collection<string> GetNeedDeleteItemIds(ErrorSet errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException("errors");
            }

            Collection<string> ids = new Collection<string>();

            foreach (Error error in errors.Errors)
            {
                if (error.Enum.GetType() == typeof(ScriptError) && error.Severity == ErrorSeverity.MustFix)
                {
                    switch ((ScriptError)error.Enum)
                    {
                        // don't need add ScriptError.DuplicateItemId, the duplicated item
                        // has already been removed
                        case ScriptError.PronunciationError:
                        case ScriptError.SentenceSeparatingError:
                        case ScriptError.UnrecognizedPos:
                        case ScriptError.WordBreakingError:
                        case ScriptError.OtherErrors:
                            ids.Add(error.Args[0]);
                            break;
                    }
                }
            }

            return ids;
        }
        
        /// <summary>
        /// Convert two-line script to XML script.
        /// </summary>
        /// <param name="twoLineScript">Input two-line script.</param>
        /// <param name="targetFile">Output script.</param>
        /// <param name="language">Language.</param>
        /// <returns>Errors.</returns>
        public static ErrorSet ConvertTwoLineScriptToXmlScript(string twoLineScript,
            string targetFile, Language language)
        {
            if (string.IsNullOrEmpty(twoLineScript))
            {
                throw new ArgumentNullException("twoLineScript");
            }

            if (string.IsNullOrEmpty(targetFile))
            {
                throw new ArgumentNullException("targetFile");
            }

            if (!Directory.Exists(Path.GetDirectoryName(targetFile)))
            {
                throw new DirectoryNotFoundException(targetFile);
            }

            return ConvertTwoLineScriptToXmlScript(twoLineScript, targetFile, language, false);
        }

        /// <summary>
        /// Convert two-line script to XML script.
        /// </summary>
        /// <param name="twoLineScript">Input two-line script.</param>
        /// <param name="targetFile">Output script.</param>
        /// <param name="language">Language.</param>
        /// <param name="inScriptWithoutPron">Whether input script without pronunciation.</param>
        /// <returns>Errors.</returns>
        public static ErrorSet ConvertTwoLineScriptToXmlScript(string twoLineScript,
            string targetFile, Language language, bool inScriptWithoutPron)
        {
            if (string.IsNullOrEmpty(twoLineScript))
            {
                throw new ArgumentNullException("twoLineScript");
            }

            if (string.IsNullOrEmpty(targetFile))
            {
                throw new ArgumentNullException("targetFile");
            }

            if (!Directory.Exists(Path.GetDirectoryName(targetFile)))
            {
                throw new DirectoryNotFoundException(targetFile);
            }

            ErrorSet errorSet = new ErrorSet();
            Collection<ScriptItem> items = new Collection<ScriptItem>();
            DataErrorSet errors = ScriptFile.ReadAllData(twoLineScript, items, !inScriptWithoutPron, true);
            if (errors.Errors.Count > 0)
            {
                foreach (DataError error in errors.Errors)
                {
                    if (!string.IsNullOrEmpty(error.SentenceId))
                    {
                        errorSet.Add(ScriptError.OtherErrors, error.SentenceId, error.ToString());
                    }
                }
            }

            XmlScriptFile script = new XmlScriptFile(language);
            foreach (ScriptItem item in items)
            {
                ErrorSet itemErrors = new ErrorSet();
                ScriptItem newItem = ConvertScriptItemToXmlFormat(item, inScriptWithoutPron, itemErrors);
                if (itemErrors.Count != 0)
                {
                    errorSet.Merge(itemErrors);
                }
                else
                {
                    script.Items.Add(newItem);
                }
            }

            script.Save(targetFile, Encoding.Unicode);

            return errorSet;
        }

        /// <summary>
        /// Convert XML script to two-line script.
        /// </summary>
        /// <param name="xmlScript">Input XML script.</param>
        /// <param name="targetFile">Output script.</param>
        /// <param name="phoneSet">
        /// Phone set used to convert pronunciation
        /// It can be null when you can directly get the word's pronunciation in the word's attribute.
        /// </param>
        /// <returns>Errors happened.</returns>
        public static ErrorSet ConvertXmlScriptToTwoLineScript(string xmlScript, 
            string targetFile, TtsPhoneSet phoneSet)
        {
            if (string.IsNullOrEmpty(xmlScript))
            {
                throw new ArgumentNullException("xmlScript");
            }

            if (string.IsNullOrEmpty(targetFile))
            {
                throw new ArgumentNullException("targetFile");
            }

            if (!Directory.Exists(Path.GetDirectoryName(targetFile)))
            {
                throw new DirectoryNotFoundException(targetFile);
            }

            ErrorSet errorSet = new ErrorSet();
            XmlScriptFile script = new XmlScriptFile();
            script.Load(xmlScript);
            ScriptFile oldScript = new ScriptFile(script.Language);
            foreach (ScriptItem item in script.Items)
            {
                ErrorSet itemErrors = new ErrorSet();
                ScriptItem oldItem = ConvertScriptItemToTwoLineFormat(item, phoneSet, itemErrors);
                if (itemErrors.Count != 0)
                {
                    errorSet.Merge(itemErrors);
                }
                else
                {
                    oldScript.Items.Add(oldItem.Id, oldItem);
                }
            }

            oldScript.Save(targetFile, true, true);

            return errorSet;
        }

        /// <summary>
        /// Based on script file, build a mono-phone MLF (See HTK document) file.
        /// </summary>
        /// <param name="scriptFilePath">Input script.</param>
        /// <param name="outFilePath">Output mlf file.</param>
        /// <param name="writeToFile">
        /// True means writing to file;
        /// False means checking whether the script can be built to mlf, but not wrting to file.
        /// </param>
        /// <param name="phoneme">Phoneme used to build mlf.</param>
        /// <param name="validateSetting">Validation data set.</param>
        /// <param name="sliceData">Slice data used to get units.</param>
        /// <returns>Errors.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public static ErrorSet BuildMonoMlf(string scriptFilePath, string outFilePath, bool writeToFile,
            Phoneme phoneme, XmlScriptValidateSetting validateSetting, SliceData sliceData)
        {
            if (phoneme == null)
            {
                throw new ArgumentNullException("phoneme");
            }

            if (sliceData == null)
            {
                throw new ArgumentNullException("phoneme");
            }

            if (validateSetting == null)
            {
                throw new ArgumentNullException("validateSetting");
            }

            validateSetting.VerifySetting();

            ErrorSet errors = new ErrorSet();
            StreamWriter sw = null;

            if (writeToFile)
            {
                sw = new StreamWriter(outFilePath, false, Encoding.ASCII);
                sw.WriteLine("#!MLF!#");
            }

            try
            {
                XmlScriptFile script = XmlScriptFile.LoadWithValidation(scriptFilePath, validateSetting);
                script.Remove(GetNeedDeleteItemIds(script.ErrorSet));
                if (script.Items.Count == 0)
                {
                    throw new InvalidDataException(
                        Helper.NeutralFormat("No valid items in {0}.", scriptFilePath));
                }

                errors.Merge(script.ErrorSet);
                foreach (ScriptItem item in script.Items)
                {
                    errors.Merge(BuildMonoMlf(item, sw, writeToFile, phoneme, sliceData));
                }
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }

            if (writeToFile)
            {
                Debug.Assert(HtkTool.VerifyMlfFormat(outFilePath));
            }

            return errors;
        }

        /// <summary>
        /// Given one item, convert
        /// One Phone-based segment file to Unit-based segment file.
        /// </summary>
        /// <param name="item">ScriptItem.</param>
        /// <param name="segmentFilePath">Phone-based segment file.</param>
        /// <param name="targetFilePath">Unit-based segment file.</param>
        /// <param name="ignoreTone">IgnoreTone.</param>
        /// <returns>Data error found.</returns>
        public static Error CombinePhonesToUnits(ScriptItem item, string segmentFilePath,
            string targetFilePath, bool ignoreTone)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            StringBuilder sb = new StringBuilder();
            foreach (ScriptSentence sentence in item.Sentences)
            {
                foreach (ScriptWord word in sentence.Words)
                {
                    if (!string.IsNullOrEmpty(word.Pronunciation))
                    {
                        sb.AppendFormat("{0} / ", word.Pronunciation);
                    }
                }
            }

            string pronunciation = Pronunciation.RemoveStress(sb.ToString());
            string[] slices = item.PronunciationSeparator.SplitSlices(pronunciation);
            Collection<WaveSegment> phoneSegs = SegmentFile.ReadAllData(segmentFilePath);

            Error dataError = null;
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
                        string strTmp = "Data does not align between phone segmentation and pronunciation in CombinePhone";
                        dataError = new Error(ScriptError.OtherErrors, item.Id, Helper.NeutralFormat(strTmp));
                        break;
                    }

                    TtsMetaUnit ttsMetaUnit = new TtsMetaUnit(item.Language);
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

                        slice.Append(ignoreTone ? phone.Name : phone.FullName);
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

        /// <summary>
        /// Renumber script items.
        /// </summary>
        /// <param name="items">Script items collection.</param>
        /// <param name="beginId">Begin item ID.</param>
        /// <param name="idLength">Item ID length.</param>
        [CLSCompliantAttribute(false)]
        public static void Renumber(Collection<ScriptItem> items, ulong beginId, uint idLength)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (idLength == 0)
            {
                throw new ArgumentException(Helper.NeutralFormat("Id length can't be 0"), "idLength");
            }

            ulong currentId = beginId;
            foreach (ScriptItem item in items)
            {
                item.Id = Helper.NeutralFormat("{0:D" + idLength + "}", currentId);
                currentId++;
            }
        }

        /// <summary>
        /// Load all the script items from a folder
        /// Note: Here don't validate the content, But duplicate item ID is not allowed.
        /// </summary>
        /// <param name="sourceDir">Script dir.</param>
        /// <param name="errors">Errors happened.</param>
        /// <returns>Loaded items collection.</returns>
        public static Collection<ScriptItem> LoadScriptsWithoutValidation(string sourceDir, ErrorSet errors)
        {
            if (string.IsNullOrEmpty(sourceDir))
            {
                throw new ArgumentNullException("sourceDir");
            }

            if (errors == null)
            {
                throw new ArgumentNullException("errors");
            }

            Collection<ScriptItem> items = new Collection<ScriptItem>();
            Dictionary<string, string> ids = new Dictionary<string, string>();
            string pattern = @"*" + XmlScriptFile.Extension;
            Language language = Language.Neutral;
            foreach (string file in Directory.GetFiles(sourceDir, pattern, SearchOption.AllDirectories))
            {
                XmlScriptFile script = new XmlScriptFile();
                XmlScriptFile.ContentControler controler = new XmlScriptFile.ContentControler();
                controler.LoadComments = true;
                script.Load(file, controler);
                if (language == Language.Neutral)
                {
                    language = script.Language;
                }
                else if (language != script.Language)
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "The language name in File [{0}] is different from other files.", file));
                }

                errors.Merge(script.ErrorSet);
                foreach (ScriptItem item in script.Items)
                {
                    if (ids.ContainsKey(item.Id))
                    {
                        errors.Add(ScriptError.DuplicateItemId, item.Id);
                    }
                    else
                    {
                        item.ScriptFile = null;
                        items.Add(item);
                    }
                }
            }

            return items;
        }

        /// <summary>
        /// Validate script path, the path can be XML script file path or directory contains XML script files.
        /// </summary>
        /// <param name="scriptPath">Xml script path.</param>
        /// <returns>Whether the script path is validated.</returns>
        public static bool IsValidScriptPath(string scriptPath)
        {
            bool valid = false;
            if (File.Exists(scriptPath))
            {
                if (scriptPath.IsWithFileExtension(FileExtensions.Xml))
                {
                    valid = true;
                }
            }
            else if (Directory.Exists(scriptPath) &&
                Directory.GetFiles(scriptPath,
                FileExtensions.Xml.CreateSearchPatternWithFileExtension()).Length > 0)
            {
                valid = true;
            }

            return valid;
        }

        #endregion

        #region private operation

        /// <summary>
        /// Convert two-line script item to XML format.
        /// </summary>
        /// <param name="item">Two-line format script item.</param>
        /// <param name="inScriptWithoutPron">Whether input script without pronunciation.</param>
        /// <param name="errors">Errors if having.</param>
        /// <returns>New format item.</returns>
        private static ScriptItem ConvertScriptItemToXmlFormat(ScriptItem item,
            bool inScriptWithoutPron, ErrorSet errors)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (errors == null)
            {
                throw new ArgumentNullException("errors");
            }

            ScriptItem newItem = new ScriptItem();
            try
            {
                newItem.Id = item.Id;
                newItem.Text = item.PlainSentence;

                if (!inScriptWithoutPron)
                {
                    ScriptSentence sentence = new ScriptSentence();
                    sentence.Text = item.PlainSentence;

                    foreach (ScriptWord word in item.Words)
                    {
                        switch (word.WordType)
                        {
                            case WordType.Exclamation:
                            case WordType.Period:
                            case WordType.Question:
                            case WordType.OtherPunctuation:
                                word.WordType = WordType.Punctuation;
                                break;
                        }

                        // postag is used for two-line format
                        if (!string.IsNullOrEmpty(word.PosTag))
                        {
                            word.PosString = word.PosTag;
                        }

                        if (word.WordType == WordType.Normal &&
                            string.IsNullOrEmpty(word.Pronunciation))
                        {
                            errors.Add(ScriptError.EmptyPronInNormalWord, item.Id, word.Grapheme);
                        }

                        sentence.Words.Add(word);
                    }

                    if (newItem != null)
                    {
                        newItem.Sentences.Add(sentence);
                    }
                }
            }
            catch (InvalidDataException e)
            {
                errors.Add(ScriptError.OtherErrors, item.Id, Helper.NeutralFormat("Invalid item {0}: {1}", item.Id, Helper.BuildExceptionMessage(e)));
                newItem = null;
            }
            catch (Exception e)
            {
                errors.Add(ScriptError.OtherErrors, item.Id, Helper.NeutralFormat("Error in item: {0}: {1}", item.Id, Helper.BuildExceptionMessage(e)));
                newItem = null;
                if (e == null)
                {
                    throw;
                }
            }

            return newItem;
        }

        /// <summary>
        /// Convert Xml script item to two-line format.
        /// </summary>
        /// <param name="item">Xml format script item.</param>
        /// <param name="phoneSet">Phone set.</param>
        /// <param name="errors">Errors happened.</param>
        /// <returns>Two-line format item.</returns>
        private static ScriptItem ConvertScriptItemToTwoLineFormat(ScriptItem item, 
            TtsPhoneSet phoneSet, ErrorSet errors)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (errors == null)
            {
                throw new ArgumentNullException("errors");
            }

            ScriptItem oldItem = new ScriptItem();
            try
            {
                oldItem.Id = item.Id;
                Collection<ScriptWord> itemWords = new Collection<ScriptWord>();
                foreach (ScriptSentence sentence in item.Sentences)
                {
                    foreach (ScriptWord word in sentence.Words)
                    {
                        if (!string.IsNullOrEmpty(word.PosString))
                        {
                            word.PosTag = word.PosString;
                        }

                        if (word.WordType == WordType.Normal && string.IsNullOrEmpty(word.Pronunciation))
                        {
                            string pron = word.GetPronunciation(phoneSet);
                            if (!string.IsNullOrEmpty(pron))
                            {
                                word.Pronunciation = pron;
                            }
                        }

                        itemWords.Add(word);
                    }
                }

                oldItem.Sentence = ScriptItem.ReverseBuildSentence(itemWords);
                oldItem.Pronunciation = ScriptItem.ReverseBuildPronunciation(itemWords, false);
            }
            catch (InvalidDataException e)
            {
                errors.Add(ScriptError.OtherErrors, item.Id, Helper.NeutralFormat("Invalid item {0}: {1}", item.Id, Helper.BuildExceptionMessage(e)));
                oldItem = null;
            }
            catch (Exception e)
            {
                errors.Add(ScriptError.OtherErrors, item.Id, Helper.NeutralFormat("Error in item: {0}: {1}", item.Id, Helper.BuildExceptionMessage(e)));
                oldItem = null;
                if (e == null)
                {
                    throw;
                }
            }

            return oldItem;
        }

        /// <summary>
        /// Build mlf from script item.
        /// </summary>
        /// <param name="item">Script item.</param>
        /// <param name="sw">Text writer.</param>
        /// <param name="writeToFile">Whether writing to file.</param>
        /// <param name="phoneme">Phoneme.</param>
        /// <param name="sliceData">Slice data.</param>
        /// <returns>Errors.</returns>
        private static ErrorSet BuildMonoMlf(ScriptItem item, StreamWriter sw, 
            bool writeToFile, Phoneme phoneme, SliceData sliceData)
        {
            Debug.Assert(item != null);
            Debug.Assert(phoneme != null);

            if (writeToFile && sw == null)
            {
                throw new ArgumentNullException("sw");
            }

            Collection<ScriptWord> allPronouncedNormalWords = item.AllPronouncedNormalWords;
            ErrorSet errors = new ErrorSet();
            if (allPronouncedNormalWords.Count == 0)
            {
                errors.Add(ScriptError.OtherErrors, item.Id, Helper.NeutralFormat("No pronounced normal word."));
            }
            else
            {
                for (int i = 0; i < allPronouncedNormalWords.Count; i++)
                {
                    ScriptWord word = allPronouncedNormalWords[i];
                    Debug.Assert(word != null);
                    if (string.IsNullOrEmpty(word.Pronunciation))
                    {
                        errors.Add(ScriptError.OtherErrors, item.Id, Helper.NeutralFormat("No pronunciation normal word '{1}' in script item {0}.", item.Id, word.Grapheme));
                    }
                }

                if (errors.Count == 0)
                {
                    if (writeToFile)
                    {
                        sw.WriteLine("\"*/{0}.lab\"", item.Id);
                        sw.WriteLine(Phoneme.SilencePhone);
                    }

                    for (int i = 0; i < allPronouncedNormalWords.Count; i++)
                    {
                        ScriptWord word = allPronouncedNormalWords[i];
                        Collection<TtsUnit> units = word.GetUnits(phoneme, sliceData);
                        if (phoneme.Tts2srMapType == Phoneme.TtsToSrMappingType.PhoneBased)
                        {
                            foreach (TtsUnit unit in units)
                            {
                                errors.Merge(BuildMonoMlf(unit, item, sw, writeToFile, phoneme));
                            }
                        }
                        else if (phoneme.Tts2srMapType == Phoneme.TtsToSrMappingType.SyllableBased)
                        {
                            foreach (ScriptSyllable syllable in word.UnitSyllables)
                            {
                                errors.Merge(BuildMonoMlf(syllable, item, sw, writeToFile, phoneme));
                            }
                        }

                        if (writeToFile && i + 1 < allPronouncedNormalWords.Count)
                        {
                            sw.WriteLine(Phoneme.ShortPausePhone);
                        }
                    }

                    if (writeToFile)
                    {
                        sw.WriteLine(Phoneme.SilencePhone);
                        sw.WriteLine(".");  // end of sentence
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Build mlf from syllable.
        /// </summary>
        /// <param name="syllable">Syllable.</param>
        /// <param name="item">Script item.</param>
        /// <param name="sw">Text writer.</param>
        /// <param name="writeToFile">Whethe writing to file.</param>
        /// <param name="phoneme">Phoneme.</param>
        /// <returns>Errors.</returns>
        private static ErrorSet BuildMonoMlf(ScriptSyllable syllable, ScriptItem item, StreamWriter sw,
            bool writeToFile, Phoneme phoneme)
        {
            Debug.Assert(syllable != null);
            Debug.Assert(item != null);

            ErrorSet errors = new ErrorSet();
            string syllableText = Pronunciation.RemoveStress(syllable.Text.Trim());
            string[] srPhones = phoneme.Tts2SrPhones(syllableText.Trim());
            if (srPhones == null)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Invalid TTS syllable[{0}], which can not be converted to Speech Recognition Phone.",
                     syllableText);
                errors.Add(ScriptError.OtherErrors, item.Id, message);
            }

            if (writeToFile && srPhones != null)
            {
                foreach (string phone in srPhones)
                {
                    sw.WriteLine(phone);
                }
            }

            return errors;
        }

        /// <summary>
        /// Build mlf from unit.
        /// </summary>
        /// <param name="unit">Unit.</param>
        /// <param name="item">Script item.</param>
        /// <param name="sw">Text writer.</param>
        /// <param name="writeToFile">Whethe writing to file.</param>
        /// <param name="phoneme">Phoneme.</param>
        /// <returns>Errors.</returns>
        private static ErrorSet BuildMonoMlf(TtsUnit unit, ScriptItem item, StreamWriter sw, 
            bool writeToFile, Phoneme phoneme)
        {
            Debug.Assert(unit != null);
            Debug.Assert(item != null);

            ErrorSet errors = new ErrorSet();
            List<string> allPhones = new List<string>();
            foreach (TtsMetaPhone phone in unit.MetaUnit.Phones)
            {
                string[] srPhones = phoneme.Tts2SrPhones(phone.Name);
                if (srPhones == null)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid TTS phone[{0}], which can not be converted to Speech Recognition Phone.",
                        phone.Name);
                    errors.Add(ScriptError.OtherErrors, item.Id, message);
                    continue;
                }

                allPhones.AddRange(srPhones);
            }

            if (writeToFile)
            {
                foreach (string phone in allPhones)
                {
                    sw.WriteLine(phone);
                }
            }

            return errors;
        }

        #endregion
    }
}