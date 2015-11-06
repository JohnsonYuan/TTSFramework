//----------------------------------------------------------------------------
// <copyright file="DataFileValidator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements data file validator.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Word entry error.
    /// </summary>
    public enum DataFileError
    {
        /// <summary>
        /// Invalid pronunciation in compound rule file.
        /// </summary>
        [ErrorAttribute(Message = "Invalid phone [{0}] in pronunciation [{1}] in line [{2}]",
            Severity = ErrorSeverity.MustFix)]
        InvalidPhoneInPron
    }

    /// <summary>
    /// DataFileValidator.
    /// </summary>
    public class DataFileValidator
    {
        /// <summary>
        /// Validate compound rule file.
        /// </summary>
        /// <param name="filePath">Compound rule file path.</param>
        /// <param name="phoneset">TTS phone set.</param>
        /// <returns>ErrorSet.</returns>
        public static ErrorSet ValidateCompoundRule(string filePath, TtsPhoneSet phoneset)
        {
            // Validate parameter
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (!File.Exists(filePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException), filePath);
            }

            ErrorSet errorSet = new ErrorSet();
            using (XmlTextReader xmlTextReader = new XmlTextReader(filePath))
            {
                while (xmlTextReader.Read())
                {
                    if (xmlTextReader.NodeType == XmlNodeType.Element &&
                        xmlTextReader.Name == "out")
                    {
                        if (xmlTextReader.Read() && xmlTextReader.NodeType == XmlNodeType.Text)
                        {
                            ValidateCompoundRuleNodePron(xmlTextReader.Value.Trim(),
                                phoneset, xmlTextReader.LineNumber, errorSet);
                        }
                    }
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Validate compound rule node pronunciation.
        /// </summary>
        /// <param name="outElementValue">OutElementValue.</param>
        /// <param name="phoneset">TtsPhoneSet.</param>
        /// <param name="lineNumber">LineNumber.</param>
        /// <param name="errorSet">ErrorSet.</param>
        private static void ValidateCompoundRuleNodePron(
            string outElementValue, TtsPhoneSet phoneset,
            int lineNumber, ErrorSet errorSet)
        {
            const string PronPattern = @"\[([^\[\]]+)\]/PRONUNCIATION";

            if (string.IsNullOrEmpty(outElementValue))
            {
                throw new ArgumentException("outElementValue");
            }

            Match match = Regex.Match(outElementValue, PronPattern);
            if (match.Success)
            {
                Debug.Assert(match.Groups.Count == 2);
                string pron = match.Groups[1].ToString();
                string[] phones = pron.Split(new char[] { ' ', '+' },
                    StringSplitOptions.RemoveEmptyEntries);
                foreach (string phone in phones)
                {
                    if (!phoneset.IsPhone(phone))
                    {
                        errorSet.Add(new Error(DataFileError.InvalidPhoneInPron,
                            phone, pron, lineNumber.ToString(CultureInfo.InvariantCulture)));
                    }
                }
            }
        }
    }
}