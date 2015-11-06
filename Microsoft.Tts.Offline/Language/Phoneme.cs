//----------------------------------------------------------------------------
// <copyright file="Phoneme.cs" company="MICROSOFT">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Phoneme
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Text;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Tone manager class, used for mapping tone name and ids.
    /// </summary>
    public class ToneManager
    {
        /// <summary>
        /// None context tone ID.
        /// </summary>
        public const int NoneContextTone = 0;

        /// <summary>
        /// Context tone start index.
        /// </summary>
        public const int ContextToneStartIndex = 1;

        #region Fileds

        private SortedDictionary<string, TtsTone> _nameMap = new SortedDictionary<string, TtsTone>();
        private SortedDictionary<int, TtsTone> _contextIdMap = new SortedDictionary<int, TtsTone>();
        private SortedDictionary<int, TtsTone> _toneIdMap = new SortedDictionary<int, TtsTone>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets Tone context ID map, map from tone context ID to TtsTone object.
        /// </summary>
        public SortedDictionary<int, TtsTone> ContextIdMap
        {
            get { return _contextIdMap; }
        }

        /// <summary>
        /// Gets Tone name map, map from tone name to TtsTone object.
        /// </summary>
        public SortedDictionary<string, TtsTone> NameMap
        {
            get { return _nameMap; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Add one tone to the tone manager.
        /// </summary>
        /// <param name="phone">TtsPhone represents the tone.</param>
        /// <param name="contextToneId">Tone context ID.</param>
        public void Add(Phone phone, int contextToneId)
        {
            if (phone == null)
            {
                throw new ArgumentNullException("phone");
            }

            if (contextToneId < 0)
            {
                throw new ArgumentException("contextToneId is null");
            }

            TtsTone ttsTone = new TtsTone(phone);
            ttsTone.ContextToneId = contextToneId;

            _nameMap.Add(ttsTone.Phone.Name, ttsTone);
            _contextIdMap.Add(contextToneId, ttsTone);
            _toneIdMap.Add(ttsTone.Phone.Id, ttsTone);
        }

        /// <summary>
        /// Get tone name from tone ID.
        /// </summary>
        /// <param name="toneId">Tone ID.</param>
        /// <returns>Tone name.</returns>
        public string GetNameFromToneId(int toneId)
        {
            if (toneId <= 0)
            {
                throw new ArgumentException("toneId is null");
            }

            if (!_toneIdMap.ContainsKey(toneId))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Invalid tone id [{0}].", toneId));
            }

            return _toneIdMap[toneId].Phone.Name;
        }

        /// <summary>
        /// Get tone name from tone context ID.
        /// </summary>
        /// <param name="contextId">Tone context ID.</param>
        /// <returns>Tone name.</returns>
        public string GetNameFromContextId(int contextId)
        {
            if (contextId <= 0)
            {
                throw new ArgumentException("contextId is null");
            }

            if (!_contextIdMap.ContainsKey(contextId))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Invalid tone context id [{0}].", contextId));
            }

            return _contextIdMap[contextId].Phone.Name;
        }

        /// <summary>
        /// Get tone id defined in phoneset.xml from tone name.
        /// </summary>
        /// <param name="name">Tone name defined in phoneset.xml.</param>
        /// <returns>Tone ID.</returns>
        public int GetToneId(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (!_nameMap.ContainsKey(name))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Invalid phone name [{0}].", name));
            }

            return _nameMap[name].Phone.Id;
        }

        /// <summary>
        /// Get tone context ID from tone name.
        /// </summary>
        /// <param name="name">Tone name defined in phoneset.xml.</param>
        /// <returns>Tone context ID.</returns>
        public int GetContextToneId(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (!_nameMap.ContainsKey(name))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Invalid phone name [{0}].", name));
            }

            return _nameMap[name].ContextToneId;
        }

        #endregion

        #region Class

        /// <summary>
        /// TtsTone class, wrap TtsPhone which represents tone and tone context ID.
        /// </summary>
        public class TtsTone
        {
            #region Fields

            private Phone _phone;

            /// <summary>
            /// Right/Left context phone id, the id should be continuously, start with 1.
            /// The same order with the phones defined in phoneset.xml.
            /// "0" presents no tone.
            /// </summary>
            private int _contextToneId;

            #endregion

            #region Constructor

            /// <summary>
            /// Initializes a new instance of the <see cref="TtsTone"/> class.
            /// </summary>
            /// <param name="phone">TtsPhone contains the tone information.</param>
            public TtsTone(Phone phone)
            {
                _phone = phone;
            }

            #endregion

            #region Properties

            /// <summary>
            /// Gets TtsPhone representes the tone.
            /// </summary>
            public Phone Phone
            {
                get { return _phone; }
            }

            /// <summary>
            /// Gets or sets Tone context ID.
            /// </summary>
            public int ContextToneId
            {
                get { return _contextToneId; }
                set { _contextToneId = value; }
            }

            #endregion
        }

        #endregion
    }

    /// <summary>
    /// Description for Phoneme.
    /// </summary>
    public class Phoneme
    {
        #region Fields, const, member variables, etc.

        /// <summary>
        /// A invalid phoneme name.
        /// </summary>
        public const string Null = "NULLPHONEME";

        /// <summary>
        /// Phone id, representing any phone.
        /// </summary>
        public const int AnyPhoneId = int.MaxValue;

        /// <summary>
        /// Silence phone id.
        /// </summary>
        public const int SilencePhoneId = 3;

        /// <summary>
        /// Silence phone, represented as "-sil-" in runtime.
        /// </summary>
        public const string RuntimeSilence = "-sil-";

        /// <summary>
        /// Silence phone, represented as "sil".
        /// </summary>
        public const string Silence = "sil";

        /// <summary>
        /// HTK silence phone, this representation is hard coded in HTK tools.
        /// All file used or generated by HTK should use this symbol to represent silence.
        /// </summary>
        public const string HtkSilence = "SIL";

        /// <summary>
        /// Any phoneme.
        /// </summary>
        private const string AnyPhoneName = "*";

        /// <summary>
        /// Short pause phone, represented as "sp", this is ussualy used 
        /// In forced alignment to indicate very short silence.
        /// </summary>
        private const string ShortPause = "sp";

        /// <summary>
        /// Short pause phone, represented as "-sp-" in runtime.
        /// </summary>
        private const string RuntimeShortPause = "-sp-";

        /// <summary>
        /// HTK short pause phone.
        /// </summary>
        private const string HtkShortPause = "SP";

        // varait phone tables
        private Collection<string> _ttsPhoneSet;
        private Collection<string> _ttsVowelPhoneSet;
        private Collection<string> _ttsSonorantPhoneSet;

        private Dictionary<string, int> _ttsPhoneIds;
        private Dictionary<string, int> _sapiVisemeIds;
        private Collection<string> _tts2SrMap;
        private TtsToSrMappingType _tts2srMapType;
        private Dictionary<string, string> _ipaPhones;

        private Dictionary<string, int> _posCodes = new Dictionary<string, int>();

        private Language _language = Language.Neutral;
        private ToneManager _toneManager = new ToneManager();

        /// <summary>
        /// TTS2SR map type.
        /// </summary>
        public enum TtsToSrMappingType
        {
            /// <summary>
            /// PhoneBased.
            /// </summary>
            PhoneBased = 0,

            /// <summary>
            /// SyllableBased.
            /// </summary>
            SyllableBased = 1
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the silence phoneme name.
        /// </summary>
        public static string SilencePhone
        {
            get
            {
                return Silence;
            }
        }

        /// <summary>
        /// Gets the short pause phoneme name.
        /// </summary>
        public static string ShortPausePhone
        {
            get
            {
                return ShortPause;
            }
        }

        /// <summary>
        /// Gets the any phoneme name.
        /// </summary>
        public static string AnyPhone
        {
            get
            {
                return AnyPhoneName;
            }
        }

        /// <summary>
        /// Gets TTS2SR map type, affect the behavior of converting the pronunciation from TTS phone to SR phone.
        /// </summary>
        public TtsToSrMappingType Tts2srMapType
        {
            get
            {
                EnsureMapLoaded(_tts2SrMap, Localor.TtsToSrPhoneFileName);
                return _tts2srMapType;
            }
        }

        /// <summary>
        /// Gets TTS phone set, which does not include Silence and Short Pause phone.
        /// </summary>
        public Collection<string> TtsPhones
        {
            get { return _ttsPhoneSet; }
        }

        /// <summary>
        /// Gets TTS phone's id.
        /// </summary>
        public Dictionary<string, int> TtsPhoneIds
        {
            get { return _ttsPhoneIds; }
        }

        /// <summary>
        /// Gets Tone manager.
        /// </summary>
        public ToneManager ToneManager
        {
            get { return _toneManager; }
        }

        /// <summary>
        /// Gets TTS phone's sapi viseme id.
        /// </summary>
        public Dictionary<string, int> SapiVisemeIds
        {
            get
            {
                EnsureMapLoaded(_sapiVisemeIds, Localor.TtsToSapiVisemeIdFileName);
                return _sapiVisemeIds;
            }
        }

        /// <summary>
        /// Gets International phonetic alphabet phones.
        /// </summary>
        public Dictionary<string, string> IpaPhones
        {
            get
            {
                EnsureMapLoaded(_ipaPhones, Localor.TtsToIpaPhoneFileName);
                return _ipaPhones;
            }
        }

        /// <summary>
        /// Gets TTS vowel phone set.
        /// </summary>
        public Collection<string> TtsVowelPhones
        {
            get { return _ttsVowelPhoneSet; }
        }

        /// <summary>
        /// Gets TTS sonorant phone set.
        /// </summary>
        public Collection<string> TtsSonorantPhones
        {
            get { return _ttsSonorantPhoneSet; }
        }

        /// <summary>
        /// Gets Part of speech code.
        /// </summary>
        public Dictionary<string, int> PosCodes
        {
            get { return _posCodes; }
        }

        /// <summary>
        /// Gets Phone map between TTS phone set and speech recognition phone set.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        public Collection<string> Tts2SrMap
        {
            get
            {
                EnsureMapLoaded(_tts2SrMap, Localor.TtsToSrPhoneFileName);
                return _tts2SrMap;
            }
        } 

        #endregion

        #region Virtual Members

        /// <summary>
        /// Gets or sets Language of this phoneme for.
        /// </summary>
        public virtual Language Language
        {
            get { return _language; }
            set { _language = value; }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Get Htk phoneme name.
        /// </summary>
        /// <param name="phone">Phone name.</param>
        /// <returns>Htk phone.</returns>
        public static string ToHtk(string phone)
        {
            if (IsSilencePhone(phone))
            {
                return HtkSilence;
            }

            if (IsShortPausePhone(phone))
            {
                return HtkShortPause;
            }

            return phone;
        }

        /// <summary>
        /// Get runtime phoneme name.
        /// </summary>
        /// <param name="phone">Phone name.</param>
        /// <returns>Htk phone.</returns>
        public static string ToRuntime(string phone)
        {
            if (IsSilencePhone(phone))
            {
                return RuntimeSilence;
            }

            if (IsShortPausePhone(phone))
            {
                return RuntimeShortPause;
            }

            return phone;
        }

        /// <summary>
        /// Get Offline phoneme name.
        /// </summary>
        /// <param name="phone">Phone name.</param>
        /// <returns>Offline phone.</returns>
        public static string ToOffline(string phone)
        {
            if (IsSilencePhone(phone))
            {
                return Silence;
            }

            if (IsShortPausePhone(phone))
            {
                return ShortPause;
            }

            return phone;
        }

        /// <summary>
        /// Tell if the phone has slience feature.
        /// </summary>
        /// <param name="phone">Phone.</param>
        /// <returns>Bool.</returns>
        public static bool IsSilenceFeature(string phone)
        {
            return IsSilencePhone(phone) || IsShortPausePhone(phone);
        }

        /// <summary>
        /// Tell if the phone is a slience.
        /// </summary>
        /// <param name="phone">Phone.</param>
        /// <returns>Bool.</returns>
        public static bool IsSilencePhone(string phone)
        {
            return string.Compare(phone, Phoneme.Silence, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(phone, Phoneme.RuntimeSilence, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Tell if the phone is a short pause.
        /// </summary>
        /// <param name="phone">Phone.</param>
        /// <returns>Bool.</returns>
        public static bool IsShortPausePhone(string phone)
        {
            return string.Compare(phone, Phoneme.ShortPause, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(phone, Phoneme.RuntimeShortPause, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Tells if the phone is an any phone.
        /// </summary>
        /// <param name="phone">Phone.</param>
        /// <returns>Bool.</returns>
        public static bool IsAnyPhone(string phone)
        {
            return string.Compare(phone, Phoneme.AnyPhoneName, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Load phoneme.
        /// </summary>
        /// <param name="language">Language of phoneme to load.</param>
        /// <returns>Phoneme.</returns>
        public static Phoneme Create(Language language)
        {
            Phoneme phoneme = new Phoneme();
            phoneme.Language = language;

            TtsPhoneSet phoneSet = Localor.GetPhoneSet(language);
            bool loaded = false;
            if (phoneSet != null)
            {
                phoneme.ParseData(phoneSet);
                loaded = true;
            }

            return loaded ? phoneme : null;
        }

        /// <summary>
        /// Check whether the tts phone set contains the specified phone.
        /// </summary>
        /// <param name="phoneName">Tts phone name to be checked.</param>
        /// <returns>Whether the phone is contained in the TTS phone set.</returns>
        public bool ContainsTtsPhone(string phoneName)
        {
            return TtsPhoneIds.ContainsKey(phoneName) || ToneManager.NameMap.ContainsKey(phoneName);
        }

        /// <summary>
        /// Tell whether there is vowel phone in the phone set.
        /// </summary>
        /// <param name="pronunciation">Pronunciation to test.</param>
        /// <returns>True if yes, otherwise false.</returns>
        public bool ContainVowel(string pronunciation)
        {
            if (string.IsNullOrEmpty(pronunciation))
            {
                throw new ArgumentNullException("pronunciation");
            }

            string[] items = pronunciation.Split(new char[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);

            return ContainVowel(items);
        }

        /// <summary>
        /// Tell whether there is vowel phone in the phone set.
        /// </summary>
        /// <param name="phones">Phone collection to test.</param>
        /// <returns>True if yes, otherwise false.</returns>
        public bool ContainVowel(string[] phones)
        {
            if (phones == null)
            {
                throw new ArgumentNullException("phones");
            }

            return GetFirstVowelIndex(phones) != -1;
        }

        /// <summary>
        /// Find the index of first vowel phone in the phone sequence.
        /// </summary>
        /// <param name="phones">Phone collection to search the index of the fist vowel.</param>
        /// <returns>Vowel phone index, if not found return -1.</returns>
        public int GetFirstVowelIndex(string[] phones)
        {
            if (phones == null)
            {
                throw new ArgumentNullException("phones");
            }

            int foundIndex = -1;

            for (int i = 0; i < phones.Length; i++)
            {
                if (TtsVowelPhones.IndexOf(phones[i]) != -1)
                {
                    foundIndex = i;
                    break;
                }
            }

            return foundIndex;
        }

        /// <summary>
        /// Filter vowel phones.
        /// </summary>
        /// <param name="phones">Phone collection to search.</param>
        /// <returns>Vowel phone indexes found.</returns>
        public int[] GetVowelIndexes(string[] phones)
        {
            if (phones == null)
            {
                throw new ArgumentNullException("phones");
            }

            List<int> indexes = new List<int>();
            for (int i = 0; i < phones.Length; i++)
            {
                if (TtsVowelPhones.IndexOf(phones[i]) != -1)
                {
                    indexes.Add(i);
                }
            }

            return indexes.ToArray();
        }

        /// <summary>
        /// Filter sonorant phones.
        /// </summary>
        /// <param name="phones">Phone collection to search.</param>
        /// <returns>Sonorant phone indexes found.</returns>
        public int[] GetSonorantIndexes(string[] phones)
        {
            if (phones == null)
            {
                throw new ArgumentNullException("phones");
            }

            List<int> indexes = new List<int>();
            for (int i = 0; i < phones.Length; i++)
            {
                if (TtsSonorantPhones.IndexOf(phones[i]) != -1)
                {
                    indexes.Add(i);
                }
            }

            return indexes.ToArray();
        }

        /// <summary>
        /// Convert phone presentation from synthesis phone set to recognition phone set
        /// Because the tts phone set to sr phone set is not 1 to 1 mapping, the method
        /// May return more than one tts phone in one string with space for separating.
        /// </summary>
        /// <param name="phone">TTS phone to convert.</param>
        /// <returns>SR phone string.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member", Justification = "Ignore.")]
        public string[] Tts2SrPhones(string phone)
        {
            if (string.IsNullOrEmpty(phone))
            {
                throw new ArgumentNullException("phone");
            }

            Debug.Assert(Tts2SrMap.Count % 2 == 0);

            // Deal with syllable with dot, like syllable "sh . y . o" in ja-JP.
            phone = Core.Pronunciation.UnTagUnitBoundary(phone);

            List<string> phones = new List<string>();

            for (int i = 0; i < Tts2SrMap.Count; i += 2)
            {
                if (Tts2SrMap[i] == phone)
                {
                    if (phones.Count > 0)
                    {
                        // TODO: is this valid
                        Debug.Assert(false);
                    }

                    phones.AddRange(Tts2SrMap[i + 1].Split(new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries));
                }
            }

            return phones.Count > 0 ? phones.ToArray() : null;
        }

        /// <summary>
        /// Convert phone presentation from recognition phone set to synthesis phone set
        /// Because the sr phone set to tts phone set is not 1 to 1 mapping, the method
        /// May return more than one tts phone in one string with space for separating.
        /// </summary>
        /// <param name="phone">SR phone to convert.</param>
        /// <returns>TTS phone string.</returns>
        public string[] Sr2TtsPhones(string phone)
        {
            if (string.IsNullOrEmpty(phone))
            {
                throw new ArgumentNullException("phone");
            }

            Debug.Assert(Tts2SrMap.Count % 2 == 0);

            List<string> phones = new List<string>();

            for (int i = 0; i < Tts2SrMap.Count; i += 2)
            {
                if (Tts2SrMap[i + 1] == phone)
                {
                    phones.Add(Tts2SrMap[i]);
                }
            }

            return phones.Count > 0 ? phones.ToArray() : null;
        }

        /// <summary>
        /// Map TTS phone to TTS phone id.
        /// </summary>
        /// <param name="phone">TTS phone string.</param>
        /// <returns>Phone id.</returns>
        public int TtsPhone2Id(string phone)
        {
            if (string.IsNullOrEmpty(phone))
            {
                throw new ArgumentNullException("phone");
            }

            if (!TtsPhoneIds.ContainsKey(phone))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "[{0}] is not a valid phone in [{1}] phone set",
                    phone, Localor.LanguageToString(Language));
                throw new InvalidDataException(message);
            }

            return TtsPhoneIds[phone];
        }

        /// <summary>
        /// Map TTS phone id to TTS phone.
        /// </summary>
        /// <param name="phoneId">TTS phone id.</param>
        /// <returns>TTS phone string.</returns>
        public string TtsId2Phone(int phoneId)
        {
            foreach (KeyValuePair<string, int> kvp in TtsPhoneIds)
            {
                if (kvp.Value == phoneId)
                {
                    return kvp.Key;
                }
            }

            return null;
        }

        /// <summary>
        /// Map phone sequence to SAPI viseme id sequence,
        /// Seperated by "+", for a phone array.
        /// </summary>
        /// <param name="phones">TTS phone string array.</param>
        /// <returns>SAPI viseme strings.</returns>
        public string Map2SapiVisemeIds(string[] phones)
        {
            return Map2Ids(phones, SapiVisemeIds);
        }

        #endregion

        #region Private operation

        /// <summary>
        /// Go through the phone sequence, and map those ids into int sequence,
        /// Seperated by "+".
        /// </summary>
        /// <param name="phones">Phone array to perform mapping.</param>
        /// <param name="map">Mapping table.</param>
        /// <returns>Mapped phone string.</returns>
        private static string Map2Ids(string[] phones, Dictionary<string, int> map)
        {
            StringBuilder ids = new StringBuilder();

            foreach (string phone in phones)
            {
                if (ids.Length != 0)
                {
                    ids.Append("+");
                }

                ids.Append(map[phone]);
            }

            return ids.ToString();
        }

        /// <summary>
        /// Parse phone map data for this instance.
        /// </summary>
        /// <param name="phoneMap">Phone map.</param>
        /// <returns>Update target data.</returns>
        private ICollection ParseData(PhoneMap phoneMap)
        {
            if (phoneMap.Language != Language)
            {
                string message = Helper.NeutralFormat("The language [{0}] of phone map " +
                    "does not match with that [{1}] of phoneme.",
                    Localor.LanguageToString(phoneMap.Language), Localor.LanguageToString(Language));
            }

            ICollection ret = null;
            if (StringComparer.Ordinal.Compare(phoneMap.Source, PhoneMap.TtsPhone) == 0)
            {
                if (phoneMap.Target == PhoneMap.SapiVisemeId)
                {
                    _sapiVisemeIds = new Dictionary<string, int>();
                    foreach (string phone in phoneMap.Items.Keys)
                    {
                        int id = 0;
                        if (!int.TryParse(phoneMap.Items[phone], out id))
                        {
                            string message = Helper.NeutralFormat("Invalid SAPI id [{0}] for phone [{1}] found.",
                                phoneMap.Items[phone], phone);
                            throw new InvalidDataException(message);
                        }

                        _sapiVisemeIds.Add(phone, id);
                    }

                    ret = _sapiVisemeIds;
                }

                if (phoneMap.Target == PhoneMap.SrPhone)
                {
                    _tts2SrMap = new Collection<string>();
                    _tts2srMapType = TtsToSrMappingType.PhoneBased;
                    foreach (string phone in phoneMap.Items.Keys)
                    {
                        _tts2SrMap.Add(phone);
                        _tts2SrMap.Add(phoneMap.Items[phone]);
                    }

                    ret = _tts2SrMap;
                }

                if (phoneMap.Target == PhoneMap.IpaPhone)
                {
                    _ipaPhones = new Dictionary<string, string>();
                    foreach (string phone in phoneMap.Items.Keys)
                    {
                        _ipaPhones.Add(phone, phoneMap.Items[phone]);
                    }

                    ret = _ipaPhones;
                }
            }

            if (StringComparer.Ordinal.Compare(phoneMap.Source, PhoneMap.TtsSyllable) == 0)
            {
                if (phoneMap.Target == PhoneMap.SrPhone)
                {
                    _tts2SrMap = new Collection<string>();
                    _tts2srMapType = TtsToSrMappingType.SyllableBased;
                    foreach (string phone in phoneMap.Items.Keys)
                    {
                        _tts2SrMap.Add(phone);
                        _tts2SrMap.Add(phoneMap.Items[phone]);
                    }

                    ret = _tts2SrMap;
                }
            }

            return ret;
        }

        /// <summary>
        /// Intiate phoneme with phoneset instance.
        /// </summary>
        /// <param name="phoneSet">Phone set.</param>
        private void ParseData(TtsPhoneSet phoneSet)
        {
            if (phoneSet.Language != Language)
            {
                string message = Helper.NeutralFormat("The language [{0}] of phoneset " +
                    "does not match with that [{1}] of phoneme.",
                    Localor.LanguageToString(phoneSet.Language), Localor.LanguageToString(Language));
                throw new InvalidDataException(message);
            }

            _ttsPhoneSet = new Collection<string>();
            _ttsSonorantPhoneSet = new Collection<string>();
            _ttsVowelPhoneSet = new Collection<string>();
            _ttsPhoneIds = new Dictionary<string, int>();

            int toneCount = 0;
            foreach (Phone phone in phoneSet.Phones)
            {
                if (phone.IsNormal || phone.Features.Contains(PhoneFeature.Silence))
                {
                    _ttsPhoneSet.Add(phone.Name);
                }

                if (phone.IsVowel)
                {
                    _ttsVowelPhoneSet.Add(phone.Name);
                }

                if (phone.IsSonorant)
                {
                    _ttsSonorantPhoneSet.Add(phone.Name);
                }

                if (phone.HasFeature(PhoneFeature.Tone))
                {
                    _toneManager.Add(phone, ToneManager.ContextToneStartIndex + toneCount);
                    toneCount++;
                }

                _ttsPhoneIds.Add(phone.Name, phone.Id);
            }
        }

        /// <summary>
        /// Ensure collection loaded.
        /// </summary>
        /// <param name="map">Map to test.</param>
        /// <param name="fileName">Resource filename for the map.</param>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        private void EnsureMapLoaded(ICollection map, string fileName)
        {
            if (map == null || map.Count == 0)
            {
                using (StreamReader reader = Localor.LoadResource(Language, fileName))
                {
                    if (reader != null)
                    {
                        PhoneMap phoneMap = new PhoneMap();
                        phoneMap.Load(reader);
                        phoneMap.Validate();
                        map = ParseData(phoneMap);
                    }
                }
            }

            if (map == null || map.Count == 0)
            {
                Localor.ReportMissingStockedLanguageData(Localor.LanguageToString(Language), fileName);
            }
        }
        #endregion
    }
}