//----------------------------------------------------------------------------
// <copyright file="ParallelStructCompiler.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      ParallelStructCompiler
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
    /// Parallel Struct Table Compiler Error.
    /// </summary>
    public enum ParallelStructCompilerError
    {
        /// <summary>
        /// Invalid POS string.
        /// </summary>
        [ErrorAttribute(Message = "There is invalid POS string data (symbol of whitespace will be ignored).")]
        InvalidPOSString,

        /// <summary>
        /// Empty Data.
        /// </summary>
        [ErrorAttribute(Message = "There is no data (symbol of whitespace will be ignored).")]
        EmptyData,
    }

    /// <summary>
    /// Pos Data Structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    internal struct WordData
    {
        public uint POS;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string Text;

        /// <summary>
        /// Initializes a new instance of the <see cref="WordData"/> struct.
        /// </summary>
        /// <param name="text">Word text.</param>
        /// <param name="pos">Word POS.</param>
        public WordData(string text, uint pos)
        {
            Debug.Assert(text.Length < 20);
            Text = text;
            POS = pos;
        }
    }

    /// <summary>
    /// CharTable compiler.
    /// </summary>
    public class ParallelStructCompiler
    {
        /// <summary>
        /// Prevents a default instance of the ParallelStructCompiler class from being created.
        /// </summary>
        private ParallelStructCompiler()
        {
        }

        /// <summary>
        /// Compiles parallel struct table into binary stream.
        /// </summary>
        /// <param name="parallelStructTable">The instance of parallel struct table.</param>
        /// <param name="posSet">The pos set .</param>
        /// <param name="outputStream">The instance of output binary stream.</param>
        /// <returns>Any error found during the compilation.</returns>
        public static ErrorSet Compile(ParallelStructTable parallelStructTable, TtsPosSet posSet, Stream outputStream)
        {
            if (parallelStructTable == null)
            {
                throw new ArgumentNullException("parallelStructTable");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            ErrorSet errorSet = new ErrorSet();
            if (parallelStructTable.SegmentItems.Count == 0)
            {
                errorSet.Add(ParallelStructCompilerError.EmptyData, "Empty segment word.");
            }

            if (parallelStructTable.TriggerItems.Count == 0)
            {
                errorSet.Add(ParallelStructCompilerError.EmptyData, "Empty trigger word.");
            }

            BinaryWriter writer = new BinaryWriter(outputStream);
            writer.Write((uint)parallelStructTable.Language);

            // write the size of the structure for verification purchase
            writer.Write(((uint)Marshal.SizeOf(typeof(WordData))));

            writer.Write((uint)parallelStructTable.SegmentItems.Count);

            foreach (var item in parallelStructTable.SegmentItems)
            {
                if (!posSet.Items.ContainsKey(item.POS))
                {
                    errorSet.Add(ParallelStructCompilerError.InvalidPOSString,
                        item.POS.ToString(CultureInfo.InvariantCulture));
                    continue;
                }

                // POS is a 16-bit integer
                WordData worddata = new WordData(item.Text, (uint)posSet.Items[item.POS]);
                writer.Write(Helper.ToBytes(worddata));
            }

            writer.Write((uint)parallelStructTable.TriggerItems.Count);
            foreach (var item in parallelStructTable.TriggerItems)
            {
                if (!posSet.Items.ContainsKey(item.POS))
                {
                    errorSet.Add(ParallelStructCompilerError.InvalidPOSString,
                        item.POS.ToString(CultureInfo.InvariantCulture));
                    continue;
                }

                // POS is a 16-bit integer
                WordData worddata = new WordData(item.Text, (uint)posSet.Items[item.POS]);
                writer.Write(Helper.ToBytes(worddata));
            }

            return errorSet;
        }
    }
}