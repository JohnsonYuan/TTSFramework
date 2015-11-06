//----------------------------------------------------------------------------
// <copyright file="UnitGeneratorDataCompiler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Unit Generator Data Compiler
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
    using System.Xml;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;

    /// <summary>
    /// Syllabify Rule Compiler Error definition.
    /// </summary>
    public enum UnitGeneratorDataCompilerError
    {
        /// <summary>
        /// Wrong Rule Side
        /// {0}: side
        /// {1}: rule content.
        /// </summary>
        [ErrorAttribute(Message = "Trunc Rule side is wrong: {0} {1}.",
            Severity = ErrorSeverity.Warning)]
        WrongRuleSide,

        /// <summary>
        /// Rule Length Exceeded
        /// {0}: maximal rule length
        /// {1}: rule content.
        /// </summary>
        [ErrorAttribute(Message = "Trunc rule length is over {0} {1}.",
            Severity = ErrorSeverity.Warning)]
        RuleLengthExceeded,

        /// <summary>
        /// Invalid Phone
        /// {0}: phone name.
        /// </summary>
        [ErrorAttribute(Message = "Invalid phone /{0}/ is found in Trunc rule.",
            Severity = ErrorSeverity.Warning)]
        InvalidPhone,

        /// <summary>
        /// Use Default Pause Length.
        /// </summary>
        [ErrorAttribute(Message = "Use default value for pause break length.",
            Severity = ErrorSeverity.Warning)]
        UseDefaultPauseLength,

        /// <summary>
        /// Invalid Phone Set.
        /// </summary>
        [ErrorAttribute(Message = "Invalid phoneset for compiling unit geneartor data.")]
        InvalidPhoneSet
    }

    /// <summary>
    /// Truncate Nucleus Rule struct.
    /// </summary>
    internal struct TruncateNucleusRule
    {
        // from left or right
        public int Direction;

        // ended with 0
        public short[] Ids;
    }

    /// <summary>
    /// Unit Generator Data Compiler.
    /// </summary>
    public class UnitGeneratorDataCompiler
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="UnitGeneratorDataCompiler"/> class from being created.
        /// </summary>
        private UnitGeneratorDataCompiler()
        {
        }

        /// <summary>
        /// Compiler.
        /// </summary>
        /// <param name="truncRuleFileName">File path of trunc rule.</param>
        /// <param name="phoneSet">Phone set.</param>
        /// <param name="outputStream">Output Stream.</param>
        /// <returns>ErrorSet.</returns>
        public static ErrorSet Compile(string truncRuleFileName,
            TtsPhoneSet phoneSet, Stream outputStream)
        {
            if (string.IsNullOrEmpty(truncRuleFileName))
            {
                throw new ArgumentNullException("truncRuleFileName");
            }

            // pauseLengthFileName could be null
            if (phoneSet == null)
            {
                throw new ArgumentNullException("phoneSet");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            ErrorSet errorSet = new ErrorSet();
            phoneSet.Validate();
            if (phoneSet.ErrorSet.Contains(ErrorSeverity.MustFix))
            {
                errorSet.Add(UnitGeneratorDataCompilerError.InvalidPhoneSet);
            }
            else
            {
                BinaryWriter bw = new BinaryWriter(outputStream);
                {
                    errorSet.Merge(CompTruncRuleData(truncRuleFileName, phoneSet, bw));
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Compile the trunc rule into binary writer.
        /// </summary>
        /// <param name="truncRuleFileName">File path of trunc rule.</param>
        /// <param name="phoneSet">Phone set.</param>
        /// <param name="bw">Binary writer.</param>
        /// <returns>Error.</returns>
        private static ErrorSet CompTruncRuleData(string truncRuleFileName, TtsPhoneSet phoneSet, BinaryWriter bw)
        {
            // maximum truncate rule length is 5 phonmes currently
            const int MaxTruncRuleLength = 5;
            ErrorSet errorSet = new ErrorSet();
            List<TruncateNucleusRule> rules = new List<TruncateNucleusRule>();

            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(truncRuleFileName);
            XmlNamespaceManager nm = new XmlNamespaceManager(xmldoc.NameTable);
            nm.AddNamespace("tts", "http://schemas.microsoft.com/tts/toolsuite");
            XmlNodeList nodeList = xmldoc.DocumentElement.SelectNodes(
                "/tts:offline/tts:truncateRules/tts:truncateRule", nm);
            if (nodeList != null)
            {
                foreach (XmlNode node in nodeList)
                {
                    XmlNodeList phoneNodeList;
                    XmlElement xmlNode = node as XmlElement;
                    string side = xmlNode.GetAttribute("side");
                    int direction = 0;

                    if (side.Equals("Right", StringComparison.OrdinalIgnoreCase))
                    {
                        direction = 2;  // TruncFromRight
                    }
                    else if (side.Equals("Left", StringComparison.OrdinalIgnoreCase))
                    {
                        direction = 1; // TruncFromLeft
                    }
                    else
                    {
                        errorSet.Add(UnitGeneratorDataCompilerError.WrongRuleSide, 
                            side, xmlNode.InnerXml);
                    }

                    phoneNodeList = xmlNode.SelectNodes("tts:phone", nm);
                    if (phoneNodeList.Count > MaxTruncRuleLength)
                    {
                        errorSet.Add(UnitGeneratorDataCompilerError.RuleLengthExceeded,
                            MaxTruncRuleLength.ToString(CultureInfo.InvariantCulture), xmlNode.InnerXml);
                    }
                    else
                    {
                        int idx = 0;
                        short[] ids = new short[MaxTruncRuleLength + 1];

                        foreach (XmlNode phoneNode in phoneNodeList)
                        {
                            XmlElement xmlPhoneNode = phoneNode as XmlElement;

                            string phoneValue = xmlPhoneNode.GetAttribute("value");
                            Phone phone = phoneSet.GetPhone(phoneValue);
                            if (phone != null)
                            {
                                ids[idx++] = (short)phone.Id;
                            }
                            else
                            {
                                errorSet.Add(UnitGeneratorDataCompilerError.InvalidPhone, phoneValue);
                            }
                        }

                        ids[idx] = 0;
                        TruncateNucleusRule rule = new TruncateNucleusRule();
                        rule.Ids = ids;
                        rule.Direction = direction;
                        rules.Add(rule);
                    }
                }
            }

            // write the data
            bw.Write(rules.Count);
            foreach (TruncateNucleusRule ci in rules)
            {
                bw.Write(ci.Direction);
                for (int i = 0; i < ci.Ids.Length; i++)
                {
                    bw.Write(BitConverter.GetBytes(ci.Ids[i]));
                }
            }

            return errorSet;
        }
    }
}