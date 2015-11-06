//----------------------------------------------------------------------------
// <copyright file="HtsFontSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements HTS model compiling
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Font.Hts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider.Compress;
    using LineSpectralPair = Microsoft.Tts.ServiceProvider.LineSpectralPair;

    /// <summary>
    /// Interface for decision tree node data.
    /// </summary>
    public interface INodeData
    {
        /// <summary>
        /// Gets name of the node data.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Write wrapped data out.
        /// </summary>
        /// <param name="writer">Target writer.</param>
        /// <returns>Bytes written.</returns>
        int Write(DataWriter writer);
    }

    /// <summary>
    /// Location for data in stream.
    /// </summary>
    public struct Location
    {
        public uint Offset;   // offset
        public uint Length;   // length

        /// <summary>
        /// Overwrites default Equals method.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>True if equal; false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Location))
            {
                return false;
            }

            Location other = (Location)obj;
            return Offset == other.Offset && Length == other.Length;
        }

        /// <summary>
        /// Overwrites default GetHashCode method.
        /// </summary>
        /// <returns>Hash code generated.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// Wrapps HTS node data (Gaussians, etc.).
    /// </summary>
    public class HtsNodeData : INodeData
    {
        private string _name;
        private Gaussian[] _gaussian;
        private DynamicOrder _streamOrder;
        private GaussianSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the HtsNodeData class.
        /// </summary>
        /// <param name="name">Name of the wrapped HTS node data.</param>
        /// <param name="gaussian">Gaussian array.</param>
        /// <param name="streamOrder">Stream order.</param>
        /// <param name="serializer">The serializer object.</param>
        public HtsNodeData(string name, Gaussian[] gaussian,
            DynamicOrder streamOrder, GaussianSerializer serializer)
        {
            Helper.ThrowIfNull(name);
            Helper.ThrowIfNull(gaussian);
            Helper.ThrowIfNull(serializer);

            _name = name;
            _gaussian = gaussian;
            _streamOrder = streamOrder;
            _serializer = serializer;
        }

        #region INodeData Members

        /// <summary>
        /// Gets name of the wrapped HTS node data.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Write wrapped HTS node data.
        /// </summary>
        /// <param name="writer">Writer object.</param>
        /// <returns>Bytes written.</returns>
        public int Write(DataWriter writer)
        {
            return (int)_serializer.Write(writer, _gaussian, _streamOrder);
        }

        #endregion
    }

    /// <summary>
    /// Wrapps HTS node data (transform, etc.).
    /// </summary>
    public class HtsNodeXformData : INodeData
    {
        private string _name;
        private LinXForm _meanXform;
        private LinXForm _varXform;
        private DynamicOrder _streamOrder;
        private LinXformSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the HtsNodeXformData class.
        /// </summary>
        /// <param name="name">Name of the wrapped HTS node data.</param>
        /// <param name="meanXform">Linear transform for Gaussian mean.</param>
        /// <param name="varXform">Linear transform for Gaussian variance.</param>
        /// <param name="streamOrder">Stream order.</param>
        /// <param name="serializer">The serializer object.</param>
        public HtsNodeXformData(string name, LinXForm meanXform, LinXForm varXform,
            DynamicOrder streamOrder, LinXformSerializer serializer)
        {
            Helper.ThrowIfNull(name);
            Helper.ThrowIfNull(meanXform);
            Helper.ThrowIfNull(varXform);
            Helper.ThrowIfNull(serializer);

            _name = name;
            _meanXform = meanXform;
            _varXform = varXform;
            _streamOrder = streamOrder;
            _serializer = serializer;
        }

        #region INodeData Members

        /// <summary>
        /// Gets name of the wrapped HTS node data.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Write wrapped HTS node data.
        /// </summary>
        /// <param name="writer">Writer object.</param>
        /// <returns>Bytes written.</returns>
        public int Write(DataWriter writer)
        {
            return (int)_serializer.Write(writer, _meanXform, _varXform, _streamOrder);
        }

        #endregion
    }

    /// <summary>
    /// HTS font serialization.
    /// </summary>
    public class HtsFontSerializer
    {
        #region Fields

        private Dictionary<HtsModelHeader, GaussianSerializer> _gaussianSerializers = new Dictionary<HtsModelHeader, GaussianSerializer>();
        private Dictionary<HtsModelHeader, LinXformSerializer> _linXformSerializers = new Dictionary<HtsModelHeader, LinXformSerializer>();

        private bool _fNeedQuantize = true;
        private bool _enableLspCorrection = false;

        private int _staticDimensionOfLsp = 0;

        private int _lspCorrectionInterval = 0;

        #endregion

        /// <summary>
        /// Gets Gaussian serializers.
        /// </summary>
        public Dictionary<HtsModelHeader, GaussianSerializer> GaussianSerializers
        {
            get { return _gaussianSerializers; }
        }

        /// <summary>
        /// Gets LinXForm serializers.
        /// </summary>
        public Dictionary<HtsModelHeader, LinXformSerializer> LinXformSerializers
        {
            get { return _linXformSerializers; }
        }

        /// <summary>
        /// Gets or sets Encoder.
        /// </summary>
        public LwHuffmEncoder Encoder
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether compressing.
        /// </summary>
        public bool EnableCompress
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether needs quantization.
        /// </summary>
        public bool IsNeedQuantize
        {
            get { return _fNeedQuantize; }
            set { _fNeedQuantize = value; }
        }

        #region Public operations

        /// <summary>
        /// Write out string pool with encrypt.
        /// </summary>
        /// <param name="stringPool">String pool.</param>
        /// <param name="writer">Binary data writer.</param>
        /// <returns>Size of bytes written out.</returns>
        public static uint Write(StringPool stringPool, DataWriter writer)
        {
            Helper.ThrowIfNull(stringPool);
            Helper.ThrowIfNull(writer);
            byte[] encryptedStringPool = new byte[stringPool.Length];
            Microsoft.Tts.ServiceProvider.HTSVoiceDataEncrypt.EncryptStringPool(
                stringPool.ToArray(), encryptedStringPool);
            return writer.Write(encryptedStringPool);
        }

        /// <summary>
        /// Write node data, saving offset for names.
        /// </summary>
        /// <param name="dataNodes">Data nodes to write.</param>
        /// <param name="writer">Writer object.</param>
        /// <param name="namedPositions">Dictionary with name as key and data offset as value.</param>
        /// <returns>Bytes written.</returns>
        public static int Write(IEnumerable<INodeData> dataNodes, DataWriter writer,
            IDictionary<string, int> namedPositions)
        {
            int size = (int)writer.Write((uint)dataNodes.Count());

            foreach (var item in dataNodes)
            {
                namedPositions[item.Name] = (int)size;
                size += item.Write(writer);
            }

            return size;
        }

        /// <summary>
        /// Enables lsp correction.
        /// </summary>
        /// <param name="enableLspCorrection">True to enable lsp correction, and false to disable lsp correction.</param>
        /// <param name="staticDimension">The static dimension of lsp without gain.</param>
        /// <param name="correctionInterval">The correction interval.</param>
        public void EnableLspCorrection(bool enableLspCorrection, int staticDimension, int correctionInterval)
        {
            if (enableLspCorrection && staticDimension <= 0)
            {
                throw new ArgumentException("staticDimension");
            }

            _enableLspCorrection = enableLspCorrection;
            _staticDimensionOfLsp = staticDimension;
            _lspCorrectionInterval = correctionInterval;
        }

        /// <summary>
        /// Writes font out into binary stream.
        /// </summary>
        /// <param name="font">HTS font to write.</param>
        /// <param name="writer">Binary data.</param>
        /// <param name="xformBandWidths">Transform band width for each model.</param>
        /// <returns>Size of bytes written out.</returns>
        public uint Write(HtsFont font, DataWriter writer, Dictionary<HmmModelType, int> xformBandWidths = null)
        {
            Helper.ThrowIfNull(font);
            Helper.ThrowIfNull(writer);
            if (EnableCompress)
            {
                Helper.ThrowIfNull(Encoder);
                Encoder.StartEncoding();
            }

            font.StringPool.Reset();
            GaussianSerializers.Clear();
            LinXformSerializers.Clear();
            if (font.Header.HtsTag == HtsFontHeader.ApmDataTag)
            {
                UpdateGaussianBits(font);
            }
            else if (font.Header.HtsTag == HtsFontHeader.AtmDataTag)
            {
                UpdateLinXformBits(font);
            }

            uint size = WriteFontHeader(font, writer);

            Console.WriteLine("Saving the global question set...\n");
            Dictionary<string, uint> questionIndexes = new Dictionary<string, uint>();
            if (font.UnionQuestions != null && font.UnionQuestions.Count() != 0)
            {
                font.Questions.Items = font.UnionQuestions;
            }

            font.Header.QuestionOffset = size;
            font.Header.QuestionSize = Write(font.Questions, writer, font.StringPool, questionIndexes, font.Questions.CustomFeatures);
            size += font.Header.QuestionSize;

            font.Header.ModelSetOffset = size;
            font.Header.ModelSetSize = WriteModels(font, writer, questionIndexes, xformBandWidths);
            size += font.Header.ModelSetSize;

            font.Header.StringPoolOffset = size;
            font.Header.StringPoolSize = Write(font.StringPool, writer);
            size += (uint)font.Header.StringPoolSize;

            if (EnableCompress)
            {
                font.Header.CodebookOffset = size;
                font.Header.CodebookSize = WriteEncoderCookies(writer);
                size += font.Header.CodebookSize;
            }
            else
            {
                font.Header.CodebookOffset = 0;
                font.Header.CodebookSize = 0;
            }

            font.Header.FontSize = size - font.Header.FontSizeOffset;
            using (PositionRecover recover = new PositionRecover(writer, 0))
            {
                font.Header.Write(writer);
            }

            if (EnableCompress)
            {
                Helper.ThrowIfNull(Encoder);
                Encoder.EndEncoding();
            }

#if SERIALIZATION_CHECKING
            if (!EnableCompress)
            {
                ConsistencyChecker.Check(font,
                    Read(new HtsFont(font.PhoneSet, font.PosSet), writer.BaseStream.Excerpt(size)));
            }
#endif

            return size;
        }

        /// <summary>
        /// Save question set (with feature set).
        /// </summary>
        /// <param name="questionSet">Question set to write out.</param>
        /// <param name="writer">Binary data writer.</param>
        /// <param name="stringPool">String pool.</param>
        /// <param name="questionIndexes">Index map for question set.</param>
        /// <param name="customFeatures">List of customized features.</param>
        /// <returns>Size of bytes written out.</returns>
        public uint Write(HtsQuestionSet questionSet,
            DataWriter writer, StringPool stringPool,
            Dictionary<string, uint> questionIndexes, HashSet<string> customFeatures)
        {
            Helper.ThrowIfNull(questionSet);
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(stringPool);
            Helper.ThrowIfNull(questionIndexes);
            Helper.ThrowIfNull(customFeatures);
            uint size = 0;

            size += WriteGlobalQuestionSetHeader(questionSet.Header, writer);

            IEnumerable<string> featureList = questionSet.Items.Select(q => q.FeatureName).Distinct();
            Dictionary<string, uint> featureIndexes = new Dictionary<string, uint>();

            // Save feature sets
            size += writer.Write((uint)featureList.Count());

            uint index = 0;
            foreach (string featureName in featureList)
            {
                size += writer.Write((uint)stringPool.Length);
                if (customFeatures.Contains(featureName))
                {
                    stringPool.PutString(Microsoft.Tts.ServiceProvider.HTSVoiceData.CustomFeaturePrefix + featureName);
                }
                else
                {
                    stringPool.PutString(featureName);
                }

                featureIndexes.Add(featureName, index++);
            }

            // Save question set
            IEnumerable<Question> questions = questionSet.Items.Distinct();
            size += writer.Write((uint)questions.Count());

            index = 0;
            foreach (Question question in questions)
            {
                questionIndexes.Add(question.Name, index++);
                if (questionSet.Header.HasQuestionName)
                {
                    size += writer.Write((uint)stringPool.Length);
                    stringPool.PutString(question.Name);
                }

                size += writer.Write((uint)featureIndexes[question.FeatureName]);
                size += writer.Write((uint)question.Oper);

                size += writer.Write((uint)question.CodeValueSet.Count);
                for (int i = 0; i < question.CodeValueSet.Count; i++)
                {
                    size += writer.Write((uint)question.CodeValueSet[i]);
                }
            }

            Debug.Assert(size % sizeof(uint) == 0, "Data must be 4-byte aligned.");

            return size;
        }

        /// <summary>
        /// Read HTS font.
        /// </summary>
        /// <param name="font">Font to read.</param>
        /// <param name="reader">Binary reader.</param>
        /// <returns>Retrieved HTS font.</returns>
        public HtsFont Read(HtsFont font, BinaryReader reader)
        {
            Helper.ThrowIfNull(font);
            Helper.ThrowIfNull(reader);
            ReadFontHeader(font, reader);
            if (Encoder != null)
            {
                Encoder.OriginalDataSize = font.Header.ModelSetSize; // for compress progress
            }

            using (PositionRecover recover = new PositionRecover(reader.BaseStream, font.Header.StringPoolOffset))
            {
                byte[] buffer = new byte[font.Header.StringPoolSize];
                Microsoft.Tts.ServiceProvider.HTSVoiceDataEncrypt.DecryptStringPool(
                    reader.ReadBytes((int)font.Header.StringPoolSize), buffer);
                font.StringPool.PutBuffer(buffer);
            }

            Read(font.Questions, reader, font.StringPool, font.Questions.CustomFeatures);
            foreach (Question question in font.Questions.Items)
            {
                question.CodeValueSetToValueSet(font.PosSet, font.PhoneSet, font.Questions.CustomFeatures);
            }

            List<Location> modelOffsets = new List<Location>();
            Read(modelOffsets, reader);

            for (int i = 0; i < modelOffsets.Count; i++)
            {
                HtsModel model = new HtsModel(font, font.PhoneSet, font.PosSet);
                Read(model, reader, font.Questions);
                font.Models.Add(model.MmfFile.ModelType, model);
            }

            // Move ahead the size of string pool
            reader.BaseStream.Seek(font.Header.StringPoolSize, SeekOrigin.Current);

            return font;
        }

        /// <summary>
        /// Read question set from binary stream.
        /// </summary>
        /// <param name="questionSet">Question set to load.</param>
        /// <param name="reader">Binary reader.</param>
        /// <param name="stringPool">String pool.</param>
        /// <param name="customFeatures">The list of customized features.</param>
        /// <returns>Question set.</returns>
        public HtsQuestionSet Read(HtsQuestionSet questionSet, BinaryReader reader,
            StringPool stringPool, HashSet<string> customFeatures)
        {
            Helper.ThrowIfNull(questionSet);
            Helper.ThrowIfNull(reader);
            Helper.ThrowIfNull(stringPool);
            Helper.ThrowIfNull(customFeatures);
            ReadGlobalQuestionSetHeader(questionSet.Header, reader);

            uint featureCount = reader.ReadUInt32();

            customFeatures.Clear();
            Dictionary<uint, uint> featurePositions = new Dictionary<uint, uint>();
            Dictionary<uint, string> features = new Dictionary<uint, string>();
            for (uint i = 0; i < featureCount; i++)
            {
                featurePositions.Add(i, reader.ReadUInt32());
                string featureName = stringPool.GetString((int)featurePositions[i]);
                if (featureName.StartsWith(Microsoft.Tts.ServiceProvider.HTSVoiceData.CustomFeaturePrefix))
                {
                    featureName = featureName.Substring(Microsoft.Tts.ServiceProvider.HTSVoiceData.CustomFeaturePrefix.Length,
                        featureName.Length - Microsoft.Tts.ServiceProvider.HTSVoiceData.CustomFeaturePrefix.Length);
                    customFeatures.Add(featureName);
                }

                features.Add(i, featureName);
            }

            List<Question> questions = new List<Question>();
            uint questionCount = reader.ReadUInt32();
            for (uint i = 0; i < questionCount; i++)
            {
                Question question = new Question();
                if (questionSet.Header.HasQuestionName)
                {
                    question.Name = stringPool.GetString((int)reader.ReadUInt32());
                }
                else
                {
                    // we need generate an union question name.
                    // it is used to load an APM file without question names.
                    question.Name = "Q" + i + "-V" + i;
                }

                uint featureIndex = reader.ReadUInt32();

                if (!featurePositions.ContainsKey(featureIndex))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Invalid feature index is found at {0}, " +
                        "which blocks to load feature name for question.", featureIndex));
                }

                question.FeatureName = features[featureIndex];
                question.Oper = (QuestionOperator)reader.ReadUInt32();
                uint codeValueCount = reader.ReadUInt32();

                List<int> codeValueSet = new List<int>();
                for (uint j = 0; j < codeValueCount; j++)
                {
                    codeValueSet.Add(reader.ReadInt32());
                }

                question.CodeValueSet = codeValueSet.AsReadOnly();

                questions.Add(question);
            }

            questionSet.Items = questions;

            return questionSet;
        }

        #endregion

        #region Protected operations

        /// <summary>
        /// Writes font header into binary stream.
        /// </summary>
        /// <param name="font">Font to write.</param>
        /// <param name="writer">Data binary writer.</param>
        /// <returns>Size of bytes written out.</returns>
        protected virtual uint WriteFontHeader(HtsFont font, DataWriter writer)
        {
            Helper.ThrowIfNull(font);
            Helper.ThrowIfNull(writer);
            return font.Header.Write(writer);
        }

        /// <summary>
        /// Reads HTS font header from binary data stream.
        /// </summary>
        /// <param name="font">Font to read header for.</param>
        /// <param name="reader">Binary data reader.</param>
        /// <returns>Retrieved font header.</returns>
        protected virtual HtsFontHeader ReadFontHeader(HtsFont font, BinaryReader reader)
        {
            Helper.ThrowIfNull(font);
            Helper.ThrowIfNull(reader);
            font.Header = HtsFontHeader.Read(reader);
            return font.Header;
        }

        /// <summary>
        /// Writes header of global question set.
        /// </summary>
        /// <param name="header">Question set header to write out.</param>
        /// <param name="writer">Binary writer.</param>
        /// <returns>Size of bytes written out.</returns>
        protected virtual uint WriteGlobalQuestionSetHeader(HtsQuestionSetHeader header, DataWriter writer)
        {
            Helper.ThrowIfNull(header);
            Helper.ThrowIfNull(writer);
            uint size = writer.Write((uint)(header.HasQuestionName ? 1 : 0));
            return size;
        }

        /// <summary>
        /// Reads header of global question set.
        /// </summary>
        /// <param name="header">Question set header to read.</param>
        /// <param name="reader">Binary reader.</param>
        /// <returns>Retrieved question set header.</returns>
        protected virtual HtsQuestionSetHeader ReadGlobalQuestionSetHeader(HtsQuestionSetHeader header, BinaryReader reader)
        {
            Helper.ThrowIfNull(header);
            Helper.ThrowIfNull(reader);
            header.HasQuestionName = reader.ReadUInt32() != 0;
            return header;
        }

        /// <summary>
        /// Writes HTS model header out into data binary stream.
        /// </summary>
        /// <param name="header">HTS model header.</param>
        /// <param name="writer">Binary writer.</param>
        /// <returns>Size of bytes written out.</returns>
        protected virtual uint WriteModelHeader(HtsModelHeader header, DataWriter writer)
        {
            Helper.ThrowIfNull(header);
            Helper.ThrowIfNull(writer);
            uint headerSize = header.Save(writer);
            ConsistencyChecker.Check(header, new HtsModelHeader(header.IsGaussian).Load(writer.BaseStream.Excerpt((uint)headerSize)));

            return headerSize;
        }

        /// <summary>
        /// Reads HTS model header from binary stream.
        /// </summary>
        /// <param name="header">HTS model header.</param>
        /// <param name="reader">Binary reader.</param>
        /// <returns>Retrieved HTS model header.</returns>
        protected virtual HtsModelHeader ReadModelHeader(HtsModelHeader header, BinaryReader reader)
        {
            Helper.ThrowIfNull(header);
            Helper.ThrowIfNull(reader);
            header.Load(reader);
            return header;
        }

        /// <summary>
        /// Gets the Gaussian serializer.
        /// </summary>
        /// <param name="header">Model header.</param>
        /// <returns>Gaussian serializer.</returns>
        protected virtual GaussianSerializer GetGaussianSerializer(HtsModelHeader header)
        {
            Helper.ThrowIfNull(header);
            GaussianSerializer gaussianSerializer = GaussianSerializer.Create(header.GaussianConfig,
                header.ModelType, header.Distribution);
            gaussianSerializer.IsNeedQuantize = IsNeedQuantize;
            return gaussianSerializer;
        }

        /// <summary>
        /// Gets the LinXForm serializer.
        /// </summary>
        /// <param name="header">Model header.</param>
        /// <returns>LinXForm serializer.</returns>
        protected virtual LinXformSerializer GetLinXformSerializer(HtsModelHeader header)
        {
            Helper.ThrowIfNull(header);
            LinXformSerializer linXformSerializer = LinXformSerializer.Create(header.LinXformConfig);
            linXformSerializer.IsNeedQuantize = IsNeedQuantize;
            return linXformSerializer;
        }

        #endregion

        #region Diagnostic assistant

        /// <summary>
        /// Get dynamic order of given stream indexes, comparing with stream indexes
        /// While there is only one stream, the dynamic order is undefined,
        /// Since dynamic feature are embedded into Gaussian distributions.
        /// </summary>
        /// <param name="streamIndexes">Stream indexes.</param>
        /// <param name="streamIndex">Stream index to test.</param>
        /// <returns>Dynamic order of the given stream.</returns>
        private static DynamicOrder GetStreamDynamicOrder(int[] streamIndexes, int streamIndex)
        {
            Helper.ThrowIfNull(streamIndexes);
            DynamicOrder streamOrder = DynamicOrder.Undefined;
            if (streamIndexes.Length > 1)
            {
                streamOrder = (DynamicOrder)(new List<int>(streamIndexes).IndexOf(streamIndex) + 1);
            }

            return streamOrder;
        }

        #endregion

        #region Private operations

        /// <summary>
        /// Get stream offset given model name and stream index
        ///     HTK will append "-" + number to model name if a couple streams are combined and shared same CART tree.
        /// </summary>
        /// <param name="modelName">Model name.</param>
        /// <param name="streamIndex">Stream index of the model to find.</param>
        /// <param name="namedStreams">Stream dictionary by model name.</param>
        /// <returns>Stream index found.</returns>
        private static int GetStream(string modelName,
            int streamIndex, IDictionary<string, int> namedStreams)
        {
            Helper.ThrowIfNull(modelName);
            Helper.ThrowIfNull(namedStreams);

            string streamTag = Helper.NeutralFormat("{0}-{1}", modelName, streamIndex);

            int stream = 0;
            if (!namedStreams.ContainsKey(modelName) && !namedStreams.ContainsKey(streamTag))
            {
                string strTmp = "The model tag [{0}] or [{1}] defined in decision tree can not be found in physical model set.";
                string message = string.Format(CultureInfo.InvariantCulture, strTmp, modelName, streamTag);
                throw new InvalidDataException(message);
            }
            else
            {
                if (namedStreams.ContainsKey(modelName))
                {
                    stream = namedStreams[modelName];
                    Debug.Assert(!namedStreams.ContainsKey(streamTag), "Can not contain both together");
                }
                else if (namedStreams.ContainsKey(streamTag))
                {
                    stream = namedStreams[streamTag];
                }
            }

            return stream;
        }

        /// <summary>
        /// Tell whether current HMM stream is wanted in the given list.
        /// </summary>
        /// <param name="stream">HMM stream.</param>
        /// <param name="streamIndexes">Wanted stream indexes.</param>
        /// <param name="names">Keyed HMM names.</param>
        /// <returns>True, if wanted, otherwise false.</returns>
        private static bool IsWantedStream(HmmStream stream, int[] streamIndexes, HashSet<string> names)
        {
            Helper.ThrowIfNull(stream);
            Helper.ThrowIfNull(streamIndexes);
            Helper.ThrowIfNull(names);
            if (!string.IsNullOrEmpty(stream.Name) &&
                names.Contains(HmmStreamName.GetStreamIndexFreeName(stream.Name)))
            {
                int index = HmmStreamName.ParseStreamIndex(stream.Name);
                if (index == HmmStreamName.NoStreamIndex || streamIndexes.Count(i => i == index) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Read MMF file from binary.
        /// </summary>
        /// <param name="mmfFile">MMF file to read.</param>
        /// <param name="reader">Binary reader.</param>
        /// <param name="serializer">Gaussian serializer.</param>
        /// <returns>MMF file instance.</returns>
        private static HtsMmfFile Read(HtsMmfFile mmfFile, BinaryReader reader, GaussianSerializer serializer)
        {
            Helper.ThrowIfNull(mmfFile);
            Helper.ThrowIfNull(reader);
            Helper.ThrowIfNull(serializer);
            uint baseOffset = (uint)reader.BaseStream.Position;

            uint hmmStreamCount = reader.ReadUInt32();

            List<HmmStream> hmmStreams = new List<HmmStream>();
            for (int i = 0; i < hmmStreamCount; i++)
            {
                HmmStream stream = new HmmStream();
                stream.Position = (int)(reader.BaseStream.Position - baseOffset);
                mmfFile.PositionedStreams.Add(stream.Position, stream);
                stream.Gaussians = serializer.ReadGaussians(reader,
                        (int)mmfFile.StreamWidths[0], DynamicOrder.Undefined);
                Debug.Assert(mmfFile.StreamWidths[0] == mmfFile.StreamWidths.Sum(w => (uint)w) / mmfFile.StreamWidths.Length,
                    "One stream one time, each stream should have equal stream widths. Otherwise, it needs more flexible configurations.");
                hmmStreams.Add(stream);
            }

            mmfFile.Streams = hmmStreams;

            return mmfFile;
        }

        /// <summary>
        /// Write out locations.
        /// </summary>
        /// <param name="locations">Locations to write.</param>
        /// <param name="writer">Binary data writer.</param>
        /// <returns>Size of bytes written.</returns>
        private static uint Write(Location[] locations, DataWriter writer)
        {
            Helper.ThrowIfNull(locations);
            Helper.ThrowIfNull(writer);
            uint size = writer.Write(locations.Length);
            for (int i = 0; i < locations.Length; i++)
            {
                size += writer.Write(locations[i].Offset);
                size += writer.Write(locations[i].Length);
            }

            return size;
        }

        /// <summary>
        /// Read location list.
        /// </summary>
        /// <param name="locations">Location list to read.</param>
        /// <param name="reader">Binary reader.</param>
        /// <returns>Location list.</returns>
        private static List<Location> Read(List<Location> locations, BinaryReader reader)
        {
            Helper.ThrowIfNull(locations);
            Helper.ThrowIfNull(reader);
            uint count = reader.ReadUInt32();
            locations.Clear();
            for (uint i = 0; i < count; i++)
            {
                Location loc = new Location();
                loc.Offset = reader.ReadUInt32();
                loc.Length = reader.ReadUInt32();
                locations.Add(loc);
            }

            return locations;
        }

        /// <summary>
        /// Correct Lsp (only for the static dimension).
        /// </summary>
        /// <param name="guassians">The input guassians.</param>
        private void CorrectLsp(Gaussian[] guassians)
        {
            foreach (Gaussian gaussian in guassians)
            {
                if (!LineSpectralPair.IsInAscending(gaussian.Mean, 0, _staticDimensionOfLsp))
                {
                    LineSpectralPair.CorrectOrdering(gaussian.Mean, 0, _staticDimensionOfLsp);
                }

                if (!LineSpectralPair.IsInNormalizedRange(gaussian.Mean, 0, _staticDimensionOfLsp))
                {
                    LineSpectralPair.CorrectNormalizedRange(gaussian.Mean, 0, _staticDimensionOfLsp);
                }

                for (int i = _lspCorrectionInterval; i > 0; --i)
                {
                    LineSpectralPair.EnsureLsfStability(gaussian.Mean, _staticDimensionOfLsp, i);
                }

                // Re-check the stability and output warning if there is unstable LSP
                if (!LineSpectralPair.IsInAscending(gaussian.Mean, 0, _staticDimensionOfLsp)
                    || !LineSpectralPair.IsInNormalizedRange(gaussian.Mean, 0, _staticDimensionOfLsp))
                {
                    Console.WriteLine("Warning: the mean of one LSP Gaussian is unstable!");
                }
            }
        }

        /// <summary>
        /// Write Encoder Cookies.
        /// </summary>
        /// <param name="writer">Writer.</param>
        /// <returns>Codebook.</returns>
        private uint WriteEncoderCookies(DataWriter writer)
        {
            return writer.Write(Encoder.GetCodebook());
        }

        /// <summary>
        /// Write stream information.
        /// </summary>
        /// <param name="mmfFile">Instance to write out.</param>
        /// <param name="writer">Binary data writer.</param>
        /// <param name="serializer">Gaussian serializer.</param>
        /// <param name="streamNames">Stream names.</param>
        /// <param name="namedPositions">Stream name to position map.</param>
        /// <returns>Physical model offset map.</returns>
        private uint WriteStreamInfo(HtsMmfFile mmfFile, DataWriter writer,
            GaussianSerializer serializer, IEnumerable<string> streamNames,
            out IDictionary<string, int> namedPositions)
        {
            Helper.ThrowIfNull(mmfFile);
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(serializer);
            Helper.ThrowIfNull(streamNames);

            namedPositions = new Dictionary<string, int>();

            HashSet<string> streamNameSet = new HashSet<string>(streamNames);

            IEnumerable<INodeData> dataNodes = GetHtsNodeData(mmfFile, serializer, streamNameSet);

            int size = Write(dataNodes, writer, namedPositions);

            mmfFile.PositionedStreams.Clear();
            mmfFile.NamedStreams.Clear();
            var namedStreams = mmfFile.Streams.ToDictionary(s => s.Name);
            foreach (var name in namedPositions.Keys)
            {
                namedStreams[name].Position = namedPositions[name];
                mmfFile.PositionedStreams.Add(namedPositions[name], namedStreams[name]);
                mmfFile.NamedStreams.Add(name, namedStreams[name]);
            }

            return (uint)size;
        }

        /// <summary>
        /// Write stream information.
        /// </summary>
        /// <param name="mmfFile">HTS MMF file to write out.</param>
        /// <param name="xformFile">Transformation file to write out.</param>
        /// <param name="mappingFile">Mapping file to write out.</param>
        /// <param name="writer">Binary data writer.</param>
        /// <param name="serializer">Linear transform serializer.</param>
        /// <param name="streamNames">Stream names.</param>
        /// <param name="namedPositions">Stream name to position map.</param>
        /// <returns>Physical model offset map.</returns>
        private uint WriteStreamInfo(HtsMmfFile mmfFile, HtsTransformFile xformFile, AdaptationMappingFile mappingFile,
            DataWriter writer, LinXformSerializer serializer, IEnumerable<string> streamNames,
            out IDictionary<string, int> namedPositions)
        {
            Helper.ThrowIfNull(mmfFile);
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(serializer);
            Helper.ThrowIfNull(streamNames);

            namedPositions = new Dictionary<string, int>();

            HashSet<string> streamNameSet = new HashSet<string>(streamNames);
            IEnumerable<INodeData> dataNodes = GetHtsNodeXformData(mmfFile, xformFile, mappingFile,
                serializer, streamNameSet);

            int size = Write(dataNodes, writer, namedPositions);

            mmfFile.PositionedStreams.Clear();
            mmfFile.NamedStreams.Clear();
            var namedStreams = mmfFile.Streams.ToDictionary(s => s.Name);
            foreach (var name in namedPositions.Keys)
            {
                namedStreams[name].Position = namedPositions[name];
                mmfFile.PositionedStreams.Add(namedPositions[name], namedStreams[name]);
                mmfFile.NamedStreams.Add(name, namedStreams[name]);
            }

            return (uint)size;
        }

        /// <summary>
        /// Returns HTS streams wrapped as node node.
        /// </summary>
        /// <param name="mmfFile">HTS MMF file.</param>
        /// <param name="serializer">The serializer object.</param>
        /// <param name="streamNameSet">Model stream name set.</param>
        /// <returns>HTS streams wrapped as node node.</returns>
        private IEnumerable<INodeData> GetHtsNodeData(
            HtsMmfFile mmfFile, GaussianSerializer serializer, HashSet<string> streamNameSet)
        {
            mmfFile.Streams = mmfFile.Streams.Where(
                hmmStream =>
                    IsWantedStream(hmmStream, mmfFile.StreamIndexes, streamNameSet) &&
                    hmmStream.ModelType == mmfFile.ModelType).ToArray();

            Debug.Assert(mmfFile.Streams.Count() > 0, "No valid streams available");

            foreach (HmmStream hmmStream in mmfFile.Streams)
            {
                int streamIndex = HmmStreamName.ParseStreamIndex(hmmStream.Name);
                DynamicOrder streamOrder = GetStreamDynamicOrder(mmfFile.StreamIndexes, streamIndex);

                Gaussian[] gaussians = hmmStream.Gaussians;
                if (hmmStream.ModelType == HmmModelType.Lsp && _enableLspCorrection)
                {
                    CorrectLsp(gaussians);
                }

                yield return new HtsNodeData(hmmStream.Name, gaussians, streamOrder, serializer);
            }
        }

        /// <summary>
        /// Returns HTS streams wrapped as node node.
        /// </summary>
        /// <param name="mmfFile">HTS MMF file.</param>
        /// <param name="xformFile">Transform file to write out.</param>
        /// <param name="mappingFile">Mapping file to write out.</param>
        /// <param name="serializer">The serializer object.</param>
        /// <param name="streamNameSet">Model stream name set.</param>
        /// <returns>HTS streams wrapped as node node.</returns>
        private IEnumerable<INodeData> GetHtsNodeXformData(
            HtsMmfFile mmfFile, HtsTransformFile xformFile, AdaptationMappingFile mappingFile,
            LinXformSerializer serializer, HashSet<string> streamNameSet)
        {
            mmfFile.Streams = mmfFile.Streams.Where(
                hmmStream =>
                    IsWantedStream(hmmStream, mmfFile.StreamIndexes, streamNameSet) &&
                    hmmStream.ModelType == mmfFile.ModelType).ToArray();

            Debug.Assert(mmfFile.Streams.Count() > 0, "No valid streams available");

            foreach (HmmStream hmmStream in mmfFile.Streams)
            {
                int streamIndex = HmmStreamName.ParseStreamIndex(hmmStream.Name);
                DynamicOrder streamOrder = GetStreamDynamicOrder(mmfFile.StreamIndexes, streamIndex);

                int xfromIndex = -1;
                foreach (AdaptationMapping mapping in mappingFile.AdaptationMappings.Values)
                {
                    xfromIndex = mapping.GetTransformIndexFromModelName(hmmStream.Name);
                    if (xfromIndex > 0)
                    {
                        break;
                    }
                }

                LinXForm meanXform = xformFile.MeanForms[xfromIndex];
                LinXForm varXform = xformFile.VarForms[xfromIndex];

                yield return new HtsNodeXformData(hmmStream.Name, meanXform, varXform, streamOrder, serializer);
            }
        }

        /// <summary>
        /// Updates number of bits for Gaussian configurations in model headers.
        /// </summary>
        /// <param name="font">HTS font to update.</param>
        private void UpdateGaussianBits(HtsFont font)
        {
            Helper.ThrowIfNull(font);
            foreach (HtsModel model in font.Models.Values)
            {
                GaussianSerializer serializer = GetGaussianSerializer(model.Header);
                model.Header.GaussianConfig.MeanBits = serializer.MeanBits;
                model.Header.GaussianConfig.VarianceBits = serializer.VarianceBits;
            }
        }

        /// <summary>
        /// Updates number of bits for Linxform configurations in model headers.
        /// </summary>
        /// <param name="font">HTS font to update.</param>
        private void UpdateLinXformBits(HtsFont font)
        {
            Helper.ThrowIfNull(font);
            foreach (HtsModel model in font.Models.Values)
            {
                LinXformSerializer serializer = GetLinXformSerializer(model.Header);
                model.Header.LinXformConfig.BiasBits = serializer.BiasBits;
                model.Header.LinXformConfig.MatrixBits = serializer.MatrixBits;
            }
        }

        /// <summary>
        /// Writes font models out into binary stream.
        /// </summary>
        /// <param name="font">HTS font to write.</param>
        /// <param name="writer">Binary data writer.</param>
        /// <param name="questionIndexes">Question indexes.</param>
        /// <param name="xformBandWidths">Transform band width for each model.</param>
        /// <returns>Size of bytes written out.</returns>
        private uint WriteModels(HtsFont font, DataWriter writer,
            Dictionary<string, uint> questionIndexes, Dictionary<HmmModelType, int> xformBandWidths)
        {
            Helper.ThrowIfNull(font);
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(questionIndexes);
            uint modelCount = (uint)font.Models.Count;
            long modelSetLocationsOffset = writer.BaseStream.Position;
            Location[] modelSetLocations = new Location[modelCount];
            uint size = Write(modelSetLocations, writer);

            int modelIndex = 0;
            uint xformBandWidth = 0;
            foreach (HmmModelType modelType in font.Models.Keys)
            {
                HtsModel model = font.Models[modelType];
                Console.WriteLine("Saving the HMM info from " + HmmNameEncoding.GetModelLabel(modelType) + " model...\n");
                modelSetLocations[modelIndex].Offset = size;
                if (xformBandWidths != null && xformBandWidths.ContainsKey(modelType))
                {
                    xformBandWidth = (uint)xformBandWidths[modelType];
                }
                else
                {
                    xformBandWidth = 0;
                }

                modelSetLocations[modelIndex].Length = Write(model, writer, questionIndexes, font.Questions, xformBandWidth);
                size += modelSetLocations[modelIndex++].Length;
            }

            using (PositionRecover recover = new PositionRecover(writer, modelSetLocationsOffset))
            {
                // Re-write acoustic model offset & size, with updated information
                Write(modelSetLocations, writer);
            }

            return size;
        }

        /// <summary>
        /// Writes HTS model into data binary writer.
        /// </summary>
        /// <param name="model">HTS model to save.</param>
        /// <param name="writer">Binary data writer.</param>
        /// <param name="questionIndexes">Question indexes.</param>
        /// <param name="questionSet">HTS question set.</param>
        /// <param name="xformBandWidth">Transform band width.</param>
        /// <returns>Size of bytes written out.</returns>
        private uint Write(HtsModel model, DataWriter writer,
            Dictionary<string, uint> questionIndexes, HtsQuestionSet questionSet, uint xformBandWidth)
        {
            Helper.ThrowIfNull(model);
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(questionIndexes);
            Helper.ThrowIfNull(questionSet);
            uint modelSize = 0;

            model.Header.ModelType = model.Forest.ModelType();
            model.Header.WindowSet = model.WindowSet;
            if (model.MmfFile != null)
            {
                model.Header.Distribution = model.MmfFile.Distribution;
                model.Header.StreamWidths = model.MmfFile.StreamWidths;
            }

            model.Header.StreamCount = (uint)model.Forest.StreamCount;
            model.Header.StreamIndexes = model.Forest.StreamIndexes;
            model.Header.StateCount = (uint)model.Forest.StateCount;
            if (model.Header.IsGaussian)
            {
                Debug.Assert(model.Header.GaussianConfig != null, "Gaussian config should exist.");
                model.Header.GaussianConfig.MixtureCount = (uint)model.MmfFile.GaussianMixtureCount;
                if (model.Header.GaussianConfig.IsFixedPoint)
                {
                    HtsFont.CheckFixedPointConsistenceOnStateCount(model.Header.StateCount);
                }
            }
            else
            {
                Debug.Assert(model.Header.LinXformConfig != null, "Linear transform config should exist.");
                string modelName = model.MmfFile.Streams.Where(hmmStream => hmmStream.ModelType == model.MmfFile.ModelType).ToArray()[0].Name;
                int xfromIndex = -1;
                foreach (AdaptationMapping mapping in model.MappingFile.AdaptationMappings.Values)
                {
                    xfromIndex = mapping.GetTransformIndexFromModelName(modelName);
                    if (xfromIndex > 0)
                    {
                        break;
                    }
                }

                LinXForm xform = model.XformFile.MeanForms[xfromIndex];
                LinXformConfig config = model.Header.LinXformConfig;
                config.MeanBandWidth = xformBandWidth;
                config.MeanBlockNum = (uint)xform.Blocks.Count;
                config.MeanBlockSizes = new uint[xform.Blocks.Count];
                for (int i = 0; i < config.MeanBlockNum; i++)
                {
                    config.MeanBlockSizes[i] = (uint)xform.Blocks[i].GetLength(0);
                }

                xform = model.XformFile.VarForms[xfromIndex];
                config.VarBlockNum = (uint)xform.Blocks.Count;
                config.VarBlockSizes = new uint[xform.Blocks.Count];
                for (int i = 0; i < config.VarBlockNum; i++)
                {
                    config.VarBlockSizes[i] = (uint)xform.Blocks[i].GetLength(0);
                }
            }

            uint headerSize = WriteModelHeader(model.Header, writer);
            modelSize += headerSize;

            using (MemoryStream streamBuffer = new MemoryStream())
            {
                DataWriter streamWriter = new DataWriter(streamBuffer);
                GaussianSerializer gaussianSerializer = null;
                LinXformSerializer linXformSerializer = null;
                IDictionary<string, int> namedStreamOffsets;
                uint streamSize = 0;
                if (model.Header.IsGaussian)
                {
                    gaussianSerializer = GetGaussianSerializer(model.Header);
                    gaussianSerializer.Encoder = Encoder;
                    gaussianSerializer.EnableCompress = EnableCompress;
                    if (model.Header.ModelType != HmmModelType.Lsp)
                    {
                        // disable the F0, Duration and other gaussian compression
                        gaussianSerializer.Encoder = null;
                        gaussianSerializer.EnableCompress = false;
                    }

                    GaussianSerializers.Add(model.Header, gaussianSerializer);
                    streamSize = WriteStreamInfo(model.MmfFile, streamWriter, gaussianSerializer,
                        model.Forest.LeafNodes.Select(n => n.Name), out namedStreamOffsets);
                }
                else
                {
                    linXformSerializer = GetLinXformSerializer(model.Header);
                    linXformSerializer.Encoder = Encoder;
                    linXformSerializer.EnableCompress = EnableCompress;
                    if (model.Header.ModelType != HmmModelType.Lsp)
                    {
                        // disable the F0, Duration and other gaussian compression
                        linXformSerializer.Encoder = null;
                        linXformSerializer.EnableCompress = false;
                    }

                    LinXformSerializers.Add(model.Header, linXformSerializer);
                    streamSize = WriteStreamInfo(model.MmfFile, model.XformFile, model.MappingFile,
                         streamWriter, linXformSerializer, model.Forest.LeafNodes.Select(n => n.Name), out namedStreamOffsets);
                }

                Debug.Assert(streamSize == streamBuffer.Length,
                    "Calculated size of byte written out should equal with stream size,");

                model.Header.TreeOffset = modelSize;
                DecisionForestSerializer forestSerializer = new DecisionForestSerializer();

                Dictionary<string, uint[]> nodeOffsets = GetNodeOffsets(model.Forest.LeafNodes, namedStreamOffsets, model.MmfFile.StreamIndexes);
                forestSerializer.Encoder = Encoder;
                forestSerializer.EnableCompress = EnableCompress;

                model.Header.TreeSize = forestSerializer.Write(model.Forest, writer, questionIndexes, questionSet, nodeOffsets);
                modelSize += model.Header.TreeSize;

                Debug.Assert(model.MmfFile.PositionedStreams.Count ==
                    model.Forest.LeafNodes.Count() * model.Header.StreamCount,
                    "The count of HMM streams should align with the count of lead nodes and stream count.");
                Debug.Assert(model.MmfFile.NamedStreams.Count == model.MmfFile.PositionedStreams.Count,
                    "The count of HMM streams should align with the count of lead nodes and stream count.");

                model.Header.StreamOffset = modelSize;
                model.Header.StreamSize = writer.Write(streamBuffer.ToArray());
                modelSize += model.Header.StreamSize;

                model.Header.AlgorithmIdOffset = (uint)model.Font.StringPool.Length;
                model.Font.StringPool.PutString(model.MmfFile.AlgorithmId);
            }
            
            using (PositionRecover recover = new PositionRecover(writer, -modelSize, SeekOrigin.Current))
            {
                model.Header.Save(writer);
            }

#if SERIALIZATION_CHECKING
            if (!EnableCompress)
            {
                ConsistencyChecker.Check(model,
                    new HtsFontSerializer().Read(new HtsModel(model.Font, model.PhoneSet, model.PosSet),
                        writer.BaseStream.Excerpt(modelSize), questionSet));
            }
#endif

            return modelSize;
        }

        /// <summary>
        /// Get node name indexed node stream offsets.
        /// </summary>
        /// <param name="nodes">Decision tree nodes.</param>
        /// <param name="namedStreamOffsets">Name indexed stream offsets.</param>
        /// <param name="streamIndexes">Stream indexes.</param>
        /// <returns>Node name indexed node stream offsets.</returns>
        private Dictionary<string, uint[]> GetNodeOffsets(
            IEnumerable<DecisionTreeNode> nodes,
            IDictionary<string, int> namedStreamOffsets, int[] streamIndexes)
        {
            Dictionary<string, uint[]> nodeOffsets = new Dictionary<string, uint[]>();

            foreach (var nodeName in nodes.Select(n => n.Name))
            {
                nodeOffsets[nodeName] = streamIndexes.Select(
                    streamIndex => (uint)GetStream(nodeName, streamIndex, namedStreamOffsets)).ToArray();
            }

            return nodeOffsets;
        }

        /// <summary>
        /// Hts model to ready.
        /// </summary>
        /// <param name="model">HTS model instance.</param>
        /// <param name="reader">Binary reader.</param>
        /// <param name="questionSet">HTS question set.</param>
        /// <returns>HTS model read.</returns>
        private HtsModel Read(HtsModel model, BinaryReader reader, HtsQuestionSet questionSet)
        {
            Helper.ThrowIfNull(model);
            Helper.ThrowIfNull(reader);
            Helper.ThrowIfNull(questionSet);
            ReadModelHeader(model.Header, reader);

            model.WindowSet = model.Header.WindowSet;

            model.Forest = new DecisionForest(model.Header.ModelType.ToString())
            {
                StreamIndexes = model.Header.StreamIndexes,
                PhoneSet = model.PhoneSet,
                PosSet = model.PosSet,
            };

            DecisionForestSerializer.Read(model.Forest, reader, model.Header, questionSet);
            //// forestSerializer.Encoder = Encoder; // Temperarily disable compression.

            model.MmfFile = new HtsMmfFile(model.Header.ModelType)
            {
                StreamCount = model.Header.StreamCount,
                StreamWidths = model.Header.StreamWidths,
                Distribution = model.Header.Distribution,
            };

            GaussianSerializer gaussianSerializer = GetGaussianSerializer(model.Header);
            gaussianSerializer.Encoder = Encoder;
            if (model.Header.ModelType != HmmModelType.Lsp)
            {
                // disable the F0, Duration and other gaussian compression
                gaussianSerializer.Encoder = null;
                gaussianSerializer.EnableCompress = false;
                if (Encoder != null)
                {
                    Encoder.OriginalDataSize -= model.Header.StreamSize;
                }
            }

            Read(model.MmfFile, reader, gaussianSerializer);
            model.MmfFile.AlgorithmId = model.Font.StringPool.GetString((int)model.Header.AlgorithmIdOffset);
            model.MmfFile.StreamIndexes = model.Header.StreamIndexes;
            model.MmfFile.StreamWidths = model.Header.StreamWidths;

            model.BuildStreamName();

            return model;
        }

        #endregion
    }
}