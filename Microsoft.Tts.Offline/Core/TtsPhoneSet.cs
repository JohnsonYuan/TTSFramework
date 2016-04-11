//----------------------------------------------------------------------------
// <copyright file="TtsPhoneSet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements PhoneSetFile
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
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// TTS phone set error.
    /// </summary>
    public enum PhoneSetError
    {
        /// <summary>
        /// Unrecognized Phone feature.
        /// </summary>
        [ErrorAttribute(Message = "Phone /{0}/ error: Unrecognized phone feature [{1}].", 
            Severity = ErrorSeverity.Warning)]
        UnrecognizedPhoneFeature,

        /// <summary>
        /// Duplicate phone name.
        /// </summary>
        [ErrorAttribute(Message = "Phone /{0}/ error: Duplicate phone name (case-insensitive) /{0}/.")]
        DuplicatePhoneName,

        /// <summary>
        /// Duplicate phone id.
        /// </summary>
        [ErrorAttribute(Message = "Phone /{0}/ error: Duplicate phone id [{1}].")]
        DuplicatePhoneId,

        /// <summary>
        /// Empty phone set.
        /// </summary>
        [ErrorAttribute(Message = "Phone set error: Phone set is empty.")]
        EmptyPhoneSet,

        /// <summary>
        /// Unsupported feature in runtime.
        /// </summary>
        [ErrorAttribute(Message = "Phone set warning: Phone /{0}/ has feature /{1}/ which is unsupported in runtime.",
            Severity = ErrorSeverity.Warning)]
        UnsupportedFeatureInRuntime,

        /// <summary>
        /// Zero id.
        /// </summary>
        [ErrorAttribute(Message = "Phone /{0}/ error: Zero id is forbidden.")]
        ZeroId,

        /// <summary>
        /// Phone length exceeds the maximal number.
        /// </summary>
        [ErrorAttribute(Message = "Phone /{0}/ error: phone length exceeds the maximal number of {1}.")]
        PhoneNameTooLong,

        /// <summary>
        /// Feature is reserved for special phone according to the naming pattern.
        /// </summary>
        [ErrorAttribute(Message = "Phone /{0}/ error: phone feature [{1}] is only reserved for phone name pattern \"{2}\".")]
        ReservedFeatureForNamePattern,

        /// <summary>
        /// Name is reserved for special phone.
        /// </summary>
        [ErrorAttribute(Message = "Phone /{0}/ error: name /{0}/ is reserved for phone feature [{2}] with id {1}.")]
        ReservedName,

        /// <summary>
        /// Naming pattern is reserved for the special phone feature.
        /// </summary>
        [ErrorAttribute(Message = "Phone /{0}/ error: name pattern \"{1}\" is reserved for phone feature [{2}].")]
        ReservedNamePattern,

        /// <summary>
        /// Id is reserved.
        /// </summary>
        [ErrorAttribute(Message = "Phone /{0}/ error: id {1} is reserved for phone feature [{2}] with name [{3}].")]
        ReservedId,

        /// <summary>
        /// Only one feature is allowed for some special phone.
        /// </summary>
        [ErrorAttribute(Message = "Phone /{0}/ error: only one feature [{1}] for this special phone.")]
        OneFeatureAllow,

        /// <summary>
        /// Name and ID should be in the same order.
        /// </summary>
        [ErrorAttribute(Message = "Phone set error: name (upper-case) and id should be in the same order.")]
        SameOrderForNameAndId,

        /// <summary>
        /// Tone id error: tone id should be in continued and started from 1.
        /// </summary>
        [ErrorAttribute(Message = "Phone set error: tone id should be in continued and started from 1.")]
        ToneIdError,
    }

    /// <summary>
    /// Phoneset.
    /// </summary>
    public class TtsPhoneSet : XmlDataFile
    {
        /// <summary>
        /// Reserved Phone.
        /// </summary>
        #region Fields
        private const int MaxPhoneNameLength = 8;
        private const int WordBoundaryId = 1;
        private const string WordBoundaryPhone = "&";

        private const int SyllableBoundaryId = 2;
        private const string SyllableBoundaryPhone = "-";

        // add |ˊ|ˇ|ˋ|· for zhTW
        private static string tonePatternString = @"^(t[1-9])|ˊ|ˇ|ˋ|·$";
        private static XmlSchema _schema;
        private static KeyValuePair<Phone, PhoneFeature>[] _reservedPhones;

        private string _version = "1.0";
        private List<Phone> _phones = new List<Phone>();

        private KeyValuePair<string, PhoneFeature>[] _reservedPhonePatterns = 
        {
                // for ja-JP native phone, \ is main stress
                new KeyValuePair<string, PhoneFeature>("^1$|[\\\\]", PhoneFeature.MainStress),

                // use tonePatternString instead
                new KeyValuePair<string, PhoneFeature>(tonePatternString, PhoneFeature.Tone),
        };

        private SyllableStructure _syllableStructure = new SyllableStructure();

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsPhoneSet"/> class.
        /// </summary>
        /// <param name="language">Language of this phone set.</param>
        public TtsPhoneSet(Language language)
            : base(language)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsPhoneSet"/> class.
        /// </summary>
        public TtsPhoneSet()
        { 
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Configuration schema.
        /// </summary>
        public override XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.phoneset.xsd");
                    _schema.Includes.Clear();

                    XmlSchemaInclude included = new XmlSchemaInclude();
                    included.Schema =
                        XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.ttscommon.xsd");
                    _schema.Includes.Add(included);
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets Individual phones in this set.
        /// </summary>
        public ICollection<Phone> Phones
        {
            get { return _phones; }
        }

        /// <summary>
        /// Gets or sets Version.
        /// </summary>
        public string Version
        {
            get
            {
                return _version;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _version = value;
            }
        }

        /// <summary>
        /// Gets Syllable Structure.
        /// </summary>
        public SyllableStructure SyllableStructure
        {
            get { return _syllableStructure; }
        }

        /// <summary>
        /// Gets a value indicating whether is short pause supported.
        /// </summary>
        public bool IsShortPauseSupported
        {
            get
            {
                foreach (Phone phone in Phones)
                {
                    if (Phoneme.IsShortPausePhone(phone.Name))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        #endregion

        /// <summary>
        /// Load phone set from file.
        /// </summary>
        /// <param name="language">The language of phone set.</param>
        /// <param name="path">Path to load phone set.</param>
        /// <returns>Loaded phone set.</returns>
        public static TtsPhoneSet LoadFromFile(Language language, string path)
        {
            TtsPhoneSet phoneSet = new TtsPhoneSet(language);

            phoneSet.Load(path);

            return phoneSet;
        }

        /// <summary>
        /// GetReservedPhones.
        /// </summary>
        /// <returns>ReservedPhones.</returns>
        public static KeyValuePair<Phone, PhoneFeature>[] GetReservedPhones()
        {
            if (_reservedPhones == null)
            {
                _reservedPhones = new KeyValuePair<Phone, PhoneFeature>[]
                {
                    new KeyValuePair<Phone, PhoneFeature>(new Phone("&", 1), PhoneFeature.Syllable),
                    new KeyValuePair<Phone, PhoneFeature>(new Phone("-", 2), PhoneFeature.Syllable),
                    new KeyValuePair<Phone, PhoneFeature>(new Phone("sil", 3), PhoneFeature.Silence)
                };
            }

            return _reservedPhones;
        }

        /// <summary>
        /// Whether the two tts phone sets are equal by nature.
        /// </summary>
        /// <param name="left">Left TtsPhoneSet.</param>
        /// <param name="right">Right TtsPhoneSet.</param>
        /// <param name="strict">Whether the comparison is strict.</param>
        /// <returns>True/false.</returns>
        public static bool Equals(TtsPhoneSet left, TtsPhoneSet right, bool strict)
        {
            if (!left.Version.Equals(right.Version))
            {
                return false;
            }

            if (left.Phones.Count != right.Phones.Count)
            {
                return false;
            }
            else
            {
                foreach (var lphone in left.Phones)
                {
                    bool found = false;
                    foreach (var rphone in right.Phones)
                    {
                        if (!found && lphone.CompareTo(rphone) == 0)
                        {
                            found = true;
                        }
                    }

                    if (!found)
                    {
                        return false;
                    }
                }
            }

            if (strict)
            {
                if (left.IsShortPauseSupported != right.IsShortPauseSupported)
                {
                    return false;
                }

                if (left.SyllableStructure.VowelCount.Max != right.SyllableStructure.VowelCount.Max ||
                    left.SyllableStructure.VowelCount.Min != right.SyllableStructure.VowelCount.Min ||
                    left.SyllableStructure.SonorantAndVowelCount.Max != right.SyllableStructure.SonorantAndVowelCount.Max ||
                    left.SyllableStructure.SonorantAndVowelCount.Min != right.SyllableStructure.SonorantAndVowelCount.Min)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Disables the short pause phoneme. This operation cannot be reverted.
        /// </summary>
        public void DisableShortPause()
        {
            Phone shortPause = null;
            foreach (Phone phone in Phones)
            {
                if (Phoneme.IsShortPausePhone(phone.Name))
                {
                    shortPause = phone;
                }
            }

            if (shortPause != null)
            {
                Phones.Remove(shortPause);
            }
        }

        /// <summary>
        /// Reset tts phone set for re-use.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _phones.Clear();
        }

        /// <summary>
        /// Validate consistence between files. If failed, InvalidDataException will be thrown.
        /// </summary>
        public override void Validate()
        {
            if (validated)
            {
                return;
            }

            if (_phones.Count == 0)
            {
                this.ErrorSet.Add(PhoneSetError.EmptyPhoneSet);
            }
            else
            {
                // Unique id and name for each phone, no zero id defined in the phone set
                Collection<string> names = new Collection<string>();
                Collection<int> ids = new Collection<int>();

                // change int to phone as zhTW's tone are strings
                List<Phone> tones = new List<Phone>();

                // use tonePatternString instead
                Regex tonePattern = new Regex(tonePatternString);

                int lastId = 0;
                foreach (Phone phone in _phones)
                {
                    if (!names.Contains(phone.Name.ToUpperInvariant()))
                    {
                        names.Add(phone.Name.ToUpperInvariant());
                    }
                    else
                    {
                        this.ErrorSet.Add(PhoneSetError.DuplicatePhoneName, phone.Name);
                    }

                    if (!ids.Contains(phone.Id))
                    {
                        ids.Add(phone.Id);
                    }
                    else
                    {
                        this.ErrorSet.Add(PhoneSetError.DuplicatePhoneId, phone.Name, phone.Id.ToString(CultureInfo.InvariantCulture));
                    }

                    if (phone.Id == 0)
                    {
                        this.ErrorSet.Add(PhoneSetError.ZeroId, phone.Name);
                    }

                    CheckReservedId(phone);

                    if (phone.HasFeature(PhoneFeature.Tone))
                    {
                        Match match = tonePattern.Match(phone.Name);
                        if (match.Success)
                        {
                            tones.Add(phone);
                        }
                    }

                    if (phone.Name.Length > MaxPhoneNameLength)
                    {
                        this.ErrorSet.Add(PhoneSetError.PhoneNameTooLong, phone.Name, 
                            MaxPhoneNameLength.ToString(CultureInfo.InvariantCulture));
                    }

                    if (phone.Id <= lastId)
                    {
                        this.ErrorSet.Add(PhoneSetError.SameOrderForNameAndId);
                    }

                    foreach (PhoneFeature feature in phone.Features)
                    {
                        if ((long)feature > uint.MaxValue)
                        {
                            ErrorSet.Add(PhoneSetError.UnsupportedFeatureInRuntime, phone.Name, feature.ToString());
                        }
                    }

                    lastId = phone.Id;
                }

                if (tones.Count > 0)
                {
                    for (int i = 1; i < tones.Count; i++)
                    {
                        if (tones[i].Id != tones[i - 1].Id + 1)
                        {
                            this.ErrorSet.Add(PhoneSetError.ToneIdError);
                            break;
                        }
                    }
                }
            }

            validated = true;
        }

        /// <summary>
        /// Check a phone is stress.
        /// </summary>
        /// <param name="phone">Phone name.</param>
        /// <returns>True/false.</returns>
        public bool IsStress(string phone)
        {
            Phone ttsPhone = GetPhone(phone);
            return ttsPhone != null && ttsPhone.IsStress;
        }

        /// <summary>
        /// Check if it is a syllable boundary.
        /// </summary>
        /// <param name="phone">Phone name.</param>
        /// <returns>True/false.</returns>
        public bool IsSyllableBoundary(string phone)
        {
            Phone ttsPhone = GetPhone(phone);
            return ttsPhone != null && ttsPhone.IsSyllableBoundary;
        }

        /// <summary>
        /// Is a vowel.
        /// </summary>
        /// <param name="phone">Phone name.</param>
        /// <returns>True/false.</returns>
        public bool IsVowel(string phone)
        {
            Phone ttsPhone = GetPhone(phone);
            return ttsPhone != null && ttsPhone.IsVowel;
        }

        /// <summary>
        /// Is a voiced phone.
        /// </summary>
        /// <param name="phone">Phone name.</param>
        /// <returns>True/false.</returns>
        public bool IsVoiced(string phone)
        {
            Phone ttsPhone = GetPhone(phone);
            return ttsPhone != null && ttsPhone.IsVoiced;
        }

        /// <summary>
        /// Is a consonant.
        /// </summary>
        /// <param name="phone">Phone name.</param>
        /// <returns>True/false.</returns>
        public bool IsConsonant(string phone)
        {
            Phone ttsPhone = GetPhone(phone);
            return ttsPhone != null && ttsPhone.IsConsonant;
        }

        /// <summary>
        /// Is a sornorant.
        /// </summary>
        /// <param name="phone">Phone name.</param>
        /// <returns>True/false.</returns>
        public bool IsSonorant(string phone)
        {
            Phone ttsPhone = GetPhone(phone);
            return ttsPhone != null && ttsPhone.IsSonorant;
        }

        /// <summary>
        /// Is a nasal.
        /// </summary>
        /// <param name="phone">Phone name.</param>
        /// <returns>True/false.</returns>
        public bool IsNasal(string phone)
        {
            Phone ttsPhone = GetPhone(phone);
            return ttsPhone != null && ttsPhone.IsNasal;
        }

        /// <summary>
        /// Is a phone.
        /// </summary>
        /// <param name="phone">Phone name.</param>
        /// <returns>True/false.</returns>
        public bool IsPhone(string phone)
        {
            Phone ttsPhone = GetPhone(phone);
            return ttsPhone != null;
        }

        /// <summary>
        /// Check a phone has silence feature.
        /// </summary>
        /// <param name="phone">Phone name.</param>
        /// <returns>True/false.</returns>
        public bool IsSilenceFeature(string phone)
        {
            Phone ttsPhone = GetPhone(phone);
            return ttsPhone != null && ttsPhone.HasFeature(PhoneFeature.Silence);
        }

        /// <summary>
        /// Checks a phone has short pause feature.
        /// </summary>
        /// <param name="phone">Phone name.</param>
        /// <returns>True/false.</returns>
        public bool IsShortPauseFeature(string phone)
        {
            Phone ttsPhone = GetPhone(phone);
            return ttsPhone != null && ttsPhone.HasFeature(PhoneFeature.ShortPause);
        }

        /// <summary>
        /// Remove the syllable boundary, stress symbols and tone from phone set.
        /// </summary>
        public void RemoveSyllableStressTones()
        {
            if ((Phones != null) && (Phones.Count > 0))
            {
                List<Phone> unnormalPhones = new List<Phone>();

                foreach (Phone phone in Phones)
                {
                    if (phone.IsSyllableBoundary)
                    {
                        unnormalPhones.Add(phone);
                        continue;
                    }

                    if (phone.IsStress)
                    {
                        unnormalPhones.Add(phone);
                        continue;
                    }

                    if (phone.HasFeature(PhoneFeature.Tone))
                    {
                        unnormalPhones.Add(phone);
                        continue;
                    }
                }

                foreach (Phone phone in unnormalPhones)
                {
                    Phones.Remove(phone);
                }
            }
        }

        /// <summary>
        /// Get the tts phone according to the phone id
        /// If failed, null will be returned.
        /// </summary>
        /// <param name="phoneId">Phone ID.</param>
        /// <returns>TTS phone.</returns>
        public Phone GetPhone(int phoneId)
        {
            Phone foundTtsPhone = null;
            foreach (Phone ttsPhone in Phones)
            {
                if (ttsPhone.Id == phoneId)
                {
                    foundTtsPhone = ttsPhone;
                    break;
                }
            }

            return foundTtsPhone;
        }

        /// <summary>
        /// Get the tts phone according to the phone string.
        /// If failed, null will be returned.
        /// </summary>
        /// <param name="phone">Phone string.</param>
        /// <returns>Tts phone.</returns>
        public Phone GetPhone(string phone)
        {
            Phone foundTtsPhone = null;
            foreach (Phone ttsPhone in Phones)
            {
                if (ttsPhone.Name.Equals(phone, StringComparison.OrdinalIgnoreCase) ||
                    (Phoneme.IsSilencePhone(phone) && Phoneme.IsSilencePhone(ttsPhone.Name)) ||
                    (Phoneme.IsShortPausePhone(phone) && Phoneme.IsShortPausePhone(ttsPhone.Name)))
                {
                    foundTtsPhone = ttsPhone;
                    break;
                }
            }

            return foundTtsPhone;
        }

        /// <summary>
        /// Assign Id for phones.
        /// </summary>
        public void AssignId()
        {
            _phones.Sort();

            // Start id from 1
            int id = 1;
            foreach (Phone phone in _phones)
            {
                phone.Id = id;
                id++;
            }
        }

        /// <summary>
        /// Load phone set instance from stream reader.
        /// </summary>
        /// <param name="xmlDoc">Document to load phone set from.</param>
        /// <param name="nsmgr">Namespace.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void Load(XmlDocument xmlDoc, XmlNamespaceManager nsmgr, object contentController)
        {
            if (xmlDoc == null)
            {
                throw new ArgumentNullException("xmlDoc");
            }

            if (nsmgr == null)
            {
                throw new ArgumentNullException("nsmgr");
            }

            this.ErrorSet.Clear();
            Language language = Localor.StringToLanguage(xmlDoc.DocumentElement.GetAttribute("lang"));
            if (!this.Language.Equals(Language.Neutral) && !language.Equals(this.Language))
            {
                this.ErrorSet.Add(CommonError.NotConsistentLanguage,
                    this.Language.ToString(), "initial one", language.ToString(), "phone set");
            }

            this.Language = language;

            XmlNode syllableStructureNode = xmlDoc.DocumentElement.SelectSingleNode("/tts:phoneSet/tts:syllableStructure", nsmgr);
            if (syllableStructureNode != null)
            {
                XmlNode vowelCountNode = syllableStructureNode.SelectSingleNode("tts:vowelCount", nsmgr);
                if (vowelCountNode != null)
                {
                    XmlElement xmlEle = vowelCountNode as XmlElement;
                    string minimum = xmlEle.GetAttribute("min");
                    if (!string.IsNullOrEmpty(minimum))
                    {
                        _syllableStructure.VowelCount.Min = int.Parse(minimum, CultureInfo.InvariantCulture);
                    }

                    string maximum = xmlEle.GetAttribute("max");
                    if (!string.IsNullOrEmpty(maximum))
                    {
                        _syllableStructure.VowelCount.Max = int.Parse(maximum, CultureInfo.InvariantCulture);
                    }
                }

                XmlNode sonorantAndVowelCountNode = syllableStructureNode.SelectSingleNode("tts:sonorantAndVowelCount", nsmgr);
                if (sonorantAndVowelCountNode != null)
                {
                    XmlElement xmlEle = sonorantAndVowelCountNode as XmlElement;
                    string minimum = xmlEle.GetAttribute("min");
                    if (!string.IsNullOrEmpty(minimum))
                    {
                        _syllableStructure.SonorantAndVowelCount.Min = int.Parse(minimum, CultureInfo.InvariantCulture);
                    }

                    string maximum = xmlEle.GetAttribute("max");
                    if (!string.IsNullOrEmpty(maximum))
                    {
                        _syllableStructure.SonorantAndVowelCount.Max = int.Parse(maximum, CultureInfo.InvariantCulture);
                    }
                }
            }

            XmlNodeList nodeList = xmlDoc.DocumentElement.SelectNodes("/tts:phoneSet/tts:phone", nsmgr);
            Debug.Assert(nodeList != null && nodeList.Count > 0);
            _phones.Clear();
            foreach (XmlNode xmlNode in nodeList)
            {
                XmlElement xmlEle = xmlNode as XmlElement;

                string name = xmlEle.GetAttribute("name");
                Debug.Assert(!string.IsNullOrEmpty(name));
                string idString = xmlEle.GetAttribute("id");
                Debug.Assert(!string.IsNullOrEmpty(idString));

                int id = int.Parse(idString, CultureInfo.InvariantCulture);

                string featureString = xmlEle.SelectSingleNode("tts:feature", nsmgr).InnerText;
                string[] features = featureString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                Phone phone = new Phone(name, id);

                foreach (string feature in features)
                {
                    if (!phone.AddFeature(feature))
                    {
                        this.ErrorSet.Add(PhoneSetError.UnrecognizedPhoneFeature, name, feature);
                    }
                }

                _phones.Add(phone);
            }

            _phones.Sort();

            // Todo: remove validate here, bug#10305
            Validate();
        }

        /// <summary>
        /// Save phone set to target file.
        /// </summary>
        /// <param name="writer">Writer file to save into.</param>
        /// <param name="contentController">Content controller.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Ignore.")]
        protected override void PerformanceSave(XmlWriter writer, object contentController)
        {
            writer.WriteStartElement("phoneSet", Schema.TargetNamespace);
            writer.WriteAttributeString("lang", Localor.LanguageToString(Language));
            writer.WriteAttributeString("version", Version);

            writer.WriteStartElement("syllableStructure");
            writer.WriteStartElement("vowelCount");
            if (_syllableStructure.VowelCount.Min != CountRange.NotApplicable)
            {
                writer.WriteAttributeString("min", _syllableStructure.VowelCount.Min.ToString(CultureInfo.InvariantCulture));
            }

            if (_syllableStructure.VowelCount.Max != CountRange.NotApplicable)
            {
                writer.WriteAttributeString("max", _syllableStructure.VowelCount.Max.ToString(CultureInfo.InvariantCulture));
            }

            writer.WriteEndElement();

            writer.WriteStartElement("sonorantAndVowelCount");
            if (_syllableStructure.SonorantAndVowelCount.Min != CountRange.NotApplicable)
            {
                writer.WriteAttributeString("min", _syllableStructure.SonorantAndVowelCount.Min.ToString(CultureInfo.InvariantCulture));
            }

            if (_syllableStructure.SonorantAndVowelCount.Max != CountRange.NotApplicable)
            {
                writer.WriteAttributeString("max", _syllableStructure.SonorantAndVowelCount.Max.ToString(CultureInfo.InvariantCulture));
            }

            writer.WriteEndElement();
            writer.WriteEndElement();

            // Use this map to save the phone in order of phone id.
            SortedDictionary<int, Phone> _phoneMap =
                new SortedDictionary<int, Phone>();

            foreach (Phone phone in _phones)
            {
                _phoneMap.Add(phone.Id, phone);
            }
 
            foreach (KeyValuePair<int, Phone> pair in _phoneMap)
            {
                writer.WriteStartElement("phone");

                // Sync with runtime silence naming design
                writer.WriteAttributeString("name", Phoneme.ToRuntime(pair.Value.Name));

                writer.WriteAttributeString("id", pair.Key.ToString(CultureInfo.InvariantCulture));

                writer.WriteStartElement("feature");
                int featureCount = pair.Value.Features.Count;
                string[] features = new string[featureCount];
                for (int i = 0; i < featureCount; i++)
                {
                    features[i] = pair.Value.Features[i].ToString().ToLowerInvariant();
                }

                writer.WriteString(string.Join(" ", features));
                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Check the phone whether satisfies the requirement of reserved phone.
        /// </summary>
        /// <param name="phone">Phone.</param>
        private void CheckReservedId(Phone phone)
        {
            foreach (KeyValuePair<Phone, PhoneFeature> reservedPhone in GetReservedPhones())
            {
                if (phone.Name.Equals(reservedPhone.Key.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (phone.Id != reservedPhone.Key.Id || !phone.HasFeature(reservedPhone.Value))
                    {
                        this.ErrorSet.Add(PhoneSetError.ReservedName, phone.Name,
                            phone.Id.ToString(CultureInfo.InvariantCulture), 
                            reservedPhone.Value.ToString());
                    }
                    else
                    {
                        foreach (PhoneFeature feature in phone.Features)
                        {
                            if (!feature.Equals(reservedPhone.Value))
                            {
                                this.ErrorSet.Add(PhoneSetError.OneFeatureAllow, phone.Name,
                                    reservedPhone.Value.ToString());
                                break;
                            }
                        }
                    }
                }
                else if (phone.Id.Equals(reservedPhone.Key.Id))
                {
                    this.ErrorSet.Add(PhoneSetError.ReservedId, phone.Name,
                        phone.Id.ToString(CultureInfo.InvariantCulture),
                        reservedPhone.Value.ToString(), reservedPhone.Key.Name);
                }
            }

            if (phone.HasFeature(PhoneFeature.MainStress) || phone.HasFeature(PhoneFeature.SubStress))
            {
                Regex regex = new Regex("^[1-9\\\\/]$", RegexOptions.IgnoreCase);
                if (!regex.Match(phone.Name).Success)
                {
                    this.ErrorSet.Add(PhoneSetError.ReservedFeatureForNamePattern, phone.Name,
                        PhoneFeature.MainStress.ToString() + " or " + PhoneFeature.SubStress.ToString(),
                        "^[1-9\\\\/]$");
                }
            }

            foreach (KeyValuePair<string, PhoneFeature> reservedPattern in _reservedPhonePatterns)
            {
                if (phone.HasFeature(reservedPattern.Value))
                {
                    Regex regex = new Regex(reservedPattern.Key, RegexOptions.IgnoreCase);
                    if (!regex.Match(phone.Name).Success)
                    {
                        this.ErrorSet.Add(PhoneSetError.ReservedFeatureForNamePattern, phone.Name,
                            reservedPattern.Value.ToString(), reservedPattern.Key);
                    }
                }
                else
                {
                    Regex regex = new Regex(reservedPattern.Key, RegexOptions.IgnoreCase);
                    if (regex.Match(phone.Name).Success)
                    {
                        this.ErrorSet.Add(PhoneSetError.ReservedNamePattern, phone.Name,
                            reservedPattern.Key, reservedPattern.Value.ToString());
                    }
                }
            }
        }
    }
}