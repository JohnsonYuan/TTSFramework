//----------------------------------------------------------------------------
// <copyright file="LinXformSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements Dynamic Window Set
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
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Htk;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider.Compress;

    /// <summary>
    /// LinXForm serializer.
    /// </summary>
    public class LinXformSerializer
    {
        #region Construction
        /// <summary>
        /// Initializes a new instance of the LinXformSerializer class.
        /// </summary>
        protected LinXformSerializer()
        {
            MeanStatistic = new StreamStatistic();
            VarStatistic = new StreamStatistic();
            IsNeedQuantize = true;
        }

        /// <summary>
        /// Initializes a new instance of the LinXformSerializer class.
        /// </summary>
        /// <param name="config">LinXForm configuration.</param>
        protected LinXformSerializer(LinXformConfig config) :
            this()
        {
            Helper.ThrowIfNull(config);
            Config = config;
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the configuration of the LinXForm writer.
        /// </summary>
        public LinXformConfig Config
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets encoder of the LinXForm writer.
        /// </summary>
        public LwHuffmEncoder Encoder
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether compress the LinXForm.
        /// </summary>
        public bool EnableCompress
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the number of bits used to store Bias.
        /// </summary>
        public virtual uint BiasBits
        {
            get { return sizeof(float) * 8; }
        }

        /// <summary>
        /// Gets the number of bits used to store matrix.
        /// </summary>
        public virtual uint MatrixBits
        {
            get { return sizeof(float) * 8; }
        }

        /// <summary>
        /// Gets or sets mean stream numerical statistics.
        /// </summary>
        public StreamStatistic MeanStatistic
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets variance stream quantized numerical statistics.
        /// </summary>
        public StreamStatistic VarStatistic
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether needs quantize.
        /// </summary>
        public bool IsNeedQuantize
        {
            get;
            set;
        }
        
        #endregion

        #region Object factory

        /// <summary>
        /// Create LinXForm serializer.
        /// </summary>
        /// <param name="config">LinXForm configuration.</param>
        /// <returns>LinXForm serializer.</returns>
        public static LinXformSerializer Create(LinXformConfig config)
        {
            Helper.ThrowIfNull(config);

            LinXformSerializer serializer = null;
            Debug.Assert(config.IsFixedPoint == false, "Only float point is supported here");

            if (serializer == null)
            {
                serializer = new LinXformSerializer(config);
            }

            return serializer;
        }

        #endregion

        #region Operations

        /// <summary>
        /// Write out linXforms of one stream.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="meanXform">Transform for Gaussian mean.</param>
        /// <param name="varXform">Transform for Gaussian variance.</param>
        /// <param name="streamOrder">The dynamic order of stream, to which the linXforms belongs.</param>
        /// <returns>Size of bytes written.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public virtual uint Write(DataWriter writer, LinXForm meanXform, LinXForm varXform, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(meanXform);
            Helper.ThrowIfNull(varXform);

            DataWriter orgWriter = writer;
            MemoryStream linXformsBuf = null;
            if (EnableCompress)
            {
                linXformsBuf = new MemoryStream();
                writer = new DataWriter(linXformsBuf);
            }

            uint size = 0;
            if (Config.HasMeanXform)
            {
                MeanStatistic.Put(meanXform, streamOrder);
                size += WriteFourBytesAlignedLinXform(writer, meanXform, streamOrder, Config.HasMeanBias, Config.MeanBandWidth);
            }

            if (Config.HasVarXform)
            {
                VarStatistic.Put(varXform, streamOrder);
                size += WriteFourBytesAlignedLinXform(writer, varXform, streamOrder, Config.HasVarBias);
            }

            if (EnableCompress)
            {
                size = orgWriter.Write(Encoder.Encode(linXformsBuf.ToArray()));
                if (size % sizeof(uint) != 0)
                {
                    size += orgWriter.Write(new byte[sizeof(uint) - (size % sizeof(uint))]);
                }

                writer = orgWriter;
            }

            return size;
        }

        /// <summary>
        /// Read linear transform.
        /// </summary>
        /// <param name="reader">Binary reader to read linear transforms.</param>
        /// <param name="dimension">Dimension of the linear transform to read.</param>
        /// <param name="blockSizes">Size of each linear transform sub matrix to read.</param>
        /// <param name="streamOrder">The dynamic order of current linear transform to read.</param>
        /// <param name="meanXform">Transform for Gaussian mean.</param>
        /// <param name="varXform">Transform for Gaussian variance.</param>
        public virtual void ReadLinXform(BinaryReader reader, int dimension, List<int> blockSizes, DynamicOrder streamOrder, out LinXForm meanXform, out LinXForm varXform)
        {
            Helper.ThrowIfNull(reader);

            long orgPos = reader.BaseStream.Position;
            meanXform = null;
            varXform = null;

            if (Config.HasMeanXform)
            {
                meanXform = ReadFourBytesAlignedLinXform(reader, dimension, blockSizes, streamOrder, Config.HasMeanBias, Config.MeanBandWidth);
            }

            if (Config.HasVarXform)
            {
                varXform = ReadFourBytesAlignedLinXform(reader, dimension, blockSizes, streamOrder, Config.HasVarBias);
            }

            if (Encoder != null)
            {
                // ramp up the encoder
                long size = reader.BaseStream.Position - orgPos;
                reader.BaseStream.Position = orgPos;
                Encoder.WarmUpData(reader.ReadBytes((int)size), 1);
            }
        }

        #endregion

        #region Fixed Point supporting functions

        /// <summary>
        /// Normalize value to the target one.
        /// </summary>
        /// <param name="value">Value to normalize.</param>
        /// <param name="max">Target value.</param>
        /// <returns>Normalized value.</returns>
        protected static double NormalizeToOne(double value, double max)
        {
            Debug.Assert(max != 0.0f, "Not to divide zero.");
            return (double)value / max;
        }

        /// <summary>
        /// Curve the given value into the range of min and max.
        /// </summary>
        /// <param name="min">Minimum value of the range.</param>
        /// <param name="value">Value to curve.</param>
        /// <param name="max">Maximum value of the range.</param>
        /// <returns>Curved value.</returns>
        protected static short Clip(short min, short value, short max)
        {
            short result = Math.Min(value, max);
            result = Math.Max(result, min);
            return result;
        }

        /// <summary>
        /// Curve the given value into the range of min and max.
        /// </summary>
        /// <param name="min">Minimum value of the range.</param>
        /// <param name="value">Value to curve.</param>
        /// <param name="max">Maximum value of the range.</param>
        /// <returns>Curved value.</returns>
        protected static double Clip(int min, double value, int max)
        {
            double result = Math.Min(value, max);
            result = Math.Max(result, min);
            return result;
        }

        #endregion

        #region Protected functions

        /// <summary>
        /// Write out linear transform.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="linXform">Linear transform.</param>
        /// <param name="streamOrder">The dynamic order of stream, to which the linear transform belongs.</param>
        /// <param name="hasBias">Indicate transform whether has bias info.</param>
        /// <param name="bandWidth">Band width of linear transform matrix.</param>
        /// <returns>Size of bytes written.</returns>
        protected virtual uint Write(DataWriter writer, LinXForm linXform, DynamicOrder streamOrder, bool hasBias, uint bandWidth)
        {
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(linXform);

            uint size = 0;

            Debug.Assert(Config.BiasBits == sizeof(float) * 8, "Only 32-bit float value is supported here");
            Debug.Assert(Config.MatrixBits == sizeof(float) * 8, "Only 32-bit float value is supported here");

            for (int i = 0; hasBias && i < linXform.VecSize; i++)
            {
                size += writer.Write(linXform.Bias[i]);
            }

            for (int i = 0; i < linXform.Blocks.Count; i++)
            {
                for (int j = 0; j < linXform.Blocks[i].GetLength(0); j++)
                {
                    for (int k = 0; k < linXform.Blocks[i].GetLength(1); k++)
                    {
                        if (k >= j - bandWidth && k <= j + bandWidth)
                        {
                            size += writer.Write(linXform.Blocks[i][j, k]);
                        }
                    }
                }
            }

            return size;
        }

        /// <summary>
        /// Read linear transform.
        /// </summary>
        /// <param name="reader">Binary reader to read linear transform.</param>
        /// <param name="dimension">Dimension of the linear transform to read.</param>
        /// <param name="blockSizes">Size of each linear transform sub matrix to read.</param>
        /// <param name="streamOrder">The dynamic order of current linear transform to read.</param>
        /// <param name="hasBias">Indicate transform whether has bias info.</param>
        /// <param name="bandWidth">Band width of linear transform matrix.</param>
        /// <returns>Linear transform.</returns>
        protected virtual LinXForm ReadOneLinXform(BinaryReader reader, int dimension, List<int> blockSizes, DynamicOrder streamOrder, bool hasBias, uint bandWidth)
        {
            Helper.ThrowIfNull(reader);
            LinXForm linXform = new LinXForm();
            linXform.VecSize = dimension;

            if (hasBias)
            {
                Debug.Assert(Config.BiasBits == sizeof(float) * 8, "Only 32-bit float value is supported here");
                linXform.Bias = new float[dimension];
                for (int i = 0; i < linXform.VecSize; i++)
                {
                    linXform.Bias[i] = reader.ReadSingle();
                }
            }

            Debug.Assert(Config.MatrixBits == sizeof(float) * 8, "Only 32-bit float value is supported here");
            for (int i = 0; i < blockSizes.Count; i++)
            {
                float[,] block = new float[blockSizes[i], blockSizes[i]];
                for (int j = 0; j < blockSizes[i]; j++)
                {
                    for (int k = 0; k < blockSizes[i]; k++)
                    {
                        if (k < j - bandWidth || k > j + bandWidth)
                        {
                            block[j, k] = 0;
                        }
                        else
                        {
                            block[j, k] = reader.ReadSingle();
                        }
                    }
                }

                linXform.Blocks.Add(block);
            }

            return linXform;
        }

        /// <summary>
        /// Write out one linear transform to be 4-byte aligned.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="linXform">Linear transform to write out.</param>
        /// <param name="streamOrder">Dynamic order of the stream of current linear transform.</param>
        /// <param name="hasBias">Indicate transform whether has bias info.</param>
        /// <param name="bandWidth">Band width of linear transform matrix.</param>
        /// <returns>Number of bytes written out.</returns>
        protected uint WriteFourBytesAlignedLinXform(DataWriter writer, LinXForm linXform, DynamicOrder streamOrder, bool hasBias, uint bandWidth = 0)
        {
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(linXform);
            uint size = Write(writer, linXform, streamOrder, hasBias, bandWidth);

            if (size == 0)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Zero length of linXform is not allowed."));
            }

            // Pad zero bytes if needed to align with 4-bytes
            if (size % sizeof(uint) > 0)
            {
                size += writer.Write(new byte[sizeof(uint) - (size % sizeof(uint))]);
            }

            Debug.Assert(size % sizeof(uint) == 0, "Data should be 4-byte aligned.");
            return size;
        }

        /// <summary>
        /// Read one linear transform from binary reader, which is to be 4-bytes aligned.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <param name="dimension">The number of dimension of the linear transform to read.</param>
        /// <param name="blockSizes">The size of each linear transform sub matrix to read.</param>
        /// <param name="streamOrder">Dynamic stream order of the linear transform belonging to.</param>
        /// <param name="hasBias">Indicate transform whether has bias info.</param>
        /// <param name="bandWidth">Band width of linear transform matrix.</param>
        /// <returns>Retrieved linear transform.</returns>
        protected LinXForm ReadFourBytesAlignedLinXform(BinaryReader reader, int dimension, List<int> blockSizes, DynamicOrder streamOrder, bool hasBias, uint bandWidth = 0)
        {
            Helper.ThrowIfNull(reader);
            long basePosition = reader.BaseStream.Position;
            LinXForm linXform = ReadOneLinXform(reader, dimension, blockSizes, streamOrder, hasBias, bandWidth);

            // Skip padding bytes for 4-byte alignment
            int readSize = (int)(reader.BaseStream.Position - basePosition);
            if (readSize % sizeof(uint) > 0)
            {
                int padByteCount = sizeof(uint) - (readSize % sizeof(uint));
                reader.ReadBytes(padByteCount);
            }

            return linXform;
        }

        #endregion

        /// <summary>
        /// Stream statistics.
        /// </summary>
        public class StreamStatistic
        {
            private Dictionary<DynamicOrder, LinXformStatistic> _linXformStatistics = new Dictionary<DynamicOrder, LinXformStatistic>();

            /// <summary>
            /// Gets LinXForm statistics, indexed by stream's dynamic order.
            /// </summary>
            public Dictionary<DynamicOrder, LinXformStatistic> LinXformStatistics
            {
                get { return _linXformStatistics; }
            }

            /// <summary>
            /// Puts a sample into this list.
            /// </summary>
            /// <param name="linXform">Linear transform.</param>
            /// <param name="streamOrder">Dynamic stream order.</param>
            public void Put(LinXForm linXform, DynamicOrder streamOrder)
            {
                Helper.ThrowIfNull(linXform);
                if (!_linXformStatistics.ContainsKey(streamOrder))
                {
                    _linXformStatistics.Add(streamOrder, new LinXformStatistic(linXform));
                }

                _linXformStatistics[streamOrder].Put(linXform);
            }

            /// <summary>
            /// Converts linXform statistics information to string.
            /// </summary>
            /// <returns>String of linXform statistics.</returns>
            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                foreach (DynamicOrder order in LinXformStatistics.Keys)
                {
                    LinXformStatistic statistic = LinXformStatistics[order];
                    for (int i = 0; i < statistic.Length; i++)
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture, "{0:00} {1:E6} {2:E6}",
                            i, statistic.Bias[i].Min, statistic.Bias[i].Max);
                    }

                    for (int i = 0; i < statistic.Blocks.Count; i++)
                    {
                        UniStatistic[,] staticBlock = statistic.Blocks[i];
                        for (int j = 0; j < staticBlock.GetLength(0); j++)
                        {
                            for (int k = 0; k < staticBlock.GetLength(1); k++)
                            {
                                builder.AppendFormat(CultureInfo.InvariantCulture, "{0:00} {1:E6} {2:E6}",
                                    (j * staticBlock.GetLength(0)) + k, staticBlock[j, k].Min, staticBlock[j, k].Max);
                            }
                        }

                        builder.AppendFormat(CultureInfo.InvariantCulture, "{5}",
                            System.Environment.NewLine);
                    }
                }

                return builder.ToString();
            }
        }
    }

    /// <summary>
    /// LinXForm statistic.
    /// </summary>
    public class LinXformStatistic
    {
        private UniStatistic[] _bias;
        private List<UniStatistic[,]> _blocks;

        /// <summary>
        /// Initializes a new instance of the LinXformStatistic class.
        /// </summary>
        /// <param name="linXform">Linear transform.</param>
        public LinXformStatistic(LinXForm linXform)
        {
            if (linXform.Bias != null)
            {
                _bias = new UniStatistic[linXform.Bias.Length];
                for (int i = 0; i < _bias.Length; i++)
                {
                    _bias[i] = new UniStatistic();
                }
            }

            _blocks = new List<UniStatistic[,]>();
            for (int i = 0; i < linXform.Blocks.Count; i++)
            {
                float[,] block = linXform.Blocks[i];
                UniStatistic[,] matrix = new UniStatistic[block.GetLength(0), block.GetLength(1)];
                for (int j = 0; j < block.GetLength(0); j++)
                {
                    for (int k = 0; k < block.GetLength(1); k++)
                    {
                        matrix[j, k] = new UniStatistic();
                    }
                }

                _blocks.Add(matrix);
            }
        }

        /// <summary>
        /// Gets the statistics information on bias.
        /// </summary>
        public UniStatistic[] Bias
        {
            get { return _bias; }
        }

        /// <summary>
        /// Gets the statistics information on blocks.
        /// </summary>
        public List<UniStatistic[,]> Blocks
        {
            get { return _blocks; }
        }

        /// <summary>
        /// Gets length of the bias.
        /// </summary>
        public int Length
        {
            get { return _bias.Length; }
        }

        /// <summary>
        /// Puts a sample into statistics.
        /// </summary>
        /// <param name="linXform">Linear transform.</param>
        public void Put(LinXForm linXform)
        {
            Helper.ThrowIfNull(linXform);
            for (int i = 0; linXform.Bias != null && i < linXform.Bias.Length; i++)
            {
                Bias[i].Put(linXform.Bias[i]);
            }

            for (int i = 0; i < linXform.Blocks.Count; i++)
            {
                UniStatistic[,] staticBlock = Blocks[i];
                float[,] block = linXform.Blocks[i];
                for (int j = 0; j < linXform.Blocks[i].GetLength(0); j++)
                {
                    for (int k = 0; k < linXform.Blocks[i].GetLength(1); k++)
                    {
                        staticBlock[j, k].Put(block[j, k]);
                    }
                }
            }
        }
    }
}