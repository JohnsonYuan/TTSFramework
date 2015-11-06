//----------------------------------------------------------------------------
// <copyright file="ToneConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     This module tests Chinese tone change based on Internal SDK
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.ServiceProvider.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Tone definition.
    /// </summary>
    public enum Tone
    {
        /// <summary>
        /// First tone.
        /// </summary>
        Tone1,

        /// <summary>
        /// Second tone.
        /// </summary>
        Tone2,

        /// <summary>
        /// Third tone.
        /// </summary>
        Tone3,

        /// <summary>
        /// Fourth tone.
        /// </summary>
        Tone4,

        /// <summary>
        /// Fifth tone.
        /// </summary>
        Tone5
    }

    public static class SyllableToneExtension
    {
        /// <summary>
        /// Get tone of syllable.
        /// </summary>
        /// <param name="syllable">TtsSyllable.</param>
        /// <returns>Tone.</returns>
        public static Tone GetTone(this TtsSyllable syllable)
        {
            if ((Language)syllable.Word.LangId != Language.ZhCN)
            {
                throw new NotSupportedException("Language not supported");
            }

            string[] phones = GetPhones(syllable);

            char toneChar1 = GetToneChar(phones[1]);
            char toneChar2 = GetToneChar(phones[2]);
            ChineseTone97 tone97 = new ChineseTone97(toneChar1, toneChar2);
            return tone97.ToChineseTone();
        }

        private static char GetToneChar(string phone)
        {
            string[] phoneTokens = SplitTonePhone(phone);
            return phoneTokens[1][0];
        }

        private static string SetToneChar(string phone, char toneChar)
        {
            string[] phoneTokens = SplitTonePhone(phone);
            return phoneTokens[0] + "_" + toneChar;
        }

        private static string[] GetPhones(TtsSyllable syllable)
        {
            string[] phones = syllable.Pronunciation.Split();
            Debug.Assert(phones.Length == 3);
            return phones;
        }

        private static string[] SplitTonePhone(string phone)
        {
            string[] phoneTokens = phone.Split(new char[] { '_' });
            Debug.Assert(phoneTokens.Length == 2);

            string tone = phoneTokens[1];
            Debug.Assert(tone.Length == 1);

            Debug.Assert(ChineseTone97.IsToneChar(tone[0]));
            return phoneTokens;
        }
    }

    /// <summary>
    /// Define Tone97 Chinese phone set and its mapping to PinYin.
    /// </summary>
    internal class ChineseTone97
    {
        public static readonly char High = 'H';
        public static readonly char Middle = 'M';
        public static readonly char Low = 'L';

        public static readonly ChineseTone97 Tone1 = new ChineseTone97(High, High);
        public static readonly ChineseTone97 Tone2 = new ChineseTone97(Low, High);
        public static readonly ChineseTone97 Tone3 = new ChineseTone97(Low, Low);
        public static readonly ChineseTone97 Tone4 = new ChineseTone97(High, Low);
        public static readonly ChineseTone97 Tone5 = new ChineseTone97(Middle, Middle);

        internal static readonly Dictionary<Tone, ChineseTone97> Tone97Map = new Dictionary<Tone, ChineseTone97>()
        {
            { Tone.Tone1, ChineseTone97.Tone1 },
            { Tone.Tone2, ChineseTone97.Tone2 },
            { Tone.Tone3, ChineseTone97.Tone3 },
            { Tone.Tone4, ChineseTone97.Tone4 },
            { Tone.Tone5, ChineseTone97.Tone5 }
        };

        public ChineseTone97(char firstChar, char secondChar)
        {
            FirstToneChar = firstChar;
            SecondToneChar = secondChar;
        }

        public char FirstToneChar
        {
            get;
            set;
        }

        public char SecondToneChar
        {
            get;
            set;
        }

        public static bool IsToneChar(char character)
        {
            return character == High || character == Middle || character == Low;
        }

        internal Tone ToChineseTone()
        {
            return Tone97Map.Where(pair =>
                pair.Value.FirstToneChar == this.FirstToneChar &&
                pair.Value.SecondToneChar == this.SecondToneChar).First().Key;
        }
    }
}