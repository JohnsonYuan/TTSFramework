//----------------------------------------------------------------------------
// <copyright file="Question.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements HTS question object model
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// HTS linguistic feature type.
    /// </summary>
    public enum HtsLingFeatureType
    {
        /// <summary>
        /// Unknown feature type.
        /// </summary>
        UnKnown = 0,

        /// <summary>
        /// Interger feature type.
        /// </summary>
        Integer,

        /// <summary>
        /// Phone feature type.
        /// </summary>
        TtsPhone,

        /// <summary>
        /// Part of speech feature type.
        /// </summary>
        TtsPos,

        /// <summary>
        /// Stress feature type.
        /// </summary>
        Stress,

        /// <summary>
        /// Boundary tone feature type.
        /// </summary>
        BoundaryTone,

        /// <summary>
        /// Tone feature type.
        /// </summary>
        Tone,
    }

    /// <summary>
    /// HTS question operators.
    /// </summary>
    public enum QuestionOperator
    {
        /// <summary>
        /// Equal operator.
        /// </summary>
        Equal = 0,

        /// <summary>
        /// Belong operator.
        /// </summary>
        Belong,

        /// <summary>
        /// Greater operator.
        /// </summary>
        Greater,

        /// <summary>
        /// Greater or equal operator.
        /// </summary>
        GreaterEqual,

        /// <summary>
        /// Less operator.
        /// </summary>
        Less,

        /// <summary>
        /// Less or equal.
        /// </summary>
        LessEqual
    }

    /// <summary>
    /// HTS question.
    /// </summary>
    public class Question
    {
        #region Field

        /// <summary>
        /// The key word to identify a question.
        /// </summary>
        public const string QuestionKeyword = "QS";

        /// <summary>
        /// The regex to match the whole question.
        /// </summary>
        public const string QuestionRegex = @"^\s*QS\s+(""|')*([^""']*)(""|')*\s+\{(.*)\}\s*$";

        /// <summary>
        /// The regex to match the name of the question.
        /// </summary>
        public const string NameRegex = @"^(.*)(==|<=|<|>=|>)(.*)$";

        /// <summary>
        /// The regex to match the single pattern of the question.
        /// </summary>
        public const string PatternRegex = @"""*([^0-9a-zA-Z_?""]*)([0-9a-zA-Z_?]+)([^0-9a-zA-Z_?""]*)""*";

        /// <summary>
        /// The delimiter for values embedded in feature name for each question .
        /// </summary>
        public const string ValueDelimiter = "-";

        /// <summary>
        /// The Dictionary whose key is operator string, and whose the value is the QuestionOperator.
        /// </summary>
        private static readonly Dictionary<string, QuestionOperator> OperatorStrings =
            new Dictionary<string, QuestionOperator>
        {
            { "==", QuestionOperator.Equal },
            { "<=", QuestionOperator.LessEqual },
            { "<", QuestionOperator.Less },
            { ">=", QuestionOperator.GreaterEqual },
            { ">", QuestionOperator.Greater },
            { "_", QuestionOperator.Belong },
        };

        /// <summary>
        /// The name of the question, which depends _featureName, _oper and _valueSetName.
        /// </summary>
        private string _name;

        /// <summary>
        /// The feature name of the question.
        /// </summary>
        private string _featureName;

        /// <summary>
        /// The operator of the question.
        /// </summary>
        private QuestionOperator _oper;

        /// <summary>
        /// The value set name of the question.
        /// </summary>
        private string _valueSetName;

        /// <summary>
        /// The value set of the question.
        /// </summary>
        private List<string> _valueSet = new List<string>();

        /// <summary>
        /// The code value set of the question.
        /// </summary>
        private List<int> _codeValueSet = new List<int>();

        /// <summary>
        /// The expresssion string of the question, which depends _name and _patterns.
        /// </summary>
        private string _expression;

        /// <summary>
        /// The match pattern string of the question, which depends _valueSet, _leftSeparator and _rightSeparator.
        /// </summary>
        private string _patterns;

        /// <summary>
        /// The left separator of the value in the question.
        /// </summary>
        private string _leftSeparator;

        /// <summary>
        /// The right separator of the value in the question.
        /// </summary>
        private string _rightSeparator;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the Question class as an empty class.
        /// </summary>
        public Question()
        {
        }

        /// <summary>
        /// Initializes a new instance of the Question class and set its Expression.
        /// </summary>
        /// <param name="expression">Question expression.</param>
        public Question(string expression)
        {
            if (string.IsNullOrEmpty(expression))
            {
                throw new ArgumentNullException("expression");
            }

            Expression = expression;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets question expression.
        /// </summary>
        public string Expression
        {
            get { return ToString(); }
            set { UpdateFields(value.Trim()); }
        }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        public Language Language
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the question name.
        /// </summary>
        public string Name
        {
            get
            {
                if (_name == null)
                {
                    if (string.IsNullOrEmpty(_featureName))
                    {
                        throw new InvalidOperationException("FeatureName is empty");
                    }

                    if (string.IsNullOrEmpty(_valueSetName))
                    {
                        throw new InvalidOperationException("ValueSetName is empty");
                    }

                    _name = _featureName + ToOperator(_oper) + _valueSetName;
                }

                return _name;
            }

            set
            {
                UpdateName(value.Trim());
            }
        }

        /// <summary>
        /// Gets or sets feature Name.
        /// </summary>
        public string FeatureName
        {
            get
            {
                return _featureName;
            }

            set
            {
                if (_featureName != value)
                {
                    _featureName = value;
                    ResetName();
                }
            }
        }

        /// <summary>
        /// Gets or sets operator of the question.
        /// </summary>
        public QuestionOperator Oper
        {
            get
            {
                return _oper;
            }

            set
            {
                if (_oper != value)
                {
                    _oper = value;
                    ResetName();
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the value set of the question.
        /// </summary>
        public string ValueSetName
        {
            get
            {
                return _valueSetName;
            }

            set
            {
                if (_valueSetName != value)
                {
                    _valueSetName = value;
                    ResetName();
                }
            }
        }

        /// <summary>
        /// Gets operator of the question.
        /// </summary>
        public string Patterns
        {
            get
            {
                if (_patterns == null)
                {
                    if (_valueSet.Count <= 0)
                    {
                        throw new InvalidDataException("Cannot convert this object to string when the ValueSet is empty.");
                    }

                    // need '*' is separator is not empty.
                    string left = string.IsNullOrEmpty(LeftSeparator) ? string.Empty : "*" + LeftSeparator;
                    string right = string.IsNullOrEmpty(RightSeparator) ? string.Empty : RightSeparator + "*";

                    string[] patterns = new string[_valueSet.Count];
                    for (int i = 0; i < _valueSet.Count; ++i)
                    {
                        patterns[i] = Helper.NeutralFormat("\"{0}{1}{2}\"", left, _valueSet[i], right);
                    }

                    _patterns = string.Join(",", patterns);
                }

                return _patterns;
            }
        }

        /// <summary>
        /// Gets or sets the value set of the question.
        /// </summary>
        public ReadOnlyCollection<string> ValueSet
        {
            get
            {
                return _valueSet.AsReadOnly();
            }

            set
            {
                _valueSet = new List<string>(value);
                ResetPatterns();
            }
        }

        /// <summary>
        /// Gets or sets the code value set.
        /// </summary>
        public ReadOnlyCollection<int> CodeValueSet
        {
            get
            {
                return _codeValueSet.AsReadOnly();
            }

            set
            {
                _codeValueSet = new List<int>(value);
            }
        }

        /// <summary>
        /// Gets or sets the left separator of the question.
        /// </summary>
        public string LeftSeparator
        {
            get
            {
                return _leftSeparator;
            }

            set
            {
                if (_leftSeparator != value)
                {
                    _leftSeparator = value;
                    ResetPatterns();
                }
            }
        }

        /// <summary>
        /// Gets or sets the right separator of the question.
        /// </summary>
        public string RightSeparator
        {
            get
            {
                return _rightSeparator;
            }

            set
            {
                if (_rightSeparator != value)
                {
                    _rightSeparator = value;
                    ResetPatterns();
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the expression of Question in HTK format.
        /// </summary>
        /// <returns>Question string.</returns>
        public override string ToString()
        {
            // Only perform the action when the _expression is null.
            if (_expression == null)
            {
                _expression = string.Join(" ", new[] { QuestionKeyword, Name, "{", Patterns, "}" });
            }

            return _expression;
        }

        #endregion

        #region Comparable

        /// <summary>
        /// Tests equal with obj.
        /// </summary>
        /// <param name="obj">Object to test with.</param>
        /// <returns>True if equal, otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            Question other = obj as Question;

            if (other != null && Expression == other.Expression)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the hash code of this object.
        /// </summary>
        /// <returns>Hash code of this object.</returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Converts string to question operator.
        /// </summary>
        /// <param name="operString">Operator string.</param>
        /// <returns>Question operator.</returns>
        private static QuestionOperator ToOperator(string operString)
        {
            return OperatorStrings.Where(o => o.Key == operString).Single().Value;
        }

        /// <summary>
        /// Converts question operator to string.
        /// </summary>
        /// <param name="oper">Question operator.</param>
        /// <returns>Question string.</returns>
        private static string ToOperator(QuestionOperator oper)
        {
            return OperatorStrings.Where(o => o.Value == oper).Single().Key;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Expand question value with wildcard ?.
        /// </summary>
        /// <param name="questionValue">Question value.</param>
        /// <param name="valueSet">Value set to add.</param>
        private static void ExpandWildCard(string questionValue, List<string> valueSet)
        {
            int count = 0;
            for (int i = 0; i < questionValue.Length; i++)
            {
                if (questionValue[i] == '?')
                {
                    count++;
                }
            }

            Debug.Assert(count > 0);
            int[] arr = new int[count];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = 0;
            }

            bool done = false;
            while (!done)
            {
                StringBuilder sb = new StringBuilder();
                int j = 0;
                for (int i = 0; i < questionValue.Length; i++)
                {
                    if (questionValue[i] == '?')
                    {
                        sb.Append(arr[j].ToString());
                        j++;
                    }
                    else
                    {
                        sb.Append(questionValue[i]);
                    }
                }

                valueSet.Add(sb.ToString());
                int n = arr[arr.Length - 1];
                if (n == 9)
                {
                    arr[arr.Length - 1] = 0;
                    int i = 0;
                    for (i = arr.Length - 2; i >= 0 && arr[i] == 9; i--)
                    {
                        arr[i] = 0;
                    }

                    if (i < 0)
                    {
                        done = true;
                    }
                    else
                    {
                        arr[i]++;
                    }
                }
                else
                {
                    arr[arr.Length - 1]++;
                }
            }
        }

        /// <summary>
        /// Updates the fields of this object according to given expression.
        /// The expression looks like: QS Phone.PhoneIdentity_High { "*-ih+*","*-iy+*","*-uh+*","*-uw+*" }
        ///                            QS Phone.BwPosInSyllable==6 { "*|6-*" }
        ///                            QS Phone.BwPosInSyllable&lt;=2 { "*|1-*","*|2-*" }.
        /// </summary>
        /// <param name="expression">The given expression.</param>
        private void UpdateFields(string expression)
        {
            Match match = Regex.Match(expression, QuestionRegex);
            if (match.Success && !string.IsNullOrEmpty(match.Groups[2].Value))
            {
                UpdateName(match.Groups[2].Value);
                _valueSet.Clear();

                // Split the patterns in the question.
                string[] patterns = match.Groups[4].Value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (patterns.Length <= 0)
                {
                    throw new InvalidDataException(Helper.NeutralFormat("Invalid question regression expression [{0}] on question string [{1}].", QuestionRegex, expression));
                }

                _leftSeparator = null;
                _rightSeparator = null;

                // Parse the each pattern and add them into value set.
                foreach (string pattern in patterns)
                {
                    Match patternMatch = Regex.Match(pattern, PatternRegex);
                    if (patternMatch.Success)
                    {
                        if (_leftSeparator == null)
                        {
                            _leftSeparator = patternMatch.Groups[1].Value.Trim('*');
                        }
                        else
                        {
                            Debug.Assert(_leftSeparator == patternMatch.Groups[1].Value.Trim('*'), "The separator from different pattern should be equal");
                        }

                        if (patternMatch.Groups[2].Value.Contains("?"))
                        {
                            ExpandWildCard(patternMatch.Groups[2].Value, _valueSet);
                        }
                        else
                        {
                            _valueSet.Add(patternMatch.Groups[2].Value);
                        }

                        if (_rightSeparator == null)
                        {
                            _rightSeparator = patternMatch.Groups[3].Value.Trim('*');
                        }
                        else
                        {
                            Debug.Assert(_rightSeparator == patternMatch.Groups[3].Value.Trim('*'), "The separator from different pattern should be equal");
                        }
                    }
                }
            }
            else
            {
                throw new InvalidDataException(Helper.NeutralFormat("Invalid question regression expression [{0}] on question string [{1}].", QuestionRegex, expression));
            }

            ResetName();
            ResetPatterns();
        }

        /// <summary>
        /// Updates the fields of this object according to the given name.
        /// The name looks like: Phone.PhoneIdentity_High
        ///                      Phone.BwPosInSyllable==6
        ///                      Phone.BwPosInSyllable&lt;=2.
        /// </summary>
        /// <param name="name">The given name.</param>
        private void UpdateName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            Match matchName = Regex.Match(name, NameRegex);
            if (matchName.Success)
            {
                _featureName = matchName.Groups[1].Value;
                _oper = ToOperator(matchName.Groups[2].Value);
                _valueSetName = matchName.Groups[3].Value;
            }
            else
            {
                int index = name.IndexOfAny(new[] { '-', '_' });
                if (index < 0 && index == name.Length)
                {
                    throw new ArgumentException(Helper.NeutralFormat("Unknown format of question name \"{0}\"", name));
                }

                _featureName = name.Substring(0, index);
                _oper = QuestionOperator.Belong;
                _valueSetName = name.Substring(index + 1);
            }

            ResetName();
        }

        /// <summary>
        /// Resets name to rebuild.
        /// </summary>
        private void ResetName()
        {
            _name = null;
            _expression = null;
        }

        /// <summary>
        /// Resets patterns to rebuild.
        /// </summary>
        private void ResetPatterns()
        {
            _patterns = null;
            _expression = null;
        }

        #endregion
    }
}