//----------------------------------------------------------------------------
// <copyright file="HtsFont.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements HTS font object model.
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
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// HTS font compiling configuration.
    /// </summary>
    public class CompilationConfig
    {
        #region Fields

        private bool _compress;

        /// <summary>
        /// The value indicating whether to include question name string in the font for easier debugging.
        /// </summary>
        private bool _hasQuestionName = true;

        /// <summary>
        /// The dynamic feature layout of target font.
        /// </summary>
        private DynamicWindowLayout _windowLayout = DynamicWindowLayout.StaticDeltaAcceleration;

        /// <summary>
        /// The types of models to merge streams into one stream during compilation.
        /// </summary>
        private List<HmmModelType> _streamMergingModels = new List<HmmModelType>();

        #endregion

        #region Public static properties

        /// <summary>
        /// Gets the configuration schema.
        /// </summary>
        public static XmlSchemaInclude SchemaInclude
        {
            get
            {
                XmlSchemaInclude included = new XmlSchemaInclude();
                Assembly assembly = Assembly.GetExecutingAssembly();
                included.Schema =
                    XmlHelper.LoadSchemaFromResource(assembly, "Microsoft.Tts.Font.Schema.SpsCompilationConfig.xsd");

                return included;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the target Gaussian models of this font as fixed point.
        /// </summary>
        public bool FixedPoint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to generate a compressed voice font for smaller size.
        /// </summary>
        public bool Compress
        {
            get { return _compress; }
            set { _compress = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the value indicating whether to include question name string in the font for easier debugging.
        /// </summary>
        public bool HasQuestionName
        {
            get { return _hasQuestionName; }
            set { _hasQuestionName = value; }
        }

        /// <summary>
        /// Gets or sets dynamic feature layout of target font.
        /// </summary>
        public DynamicWindowLayout WindowLayout
        {
            get { return _windowLayout; }
            set { _windowLayout = value; }
        }

        /// <summary>
        /// Gets the types of models to merge streams into one stream during compilation.
        /// </summary>
        public IList<HmmModelType> StreamMergingModels
        {
            get { return _streamMergingModels; }
        }

        #endregion

        #region Public members

        /// <summary>
        /// Loads configuration from xml snippet.
        /// </summary>
        /// <param name="nsmgr">Namespace manager.</param>
        /// <param name="configNode">Xml node containing the config.</param>
        public void ParseCompilationConfig(XmlNamespaceManager nsmgr, XmlNode configNode)
        {
            if (nsmgr == null)
            {
                throw new ArgumentNullException("nsmgr");
            }

            if (configNode == null)
            {
                throw new ArgumentNullException("configNode");
            }

            XmlNode node = configNode.SelectSingleNode(@"fixedPoint/@enable", nsmgr);
            if (node != null)
            {
                FixedPoint = bool.Parse(node.InnerText);
            }
            else
            {
                FixedPoint = false;
            }

            node = configNode.SelectSingleNode(@"dynamicFeature/@layout", nsmgr);
            if (node != null)
            {
                WindowLayout = (DynamicWindowLayout)Enum.Parse(typeof(DynamicWindowLayout), node.InnerText);
            }
            else
            {
                WindowLayout = DynamicWindowLayout.StaticDeltaAcceleration;
            }

            node = configNode.SelectSingleNode(@"stringPool/@hasQuestion", nsmgr);
            if (node != null)
            {
                HasQuestionName = bool.Parse(node.InnerText);
            }
            else
            {
                HasQuestionName = true;
            }

            XmlNodeList nodes = configNode.SelectNodes(@"model", nsmgr);
            List<HmmModelType> modelTypes = new List<HmmModelType>();
            StreamMergingModels.Clear();
            if (node != null)
            {
                foreach (XmlNode streamNode in nodes)
                {
                    XmlElement streamEle = (XmlElement)streamNode;
                    if (bool.Parse(streamEle.GetAttribute("mergeStream")))
                    {
                        HmmModelType modelType = (HmmModelType)Enum.Parse(typeof(HmmModelType), streamEle.GetAttribute("type"));
                        if (StreamMergingModels.IndexOf(modelType) >= 0)
                        {
                            throw new InvalidDataException(
                                Helper.NeutralFormat("Duplicated model [{0}] compilation configuration is found.", modelTypes));
                        }

                        StreamMergingModels.Add(modelType);
                    }
                }
            }

            if (FixedPoint && StreamMergingModels.IndexOf(HmmModelType.FundamentalFrequency) < 0)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Fixed point font needs FundamentalFrequency as merged."));
            }
        }

        /// <summary>
        /// Creates configuration as an xml element.
        /// </summary>
        /// <param name="dom">Xml document to be saved into.</param>
        /// <param name="schema">Xml schema.</param>
        /// <param name="name">The name of the element.</param>
        /// <returns>The created configuration xml element.</returns>
        public XmlElement CreateElement(XmlDocument dom, XmlSchema schema, string name)
        {
            if (dom == null)
            {
                throw new ArgumentNullException("dom");
            }

            if (schema == null)
            {
                throw new ArgumentNullException("schema");
            }

            XmlElement root = dom.CreateElement(name, schema.TargetNamespace);

            XmlElement ele = dom.CreateElement("fixedPoint", schema.TargetNamespace);
            ele.SetAttribute("enable", FixedPoint.ToString().ToLower());
            root.AppendChild(ele);

            ele = dom.CreateElement("dynamicFeature", schema.TargetNamespace);
            ele.SetAttribute("layout", WindowLayout.ToString());
            root.AppendChild(ele);

            ele = dom.CreateElement("stringPool", schema.TargetNamespace);
            ele.SetAttribute("hasQuestion", HasQuestionName.ToString().ToLower());
            root.AppendChild(ele);

            foreach (HmmModelType modeTyle in StreamMergingModels)
            {
                ele = dom.CreateElement("model", schema.TargetNamespace);
                ele.SetAttribute("type", modeTyle.ToString());
                ele.SetAttribute("mergeStream", "true");
                root.AppendChild(ele);
            }

            return root;
        }

        #endregion
    }

    /// <summary>
    /// HTS binary font.
    /// </summary>
    public class HtsFont : IDisposable
    {
        #region Private fields

        private HtsFontHeader _header = new HtsFontHeader();
        private Dictionary<Microsoft.Tts.Offline.Htk.HmmModelType, HtsModel> _models = new Dictionary<HmmModelType, HtsModel>();
        private StringPool _stringPool = new StringPool();
        private HtsQuestionSet _questionSet = new HtsQuestionSet();
        private TtsPhoneSet _phoneSet;
        private TtsPosSet _posSet;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the HtsFont class.
        /// </summary>
        /// <param name="phoneSet">Phone set.</param>
        /// <param name="posSet">Part of speech.</param>
        public HtsFont(TtsPhoneSet phoneSet, TtsPosSet posSet)
        {
            Helper.ThrowIfNull(phoneSet);
            Helper.ThrowIfNull(posSet);
            _phoneSet = phoneSet;
            _posSet = posSet;
        }

        #endregion

        #region Data properties

        /// <summary>
        /// Gets or sets th font header.
        /// </summary>
        public HtsFontHeader Header
        {
            get { return _header; }
            set { _header = value; }
        }

        /// <summary>
        /// Gets or sets global question set of this font.
        /// </summary>
        public HtsQuestionSet Questions
        {
            get { return _questionSet; }
            set { _questionSet = value; }
        }

        /// <summary>
        /// Gets models of this font.
        /// </summary>
        public Dictionary<HmmModelType, HtsModel> Models
        {
            get { return _models; }
        }

        /// <summary>
        /// Gets string pool of this font.
        /// </summary>
        public StringPool StringPool
        {
            get { return _stringPool; }
        }

        #endregion

        #region Supporting Properties

        /// <summary>
        /// Gets phone set.
        /// </summary>
        public TtsPhoneSet PhoneSet
        {
            get { return _phoneSet; }
        }

        /// <summary>
        /// Gets part of speech set.
        /// </summary>
        public TtsPosSet PosSet
        {
            get { return _posSet; }
        }

        /// <summary>
        /// Gets global question, union from the question sets of sub models.
        /// </summary>
        public IEnumerable<Question> UnionQuestions
        {
            get
            {
                IEnumerable<Question> questions = null;
                foreach (HmmModelType modeType in _models.Keys)
                {
                    if (questions == null)
                    {
                        questions = _models[modeType].Forest.QuestionList;
                    }
                    else
                    {
                        questions = questions.Union(_models[modeType].Forest.QuestionList);
                    }
                }

                Debug.Assert(Questions.Items == null || questions.Except(Questions.Items).Count() == 0,
                    "Global question set should be the same as what union from model questions.");

                return questions;
            }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Checks whether given state count is supported in fixed point voice font.
        /// </summary>
        /// <param name="stateCount">The state count to check with.</param>
        public static void CheckFixedPointConsistenceOnStateCount(uint stateCount)
        {
            if ((int)stateCount != Microsoft.Tts.Offline.Htk.SpsModeling.DefaultStateCount)
            {
                throw new NotSupportedException(Helper.NeutralFormat(
                    "[{0}] states model is not supported for fixed point. Only [{1}] is supported.",
                    stateCount,
                    Microsoft.Tts.Offline.Htk.SpsModeling.DefaultStateCount));
            }
        }

        /// <summary>
        /// Loads Hts model.
        /// </summary>
        /// <param name="treePath">The location of decision tree file.</param>
        /// <param name="mmfPath">The location of MMF file.</param>
        /// <param name="windowSet">Dynamic windows set.</param>
        /// <returns>Hts model.</returns>
        public HtsModel LoadModel(string treePath, string mmfPath, DynamicWindowSet windowSet)
        {
            Helper.ThrowIfNull(treePath);
            Helper.ThrowIfNull(mmfPath);
            HtsModel model = new HtsModel(this, PhoneSet, PosSet);
            model.Load(treePath, mmfPath, windowSet, Questions.CustomFeatures);
            _models.Add(model.Forest.ModelType(), model);
            return model;
        }

        /// <summary>
        /// Loads Hts transform model.
        /// </summary>
        /// <param name="treePath">The location of decision tree file.</param>
        /// <param name="mmfPath">The location of MMF file.</param>
        /// <param name="xformPath">The location of transform model file.</param>
        /// <param name="mappingPath">The location of transform mapping file.</param>
        /// <param name="windowSet">Dynamic windows set.</param>
        /// <param name="cmpMmf">The MasterMacroFile.</param>
        /// <param name="varFloorsFile">The varFloors file.</param>
        /// <param name="mgelrRefinedAlignmentMlf">The mgelr Refined alignment Mlf file.</param>
        /// <param name="streamRange">The stream range.</param>
        /// <param name="stateCount">The state count.</param>
        /// <returns>Hts model.</returns>
        public HtsModel LoadXformModel(string treePath, string mmfPath, string xformPath, string mappingPath, DynamicWindowSet windowSet, MasterMacroFile cmpMmf, string varFloorsFile, string mgelrRefinedAlignmentMlf, string streamRange, int stateCount)
        {
            Helper.ThrowIfNull(treePath);
            Helper.ThrowIfNull(mmfPath);
            Helper.ThrowIfNull(xformPath);
            Helper.ThrowIfNull(mappingPath);
            Helper.ThrowIfNull(cmpMmf);
            Helper.ThrowIfFileNotExist(varFloorsFile);
            HtsModel model = new HtsModel(this, PhoneSet, PosSet);
            model.Load(treePath, mmfPath, xformPath, mappingPath, windowSet, Questions.CustomFeatures, cmpMmf, varFloorsFile, mgelrRefinedAlignmentMlf, streamRange, stateCount);
            _models.Add(model.Forest.ModelType(), model);
            return model;
        }

        /// <summary>
        /// Adds Hts model.
        /// </summary>
        /// <param name="model">The location of decision tree file.</param>
        public void AddModel(HtsModel model)
        {
            _models.Add(model.Forest.ModelType(), model);
            model.Font = this;

            // Clears out the question items as there has questions from external model.
            Questions.Items = null;
        }

        /// <summary>
        /// Saves current HTS font out into the target file, given file location, by default serializer.
        /// </summary>
        /// <param name="fontPath">The location of the target file to write into.</param>
        public void Save(string fontPath)
        {
            Helper.ThrowIfNull(fontPath);
            Save(fontPath, new HtsFontSerializer());
        }

        /// <summary>
        /// Saves current HTS font out into the target file, given file location.
        /// </summary>
        /// <param name="fontPath">The location of the target file to write into.</param>
        /// <param name="serializer">Serializer used to save out font.</param>
        /// <param name="xformBandWidths">Transform band width for each model.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Ignore.")]
        public void Save(string fontPath, HtsFontSerializer serializer, Dictionary<HmmModelType, int> xformBandWidths = null)
        {
            Helper.ThrowIfNull(fontPath);
            Helper.ThrowIfNull(serializer);
            Helper.EnsureFolderExistForFile(fontPath);
            using (FileStream fontStream = new FileStream(fontPath, FileMode.Create))
            {
                using (DataWriter writer = new DataWriter(fontStream))
                {
                    uint size = serializer.Write(this, writer, xformBandWidths);
                    Debug.Assert(size == fontStream.Length,
                        "Calculated size of byte written out should equal with stream size.");
                }
            }
        }

        /// <summary>
        /// Loads current HTS font out from the target file, given file location, with default serializer.
        /// </summary>
        /// <param name="fontPath">The location of the target file to read from.</param>
        public void Load(string fontPath)
        {
            Helper.ThrowIfNull(fontPath);
            Load(fontPath, new HtsFontSerializer());
        }

        /// <summary>
        /// Loads current HTS font out from the target file, given file location.
        /// </summary>
        /// <param name="fontPath">The location of the target file to read from.</param>
        /// <param name="serializer">Serializer used to read font.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Ignore.")]
        public void Load(string fontPath, HtsFontSerializer serializer)
        {
            Helper.ThrowIfNull(fontPath);
            Helper.ThrowIfNull(serializer);
            try
            {
                using (FileStream stream = new FileStream(fontPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        serializer.Read(this, reader);
                        Debug.Assert(stream.Position == stream.Length,
                            "Read size of byte should equal with stream size.");
                    }
                }
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Failed to load HTS font from {0}.",
                    fontPath), ex);
            }
        }

        /// <summary>
        /// Configures HTS font compilation.
        /// </summary>
        /// <param name="config">Compilation config.</param>
        public void Configure(CompilationConfig config)
        {
            Helper.ThrowIfNull(config);
            Header.IsFixedPoint = (uint)(config.FixedPoint ? 1 : 0);
            Questions.Header.HasQuestionName = config.HasQuestionName;

            Configure(config.FixedPoint);

            foreach (HmmModelType modelType in config.StreamMergingModels)
            {
                if (Models.ContainsKey(modelType))
                {
                    Models[modelType].MergeStreams();
                }
            }

            if (config.WindowLayout == DynamicWindowLayout.StaticDelta)
            {
                PruneAcceleration();
            }
        }

        /// <summary>
        /// Config HTS font to be as fixed point font.
        /// </summary>
        /// <param name="fixedPoint">Fixed point.</param>
        public void Configure(bool fixedPoint)
        {
            if (fixedPoint)
            {
                // For runtime performance optimization, F0 models are loaded into memory to reduce the number of
                // disk access in embedded. Making it with flexible state number will lead to performance or memory hit.
                CheckFixedPointConsistenceOnStateCount((uint)Models[HmmModelType.Lsp].Forest.StateCount);
            }

            if (Header.HtsTag == HtsFontHeader.ApmDataTag)
            {
                Debug.Assert(Models[HmmModelType.Lsp].Header.GaussianConfig != null, "Gaussian config of LSP model should exist.");
                Debug.Assert(Models[HmmModelType.FundamentalFrequency].Header.GaussianConfig != null, "Gaussian config of LogF0 model should exist.");
                Models[HmmModelType.Lsp].Header.GaussianConfig.IsFixedPoint = fixedPoint;
                Models[HmmModelType.Lsp].Header.GaussianConfig.HasWeight = !fixedPoint;
                Models[HmmModelType.FundamentalFrequency].Header.GaussianConfig.IsFixedPoint = fixedPoint;
                if (Models.ContainsKey(HmmModelType.Mbe))
                {
                    Debug.Assert(Models[HmmModelType.Mbe].Header.GaussianConfig != null, "Gaussian config of MBE model should exist.");
                    Models[HmmModelType.Mbe].Header.GaussianConfig.IsFixedPoint = fixedPoint;
                    Models[HmmModelType.Mbe].Header.GaussianConfig.HasWeight = !fixedPoint;
                }
            }
            else if (Header.HtsTag == HtsFontHeader.AtmDataTag)
            {
                Debug.Assert(Models[HmmModelType.Lsp].Header.LinXformConfig != null, "Linear transform config of LSP model should exist.");
                Debug.Assert(Models[HmmModelType.FundamentalFrequency].Header.LinXformConfig != null, "Linear transform config of LogF0 model should exist.");
                Models[HmmModelType.Lsp].Header.LinXformConfig.IsFixedPoint = fixedPoint;
                Models[HmmModelType.FundamentalFrequency].Header.LinXformConfig.IsFixedPoint = fixedPoint;
                if (Models.ContainsKey(HmmModelType.Mbe))
                {
                    Debug.Assert(Models[HmmModelType.Mbe].Header.LinXformConfig != null, "Linear transform config of LogF0 model should exist.");
                    Models[HmmModelType.Mbe].Header.LinXformConfig.IsFixedPoint = fixedPoint;
                }
            }
        }

        /// <summary>
        /// Prunes acceleration dynamic features from the font.
        /// </summary>
        public void PruneAcceleration()
        {
            foreach (HtsModel model in Models.Values)
            {
                model.WindowSet = model.WindowSet.Clone(DynamicWindowLayout.StaticDelta);

                if (model.Header.ModelType == HmmModelType.Lsp ||
                    model.Header.ModelType == HmmModelType.FundamentalFrequency)
                {
                    model.PruneAcceleration();
                }

                model.MmfFile.StreamCount = (uint)model.Forest.StreamCount;
                model.MmfFile.StreamIndexes = model.MmfFile.StreamIndexes.Take((int)model.MmfFile.StreamCount).ToArray();
                model.MmfFile.StreamWidths = model.MmfFile.StreamWidths.Take((int)model.MmfFile.StreamCount).ToArray();
                model.Forest.StreamIndexes = model.Forest.StreamIndexes.Take((int)DynamicOrder.Delta).ToArray();
            }
        }

        /// <summary>
        /// Set the customized generation settings.
        /// </summary>
        /// <param name="f0CustomizedGeneration">The customized generation setting for F0 stream.</param>
        /// <param name="modelType">The model which will be handled by customized generation module.</param>
        public void SetCustomizationSetting(HtsF0CustomizedGeneration f0CustomizedGeneration, HmmModelType modelType)
        {
            HtsModel model = Models[modelType];
            model.Header.F0CustomizedGeneration = f0CustomizedGeneration;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes the resources used in this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the resources used in this object.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources;
        /// False to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != _stringPool)
                {
                    _stringPool.Dispose();
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// HTS font file header serialization data block.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public class HtsFontHeader
    {
        #region Const fields

        // HTS voice data file tag: "APM "
        public const uint ApmDataTag = 0x204D5041;

        // HTS transform data file tag: "ATM "
        public const uint AtmDataTag = 0x204D5441;

        // 3.0.0.0
        public const uint Version = 0x3;

        #endregion

        #region Fields
        public uint HtsTag;               // HTS Tag: "APM " or "ATM "
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] FormatTag;
        public uint FontSize;             // Length in byte of data followed by this field of APM/ATM font file
        public uint VersionNumber;        // Version Number
        public uint BuildNumber;          // Build Number
        public ushort LangId;               // Language ID
        public ushort IsShortPauseSupported; // First bit to indicate whether is short pause supported
        public uint IsFixedPoint;         // First bit to indicate whether as fixed point font, others as zero
        public uint SamplesPerSecond;     // Samples per secondLanguage ID
        public uint BitsPerSample;        // Bits per sample 
        public uint SamplePerFrame;       // Samples per frame

        public uint QuestionOffset;           // Global question offset
        public uint QuestionSize;             // Global question size
        public uint ModelSetOffset;       // HMM model sets offset
        public uint ModelSetSize;              // HMM model sets size
        public uint StringPoolOffset;     // string pool offset
        public uint StringPoolSize;       // string pool size
        public uint CodebookOffset;       // Codebook offset
        public uint CodebookSize;         // Codebook size
        public uint ReservedSize;         // Reserve size of buffer, as zero as far

        #endregion

        #region Properties

        /// <summary>
        /// Gets the offset of the Font Size field in header.
        /// </summary>
        public uint FontSizeOffset
        {
            get
            {
                Debug.Assert(FormatTag.Length == 16, "Format tag should be Guid with 16 bytes.");
                return (sizeof(uint) * 2) + (uint)FormatTag.Length;
            }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Reads a WeightTableHeaderSerial block from binary stream.
        /// </summary>
        /// <param name="reader">Binary reader to read data for weight table header.</param>
        /// <returns>Weight table header serial.</returns>
        public static HtsFontHeader Read(BinaryReader reader)
        {
            Helper.ThrowIfNull(reader);
            int size = Marshal.SizeOf(typeof(HtsFontHeader));
            byte[] buff = reader.ReadBytes(size);

            if (buff.Length != size)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Malformed data found, for there is no enough data for HTS font header.");
                throw new InvalidDataException(message);
            }

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(buff, 0, ptr, size);
                HtsFontHeader header = (HtsFontHeader)Marshal.PtrToStructure(ptr,
                    typeof(HtsFontHeader));

                header.Validate();

                return header;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        /// <summary>
        /// Writes HTS font header into binary stream.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <returns>Size of bytes written.</returns>
        public uint Write(DataWriter writer)
        {
            Helper.ThrowIfNull(writer);
            byte[] buff = ToBytes();
            uint size = writer.Write(buff);

#if SERIALIZATION_CHECKING
            ConsistencyChecker.Check(this, Read(writer.BaseStream.Excerpt(size)));
#endif

            return size;
        }

        /// <summary>
        /// Converts this instance into byte array.
        /// </summary>
        /// <returns>Byte array presenting this instance.</returns>
        public byte[] ToBytes()
        {
            byte[] buff = new byte[Marshal.SizeOf(typeof(HtsFontHeader))];

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(buff.Length);
                Marshal.StructureToPtr(this, ptr, false);
                Marshal.Copy(ptr, buff, 0, buff.Length);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return buff;
        }

        /// <summary>
        /// Validates data within current header.
        /// </summary>
        public void Validate()
        {
            if (HtsTag != ApmDataTag && HtsTag != AtmDataTag)
            {
                throw new InvalidDataException("The HTS data tag is invalid one.");
            }

            if (IsFixedPoint != 0 && IsFixedPoint != 1)
            {
                throw new InvalidDataException("The flag of as fixed point should be zero or one.");
            }

            if (IsShortPauseSupported != 0 && IsShortPauseSupported != 1)
            {
                throw new InvalidDataException("The flag of short pauase supported should be zero or one");
            }
        }

        #endregion
    }
}