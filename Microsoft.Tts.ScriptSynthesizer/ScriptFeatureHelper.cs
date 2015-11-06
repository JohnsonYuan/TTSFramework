//----------------------------------------------------------------------------
// <copyright file="ScriptFeatureHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script feature helper
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.ScriptSynthesizer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Speech.Synthesis;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Research;
    using Microsoft.Tts.ServiceProvider;
    using Offline.Common;
    using Offline.Core;
    using Offline = Microsoft.Tts.Offline;
    using SP = Microsoft.Tts.ServiceProvider;
    using TtsTobiBoundaryTone = Microsoft.Tts.ServiceProvider.TtsTobiBoundaryTone;

    /// <summary>
    /// Script feature import.
    /// </summary>
    public class ScriptFeatureHelper
    {
        #region Fields
        /// <summary>
        /// Phone index of current sentence.
        /// </summary>
        private int[][,] _totalPhoneIndex;

        #endregion

        #region Constructor and destructor

        #endregion

        #region Public static operations

        /// <summary>
        /// Check the word consistency.
        /// </summary>
        /// <param name="utt">Tts utterance.</param>
        /// <param name="scriptSentence">Script sentence.</param>
        public static void CheckWordConsistency(SP.TtsUtterance utt, ScriptSentence scriptSentence)
        {
            int wordIndex = 0;

            foreach (TtsWord uttWord in utt.Words)
            {
                if (uttWord.IsPronounceable)
                {
                    string wordText = utt.OriginalText.Substring((int)uttWord.TextOffset,
                        (int)uttWord.TextLength).ToLower(CultureInfo.CurrentCulture);
                    if (wordIndex < scriptSentence.PronouncedWords.Count &&
                        !wordText.Equals(scriptSentence.PronouncedWords[wordIndex].Description.ToLower(CultureInfo.CurrentCulture)))
                    {
                        string message = Helper.NeutralFormat("Runtime's word [{0}] " +
                            "and script word [{1}] has no consistence.", wordText,
                            scriptSentence.PronouncedWords[wordIndex].Description);
                        throw new InvalidDataException(message);
                    }

                    wordIndex++;
                }
            }

            if (wordIndex != scriptSentence.PronouncedWords.Count)
            {
                throw new InvalidDataException("Runtime's normal words' count must equal to the script's.");
            }
        }

        /// <summary>
        /// Get the orignal internal and external F0s.
        /// </summary>
        /// <param name="intUtt">Internal utterance.</param>
        /// <param name="extUvSeg">External uvSeg.</param>
        /// <param name="layerIndex">Certain syllable/phone/state's position.</param>
        /// <param name="extNotNullF0">Out float[], External F0s.</param> 
        /// <param name="intNotNullF0Position">Not null F0s' phones' position.</param>
        public static void GetF0(SP.TtsUtterance intUtt, ScriptUvSeg extUvSeg, LayerIndex layerIndex, 
            out float[] extNotNullF0, out List<int> intNotNullF0Position)
        {
            intNotNullF0Position = new List<int>();
            if (extUvSeg.SegType == ScriptUvSegType.Voiced)
            {
                extNotNullF0 = new float[extUvSeg.F0Contour.Contour.Count];
                for (int i = 0; i < extUvSeg.F0Contour.Contour.Count; i++)
                {
                    extNotNullF0[i] = extUvSeg.F0Contour.Contour[i];
                }
            }
            else if (extUvSeg.SegType == ScriptUvSegType.Mixed)
            {
                List<float> extNotNullF0List = new List<float>();
                for (int i = 0; i < extUvSeg.F0Contour.Contour.Count; i++)
                {
                    if (extUvSeg.F0Contour.Contour[i] != 0)
                    {
                        extNotNullF0List.Add(extUvSeg.F0Contour.Contour[i]);
                    }
                }

                extNotNullF0 = new float[extNotNullF0List.Count];
                extNotNullF0List.CopyTo(extNotNullF0);
            }
            else
            {
                extNotNullF0 = null;
            }

            int interF0Index = 0;
            for (int i = 0; i < layerIndex.StartPhone; i++)
            {
                for (int j = 0; j < (int)intUtt.Acoustic.Durations.Column; j++)
                {
                    interF0Index += (int)intUtt.Acoustic.Durations[i][j];
                }
            }

            for (int i = layerIndex.StartPhone; i < layerIndex.EndPhone; i++)
            {
                for (int j = 0; j < (int)intUtt.Acoustic.Durations.Column; j++)
                {
                    for (int k = 0; k < intUtt.Acoustic.Durations[i][j]; k++)
                    {
                        if (intUtt.Acoustic.F0s[interF0Index][0] != 0)
                        {
                            intNotNullF0Position.Add(interF0Index);
                        }

                        interF0Index++;
                    }
                }
            }
        }

        /// <summary>
        /// Check consistency of script and utterance.
        /// </summary>
        /// <param name="utt">Tts utterance.</param>
        /// <param name="scriptSentence">Script sentence.</param>
        /// <param name="option">Checking option.</param>
        private static void CheckConsistency(SP.TtsUtterance utt, ScriptSentence scriptSentence,
            ScriptFeatureImportConfig.CheckingOptions option)
        {
            if ((option & ScriptFeatureImportConfig.CheckingOptions.Word) ==
                ScriptFeatureImportConfig.CheckingOptions.Word)
            {
                CheckWordConsistency(utt, scriptSentence);
            }
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Get internal normal words' phones' position.
        /// </summary>
        /// <param name="intUtt">Internal utterance.</param>
        [SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional",
            Justification = "it is not culture specific array")]
        private void GetPhonePosi(SP.TtsUtterance intUtt)
        {
            int normalWordCount = 0;
            int phoneCount = 0;

            foreach (TtsWord uttWord in intUtt.Words)
            {
                if (uttWord.IsPronounceable)
                {
                    normalWordCount++;
                }
            }

            _totalPhoneIndex = new int[normalWordCount][,];
            int normalWordIndex = 0;

            foreach (TtsWord uttWord in intUtt.Words)
            {
                int syllableCount = 0;
                if (!uttWord.IsPronounceable)
                {
                    TtsSyllable intSyllable = uttWord.FirstSyllable;
                    if (intSyllable != null)
                    {
                        while (intSyllable != uttWord.LastSyllable.Next)
                        {
                            TtsPhone intPhone = intSyllable.FirstPhone;
                            if (intPhone != null)
                            {
                                while (intPhone != intSyllable.LastPhone.Next)
                                {
                                    phoneCount++;
                                    intPhone = intPhone.Next;
                                }
                            }

                            intSyllable = intSyllable.Next;
                        }
                    }
                }
                else
                {
                    TtsSyllable intSyllable = uttWord.FirstSyllable;
                    if (intSyllable == null)
                    {
                        string message = Helper.NeutralFormat("Runtime's normal word [{0}] has no syllable.",
                                uttWord.WordText);
                        throw new InvalidDataException(message);
                    }

                    while (intSyllable != uttWord.LastSyllable.Next)
                    {
                        syllableCount++;
                        intSyllable = intSyllable.Next;
                    }

                    _totalPhoneIndex[normalWordIndex] = new int[syllableCount, 2];
                    int syllableIndex = 0;
                    intSyllable = uttWord.FirstSyllable;

                    while (intSyllable != uttWord.LastSyllable.Next)
                    {
                        TtsPhone intPhone = intSyllable.FirstPhone;
                        if (intPhone == null)
                        {
                            string message = Helper.NeutralFormat("Runtime's normal word [{0}]'s syllable [{1}] has no phone.",
                                    uttWord.WordText, intSyllable.Pronunciation);
                            throw new InvalidDataException(message);
                        }

                        _totalPhoneIndex[normalWordIndex][syllableIndex, 0] = phoneCount;
                        while (intPhone != intSyllable.LastPhone.Next)
                        {
                            phoneCount++;
                            intPhone = intPhone.Next;
                        }

                        _totalPhoneIndex[normalWordIndex][syllableIndex, 1] = phoneCount;

                        intSyllable = intSyllable.Next;
                        syllableIndex++;
                    }

                    normalWordIndex++;
                }
            }
        }

        /// <summary>
        /// Check whether the internal normal words matches the external normal words.
        /// </summary>
        /// <param name="extSentence">External script sentence.</param>
        private void CheckMatched(ScriptSentence extSentence)
        {
            if (_totalPhoneIndex.Length != extSentence.PronouncedWords.Count)
            {
                throw new InvalidDataException("Runtime's normal words' count must equal to the script's.");
            }

            for (int wordIndex = 0; wordIndex < _totalPhoneIndex.Length; wordIndex++)
            {
                int uttSyllableCount = _totalPhoneIndex[wordIndex].GetLength(0);
                if (uttSyllableCount != extSentence.PronouncedWords[wordIndex].Syllables.Count)
                {
                    string message = Helper.NeutralFormat("Script's normal word [{0}]'s syllables' count "
                        + "must equal to the runtime's.", extSentence.PronouncedWords[wordIndex].Pronunciation);
                    throw new InvalidDataException(message);
                }

                for (int syllableIndex = 0; syllableIndex < uttSyllableCount; syllableIndex++)
                {
                    int phoneCount = _totalPhoneIndex[wordIndex][syllableIndex, 1] - 
                        _totalPhoneIndex[wordIndex][syllableIndex, 0];
                    if (phoneCount != 
                        extSentence.PronouncedWords[wordIndex].Syllables[syllableIndex].Phones.Count)
                    {
                        string message = Helper.NeutralFormat("Script's normal word [{0}]'s " + 
                            "syllable [{1}]'s phones' count must equal to the runtime's.", 
                            extSentence.PronouncedWords[wordIndex].Pronunciation, 
                            extSentence.PronouncedWords[wordIndex].Syllables[syllableIndex].Text);
                        throw new InvalidDataException(message);
                    }
                }
            }
        }

        /// <summary>
        /// Fix f0 no consistence.
        /// </summary>
        /// <param name="intUtt">Internal utterance.</param>
        /// /// <param name="nFixF0NoConsistenceNum">Num of FixF0NoConsistence.</param>
        private void FixF0NoConsistence(SP.TtsUtterance intUtt, int nFixF0NoConsistenceNum)
        {
            int enginePhoneIndex = 0;
            int f0Index = 0;
            foreach (TtsWord engineWord in intUtt.Words)
            {
                TtsSyllable intSyllable = engineWord.FirstSyllable;
                if (intSyllable != null)
                {
                    while (intSyllable != engineWord.LastSyllable.Next)
                    {
                        TtsPhone intPhone = intSyllable.FirstPhone;
                        if (intPhone != null)
                        {
                            while (intPhone != intSyllable.LastPhone.Next)
                            {
                                uint phoneDuration = 0;
                                for (int i = 0; i < intUtt.Acoustic.Durations.Column; i++)
                                {
                                    phoneDuration += intUtt.Acoustic.Durations[enginePhoneIndex][i];
                                }

                                int f0TempIndex = f0Index;
                                List<float> f0List = new List<float>();
                                for (int i = 0; i < phoneDuration; i++)
                                {
                                    f0List.Add(intUtt.Acoustic.F0s[f0Index][0]);
                                    f0Index++;
                                }

                                int zeroCount = 0;
                                for (int i = 0; i < f0List.Count; i++)
                                {
                                    if (f0List[i] == 0)
                                    {
                                        zeroCount++;
                                    }
                                }

                                if (zeroCount <= nFixF0NoConsistenceNum)
                                {
                                    if (f0List[0] == 0)
                                    {
                                        int notZeroIndex = 1;
                                        while (f0List[notZeroIndex] == 0)
                                        {
                                            notZeroIndex++;
                                        }

                                        intUtt.Acoustic.F0s[f0TempIndex][0] = f0List[notZeroIndex];
                                    }

                                    for (int i = 1; i < f0List.Count; i++)
                                    {
                                        if (f0List[i] == 0)
                                        {
                                            intUtt.Acoustic.F0s[f0TempIndex + i][0] = f0List[i - 1];
                                        }
                                    }
                                }
                                else if (zeroCount >= f0List.Count - nFixF0NoConsistenceNum)
                                {
                                    for (int i = 0; i < f0List.Count; i++)
                                    {
                                        if (f0List[i] != 0)
                                        {
                                            intUtt.Acoustic.F0s[f0TempIndex + i][0] = 0;
                                        }
                                    }
                                }

                                intPhone = intPhone.Next;
                                enginePhoneIndex++;
                            }
                        }

                        intSyllable = intSyllable.Next;
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Indicate certain syllable/phone/state's position.
        /// </summary>
        public struct LayerIndex
        {
            /// <summary>
            /// The start phone's index of all phones.
            /// </summary>
            public int StartPhone;

            /// <summary>
            /// The end phone's index of all phones.
            /// </summary>
            public int EndPhone;

            /// <summary>
            /// The certain state's index of one phone's states.
            /// </summary>
            public int State;
        }
    }
}
