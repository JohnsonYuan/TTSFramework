//----------------------------------------------------------------------------
// <copyright file="PhonemeMapCompiler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Phoneme Map Compiler
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;

    /// <summary>
    /// Phoneme Map Compiler.
    /// </summary>
    public class PhonemeMapCompiler
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="PhonemeMapCompiler" /> class from being created.
        /// </summary>
        private PhonemeMapCompiler()
        {
        }

        /// <summary>
        /// Compiler.
        /// </summary>
        /// <param name="mapFileName">Path of phoneme mapping file.</param>
        /// <param name="sourceAsId">Whether source phone is phone id, to converted into int.</param>
        /// <param name="outputStream">Output Stream.</param>
        /// <returns>ErrorSet.</returns>
        public static ErrorSet Compile(string mapFileName, bool sourceAsId, Stream outputStream)
        {
            if (string.IsNullOrEmpty(mapFileName))
            {
                throw new ArgumentNullException("mapFileName");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            ErrorSet errorSet = new ErrorSet();
            PhonemeMap phonemeMap = new PhonemeMap();
            phonemeMap.LoadXml(mapFileName);

            // Convert SAPI phoneme string
            if (sourceAsId)
            {
                ConvertSourcePhoneID(phonemeMap.Pairs);
            }

            phonemeMap.Sort();

            // Write the binary mapping file
            BinaryWriter writer = new BinaryWriter(outputStream);
            {
                // Write number of phoneme mapping tables
                writer.Write(1);
                byte[] data = phonemeMap.ToBytes();
                writer.Write(data.Length);
                writer.Write(data);
            }

            return errorSet;
        }

        /// <summary>
        /// Convert source phone IDs (string) to int string.
        /// </summary>
        /// <param name="phonemePairArray">Array of phoneme pair.</param>
        private static void ConvertSourcePhoneID(IList<PhonemePair> phonemePairArray)
        {
            Debug.Assert(phonemePairArray != null);

            foreach (PhonemePair pair in phonemePairArray)
            {
                string thirdPartyPhoneme = pair.ThirdPartyPhoneme;

                // The format of source phoneme is phone ID, for example, "11 34"
                string[] subPhonemes = thirdPartyPhoneme.Split(' ');

                StringBuilder sb = new StringBuilder();
                foreach (string subPhoneme in subPhonemes)
                {
                    short subPhonemeID = Convert.ToInt16(subPhoneme, 10);
                    sb.Append((char)subPhonemeID);
                }

                if (sb.Length == 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Error SAPI phoneme string: {0}", thirdPartyPhoneme);
                    throw new InvalidDataException(message);
                }

                pair.ThirdPartyPhoneme = sb.ToString();
            }
        }
    }

    /// <summary>
    /// Phoneme pair.
    /// </summary>
    internal class PhonemePair
    {
        #region Fields

        private string _thirdPartyPhoneme;
        private string _enginePhoneme;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets The third party phoneme, such as IPA and SAPI.
        /// </summary>
        public string ThirdPartyPhoneme
        {
            get
            {
                return _thirdPartyPhoneme;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _thirdPartyPhoneme = value;
            }
        }

        /// <summary>
        /// Gets or sets The engine phoneme.
        /// </summary>
        public string EnginePhoneme
        {
            get
            {
                return _enginePhoneme;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _enginePhoneme = value;
            }
        }

        #endregion
    }

    /// <summary>
    /// Phonemap mapping between sources, which could be cross language or type of phone set.
    /// </summary>
    internal class PhonemeMap
    {
        #region Const variables

        /// <summary>
        /// The start charcter of binary phoneme map.
        /// </summary>
        public const char StartCharacter = 's';

        /// <summary>
        ///  The delimiter character of binary phoneme map.
        /// </summary>
        public const char DelimiterCharacter = '\x0000';

        #endregion

        #region Fields

        private List<PhonemePair> _pairs;
        private Language _sourceLanguage;
        private Language _targetLanguage;

        #endregion

        #region Properties

        /// <summary>
        /// Gets Phoneme mapping pairs.
        /// </summary>
        public IList<PhonemePair> Pairs
        {
            get
            {
                return _pairs;
            }
        }

        #endregion

        /// <summary>
        /// Sort the PhonemePair by comparing the third party phoneme string.
        /// </summary>
        public void Sort()
        {
            _pairs.Sort(Compare);
        }

        /// <summary>
        /// Serialize current instance into binary stream.
        /// </summary>
        /// <returns>Byte array.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public byte[] ToBytes()
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(ms))
            {
                binaryWriter.Write(BitConverter.GetBytes((short)_sourceLanguage));
                binaryWriter.Write(BitConverter.GetBytes((short)_targetLanguage));
                binaryWriter.Write(BitConverter.GetBytes(StartCharacter));
                binaryWriter.Write(BitConverter.GetBytes(DelimiterCharacter));

                foreach (PhonemePair pair in _pairs)
                {
                    string thirdPartyPhoneme = pair.ThirdPartyPhoneme;

                    foreach (char subPhoneme in thirdPartyPhoneme)
                    {
                        binaryWriter.Write(BitConverter.GetBytes(subPhoneme));
                    }

                    binaryWriter.Write(BitConverter.GetBytes(DelimiterCharacter));

                    // The XML format of engine phoneme is phone ID, for example, "11 34"
                    string enginePhoneme = pair.EnginePhoneme;
                    string[] subEnginePhonemes = enginePhoneme.Split(' ');

                    foreach (string subPhoneme in subEnginePhonemes)
                    {
                        short subPhonemeID = Convert.ToInt16(subPhoneme, 10);

                        binaryWriter.Write(BitConverter.GetBytes(subPhonemeID));
                    }

                    binaryWriter.Write(BitConverter.GetBytes(DelimiterCharacter));
                }
            }

            return ms.ToArray();
        }

        /// <summary>
        /// Read phoneme mapping file.
        /// </summary>
        /// <param name="mapFileName">Mapping file name.</param>
        public void LoadXml(string mapFileName)
        {
            _pairs = new List<PhonemePair>();

            XmlDocument dom = new XmlDocument();
            dom.Load(mapFileName);

            if (dom.DocumentElement.HasAttribute("sourceLanguage"))
            {
                _sourceLanguage = Localor.StringToLanguage(dom.DocumentElement.GetAttribute("sourceLanguage"));
            }
            else
            {
                _sourceLanguage = Language.Neutral;
            }

            if (dom.DocumentElement.HasAttribute("targetLanguage"))
            {
                _targetLanguage = Localor.StringToLanguage(dom.DocumentElement.GetAttribute("targetLanguage"));
            }
            else
            {
                _targetLanguage = Language.Neutral;
            }

            foreach (XmlNode node in dom.DocumentElement.SelectNodes("map"))
            {
                XmlElement phonemeEle = (XmlElement)node.SelectSingleNode("phoneme");
                string thirdPartyPhoneme = phonemeEle.InnerText;
                if (string.IsNullOrEmpty(thirdPartyPhoneme))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Phoneme can't be empty!");
                    throw new InvalidDataException(message);
                }

                XmlElement idEle = (XmlElement)node.SelectSingleNode("id");
                string enginePhoneme = idEle.InnerText;
                if (string.IsNullOrEmpty(enginePhoneme))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Id can't be empty!");
                    throw new InvalidDataException(message);
                }

                PhonemePair pair = new PhonemePair();
                pair.ThirdPartyPhoneme = thirdPartyPhoneme;
                pair.EnginePhoneme = enginePhoneme;

                _pairs.Add(pair);
            }
        }

        /// <summary>
        /// Compare the PhonemePair, used by List.Sort() method.
        /// </summary>
        /// <param name="firstPair">Source PhonemePair.</param>
        /// <param name="secondPair">Destination PhonemPair.</param>
        /// <returns>Compare result.</returns>        
        private static int Compare(PhonemePair firstPair, PhonemePair secondPair)
        {
            return string.CompareOrdinal(firstPair.ThirdPartyPhoneme,
                secondPair.ThirdPartyPhoneme);
        }
    }
}