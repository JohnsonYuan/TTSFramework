//----------------------------------------------------------------------------
// <copyright file="CharTableCompiler.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      CharTableCompiler
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// CharTable Compiler Error.
    /// </summary>
    public enum CharTableCompilerError
    {
        /// <summary>
        /// Converting Pronunciation Error
        /// Parameters: 
        /// {0}: character.
        /// </summary>
        [ErrorAttribute(Message = "Converting Pronunciation error for character \"{0}\"", ConcateString = ":",
            Severity = ErrorSeverity.Warning)]
        ConvertingPronunciationError,

        /// <summary>
        /// Empty Data.
        /// </summary>
        [ErrorAttribute(Message = "There is no data (symbol of whitespace will be ignored) in chartable")]
        EmptyData,

        /// <summary>
        /// Duplicate Symbol.
        /// </summary>
        [ErrorAttribute(Message = "Duplicate symbol \"{0}\" is found, which will be skipped for compiling",
            Severity = ErrorSeverity.Warning)]
        DuplicateSymbol
    }

    /// <summary>
    /// Char Table Data Structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    internal struct CharData
    {
        public uint SpellChar;
        public ushort Flags;
        public uint WordOffset;
        public uint PronOffset;
    }

    /// <summary>
    /// CharTable compiler.
    /// </summary>
    public class CharTableCompiler
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="CharTableCompiler"/> class from being created.
        /// </summary>
        private CharTableCompiler()
        {
        }

        /// <summary>
        /// Char Feature.
        /// </summary>
        [Flags]
        public enum CharFeature
        {
            /// <summary>
            /// None.
            /// </summary>
            None = 0,

            /// <summary>
            /// Vowel.
            /// </summary>
            Vowel = (1 << 0),

            /// <summary>
            /// Consonant.
            /// </summary>
            Consonant = (1 << 2)
        }

        /// <summary>
        /// Compile.
        /// </summary>
        /// <param name="charTable">Char table.</param>
        /// <param name="phoneSet">Phone set.</param>
        /// <param name="outputStream">Output Stream.</param>
        /// <returns>Error.</returns>
        public static ErrorSet Compile(CharTable charTable, TtsPhoneSet phoneSet, Stream outputStream)
        {
            if (charTable == null)
            {
                throw new ArgumentNullException("charTable");
            }

            if (phoneSet == null)
            {
                throw new ArgumentNullException("phoneSet");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            ErrorSet errorSet = new ErrorSet();
            List<CharData> charDatas = new List<CharData>();
            using (StringPool sp = new StringPool())
            {
                Collection<string> chars = new Collection<string>();
                foreach (CharElement element in charTable.CharList)
                {
                    if (chars.Contains(element.Symbol))
                    {
                        errorSet.Add(CharTableCompilerError.DuplicateSymbol,
                        element.Symbol);
                        continue;
                    }

                    chars.Add(element.Symbol);
                    CharData ci = new CharData();

                    ci.SpellChar = element.EncodedSymbol;
                    ci.Flags = (ushort)element.Feature;
                    ci.WordOffset = Convert.ToUInt32(sp.PutString(element.IsolatedExpansion));

                    // convert pronunciation first into phoneIDs.
                    using (MemoryStream ms = new MemoryStream())
                    {
                        ErrorSet convertingErrorSet = PronStringToPhoneID(ms, phoneSet, element.Pronunciation);
                        foreach (Error error in convertingErrorSet.Errors)
                        {
                            errorSet.Add(CharTableCompilerError.ConvertingPronunciationError, error,
                            element.Symbol);
                        }

                        ci.PronOffset = (uint)sp.Position;
                        sp.PutBuffer(ms.ToArray());
                        charDatas.Add(ci);
                    }
                }

                if (charTable.CharList.Count == 0)
                {
                    errorSet.Add(CharTableCompilerError.EmptyData);
                }
                else
                {
                    BinaryWriter bw = new BinaryWriter(outputStream);
                    CharItemComparer comparer = new CharItemComparer();
                    charDatas.Sort(comparer);

                    bw.Write(charDatas.Count);
                    foreach (CharData ci in charDatas)
                    {
                        bw.Write(Helper.ToBytes(ci));
                    }

                    byte[] pool = sp.ToArray();
                    bw.Write(pool, 0, pool.Length);
                }

                return errorSet;
            }         
        }

        /// <summary>
        /// Generate feature string.
        /// </summary>
        /// <param name="features">Features.</param>
        /// <returns>Feature string.</returns>
        public static string FeaturesToString(CharFeature features)
        {
            StringBuilder sb = new StringBuilder();
            foreach (CharFeature feature in Enum.GetValues(typeof(CharFeature)))
            {
                if ((features & feature) != 0)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(" ");
                    }

                    sb.Append(feature.ToString());
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Convert Pronunciation to id string.
        /// </summary>
        /// <param name="memoryStream">MemoryStream.</param>
        /// <param name="phoneSet">Phone set.</param>
        /// <param name="pron">Pronunciation string.</param>
        /// <returns>Error Set.</returns>
        private static ErrorSet PronStringToPhoneID(MemoryStream memoryStream, 
            TtsPhoneSet phoneSet, string pron)
        {
            ErrorSet errorSet = new ErrorSet();
            if (!string.IsNullOrEmpty(pron) && !string.IsNullOrEmpty(pron.Trim()))
            {
                Phone[] phones = Pronunciation.SplitIntoPhones(pron, phoneSet, errorSet);
                if (!errorSet.Contains(ErrorSeverity.MustFix) && phones != null)
                {
                    foreach (Phone phone in phones)
                    {
                        memoryStream.WriteByte((byte)(phone.Id % 0x100));
                        memoryStream.WriteByte((byte)(phone.Id / 0x100));
                    }
                }
            }

            memoryStream.WriteByte(0);
            memoryStream.WriteByte(0);
            return errorSet;
        }
    }

    /// <summary>
    /// Comparaer for CharData.
    /// </summary>
    internal class CharItemComparer : IComparer<CharData>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="x">Char data x.</param>
        /// <param name="y">Char data y.</param>
        /// <returns>Positive for x greater than y, 0 for equal and negative for x less than y.</returns>
        public int Compare(CharData x, CharData y)
        {
            int ret = 0;
            if (x.SpellChar > y.SpellChar)
            {
                ret = 1;
            }
            else if (x.SpellChar == y.SpellChar)
            {
                ret = 0;
            }
            else
            {
                ret = -1;
            }

            return ret;
        }
    }
}