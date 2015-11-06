//----------------------------------------------------------------------------
// <copyright file="SyllabifyRuleCompiler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Syllabify Rule Compiler
// </summary>
//----------------------------------------------------------------------------
namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;

    /// <summary>
    /// Syllabify Rule Compiler Error definition.
    /// </summary>
    public enum SyllabifyRuleCompilerError
    {
        /// <summary>
        /// Rule Length Exceeded
        /// Parameters: 
        /// {0}: maximal rule length
        /// {1}: rule content.
        /// </summary>
        [ErrorAttribute(Message = "Syllabify Rule length is over {0}, {1}.",
            Severity = ErrorSeverity.Warning)]
        RuleLengthExceeded,

        /// <summary>
        /// Invalid Phone
        /// Parameters: 
        /// {0}: phone name.
        /// </summary>
        [ErrorAttribute(Message = "Invalid phone /{0}/ is found in syllabify rule.",
            Severity = ErrorSeverity.MustFix)]
        InvalidPhone,

        /// <summary>
        /// Invalid Phone Set.
        /// </summary>
        [ErrorAttribute(Message = "Invalid phoneset for compiling syllabify rule.")]
        InvalidPhoneSet
    }

    /// <summary>
    /// Syllabify Rule Compiler.
    /// </summary>
    public class SyllabifyRuleCompiler
    {
        #region Fields
        private static string _ttsSchemaUri = "http://schemas.microsoft.com/tts";
        #endregion

        /// <summary>
        /// Prevents a default instance of the <see cref="SyllabifyRuleCompiler"/> class from being created.
        /// </summary>
        private SyllabifyRuleCompiler()
        {
        }

        /// <summary>
        /// Compiler.
        /// </summary>
        /// <param name="syllabifyRuleFileName">Path of syllabify rule.</param>
        /// <param name="phoneSet">Phone set.</param>
        /// <param name="outputStream">Output Stream.</param>
        /// <returns>ErrorSet.</returns>
        public static ErrorSet Compile(string syllabifyRuleFileName, TtsPhoneSet phoneSet, 
            Stream outputStream)
        {
            if (string.IsNullOrEmpty(syllabifyRuleFileName))
            {
                throw new ArgumentNullException("syllabifyRuleFileName");
            }

            if (phoneSet == null)
            {
                throw new ArgumentNullException("phoneSet");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            // maximum rule length is 3 phonmes currently
            const int MaxRuleLength = 3;
            ErrorSet errorSet = new ErrorSet();
            phoneSet.Validate();
            if (phoneSet.ErrorSet.Contains(ErrorSeverity.MustFix))
            {
                errorSet.Add(SyllabifyRuleCompilerError.InvalidPhoneSet);
            }
            else
            {
                BinaryWriter bw = new BinaryWriter(outputStream);
                {
                    List<ushort[]> rules = new List<ushort[]>();

                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.Load(syllabifyRuleFileName);
                    XmlNamespaceManager nm = new XmlNamespaceManager(xmldoc.NameTable);
                    nm.AddNamespace("tts", _ttsSchemaUri);
                    XmlNodeList nodeList = xmldoc.DocumentElement.SelectNodes(
                        "/tts:syllabifyRules/tts:initialConsonants", nm);

                    if (nodeList != null)
                    {
                        foreach (XmlNode node in nodeList)
                        {
                            XmlNodeList phoneNodeList;
                            XmlElement xmlNode = node as XmlElement;

                            phoneNodeList = xmlNode.SelectNodes("tts:phone", nm);
                            if (phoneNodeList.Count > MaxRuleLength)
                            {
                                errorSet.Add(SyllabifyRuleCompilerError.RuleLengthExceeded,
                                    MaxRuleLength.ToString(CultureInfo.InvariantCulture), xmlNode.InnerXml);
                            }
                            else
                            {
                                ushort[] rule = new ushort[MaxRuleLength + 1];
                                int idx = 0;

                                foreach (XmlNode phoneNode in phoneNodeList)
                                {
                                    XmlElement xmlPhoneNode = phoneNode as XmlElement;

                                    string phoneValue = xmlPhoneNode.GetAttribute("value");
                                    Phone phone = phoneSet.GetPhone(phoneValue);
                                    if (phone != null)
                                    {
                                        rule[idx++] = (ushort)phone.Id;
                                    }
                                    else
                                    {
                                        errorSet.Add(SyllabifyRuleCompilerError.InvalidPhone, phoneValue);
                                    }
                                }

                                rule[idx] = 0;
                                rules.Add(rule);
                            }
                        }

                        bw.Write(rules.Count);
                        foreach (ushort[] ci in rules)
                        {
                            for (int i = 0; i < ci.Length; i++)
                            {
                                bw.Write(BitConverter.GetBytes(ci[i]));
                            }
                        }
                    }
                }
            }

            return errorSet;
        }
    }
}