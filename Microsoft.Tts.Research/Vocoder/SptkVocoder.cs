//----------------------------------------------------------------------------
// <copyright file="SptkVocoder.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements the SPTK vocoder class
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Research
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Security.Permissions;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// SPTK vocoder, call sptk tools to generate wave from f0 and lsp.
    /// SPTK tool dependencies: 1. excite.exe, 2. lspdf.exe, 3. cygwin1.dll.
    /// </summary>
    public class SptkVocoder : IVocoder
    {
        private const float MinF0Value = 1e-3F;

        private string _sptkPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="SptkVocoder"/> class.
        /// </summary>
        /// <param name="sptkPath">Path where SPTK tools are stored.</param>
        public SptkVocoder(string sptkPath)
        {
            if (!Directory.Exists(sptkPath))
            {
                throw new ArgumentException("Directory of SPTK tools doesn't exist.", "sptkPath");
            }

            _sptkPath = sptkPath;
        }

        /// <summary>
        /// Using f0, gain and lsp vector to generate speech data.
        /// </summary>
        /// <param name="f0Vector">F0 vector, value in linear Hz domain.</param>
        /// <param name="lspVector">LSP vector, vlaue in interval [0, 0.5).</param>
        /// <param name="gainVector">Gain vector.</param>
        /// <param name="samplingRate">Sampling rate.</param>
        /// <param name="framePeriod">Frame period, in seconds.</param>
        /// <returns>Resultant wave data, in short.</returns>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        public short[] LspExcite(float[] f0Vector, float[,] lspVector, float[] gainVector,
            int samplingRate, float framePeriod)
        {
            const string ExciteName = "excite.exe";
            string excitePath = Path.Combine(_sptkPath, ExciteName);
            if (!File.Exists(excitePath))
            {
                string message = string.Format(CultureInfo.InvariantCulture, "{0} is not found in the directory {1}.", ExciteName, _sptkPath);
                throw new FileNotFoundException(message);
            }

            const string LspdfName = "lspdf.exe";
            string lspdfPath = Path.Combine(_sptkPath, LspdfName);
            if (!File.Exists(lspdfPath))
            {
                string message = string.Format(CultureInfo.InvariantCulture, "{0} is not found in the directory {1}.", LspdfName, _sptkPath);
                throw new FileNotFoundException(message);
            }

            int sampleCount = f0Vector.GetLength(0);
            if (sampleCount != lspVector.GetLength(0))
            {
                string message = string.Format(CultureInfo.InvariantCulture, "SampleCount between f0Vector and lspVector don't match.");
                throw new ArgumentException(message);
            }

            if (sampleCount != gainVector.GetLength(0))
            {
                string message = string.Format(CultureInfo.InvariantCulture, "SampleCount between f0Vector and gainVector don't match.");
                throw new ArgumentException(message);
            }

            string f0File = WriteToF0File(f0Vector, samplingRate);
            string lspFile = WriteToLspFile(lspVector, gainVector);

            string exciteFile = Path.GetTempFileName();
            string synthesisFile = Path.GetTempFileName();

            string arguments;
            string error;
            bool succeeded;

            int frameSamples = (int)Math.Round(samplingRate * framePeriod);
            int lspOrder = lspVector.GetLength(1);

            arguments = string.Format(CultureInfo.InvariantCulture, "-p {0}", frameSamples);
            succeeded = CommandLineExecute.Execute(excitePath, arguments, f0File, exciteFile, out error);

            arguments = string.Format(CultureInfo.InvariantCulture, "-m {0} -p {1} \"{2}\"", lspOrder, frameSamples, lspFile);
            succeeded = CommandLineExecute.Execute(lspdfPath, arguments, exciteFile, synthesisFile, out error);

            short[] waveData = LoadInt16DataFromSingleFile(synthesisFile);

            Helper.SafeDelete(f0File);
            Helper.SafeDelete(lspFile);
            Helper.SafeDelete(exciteFile);
            Helper.SafeDelete(synthesisFile);

            return waveData;
        }

        private static string WriteToF0File(float[] f0Vector, int samplingRate)
        {
            string fileName = Path.GetTempFileName();

            FileStream stream = new FileStream(fileName, FileMode.Create);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    stream = null;
                    for (int i = 0; i < f0Vector.GetLength(0); i++)
                    {
                        if (f0Vector[i] <= MinF0Value)
                        {
                            writer.Write(0F);
                        }
                        else
                        {
                            writer.Write((float)Math.Round(samplingRate / f0Vector[i]));
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

            return fileName;
        }

        [SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", Justification = "Ignore.")]
        private static string WriteToLspFile(float[,] lspVector, float[] gainVector)
        {
            Debug.Assert(lspVector.GetLength(0) == gainVector.GetLength(0));

            string fileName = Path.GetTempFileName();

            FileStream stream = new FileStream(fileName, FileMode.Create);
            try
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    stream = null;
                    for (int i = 0; i < gainVector.GetLength(0); i++)
                    {
                        writer.Write(gainVector[i]);
                        for (int j = 0; j < lspVector.GetLength(1); j++)
                        {
                            writer.Write((float)(lspVector[i, j] * Math.PI * 2));
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

            return fileName;
        }

        private static short[] LoadInt16DataFromSingleFile(string fileName)
        {
            Debug.Assert(File.Exists(fileName));

            short[] buffer = null;

            FileStream stream = new FileStream(fileName, FileMode.Open);
            try
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    stream = null;
                    int fileLength = (int)stream.Length;
                    Debug.Assert(fileLength % sizeof(float) == 0);

                    int dataCount = fileLength / sizeof(float);
                    buffer = new short[dataCount];

                    for (int i = 0; i < dataCount; i++)
                    {
                        float value = reader.ReadSingle();
                        buffer[i] = (short)Math.Round(value);
                    }

                    Debug.Assert(stream.Position == fileLength);
                }
            }
            finally
            {
                if (null != stream)
                {
                    stream.Dispose();
                }
            }

            return buffer;
        }
    }
}