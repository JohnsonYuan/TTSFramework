//----------------------------------------------------------------------------
// <copyright file="Utility.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This file defines the FontUtility class,
//     which contains static utility functions related to voice font
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Font
{
    using System.IO;

    /// <summary>
    /// Contains static utility functions related to voice font.
    /// </summary>
    public static class FontUtility
    {
        /// <summary>
        /// Judge whether the given voice font is SPS font.
        /// </summary>
        /// <param name="fontPath">
        /// The font path,
        /// E.g. C:\Program Files\Common Files\microsoft shared\Speech\Tokens\TTS_MS_en-US_Helen_10.0\HelenT.
        /// </param>
        /// <returns>The judgement result.</returns>
        public static bool IsSpsFont(string fontPath)
        {
            string[] spsFontFileExtensions = new string[] { ".APM" };

            bool isSpsFont = true;
            foreach (string fontFileExtension in spsFontFileExtensions)
            {
                if (!File.Exists(fontPath + fontFileExtension))
                {
                    isSpsFont = false;
                }
            }

            return isSpsFont;
        }

        /// <summary>
        /// Judge whether the given voice font is RUS font.
        /// </summary>
        /// <param name="fontPath">
        /// The font path,
        /// E.g. C:\Program Files\Common Files\microsoft shared\Speech\Tokens\TTS_MS_en-US_Zira_10.0\ZiraT.
        /// </param>
        /// <returns>The judgement result.</returns>
        public static bool IsRusFont(string fontPath)
        {
            string[] rusFontFileExtensions = new string[] { ".ACD", ".APM", ".BEP", ".CCT", ".PST", ".UNT", ".WIH", ".WVE" };

            bool isRusFont = true;
            foreach (string fontFileExtension in rusFontFileExtensions)
            {
                if (!File.Exists(fontPath + fontFileExtension))
                {
                    isRusFont = false;
                }
            }

            return isRusFont;
        }
    }
}