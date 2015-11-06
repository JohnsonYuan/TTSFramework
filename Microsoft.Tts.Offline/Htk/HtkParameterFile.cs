//----------------------------------------------------------------------------
// <copyright file="HtkParameterFile.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements HTS parameter file
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Htk
{
    using System;
    using System.Data;
    using System.IO;
    using System.Text;
    using Utility;

    /// <summary>
    /// The parameter kinds.
    /// </summary>
    public enum ParameterKinds
    {
        /// <summary>
        /// Sampled waveform.
        /// </summary>
        WaveForm = 0,

        /// <summary>
        /// Linear prediction filter coefficients.
        /// </summary>
        Lpc = 1,

        /// <summary>
        /// Linear prediction reflection coefficients.
        /// </summary>
        LpcReflection = 2,

        /// <summary>
        /// LPC cepstral coefficients.
        /// </summary>
        LpcCepstral = 3,

        /// <summary>
        /// LPC cepstral plus delta coefficients.
        /// </summary>
        LpcCepstralWithDela = 4,

        /// <summary>
        /// LPC reflection coefficients in 16 bit integer.
        /// </summary>
        LpcReflection16Bit = 5,

        /// <summary>
        /// Mel-frequncy cepstral coefficients.
        /// </summary>
        Mfcc = 6,

        /// <summary>
        /// Log mel-filter bank channel outpus.
        /// </summary>
        LogMelBankChannel = 7,

        /// <summary>
        /// Linear mel-filter bank channel outputs.
        /// </summary>
        LinearMelBankChannel = 8,

        /// <summary>
        /// User defined sample kind.
        /// </summary>
        UserDefined = 9,

        /// <summary>
        /// Vector quantised data.
        /// </summary>
        Discrete = 10,
    }

    /// <summary>
    /// The HTK format parameter file, which consist of a contiguous sequance of samples preceded by a header.
    /// </summary>
    public class HtkParameterFile
    {
        /// <summary>
        /// Gets number of samples in file.
        /// </summary>
        public int SampleCount
        {
            get
            {
                return Data.Length;
            }
        }

        /// <summary>
        /// Gets or sets sample period in 100ns units.
        /// </summary>
        public int SamplePeriod { get; set; }

        /// <summary>
        /// Gets number of bytes per sample.
        /// </summary>
        public short SampleSize
        {
            get
            {
                return (short)(Data[0].Length * 4);
            }
        }

        /// <summary>
        /// Gets or sets a code indicating the sample kind.
        /// </summary>
        public ParameterKinds ParameterKind { get; set; }

        /// <summary>
        /// Gets or sets the sample period in second units.
        /// </summary>
        public float SamplePeriodInSecond
        {
            get
            {
                return (float)(SamplePeriod / 10000000.0f);
            }

            set
            {
                SamplePeriod = (int)(value * 10000000.0);
            }
        }

        /// <summary>
        /// Gets or sets the data in the parameter file.
        /// </summary>
        public float[][] Data { get; set; }

        /// <summary>
        /// Loads data file file.
        /// </summary>
        /// <param name="file">The given file name.</param>
        public void Load(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("file");
            }

            FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read);
            try
            {
                using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII))
                {
                    stream = null;
                    int sampleCount = reader.ReadInt32();
                    SamplePeriod = reader.ReadInt32();
                    short sampleSize = reader.ReadInt16();
                    ParameterKind = (ParameterKinds)reader.ReadInt16();
                    if (ParameterKind != ParameterKinds.UserDefined)
                    {
                        throw new NotSupportedException("Only user defined type supported now");
                    }

                    try
                    {
                        Data = new float[sampleCount][];
                        for (int i = 0; i < Data.Length; ++i)
                        {
                            Data[i] = new float[sampleSize];
                            for (int j = 0; j < Data[i].Length; ++j)
                            {
                                Data[i][j] = reader.ReadSingle();
                            }
                        }
                    }
                    catch (IOException e)
                    {
                        throw new InvalidDataException(
                            Helper.NeutralFormat("Exception thrown when read file \"{0}\"", file), e);
                    }
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
        /// Saves data into file.
        /// </summary>
        /// <param name="file">The given file name.</param>
        public void Save(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("file");
            }

            if (Data == null)
            {
                throw new NoNullAllowedException("Data");
            }

            FileStream stream = new FileStream(file, FileMode.Create, FileAccess.Write);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII))
                {
                    stream = null;
                    writer.Write(SampleCount);
                    writer.Write(SamplePeriod);
                    writer.Write(SampleSize);
                    writer.Write((short)ParameterKind);

                    for (int i = 0; i < Data.Length; ++i)
                    {
                        for (int j = 0; j < Data[i].Length; ++j)
                        {
                            writer.Write(Data[i][j]);
                        }
                    }
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
    }
}