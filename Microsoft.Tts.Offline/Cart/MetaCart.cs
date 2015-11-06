//----------------------------------------------------------------------------
// <copyright file="MetaCart.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements MetaCart
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Cart
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// MetaCart, manage data for CART.
    /// </summary>
    public class MetaCart
    {
        #region Fields

        private Dictionary<string, MetaFeature> _namedMetaFeatures = new Dictionary<string, MetaFeature>();

        private Dictionary<int, MetaFeature> _indexedMetaFeatures = new Dictionary<int, MetaFeature>();
        private Dictionary<int, Feature> _features = new Dictionary<int, Feature>();
        private Language _language;
        private EngineType _engineType;

        #endregion

        #region Constructions

        /// <summary>
        /// Initializes a new instance of the <see cref="MetaCart"/> class.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <param name="engineType">Engine type.</param>
        public MetaCart(Language language, EngineType engineType)
        {
            _language = language;
            _engineType = engineType;

            InitializeFeatureMeta();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets EngineType.
        /// </summary>
        public EngineType EngineType
        {
            get { return _engineType; }
        }

        /// <summary>
        /// Gets Language.
        /// </summary>
        public Language Language
        {
            get { return _language; }
        }

        /// <summary>
        /// Gets Features indexed by name.
        /// </summary>
        public Dictionary<string, MetaFeature> NamedMetaFeatures
        {
            get { return _namedMetaFeatures; }
        }

        #endregion

        #region Static fields

        /// <summary>
        /// Gets Features indexed by Id.
        /// </summary>
        public Dictionary<int, MetaFeature> IndexedMetaFeatures
        {
            get { return _indexedMetaFeatures; }
        }

        /// <summary>
        /// Gets All features.
        /// </summary>
        public Dictionary<int, Feature> Features
        {
            get { return _features; }
        }

        #endregion

        #region Loading & saving

        /// <summary>
        /// Convert to byte array.
        /// </summary>
        /// <returns>Byte array.</returns>
        public byte[] ToBytes()
        {
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(Features.Count));

            foreach (int index in Features.Keys)
            {
                Feature feature = Features[index];
                data.AddRange(BitConverter.GetBytes(2 + feature.Values.Count));
                data.AddRange(BitConverter.GetBytes(index));
                data.AddRange(BitConverter.GetBytes(feature.MetaFeatureIndex));

                foreach (int value in feature.Values)
                {
                    data.AddRange(BitConverter.GetBytes(value));
                }
            }

            return data.ToArray();
        }

        /// <summary>
        /// Initialize MetaCart question data from CRT file, which is 
        /// Compitiable with Mulan.
        /// <param />
        /// Data format:
        /// NumberOfFeature
        /// NumberCount FeatureId MetaFeatureId (FeatureValueId)+.
        /// </summary>
        /// <param name="br">Binary sream to load.</param>
        public void Initialize(BinaryReader br)
        {
            if (br == null)
            {
                throw new ArgumentNullException("br");
            }

            Features.Clear();

            try
            {
                int questionCount = br.ReadInt32();
                for (int i = 0; i < questionCount; i++)
                {
                    int numberCount = br.ReadInt32();

                    Feature quest = new Feature(this);
                    quest.Index = br.ReadInt32();
                    quest.MetaFeatureIndex = br.ReadInt32();

                    for (int j = 0; j < numberCount - 2; j++)
                    {
                        // feature values
                        int valueId = br.ReadInt32();
                        quest.Values.Add(valueId);
                    }

                    Features.Add(quest.Index, quest);
                }
            }
            catch (EndOfStreamException ese)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Fail to read features from binary stream for invalid data.");
                throw new InvalidDataException(message, ese);
            }
        }

        /// <summary>
        /// Initialize questions in feature set
        /// <param />
        /// Load questions/features from file, question string file is 
        /// In string format, not in phoneId/indexing format.
        /// </summary>
        /// <param name="questionFilePath">Question file path.</param>
        public void Initialize(string questionFilePath)
        {
            Features.Clear();

            Phoneme phoneme = Localor.GetPhoneme(_language, _engineType);
            using (StreamReader sr = new StreamReader(questionFilePath))
            {
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    string[] items = line.Split(new char[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

                    Feature quest = new Feature(this);
                    quest.Index = int.Parse(items[0], CultureInfo.InvariantCulture);
                    TtsFeature feature = (TtsFeature)Enum.Parse(typeof(TtsFeature), items[1]);
                    quest.MetaFeatureIndex = (int)feature;

                    string[] values = items[2].Split(new char[] { ',' },
                        StringSplitOptions.RemoveEmptyEntries);

                    StringBuilder sb = new StringBuilder();
                    foreach (string val in values)
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append(",");
                        }

                        int valId = 0;
                        switch (feature)
                        {
                            case TtsFeature.PosInSentence:
                                PosInSentence pis = (PosInSentence)Enum.Parse(typeof(PosInSentence), val);
                                valId = (int)pis;
                                break;
                            case TtsFeature.PosInWord:
                                PosInWord piw = (PosInWord)Enum.Parse(typeof(PosInWord), val);
                                valId = (int)piw;
                                break;
                            case TtsFeature.PosInSyllable:
                                PosInSyllable piy = (PosInSyllable)Enum.Parse(typeof(PosInSyllable), val);
                                valId = (int)piy;
                                break;
                            case TtsFeature.LeftContextPhone:
                                valId = phoneme.TtsPhone2Id(val);
                                break;
                            case TtsFeature.RightContextPhone:
                                valId = phoneme.TtsPhone2Id(val);
                                break;
                            case TtsFeature.LeftContextTone:
                                valId = phoneme.ToneManager.GetToneId(val);
                                break;
                            case TtsFeature.RightContextTone:
                                valId = phoneme.ToneManager.GetToneId(val);
                                break;
                            case TtsFeature.TtsStress:
                                TtsStress stress = (TtsStress)Enum.Parse(typeof(TtsStress), val);
                                valId = (int)stress;
                                break;
                            case TtsFeature.TtsEmphasis:
                                TtsEmphasis emphasis = (TtsEmphasis)Enum.Parse(typeof(TtsEmphasis), val);
                                valId = (int)emphasis;
                                break;
                            default:
                                break;
                        }

                        quest.Values.Add(valId);
                    }

                    Features.Add(quest.Index, quest);
                }
            }
        }

        /// <summary>
        /// Save meta cart data into string question file.
        /// </summary>
        /// <param name="questionFilePath">Target question file to save.</param>
        public void Save(string questionFilePath)
        {
            Phoneme phoneme = Localor.GetPhoneme(_language, _engineType);
            using (StreamWriter sw = new StreamWriter(questionFilePath, false, Encoding.ASCII))
            {
                foreach (int quesIndex in Features.Keys)
                {
                    Feature quest = Features[quesIndex];

                    TtsFeature feature = (TtsFeature)quest.MetaFeatureIndex;
                    sw.Write("{0} {1}", quesIndex, feature.ToString());
                    switch (feature)
                    {
                        case TtsFeature.PosInSentence:
                            SaveValues(sw, quest, typeof(PosInSentence));
                            break;
                        case TtsFeature.PosInWord:
                            SaveValues(sw, quest, typeof(PosInWord));
                            break;
                        case TtsFeature.PosInSyllable:
                            SaveValues(sw, quest, typeof(PosInSyllable));
                            break;
                        case TtsFeature.LeftContextPhone:
                            foreach (int value in quest.Values)
                            {
                                sw.Write(" {0}", phoneme.TtsId2Phone(value));
                            }

                            break;
                        case TtsFeature.RightContextPhone:
                            foreach (int value in quest.Values)
                            {
                                sw.Write(" {0}", phoneme.TtsId2Phone(value));
                            }

                            break;
                        case TtsFeature.LeftContextTone:
                            foreach (int value in quest.Values)
                            {
                                sw.Write(" {0}", phoneme.ToneManager.GetNameFromContextId(value));
                            }

                            break;
                        case TtsFeature.RightContextTone:
                            foreach (int value in quest.Values)
                            {
                                sw.Write(" {0}", phoneme.ToneManager.GetNameFromContextId(value));
                            }

                            break;
                        case TtsFeature.TtsStress:
                            SaveValues(sw, quest, typeof(TtsStress));
                            break;
                        case TtsFeature.TtsEmphasis:
                            SaveValues(sw, quest, typeof(TtsFeature));
                            break;
                        default:
                            break;
                    }

                    sw.WriteLine();
                }
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Save values.
        /// </summary>
        /// <param name="sw">Stream writer to save.</param>
        /// <param name="quest">Question.</param>
        /// <param name="enumType">Type of the values in the question.</param>
        private static void SaveValues(StreamWriter sw, Feature quest, Type enumType)
        {
            foreach (int value in quest.Values)
            {
                sw.Write(" {0}", Enum.GetNames(enumType)[value]);
            }
        }

        /// <summary>
        /// Get id indexed phone array.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <param name="engineType">Engine type.</param>
        /// <returns>Id indexed phone array.</returns>
        private static string[] IdIndexedPhones(Language language, EngineType engineType)
        {
            Phoneme phoneme = Localor.GetPhoneme(language, engineType);
            int maxId = 0;
            foreach (string phone in phoneme.TtsPhoneIds.Keys)
            {
                if (phoneme.TtsPhoneIds[phone] > maxId)
                {
                    maxId = phoneme.TtsPhoneIds[phone];
                }
            }

            string[] phones = new string[maxId + 1];
            for (int phoneId = 0; phoneId < phones.Length; phoneId++)
            {
                phones[phoneId] = phoneme.TtsId2Phone(phoneId);
            }

            return phones;
        }

        /// <summary>
        /// Initalize questions/features metadata.
        /// </summary>
        private void InitializeFeatureMeta()
        {
            NamedMetaFeatures.Clear();
            IndexedMetaFeatures.Clear();

            string[] ttsFeatures = Enum.GetNames(typeof(TtsFeature));
            ToneManager toneManager = Localor.GetPhoneme(_language).ToneManager;

            for (int index = 0; index < ttsFeatures.Length; index++)
            {
                MetaFeature mf = new MetaFeature();
                mf.Name = ttsFeatures[index];

                TtsFeature feature =
                    (TtsFeature)Enum.Parse(typeof(TtsFeature), mf.Name);

                string[] values = null;
                switch (feature)
                {
                    case TtsFeature.PosInSentence:
                        values = Enum.GetNames(typeof(PosInSentence));
                        break;
                    case TtsFeature.PosInWord:
                        values = Enum.GetNames(typeof(PosInWord));
                        break;
                    case TtsFeature.PosInSyllable:
                        values = Enum.GetNames(typeof(PosInSyllable));
                        break;
                    case TtsFeature.LeftContextPhone:
                        values = IdIndexedPhones(_language, _engineType);
                        break;
                    case TtsFeature.RightContextPhone:
                        values = IdIndexedPhones(_language, _engineType);
                        break;
                    case TtsFeature.LeftContextTone:
                        {
                            List<string> names = new List<string>();
                            names.AddRange(toneManager.NameMap.Keys);
                            values = names.ToArray();
                            break;
                        }

                    case TtsFeature.RightContextTone:
                        {
                            List<string> names = new List<string>();
                            names.AddRange(toneManager.NameMap.Keys);
                            values = names.ToArray();
                            break;
                        }

                    case TtsFeature.TtsStress:
                        values = Enum.GetNames(typeof(TtsStress));
                        break;
                    case TtsFeature.TtsEmphasis:
                        values = Enum.GetNames(typeof(TtsEmphasis));
                        break;
                    case TtsFeature.TtsWordTone:
                        values = Enum.GetNames(typeof(TtsWordTone));
                        break;
                    default:
                        break;
                }

                if (values == null)
                {
                    continue;
                }

                for (int valueId = 0; valueId < values.Length; valueId++)
                {
                    mf.Values.Add(valueId, values[valueId]);
                }

                NamedMetaFeatures.Add(mf.Name, mf);
                IndexedMetaFeatures.Add(index, mf);
            }
        }

        #endregion
    }
}