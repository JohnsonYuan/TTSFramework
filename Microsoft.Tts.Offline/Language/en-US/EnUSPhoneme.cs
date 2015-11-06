//----------------------------------------------------------------------------
// <copyright file="EnUSPhoneme.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements English phoneme
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Class to manage TTS 30 English Phonemes.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
        "CA1706:ShortAcronymsShouldBeUppercase")]
    public class EnUSPhoneme : Phoneme
    {
        #region Fields

        private static readonly string[] _ttsPhonesData = new string[]
            {
                "iy", "ih", "eh", "ae", "aa", "ah", "ao", "uh", "ax", "er",
                "ey", "ay", "oy", "aw", "ow", "uw", "ix", Phoneme.Silence, "w", "y",
                "r", "l", "h", "m", "n", "ng", "f", "v", "th", "dh",
                "s", "z", "sh", "zh", "p", "b", "t", "d", "k", "g",
                "ch", "jh", "dx", Phoneme.ShortPause
            };

        private static readonly string[] _ttsVowelPhonesData = new string[]
            {
                "ah", "aa", "eh", "ae", "ao", "ax", "er",
                "ih", "iy", "uh", "uw", "ay", "aw", "ey", "ow", "oy"
            };
        private static readonly string[] _ttsSonorantPhonesData = new string[]
            {
                "m", "n", "ng", "y", "w", "l", "r"
            };

        private static string[] _srPhonesData = new string[]
            {
                "iy", "ih", "eh", "ae", "aa", "ah", "ao", "uh", "ax", "er",
                "ey", "ay", "oy", "aw", "ow", "uw", "ix", Phoneme.Silence, "w", "y",
                "r", "l", "hh", "m", "n", "ng", "f", "v", "th", "dh",
                "s", "z", "sh", "zh", "p", "b", "t", "d", "k", "g",
                "ch", "jh", "dx", Phoneme.ShortPause
            };

        private static string[] _ttsPhoneIdsData = new string[]
            {
                "-",    "1",
                Phoneme.Silence,  "2",
                "1",    "3",
                "2",    "4",
                "aa",   "5",
                "ae",   "6",
                "ah",   "7",
                "ao",   "8",
                "aw",   "9",
                "ax",   "10",
                "ay",   "11",
                "b",    "12",
                "ch",   "13",
                "d",    "14",
                "dh",   "15",
                "dx",   "16",
                "eh",   "17",
                "er",   "18",
                "ey",   "19",
                "f",    "20",
                "g",    "21",
                "h",    "22",
                "ih",   "23",
                "ix",   "24",
                "iy",   "25",
                "jh",   "26",
                "k",    "27",
                "l",    "28",
                "m",    "29",
                "n",    "30",
                "ng",   "31",
                "ow",   "32",
                "oy",   "33",
                "p",    "34",
                "r",    "35",
                "s",    "36",
                "sh",   "37",
                "t",    "38",
                "th",   "39",
                "uh",   "40",
                "uw",   "41",
                "v",    "42",
                "w",    "43",
                "y",    "44",
                "z",    "45",
                "zh",   "46"
            };

        private static string[] _sapiVisemeIdsData = new string[]
            {
                "aa", "2", 
                "ae", "1", 
                "ah", "1", 
                "ao", "3", 
                "aw", "9", 
                "ax", "1", 
                "ay", "11",
                "b",  "19",
                "ch", "16",
                "d",  "19",
                "dh", "17",
                "eh", "4", 
                "er", "5", 
                "ey", "4", 
                "f",  "18",
                "g",  "20",
                "h",  "12",
                "ih", "6", 
                "iy", "6", 
                "jh", "16",
                "k",  "20",
                "l",  "14",
                "m",  "21",
                "n",  "19",
                "ng", "20",
                "ow", "8", 
                "oy", "10",
                "p",  "21",
                "r",  "13",
                "s",  "15",
                "sh", "16",
                "t",  "19",
                "th", "17",
                "uh", "4", 
                "uw", "7", 
                "v",  "18",
                "w",  "7", 
                "y",  "6", 
                "z",  "15",
                "zh", "16" 
            };

        private string[] _posCodesData = new string[]
            {
                "noun", "4096",
                "pron", "4097",
                "subjpron", "4098",
                "objpron", "4099",
                "relpron", "4100",
                "ppron", "4100",
                "ipron", "4102",
                "rpron", "4103",
                "dpron", "4104",
                "verb", "8192",
                "modifier", "12288",
                "adj", "12289",
                "adv", "12290",
                "function", "16384",
                "vaux", "16385",
                "rvaux", "16386",
                "conj", "16387",
                "cconj", "16388",
                "interr", "16389",
                "det", "16390",
                "contr", "16391",
                "vpart", "16392",
                "prep", "16393",
                "quant", "16394",
                "interjection", "20480",
                "None", "0"
            };

        #endregion

        #region Construction

        /// <summary>
        /// Construction.
        /// </summary>
        public EnUSPhoneme()
        {
            Helper.FillData(_ttsPhonesData, TtsPhones);
            Helper.FillData(_ttsPhoneIdsData, TtsPhoneIds);

            Helper.FillData(_ttsVowelPhonesData, TtsVowelPhones);
            Helper.FillData(_ttsSonorantPhonesData, TtsSonorantPhones);

            Helper.FillData(_srPhonesData, SrPhones);
            Helper.FillMap(_ttsPhonesData, _srPhonesData, Tts2SrMap);

            Helper.FillData(_sapiVisemeIdsData, SapiVisemeIds);

            Helper.FillData(_posCodesData, PosCodes);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Language of this instance.
        /// </summary>
        public override Language Language
        {
            get { return Language.EnUS; }
        }

        #endregion
    }
}