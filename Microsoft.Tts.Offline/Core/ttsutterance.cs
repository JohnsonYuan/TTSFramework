//----------------------------------------------------------------------------
// <copyright file="TtsUtterance.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements TTS utterance class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Text;
    using System.Xml;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.Offline.Viterbi;

    /// <summary>
    /// Tts utterance.
    /// </summary>
    public class TtsUtterance
    {
        #region Fields

        private string _rawText;
        private string _tnedText;

        private ScriptItem _script;
        private SegmentFile _segmentFile;

        private AcousticItem _acoustic;

        private Collection<WaveUnit> _waveUnits;

        private ViterbiSearch _viterbi;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="TtsUtterance"/> class.
        /// </summary>
        /// <param name="language">Language of the utterance.</param>
        /// <param name="engine">Engine of the utterance.</param>
        public TtsUtterance(Language language, EngineType engine)
        {
            _script = Localor.CreateScriptItem(language, engine);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Text-normalized string.
        /// </summary>
        public string TNedText
        {
            get
            {
                return _tnedText;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _tnedText = value;
            }
        }

        /// <summary>
        /// Gets or sets Viterbi.
        /// </summary>
        public ViterbiSearch Viterbi
        {
            get
            {
                return _viterbi;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _viterbi = value;
            }
        }

        /// <summary>
        /// Gets Wave units.
        /// </summary>
        public Collection<WaveUnit> WaveUnits
        {
            get
            {
                if (_waveUnits == null)
                {
                    _waveUnits = new Collection<WaveUnit>();
                }

                return _waveUnits;
            }
        }

        /// <summary>
        /// Gets or sets Acoustic.
        /// </summary>
        public AcousticItem Acoustic
        {
            get
            {
                return _acoustic;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _acoustic = value;
            }
        }

        /// <summary>
        /// Gets or sets Segment file path.
        /// </summary>
        public SegmentFile SegmentFile
        {
            get
            {
                return _segmentFile;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _segmentFile = value;
            }
        }

        /// <summary>
        /// Gets or sets Raw text for text string before normalization.
        /// </summary>
        public string RawText
        {
            get
            {
                return _rawText;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _rawText = value;
            }
        }

        /// <summary>
        /// Gets or sets Script information of this utterance.
        /// </summary>
        public ScriptItem Script
        {
            get
            {
                return _script;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _script = value;
            }
        }

        #endregion

        #region XML serialization and deserialization

        /// <summary>
        /// Save TTS utterance to file in XML format.
        /// </summary>
        /// <param name="utterance">Utterance to save.</param>
        /// <param name="filePath">File to save in.</param>
        public static void SaveAsXml(TtsUtterance utterance, string filePath)
        {
            if (utterance == null)
            {
                throw new ArgumentNullException("utterance");
            }

            if (utterance.Script == null)
            {
                throw new ArgumentException("utterance.Script is null");
            }

            using (XmlTextWriter tw = new XmlTextWriter(filePath, Encoding.Unicode))
            {
                tw.Formatting = Formatting.Indented;
                tw.Indentation = 4;

                tw.WriteStartElement("utterance");
                tw.WriteAttributeString("lang",
                    Localor.LanguageToString(utterance.Script.Language));

                tw.WriteStartElement("s");
                tw.WriteAttributeString("val", utterance.Script.Sentence);
                tw.WriteEndElement();
                tw.WriteStartElement("p");
                tw.WriteAttributeString("val", utterance.Script.Pronunciation);
                tw.WriteEndElement();

                // Save words
                if (!string.IsNullOrEmpty(utterance.Script.Sentence)  &&
                    utterance.Script.Words != null && utterance.Script.Words.Count > 0)
                {
                    tw.WriteStartElement("words");

                    foreach (ScriptWord word in utterance.Script.Words)
                    {
                        tw.WriteStartElement("w");
                        tw.WriteAttributeString("val", word.Grapheme);

                        if (word.WordType == WordType.Normal)
                        {
                            tw.WriteAttributeString("p", word.Pronunciation);
                        }
                        else
                        {
                            // word.WordType != WordType.Normal
                            tw.WriteAttributeString("type",
                                Enum.GetName(typeof(WordType), word.WordType));
                        }

                        if (word.Pos != PartOfSpeech.Unknown)
                        {
                            tw.WriteAttributeString("pos",
                                Enum.GetName(typeof(PartOfSpeech), word.WordType));
                        }

                        if (word.Emphasis != TtsEmphasis.None)
                        {
                            tw.WriteAttributeString("emphasis",
                                Enum.GetName(typeof(TtsEmphasis), word.WordType));
                        }

                        if (word.Break != TtsBreak.Phone)
                        {
                            tw.WriteAttributeString("break",
                                Enum.GetName(typeof(TtsBreak), word.Break));
                        }

                        tw.WriteEndElement();
                    }

                    tw.WriteEndElement();
                }

                // Save units
                if (!string.IsNullOrEmpty(utterance.Script.Pronunciation)  &&
                    utterance.Script.Units != null && utterance.Script.Units.Count > 0)
                {
                    SaveUnitsAsXml(utterance, tw);
                }

                tw.WriteEndElement();
            }
        }

        /// <summary>
        /// Load TtsUtterance from XML data file.
        /// </summary>
        /// <param name="filePath">File to load from.</param>
        /// <returns>Utterance instance read from file.</returns>
        public static TtsUtterance ReadFromXml(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            TtsUtterance utterance = null;
            using (XmlTextReader reader = new XmlTextReader(filePath))
            {
                while (reader.Read())
                {
                    // Ignore XmlDeclaration, ProcessingInstruction, 
                    // Comment, DocumentType, Entity, Notation.
                    if ((reader.NodeType == XmlNodeType.Element)
                        && (reader.LocalName == "utterance"))
                    {
                        Language language = Localor.StringToLanguage(
                            reader.GetAttribute("lang"));
                        utterance = new TtsUtterance(language, EngineType.Tts30);

                        ProcessUtterance(reader, utterance);
                    }
                }
            }

            return utterance;
        }

        /// <summary>
        /// Save TTS units to file in XML format.
        /// </summary>
        /// <param name="utterance">Units of utterance to save.</param>
        /// <param name="writer">XML text writer to write units information.</param>
        private static void SaveUnitsAsXml(TtsUtterance utterance, XmlTextWriter writer)
        {
            writer.WriteStartElement("units");

            Phoneme phoneme = Localor.GetPhoneme(utterance.Script.Language, utterance.Script.Engine);
            foreach (TtsUnit unit in utterance.Script.Units)
            {
                writer.WriteStartElement("u");
                writer.WriteAttributeString("val", unit.MetaUnit.Name);

                writer.WriteAttributeString("iSyll",
                    Enum.GetName(typeof(PosInSyllable), unit.Feature.PosInSyllable));
                writer.WriteAttributeString("iWord",
                    Enum.GetName(typeof(PosInWord), unit.Feature.PosInWord));
                writer.WriteAttributeString("iSent",
                    Enum.GetName(typeof(PosInSentence), unit.Feature.PosInSentence));

                writer.WriteAttributeString("lPh", phoneme.TtsId2Phone(unit.Feature.LeftContextPhone));
                writer.WriteAttributeString("rPh", phoneme.TtsId2Phone(unit.Feature.RightContextPhone));

                writer.WriteAttributeString("st",
                    Enum.GetName(typeof(TtsStress), unit.Feature.TtsStress));
                writer.WriteAttributeString("em",
                    Enum.GetName(typeof(TtsEmphasis), unit.Feature.TtsEmphasis));

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Read and parse utterance data from the XML text reader.
        /// </summary>
        /// <param name="reader">XML text reader to read data from.</param>
        /// <param name="utterance">Target utterance to save.</param>
        private static void ProcessUtterance(XmlTextReader reader, TtsUtterance utterance)
        {
            // Move to containing element of attributes
            reader.MoveToElement();
            if (!reader.IsEmptyElement)
            {
                // Move to first child element
                reader.Read();

                // Process each child element while not at end element
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    bool invalidNode = false;
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.LocalName)
                        {
                            case "s":
                                utterance.Script.Sentence = reader.GetAttribute("val");
                                reader.Skip();
                                break;
                            case "p":
                                utterance.Script.Pronunciation = reader.GetAttribute("val");
                                reader.Skip();
                                break;
                            case "words":
                                ProcessWords(reader, utterance);
                                break;
                            case "units":
                                ProcessUnits(reader, utterance);
                                break;
                            default:
                                invalidNode = true;
                                break;
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Text)
                    {
                        reader.Skip();
                    }
                    else
                    {
                        // Skip over non-element/text node types
                        reader.Skip();
                    }

                    if (invalidNode)
                    {
                        throw new ArgumentException(reader.Name);
                    }
                }
            }

            // Move to next sibling
            reader.Read();
        }

        /// <summary>
        /// Read and parse words data from the XML text reader to utterance.
        /// </summary>
        /// <param name="reader">XML text reader to read data from.</param>
        /// <param name="utterance">Target utterance to save result words.</param>
        private static void ProcessWords(XmlTextReader reader, TtsUtterance utterance)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            if (utterance == null)
            {
                throw new ArgumentNullException("utterance");
            }

            utterance.Script.Words.Clear();

            // Move to containing element of attributes
            reader.MoveToElement();
            if (!reader.IsEmptyElement)
            {
                // Move to first child element
                reader.Read();

                // Process each child element while not at end element
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    bool invalidNode = false;
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.LocalName)
                        {
                            case "w":
                                ProcessWord(reader, utterance);
                                break;
                            default:
                                invalidNode = true;
                                break;
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Text)
                    {
                        reader.Skip();
                    }
                    else
                    {
                        // Skip over non-element/text node types
                        reader.Skip();
                    }

                    if (invalidNode)
                    {
                        throw new ArgumentException("Invalid element '" + reader.Name + "'");
                    }
                }
            }

            // Move to next sibling
            reader.Read();
        }

        /// <summary>
        /// Read and parse word data from the XML text reader to utterance.
        /// </summary>
        /// <param name="reader">XML text reader to read data from.</param>
        /// <param name="utterance">Target utterance to save result words.</param>
        private static void ProcessWord(XmlTextReader reader, TtsUtterance utterance)
        {
            ScriptWord word = new ScriptWord(utterance.Script.Language);
            word.Grapheme = reader.GetAttribute("val");

            if (reader.GetAttribute("p") != null)
            {
                word.Pronunciation = reader.GetAttribute("p");
            }

            if (reader.GetAttribute("pos") != null)
            {
                word.Pos = (PartOfSpeech)Enum.Parse(typeof(PartOfSpeech), reader.GetAttribute("pos"));
            }

            if (reader.GetAttribute("emphasis") != null)
            {
                word.Emphasis =
                    (TtsEmphasis)Enum.Parse(typeof(TtsEmphasis), reader.GetAttribute("emphasis"));
            }

            if (reader.GetAttribute("break") != null)
            {
                word.Break = (TtsBreak)Enum.Parse(typeof(TtsBreak), reader.GetAttribute("break"));
            }

            if (reader.GetAttribute("type") != null)
            {
                word.WordType = (WordType)Enum.Parse(typeof(WordType), reader.GetAttribute("type"));
            }

            utterance.Script.Words.Add(word);
            reader.Skip();
        }

        /// <summary>
        /// Read and parse units data from the XML text reader to utterance.
        /// </summary>
        /// <param name="reader">XML text reader to read data from.</param>
        /// <param name="utterance">Target utterance to save result units.</param>
        private static void ProcessUnits(XmlTextReader reader, TtsUtterance utterance)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            if (utterance == null)
            {
                throw new ArgumentNullException("utterance");
            }

            utterance.Script.Units.Clear();

            // Move to containing element of attributes
            reader.MoveToElement();
            if (!reader.IsEmptyElement)
            {
                // Move to first child element
                reader.Read();

                // Process each child element while not at end element
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    bool invalidNode = false;
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.LocalName)
                        {
                            case "u":
                                ParseUnit(reader, utterance);
                                reader.Skip();
                                break;
                            default:
                                invalidNode = true;
                                break;
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Text)
                    {
                        reader.Skip();
                    }
                    else
                    {
                        // Skip over non-element/text node types
                        reader.Skip();
                    }

                    if (invalidNode)
                    {
                        throw new ArgumentException("Invalid element '" + reader.Name + "'");
                    }
                }
            }

            // Move to next sibling
            reader.Read();
        }

        /// <summary>
        /// Read and parse unit data from the XML text reader to utterance.
        /// </summary>
        /// <param name="reader">XML text reader to read data from.</param>
        /// <param name="utterance">Target utterance to save result units.</param>
        private static void ParseUnit(XmlTextReader reader, TtsUtterance utterance)
        {
            TtsUnit unit = new TtsUnit(utterance.Script.Language);
            unit.MetaUnit.Name = reader.GetAttribute("val");

            if (reader.GetAttribute("iSyll") != null)
            {
                unit.Feature.PosInSyllable =
                    (PosInSyllable)Enum.Parse(typeof(PosInSyllable), reader.GetAttribute("iSyll"));
            }

            if (reader.GetAttribute("iWord") != null)
            {
                unit.Feature.PosInWord =
                    (PosInWord)Enum.Parse(typeof(PosInWord), reader.GetAttribute("iWord"));
            }

            if (reader.GetAttribute("iSent") != null)
            {
                unit.Feature.PosInSentence =
                    (PosInSentence)Enum.Parse(typeof(PosInSentence), reader.GetAttribute("iSent"));
            }

            Phoneme phoneme = Localor.GetPhoneme(utterance.Script.Language, utterance.Script.Engine);
            unit.Feature.LeftContextPhone = phoneme.TtsPhone2Id(reader.GetAttribute("lPh"));
            unit.Feature.RightContextPhone = phoneme.TtsPhone2Id(reader.GetAttribute("rPh"));

            if (reader.GetAttribute("em") != null)
            {
                unit.Feature.TtsEmphasis =
                    (TtsEmphasis)Enum.Parse(typeof(TtsEmphasis),
                    reader.GetAttribute("em"));
            }

            if (reader.GetAttribute("st") != null)
            {
                unit.Feature.TtsStress =
                    (TtsStress)Enum.Parse(typeof(TtsStress), reader.GetAttribute("st"));
            }

            utterance.Script.Units.Add(unit);
        }
        #endregion
    }
}