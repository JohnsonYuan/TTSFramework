//----------------------------------------------------------------------------
// <copyright file="PhoneQuestionSet.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements phone question set definition
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Delegate to map phone.
    /// </summary>
    /// <param name="phone">Phone to be mapped.</param>
    /// <returns>Mapped phone.</returns>
    public delegate string[] MapPhone(string phone);

    /// <summary>
    /// Phone question set error.
    /// </summary>
    public enum PhoneQuestionErrorType
    {
        /// <summary>
        /// Unrecognized phone.
        /// </summary>
        [ErrorAttribute(Message = "Unrecognized phone [{0}] in question [{1}]")]
        UnrecognizedPhone
    }

    /// <summary>
    /// Phone question converter.
    /// </summary>
    public static class PhoneQuestionConverter
    {
        /// <summary>
        /// Convert phoneset to the mapped phoneset, using delegate mapPhone, if the parameter
        /// Delimiters is not null, mapped phones string will be splitted to several phones
        /// And added to the new phone set.
        /// </summary>
        /// <param name="phoneQuestion">Phone set to be converted.</param>
        /// <param name="mapPhone">Map old phone to new phone delegate.</param>
        /// <returns>Mapped phoneset.</returns>
        public static PhoneQuestion Convert(PhoneQuestion phoneQuestion, MapPhone mapPhone)
        {
            PhoneQuestion question = new PhoneQuestion(phoneQuestion.Name);

            foreach (string phone in phoneQuestion.Phones)
            {
                string[] mappedPhones = mapPhone(phone);

                if (mappedPhones == null || mappedPhones.Length <= 0 ||
                    string.IsNullOrEmpty(mappedPhones[0]))
                {
                    string message = Helper.NeutralFormat("Can't find phone [{0}]'s " +
                        "mapped phone in phoneset [{1}]", phone, phoneQuestion.Name);
                    continue;
                }

                foreach (string mappedPhone in mappedPhones)
                {
                    question.Phones.Add(mappedPhone);
                }
            }

            return question;
        }

        /// <summary>
        /// Map several phoneset to a new phoneset using the mapPhone delegate.
        /// </summary>
        /// <param name="phoneQuestions">Phone questions to be mapped.</param>
        /// <param name="mapPhone">MapPhone delegate map original phone to new phone.</param>
        /// <returns>Mapped phonesets.</returns>
        public static PhoneQuestion[] Convert(IEnumerable<PhoneQuestion> phoneQuestions, MapPhone mapPhone)
        {
            List<PhoneQuestion> questions = new List<PhoneQuestion>();

            foreach (PhoneQuestion set in phoneQuestions)
            {
                questions.Add(Convert(set, mapPhone));
            }

            return RemoveDuplicate(questions);
        }

        /// <summary>
        /// Get missing phone set: each phone should contain one question for it self.
        /// </summary>
        /// <param name="phoneQuestions">Phone questions to be checked.</param>
        /// <param name="phones">Phone set to be checked.</param>
        /// <returns>Missed phoneset.</returns>
        public static PhoneQuestion[] GetMissingPhoneSet(IEnumerable<PhoneQuestion> phoneQuestions, Collection<string> phones)
        {
            const string MissingQuestionNameSuffix = "_AutoGen_";
            Dictionary<string, bool> phoneExistDictionary = new Dictionary<string, bool>();

            foreach (string phone in phones)
            {
                phoneExistDictionary.Add(phone, false);
            }

            foreach (PhoneQuestion set in phoneQuestions)
            {
                // Check the questions with only one phone
                if (set.Phones.Count != 1)
                {
                    continue;
                }

                if (phoneExistDictionary.ContainsKey(set.Phones[0]))
                {
                    phoneExistDictionary[set.Phones[0]] = true;
                }                
            }

            List<PhoneQuestion> missedPhonesets = new List<PhoneQuestion>();

            foreach (string phone in phoneExistDictionary.Keys)
            {
                if (!phoneExistDictionary[phone])
                {
                    PhoneQuestion phoneset = new PhoneQuestion(MissingQuestionNameSuffix + phone);
                    phoneset.Phones.Add(phone); 
                    missedPhonesets.Add(phoneset);
                }
            }

            return missedPhonesets.ToArray();
        }

        /// <summary>
        /// Remove duplicate naming phoneset.
        /// </summary>
        /// <param name="sets">PhoneSet list to remove duplicate items.</param>
        /// <returns>Phonesets which are removed duplicated questions.</returns>
        private static PhoneQuestion[] RemoveDuplicate(List<PhoneQuestion> sets)
        {
            Dictionary<string, PhoneQuestion> uniqueNameSets = new Dictionary<string, PhoneQuestion>();

            foreach (PhoneQuestion set in sets)
            {
                if (set.Phones.Count > 0 && !uniqueNameSets.ContainsKey(set.Name))
                {
                    uniqueNameSets.Add(set.Name, set);
                }
            }

            Dictionary<string, PhoneQuestion> uniquePhoneListSet = new Dictionary<string, PhoneQuestion>();

            foreach (PhoneQuestion set in uniqueNameSets.Values)
            {
                if (set.Phones.Count > 0 && !uniquePhoneListSet.ContainsKey(set.PhoneListString()))
                {
                    uniquePhoneListSet.Add(set.PhoneListString(), set);
                }
            }

            return (new List<PhoneQuestion>(uniquePhoneListSet.Values)).ToArray();
        }
    }

    /// <summary>
    /// Phone question class.
    /// </summary>
    public class PhoneQuestion
    {
        private string _name;
        private List<string> _phones;

        /// <summary>
        /// Initializes a new instance of the <see cref="PhoneQuestion"/> class.
        /// </summary>
        /// <param name="name">Name of the phoneset.</param>
        public PhoneQuestion(string name)
        {
            Name = name;
            _phones = new List<string>();
        }

        /// <summary>
        /// Gets or sets Name of the phone set.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets Phone list of the phoneset.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "Ignore.")]
        public List<string> Phones
        {
            get { return _phones; }
        }

        /// <summary>
        /// Whether the phoneset contains the phone.
        /// </summary>
        /// <param name="phone">Phone to be checked contained in the phone set..</param>
        /// <returns>Whether the phone contained in the phoneset.</returns>
        public bool Contains(string phone)
        {
            return _phones.IndexOf(phone) >= 0;
        }

        /// <summary>
        /// Get phone list string.
        /// </summary>
        /// <returns>Phone list string.</returns>
        public string PhoneListString()
        {
            StringBuilder list = new StringBuilder();

            Phones.Sort();

            foreach (string phone in Phones)
            {
                if (list.Length != 0)
                {
                    list.Append(" ");
                }

                list.Append(phone);
            }

            return list.ToString();
        }

        /// <summary>
        /// Convert phoneset to string.
        /// </summary>
        /// <returns>Phoneset string.</returns>
        public override string ToString()
        {
            return ToString(@"QS {0} {{1}}", @"{0}", ",");
        }

        /// <summary>
        /// Convert phoneset to string with the specified format.
        /// </summary>
        /// <param name="questionFormat">Question format.</param>
        /// <param name="itemFormat">Question item format.</param>
        /// <param name="delimiter">Question item delimiter.</param>
        /// <returns>Question string.</returns>
        public string ToString(string questionFormat, string itemFormat, string delimiter)
        {
            if (Phones == null || Phones.Count <= 0)
            {
                return string.Empty;
            }

            StringBuilder list = new StringBuilder();
            foreach (string phone in Phones)
            {
                if (list.Length != 0)
                {
                    list.Append(delimiter);
                }

                list.AppendFormat(itemFormat, phone);
            }

            return Helper.NeutralFormat(questionFormat, Name, list.ToString());
        }

        /// <summary>
        /// Check if the phone in phone question is in tts phone set.
        /// </summary>
        /// <param name="phoneSet">TTS Phone set.</param>
        /// <returns>Errors.</returns>
        public ErrorSet Validate(TtsPhoneSet phoneSet)
        {
            ErrorSet errors = new ErrorSet();
            foreach (string phone in _phones)
            {
                if (phoneSet.GetPhone(phone) == null)
                {
                    errors.Add(PhoneQuestionErrorType.UnrecognizedPhone, phone, _name);
                }
            }

            return errors;
        }
    }

    /// <summary>
    /// Question file class.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1053:StaticHolderTypesShouldNotHaveConstructors", Justification = "Ignore.")]
    public class QuestionFile
    {
        #region Fields

        /// <summary>
        /// The default Regex string to load phone question.
        /// </summary>
        private const string DefaultQuestionRegex = @"^\s*QS\s+'L_(.*)'\s+\{(.*)\}\s*$";

        /// <summary>
        /// The default Regex string to load phone question item.
        /// </summary>
        private const string DefaultQuestionItemRegex = @"""(.*)-\*""";

        /// <summary>
        /// The default question item delimiters.
        /// </summary>
        private static char[] _defaultQuestionItemDelimiters = new char[] { ',', ' ' };

        #endregion

        /// <summary>
        /// Load PhoenSet files with the default question and question item regex string.
        /// </summary>
        /// <param name="filePath">Question file to be loaded.</param>
        /// <returns>Loaded phoneset array.</returns>
        public static PhoneQuestion[] Load(string filePath)
        {
            return Load(filePath, DefaultQuestionRegex, DefaultQuestionItemRegex, _defaultQuestionItemDelimiters);
        }

        /// <summary>
        /// Load PhoenSet files with the specified question and question item regex string.
        /// </summary>
        /// <param name="filePath">Question file to be loaded.</param>
        /// <param name="questionPattern">Question regex pattern, the first group of the pattern should be question name,
        /// and the second group should be QuestionItems.</param>
        /// <param name="itemPattern">QuestionItem regex pattern, the first group of the pattern should be phone.</param>
        /// <param name="delimiters">Delimiters of the question items.</param>
        /// <returns>Loaded phoneset array.</returns>
        public static PhoneQuestion[] Load(string filePath, string questionPattern, string itemPattern, char[] delimiters)
        {
            List<PhoneQuestion> sets = new List<PhoneQuestion>();

            foreach (string rawLine in Helper.FileLines(filePath))
            {
                string line = rawLine.Trim();

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (Regex.Match(line, questionPattern).Success)
                {
                    PhoneQuestion phoneSet = LoadOneQuestion(line, questionPattern, itemPattern, delimiters);
                    if (phoneSet != null)
                    {
                        sets.Add(phoneSet);
                    }
                }
            }

            return sets.ToArray();
        }

        /// <summary>
        /// Load one question set from the specified string.
        /// </summary>
        /// <param name="questionString">One line string to be parsed to one phoneset.</param>
        /// <param name="questionPattern">Question regex pattern, the first group of the pattern should be question name,
        /// and the second group should be QuestionItems.</param>
        /// <param name="itemPattern">QuestionItem regex pattern, the first group of the pattern should be phone.</param>
        /// <param name="delimiters">Delimiters of the question items.</param>
        /// <returns>Loaded phoneset.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#", Justification = "Ignore.")]
        public static PhoneQuestion LoadOneQuestion(string questionString, string questionPattern, string itemPattern, char[] delimiters)
        {
            if (string.IsNullOrEmpty(questionString))
            {
                return null;
            }

            PhoneQuestion question = null;

            Match matchQuestion = Regex.Match(questionString.Trim(), questionPattern);
            if (matchQuestion.Success)
            {
                if (matchQuestion.Groups.Count < 3)
                {
                    throw new InvalidDataException(Helper.NeutralFormat("Invalid question regex [{0}].", questionPattern));
                }

                if (!string.IsNullOrEmpty(matchQuestion.Groups[1].Value) &&
                    !string.IsNullOrEmpty(matchQuestion.Groups[2].Value))
                {
                    question = BuildPhoneQuestion(matchQuestion.Groups[1].Value,
                        matchQuestion.Groups[2].Value,
                        itemPattern, delimiters);
                }
            }

            return question;
        }

        /// <summary>
        /// Build phoneset with the specified question item string.
        /// </summary>
        /// <param name="name">Phoneset name.</param>
        /// <param name="questions">String contained question items, the items should be separated by delimiters.</param>
        /// <param name="itemPattern">QuestionItem regex pattern, the first group of the pattern should be phone.</param>
        /// <param name="delimiters">Question item delimiters.</param>
        /// <returns>Parsed phone set.</returns>
        private static PhoneQuestion BuildPhoneQuestion(string name, string questions, string itemPattern, char[] delimiters)
        {
            PhoneQuestion question = new PhoneQuestion(name);

            string[] items = questions.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < items.Length; i++)
            {
                string item = items[i].Trim();
                Match matchItem = Regex.Match(item, itemPattern);

                if (!matchItem.Success)
                {
                    throw new InvalidDataException(Helper.NeutralFormat("Invalid question item [{0}] in question [{1}].", item, question.Name));
                }

                if (matchItem.Groups.Count < 2)
                {
                    throw new InvalidDataException(Helper.NeutralFormat("Invalid question item regex [{0}].", itemPattern.ToString()));
                }

                string phone = matchItem.Groups[1].Value;
                if (!question.Contains(phone))
                {
                    question.Phones.Add(phone);
                }
            }

            return question;
        }
    }
}