//----------------------------------------------------------------------------
// <copyright file="DictionaryModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     Dictionary Model. Interface of other dictionaries. Have some common method.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Dictionary
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Online dictionary interface .
    /// </summary>
    public abstract class DictionaryModel : IDisposable
    {
        #region Fields

        /// <summary>
        /// Define a space.
        /// </summary>
        protected static string space = " ";

        /// <summary>
        /// Embedded config file path.
        /// </summary>
        protected string resourceName = string.Empty;

        /// <summary>
        /// Url of online dictionary.
        /// </summary>
        protected string url = string.Empty;

        /// <summary>
        /// Regex pattern that is used to match raw pronunciatons.
        /// </summary>
        protected string rawPronPattern = string.Empty;

        /// <summary>
        /// No result pattern to detect if result is found.
        /// </summary>
        protected string noResultPattern = string.Empty;

        /// <summary>
        /// POS pattern.
        /// </summary>
        protected string posPattern = string.Empty;

        /// <summary>
        /// Word pattern.
        /// </summary>
        protected string wordPattern = string.Empty;

        /// <summary>
        /// Word delimiter.
        /// </summary>
        protected string wordDelimiter = string.Empty;

        /// <summary>
        /// Mapping between online phone and tts phone.
        /// </summary>
        protected Dictionary<string, string> phoneMappping = new Dictionary<string, string>();

        /// <summary>
        /// Mapping between online POS and tts POS.
        /// </summary>
        protected Dictionary<string, string> posMapping = new Dictionary<string, string>();

        /// <summary>
        /// Whole page .
        /// </summary>
        protected string src = string.Empty;

        /// <summary>
        /// WebClient to download the page.
        /// </summary>
        protected WebClient webClient;

        /// <summary>
        /// Vowels collection.
        /// </summary>
        protected Collection<string> vowels = new Collection<string>();

        /// <summary>
        /// Syllable phone.
        /// </summary>
        protected char syllable;

        /// <summary>
        /// Primary stress.
        /// </summary>
        protected char primaryStress;

        /// <summary>
        /// Secondary stress.
        /// </summary>
        protected char secondaryStress;

        /// <summary>
        /// Pronunciations.
        /// </summary>
        private Collection<string> _pronunciations = new Collection<string>();

        /// <summary>
        /// POS field.
        /// </summary>
        private string _pos = string.Empty;

        /// <summary>
        /// Word field .
        /// </summary>
        private string _word = string.Empty;

        /// <summary>
        /// Default POS .
        /// </summary>
        private string _defaultPOS = string.Empty;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the DictionaryModel class .
        /// </summary>
        public DictionaryModel()
        {
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="DictionaryModel" /> class.
        /// </summary>
        ~DictionaryModel()
        {
            this.Dispose(false);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Pronunciatons .
        /// </summary>
        public Collection<string> Pronunciations
        {
            get
            {
                return _pronunciations;
            }
        }

        /// <summary>
        /// Gets part of speech .
        /// </summary>
        public string POS
        {
            get
            {
                return _pos;
            }
        }

        /// <summary>
        /// Gets word .
        /// </summary>
        public string Word
        {
            get
            {
                return _word;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the word is OOV or not.
        /// </summary>
        public bool IsOOV { get; protected set; }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes the resources used in this object.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Public Method

        /// <summary>
        /// Lookup word pronunciation from online .
        /// </summary>
        /// <param name="word">Word paramter .</param>
        /// <returns>Look-up pronunciations .</returns>
        public abstract Collection<string> Lookup(string word);

        /// <summary>
        /// Reset method .
        /// </summary>
        public void Reset()
        {
            _pronunciations.Clear();
            _word = string.Empty;
            _pos = string.Empty;
            src = string.Empty;
            IsOOV = false;
        }

        #endregion

        #region Protected Method

        /// <summary>
        /// Releases the unmanaged resources used by the RewindableTextReader.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources;
        /// False to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.webClient != null)
                {
                    this.webClient.Dispose();
                }
            }
        }

        /// <summary>
        /// Load embedded configuration file.
        /// </summary>
        /// <param name="resourceName">Resource name.</param>
        protected void LoadResource(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream resourceStream = assembly.GetManifestResourceStream(resourceName);
            LoadData(resourceStream);
        }

        /// <summary>
        /// Process method .
        /// </summary>
        protected void Process()
        {
            string rawPron = GetRawPron();
            _word = GetWord();
            _pos = GetPOS();
            if (!string.IsNullOrEmpty(rawPron))
            {
                PostProcess(rawPron);
            }
        }

        /// <summary>
        /// Convert online pron to TTS pron .
        /// </summary>
        /// <param name="sourcePron">Online pron .</param>
        /// <param name="phoneMapping">Phonemapping data .</param>
        /// <returns>Tts pron .</returns>
        protected string ConvertToTTSPron(string sourcePron, Dictionary<string, string> phoneMapping)
        {
            string[] phones = sourcePron.Split(' ');
            string ttsPhone = string.Empty;
            foreach (string phone in phones)
            {
                if (!string.IsNullOrEmpty(phone))
                {
                    ttsPhone += phoneMapping[phone] + " ";
                }
            }

            return ttsPhone.Trim();
        }

        /// <summary>
        /// Process some partial online like /trænsˈleɪt, trænz-, ˈtrænsleɪt, ˈtrænz-/ .
        /// </summary>
        /// <param name="sourcePron">Partial pron.</param>
        /// <param name="refFullPron">Full pron reference.</param>
        /// <returns>Processed pron.</returns>
        protected string HandlePartialPron(string sourcePron, string refFullPron)
        {
            string targetPron = string.Empty;
            int syllableCount = Regex.Matches(sourcePron, syllable.ToString()).Count;
            int refSyllableCount = Regex.Matches(refFullPron, syllable.ToString()).Count;
            if (syllableCount >= refSyllableCount && (sourcePron[0].Equals(syllable) || sourcePron[sourcePron.Length - 1].Equals(syllable)))
            {
                return string.Empty;
            }

            int index = 0;
            if (sourcePron[0].Equals(syllable))
            {
                for (int i = refFullPron.Length - 1; i >= 0; i--)
                {
                    if (refFullPron[i].Equals(syllable))
                    {
                        syllableCount--;
                        if (syllableCount == 0)
                        {
                            index = i;
                            break;
                        }
                    }
                }

                targetPron = refFullPron.Substring(0, index) + sourcePron;
            }
            else if (sourcePron[sourcePron.Length - 1].Equals(syllable) || sourcePron[sourcePron.Length - 1].Equals(primaryStress) || sourcePron[sourcePron.Length - 1].Equals(secondaryStress))
            {
                for (int j = 0; j < refFullPron.Length; j++)
                {
                    if (refFullPron[j].Equals(syllable))
                    {
                        syllableCount--;
                        if (syllableCount == 0)
                        {
                            index = j + 1;
                            break;
                        }
                    }
                }

                targetPron = sourcePron + refFullPron.Substring(index);
            }
            else
            {
                targetPron = sourcePron;
            }

            return targetPron;
        }

        /// <summary>
        /// Split pron into phone array .
        /// </summary>
        /// <param name="pron">Pronunciaton .</param>
        /// <returns>Phone array .</returns>
        protected ArrayList SplitPhones(string pron)
        {
            ArrayList phoneList = new ArrayList();
            int startIndex = 0;
            bool flag = false;
            string stressPhone = string.Empty;
            while (startIndex < pron.Length)
            {
                string phone = FindPhone(pron, ref startIndex);

                // It is reasonable that phone is equal to " ", because, in fact, one more space is acceptable. 
                // But, we must ignore it before converting into TTS phone
                if (phone.Equals(space))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(phone))
                {
                    throw new InvalidDataException(pron + " contain invalid phone");
                }

                if (IsStress(phone))
                {
                    flag = true;
                    stressPhone = phone;
                }
                else
                {
                    phoneList.Add(phone);
                    if (IsVowel(phone) && flag)
                    {
                        phoneList.Add(stressPhone);
                        flag = false;
                    }
                }
            }

            return phoneList;
        }

        /// <summary>
        /// Find longest phone from pronunciation .
        /// </summary>
        /// <param name="pron">Pron parameter .</param>
        /// <param name="startIndex">Start index .</param>
        /// <returns>Return phone .</returns>
        protected string FindPhone(string pron, ref int startIndex)
        {
            bool flag = false;

            int endIndex = startIndex + 1;

            if (pron[startIndex].Equals(syllable) || pron[startIndex].Equals(secondaryStress) || pron[startIndex].Equals(primaryStress))
            {
                return pron[startIndex++].ToString();
            }
            else
            {
                int length = 3;
                int tempIndex = 0;
                while (endIndex - startIndex <= length && endIndex <= pron.Length)
                {
                    if (IsValidPhone(pron.Substring(startIndex, endIndex - startIndex)))
                    {
                        tempIndex = endIndex;
                        flag = true;
                    }

                    endIndex++;
                }

                if (flag)
                {
                    endIndex = tempIndex;
                }
                else
                {
                    // if flag == false, it represents that all three consecutive characters are not vailable phone, such as: " ei"
                    endIndex = startIndex + 1;
                }

                string phone = pron.Substring(startIndex, endIndex - startIndex);
                startIndex = endIndex;
                return phone;
            }
        }

        /// <summary>
        /// Is it a stress phone .
        /// </summary>
        /// <param name="phone">Phone parameter .</param>
        /// <returns>True/false .</returns>
        protected bool IsStress(string phone)
        {
            return phone.Equals(secondaryStress.ToString()) || phone.Equals(primaryStress.ToString());
        }

        /// <summary>
        /// Is it a valid phone .
        /// </summary>
        /// <param name="phone">Phone parameter .</param>
        /// <returns>True/false .</returns>
        protected bool IsValidPhone(string phone)
        {
            foreach (string key in phoneMappping.Keys)
            {
                if (key.Equals(phone, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Is it a vowel phone .
        /// </summary>
        /// <param name="phone">Phone parameter .</param>
        /// <returns>True/false .</returns>
        protected bool IsVowel(string phone)
        {
            foreach (string vowel in vowels)
            {
                if (vowel.Equals(phone, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Identify whether result is found .
        /// </summary>
        /// <returns>True/false .</returns>
        protected bool IsFind()
        {
            Regex regex = new Regex(noResultPattern, RegexOptions.IgnoreCase);
            return !regex.IsMatch(src);
        }

        /// <summary>
        /// Ensure stress is after vowel.
        /// </summary>
        /// <param name="pron">Source pron.</param>
        /// <returns>Target pron.</returns>
        protected string EnsureStress(string pron)
        {
            Match m = Regex.Match(pron, "\\sr(\\s+[12])");
            if (m.Success)
            {
                string stress = m.Groups[1].Value;
                pron = pron.Replace(" r" + stress, stress + " r");
            }

            m = Regex.Match(pron, "\\sr(\\s+[12])");
            if (m.Success)
            {
                string stress = m.Groups[1].Value;
                pron = pron.Replace(" n" + stress, stress + " n");
            }

            return pron;
        }

        /// <summary>
        /// If online dictionary shows result found but pronunciations can't be extracted, it will throw a exception.
        /// </summary>
        protected void ThrowExceptionIfPronsNull()
        {
            if (Pronunciations.Count == 0)
            {
                string message = Helper.NeutralFormat("Can't extract pronunciation for \"{0}\"", Word);
                throw new InvalidDataException(message);
            }
        }

        /// <summary>
        /// Process the raw pronunciation .
        /// </summary>
        /// <param name="rawPron">Raw pron .</param>
        protected abstract void PostProcess(string rawPron);

        #endregion

        #region Private Method

        /// <summary>
        /// Gets raw pron .
        /// </summary>
        /// <returns>Raw pron .</returns>
        private string GetRawPron()
        {
            string rawPron = string.Empty;
            Match m = Regex.Match(src, rawPronPattern, RegexOptions.IgnoreCase);
            if (m.Success)
            {
                rawPron = m.Groups["pron"].Value;
            }

            return rawPron;
        }

        /// <summary>
        /// Get word from page .
        /// </summary>
        /// <returns>Return word .</returns>
        private string GetWord()
        {       
            string word = string.Empty;
            Match m = Regex.Match(src, wordPattern, RegexOptions.IgnoreCase);
            if (m.Success)
            {
                word = m.Groups["word"].Value.Replace(wordDelimiter, string.Empty).Replace("–", "-").ToLower();
                if (word.Contains(","))
                {
                    string[] words = word.Split(',');
                    word = words[0];
                }
            }

            return word;
        }

        /// <summary>
        /// Get POS from page .
        /// </summary>
        /// <returns>Return POS .</returns>
        private string GetPOS()
        {
            string pos = string.Empty;
            Match m = Regex.Match(src, posPattern, RegexOptions.IgnoreCase);
            if (m.Success)
            {
                pos = m.Groups["pos"].Value;
            }

            if (posMapping.Keys.Contains(pos))
            {
                return posMapping[pos];
            }
            else
            {
                return _defaultPOS;
            }
        }

        /// <summary>
        /// Read configuration file.
        /// </summary>
        /// <param name="stream">File stream.</param>
        private void LoadData(Stream stream)
        {
            Helper.ThrowIfNull(stream);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(stream);
            XmlElement root = xmlDoc.DocumentElement;

            XmlNode singleNode;
            singleNode = root.SelectSingleNode("/OnlineDictionary/URL");
            url = singleNode.Attributes["v"].Value;

            singleNode = root.SelectSingleNode("/OnlineDictionary/RawPron");
            rawPronPattern = singleNode.Attributes["Pattern"].Value;

            singleNode = root.SelectSingleNode("/OnlineDictionary/NoResult");
            noResultPattern = singleNode.Attributes["Pattern"].Value;

            singleNode = root.SelectSingleNode("/OnlineDictionary/POS");
            posPattern = singleNode.Attributes["Pattern"].Value;
            _defaultPOS = singleNode.Attributes["v"].Value;

            singleNode = root.SelectSingleNode("/OnlineDictionary/Word");
            wordPattern = singleNode.Attributes["Pattern"].Value;

            XmlNodeList nodeList = root.SelectNodes("/OnlineDictionary/PhoneMapping/item");
            foreach (XmlNode node in nodeList)
            {
                string source = node.Attributes["from"].Value;
                string target = node.Attributes["to"].Value;
                string isVowel = node.Attributes["IsVowel"].Value;
                phoneMappping.Add(source, target);
                if (isVowel.Equals("true"))
                {
                    vowels.Add(source);
                }
            }

            nodeList = root.SelectNodes("/OnlineDictionary/POS/item");
            foreach (XmlNode node in nodeList)
            {
                string source = node.Attributes["from"].Value;
                string target = node.Attributes["to"].Value;
                posMapping.Add(source, target);
            }
        }

        #endregion
    }
}