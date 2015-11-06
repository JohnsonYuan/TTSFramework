//----------------------------------------------------------------------------
// <copyright file="FeatureMeta.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements feature Meta.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// FeatureMeta.
    /// </summary>
    public class FeatureMeta : XmlDataFile
    {
        #region Fields
        /// <summary>
        /// FontMetaFileName .
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "MetaFile", Justification = "this is not win32 metafile")]
        public const string FontMetaFileName = "FontMeta.xml";

        /// <summary>
        /// DefaultBitWidth .
        /// </summary>
        protected const int DefaultBitWidth = 8;

        /// <summary>
        /// Default feature Meta List.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1814:PreferJaggedArraysOverMultidimensional", Justification = "Ignore.")]
        private static readonly int[,] _defaultMetaDataList = new int[,]
        {
            // {FeatureID,BitWidth}
            { (int)TtsFeature.PosInSentence, DefaultBitWidth },
            { (int)TtsFeature.PosInWord, DefaultBitWidth },
            { (int)TtsFeature.PosInSyllable, DefaultBitWidth },
            { (int)TtsFeature.LeftContextPhone, DefaultBitWidth },
            { (int)TtsFeature.RightContextPhone, DefaultBitWidth },
            { (int)TtsFeature.TtsStress, DefaultBitWidth },
            { (int)TtsFeature.TtsEmphasis, DefaultBitWidth },
            { (int)TtsFeature.TtsNeighborPrev, DefaultBitWidth },
        };

        private static XmlSchema _schema;

        private Dictionary<TtsFeature, int> _metaData = new Dictionary<TtsFeature, int>();

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureMeta"/> class.
        /// </summary>
        public FeatureMeta() : base(Language.Neutral)
        {
            InitiateData();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureMeta"/> class.
        /// </summary>
        /// <param name="language">Language of this feature meta.</param>
        public FeatureMeta(Language language)
            : base(language)
        {
            InitiateData();
        }

        #region Properties

        /// <summary>
        /// Gets Configuration schema.
        /// </summary>
        public override XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Schema.fontmeta.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets Featuer Meta.
        /// </summary>
        public Dictionary<TtsFeature, int> Metadata
        {
            get { return _metaData; }
        }

        /// <summary>
        /// Gets The total bit width of one feature vector which is decribed by feature Meta.
        /// </summary>
        public int VectorSize
        {
            get
            {
                int length = 0;
                foreach (TtsFeature key in _metaData.Keys)
                {
                    length += _metaData[key];
                }

                return length;
            }
        }

        /// <summary>
        /// Gets The total bytes of feature Meta bitstream.
        /// </summary>
        public int BinarySize
        {
            get
            {
                // The binary format of Feature Meta:
                // <FeatureNumber>(<FeatureId><BitWidth>){<FeatureNumber>}
                // FeatureNumber:=UINT32, the number of feature meta item.
                // FeatureId:=UINT32, featue id.
                // BitWidth:=UINT32, The bit width of feature value.
                return sizeof(uint) + (_metaData.Count * (sizeof(uint) + sizeof(uint)));
            }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Set defualt feature Meta.
        /// </summary>
        public void InitiateData()
        {
            SetFeatureMeta(_defaultMetaDataList);

            using (StreamReader reader = Localor.LoadResource(Language, FontMetaFileName))
            {
                if (reader != null)
                {
                    Load(reader);
                }
            }
        }

        /// <summary>
        /// Get bitstream of feature Meta.
        /// </summary>
        /// <returns>Bitstream of feature meta.</returns>
        public byte[] ToBytes()
        {
            byte[] buffer = new byte[this.BinarySize];
            int index = 0;

            // The binary format is as follows.
            // Feature Meta := <FeatureNumber>(<FeatureId><BitWidth>){<FeatureNumber>}
            // FeatureNumber:=UINT32, the number of feature meta item.
            // FeatureId:=UINT32, featue id.
            // BitWidth:=UINT32, The bit width of feature value.

            // FeatureNumber:=UINT32, the number of feature meta item.
            Buffer.BlockCopy(BitConverter.GetBytes(_metaData.Count), 0,
                buffer, index, sizeof(uint));
            index += sizeof(uint);

            foreach (TtsFeature featureId in _metaData.Keys)
            {
                // FeatureId:=UINT32, feature id.
                Buffer.BlockCopy(BitConverter.GetBytes((int)featureId), 0,
                    buffer, index, sizeof(uint));
                index += sizeof(uint);

                // BitWidth:=UINT32, bit width of feature value.
                Buffer.BlockCopy(BitConverter.GetBytes(_metaData[featureId]), 0,
                    buffer, index, sizeof(uint));
                index += sizeof(uint);
            }

            return buffer;
        }

        #endregion

        #region Protected Members

        /// <summary>
        /// Set feature meta with given feature meta list.
        /// </summary>
        /// <param name="featureMetaList">Feature Meta list.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1814:PreferJaggedArraysOverMultidimensional", Justification = "Ignore.")]
        protected void SetFeatureMeta(int[,] featureMetaList)
        {
            if (featureMetaList == null)
            {
                throw new ArgumentNullException("featureMetaList");
            }

            int count = featureMetaList.GetLength(0);
            if (count <= 0)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Feature Meta data list is empty.");
                throw new ArgumentException(message);
            }

            _metaData.Clear();

            for (int i = 0; i < count; i++)
            {
                if (featureMetaList[i, 0] < 0 ||
                    featureMetaList[i, 0] >= Enum.GetValues(typeof(TtsFeature)).Length)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Invalid feature id {0}.", featureMetaList[i, 0]);
                    throw new ArgumentException(message);
                }

                TtsFeature featureId = (TtsFeature)featureMetaList[i, 0];

                if (Metadata.ContainsKey(featureId))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Find duplicate feature id {0}.", featureId.ToString());
                    throw new ArgumentException(message);
                }
                else
                {
                    Metadata.Add(featureId, featureMetaList[i, 1]);
                }
            }
        }

        /// <summary>
        /// PerformanceSave.
        /// </summary>
        /// <param name="writer">Writer.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void PerformanceSave(XmlWriter writer, object contentController)
        {
            writer.WriteStartElement("fontMeta", "aa");
            writer.WriteAttributeString("lang", Localor.LanguageToString(Language));

            writer.WriteStartElement("unitFeatures");

            foreach (TtsFeature ttsFeature in Metadata.Keys)
            {
                writer.WriteStartElement("feature");

                writer.WriteAttributeString("name", ttsFeature.ToString());
                writer.WriteAttributeString("bitWidth",
                    Metadata[ttsFeature].ToString(CultureInfo.InvariantCulture));

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Load XML file.
        /// </summary>
        /// <param name="xmlDoc">Xml document.</param>
        /// <param name="nsmgr">Nsmgr.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void Load(XmlDocument xmlDoc, XmlNamespaceManager nsmgr, object contentController)
        {
            if (xmlDoc == null)
            {
                throw new ArgumentNullException("xmlDoc");
            }

            if (nsmgr == null)
            {
                throw new ArgumentNullException("nsmgr");
            }

            Language = Localor.StringToLanguage(xmlDoc.DocumentElement.GetAttribute("lang"));

            XmlNodeList nodeList = xmlDoc.DocumentElement.SelectNodes("tts:unitFeatures/tts:feature", nsmgr);
            Metadata.Clear();
            foreach (XmlNode xmlNode in nodeList)
            {
                XmlElement xmlEle = xmlNode as XmlElement;

                string name = xmlEle.GetAttribute("name");
                string bitWidthString = xmlEle.GetAttribute("bitWidth");

                try
                {
                    TtsFeature feature = (TtsFeature)Enum.Parse(typeof(TtsFeature), name);
                    Metadata.Add(feature, int.Parse(bitWidthString, CultureInfo.InvariantCulture));
                }
                catch (ArgumentException exp)
                {
                    string message = Helper.NeutralFormat("Failed to parse Tts Feature type [{0}] for [{1}]",
                        name, Helper.BuildExceptionMessage(exp));
                    throw new InvalidDataException(message);
                }
            }
        }

        #endregion
    }
}