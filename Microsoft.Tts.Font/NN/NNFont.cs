//----------------------------------------------------------------------------
// <copyright file="NNFont.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements NN font object model.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Font.NN
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
    /// NN binary font.
    /// </summary>
    public class NNFont : IDisposable
    {
        #region Private fields

        private NNFontHeader _header = new NNFontHeader();
        private StringPool _stringPool = new StringPool();
        private List<HmmModelType> _modelTypes = null;
        private Dictionary<HmmModelType, NNModel> _models = new Dictionary<HmmModelType, NNModel>();

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the NNFont class.
        /// </summary>
        public NNFont()
        {
        }

        #endregion

        #region Data properties

        /// <summary>
        /// Gets or sets nn models.
        /// </summary>
        public Dictionary<HmmModelType, NNModel> Models
        {
            get { return _models; }
            set { _models = value; }
        }

        /// <summary>
        /// Gets or sets NN font header.
        /// </summary>
        public NNFontHeader Header
        {
            get { return _header; }
            set { _header = value; }
        }

        /// <summary>
        /// Gets string pool of this font.
        /// </summary>
        public StringPool StringPool
        {
            get { return _stringPool; }
        }

        /// <summary>
        /// Gets or sets model types of this font.
        /// </summary>
        public List<HmmModelType> ModelTypes
        {
            get { return _modelTypes; }
            set { _modelTypes = value; }
        }

        #endregion

        #region Supporting Properties

        #endregion

        #region Public operations

        /// <summary>
        /// Saves current NN font out into the target file, given file location, by default serializer.
        /// </summary>
        /// <param name="fontPath">The location of the target file to write into.</param>
        /// <param name="language">The language.</param>
        /// <param name="schemaFile">The schema File.</param>
        /// <param name="outVarFile">The out VarFile.</param>
        /// <param name="phoneToIdIndexes">The phone to id Indexes.</param>
        public void Save(string fontPath, Language language, string schemaFile, string outVarFile, Dictionary<string, string> phoneToIdIndexes)
        {
            Helper.ThrowIfNull(fontPath);
            Save(fontPath, new NNFontSerializer(), language, schemaFile, outVarFile, phoneToIdIndexes);
        }

        /// <summary>
        /// Saves current NN font out into the target file, given file location.
        /// </summary>
        /// <param name="fontPath">The location of the target file to write into.</param>
        /// <param name="serializer">Serializer used to save out font.</param>
        /// <param name="language">Language.</param>
        /// <param name="schemaFile">The schema File.</param>
        /// <param name="outVarFile">The outVar File.</param>
        /// <param name="phoneToIdIndexes">The phone To Id Indexes.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Ignore.")]
        public void Save(string fontPath, NNFontSerializer serializer, Language language, string schemaFile, string outVarFile, Dictionary<string, string> phoneToIdIndexes)
        {
            Helper.ThrowIfNull(fontPath);
            Helper.ThrowIfNull(serializer);
            Helper.EnsureFolderExistForFile(fontPath);
            using (FileStream fontStream = new FileStream(fontPath, FileMode.Create))
            {
                using (DataWriter writer = new DataWriter(fontStream))
                {
                    uint size = serializer.Write(this, writer, language, schemaFile, outVarFile, phoneToIdIndexes);
                    Debug.Assert(size == fontStream.Length,
                        "Calculated size of byte written out should equal with stream size.");
                }
            }
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
    /// NN font file header serialization data block.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public class NNFontHeader
    {
        #region Const fields

        // NN voice data file tag: "NNM "
        public const uint NNMDataTag = 0x204D4e4e;

        // 1.0.0.0
        public const uint Version = 0x1;

        #endregion

        #region Fields
        public uint NNTag;                      // NN Tag: "NNM "
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] FormatTag;
        public uint FontSize;                   // Length in byte of data followed by this field of NN font file
        public uint VersionNumber;              // Version Number
        public uint BuildNumber;                // Build Number
        public ushort LangId;                   // Language ID
        public ushort IsShortPauseSupported;    // First bit to indicate whether is short pause supported
        public uint IsFixedPoint;               // First bit to indicate whether as fixed point font, others as zero
        public uint SamplesPerSecond;           // Samples per secondLanguage ID
        public uint BitsPerSample;              // Bits per sample 
        public uint SamplePerFrame;             // Samples per frame
        public uint StateCount;                 // StateCount

        public uint QuestionOffset;             // Global question offset
        public uint QuestionSize;               // Global question size
        public uint ModelSetOffset;             // HMM model sets offset
        public uint ModelSetSize;               // HMM model sets size
        public uint StringPoolOffset;           // string pool offset
        public uint StringPoolSize;             // string pool size
        public uint CodebookOffset;             // Codebook offset
        public uint CodebookSize;               // Codebook size
        public uint ReservedSize;               // Reserve size of buffer, as zero as far

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
        public static NNFontHeader Read(BinaryReader reader)
        {
            Helper.ThrowIfNull(reader);
            int size = Marshal.SizeOf(typeof(NNFontHeader));
            byte[] buff = reader.ReadBytes(size);

            if (buff.Length != size)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Malformed data found, for there is no enough data for NN font header.");
                throw new InvalidDataException(message);
            }

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(buff, 0, ptr, size);
                NNFontHeader header = (NNFontHeader)Marshal.PtrToStructure(ptr,
                    typeof(NNFontHeader));

                header.Validate();

                return header;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        /// <summary>
        /// Writes NN font header into binary stream.
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
            byte[] buff = new byte[Marshal.SizeOf(typeof(NNFontHeader))];

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
            if (NNTag != NNMDataTag)
            {
                throw new InvalidDataException("The NN data tag is invalid one.");
            }

            if (IsFixedPoint != 0 && IsFixedPoint != 1)
            {
                throw new InvalidDataException("The flag of as fixed point should be zero or one.");
            }
        }

        #endregion
    }

    /// <summary>
    /// NN model position.
    /// </summary>
    public class NNModelPosition : IBinarySerializer<NNModelPosition>
    {
        #region Public const fields

        public static int NNModelTimes = 3;

        #endregion

        #region Private fields

        private List<Location> _locations = null;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the NNModelPosition class.
        /// </summary>
        public NNModelPosition()
        {
        }

        #endregion

        #region Properties

        #endregion

        #region Data properties

        /// <summary>
        /// Gets or sets the model location.
        /// </summary>
        public List<Location> Location
        {
            get { return _locations; }
            set { _locations = value; }
        }

        #endregion

        #region Public operations

        /// <summary>
        /// Write windows coefficients to binary file.
        /// </summary>
        /// <param name="writer">Binary file writer.</param>
        /// <returns>Size of bytes written out.</returns>
        public uint Save(DataWriter writer)
        {
            Helper.ThrowIfNull(writer);

            uint size = 0;
            size += writer.Write((uint)_locations.Count);

            for (int i = 0; i < _locations.Count; i++)
            {
                size += writer.Write(_locations[i].Offset);
                size += writer.Write(_locations[i].Length);
            }

            return size;
        }

        /// <summary>
        /// Load dynamic window set from binary reader.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <returns>Dynamic window set.</returns>
        public NNModelPosition Load(BinaryReader reader)
        {
            Helper.ThrowIfNull(reader);

            return this;
        }

        #endregion
    }

    /// <summary>
    /// NN Model.
    /// </summary>
    public class NNModel
    {
        #region Private fields

        private HmmModelType _type;
        private NNDynamicWindowSet _windowSet;
        private NNModelPosition _position;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the NNModel class.
        /// </summary>
        /// <param name="type">Model type.</param>
        /// <param name="windowSet">Window set.</param>
        /// <param name="position">Model position.</param>
        public NNModel(HmmModelType type, NNDynamicWindowSet windowSet, NNModelPosition position)
        {
            Helper.ThrowIfNull(type);
            Helper.ThrowIfNull(position);

            _type = type;
            _windowSet = windowSet;
            _position = position;
        }

        #endregion

        #region Data properties

        /// <summary>
        /// Gets or sets the model type.
        /// </summary>
        public HmmModelType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// Gets or sets the dynamic window set.
        /// </summary>
        public NNDynamicWindowSet WindowSet
        {
            get { return _windowSet; }
            set { _windowSet = value; }
        }

        /// <summary>
        /// Gets or sets the model position.
        /// </summary>
        public NNModelPosition Position
        {
            get { return _position; }
            set { _position = value; }
        }

        #endregion

        #region Supporting properties

        #endregion

        #region Operations

        #endregion

        #region Private operation

        #endregion
    }
}