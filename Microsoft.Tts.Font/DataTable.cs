//----------------------------------------------------------------------------
// <copyright file="DataTable.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module defines classes to write data tables.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// Contains data table setting.
    /// </summary>
    public class DataTableSetting
    {
        /// <summary>
        /// Gets or sets a value used for quantize table values.
        /// </summary>
        public IQuantizer Quantizer { get; set; }

        /// <summary>
        /// Gets or sets the quantizer parameters when the quantizer is not exist.
        /// </summary>
        public QuantizerParameters QuantizerParas { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether need row map.
        /// </summary>
        public bool MapRow { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether need column map.
        /// </summary>
        public bool MapColumn { get; set; }

        /// <summary>
        /// Gets bits mask of table attributes.
        /// </summary>
        public int AttributeBits
        {
            get
            {
                int ret = 0;

                if (MapRow)
                {
                    ret |= (int)TtsDataTableAttr.TTS_DATA_TABLE_MAP_ROW;
                }

                if (MapColumn)
                {
                    ret |= (int)TtsDataTableAttr.TTS_DATA_TABLE_MAP_COLUMN;
                }

                return ret;
            }
        }
    }

    public class QuantizerParameters
    {
        /// <summary>
        /// Gets or sets the target bits number to quantize to.
        /// </summary>
        public int TargetSize { get; set; }

        /// <summary>
        ///  Gets or sets the scale factor for dequantization.
        /// </summary>
        public float DequantizeScaleFactor { get; set; }

        /// <summary>
        ///  Gets or sets quantization error for the quantizer.
        /// </summary>
        public float DequantizeOffset { get; set; }
    }

    /// <summary>
    /// Writer class for data tables.
    /// </summary>
    public class DataTableWriter : IDisposable
    {
        private HashSet<IntArray> _keySet = new HashSet<IntArray>();

        /// <summary>
        /// Writer of intermediate table data.
        /// </summary>
        private BinaryWriter _tableWriter;

        private BinaryWriter _indexWriter;

        private string _tablePath;

        /// <summary>
        /// Initializes a new instance of the DataTableWriter class.
        /// </summary>
        /// <param name="path">Data table path.</param>
        /// <param name="keyLength">Lenth of the key.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public DataTableWriter(string path, int keyLength)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (keyLength < 0)
            {
                throw new ArgumentException("keyLength");
            }

            KeyLength = keyLength;
            _indexWriter = new BinaryWriter(new MemoryStream());

            _tablePath = path;

            var tempFile = path + ".tmp";
            _tableWriter = new BinaryWriter(new FileStream(tempFile, FileMode.Create, FileAccess.ReadWrite));
        }

        /// <summary>
        /// Gets data table key length.
        /// </summary>
        public int KeyLength { get; private set; }

        /// <summary>
        /// Gets table count added.
        /// </summary>
        public int TableCount
        {
            get
            {
                return _keySet.Count;
            }
        }

        /// <summary>
        /// Add a table to table file.
        /// </summary>
        /// <param name="item">A data table.</param>
        /// <param name="setting">Data table setting.</param>
        public void Add(DataTable item, DataTableSetting setting)
        {
            if (!IsOpened())
            {
                throw new InvalidOperationException("data table not opened");
            }

            Validate(item, setting);

            long tableOffset = _tableWriter.BaseStream.Position;

            WriteTable(item, setting);

            WriteIndex(tableOffset,
                (uint)(_tableWriter.BaseStream.Position - tableOffset), item.Key);
        }

        /// <summary>
        /// Dispose() calls Dispose(true).
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Close the writer and underlying file.
        /// </summary>
        public void Close()
        {
            if (!IsOpened())
            {
                throw new InvalidOperationException("data table not opened");
            }

            byte[] tableHeaderBytes = WriteTableHeader();
            byte[] indexBytes = ((MemoryStream)_indexWriter.BaseStream).ToArray();

            ulong dataSize = (ulong)(tableHeaderBytes.Length +
                indexBytes.Length + _tableWriter.BaseStream.Position);

            byte[] fontHeaderBytes = WriteFontHeader(dataSize);

            FileStream file = new FileStream(_tablePath, FileMode.Create);
            try
            {
                using (var writer = new BinaryWriter(file))
                {
                    file = null;
                    writer.Write(fontHeaderBytes);
                    writer.Write(tableHeaderBytes);
                    writer.Write(indexBytes);

                    _tableWriter.Seek(0, SeekOrigin.Begin);
                    CopyStream(_tableWriter.BaseStream, writer.BaseStream);
                }
            }
            finally
            {
                if (null != file)
                {
                    file.Dispose();
                }
            }

            var tempFile = ((FileStream)_tableWriter.BaseStream).Name;
            _tableWriter.Close();
            _tableWriter = null;

            File.Delete(tempFile);
        }

        /// <summary>
        /// Close data table file.
        /// </summary>
        /// <param name="disposing">Dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (IsOpened())
                {
                    Close();
                }

                if (null != _indexWriter)
                {
                    _indexWriter.Dispose();
                }
            }
        }

        /// <summary>
        /// Writes font header for data table file.
        /// </summary>
        /// <param name="dataSize">The data size field of font file.</param>
        /// <returns>Bytes number written.</returns>
        private static byte[] WriteFontHeader(ulong dataSize)
        {
            VoiceFontHeader voiceFontHeader = new VoiceFontHeader
            {
                FileTag = DataTableManager.DataTableTag,
                FormatTag = VoiceFontTag.FmtIdDataTable,
                DataSize = dataSize,
                Version = 0,
                Build = 0
            };

            MemoryStream stream = new MemoryStream();
            try
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    stream = null;
                    voiceFontHeader.Save(writer);
                    byte[] bytes = ((MemoryStream)writer.BaseStream).ToArray();
                    return bytes;
                }
            }
            finally
            {
                if (null != stream)
                {
                    stream.Dispose();
                }
            }
        }

        /// <summary>
        /// Copy stream content.
        /// </summary>
        /// <param name="source">Source stream.</param>
        /// <param name="target">Target stream.</param>
        private static void CopyStream(Stream source, Stream target)
        {
            int chunk = 1 << 20;
            var buffer = new byte[chunk];

            int read;

            while ((read = source.Read(buffer, 0, chunk)) > 0)
            {
                target.Write(buffer, 0, read);
            }
        }

        /// <summary>
        /// Writes table index.
        /// </summary>
        /// <param name="tableOffset">The table offset.</param>
        /// <param name="tableSize">The table size.</param>
        /// <param name="key">The table key.</param>
        private void WriteIndex(long tableOffset, uint tableSize, int[] key)
        {
            // index field: key
            byte[] bytes = new byte[key.Length * sizeof(int)];
            Buffer.BlockCopy(key, 0, bytes, 0, bytes.Length);
            _indexWriter.Write(bytes);

            // index field: offset
            _indexWriter.Write(tableOffset);

            // index field: size
            _indexWriter.Write(tableSize);
        }

        /// <summary>
        /// Writes a data table item.
        /// </summary>
        /// <param name="item">Data table item.</param>
        /// <param name="setting">Data table setting.</param>
        private void WriteTable(DataTable item, DataTableSetting setting)
        {
            int targetSize = 0;
            float scaleFacor = 0.0f;
            float dequantizeOffset = 0.0f;

            if (setting.Quantizer != null)
            {
                targetSize = setting.Quantizer.TargetSize;
                scaleFacor = setting.Quantizer.DequantizeScaleFactor;
                dequantizeOffset = setting.Quantizer.DequantizeOffset;
            }
            else
            {
                targetSize = setting.QuantizerParas.TargetSize;
                scaleFacor = setting.QuantizerParas.DequantizeScaleFactor;
                dequantizeOffset = setting.QuantizerParas.DequantizeOffset;
            }

            // table field: setting: attribution
            _tableWriter.Write(setting.AttributeBits);

            // table field: setting: itemBits
            _tableWriter.Write(targetSize);

            // table field: setting: scale
            _tableWriter.Write(scaleFacor);

            // table field: setting: offset
            _tableWriter.Write(dequantizeOffset);

            // table field: row number
            if (setting.MapRow)
            {
                _tableWriter.Write(item.RowMap.Length);
            }

            int matrixRowLength = 0;
            int matrixColLength = 0;
            if (item.DataMatrix != null)
            {
                matrixRowLength = item.DataMatrix.GetLength(0);
                matrixColLength = item.DataMatrix.GetLength(1);
            }
            else
            {
                matrixRowLength = item.RowCount;
                matrixColLength = item.ColumnCount;
            }

            _tableWriter.Write(matrixRowLength);

            // table field: column number
            if (setting.MapColumn)
            {
                _tableWriter.Write(item.ColumnMap.Length);
            }

            _tableWriter.Write(matrixColLength);

            byte[] bytes = null;

            // table field: row map
            if (setting.MapRow)
            {
                bytes = new byte[item.RowMap.Length * sizeof(ushort)];
                Buffer.BlockCopy(item.RowMap, 0, bytes, 0, bytes.Length);
                _tableWriter.Write(bytes);
            }

            // table field: column map
            if (setting.MapColumn)
            {
                bytes = new byte[item.ColumnMap.Length * sizeof(ushort)];
                Buffer.BlockCopy(item.ColumnMap, 0, bytes, 0, bytes.Length);
                _tableWriter.Write(bytes);
            }

            // table field: value
            if (setting.Quantizer != null)
            {
                float[] values = new float[item.DataMatrix.Length];
                Buffer.BlockCopy(item.DataMatrix, 0, values, 0, values.Length * sizeof(float));
                bytes = setting.Quantizer.Quantize(values);
                Debug.Assert(
                bytes.Length == ((values.Length * setting.Quantizer.TargetSize) + 7) / 8,
                "quantization output length incorrect");
            }
            else
            {
                // when quantilizer is null, means no orignal data matri, table will get data from the Matrix Bytes
                bytes = item.MatrixBytes;
            }

            _tableWriter.Write(bytes);
        }

        /// <summary>
        /// Validates the new item to be added.
        /// </summary>
        /// <param name="item">The new item to be added.</param>
        /// <param name="setting">The data table setting.</param>
        private void Validate(DataTable item, DataTableSetting setting)
        {
            if (KeyLength != item.Key.Length)
            {
                throw new ArgumentException("table key length mismatch");
            }

            if (setting.MapRow ^ item.RowMap != null)
            {
                throw new ArgumentException("table row map invalid");
            }

            if (setting.MapColumn ^ item.ColumnMap != null)
            {
                throw new ArgumentException("table column map invalid");
            }

            if (item.MatrixBytes == null && item.DataMatrix.Rank != 2)
            {
                throw new ArgumentException("table must be 2-D");
            }

            if (item.MatrixBytes == null && item.DataMatrix.Length <= 0)
            {
                throw new ArgumentException("table can't be empty");
            }

            var key = new IntArray(item.Key);

            if (_keySet.Contains(key))
            {
                throw new ArgumentException("duplicated data table key");
            }

            if (item.MatrixBytes != null && item.RowCount == 0)
            {
                throw new ArgumentException("Row count is not correctly given when using table bytes");
            }

            if (item.MatrixBytes != null && item.ColumnCount == 0)
            {
                throw new ArgumentException("column count is not correctly given when using table bytes");
            }

            _keySet.Add(key);
        }

        /// <summary>
        /// Writes data table header.
        /// </summary>
        /// <returns>The bytes number written.</returns>
        private byte[] WriteTableHeader()
        {
            byte[] bytes;

            MemoryStream stream = new MemoryStream();
            try
            {
                using (BinaryWriter headerWriter = new BinaryWriter(stream))
                {
                    stream = null;

                    // Header field: index count
                    headerWriter.Write(_keySet.Count);

                    // Header field: key length
                    headerWriter.Write(KeyLength);

                    bytes = ((MemoryStream)headerWriter.BaseStream).ToArray();
                    return bytes;
                }
            }
            finally
            {
                if (null != stream)
                {
                    stream.Dispose();
                }
            }
        }

        /// <summary>
        /// Test if the data table is in open.
        /// </summary>
        /// <returns>True if the data table is opened, false otherwise.</returns>
        private bool IsOpened()
        {
            return _tableWriter != null;
        }
    }

    /// <summary>
    /// The class to read single data table.
    /// </summary>
    public class SingleDataTableReader : IDisposable
    {
        /// <summary>
        /// Reader of intermediate table data.
        /// </summary>
        private BinaryReader _tableReader;
        private DataTableSetting _tableSetting;
        private DataTable _dataTable;
        private int[] _key = null;
        private TableHeader _tableHader = null;

        /// <summary>
        /// Initializes a new instance of the SingleDataTableReader class.
        /// </summary>
        /// <param name="path">Data table path.</param>
        /// <param name="keyLength">The length of the key.</param>
        /// <param name="dataTableSetting">The setting of data table.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public SingleDataTableReader(string path, int keyLength, DataTableSetting dataTableSetting)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (keyLength < 0)
            {
                throw new ArgumentException("keyLength");
            }

            if (dataTableSetting == null)
            {
                throw new ArgumentException("dataTablesetting");
            }

            _tableReader = new BinaryReader(File.Open(path, FileMode.Open));
            _tableSetting = dataTableSetting;
            _dataTable = new DataTable();
            KeyLength = keyLength;
        }

        /// <summary>
        /// Gets data table key length.
        /// </summary>
        public int KeyLength { get; private set; }

        /// <summary>
        /// Gets the setting of table.
        /// </summary>
        public DataTableSetting TableSetting
        {
            get
            {
                return _tableSetting;
            }

            private set
            {
            }
        }

        /// <summary>
        /// Gets the data table.
        /// </summary>
        public DataTable DataTable
        {
            get
            {
                return _dataTable;
            }

            private set
            {
            }
        }

        /// <summary>
        /// Read single table from table file.
        /// </summary>
        public void ReadSingleDataTable()
        {
            try
            {
                if (!IsOpened())
                {
                    throw new InvalidOperationException("data table not opened");
                }

                VoiceFontHeader voiceFontHeader = new VoiceFontHeader();

                // Load font header
                voiceFontHeader.Load(_tableReader);

                ReadTableHeader();
                LoadIndexer();
                LoadTable();

                // End of stream
                _tableReader.ReadInt32();
            }
            catch (EndOfStreamException)
            {
                // Do nothing here
            }
            finally
            {
                _tableReader.Close();
                _tableReader = null;
            }
        }

        /// <summary>
        /// Dispose() calls Dispose(true).
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Close data table file.
        /// </summary>
        /// <param name="disposing">Dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (IsOpened())
            {
                _tableReader.Close();
            }
        }

        private void ReadTableHeader()
        {
            // It read the count and the length of key
            _tableHader = DataTableReaderUtil.ReadTableHeader(_tableReader);
        }

        private void LoadIndexer()
        {
            List<IndexInfo> indexInfo = DataTableReaderUtil.ReadIndexInfo<IndexInfo>(_tableReader, _tableHader);
            if (indexInfo.Count != 1)
            {
                throw new Exception("corrupt data file");
            }

            _key = indexInfo[0].Key;
        }

        private void LoadTable()
        {
            _dataTable = DataTableReaderUtil.ReadTable(_tableReader, _tableSetting, _key);
        }

        /// <summary>
        /// Test if the data table is in open.
        /// </summary>
        /// <returns>True if the data table is opened, false otherwise.</returns>
        private bool IsOpened()
        {
            return _tableReader != null;
        }
    }

    /// <summary>
    /// The ACD Data Reader to read the acd data.
    /// </summary>
    public class ACDDataReader : IDisposable
    {
        /// <summary>
        /// The binary reader to do the read.
        /// </summary>
        private BinaryReader reader = null;

        public ACDDataReader(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            this.reader = reader;
        }

        /// <summary>
        /// Dispose the instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Read the ACD Data from the reader.
        /// </summary>
        /// <returns>The ACDData.</returns>
        public ACDData ReadACDData()
        {
            ACDData data = null;
            try
            {
                if (!IsOpened())
                {
                    throw new InvalidOperationException("data table not opened");
                }

                data = new ACDData();

                data.FontHeader = DataTableReaderUtil.ReadVoiceFontHeader(reader);
                data.DataHeader = DataTableReaderUtil.ReadTableHeader(reader);
                data.IndexInfo = DataTableReaderUtil.ReadIndexInfo<AcdIndexInfo>(reader, data.DataHeader);
                foreach (AcdIndexInfo info in data.IndexInfo)
                {
                    if (AcdIndexInfo.EqualKey(info.Key, AcdIndexInfo.LpccKey))
                    {
                        data.LpccData = ReadACDTable(reader, info);
                    }
                    else if (AcdIndexInfo.EqualKey(info.Key, AcdIndexInfo.RealLpccKey))
                    {
                        data.RealLpccData = ReadACDTable(reader, info);
                    }
                    else if (AcdIndexInfo.EqualKey(info.Key, AcdIndexInfo.F0Key))
                    {
                        data.F0Data = ReadACDTable(reader, info);
                    }
                    else if (AcdIndexInfo.EqualKey(info.Key, AcdIndexInfo.GainKey))
                    {
                        data.GainData = ReadACDTable(reader, info);
                    }
                    else if (AcdIndexInfo.EqualKey(info.Key, AcdIndexInfo.PowerKey))
                    {
                        data.PowerData = ReadACDTable(reader, info);
                    }
                    else if (AcdIndexInfo.EqualKey(info.Key, AcdIndexInfo.PitchMarkerKey))
                    {
                        data.PitchMarkerData = ReadACDTable(reader, info);
                    }
                }
            }
            catch (EndOfStreamException)
            {
                // Do nothing here
            }
            finally
            {
                reader.Close();
                reader = null;
            }

            return data;
        }

        /// <summary>
        /// Close data table file.
        /// </summary>
        /// <param name="disposing">Dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (IsOpened())
            {
                reader.Close();
            }
        }

        /// <summary>
        /// Test if the data table is in open.
        /// </summary>
        /// <returns>True if the data table is opened, false otherwise.</returns>
        private bool IsOpened()
        {
            return reader != null;
        }

        /// <summary>
        /// Read the ACD Table from the file.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <param name="info">The index info.</param>
        /// <returns>The acd table.</returns>
        private ACDTable ReadACDTable(BinaryReader reader, AcdIndexInfo info)
        {
            ACDTable table = new ACDTable();
            DataTableSetting setting = new DataTableSetting();
            table.DataTable = DataTableReaderUtil.ReadTable(reader, setting, info, info.Key);
            table.Setting = setting;
            return table;
        }
    }

    /// <summary>
    /// The index Infomation for the ACD file.
    /// </summary>
    public class IndexInfo
    {
        /// <summary>
        /// Gets or sets the index key.
        /// </summary>
        public int[] Key { get; set; }

        /// <summary>
        /// Gets or sets the offset for the table.
        /// </summary>
        public long OffSet { get; set; }

        /// <summary>
        /// Gets or sets the size for the table.
        /// </summary>
        public uint Size { get; set; }

        /// <summary>
        /// Load the index info from the reader.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <param name="indexLen">Length of the index info.</param>
        public virtual void Load(BinaryReader reader, uint indexLen)
        {
            byte[] bytes = new byte[indexLen * sizeof(int)];
            Key = new int[indexLen];
            bytes = reader.ReadBytes(bytes.Length);
            Buffer.BlockCopy(bytes, 0, Key, 0, bytes.Length);
            OffSet = reader.ReadInt64();
            Size = reader.ReadUInt32();
        }
    }

    /// <summary>
    /// The ACD Index info.
    /// </summary>
    public class AcdIndexInfo : IndexInfo
    {
        /// <summary>
        /// LPCC Key.
        /// </summary>
        public static AcdTableKey LpccKey = new AcdTableKey()
        {
            AcousticDataType = (int)Microsoft.Tts.ServiceProvider.HmmModelType.HMT_SPECTRUM_LSF,
        };

        /// <summary>
        /// Real LPCC Key.
        /// </summary>
        public static AcdTableKey RealLpccKey = new AcdTableKey()
        {
            AcousticDataType = (int)Microsoft.Tts.ServiceProvider.HmmModelType.HMT_SPECTRUM_LSF_REAL,
        };

        /// <summary>
        /// F0Key.
        /// </summary>
        public static AcdTableKey F0Key = new AcdTableKey()
        {
            AcousticDataType = (int)Microsoft.Tts.ServiceProvider.HmmModelType.HMT_FUNDAMENTAL_FREQUENCY,
        };

        /// <summary>
        /// Gain Key.
        /// </summary>
        public static AcdTableKey GainKey = new AcdTableKey()
        {
            AcousticDataType = (int)Microsoft.Tts.ServiceProvider.HmmModelType.HMT_GAIN,
        };

        /// <summary>
        /// Power Key.
        /// </summary>
        public static AcdTableKey PowerKey = new AcdTableKey()
        {
            AcousticDataType = (int)Microsoft.Tts.ServiceProvider.HmmModelType.HMT_POWER,
        };

        /// <summary>
        /// PitchMarker Key.
        /// </summary>
        public static AcdTableKey PitchMarkerKey = new AcdTableKey()
        {
            AcousticDataType = (int)Microsoft.Tts.ServiceProvider.HmmModelType.HMT_PITCHMARKER,
        };

        /// <summary>
        /// Gets or sets the ACDKey for the index info.
        /// </summary>
        public AcdTableKey AcdKey { get; set; }

        /// <summary>
        /// Whether two keys are all the same.
        /// </summary>
        /// <param name="key">Given key.</param>
        /// <param name="acdKey">AcdKey to comopare.</param>
        /// <returns>Whether the two keys are equal.</returns>
        public static bool EqualKey(int[] key, AcdTableKey acdKey)
        {
            bool equal = key.Length == acdKey.Key.Length;

            for (int i = 0; i < key.Length && equal; i++)
            {
                equal &= key[i] == acdKey.Key[i];
            }

            return equal;
        }

        /// <summary>
        /// Load the index info data from binary reader.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <param name="indexLen">The index length.</param>
        public override void Load(BinaryReader reader, uint indexLen)
        {
            base.Load(reader, indexLen);
            if (EqualKey(this.Key, LpccKey))
            {
                this.AcdKey = LpccKey;
            }
            else if (EqualKey(this.Key, RealLpccKey))
            {
                this.AcdKey = RealLpccKey;
            }
            else if (EqualKey(this.Key, F0Key))
            {
                this.AcdKey = F0Key;
            }
            else if (EqualKey(this.Key, GainKey))
            {
                this.AcdKey = GainKey;
            }
            else if (EqualKey(this.Key, PowerKey))
            {
                this.AcdKey = PowerKey;
            }
            else if (EqualKey(this.Key, PitchMarkerKey))
            {
                this.AcdKey = PitchMarkerKey;
            }
        }
    }

    /// <summary>
    /// The table header for the data table.
    /// </summary>
    public class TableHeader
    {
        /// <summary>
        /// Gets or sets the table number.
        /// </summary>
        public uint TableNumber { get; set; }

        /// <summary>
        /// Gets or sets the Index Length.
        /// </summary>
        public uint IndexLen { get; set; }

        /// <summary>
        /// Load the Table Header from the binary reader.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        public void Load(BinaryReader reader)
        {
            TableNumber = reader.ReadUInt32();
            IndexLen = reader.ReadUInt32();
        }
    }

    /// <summary>
    /// The ACD Data load from the ACD file.
    /// </summary>
    public class ACDData
    {
        /// <summary>
        /// Gets or sets the voice font Header.
        /// </summary>
        public VoiceFontHeader FontHeader { get; set; }

        /// <summary>
        /// Gets or sets the Table Header.
        /// </summary>
        public TableHeader DataHeader { get; set; }

        /// <summary>
        /// Gets or sets the IndexInfo.
        /// </summary>
        public List<AcdIndexInfo> IndexInfo { get; set; }

        /// <summary>
        /// Gets or sets the LpccData.
        /// </summary>
        public ACDTable LpccData { get; set; }

        /// <summary>
        /// Gets or sets the RealLpccData.
        /// </summary>
        public ACDTable RealLpccData { get; set; }

        /// <summary>
        /// Gets or sets the F0Data.
        /// </summary>
        public ACDTable F0Data { get; set; }

        /// <summary>
        /// Gets or sets the GainData.
        /// </summary>
        public ACDTable GainData { get; set; }

        /// <summary>
        /// Gets or sets the PowerData.
        /// </summary>
        public ACDTable PowerData { get; set; }

        /// <summary>
        /// Gets or sets the PitchMarkerData.
        /// </summary>
        public ACDTable PitchMarkerData { get; set; }
    }

    /// <summary>
    /// The ACD Table which used to store acd data.
    /// </summary>
    public class ACDTable
    {
        /// <summary>
        /// Gets or sets the Data Table.
        /// </summary>
        public DataTable DataTable { get; set; }

        /// <summary>
        /// Gets or sets the Data table setting.
        /// </summary>
        public DataTableSetting Setting { get; set; }
    }

    /// <summary>
    /// The data table reader util to read the contents.
    /// </summary>
    public class DataTableReaderUtil
    {
        /// <summary>
        /// Read the voice font header.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <returns>The voice font header.</returns>
        public static VoiceFontHeader ReadVoiceFontHeader(BinaryReader reader)
        {
            VoiceFontHeader header = new VoiceFontHeader();
            header.Load(reader);
            return header;
        }

        /// <summary>
        /// Read the table header.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <returns>The table header.</returns>
        public static TableHeader ReadTableHeader(BinaryReader reader)
        {
            TableHeader header = new TableHeader();
            header.Load(reader);
            return header;
        }

        /// <summary>
        /// Read the Index Info.
        /// </summary>
        /// <typeparam name="T">The index info type.</typeparam>
        /// <param name="reader">The binray reader.</param>
        /// <param name="header">The table header.</param>
        /// <returns>The List of the index info.</returns>
        public static List<T> ReadIndexInfo<T>(BinaryReader reader, TableHeader header) where T : IndexInfo, new()
        {
            List<T> infoList = new List<T>();

            for (int i = 0; i < header.TableNumber; i++)
            {
                T info = new T();
                info.Load(reader, header.IndexLen);
                infoList.Add(info);
            }

            return infoList;
        }

        /// <summary>
        /// Read a table from binary reader.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <param name="setting">The data table setting.</param>
        /// <param name="key">The key for the data table.</param>
        /// <returns>The data table.</returns>
        public static DataTable ReadTable(BinaryReader reader, DataTableSetting setting, int[] key)
        {
            return ReadTable(reader, setting, null, key);
        }

        /// <summary>
        /// Read a data table from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <param name="setting">The settings information.</param>
        /// <param name="info">The index inforamtion.</param>
        /// <param name="key">The data table key.</param>
        /// <returns>A data table read from the reader.</returns>
        public static DataTable ReadTable(BinaryReader reader, DataTableSetting setting, IndexInfo info, int[] key)
        {
            DataTable table = new DataTable();
            long startPosition = reader.BaseStream.Position;

            // table field: setting: attribution
            var settingAttributes = reader.ReadInt32();

            // table field: setting: itemBits
            var quantizerTargetsize = reader.ReadInt32();

            // table field: setting: scale
            var dequantizeScaleFactor = reader.ReadSingle();

            // table field: setting: offset
            var dequantizeOffset = reader.ReadSingle();

            if (setting.Quantizer == null)
            {
                setting.QuantizerParas = new QuantizerParameters();
                setting.MapColumn = (settingAttributes & (int)TtsDataTableAttr.TTS_DATA_TABLE_MAP_COLUMN) == (int)TtsDataTableAttr.TTS_DATA_TABLE_MAP_COLUMN;
                setting.MapRow = (settingAttributes & (int)TtsDataTableAttr.TTS_DATA_TABLE_MAP_ROW) == (int)TtsDataTableAttr.TTS_DATA_TABLE_MAP_ROW;
                setting.QuantizerParas.TargetSize = quantizerTargetsize;
                setting.QuantizerParas.DequantizeScaleFactor = dequantizeScaleFactor;
                setting.QuantizerParas.DequantizeOffset = dequantizeOffset;
            }

            // table field: row number
            int itemRowMapLength = 0;
            if (setting.MapRow)
            {
                itemRowMapLength = reader.ReadInt32();
            }

            int dataMatrixRowLength = reader.ReadInt32();
            table.RowCount = dataMatrixRowLength;

            // table field: column number
            int itemColumnMapLength = 0;
            if (setting.MapColumn)
            {
                itemColumnMapLength = reader.ReadInt32();
            }

            int dataMatrixColumnLength = reader.ReadInt32();
            table.ColumnCount = dataMatrixColumnLength;

            byte[] bytes = null;
            ushort[] rowMap = null;
            ushort[] columnMap = null;

            // table field: row map
            if (setting.MapRow)
            {
                bytes = new byte[itemRowMapLength * sizeof(ushort)];
                bytes = reader.ReadBytes(bytes.Length);
                rowMap = new ushort[itemRowMapLength];
                Buffer.BlockCopy(bytes, 0, rowMap, 0, bytes.Length);
            }

            // table field: column map
            if (setting.MapColumn)
            {
                bytes = new byte[itemColumnMapLength * sizeof(ushort)];
                bytes = reader.ReadBytes(bytes.Length);
                columnMap = new ushort[itemColumnMapLength];
                Buffer.BlockCopy(bytes, 0, columnMap, 0, bytes.Length);
            }

            // table field: value
            float[,] dataMatrix = null;

            if (info != null)
            {
                // read for those quantilized, only read into bytes, not into the data matrix
                long currentPosition = reader.BaseStream.Position;
                long offset = currentPosition - startPosition;
                long tableSize = info.Size - offset;
                bytes = new byte[tableSize];
                bytes = reader.ReadBytes(bytes.Length);
                table.MatrixBytes = bytes;
                table.Key = info.Key;
            }
            else
            {
                // directly read, for CCTable used.
                bytes = new byte[dataMatrixRowLength * dataMatrixColumnLength * sizeof(float)];
                bytes = reader.ReadBytes(bytes.Length);
                dataMatrix = new float[dataMatrixRowLength, dataMatrixColumnLength];
                Buffer.BlockCopy(bytes, 0, dataMatrix, 0, bytes.Length);
                table.Key = key;
            }

            // Set the key value to table
            table.RowMap = rowMap;
            table.ColumnMap = columnMap;
            table.DataMatrix = dataMatrix;

            return table;
        }
    }

    /// <summary>
    /// The ACDData Writer to write the data to a file.
    /// </summary>
    public class ACDDataWriter
    {
        /// <summary>
        /// Write the acd data to a file.
        /// </summary>
        /// <param name="data">The acd data.</param>
        /// <param name="file">The file path.</param>
        public void Write(ACDData data, string file)
        {
            using (var writer = new DataTableWriter(file, AcdTableKey.KeyLength))
            {
                if (data.LpccData != null)
                {
                    writer.Add(data.LpccData.DataTable, data.LpccData.Setting);
                }

                if (data.RealLpccData != null)
                {
                    writer.Add(data.RealLpccData.DataTable, data.RealLpccData.Setting);
                }

                if (data.F0Data != null)
                {
                    writer.Add(data.F0Data.DataTable, data.F0Data.Setting);
                }

                if (data.GainData != null)
                {
                    writer.Add(data.GainData.DataTable, data.GainData.Setting);
                }

                if (data.PowerData != null)
                {
                    writer.Add(data.PowerData.DataTable, data.PowerData.Setting);
                }

                if (data.PitchMarkerData != null)
                {
                    writer.Add(data.PitchMarkerData.DataTable, data.PitchMarkerData.Setting);
                }
            }
        }
    }

    /// <summary>
    /// Key for ACD data table.
    /// </summary>
    public class AcdTableKey
    {
        public const int KeyLength = 1;

        /// <summary>
        /// Gets or sets the acoustic data type.
        /// </summary>
        public int AcousticDataType { get; set; }

        /// <summary>
        /// Gets the key as int array.
        /// </summary>
        public int[] Key
        {
            get
            {
                return new[] { AcousticDataType };
            }
        }
    }

    /// <summary>
    /// Key for concatenation data table.
    /// </summary>
    public class CcTableKey
    {
        public const int KeyLength = 2;

        /// <summary>
        /// Gets or sets the previous candidate group id.
        /// </summary>
        public int PreviousCandidateGroupId { get; set; }

        /// <summary>
        /// Gets or sets the next candidate group id.
        /// </summary>
        public int NextCandidateGroupId { get; set; }

        /// <summary>
        /// Gets the key as int array.
        /// </summary>
        public int[] Key
        {
            get
            {
                return new[] { PreviousCandidateGroupId, NextCandidateGroupId };
            }
        }
    }

    /// <summary>
    /// Add hashing for int array.
    /// </summary>
    internal class IntArray
    {
        /// <summary>
        /// Initializes a new instance of the IntArray class.
        /// </summary>
        /// <param name="values">The int array.</param>
        public IntArray(int[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            Values = values;
        }

        /// <summary>
        /// Gets internal int array.
        /// </summary>
        public int[] Values { get; private set; }

        /// <summary>
        /// Overrides default Equals.
        /// </summary>
        /// <param name="obj">The object to compare with this one.</param>
        /// <returns>True if both have none null, sequentially equal key vectors; false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            IntArray item = obj as IntArray;

            if (item == null)
            {
                return false;
            }

            return Values.SequenceEqual(item.Values);
        }

        /// <summary>
        /// Overrides default GetHashCode.
        /// </summary>
        /// <returns>Key vector based hashcode.</returns>
        public override int GetHashCode()
        {
            return Values.Sum();
        }
    }
}