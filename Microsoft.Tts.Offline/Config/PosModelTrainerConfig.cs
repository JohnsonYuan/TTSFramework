//----------------------------------------------------------------------------
// <copyright file="PosModelTrainerConfig.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements PosModelTrainerConfig configuration
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Config
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// PosModelTrainerConfig.
    /// </summary>
    public class PosModelTrainerConfig : XmlDataFile
    {
        private static XmlSchema _schema;
        private string _defaultPos;
        private Collection<CharFirstRuleConfig> _charFirstRules = new Collection<CharFirstRuleConfig>();

        /// <summary>
        /// Gets Schema of LangDataCompiler.xml.
        /// </summary>
        public override System.Xml.Schema.XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.PosModelTrainer.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets or sets Default pos of the word.
        /// </summary>
        public string DefaultPos
        {
            get { return _defaultPos; }
            set { _defaultPos = value; }
        }

        /// <summary>
        /// Gets Char first rules.
        /// </summary>
        public Collection<CharFirstRuleConfig> CharFirstRules
        {
            get { return _charFirstRules; }
        }

        /// <summary>
        /// Validate the config.
        /// </summary>
        /// <param name="attrSchema">LexicalAttributeSchema.</param>
        public void Validate(LexicalAttributeSchema attrSchema)
        {
            TtsPosSet posSet = TtsPosSet.LoadPosTaggingPosFromSchema(attrSchema);
            if (posSet.Language != attrSchema.Language)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Mismatch language between " +
                    "attribute schema [{0}] and common rule [{1}]",
                    Localor.LanguageToString(attrSchema.Language),
                    Localor.LanguageToString(Language)));
            }

            if (!posSet.Items.ContainsKey(_defaultPos))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Can't find default pos [{0}] in pos tagging pos set",
                    _defaultPos));
            }

            foreach (CharFirstRuleConfig charFirstRuleConfig in _charFirstRules)
            {
                if (!posSet.Items.ContainsKey(charFirstRuleConfig.TargetPos))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Can't find first char rule [charList={0}]'s pos [{1}] in pos tagging pos set",
                        charFirstRuleConfig.FirstCharList, charFirstRuleConfig.TargetPos));
                }

                foreach (char c in charFirstRuleConfig.FirstCharList)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        throw new InvalidDataException(Helper.NeutralFormat(
                            "Can't contain white space in first char list [{0}]",
                            charFirstRuleConfig.FirstCharList));
                    }
                }
            }
        }

        /// <summary>
        /// Load XML file.
        /// </summary>
        /// <param name="xmlDoc">XmlDoc.</param>
        /// <param name="nsmgr">Nsmgr.</param>
        /// <param name="contentController">Content controler.</param>
        protected override void Load(XmlDocument xmlDoc, XmlNamespaceManager nsmgr, object contentController)
        {
            Language = Localor.StringToLanguage(xmlDoc.DocumentElement.Attributes["lang"].InnerText);
            XmlNode defaultPosNode = xmlDoc.DocumentElement.SelectSingleNode(@"//tts:commonLexicalRules/@defaultPos", nsmgr);
            if (defaultPosNode != null)
            {
                _defaultPos = defaultPosNode.InnerText.Trim();
            }

            XmlNodeList firstCharRulesNodeList = xmlDoc.DocumentElement.SelectNodes(
                    @"//tts:commonLexicalRules/tts:firstChar", nsmgr);
            if (firstCharRulesNodeList != null)
            {
                foreach (XmlNode firstCharNode in firstCharRulesNodeList)
                {
                    CharFirstRuleConfig charFirstRule = new CharFirstRuleConfig();
                    charFirstRule.FirstCharList = firstCharNode.Attributes["charList"].InnerText.Trim();
                    charFirstRule.TargetPos = firstCharNode.Attributes["targetPos"].InnerText.Trim();
                    _charFirstRules.Add(charFirstRule);
                }
            }
        }

        /// <summary>
        /// Save Pos model trainer config file into Xml writer.
        /// </summary>
        /// <param name="writer">Writer file to save into.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void PerformanceSave(XmlWriter writer, object contentController)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteStartElement("posModelTrainer", "http://schemas.microsoft.com/tts");
            writer.WriteAttributeString("language", Localor.LanguageToString(Language));

            writer.WriteStartElement("commonLexicalRules");
            writer.WriteAttributeString("defaultPos", _defaultPos);

            foreach (CharFirstRuleConfig charFirstRule in _charFirstRules)
            {
                writer.WriteStartElement("firstChar");
                writer.WriteAttributeString("charList", charFirstRule.FirstCharList);
                writer.WriteAttributeString("targetPos", charFirstRule.TargetPos);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        /// <summary>
        /// CharFirstRuleConfig.
        /// </summary>
        public class CharFirstRuleConfig
        {
            private string _firstCharList;
            private string _targetPos;

            /// <summary>
            /// Gets or sets First char list.
            /// </summary>
            public string FirstCharList
            {
                get { return _firstCharList; }
                set { _firstCharList = value; }
            }

            /// <summary>
            /// Gets or sets Target pos.
            /// </summary>
            public string TargetPos
            {
                get { return _targetPos; }
                set { _targetPos = value; }
            }
        }
    }
}