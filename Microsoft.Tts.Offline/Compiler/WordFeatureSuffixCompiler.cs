//----------------------------------------------------------------------------
// <copyright file="WordFeatureSuffixCompiler.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      WordFeatureSuffixCompiler
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Word Feature Suffix Compiler Error.
    /// </summary>
    public enum WordFeatureSuffixCompilerError
    {
        /// <summary>
        /// Empty Data.
        /// </summary>
        [ErrorAttribute(Message = "There is no data (symbol of whitespace will be ignored).")]
        EmptyData,
    }

    /// <summary>
    /// Data Structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    internal struct SuffixData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string Text;

        /// <summary>
        /// Initializes a new instance of the <see cref="SuffixData"/> struct.
        /// </summary>
        /// <param name="text">Word text.</param>
        public SuffixData(string text)
        {
            Debug.Assert(text.Length < 10);
            Text = text;
        }
    }

    /// <summary>
    /// CharTable compiler.
    /// </summary>
    public class WordFeatureSuffixCompiler
    {
        /// <summary>
        /// Prevents a default instance of the WordFeatureSuffixCompiler class from being created.
        /// </summary>
        private WordFeatureSuffixCompiler()
        {
        }

        /// <summary>
        /// Compiles suffix table into binary stream.
        /// </summary>
        /// <param name="wordFeatureSuffixTable">The instance of suffix table.</param>
        /// <param name="outputStream">The instance of output binary stream.</param>
        /// <returns>Any error found during the compilation.</returns>
        public static ErrorSet Compile(WordFeatureSuffixTable wordFeatureSuffixTable, Stream outputStream)
        {
            if (wordFeatureSuffixTable == null)
            {
                throw new ArgumentNullException("wordFeatureSuffixTable");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            ErrorSet errorSet = new ErrorSet();
            if (wordFeatureSuffixTable.NounItems.Count == 0)
            {
                errorSet.Add(WordFeatureSuffixCompilerError.EmptyData, "Empty noun suffix.");
            }

            if (wordFeatureSuffixTable.AdjItems.Count == 0)
            {
                errorSet.Add(WordFeatureSuffixCompilerError.EmptyData, "Empty adjective suffix.");
            }

            if (wordFeatureSuffixTable.VerbItems.Count == 0)
            {
                errorSet.Add(WordFeatureSuffixCompilerError.EmptyData, "Empty verb suffix.");
            }

            if (wordFeatureSuffixTable.SeparatorItems.Count == 0)
            {
                errorSet.Add(WordFeatureSuffixCompilerError.EmptyData, "Empty separator character.");
            }

            BinaryWriter writer = new BinaryWriter(outputStream);
            writer.Write((uint)wordFeatureSuffixTable.Language);

            // write the size of the structure for verification purchase
            writer.Write(((uint)Marshal.SizeOf(typeof(SuffixData))));

            writer.Write((uint)wordFeatureSuffixTable.NounItems.Count);
            foreach (var item in wordFeatureSuffixTable.NounItems)
            {
                SuffixData suffixdata = new SuffixData(item.Text);
                writer.Write(Helper.ToBytes(suffixdata));
            }

            writer.Write((uint)wordFeatureSuffixTable.AdjItems.Count);
            foreach (var item in wordFeatureSuffixTable.AdjItems)
            {
                SuffixData suffixdata = new SuffixData(item.Text);
                writer.Write(Helper.ToBytes(suffixdata));
            }

            writer.Write((uint)wordFeatureSuffixTable.VerbItems.Count);
            foreach (var item in wordFeatureSuffixTable.VerbItems)
            {
                SuffixData suffixdata = new SuffixData(item.Text);
                writer.Write(Helper.ToBytes(suffixdata));
            }

            writer.Write((uint)wordFeatureSuffixTable.SeparatorItems.Count);
            foreach (var item in wordFeatureSuffixTable.SeparatorItems)
            {
                SuffixData suffixdata = new SuffixData(item.Text);
                writer.Write(Helper.ToBytes(suffixdata));
            }

            return errorSet;
        }
    }
}