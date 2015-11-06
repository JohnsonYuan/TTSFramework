//----------------------------------------------------------------------------
// <copyright file="ScriptSyllable.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script syllable
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Script syllable.
    /// </summary>
    public class ScriptSyllable : ScriptAcousticsHolder
    {
        #region Fields

        // the following ones will be discarded when scripts are converted to XML
        private TtsBreak _ttsBreak = TtsBreak.Syllable;
        private TtsEmphasis _ttsEmphasis;
        private object _tag;
        private string _text;

        private Language _language;
        private TtsStress _stress = TtsStress.None;
        private Collection<ScriptPhone> _phones = new Collection<ScriptPhone>();
        private ScriptWord _word;

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptSyllable"/> class.
        /// </summary>
        public ScriptSyllable()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptSyllable"/> class.
        /// </summary>
        /// <param name="language">Language name.</param>
        public ScriptSyllable(Language language)
        {
            _language = language;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets TtsEmphasis.
        /// </summary>
        public TtsEmphasis TtsEmphasis
        {
            get { return _ttsEmphasis; }
            set { _ttsEmphasis = value; }
        }

        /// <summary>
        /// Gets or sets TtsBreak.
        /// </summary>
        public TtsBreak TtsBreak
        {
            get { return _ttsBreak; }
            set { _ttsBreak = value; }
        }

        /// <summary>
        /// Gets or sets Tag.
        /// </summary>
        public object Tag
        {
            get
            {
                return _tag;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _tag = value;
            }
        }

        /// <summary>
        /// Gets or sets Text
        /// Leave this property for two-line script compatible.
        /// </summary>
        public string Text
        {
            get
            {
                return _text;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _text = value;
            }
        }

        /// <summary>
        /// Gets or sets Language.
        /// </summary>
        public Language Language
        {
            get
            {
                return _language;
            }

            set
            {
                _language = value;
            }
        }

        /// <summary>
        /// Gets or sets TtsStress.
        /// </summary>
        public TtsStress Stress
        {
            get { return _stress; }
            set { _stress = value; }
        }

        /// <summary>
        /// Gets or sets TobiPitchAccent.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Tobi", Justification = "tobi is word")]
        public TobiLabel TobiPitchAccent { get; set; }

        /// <summary>
        /// Gets or sets ScriptWord.
        /// </summary>
        public ScriptWord Word
        {
            get { return _word; }
            set { _word = value; }
        }

        /// <summary>
        /// Gets The phones this syllable has.
        /// </summary>
        public Collection<ScriptPhone> Phones
        {
            get 
            {
                return _phones;
            }
        }

        #endregion

        #region public static methods

        /// <summary>
        /// Parsing the syllable string to a script syllable
        /// Here we suppose syllable is a valid pronunciation string.
        /// </summary>
        /// <param name="syllable">Syllable string, doesn't include unit boundary.</param>
        /// <param name="phoneSet">TtsPhoneSet.</param>
        /// <returns>The constructed script syllable.</returns>
        public static ScriptSyllable ParseStringToSyllable(string syllable, TtsPhoneSet phoneSet)
        {
            if (string.IsNullOrEmpty(syllable))
            {
                throw new ArgumentNullException("syllable");
            }

            if (phoneSet == null)
            {
                throw new ArgumentNullException("phoneSet");
            }

            ScriptSyllable scriptSyllable = new ScriptSyllable(phoneSet.Language);
            ErrorSet errors = new ErrorSet();
            Phone[] phones = Pronunciation.SplitIntoPhones(syllable, phoneSet, errors);
            if (errors.Count > 0)
            {
                string message = Helper.NeutralFormat(
                    "The syllable string [{0}] isn't valid : {1}{2}",
                    syllable, Environment.NewLine, errors.ErrorsString());
                throw new InvalidDataException(message);
            }

            Collection<ScriptPhone> scriptPhones = new Collection<ScriptPhone>();
            foreach (Phone phone in phones)
            {
                if (phone.HasFeature(PhoneFeature.MainStress) ||
                    phone.HasFeature(PhoneFeature.SubStress))
                {
                    switch (phone.Name)
                    {
                        case "1":
                            scriptSyllable.Stress = TtsStress.Primary;
                            break;
                        case "2":
                            scriptSyllable.Stress = TtsStress.Secondary;
                            break;
                        case "3":
                            scriptSyllable.Stress = TtsStress.Tertiary;
                            break;
                    }
                }
                else if (phone.HasFeature(PhoneFeature.Tone))
                {
                    scriptPhones[scriptPhones.Count - 1].Tone = phone.Name;
                }
                else
                {
                    ScriptPhone scriptPhone = new ScriptPhone(phone.Name);
                    scriptPhone.Syllable = scriptSyllable;
                    scriptPhones.Add(scriptPhone);
                }
            }

            scriptSyllable.Phones.Clear();
            Helper.AppendCollection(scriptSyllable.Phones, scriptPhones);
            return scriptSyllable;
        }

        /// <summary>
        /// Get the stress according to stress name in script file.
        /// </summary>
        /// <param name="name">Stress name.</param>
        /// <returns>TtsStress.</returns>
        public static TtsStress StringToStress(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            TtsStress stress = TtsStress.None;
            switch (name)
            {
                case "0":
                    break;
                case "1":
                    stress = TtsStress.Primary;
                    break;
                case "2":
                    stress = TtsStress.Secondary;
                    break;
                case "3":
                    stress = TtsStress.Tertiary;
                    break;
                default:
                    string message = Helper.NeutralFormat("Unrecognized stress name: \"{0}\"!", name);
                    throw new InvalidDataException(message);
            }

            return stress;
        }

        /// <summary>
        /// Convert TtsStress to string used in script file.
        /// </summary>
        /// <param name="stress">TtsStress.</param>
        /// <returns>
        /// String representation of TtsStress.
        /// </returns>
        public static string StressToString(TtsStress stress)
        {
            string name = string.Empty;

            switch (stress)
            {
                case TtsStress.Primary:
                    name = @"1";
                    break;
                case TtsStress.Secondary:
                    name = @"2";
                    break;
                case TtsStress.Tertiary:
                    name = @"3";
                    break;
            }

            return name;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Build the syllable's pronunciation.
        /// </summary>
        /// <param name="phoneSet">Phone set.</param>
        /// <returns>The built pronunciation string.</returns>
        public string BuildTextFromPhones(TtsPhoneSet phoneSet)
        {
            if (phoneSet == null)
            {
                throw new ArgumentNullException("phoneSet");
            }

            StringBuilder sb = new StringBuilder();

            foreach (ScriptPhone phone in _phones)
            {
                sb.AppendFormat("{0}{1}", phone.Name, " ");

                // append stress
                if (_stress != TtsStress.None)
                {
                    Phone ttsPhone = phoneSet.GetPhone(phone.Name);
                    if (ttsPhone != null && ttsPhone.IsVowel)
                    {
                        switch (_stress)
                        {
                            case TtsStress.Primary:
                                sb.Append("1 ");
                                break;
                            case TtsStress.Secondary:
                                sb.Append("2 ");
                                break;
                            case TtsStress.Tertiary:
                                sb.Append("3 ");
                                break;
                        }
                    }
                }

                // append tone
                if (!string.IsNullOrEmpty(phone.Tone))
                {
                    sb.AppendFormat("{0}{1}", phone.Tone, " ");
                }
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Check whether the element is valid or not.
        /// </summary>
        /// <param name="itemID">ID of the script item.</param>
        /// <param name="nodePath">Path of the node.</param>
        /// <param name="scope">The validation scope.</param>
        /// <param name="errors">Contains errors found at present.</param>
        /// <returns>Valid or not. Always true if no validation is performed.</returns>
        public bool IsValid(string itemID, string nodePath, XmlScriptValidationScope scope, ErrorSet errors)
        {
            bool valid = true;

            for (int i = 0; i < _phones.Count; i++)
            {
                ScriptPhone phone = _phones[i];
                string path = string.Format(CultureInfo.InvariantCulture, "{0}.Phone[{1}]", nodePath, i);
                if (!phone.IsValid(itemID, path, scope, errors))
                {
                    valid = false;
                }
            }

            if (HasAcousticsValue)
            {
                string path = string.Format(CultureInfo.InvariantCulture, "{0}.Acoustics", nodePath);
                if (!Acoustics.IsValid(itemID, path, scope, errors))
                {
                    valid = false;
                }
            }

            return valid;
        }

        /// <summary>
        /// Write syllable to xml.
        /// </summary>
        /// <param name="writer">Xml writer.</param>
        public void WriteToXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            // write <syl> node and its attributes
            writer.WriteStartElement("syl");

            string stressName = StressToString(Stress);
            if (!string.IsNullOrEmpty(stressName))
            {
                writer.WriteAttributeString("stress", stressName);
            }

            if (TobiPitchAccent != null)
            {
                writer.WriteAttributeString("tobipa", TobiPitchAccent.Label);
            }

            // write phones
            if (Phones.Count != 0)
            {
                writer.WriteStartElement("phs");
                foreach (ScriptPhone phone in Phones)
                {
                    phone.WriteToXml(writer);
                }

                writer.WriteEndElement();
            }

            if (HasAcousticsValue)
            {
                Acoustics.WriteToXml(writer);
            }

            writer.WriteEndElement();
        }

        #endregion
    }
}