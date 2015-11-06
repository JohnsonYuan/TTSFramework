//----------------------------------------------------------------------------
// <copyright file="UnicodeCharRange.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements speech environment helper funtions
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Unicode char wrap.
    /// </summary>
    public class UnicodeCharWrap
    {
        #region Fields
        /// <summary>
        /// ExpressionPattern .
        /// </summary>
        public const string ExpressionPattern = @"\\u[0-9a-fA-F]{4}";

        // Unicode expression, can be two formats: "\u0066"(unicode value) or "a"(char)
        private string _expression;

        // Unicode char : "f".
        private char _char;

        // Unicode value for comaration : "43".
        private short _encodingValue;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UnicodeCharWrap"/> class.
        /// </summary>
        /// <param name="expression">Expression should be unicode expression(^\\u[0-9a-fA-F]{4}$)
        /// or single char.</param>
        public UnicodeCharWrap(string expression)
        {
            bool unicodeExpression = Regex.Match(expression, "^" + ExpressionPattern + "$").Success;
            if (!unicodeExpression && expression.Length != 1)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Invalid expression " +
                    " [{0}], the format should be unicode format(like : \u0066) or single char(like : a)",
                    expression));
            }

            _expression = expression;

            byte[] bytes;

            if (unicodeExpression)
            {
                string s1 = expression.Substring(2, 2);
                string s2 = expression.Substring(4, 2);

                bytes = new byte[2];
                bytes[0] = Convert.ToByte(s2, 16);
                bytes[1] = Convert.ToByte(s1, 16);
            }
            else
            {
                bytes = Encoding.Unicode.GetBytes(expression.ToCharArray());
                if (bytes.Length != 2)
                {
                    throw new InvalidDataException("Unicode char bytes length should be 2");
                }
            }

            _encodingValue = BitConverter.ToInt16(bytes, 0);

            string stringValue = Encoding.Unicode.GetString(bytes);
            Trace.Assert(stringValue.Length == 1);
            _char = stringValue[0];
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Unicode expression, sammple : "\u0066".
        /// </summary>
        public string Expression
        {
            get { return _expression; }
        }

        /// <summary>
        /// Gets Unicode char.
        /// </summary>
        public char Char
        {
            get { return _char; }
        }

        /// <summary>
        /// Gets Unocode encoding value.
        /// </summary>
        public long EncodingValue
        {
            get { return _encodingValue; }
        }
        #endregion
    }

    /// <summary>
    /// Unicode char range.
    /// </summary>
    public class UnicodeCharRange
    {
        #region Fields

        // "\u0066"
        private UnicodeCharWrap _beginUnicode;
        private UnicodeCharWrap _endUnicode;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UnicodeCharRange"/> class.
        /// </summary>
        /// <param name="beginExpression">Begin unicode expression.</param>
        /// <param name="endExpression">End unicode expression.</param>
        public UnicodeCharRange(string beginExpression, string endExpression)
        {
            _beginUnicode = new UnicodeCharWrap(beginExpression);
            _endUnicode = new UnicodeCharWrap(endExpression);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets Unicode char range begin value.
        /// </summary>
        public UnicodeCharWrap BeginUnicode
        {
            get { return _beginUnicode; }
        }

        /// <summary>
        /// Gets Unicode char range end value.
        /// </summary>
        public UnicodeCharWrap EndUnicode
        {
            get { return _endUnicode; }
        }

        #endregion
    }

    /// <summary>
    /// Unicode char ranges.
    /// </summary>
    public class UnicodeCharRanges
    {
        #region Fields

        private List<UnicodeCharRange> _charRangeList = new List<UnicodeCharRange>();
        private List<UnicodeCharWrap> _charList = new List<UnicodeCharWrap>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the range is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return _charRangeList.Count == 0 && _charList.Count == 0; }
        }

        /// <summary>
        /// Gets Char enum list.
        /// </summary>
        public List<UnicodeCharWrap> CharList
        {
            get { return _charList; }
        }

        /// <summary>
        /// Gets Char range list.
        /// </summary>
        public List<UnicodeCharRange> CharRangeList
        {
            get { return _charRangeList; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Check the encoding value is in the range.
        /// </summary>
        /// <param name="encodingValue">Encoding value.</param>
        /// <returns>Check whether the encoding value is in the range.</returns>
        public bool IsInRange(long encodingValue)
        {
            bool isInRange = false;

            foreach (UnicodeCharRange range in _charRangeList)
            {
                if (encodingValue >= range.BeginUnicode.EncodingValue &&
                    encodingValue <= range.EndUnicode.EncodingValue)
                {
                    isInRange = true;
                    break;
                }
            }

            if (!isInRange)
            {
                foreach (UnicodeCharWrap charWrap in _charList)
                {
                    if (encodingValue == charWrap.EncodingValue)
                    {
                        isInRange = true;
                        break;
                    }
                }
            }

            return isInRange;
        }

        /// <summary>
        /// Add chars expression.
        /// </summary>
        /// <param name="charsExpression">Chars expression.</param>
        public void AddChars(string charsExpression)
        {
            string regex = string.Format(CultureInfo.InvariantCulture, "({0})+", UnicodeCharWrap.ExpressionPattern);
            const int UnicodeExpressionLength = 6;
            if (!Regex.Match(charsExpression, regex).Success)
            {
                foreach (char c in charsExpression)
                {
                    _charList.Add(new UnicodeCharWrap(c.ToString()));
                }
            }
            else
            {
                if (charsExpression.Length % UnicodeExpressionLength != 0)
                {
                    throw new InvalidDataException(Helper.NeutralFormat("Invlid unicode expression " +
                        "[{0}] length [{1}] should be in format : {2}",
                        charsExpression, charsExpression.Length, regex));
                }

                for (int i = 0; i < charsExpression.Length; i += UnicodeExpressionLength)
                {
                    string charUnicodeExpression = charsExpression.Substring(i, UnicodeExpressionLength);
                    _charList.Add(new UnicodeCharWrap(charUnicodeExpression));
                }
            }
        }

        /// <summary>
        /// Add single char expression.
        /// </summary>
        /// <param name="charExpression">Single char expression.</param>
        public void AddChar(string charExpression)
        {
            _charList.Add(new UnicodeCharWrap(charExpression));
        }

        /// <summary>
        /// Add one unicode range to the range list.
        /// </summary>
        /// <param name="beginUnicodeExpression">Unicode range begin value.</param>
        /// <param name="endUnicodeExpression">Unicode range end value.</param>
        public void AddRange(string beginUnicodeExpression, string endUnicodeExpression)
        {
            UnicodeCharRange range = new UnicodeCharRange(beginUnicodeExpression, endUnicodeExpression);
            _charRangeList.Add(range);
        }

        #endregion
    }
}