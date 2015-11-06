//----------------------------------------------------------------------------
// <copyright file="ScriptItemComment.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements ScriptItemComment for ScriptItem
// </summary>
//----------------------------------------------------------------------------

namespace ScriptReviewer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Xml script comment helper.
    /// </summary>
    public static class XmlScriptCommentHelper
    {
        #region Public fields

        /// <summary>
        /// Comment to store whether item has been approved.
        /// </summary>
        public const string ItemApproveCommentName = "approve";

        /// <summary>
        /// Words pronunciation status XML element name.
        /// </summary>
        public const string WordPronStatusName = "p";

        /// <summary>
        /// Words TCGPP score status XML element name.
        /// </summary>
        public const string WordTcgppScoreStatusName = "tcgppScore";

        /// <summary>
        /// Words text status XML element name.
        /// </summary>
        public const string WordTextStatusName = "v";

        /// <summary>
        /// High severity of the operation.
        /// </summary>
        public const string HighSecverity = "high";

        /// <summary>
        /// Low severity of the operation.
        /// </summary>
        public const string LowSecverity = "low";

        #endregion

        /// <summary>
        /// Script item status.
        /// </summary>
        public enum ScriptItemStatus
        {
            /// <summary>
            /// Not in any status.
            /// </summary>
            Original,

            /// <summary>
            /// Approved.
            /// </summary>
            Approved,

            /// <summary>
            /// Rejected.
            /// </summary>
            Reject
        }

        /// <summary>
        /// Get original sentence word list.
        /// </summary>
        /// <param name="scriptSentence">Script sentence.</param>
        /// <returns>Original word list.</returns>
        public static Collection<ScriptWord> GetOriginalWordList(ScriptSentence scriptSentence)
        {
            List<ScriptWord> displayedWords = new List<ScriptWord>();
            displayedWords.AddRange(scriptSentence.Words);

            foreach (ScriptWord deletedWord in scriptSentence.DeletedWordsDict.Keys)
            {
                if (displayedWords.Contains(deletedWord))
                {
                    continue;
                }

                List<ScriptWord> deletedWords = new List<ScriptWord>();
                deletedWords.Add(deletedWord);
                ScriptWord nextDeletedWord = deletedWord;
                while (scriptSentence.DeletedWordAndFollowingWordDict.ContainsKey(nextDeletedWord))
                {
                    nextDeletedWord = scriptSentence.DeletedWordAndFollowingWordDict[nextDeletedWord];
                    if (nextDeletedWord == null || displayedWords.IndexOf(nextDeletedWord) >= 0)
                    {
                        break;
                    }

                    deletedWords.Add(nextDeletedWord);
                }

                Debug.Assert(nextDeletedWord == null || displayedWords.IndexOf(nextDeletedWord) >= 0);
                if (nextDeletedWord == null)
                {
                    displayedWords.AddRange(deletedWords);
                }
                else
                {
                    int insertPosition = displayedWords.IndexOf(nextDeletedWord);
                    Debug.Assert(insertPosition >= 0);
                    displayedWords.InsertRange(insertPosition, deletedWords);
                }
            }

            Collection<ScriptWord> addedWords = new Collection<ScriptWord>();
            foreach (ScriptWord scriptWord in displayedWords)
            {
                if (GetScriptWordEditStatus(scriptWord) == TtsXmlStatus.EditStatus.Add)
                {
                    addedWords.Add(scriptWord);
                }
            }

            foreach (ScriptWord scriptWord in addedWords)
            {
                displayedWords.Remove(scriptWord);
            }

            return new Collection<ScriptWord>(displayedWords);
        }

        /// <summary>
        /// ScriptItemStatus display string.
        /// </summary>
        /// <param name="status">ScriptItemStatus.</param>
        /// <returns>Script item display string.</returns>
        public static string GetItemStatusActionDisplayString(ScriptItemStatus status)
        {
            string displayString = string.Empty;
            if (status == ScriptItemStatus.Approved)
            {
                displayString = "approve";
            }
            else if (status == ScriptItemStatus.Reject)
            {
                displayString = "reject";
            }
            else if (status == ScriptItemStatus.Original)
            {
                displayString = "reset";
            }

            return displayString;
        }

        /// <summary>
        /// Get ChangeType string.
        /// </summary>
        /// <param name="status">Script item status.</param>
        /// <returns>Change type string.</returns>
        public static string GetChangeTypeString(ScriptItemStatus status)
        {
            string displayString = string.Empty;
            if (status == ScriptItemStatus.Approved)
            {
                displayString = "Approved items";
            }
            else if (status == XmlScriptCommentHelper.ScriptItemStatus.Reject)
            {
                displayString = "Rejected items";
            }
            else if (status == XmlScriptCommentHelper.ScriptItemStatus.Original)
            {
                displayString = "Unchecked items";
            }

            return displayString;
        }

        /// <summary>
        /// Get ChangeType string.
        /// </summary>
        /// <param name="wordEditStatus">Edit status.</param>
        /// <param name="wordTextChanged">Word text changed.</param>
        /// <param name="wordPronChanged">Word pronunciation changed.</param>
        /// <returns>Change type string.</returns>
        public static string GetChangeTypeString(TtsXmlStatus.EditStatus wordEditStatus,
            bool wordTextChanged, bool wordPronChanged)
        {
            string displayString = string.Empty;
            if (wordEditStatus == TtsXmlStatus.EditStatus.Add)
            {
                displayString = "Word inserted";
            }
            else if (wordEditStatus == TtsXmlStatus.EditStatus.Modify)
            {
                if (wordTextChanged && wordPronChanged)
                {
                    displayString = "Word text/pronunciation changed";
                }
                else if (wordTextChanged)
                {
                    displayString = "Word text changed";
                }
                else if (wordPronChanged)
                {
                    displayString = "Word pronunciation changed";
                }
            }
            else if (wordEditStatus == TtsXmlStatus.EditStatus.Delete)
            {
                displayString = "Word deleted";
            }
            else if (wordEditStatus == TtsXmlStatus.EditStatus.Original)
            {
                displayString = "Word original";
            }

            return displayString;
        }

        /// <summary>
        /// Find deleted word index, when delete more than one words in the same position.
        /// For example:
        /// Original sentence:  A, B, C, D.
        /// Current sentence:   A, D.
        /// Then:   Both B and C's deleted word position is 1.
        ///         But B's deleted word index is 0, and C's deleted word index is 1.
        /// </summary>
        /// <param name="scriptWord">Script word to be checked.</param>
        /// <returns>The word index.</returns>
        public static int GetDeletedWordIndex(ScriptWord scriptWord)
        {
            ScriptSentence scriptSentence = scriptWord.Sentence;
            Debug.Assert(scriptSentence != null);
            Debug.Assert(scriptSentence.DeletedWordsDict.ContainsKey(scriptWord));

            ScriptWord followingWord = scriptWord;
            int deletedIndex = -1;
            do
            {
                followingWord = scriptSentence.DeletedWordAndFollowingWordDict[followingWord];
                deletedIndex++;
            }
            while (followingWord != null && !scriptSentence.Words.Contains(followingWord));

            return deletedIndex;
        }

        /// <summary>
        /// Find the deleted word position in original sentence.
        /// For example:
        /// Original sentence:  A, B, C, D.
        /// Current sentence:   A, D.
        /// Then:   Both B and C's deleted word position is 1.
        ///         But B's deleted word index is 0, and C's deleted word index is 1.
        /// </summary>
        /// <param name="scriptWord">Script word to be checked.</param>
        /// <returns>Deleted word position in original word, if revert this deleted word,
        /// should insert this word to this position in the sentence word list.</returns>
        public static int GetDeletedWordPosition(ScriptWord scriptWord)
        {
            ScriptSentence scriptSentence = scriptWord.Sentence;
            Debug.Assert(scriptSentence != null);
            Debug.Assert(scriptSentence.DeletedWordsDict.ContainsKey(scriptWord));

            ScriptWord followingWord = scriptWord;
            do
            {
                followingWord = scriptSentence.DeletedWordAndFollowingWordDict[followingWord];
            }
            while (followingWord != null && !scriptSentence.Words.Contains(followingWord));

            int position = TtsXmlStatus.UnsetPosition;
            if (followingWord == null)
            {
                position = scriptWord.Sentence.Words.Count;
            }
            else
            {
                position = scriptWord.Sentence.Words.IndexOf(followingWord);
            }

            return position;
        }

        /// <summary>
        /// Undo changes in script item.
        /// </summary>
        /// <param name="scriptItem">Script item to undo.</param>
        public static void UndoChangeInScript(ScriptItem scriptItem)
        {
            SetApproved(scriptItem, ScriptItemStatus.Original);
        }

        /// <summary>
        /// Undo changes in script.
        /// </summary>
        /// <param name="scriptWord">Word of which the comment to be undo.</param>
        public static void UndoChangeInScript(ScriptWord scriptWord)
        {
            TtsXmlStatus.EditStatus wordStatus = GetScriptWordEditStatus(scriptWord);
            if (wordStatus == TtsXmlStatus.EditStatus.Add)
            {
                scriptWord.Sentence.DeleteWord(scriptWord);
            }
            else if (wordStatus == TtsXmlStatus.EditStatus.Delete)
            {
                ScriptWord afterScriptWord = scriptWord;

                do
                {
                    Debug.Assert(scriptWord.Sentence.DeletedWordAndFollowingWordDict.ContainsKey(afterScriptWord));
                    afterScriptWord = scriptWord.Sentence.DeletedWordAndFollowingWordDict[afterScriptWord];
                }
                while (afterScriptWord != null && !scriptWord.Sentence.Words.Contains(afterScriptWord));

                if (afterScriptWord == null)
                {
                    scriptWord.Sentence.InsertWord(scriptWord, scriptWord.Sentence.Words.Count);
                }
                else
                {
                    scriptWord.Sentence.InsertWord(scriptWord, scriptWord.Sentence.Words.IndexOf(afterScriptWord));
                }

                scriptWord.Sentence.DeletedWordsDict.Remove(scriptWord);
                scriptWord.Sentence.DeletedWordAndFollowingWordDict.Remove(scriptWord);
            }
            else if (wordStatus == TtsXmlStatus.EditStatus.Modify)
            {
                TtsXmlStatus wordTextStatus = GetWordCommentStatus(scriptWord, WordTextStatusName);

                // Check edit status
                if (wordTextStatus != null && wordTextStatus.Status == TtsXmlStatus.EditStatus.Modify)
                {
                    scriptWord.Grapheme = wordTextStatus.OriginalValue;
                    scriptWord.TtsXmlComments.RemoveStatus(XmlScriptCommentHelper.WordTextStatusName);
                }

                TtsXmlStatus wordPronStatus = GetWordCommentStatus(scriptWord, WordPronStatusName);

                // Check edit status
                if (wordPronStatus != null && wordPronStatus.Status == TtsXmlStatus.EditStatus.Modify)
                {
                    scriptWord.Pronunciation = wordPronStatus.OriginalValue;
                    scriptWord.TtsXmlComments.RemoveStatus(XmlScriptCommentHelper.WordPronStatusName);

                    TtsXmlStatus wordTcgppStatus = GetWordCommentStatus(
                        scriptWord, XmlScriptCommentHelper.WordTcgppScoreStatusName);
                    Debug.Assert(wordTcgppStatus.Status == TtsXmlStatus.EditStatus.Modify);
                    if (wordTcgppStatus != null)
                    {
                        scriptWord.TcgppScores = wordTcgppStatus.OriginalValue;
                        scriptWord.TtsXmlComments.RemoveStatus(XmlScriptCommentHelper.WordTcgppScoreStatusName);
                    }
                }
            }
        }

        /// <summary>
        /// Get word edit status.
        /// </summary>
        /// <param name="scriptWord">Script word to get status.</param>
        /// <returns>Edit status of the word.</returns>
        public static TtsXmlStatus.EditStatus GetScriptWordEditStatus(ScriptWord scriptWord)
        {
            if (scriptWord == null)
            {
                throw new ArgumentNullException("scriptWord");
            }

            TtsXmlStatus status = GetScriptWordStatus(scriptWord);

            return status == null ? TtsXmlStatus.EditStatus.Original : status.Status;
        }

        /// <summary>
        /// Get script item status.
        /// </summary>
        /// <param name="scriptItem">Script item to be checked.</param>
        /// <returns>ScriptItemStatus.</returns>
        public static ScriptItemStatus GetScriptItemStatus(ScriptItem scriptItem)
        {
            string timeStamp;
            return GetScriptItemStatus(scriptItem, out timeStamp);
        }

        /// <summary>
        /// Get script item status.
        /// </summary>
        /// <param name="scriptItem">Script item to be checked.</param>
        /// <param name="timeStamp">Time stamp of the operation.</param>
        /// <returns>ScriptItemStatus.</returns>
        public static ScriptItemStatus GetScriptItemStatus(ScriptItem scriptItem, out string timeStamp)
        {
            timeStamp = string.Empty;
            ScriptItemStatus status = ScriptItemStatus.Original;

            if (scriptItem.ScriptFile.DeletedItemsDict.ContainsKey(scriptItem))
            {
                status = ScriptItemStatus.Reject;
                timeStamp = scriptItem.ScriptFile.DeletedItemsDict[scriptItem].Timestamp;
            }
            else
            {
                TtsXmlComment comment = scriptItem.TtsXmlComments.GetSingleComment(ItemApproveCommentName);
                if (comment != null)
                {
                    bool approved = false;
                    if (bool.TryParse(comment.Value, out approved) && approved)
                    {
                        status = ScriptItemStatus.Approved;
                        timeStamp = comment.Timestamp;
                    }
                }
            }

            return status;
        }

        /// <summary>
        /// Set script items to approve status.
        /// </summary>
        /// <param name="scriptItem">Script items to be set.</param>
        /// <param name="status">Whether approve or reject the script item.</param>
        public static void SetApproved(ScriptItem scriptItem, ScriptItemStatus status)
        {
            scriptItem.TtsXmlComments.RemoveComment(ItemApproveCommentName);

            if (status == ScriptItemStatus.Approved || status == ScriptItemStatus.Original)
            {
                if (scriptItem.ScriptFile.DeletedItemsDict.ContainsKey(scriptItem))
                {
                    scriptItem.ScriptFile.DeletedItemsDict.Remove(scriptItem);
                    ErrorSet errorSet = new ErrorSet();
                    scriptItem.ScriptFile.Add(scriptItem, errorSet, false, true);
                }

                if (status == ScriptItemStatus.Approved)
                {
                    TtsXmlComment comment = new TtsXmlComment(ItemApproveCommentName, bool.TrueString);
                    scriptItem.TtsXmlComments.AppendComment(comment, false);
                }
            }
            else if (status == ScriptItemStatus.Reject)
            {
                TtsXmlStatus ttsXmlStatus = new TtsXmlStatus(XmlScriptFile.DeletedItemStatusName, TtsXmlStatus.EditStatus.Delete);
                if (scriptItem.ScriptFile.Items.Contains(scriptItem))
                {
                    scriptItem.ScriptFile.Remove(scriptItem.Id);
                    scriptItem.ScriptFile.DeletedItemsDict.Add(scriptItem, ttsXmlStatus);
                }
            }
        }

        /// <summary>
        /// Check whether script item is approved.
        /// </summary>
        /// <param name="scriptItem">Script item to be checked.</param>
        /// <returns>Whether the script item is approved.</returns>
        public static bool CheckScriptItemApproved(ScriptItem scriptItem)
        {
            if (scriptItem == null)
            {
                throw new ArgumentNullException("scriptItem");
            }

            TtsXmlComment comment = scriptItem.TtsXmlComments.GetSingleComment(ItemApproveCommentName);

            bool hasBeenApproved = false;
            if (comment != null)
            {
                Debug.Assert(!string.IsNullOrEmpty(comment.Value));
                hasBeenApproved = bool.Parse(comment.Value);
            }

            return hasBeenApproved;
        }

        /// <summary>
        /// Get word pronunciation status.
        /// </summary>
        /// <param name="scriptWord">Script word to check.</param>
        /// <param name="commentName">Comment name.</param>
        /// <returns>Tts xml status.</returns>
        public static TtsXmlStatus GetWordCommentStatus(ScriptWord scriptWord, string commentName)
        {
            TtsXmlStatus status = scriptWord.TtsXmlComments.GetSingleStatus(TtsXmlComments.SelfStatusName);
            if (status == null || status.Status != TtsXmlStatus.EditStatus.Add)
            {
                if (scriptWord.Sentence.DeletedWordsDict.ContainsKey(scriptWord))
                {
                    status = new TtsXmlStatus();
                    status.Status = TtsXmlStatus.EditStatus.Delete;
                }
                else
                {
                    status = scriptWord.TtsXmlComments.GetSingleStatus(commentName);
                    if (status != null)
                    {
                        Debug.Assert(status.Status == TtsXmlStatus.EditStatus.Modify);
                    }
                }
            }

            return status;
        }

        /// <summary>
        /// Get word status.
        /// </summary>
        /// <param name="scriptWord">Script word to get status.</param>
        /// <returns>Edit status of the word.</returns>
        public static TtsXmlStatus GetScriptWordStatus(ScriptWord scriptWord)
        {
            if (scriptWord == null)
            {
                throw new ArgumentNullException("scriptWord");
            }

            Debug.Assert(scriptWord.Sentence != null);
            TtsXmlStatus ttsXmlStatus = null;

            if (scriptWord.Sentence.DeletedWordsDict.ContainsKey(scriptWord))
            {
                // Check delete status.
                ttsXmlStatus = scriptWord.Sentence.DeletedWordsDict[scriptWord];
            }
            else if (scriptWord.TtsXmlComments.TtsXmlStatusDict.ContainsKey(TtsXmlComments.SelfStatusName) &&
                scriptWord.TtsXmlComments.TtsXmlStatusDict[TtsXmlComments.SelfStatusName].Count > 0 &&
                scriptWord.TtsXmlComments.TtsXmlStatusDict[TtsXmlComments.SelfStatusName][0].Status ==
                TtsXmlStatus.EditStatus.Add)
            {
                // Check add status.
                Debug.Assert(scriptWord.TtsXmlComments.TtsXmlStatusDict[TtsXmlComments.SelfStatusName].Count == 1);
                ttsXmlStatus = scriptWord.TtsXmlComments.TtsXmlStatusDict[TtsXmlComments.SelfStatusName][0];
            }
            else
            {
                ttsXmlStatus = GetWordCommentStatus(scriptWord, WordTextStatusName);

                // Check edit status
                if (ttsXmlStatus != null)
                {
                    Debug.Assert(ttsXmlStatus.Status == TtsXmlStatus.EditStatus.Modify);
                }
                else
                {
                    ttsXmlStatus = GetWordCommentStatus(scriptWord, WordPronStatusName);

                    // Check edit status
                    if (ttsXmlStatus != null)
                    {
                        Debug.Assert(ttsXmlStatus.Status == TtsXmlStatus.EditStatus.Modify);
                    }
                }
            }

            return ttsXmlStatus;
        }

        /// <summary>
        /// Delete script word from sentence.
        /// </summary>
        /// <param name="scriptWord">Script word to be deleted.</param>
        public static void DeleteScriptWord(ScriptWord scriptWord)
        {
            if (scriptWord == null)
            {
                throw new ArgumentNullException("scriptWord");
            }

            if (scriptWord.Sentence == null)
            {
                throw new ArgumentNullException("scriptWord");
            }

            TtsXmlStatus status = new TtsXmlStatus(ScriptSentence.DeletedWordStatusName, TtsXmlStatus.EditStatus.Delete);
            int deletedPosition = scriptWord.Sentence.DeleteWord(scriptWord);

            ScriptWord preDeletedWord = null;
            if (deletedPosition < scriptWord.Sentence.Words.Count)
            {
                preDeletedWord = scriptWord.Sentence.Words[deletedPosition];
            }

            // Find first deleted word.
            do
            {
                foreach (ScriptWord deletedWord in scriptWord.Sentence.DeletedWordAndFollowingWordDict.Keys)
                {
                    if (scriptWord.Sentence.DeletedWordAndFollowingWordDict[deletedWord] == preDeletedWord)
                    {
                        preDeletedWord = deletedWord;
                        break;
                    }
                }
            }
            while (scriptWord.Sentence.DeletedWordAndFollowingWordDict.ContainsValue(preDeletedWord));

            scriptWord.Sentence.DeletedWordsDict.Add(scriptWord, status);
            scriptWord.Sentence.DeletedWordAndFollowingWordDict.Add(scriptWord, preDeletedWord);
        }

        /// <summary>
        /// Submit word's changes.
        /// </summary>
        /// <param name="scriptWord">Script word.</param>
        public static void SubmitChanges(ScriptWord scriptWord)
        {
            scriptWord.TtsXmlComments.TtsXmlStatusDict.Clear();
        }

        /// <summary>
        /// Insert word text.
        /// </summary>
        /// <param name="nearWord">Insert the word near this word.</param>
        /// <param name="insertBefore">Insert before or after the near word.</param>
        /// <param name="wordText">Word text to add.</param>
        /// <param name="pronText">Word pronunciation to add.</param>
        /// <param name="isPunctuation">Whether the word is punctuation.</param>
        /// <param name="highSeverity">Insert secverity.</param>
        /// <returns>ScriptWord.</returns>
        public static ScriptWord InsertWordText(ScriptWord nearWord, bool insertBefore,
            string wordText, string pronText, bool isPunctuation, bool highSeverity)
        {
            if (nearWord == null)
            {
                throw new ArgumentNullException("nearWord");
            }

            if (string.IsNullOrEmpty(wordText))
            {
                throw new ArgumentNullException("wordText");
            }

            if (string.IsNullOrEmpty(pronText) && !isPunctuation)
            {
                throw new ArgumentNullException("pronText");
            }

            ScriptWord scriptWord = new ScriptWord(nearWord.Language);
            scriptWord.Grapheme = wordText;
            scriptWord.Pronunciation = pronText;
            scriptWord.WordType = isPunctuation ? WordType.Punctuation : WordType.Normal;
            TtsXmlStatus status = new TtsXmlStatus(TtsXmlComments.SelfStatusName, TtsXmlStatus.EditStatus.Add);
            if (highSeverity)
            {
                status.Severity = XmlScriptCommentHelper.HighSecverity;
            }

            scriptWord.TtsXmlComments.AppendStatus(status, false);
            int position = nearWord.Sentence.Words.IndexOf(nearWord);
            if (!insertBefore)
            {
                position += 1;
            }

            nearWord.Sentence.InsertWord(scriptWord, position);
            return scriptWord;
        }

        /// <summary>
        /// Modify word's pronunciation text.
        /// </summary>
        /// <param name="scriptWord">Script word to be modified.</param>
        /// <param name="pronText">Pronunciation text to be modified.</param>
        /// <returns>Whether the word's pronciation has been modified.</returns>
        public static bool ModifyPronText(ScriptWord scriptWord, string pronText)
        {
            return ModifyPronText(scriptWord, pronText, string.Empty);
        }

        /// <summary>
        /// Modify word's pronunciation text.
        /// </summary>
        /// <param name="scriptWord">Script word to be modified.</param>
        /// <param name="pronText">Pronunciation text to be modified.</param>
        /// <param name="severity">Severity.</param>
        /// <returns>Whether the word's pronciation has been modified.</returns>
        public static bool ModifyPronText(ScriptWord scriptWord, string pronText, string severity)
        {
            if (scriptWord == null)
            {
                throw new ArgumentNullException("scriptWord");
            }

            if (string.IsNullOrEmpty(pronText))
            {
                throw new ArgumentNullException("pronText");
            }

            bool updated = false;
            TtsXmlStatus pronStatus = scriptWord.TtsXmlComments.GetSingleStatus(WordPronStatusName);

            if (pronStatus != null)
            {
                if (pronStatus.Status == TtsXmlStatus.EditStatus.Delete)
                {
                    updated = false;
                }
                else if (!pronText.Equals(scriptWord.Pronunciation, StringComparison.Ordinal))
                {
                    updated = true;
                    scriptWord.Pronunciation = pronText;

                    if (pronStatus.Status == TtsXmlStatus.EditStatus.Original)
                    {
                        pronStatus.Status = TtsXmlStatus.EditStatus.Modify;
                        pronStatus.OriginalValue = scriptWord.Pronunciation;
                        scriptWord.TtsXmlComments.RemoveStatus(WordTcgppScoreStatusName);
                        if (!string.IsNullOrEmpty(scriptWord.TcgppScores))
                        {
                            TtsXmlStatus tcgppStatus = new TtsXmlStatus(WordTcgppScoreStatusName,
                                TtsXmlStatus.EditStatus.Modify);
                            tcgppStatus.OriginalValue = scriptWord.TcgppScores;
                            scriptWord.TtsXmlComments.AppendStatus(tcgppStatus, false);
                            scriptWord.TcgppScores = string.Empty;
                        }
                    }
                    else if (pronStatus.Status == TtsXmlStatus.EditStatus.Modify &&
                        pronText.Equals(pronStatus.OriginalValue, StringComparison.Ordinal))
                    {
                        TtsXmlStatus tcgppStatus = new TtsXmlStatus(WordTcgppScoreStatusName,
                            TtsXmlStatus.EditStatus.Modify);
                        Debug.Assert(tcgppStatus != null && tcgppStatus.Status == TtsXmlStatus.EditStatus.Modify);
                        scriptWord.TcgppScores = tcgppStatus.OriginalValue;
                        scriptWord.TtsXmlComments.RemoveStatus(WordPronStatusName);
                    }
                }

                if (UpdateStatusSeverity(pronStatus, severity))
                {
                    updated = true;
                }
            }
            else if (!pronText.Equals(scriptWord.Pronunciation, StringComparison.Ordinal))
            {
                updated = true;
                pronStatus = new TtsXmlStatus(WordPronStatusName, TtsXmlStatus.EditStatus.Modify);
                pronStatus.OriginalValue = scriptWord.Pronunciation;
                pronStatus.Tag = scriptWord.TtsXmlComments;
                scriptWord.Pronunciation = pronText;
                scriptWord.TtsXmlComments.AppendStatus(pronStatus, false);

                if (!string.IsNullOrEmpty(scriptWord.TcgppScores))
                {
                    TtsXmlStatus tcgppStatus = new TtsXmlStatus(WordTcgppScoreStatusName, TtsXmlStatus.EditStatus.Modify);
                    tcgppStatus.OriginalValue = scriptWord.TcgppScores;
                    scriptWord.TtsXmlComments.AppendStatus(tcgppStatus, false);
                    scriptWord.TcgppScores = string.Empty;
                }

                if (UpdateStatusSeverity(pronStatus, severity))
                {
                    updated = true;
                }
            }

            return updated;
        }

        /// <summary>
        /// Update status severity.
        /// </summary>
        /// <param name="status">TtsXmlStatus to be updated.</param>
        /// <param name="severity">Severity to be updated.</param>
        /// <returns>Whether the severity has been updated.</returns>
        public static bool UpdateStatusSeverity(TtsXmlStatus status, string severity)
        {
            bool updated = false;
            if (!string.IsNullOrEmpty(severity))
            {
                string oldSeverity = status.Severity;
                if (string.IsNullOrEmpty(oldSeverity))
                {
                    oldSeverity = XmlScriptCommentHelper.LowSecverity;
                }

                if (!severity.Equals(oldSeverity, StringComparison.OrdinalIgnoreCase))
                {
                    status.Severity = severity;
                    if (status.Severity.Equals(XmlScriptCommentHelper.LowSecverity, StringComparison.OrdinalIgnoreCase))
                    {
                        status.Severity = string.Empty;
                    }

                    updated = true;
                }
            }

            return updated;
        }

        /// <summary>
        /// Modify word text.
        /// </summary>
        /// <param name="scriptWord">Words to be modified.</param>
        /// <param name="wordText">Word text to modify.</param>
        /// <returns>Whether the word text has been modified.</returns>
        public static bool ModifyWordText(ScriptWord scriptWord, string wordText)
        {
            return ModifyWordText(scriptWord, wordText, string.Empty);
        }

        /// <summary>
        /// Modify word text.
        /// </summary>
        /// <param name="scriptWord">Words to be modified.</param>
        /// <param name="wordText">Word text to modify.</param>
        /// <param name="severity">Severity.</param>
        /// <returns>Whether the word text has been modified.</returns>
        public static bool ModifyWordText(ScriptWord scriptWord, string wordText, string severity)
        {
            if (scriptWord == null)
            {
                throw new ArgumentNullException("scriptWord");
            }

            if (string.IsNullOrEmpty(wordText))
            {
                throw new ArgumentNullException("wordText");
            }

            bool updated = false;
            TtsXmlStatus status = scriptWord.TtsXmlComments.GetSingleStatus(WordTextStatusName);
            if (status != null)
            {
                if (status.Status == TtsXmlStatus.EditStatus.Delete)
                {
                    // Not update when in deleted status
                    updated = false;
                }
                else if (!wordText.Equals(scriptWord.Grapheme, StringComparison.Ordinal))
                {
                    updated = true;
                    if (status.Status == TtsXmlStatus.EditStatus.Original)
                    {
                        status.Status = TtsXmlStatus.EditStatus.Modify;
                        status.OriginalValue = scriptWord.Grapheme;
                    }
                    else if (status.Status == TtsXmlStatus.EditStatus.Modify &&
                        wordText.Equals(status.OriginalValue, StringComparison.Ordinal))
                    {
                        scriptWord.TtsXmlComments.RemoveStatus(WordTextStatusName);
                    }

                    scriptWord.Grapheme = wordText;
                }

                if (UpdateStatusSeverity(status, severity))
                {
                    updated = true;
                }
            }
            else if (!wordText.Equals(scriptWord.Grapheme, StringComparison.Ordinal))
            {
                updated = true;
                status = new TtsXmlStatus(WordTextStatusName, TtsXmlStatus.EditStatus.Modify);
                status.Tag = scriptWord.TtsXmlComments;
                status.OriginalValue = scriptWord.Grapheme;
                scriptWord.Grapheme = wordText;
                scriptWord.TtsXmlComments.AppendStatus(status, false);
                if (UpdateStatusSeverity(status, severity))
                {
                    updated = true;
                }
            }

            return updated;
        }
    }

    /// <summary>
    /// Listitem attribute for Column information for ListView.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class ListItemAttribute : Attribute
    {
        #region Fileds

        private int _index;
        private string _name;
        private int _width;

        #endregion

        #region Properies

        /// <summary>
        /// Gets or sets Width.
        /// </summary>
        public int Width
        {
            get { return _width; }
            set { _width = value; }
        }

        /// <summary>
        /// Gets or sets Name.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _name = value;
            }
        }

        /// <summary>
        /// Gets or sets Index.
        /// </summary>
        public int Index
        {
            get { return _index; }
            set { _index = value; }
        }

        #endregion
    }

    /// <summary>
    /// ScriptItemComment class manages the comments of ScriptItem.
    /// </summary>
    public class ScriptWordComment
    {
        #region Private fields

        private const string InsertedPlaceHolder = "[inserted]";
        private const string DeletedPlaceHolder = "[deleted]";

        private ScriptWord _scriptWord;
        private ScriptItem _scriptItem;
        private PhoneMap _phoneMap;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptWordComment"/> class.
        /// </summary>
        public ScriptWordComment()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptWordComment"/> class.
        /// </summary>
        /// <param name="scriptItem">Script Item.</param>
        public ScriptWordComment(ScriptItem scriptItem)
            : this()
        {
            _scriptItem = scriptItem;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptWordComment"/> class.
        /// </summary>
        /// <param name="scriptWord">Script word.</param>
        /// <param name="phoneMap">The phone map.</param>
        public ScriptWordComment(ScriptWord scriptWord, PhoneMap phoneMap)
        {
            _scriptWord = scriptWord;
            _phoneMap = phoneMap;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Script word of the comment.
        /// </summary>
        public ScriptWord ScriptWord
        {
            get { return _scriptWord; }
        }

        /// <summary>
        /// Gets Script item of the comment.
        /// </summary>
        public ScriptItem ScriptItem
        {
            get { return _scriptItem; }
        }

        /// <summary>
        /// Gets Comment id, used to distinguish comment list on different ScriptItems.
        /// </summary>
        [ListItem(Index = 0, Name = "Sentence ID", Width = 122)]
        public string Sid
        {
            get
            {
                string sid = string.Empty;

                if (_scriptItem != null)
                {
                    Debug.Assert(_scriptItem.Sentences.Count > 0);

                    // Currently only support sentenceID.
                    sid = _scriptItem.GetSentenceId(_scriptItem.Sentences[0]);
                }
                else
                {
                    if (_scriptWord == null)
                    {
                        throw new InvalidOperationException("_scriptWord is null");
                    }

                    if (_scriptWord.Sentence == null)
                    {
                        throw new InvalidOperationException("_scriptWord.Sentence is null");
                    }

                    if (_scriptWord.Sentence.ScriptItem == null)
                    {
                        throw new InvalidOperationException("_scriptWord.Sentence.ScriptItem is null");
                    }

                    sid = _scriptWord.Sentence.ScriptItem.GetSentenceId(_scriptWord.Sentence);
                }

                return sid;
            }
        }

        /// <summary>
        /// Gets Issue type.
        /// </summary>
        [ListItem(Index = 1, Name = "Change Type", Width = 81)]
        public string ChangeType
        {
            get
            {
                string displayString = string.Empty;
                if (_scriptItem != null)
                {
                    string timeStamp;
                    XmlScriptCommentHelper.ScriptItemStatus status = XmlScriptCommentHelper.GetScriptItemStatus(
                        _scriptItem, out timeStamp);
                    Debug.Assert(status != XmlScriptCommentHelper.ScriptItemStatus.Original);

                    displayString = XmlScriptCommentHelper.GetChangeTypeString(status);
                }
                else
                {
                    if (_scriptWord == null)
                    {
                        throw new InvalidOperationException("_scriptWord is null");
                    }

                    TtsXmlStatus wordPronStatus = XmlScriptCommentHelper.GetWordCommentStatus(
                        _scriptWord, XmlScriptCommentHelper.WordPronStatusName);
                    TtsXmlStatus wordTextStatus = XmlScriptCommentHelper.GetWordCommentStatus(
                        _scriptWord, XmlScriptCommentHelper.WordTextStatusName);
                    bool wordTextChanged = wordTextStatus != null && wordTextStatus.Status == TtsXmlStatus.EditStatus.Modify;
                    bool wordPronChanged = wordPronStatus != null && wordPronStatus.Status == TtsXmlStatus.EditStatus.Modify;

                    displayString = XmlScriptCommentHelper.GetChangeTypeString(WordEditStatus,
                        wordTextChanged, wordPronChanged);
                }

                return displayString;
            }
        }

        /// <summary>
        /// Gets Comment status.
        /// </summary>
        public TtsXmlStatus.EditStatus WordEditStatus
        {
            get
            {
                Debug.Assert(_scriptWord != null);
                return XmlScriptCommentHelper.GetScriptWordEditStatus(_scriptWord);
            }
        }

        /// <summary>
        /// Gets Original word.
        /// </summary>
        [ListItem(Index = 2, Name = "Original Word", Width = 88)]
        public string OriginalWord
        {
            get
            {
                if (_scriptItem != null)
                {
                    return string.Empty;
                }

                if (_scriptWord == null)
                {
                    throw new InvalidOperationException("_scriptWord is null");
                }

                string originalWordText = string.Empty;

                TtsXmlStatus wordTextStatus = XmlScriptCommentHelper.GetWordCommentStatus(
                    _scriptWord, XmlScriptCommentHelper.WordTextStatusName);

                if (wordTextStatus == null)
                {
                    originalWordText = string.Empty;
                }
                else if (wordTextStatus.Status == TtsXmlStatus.EditStatus.Add)
                {
                    originalWordText = InsertedPlaceHolder;
                }
                else if (wordTextStatus.Status == TtsXmlStatus.EditStatus.Delete)
                {
                    originalWordText = _scriptWord.Grapheme;
                }
                else if (wordTextStatus.Status == TtsXmlStatus.EditStatus.Modify)
                {
                    originalWordText = wordTextStatus.OriginalValue;
                }

                return originalWordText;
            }
        }

        /// <summary>
        /// Gets Fixed word.
        /// </summary>
        [ListItem(Index = 3, Name = "Original Pron", Width = 88)]
        public string OriginalPron
        {
            get
            {
                if (_scriptItem != null)
                {
                    return string.Empty;
                }

                if (_scriptWord == null)
                {
                    throw new InvalidOperationException("_scriptWord is null");
                }

                string originalPronText = string.Empty;

                TtsXmlStatus wordPronStatus = XmlScriptCommentHelper.GetWordCommentStatus(
                    _scriptWord, XmlScriptCommentHelper.WordPronStatusName);

                // If change tyep is "word changed", then doesn't display pronunciation.
                if (wordPronStatus == null)
                {
                    originalPronText = string.Empty;
                }
                else if (wordPronStatus.Status == TtsXmlStatus.EditStatus.Add)
                {
                    originalPronText = InsertedPlaceHolder;
                }
                else if (wordPronStatus.Status == TtsXmlStatus.EditStatus.Delete)
                {
                    originalPronText = _scriptWord.Pronunciation;
                }
                else if (wordPronStatus.Status == TtsXmlStatus.EditStatus.Modify)
                {
                    originalPronText = wordPronStatus.OriginalValue;
                }

                if (!string.IsNullOrEmpty(originalPronText) && _phoneMap != null)
                {
                    originalPronText = Pronunciation.GetMappedPronunciation(originalPronText, _phoneMap);
                }

                return originalPronText;
            }
        }

        /// <summary>
        /// Gets Fixed word.
        /// </summary>
        [ListItem(Index = 4, Name = "Fixed Word", Width = 88)]
        public string FixedWord
        {
            get
            {
                if (_scriptItem != null)
                {
                    return string.Empty;
                }

                if (_scriptWord == null)
                {
                    throw new InvalidOperationException("_scriptWord is null");
                }

                string fixedWordText = string.Empty;

                TtsXmlStatus wordTextStatus = XmlScriptCommentHelper.GetWordCommentStatus(
                    _scriptWord, XmlScriptCommentHelper.WordTextStatusName);

                if (wordTextStatus == null)
                {
                    fixedWordText = string.Empty;
                }
                else if (wordTextStatus.Status == TtsXmlStatus.EditStatus.Add)
                {
                    fixedWordText = _scriptWord.Grapheme;
                }
                else if (wordTextStatus.Status == TtsXmlStatus.EditStatus.Delete)
                {
                    fixedWordText = DeletedPlaceHolder;
                }
                else if (wordTextStatus.Status == TtsXmlStatus.EditStatus.Modify)
                {
                    fixedWordText = _scriptWord.Grapheme;
                }

                return fixedWordText;
            }
        }

        /// <summary>
        /// Gets Fixed word.
        /// </summary>
        [ListItem(Index = 5, Name = "Fixed Pron", Width = 88)]
        public string FixedPron
        {
            get
            {
                if (_scriptItem != null)
                {
                    return string.Empty;
                }

                if (_scriptWord == null)
                {
                    throw new InvalidOperationException("_scriptWord is null");
                }

                string fixedPronText = string.Empty;

                TtsXmlStatus wordPronStatus = XmlScriptCommentHelper.GetWordCommentStatus(
                    _scriptWord, XmlScriptCommentHelper.WordPronStatusName);

                if (wordPronStatus == null)
                {
                    fixedPronText = string.Empty;
                }
                else if (wordPronStatus.Status == TtsXmlStatus.EditStatus.Add)
                {
                    fixedPronText = _scriptWord.Pronunciation;
                }
                else if (wordPronStatus.Status == TtsXmlStatus.EditStatus.Delete)
                {
                    fixedPronText = DeletedPlaceHolder;
                }
                else if (wordPronStatus.Status == TtsXmlStatus.EditStatus.Modify)
                {
                    fixedPronText = _scriptWord.Pronunciation;
                }

                if (!string.IsNullOrEmpty(fixedPronText) && _phoneMap != null)
                {
                    fixedPronText = Pronunciation.GetMappedPronunciation(fixedPronText, _phoneMap);
                }

                return fixedPronText;
            }
        }

        /// <summary>
        /// Gets The severity of one comment.
        /// </summary>
        [ListItem(Index = 6, Name = "Severity", Width = 30)]
        public string Severity
        {
            get
            {
                if (_scriptItem != null)
                {
                    return XmlScriptCommentHelper.LowSecverity;
                }

                if (_scriptWord == null)
                {
                    throw new InvalidOperationException("_scriptWord is null");
                }

                string severity = XmlScriptCommentHelper.LowSecverity;
                TtsXmlStatus wordStatus = XmlScriptCommentHelper.GetScriptWordStatus(_scriptWord);
                if (wordStatus != null && XmlScriptCommentHelper.HighSecverity.Equals(
                    wordStatus.Severity, StringComparison.OrdinalIgnoreCase))
                {
                    severity = XmlScriptCommentHelper.HighSecverity;
                }

                return severity;
            }
        }

        /// <summary>
        /// Gets The severity of one comment.
        /// </summary>
        [ListItem(Index = 7, Name = "Time Stamp", Width = 30)]
        public string TimeStamp
        {
            get
            {
                string timestamp = string.Empty;

                if (_scriptItem != null)
                {
                    XmlScriptCommentHelper.ScriptItemStatus status = XmlScriptCommentHelper.GetScriptItemStatus(
                        _scriptItem, out timestamp);
                    Debug.Assert(status != XmlScriptCommentHelper.ScriptItemStatus.Original);
                }
                else
                {
                    TtsXmlStatus wordStatus = XmlScriptCommentHelper.GetScriptWordStatus(_scriptWord);

                    if (wordStatus != null)
                    {
                        timestamp = wordStatus.Timestamp;
                    }
                }

                return timestamp;
            }
        }

        #endregion

        #region Public Operations

        /// <summary>
        /// Combin the comment elements to one string.
        /// </summary>
        /// <returns>The string type.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Sentence ID: {0} ", Sid);
            sb.AppendFormat("Change type: {0} ", ChangeType);
            sb.AppendFormat("Original Word: {0} ", OriginalWord);
            sb.AppendFormat("Fixed Word: {0} ", FixedWord);
            sb.AppendFormat("Original Pron: {0} ", OriginalPron);
            sb.AppendFormat("Fixed Pron: {0} ", FixedPron);
            sb.AppendFormat("Severity: {0} ", Severity);
            sb.AppendFormat("Timestamp: {0} ", TimeStamp);

            return sb.ToString();
        }

        #endregion
    }

    /// <summary>
    /// Detect mismatch item.
    /// </summary>
    public class DetectMismatchItem
    {
        #region Private fields

        private bool _isOriginalText;
        private ScriptSentence _scriptSentence;

        #endregion

        #region Construction
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DetectMismatchItem"/> class.
        /// </summary>
        /// <param name="scriptSentence">Script sentence.</param>
        /// <param name="isOriginalText">If it is original text.</param>
        public DetectMismatchItem(ScriptSentence scriptSentence, bool isOriginalText)
        {
            _scriptSentence = scriptSentence;
            _isOriginalText = isOriginalText;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Script sentence.
        /// </summary>
        public ScriptSentence ScriptSentence
        {
            get { return _scriptSentence; }
        }

        /// <summary>
        /// Gets Comment id.
        /// </summary>
        [ListItem(Index = 0, Name = "Sentence ID", Width = 122)]
        public string Sid
        {
            get
            {
                Debug.Assert(_scriptSentence != null);
                Debug.Assert(_scriptSentence.ScriptItem != null);
                string sid = string.Empty;
                if (_isOriginalText)
                {
                    sid = _scriptSentence.ScriptItem.GetSentenceId(_scriptSentence);
                }

                return sid;
            }
        }

        /// <summary>
        /// Gets Comment id.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "Related to comment key word")]
        [ListItem(Index = 1, Name = "Type", Width = 122)]
        public string Type
        {
            get
            {
                string type = string.Empty;
                if (_isOriginalText)
                {
                    type = "Original";
                }
                else
                {
                    type = "Word list";
                }

                return type;
            }
        }

        /// <summary>
        /// Gets Sentence text.
        /// </summary>
        [ListItem(Index = 2, Name = "Sentence text", Width = 122)]
        public string SentenceText
        {
            get
            {
                string sentenceText = string.Empty;
                if (_isOriginalText)
                {
                    sentenceText = _scriptSentence.Text;
                }
                else
                {
                    sentenceText = _scriptSentence.BuildTextFromWords();
                }

                return sentenceText;
            }
        }

        #endregion
    }

    /// <summary>
    /// Script find result.
    /// </summary>
    public class FindResultItem
    {
        #region Private fields

        private ScriptWord _scriptWord;
        private PhoneMap _phoneMap;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="FindResultItem"/> class.
        /// </summary>
        public FindResultItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FindResultItem"/> class.
        /// </summary>
        /// <param name="scriptWord">Script word.</param>
        /// <param name="phoneMap">The phone map.</param>
        public FindResultItem(ScriptWord scriptWord, PhoneMap phoneMap)
            : this()
        {
            _scriptWord = scriptWord;
            _phoneMap = phoneMap;
        }

        #endregion

        #region Porperty

        /// <summary>
        /// Gets Script word of the comment.
        /// </summary>
        public ScriptWord ScriptWord
        {
            get { return _scriptWord; }
        }

        /// <summary>
        /// Gets Phone map of the comment.
        /// </summary>
        public PhoneMap PhoneMap
        {
            get { return _phoneMap; }
        }

        /// <summary>
        /// Gets Comment id.
        /// </summary>
        [ListItem(Index = 0, Name = "Sentence ID", Width = 122)]
        public string Sid
        {
            get
            {
                if (_scriptWord == null)
                {
                    throw new InvalidOperationException("_scriptWord is null");
                }

                if (_scriptWord.Sentence == null)
                {
                    throw new InvalidOperationException("_scriptWord.Sentence is null");
                }

                if (_scriptWord.Sentence.ScriptItem == null)
                {
                    throw new InvalidOperationException("_scriptWord.Sentence.ScriptItem is null");
                }

                return _scriptWord.Sentence.ScriptItem.GetSentenceId(_scriptWord.Sentence);
            }
        }

        /// <summary>
        /// Gets Word text.
        /// </summary>
        [ListItem(Index = 1, Name = "Word Text", Width = 122)]
        public string WordText
        {
            get
            {
                if (_scriptWord == null)
                {
                    throw new InvalidOperationException("_scriptWord is null");
                }

                return _scriptWord.Grapheme;
            }
        }

        /// <summary>
        /// Gets Word pronunciation.
        /// </summary>
        [ListItem(Index = 2, Name = "Word Pron", Width = 122)]
        public string WordPron
        {
            get
            {
                if (_scriptWord == null)
                {
                    throw new InvalidOperationException("_scriptWord is null");
                }

                string wordPron = _scriptWord.Pronunciation;
                if (!string.IsNullOrEmpty(wordPron) && _phoneMap != null)
                {
                    wordPron = Pronunciation.GetMappedPronunciation(_scriptWord.Pronunciation, _phoneMap);
                }

                return wordPron;
            }
        }

        #endregion
    }

    /// <summary>
    /// ScriptItemComment class manages the comments of ScriptItem.
    /// </summary>
    public class ScriptItemComment : IComparable<ScriptItemComment>
    {
        #region Public const fields

        /// <summary>
        /// Empty issue.
        /// </summary>
        public const string EmptyIssue = "[Empty]";

        /// <summary>
        /// Sentence no issue tag.
        /// </summary>
        public const string SentenceNoIssueTag = "[NoIssueTag]";

        /// <summary>
        /// Sentence passed tag.
        /// </summary>
        public const string SentencePassed = "[Passed]";

        /// <summary>
        /// Reject the modify tag.
        /// </summary>
        public const string SentenceRejected = "[Rejected]";

        /// <summary>
        /// Unreviewed tag.
        /// </summary>
        public const string SentenceUnreviewed = "[Unreviewed]";

        #endregion

        #region Private fields

        private int _issueOffset;
        private int _issueLength;

        private string _sid;
        private string _issue;
        private string _issueType;
        private string _originalWord;
        private string _fixedWord;
        private string _originalWordPron;
        private string _fixedPron;
        private string _description;
        private int? _severity;
        private int _originalPronOffset;
        private int _originalPronLength;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptItemComment"/> class.
        /// </summary>
        public ScriptItemComment()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptItemComment"/> class.
        /// </summary>
        /// <param name="issue">Issue.</param>
        public ScriptItemComment(string issue)
        {
            if (string.IsNullOrEmpty(issue))
            {
                throw new ArgumentNullException("issue");
            }

            Issue = issue;
        }

        #region Properties

        /// <summary>
        /// Gets or sets Comment id, used to distinguish comment list on different ScriptItems.
        /// </summary>
        [ListItem(Index = 0, Name = "Sentence", Width = 122)]
        public string Sid
        {
            get
            {
                return _sid;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _sid = value;
            }
        }

        /// <summary>
        /// Gets or sets Comment started position of one item .
        /// </summary>
        public int IssueOffset
        {
            get { return _issueOffset; }
            set { _issueOffset = value; }
        }

        /// <summary>
        /// Gets or sets Comment length.
        /// </summary>
        public int IssueLength
        {
            get { return _issueLength; }
            set { _issueLength = value; }
        }

        /// <summary>
        /// Gets Issue end.
        /// </summary>
        public int IssueEnd
        {
            get { return IssueOffset + IssueLength; }
        }

        /// <summary>
        /// Gets Comment sub-id, used to distinguish comments on one ScriptItem.
        /// </summary>
        public string Id
        {
            get { return BuildCommentId(_sid, _issueOffset, _issueLength); }
        }

        /// <summary>
        /// Gets or sets Issue text.
        /// </summary>
        [ListItem(Index = 1, Name = "Key", Width = 86)]
        public string Issue
        {
            get
            {
                return _issue;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _issue = string.Empty;
                }
                else
                {
                    _issue = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets Issue type.
        /// </summary>
        [ListItem(Index = 2, Name = "Type", Width = 81)]
        public string IssueType
        {
            get
            {
                return _issueType;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _issueType = string.Empty;
                }
                else
                {
                    _issueType = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets Original word.
        /// </summary>
        [ListItem(Index = 3, Name = "OriginalWord", Width = 88)]
        public string OriginalWord
        {
            get
            {
                return _originalWord;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _originalWord = string.Empty;
                }
                else
                {
                    _originalWord = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets Fixed word.
        /// </summary>
        [ListItem(Index = 4, Name = "FixedWord", Width = 88)]
        public string FixedWord
        {
            get
            {
                return _fixedWord;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _fixedWord = string.Empty;
                }
                else
                {
                    _fixedWord = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets Original unit pronunciation offset from the word's pronunciation start position.
        /// </summary>
        public int OriginalPronOffset
        {
            get
            {
                return _originalPronOffset;
            }

            set
            {
                _originalPronOffset = value;
            }
        }

        /// <summary>
        /// Gets or sets Original unit pronunciation lenght.
        /// </summary>
        public int OriginalPronLength
        {
            get
            {
                return _originalPronLength;
            }

            set
            {
                _originalPronLength = value;
            }
        }

        /// <summary>
        /// Gets Original pronunciation.
        /// </summary>
        [ListItem(Index = 5, Name = "OriginalPron", Width = 88)]
        public string OriginalPron
        {
            get
            {
                string originalPron = string.Empty;
                if (!string.IsNullOrEmpty(_originalWordPron))
                {
                    originalPron = _originalWordPron.Substring(OriginalPronOffset, OriginalPronLength);
                }

                return originalPron;
            }
        }

        /// <summary>
        /// Gets or sets Fixed pronunciation.
        /// </summary>
        [ListItem(Index = 6, Name = "FixedPron", Width = 88)]
        public string FixedPron
        {
            get
            {
                return _fixedPron;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _fixedPron = string.Empty;
                }
                else
                {
                    _fixedPron = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets Original whole word's pronunciation.
        /// </summary>
        [ListItem(Index = 7, Name = "OriginalWordPron", Width = 88)]
        public string OriginalWordPron
        {
            get
            {
                return _originalWordPron;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _originalWordPron = string.Empty;
                }
                else
                {
                    _originalWordPron = value;
                }
            }
        }

        /// <summary>
        /// Gets Fixed pronunciation.
        /// </summary>
        [ListItem(Index = 8, Name = "FixedWordPron", Width = 88)]
        public string FixedWordPron
        {
            get
            {
                string fixedWordPron = string.Empty;
                if (!string.IsNullOrEmpty(_originalWordPron))
                {
                    fixedWordPron = _originalWordPron.Remove(_originalPronOffset,
                        _originalPronLength);
                    fixedWordPron = fixedWordPron.Insert(_originalPronOffset, _fixedPron);
                }

                return fixedWordPron;
            }
        }

        /// <summary>
        /// Gets or sets The severity of one comment.
        /// </summary>
        [ListItem(Index = 9, Name = "Severity", Width = 30)]
        public int? Severity
        {
            get { return _severity; }
            set { _severity = value; }
        }

        /// <summary>
        /// Gets or sets More descriptions on one comment.
        /// </summary>
        [ListItem(Index = 10, Name = "Description", Width = 100)]
        public string Description
        {
            get
            {
                return _description;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _description = string.Empty;
                }
                else
                {
                    _description = value;
                }
            }
        }

        #endregion

        #region Public Operations

        /// <summary>
        /// Combine the three parameters to one string.
        /// </summary>
        /// <param name="sid">String type.</param>
        /// <param name="issueOffset">Offset.</param>
        /// <param name="issueLength">Length.</param>
        /// <returns>String.</returns>
        public static string BuildCommentId(string sid, int issueOffset, int issueLength)
        {
            if (string.IsNullOrEmpty(sid))
            {
                throw new ArgumentNullException("sid");
            }

            return sid + " [" + issueOffset.ToString(CultureInfo.InvariantCulture) + "]" + " [" + issueLength.ToString(CultureInfo.InvariantCulture) + "]";
        }

        /// <summary>
        /// Create scriptItemComment.
        /// </summary>
        /// <param name="sid">Sentence id.</param>
        /// <param name="word">Word to be comment.</param>
        /// <param name="wordOffset">The word's offset in script item.</param>
        /// <param name="wordFix">Word replaced with.</param>
        /// <param name="pronFix">Pronunciation repalced with.</param>
        /// <returns>ScriptItemComment that created.</returns>
        public static ScriptItemComment CreateComment(string sid, ScriptWord word, int wordOffset,
            string wordFix, string pronFix)
        {
            ScriptItemComment comment = new ScriptItemComment(word.Grapheme);
            comment.Sid = sid;
            comment.IssueOffset = wordOffset;
            comment.IssueLength = word.LengthInString;
            comment.OriginalWord = word.Grapheme;
            comment.OriginalPronOffset = 0;
            comment.OriginalPronLength = word.Pronunciation.Length;
            comment.OriginalWordPron = word.Pronunciation;
            comment.FixedWord = wordFix;
            comment.FixedPron = pronFix;

            return comment;
        }

        /// <summary>
        /// Combin the comment elements to one string.
        /// </summary>
        /// <returns>The string type.</returns>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendFormat("Comment ID: {0} ", Sid);

            builder.AppendFormat("Severity: {0} ",
                Severity.HasValue ? Severity.Value.ToString(CultureInfo.InvariantCulture) : "null");
            builder.AppendFormat("Issue: {0} ",
                string.IsNullOrEmpty(Issue) ? "null" : Issue);

            builder.AppendFormat("IssueType: {0} ", IssueType);
            builder.AppendFormat("Offset: {0} ", IssueOffset);
            builder.AppendFormat("Length: {0} ", IssueLength);

            builder.AppendFormat("OriginalWord: {0} ",
                string.IsNullOrEmpty(OriginalWord) ? "null" : OriginalWord);
            builder.AppendFormat("FixedWord: {0} ",
                string.IsNullOrEmpty(FixedWord) ? "null" : FixedWord);

            builder.AppendFormat("OriginalPron: {0} ",
                string.IsNullOrEmpty(OriginalPron) ? "null" : OriginalPron);
            builder.AppendFormat("FixedPron: {0} ",
                string.IsNullOrEmpty(FixedPron) ? "null" : FixedPron);

            builder.AppendFormat("OriginalWordPron: {0} ",
                string.IsNullOrEmpty(OriginalWordPron) ? "null" : OriginalWordPron);
            builder.AppendFormat("OriginalPronOffset: {0} ", OriginalPronOffset);
            builder.AppendFormat("OriginalPronLength: {0} ", OriginalPronLength);
            builder.AppendFormat("FixedWordPron: {0} ",
                string.IsNullOrEmpty(FixedWordPron) ? "null" : FixedWordPron);

            builder.AppendFormat("Description: {0}",
                string.IsNullOrEmpty(Description) ? "null" : Description);

            return builder.ToString();
        }

        #endregion

        #region IComparable<ScriptItemComment> Members

        /// <summary>
        /// Implement the CompareTo method.
        /// </summary>
        /// <param name="other">ScriptItemComment instance.</param>
        /// <returns>IComparable instance.</returns>
        int IComparable<ScriptItemComment>.CompareTo(ScriptItemComment other)
        {
            return CompareTo(other);
        }

        #endregion

        #region Internal Operations

        /// <summary>
        /// Clone the element of the current instance to another instance.
        /// </summary>
        /// <param name="comment">The instance of scriptItemComment.</param>
        public void Clone(ScriptItemComment comment)
        {
            if (comment == null)
            {
                throw new ArgumentNullException("comment");
            }

            this._sid = comment._sid;
            this._issueOffset = comment._issueOffset;
            this._issueLength = comment._issueLength;
            this._issue = comment._issue;
            this._issueType = comment._issueType;
            this._originalWord = comment._originalWord;
            this._originalWordPron = comment._originalWordPron;
            this._originalPronOffset = comment._originalPronOffset;
            this._originalPronLength = comment._originalPronLength;
            this._fixedWord = comment._fixedWord;
            this._fixedPron = comment._fixedPron;
            this._description = comment._description;
            this._severity = comment._severity;
        }

        #endregion

        #region Override equal operations

        /// <summary>
        /// Get hash code for this object.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            // use default implementation
            return base.GetHashCode();
        }

        /// <summary>
        /// Equal.
        /// </summary>
        /// <param name="obj">Other object to compare with.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public override bool Equals(object obj)
        {
            ScriptItemComment other = obj as ScriptItemComment;
            if (other == null)
            {
                return false;
            }

            return Equals(other);
        }

        /// <summary>
        /// Equal.
        /// </summary>
        /// <param name="other">Other object to compare with.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public bool Equals(ScriptItemComment other)
        {
            bool ret = false;

            if (other != null)
            {
                ret = this.Sid == other.Sid &&
                    this.IssueOffset == other.IssueOffset &&
                    this.IssueLength == other.IssueLength &&
                    this.Issue == other.Issue &&
                    this.IssueType == other.IssueType &&
                    this.OriginalWord == other.OriginalWord &&
                    this.FixedWord == other.FixedWord &&
                    this.OriginalPron == other.OriginalPron &&
                    this.FixedPron == other.FixedPron &&
                    this.OriginalWordPron == other.OriginalWordPron &&
                    this.FixedWordPron == other.FixedWordPron &&
                    this.Description == other.Description &&
                    this.Severity == other.Severity;
            }

            return ret;
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Implement the CompareTo method.
        /// </summary>
        /// <param name="other">ScriptItemComment instance.</param>
        /// <returns>IComparable instance.</returns>
        protected int CompareTo(ScriptItemComment other)
        {
            return this._issueOffset.CompareTo(other.IssueOffset);
        }

        #endregion
    }
}