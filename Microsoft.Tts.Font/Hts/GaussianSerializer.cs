//----------------------------------------------------------------------------
// <copyright file="GaussianSerializer.cs" company="Microsoft">
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
    /// Gaussian serializer.
    /// </summary>
    public class GaussianSerializer
    {
        #region Fields

        public static readonly float DefaultVarianceFloor = (float)1e-010;

        #endregion

        #region Construction
        /// <summary>
        /// Initializes a new instance of the GaussianSerializer class.
        /// </summary>
        protected GaussianSerializer()
        {
            Statistic = new StreamStatistic();
            QuantizedStatistic = new StreamStatistic();
            IsNeedQuantize = true;
        }

        /// <summary>
        /// Initializes a new instance of the GaussianSerializer class.
        /// </summary>
        /// <param name="config">Gaussian configuration.</param>
        protected GaussianSerializer(GaussianConfig config) :
            this()
        {
            Helper.ThrowIfNull(config);
            Config = config;
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the configuration of the gaussian writer.
        /// </summary>
        public GaussianConfig Config
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets encoder of the gaussian writer.
        /// </summary>
        public LwHuffmEncoder Encoder
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether compress the gaussian.
        /// </summary>
        public bool EnableCompress
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the number of bits used to store Mean.
        /// </summary>
        public virtual uint MeanBits
        {
            get { return sizeof(float) * 8; }
        }

        /// <summary>
        /// Gets the number of bits used to store Variance.
        /// </summary>
        public virtual uint VarianceBits
        {
            get { return sizeof(float) * 8; }
        }

        /// <summary>
        /// Gets or sets stream numerical statistics.
        /// </summary>
        public StreamStatistic Statistic
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets stream quantized numerical statistics.
        /// </summary>
        public StreamStatistic QuantizedStatistic
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
        /// Create Gaussian serializer.
        /// </summary>
        /// <param name="config">Gaussian configuration.</param>
        /// <param name="modelType">Model type.</param>
        /// <param name="modelDistribution">Model distribution.</param>
        /// <returns>Gaussian serializer.</returns>
        public static GaussianSerializer Create(GaussianConfig config, HmmModelType modelType,
             ModelDistributionType modelDistribution)
        {
            Helper.ThrowIfNull(config);

            GaussianSerializer serializer = null;
            if (config.IsFixedPoint)
            {
                if (modelType == HmmModelType.Lsp)
                {
                    serializer = new FixedPointLspGaussianSerializer(config);
                }
                else if (modelType == HmmModelType.FundamentalFrequency)
                {
                    serializer = new FixedPointF0GaussianSerializer(config);
                }
                else if (modelType == HmmModelType.Mbe)
                {
                    serializer = new FixedPointMBEGaussianSerializer(config);
                }
            }

            if (serializer == null)
            {
                if (modelDistribution == ModelDistributionType.Msd)
                {
                    serializer = new MultipleSpaceDistributionGaussianSerializer(config);
                }
                else
                {
                    serializer = new GaussianSerializer(config);
                }
            }

            return serializer;
        }

        #endregion

        #region Operations

        /// <summary>
        /// Write out Gaussians of one stream.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="gaussians">Gaussians.</param>
        /// <param name="streamOrder">The dynamic order of stream, to which the Gaussian belongs.</param>
        /// <returns>Size of bytes written.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore.")]
        public virtual uint Write(DataWriter writer, Gaussian[] gaussians, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(gaussians);

            DataWriter orgWriter = writer;
            MemoryStream gaussiansBuf = null;
            if (EnableCompress)
            {
                gaussiansBuf = new MemoryStream();
                writer = new DataWriter(gaussiansBuf);
            }

            uint size = 0;
            for (int i = 0; i < gaussians.Length; i++)
            {
                Statistic.Put(gaussians[i], streamOrder);
                size += WriteFourBytesAlignedGaussian(writer, gaussians[i], streamOrder);
            }

            if (EnableCompress)
            {
                size = orgWriter.Write(Encoder.Encode(gaussiansBuf.ToArray()));
                if (size % sizeof(uint) != 0)
                {
                    size += orgWriter.Write(new byte[sizeof(uint) - (size % sizeof(uint))]);
                }

                writer = orgWriter;
            }

            return size;
        }

        /// <summary>
        /// Read Gaussian distributions.
        /// </summary>
        /// <param name="reader">Binary reader to read Gaussian distributions.</param>
        /// <param name="dimension">Dimension of the Gaussian distribution to read.</param>
        /// <param name="streamOrder">The dynamic order of current Gaussian distribution to read.</param>
        /// <returns>Gaussian distributions.</returns>
        public virtual Gaussian[] ReadGaussians(BinaryReader reader, int dimension, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(reader);

            long orgPos = reader.BaseStream.Position;

            Gaussian[] gaussians = new Gaussian[Config.MixtureCount];
            for (uint i = 0; i < Config.MixtureCount; i++)
            {
                gaussians[i] = ReadFourBytesAlignedGaussian(reader, dimension, streamOrder);
            }

            if (Encoder != null)
            {
                // ramp up the encoder
                long size = reader.BaseStream.Position - orgPos;
                reader.BaseStream.Position = orgPos;
                Encoder.WarmUpData(reader.ReadBytes((int)size), 1);
            }

            return gaussians;
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

        /// <summary>
        /// Calculate inverted variance with given floor for the variance.
        /// </summary>
        /// <param name="variance">Variance value to calculate.</param>
        /// <param name="floor">Variance floor.</param>
        /// <returns>Inverted variance.</returns>
        protected static double CalculateInvertedVariance(double variance, double floor)
        {
            return 1.0d / ((variance < floor) ? floor : variance);
        }

        #endregion

        #region Protected functions

        /// <summary>
        /// Write out Gaussian distribution.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="gaussian">Gaussian.</param>
        /// <param name="streamOrder">The dynamic order of stream, to which the Gaussian belongs.</param>
        /// <returns>Size of bytes written.</returns>
        protected virtual uint Write(DataWriter writer, Gaussian gaussian, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(gaussian);

            uint size = 0;
            if (Config.HasWeight)
            {
                size += writer.Write((float)gaussian.Weight);
            }

            Debug.Assert(Config.MeanBits == sizeof(float) * 8, "Only 32-bit float value is supported here");
            Debug.Assert(Config.VarianceBits == sizeof(float) * 8, "Only 32-bit float value is supported here");

            for (int i = 0; Config.HasMean && i < gaussian.Length; i++)
            {
                Debug.Assert(Config.HasVariance, "Variance is needed to encode mean for runtime.");

                double invVar = CalculateInvertedVariance(gaussian.Variance[i], DefaultVarianceFloor);
                double mean = gaussian.Mean[i] * invVar;
                size += writer.Write((float)mean);
            }

            for (int i = 0; Config.HasVariance && i < gaussian.Length; i++)
            {
                double invVar = CalculateInvertedVariance(gaussian.Variance[i], DefaultVarianceFloor);
                size += writer.Write((float)invVar);
            }

            return size;
        }

        /// <summary>
        /// Read Gaussian distribution.
        /// </summary>
        /// <param name="reader">Binary reader to read Gaussian distributions.</param>
        /// <param name="dimension">Dimension of the Gaussian distribution to read.</param>
        /// <param name="streamOrder">The dynamic order of current Gaussian distribution to read.</param>
        /// <returns>Gaussian distribution.</returns>
        protected virtual Gaussian ReadGaussian(BinaryReader reader, int dimension, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(reader);
            Gaussian gaussian = new Gaussian();
            gaussian.Length = dimension;

            gaussian.Weight = Config.HasWeight ? reader.ReadSingle() : float.NaN;

            if (Config.HasMean)
            {
                if (!Config.HasVariance)
                {
                    throw new InvalidDataException(Helper.NeutralFormat("Variance is needed as Mean depends on that."));
                }

                Debug.Assert(Config.MeanBits == sizeof(float) * 8, "Only 32-bit float value is supported here");
                gaussian.Mean = new double[gaussian.Length];
                for (int i = 0; i < gaussian.Length; i++)
                {
                    gaussian.Mean[i] = reader.ReadSingle();
                }
            }

            if (Config.HasVariance)
            {
                Debug.Assert(Config.VarianceBits == sizeof(float) * 8, "Only 32-bit float value is supported here");
                gaussian.Variance = new double[gaussian.Length];
                for (int i = 0; i < gaussian.Length; i++)
                {
                    gaussian.Variance[i] = reader.ReadSingle();
                    gaussian.Variance[i] = 1.0f / gaussian.Variance[i]; // Revert back
                }
            }

            if (Config.HasMean)
            {
                for (int i = 0; i < gaussian.Length; i++)
                {
                    gaussian.Mean[i] *= gaussian.Variance[i];   // Revert back
                }
            }

            return gaussian;
        }

        /// <summary>
        /// Write out one Gaussian distribution to be 4-byte aligned.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="gaussian">Gaussian distribution to write out.</param>
        /// <param name="streamOrder">Dynamic order of the stream of current Gaussian distribution.</param>
        /// <returns>Number of bytes written out.</returns>
        protected uint WriteFourBytesAlignedGaussian(DataWriter writer, Gaussian gaussian, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(gaussian);
            uint size = Write(writer, gaussian, streamOrder);

            if (size == 0)
            {
                throw new InvalidDataException(Helper.NeutralFormat("Zero length of Gaussian is not allowed."));
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
        /// Read one Gaussian distribution from binary reader, which is to be 4-bytes aligned.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <param name="dimension">The number of dimension of the Gaussian distribution to read.</param>
        /// <param name="streamOrder">Dynamic stream order of the Gaussian distribution belonging to.</param>
        /// <returns>Retrieved Gaussian distribution.</returns>
        protected Gaussian ReadFourBytesAlignedGaussian(BinaryReader reader, int dimension, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(reader);
            long basePosition = reader.BaseStream.Position;
            Gaussian gaussian = ReadGaussian(reader, dimension, streamOrder);

            // Skip padding bytes for 4-byte alignment
            int readSize = (int)(reader.BaseStream.Position - basePosition);
            if (readSize % sizeof(uint) > 0)
            {
                int padByteCount = sizeof(uint) - (readSize % sizeof(uint));
                reader.ReadBytes(padByteCount);
            }

            return gaussian;
        }

        #endregion

        /// <summary>
        /// Stream statistics.
        /// </summary>
        public class StreamStatistic
        {
            private Dictionary<DynamicOrder, GaussianStatistic> _guassianStatistics = new Dictionary<DynamicOrder, GaussianStatistic>();

            /// <summary>
            /// Gets Gaussian statistics, indexed by stream's dynamic order.
            /// </summary>
            public Dictionary<DynamicOrder, GaussianStatistic> GuassianStatistics
            {
                get { return _guassianStatistics; }
            }

            /// <summary>
            /// Puts a sample into this list.
            /// </summary>
            /// <param name="gaussian">Gaussian distribution.</param>
            /// <param name="streamOrder">Dynamic stream order.</param>
            public void Put(Gaussian gaussian, DynamicOrder streamOrder)
            {
                Helper.ThrowIfNull(gaussian);
                if (!GuassianStatistics.ContainsKey(streamOrder))
                {
                    GuassianStatistics.Add(streamOrder, new GaussianStatistic(gaussian.Length));
                }

                GuassianStatistics[streamOrder].Put(gaussian);
            }

            /// <summary>
            /// Converts Gaussian statistics information to string.
            /// </summary>
            /// <returns>String of Gaussian statistics.</returns>
            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                foreach (DynamicOrder order in GuassianStatistics.Keys)
                {
                    GaussianStatistic statistic = GuassianStatistics[order];
                    for (int i = 0; i < statistic.Length; i++)
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture, "{0:00} {1:E6} {2:E6} {3:E6} {4:E6} {5}",
                            i, statistic.Mean[i].Min, statistic.Mean[i].Max,
                            statistic.Variance[i].Min, statistic.Variance[i].Max, System.Environment.NewLine);
                    }
                }

                return builder.ToString();
            }
        }
    }

    /// <summary>
    /// Gaussian serializer for multi-space distribution.
    /// </summary>
    public class MultipleSpaceDistributionGaussianSerializer : GaussianSerializer
    {
        #region Construction
        /// <summary>
        /// Initializes a new instance of the MultipleSpaceDistributionGaussianSerializer class.
        /// </summary>
        /// <param name="config">Gaussian configuration.</param>
        internal MultipleSpaceDistributionGaussianSerializer(GaussianConfig config) :
            base(config)
        {
        }
        #endregion

        /// <summary>
        /// Write out Gaussians of one stream.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="gaussians">Gaussians.</param>
        /// <param name="streamOrder">The dynamic order of stream, to which the Gaussian belongs.</param>
        /// <returns>Size of bytes written.</returns>
        public override uint Write(DataWriter writer, Gaussian[] gaussians, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(gaussians);

            if (gaussians.Count(g => g.Length > 0) != 1)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    "Multi-space distribution with [{0}] non-zero-length mixtures is not supported",
                    gaussians.Count(g => g.Length > 0));
                throw new InvalidDataException(message);
            }

            // Maximum dimension among Gaussian models in this stream
            int maxDimension = gaussians.Max(g => g.Length);

            uint size = 0;
            Debug.Assert(gaussians.Count(g => g.Length == maxDimension) == 1,
                "The number of meaningful MSD mixture number should be 1");
            for (int i = 0; i < gaussians.Length; i++)
            {
                if (gaussians[i].Length == maxDimension)
                {
                    Statistic.Put(gaussians[i], streamOrder);
                    size += WriteFourBytesAlignedGaussian(writer, gaussians[i], streamOrder);
                }
            }

            return size;
        }
    }

    /// <summary>
    /// Gaussian serializer for F0 as fixed point one.
    /// </summary>
    public class FixedPointF0GaussianSerializer : MultipleSpaceDistributionGaussianSerializer
    {
        #region Private fields
        private const int MaxLogF0DownScaleFactor = 7; // e^7 = 1096.6Hz, to curve static/delta/acceleration value into 0~1
        private const int MeanUpScaleAsShortFactor = 32768; // 2^15
        private const int StaticVarianceUpscaleFactor = 4;
        #endregion

        #region Construction
        /// <summary>
        /// Initializes a new instance of the FixedPointF0GaussianSerializer class.
        /// </summary>
        /// <param name="config">Gaussian configuration.</param>
        internal FixedPointF0GaussianSerializer(GaussianConfig config) :
            base(config)
        {
        }
        #endregion

        #region Operations

        /// <summary>
        /// Number of bits used to store Mean.
        /// </summary>
        public override uint MeanBits
        {
            get { return sizeof(short) * 8; }
        }

        /// <summary>
        /// Number of bits used to store Variance.
        /// </summary>
        public override uint VarianceBits
        {
            get { return sizeof(byte) * 8; }
        }

        /// <summary>
        /// Write out Gaussian.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="gaussian">Gaussian.</param>
        /// <param name="streamOrder">The dynamic order of stream, to which the Gaussian belongs.</param>
        /// <returns>Size of bytes written.</returns>
        protected override uint Write(DataWriter writer, Gaussian gaussian, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(gaussian);
            uint size = 0;

            Gaussian quantized = gaussian;
            if (IsNeedQuantize)
            {
                quantized = Quantize(gaussian, streamOrder);
                QuantizedStatistic.Put(quantized, streamOrder);
            }

            if (Config.HasWeight)
            {
                if (quantized.Weight == 0.0f)
                {
                    throw new InvalidDataException(Helper.NeutralFormat("Zero weight of LogF0 is found."));
                }

                size += writer.Write((float)quantized.Weight);
            }

            Debug.Assert(Config.MeanBits == sizeof(short) * 8, "Only 16-bit short value is supported here");
            for (int i = 0; Config.HasMean && i < gaussian.Length; i++)
            {
                size += writer.Write((short)quantized.Mean[i]);
            }

            Debug.Assert(Config.VarianceBits == sizeof(byte) * 8, "Only 8-bit byte value is supported here");
            for (int i = 0; Config.HasVariance && i < gaussian.Length; i++)
            {
                size += writer.Write((byte)quantized.Variance[i]);
            }

            return size;
        }

        /// <summary>
        /// Read Gaussian distribution.
        /// </summary>
        /// <param name="reader">Binary reader to read Gaussian distributions.</param>
        /// <param name="dimension">Dimension of the Gaussian distribution to read.</param>
        /// <param name="streamOrder">The dynamic order of current Gaussian distribution to read.</param>
        /// <returns>Gaussian distribution.</returns>
        protected override Gaussian ReadGaussian(BinaryReader reader, int dimension, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(reader);
            Gaussian gaussian = new Gaussian();
            gaussian.Length = dimension;

            gaussian.Weight = Config.HasWeight ? reader.ReadSingle() : float.NaN;

            Debug.Assert(Config.MeanBits == sizeof(short) * 8, "Only 16-bit short value is supported here");
            gaussian.Mean = new double[gaussian.Length];

            for (int i = 0; i < gaussian.Length; i++)
            {
                gaussian.Mean[i] = reader.ReadInt16();
            }

            Debug.Assert(Config.VarianceBits == sizeof(byte) * 8, "Only 8-bit byte value is supported here");
            gaussian.Variance = new double[gaussian.Length];

            for (int i = 0; Config.HasVariance && i < gaussian.Length; i++)
            {
                gaussian.Variance[i] = (int)reader.ReadByte();
            }

            if (IsNeedQuantize)
            {
                gaussian = Dequantize(gaussian, streamOrder);
            }

            return gaussian;
        }

        #endregion

        #region Quantization

        /// <summary>
        /// Quantize means and variances in Gaussian distribution into value range of fixed point numerical representation.
        /// </summary>
        /// <param name="gaussian">Gaussian distribution to quantize.</param>
        /// <param name="streamOrder">Stream order of current Gaussian distribution to quantize.</param>
        /// <returns>Quantized Gaussian distribution.</returns>
        private Gaussian Quantize(Gaussian gaussian, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(gaussian);
            Gaussian quantized = new Gaussian(gaussian.Weight, gaussian.Length);

            for (int i = 0; Config.HasMean && i < gaussian.Length; i++)
            {
                double mean = (double)gaussian.Mean[i] / MaxLogF0DownScaleFactor * MeanUpScaleAsShortFactor;
                quantized.Mean[i] = (short)Clip(short.MinValue, Math.Round(mean), short.MaxValue);
            }

            Debug.Assert(Config.VarianceBits == sizeof(byte) * 8, "Only 8-bit byte value is supported here");
            for (int i = 0; Config.HasVariance && i < gaussian.Length; i++)
            {
                double invVar = CalculateInvertedVariance(gaussian.Variance[i], DefaultVarianceFloor);
                if (streamOrder != DynamicOrder.Static && i >= Config.StaticVectorSize)
                {
                    // Treat non-static features differently as it has bigger range
                    invVar /= byte.MaxValue;
                }
                else
                {
                    invVar *= StaticVarianceUpscaleFactor;
                }

                invVar = Math.Sqrt(invVar);
                quantized.Variance[i] = (byte)Clip(1, Math.Round(invVar), byte.MaxValue);
                Debug.Assert(quantized.Variance[i] >= 1);
            }

            return quantized;
        }

        /// <summary>
        /// De-quantize Gaussian distribution of LogF0 model.
        /// </summary>
        /// <param name="gaussian">Gaussian distribution to de-quantize.</param>
        /// <param name="streamOrder">The dynamic order of current Gaussian distribution to read.</param>
        /// <returns>De-quantized Gaussian distribution.</returns>
        private Gaussian Dequantize(Gaussian gaussian, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(gaussian);
            Gaussian result = new Gaussian(gaussian.Weight, gaussian.Length);
            Debug.Assert(Config.MeanBits == sizeof(short) * 8, "Only 16-bit short value is supported here");
            for (int i = 0; i < gaussian.Length; i++)
            {
                result.Mean[i] = (double)(gaussian.Mean[i] * MaxLogF0DownScaleFactor) / MeanUpScaleAsShortFactor;
            }

            Debug.Assert(Config.VarianceBits == sizeof(byte) * 8, "Only 8-bit byte value is supported here");
            for (int i = 0; Config.HasVariance && i < gaussian.Length; i++)
            {
                double value = (int)gaussian.Variance[i];
                if (i >= Config.StaticVectorSize && streamOrder != DynamicOrder.Static)
                {
                    value = (double)(value * value);
                    value *= byte.MaxValue;
                }

                result.Variance[i] = 1.0f / value;
            }

            return result;
        }

        #endregion
    }

    /// <summary>
    /// Gaussian serializer for LSP and Gain as fixed point one.
    /// </summary>
    public class FixedPointLspGaussianSerializer : GaussianSerializer
    {
        #region Private const fields
        private const int LsfDownScaleFactor = 100;
        private const int GainUpScaleFactor = 100;
        private const int GainMaximumMean = 10;  // e^MeanRange as maximum value of gain, data is in log
        #endregion

        #region Private fields
        private int _staticLsfMeanDownScaleTo256Factor = 4;    // 40D, 8.0 for other LSP order
        private int _nonStaticLsfMeanDownScaleFactor = 2;    // 40D, 8.0 for other LSP order
        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the FixedPointLspGaussianSerializer class.
        /// </summary>
        /// <param name="config">Gaussian configuration.</param>
        internal FixedPointLspGaussianSerializer(GaussianConfig config) :
            base(config)
        {
            Helper.ThrowIfNull(config);

            // 32768, quantize delta (LSF[i+1]-LSP[i]) to 256, LSF (0, 0.5), so delta will depend on dimension count
            // 4.0 if dimension >= 40D
            // 8.0 if dimension < 40D
            // Maximum range of LSF (16384 -> 0.5)
            // 1024 = (256 * 4), maximum delta of LSF should be 1/16 of 0.5 in term of 40D
            // 256 is what to quantize
            // 1 is the smallest difference, to make sure the stabilize of the LPC filter.
            if (Config.StaticVectorSize >= 40)
            {
                _staticLsfMeanDownScaleTo256Factor = 4;
                _nonStaticLsfMeanDownScaleFactor = 2;
            }
            else
            {
                _staticLsfMeanDownScaleTo256Factor = 8;
                _nonStaticLsfMeanDownScaleFactor = 4;
            }
        }

        #endregion

        #region Operations

        /// <summary>
        /// Number of bits used to store Mean.
        /// </summary>
        public override uint MeanBits
        {
            get { return sizeof(byte) * 8; }
        }

        /// <summary>
        /// Number of bits used to store Variance.
        /// </summary>
        public override uint VarianceBits
        {
            get { return sizeof(byte) * 8; }
        }

        /// <summary>
        /// Write out Gaussian.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="gaussian">Gaussian.</param>
        /// <param name="streamOrder">The dynamic order of stream, to which the Gaussian belongs.</param>
        /// <returns>Size of bytes written.</returns>
        protected override uint Write(DataWriter writer, Gaussian gaussian, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(gaussian);
            uint size = 0;

            Gaussian quantized = gaussian;
            if (IsNeedQuantize)
            {
                quantized = Quantize(gaussian);
                QuantizedStatistic.Put(quantized, streamOrder);
            }

            if (Config.HasWeight)
            {
                size += writer.Write((float)quantized.Weight);
            }

            if (!Config.HasMean || !Config.HasVariance)
            {
                throw new InvalidDataException("Needs both mean and variance.");
            }

            for (int i = 0; i < quantized.Length; i++)
            {
                if ((i + 1) % Config.StaticVectorSize == 0)
                {
                    size += writer.Write((short)quantized.Mean[i]);
                }
                else if (i < Config.StaticVectorSize)
                {
                    size += writer.Write((byte)quantized.Mean[i]);
                }
                else
                {
                    size += writer.Write((sbyte)quantized.Mean[i]);
                }
            }

            for (int i = 0; i < gaussian.Length; i++)
            {
                size += writer.Write((byte)quantized.Variance[i]);
            }

            return size;
        }

        /// <summary>
        /// Read Gaussian distribution.
        /// </summary>
        /// <param name="reader">Binary reader to read Gaussian distributions.</param>
        /// <param name="dimension">Dimension of the Gaussian distribution to read.</param>
        /// <param name="streamOrder">The dynamic order of current Gaussian distribution to read.</param>
        /// <returns>Gaussian distribution.</returns>
        protected override Gaussian ReadGaussian(BinaryReader reader, int dimension, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(reader);
            Gaussian gaussian = new Gaussian();
            gaussian.Length = dimension;
            gaussian.Weight = Config.HasWeight ? reader.ReadSingle() : float.NaN;

            Debug.Assert(Config.MeanBits == sizeof(byte) * 8, "Only 8-bit byte value is supported here");
            gaussian.Mean = new double[gaussian.Length];
            for (int i = 0; i < gaussian.Length; i++)
            {
                if ((i + 1) % Config.StaticVectorSize == 0)
                {
                    // Gain
                    gaussian.Mean[i] = reader.ReadInt16();
                }
                else if (i < Config.StaticVectorSize)
                {
                    gaussian.Mean[i] = reader.ReadByte();
                }
                else
                {
                    gaussian.Mean[i] = reader.ReadSByte();
                }
            }

            Debug.Assert(Config.VarianceBits == sizeof(byte) * 8, "Only 8-bit byte value is supported here");
            gaussian.Variance = new double[gaussian.Length];
            for (int i = 0; i < gaussian.Length; i++)
            {
                gaussian.Variance[i] = reader.ReadByte();
            }

            if (IsNeedQuantize)
            {
                gaussian = Dequantize(gaussian, streamOrder);
            }

            return gaussian;
        }

        #endregion

        #region Quantization

        /// <summary>
        /// Quantize means and variances in Gaussian distribution into value range of fixed point numerical representation.
        /// </summary>
        /// <param name="gaussian">Gaussian distribution to quantize.</param>
        /// <returns>Quantized Gaussian distribution.</returns>
        private Gaussian Quantize(Gaussian gaussian)
        {
            Helper.ThrowIfNull(gaussian);
            Gaussian quantized = new Gaussian();
            quantized.Weight = gaussian.Weight;
            quantized.Length = gaussian.Length;
            quantized.Variance = new double[gaussian.Length];
            quantized.Mean = new double[gaussian.Length];

            short lastQuantizedMean = 0;
            for (int i = 0; i < gaussian.Length; i++)
            {
                if ((i + 1) % Config.StaticVectorSize == 0)
                {
                    // Gain
                    double mean = NormalizeToOne(gaussian.Mean[i], GainMaximumMean) * short.MaxValue;
                    mean = Clip(short.MinValue, Math.Round(mean), short.MaxValue);
                    quantized.Mean[i] = (short)mean;
                }
                else if (i < Config.StaticVectorSize)
                {
                    // Quantize the delta of LSF for static dimensions
                    double quantizedMean = gaussian.Mean[i] * short.MaxValue;
                    quantizedMean /= _staticLsfMeanDownScaleTo256Factor;
                    short deltaOfTwoLsf = (short)Clip((short)1, Math.Round(quantizedMean - lastQuantizedMean), (short)byte.MaxValue);
                    lastQuantizedMean = (short)(lastQuantizedMean + deltaOfTwoLsf);
                    quantized.Mean[i] = (byte)deltaOfTwoLsf;
                }
                else
                {
                    // Quantize LSF for non-static dimensions
                    double quantizedMean = gaussian.Mean[i] * short.MaxValue / _nonStaticLsfMeanDownScaleFactor;
                    quantizedMean = (float)Clip(sbyte.MinValue, Math.Round(quantizedMean), sbyte.MaxValue);
                    quantized.Mean[i] = (sbyte)quantizedMean;
                }
            }

            for (int i = 0; i < gaussian.Length; i++)
            {
                double invVar = CalculateInvertedVariance(gaussian.Variance[i], DefaultVarianceFloor);
                if ((i + 1) % Config.StaticVectorSize == 0)
                {
                    // Gain
                    invVar *= GainUpScaleFactor;
                }
                else
                {
                    invVar /= LsfDownScaleFactor;
                }

                if (i >= Config.StaticVectorSize)
                {
                    invVar /= byte.MaxValue;
                }

                invVar = Clip(1, Math.Round(Math.Sqrt(invVar)), byte.MaxValue);
                Debug.Assert((byte)invVar > 0);
                quantized.Variance[i] = (byte)invVar;
            }

            return quantized;
        }

        /// <summary>
        /// De-quantize Gaussian distribution of LSP model.
        /// </summary>
        /// <param name="gaussian">Gaussian distribution to de-quantize.</param>
        /// <param name="streamOrder">The dynamic order of current Gaussian distribution to read.</param>
        /// <returns>De-quantized Gaussian distribution.</returns>
        private Gaussian Dequantize(Gaussian gaussian, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(gaussian);
            Gaussian result = new Gaussian();
            result.Length = gaussian.Length;
            result.Weight = gaussian.Weight;
            result.Mean = new double[gaussian.Length];
            result.Variance = new double[gaussian.Length];

            Debug.Assert(Config.MeanBits == sizeof(byte) * 8, "Only 8-bit byte value is supported here");
            double accumulatedMean = 0.0f;
            for (int i = 0; i < gaussian.Length; i++)
            {
                if ((i + 1) % Config.StaticVectorSize == 0)
                {
                    // Gain
                    result.Mean[i] = (double)(gaussian.Mean[i] * GainMaximumMean) / short.MaxValue;
                }
                else if (i < Config.StaticVectorSize)
                {
                    accumulatedMean += (double)(gaussian.Mean[i] * _staticLsfMeanDownScaleTo256Factor) / short.MaxValue;
                    result.Mean[i] = accumulatedMean;
                }
                else
                {
                    result.Mean[i] = (double)(gaussian.Mean[i] * _nonStaticLsfMeanDownScaleFactor) / short.MaxValue;
                }
            }

            Debug.Assert(Config.VarianceBits == sizeof(byte) * 8, "Only 8-bit byte value is supported here");
            for (int i = 0; i < gaussian.Length; i++)
            {
                double value = (int)gaussian.Variance[i];
                value = (double)(value * value);
                if (i >= Config.StaticVectorSize)
                {
                    value *= byte.MaxValue;
                }

                if ((i + 1) % Config.StaticVectorSize == 0)
                {
                    // Gain
                    value /= GainUpScaleFactor;
                }
                else
                {
                    value *= LsfDownScaleFactor;
                }

                result.Variance[i] = 1.0f / value;
            }

            return result;
        }

        #endregion
    }

    /// <summary>
    /// Gaussian serializer for MBE as fixed point one.
    /// </summary>
    public class FixedPointMBEGaussianSerializer : MultipleSpaceDistributionGaussianSerializer
    {
        #region Private fields
        private const int MeanUpScaleAsShortFactor = 1 << 15;
        private const int NonStaticDownScaleFactor = 1 << 4;

        #endregion

        #region Construction
        /// <summary>
        /// Initializes a new instance of the FixedPointMBEGaussianSerializer class.
        /// </summary>
        /// <param name="config">Gaussian configuration.</param>
        internal FixedPointMBEGaussianSerializer(GaussianConfig config) :
            base(config)
        {
        }
        #endregion

        #region Operations

        /// <summary>
        /// Number of bits used to store Mean.
        /// </summary>
        public override uint MeanBits
        {
            get { return sizeof(short) * 8; }
        }

        /// <summary>
        /// Number of bits used to store Variance.
        /// </summary>
        public override uint VarianceBits
        {
            get { return sizeof(byte) * 8; }
        }

        /// <summary>
        /// Write out Gaussian.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        /// <param name="gaussian">Gaussian.</param>
        /// <param name="streamOrder">The dynamic order of stream, to which the Gaussian belongs.</param>
        /// <returns>Size of bytes written.</returns>
        protected override uint Write(DataWriter writer, Gaussian gaussian, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(writer);
            Helper.ThrowIfNull(gaussian);
            uint size = 0;

            Gaussian quantized = gaussian;
            if (IsNeedQuantize)
            {
                quantized = Quantize(gaussian, streamOrder);
                QuantizedStatistic.Put(quantized, streamOrder);
            }

            Debug.Assert(Config.MeanBits == sizeof(short) * 8, "Only 16-bit short value is supported here");
            for (int i = 0; Config.HasMean && i < gaussian.Length; i++)
            {
                size += writer.Write((short)quantized.Mean[i]);
            }

            Debug.Assert(Config.VarianceBits == sizeof(byte) * 8, "Only 8-bit byte value is supported here");
            for (int i = 0; Config.HasVariance && i < gaussian.Length; i++)
            {
                size += writer.Write((byte)quantized.Variance[i]);
            }

            return size;
        }

        /// <summary>
        /// Read Gaussian distribution.
        /// </summary>
        /// <param name="reader">Binary reader to read Gaussian distributions.</param>
        /// <param name="dimension">Dimension of the Gaussian distribution to read.</param>
        /// <param name="streamOrder">The dynamic order of current Gaussian distribution to read.</param>
        /// <returns>Gaussian distribution.</returns>
        protected override Gaussian ReadGaussian(BinaryReader reader, int dimension, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(reader);
            Gaussian gaussian = new Gaussian();
            gaussian.Length = dimension;

            Debug.Assert(Config.MeanBits == sizeof(short) * 8, "Only 16-bit short value is supported here");
            gaussian.Mean = new double[gaussian.Length];
            for (int i = 0; i < gaussian.Length; i++)
            {
                gaussian.Mean[i] = reader.ReadInt16();
            }

            Debug.Assert(Config.VarianceBits == sizeof(byte) * 8, "Only 8-bit byte value is supported here");
            gaussian.Variance = new double[gaussian.Length];
            for (int i = 0; Config.HasVariance && i < gaussian.Length; i++)
            {
                gaussian.Variance[i] = (int)reader.ReadByte();
            }

            if (IsNeedQuantize)
            {
                gaussian = Dequantize(gaussian, streamOrder);
            }

            return gaussian;
        }

        #endregion

        #region Quantization

        /// <summary>
        /// Quantize means and variances in Gaussian distribution into value range of fixed point numerical representation.
        /// </summary>
        /// <param name="gaussian">Gaussian distribution to quantize.</param>
        /// <param name="streamOrder">Stream order of current Gaussian distribution to quantize.</param>
        /// <returns>Quantized Gaussian distribution.</returns>
        private Gaussian Quantize(Gaussian gaussian, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(gaussian);
            Gaussian quantized = new Gaussian(gaussian.Weight, gaussian.Length);

            for (int i = 0; Config.HasMean && i < gaussian.Length; i++)
            {
                double mean = (double)gaussian.Mean[i] * MeanUpScaleAsShortFactor;
                quantized.Mean[i] = (short)Clip(short.MinValue, Math.Round(mean), short.MaxValue);
            }

            Debug.Assert(Config.VarianceBits == sizeof(byte) * 8, "Only 8-bit byte value is supported here");
            for (int i = 0; Config.HasVariance && i < gaussian.Length; i++)
            {
                double invVar = CalculateInvertedVariance(gaussian.Variance[i], DefaultVarianceFloor);
                invVar = Math.Sqrt(invVar);
                if (streamOrder != DynamicOrder.Static && i >= Config.StaticVectorSize)
                {
                    // Treat non-static features differently as it has bigger range
                    invVar /= NonStaticDownScaleFactor;
                }

                quantized.Variance[i] = (byte)Clip(1, Math.Round(invVar), byte.MaxValue);
                Debug.Assert(quantized.Variance[i] >= 1);
            }

            return quantized;
        }

        /// <summary>
        /// De-quantize Gaussian distribution of MBE model.
        /// </summary>
        /// <param name="gaussian">Gaussian distribution to de-quantize.</param>
        /// <param name="streamOrder">The dynamic order of current Gaussian distribution to read.</param>
        /// <returns>De-quantized Gaussian distribution.</returns>
        private Gaussian Dequantize(Gaussian gaussian, DynamicOrder streamOrder)
        {
            Helper.ThrowIfNull(gaussian);
            Gaussian result = new Gaussian(gaussian.Weight, gaussian.Length);
            Debug.Assert(Config.MeanBits == sizeof(short) * 8, "Only 16-bit short value is supported here");
            for (int i = 0; i < gaussian.Length; i++)
            {
                result.Mean[i] = (double)(gaussian.Mean[i] / MeanUpScaleAsShortFactor);
            }

            Debug.Assert(Config.VarianceBits == sizeof(byte) * 8, "Only 8-bit byte value is supported here");
            for (int i = 0; Config.HasVariance && i < gaussian.Length; i++)
            {
                double value = (int)gaussian.Variance[i];
                value *= value;
                if (streamOrder != DynamicOrder.Static && i >= Config.StaticVectorSize)
                {
                    value *= NonStaticDownScaleFactor;
                }

                result.Variance[i] = 1.0f / value;
            }

            return result;
        }

        #endregion
    }

    /// <summary>
    /// Uni-statistic class.
    /// </summary>
    public class UniStatistic
    {
        private double _max = double.MinValue;
        private double _min = double.MaxValue;

        /// <summary>
        /// Gets maximum value of this list.
        /// </summary>
        public double Max
        {
            get { return _max; }
        }

        /// <summary>
        /// Gets minimum value of this list.
        /// </summary>
        public double Min
        {
            get { return _min; }
        }

        /// <summary>
        /// Put a sample into this statistic list.
        /// </summary>
        /// <param name="value">Value of the sample.</param>
        public void Put(double value)
        {
            _max = Math.Max(value, _max);
            _min = Math.Min(value, _min);
        }
    }

    /// <summary>
    /// Gaussian statistic.
    /// </summary>
    public class GaussianStatistic
    {
        private UniStatistic[] _mean;
        private UniStatistic[] _variance;

        /// <summary>
        /// Initializes a new instance of the GaussianStatistic class.
        /// </summary>
        /// <param name="length">Length of the Gaussian distribution.</param>
        public GaussianStatistic(int length)
        {
            _mean = new UniStatistic[length];
            _variance = new UniStatistic[length];
            for (int i = 0; i < length; i++)
            {
                Mean[i] = new UniStatistic();
                Variance[i] = new UniStatistic();
            }
        }

        /// <summary>
        /// Gets the statistics information on Mean.
        /// </summary>
        public UniStatistic[] Mean
        {
            get { return _mean; }
        }

        /// <summary>
        /// Gets the statistics information on Variance.
        /// </summary>
        public UniStatistic[] Variance
        {
            get { return _variance; }
        }

        /// <summary>
        /// Gets length of the Gaussian distribution.
        /// </summary>
        public int Length
        {
            get { return Mean.Length; }
        }

        /// <summary>
        /// Puts a sample into statistics.
        /// </summary>
        /// <param name="gaussian">Gaussian distribution.</param>
        public void Put(Gaussian gaussian)
        {
            Helper.ThrowIfNull(gaussian);
            for (int i = 0; i < gaussian.Length; i++)
            {
                Mean[i].Put(gaussian.Mean[i]);
                Variance[i].Put(gaussian.Variance[i]);
            }
        }
    }
}