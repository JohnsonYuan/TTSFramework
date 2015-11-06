//----------------------------------------------------------------------------
// <copyright file="ScriptFeatureImportConfig.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements script feature import configuration
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.ScriptSynthesizer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// Script feature import config.
    /// </summary>
    public class ScriptFeatureImportConfig : IConfigSnippet
    {
        #region Fields

        /// <summary>
        /// Feature update setting, which feature need be updated.
        /// </summary>
        private CheckingOptions _consistencyCheckOption;

        private bool _updateNormalWordDuration;
        private bool _updateSilenceWord;
        private bool _updateScriptSilenceDuration;
        private bool _updateFixedSilenceDuration;
        private int _intermPhraseBreakLength;
        private int _intonaPhraseBreakLength;
        private int _sentenceBreakLength;
        private bool _updateF0;
        private int _fixF0NoConsistenceNum;
        private bool _updatePronunciation;
        private bool _updatePartOfSpeech;
        private bool _updateBreakLevel;
        private bool _updateEmphasis;
        private bool _updateToBIAccent;
        private bool _updateBoundaryTone;

        #endregion

        #region Public types

        /// <summary>
        /// Consistency checking options.
        /// </summary>
        public enum CheckingOptions
        {
            /// <summary>
            /// Do not check.
            /// </summary>
            None = 0,

            /// <summary>
            /// Check the consistency of words.
            /// </summary>
            Word = 1
        }

        #endregion

        #region Properties

        #region Public static properties

        /// <summary>
        /// Gets configuration schema.
        /// </summary>
        public static XmlSchemaInclude SchemaInclude
        {
            get
            {
                XmlSchemaInclude included = new XmlSchemaInclude();
                included.Schema =
                    XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.ScriptSynthesizer.xsd");

                return included;
            }
        }

        #endregion

        /// <summary>
        /// Gets or sets consistency Check Option.
        /// </summary>
        public CheckingOptions ConsistencyCheckOption
        {
            get { return _consistencyCheckOption; }
            set { _consistencyCheckOption = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether update normal words' duration.
        /// </summary>
        public bool UpdateNormalWordDuration
        {
            get { return _updateNormalWordDuration; }
            set { _updateNormalWordDuration = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether update silence words.
        /// </summary>
        public bool UpdateSilenceWord
        {
            get { return _updateSilenceWord; }
            set { _updateSilenceWord = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether update silence words' duration use duration in script.
        /// </summary>
        public bool UpdateScriptSilenceDuration
        {
            get { return _updateScriptSilenceDuration; }
            set { _updateScriptSilenceDuration = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether update silence words' duration use fixed length.
        /// </summary>
        public bool UpdateFixedSilenceDuration
        {
            get { return _updateFixedSilenceDuration; }
            set { _updateFixedSilenceDuration = value; }
        }

        /// <summary>
        /// Gets or sets fixed silence length of interm_phrase break level.
        /// </summary>
        public int IntermPhraseBreakLength
        {
            get { return _intermPhraseBreakLength; }
            set { _intermPhraseBreakLength = value; }
        }

        /// <summary>
        /// Gets or sets fixed silence length of intona_phrase break level.
        /// </summary>
        public int IntonaPhraseBreakLength
        {
            get { return _intonaPhraseBreakLength; }
            set { _intonaPhraseBreakLength = value; }
        }

        /// <summary>
        /// Gets or sets fixed silence length of sentence break level.
        /// </summary>
        public int SentenceBreakLength
        {
            get { return _sentenceBreakLength; }
            set { _sentenceBreakLength = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether update f0.
        /// </summary>
        public bool UpdateF0
        {
            get { return _updateF0; }
            set { _updateF0 = value; }
        }

        /// <summary>
        /// Gets or sets the number to be fixed of f0 no consistence.
        /// </summary>
        public int FixF0NoConsistenceNum
        {
            get { return _fixF0NoConsistenceNum; }
            set { _fixF0NoConsistenceNum = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether update pronunciation.
        /// </summary>
        public bool UpdatePronunciation
        {
            get { return _updatePronunciation; }
            set { _updatePronunciation = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether update POS.
        /// </summary>
        public bool UpdatePartOfSpeech
        {
            get { return _updatePartOfSpeech; }
            set { _updatePartOfSpeech = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether update break level.
        /// </summary>
        public bool UpdateBreakLevel
        {
            get { return _updateBreakLevel; }
            set { _updateBreakLevel = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether update emphasis.
        /// </summary>
        public bool UpdateEmphasis
        {
            get { return _updateEmphasis; }
            set { _updateEmphasis = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether update pitch accent.
        /// </summary>
        public bool UpdateToBIAccent
        {
            get { return _updateToBIAccent; }
            set { _updateToBIAccent = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether update boundary tone.
        /// </summary>
        public bool UpdateBoundaryTone
        {
            get { return _updateBoundaryTone; }
            set { _updateBoundaryTone = value; }
        }

        #endregion

        #region Public static members

        /// <summary>
        /// Parse config snippet and create the config object.
        /// </summary>
        /// <param name="nsmgr">Namespace manager.</param>
        /// <param name="configNode">Xml node containing the config.</param>
        /// <returns>The config object.</returns>
        public static ScriptFeatureImportConfig ParseConfig(XmlNamespaceManager nsmgr, XmlNode configNode)
        {
            ScriptFeatureImportConfig scriptFeatureImportConfig = null;
            if (nsmgr != null && configNode != null)
            {
                scriptFeatureImportConfig = new ScriptFeatureImportConfig();
                scriptFeatureImportConfig.Load(nsmgr, configNode);
            }

            return scriptFeatureImportConfig;
        }

        #endregion

        #region IConfigSnippet methods

        /// <summary>
        /// Load configuration from xml snippet.
        /// </summary>
        /// <param name="nsmgr">Namespace manager.</param>
        /// <param name="configNode">Xml node containing the config.</param>
        public void Load(XmlNamespaceManager nsmgr, XmlNode configNode)
        {
            if (nsmgr == null)
            {
                throw new ArgumentNullException("nsmgr");
            }

            if (configNode == null)
            {
                throw new ArgumentNullException("configNode");
            }

            if (configNode.Name != "scriptFeatureImport")
            {
                throw new InvalidDataException("scriptFeatureImport element expected!");
            }

            XmlNode node = configNode.SelectSingleNode(@"tts:consistencyCheckOption/@value", nsmgr);
            _consistencyCheckOption = (CheckingOptions)Enum.Parse(typeof(CheckingOptions), node.InnerText);

            node = configNode.SelectSingleNode(@"tts:normalWordDuration/@update", nsmgr);
            _updateNormalWordDuration = ParseBoolValue(node);

            node = configNode.SelectSingleNode(@"tts:toBI/tts:breakLevel/@update", nsmgr);
            _updateBreakLevel = ParseBoolValue(node);

            if (_updateBreakLevel)
            {
                node = configNode.SelectSingleNode(@"tts:toBI/tts:breakLevel/tts:boundaryTone/@update", nsmgr);
                _updateBoundaryTone = ParseBoolValue(node);
            }
            else
            {
                _updateBoundaryTone = false;
            }

            node = configNode.SelectSingleNode(@"tts:toBI/tts:toBIAccent/@update", nsmgr);
            _updateToBIAccent = ParseBoolValue(node);

            node = configNode.SelectSingleNode(@"tts:silence/tts:silenceWord/@update", nsmgr);
            _updateSilenceWord = ParseBoolValue(node);

            if (_updateSilenceWord)
            {
                node = configNode.SelectSingleNode(@"tts:silence/tts:silenceWord/tts:scriptSilenceDuration/@update", nsmgr);
                _updateScriptSilenceDuration = ParseBoolValue(node);
            }
            else
            {
                _updateScriptSilenceDuration = false;
            }

            node = configNode.SelectSingleNode(@"tts:silence/tts:fixedSilenceDuration/@update", nsmgr);
            _updateFixedSilenceDuration = ParseBoolValue(node);

            if (_updateSilenceWord && _updateFixedSilenceDuration)
            {
                throw new InvalidDataException("Cannot implement updating script silence word and fixed silence duration together.");
            }

            if (!_updateFixedSilenceDuration)
            {
                _intermPhraseBreakLength = 0;
                _intonaPhraseBreakLength = 0;
                _sentenceBreakLength = 0;
            }
            else
            {
                node = configNode.SelectSingleNode(@"tts:silence/tts:fixedSilenceDuration/tts:fixedSilenceLength/tts:intermPhraseBreakLength/@value", nsmgr);
                _intermPhraseBreakLength = ParseIntegerValue(node);

                node = configNode.SelectSingleNode(@"tts:silence/tts:fixedSilenceDuration/tts:fixedSilenceLength/tts:intonaPhraseBreakLength/@value", nsmgr);
                _intonaPhraseBreakLength = ParseIntegerValue(node);

                node = configNode.SelectSingleNode(@"tts:silence/tts:fixedSilenceDuration/tts:fixedSilenceLength/tts:sentenceBreakLength/@value", nsmgr);
                _sentenceBreakLength = ParseIntegerValue(node);
            }

            node = configNode.SelectSingleNode(@"tts:f0/@update", nsmgr);
            _updateF0 = ParseBoolValue(node);

            if (!_updateF0)
            {
                _fixF0NoConsistenceNum = 0;
            }
            else
            {
                node = configNode.SelectSingleNode(@"tts:f0/tts:fixF0NoConsistenceNum/@value", nsmgr);
                _fixF0NoConsistenceNum = ParseIntegerValue(node);
            }

            node = configNode.SelectSingleNode(@"tts:pronunciation/@update", nsmgr);
            _updatePronunciation = ParseBoolValue(node);

            node = configNode.SelectSingleNode(@"tts:partOfSpeech/@update", nsmgr);
            _updatePartOfSpeech = ParseBoolValue(node);

            node = configNode.SelectSingleNode(@"tts:emphasis/@update", nsmgr);
            _updateEmphasis = ParseBoolValue(node);
        }

        /// <summary>
        /// Save configuration file as a xml snippet.
        /// </summary>
        /// <param name="dom">Xml document to be saved into.</param>
        /// <param name="parent">Xml parent element to be saved into.</param>
        /// <param name="schema">Xml schema.</param>
        public void Save(XmlDocument dom, XmlElement parent, XmlSchema schema)
        {
            if (dom == null)
            {
                throw new ArgumentNullException("dom");
            }

            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            if (schema == null)
            {
                throw new ArgumentNullException("schema");
            }

            if (_updateScriptSilenceDuration && _updateFixedSilenceDuration)
            {
                throw new InvalidDataException("Cannot implement updating script and fixed silence duration together.");
            }

            // root element of this snippet
            XmlElement root = dom.CreateElement("scriptFeatureImport", schema.TargetNamespace);
            parent.AppendChild(root);

            XmlHelper.AppendElement(dom, root, "consistencyCheckOption", "value",
                _consistencyCheckOption.ToString(), schema);

            XmlHelper.AppendElement(dom, root, "normalWordDuration", "update",
                _updateNormalWordDuration.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture), schema);

            XmlElement tobiUpdateEle = dom.CreateElement("toBI", schema.TargetNamespace);
            XmlElement breakLevelUpdateEle = dom.CreateElement("breakLevel", schema.TargetNamespace);
            breakLevelUpdateEle.SetAttribute("update", _updateBreakLevel.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
            XmlHelper.AppendElement(dom, breakLevelUpdateEle, "boundaryTone", "update",
                _updateBoundaryTone.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture), schema);
            tobiUpdateEle.AppendChild(breakLevelUpdateEle);
            XmlHelper.AppendElement(dom, tobiUpdateEle, "toBIAccent", "update",
                _updateToBIAccent.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture), schema);
            root.AppendChild(tobiUpdateEle);

            XmlElement silenceUpdateEle = dom.CreateElement("silence", schema.TargetNamespace);
            XmlElement silenceWordUpdateEle = dom.CreateElement("silenceWord", schema.TargetNamespace);
            silenceWordUpdateEle.SetAttribute("update", _updateSilenceWord.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
            XmlHelper.AppendElement(dom, silenceWordUpdateEle, "scriptSilenceDuration", "update",
                _updateScriptSilenceDuration.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture), schema);
            silenceUpdateEle.AppendChild(silenceWordUpdateEle);
            
            XmlElement updateFixedSilenceDurationEle = CreateFixedSilenceDurationElement(dom, schema);
            silenceUpdateEle.AppendChild(updateFixedSilenceDurationEle);
            root.AppendChild(silenceUpdateEle);

            XmlElement updateF0Ele = dom.CreateElement("f0", schema.TargetNamespace);
            updateF0Ele.SetAttribute("update", _updateF0.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));
            XmlHelper.AppendElement(dom, updateF0Ele, "fixF0NoConsistenceNum", "value",
                _fixF0NoConsistenceNum.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture), schema);
            root.AppendChild(updateF0Ele);

            XmlHelper.AppendElement(dom, root, "pronunciation", "update",
                _updatePronunciation.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture), schema);

            XmlHelper.AppendElement(dom, root, "partOfSpeech", "update",
                _updatePartOfSpeech.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture), schema);

            XmlHelper.AppendElement(dom, root, "emphasis", "update",
                _updateEmphasis.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture), schema);
        }

        /// <summary>
        /// Get pause length to update.
        /// </summary>
        /// <param name="count">The count of pause durations.</param>
        /// <returns>Pause durations array.</returns>
        public int[] GetPauseLengths(int count)
        {
            List<int> durations = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                durations.Add(0);
            }

            if (_updateFixedSilenceDuration)
            {
                durations[(int)TtsPauseLevel.PAU_IDX_SENTENCE] = _sentenceBreakLength;
                durations[(int)TtsPauseLevel.PAU_IDX_PUNC_INTONA] = _intonaPhraseBreakLength;
                durations[(int)TtsPauseLevel.PAU_IDX_INTERM] = _intermPhraseBreakLength;
            }
            else if (!_updateSilenceWord)
            {
                throw new InvalidDataException("Should update silcence word or update fixed silence duration.");
            }

            return durations.ToArray();
        }

        #endregion

        #region Private static methods

        /// <summary>
        /// Parse the configed the bool value of whether update certain data.
        /// </summary>
        /// <param name="node">Config file node.</param>
        /// <returns>Whether update certain data.</returns>
        private static bool ParseBoolValue(XmlNode node)
        {
            bool update = false;
            if (node != null)
            {
                update = bool.Parse(node.InnerText);
            }

            return update;
        }

        /// <summary>
        /// Parse the configed the int value.
        /// </summary>
        /// <param name="node">Config file node.</param>
        /// <returns>The configed integer value.</returns>
        private static int ParseIntegerValue(XmlNode node)
        {
            int value = 0;
            if (node != null)
            {
                value = int.Parse(node.InnerText, CultureInfo.InvariantCulture);
            }

            return value;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Create update fixed silence duration setting as XML format.
        /// </summary>
        /// <param name="dom">Xml document.</param>
        /// <param name="schema">Xml schema specified.</param>
        /// <returns>Update fixed silence duration element.</returns>
        private XmlElement CreateFixedSilenceDurationElement(XmlDocument dom, XmlSchema schema)
        {
            XmlElement updateFixedSilenceDurationEle = dom.CreateElement("fixedSilenceDuration", schema.TargetNamespace);
            updateFixedSilenceDurationEle.SetAttribute("update",
                _updateFixedSilenceDuration.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));

            XmlElement fixedSilenceLengthEle = dom.CreateElement("fixedSilenceLength", schema.TargetNamespace);

            XmlHelper.AppendElement(dom, fixedSilenceLengthEle, "intermPhraseBreakLength", "value",
                _intermPhraseBreakLength.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture), schema);

            XmlHelper.AppendElement(dom, fixedSilenceLengthEle, "intonaPhraseBreakLength", "value",
                _intonaPhraseBreakLength.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture), schema);

            XmlHelper.AppendElement(dom, fixedSilenceLengthEle, "sentenceBreakLength", "value",
                _sentenceBreakLength.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture), schema);

            updateFixedSilenceDurationEle.AppendChild(fixedSilenceLengthEle);

            return updateFixedSilenceDurationEle;
        }        
    
        #endregion
    }
}
