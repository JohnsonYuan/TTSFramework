//----------------------------------------------------------------------------
// <copyright file="FileAssert.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements FileAssert
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Test
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// A class to assist the assertion test on file comparison.
    /// </summary>
    public static class FileAssert
    {
        /// <summary>
        /// Assert message for differences.
        /// </summary>
        private const string AssertMessage = "The contents of item [{0}] and item [{1}] don't " +
            "match with each other. To view details, please use windiff.exe \"{0}\" \"{1}\"";

        /// <summary>
        /// Assets whether two text files equal with each other.
        /// </summary>
        /// <param name="expected">The expected file.</param>
        /// <param name="actual">The actual file.</param>
        public static void AreTextEqual(string expected, string actual)
        {
            bool identical = Helper.CompareTextFile(expected, actual, true);
            string message = Helper.NeutralFormat(AssertMessage, expected, actual);
            Assert.IsTrue(identical, message);
        }

        /// <summary>
        /// Assets whether two text directories equal with each other.
        /// </summary>
        /// <param name="expected">The expected directory path.</param>
        /// <param name="actual">The actual directory path.</param>
        public static void AreTextDirEqual(string expected, string actual)
        {
            bool identical = Helper.CompareTextDir(expected, actual, true);
            string message = Helper.NeutralFormat(AssertMessage, expected, actual);
            Assert.IsTrue(identical, message);
        }

        /// <summary>
        /// Assets whether two binary files equal with each other.
        /// </summary>
        /// <param name="expected">The expected file.</param>
        /// <param name="actual">The actual file.</param>
        public static void AreBinaryEqual(string expected, string actual)
        {
            bool identical = Helper.CompareBinary(expected, actual);
            string message = Helper.NeutralFormat(AssertMessage, expected, actual);
            Assert.IsTrue(identical, message);
        }

        /// <summary>
        /// Assets whether two binary directories equal with each other.
        /// </summary>
        /// <param name="expected">The expected directory path.</param>
        /// <param name="actual">The actual directory path.</param>
        public static void AreBinaryDirEqual(string expected, string actual)
        {
            bool identical = Helper.CompareDirectory(expected, actual, true);
            string message = Helper.NeutralFormat(AssertMessage, expected, actual);
            Assert.IsTrue(identical, message);
        }

        /// <summary>
        /// Assets whether two APM files equal with each other.
        /// </summary>
        /// <param name="expected">The expected file.</param>
        /// <param name="actual">The actual file.</param>
        public static void AreApmEqual(string expected, string actual)
        {
            bool identical = FontComparer.CompareVoiceFont(expected, actual);
            string message = Helper.NeutralFormat(AssertMessage, expected, actual);
            Assert.IsTrue(identical, message);
        }
    }
}