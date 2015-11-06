//----------------------------------------------------------------------------
// <copyright file="TtsUnitFeature.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements unit feature class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Utility;

    #region Feature dimensions definition

    /// <summary>
    /// Definition of position of unit at syllable level.
    /// </summary>
    public enum PosInSyllable
    {
        /// <summary>
        /// 0 First slice in syllable, usually consonant.
        /// </summary>
        Onset = 0,

        /// <summary>
        /// 1 Next to first slice in syllable, usually consonant.
        /// </summary>
        OnsetNext,

        /// <summary>
        /// 2 Middle slice in sllable, usually vowel, 
        /// Must has no Onset and no Coda in syllable.
        /// </summary>
        NucleusInV,

        /// <summary>
        /// 2 Middle slice in sllable, usually vowel,
        /// Must has no Onset but with Coda in syllable.
        /// </summary>
        NucleusInVC,

        /// <summary>
        /// 2 Middle slice in sllable, usually vowel
        /// Must has Onset but with no Coda in syllable.
        /// </summary>
        NucleusInCV,

        /// <summary>
        /// 2 Middle slice in sllable, usually vowel
        /// Must has both Onset and Coda in syllable.
        /// </summary>
        NucleusInCVC,

        /// <summary>
        /// 3 Previous to last slice in syllable, usually consonant.
        /// </summary>
        CodaNext,

        /// <summary>
        /// 4 Last slice in syllable, usually consonant.
        /// </summary>
        Coda
    }

    /// <summary>
    /// Definition of position of syllable at word level. 
    /// It describes the position information of slice/unit 
    /// Through the syllable it belongs to.
    /// </summary>
    public enum PosInWord
    {
        /// <summary>
        /// 0 At head position.
        /// </summary>
        Head = 0,

        /// <summary>
        /// 1 In the middle of word.
        /// </summary>
        Middle = 1,

        /// <summary>
        /// 2 At tail position.
        /// </summary>
        Tail = 2,

        /// <summary>
        /// 3 This word is mono-syllable word.
        /// </summary>
        Mono = 3
    }

    /// <summary>
    /// Definition of position of word in sentence level.
    /// This types are defined through preceding and following break level of the word.
    /// Left break level and right break level.
    /// </summary>
    public enum PosInSentence
    {
        /// <summary>
        /// 0 Left break level 1, right break level 1.
        /// </summary>
        L1R1 = 0,

        /// <summary>
        /// 1 Left break level 2, right break level 1.
        /// </summary>
        L2R1,

        /// <summary>
        /// 2 Left break level 3/4, right break level 1.
        /// </summary>
        L34R1,

        /// <summary>
        /// 3 Left break level 1, right break level 2.
        /// </summary>
        L1R2,

        /// <summary>
        /// 4 Left break level 2, right break level 2.
        /// </summary>
        L2R2,

        /// <summary>
        /// 5 Left break level 3/4, right break level 2.
        /// </summary>
        L34R2,

        /// <summary>
        /// 6 Left break level 1, right break level 3.
        /// </summary>
        L1R3,

        /// <summary>
        /// 7 Left break level 1, right break level 4.
        /// </summary>
        L1R4,

        /// <summary>
        /// 8 Left break level 2, right break level 3.
        /// </summary>
        L2R3,

        /// <summary>
        /// 9 Left break level 2, right break level 4.
        /// </summary>
        L2R4,

        /// <summary>
        /// 10 Left break level 3/4, right break level 3/4.
        /// </summary>
        L34R34,

        /// <summary>
        /// 11 This is question mark.
        /// </summary>
        Quest
    }

    /// <summary>
    /// Definition of stress level for syllable.
    /// </summary>
    public enum TtsStress
    {
        /// <summary>
        /// 0 Not stress level appied.
        /// </summary>
        None = 0,

        /// <summary>
        /// 1 Primary stress level.
        /// </summary>
        Primary,

        /// <summary>
        /// 2 Second stress level.
        /// </summary>
        Secondary,

        /// <summary>
        /// 3 Third stress level.
        /// </summary>
        Tertiary
    }

    /// <summary>
    /// Definition of Emphasis.
    /// </summary>
    public enum TtsEmphasis
    {
        /// <summary>
        /// 0 No emphasis applied.
        /// </summary>
        None = 0,

        /// <summary>
        /// 1 Apply emphasis.
        /// </summary>
        Yes
    }

    /// <summary>
    /// Break.
    /// </summary>
    public enum TtsBreak
    {
        /// <summary>
        /// 0 Phone.
        /// </summary>
        Phone = 0,

        /// <summary>
        /// 1 Break level 1.
        /// </summary>
        Syllable,

        /// <summary>
        /// 2 Between words.
        /// </summary>
        Word,

        /// <summary>
        /// 3 Between prosodic phrases.
        /// </summary>
        InterPhrase,

        /// <summary>
        /// 4 Between ntonation phrases.
        /// </summary>
        IntonationPhrase,

        /// <summary>
        /// 5 Between sentence.
        /// </summary>
        Sentence
    }

    /// <summary>
    /// Break.
    /// </summary>
    public enum TtsWordTone
    {
        /// <summary>
        /// 0, c, no perceptual pitch movement at the boundary.
        /// </summary>
        Continue = 0,

        /// <summary>
        /// 1, R, a clear pitch excursion into the higher portion of
        /// The speaker�s pitch range, as at the end of
        /// A typical American English yes/no question. 
        /// </summary>
        FullRise,

        /// <summary>
        /// 2, r, A minor, yet, perceptible pitch excursion up at a phrase boundary.
        /// </summary>
        MinorRise,

        /// <summary>
        /// 3, F, a clear pitch excursion into the lower portion of
        /// The speaker�s pitch range, as at the end of
        /// A typical American English declarative statement .
        /// </summary>
        FullFall,

        /// <summary>
        /// 4, f, A minor, yet, perceptible pitch excursion down at a phrase boundary.
        /// </summary>
        MinorFall,

        /// <summary>
        /// 5, A strict rise, in the word tone prediction of the run-time engine,
        /// The last word of question sentence will be set to strict rise.
        /// </summary>
        StrictRise,

        /// <summary>
        /// 6, A strict fall, in the word tone prediction of the run-time engine,
        /// The last word of non-question sentence will be set to strict fall.
        /// </summary>
        StrictFall
    }

    /// <summary>
    /// Tobi Accent.
    /// </summary>
    public enum TtsTobiAccent
    {
        /// <summary>
        /// 0, for non accent.
        /// </summary>
        NoAccent = 0,

        /// <summary>
        /// 1, for H*.
        /// </summary>
        HighStar,

        /// <summary>
        /// 2, for L*.
        /// </summary>
        LowStar,

        /// <summary>
        /// 3, for L*+H (late rise).
        /// </summary>
        LowStarHigh,

        /// <summary>
        /// 4, for Rising Step? (Not clear, just followed TTS_TOBI_ACCENT in ttsdatadef.h);.
        /// </summary>
        RisingStar,

        /// <summary>
        /// 5, for L+H* (early rise).
        /// </summary>
        LowHighStar, 

        /// <summary>
        /// 6, for down step? Not clear, just followed TTS_TOBI_ACCENT in ttsdatadef.h);.
        /// </summary>
        DownHighStar,

        /// <summary>
        /// 7, for H*+L*.
        /// </summary>
        HighStarLowStar,

        /// <summary>
        /// 8, for L+!H*.
        /// </summary>
        LowDownHighStar,
    }
    
    /// <summary>
    /// Tobi Boundary.
    /// </summary>
    public enum TtsTobiBoundary
    {
        /// <summary>
        /// Default value. No boundary tone.
        /// </summary>
        NoBoundaryTone = 0,

        /// <summary>
        /// L-.
        /// </summary>
        LMinus = 1000,

        /// <summary>
        /// H-.
        /// </summary>
        HMinus,

        /// <summary>
        /// L-L%.
        /// </summary>
        LMinusLPerc,

        /// <summary>
        /// L-H%.
        /// </summary>
        LMinusHPerc,

        /// <summary>
        /// H-H%.
        /// </summary>
        HMinusHPerc,

        /// <summary>
        /// H-L%.
        /// </summary>
        HMinusLPerc,

        /// <summary>
        /// S-.
        /// </summary>
        SMinus,
    }

    /// <summary>
    /// Tts features.
    /// </summary>
    public enum TtsFeature
    {
        /// <summary>
        /// 0 Index of position in sentence.
        /// </summary>
        PosInSentence = 0,

        /// <summary>
        /// 1 Index of position in word.
        /// </summary>
        PosInWord = 1,

        /// <summary>
        /// 2 Index of position in syllable.
        /// </summary>
        PosInSyllable = 2,

        /// <summary>
        /// 3 Index of left context phone id.
        /// </summary>
        LeftContextPhone = 3,

        /// <summary>
        /// 4 Index of right context phone id.
        /// </summary>
        RightContextPhone = 4,

        /// <summary>
        /// 5 Index of left context tone id.
        /// </summary>
        LeftContextTone = 5,

        /// <summary>
        /// 6 Index of right context tone id.
        /// </summary>
        RightContextTone = 6,

        /// <summary>
        /// 7 Index of tts stress.
        /// </summary>
        TtsStress = 7,

        /// <summary>
        /// 8 Index of tts emphasis.
        /// </summary>
        TtsEmphasis = 8,

        /// <summary>
        /// 9 Index of tts word tone.
        /// </summary>
        TtsWordTone = 9,

        /// <summary>
        /// 10 Index of tts neighbor previous.
        /// </summary>
        TtsNeighborPrev = 10,

        /// <summary>
        /// 11 Index of tts energy feature.
        /// </summary>
        TtsEnergy = 11,

        /// <summary>
        /// 12 Index of tts normalized average epoch feature.
        /// </summary>
        TtsNormalizedEpoch = 12
    }

    /// <summary>
    /// Liaison.
    /// </summary>
    public enum TtsLiaison
    {
        /// <summary>
        /// Default rule.
        /// </summary>
        Default,

        /// <summary>
        /// Explicit labelled with liaison mark.
        /// </summary>
        Labelled
    }

    /// <summary>
    /// Pronunciation source.
    /// </summary>
    public enum TtsPronSource
    {
        /// <summary>
        /// 0 No pronunciation.
        /// </summary>
        None = 0,

        /// <summary>
        /// 1 The pronunciation is from main lexicon.
        /// </summary>
        MainLexicon,

        /// <summary>
        /// 2 The pronunciation is from optional morphology analysis.
        /// </summary>
        Mophology,

        /// <summary>
        /// 3 The pronunciation is from domain lexicon.
        /// </summary>
        DomainLexicon,

        /// <summary>
        /// 4 The pronunciation is from voice specific lexicon.
        /// </summary>
        VoiceLexicon,

        /// <summary>
        /// 5 The pronunciation is from custom lexicon.
        /// </summary>
        CustomLexicon,

        /// <summary>
        /// 6 The pronunciation is from LTS.
        /// </summary>
        LTS,

        /// <summary>
        /// 7 The pronunciation is from Domain LTS.
        /// </summary>
        DomainLTS,

        /// <summary>
        /// 8 The pronunciation is from Char Table and read letter-by-letter.
        /// </summary>
        Spelling,

        /// <summary>
        /// 9 The pronunciation is from compound module.
        /// </summary>
        Compound,

        /// <summary>
        /// 10 The pronunciation is from other language like English.
        /// </summary>
        ExtraLanguage,

        /// <summary>
        /// 11 The pronunciation is from main polyphony.
        /// </summary>
        MainPolyphony,

        /// <summary>
        /// 12 The pronunciation is from domain polyphony.
        /// </summary>
        DomainPolyphony,

        /// <summary>
        /// 13 The pronunciation is from pronunciation change.
        /// </summary>
        PronunciationChange,

        /// <summary>
        /// 14 The pronunciation is from OOV handler pronunciation in lochander.
        /// </summary>
        OovLochandler,

        /// <summary>
        /// 15 The pronunciation is from Foreign name handling.
        /// </summary>
        ForeignName,

        /// <summary>
        /// 16 The pronunciation is from Foreign LTS handling.
        /// </summary>
        ForeignLTS,

        /// <summary>
        /// 17 The pronunciation is from post pronunciation function in lochander.
        /// </summary>
        PostPronLochandler,

        /// <summary>
        /// 18 The pronunciation is from XML input.
        /// </summary>
        XmlTag,

        /// <summary>
        /// 19 The pronunciation is from RNN polyphony model.
        /// </summary>
        PolyphonyRNNModel,

        /// <summary>
        /// 20 The pronunciation is from CRF polyphony model.
        /// </summary>
        PolyphonyCRFModel,

        /// <summary>
        /// 21 The pronunciation is from other sources.
        /// </summary>
        Other
    }

    #endregion

    /// <summary>
    /// Class for unit feature dimensions.
    /// </summary>
    public class TtsUnitFeature
    {
        #region Fields

        private PosInSyllable _posInSyllable;
        private PosInWord _posInWord;
        private PosInSentence _posInSentence;
        private int _leftContextPhone;
        private int _rightContextPhone;
        private int _leftContextTone;
        private int _rightContextTone;
        private TtsStress _ttsStress;
        private TtsEmphasis _ttsEmphasis;
        private TtsWordTone _ttsWordTone;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Position in syllable of unit.
        /// </summary>
        public PosInSyllable PosInSyllable
        {
            get
            {
                return _posInSyllable;
            }

            set
            {
                Helper.ValidateEnumValue(typeof(PosInSyllable), (int)value);
                _posInSyllable = value;
            }
        }

        /// <summary>
        /// Gets or sets Position in word of syllable.
        /// </summary>
        public PosInWord PosInWord
        {
            get
            {
                return _posInWord;
            }

            set
            {
                Helper.ValidateEnumValue(typeof(PosInWord), (int)value);
                _posInWord = value;
            }
        }

        /// <summary>
        /// Gets or sets Position in sentence of word.
        /// </summary>
        public PosInSentence PosInSentence
        {
            get
            {
                return _posInSentence;
            }

            set
            {
                Helper.ValidateEnumValue(typeof(PosInSentence), (int)value);
                _posInSentence = value;
            }
        }

        /// <summary>
        /// Gets or sets Left context phone id of current unit.
        /// </summary>
        public int LeftContextPhone
        {
            get { return _leftContextPhone; }
            set { _leftContextPhone = value; }
        }

        /// <summary>
        /// Gets or sets Right context phone id of current unit.
        /// </summary>
        public int RightContextPhone
        {
            get { return _rightContextPhone; }
            set { _rightContextPhone = value; }
        }

        /// <summary>
        /// Gets or sets Left context tone id of current unit.
        /// </summary>
        public int LeftContextTone
        {
            get { return _leftContextTone; }
            set { _leftContextTone = value; }
        }

        /// <summary>
        /// Gets or sets Right context tone id of current unit.
        /// </summary>
        public int RightContextTone
        {
            get { return _rightContextTone; }
            set { _rightContextTone = value; }
        }

        /// <summary>
        /// Gets or sets Stress.
        /// </summary>
        public TtsStress TtsStress
        {
            get
            {
                return _ttsStress;
            }

            set
            {
                Helper.ValidateEnumValue(typeof(TtsStress), (int)value);
                _ttsStress = value;
            }
        }

        /// <summary>
        /// Gets or sets Emphasis.
        /// </summary>
        public TtsEmphasis TtsEmphasis
        {
            get
            {
                return _ttsEmphasis;
            }

            set
            {
                Helper.ValidateEnumValue(typeof(TtsEmphasis), (int)value);
                _ttsEmphasis = value;
            }
        }

        /// <summary>
        /// Gets or sets WordTone.
        /// </summary>
        public TtsWordTone TtsWordTone
        {
            get
            {
                return _ttsWordTone;
            }

            set
            {
                Helper.ValidateEnumValue(typeof(TtsWordTone), (int)value);
                _ttsWordTone = value;
            }
        }

        /// <summary>
        /// Gets Feature value specified by index.
        /// </summary>
        /// <param name="index">Index of which feature to look.</param>
        /// <returns>Feature value.</returns>
        public int this[int index]
        {
            get
            {
                if (index > (int)TtsFeature.TtsWordTone || index < 0)
                {
                    throw new ArgumentOutOfRangeException(index.ToString(CultureInfo.InvariantCulture));
                }

                int featureValue = 0;

                switch (index)
                {
                    case 0:
                        featureValue = (int)PosInSentence;
                        break;
                    case 1:
                        featureValue = (int)PosInWord;
                        break;
                    case 2:
                        featureValue = (int)PosInSyllable;
                        break;
                    case 3:
                        featureValue = LeftContextPhone;
                        break;
                    case 4:
                        featureValue = RightContextPhone;
                        break;
                    case 5:
                        featureValue = LeftContextTone;
                        break;
                    case 6:
                        featureValue = RightContextTone;
                        break;
                    case 7:
                        featureValue = (int)TtsStress;
                        break;
                    case 8:
                        featureValue = (int)TtsEmphasis;
                        break;
                    case 9:
                        featureValue = (int)TtsWordTone;
                        break;
                    default:
                        System.Diagnostics.Debug.Assert(false);
                        break;
                }

                return featureValue;
            }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Whether is vowel for position in syllable.
        /// </summary>
        /// <param name="posInSyllable">Position in syllable.</param>
        /// <returns>True for vowel.</returns>
        public static bool IsVowel(PosInSyllable posInSyllable)
        {
            return posInSyllable == PosInSyllable.NucleusInV ||
                posInSyllable == PosInSyllable.NucleusInVC ||
                posInSyllable == PosInSyllable.NucleusInCV ||
                posInSyllable == PosInSyllable.NucleusInCVC;
        }

        /// <summary>
        /// Parse all of unit feature from string line presentation.
        /// </summary>
        /// <param name="value">String line presentation.</param>
        public void Parse(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException("value");
            }

            string[] values = value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            Parse(values, 0);
        }

        /// <summary>
        /// Parse all of unit feature from string array, start from offset.
        /// </summary>
        /// <param name="values">String array of fields.</param>
        /// <param name="offset">Start offset of array to take data.</param>
        public void Parse(string[] values, int offset)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            if (offset < 0 || offset >= values.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            System.Diagnostics.Debug.Assert(values.Length > (int)TtsFeature.TtsWordTone);

            this.PosInSyllable = (PosInSyllable)int.Parse(values[(int)TtsFeature.PosInSyllable + offset],
                CultureInfo.InvariantCulture);
            this.PosInWord = (PosInWord)int.Parse(values[(int)TtsFeature.PosInWord + offset],
                CultureInfo.InvariantCulture);
            this.PosInSentence = (PosInSentence)int.Parse(values[(int)TtsFeature.PosInSentence + offset],
                CultureInfo.InvariantCulture);

            this.LeftContextPhone = int.Parse(values[(int)TtsFeature.LeftContextPhone + offset],
                CultureInfo.InvariantCulture);
            this.RightContextPhone = int.Parse(values[(int)TtsFeature.RightContextPhone + offset],
                CultureInfo.InvariantCulture);

            this.LeftContextTone = int.Parse(values[(int)TtsFeature.LeftContextTone + offset],
                CultureInfo.InvariantCulture);
            this.RightContextTone = int.Parse(values[(int)TtsFeature.RightContextTone + offset],
                CultureInfo.InvariantCulture);

            this.TtsStress = (TtsStress)int.Parse(values[(int)TtsFeature.TtsStress + offset],
                CultureInfo.InvariantCulture);
            this.TtsEmphasis = (TtsEmphasis)int.Parse(values[(int)TtsFeature.TtsEmphasis + offset],
                CultureInfo.InvariantCulture);

            this.TtsWordTone = (TtsWordTone)int.Parse(values[(int)TtsFeature.TtsWordTone + offset],
                CultureInfo.InstalledUICulture);
        }

        #endregion
    }

    /// <summary>
    /// Class for unit acoustic feature dimensions.
    /// </summary>
    public class TtsAcousticFeature
    {
        #region Fields

        private float _startTime;
        private float _duration;
        private int _sampleOffset;
        private int _sampleLength;
        private int _epochOffset;
        private int _epochLength;
        private float _cartRms;
        private float _energyRms;
        private float _energy;
        private float _averagePitch;
        private float _pitchRange;
        private int _epoch16KCompressLength;
        private int _epoch8KCompressLength;
        private int _sample8KLength;
        private float _averageEpoch;
        private float _normalizedEpoch;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Start time of unit wave segment.
        /// </summary>
        public float StartTime
        {
            get { return _startTime; }
            set { _startTime = value; }
        }

        /// <summary>
        /// Gets or sets Time duration of unit wave segment.
        /// </summary>
        public float Duration
        {
            get { return _duration; }
            set { _duration = value; }
        }

        /// <summary>
        /// Gets or sets Sample offset.
        /// </summary>
        public int SampleOffset
        {
            get { return _sampleOffset; }
            set { _sampleOffset = value; }
        }

        /// <summary>
        /// Gets or sets Sample length.
        /// </summary>
        public int SampleLength
        {
            get { return _sampleLength; }
            set { _sampleLength = value; }
        }

        /// <summary>
        /// Gets or sets Epoch offset.
        /// </summary>
        public int EpochOffset
        {
            get { return _epochOffset; }
            set { _epochOffset = value; }
        }

        /// <summary>
        /// Gets or sets Epoch length.
        /// </summary>
        public int EpochLength
        {
            get { return _epochLength; }
            set { _epochLength = value; }
        }

        /// <summary>
        /// Gets or sets Root mean square for build cart tree.
        /// </summary>
        public float CartRms
        {
            get { return _cartRms; }
            set { _cartRms = value; }
        }

        /// <summary>
        /// Gets or sets Root mean square for energy feature.
        /// </summary>
        public float EnergyRms
        {
            get { return _energyRms; }
            set { _energyRms = value; }
        }

        /// <summary>
        /// Gets or sets Energy feature.
        /// </summary>
        public float Energy
        {
            get { return _energy; }
            set { _energy = value; }
        }

        /// <summary>
        /// Gets or sets Average pitch.
        /// </summary>
        public float AveragePitch
        {
            get { return _averagePitch; }
            set { _averagePitch = value; }
        }

        /// <summary>
        /// Gets or sets Pitch range.
        /// </summary>
        public float PitchRange
        {
            get { return _pitchRange; }
            set { _pitchRange = value; }
        }

        /// <summary>
        /// Gets or sets Epoch length for compressed 16K epoch data.
        /// </summary>
        public int Epoch16KCompressLength
        {
            get { return _epoch16KCompressLength; }
            set { _epoch16KCompressLength = value; }
        }

        /// <summary>
        /// Gets or sets Epoch length for compressed 8K epoch data.
        /// </summary>
        public int Epoch8KCompressLength
        {
            get { return _epoch8KCompressLength; }
            set { _epoch8KCompressLength = value; }
        }

        /// <summary>
        /// Gets or sets Sample length for 8K wave data.
        /// </summary>
        public int Sample8KLength
        {
            get { return _sample8KLength; }
            set { _sample8KLength = value; }
        }

        /// <summary>
        /// Gets or sets Average epoch.
        /// </summary>
        public float AverageEpoch
        {
            get { return _averageEpoch; }
            set { _averageEpoch = value; }
        }

        /// <summary>
        /// Gets or sets Normalized epoch feature.
        /// </summary>
        public float NormalizedEpoch
        {
            get { return _normalizedEpoch; }
            set { _normalizedEpoch = value; }
        }

        #endregion
    }
}