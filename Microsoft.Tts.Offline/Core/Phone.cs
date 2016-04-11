//----------------------------------------------------------------------------
// <copyright file="Phone.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements TTS Phone
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;

    /// <summary>
    /// TTS phone features.
    /// </summary>
    [FlagsAttribute]
    public enum PhoneFeature : long
    {
        #region linguisted features
        /// <summary>
        /// Vowel.
        /// </summary>
        Vowel = (1 << 0),

        /// <summary>
        /// Consonant.
        /// </summary>
        Consonant = (1 << 1),

        /// <summary>
        /// Sonorant.
        /// </summary>
        Sonorant = (1 << 2),

        /// <summary>
        /// Voiced.
        /// </summary>
        Voiced = (1 << 3),

        /// <summary>
        /// Aspirated.
        /// </summary>
        Aspirated = (1 << 4),

        /// <summary>
        /// Plosive.
        /// </summary>
        Plosive = (1 << 5),

        /// <summary>
        /// Fricative.
        /// </summary>
        Fricative = (1 << 6),

        /// <summary>
        /// Affricate.
        /// </summary>
        Affricate = (1 << 7),

        /// <summary>
        /// Nasal.
        /// </summary>
        Nasal = (1 << 8),

        /// <summary>
        /// Liquid.
        /// </summary>
        Liquid = (1 << 9),

        /// <summary>
        /// Glide.
        /// </summary>
        Glide = (1 << 10),

        /// <summary>
        /// Bilabial.
        /// </summary>
        Bilabial = (1 << 11),

        /// <summary>
        /// Labiodental.
        /// </summary>
        Labiodental = (1 << 12),

        /// <summary>
        /// Dental.
        /// </summary>
        Dental = (1 << 13),

        /// <summary>
        /// Alveolar.
        /// </summary>
        Alveolar = (1 << 14),

        /// <summary>
        /// Palatal.
        /// </summary>
        Palatal = (1 << 15),

        /// <summary>
        /// Velar.
        /// </summary>
        Velar = (1 << 16),

        /// <summary>
        /// Glottal.
        /// </summary>
        Glottal = (1 << 17),

        /// <summary>
        /// High.
        /// </summary>
        High = (1 << 18),

        /// <summary>
        /// MidHeight.
        /// </summary>
        MidHeight = (1 << 19),

        /// <summary>
        /// Low.
        /// </summary>
        Low = (1 << 20),

        /// <summary>
        /// MidLow.
        /// </summary>
        MidLow = Low,

        /// <summary>
        /// Front.
        /// </summary>
        Front = (1 << 21),

        /// <summary>
        /// Central.
        /// </summary>
        Central = (1 << 22),

        /// <summary>
        /// Back.
        /// </summary>
        Back = (1 << 23),

        /// <summary>
        /// Round.
        /// </summary>
        Round = (1 << 24),

        /// <summary>
        /// Short.
        /// </summary>
        Short = (1 << 25),

        /// <summary>
        /// Long.
        /// </summary>
        Long = (1 << 26),

        /// <summary>
        /// Diphthong.
        /// </summary>
        Diphthong = (1 << 27),

        /// <summary>
        /// Approximant.
        /// </summary>
        Approximant = Liquid | Glide,

        /// <summary>
        /// Trill.
        /// </summary>
        Trill = ((long)1 << 33),

        /// <summary>
        /// Tap.
        /// </summary>
        Tap = ((long)1 << 34),

        /// <summary>
        /// Lateral.
        /// </summary>
        Lateral = ((long)1 << 35),

        /// <summary>
        /// Postalveolar.
        /// </summary>
        Postalveolar = ((long)1 << 36),

        /// <summary>
        /// Retroflex.
        /// </summary>
        Retroflex = ((long)1 << 37),

        /// <summary>
        /// Uvular.
        /// </summary>
        Uvular = ((long)1 << 38),

        /// <summary>
        /// Pharyngeal.
        /// </summary>
        Pharyngeal = ((long)1 << 39),

        #endregion

        #region TTS specific features
        /// <summary>
        /// Syllable: word boundary and syllable boundary.
        /// </summary>
        Syllable = (1 << 30),
        
        /// <summary>
        /// Main stress.
        /// </summary>
        MainStress = (1 << 28),

        /// <summary>
        /// Sub-stress.
        /// </summary>
        SubStress = (1 << 29),

        /// <summary>
        /// Silence, this feature is not used in runtime.
        /// </summary>
        Silence = 0,

        /// <summary>
        /// Tone.
        /// </summary>
        Tone = (long)1 << 31,

        /// <summary>
        /// Short pause, this feature is not used in runtime.
        /// </summary>
        ShortPause = (long)1 << 32,

        #endregion
    }

    /// <summary>
    /// Phone.
    /// </summary>
    public class Phone : IComparable<Phone>
    {
        #region Fields
        private string _name;
        private string _compilingName;
        private int _id;
        private Collection<PhoneFeature> _features = new Collection<PhoneFeature>();
        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="Phone"/> class.
        /// </summary>
        /// <param name="name">Phone name.</param>
        public Phone(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Phone"/> class.
        /// </summary>
        /// <param name="name">Phone name.</param>
        /// <param name="id">Phone id.</param>
        public Phone(string name, int id)
            : this(name)
        {
            this.Id = id;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Phone"/> class.
        /// </summary>
        /// <param name="name">Phone name.</param>
        /// <param name="id">Phone id.</param>
        /// <param name="feature">Phone feature.</param>
        public Phone(string name, int id, PhoneFeature feature)
            : this(name, id)
        {
            _features.Add(feature);
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Phone name.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _name = Phoneme.ToOffline(value).ToLowerInvariant();
                _compilingName = Phoneme.ToRuntime(value).ToUpperInvariant();
            }
        }

        /// <summary>
        /// Gets Name used for compiling, especially for silence phone "-sil-", "-sp-".
        /// </summary>
        public string CompilingName
        {
            get
            {
                return _compilingName;
            }
        }

        /// <summary>
        /// Gets or sets Numerical id of this phone instance.
        /// </summary>
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Gets Features of this phone.
        /// </summary>
        public Collection<PhoneFeature> Features
        {
            get { return _features; }
        }

        /// <summary>
        /// Gets Feature id.
        /// </summary>
        public long FeatureId
        {
            get
            {
                long id = 0;
                foreach (PhoneFeature feature in _features)
                {
                    id |= (long)feature;
                }

                return id;
            }
        }

        /// <summary>
        /// Gets a value indicating whether Vowel.
        /// </summary>
        public bool IsVowel
        {
            get { return _features.Contains(PhoneFeature.Vowel); }
        }

        /// <summary>
        /// Gets a value indicating whether Consonant.
        /// </summary>
        public bool IsConsonant
        {
            get { return _features.Contains(PhoneFeature.Consonant); }
        }

        /// <summary>
        /// Gets a value indicating whether Sonorant.
        /// </summary>
        public bool IsSonorant
        {
            get { return _features.Contains(PhoneFeature.Sonorant); }
        }

        /// <summary>
        /// Gets a value indicating whether Nasal.
        /// </summary>
        public bool IsNasal
        {
            get { return _features.Contains(PhoneFeature.Nasal); }
        }

        /// <summary>
        /// Gets a value indicating whether voiced.
        /// </summary>
        public bool IsVoiced
        {
            get { return _features.Contains(PhoneFeature.Voiced); }
        }

        /// <summary>
        /// Gets a value indicating whether Normal phone: vowel or consonant.
        /// </summary>
        public bool IsNormal
        {
            get { return IsVowel || IsConsonant; }
        }

        /// <summary>
        /// Gets a value indicating whether is stress.
        /// </summary>
        public bool IsStress
        {
            get 
            { 
                return _features.Contains(PhoneFeature.MainStress) ||
                _features.Contains(PhoneFeature.SubStress); 
            }
        }

        /// <summary>
        /// Gets a value indicating whether is syllable boundary.
        /// </summary>
        public bool IsSyllableBoundary
        {
            get
            {
                return _features.Contains(PhoneFeature.Syllable);
            }
        }

        /// <summary>
        /// Gets a value indicating whether is silence.
        /// </summary>
        public bool IsSilenceFeature
        {
            get
            {
                return _features.Contains(PhoneFeature.Silence);
            }
        }

        /// <summary>
        /// Gets a value indicating whether is short pause.
        /// </summary>
        public bool IsShortPauseFeature
        {
            get
            {
                return _features.Contains(PhoneFeature.ShortPause);
            }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Tell whether current phone has given feature.
        /// </summary>
        /// <param name="feature">Feature.</param>
        /// <returns>True if yes, otherwise false.</returns>
        public bool HasFeature(PhoneFeature feature)
        {
            return _features.Contains(feature);
        }

        /// <summary>
        /// Add feature string into phone.
        /// </summary>
        /// <param name="feature">Feature string.</param>
        /// <returns>True if the feature exists.</returns>
        public bool AddFeature(string feature)
        {
            bool foundFeature = false;
            foreach (string featureName in Enum.GetNames(typeof(PhoneFeature)))
            {
                if (featureName.Equals(feature, StringComparison.OrdinalIgnoreCase))
                {
                    foundFeature = true;
                    break;
                }
            }

            if (foundFeature)
            {
                Features.Add((PhoneFeature)Enum.Parse(typeof(PhoneFeature), feature, true));
            }

            return foundFeature;
        }

        #endregion

        #region IComparable<TtsPhone> Members

        /// <summary>
        /// Compare this instance with other instance. It will compare by Name first, and then Id.
        /// </summary>
        /// <param name="other">Other instance to compare with.</param>
        /// <returns>Comparing result.</returns>
        public int CompareTo(Phone other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            int ret = 0;
            ret = StringComparer.Ordinal.Compare(
                this.CompilingName, other.CompilingName);
            if (ret == 0)
            {
                ret = this.Id.CompareTo(other.Id);
            }

            return ret;
        }

        #endregion
    }
}