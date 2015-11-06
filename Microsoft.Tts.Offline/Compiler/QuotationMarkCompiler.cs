//----------------------------------------------------------------------------
// <copyright file="QuotationMarkCompiler.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      QuotationMarkCompiler
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.IO;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;

    /// <summary>
    /// Quotation Mark Compiler Error.
    /// </summary>
    public enum QuotationMarkCompilerError
    {
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
    /// CharTable compiler.
    /// </summary>
    public class QuotationMarkCompiler
    {
        /// <summary>
        /// Prevents a default instance of the QuotationMarkCompiler class from being created.
        /// </summary>
        private QuotationMarkCompiler()
        {
        }

        /// <summary>
        /// Compiles quotation mark table into binary stream.
        /// </summary>
        /// <param name="quoteTable">The instance of quotation mark table.</param>
        /// <param name="outputStream">The instance of output binary stream.</param>
        /// <returns>Any error found during the compilation.</returns>
        public static ErrorSet Compile(QuotationMarkTable quoteTable, Stream outputStream)
        {
            if (quoteTable == null)
            {
                throw new ArgumentNullException("quoteTable");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            ErrorSet errorSet = new ErrorSet();

            BinaryWriter writer = new BinaryWriter(outputStream);
            writer.Write((uint)quoteTable.Language);
            writer.Write((uint)quoteTable.Items.Count);
            foreach (var item in quoteTable.Items)
            {
                writer.Write((ushort)item.Left);
                writer.Write((ushort)item.Right);
                writer.Write((uint)item.Direct);
            }

            return errorSet;
        }
    }
}