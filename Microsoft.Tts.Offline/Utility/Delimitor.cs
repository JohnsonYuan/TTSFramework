//----------------------------------------------------------------------------
// <copyright file="Delimitor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This is a delimitor helper static class.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Text helper static class.
    /// </summary>
    public static class Delimitor
    {
        /// <summary>
        /// White space char.
        /// </summary>
        public const char WhitespaceChar = ' ';

        /// <summary>
        /// Tab char.
        /// </summary>
        public const char TabChar = '\t';

        /// <summary>
        /// Comment char.
        /// </summary>
        public const char CommentChar = '#';

        /// <summary>
        /// Period char.
        /// </summary>
        public const char PeriodChar = '.';

        /// <summary>
        /// String ending char.
        /// </summary>
        public const char StringEndingChar = '\0';

        /// <summary>
        /// Colon char.
        /// </summary>
        public const char ColonChar = ':';

        /// <summary>
        /// Colon char.
        /// </summary>
        public const char DashChar = '-';

        /// <summary>
        /// Gets Blank char array.
        /// </summary>
        public static char[] BlankChars
        {
            get
            {
                return new char[] { WhitespaceChar, TabChar };
            }
        }

        /// <summary>
        /// Gets white space chars.
        /// </summary>
        /// <returns>Blank chars.</returns>
        public static char[] WhiteSpaceChars
        {
            get
            {
                return new char[] { WhitespaceChar };
            }
        }

        /// <summary>
        /// Gets tab chars.
        /// </summary>
        /// <returns>Blank chars.</returns>
        public static char[] TabChars
        {
            get
            {
                return new char[] { TabChar };
            }
        }

        /// <summary>
        /// Convert char arrays to array.
        /// </summary>
        /// <param name="chars">Chars to be converted.</param>
        /// <returns>Char array.</returns>
        public static char[] ToArray(params char[] chars)
        {
            Helper.ThrowIfNull(chars);
            char[] array = new char[chars.Length];
            for (int i = 0; i < chars.Length; i++)
            {
                array[i] = chars[i];
            }

            return array;
        }

        /// <summary>
        /// Convert char to array.
        /// </summary>
        /// <param name="c">Char to be converted.</param>
        /// <returns>Char arrray.</returns>
        public static char[] ToArray(this char c)
        {
            char[] array = new char[1];
            array[0] = c;
            return array;
        }

        /// <summary>
        /// Remove the '\0' at the end of string.
        /// </summary>
        /// <param name="line">String line to be processed.</param>
        /// <returns>Line have removed ending char.</returns>
        public static string RemoveStringEndingChar(this string line)
        {
            int index = 0;
            while (index < line.Length && line[index] != StringEndingChar)
            {
                index++;
            }

            string processedLine = line;
            if (index < line.Length && line[index] == StringEndingChar)
            {
                processedLine = line.Substring(0, index);
            }

            return processedLine;
        }

        /// <summary>
        /// Split the string with RemoveEmptyEntries option.
        /// </summary>
        /// <param name="line">The line to be split.</param>
        /// <param name="delimeters">Delimeters used to split.</param>
        /// <returns>Splitted items.</returns>
        public static string[] CleanSplit(this string line, params char[] delimeters)
        {
            Helper.ThrowIfNull(line);
            return line.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}