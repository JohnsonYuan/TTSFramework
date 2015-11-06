//----------------------------------------------------------------------------
// <copyright file="QuestionExtension.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines a common library to question values
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Font.Hts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;
    using FE = Microsoft.Tts.ServiceProvider.FeatureExtractor;
    using SP = Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// Question to Question for HTS font compiling.
    /// </summary>
    public static class QuestionExtension
    {
        #region Fields

        /// <summary>
        /// Since almost all integer in Sps is less than 999, the upperbound is set to 999 now.
        /// </summary>
        private const int IntegerUpperBound = 999;

        private const int InvalideCodeValue = -1;

        #endregion

        #region Public operations

        /// <summary>
        /// Converts value set of the given question to code value set.
        /// </summary>
        /// <param name="question">The given question.</param>
        /// <param name="posSet">The corresponding TtsPosSet may needed.</param>
        /// <param name="phoneSet">The corresponding TtsPhoneSet may needed.</param>
        /// <param name="customFeatures">Customized feature set.</param>
        public static void ValueSetToCodeValueSet(this Question question, TtsPosSet posSet, TtsPhoneSet phoneSet,
            HashSet<string> customFeatures)
        {
            Helper.ThrowIfNull(question);
            Helper.ThrowIfNull(posSet);
            Helper.ThrowIfNull(phoneSet);

            HtsLingFeatureType type = GetFeatureType(question.FeatureName, customFeatures);
            switch (type)
            {
                case HtsLingFeatureType.Integer:
                    IntegerValueSetToCodeValueSet(question);
                    break;
                case HtsLingFeatureType.TtsPhone:
                    PhoneIdValueSetToCodeValueSet(question, phoneSet);
                    break;
                case HtsLingFeatureType.TtsPos:
                    PartOfSpeechValueSetToCodeValueSet(question, posSet);
                    break;
                default:
                    throw new NotSupportedException(
                        Helper.NeutralFormat("Linguistic feature type \"{0}\" is not supported", type.ToString()));
            }
        }

        /// <summary>
        /// Converts code value set of the given question to value set.
        /// </summary>
        /// <param name="question">The given question.</param>
        /// <param name="posSet">The corresponding TtsPosSet may needed.</param>
        /// <param name="phoneSet">The corresponding TtsPhoneSet may needed.</param>
        /// <param name="customFeatures">The custom features.</param>
        public static void CodeValueSetToValueSet(this Question question, TtsPosSet posSet,
            TtsPhoneSet phoneSet, HashSet<string> customFeatures)
        {
            Helper.ThrowIfNull(question);
            Helper.ThrowIfNull(posSet);
            Helper.ThrowIfNull(phoneSet);
            Helper.ThrowIfNull(customFeatures);

            HtsLingFeatureType type = GetFeatureType(question.FeatureName, customFeatures);

            string valueSetName;
            switch (type)
            {
                case HtsLingFeatureType.Integer:
                    valueSetName = IntegerCodeValueSetToValueSet(question);
                    break;
                case HtsLingFeatureType.TtsPhone:
                    valueSetName = PhoneIdCodeValueSetToValueSet(question, phoneSet);
                    break;
                case HtsLingFeatureType.TtsPos:
                    valueSetName = PartOfSpeechCodeValueSetToValueSet(question, posSet);
                    break;
                default:
                    throw new NotSupportedException(
                        Helper.NeutralFormat("Linguistic feature type \"{0}\" is not supported", type.ToString()));
            }

            if (string.IsNullOrEmpty(question.ValueSetName))
            {
                question.ValueSetName = valueSetName;
            }
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Convert integer value set of the given question to code value set.
        /// </summary>
        /// <param name="question">The given question.</param>
        private static void IntegerValueSetToCodeValueSet(Question question)
        {
            List<int> codeValueSet = new List<int>();

            // Currently, the integer code value set just need only one value - the value set name.
            if (question.ValueSetName == Label.NotApplicableFeatureValue)
            {
                codeValueSet.Add(InvalideCodeValue);
                Debug.Assert(question.Oper == QuestionOperator.Equal, "Invalid code value only used in equal operation");
            }
            else
            {
                string[] items = question.ValueSetName.Split(Question.ValueDelimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (string item in items)
                {
                    codeValueSet.Add(int.Parse(item, CultureInfo.InvariantCulture));
                }
            }

            // Validates the value set name is correct.
            List<int> valueSet = new List<int>();
            foreach (string s in QuestionBuilder.ExtendQuestionMarkInValueSet(question.ValueSet))
            {
                if (s == Label.NotApplicableFeatureValue)
                {
                    valueSet.Add(InvalideCodeValue);
                    Debug.Assert(question.Oper == QuestionOperator.Equal, "Invalid code value only used in equal operation");
                }
                else
                {
                    valueSet.Add(int.Parse(s, CultureInfo.InvariantCulture));
                }
            }

            valueSet.Sort();

            bool invalid;
            switch (question.Oper)
            {
                case QuestionOperator.Equal:
                    Debug.Assert(valueSet.Count == 1, "Equal question can only have one value.");
                    invalid = !Helper.Compare(valueSet, codeValueSet, true);
                    break;
                case QuestionOperator.Belong:
                    invalid = valueSet.Union(codeValueSet).Count() != codeValueSet.Count;
                    break;
                case QuestionOperator.Greater:
                    invalid = valueSet[0] <= codeValueSet[0];
                    break;
                case QuestionOperator.GreaterEqual:
                    invalid = valueSet[0] < codeValueSet[0];
                    break;
                case QuestionOperator.Less:
                    invalid = valueSet.Last() >= codeValueSet[0];
                    break;
                case QuestionOperator.LessEqual:
                    invalid = valueSet.Last() > codeValueSet[0];
                    break;
                default:
                    throw new NotSupportedException(
                        Helper.NeutralFormat(
                            "Question doesn't support this operator \"{0}\" in integer value in question \"{1}\" ",
                            question.Oper, question.Name));
            }

            if (invalid)
            {
                throw new InvalidDataException(
                    Helper.NeutralFormat(
                        "Question \"{0}\" contains invalid data since value set is mismatched with value set name",
                        question.Name));
            }

            question.CodeValueSet = codeValueSet.AsReadOnly();
        }

        /// <summary>
        /// Convert phone id value set of the given question to code value set.
        /// </summary>
        /// <param name="question">The given question.</param>
        /// <param name="phoneSet">The corresponding TtsPhoneSet.</param>
        private static void PhoneIdValueSetToCodeValueSet(Question question,
            TtsPhoneSet phoneSet)
        {
            List<int> codeValueSet = new List<int>();

            foreach (string phoneName in question.ValueSet)
            {
                if (phoneName == Label.NotApplicableFeatureValue)
                {
                    codeValueSet.Add(InvalideCodeValue);
                }
                else
                {
                    Phone phone = phoneSet.ToPhone(phoneName);
                    if (phone == null)
                    {
                        Console.WriteLine("Invalid phone \"{0}\" found in question \"{1}\". Ignored.", phoneName,
                            question.Name);
                    }
                    else
                    {
                        codeValueSet.Add(phone.Id);
                    }
                }
            }

            question.CodeValueSet = codeValueSet.AsReadOnly();
        }

        /// <summary>
        /// Convert part of speech value set of the given question to code value set.
        /// </summary>
        /// <param name="question">The given question.</param>
        /// <param name="posSet">The corresponding TtsPosSet.</param>
        private static void PartOfSpeechValueSetToCodeValueSet(Question question, TtsPosSet posSet)
        {
            List<int> codeValueSet = new List<int>();

            foreach (string posId in question.ValueSet)
            {
                if (posId == Label.NotApplicableFeatureValue || posId == "0")
                {
                    codeValueSet.Add(InvalideCodeValue);
                }
                else
                {
                    if (!posSet.Items.ContainsKey(question.ValueSetName))
                    {
                        throw new InvalidDataException(Helper.NeutralFormat(
                            "Invalid POS id [{0}] is found in question [{1}]", posId, question.Name));
                    }

                    int posIdValue = int.Parse(posId, CultureInfo.InvariantCulture);
                    if (posSet.Items[question.ValueSetName] != (uint)posIdValue)
                    {
                        Console.WriteLine(
                            "Question \"{0}\" contains invalid POS since value set is mismatched with value set name, the value set will be used.",
                            question.Name);
                    }

                    codeValueSet.Add(posIdValue);
                }
            }

            question.CodeValueSet = codeValueSet.AsReadOnly();
        }

        /// <summary>
        /// Convert integer code value set of the given question to value set.
        /// </summary>
        /// <param name="question">The given question.</param>
        /// <returns>Question set name.</returns>
        private static string IntegerCodeValueSetToValueSet(Question question)
        {
            List<string> valueSet = new List<string>();

            foreach (int value in question.CodeValueSet)
            {
                if (value == InvalideCodeValue)
                {
                    valueSet.Add(Label.NotApplicableFeatureValue);
                    Debug.Assert(question.Oper == QuestionOperator.Equal, "Invalid code value only used in equal operation");
                }
                else
                {
                    switch (question.Oper)
                    {
                        case QuestionOperator.Equal:
                            Debug.Assert(question.CodeValueSet.Count == 1, "Equal question can only have one value.");
                            valueSet.Add(value.ToString(CultureInfo.InvariantCulture));
                            break;
                        case QuestionOperator.Belong:
                            valueSet.Add(value.ToString(CultureInfo.InvariantCulture));
                            break;
                        case QuestionOperator.Less:
                            Debug.Assert(question.CodeValueSet.Count == 1, "Less question can only have one value.");
                            for (int i = 0; i < value; ++i)
                            {
                                valueSet.Add(i.ToString(CultureInfo.InvariantCulture));
                            }

                            break;
                        case QuestionOperator.LessEqual:
                            Debug.Assert(question.CodeValueSet.Count == 1, "LessEqual question can only have one value.");
                            for (int i = 0; i <= value; ++i)
                            {
                                valueSet.Add(i.ToString(CultureInfo.InvariantCulture));
                            }

                            break;
                        case QuestionOperator.Greater:
                            Debug.Assert(question.CodeValueSet.Count == 1, "Great question can only have one value.");
                            for (int i = value + 1; i <= IntegerUpperBound; ++i)
                            {
                                valueSet.Add(i.ToString(CultureInfo.InvariantCulture));
                            }

                            break;
                        case QuestionOperator.GreaterEqual:
                            Debug.Assert(question.CodeValueSet.Count == 1, "Great question can only have one value.");
                            for (int i = value; i <= IntegerUpperBound; ++i)
                            {
                                valueSet.Add(i.ToString(CultureInfo.InvariantCulture));
                            }

                            break;
                        default:
                            throw new NotSupportedException(
                                Helper.NeutralFormat(
                                    "Question doesn't support this operator \"{0}\" in integer value in question \"{1}\" ",
                                    question.Oper, question.Name));
                    }
                }
            }

            question.ValueSet = valueSet.AsReadOnly();

            string valueSetName;
            if (QuestionOperator.Belong == question.Oper)
            {
                valueSetName = string.Join("_", valueSet.ToArray());
            }
            else if (QuestionOperator.Equal == question.Oper && question.CodeValueSet[0] == InvalideCodeValue)
            {
                valueSetName = Label.NotApplicableFeatureValue;
            }
            else
            {
                valueSetName = question.CodeValueSet[0].ToString(CultureInfo.InvariantCulture);
            }

            return valueSetName;
        }

        /// <summary>
        /// Convert phone id code value set of the given question to value set.
        /// </summary>
        /// <param name="question">The given question.</param>
        /// <param name="phoneSet">The corresponding TtsPhoneSet.</param>
        /// <returns>Question set name.</returns>
        private static string PhoneIdCodeValueSetToValueSet(Question question, TtsPhoneSet phoneSet)
        {
            List<string> valueSet = new List<string>();

            string valueSetName = null;
            foreach (int phoneId in question.CodeValueSet)
            {
                if (phoneId == InvalideCodeValue)
                {
                    valueSet.Add(Label.NotApplicableFeatureValue);
                    valueSetName = Label.NotApplicableFeatureValue;
                }
                else
                {
                    Phone phone = phoneSet.GetPhone(phoneId);
                    if (phone == null)
                    {
                        Console.WriteLine("Invalid phone id \"{0}\" found in question \"{1}\". Ignored.", phoneId,
                            question.Name);
                    }
                    else
                    {
                        valueSet.Add(phone.Name);
                    }
                }
            }

            question.ValueSet = valueSet.AsReadOnly();
            if (string.IsNullOrEmpty(valueSetName))
            {
                valueSetName = string.Join("_", valueSet.ToArray());
            }

            return valueSetName;
        }

        /// <summary>
        /// Convert part of speech code value set of the given question to value set.
        /// </summary>
        /// <param name="question">The given question.</param>
        /// <param name="posSet">The corresponding TtsPosSet.</param>
        /// <returns>Question set name.</returns>
        private static string PartOfSpeechCodeValueSetToValueSet(Question question, TtsPosSet posSet)
        {
            List<string> valueSet = new List<string>();

            string valueSetName = null;
            foreach (int posId in question.CodeValueSet)
            {
                if (posId == InvalideCodeValue)
                {
                    valueSet.Add(Label.NotApplicableFeatureValue);
                    valueSetName = Label.NotApplicableFeatureValue;
                }
                else
                {
                    if (!posSet.IdItems.ContainsKey((uint)posId))
                    {
                        Console.WriteLine(Helper.NeutralFormat(
                            "Invalid POS id [{0}] is found in question [{1}].", posId, question.Name));
                    }
                    else
                    {
                        valueSet.Add(posId.ToString(CultureInfo.InvariantCulture));
                        valueSetName = posSet.IdItems[(uint)posId];
                    }
                }
            }

            question.ValueSet = valueSet.AsReadOnly();
            return valueSetName;
        }

        /// <summary>
        /// Gets feature type through feature name from runtime, currently it is fake to support prototype model.
        /// </summary>
        /// <param name="name">Feature name.</param>
        /// <param name="customFeatures">Customized feature set.</param>
        /// <returns>Feature type.</returns>
        private static HtsLingFeatureType GetFeatureType(string name, HashSet<string> customFeatures)
        {
            Helper.ThrowIfNull(name);
            Helper.ThrowIfNull(customFeatures);

            HtsLingFeatureType type;
            try
            {
                using (FE.FeatureExtractionEngine featureExtractor = new FE.FeatureExtractionEngine())
                {
                    using (FE.FeatureMeta featureMeta = featureExtractor.Convert(name, customFeatures.Contains(name)))
                    {
                        if (featureMeta.Property == SP.TtsFeatureProperty.TTS_FEATURE_PROPERTY_PHONE_ID)
                        {
                            type = HtsLingFeatureType.TtsPhone;
                        }
                        else if (featureMeta.Property == SP.TtsFeatureProperty.TTS_FEATURE_PROPERTY_POS)
                        {
                            type = HtsLingFeatureType.TtsPos;
                        }
                        else
                        {
                            type = HtsLingFeatureType.Integer;
                        }
                    }
                }
            }
            catch (Exception)
            {
                type = HtsLingFeatureType.Integer;
            }

            return type;
        }

        #endregion
    }
}