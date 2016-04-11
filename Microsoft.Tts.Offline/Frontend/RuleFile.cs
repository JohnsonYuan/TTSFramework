//----------------------------------------------------------------------------
// <copyright file="RuleFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements general rule actions : load, save, and split in domain.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Frontend
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// RuleItem class.
    /// </summary>
    public class RuleItem
    {
        private string _domain = DomainItem.GeneralDomain;
        private List<string> _ruleContent = new List<string>();
        private List<string> _keyComment = new List<string>();

        /// <summary>
        /// Gets or sets Rule entry string such as CurW = ".".
        /// </summary>
        public string EntryString { get; set; }

        /// <summary>
        /// Gets or sets Domain.
        /// </summary>
        public string DomainTag
        {
            get
            {
                return _domain;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException();
                }

                _domain = value.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Gets Rule content.
        /// </summary>
        public List<string> RuleContent
        {
            get { return _ruleContent; }
        }

        /// <summary>
        /// Gets or sets key comment.
        /// </summary>
        public List<string> KeyComment
        {
            get { return _keyComment; }
            set { _keyComment = value; }
        }
    }

    /// <summary>
    /// RuleFile class.
    /// </summary>
    public class RuleFile
    {
        #region Fields

        private const string DeclearKeyRegex = @"^[ \t]*([a-zA-Z]+)[ \t]*#[ \t]*(string|int|(enum.*))[ \t]*;[ \t]*(//.*)?$";
        private const string CommentLineRegex = @"^[ \t]*//.*";
        private const string DomainLineRegex = @"^[ \t]*(\[domain=)([a-zA-Z]+)*(\])$";
        private const string DefaultRuleRegex = @"^[ \t]*All[ \t]*>=[ \t]*0[ \t]*:(.+)[ \t]*$";

        private const string DomainLineFormat = "[domain={0}]";
        private const string RuleEntryStartTag = "CurW";

        private List<string> _declearLines = new List<string>();
        private Collection<RuleItem> _ruleItems = new Collection<RuleItem>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets KeyLineRegex.
        /// </summary>
        public static string KeyLineRegex
        {
            get
            {
                return Helper.NeutralFormat(@"^{0}[ \t]*=[ \t]*""(.*)""[ \t]*;[ \t]*$", RuleEntryStartTag);
            }
        }

        /// <summary>
        /// Gets or sets domain tag.
        /// </summary>
        public string DomainTag { get; set; }

        /// <summary>
        /// Gets the rule items.
        /// </summary>
        public Collection<RuleItem> RuleItems
        {
            get
            {
                return _ruleItems;
            }
        }

        #endregion

        #region Public static methods

        /// <summary>
        /// Indicating whether the line is default rule.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <returns>Whether the line is default rule.</returns>
        public static bool IsPolyRuleDefaultRule(string line)
        {
            return Regex.Match(line, DefaultRuleRegex).Success;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Split into domain RuleFile instances.
        /// </summary>
        /// <returns>RuleFile Array.</returns>
        public RuleFile[] Split()
        {
            Dictionary<string, RuleFile> ruleFiles = new Dictionary<string, RuleFile>();

            foreach (RuleItem ruleItem in _ruleItems)
            {
                if (ruleFiles.ContainsKey(ruleItem.DomainTag))
                {
                    ruleFiles[ruleItem.DomainTag]._ruleItems.Add(ruleItem);
                }
                else
                {
                    RuleFile file = new RuleFile();
                    file.DomainTag = ruleItem.DomainTag;
                    file._declearLines = _declearLines;
                    file._ruleItems.Add(ruleItem);
                    ruleFiles.Add(ruleItem.DomainTag, file);
                }
            }

            return ruleFiles.Values.ToArray();
        }

        /// <summary>
        /// Load.
        /// </summary>
        /// <param name="filePath">FilePath.</param>
        /// <param name="loadComment">Whether load comment line.</param>
        public void Load(string filePath, bool loadComment = false)
        {
            ////#region Validate parameter

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (!File.Exists(filePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException), filePath);
            }

            if (!Helper.IsUnicodeFile(filePath))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Polypony rule file [{0}] is not unicode.", filePath));
            }

            ////#endregion

            using (StreamReader sr = new StreamReader(filePath, Encoding.Unicode))
            {
                string line = null;
                string domain = DomainItem.GeneralDomain;
                RuleItem newItem = null;
                List<string> keyComment = null;

                while ((line = sr.ReadLine()) != null)
                {
                    string trimedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimedLine))
                    {
                        continue;
                    }

                    if (IsComment(trimedLine) && !loadComment)
                    {
                        continue;
                    }

                    if (IsDeclearKey(trimedLine))
                    {
                        _declearLines.Add(trimedLine);
                        continue;
                    }

                    if (IsDomainTag(trimedLine))
                    {
                        ParseDomainKey(trimedLine, ref domain);
                        continue;
                    }

                    if (IsRuleItemEntry(trimedLine))
                    {
                        if (newItem != null)
                        {
                            if (loadComment &&
                                newItem.RuleContent.Count > 1)
                            {
                                // remove the last comment line from the item, it belongs to next item
                                int lastIndex = newItem.RuleContent.Count - 1;
                                while (IsComment(newItem.RuleContent[lastIndex]))
                                {
                                    if (keyComment == null)
                                    {
                                        keyComment = new List<string>();
                                    }

                                    keyComment.Add(newItem.RuleContent[lastIndex]);

                                    newItem.RuleContent.RemoveAt(lastIndex);

                                    --lastIndex;
                                }
                            }

                            _ruleItems.Add(newItem);
                        }

                        newItem = new RuleItem();

                        if (loadComment &&
                            keyComment != null && keyComment.Count > 0)
                        {
                            newItem.KeyComment = keyComment;
                            keyComment = null;
                        }

                        newItem.EntryString = trimedLine;
                        newItem.DomainTag = domain;
                        domain = DomainItem.GeneralDomain;
                        continue;
                    }

                    if (newItem != null)
                    {
                        newItem.RuleContent.Add(trimedLine);
                    }
                }

                if (newItem != null)
                {
                    _ruleItems.Add(newItem);
                }
            }
        }

        /// <summary>
        /// Save.
        /// </summary>
        /// <param name="filePath">FilePath.</param>
        /// <param name="writeDomainAndComment">Whethe write the domain and comment line, always use false when used for compile.</param>
        public void Save(string filePath, bool writeDomainAndComment = false)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            Helper.EnsureFolderExistForFile(filePath);
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.Unicode))
            {
                foreach (string declear in _declearLines)
                {
                    sw.WriteLine(declear);
                }

                // append two blank line if writeDomainAndComment = true
                if (writeDomainAndComment)
                {
                    sw.WriteLine();
                    sw.WriteLine();
                }

                foreach (RuleItem ruleItem in _ruleItems)
                {
                    sw.WriteLine();

                    if (writeDomainAndComment)
                    {
                        if (ruleItem.KeyComment != null &&
                            ruleItem.KeyComment.Count > 0)
                        {
                            foreach (var comment in ruleItem.KeyComment)
                            {
                                sw.WriteLine(comment);
                            }
                        }

                        string doaminTag = ruleItem.DomainTag;
                        if (doaminTag != DomainItem.GeneralDomain)
                        {
                            sw.WriteLine(Helper.NeutralFormat(DomainLineFormat, doaminTag));
                        }
                    }

                    sw.WriteLine(ruleItem.EntryString);
                    foreach (string content in ruleItem.RuleContent)
                    {
                        sw.WriteLine(content);
                    }
                }
            }
        }

        /// <summary>
        /// Get duplicate item keys.
        /// </summary>
        /// <param name="filePath">Rule file path.</param>
        /// <returns>Duplicate key list.</returns>
        public List<string> GetDupKeys(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (!File.Exists(filePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException), filePath);
            }

            if (!Helper.IsUnicodeFile(filePath))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Rule file [{0}] is not unicode.", filePath));
            }

            List<string> allKeys = new List<string>();
            List<string> dupKeys = new List<string>();

            using (StreamReader sr = new StreamReader(filePath, Encoding.Unicode))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (IsRuleItemEntry(line))
                    {
                        string key = ParseItemKey(line);
                        if (allKeys.Contains(key))
                        {
                            dupKeys.Add(key);
                        }
                        else
                        {
                            allKeys.Add(key);
                        }
                    }
                }
            }

            return dupKeys;
        }

        #endregion

        #region Private static methods

        /// <summary>
        /// Indicating whether the line is declear.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <returns>Whether the line is declear.</returns>
        private static bool IsDeclearKey(string line)
        {
            return Regex.Match(line, DeclearKeyRegex).Success;
        }

        /// <summary>
        /// Indicating whether the line is comment.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <returns>Whether the line is comment.</returns>
        private static bool IsComment(string line)
        {
            return Regex.Match(line, CommentLineRegex).Success;
        }

        /// <summary>
        /// Indicating whether the line is domain tag.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <returns>Whether the line is domain tag.</returns>
        private static bool IsDomainTag(string line)
        {
            return Regex.Match(line, DomainLineRegex).Success;
        }

        /// <summary>
        /// Whether the line is rule entry.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <returns>Whether the line is declear.</returns>
        private static bool IsRuleItemEntry(string line)
        {
            return Regex.Match(line, KeyLineRegex).Success;
        }

        /// <summary>
        /// ParseDomainKey.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <param name="keyName">KeyName.</param>
        private static void ParseDomainKey(string line, ref string keyName)
        {
            Match match = Regex.Match(line, DomainLineRegex);

            if (match.Groups.Count != 4)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Invalid domain line : [{0}]", line));
            }

            keyName = match.Groups[2].ToString();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Parse rule item key.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <returns>Key value.</returns>
        private string ParseItemKey(string line)
        {
            string itemKey = string.Empty;
            Match match = Regex.Match(line, KeyLineRegex);
            if (match.Groups.Count != 2)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Invalid key line : [{0}]", line));
            }
            else
            {
                itemKey = match.Groups[1].ToString();
            }

            return itemKey;
        }

        #endregion
    }
}