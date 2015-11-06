//----------------------------------------------------------------------------
// <copyright file="QuestionBuilder.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines a common library to builder Htk question.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Linguistic feature value type.
    /// </summary>
    public enum LingFeatureValueType
    {
        /// <summary>
        /// Enum type feature.
        /// </summary>
        Null = -1,

        /// <summary>
        /// Enum type feature.
        /// </summary>
        Enumerable = 0,

        /// <summary>
        /// Integer type feature.
        /// </summary>
        Integer = 1,

        /// <summary>
        /// Enum type feature.
        /// </summary>
        PhoneID = 2,
    }

    /// <summary>
    /// The all values for QuestionMode.
    /// </summary>
    public enum QuestionMode
    {
        /// <summary>
        /// Build question for LSP.
        /// </summary>
        Lsp = 1,

        /// <summary>
        /// Build question for logF0.
        /// </summary>
        LogF0 = 2,

        /// <summary>
        /// Build question for duration.
        /// </summary>
        Duration = 4,

        /// <summary>
        /// Build question for multi-band excitation.
        /// </summary>
        Mbe = 8,

        /// <summary>
        /// Build question for power.
        /// </summary>
        Power = 16,

        /// <summary>
        /// Build question for logF0 and duration.
        /// </summary>
        Prosody = LogF0 | Duration,

        /// <summary>
        /// Build question for LSP, logF0, duration and power.
        /// </summary>
        All = Lsp | LogF0 | Duration | Mbe | Power
    }

    /// <summary>
    /// The class to build Hts Question builder.
    /// </summary>
    public class QuestionBuilder
    {
        #region Fields

        /// <summary>
        /// The question list of this question builder.
        /// </summary>
        private readonly List<Question> _questionList = new List<Question>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the QuestionBuilder class according to phonetic question file.
        /// </summary>
        /// <param name="phoneticQuestionFile">The phonetic question file from Hts.</param>
        /// <param name="phonemes">The valid phoneme set, used to validate the phonetic questions.</param>
        public QuestionBuilder(string phoneticQuestionFile, IEnumerable<string> phonemes)
        {
            // Load the all phonetic question firstly.
            PhoneQuestion[] phoneticQuestions = LoadPhoneticQuestions(phoneticQuestionFile, phonemes);

            // For each phonetic question, assign its fields to a Question item.
            foreach (PhoneQuestion phoneQuestion in phoneticQuestions)
            {
                Question question = new Question
                {
                    Oper = QuestionOperator.Belong,
                    ValueSetName = phoneQuestion.Name,
                    ValueSet = phoneQuestion.Phones.AsReadOnly(),
                };

                _questionList.Add(question);
            }
        }

        /// <summary>
        /// Initializes a new instance of the QuestionBuilder class according to a name and its value.
        /// </summary>
        /// <param name="namedValues">A dictionary to hold the name and its value.</param>
        public QuestionBuilder(Dictionary<string, object> namedValues)
        {
            foreach (KeyValuePair<string, object> kvp in namedValues)
            {
                List<string> valueSet = new List<string> { kvp.Value.ToString() };
                Question question = new Question
                {
                    Oper = QuestionOperator.Equal,
                    ValueSetName = kvp.Key,
                    ValueSet = valueSet.AsReadOnly(),
                };

                _questionList.Add(question);
            }
        }

        /// <summary>
        /// Initializes a new instance of the QuestionBuilder class according to enum.
        /// </summary>
        /// <param name="enumType">A enum type used to generate questions.</param>
        public QuestionBuilder(Type enumType)
        {
            string[] names = Enum.GetNames(enumType);
            foreach (string name in names)
            {
                Question question = new Question
                {
                    Oper = QuestionOperator.Equal,
                    ValueSetName = name,
                };

                int value = (int)Enum.Parse(enumType, name);
                List<string> valueSet = new List<string> { value.ToString(CultureInfo.InvariantCulture) };

                question.ValueSet = valueSet.AsReadOnly();
                _questionList.Add(question);
            }
        }

        /// <summary>
        /// Initializes a new instance of the QuestionBuilder class according to integer values.
        /// </summary>
        /// <param name="values">The valid values.</param>
        public QuestionBuilder(IEnumerable<int> values)
        {
            List<int> sortedValues = new List<int>(values);
            sortedValues.Sort();

            // Creates the equal question first.
            for (int i = 0; i < sortedValues.Count; i++)
            {
                string value = sortedValues[i].ToString(CultureInfo.InvariantCulture);
                List<string> valueSet = new List<string> { value };
                Question question = new Question
                {
                    Oper = QuestionOperator.Equal,
                    ValueSetName = value,
                    ValueSet = valueSet.AsReadOnly(),
                };

                _questionList.Add(question);
            }

            // Then the less equal question.
            for (int i = 1; i < sortedValues.Count; i++)
            {
                Question question = new Question
                {
                    Oper = QuestionOperator.LessEqual,
                    ValueSetName = sortedValues[i].ToString(CultureInfo.InvariantCulture),
                    ValueSet = BuildLessQuestionValueSetStartsWithDigit(sortedValues[i] + 1).AsReadOnly(),
                };

                _questionList.Add(question);
            }
        }

        /// <summary>
        /// Initializes a new instance of the QuestionBuilder class according to values, range and value type.
        /// </summary>
        /// <param name="values">The valid values.</param>
        /// <param name="minValue">The minimum feature value for generating questions.</param>
        /// <param name="maxValue">The maximum feature value for generating questions.</param>
        /// <param name="valueType">The value type.</param>
        public QuestionBuilder(IEnumerable<int> values, int minValue, int maxValue, LingFeatureValueType valueType)
        {
            List<int> sortedValues = new List<int>(values);
            sortedValues.Sort();

            // Creates the equal questions for enumerable or null type feature.
            if (valueType == LingFeatureValueType.Null || valueType == LingFeatureValueType.Enumerable)
            {
                for (int i = 0; i < sortedValues.Count; i++)
                {
                    if ((minValue > 0 && sortedValues[i] < minValue) || (maxValue > 0 && sortedValues[i] > maxValue))
                    {
                        continue;
                    }

                    string value = sortedValues[i].ToString(CultureInfo.InvariantCulture);
                    List<string> valueSet = new List<string> { value };
                    Question question = new Question
                    {
                        Oper = QuestionOperator.Equal,
                        ValueSetName = value,
                        ValueSet = valueSet.AsReadOnly(),
                    };

                    _questionList.Add(question);
                }
            }

            // Creates the less equal questions for integer or null type feature.
            if (valueType == LingFeatureValueType.Null || valueType == LingFeatureValueType.Integer)
            {
                for (int i = 0; i < sortedValues.Count; i++)
                {
                    if ((minValue > 0 && sortedValues[i] < minValue) || (maxValue > 0 && sortedValues[i] > maxValue))
                    {
                        continue;
                    }

                    string value = sortedValues[i].ToString(CultureInfo.InvariantCulture);
                    List<string> valueSet = new List<string>();
                    for (int j = 0; j <= sortedValues[i]; ++j)
                    {
                        valueSet.Add(j.ToString(CultureInfo.InvariantCulture));
                    }

                    Question question = new Question
                    {
                        Oper = QuestionOperator.LessEqual,
                        ValueSetName = value,
                        ValueSet = valueSet.AsReadOnly(),
                    };

                    _questionList.Add(question);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the QuestionBuilder class according to integer values.
        /// </summary>
        /// <param name="minValue">The minValue of this item.</param>
        /// <param name="maxValue">The maxValue of this item.</param>
        public QuestionBuilder(int minValue, int maxValue) :
            this(Enumerable.Range(minValue, maxValue - minValue + 1))
        {
        }

        /// <summary>
        /// Initializes a new instance of the QuestionBuilder class according to given values.
        /// </summary>
        /// <param name="oper">
        /// The given value of operator.
        /// </param>
        /// <param name="name">
        /// The given name.
        /// </param>
        /// <param name="valueSet">
        /// The given value set.
        /// </param>
        public QuestionBuilder(QuestionOperator oper, string name, List<string> valueSet)
        {
            Question question = new Question
            {
                Oper = oper,
                ValueSetName = name,
                ValueSet = valueSet.AsReadOnly(),
            };

            _questionList.Add(question);
        }

        /// <summary>
        /// Initializes a new instance of the QuestionBuilder class according to given values.
        /// </summary>
        /// <param name="oper">The given value of operator.</param>
        /// <param name="value">The given value.</param>
        public QuestionBuilder(QuestionOperator oper, int value)
        {
            if (value < 0)
            {
                throw new ArgumentException("The given integer value shouldn't be less than 0");
            }

            // If the operator is belong to, it can be consider as equal since the value set only have one value.
            if (oper == QuestionOperator.Belong)
            {
                oper = QuestionOperator.Equal;
            }

            // If the operator is greater or equal, it can be consider as less.
            // So, the returned question will be an equalable question with opposite answer.
            // The opposite answer won't impact the decision tree and related logic.
            if (oper == QuestionOperator.GreaterEqual)
            {
                oper = QuestionOperator.Less;
            }

            // If the operator is greater, it can be consider as less or equal.
            // So, the returned question will be an equalable question with opposite answer.
            // The opposite answer won't impact the decision tree and related logic.
            if (oper == QuestionOperator.Greater)
            {
                oper = QuestionOperator.LessEqual;
            }

            Question question = new Question
            {
                Oper = oper,
                ValueSetName = value.ToString(CultureInfo.InvariantCulture),
            };

            switch (oper)
            {
                case QuestionOperator.Equal:
                    question.ValueSet = new List<string> { question.ValueSetName }.AsReadOnly();
                    break;
                case QuestionOperator.Less:
                    question.ValueSet = BuildLessQuestionValueSetStartsWithDigit(value).AsReadOnly();
                    break;
                case QuestionOperator.LessEqual:
                    question.ValueSet = BuildLessQuestionValueSetStartsWithDigit(value + 1).AsReadOnly();
                    break;
                default:
                    throw new NotSupportedException(Helper.NeutralFormat("Unsupported question operator \"{0}\"", oper.ToString()));
            }

            _questionList.Add(question);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Build question for not applicable feature value.
        /// </summary>
        /// <param name="featureName">The corresponding feature name.</param>
        /// <param name="leftSeparator">The left separator of this feature.</param>
        /// <param name="rightSeparator">The right separator of this feature.</param>
        /// <returns>The question for not applicable feature value.</returns>
        public static string BuildNotApplicableFeatureQuestion(string featureName, string leftSeparator, string rightSeparator)
        {
            Question question = new Question
            {
                FeatureName = featureName,
                LeftSeparator = leftSeparator,
                RightSeparator = rightSeparator,
                Oper = QuestionOperator.Equal,
                ValueSetName = Label.NotApplicableFeatureValue,
                ValueSet = new List<string> { Label.NotApplicableFeatureValue }.AsReadOnly(),
            };

            return question.Expression;
        }

        /// <summary>
        /// Extends question mark in value set.
        /// </summary>
        /// <param name="valueSet">The input value set of the question.</param>
        /// <returns>The all possible values.</returns>
        public static List<string> ExtendQuestionMarkInValueSet(IEnumerable<string> valueSet)
        {
            List<string> results = new List<string>();

            foreach (string value in valueSet)
            {
                int indexOfFirstQuestionMark = value.IndexOf('?');
                if (indexOfFirstQuestionMark < 0)
                {
                    results.Add(value);
                }
                else
                {
                    string subString1 = indexOfFirstQuestionMark == 0 ?
                        string.Empty :
                        value.Substring(0, indexOfFirstQuestionMark);
                    string subString2 = indexOfFirstQuestionMark == value.Length - 1 ?
                        string.Empty :
                        value.Substring(indexOfFirstQuestionMark + 1);
                    results.AddRange(
                        ExtendQuestionMarkInValueSet(
                            Enumerable.Range(0, 10).Select(
                                i => subString1 + i.ToString(CultureInfo.InvariantCulture) + subString2)));
                }
            }

            return results;
        }

        /// <summary>
        /// Build questions.
        /// </summary>
        /// <param name="featureName">The corresponding feature name.</param>
        /// <param name="leftSeparator">The left separator of this feature.</param>
        /// <param name="rightSeparator">The right separator of this feature.</param>
        /// <returns>The array of questions.</returns>
        public string[] BuildQuestions(string featureName, string leftSeparator, string rightSeparator)
        {
            List<string> questions = new List<string>();

            foreach (Question question in _questionList)
            {
                question.FeatureName = featureName;
                question.LeftSeparator = leftSeparator;
                question.RightSeparator = rightSeparator;

                questions.Add(question.Expression);
            }

            return questions.ToArray();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Load and verify the phonetic questions.
        /// </summary>
        /// <param name="phoneticQuestionFile">The file name of phonetic question.</param>
        /// <param name="phonemes">The current phoneme set.</param>
        /// <returns>The array which containts the all phonetic question.</returns>
        private static PhoneQuestion[] LoadPhoneticQuestions(string phoneticQuestionFile, IEnumerable<string> phonemes)
        {
            // Load phone question by QuestionFile.
            List<PhoneQuestion> phoneQuestions = new List<PhoneQuestion>(QuestionFile.Load(phoneticQuestionFile));

            // Test whether some question contains no value.
            if (phoneQuestions.Where(o => o.Phones.Count <= 0).Count() > 0)
            {
                throw new InvalidDataException(Helper.NeutralFormat("There are some questions have no value in file \"{0}\"", phoneticQuestionFile));
            }

            foreach (PhoneQuestion question in phoneQuestions)
            {
                for (int i = 0; i < question.Phones.Count; ++i)
                {
                    if (Phoneme.IsShortPausePhone(question.Phones[i]))
                    {
                        question.Phones[i] = Phoneme.ToHtk(Phoneme.ShortPausePhone);
                    }
                    else if (Phoneme.IsSilencePhone(question.Phones[i]))
                    {
                        question.Phones[i] = Phoneme.ToHtk(Phoneme.SilencePhone);
                    }
                }
            }

            phoneQuestions = phoneQuestions.Where(o => o.Phones.Count > 0).ToList();

            // Chech the question number.
            if (phoneQuestions.Count <= 0)
            {
                throw new InvalidDataException(Helper.NeutralFormat("No proper phonetic question loaded in file \"{0}\"", phoneticQuestionFile));
            }

            // Setup the unseen phoneme hash set and igored the silence and in phoneme set.
            HashSet<string> unseenPhonemes = new HashSet<string>(phonemes);

            // Visit the all questions to check whether it covers all the phonemes.
            foreach (PhoneQuestion question in phoneQuestions)
            {
                foreach (string phoneme in question.Phones)
                {
                    unseenPhonemes.Remove(phoneme);
                }
            }

            // Check whether there is some phonemes in the unsee set.
            if (unseenPhonemes.Count > 0)
            {
                StringBuilder error = new StringBuilder();
                foreach (string phoneme in unseenPhonemes)
                {
                    error.Append(' ');
                    error.Append(phoneme);
                }

                throw new InvalidDataException(
                    Helper.NeutralFormat("The phonetic question doesn't cover phonemes: {0}, please check file \"{1}\"", error.ToString(), phoneticQuestionFile));
            }

            return phoneQuestions.ToArray();
        }

        /// <summary>
        /// Builds less question including '?' mark for the value (digit * times, i.e. digit is 2, times is 100).
        /// </summary>
        /// <param name="digit">The first digit of the value, must be range in [1 ~ 9].</param>
        /// <param name="times">The times of the digit, must be multiple of 10.</param>
        /// <returns>The all possible pattern in the question.</returns>
        private static List<string> BuildQuestionMarkLessQuestion(int digit, int times)
        {
            List<string> results = new List<string>();

            // Adds the ?? patterns.
            string str = string.Empty;
            for (int i = 1; i < times; i *= 10)
            {
                str += "?";
                results.Add(str);
            }

            // Adds the x?? patterns.
            results.AddRange(Enumerable.Range(1, digit - 1).Select(i => i.ToString(CultureInfo.InvariantCulture) + str));

            return results;
        }

        /// <summary>
        /// Builds value set for less questions.
        /// </summary>
        /// <param name="value">The given value to build less question.</param>
        /// <returns>The all possible pattern in the less question.</returns>
        private static List<string> BuildLessQuestionValueSet(int value)
        {
            List<string> results = new List<string>();

            if (value <= 9)
            {
                // Less than 9, so that normal value can meet the request.
                results.AddRange(Enumerable.Range(0, value).Select(i => i.ToString(CultureInfo.InvariantCulture)));
            }
            else
            {
                long times = 1;
                while (times <= value)
                {
                    times *= 10;
                }

                times /= 10;
                int firstDigit = (int)(value / times);

                results.AddRange(BuildQuestionMarkLessQuestion(firstDigit, (int)times));

                string strOfFirstDigit = firstDigit.ToString(CultureInfo.InvariantCulture);
                int strLength = times.ToString(CultureInfo.InvariantCulture).Length - 1;
                foreach (string s in BuildLessQuestionValueSet(value - (int)(firstDigit * times)))
                {
                    if (s.Length == strLength)
                    {
                        results.Add(strOfFirstDigit + s);
                    }
                    else if (s.Length == strLength - 1)
                    {
                        results.Add(strOfFirstDigit + '0' + s);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Builds value set for less questions and ensure all the pattern starts with digit (not '?').
        /// </summary>
        /// <param name="value">The given value to build less question.</param>
        /// <returns>The all possible pattern in the less question.</returns>
        private static List<string> BuildLessQuestionValueSetStartsWithDigit(int value)
        {
            List<string> results = new List<string>();

            foreach (string s in BuildLessQuestionValueSet(value))
            {
                if (s.Trim('?').Length == 0)
                {
                    // Alls character is '?'.
                    string subStr = s.Substring(1);
                    if (s.Length == 1)
                    {
                        results.AddRange(Enumerable.Range(0, 10).Select(i => i.ToString(CultureInfo.InvariantCulture) + subStr));
                    }
                    else
                    {
                        results.AddRange(Enumerable.Range(1, 9).Select(i => i.ToString(CultureInfo.InvariantCulture) + subStr));
                    }
                }
                else
                {
                    results.Add(s);
                }
            }

            return results;
        }

        #endregion
    }
}