//----------------------------------------------------------------------------
// <copyright file="PolyphonyRuleFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements polyphony rule
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Frontend
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Polyphony rule error.
    /// </summary>
    public enum PolyRuleError
    {
        /// <summary>
        /// Can't find primary key name in polypony file.
        /// </summary>
        [ErrorAttribute(Message = "Can't find primary key name in polypony file [{0}]",
            Severity = ErrorSeverity.MustFix)]
        MissPrimaryKey,

        /// <summary>
        /// Invalid file line format [Line {0}: {1}].
        /// </summary>
        [ErrorAttribute(Message = "Invalid file line format [Line {0}: {1}].",
            Severity = ErrorSeverity.MustFix)]
        InvalidLineFormat,

        /// <summary>
        /// Duplicate delceared key name [{0}].
        /// </summary>
        [ErrorAttribute(Message = "Duplicate delceared key name [{0}].",
            Severity = ErrorSeverity.MustFix)]
        DuplicateKeyName,

        /// <summary>
        /// Parse error in line [{0}].
        /// </summary>
        [ErrorAttribute(Message = "Parse error in line [{0}].",
            Severity = ErrorSeverity.MustFix)]
        ParseError,

        /// <summary>
        /// Invalid primary key value line [{0}].
        /// </summary>
        [ErrorAttribute(Message = "Invalid primary key value line [{0}].",
            Severity = ErrorSeverity.MustFix)]
        InvalidPrimaryKeyValueForamt,

        /// <summary>
        /// Can't find primary key line before parsing condition line [{0}].
        /// </summary>
        [ErrorAttribute(Message = "Can't find primary key line before parsing condition line [{0}].",
            Severity = ErrorSeverity.MustFix)]
        MissKeyValueLine,

        /// <summary>
        /// Invalid condition format : [{0}].
        /// </summary>
        [ErrorAttribute(Message = "Invalid condition format : [{0}].",
            Severity = ErrorSeverity.MustFix)]
        InvalidConditionFormat,

        /// <summary>
        /// Can't find operator in expression [{0}].
        /// </summary>
        [ErrorAttribute(Message = "Can't find operator in expression [{0}].",
            Severity = ErrorSeverity.MustFix)]
        MissingOperatorInCondition,

        /// <summary>
        /// Can't find condition key [{0}].
        /// </summary>
        [ErrorAttribute(Message = "The condition key [{0}] has not been decleared in expression [{1}].",
            Severity = ErrorSeverity.MustFix)]
        NotDeclearedConditionKey,

        /// <summary>
        /// There is no condition for key [{0}].
        /// </summary>
        [ErrorAttribute(Message = "There is no valid condition for word [{0}].",
            Severity = ErrorSeverity.MustFix)]
        NoConditionForWord,

        /// <summary>
        /// There are duplicate definitions of word [{0}].
        /// </summary>
        [ErrorAttribute(Message = "There are duplicate definitions of word [{0}].",
            Severity = ErrorSeverity.MustFix)]
        DuplicateWordDefinitions,

        /// <summary>
        /// There are duplicate conditions [{0}] defined for pronunciations [{1}] and [{2}] of word [{3}].
        /// </summary>
        [ErrorAttribute(Message = "There are duplicate conditions [{0}] defined for pronunciations [{1}] and [{2}] of word [{3}].",
            Severity = ErrorSeverity.MustFix)]
        DuplicateRuleConditionsForDifferentPron,

        /// <summary>
        /// There are duplicate conditions [{0}] defined for pronunciation [{1}] of word [{2}].
        /// </summary>
        [ErrorAttribute(Message = "There are duplicate conditions [{0}] defined for pronunciation [{1}] of word [{2}].",
            Severity = ErrorSeverity.Warning)]
        DuplicateRuleConditionsForSamePron
    }

    /// <summary>
    /// PolyphonyCondition.
    /// </summary>
    public class PolyphonyCondition
    {
        #region Fields

        private string _key;
        private string _operator;
        private string _value;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Polyphony condition key.
        /// </summary>
        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        /// <summary>
        /// Gets or sets Polyphony condition operator.
        /// </summary>
        public string Operator
        {
            get { return _operator; }
            set { _operator = value; }
        }

        /// <summary>
        /// Gets or sets Polyphony condition value.
        /// </summary>
        public string Value
        {
            get { return _value; }
            set { _value = value; }
        }

        #endregion
    }

    /// <summary>
    /// PolyphonyPron.
    /// </summary>
    public class PolyphonyPron
    {
        #region Fields

        private string _pron;
        private Collection<PolyphonyCondition> _conditions =
            new Collection<PolyphonyCondition>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Pron.
        /// </summary>
        public string Pron
        {
            get { return _pron; }
            set { _pron = value; }
        }

        /// <summary>
        /// Gets Conditions.
        /// </summary>
        public Collection<PolyphonyCondition> Conditions
        {
            get { return _conditions; }
        }

        /// <summary>
        /// Gets Condition string in which the conditions are in the same order as polyrule file.
        /// </summary>
        public string ConditionString
        {
            get { return GenerateConditionString(false); }
        }

        /// <summary>
        /// Gets Condition string in which the conditions are sorted by keys.
        /// </summary>
        public string SortedConditionString
        {
            get { return GenerateConditionString(true); }
        }

        /// <summary>
        /// Generate a condition string.
        /// </summary>
        /// <param name="isSorted">Indicate whether the conditions are sorted by condition keys.</param>
        /// <returns>A condition string.</returns>
        private string GenerateConditionString(bool isSorted)
        {
            StringBuilder sb = new StringBuilder();

            List<string> subConditions = new List<string>();
            foreach (PolyphonyCondition condition in _conditions)
            {
                string conditionValue = condition.Value;

                if (PolyruleKeys.Instance.KeyTypes == null)
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "The keys were not defined."));
                }

                if (!PolyruleKeys.Instance.KeyTypes.ContainsKey(condition.Key))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "The key [{0}] has not been decleared.", condition.Value));
                }

                if (PolyruleKeys.Instance.KeyTypes[condition.Key] == PolyphonyRuleFile.KeyType.String)
                {
                    conditionValue = Helper.NeutralFormat(@"""{0}""", conditionValue);
                }

                subConditions.Add(string.Format("{0} {1} {2} , ", condition.Key, condition.Operator, conditionValue));
            }

            if (isSorted)
            {
                subConditions.Sort();
            }
            
            foreach (string condition in subConditions)
            {
                sb.Append(condition);
            }

            return sb.ToString().Trim().TrimEnd(',');
        }

        #endregion
    }

    /// <summary>
    /// PolyphonyRule.
    /// </summary>
    public class PolyphonyRule
    {
        #region Fields

        private string _domain;

        private Collection<PolyphonyPron> _polyphonyProns =
            new Collection<PolyphonyPron>();

        private TtsPhoneSet _ttsPhoneSet = new TtsPhoneSet();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Word.
        /// </summary>
        public string Word { get; set; }

        /// <summary>
        /// Gets or sets Domain.
        /// </summary>
        public string Domain
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
        /// Gets PolyphonyProns.
        /// </summary>
        public Collection<PolyphonyPron> PolyphonyProns
        {
            get { return _polyphonyProns; }
        }

        #endregion

        /// <summary>
        /// Check if there're duplicate conditions in a rule.
        /// </summary>
        /// <returns>ErrorSet.</returns>
        public ErrorSet CheckDupRuleConditions()
        {
            ErrorSet errorSet = new ErrorSet();
            SortedDictionary<string, string> conditions = new SortedDictionary<string, string>();

            foreach (PolyphonyPron pron in this.PolyphonyProns)
            {
                string sortedConditionStr = pron.SortedConditionString;

                if (conditions.ContainsKey(sortedConditionStr))
                {
                    if (conditions[sortedConditionStr] == pron.Pron)
                    {
                        errorSet.Add(PolyRuleError.DuplicateRuleConditionsForSamePron, pron.ConditionString, pron.Pron, this.Word);
                    }
                    else
                    {
                        errorSet.Add(PolyRuleError.DuplicateRuleConditionsForDifferentPron, pron.ConditionString, conditions[sortedConditionStr], pron.Pron, this.Word);
                    }
                }
                else
                {
                    conditions.Add(sortedConditionStr, pron.Pron);
                }
            }

            return errorSet;
        }
    }

    /// <summary>
    /// PolyphonyRuleFile.
    /// </summary>
    public class PolyphonyRuleFile
    {
        #region Fields

        private const string DeclearKeyRegex = @"^([a-zA-Z]+)[ \t]*#[ \t]*(string|int)[ \t]*;[ \t]*(//.*)?$";
        private const string ConditionLineRegex = @"^(.*):[ \t]*""(.*)""[ \t]*;[ \t]*(//.*)?$";
        private const string CommentLineRegex = @"^[ \t]*//.*";
        private const string DomainLineRegex = @"^(\[domain=)([a-zA-Z]+)*(\])$";

        private static Collection<string> _allOperators = new Collection<string>();

        private string _keyString;
        private Dictionary<string, KeyType> _keyTypes = new Dictionary<string, KeyType>();
        private Collection<PolyphonyRule> _polyphonyWords = new Collection<PolyphonyRule>();

        #endregion

        #region Constructions

        /// <summary>
        /// Initializes static members of the <see cref="PolyphonyRuleFile"/> class.
        /// </summary>
        static PolyphonyRuleFile()
        {
            _allOperators.Add("!~^");
            _allOperators.Add("!~=");
            _allOperators.Add("!~$");

            _allOperators.Add("!$");
            _allOperators.Add("!^");
            _allOperators.Add("!=");
            _allOperators.Add("!^");
            _allOperators.Add("~^");
            _allOperators.Add("~$");
            _allOperators.Add("~}");
            _allOperators.Add("~=");
            _allOperators.Add("~{");
            _allOperators.Add("<=");
            _allOperators.Add(">=");

            _allOperators.Add("=");
            _allOperators.Add("<");
            _allOperators.Add(">");
            _allOperators.Add("^");
            _allOperators.Add("$");
            _allOperators.Add("}");
            _allOperators.Add("{");
        }

        #endregion

        #region Enums

        /// <summary>
        /// KeyType.
        /// </summary>
        public enum KeyType
        {
            /// <summary>
            /// String.
            /// </summary>
            String,

            /// <summary>
            /// Int.
            /// </summary>
            Int
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets PolyphonyWords.
        /// </summary>
        public Collection<PolyphonyRule> PolyphonyWords
        {
            get { return _polyphonyWords; }
        }

        /// <summary>
        /// Gets ConditionRegex.
        /// </summary>
        public string ConditionRegex
        {
            get
            {
                if (_keyTypes.Count == 0)
                {
                    throw new InvalidDataException(Helper.NeutralFormat("Need load key declear at first."));
                }

                StringBuilder allOperators = new StringBuilder();
                foreach (string oper in _allOperators)
                {
                    if (allOperators.Length >= 0)
                    {
                        allOperators.Append("|");
                    }

                    foreach (char c in oper)
                    {
                        // All operator should be escaped, because they all C# regex key word.
                        allOperators.AppendFormat(@"\{0}", c.ToString());
                    }
                }

                StringBuilder allStringKeys = new StringBuilder();
                StringBuilder allIntKeys = new StringBuilder();
                foreach (string key in _keyTypes.Keys)
                {
                    if (_keyTypes[key] == KeyType.String)
                    {
                        if (allStringKeys.Length > 0)
                        {
                            allStringKeys.AppendFormat("|");
                        }

                        allStringKeys.Append(key);
                    }
                    else if (_keyTypes[key] == KeyType.Int)
                    {
                        if (allIntKeys.Length > 0)
                        {
                            allIntKeys.AppendFormat("|");
                        }

                        allIntKeys.Append(key);
                    }
                }

                return Helper.NeutralFormat(@"(({1})[ ]*({0})[ ]*""[^""]*"")|(({2})[ ]*({0})[ ]*[+-]?[0-9]+)",
                    allOperators.ToString(), allStringKeys.ToString(),
                    allIntKeys.ToString());
            }
        }

        /// <summary>
        /// Gets KeyLineRegex.
        /// </summary>
        public string KeyLineRegex
        {
            get
            {
                if (string.IsNullOrEmpty(KeyString))
                {
                    throw new InvalidDataException("_keyString should not be empty");
                }

                return Helper.NeutralFormat(@"^{0}[ \t]*=[ \t]*""(.*)""[ \t]*;[ \t]*$", KeyString);
            }
        }

        /// <summary>
        /// Gets or sets KeyString.
        /// </summary>
        public string KeyString
        {
            get
            {
                return _keyString;
            }

            set
            {
                _keyString = value;
            }
        }

        /// <summary>
        /// Gets or sets Domain tag.
        /// </summary>
        public string DomainTag { get; set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Split into domain PolyphonyRuleFile array.
        /// </summary>
        /// <returns>PolyphonyRuleFile array.</returns>
        public PolyphonyRuleFile[] SplitIntoPolyphonyRuleFiles()
        {
            Dictionary<string, PolyphonyRuleFile> files = new Dictionary<string, PolyphonyRuleFile>();

            foreach (PolyphonyRule word in _polyphonyWords)
            {
                if (files.ContainsKey(word.Domain))
                {
                    files[word.Domain]._polyphonyWords.Add(word);
                }
                else
                {
                    PolyphonyRuleFile file = new PolyphonyRuleFile();
                    file.DomainTag = word.Domain;
                    file._keyTypes = _keyTypes;
                    file._keyString = _keyString;
                    file._polyphonyWords.Add(word);
                    files.Add(word.Domain, file);
                }
            }

            return files.Values.ToArray();
        }

        /// <summary>
        /// Save.
        /// </summary>
        /// <param name="filePath">FilePath.</param>
        public void Save(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            Helper.EnsureFolderExistForFile(filePath);
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.Unicode))
            {
                foreach (KeyValuePair<string, KeyType> pair in _keyTypes)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("{0} ", pair.Key);
                    if (15 - pair.Key.Length > 0)
                    {
                        sb.Append(' ', 15 - pair.Key.Length);
                    }

                    sb.AppendFormat("# {0};", pair.Value.ToString().ToLower(CultureInfo.InvariantCulture));
                    sw.WriteLine(sb.ToString());
                }

                foreach (PolyphonyRule polyphonyWord in _polyphonyWords)
                {
                    sw.WriteLine();
                    sw.WriteLine(@"{0} = ""{1}"";", _keyString, polyphonyWord.Word);
                    foreach (PolyphonyPron polyphonyPron in polyphonyWord.PolyphonyProns)
                    {
                        sw.WriteLine(@"{0} : ""{1}"";", polyphonyPron.ConditionString, polyphonyPron.Pron);
                    }
                }
            }
        }

        /// <summary>
        /// Load.
        /// </summary>
        /// <param name="filePath">FilePath.</param>
        /// <param name="phoneSet">PhoneSet.</param>
        /// <returns>ErrorSet.</returns>
        public ErrorSet Load(string filePath, TtsPhoneSet phoneSet)
        {
            // This validation is needed by Fxcop checking parameters.
            if (phoneSet == null)
            {
                phoneSet = null;
            }

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
                    "Polyphony rule file [{0}] is not unicode.", filePath));
            }

            ErrorSet errorSet = new ErrorSet();
            _keyTypes.Clear();
            bool finishReadHead = false;
            bool firstKeyString = true;
            PolyphonyRule polyphonyWord = null;

            int lineNum = 0;
            string domain = DomainItem.GeneralDomain;
            foreach (string line in Helper.FileLines(filePath, Encoding.Unicode, false))
            {
                lineNum++;
                string trimedLine = line.Trim();
                if (string.IsNullOrEmpty(trimedLine))
                {
                    continue;
                }

                if (IsComment(trimedLine))
                {
                    continue;
                }

                if (IsDomainTag(trimedLine))
                {
                    ParseDomainKey(trimedLine, ref domain);
                    continue;
                }

                ErrorSet parseErrorSet = new ErrorSet();
                if (!finishReadHead)
                {
                    bool isKeyDeclear = TryParseKeyDeclear(trimedLine,
                        ref firstKeyString, parseErrorSet);
                    AddParseError(errorSet, lineNum, parseErrorSet);
                    if (isKeyDeclear)
                    {
                        continue;
                    }
                    else
                    {
                        finishReadHead = true;
                    }
                }

                PolyruleKeys.Instance.KeyTypes = _keyTypes;

                parseErrorSet.Clear();
                bool isKeyLine = TryParseKeyLine(trimedLine,
                    ref polyphonyWord, parseErrorSet, domain);

                domain = DomainItem.GeneralDomain;
                AddParseError(errorSet, lineNum, parseErrorSet);
                if (isKeyLine)
                {
                    continue;
                }

                parseErrorSet.Clear();
                bool isConditionLine = TryParseConditionLine(trimedLine, phoneSet,
                    polyphonyWord, parseErrorSet);
                AddParseError(errorSet, lineNum, parseErrorSet);
                if (isConditionLine)
                {
                    continue;
                }

                errorSet.Add(PolyRuleError.InvalidLineFormat,
                    lineNum.ToString(CultureInfo.InvariantCulture), trimedLine);
            }

            if (polyphonyWord != null)
            {
                _polyphonyWords.Add(polyphonyWord);
            }

            if (string.IsNullOrEmpty(_keyString))
            {
                errorSet.Add(PolyRuleError.MissPrimaryKey,
                    filePath);
            }

            errorSet.AddRange(CheckDupWordDefinitions());

            foreach (PolyphonyRule rule in _polyphonyWords)
            {
                errorSet.AddRange(rule.CheckDupRuleConditions());
            }

            return errorSet;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// ParseDeclearKey.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <param name="keyName">KeyName.</param>
        /// <param name="keyType">KeyType.</param>
        private static void ParseDeclearKey(string line, ref string keyName, ref KeyType keyType)
        {
            Match match = Regex.Match(line, DeclearKeyRegex);

            // Please refer to definition "DeclearKeyRegex".
            // Should contains at least key declear and type, and the group may be 3 or 4
            // If there is no comment the value is 3, or the value is 4.
            if (match.Groups.Count < 3)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Invalid declear line : [{0}]", line));
            }

            keyName = match.Groups[1].ToString();

            try
            {
                keyType = (KeyType)Enum.Parse(typeof(KeyType),
                    match.Groups[2].ToString(), true);
            }
            catch (ArgumentException)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Invalid file format for line : [{0}]", line));
            }
        }

        /// <summary>
        /// ParseDomainKey.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <param name="keyName">KeyName.</param>
        private static void ParseDomainKey(string line, ref string keyName)
        {
            Match match = Regex.Match(line, DomainLineRegex);

            // domain line is as [domain=address]
            if (match.Groups.Count != 4)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Invalid domain line : [{0}]", line));
            }

            keyName = match.Groups[2].ToString();
        }

        /// <summary>
        /// Whether the line is comment.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <returns>Indicating whether the line is comment.</returns>
        private static bool IsComment(string line)
        {
            return Regex.Match(line, CommentLineRegex).Success;
        }

        /// <summary>
        /// Whether the line is domain tag.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <returns>Indicating whether the line is domain tag.</returns>
        private static bool IsDomainTag(string line)
        {
            return Regex.Match(line, DomainLineRegex).Success;
        }

        /// <summary>
        /// Whether the line is condition.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <returns>Indicating whether the line is condition.</returns>
        private static bool IsConditionLine(string line)
        {
            return Regex.Match(line, ConditionLineRegex).Success;
        }

        /// <summary>
        /// Whether the line is declear.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <returns>Indicating whether the line is declear.</returns>
        private static bool IsDeclearKey(string line)
        {
            return Regex.Match(line, DeclearKeyRegex).Success;
        }

        /// <summary>
        /// AddParseError.
        /// </summary>
        /// <param name="errorSet">ErrorSet.</param>
        /// <param name="lineNum">LineNum.</param>
        /// <param name="parseErrorSet">ParseErrorSet.</param>
        private void AddParseError(ErrorSet errorSet, int lineNum, ErrorSet parseErrorSet)
        {
            foreach (Error parseError in parseErrorSet.Errors)
            {
                Error error = new Error(PolyRuleError.ParseError, parseError,
                    lineNum.ToString(CultureInfo.InvariantCulture));

                // Keep the same severity with the original error severity.
                error.Severity = parseError.Severity;
                errorSet.Add(error);
            }
        }

        /// <summary>
        /// TryParseConditionLine.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <param name="phoneSet">PhoneSet.</param>
        /// <param name="polyphonyWord">PolyphonyWord.</param>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>Whether the line is condition line.</returns>
        private bool TryParseConditionLine(string line, TtsPhoneSet phoneSet,
            PolyphonyRule polyphonyWord, ErrorSet errorSet)
        {
            bool isConditionLine = false;
            if (IsConditionLine(line))
            {
                isConditionLine = true;
                if (polyphonyWord == null)
                {
                    errorSet.Add(PolyRuleError.MissKeyValueLine, line);
                }

                errorSet.AddRange(ParseConditionLine(line, phoneSet, polyphonyWord));
            }

            return isConditionLine;
        }

        /// <summary>
        /// TryParseKeyLine.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <param name="polyphonyWord">PolyphonyWord.</param>
        /// <param name="errorSet">ErrorSet.</param>
        /// <param name="domain">Domain.</param>
        /// <returns>Whether the line is key line.</returns>
        private bool TryParseKeyLine(string line, ref PolyphonyRule polyphonyWord,
            ErrorSet errorSet, string domain)
        {
            bool isKeyLine = false;

            if (IsKeyLine(line))
            {
                isKeyLine = true;
                if (polyphonyWord != null)
                {
                    if (polyphonyWord.PolyphonyProns.Count == 0)
                    {
                        errorSet.Add(PolyRuleError.NoConditionForWord, polyphonyWord.Word);
                    }
                    else
                    {
                        _polyphonyWords.Add(polyphonyWord);
                    }
                }

                polyphonyWord = new PolyphonyRule();
                polyphonyWord.Domain = domain;

                int errorCountBeforeParsing = errorSet.Errors.Count;
                string keyValue = ParseKeyValueLine(line, errorSet);
                if (errorSet.Errors.Count == errorCountBeforeParsing)
                {
                    polyphonyWord.Word = keyValue;
                }
            }

            return isKeyLine;
        }

        /// <summary>
        /// TryParseKeyDeclear.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <param name="firstKeyString">FirstKeyString.</param>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>Whether the line is key declear line.</returns>
        private bool TryParseKeyDeclear(string line,
            ref bool firstKeyString, ErrorSet errorSet)
        {
            bool isKeyDeclearLine = false;

            // No need check key declear after finish parsing the declear part.
            if (IsDeclearKey(line))
            {
                isKeyDeclearLine = true;
                string keyName = string.Empty;
                KeyType keyType = KeyType.String;
                ParseDeclearKey(line, ref keyName, ref keyType);
                if (_keyTypes.ContainsKey(keyName))
                {
                    errorSet.Add(PolyRuleError.DuplicateKeyName, keyName);
                }
                else
                {
                    _keyTypes.Add(keyName, keyType);
                    if (firstKeyString)
                    {
                        _keyString = keyName;
                        firstKeyString = false;
                    }
                }
            }

            return isKeyDeclearLine;
        }

        /// <summary>
        /// ParsePolyCondition.
        /// </summary>
        /// <param name="expression">Expression.</param>
        /// <param name="condition">Condition.</param>
        /// <param name="errorSet">ErrorSet.</param>
        private void ParsePolyCondition(string expression,
            PolyphonyCondition condition, ErrorSet errorSet)
        {
            string subExpression = expression;

            // If the value is string, then search operator before """
            if (subExpression.IndexOf('"') > 0)
            {
                subExpression = subExpression.Substring(0, subExpression.IndexOf('"'));
            }

            foreach (string oper in _allOperators)
            {
                if (subExpression.IndexOf(oper) >= 0)
                {
                    condition.Operator = oper;
                    break;
                }
            }

            bool succeeded = true;
            if (string.IsNullOrEmpty(condition.Operator))
            {
                errorSet.Add(PolyRuleError.MissingOperatorInCondition,
                    expression);
                succeeded = false;
            }

            if (succeeded)
            {
                condition.Key = expression.Substring(0,
                    expression.IndexOf(condition.Operator)).Trim();
                if (!_keyTypes.ContainsKey(condition.Key))
                {
                    errorSet.Add(PolyRuleError.NotDeclearedConditionKey,
                        condition.Key, expression);
                    succeeded = false;
                }
            }

            if (succeeded)
            {
                string valueExpression = expression.Substring(
                    expression.IndexOf(condition.Operator) + condition.Operator.Length).Trim();
                if (_keyTypes[condition.Key] == KeyType.String)
                {
                    Match match = Regex.Match(valueExpression, @"^""(.*)""$");
                    if (match.Success)
                    {
                        valueExpression = match.Groups[1].ToString();
                    }
                    else
                    {
                        errorSet.Add(PolyRuleError.InvalidConditionFormat,
                            expression);
                        succeeded = false;
                    }
                }
                else
                {
                    int intValue;
                    if (!int.TryParse(valueExpression, out intValue))
                    {
                        errorSet.Add(PolyRuleError.InvalidConditionFormat,
                            expression);
                        succeeded = false;
                    }
                }

                condition.Value = valueExpression;
            }
        }

        /// <summary>
        /// ParseConditionLine.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <param name="phoneSet">PhoneSet.</param>
        /// <param name="polyphonyWord">PolyphonyWord.</param>
        /// <returns>ErrorSet.</returns>
        private ErrorSet ParseConditionLine(string line, TtsPhoneSet phoneSet,
            PolyphonyRule polyphonyWord)
        {
            ErrorSet errorSet = new ErrorSet();
            Match match = Regex.Match(line, ConditionLineRegex);
            if (match.Groups.Count < 3)
            {
                errorSet.Add(PolyRuleError.InvalidConditionFormat,
                    line);
            }
            else
            {
                PolyphonyPron polyphonyPron = new PolyphonyPron();
                polyphonyPron.Pron = match.Groups[2].ToString().Trim();

                // Allow empty pronunciation for polyphony rule.
                if (!string.IsNullOrEmpty(polyphonyPron.Pron) && phoneSet != null)
                {
                    errorSet.AddRange(Pronunciation.Validate(polyphonyPron.Pron, phoneSet));
                }

                string conditions = match.Groups[1].ToString().Trim();
                bool hasMatched = false;
                foreach (Match conditionMatch in Regex.Matches(conditions, ConditionRegex))
                {
                    hasMatched = true;
                    string expression = conditionMatch.Value;
                    PolyphonyCondition condition = new PolyphonyCondition();
                    ParsePolyCondition(expression.Trim(), condition, errorSet);
                    polyphonyPron.Conditions.Add(condition);
                }

                if (hasMatched)
                {
                    if (errorSet.GetSeverityCount(ErrorSeverity.MustFix) == 0)
                    {
                        if (polyphonyWord == null)
                        {
                            errorSet.Add(PolyRuleError.MissKeyValueLine, line);
                        }
                        else
                        {
                            polyphonyWord.PolyphonyProns.Add(polyphonyPron);
                        }
                    }
                }
                else
                {
                    errorSet.Add(PolyRuleError.InvalidConditionFormat, line);
                }
            }

            return errorSet;
        }

        /// <summary>
        /// ParseKeyValueLine.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <param name="errorSet">ErrorSet.</param>
        /// <returns>Condition value.</returns>
        private string ParseKeyValueLine(string line, ErrorSet errorSet)
        {
            string primaryKeyValue = string.Empty;
            Match match = Regex.Match(line, KeyLineRegex);
            if (match.Groups.Count != 2)
            {
                errorSet.Add(PolyRuleError.InvalidPrimaryKeyValueForamt, line);
            }
            else
            {
                primaryKeyValue = match.Groups[1].ToString();
            }

            return primaryKeyValue;
        }

        /// <summary>
        /// Whether the line is key value line.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <returns>Indicating whether the line is key value line.</returns>
        private bool IsKeyLine(string line)
        {
            return Regex.Match(line, KeyLineRegex).Success;
        }

        /// <summary>
        /// Check if there're duplicate polyphony word definitions.
        /// </summary>
        /// <returns>ErrorSet.</returns>
        private ErrorSet CheckDupWordDefinitions()
        {
            ErrorSet errorSet = new ErrorSet();

            List<string> polyWords = new List<string>();
            foreach (PolyphonyRule rule in _polyphonyWords)
            {
                if (polyWords.Contains(rule.Word))
                {
                    errorSet.Add(PolyRuleError.DuplicateWordDefinitions, rule.Word);
                }
                else
                {
                    polyWords.Add(rule.Word);
                }
            }

            return errorSet;
        }

        #endregion
    }

    /// <summary>
    /// Polyphony rule keys.
    /// </summary>
    public class PolyruleKeys
    {
        private static PolyruleKeys _instance;

        private Dictionary<string, PolyphonyRuleFile.KeyType> _keyTypes;

        private PolyruleKeys()
        {
        }

        /// <summary>
        /// Gets instance of PolyruleKeys.
        /// </summary>
        public static PolyruleKeys Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PolyruleKeys();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Gets or sets keyTypes.
        /// </summary>
        public Dictionary<string, PolyphonyRuleFile.KeyType> KeyTypes
        {
            get { return _keyTypes; }
            set { _keyTypes = value; }
        }
    }
}